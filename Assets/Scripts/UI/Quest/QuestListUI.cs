using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// クエストリスト全体のUI制御クラス
/// 責任範囲：
/// - クエストリスト全体の表示制御
/// - クエストスロットの動的生成・削除
/// - ScrollRectによる一覧スクロール機能
/// - 詳細パネルの表示/非表示制御
/// - 決定・戻るボタンの制御
/// </summary>
public class QuestListUI : MonoBehaviour
{
    [Header("クエストリスト表示")]
    [SerializeField] private ScrollRect questScrollRect;
    [SerializeField] private Transform questListParent;
    [SerializeField] private GameObject questSlotPrefab;

    [Header("クエスト詳細表示")]
    [SerializeField] private GameObject questDetailPanel;
    [SerializeField] private QuestDetailUI questDetailUI;

    [Header("制御ボタン")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button backButton;

    [Header("状態表示")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;

    // イベント
    public event Action<int> OnQuestSelected;
    public event Action<int> OnQuestStartRequested;
    public event Action OnBackRequested;

    // 内部状態
    private Dictionary<int, QuestSlotUI> questSlots;
    private int selectedQuestId = -1;
    private bool isInitialized = false;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        RegisterEvents();
        Initialize();
    }

    private void OnEnable()
    {
        RefreshQuestList();
    }

    private void OnDestroy()
    {
        UnregisterEvents();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネントの初期化
    /// </summary>
    private void InitializeComponents()
    {
        questSlots = new Dictionary<int, QuestSlotUI>();

        // 必須コンポーネントの確認
        if (questScrollRect == null)
        {
            LogError("questScrollRectが設定されていません");
            return;
        }

        if (questListParent == null)
        {
            LogError("questListParentが設定されていません");
            return;
        }

        if (questSlotPrefab == null)
        {
            LogError("questSlotPrefabが設定されていません");
            return;
        }

        if (questDetailPanel == null)
        {
            LogError("questDetailPanelが設定されていません");
            return;
        }

        // 初期状態設定
        questDetailPanel.SetActive(false);

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// QuestListUIを初期化
    /// </summary>
    public void Initialize()
    {
        try
        {
            Log("QuestListUI初期化開始");

            // ボタンイベント設定
            SetupButtons();

            // Manager連携確認
            if (!ValidateManagerDependencies())
            {
                LogError("必要なManagerが見つかりません");
                return;
            }

            isInitialized = true;
            Log("QuestListUI初期化完了");

            // 初期表示
            RefreshQuestList();
        }
        catch (Exception e)
        {
            LogError($"QuestListUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ボタン設定
    /// </summary>
    private void SetupButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            confirmButton.interactable = false; // 初期状態では無効
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    /// <summary>
    /// 依存関係の検証
    /// </summary>
    private bool ValidateManagerDependencies()
    {
        if (QuestListManager.Instance == null)
        {
            LogError("QuestListManagerが見つかりません");
            return false;
        }

        if (!QuestListManager.Instance.IsInitialized)
        {
            LogError("QuestListManagerが初期化されていません");
            return false;
        }

        return true;
    }

    #endregion

    #region イベント処理

    /// <summary>
    /// イベント登録
    /// </summary>
    private void RegisterEvents()
    {
        if (QuestListManager.Instance != null)
        {
            QuestListManager.OnQuestListUpdated += OnQuestListUpdated;
            QuestListManager.OnQuestStarted += OnQuestStarted;
            QuestListManager.OnQuestError += OnQuestError;
        }
    }

    /// <summary>
    /// イベント解除
    /// </summary>
    private void UnregisterEvents()
    {
        if (QuestListManager.Instance != null)
        {
            QuestListManager.OnQuestListUpdated -= OnQuestListUpdated;
            QuestListManager.OnQuestStarted -= OnQuestStarted;
            QuestListManager.OnQuestError -= OnQuestError;
        }
    }

    /// <summary>
    /// クエストリスト更新イベント
    /// </summary>
    private void OnQuestListUpdated()
    {
        RefreshQuestList();
    }

    /// <summary>
    /// クエスト開始イベント
    /// </summary>
    private void OnQuestStarted(QuestStartResult result)
    {
        if (result.isSuccess)
        {
            Log($"クエスト開始成功: {result.questId}");
            HideQuestDetail();
            ClearSelection();
        }
        else
        {
            LogError($"クエスト開始失敗: {result.message}");
            UpdateStatusText($"エラー: {result.message}");
        }
    }

    /// <summary>
    /// クエストエラーイベント
    /// </summary>
    private void OnQuestError(string errorMessage)
    {
        LogError($"クエストエラー: {errorMessage}");
        UpdateStatusText($"エラー: {errorMessage}");
    }

    #endregion

    #region 公開メソッド - 表示制御

    /// <summary>
    /// クエストリストを更新
    /// </summary>
    public void RefreshQuestList()
    {
        if (!isInitialized) return;

        try
        {
            Log("クエストリスト更新開始");

            ShowLoading();

            // 利用可能なクエストを取得
            var availableQuests = QuestListManager.Instance.GetAvailableQuests();

            // クエストリスト表示
            DisplayQuestList(availableQuests);

            HideLoading();
            UpdateStatusText($"{availableQuests.Count}個のクエストが利用可能");

            Log($"クエストリスト更新完了: {availableQuests.Count}個");
        }
        catch (Exception e)
        {
            LogError($"クエストリスト更新エラー: {e.Message}");
            HideLoading();
            UpdateStatusText("クエストリストの読み込みに失敗しました");
        }
    }

    /// <summary>
    /// 指定タイプのクエストを表示
    /// </summary>
    /// <param name="questType">表示するクエストタイプ</param>
    public void DisplayQuestsByType(QuestType questType)
    {
        if (!isInitialized) return;

        try
        {
            Log($"クエストタイプフィルタ: {questType}");

            ShowLoading();

            var questsByType = QuestListManager.Instance.GetQuestsByType(questType);
            DisplayQuestList(questsByType);

            HideLoading();
            UpdateStatusText($"{questType}: {questsByType.Count}個");
        }
        catch (Exception e)
        {
            LogError($"クエストタイプ表示エラー: {e.Message}");
            HideLoading();
            UpdateStatusText("クエストの表示に失敗しました");
        }
    }

    /// <summary>
    /// プレイヤーレベルに適したクエストを表示
    /// </summary>
    /// <param name="playerLevel">プレイヤーレベル</param>
    public void DisplayQuestsForPlayerLevel(int playerLevel)
    {
        if (!isInitialized) return;

        try
        {
            Log($"プレイヤーレベル適合クエスト: Lv.{playerLevel}");

            ShowLoading();

            var suitableQuests = QuestListManager.Instance.GetQuestsForPlayerLevel(playerLevel);
            DisplayQuestList(suitableQuests);

            HideLoading();
            UpdateStatusText($"Lv.{playerLevel}に適した{suitableQuests.Count}個のクエスト");
        }
        catch (Exception e)
        {
            LogError($"レベル適合クエスト表示エラー: {e.Message}");
            HideLoading();
            UpdateStatusText("クエストの表示に失敗しました");
        }
    }

    /// <summary>
    /// おすすめクエストを表示
    /// </summary>
    /// <param name="playerPower">プレイヤー戦闘力</param>
    public void DisplayRecommendedQuests(int playerPower)
    {
        if (!isInitialized) return;

        try
        {
            Log($"おすすめクエスト: 戦闘力{playerPower}");

            ShowLoading();

            var recommendedQuests = QuestListManager.Instance.GetRecommendedQuests(playerPower);
            DisplayQuestList(recommendedQuests);

            HideLoading();
            UpdateStatusText($"おすすめ: {recommendedQuests.Count}個");
        }
        catch (Exception e)
        {
            LogError($"おすすめクエスト表示エラー: {e.Message}");
            HideLoading();
            UpdateStatusText("おすすめクエストの表示に失敗しました");
        }
    }

    #endregion

    #region 内部メソッド - 表示処理

    /// <summary>
    /// クエストリストを表示
    /// </summary>
    /// <param name="quests">表示するクエストリスト</param>
    private void DisplayQuestList(List<QuestDisplayData> quests)
    {
        // 既存スロットをクリア
        ClearQuestSlots();

        if (quests == null || quests.Count == 0)
        {
            Log("表示するクエストがありません");
            return;
        }

        // 新しいスロットを生成
        foreach (var questData in quests)
        {
            CreateQuestSlot(questData);
        }

        // スクロール位置をリセット
        if (questScrollRect != null)
        {
            questScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>
    /// クエストスロットを作成
    /// </summary>
    /// <param name="questData">クエストデータ</param>
    private void CreateQuestSlot(QuestDisplayData questData)
    {
        try
        {
            var slotObject = Instantiate(questSlotPrefab, questListParent);
            var questSlot = slotObject.GetComponent<QuestSlotUI>();

            if (questSlot == null)
            {
                LogError("QuestSlotUIコンポーネントが見つかりません");
                Destroy(slotObject);
                return;
            }

            // スロット設定
            questSlot.Initialize(questData);
            questSlot.OnSlotClicked += OnQuestSlotClicked;

            // 辞書に登録
            questSlots[questData.questId] = questSlot;

            Log($"クエストスロット作成: {questData.questName}");
        }
        catch (Exception e)
        {
            LogError($"クエストスロット作成エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 既存のクエストスロットをクリア
    /// </summary>
    private void ClearQuestSlots()
    {
        // イベント解除
        foreach (var slot in questSlots.Values)
        {
            if (slot != null)
            {
                slot.OnSlotClicked -= OnQuestSlotClicked;
                Destroy(slot.gameObject);
            }
        }

        questSlots.Clear();
        selectedQuestId = -1;

        Log("クエストスロットをクリアしました");
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// クエストスロットクリック処理
    /// </summary>
    /// <param name="questId">選択されたクエストID</param>
    private void OnQuestSlotClicked(int questId)
    {
        try
        {
            Log($"クエスト選択: {questId}");

            // 選択状態更新
            UpdateQuestSelection(questId);

            // 詳細表示
            ShowQuestDetail(questId);

            // イベント通知
            OnQuestSelected?.Invoke(questId);
        }
        catch (Exception e)
        {
            LogError($"クエスト選択エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 決定ボタンクリック処理
    /// </summary>
    private void OnConfirmButtonClicked()
    {
        if (selectedQuestId == -1) return;

        try
        {
            Log($"クエスト出発: {selectedQuestId}");

            // クエスト開始要求
            OnQuestStartRequested?.Invoke(selectedQuestId);
        }
        catch (Exception e)
        {
            LogError($"クエスト出発エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戻るボタンクリック処理
    /// </summary>
    private void OnBackButtonClicked()
    {
        try
        {
            Log("戻るボタンクリック");

            // 詳細パネルが表示されている場合は閉じる
            if (questDetailPanel.activeInHierarchy)
            {
                HideQuestDetail();
                ClearSelection();
            }
            else
            {
                // 上位画面への遷移イベント
                OnBackRequested?.Invoke();
            }
        }
        catch (Exception e)
        {
            LogError($"戻るボタン処理エラー: {e.Message}");
        }
    }

    #endregion

    #region 詳細表示制御

    /// <summary>
    /// クエスト詳細を表示
    /// </summary>
    /// <param name="questId">クエストID</param>
    private void ShowQuestDetail(int questId)
    {
        try
        {
            // 詳細データ取得
            var questDetail = QuestListManager.Instance.GetQuestDetail(questId);
            if (questDetail == null)
            {
                LogError($"クエスト詳細データが見つかりません: {questId}");
                return;
            }

            // 詳細UI表示
            if (questDetailUI != null)
            {
                questDetailUI.DisplayQuestDetail(questDetail);
            }

            questDetailPanel.SetActive(true);

            Log($"クエスト詳細表示: {questDetail.questMaster?.questName}");
        }
        catch (Exception e)
        {
            LogError($"クエスト詳細表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// クエスト詳細を非表示
    /// </summary>
    private void HideQuestDetail()
    {
        questDetailPanel.SetActive(false);
        Log("クエスト詳細非表示");
    }

    #endregion

    #region 選択状態制御

    /// <summary>
    /// クエスト選択状態を更新
    /// </summary>
    /// <param name="questId">選択するクエストID</param>
    private void UpdateQuestSelection(int questId)
    {
        // 前の選択を解除
        if (selectedQuestId != -1 && questSlots.ContainsKey(selectedQuestId))
        {
            questSlots[selectedQuestId].SetSelected(false);
        }

        // 新しい選択を設定
        selectedQuestId = questId;
        if (questSlots.ContainsKey(questId))
        {
            questSlots[questId].SetSelected(true);
        }

        // 決定ボタンの状態更新
        if (confirmButton != null)
        {
            bool canStart = QuestListManager.Instance.CanStartQuest(questId);
            confirmButton.interactable = canStart;
        }

        Log($"選択状態更新: {questId}");
    }

    /// <summary>
    /// 選択をクリア
    /// </summary>
    private void ClearSelection()
    {
        if (selectedQuestId != -1 && questSlots.ContainsKey(selectedQuestId))
        {
            questSlots[selectedQuestId].SetSelected(false);
        }

        selectedQuestId = -1;

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }

        Log("選択クリア");
    }

    #endregion

    #region UI状態制御

    /// <summary>
    /// ローディング表示
    /// </summary>
    private void ShowLoading()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
    }

    /// <summary>
    /// ローディング非表示
    /// </summary>
    private void HideLoading()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// ステータステキスト更新
    /// </summary>
    /// <param name="message">表示メッセージ</param>
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[QuestListUI] {message}");
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[QuestListUI] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("クエストリストを手動更新")]
    private void ManualRefreshQuestList()
    {
        RefreshQuestList();
    }

    [ContextMenu("選択状態をクリア")]
    private void ManualClearSelection()
    {
        ClearSelection();
        HideQuestDetail();
    }

    [ContextMenu("クエストスロットをクリア")]
    private void ManualClearQuestSlots()
    {
        ClearQuestSlots();
    }
#endif

    #endregion
}