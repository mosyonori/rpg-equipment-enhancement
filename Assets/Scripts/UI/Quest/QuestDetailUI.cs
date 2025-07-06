using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// クエスト詳細パネル制御クラス（完全修正版）
/// 責任範囲：
/// - 選択されたクエストの詳細情報表示
/// - 出現モンスターリストの動的生成・表示
/// - ドロップアイテムリストの動的生成・表示
/// - 初回クリア報酬の表示
/// - 出撃ボタンの状態管理
/// </summary>
public class QuestDetailUI : MonoBehaviour
{
    [Header("基本情報")]
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    [SerializeField] private TextMeshProUGUI questTypeText;
    [SerializeField] private Image questTypeIcon;

    [Header("クエスト条件")]
    [SerializeField] private TextMeshProUGUI needLevelText;
    [SerializeField] private TextMeshProUGUI requiredStaminaText;
    [SerializeField] private TextMeshProUGUI recommendedPowerText;
    [SerializeField] private TextMeshProUGUI turnLimitText;

    [Header("進行状況")]
    [SerializeField] private TextMeshProUGUI clearCountText;
    [SerializeField] private TextMeshProUGUI maxClearCountText;
    [SerializeField] private Slider clearProgressSlider;

    [Header("出現モンスター")]
    [SerializeField] private Transform monsterListParent;
    [SerializeField] private GameObject monsterSlotPrefab;
    [SerializeField] private TextMeshProUGUI monsterSectionTitle;

    [Header("ドロップアイテム")]
    [SerializeField] private Transform dropItemListParent;
    [SerializeField] private GameObject dropItemSlotPrefab;
    [SerializeField] private TextMeshProUGUI dropItemSectionTitle;

    [Header("初回クリア報酬")]
    [SerializeField] private Transform firstClearRewardParent;
    [SerializeField] private GameObject firstClearRewardSlotPrefab;
    [SerializeField] private GameObject firstClearRewardSection;
    [SerializeField] private TextMeshProUGUI firstClearRewardTitle;

    [Header("基本報酬")]
    [SerializeField] private TextMeshProUGUI expRewardText;
    [SerializeField] private TextMeshProUGUI goldRewardText;

    [Header("制御ボタン")]
    [SerializeField] private Button startBattleButton;
    [SerializeField] private TextMeshProUGUI startBattleButtonText;
    [SerializeField] private Button closeButton;  // 閉じるボタン（×ボタン）

    [Header("状態表示")]
    [SerializeField] private GameObject availabilityPanel;
    [SerializeField] private TextMeshProUGUI availabilityReasonText;
    [SerializeField] private Image availabilityIcon;

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private int maxDisplayMonsters = 6;
    [SerializeField] private int maxDisplayDropItems = 8;

    // イベント
    public event Action<int> OnStartBattleClicked;
    public event Action OnCloseClicked;  // 閉じるボタンクリックイベント

    // 内部状態
    private QuestDetailData currentQuestDetail;
    private List<MonsterSlotUI> monsterSlots;
    private List<DropItemSlotUI> dropItemSlots;
    private List<FirstClearRewardSlotUI> firstClearRewardSlots;

