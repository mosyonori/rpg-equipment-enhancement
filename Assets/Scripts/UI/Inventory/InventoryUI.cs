using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// InventoryUI - スキル機能完全実装版
/// 装備、強化アイテム、補助アイテム、スキルの4つのタブを管理
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("タブ設定")]
    [SerializeField] private Button equipmentTabButton;
    [SerializeField] private Button enhanceItemTabButton;
    [SerializeField] private Button supportItemTabButton;
    [SerializeField] private Button skillTabButton; // スキルタブボタン
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject enhanceItemPanel;
    [SerializeField] private GameObject supportItemPanel;
    [SerializeField] private GameObject skillPanel; // スキルパネル

    [Header("装備パネル")]
    [SerializeField] private Transform equipmentGridParent;
    [SerializeField] private GameObject equipmentSlotPrefab;
    [SerializeField] private ScrollRect equipmentScrollRect;

    [Header("強化アイテムパネル")]
    [SerializeField] private Transform enhanceItemGridParent;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private ScrollRect enhanceItemScrollRect;

    [Header("補助アイテムパネル")]
    [SerializeField] private Transform supportItemGridParent;
    [SerializeField] private ScrollRect supportItemScrollRect;

    [Header("スキルパネル")]
    [SerializeField] private Transform skillGridParent; // スキルグリッド親オブジェクト
    [SerializeField] private GameObject skillSlotPrefab; // スキルスロットプレハブ
    [SerializeField] private ScrollRect skillScrollRect; // スキルスクロールビュー

    [Header("情報表示")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI gemText;
    [SerializeField] private TextMeshProUGUI equipmentCountText;
    [SerializeField] private TextMeshProUGUI totalPowerText;

    [Header("アイテム詳細表示")]
    [SerializeField] private GameObject itemDetailPanel;
    [SerializeField] private TextMeshProUGUI detailItemNameText;
    [SerializeField] private Image detailItemIconImage;
    [SerializeField] private TextMeshProUGUI detailItemDescriptionText;
    [SerializeField] private TextMeshProUGUI detailItemQuantityText;

    [Header("装備詳細ステータス表示")]
    [SerializeField] private GameObject equipmentDetailPanel;
    [SerializeField] private TextMeshProUGUI equipmentNameText;
    [SerializeField] private Image equipmentIconImage;
    [SerializeField] private TextMeshProUGUI equipmentEnhanceValueText;
    [SerializeField] private TextMeshProUGUI equipmentStaminaText;
    [SerializeField] private TextMeshProUGUI equipmentHpText;
    [SerializeField] private TextMeshProUGUI equipmentOffenseText;
    [SerializeField] private TextMeshProUGUI equipmentDefenseText;
    [SerializeField] private TextMeshProUGUI equipmentSpeedText;
    [SerializeField] private TextMeshProUGUI equipmentCriticalRateText;
    [SerializeField] private TextMeshProUGUI equipmentCriticalDamageText;
    [SerializeField] private TextMeshProUGUI equipmentFireOffenceText;
    [SerializeField] private TextMeshProUGUI equipmentWaterOffenceText;
    [SerializeField] private TextMeshProUGUI equipmentWindOffenceText;
    [SerializeField] private TextMeshProUGUI equipmentEarthOffenceText;

    [Header("スキル詳細表示")]
    [SerializeField] private GameObject skillDetailPanel; // スキル詳細パネル
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private Image skillIconImage;
    [SerializeField] private TextMeshProUGUI skillTypeText;
    [SerializeField] private TextMeshProUGUI skillAttributeText;
    [SerializeField] private TextMeshProUGUI skillRarityText;
    [SerializeField] private TextMeshProUGUI skillDamageText;
    [SerializeField] private TextMeshProUGUI skillTargetText;
    [SerializeField] private TextMeshProUGUI skillCoolTimeText;
    [SerializeField] private TextMeshProUGUI skillHpCostText;
    [SerializeField] private TextMeshProUGUI skillMpCostText;
    [SerializeField] private TextMeshProUGUI skillDescriptionText;

    [Header("フィルター・ソート")]
    [SerializeField] private TMP_Dropdown sortDropdown;
    [SerializeField] private TMP_Dropdown filterDropdown;
    [SerializeField] private Button refreshButton;

    [Header("装備管理機能")]
    [SerializeField] private Button favoriteButton;
    [SerializeField] private TextMeshProUGUI favoriteButtonText;
    [SerializeField] private Image favoriteButtonIcon;
    [SerializeField] private Button lockButton;
    [SerializeField] private TextMeshProUGUI lockButtonText;
    [SerializeField] private Image lockButtonIcon;

    [Header("装備削除機能")]
    [SerializeField] private Button equipmentDeleteButton;
    [SerializeField] private GameObject deleteConfirmationPanel;
    [SerializeField] private GameObject lockedEquipmentWarningPanel;
    [SerializeField] private Button deleteConfirmYesButton;
    [SerializeField] private Button deleteConfirmNoButton;
    [SerializeField] private Button warningOkButton;
    [SerializeField] private TextMeshProUGUI warningMessageText;
    [SerializeField] private TextMeshProUGUI deleteTargetNameText;
    [SerializeField] private TextMeshProUGUI deleteTargetEnhanceText;

    [Header("デバッグ")]
    [SerializeField] private bool enableDebugLog = true;

    // 現在の表示状態
    private InventoryTab currentTab = InventoryTab.Equipment;
    private List<EquipmentSlotUI> equipmentSlots = new List<EquipmentSlotUI>();
    private List<ItemSlotUI> enhanceItemSlots = new List<ItemSlotUI>();
    private List<ItemSlotUI> supportItemSlots = new List<ItemSlotUI>();
    private List<SkillSlotUI> skillSlots = new List<SkillSlotUI>(); // スキルスロットリスト

    // 選択状態
    private UserEquipmentData selectedEquipment;
    private UserItemData selectedItem;
    private UserSkillData selectedSkill; // 選択されたスキル

    #region Unity Lifecycle

    private void Awake()
    {
        SetupButtons();
        SetupDropdowns();
    }

    private void Start()
    {
        // イベント購読
        SubscribeToEvents();

        // 初期表示
        ShowTab(InventoryTab.Equipment);
        RefreshDisplay();
    }

    private void OnDestroy()
    {
        // イベント購読解除
        UnsubscribeFromEvents();
    }

    #endregion

    #region 初期化

    private void SetupButtons()
    {
        // タブボタン設定
        if (equipmentTabButton != null)
        {
            equipmentTabButton.onClick.AddListener(() => ShowTab(InventoryTab.Equipment));
        }

        if (enhanceItemTabButton != null)
        {
            enhanceItemTabButton.onClick.AddListener(() => ShowTab(InventoryTab.EnhanceItem));
        }

        if (supportItemTabButton != null)
        {
            supportItemTabButton.onClick.AddListener(() => ShowTab(InventoryTab.SupportItem));
        }

        // スキルタブボタン設定
        if (skillTabButton != null)
        {
            skillTabButton.onClick.AddListener(() => ShowTab(InventoryTab.Skill));
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshDisplay);
        }

        // 装備管理ボタン設定
        if (favoriteButton != null)
        {
            favoriteButton.onClick.AddListener(OnFavoriteButtonClicked);
        }

        if (lockButton != null)
        {
            lockButton.onClick.AddListener(OnLockButtonClicked);
        }

        // 装備削除機能のボタン設定
        if (equipmentDeleteButton != null)
        {
            equipmentDeleteButton.onClick.AddListener(OnEquipmentDeleteButtonClicked);
        }

        if (deleteConfirmYesButton != null)
        {
            deleteConfirmYesButton.onClick.AddListener(OnDeleteConfirmYes);
        }

        if (deleteConfirmNoButton != null)
        {
            deleteConfirmNoButton.onClick.AddListener(OnDeleteConfirmNo);
        }

        if (warningOkButton != null)
        {
            warningOkButton.onClick.AddListener(OnWarningOkClicked);
        }
    }

    private void SetupDropdowns()
    {
        if (sortDropdown != null)
        {
            sortDropdown.onValueChanged.AddListener(OnSortChanged);
        }

        if (filterDropdown != null)
        {
            filterDropdown.onValueChanged.AddListener(OnFilterChanged);
        }
    }

    private void SubscribeToEvents()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnInventoryChanged += OnInventoryChanged;
            InventoryManager.OnEquipmentAdded += OnEquipmentAdded;
            InventoryManager.OnItemAdded += OnItemAdded;
            InventoryManager.OnEquipmentEquipped += OnEquipmentEquipped;
            InventoryManager.OnEquipmentUnequipped += OnEquipmentUnequipped;
        }

        if (SaveDataManager.Instance != null)
        {
            SaveDataManager.OnDataLoaded += OnSaveDataLoaded;
        }

        // スキルマネージャーのイベント購読
        if (SkillManager.Instance != null)
        {
            SkillManager.OnSkillAdded += OnSkillAdded;
            SkillManager.OnSkillRemoved += OnSkillRemoved;
            SkillManager.OnSkillInventoryChanged += OnSkillInventoryChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnInventoryChanged -= OnInventoryChanged;
            InventoryManager.OnEquipmentAdded -= OnEquipmentAdded;
            InventoryManager.OnItemAdded -= OnItemAdded;
            InventoryManager.OnEquipmentEquipped -= OnEquipmentEquipped;
            InventoryManager.OnEquipmentUnequipped -= OnEquipmentUnequipped;
        }

        if (SaveDataManager.Instance != null)
        {
            SaveDataManager.OnDataLoaded -= OnSaveDataLoaded;
        }

        // スキルマネージャーのイベント購読解除
        if (SkillManager.Instance != null)
        {
            SkillManager.OnSkillAdded -= OnSkillAdded;
            SkillManager.OnSkillRemoved -= OnSkillRemoved;
            SkillManager.OnSkillInventoryChanged -= OnSkillInventoryChanged;
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 表示を更新
    /// </summary>
    public void RefreshDisplay()
    {
        if (!IsManagersReady())
        {
            DebugLog("マネージャーが準備できていません");
            return;
        }

        UpdatePlayerInfo();
        UpdateCurrentTab();
        UpdateEquipmentManagementButtons();
        DebugLog("インベントリ表示を更新しました");
    }

    /// <summary>
    /// タブを表示
    /// </summary>
    public void ShowTab(InventoryTab tab)
    {
        currentTab = tab;

        // パネルの表示/非表示
        if (equipmentPanel != null) equipmentPanel.SetActive(tab == InventoryTab.Equipment);
        if (enhanceItemPanel != null) enhanceItemPanel.SetActive(tab == InventoryTab.EnhanceItem);
        if (supportItemPanel != null) supportItemPanel.SetActive(tab == InventoryTab.SupportItem);
        if (skillPanel != null) skillPanel.SetActive(tab == InventoryTab.Skill); // スキルパネル

        // タブボタンの見た目更新
        UpdateTabButtonAppearance();

        // 詳細表示を非表示（タブ切り替え後にリセット）
        HideAllDetails();

        // 該当タブの内容を更新
        UpdateCurrentTab();

        // 選択状態をクリア
        selectedEquipment = null;
        selectedItem = null;
        selectedSkill = null; // スキル選択状態もクリア

        // 装備管理ボタンの状態更新
        UpdateEquipmentManagementButtons();

        DebugLog($"タブを切り替えました: {tab}");
    }

    #endregion

    #region 内部メソッド - 表示更新

    private void UpdateCurrentTab()
    {
        switch (currentTab)
        {
            case InventoryTab.Equipment:
                UpdateEquipmentDisplay();
                break;
            case InventoryTab.EnhanceItem:
                UpdateEnhanceItemDisplay();
                break;
            case InventoryTab.SupportItem:
                UpdateSupportItemDisplay();
                break;
            case InventoryTab.Skill:
                UpdateSkillDisplay(); // スキル表示更新
                break;
        }
    }

    private void UpdatePlayerInfo()
    {
        var saveData = SaveDataManager.Instance?.CurrentSaveData;
        if (saveData == null) return;

        if (playerNameText != null) playerNameText.text = saveData.playerName;
        if (playerLevelText != null) playerLevelText.text = $"Lv.{saveData.playerLevel}";
        if (goldText != null) goldText.text = saveData.gold.ToString("N0");
        if (gemText != null) gemText.text = saveData.gems.ToString();

        if (equipmentCountText != null)
        {
            int currentCount = saveData.equipments.Count;
            int maxCount = 1000;
            equipmentCountText.text = $"{currentCount}/{maxCount}";
        }

        if (totalPowerText != null)
        {
            int totalPower = InventoryManager.Instance?.CalculateTotalPower() ?? 0;
            totalPowerText.text = totalPower.ToString();
        }
    }

    private void UpdateEquipmentDisplay()
    {
        if (equipmentGridParent == null || equipmentSlotPrefab == null) return;

        var equipments = InventoryManager.Instance.GetAllEquipments();

        // 必要な数だけスロットを作成/削除
        AdjustSlotCount(equipmentSlots, equipments.Count, equipmentGridParent, equipmentSlotPrefab, true);

        // データを設定
        for (int i = 0; i < equipments.Count; i++)
        {
            equipmentSlots[i].SetEquipmentData(equipments[i]);
            equipmentSlots[i].OnSlotClicked = OnEquipmentSlotClicked;
        }

        DebugLog($"装備表示を更新: {equipments.Count}個");
    }

    private void UpdateEnhanceItemDisplay()
    {
        if (enhanceItemGridParent == null || itemSlotPrefab == null) return;

        var items = InventoryManager.Instance.GetItemsByType(ItemType.EnhanceItem);

        // 必要な数だけスロットを作成/削除
        AdjustSlotCount(enhanceItemSlots, items.Count, enhanceItemGridParent, itemSlotPrefab, false);

        // データを設定
        for (int i = 0; i < items.Count; i++)
        {
            enhanceItemSlots[i].SetItemData(items[i]);
            enhanceItemSlots[i].OnSlotClicked = OnItemSlotClicked;
        }

        DebugLog($"強化アイテム表示を更新: {items.Count}個");
    }

    private void UpdateSupportItemDisplay()
    {
        if (supportItemGridParent == null || itemSlotPrefab == null) return;

        var items = InventoryManager.Instance.GetItemsByType(ItemType.SupportItem);

        // 必要な数だけスロットを作成/削除
        AdjustSlotCount(supportItemSlots, items.Count, supportItemGridParent, itemSlotPrefab, false);

        // データを設定
        for (int i = 0; i < items.Count; i++)
        {
            supportItemSlots[i].SetItemData(items[i]);
            supportItemSlots[i].OnSlotClicked = OnItemSlotClicked;
        }

        DebugLog($"補助アイテム表示を更新: {items.Count}個");
    }

    /// <summary>
    /// スキル表示を更新
    /// </summary>
    private void UpdateSkillDisplay()
    {
        if (skillGridParent == null || skillSlotPrefab == null) return;

        var skills = InventoryManager.Instance.GetAllSkills();

        // 必要な数だけスキルスロットを作成/削除
        AdjustSkillSlotCount(skills.Count);

        // データを設定
        for (int i = 0; i < skills.Count; i++)
        {
            skillSlots[i].SetSkillData(skills[i]);
            skillSlots[i].OnSlotClicked = OnSkillSlotClicked;
        }

        DebugLog($"スキル表示を更新: {skills.Count}個");
    }

    /// <summary>
    /// スキルスロット数を調整
    /// </summary>
    private void AdjustSkillSlotCount(int targetCount)
    {
        // 不足分を作成
        while (skillSlots.Count < targetCount)
        {
            GameObject newSlot = Instantiate(skillSlotPrefab, skillGridParent);
            SkillSlotUI slotComponent = newSlot.GetComponent<SkillSlotUI>();

            if (slotComponent != null)
            {
                skillSlots.Add(slotComponent);
            }
        }

        // 余分なものを非表示
        for (int i = 0; i < skillSlots.Count; i++)
        {
            skillSlots[i].gameObject.SetActive(i < targetCount);
        }
    }

    private void AdjustSlotCount<T>(List<T> slotList, int targetCount, Transform parent, GameObject prefab, bool isEquipmentSlot) where T : Component
    {
        // 不足分を作成
        while (slotList.Count < targetCount)
        {
            GameObject newSlot = Instantiate(prefab, parent);
            T slotComponent = isEquipmentSlot ?
                newSlot.GetComponent<EquipmentSlotUI>() as T :
                newSlot.GetComponent<ItemSlotUI>() as T;

            if (slotComponent != null)
            {
                slotList.Add(slotComponent);
            }
        }

        // 余分なものを非表示
        for (int i = 0; i < slotList.Count; i++)
        {
            slotList[i].gameObject.SetActive(i < targetCount);
        }
    }

    private void UpdateTabButtonAppearance()
    {
        // タブボタンの見た目を更新（色やスケールなど）
        SetTabButtonActive(equipmentTabButton, currentTab == InventoryTab.Equipment);
        SetTabButtonActive(enhanceItemTabButton, currentTab == InventoryTab.EnhanceItem);
        SetTabButtonActive(supportItemTabButton, currentTab == InventoryTab.SupportItem);
        SetTabButtonActive(skillTabButton, currentTab == InventoryTab.Skill); // スキルタブボタン
    }

    private void SetTabButtonActive(Button button, bool isActive)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.normalColor = isActive ? Color.white : Color.white;
        button.colors = colors;
    }

    /// <summary>
    /// 装備スロットの表示を強制更新
    /// </summary>
    private void ForceUpdateEquipmentSlots()
    {
        if (currentTab != InventoryTab.Equipment) return;

        foreach (var slot in equipmentSlots)
        {
            if (slot.gameObject.activeInHierarchy)
            {
                var equipment = slot.GetEquipmentData();
                if (equipment != null)
                {
                    slot.SetEquipmentData(equipment);
                }
            }
        }

        DebugLog("装備スロットの表示を強制更新しました");
    }

    #endregion

    #region 装備管理機能

    private void OnFavoriteButtonClicked()
    {
        if (currentTab != InventoryTab.Equipment || selectedEquipment == null)
        {
            DebugLog("お気に入り操作: 装備が選択されていないか、装備タブではありません");
            return;
        }

        bool success = InventoryManager.Instance.ToggleEquipmentFavorite(selectedEquipment.userEquipmentId);

        if (success)
        {
            DebugLog($"お気に入り状態を変更: {selectedEquipment.userEquipmentId} -> {!selectedEquipment.isFavorite}");
            UpdateEquipmentManagementButtons();
            UpdateSelectionVisual();
            ForceUpdateEquipmentSlots();
        }
    }

    private void OnLockButtonClicked()
    {
        if (currentTab != InventoryTab.Equipment || selectedEquipment == null)
        {
            DebugLog("ロック操作: 装備が選択されていないか、装備タブではありません");
            return;
        }

        bool success = InventoryManager.Instance.ToggleEquipmentLock(selectedEquipment.userEquipmentId);

        if (success)
        {
            DebugLog($"ロック状態を変更: {selectedEquipment.userEquipmentId} -> {!selectedEquipment.isLocked}");
            UpdateEquipmentManagementButtons();
            UpdateSelectionVisual();
            ForceUpdateEquipmentSlots();
        }
    }

    private void UpdateEquipmentManagementButtons()
    {
        bool shouldShowButtons = (currentTab == InventoryTab.Equipment) && (selectedEquipment != null);

        UpdateFavoriteButton(shouldShowButtons);
        UpdateLockButton(shouldShowButtons);
    }

    private void UpdateFavoriteButton(bool shouldShow)
    {
        if (favoriteButton == null) return;

        favoriteButton.gameObject.SetActive(shouldShow);

        if (shouldShow)
        {
            bool isFavorite = selectedEquipment.isFavorite;

            if (favoriteButtonText != null)
            {
                favoriteButtonText.text = isFavorite ? "お気に入り解除" : "お気に入り登録";
            }

            if (favoriteButtonIcon != null)
            {
                favoriteButtonIcon.color = isFavorite ? Color.red : Color.white;
            }

            favoriteButton.interactable = true;
        }
    }

    private void UpdateLockButton(bool shouldShow)
    {
        if (lockButton == null) return;

        lockButton.gameObject.SetActive(shouldShow);

        if (shouldShow)
        {
            bool isLocked = selectedEquipment.isLocked;

            if (lockButtonText != null)
            {
                lockButtonText.text = isLocked ? "ロック解除" : "ロック";
            }

            if (lockButtonIcon != null)
            {
                lockButtonIcon.color = isLocked ? Color.grey : Color.white;
            }

            lockButton.interactable = true;
        }
    }

    #endregion

    #region 装備削除機能

    private void OnEquipmentDeleteButtonClicked()
    {
        if (currentTab != InventoryTab.Equipment || selectedEquipment == null) return;

        ShowDeleteConfirmationPanel();
    }

    private void ShowDeleteConfirmationPanel()
    {
        if (deleteConfirmationPanel == null || selectedEquipment == null) return;

        var masterData = MasterDataManager.Instance.GetEquipmentData(selectedEquipment.equipmentMasterId);
        if (masterData != null)
        {
            if (deleteTargetNameText != null)
            {
                deleteTargetNameText.text = masterData.equipmentName;
            }

            if (deleteTargetEnhanceText != null)
            {
                deleteTargetEnhanceText.text = $"+{selectedEquipment.currentEnhancedValue}";
            }
        }

        deleteConfirmationPanel.SetActive(true);
    }

    private void OnDeleteConfirmYes()
    {
        if (selectedEquipment == null) return;

        var (canDelete, errorMessage) = InventoryManager.Instance.CanDeleteEquipment(selectedEquipment.userEquipmentId);

        if (!canDelete)
        {
            ShowWarningWithMessage(errorMessage);
            return;
        }

        bool success = InventoryManager.Instance.DeleteEquipment(selectedEquipment.userEquipmentId);

        if (success)
        {
            ClearEquipmentSelection();
            HideAllDeletePanels();
        }
    }

    private void OnDeleteConfirmNo()
    {
        HideAllDeletePanels();
    }

    private void OnWarningOkClicked()
    {
        HideAllDeletePanels();
    }

    private void ShowWarningWithMessage(string message)
    {
        if (lockedEquipmentWarningPanel == null) return;

        if (deleteConfirmationPanel != null)
        {
            deleteConfirmationPanel.SetActive(false);
        }

        if (warningMessageText != null)
        {
            warningMessageText.text = message;
        }

        lockedEquipmentWarningPanel.SetActive(true);
    }

    private void HideAllDeletePanels()
    {
        if (deleteConfirmationPanel != null)
        {
            deleteConfirmationPanel.SetActive(false);
        }

        if (lockedEquipmentWarningPanel != null)
        {
            lockedEquipmentWarningPanel.SetActive(false);
        }
    }

    private void ClearEquipmentSelection()
    {
        selectedEquipment = null;
        selectedItem = null;
        selectedSkill = null; // スキル選択もクリア

        HideAllDetails();
        UpdateEquipmentManagementButtons();
        UpdateSelectionVisual();
    }

    #endregion

    #region スキル詳細表示

    /// <summary>
    /// スキル詳細を表示
    /// </summary>
    private void ShowSkillDetail(UserSkillData skill)
    {
        if (skill == null) return;

        // 他の詳細パネルを非表示
        HideItemDetail();
        HideEquipmentDetail();

        if (skillDetailPanel == null) return;

        var masterData = MasterDataManager.Instance?.GetSkillData(skill.skillMasterId);
        if (masterData != null)
        {
            SetSkillDetailData(skill, masterData);
        }

        skillDetailPanel.SetActive(true);
        DebugLog($"スキル詳細を表示: {masterData?.skillName} ID:{skill.skillMasterId}");
    }

    /// <summary>
    /// スキル詳細データを設定
    /// </summary>
    private void SetSkillDetailData(UserSkillData skill, SkillMasterData masterData)
    {
        // 基本情報
        if (skillNameText != null) skillNameText.text = masterData.skillName;
        if (skillIconImage != null && masterData.skillIcon != null)
            skillIconImage.sprite = masterData.skillIcon;

        // スキル種別・属性・レアリティ
        if (skillTypeText != null) skillTypeText.text = GetSkillTypeDisplayName(masterData.skillType);
        if (skillAttributeText != null) skillAttributeText.text = GetAttributeDisplayName(masterData.attributeType);
        if (skillRarityText != null) skillRarityText.text = GetRarityDisplayName(masterData.rarity);

        // 詳細ステータス
        if (skillDamageText != null) skillDamageText.text = $"{masterData.skillDamageMultiplier:F1}倍";
        if (skillTargetText != null) skillTargetText.text = GetTargetTypeDisplayName(masterData.skillTargetType);
        if (skillCoolTimeText != null) skillCoolTimeText.text = $"{masterData.skillMaxCoolTime}ターン";
        if (skillHpCostText != null) skillHpCostText.text = masterData.skillHpCost.ToString();
        if (skillMpCostText != null) skillMpCostText.text = masterData.skillMpCost.ToString();
        if (skillDescriptionText != null) skillDescriptionText.text = masterData.description;
    }

    /// <summary>
    /// スキル詳細表示を非表示
    /// </summary>
    private void HideSkillDetail()
    {
        if (skillDetailPanel != null)
        {
            skillDetailPanel.SetActive(false);
            DebugLog("スキル詳細パネルを非表示にしました");
        }
    }

    #endregion

    #region アイテム詳細表示

    /// <summary>
    /// アイテム詳細を表示
    /// </summary>
    private void ShowItemDetail(UserItemData item)
    {
        if (item == null) return;

        // 装備詳細パネルを明示的に非表示
        HideEquipmentDetail();
        HideSkillDetail(); // スキル詳細も非表示

        if (itemDetailPanel == null) return;

        // アイテムタイプに応じてマスターデータを取得
        if (item.itemType == ItemType.EnhanceItem)
        {
            var masterData = MasterDataManager.Instance?.GetEnhanceItemData(item.itemMasterId);
            if (masterData != null)
            {
                SetDetailPanelData(masterData.enhanceItemName, masterData.enhanceItemIcon,
                                  masterData.description, item.quantity);
            }
        }
        else if (item.itemType == ItemType.SupportItem)
        {
            var masterData = MasterDataManager.Instance?.GetSupportItemData(item.itemMasterId);
            if (masterData != null)
            {
                SetDetailPanelData(masterData.supportItemName, masterData.supportItemIcon,
                                  masterData.description, item.quantity);
            }
        }

        // アイテム詳細パネルを表示
        itemDetailPanel.SetActive(true);

        DebugLog($"アイテム詳細を表示: {item.itemType} ID:{item.itemMasterId}");
    }

    /// <summary>
    /// 装備詳細を表示
    /// </summary>
    private void ShowEquipmentDetail(UserEquipmentData equipment)
    {
        if (equipment == null) return;

        // アイテム詳細パネルを明示的に非表示
        HideItemDetail();
        HideSkillDetail(); // スキル詳細も非表示

        if (equipmentDetailPanel == null) return;

        var masterData = MasterDataManager.Instance?.GetEquipmentData(equipment.equipmentMasterId);
        if (masterData != null)
        {
            SetEquipmentDetailData(equipment, masterData);
        }

        // 装備詳細パネルを表示
        equipmentDetailPanel.SetActive(true);

        DebugLog($"装備詳細を表示: {masterData?.equipmentName} ID:{equipment.equipmentMasterId}");
    }

    /// <summary>
    /// 装備詳細データを設定
    /// </summary>
    private void SetEquipmentDetailData(UserEquipmentData equipment, EquipmentMasterData masterData)
    {
        // 基本情報
        if (equipmentNameText != null) equipmentNameText.text = masterData.equipmentName;
        if (equipmentIconImage != null && masterData.equipmentIcon != null)
            equipmentIconImage.sprite = masterData.equipmentIcon;

        // 強化値・耐久
        if (equipmentEnhanceValueText != null)
            equipmentEnhanceValueText.text = $"+{equipment.currentEnhancedValue}";
        if (equipmentStaminaText != null)
            equipmentStaminaText.text = equipment.currentEnhanceStamina.ToString();

        // 基本ステータス + 強化値を計算
        int totalHp = masterData.hp + equipment.enhancedHp;
        int totalOffense = masterData.offense + equipment.enhancedOffense;
        int totalDefense = masterData.defense + equipment.enhancedDefense;
        int totalSpeed = masterData.speed + equipment.enhancedSpeed;
        int totalCriticalRate = masterData.criticalRate + equipment.enhancedCriticalRate;
        int totalCriticalDamage = masterData.criticalDamageRate + equipment.enhancedCriticalDamageRate;
        int totalFireOffence = masterData.fireOffence + equipment.enhancedFireOffence;
        int totalWaterOffence = masterData.waterOffence + equipment.enhancedWaterOffence;
        int totalWindOffence = masterData.windOffence + equipment.enhancedWindOffence;
        int totalEarthOffence = masterData.earthOffence + equipment.enhancedEarthOffence;

        // ステータス表示
        if (equipmentHpText != null) equipmentHpText.text = totalHp.ToString();
        if (equipmentOffenseText != null) equipmentOffenseText.text = totalOffense.ToString();
        if (equipmentDefenseText != null) equipmentDefenseText.text = totalDefense.ToString();
        if (equipmentSpeedText != null) equipmentSpeedText.text = totalSpeed.ToString();
        if (equipmentCriticalRateText != null) equipmentCriticalRateText.text = $"{totalCriticalRate}%";
        if (equipmentCriticalDamageText != null) equipmentCriticalDamageText.text = $"{totalCriticalDamage}%";
        if (equipmentFireOffenceText != null) equipmentFireOffenceText.text = totalFireOffence.ToString();
        if (equipmentWaterOffenceText != null) equipmentWaterOffenceText.text = totalWaterOffence.ToString();
        if (equipmentWindOffenceText != null) equipmentWindOffenceText.text = totalWindOffence.ToString();
        if (equipmentEarthOffenceText != null) equipmentEarthOffenceText.text = totalEarthOffence.ToString();
    }

    /// <summary>
    /// アイテム詳細パネルにデータを設定
    /// </summary>
    private void SetDetailPanelData(string itemName, Sprite itemIcon, string description, int quantity)
    {
        if (detailItemNameText != null) detailItemNameText.text = itemName;
        if (detailItemIconImage != null && itemIcon != null) detailItemIconImage.sprite = itemIcon;
        if (detailItemDescriptionText != null) detailItemDescriptionText.text = description;
        if (detailItemQuantityText != null) detailItemQuantityText.text = $"所持数: {quantity}";
    }

    /// <summary>
    /// アイテム詳細表示を非表示
    /// </summary>
    private void HideItemDetail()
    {
        if (itemDetailPanel != null)
        {
            itemDetailPanel.SetActive(false);
            DebugLog("アイテム詳細パネルを非表示にしました");
        }
    }

    /// <summary>
    /// 装備詳細表示を非表示
    /// </summary>
    private void HideEquipmentDetail()
    {
        if (equipmentDetailPanel != null)
        {
            equipmentDetailPanel.SetActive(false);
            DebugLog("装備詳細パネルを非表示にしました");
        }
    }

    /// <summary>
    /// 全ての詳細表示を非表示
    /// </summary>
    private void HideAllDetails()
    {
        HideItemDetail();
        HideEquipmentDetail();
        HideSkillDetail(); // スキル詳細も非表示
        DebugLog("全ての詳細パネルを非表示にしました");
    }

    #endregion

    #region 表示名変換ユーティリティ

    private string GetSkillTypeDisplayName(SkillType skillType)
    {
        return skillType switch
        {
            SkillType.Attack => "攻撃系",
            SkillType.Heal => "回復系",
            SkillType.Buff => "バフ系",
            SkillType.Debuff => "デバフ系",
            SkillType.Support => "サポート系",
            SkillType.Special => "特殊系",
            _ => "不明"
        };
    }

    private string GetAttributeDisplayName(AttributeType attributeType)
    {
        return attributeType switch
        {
            AttributeType.Fire => "火",
            AttributeType.Water => "水",
            AttributeType.Wind => "風",
            AttributeType.Earth => "土",
            AttributeType.None => "無",
            _ => "不明"
        };
    }

    private string GetRarityDisplayName(RarityType rarity)
    {
        return rarity switch
        {
            RarityType.Common => "コモン",
            RarityType.Rare => "レア",
            RarityType.Epic => "エピック",
            RarityType.Legendary => "レジェンダリー",
            _ => "不明"
        };
    }

    private string GetTargetTypeDisplayName(TargetType targetType)
    {
        return targetType switch
        {
            TargetType.Self => "自分",
            TargetType.EnemySingle => "敵単体",
            TargetType.EnemyAll => "敵全体",
            TargetType.AllySingle => "味方単体",
            TargetType.AllyAll => "味方全体",
            TargetType.Random => "ランダム",
            _ => "不明"
        };
    }

    #endregion

    #region イベントハンドラー

    private void OnInventoryChanged()
    {
        RefreshDisplay();
        ForceUpdateEquipmentSlots();
    }

    private void OnEquipmentAdded(UserEquipmentData equipment)
    {
        if (currentTab == InventoryTab.Equipment)
        {
            UpdateEquipmentDisplay();
        }
        DebugLog($"装備が追加されました: {equipment.userEquipmentId}");
    }

    private void OnItemAdded(UserItemData item)
    {
        if ((currentTab == InventoryTab.EnhanceItem && item.itemType == ItemType.EnhanceItem) ||
            (currentTab == InventoryTab.SupportItem && item.itemType == ItemType.SupportItem))
        {
            UpdateCurrentTab();
        }
        DebugLog($"アイテムが追加されました: {item.userItemId}");
    }

    private void OnSaveDataLoaded(UserSaveData saveData)
    {
        RefreshDisplay();
        DebugLog("セーブデータが読み込まれました");
    }

    /// <summary>
    /// スキル追加イベント
    /// </summary>
    private void OnSkillAdded(UserSkillData skill)
    {
        if (currentTab == InventoryTab.Skill)
        {
            UpdateSkillDisplay();
        }
        DebugLog($"スキルが追加されました: {skill.userSkillId}");
    }

    /// <summary>
    /// スキル削除イベント
    /// </summary>
    private void OnSkillRemoved(UserSkillData skill)
    {
        if (currentTab == InventoryTab.Skill)
        {
            UpdateSkillDisplay();
        }

        // 削除されたスキルが選択されていた場合は選択解除
        if (selectedSkill != null && selectedSkill.userSkillId == skill.userSkillId)
        {
            selectedSkill = null;
            HideSkillDetail();
        }

        DebugLog($"スキルが削除されました: {skill.userSkillId}");
    }

    /// <summary>
    /// スキルインベントリ変更イベント
    /// </summary>
    private void OnSkillInventoryChanged()
    {
        if (currentTab == InventoryTab.Skill)
        {
            UpdateSkillDisplay();
        }
        DebugLog("スキルインベントリが変更されました");
    }

    private void OnEquipmentSlotClicked(UserEquipmentData equipment)
    {
        selectedEquipment = equipment;
        selectedItem = null;
        selectedSkill = null; // スキル選択解除

        UpdateSelectionVisual();
        ShowEquipmentDetail(equipment);
        UpdateEquipmentManagementButtons();

        DebugLog($"装備が選択されました: {equipment.userEquipmentId}");
    }

    private void OnItemSlotClicked(UserItemData item)
    {
        selectedItem = item;
        selectedEquipment = null;
        selectedSkill = null; // スキル選択解除

        UpdateSelectionVisual();
        ShowItemDetail(item);
        UpdateEquipmentManagementButtons();

        DebugLog($"アイテムが選択されました: {item.userItemId}");
    }

    /// <summary>
    /// スキルスロットクリックイベント
    /// </summary>
    private void OnSkillSlotClicked(UserSkillData skill)
    {
        selectedSkill = skill;
        selectedEquipment = null;
        selectedItem = null;

        UpdateSelectionVisual();
        ShowSkillDetail(skill);
        UpdateEquipmentManagementButtons(); // スキル以外では非表示

        DebugLog($"スキルが選択されました: {skill?.userSkillId ?? "null"}");
    }

    /// <summary>
    /// 装備装着イベント
    /// </summary>
    private void OnEquipmentEquipped(UserEquipmentData equipment)
    {
        DebugLog($"装備装着イベント: {equipment.userEquipmentId}");
        ForceUpdateEquipmentSlots();
        UpdatePlayerInfo();
    }

    /// <summary>
    /// 装備解除イベント
    /// </summary>
    private void OnEquipmentUnequipped(UserEquipmentData equipment)
    {
        DebugLog($"装備解除イベント: {equipment.userEquipmentId}");
        ForceUpdateEquipmentSlots();
        UpdatePlayerInfo();
    }

    private void OnSortChanged(int sortIndex)
    {
        DebugLog($"ソート変更: {sortIndex}");
        UpdateCurrentTab();
    }

    private void OnFilterChanged(int filterIndex)
    {
        DebugLog($"フィルタ変更: {filterIndex}");
        UpdateCurrentTab();
    }

    #endregion

    #region 内部メソッド - ユーティリティ

    private void UpdateSelectionVisual()
    {
        // 装備スロットの選択状態を更新
        foreach (var slot in equipmentSlots)
        {
            if (slot.gameObject.activeInHierarchy)
            {
                bool isSelected = selectedEquipment != null &&
                    slot.GetEquipmentData()?.userEquipmentId == selectedEquipment.userEquipmentId;
                slot.SetSelected(isSelected);
            }
        }

        // アイテムスロットの選択状態を更新
        var currentItemSlots = currentTab == InventoryTab.EnhanceItem ? enhanceItemSlots : supportItemSlots;
        foreach (var slot in currentItemSlots)
        {
            if (slot.gameObject.activeInHierarchy)
            {
                bool isSelected = selectedItem != null &&
                    slot.GetItemData()?.userItemId == selectedItem.userItemId;
                slot.SetSelected(isSelected);
            }
        }

        // スキルスロットの選択状態を更新
        foreach (var slot in skillSlots)
        {
            if (slot.gameObject.activeInHierarchy)
            {
                bool isSelected = selectedSkill != null &&
                    slot.GetSkillData()?.userSkillId == selectedSkill.userSkillId;
                slot.SetSelected(isSelected);
            }
        }
    }

    private bool IsManagersReady()
    {
        return InventoryManager.Instance != null &&
               InventoryManager.Instance.IsInitialized &&
               SaveDataManager.Instance != null &&
               SaveDataManager.Instance.IsDataLoaded &&
               MasterDataManager.Instance != null &&
               MasterDataManager.Instance.IsDataLoaded &&
               SkillManager.Instance != null &&
               SkillManager.Instance.IsInitialized;
    }

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[InventoryUI] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("表示を強制更新")]
    private void ForceRefresh()
    {
        RefreshDisplay();
    }

    [ContextMenu("装備タブを表示")]
    private void ShowEquipmentTab()
    {
        ShowTab(InventoryTab.Equipment);
    }

    [ContextMenu("強化アイテムタブを表示")]
    private void ShowEnhanceItemTab()
    {
        ShowTab(InventoryTab.EnhanceItem);
    }

    [ContextMenu("補助アイテムタブを表示")]
    private void ShowSupportItemTab()
    {
        ShowTab(InventoryTab.SupportItem);
    }

    [ContextMenu("スキルタブを表示")]
    private void ShowSkillTab()
    {
        ShowTab(InventoryTab.Skill);
    }

    [ContextMenu("詳細表示をテスト")]
    private void TestDetailDisplay()
    {
        if (selectedEquipment != null)
        {
            ShowEquipmentDetail(selectedEquipment);
        }
        else if (selectedItem != null)
        {
            ShowItemDetail(selectedItem);
        }
        else if (selectedSkill != null)
        {
            ShowSkillDetail(selectedSkill);
        }
        else
        {
            Debug.LogWarning("選択されたアイテムがありません");
        }
    }

    [ContextMenu("詳細表示を非表示")]
    private void TestHideDetail()
    {
        HideAllDetails();
    }

    [ContextMenu("装備管理ボタンテスト")]
    private void TestEquipmentManagementButtons()
    {
        if (selectedEquipment != null)
        {
            Debug.Log("=== 装備管理ボタンテスト ===");
            Debug.Log($"選択装備: {selectedEquipment.userEquipmentId}");
            Debug.Log($"お気に入り: {selectedEquipment.isFavorite}");
            Debug.Log($"ロック: {selectedEquipment.isLocked}");
            Debug.Log($"装備中: {selectedEquipment.isEquipped}");
            UpdateEquipmentManagementButtons();
        }
        else
        {
            Debug.LogWarning("装備が選択されていません");
        }
    }

    [ContextMenu("スキル表示テスト")]
    private void TestSkillDisplay()
    {
        UpdateSkillDisplay();
        Debug.Log("スキル表示をテスト更新しました");
    }

    [ContextMenu("テストデータ追加")]
    private void AddTestData()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsInitialized)
        {
            InventoryManager.Instance.AddEquipment(1);
            InventoryManager.Instance.AddItem(ItemType.EnhanceItem, 1, 5);
            InventoryManager.Instance.AddItem(ItemType.SupportItem, 1, 3);

            // スキル追加テスト
            if (SkillManager.Instance != null && SkillManager.Instance.IsInitialized)
            {
                SkillManager.Instance.AddSkill(1);
                Debug.Log("テストスキルを追加しました");
            }

            Debug.Log("テストデータを追加しました");
        }
    }
#endif

    #endregion
}

/// <summary>
/// インベントリタブの種類
/// </summary>
public enum InventoryTab
{
    Equipment,      // 装備
    EnhanceItem,    // 強化アイテム
    SupportItem,    // 補助アイテム
    Skill           // スキル
}