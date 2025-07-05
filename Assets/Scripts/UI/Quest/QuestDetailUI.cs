using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// クエスト詳細パネル制御クラス
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

    // 内部状態
    private QuestDetailData currentQuestDetail;
    private List<MonsterSlotUI> monsterSlots;
    private List<DropItemSlotUI> dropItemSlots;
    private List<FirstClearRewardSlotUI> firstClearRewardSlots;

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
        monsterSlots = new List<MonsterSlotUI>();
        dropItemSlots = new List<DropItemSlotUI>();
        firstClearRewardSlots = new List<FirstClearRewardSlotUI>();

        // ボタンイベント設定
        if (startBattleButton != null)
        {
            startBattleButton.onClick.RemoveAllListeners();
            startBattleButton.onClick.AddListener(OnStartBattleButtonClicked);
        }

        // 初期状態設定
        if (firstClearRewardSection != null)
        {
            firstClearRewardSection.SetActive(false);
        }

        if (availabilityPanel != null)
        {
            availabilityPanel.SetActive(false);
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// クエスト詳細を表示
    /// </summary>
    /// <param name="questDetail">クエスト詳細データ</param>
    public void DisplayQuestDetail(QuestDetailData questDetail)
    {
        try
        {
            if (questDetail == null)
            {
                LogError("QuestDetailDataがnullです");
                return;
            }

            currentQuestDetail = questDetail;

            Log($"クエスト詳細表示開始: {questDetail.questMaster?.questName}");

            // 基本情報表示
            DisplayBasicInfo();

            // 条件・進行状況表示
            DisplayQuestConditions();
            DisplayQuestProgress();

            // 報酬表示
            DisplayBasicRewards();
            DisplayFirstClearReward();

            // モンスター・ドロップアイテム表示
            DisplaySpawnMonsters();
            DisplayDropItems();

            // ボタン状態更新
            UpdateStartBattleButton();

            // 利用可能性表示
            DisplayAvailability();

            Log("クエスト詳細表示完了");
        }
        catch (Exception e)
        {
            LogError($"クエスト詳細表示エラー: {e.Message}");
        }
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
        if (questTitleText != null)
        {
            questTitleText.text = questMaster.questName;
        }

        // 説明
        if (questDescriptionText != null)
        {
            questDescriptionText.text = questMaster.description;
        }

        // クエストタイプ
        if (questTypeText != null)
        {
            questTypeText.text = GetQuestTypeDisplayName(questMaster.questType);
        }

        // タイプアイコン
        if (questTypeIcon != null)
        {
            LoadQuestTypeIcon(questMaster.questType);
        }
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
            questTypeIcon.gameObject.SetActive(false);
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
        if (needLevelText != null)
        {
            needLevelText.text = $"必要レベル: Lv.{questMaster.needLevel}";
        }

        // 必要スタミナ
        if (requiredStaminaText != null)
        {
            requiredStaminaText.text = $"消費スタミナ: {questMaster.requiredStamina}";
        }

        // 推奨戦闘力
        if (recommendedPowerText != null)
        {
            recommendedPowerText.text = $"推奨戦闘力: {questMaster.recommendedPower:N0}";
        }

        // ターン制限
        if (turnLimitText != null)
        {
            if (questMaster.HasTurnLimit())
            {
                turnLimitText.text = $"ターン制限: {questMaster.turnLimit}ターン";
                turnLimitText.gameObject.SetActive(true);
            }
            else
            {
                turnLimitText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// クエスト進行状況を表示
    /// </summary>
    private void DisplayQuestProgress()
    {
        if (currentQuestDetail?.userQuestData == null || currentQuestDetail?.questMaster == null) return;

        var userQuest = currentQuestDetail.userQuestData;
        var questMaster = currentQuestDetail.questMaster;

        // クリア回数
        if (clearCountText != null)
        {
            clearCountText.text = $"クリア回数: {userQuest.clearCount}";
        }

        // 最大クリア回数
        if (maxClearCountText != null)
        {
            if (questMaster.IsUnlimitedClear())
            {
                maxClearCountText.text = "制限なし";
            }
            else
            {
                maxClearCountText.text = $"/ {questMaster.dailyClearLimit}";
            }
        }

        // 進行度スライダー
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
                clearProgressSlider.value = userQuest.clearCount;
            }
        }
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
        if (expRewardText != null)
        {
            expRewardText.text = $"EXP: {questMaster.rewardExp:N0}";
        }

        // ゴールド報酬
        if (goldRewardText != null)
        {
            goldRewardText.text = $"ゴールド: {questMaster.rewardGold:N0}";
        }
    }

    /// <summary>
    /// 初回クリア報酬を表示
    /// </summary>
    private void DisplayFirstClearReward()
    {
        if (currentQuestDetail?.questMaster == null) return;

        var questMaster = currentQuestDetail.questMaster;
        bool hasFirstClearReward = questMaster.HasFirstClearReward();
        bool isFirstClear = currentQuestDetail.userQuestData?.clearCount == 0;

        if (firstClearRewardSection != null)
        {
            firstClearRewardSection.SetActive(hasFirstClearReward && isFirstClear);
        }

        if (hasFirstClearReward && isFirstClear)
        {
            ClearFirstClearRewardSlots();
            CreateFirstClearRewardSlot(questMaster);
        }
    }

    /// <summary>
    /// 初回クリア報酬スロットを作成
    /// </summary>
    /// <param name="questMaster">クエストマスターデータ</param>
    private void CreateFirstClearRewardSlot(QuestMasterData questMaster)
    {
        try
        {
            if (firstClearRewardSlotPrefab == null || firstClearRewardParent == null) return;

            var slotObject = Instantiate(firstClearRewardSlotPrefab, firstClearRewardParent);
            var rewardSlot = slotObject.GetComponent<FirstClearRewardSlotUI>();

            if (rewardSlot != null)
            {
                rewardSlot.Initialize(questMaster);
                firstClearRewardSlots.Add(rewardSlot);
                Log("初回クリア報酬スロット作成");
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
        ClearMonsterSlots();

        if (currentQuestDetail?.spawnMonsters == null) return;

        var monsters = currentQuestDetail.spawnMonsters;
        int displayCount = Mathf.Min(monsters.Count, maxDisplayMonsters);

        if (monsterSectionTitle != null)
        {
            monsterSectionTitle.text = $"出現モンスター ({displayCount}体)";
        }

        for (int i = 0; i < displayCount; i++)
        {
            CreateMonsterSlot(monsters[i]);
        }

        Log($"出現モンスター表示: {displayCount}体");
    }

    /// <summary>
    /// モンスタースロットを作成
    /// </summary>
    /// <param name="monsterData">モンスターデータ</param>
    private void CreateMonsterSlot(MonsterMasterData monsterData)
    {
        try
        {
            if (monsterSlotPrefab == null || monsterListParent == null) return;

            var slotObject = Instantiate(monsterSlotPrefab, monsterListParent);
            var monsterSlot = slotObject.GetComponent<MonsterSlotUI>();

            if (monsterSlot != null)
            {
                monsterSlot.Initialize(monsterData);
                monsterSlots.Add(monsterSlot);
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
        ClearDropItemSlots();

        if (currentQuestDetail?.dropTable?.dropItems == null) return;

        var dropItems = currentQuestDetail.dropTable.dropItems;
        int displayCount = Mathf.Min(dropItems.Count, maxDisplayDropItems);

        if (dropItemSectionTitle != null)
        {
            dropItemSectionTitle.text = $"ドロップアイテム ({displayCount}種類)";
        }

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
        try
        {
            if (dropItemSlotPrefab == null || dropItemListParent == null) return;

            var slotObject = Instantiate(dropItemSlotPrefab, dropItemListParent);
            var dropItemSlot = slotObject.GetComponent<DropItemSlotUI>();

            if (dropItemSlot != null)
            {
                dropItemSlot.Initialize(dropItem);
                dropItemSlots.Add(dropItemSlot);
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

        if (startBattleButtonText != null)
        {
            startBattleButtonText.text = canStart ? "出撃" : "出撃不可";
        }
    }

    /// <summary>
    /// 利用可能性を表示
    /// </summary>
    private void DisplayAvailability()
    {
        if (availabilityPanel == null || currentQuestDetail == null) return;

        bool showPanel = !currentQuestDetail.isAvailable;
        availabilityPanel.SetActive(showPanel);

        if (showPanel && availabilityReasonText != null)
        {
            availabilityReasonText.text = currentQuestDetail.availabilityReason;
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
    }

    /// <summary>
    /// モンスタースロットをクリア
    /// </summary>
    private void ClearMonsterSlots()
    {
        foreach (var slot in monsterSlots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }
        monsterSlots.Clear();
    }

    /// <summary>
    /// ドロップアイテムスロットをクリア
    /// </summary>
    private void ClearDropItemSlots()
    {
        foreach (var slot in dropItemSlots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }
        dropItemSlots.Clear();
    }

    /// <summary>
    /// 初回クリア報酬スロットをクリア
    /// </summary>
    private void ClearFirstClearRewardSlots()
    {
        foreach (var slot in firstClearRewardSlots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }
        firstClearRewardSlots.Clear();
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
        Log($"モンスタースロット: {monsterSlots.Count}, " +
            $"ドロップアイテムスロット: {dropItemSlots.Count}, " +
            $"初回報酬スロット: {firstClearRewardSlots.Count}");
    }
#endif

    #endregion
}