using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 初回クリア報酬表示制御UI（データアクセス統一ルール準拠版）
/// 責任範囲：
/// - 初回クリア報酬ウィンドウの表示
/// - ウィンドウの確認ボタンを押すことで非表示にする
/// - 報酬受け取り処理との連携
/// データアクセス統一ルール: UI層 → Manager層 → Data層
/// </summary>
public class RewardUI : MonoBehaviour
{
    [Header("ウィンドウ要素")]
    [SerializeField] private GameObject rewardWindow;
    [SerializeField] private GameObject backgroundOverlay;
    [SerializeField] private TextMeshProUGUI windowTitleText;
    [SerializeField] private Button confirmButton;

    [Header("報酬表示要素")]
    [SerializeField] private Transform rewardSlotParent;
    [SerializeField] private GameObject rewardSlotPrefab;
    [SerializeField] private TextMeshProUGUI rewardDescriptionText;

    [Header("メッセージ設定")]
    [SerializeField] private string windowTitle = "初回クリア報酬";
    [SerializeField] private string rewardDescription = "以下の報酬を獲得しました！";
    [SerializeField] private string confirmButtonText = "確認";

    [Header("デバッグ設定")]
    [SerializeField] private bool enableDebugLog = true;

    // 内部状態
    private bool isInitialized = false;
    private bool isDisplaying = false;
    private QuestMasterData currentQuest = null;
    private System.Collections.Generic.List<GameObject> rewardSlots = new System.Collections.Generic.List<GameObject>();

    // イベント
    public static event System.Action<int> OnRewardReceived; // questId
    public static event System.Action OnRewardWindowClosed;

    #region Unity Lifecycle

    void Start()
    {
        InitializeUI();
        SetupEventListeners();
        Log("RewardUI初期化完了");
    }

    void OnDestroy()
    {
        CleanupEventListeners();
        CleanupRewardSlots();
    }

    #endregion

    #region 初期化・終了処理

    /// <summary>
    /// UI初期化
    /// </summary>
    private void InitializeUI()
    {
        // コンポーネント存在確認
        if (rewardWindow == null)
        {
            LogError("rewardWindowが設定されていません。Inspectorで設定してください。");
        }

        if (confirmButton == null)
        {
            LogError("confirmButtonが設定されていません。Inspectorで設定してください。");
        }
        else
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }

        // テキスト初期設定
        if (windowTitleText != null)
        {
            windowTitleText.text = windowTitle;
        }

        if (rewardDescriptionText != null)
        {
            rewardDescriptionText.text = rewardDescription;
        }

