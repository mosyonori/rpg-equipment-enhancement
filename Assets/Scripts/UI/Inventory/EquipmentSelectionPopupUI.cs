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
    [SerializeField] private Button removeEquipmentButton; // 旧来のボタン（念のため残す）
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
    private GameObject removeEquipmentSlot; // Grid内の装備解除ボタン

    #region Unity Lifecycle

    private void Awake()
    {
        SetupButtons();
        // Awake時は単純に非表示にする（元の方法）
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
        // イベントの購読を解除して参照をクリア
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
            closeButton.onClick.AddListener(HidePopup);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmSelection);
        }

        // 旧来の装備解除ボタンを無効化（Grid内のボタンを使用するため）
        if (removeEquipmentButton != null)
        {
            removeEquipmentButton.gameObject.SetActive(false); // 非表示にする
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 装備選択ポップアップを表示（元の方法に戻す）
    /// </summary>
    public void ShowEquipmentSelection(EquipmentType equipmentType)
    {
        currentEquipmentType = equipmentType;
        selectedEquipment = null;

        // === 追加: 他のポップアップを確実に非表示 ===
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
    /// 他のポップアップを確実に非表示にする
    /// </summary>
    private void EnsureOtherPopupsHidden()
    {
        // SkillSelectionPopupを非表示にする
        var skillPopup = FindFirstObjectByType<SkillSelectionPopup>();
        if (skillPopup != null)
        {
            skillPopup.HidePopup();
        }

        DebugLog("他のポップアップを非表示にしました");
    }

    /// <summary>
    /// ポップアップを非表示（元の方法に戻す）
    /// </summary>
    public void HidePopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        selectedEquipment = null;
        HideDetailsPanel();

        // 元のシンプルな方法に戻す
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

        // 既存のアイテムをクリア
        ClearAllSlots();

        // 装備解除ボタンを最初に作成
        CreateRemoveEquipmentSlot();

        // 指定タイプの装備可能アイテムを取得
        var availableEquipments = InventoryManager.Instance.GetEquippableItems(currentEquipmentType);

        DebugLog($"表示可能装備数: {availableEquipments.Count}");

        // 装備アイテムスロットを作成
        CreateEquipmentSlots(availableEquipments);

        // ボタン状態更新
        UpdateButtonStates();
    }

    /// <summary>
    /// 全スロットをクリア（元の方法）
    /// </summary>
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

        // グリッド内の全ての子オブジェクトを安全に削除
        ClearAllGridChildrenSafe();

        DebugLog("全スロットをクリアしました");
    }

    /// <summary>
    /// グリッド内の全ての子オブジェクトを安全に削除
    /// </summary>
    private void ClearAllGridChildrenSafe()
    {
        if (equipmentGridParent == null) return;

        // 子オブジェクトのリストを事前に取得（破棄中の参照エラーを避けるため）
        List<Transform> childrenToDestroy = new List<Transform>();

        for (int i = 0; i < equipmentGridParent.childCount; i++)
        {
            var child = equipmentGridParent.GetChild(i);
            if (child != null)
            {
                childrenToDestroy.Add(child);
            }
        }

        // 事前に取得したリストから安全に削除
        foreach (var child in childrenToDestroy)
        {
            if (child != null && child.gameObject != null)
            {
                // オブジェクトを削除
                DestroyImmediate(child.gameObject);
            }
        }

        DebugLog($"グリッド内の{childrenToDestroy.Count}個のオブジェクトを削除しました");
    }

    /// <summary>
    /// 装備解除ボタンスロットを作成（Grid内の最初の位置）
    /// </summary>
    private void CreateRemoveEquipmentSlot()
    {
        if (removeEquipmentSlotPrefab == null)
        {
            DebugLogError("装備解除スロットプレハブが設定されていません");
            return;
        }

        // 装備解除スロットを生成
        removeEquipmentSlot = Instantiate(removeEquipmentSlotPrefab, equipmentGridParent);

        // 最初の位置に配置
        removeEquipmentSlot.transform.SetSiblingIndex(0);

        // Layout Elementを追加してGrid Layoutに参加させる
        LayoutElement layoutElement = removeEquipmentSlot.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = removeEquipmentSlot.AddComponent<LayoutElement>();
        }
        // ignoreLayout = false にして Grid Layout に参加させる
        layoutElement.ignoreLayout = false;

        // Grid Layout Groupのセルサイズに合わせる（推奨サイズとして設定）
        GridLayoutGroup gridLayout = equipmentGridParent.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            layoutElement.preferredWidth = gridLayout.cellSize.x;
            layoutElement.preferredHeight = gridLayout.cellSize.y;
        }

        // ボタンイベントを設定
        Button removeButton = removeEquipmentSlot.GetComponent<Button>();
        if (removeButton != null)
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(RemoveEquipment);
        }

        DebugLog("装備解除スロットを作成しました（Grid内最初の位置、Grid Layoutに参加）");
    }

    /// <summary>
    /// 装備アイテムスロットを作成
    /// </summary>
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

    private void OnEquipmentSlotClicked(UserEquipmentData equipment)
    {
        selectedEquipment = equipment;

        // 選択状態の見た目更新
        UpdateSelectionVisual();

        // 詳細ステータス表示更新
        UpdateDetailsPanel();

        // ボタン状態更新
        UpdateButtonStates();

        DebugLog($"装備が選択されました: {equipment.userEquipmentId}");
    }

    /// <summary>
    /// 詳細ステータスパネルを更新
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

        // Grid内の装備解除ボタンの状態を更新
        UpdateRemoveButtonState();
    }

    /// <summary>
    /// 装備解除ボタンの状態を更新
    /// </summary>
    private void UpdateRemoveButtonState()
    {
        bool hasEquippedItem = HasEquippedItem();

        // 旧来のボタン（パネル外）- もしInspectorで設定されている場合
        if (removeEquipmentButton != null)
        {
            removeEquipmentButton.interactable = hasEquippedItem;
        }

        // Grid内のボタン
        if (removeEquipmentSlot != null)
        {
            Button gridRemoveButton = removeEquipmentSlot.GetComponent<Button>();
            if (gridRemoveButton != null)
            {
                gridRemoveButton.interactable = hasEquippedItem;

                // ボタンの見た目を更新
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

    /// <summary>
    /// 装備選択確定（元のシンプルな方法に戻す）
    /// </summary>
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
            // 元のシンプルな方法：イベントを先に呼び出してからポップアップを閉じる
            OnEquipmentSelected.Invoke(selectedEquipment);
            HidePopup();
        }
        catch (System.Exception e)
        {
            DebugLogError($"装備選択確定時にエラーが発生: {e.Message}");
        }
    }

    /// <summary>
    /// 装備解除（元のシンプルな方法に戻す）
    /// </summary>
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
            // 元のシンプルな方法：イベントを先に呼び出してからポップアップを閉じる
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

    [ContextMenu("Grid内装備解除ボタンテスト")]
    private void TestGridRemoveButton()
    {
        CreateRemoveEquipmentSlot();
        Debug.Log("Grid内装備解除ボタンをテスト作成しました");
    }
#endif

    #endregion
}