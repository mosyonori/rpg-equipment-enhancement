using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 装備選択UI制御クラス
/// 所持装備一覧表示、装備選択処理、選択装備の表示を担当
/// </summary>
public class Enhance_EquipmentSelectUIController : MonoBehaviour
{
    [Header("UI Elements - Main")]
    [SerializeField] private Button equipmentSelectButton;
    [SerializeField] private Image equipmentIconImage;
    [SerializeField] private Text equipmentNameText;
    [SerializeField] private Transform equipmentOptionsDisplay;

    [Header("UI Elements - Equipment List")]
    [SerializeField] private GameObject equipmentListPanel;
    [SerializeField] private Transform equipmentListContent;
    [SerializeField] private GameObject equipmentItemPrefab;
    [SerializeField] private Button listCloseButton;
    [SerializeField] private ScrollRect equipmentListScrollRect;

    [Header("Display Settings")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = Color.gray;
    [SerializeField] private GameObject equipmentStatusTextPrefab;

    // Service層
    private EnhanceDataService dataService = new EnhanceDataService();

    // 選択状態
    private UserEquipment selectedEquipment;
    private bool isInteractable = true;

    // UI制御
    private List<GameObject> createdEquipmentItems = new List<GameObject>();
    private List<GameObject> createdStatusTexts = new List<GameObject>();

    // イベント
    public event System.Action<UserEquipment> OnEquipmentSelected;

    #region Unity Lifecycle

    private void Start()
    {
        InitializeUI();
        SetupEventListeners();
    }

    private void OnDestroy()
    {
        RemoveEventListeners();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// UI初期化
    /// </summary>
    private void InitializeUI()
    {
        // 初期状態設定
        ResetSelection();

        // 装備リストパネルを非表示
        if (equipmentListPanel != null)
        {
            equipmentListPanel.SetActive(false);
        }

        Debug.Log("[Enhance_EquipmentSelectUIController] UI初期化完了");
    }

    /// <summary>
    /// イベントリスナー設定
    /// </summary>
    private void SetupEventListeners()
    {
        equipmentSelectButton.onClick.AddListener(OnEquipmentSelectButtonClicked);

        if (listCloseButton != null)
        {
            listCloseButton.onClick.AddListener(CloseEquipmentList);
        }
    }

    /// <summary>
    /// イベントリスナー削除
    /// </summary>
    private void RemoveEventListeners()
    {
        equipmentSelectButton.onClick.RemoveAllListeners();

        if (listCloseButton != null)
        {
            listCloseButton.onClick.RemoveAllListeners();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 操作可能状態設定
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        equipmentSelectButton.interactable = interactable;

        Debug.Log($"[Enhance_EquipmentSelectUIController] 操作可能状態: {interactable}");
    }

    /// <summary>
    /// 選択状態リセット
    /// </summary>
    public void ResetSelection()
    {
        selectedEquipment = null;
        UpdateEquipmentDisplay();
        UpdateEquipmentOptions();

        Debug.Log("[Enhance_EquipmentSelectUIController] 選択状態をリセット");
    }

    #endregion

    #region Button Event Handlers

    /// <summary>
    /// 装備選択ボタンクリック処理
    /// </summary>
    public void OnEquipmentSelectButtonClicked()
    {
        if (!isInteractable)
        {
            Debug.LogWarning("[Enhance_EquipmentSelectUIController] UIが無効状態のため装備選択を無視");
            return;
        }

        ShowEquipmentList();
    }

    #endregion

    #region Equipment List Management

    /// <summary>
    /// 装備一覧表示
    /// </summary>
    private void ShowEquipmentList()
    {
        try
        {
            // 所持装備取得
            List<UserEquipment> ownedEquipments = dataService.GetOwnedEquipments();

            if (ownedEquipments == null || ownedEquipments.Count == 0)
            {
                Debug.LogWarning("[Enhance_EquipmentSelectUIController] 所持装備が見つかりません");
                return;
            }

            // 既存のアイテムを削除
            ClearEquipmentList();

            // 装備アイテム生成
            CreateEquipmentListItems(ownedEquipments);

            // パネル表示
            if (equipmentListPanel != null)
            {
                equipmentListPanel.SetActive(true);
            }

            // スクロール位置をトップに戻す
            if (equipmentListScrollRect != null)
            {
                equipmentListScrollRect.verticalNormalizedPosition = 1f;
            }

            Debug.Log($"[Enhance_EquipmentSelectUIController] 装備一覧表示: {ownedEquipments.Count}件");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Enhance_EquipmentSelectUIController] 装備一覧表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 装備一覧アイテム生成
    /// </summary>
    private void CreateEquipmentListItems(List<UserEquipment> equipments)
    {
        foreach (var equipment in equipments)
        {
            try
            {
                // プレハブからアイテム生成
                GameObject itemObj = Instantiate(equipmentItemPrefab, equipmentListContent);
                createdEquipmentItems.Add(itemObj);

                // EquipmentListItemUIコンポーネントを取得してセットアップ
                EquipmentListItemUI itemUI = itemObj.GetComponent<EquipmentListItemUI>();
                if (itemUI != null)
                {
                    itemUI.Setup(equipment, OnEquipmentItemClicked);
                }
                else
                {
                    Debug.LogWarning($"[Enhance_EquipmentSelectUIController] EquipmentListItemUIコンポーネントが見つかりません: {equipment.equipment_id}");

                    // フォールバック: 基本的なボタン設定
                    Button itemButton = itemObj.GetComponent<Button>();
                    if (itemButton != null)
                    {
                        itemButton.onClick.AddListener(() => OnEquipmentItemClicked(equipment));
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Enhance_EquipmentSelectUIController] 装備アイテム生成エラー: {equipment.equipment_id}, {e.Message}");
            }
        }
    }

    /// <summary>
    /// 装備一覧クリア
    /// </summary>
    private void ClearEquipmentList()
    {
        foreach (GameObject item in createdEquipmentItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        createdEquipmentItems.Clear();
    }

    /// <summary>
    /// 装備一覧を閉じる
    /// </summary>
    private void CloseEquipmentList()
    {
        if (equipmentListPanel != null)
        {
            equipmentListPanel.SetActive(false);
        }

        Debug.Log("[Enhance_EquipmentSelectUIController] 装備一覧を閉じました");
    }

    #endregion

    #region Equipment Selection

    /// <summary>
    /// 装備アイテムクリック処理
    /// </summary>
    private void OnEquipmentItemClicked(UserEquipment equipment)
    {
        if (equipment == null)
        {
            Debug.LogWarning("[Enhance_EquipmentSelectUIController] 無効な装備が選択されました");
            return;
        }

        selectedEquipment = equipment;

        // UI更新
        UpdateEquipmentDisplay();
        UpdateEquipmentOptions();

        // パネルを閉じる
        CloseEquipmentList();

        // イベント通知
        OnEquipmentSelected?.Invoke(equipment);

        Debug.Log($"[Enhance_EquipmentSelectUIController] 装備選択: {equipment.equipment_id} (ユニークID: {equipment.unique_id})");
    }

    #endregion

    #region Display Update

    /// <summary>
    /// 選択装備表示更新
    /// </summary>
    private void UpdateEquipmentDisplay()
    {
        if (selectedEquipment != null)
        {
            try
            {
                // マスターデータ取得
                EquipmentMasterData masterData = dataService.GetEquipmentMaster(selectedEquipment.equipment_id);

                if (masterData != null)
                {
                    // アイコン表示
                    if (equipmentIconImage != null)
                    {
                        Sprite icon = LoadEquipmentIcon(masterData.equipment_icon_path);
                        equipmentIconImage.sprite = icon;
                        equipmentIconImage.color = selectedColor;
                    }

                    // 名前表示
                    if (equipmentNameText != null)
                    {
                        string enhanceLevel = selectedEquipment.current_enhanced_value > 0 ? $"+{selectedEquipment.current_enhanced_value}" : "";
                        equipmentNameText.text = $"{masterData.equipment_name}{enhanceLevel}";
                        equipmentNameText.color = selectedColor;
                    }
                }
                else
                {
                    Debug.LogWarning($"[Enhance_EquipmentSelectUIController] 装備マスターデータが見つかりません: {selectedEquipment.equipment_id}");
                    SetDefaultDisplay();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Enhance_EquipmentSelectUIController] 装備表示更新エラー: {e.Message}");
                SetDefaultDisplay();
            }
        }
        else
        {
            SetDefaultDisplay();
        }
    }

    /// <summary>
    /// デフォルト表示設定
    /// </summary>
    private void SetDefaultDisplay()
    {
        if (equipmentIconImage != null)
        {
            equipmentIconImage.sprite = null;
            equipmentIconImage.color = unselectedColor;
        }

        if (equipmentNameText != null)
        {
            equipmentNameText.text = "装備を選択";
            equipmentNameText.color = unselectedColor;
        }
    }

    /// <summary>
    /// 装備オプション表示更新
    /// </summary>
    private void UpdateEquipmentOptions()
    {
        // 既存のステータステキストをクリア
        ClearEquipmentStatusTexts();

        if (selectedEquipment != null && equipmentOptionsDisplay != null)
        {
            try
            {
                CreateEquipmentStatusDisplay();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Enhance_EquipmentSelectUIController] 装備オプション表示エラー: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 装備ステータス表示生成
    /// </summary>
    private void CreateEquipmentStatusDisplay()
    {
        // 基本ステータス表示
        if (selectedEquipment.hp > 0)
        {
            CreateStatusText($"HP: {selectedEquipment.hp}");
        }

        if (selectedEquipment.offense > 0)
        {
            CreateStatusText($"攻撃力: {selectedEquipment.offense}");
        }

        if (selectedEquipment.defense > 0)
        {
            CreateStatusText($"防御力: {selectedEquipment.defense}");
        }

        if (selectedEquipment.speed > 0)
        {
            CreateStatusText($"速度: {selectedEquipment.speed}");
        }

        if (selectedEquipment.critical_rate > 0)
        {
            CreateStatusText($"クリティカル率: {selectedEquipment.critical_rate}%");
        }

        if (selectedEquipment.critical_damage_rate > 0)
        {
            CreateStatusText($"クリティカルダメージ: {selectedEquipment.critical_damage_rate}%");
        }

        // 属性攻撃表示
        CreateAttributeStatusDisplay();

        // 強化値表示
        if (selectedEquipment.current_enhanced_value > 0)
        {
            CreateStatusText($"強化値: +{selectedEquipment.current_enhanced_value}");
        }
    }

    /// <summary>
    /// 属性ステータス表示生成
    /// </summary>
    private void CreateAttributeStatusDisplay()
    {
        if (selectedEquipment.fire_offence > 0)
        {
            CreateStatusText($"火属性攻撃: {selectedEquipment.fire_offence}");
        }

        if (selectedEquipment.water_offence > 0)
        {
            CreateStatusText($"水属性攻撃: {selectedEquipment.water_offence}");
        }

        if (selectedEquipment.wind_offence > 0)
        {
            CreateStatusText($"風属性攻撃: {selectedEquipment.wind_offence}");
        }

        if (selectedEquipment.earth_offence > 0)
        {
            CreateStatusText($"土属性攻撃: {selectedEquipment.earth_offence}");
        }
    }

    /// <summary>
    /// ステータステキスト生成
    /// </summary>
    private void CreateStatusText(string statusText)
    {
        if (equipmentStatusTextPrefab != null && equipmentOptionsDisplay != null)
        {
            GameObject statusObj = Instantiate(equipmentStatusTextPrefab, equipmentOptionsDisplay);
            createdStatusTexts.Add(statusObj);

            Text textComponent = statusObj.GetComponent<Text>();
            if (textComponent != null)
            {
                textComponent.text = statusText;
            }
        }
        else
        {
            Debug.LogWarning($"[Enhance_EquipmentSelectUIController] ステータステキスト表示失敗: {statusText}");
        }
    }

    /// <summary>
    /// 装備ステータステキストクリア
    /// </summary>
    private void ClearEquipmentStatusTexts()
    {
        foreach (GameObject statusText in createdStatusTexts)
        {
            if (statusText != null)
            {
                Destroy(statusText);
            }
        }
        createdStatusTexts.Clear();
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 装備アイコン読み込み
    /// </summary>
    private Sprite LoadEquipmentIcon(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath))
        {
            return null;
        }

        try
        {
            // TODO: 実際のアイコン読み込み実装
            // Resources.Load<Sprite>(iconPath) などを使用
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Enhance_EquipmentSelectUIController] アイコン読み込み失敗: {iconPath}, {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 現在選択中の装備取得
    /// </summary>
    public UserEquipment GetSelectedEquipment()
    {
        return selectedEquipment;
    }

    /// <summary>
    /// 装備が選択されているか確認
    /// </summary>
    public bool HasSelection()
    {
        return selectedEquipment != null;
    }

    #endregion

    #region Debug Methods

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogEquipmentSelectionDetails()
    {
        if (selectedEquipment != null)
        {
            Debug.Log($"[Enhance_EquipmentSelectUIController] 選択装備詳細:");
            Debug.Log($"  装備ID: {selectedEquipment.equipment_id}");
            Debug.Log($"  ユニークID: {selectedEquipment.unique_id}");
            Debug.Log($"  強化値: +{selectedEquipment.current_enhanced_value}");
            Debug.Log($"  HP: {selectedEquipment.hp}");
            Debug.Log($"  攻撃力: {selectedEquipment.offense}");
            Debug.Log($"  防御力: {selectedEquipment.defense}");
        }
    }

    #endregion
}