        // 確認ボタンテキスト設定
        if (confirmButton != null)
        {
            var buttonText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = confirmButtonText;
            }
        }

        // 初期状態（非表示）
        SetWindowVisible(false);

        isInitialized = true;
        Log("RewardUI初期化処理完了");
    }

    /// <summary>
    /// イベントリスナー設定
    /// </summary>
    private void SetupEventListeners()
    {
        // QuestListManagerのイベントに登録
        if (QuestListManager.Instance != null)
        {
            QuestListManager.OnQuestCompleted += OnQuestCompleted;
        }

        Log("イベントリスナー設定完了");
    }

    /// <summary>
    /// イベントリスナー解除
    /// </summary>
    private void CleanupEventListeners()
    {
        // QuestListManagerのイベントから解除
        if (QuestListManager.Instance != null)
        {
            QuestListManager.OnQuestCompleted -= OnQuestCompleted;
        }

        Log("イベントリスナー解除完了");
    }

    /// <summary>
    /// 報酬スロットクリーンアップ
    /// </summary>
    private void CleanupRewardSlots()
    {
        foreach (var slot in rewardSlots)
        {
            if (slot != null)
            {
                Destroy(slot);
            }
        }
        rewardSlots.Clear();
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// クエスト完了イベントハンドラ
    /// </summary>
    /// <param name="questId">完了したクエストID</param>
    /// <param name="questDisplayData">クエスト表示データ</param>
    private void OnQuestCompleted(int questId, QuestDisplayData questDisplayData)
    {
        try
        {
            Log($"クエスト完了イベント受信: Quest={questId}");

            // Manager層経由で初回クリア判定
            bool isFirstClear = IsFirstClearQuestViaManager(questId);

            if (isFirstClear)
            {
                ShowFirstClearReward(questId);
            }
        }
        catch (Exception e)
        {
            LogError($"クエスト完了処理エラー: {e.Message}");
        }
    }

    #endregion

    #region Manager層経由のデータアクセスメソッド

    /// <summary>
    /// Manager層経由で初回クリア判定
    /// </summary>
    /// <param name="questId">クエストID</param>
    /// <returns>初回クリアかどうか</returns>
    private bool IsFirstClearQuestViaManager(int questId)
    {
        try
        {
            // SaveDataManager経由でユーザークエストデータを取得
            if (SaveDataManager.Instance?.CurrentSaveData == null)
            {
                LogError("SaveDataが取得できません");
                return false;
            }

            var userData = SaveDataManager.Instance.CurrentSaveData;

            // ユーザークエストデータから該当クエストを検索
            var userQuest = userData.quests?.Find(q => q.questId == questId);

            // 初回クリア判定: クリア回数が1の場合は初回クリア
            // QuestListManagerの実装に合わせて、clearCount == 1で初回クリア判定
            bool isFirstClear = userQuest?.clearCount == 1;

            Log($"初回クリア判定: Quest={questId}, ClearCount={userQuest?.clearCount ?? 0}, IsFirstClear={isFirstClear}");

            return isFirstClear;
        }
        catch (Exception e)
        {
            LogError($"初回クリア判定エラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Manager層経由でクエストデータ取得
    /// </summary>
    /// <param name="questId">クエストID</param>
    /// <returns>クエストマスターデータ</returns>
    private QuestMasterData GetQuestDataViaManager(int questId)
    {
        try
        {
            // QuestDataManagerが存在する場合はそれを使用
            if (QuestDataManager.Instance != null)
            {
                return QuestDataManager.Instance.GetQuestData(questId);
            }

            // フォールバック: QuestListManagerから取得を試行
            if (QuestListManager.Instance != null)
            {
                var questDetail = QuestListManager.Instance.GetQuestDetail(questId);
                return questDetail?.questMaster;
            }

            LogError("利用可能なQuest系Managerが見つかりません");
            return null;
        }
        catch (Exception e)
        {
            LogError($"クエストデータ取得エラー: {e.Message}");
            return null;
        }
    }

    #endregion

    #region 報酬ウィンドウ表示制御

    /// <summary>
    /// 初回クリア報酬を表示
    /// </summary>
    /// <param name="questId">クエストID</param>
    public void ShowFirstClearReward(int questId)
    {
        if (!isInitialized)
        {
            LogError("RewardUIが初期化されていません");
            return;
        }

        try
        {
            // Manager層経由でクエストデータ取得
            var questData = GetQuestDataViaManager(questId);
            if (questData == null)
            {
                LogError($"クエストデータが見つかりません: ID={questId}");
                return;
            }

            // 初回クリア報酬があるかチェック
            if (!questData.HasFirstClearReward())
            {
                Log($"初回クリア報酬が設定されていません: Quest={questId}");
                return;
            }

            Log($"初回クリア報酬表示開始: Quest={questData.questName}");

            currentQuest = questData;
            DisplayReward();
            SetWindowVisible(true);
            isDisplaying = true;

            Log("初回クリア報酬ウィンドウ表示完了");
        }
        catch (Exception e)
        {
            LogError($"初回クリア報酬表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 報酬情報を表示
    /// </summary>
    private void DisplayReward()
    {
        if (currentQuest == null)
        {
            LogError("currentQuestがnullです");
            return;
        }

        try
        {
            // 既存のスロットをクリア
            CleanupRewardSlots();

            // 報酬スロット作成
            CreateRewardSlot();

            Log($"報酬表示完了: {currentQuest.firstClearItemType} ID={currentQuest.firstClearItemId} x{currentQuest.firstClearItemQuantity}");
        }
        catch (Exception e)
        {
            LogError($"報酬表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 報酬スロット作成
    /// </summary>
    private void CreateRewardSlot()
    {
        if (rewardSlotPrefab == null)
        {
            LogError("rewardSlotPrefabが設定されていません");
            return;
        }

        if (rewardSlotParent == null)
        {
            LogError("rewardSlotParentが設定されていません");
            return;
        }

        try
        {
            var slotObject = Instantiate(rewardSlotPrefab, rewardSlotParent);
            rewardSlots.Add(slotObject);

            // FirstClearRewardSlotUIコンポーネントがある場合は初期化
            var rewardSlot = slotObject.GetComponent<FirstClearRewardSlotUI>();
            if (rewardSlot != null)
            {
                rewardSlot.Initialize(currentQuest);
                Log("FirstClearRewardSlotUIで報酬スロット初期化完了");
            }
            else
            {
                // フォールバック: 基本的なテキスト表示
                SetupFallbackRewardSlot(slotObject);
            }

            Log("報酬スロット作成完了");
        }
        catch (Exception e)
        {
            LogError($"報酬スロット作成エラー: {e.Message}");
        }
    }

    /// <summary>
    /// フォールバック用の報酬スロット設定
    /// </summary>
    /// <param name="slotObject">スロットオブジェクト</param>
    private void SetupFallbackRewardSlot(GameObject slotObject)
    {
        var textComponent = slotObject.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            string itemName = GetRewardItemName();
            textComponent.text = $"{itemName} x{currentQuest.firstClearItemQuantity}";
        }

        Log("フォールバック報酬スロット設定完了");
    }

    /// <summary>
    /// 報酬アイテム名取得（Manager層経由）
    /// </summary>
    /// <returns>アイテム名</returns>
    private string GetRewardItemName()
    {
        if (currentQuest == null) return "不明なアイテム";

        try
        {
            // Manager層経由でアイテム名を取得
            return currentQuest.firstClearItemType?.ToLower() switch
            {
                "equipment" => GetEquipmentNameViaManager(currentQuest.firstClearItemId),
                "enhanceitem" => GetEnhanceItemNameViaManager(currentQuest.firstClearItemId),
                "enhance" => GetEnhanceItemNameViaManager(currentQuest.firstClearItemId),
                "supportitem" => GetSupportItemNameViaManager(currentQuest.firstClearItemId),
                "support" => GetSupportItemNameViaManager(currentQuest.firstClearItemId),
                "gold" => "ゴールド",
                "gem" => "ジェム",
                _ => $"{currentQuest.firstClearItemType} ID:{currentQuest.firstClearItemId}"
            };
        }
        catch (Exception e)
        {
            LogError($"報酬アイテム名取得エラー: {e.Message}");
            return $"{currentQuest.firstClearItemType} ID:{currentQuest.firstClearItemId}";
        }
    }

    /// <summary>
    /// Manager層経由で装備名取得
    /// </summary>
    private string GetEquipmentNameViaManager(int equipmentId)
    {
        if (MasterDataManager.Instance == null) return $"装備 ID:{equipmentId}";

        var equipmentData = MasterDataManager.Instance.GetEquipmentData(equipmentId);
        return equipmentData?.equipmentName ?? $"装備 ID:{equipmentId}";
    }

    /// <summary>
    /// Manager層経由で強化アイテム名取得
    /// </summary>
    private string GetEnhanceItemNameViaManager(int itemId)
    {
        if (MasterDataManager.Instance == null) return $"強化素材 ID:{itemId}";

        var enhanceItemData = MasterDataManager.Instance.GetEnhanceItemData(itemId);
        return enhanceItemData?.enhanceItemName ?? $"強化素材 ID:{itemId}";
    }

    /// <summary>
    /// Manager層経由で補助アイテム名取得
    /// </summary>
    private string GetSupportItemNameViaManager(int itemId)
    {
        if (MasterDataManager.Instance == null) return $"補助アイテム ID:{itemId}";

        var supportItemData = MasterDataManager.Instance.GetSupportItemData(itemId);
        return supportItemData?.supportItemName ?? $"補助アイテム ID:{itemId}";
    }

    /// <summary>
    /// ウィンドウ表示制御
    /// </summary>
    /// <param name="visible">表示するかどうか</param>
    private void SetWindowVisible(bool visible)
    {
        if (rewardWindow != null)
        {
            rewardWindow.SetActive(visible);
        }

        if (backgroundOverlay != null)
        {
            backgroundOverlay.SetActive(visible);
        }
    }

    /// <summary>
    /// 報酬ウィンドウを非表示
    /// </summary>
    private void HideRewardWindow()
    {
        SetWindowVisible(false);
        isDisplaying = false;
        currentQuest = null;
        CleanupRewardSlots();
        Log("報酬ウィンドウ非表示");
    }

    #endregion

    #region ユーザー操作

    /// <summary>
    /// 確認ボタンクリック処理
    /// </summary>
    private void OnConfirmButtonClicked()
    {
        try
        {
            Log("確認ボタンクリック");

            if (currentQuest != null)
            {
                // Manager層経由で報酬受け取り処理
                ProcessRewardReceival();

                // イベント発行
                OnRewardReceived?.Invoke(currentQuest.questId);
            }

            // ウィンドウを閉じる
            HideRewardWindow();

            // 閉じるイベント発行
            OnRewardWindowClosed?.Invoke();

            Log("報酬受け取り完了");
        }
        catch (Exception e)
        {
            LogError($"確認ボタンクリック処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 報酬受け取り処理（Manager層経由）
    /// </summary>
    private void ProcessRewardReceival()
    {
        if (currentQuest == null)
        {
            LogError("currentQuestがnullのため報酬受け取り処理をスキップ");
            return;
        }

        try
        {
            // Manager層経由でセーブデータ取得
            if (SaveDataManager.Instance == null)
            {
                LogError("SaveDataManagerが取得できません");
                return;
            }

            var saveData = SaveDataManager.Instance.CurrentSaveData;
            if (saveData == null)
            {
                LogError("SaveDataが取得できません");
                return;
            }

            // Manager層経由で報酬をインベントリに追加
            bool success = AddRewardToInventoryViaManager(saveData);

            if (success)
            {
                // Manager層経由でセーブデータを更新
                SaveDataManager.Instance.MarkDataDirty();
                Log($"報酬受け取り成功: {currentQuest.firstClearItemType} ID={currentQuest.firstClearItemId} x{currentQuest.firstClearItemQuantity}");
            }
            else
            {
                LogError("報酬受け取りに失敗しました");
            }
        }
        catch (Exception e)
        {
            LogError($"報酬受け取り処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// Manager層経由で報酬をインベントリに追加
    /// </summary>
    /// <param name="saveData">セーブデータ</param>
    /// <returns>成功したかどうか</returns>
    private bool AddRewardToInventoryViaManager(UserSaveData saveData)
    {
        try
        {
            string itemType = currentQuest.firstClearItemType?.ToLower();
            int itemId = currentQuest.firstClearItemId;
            int quantity = currentQuest.firstClearItemQuantity;

            switch (itemType)
            {
                case "equipment":
                    return AddEquipmentRewardViaManager(saveData, itemId, quantity);
                case "enhanceitem":
                case "enhance":
                    return AddEnhanceItemRewardViaManager(saveData, itemId, quantity);
                case "supportitem":
                case "support":
                    return AddSupportItemRewardViaManager(saveData, itemId, quantity);
                case "gold":
                    saveData.gold += quantity;
                    Log($"ゴールド追加: +{quantity}");
                    return true;
                case "gem":
                    saveData.gems += quantity;
                    Log($"ジェム追加: +{quantity}");
                    return true;
                default:
                    LogError($"不明な報酬タイプ: {itemType}");
                    return false;
            }
        }
        catch (Exception e)
        {
            LogError($"インベントリ追加エラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Manager層経由で装備報酬をインベントリに追加
    /// </summary>
    private bool AddEquipmentRewardViaManager(UserSaveData saveData, int equipmentId, int quantity)
    {
        var masterData = MasterDataManager.Instance?.GetEquipmentData(equipmentId);
        if (masterData == null)
        {
            LogError($"装備マスターデータが見つかりません: ID={equipmentId}");
            return false;
        }

        for (int i = 0; i < quantity; i++)
        {
            var newEquipment = new UserEquipmentData(masterData);
            saveData.AddEquipment(newEquipment);
        }
        Log($"装備追加: ID={equipmentId} x{quantity}");
        return true;
    }

    /// <summary>
    /// Manager層経由で強化アイテム報酬をインベントリに追加
    /// </summary>
    private bool AddEnhanceItemRewardViaManager(UserSaveData saveData, int itemId, int quantity)
    {
        var existingItem = saveData.items.Find(item =>
            item.itemType == ItemType.EnhanceItem && item.itemMasterId == itemId);

        if (existingItem != null)
        {
            existingItem.AddItem(quantity);
        }
        else
        {
            var masterData = MasterDataManager.Instance?.GetEnhanceItemData(itemId);
            if (masterData != null)
            {
                var newItem = new UserItemData(masterData, quantity);
                saveData.AddItem(newItem);
            }
            else
            {
                LogError($"強化アイテムマスターデータが見つかりません: ID={itemId}");
                return false;
            }
        }
        Log($"強化アイテム追加: ID={itemId} x{quantity}");
        return true;
    }

    /// <summary>
    /// Manager層経由で補助アイテム報酬をインベントリに追加
    /// </summary>
    private bool AddSupportItemRewardViaManager(UserSaveData saveData, int itemId, int quantity)
    {
        var existingItem = saveData.items.Find(item =>
            item.itemType == ItemType.SupportItem && item.itemMasterId == itemId);

        if (existingItem != null)
        {
            existingItem.AddItem(quantity);
        }
        else
        {
            var masterData = MasterDataManager.Instance?.GetSupportItemData(itemId);
            if (masterData != null)
            {
                var newItem = new UserItemData(masterData, quantity);
                saveData.AddItem(newItem);
            }
            else
            {
                LogError($"補助アイテムマスターデータが見つかりません: ID={itemId}");
                return false;
            }
        }
        Log($"補助アイテム追加: ID={itemId} x{quantity}");
        return true;
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 手動で初回クリア報酬を表示（デバッグ用）
    /// </summary>
    /// <param name="questData">クエストデータ</param>
    public void ShowReward(QuestMasterData questData)
    {
        if (questData == null)
        {
            LogError("クエストデータがnullです");
            return;
        }

        currentQuest = questData;
        DisplayReward();
        SetWindowVisible(true);
        isDisplaying = true;
        Log("手動で報酬ウィンドウを表示");
    }

    /// <summary>
    /// 報酬ウィンドウを手動で非表示（デバッグ用）
    /// </summary>
    public void HideReward()
    {
        HideRewardWindow();
        Log("手動で報酬ウィンドウを非表示");
    }

    /// <summary>
    /// 表示状態確認
    /// </summary>
    /// <returns>表示中かどうか</returns>
    public bool IsDisplaying()
    {
        return isDisplaying;
    }

    /// <summary>
    /// 初期化状態確認
    /// </summary>
    /// <returns>初期化済みかどうか</returns>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    /// <summary>
    /// 現在のクエストデータ取得
    /// </summary>
    /// <returns>現在のクエストデータ</returns>
    public QuestMasterData GetCurrentQuest()
    {
        return currentQuest;
    }

    #endregion

    #region ログ・デバッグ機能

    /// <summary>
    /// ログ出力
    /// </summary>
    /// <param name="message">ログメッセージ</param>
    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[RewardUI] {message}");
        }
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    private void LogError(string message)
    {
        Debug.LogError($"[RewardUI] {message}");
    }

    /// <summary>
    /// デバッグ情報出力
    /// </summary>
    [ContextMenu("デバッグ情報出力")]
    private void DumpDebugInfo()
    {
        Log("=== RewardUI デバッグ情報 ===");
        Log($"初期化状態: {isInitialized}");
        Log($"表示状態: {isDisplaying}");
        Log($"現在のクエスト: {currentQuest?.questName ?? "なし"}");

        if (currentQuest != null)
        {
            Log($"報酬詳細: {currentQuest.firstClearItemType} ID={currentQuest.firstClearItemId} x{currentQuest.firstClearItemQuantity}");
        }

        Log($"報酬スロット数: {rewardSlots.Count}");
        Log($"UI要素確認:");
        Log($"  rewardWindow: {rewardWindow != null}");
        Log($"  confirmButton: {confirmButton != null}");
        Log($"  rewardSlotParent: {rewardSlotParent != null}");
        Log($"  rewardSlotPrefab: {rewardSlotPrefab != null}");

        // Manager層の接続状態確認
        Log($"Manager層接続状態:");
        Log($"  SaveDataManager: {SaveDataManager.Instance != null}");
        Log($"  MasterDataManager: {MasterDataManager.Instance != null}");
        Log($"  QuestDataManager: {QuestDataManager.Instance != null}");
        Log($"  QuestListManager: {QuestListManager.Instance != null}");
    }

    #endregion
}