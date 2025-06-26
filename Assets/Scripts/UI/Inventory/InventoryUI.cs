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

    [Header("フィルタ・ソート")]
    [SerializeField] private TMP_Dropdown sortDropdown;
    [SerializeField] private TMP_Dropdown filterDropdown;
    [SerializeField] private Button refreshButton;

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

        // 該当タブの内容を更新
        UpdateCurrentTab();

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

        DebugLog($"装備表示を更新: {equipments.Count}件");
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

        DebugLog($"強化アイテム表示を更新: {items.Count}件");
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

        DebugLog($"補助アイテム表示を更新: {items.Count}件");
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
        colors.normalColor = isActive ? Color.cyan : Color.white;
        button.colors = colors;
    }

    #endregion

    #region イベントハンドラー

    private void OnInventoryChanged()
    {
        RefreshDisplay();
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

        DebugLog($"装備が選択されました: {equipment.userEquipmentId}");

        // 装備詳細表示やコンテキストメニューの表示などをここに追加
    }

    private void OnItemSlotClicked(UserItemData item)
    {
        selectedItem = item;
        selectedEquipment = null;

        // 選択状態の見た目更新
        UpdateSelectionVisual();

        DebugLog($"アイテムが選択されました: {item.userItemId}");

        // アイテム詳細表示やコンテキストメニューの表示などをここに追加
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