    // フレーム遅延用フラグ
    private bool isDisplaying = false;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネントの初期化
    /// </summary>
    private void InitializeComponents()
    {
        // リストの安全な初期化
        if (monsterSlots == null)
            monsterSlots = new List<MonsterSlotUI>();
        else
            monsterSlots.Clear();

        if (dropItemSlots == null)
            dropItemSlots = new List<DropItemSlotUI>();
        else
            dropItemSlots.Clear();

        if (firstClearRewardSlots == null)
            firstClearRewardSlots = new List<FirstClearRewardSlotUI>();
        else
            firstClearRewardSlots.Clear();

        // ボタンイベント設定
        if (startBattleButton != null)
        {
            startBattleButton.onClick.RemoveAllListeners();
            startBattleButton.onClick.AddListener(OnStartBattleButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        // 初期状態設定
        SafeSetActive(firstClearRewardSection, false);
        SafeSetActive(availabilityPanel, false);

        Log("QuestDetailUI初期化完了");
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// クエスト詳細を表示（修正版: GameObject状態対応）
    /// </summary>
    /// <param name="questDetail">クエスト詳細データ</param>
    public void DisplayQuestDetail(QuestDetailData questDetail)
    {
        if (isDisplaying)
        {
            Log("表示処理中のため、前の処理完了を待機します");
            return;
        }

        // 修正: GameObjectが非アクティブな場合はアクティブにしてからコルーチン開始
        if (!gameObject.activeInHierarchy)
        {
            Log("QuestDetailPanelが非アクティブのため、アクティブにします");
            gameObject.SetActive(true);
        }

        StartCoroutine(DisplayQuestDetailCoroutine(questDetail));
    }

    /// <summary>
    /// クエスト詳細を即座に表示（非コルーチン版）
    /// </summary>
    /// <param name="questDetail">クエスト詳細データ</param>
    public void DisplayQuestDetailImmediate(QuestDetailData questDetail)
    {
        try
        {
            if (questDetail == null)
            {
                LogError("QuestDetailDataがnullです");
                return;
            }

            if (questDetail.questMaster == null)
            {
                LogError("QuestDetailData.questMasterがnullです");
                return;
            }

            // GameObjectをアクティブにする
            if (!gameObject.activeInHierarchy)
            {
                Log("QuestDetailPanelが非アクティブのため、アクティブにします");
                gameObject.SetActive(true);
            }

            Log($"クエスト詳細表示開始（即座版）: {questDetail.questMaster.questName}");

            // 既存のスロットをクリア
            ClearAllSlots();

            currentQuestDetail = questDetail;

            // 基本情報表示
            DisplayBasicInfo();

            // 条件・進行状況表示
            DisplayQuestConditions();
            DisplayQuestProgress();

            // 報酬表示
            DisplayBasicRewards();

            // モンスター・ドロップアイテム表示
            DisplaySpawnMonsters();
            DisplayDropItems();

            // 初回クリア報酬表示
            DisplayFirstClearReward();

            // ボタン状態更新
            UpdateStartBattleButton();

            // 利用可能性表示
            DisplayAvailability();

            Log("クエスト詳細表示完了（即座版）");
        }
        catch (Exception e)
        {
            LogError($"クエスト詳細表示エラー: {e.Message}");
            LogError($"スタックトレース: {e.StackTrace}");
        }
    }

    /// <summary>
    /// クエスト詳細表示のコルーチン（修正版: try-catch制約対応）
    /// </summary>
    private IEnumerator DisplayQuestDetailCoroutine(QuestDetailData questDetail)
    {
        isDisplaying = true;

        // 修正: try-catchをコルーチン外で実行するため、事前チェックを行う
        if (questDetail == null)
        {
            LogError("QuestDetailDataがnullです");
            isDisplaying = false;
            yield break;
        }

        if (questDetail.questMaster == null)
        {
            LogError("QuestDetailData.questMasterがnullです");
            isDisplaying = false;
            yield break;
        }

        Log($"クエスト詳細表示開始: {questDetail.questMaster.questName}");

        // 修正: 確実なクリア処理（フレーム待機）
        ClearAllSlots();
        yield return null; // 1フレーム待機してDestroy処理を確実に実行

        currentQuestDetail = questDetail;

        // 基本情報表示
        yield return StartCoroutine(SafeDisplayBasicInfo());

        // 条件・進行状況表示
        yield return StartCoroutine(SafeDisplayConditionsAndProgress());

        // 報酬表示
        yield return StartCoroutine(SafeDisplayRewards());

        // モンスター・ドロップアイテム表示
        yield return StartCoroutine(SafeDisplaySpawnMonsters());

        yield return StartCoroutine(SafeDisplayDropItems());

        // 修正: 初回クリア報酬は最後に表示（詳細なログ付き）
        yield return StartCoroutine(SafeDisplayFirstClearReward());

        // ボタン状態更新
        UpdateStartBattleButton();

        // 利用可能性表示
        DisplayAvailability();

        Log("クエスト詳細表示完了");
        isDisplaying = false;
    }

    /// <summary>
    /// 安全な基本情報表示
    /// </summary>
    private IEnumerator SafeDisplayBasicInfo()
    {
        try
        {
            DisplayBasicInfo();
        }
        catch (Exception e)
        {
            LogError($"基本情報表示エラー: {e.Message}");
        }
        yield return null;
    }

    /// <summary>
    /// 安全な条件・進行状況表示
    /// </summary>
    private IEnumerator SafeDisplayConditionsAndProgress()
    {
        try
        {
            DisplayQuestConditions();
            DisplayQuestProgress();
        }
        catch (Exception e)
        {
            LogError($"条件・進行状況表示エラー: {e.Message}");
        }
        yield return null;
    }

    /// <summary>
    /// 安全な報酬表示
    /// </summary>
    private IEnumerator SafeDisplayRewards()
    {
        try
        {
            DisplayBasicRewards();
        }
        catch (Exception e)
        {
            LogError($"基本報酬表示エラー: {e.Message}");
        }
        yield return null;
    }

    /// <summary>
    /// 安全なモンスター表示
    /// </summary>
    private IEnumerator SafeDisplaySpawnMonsters()
    {
        try
        {
            DisplaySpawnMonsters();
        }
        catch (Exception e)
        {
            LogError($"出現モンスター表示エラー: {e.Message}");
        }
        yield return null;
    }

    /// <summary>
    /// 安全なドロップアイテム表示
    /// </summary>
    private IEnumerator SafeDisplayDropItems()
    {
        try
        {
            DisplayDropItems();
        }
        catch (Exception e)
        {
            LogError($"ドロップアイテム表示エラー: {e.Message}");
        }
        yield return null;
    }

    /// <summary>
    /// 安全な初回クリア報酬表示
    /// </summary>
    private IEnumerator SafeDisplayFirstClearReward()
    {
        try
        {
            DisplayFirstClearReward();
        }
        catch (Exception e)
        {
            LogError($"初回クリア報酬表示エラー: {e.Message}");
        }
        yield return null;
    }

    /// <summary>
    /// 詳細パネルをクリア
    /// </summary>
    public void ClearDetail()
    {
        currentQuestDetail = null;
        ClearAllSlots();
        Log("クエスト詳細クリア");
    }

    /// <summary>
    /// 詳細パネルを非表示にする（修正: 必ずクリアを実行）
    /// </summary>
    public void HideDetailPanel()
    {
        // 修正: パネルを非表示にする際は必ずスロット内容をクリア
        ClearDetail();

        // パネル自体を非表示にする場合
        gameObject.SetActive(false);

        Log("クエスト詳細パネルを非表示にしました");
    }

    #endregion

    #region 基本情報表示

    /// <summary>
    /// 基本情報を表示
    /// </summary>
    private void DisplayBasicInfo()
    {
        if (currentQuestDetail?.questMaster == null) return;

        var questMaster = currentQuestDetail.questMaster;

        // タイトル
        SafeSetText(questTitleText, questMaster.questName);

        // 説明
        SafeSetText(questDescriptionText, questMaster.description);

        // クエストタイプ
        SafeSetText(questTypeText, GetQuestTypeDisplayName(questMaster.questType));

        // タイプアイコン
        LoadQuestTypeIcon(questMaster.questType);

        Log("基本情報表示完了");
    }

    /// <summary>
    /// クエストタイプの表示名を取得
    /// </summary>
    /// <param name="questType">クエストタイプ</param>
    /// <returns>表示名</returns>
    private string GetQuestTypeDisplayName(QuestType questType)
    {
        return questType switch
        {
            QuestType.Story => "ストーリー",
            QuestType.Daily => "デイリー",
            QuestType.Weekly => "ウィークリー",
            QuestType.Event => "イベント",
            _ => "不明"
        };
    }

    /// <summary>
    /// クエストタイプアイコンを読み込み
    /// </summary>
    /// <param name="questType">クエストタイプ</param>
    private void LoadQuestTypeIcon(QuestType questType)
    {
        if (questTypeIcon == null) return;

        try
        {
            string iconPath = $"Icons/Quest/type_{questType.ToString().ToLower()}";
            var sprite = Resources.Load<Sprite>(iconPath);

            if (sprite != null)
            {
                questTypeIcon.sprite = sprite;
                questTypeIcon.gameObject.SetActive(true);
            }
            else
            {
                questTypeIcon.gameObject.SetActive(false);
            }
        }
        catch (Exception e)
        {
            LogError($"クエストタイプアイコン読み込みエラー: {e.Message}");
            SafeSetActive(questTypeIcon, false);
        }
    }

    #endregion

    #region 条件・進行状況表示

    /// <summary>
    /// クエスト条件を表示
    /// </summary>
    private void DisplayQuestConditions()
    {
        if (currentQuestDetail?.questMaster == null) return;

        var questMaster = currentQuestDetail.questMaster;

        // 必要レベル
        SafeSetText(needLevelText, $"必要レベル: Lv.{questMaster.needLevel}");

        // 必要スタミナ
        SafeSetText(requiredStaminaText, $"消費スタミナ: {questMaster.requiredStamina}");

        // 推奨戦闘力
        SafeSetText(recommendedPowerText, $"推奨戦闘力: {questMaster.recommendedPower:N0}");

        // ターン制限
        if (questMaster.HasTurnLimit())
        {
            SafeSetText(turnLimitText, $"ターン制限: {questMaster.turnLimit}ターン");
            SafeSetActive(turnLimitText, true);
        }
        else
        {
            SafeSetActive(turnLimitText, false);
        }

        Log("クエスト条件表示完了");
    }

    /// <summary>
    /// クエスト進行状況を表示
    /// </summary>
    private void DisplayQuestProgress()
    {
        if (currentQuestDetail?.questMaster == null) return;

        var questMaster = currentQuestDetail.questMaster;
        var userQuest = currentQuestDetail.userQuestData; // nullの可能性あり

        // クリア回数（userQuestDataがnullの場合は0）
        int clearCount = userQuest?.clearCount ?? 0;
        SafeSetText(clearCountText, $"クリア回数: {clearCount}");

        // 最大クリア回数
        if (questMaster.IsUnlimitedClear())
        {
            SafeSetText(maxClearCountText, "制限なし");
        }
        else
        {
            SafeSetText(maxClearCountText, $"/ {questMaster.dailyClearLimit}");
        }

        // 進捗スライダー
        if (clearProgressSlider != null)
        {
            if (questMaster.IsUnlimitedClear())
            {
                clearProgressSlider.gameObject.SetActive(false);
            }
            else
            {
                clearProgressSlider.gameObject.SetActive(true);
                clearProgressSlider.maxValue = questMaster.dailyClearLimit;
                clearProgressSlider.value = clearCount;
            }
        }

        Log($"進行状況表示完了: クリア回数 {clearCount}");
    }

    #endregion

    #region 報酬表示

    /// <summary>
    /// 基本報酬を表示
    /// </summary>
    private void DisplayBasicRewards()
    {
        if (currentQuestDetail?.questMaster == null) return;

        var questMaster = currentQuestDetail.questMaster;

        // 経験値報酬
        SafeSetText(expRewardText, $"EXP: {questMaster.rewardExp:N0}");

        // ゴールド報酬
        SafeSetText(goldRewardText, $"ゴールド: {questMaster.rewardGold:N0}");

        Log("基本報酬表示完了");
    }

    /// <summary>
    /// 初回クリア報酬を表示（修正版: 詳細なログ付き）
    /// </summary>
    private void DisplayFirstClearReward()
    {
        if (currentQuestDetail?.questMaster == null)
        {
            Log("questMaster が null のため初回クリア報酬表示をスキップ");
            return;
        }

        var questMaster = currentQuestDetail.questMaster;
        var userQuest = currentQuestDetail.userQuestData;

        // 修正: より詳細な判定ログ
        bool hasFirstClearReward = questMaster.HasFirstClearReward();
        int clearCount = userQuest?.clearCount ?? 0;
        bool isFirstClear = clearCount == 0;

        Log($"初回クリア報酬判定:");
        Log($"  hasFirstClearReward: {hasFirstClearReward}");
        Log($"  clearCount: {clearCount}");
        Log($"  isFirstClear: {isFirstClear}");
        Log($"  firstClearItemType: '{questMaster.firstClearItemType}'");
        Log($"  firstClearItemId: {questMaster.firstClearItemId}");
        Log($"  firstClearItemQuantity: {questMaster.firstClearItemQuantity}");

        // 修正: 初回クリア報酬があり、かつ初回クリア時のみ表示
        bool shouldShow = hasFirstClearReward && isFirstClear;
        Log($"  shouldShow: {shouldShow}");

        SafeSetActive(firstClearRewardSection, shouldShow);

        if (shouldShow)
        {
            Log($"初回クリア報酬表示開始: {questMaster.firstClearItemType} ID:{questMaster.firstClearItemId} x{questMaster.firstClearItemQuantity}");

            // 修正: 既存のスロットをクリアしてから新しいスロットを作成
            ClearFirstClearRewardSlots();
            CreateFirstClearRewardSlot(questMaster);

            Log("初回クリア報酬表示完了");
        }
        else
        {
            Log($"初回クリア報酬非表示: hasReward={hasFirstClearReward}, isFirstClear={isFirstClear}");
        }
    }

    /// <summary>
    /// 初回クリア報酬スロットを作成
    /// </summary>
    /// <param name="questMaster">クエストマスターデータ</param>
    private void CreateFirstClearRewardSlot(QuestMasterData questMaster)
    {
        // 修正: より詳細なエラーログとデバッグ情報
        if (firstClearRewardSlotPrefab == null)
        {
            LogError("firstClearRewardSlotPrefabが設定されていません。Inspectorで設定してください。");
            return;
        }

        if (firstClearRewardParent == null)
        {
            LogError("firstClearRewardParentが設定されていません。Inspectorで設定してください。");
            return;
        }

        try
        {
            Log($"初回クリア報酬スロット作成開始: {questMaster.firstClearItemType} ID:{questMaster.firstClearItemId}");

            var slotObject = Instantiate(firstClearRewardSlotPrefab, firstClearRewardParent);
            var rewardSlot = slotObject.GetComponent<FirstClearRewardSlotUI>();

            if (rewardSlot != null)
            {
                rewardSlot.Initialize(questMaster);
                firstClearRewardSlots.Add(rewardSlot);
                Log("初回クリア報酬スロット作成成功");
            }
            else
            {
                LogError("FirstClearRewardSlotUIコンポーネントがプレハブに付いていません");
                Destroy(slotObject);
            }
        }
        catch (Exception e)
        {
            LogError($"初回クリア報酬スロット作成エラー: {e.Message}");
        }
    }

    #endregion

    #region モンスター表示

    /// <summary>
    /// 出現モンスターを表示
    /// </summary>
    private void DisplaySpawnMonsters()
    {
        try
        {
            // 修正: 既存のスロットを必ずクリア
            ClearMonsterSlots();

            if (currentQuestDetail?.spawnMonsters == null)
            {
                Log("出現モンスターデータがnullです");
                return;
            }

            var monsters = currentQuestDetail.spawnMonsters;
            int displayCount = Mathf.Min(monsters.Count, maxDisplayMonsters);

            SafeSetText(monsterSectionTitle, $"出現モンスター ({displayCount}体)");

            for (int i = 0; i < displayCount; i++)
            {
                if (monsters[i] != null)
                {
                    CreateMonsterSlot(monsters[i]);
                }
                else
                {
                    Log($"モンスターデータ[{i}]がnullのためスキップします");
                }
            }

            Log($"出現モンスター表示: {displayCount}体");
        }
        catch (Exception e)
        {
            LogError($"出現モンスター表示エラー: {e.Message}");
            LogError($"スタックトレース: {e.StackTrace}");
        }
    }

    /// <summary>
    /// モンスタースロットを作成
    /// </summary>
    /// <param name="monsterData">モンスターデータ</param>
    private void CreateMonsterSlot(MonsterMasterData monsterData)
    {
        if (monsterSlotPrefab == null)
        {
            LogError("monsterSlotPrefabが設定されていません。Inspectorで設定してください。");
            return;
        }

        if (monsterListParent == null)
        {
            LogError("monsterListParentが設定されていません。Inspectorで設定してください。");
            return;
        }

        try
        {
            var slotObject = Instantiate(monsterSlotPrefab, monsterListParent);
            if (slotObject == null)
            {
                LogError("モンスタースロットオブジェクトの作成に失敗しました");
                return;
            }

            var monsterSlot = slotObject.GetComponent<MonsterSlotUI>();

            if (monsterSlot != null)
            {
                monsterSlot.Initialize(monsterData);

                // 安全にリストに追加
                if (monsterSlots == null)
                    monsterSlots = new List<MonsterSlotUI>();

                monsterSlots.Add(monsterSlot);
                Log($"モンスタースロット作成成功: {monsterData.monsterName}");
            }
            else
            {
                LogError("MonsterSlotUIコンポーネントが見つかりません");
                Destroy(slotObject);
            }
        }
        catch (Exception e)
        {
            LogError($"モンスタースロット作成エラー: {e.Message}");
        }
    }

    #endregion

    #region ドロップアイテム表示

    /// <summary>
    /// ドロップアイテムを表示
    /// </summary>
    private void DisplayDropItems()
    {
        // 修正: 既存のスロットを必ずクリア
        ClearDropItemSlots();

        if (currentQuestDetail?.dropTable?.dropItems == null)
        {
            Log("ドロップテーブルがnullです");
            return;
        }

        var dropItems = currentQuestDetail.dropTable.dropItems;
        int displayCount = Mathf.Min(dropItems.Count, maxDisplayDropItems);

        SafeSetText(dropItemSectionTitle, $"ドロップアイテム ({displayCount}種類)");

        for (int i = 0; i < displayCount; i++)
        {
            CreateDropItemSlot(dropItems[i]);
        }

        Log($"ドロップアイテム表示: {displayCount}種類");
    }

    /// <summary>
    /// ドロップアイテムスロットを作成
    /// </summary>
    /// <param name="dropItem">ドロップアイテムデータ</param>
    private void CreateDropItemSlot(DropItemData dropItem)
    {
        if (dropItemSlotPrefab == null)
        {
            LogError("dropItemSlotPrefabが設定されていません。Inspectorで設定してください。");
            return;
        }

        if (dropItemListParent == null)
        {
            LogError("dropItemListParentが設定されていません。Inspectorで設定してください。");
            return;
        }

        try
        {
            var slotObject = Instantiate(dropItemSlotPrefab, dropItemListParent);
            var dropItemSlot = slotObject.GetComponent<DropItemSlotUI>();

            if (dropItemSlot != null)
            {
                dropItemSlot.Initialize(dropItem);
                dropItemSlots.Add(dropItemSlot);
            }
            else
            {
                LogError("DropItemSlotUIコンポーネントが見つかりません");
                Destroy(slotObject);
            }
        }
        catch (Exception e)
        {
            LogError($"ドロップアイテムスロット作成エラー: {e.Message}");
        }
    }

    #endregion

    #region ボタン制御

    /// <summary>
    /// 出撃ボタンの状態を更新
    /// </summary>
    private void UpdateStartBattleButton()
    {
        if (startBattleButton == null || currentQuestDetail == null) return;

        bool canStart = currentQuestDetail.isAvailable;
        startBattleButton.interactable = canStart;

        SafeSetText(startBattleButtonText, canStart ? "出撃" : "出撃不可");
    }

    /// <summary>
    /// 利用可能性を表示
    /// </summary>
    private void DisplayAvailability()
    {
        if (availabilityPanel == null || currentQuestDetail == null) return;

        bool showPanel = !currentQuestDetail.isAvailable;
        availabilityPanel.SetActive(showPanel);

        if (showPanel)
        {
            SafeSetText(availabilityReasonText, currentQuestDetail.availabilityReason);
        }
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// 出撃ボタンクリック処理
    /// </summary>
    private void OnStartBattleButtonClicked()
    {
        try
        {
            if (currentQuestDetail == null)
            {
                LogError("クエスト詳細データがnullです");
                return;
            }

            if (!currentQuestDetail.isAvailable)
            {
                Log($"利用できないクエストです: {currentQuestDetail.availabilityReason}");
                return;
            }

            Log($"出撃ボタンクリック: {currentQuestDetail.questMaster.questName}");

            OnStartBattleClicked?.Invoke(currentQuestDetail.questMaster.questId);
        }
        catch (Exception e)
        {
            LogError($"出撃ボタンクリック処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 閉じるボタンクリック処理
    /// </summary>
    private void OnCloseButtonClicked()
    {
        try
        {
            Log("閉じるボタンクリック - 詳細画面を非表示にします");

            // 修正: 詳細をクリアしてからイベントを発火
            ClearDetail();

            // 閉じるイベントを発火
            OnCloseClicked?.Invoke();
        }
        catch (Exception e)
        {
            LogError($"閉じるボタンクリック処理エラー: {e.Message}");
        }
    }

    #endregion

    #region スロット管理

    /// <summary>
    /// 全スロットをクリア
    /// </summary>
    private void ClearAllSlots()
    {
        ClearMonsterSlots();
        ClearDropItemSlots();
        ClearFirstClearRewardSlots();
        Log("全スロットクリア完了");
    }

    /// <summary>
    /// モンスタースロットをクリア
    /// </summary>
    private void ClearMonsterSlots()
    {
        try
        {
            if (monsterSlots == null)
            {
                Log("monsterSlotsがnullのため初期化します");
                monsterSlots = new List<MonsterSlotUI>();
                return;
            }

            for (int i = monsterSlots.Count - 1; i >= 0; i--)
            {
                var slot = monsterSlots[i];
                if (slot != null)
                {
                    if (slot.gameObject != null)
                    {
                        Destroy(slot.gameObject);
                    }
                }
            }
            monsterSlots.Clear();
            Log("モンスタースロットクリア完了");
        }
        catch (Exception e)
        {
            LogError($"モンスタースロットクリアエラー: {e.Message}");
            // エラー時は強制的に新しいリストを作成
            monsterSlots = new List<MonsterSlotUI>();
        }
    }

    /// <summary>
    /// ドロップアイテムスロットをクリア
    /// </summary>
    private void ClearDropItemSlots()
    {
        try
        {
            if (dropItemSlots == null)
            {
                Log("dropItemSlotsがnullのため初期化します");
                dropItemSlots = new List<DropItemSlotUI>();
                return;
            }

            for (int i = dropItemSlots.Count - 1; i >= 0; i--)
            {
                var slot = dropItemSlots[i];
                if (slot != null)
                {
                    if (slot.gameObject != null)
                    {
                        Destroy(slot.gameObject);
                    }
                }
            }
            dropItemSlots.Clear();
            Log("ドロップアイテムスロットクリア完了");
        }
        catch (Exception e)
        {
            LogError($"ドロップアイテムスロットクリアエラー: {e.Message}");
            // エラー時は強制的に新しいリストを作成
            dropItemSlots = new List<DropItemSlotUI>();
        }
    }

    /// <summary>
    /// 初回クリア報酬スロットをクリア
    /// </summary>
    private void ClearFirstClearRewardSlots()
    {
        try
        {
            if (firstClearRewardSlots == null)
            {
                Log("firstClearRewardSlotsがnullのため初期化します");
                firstClearRewardSlots = new List<FirstClearRewardSlotUI>();
                return;
            }

            for (int i = firstClearRewardSlots.Count - 1; i >= 0; i--)
            {
                var slot = firstClearRewardSlots[i];
                if (slot != null)
                {
                    if (slot.gameObject != null)
                    {
                        Destroy(slot.gameObject);
                    }
                }
            }
            firstClearRewardSlots.Clear();
            Log("初回クリア報酬スロットクリア完了");
        }
        catch (Exception e)
        {
            LogError($"初回クリア報酬スロットクリアエラー: {e.Message}");
            // エラー時は強制的に新しいリストを作成
            firstClearRewardSlots = new List<FirstClearRewardSlotUI>();
        }
    }

    #endregion

    #region 安全なUI操作メソッド

    /// <summary>
    /// 安全にテキストを設定
    /// </summary>
    private void SafeSetText(TextMeshProUGUI textComponent, string text)
    {
        if (textComponent != null)
        {
            textComponent.text = text ?? "";
        }
    }

    /// <summary>
    /// 安全にGameObjectをアクティブ設定
    /// </summary>
    private void SafeSetActive(GameObject obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);
        }
    }

    /// <summary>
    /// 安全にコンポーネントをアクティブ設定
    /// </summary>
    private void SafeSetActive(Component component, bool active)
    {
        if (component != null && component.gameObject != null)
        {
            component.gameObject.SetActive(active);
        }
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[QuestDetailUI] {message}");
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[QuestDetailUI] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("詳細パネルをクリア")]
    private void ManualClearDetail()
    {
        ClearDetail();
    }

    [ContextMenu("スロット数をログ出力")]
    private void LogSlotCounts()
    {
        Log($"モンスタースロット: {monsterSlots?.Count ?? 0}, " +
            $"ドロップアイテムスロット: {dropItemSlots?.Count ?? 0}, " +
            $"初回報酬スロット: {firstClearRewardSlots?.Count ?? 0}");
    }

    [ContextMenu("アサイン状況をチェック")]
    private void CheckAssignments()
    {
        Log("=== アサイン状況チェック ===");
        Log($"monsterSlotPrefab: {(monsterSlotPrefab != null ? "OK" : "未設定")}");
        Log($"dropItemSlotPrefab: {(dropItemSlotPrefab != null ? "OK" : "未設定")}");
        Log($"firstClearRewardSlotPrefab: {(firstClearRewardSlotPrefab != null ? "OK" : "未設定")}");
        Log($"firstClearRewardParent: {(firstClearRewardParent != null ? "OK" : "未設定")}");
        Log($"firstClearRewardSection: {(firstClearRewardSection != null ? "OK" : "未設定")}");
        Log($"closeButton: {(closeButton != null ? "OK" : "未設定")}");
    }

    [ContextMenu("初回クリア報酬デバッグ")]
    private void DebugFirstClearReward()
    {
        if (currentQuestDetail?.questMaster != null)
        {
            var questMaster = currentQuestDetail.questMaster;
            Log($"=== 初回クリア報酬デバッグ ===");
            Log($"questId: {questMaster.questId}");
            Log($"questName: {questMaster.questName}");
            Log($"HasFirstClearReward(): {questMaster.HasFirstClearReward()}");
            Log($"firstClearItemType: '{questMaster.firstClearItemType}'");
            Log($"firstClearItemId: {questMaster.firstClearItemId}");
            Log($"firstClearItemQuantity: {questMaster.firstClearItemQuantity}");

            if (currentQuestDetail.userQuestData != null)
            {
                Log($"userQuestData.clearCount: {currentQuestDetail.userQuestData.clearCount}");
            }
            else
            {
                Log("userQuestData: null");
            }
        }
        else
        {
            Log("currentQuestDetail または questMaster が null です");
        }
    }
#endif

    #endregion
}