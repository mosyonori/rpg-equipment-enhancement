using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// インベントリUI全体を管理するメインコンポーネント
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("タブ設定")]
    [SerializeField] private Button equipmentTabButton;
    [SerializeField] private Button enhanceItemTabButton;
    [SerializeField] private Button supportItemTabButton;
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject enhanceItemPanel;
    [SerializeField] private GameObject supportItemPanel;

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
    [SerializeField] private Button equipmentDeleteButton;          // 装備削除ボタン
    [SerializeField] private GameObject deleteConfirmationPanel;    // 削除確認パネル
    [SerializeField] private GameObject lockedEquipmentWarningPanel; // ロック中警告パネル
    [SerializeField] private Button deleteConfirmYesButton;         // 削除確認「はい」ボタン
    [SerializeField] private Button deleteConfirmNoButton;          // 削除確認「いいえ」ボタン
    [SerializeField] private Button warningOkButton;                // 警告パネルOKボタン
    [SerializeField] private TextMeshProUGUI warningMessageText;    // 警告メッセージテキスト（新規追加）
    [SerializeField] private TextMeshProUGUI deleteTargetNameText;  // 削除対象装備名
    [SerializeField] private TextMeshProUGUI deleteTargetEnhanceText; // 削除対象強化値

    [Header("デバッグ")]
    [SerializeField] private bool enableDebugLog = true;

    // 現在の表示状態
    private InventoryTab currentTab = InventoryTab.Equipment;
    private List<EquipmentSlotUI> equipmentSlots = new List<EquipmentSlotUI>();
    private List<ItemSlotUI> enhanceItemSlots = new List<ItemSlotUI>();
    private List<ItemSlotUI> supportItemSlots = new List<ItemSlotUI>();

    // 選択状態
    private UserEquipmentData selectedEquipment;
    private UserItemData selectedItem;

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

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshDisplay);
        }

        // お気に入りボタンの設定
        if (favoriteButton != null)
        {
            favoriteButton.onClick.AddListener(OnFavoriteButtonClicked);
        }

        // ロックボタンの設定
        if (lockButton != null)
        {
            lockButton.onClick.AddListener(OnLockButtonClicked);
        }

        // 装備削除機能のボタン設定（新規追加）
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
        UpdateEquipmentManagementButtons(); // 装備管理ボタンの状態更新
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

        // タブボタンの見た目更新
        UpdateTabButtonAppearance();

        // 詳細表示を非表示（タブ切り替え時にリセット）
        HideAllDetails();

        // 該当タブの内容を更新
        UpdateCurrentTab();

        // 選択状態をクリア
        selectedEquipment = null;
        selectedItem = null;

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
            int maxCount = 1000; // 最大スロット数（設定可能にする）
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
    }

    private void SetTabButtonActive(Button button, bool isActive)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.normalColor = isActive ? Color.white : Color.white;
        button.colors = colors;
    }

    /// <summary>
    /// 装備スロットの表示を強制更新 - 新規追加
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
                    // SetEquipmentDataを再度呼び出してUpdateStatusMarks()も実行
                    slot.SetEquipmentData(equipment);
                }
            }
        }

        DebugLog("装備スロットの表示を強制更新しました");
    }

    #endregion

    #region 装備管理機能

    /// <summary>
    /// お気に入りボタンがクリックされた時の処理
    /// </summary>
    private void OnFavoriteButtonClicked()
    {
        // 装備タブで且つ装備が選択されている場合のみ処理
        if (currentTab != InventoryTab.Equipment || selectedEquipment == null)
        {
            DebugLog("お気に入り操作: 装備が選択されていないか、装備タブではありません");
            return;
        }

        // お気に入り状態をトグル
        bool success = InventoryManager.Instance.ToggleEquipmentFavorite(selectedEquipment.userEquipmentId);

        if (success)
        {
            DebugLog($"お気に入り状態を変更: {selectedEquipment.userEquipmentId} -> {!selectedEquipment.isFavorite}");

            // 装備管理ボタンの表示を即座に更新
            UpdateEquipmentManagementButtons();

            // 選択されたスロットの表示も更新
            UpdateSelectionVisual();

            // 装備スロットの表示を強制更新
            ForceUpdateEquipmentSlots();
        }
        else
        {
            DebugLog($"お気に入り状態の変更に失敗: {selectedEquipment.userEquipmentId}");
        }
    }

    /// <summary>
    /// ロックボタンがクリックされた時の処理
    /// </summary>
    private void OnLockButtonClicked()
    {
        // 装備タブで且つ装備が選択されている場合のみ処理
        if (currentTab != InventoryTab.Equipment || selectedEquipment == null)
        {
            DebugLog("ロック操作: 装備が選択されていないか、装備タブではありません");
            return;
        }

        // ロック状態をトグル
        bool success = InventoryManager.Instance.ToggleEquipmentLock(selectedEquipment.userEquipmentId);

        if (success)
        {
            DebugLog($"ロック状態を変更: {selectedEquipment.userEquipmentId} -> {!selectedEquipment.isLocked}");

            // 装備管理ボタンの表示を即座に更新
            UpdateEquipmentManagementButtons();

            // 選択されたスロットの表示も更新
            UpdateSelectionVisual();

            // 装備スロットの表示を強制更新
            ForceUpdateEquipmentSlots();
        }
        else
        {
            DebugLog($"ロック状態の変更に失敗: {selectedEquipment.userEquipmentId}");
        }
    }

    /// <summary>
    /// 装備管理ボタンの状態を更新
    /// </summary>
    private void UpdateEquipmentManagementButtons()
    {
        // 装備タブで装備が選択されている場合のみボタンを表示・有効化
        bool shouldShowButtons = (currentTab == InventoryTab.Equipment) && (selectedEquipment != null);

        UpdateFavoriteButton(shouldShowButtons);
        UpdateLockButton(shouldShowButtons);
    }

    /// <summary>
    /// お気に入りボタンの状態を更新
    /// </summary>
    private void UpdateFavoriteButton(bool shouldShow)
    {
        if (favoriteButton == null) return;

        favoriteButton.gameObject.SetActive(shouldShow);

        if (shouldShow)
        {
            // 現在のお気に入り状態に応じてボタンの表示を変更
            bool isFavorite = selectedEquipment.isFavorite;

            // ボタンテキストの更新
            if (favoriteButtonText != null)
            {
                favoriteButtonText.text = isFavorite ? "お気に入り解除" : "お気に入り登録";
            }

            // ボタンアイコンの色変更（お気に入り済みなら赤、未登録なら白）
            if (favoriteButtonIcon != null)
            {
                favoriteButtonIcon.color = isFavorite ? Color.red : Color.white;
            }

            // ボタンを有効化
            favoriteButton.interactable = true;

            DebugLog($"お気に入りボタン更新: 表示={shouldShow}, お気に入り={isFavorite}");
        }
        else
        {
            DebugLog("お気に入りボタンを非表示にしました");
        }
    }

    /// <summary>
    /// ロックボタンの状態を更新
    /// </summary>
    private void UpdateLockButton(bool shouldShow)
    {
        if (lockButton == null) return;

        lockButton.gameObject.SetActive(shouldShow);

        if (shouldShow)
        {
            // 現在のロック状態に応じてボタンの表示を変更
            bool isLocked = selectedEquipment.isLocked;

            // ボタンテキストの更新
            if (lockButtonText != null)
            {
                lockButtonText.text = isLocked ? "ロック解除" : "ロック";
            }

            // ボタンアイコンの色変更（ロック済みなら灰色、未ロックなら白）
            if (lockButtonIcon != null)
            {
                lockButtonIcon.color = isLocked ? Color.grey : Color.white;
            }

            // ボタンを有効化
            lockButton.interactable = true;

            DebugLog($"ロックボタン更新: 表示={shouldShow}, ロック={isLocked}");
        }
        else
        {
            DebugLog("ロックボタンを非表示にしました");
        }
    }

    #endregion

    #region 装備削除機能（新規追加）

    /// <summary>
    /// 装備削除ボタンがクリックされた時の処理（Null安全版）
    /// </summary>
    private void OnEquipmentDeleteButtonClicked()
    {
        DebugLog("=== 装備削除ボタン処理開始 ===");
        DebugLog($"currentTab: {currentTab}");
        DebugLog($"selectedEquipment: {(selectedEquipment != null ? selectedEquipment.userEquipmentId : "null")}");

        // 基本的な条件チェック
        if (currentTab != InventoryTab.Equipment)
        {
            DebugLog("装備削除: 装備タブではありません");
            return;
        }

        if (selectedEquipment == null)
        {
            DebugLog("装備削除: 装備が選択されていません");
            return;
        }

        // InventoryManagerの存在確認
        if (InventoryManager.Instance == null)
        {
            DebugLog("エラー: InventoryManager.Instanceがnullです");
            return;
        }

        if (!InventoryManager.Instance.IsInitialized)
        {
            DebugLog("エラー: InventoryManagerが初期化されていません");
            return;
        }

        DebugLog($"装備削除ボタンがクリックされました: {selectedEquipment.userEquipmentId}");
        ShowDeleteConfirmationPanel();
    }

    /// <summary>
    /// 削除確認パネルを表示（Null安全版）
    /// </summary>
    private void ShowDeleteConfirmationPanel()
    {
        DebugLog("=== ShowDeleteConfirmationPanel開始 ===");
        DebugLog($"deleteConfirmationPanel: {(deleteConfirmationPanel != null ? "存在" : "null")}");
        DebugLog($"selectedEquipment: {(selectedEquipment != null ? "存在" : "null")}");

        if (deleteConfirmationPanel == null)
        {
            DebugLog("エラー: deleteConfirmationPanelがnullです。Inspectorで設定してください。");
            return;
        }

        if (selectedEquipment == null)
        {
            DebugLog("エラー: selectedEquipmentがnullです。");
            return;
        }

        // MasterDataManagerの存在確認
        if (MasterDataManager.Instance == null)
        {
            DebugLog("エラー: MasterDataManager.Instanceがnullです");
            return;
        }

        try
        {
            // 削除対象の装備情報を表示
            var masterData = MasterDataManager.Instance.GetEquipmentData(selectedEquipment.equipmentMasterId);
            DebugLog($"masterData: {(masterData != null ? masterData.equipmentName : "null")}");

            if (masterData != null)
            {
                if (deleteTargetNameText != null)
                {
                    deleteTargetNameText.text = masterData.equipmentName;
                    DebugLog($"装備名を設定: {masterData.equipmentName}");
                }
                else
                {
                    DebugLog("警告: deleteTargetNameTextがnullです");
                }

                if (deleteTargetEnhanceText != null)
                {
                    string enhanceText = $"+{selectedEquipment.currentEnhancedValue}";
                    deleteTargetEnhanceText.text = enhanceText;
                    DebugLog($"強化値を設定: {enhanceText}");
                }
                else
                {
                    DebugLog("警告: deleteTargetEnhanceTextがnullです");
                }
            }

            // 削除確認パネルを表示
            DebugLog($"パネルアクティブ前の状態: {deleteConfirmationPanel.activeInHierarchy}");
            deleteConfirmationPanel.SetActive(true);
            DebugLog($"パネルアクティブ後の状態: {deleteConfirmationPanel.activeInHierarchy}");

            DebugLog("削除確認パネルを表示しました");
        }
        catch (System.Exception ex)
        {
            DebugLog($"パネル表示中にエラーが発生しました: {ex.Message}");
        }
    }

    /// <summary>
    /// 削除確認「はい」ボタンがクリックされた時の処理（テキスト切り替え版）
    /// </summary>
    private void OnDeleteConfirmYes()
    {
        DebugLog("=== 削除確認「はい」処理開始 ===");

        if (selectedEquipment == null)
        {
            DebugLog("エラー: selectedEquipmentがnullです");
            HideAllDeletePanels();
            return;
        }

        // InventoryManagerの存在確認
        if (InventoryManager.Instance == null)
        {
            DebugLog("エラー: InventoryManager.Instanceがnullです");
            HideAllDeletePanels();
            return;
        }

        if (!InventoryManager.Instance.IsInitialized)
        {
            DebugLog("エラー: InventoryManagerが初期化されていません");
            HideAllDeletePanels();
            return;
        }

        try
        {
            // 削除可能かチェック
            var (canDelete, errorMessage) = InventoryManager.Instance.CanDeleteEquipment(selectedEquipment.userEquipmentId);
            DebugLog($"削除チェック結果: canDelete={canDelete}, message={errorMessage}");

            if (!canDelete)
            {
                // 削除不可の理由に応じて適切なメッセージを表示
                if (selectedEquipment.isEquipped)
                {
                    ShowWarningWithMessage("装備中は削除できません");
                    DebugLog($"装備中のため削除不可: {errorMessage}");
                }
                else if (selectedEquipment.isLocked)
                {
                    ShowWarningWithMessage("装備はロック中です");
                    DebugLog($"ロック中のため削除不可: {errorMessage}");
                }
                else
                {
                    // その他の理由の場合はロック警告を表示（フォールバック）
                    ShowWarningWithMessage("この装備は削除できません");
                    DebugLog($"その他の理由で削除不可: {errorMessage}");
                }
                return;
            }

            // 削除実行
            bool success = InventoryManager.Instance.DeleteEquipment(selectedEquipment.userEquipmentId);
            DebugLog($"削除実行結果: {success}");

            if (success)
            {
                // 削除成功時の処理
                string deletedEquipmentId = selectedEquipment.userEquipmentId;
                DebugLog($"装備を削除しました: {deletedEquipmentId}");

                ClearEquipmentSelection();
                HideAllDeletePanels();
            }
            else
            {
                DebugLog($"装備削除に失敗しました: {selectedEquipment.userEquipmentId}");
                HideAllDeletePanels();
            }
        }
        catch (System.Exception ex)
        {
            DebugLog($"削除処理中にエラーが発生しました: {ex.Message}");
            HideAllDeletePanels();
        }
    }

    /// <summary>
    /// 指定されたメッセージで警告パネルを表示（新規追加）
    /// </summary>
    private void ShowWarningWithMessage(string message)
    {
        DebugLog("=== ShowWarningWithMessage開始 ===");
        DebugLog($"lockedEquipmentWarningPanel: {(lockedEquipmentWarningPanel != null ? "存在" : "null")}");
        DebugLog($"warningMessageText: {(warningMessageText != null ? "存在" : "null")}");
        DebugLog($"表示メッセージ: {message}");

        if (lockedEquipmentWarningPanel == null)
        {
            DebugLog("エラー: lockedEquipmentWarningPanelがnullです。Inspectorで設定してください。");
            return;
        }

        try
        {
            // 削除確認パネルを先に非表示
            if (deleteConfirmationPanel != null)
            {
                deleteConfirmationPanel.SetActive(false);
            }

            // 警告メッセージテキストを設定
            if (warningMessageText != null)
            {
                warningMessageText.text = message;
                DebugLog($"警告メッセージを設定: {message}");
            }
            else
            {
                DebugLog("警告: warningMessageTextがnullです。Inspectorで設定してください。");
            }

            // 警告パネルを表示
            DebugLog($"警告パネルアクティブ前: {lockedEquipmentWarningPanel.activeInHierarchy}");
            lockedEquipmentWarningPanel.SetActive(true);
            DebugLog($"警告パネルアクティブ後: {lockedEquipmentWarningPanel.activeInHierarchy}");

            DebugLog($"警告パネルを表示しました: {message}");
        }
        catch (System.Exception ex)
        {
            DebugLog($"警告パネル表示中にエラーが発生しました: {ex.Message}");
        }
    }

    /// <summary>
    /// 削除確認「いいえ」ボタンがクリックされた時の処理
    /// </summary>
    private void OnDeleteConfirmNo()
    {
        HideAllDeletePanels();
        DebugLog("装備削除をキャンセルしました");
    }

    /// <summary>
    /// ロック中警告パネルを表示（旧メソッド・下位互換性のため残存）
    /// </summary>
    private void ShowLockedEquipmentWarning(string message = "装備はロック中です")
    {
        ShowWarningWithMessage(message);
    }

    /// <summary>
    /// 警告パネルOKボタンがクリックされた時の処理
    /// </summary>
    private void OnWarningOkClicked()
    {
        DebugLog("警告パネルOKがクリックされました");
        HideAllDeletePanels();
    }

    /// <summary>
    /// 全ての削除関連パネルを非表示にする
    /// </summary>
    private void HideAllDeletePanels()
    {
        DebugLog("=== HideAllDeletePanels開始 ===");

        try
        {
            if (deleteConfirmationPanel != null)
            {
                DebugLog($"削除確認パネル非表示前: {deleteConfirmationPanel.activeInHierarchy}");
                deleteConfirmationPanel.SetActive(false);
                DebugLog($"削除確認パネル非表示後: {deleteConfirmationPanel.activeInHierarchy}");
            }

            if (lockedEquipmentWarningPanel != null)
            {
                DebugLog($"警告パネル非表示前: {lockedEquipmentWarningPanel.activeInHierarchy}");
                lockedEquipmentWarningPanel.SetActive(false);
                DebugLog($"警告パネル非表示後: {lockedEquipmentWarningPanel.activeInHierarchy}");
            }

            DebugLog("全ての削除関連パネルを非表示にしました");
        }
        catch (System.Exception ex)
        {
            DebugLog($"パネル非表示中にエラーが発生しました: {ex.Message}");
        }
    }

    /// <summary>
    /// 装備選択状態をリセット（Null安全版）
    /// </summary>
    private void ClearEquipmentSelection()
    {
        DebugLog("=== ClearEquipmentSelection開始 ===");

        try
        {
            selectedEquipment = null;
            selectedItem = null;

            // 詳細パネルを非表示
            HideAllDetails();

            // 装備管理ボタンの状態を更新
            UpdateEquipmentManagementButtons();

            // 選択表示を更新
            UpdateSelectionVisual();

            DebugLog("装備選択状態をリセットしました");
        }
        catch (System.Exception ex)
        {
            DebugLog($"選択状態リセット中にエラーが発生しました: {ex.Message}");
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
        DebugLog("全ての詳細パネルを非表示にしました");
    }

    /// <summary>
    /// 装備の戦闘力を計算
    /// </summary>
    private int CalculateEquipmentPower(EquipmentTotalStats stats)
    {
        int power = 0;
        power += stats.hp / 10;
        power += stats.offense * 2;
        power += stats.defense;
        power += stats.speed;
        power += stats.criticalRate / 5;
        power += stats.criticalDamageRate / 10;
        power += stats.fireOffence;
        power += stats.waterOffence;
        power += stats.windOffence;
        power += stats.earthOffence;
        return power;
    }

    #endregion

    #region イベントハンドラー

    private void OnInventoryChanged()
    {
        RefreshDisplay();
        // 装備スロットの表示を強制更新
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

    private void OnEquipmentSlotClicked(UserEquipmentData equipment)
    {
        selectedEquipment = equipment;
        selectedItem = null;

        // 選択状態の見た目更新
        UpdateSelectionVisual();

        // 装備詳細を表示
        ShowEquipmentDetail(equipment);

        // 装備管理ボタンの状態更新
        UpdateEquipmentManagementButtons();

        DebugLog($"装備が選択されました: {equipment.userEquipmentId}");
    }

    private void OnItemSlotClicked(UserItemData item)
    {
        selectedItem = item;
        selectedEquipment = null;

        // 選択状態の見た目更新
        UpdateSelectionVisual();

        // アイテム詳細を表示
        ShowItemDetail(item);

        // 装備管理ボタンの状態更新（装備以外では非表示）
        UpdateEquipmentManagementButtons();

        DebugLog($"アイテムが選択されました: {item.userItemId}");
    }

    /// <summary>
    /// 装備装着イベント - 新規追加
    /// </summary>
    private void OnEquipmentEquipped(UserEquipmentData equipment)
    {
        DebugLog($"装備装着イベント: {equipment.userEquipmentId}");

        // 装備スロットの表示を強制更新
        ForceUpdateEquipmentSlots();

        // 戦闘力などの情報表示も更新
        UpdatePlayerInfo();
    }

    /// <summary>
    /// 装備解除イベント - 新規追加
    /// </summary>
    private void OnEquipmentUnequipped(UserEquipmentData equipment)
    {
        DebugLog($"装備解除イベント: {equipment.userEquipmentId}");

        // 装備スロットの表示を強制更新
        ForceUpdateEquipmentSlots();

        // 戦闘力などの情報表示も更新
        UpdatePlayerInfo();
    }

    private void OnSortChanged(int sortIndex)
    {
        // ソート処理を実装
        // 例: 0=名前順, 1=レアリティ順, 2=強化値順, 3=取得日順
        DebugLog($"ソート変更: {sortIndex}");
        UpdateCurrentTab();
    }

    private void OnFilterChanged(int filterIndex)
    {
        // フィルタ処理を実装
        // 例: 0=全て, 1=武器のみ, 2=防具のみ, 3=アクセサリーのみ
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
    }

    private bool IsManagersReady()
    {
        return InventoryManager.Instance != null &&
               InventoryManager.Instance.IsInitialized &&
               SaveDataManager.Instance != null &&
               SaveDataManager.Instance.IsDataLoaded &&
               MasterDataManager.Instance != null &&
               MasterDataManager.Instance.IsDataLoaded;
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

    [ContextMenu("装備削除ボタンテスト")]
    private void TestDeleteButton()
    {
        if (selectedEquipment != null)
        {
            OnEquipmentDeleteButtonClicked();
        }
        else
        {
            Debug.LogWarning("装備が選択されていません");
        }
    }

    [ContextMenu("テストデータ追加")]
    private void AddTestData()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsInitialized)
        {
            InventoryManager.Instance.AddEquipment(1);
            InventoryManager.Instance.AddItem(ItemType.EnhanceItem, 1, 5);
            InventoryManager.Instance.AddItem(ItemType.SupportItem, 1, 3);
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
    SupportItem     // 補助アイテム
}