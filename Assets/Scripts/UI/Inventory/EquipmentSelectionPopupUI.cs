using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 装備選択用ポップアップUI
/// </summary>
public class EquipmentSelectionPopup : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI confirmButtonText;
    [SerializeField] private Button removeEquipmentButton;
    [SerializeField] private TextMeshProUGUI removeEquipmentButtonText;
    [SerializeField] private Transform equipmentGridParent;
    [SerializeField] private GameObject equipmentSlotPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("詳細ステータス表示")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private TextMeshProUGUI selectedEquipmentNameText;
    [SerializeField] private TextMeshProUGUI selectedEquipmentEnhanceText;
    [SerializeField] private TextMeshProUGUI selectedEquipmentPowerText;
    [SerializeField] private TextMeshProUGUI detailHpText;
    [SerializeField] private TextMeshProUGUI detailOffenseText;
    [SerializeField] private TextMeshProUGUI detailDefenseText;
    [SerializeField] private TextMeshProUGUI detailSpeedText;
    [SerializeField] private TextMeshProUGUI detailCriticalRateText;
    [SerializeField] private TextMeshProUGUI detailCriticalDamageText;
    [SerializeField] private TextMeshProUGUI detailFireOffenceText;
    [SerializeField] private TextMeshProUGUI detailWaterOffenceText;
    [SerializeField] private TextMeshProUGUI detailWindOffenceText;
    [SerializeField] private TextMeshProUGUI detailEarthOffenceText;

    [Header("ボタンテキスト色設定")]
    [SerializeField] private Color enabledTextColor = Color.white;
    [SerializeField] private Color disabledTextColor = Color.gray;

    [Header("デバッグ")]
    [SerializeField] private bool enableDebugLog = true;

    // イベント
    public System.Action<UserEquipmentData> OnEquipmentSelected;
    public System.Action OnEquipmentRemoved;
    public System.Action OnPopupClosed;

    // 状態
    private EquipmentType currentEquipmentType;
    private UserEquipmentData selectedEquipment;
    private List<EquipmentSlotUI> equipmentSlots = new List<EquipmentSlotUI>();

    #region Unity Lifecycle

    private void Awake()
    {
        SetupButtons();
        HidePopup();
    }

    #endregion

    #region 初期化

    private void SetupButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePopup);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmSelection);
        }

        if (removeEquipmentButton != null)
        {
            removeEquipmentButton.onClick.AddListener(RemoveEquipment);
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 装備選択ポップアップを表示
    /// </summary>
    public void ShowEquipmentSelection(EquipmentType equipmentType)
    {
        currentEquipmentType = equipmentType;
        selectedEquipment = null;

        // タイトル設定
        UpdateTitle();

        // 装備リスト表示
        DisplayEquipmentList();

        // 詳細パネル初期化
        HideDetailsPanel();

        // ポップアップ表示
        ShowPopup();

        DebugLog($"装備選択ポップアップを表示: {equipmentType}");
    }

    /// <summary>
    /// ポップアップを非表示
    /// </summary>
    public void HidePopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        selectedEquipment = null;
        HideDetailsPanel();
        OnPopupClosed?.Invoke();

        DebugLog("装備選択ポップアップを非表示");
    }

    /// <summary>
    /// 現在表示中の装備タイプを取得
    /// </summary>
    public EquipmentType GetCurrentEquipmentType()
    {
        return currentEquipmentType;
    }

    #endregion

    #region 内部メソッド

    private void ShowPopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }
    }

    private void UpdateTitle()
    {
        if (titleText == null) return;

        string titleStr = currentEquipmentType switch
        {
            EquipmentType.Weapon => "武器を選択",
            EquipmentType.Armor => "防具を選択",
            EquipmentType.Accessory => "アクセサリーを選択",
            _ => "装備を選択"
        };

        titleText.text = titleStr;
    }

    private void DisplayEquipmentList()
    {
        if (!IsManagersReady()) return;

        // 指定タイプの装備可能アイテムを取得
        var availableEquipments = InventoryManager.Instance.GetEquippableItems(currentEquipmentType);

        DebugLog($"表示可能装備数: {availableEquipments.Count}");

        // スロット数を調整
        AdjustSlotCount(availableEquipments.Count);

        // 各スロットにデータ設定
        for (int i = 0; i < availableEquipments.Count; i++)
        {
            equipmentSlots[i].SetEquipmentData(availableEquipments[i]);
            equipmentSlots[i].OnSlotClicked = OnEquipmentSlotClicked;
            equipmentSlots[i].SetSelected(false);
        }

        // ボタン状態更新
        UpdateButtonStates();
    }

    private void AdjustSlotCount(int targetCount)
    {
        // 不足分を作成
        while (equipmentSlots.Count < targetCount)
        {
            GameObject newSlot = Instantiate(equipmentSlotPrefab, equipmentGridParent);
            EquipmentSlotUI slotUI = newSlot.GetComponent<EquipmentSlotUI>();

            if (slotUI != null)
            {
                equipmentSlots.Add(slotUI);
            }
        }

        // 表示/非表示を制御
        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            equipmentSlots[i].gameObject.SetActive(i < targetCount);
        }
    }

    private void OnEquipmentSlotClicked(UserEquipmentData equipment)
    {
        selectedEquipment = equipment;

        // 選択状態の見た目更新
        UpdateSelectionVisual();

        // 詳細ステータス表示更新（新規追加）
        UpdateDetailsPanel();

        // ボタン状態更新
        UpdateButtonStates();

        DebugLog($"装備が選択されました: {equipment.userEquipmentId}");
    }

    /// <summary>
    /// 詳細ステータスパネルを更新（新規追加）
    /// </summary>
    private void UpdateDetailsPanel()
    {
        if (selectedEquipment == null)
        {
            HideDetailsPanel();
            return;
        }

        var masterData = MasterDataManager.Instance?.GetEquipmentData(selectedEquipment.equipmentMasterId);
        if (masterData == null)
        {
            DebugLogError($"装備マスターデータが見つかりません: {selectedEquipment.equipmentMasterId}");
            HideDetailsPanel();
            return;
        }

        ShowDetailsPanel();

        // 基本情報表示
        UpdateBasicInfo(masterData);

        // 詳細ステータス表示
        UpdateDetailedStats(masterData);

        DebugLog($"詳細ステータス表示を更新: {masterData.equipmentName}");
    }

    /// <summary>
    /// 基本情報を更新
    /// </summary>
    private void UpdateBasicInfo(EquipmentMasterData masterData)
    {
        // 装備名
        if (selectedEquipmentNameText != null)
        {
            selectedEquipmentNameText.text = masterData.equipmentName;
        }

        // 強化値
        if (selectedEquipmentEnhanceText != null)
        {
            if (selectedEquipment.currentEnhancedValue > 0)
            {
                selectedEquipmentEnhanceText.text = $"+{selectedEquipment.currentEnhancedValue}";
                selectedEquipmentEnhanceText.gameObject.SetActive(true);
            }
            else
            {
                selectedEquipmentEnhanceText.gameObject.SetActive(false);
            }
        }

        // 戦闘力
        if (selectedEquipmentPowerText != null)
        {
            var totalStats = selectedEquipment.CalculateTotalStats(masterData);
            int power = CalculateEquipmentPower(totalStats);
            selectedEquipmentPowerText.text = power.ToString();
        }
    }

    /// <summary>
    /// 詳細ステータスを更新
    /// </summary>
    private void UpdateDetailedStats(EquipmentMasterData masterData)
    {
        // 基本ステータス + 強化値を計算
        int totalHp = masterData.hp + selectedEquipment.enhancedHp;
        int totalOffense = masterData.offense + selectedEquipment.enhancedOffense;
        int totalDefense = masterData.defense + selectedEquipment.enhancedDefense;
        int totalSpeed = masterData.speed + selectedEquipment.enhancedSpeed;
        int totalCriticalRate = masterData.criticalRate + selectedEquipment.enhancedCriticalRate;
        int totalCriticalDamage = masterData.criticalDamageRate + selectedEquipment.enhancedCriticalDamageRate;
        int totalFireOffence = masterData.fireOffence + selectedEquipment.enhancedFireOffence;
        int totalWaterOffence = masterData.waterOffence + selectedEquipment.enhancedWaterOffence;
        int totalWindOffence = masterData.windOffence + selectedEquipment.enhancedWindOffence;
        int totalEarthOffence = masterData.earthOffence + selectedEquipment.enhancedEarthOffence;

        // 各ステータステキストを更新
        if (detailHpText != null) detailHpText.text = totalHp.ToString();
        if (detailOffenseText != null) detailOffenseText.text = totalOffense.ToString();
        if (detailDefenseText != null) detailDefenseText.text = totalDefense.ToString();
        if (detailSpeedText != null) detailSpeedText.text = totalSpeed.ToString();
        if (detailCriticalRateText != null) detailCriticalRateText.text = $"{totalCriticalRate}%";
        if (detailCriticalDamageText != null) detailCriticalDamageText.text = $"{totalCriticalDamage}%";
        if (detailFireOffenceText != null) detailFireOffenceText.text = totalFireOffence.ToString();
        if (detailWaterOffenceText != null) detailWaterOffenceText.text = totalWaterOffence.ToString();
        if (detailWindOffenceText != null) detailWindOffenceText.text = totalWindOffence.ToString();
        if (detailEarthOffenceText != null) detailEarthOffenceText.text = totalEarthOffence.ToString();
    }

    /// <summary>
    /// 詳細パネルを表示
    /// </summary>
    private void ShowDetailsPanel()
    {
        if (detailsPanel != null)
        {
            detailsPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 詳細パネルを非表示
    /// </summary>
    private void HideDetailsPanel()
    {
        if (detailsPanel != null)
        {
            detailsPanel.SetActive(false);
        }
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

    private void UpdateSelectionVisual()
    {
        foreach (var slot in equipmentSlots)
        {
            if (slot.gameObject.activeInHierarchy)
            {
                bool isSelected = selectedEquipment != null &&
                    slot.GetEquipmentData()?.userEquipmentId == selectedEquipment.userEquipmentId;
                slot.SetSelected(isSelected);
            }
        }
    }

    private void UpdateButtonStates()
    {
        bool hasSelection = selectedEquipment != null;

        // Confirmボタンのテキスト色を変更（ボタン自体は常に有効）
        if (confirmButton != null)
        {
            confirmButton.interactable = true; // 常に有効

            if (confirmButtonText != null)
            {
                confirmButtonText.color = hasSelection ? enabledTextColor : disabledTextColor;
            }
        }

        // 装備外しボタンは、現在装備中のアイテムがある場合のみ有効
        if (removeEquipmentButton != null)
        {
            bool hasEquippedItem = HasEquippedItem();
            removeEquipmentButton.interactable = true; // 常に有効

            if (removeEquipmentButtonText != null)
            {
                removeEquipmentButtonText.color = hasEquippedItem ? enabledTextColor : disabledTextColor;
            }
        }
    }

    private bool HasEquippedItem()
    {
        var equippedItems = InventoryManager.Instance.GetEquippedItems();
        return equippedItems.Exists(eq =>
        {
            var masterData = MasterDataManager.Instance?.GetEquipmentData(eq.equipmentMasterId);
            return masterData?.equipmentType == currentEquipmentType;
        });
    }

    private void ConfirmSelection()
    {
        // 選択状態のチェック
        if (selectedEquipment == null)
        {
            DebugLog("装備が選択されていません");
            return;
        }

        // イベントが設定されているかチェック
        if (OnEquipmentSelected == null)
        {
            DebugLogError("OnEquipmentSelectedイベントが設定されていません");
            return;
        }

        DebugLog($"装備選択を確定: {selectedEquipment.userEquipmentId}");

        try
        {
            OnEquipmentSelected.Invoke(selectedEquipment);
            HidePopup();
        }
        catch (System.Exception e)
        {
            DebugLogError($"装備選択確定時にエラーが発生: {e.Message}");
        }
    }

    private void RemoveEquipment()
    {
        // 装備外し可能かチェック
        if (!HasEquippedItem())
        {
            DebugLog("外す装備がありません");
            return;
        }

        // イベントが設定されているかチェック
        if (OnEquipmentRemoved == null)
        {
            DebugLogError("OnEquipmentRemovedイベントが設定されていません");
            return;
        }

        DebugLog($"装備を外します: {currentEquipmentType}");

        try
        {
            OnEquipmentRemoved.Invoke();
            HidePopup();
        }
        catch (System.Exception e)
        {
            DebugLogError($"装備外し時にエラーが発生: {e.Message}");
        }
    }

    private bool IsManagersReady()
    {
        return InventoryManager.Instance != null &&
               InventoryManager.Instance.IsInitialized &&
               MasterDataManager.Instance != null &&
               MasterDataManager.Instance.IsDataLoaded;
    }

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[EquipmentSelectionPopup] {message}");
        }
    }

    private void DebugLogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[EquipmentSelectionPopup] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("テスト表示 - 武器")]
    private void TestShowWeapons()
    {
        ShowEquipmentSelection(EquipmentType.Weapon);
    }

    [ContextMenu("テスト表示 - 防具")]
    private void TestShowArmors()
    {
        ShowEquipmentSelection(EquipmentType.Armor);
    }

    [ContextMenu("テスト表示 - アクセサリー")]
    private void TestShowAccessories()
    {
        ShowEquipmentSelection(EquipmentType.Accessory);
    }

    [ContextMenu("詳細ステータステスト")]
    private void TestDetailedStats()
    {
        if (selectedEquipment != null)
        {
            UpdateDetailsPanel();
            Debug.Log("詳細ステータス表示をテスト更新しました");
        }
        else
        {
            Debug.LogWarning("装備が選択されていません");
        }
    }
#endif

    #endregion
}