using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 装備選択用ポップアップUI
/// 修正版：イベント実行の安全化とオブジェクトライフサイクル管理
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
    [SerializeField] private Transform equipmentGridParent;
    [SerializeField] private GameObject equipmentSlotPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("装備解除ボタン設定")]
    [SerializeField] private GameObject removeEquipmentSlotPrefab;

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

    // 内部状態
    private EquipmentType currentEquipmentType;
    private UserEquipmentData selectedEquipment;
    private List<EquipmentSlotUI> equipmentSlots = new List<EquipmentSlotUI>();
    private GameObject removeEquipmentSlot;

    // === 安全性管理用フラグ追加 ===
    private bool isDestroying = false;
    private bool isEventProcessing = false;

    #region Unity Lifecycle

    private void Awake()
    {
        SetupButtons();
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
        selectedEquipment = null;
        if (detailsPanel != null)
        {
            detailsPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // === 破棄フラグを設定 ===
        isDestroying = true;

        // イベントの購読を解除してコールバック参照をクリア
        OnEquipmentSelected = null;
        OnEquipmentRemoved = null;
        OnPopupClosed = null;

        DebugLog("EquipmentSelectionPopup が破棄されました");
    }

    #endregion

    #region 初期化

    private void SetupButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePopupSafe);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmSelectionSafe);
        }

        if (removeEquipmentButton != null)
        {
            removeEquipmentButton.gameObject.SetActive(false);
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 装備選択ポップアップを表示（安全版）
    /// </summary>
    public void ShowEquipmentSelection(EquipmentType equipmentType)
    {
        if (isDestroying) return;

        currentEquipmentType = equipmentType;
        selectedEquipment = null;

        // 他のポップアップを確実に非表示
        EnsureOtherPopupsHidden();

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
    /// ポップアップを非表示（安全版）
    /// </summary>
    public void HidePopupSafe()
    {
        if (isDestroying) return;

        DebugLog("ポップアップを安全に非表示にします");

        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        selectedEquipment = null;
        HideDetailsPanel();

        // イベント発火は最後に（安全チェック付き）
        if (!isDestroying && OnPopupClosed != null)
        {
            OnPopupClosed.Invoke();
        }

        DebugLog("装備選択ポップアップを非表示");
    }

    /// <summary>
    /// 従来のHidePopupメソッド（互換性維持）
    /// </summary>
    public void HidePopup()
    {
        HidePopupSafe();
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

    private void EnsureOtherPopupsHidden()
    {
        var skillPopup = FindFirstObjectByType<SkillSelectionPopup>();
        if (skillPopup != null)
        {
            skillPopup.HidePopup();
        }
        DebugLog("他のポップアップを非表示にしました");
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

        ClearAllSlots();
        CreateRemoveEquipmentSlot();

        var availableEquipments = InventoryManager.Instance.GetEquippableItems(currentEquipmentType);
        DebugLog($"表示可能装備数: {availableEquipments.Count}");

        CreateEquipmentSlots(availableEquipments);
        UpdateButtonStates();
    }

    private void ClearAllSlots()
    {
        // 装備スロットを破棄
        foreach (var slot in equipmentSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                DestroyImmediate(slot.gameObject);
            }
        }
        equipmentSlots.Clear();

        // 装備解除スロットを破棄
        if (removeEquipmentSlot != null)
        {
            DestroyImmediate(removeEquipmentSlot);
            removeEquipmentSlot = null;
        }

        ClearAllGridChildrenSafe();
        DebugLog("全スロットをクリアしました");
    }

    private void ClearAllGridChildrenSafe()
    {
        if (equipmentGridParent == null) return;

        List<Transform> childrenToDestroy = new List<Transform>();
        for (int i = 0; i < equipmentGridParent.childCount; i++)
        {
            var child = equipmentGridParent.GetChild(i);
            if (child != null)
            {
                childrenToDestroy.Add(child);
            }
        }

        foreach (var child in childrenToDestroy)
        {
            if (child != null && child.gameObject != null)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        DebugLog($"グリッド内の{childrenToDestroy.Count}個のオブジェクトを削除しました");
    }

    private void CreateRemoveEquipmentSlot()
    {
        if (removeEquipmentSlotPrefab == null)
        {
            DebugLogError("装備解除スロットプレハブが設定されていません");
            return;
        }

        removeEquipmentSlot = Instantiate(removeEquipmentSlotPrefab, equipmentGridParent);
        removeEquipmentSlot.transform.SetSiblingIndex(0);

        LayoutElement layoutElement = removeEquipmentSlot.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = removeEquipmentSlot.AddComponent<LayoutElement>();
        }
        layoutElement.ignoreLayout = false;

        GridLayoutGroup gridLayout = equipmentGridParent.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            layoutElement.preferredWidth = gridLayout.cellSize.x;
            layoutElement.preferredHeight = gridLayout.cellSize.y;
        }

        Button removeButton = removeEquipmentSlot.GetComponent<Button>();
        if (removeButton != null)
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(RemoveEquipmentSafe);
        }

        DebugLog("装備解除スロットを作成しました");
    }

    private void CreateEquipmentSlots(List<UserEquipmentData> availableEquipments)
    {
        foreach (var equipment in availableEquipments)
        {
            GameObject newSlot = Instantiate(equipmentSlotPrefab, equipmentGridParent);
            EquipmentSlotUI slotUI = newSlot.GetComponent<EquipmentSlotUI>();

            if (slotUI != null)
            {
                slotUI.SetEquipmentData(equipment);
                slotUI.OnSlotClicked = OnEquipmentSlotClicked;
                slotUI.SetSelected(false);
                equipmentSlots.Add(slotUI);
            }
        }

        DebugLog($"装備スロットを{availableEquipments.Count}個作成しました");
    }

    #endregion

    #region 安全なイベント処理

    /// <summary>
    /// 装備選択確定（安全版）
    /// </summary>
    private void ConfirmSelectionSafe()
    {
        if (isDestroying || isEventProcessing) return;

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

        // 安全なイベント実行のためコルーチンを使用
        StartCoroutine(SafeEventExecution(() => OnEquipmentSelected?.Invoke(selectedEquipment)));
    }

    /// <summary>
    /// 装備解除（安全版）
    /// </summary>
    private void RemoveEquipmentSafe()
    {
        if (isDestroying || isEventProcessing) return;

        if (!HasEquippedItem())
        {
            DebugLog("外す装備がありません");
            return;
        }

        if (OnEquipmentRemoved == null)
        {
            DebugLogError("OnEquipmentRemovedイベントが設定されていません");
            return;
        }

        DebugLog($"装備を外します: {currentEquipmentType}");

        // 安全なイベント実行のためコルーチンを使用
        StartCoroutine(SafeEventExecution(() => OnEquipmentRemoved?.Invoke()));
    }

    /// <summary>
    /// 安全なイベント実行
    /// </summary>
    private System.Collections.IEnumerator SafeEventExecution(System.Action eventAction)
    {
        if (isDestroying)
        {
            yield break;
        }

        isEventProcessing = true;
        bool eventExecuted = false;
        System.Exception caughtException = null;

        // try-catch内ではyieldを使わない
        try
        {
            DebugLog("イベント実行開始");

            // イベントを実行
            eventAction?.Invoke();
            eventExecuted = true;

            DebugLog("イベント実行完了");
        }
        catch (System.Exception e)
        {
            caughtException = e;
            DebugLogError($"イベント実行中にエラーが発生: {e.Message}");
        }

        // try-catchの外でyieldを使用
        if (eventExecuted)
        {
            // 1フレーム待機
            yield return new WaitForEndOfFrame();

            // オブジェクトがまだ有効かチェック
            if (!isDestroying && this != null)
            {
                // ポップアップを安全に閉じる
                HidePopupSafe();
                DebugLog("ポップアップを閉じました");
            }
            else
            {
                DebugLogError("イベント実行中にオブジェクトが破棄されました");
            }
        }

        // 最終的にフラグをリセット
        isEventProcessing = false;
    }

    private void OnEquipmentSlotClicked(UserEquipmentData equipment)
    {
        if (isDestroying) return;

        selectedEquipment = equipment;
        UpdateSelectionVisual();
        UpdateDetailsPanel();
        UpdateButtonStates();

        DebugLog($"装備が選択されました: {equipment.userEquipmentId}");
    }

    #endregion

    #region 表示更新

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
        UpdateBasicInfo(masterData);
        UpdateDetailedStats(masterData);

        DebugLog($"詳細ステータス表示を更新: {masterData.equipmentName}");
    }

    private void UpdateBasicInfo(EquipmentMasterData masterData)
    {
        if (selectedEquipmentNameText != null)
        {
            selectedEquipmentNameText.text = masterData.equipmentName;
        }

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

        if (selectedEquipmentPowerText != null)
        {
            var totalStats = selectedEquipment.CalculateTotalStats(masterData);
            int power = CalculateEquipmentPower(totalStats);
            selectedEquipmentPowerText.text = power.ToString();
        }
    }

    private void UpdateDetailedStats(EquipmentMasterData masterData)
    {
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

    private void ShowDetailsPanel()
    {
        if (detailsPanel != null)
        {
            detailsPanel.SetActive(true);
        }
    }

    private void HideDetailsPanel()
    {
        if (detailsPanel != null)
        {
            detailsPanel.SetActive(false);
        }
    }

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

        if (confirmButton != null)
        {
            confirmButton.interactable = true;

            if (confirmButtonText != null)
            {
                confirmButtonText.color = hasSelection ? enabledTextColor : disabledTextColor;
            }
        }

        UpdateRemoveButtonState();
    }

    private void UpdateRemoveButtonState()
    {
        bool hasEquippedItem = HasEquippedItem();

        if (removeEquipmentButton != null)
        {
            removeEquipmentButton.interactable = hasEquippedItem;
        }

        if (removeEquipmentSlot != null)
        {
            Button gridRemoveButton = removeEquipmentSlot.GetComponent<Button>();
            if (gridRemoveButton != null)
            {
                gridRemoveButton.interactable = hasEquippedItem;

                Image buttonImage = gridRemoveButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = hasEquippedItem ? Color.white : Color.gray;
                }
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

    #endregion

    #region ユーティリティ

    private bool IsManagersReady()
    {
        return !isDestroying &&
               InventoryManager.Instance != null &&
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
#endif

    #endregion
}