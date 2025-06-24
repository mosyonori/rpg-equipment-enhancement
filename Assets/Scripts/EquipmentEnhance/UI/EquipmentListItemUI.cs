using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 装備リスト個別アイテムUI制御クラス - プロパティ名修正版
/// 装備一覧での個別装備の表示とクリック処理を全担当
/// </summary>
public class EquipmentListItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button itemButton;
    [SerializeField] private Image equipmentIcon;
    [SerializeField] private TextMeshProUGUI equipmentNameText;
    [SerializeField] private TextMeshProUGUI enhanceLevelText;
    [SerializeField] private TextMeshProUGUI equipmentTypeText;
    [SerializeField] private TextMeshProUGUI powerText;

    [Header("Display Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;

    // データ
    private UserEquipment equipmentData;
    private EquipmentMasterData masterData;
    private System.Action<UserEquipment> onItemClicked;

    // Service層
    private EnhanceDataService dataService = new EnhanceDataService();

    #region Public Methods

    /// <summary>
    /// 装備アイテムセットアップ
    /// </summary>
    /// <param name="equipment">装備データ</param>
    /// <param name="clickCallback">クリック時のコールバック</param>
    public void Setup(UserEquipment equipment, System.Action<UserEquipment> clickCallback)
    {
        if (equipment == null)
        {
            Debug.LogWarning("[EquipmentListItemUI] 無効な装備データです");
            return;
        }

        equipmentData = equipment;
        onItemClicked = clickCallback;

        try
        {
            // マスターデータ取得
            masterData = dataService.GetEquipmentMaster(equipment.equipment_id);

            if (masterData == null)
            {
                Debug.LogWarning($"[EquipmentListItemUI] 装備マスターデータが見つかりません: {equipment.equipment_id}");
                SetDefaultDisplay();
                return;
            }

            // UI更新
            UpdateDisplay();

            // ボタンイベント設定
            SetupButton();

            Debug.Log($"[EquipmentListItemUI] セットアップ完了: {equipment.equipment_id}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EquipmentListItemUI] セットアップエラー: {e.Message}");
            SetDefaultDisplay();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 表示更新
    /// </summary>
    private void UpdateDisplay()
    {
        if (equipmentData == null || masterData == null)
        {
            SetDefaultDisplay();
            return;
        }

        // 装備名表示
        UpdateEquipmentName();

        // 強化レベル表示
        UpdateEnhanceLevel();

        // 装備種類表示
        UpdateEquipmentType();

        // 戦闘力表示
        UpdatePowerDisplay();

        // アイコン表示
        UpdateEquipmentIcon();
    }

    /// <summary>
    /// 装備名表示更新
    /// </summary>
    private void UpdateEquipmentName()
    {
        if (equipmentNameText != null && masterData != null)
        {
            equipmentNameText.text = masterData.equipment_name;
            equipmentNameText.color = normalColor;
        }
    }

    /// <summary>
    /// 強化レベル表示更新
    /// </summary>
    private void UpdateEnhanceLevel()
    {
        if (enhanceLevelText != null)
        {
            if (equipmentData.current_enhanced_value > 0)
            {
                enhanceLevelText.text = $"+{equipmentData.current_enhanced_value}";
                enhanceLevelText.color = highlightColor;
            }
            else
            {
                enhanceLevelText.text = "";
            }
        }
    }

    /// <summary>
    /// 装備種類表示更新
    /// </summary>
    private void UpdateEquipmentType()
    {
        if (equipmentTypeText != null && masterData != null)
        {
            string typeText = GetEquipmentTypeText(masterData.equipment_type);
            equipmentTypeText.text = typeText;
            equipmentTypeText.color = normalColor;
        }
    }

    /// <summary>
    /// 戦闘力表示更新
    /// </summary>
    private void UpdatePowerDisplay()
    {
        if (powerText != null)
        {
            int totalPower = CalculateEquipmentPower();
            powerText.text = $"戦闘力: {totalPower}";
            powerText.color = normalColor;
        }
    }

    /// <summary>
    /// アイコン表示更新
    /// </summary>
    private void UpdateEquipmentIcon()
    {
        if (equipmentIcon != null && masterData != null)
        {
            Sprite icon = LoadEquipmentIcon(masterData.equipment_id);
            equipmentIcon.sprite = icon;
            equipmentIcon.color = normalColor;
        }
    }

    /// <summary>
    /// デフォルト表示設定
    /// </summary>
    private void SetDefaultDisplay()
    {
        if (equipmentNameText != null)
        {
            equipmentNameText.text = "装備エラー";
            equipmentNameText.color = Color.red;
        }

        if (enhanceLevelText != null)
        {
            enhanceLevelText.text = "";
        }

        if (equipmentTypeText != null)
        {
            equipmentTypeText.text = "不明";
        }

        if (powerText != null)
        {
            powerText.text = "戦闘力: 0";
        }

        if (equipmentIcon != null)
        {
            equipmentIcon.sprite = null;
            equipmentIcon.color = Color.gray;
        }
    }

    /// <summary>
    /// ボタンセットアップ
    /// </summary>
    private void SetupButton()
    {
        if (itemButton != null)
        {
            // 既存のリスナーを削除
            itemButton.onClick.RemoveAllListeners();

            // 新しいリスナーを追加
            itemButton.onClick.AddListener(OnItemButtonClicked);
        }
        else
        {
            Debug.LogWarning("[EquipmentListItemUI] itemButtonが設定されていません");
        }
    }

    /// <summary>
    /// アイテムボタンクリック処理
    /// </summary>
    private void OnItemButtonClicked()
    {
        if (equipmentData != null && onItemClicked != null)
        {
            onItemClicked.Invoke(equipmentData);
            Debug.Log($"[EquipmentListItemUI] 装備クリック: {equipmentData.equipment_id}");
        }
    }

    /// <summary>
    /// 装備種類テキスト取得
    /// </summary>
    private string GetEquipmentTypeText(EquipmentType equipmentType)
    {
        switch (equipmentType)
        {
            case EquipmentType.Weapon:
                return "武器";
            case EquipmentType.Armor:
                return "防具";
            case EquipmentType.Accessory:
                return "アクセサリ";
            default:
                return "不明";
        }
    }

    /// <summary>
    /// 装備戦闘力計算
    /// </summary>
    private int CalculateEquipmentPower()
    {
        if (equipmentData == null) return 0;

        int power = 0;

        // 基本ステータスから戦闘力計算
        power += equipmentData.hp / 10; // HPは10分の1で戦闘力に換算
        power += equipmentData.offense;
        power += equipmentData.defense;
        power += equipmentData.speed / 2; // 速度は2分の1で戦闘力に換算

        // 属性攻撃も加算
        power += equipmentData.fire_offence;
        power += equipmentData.water_offence;
        power += equipmentData.wind_offence;
        power += equipmentData.earth_offence;

        // クリティカル関連の計算（簡略化）
        if (equipmentData.critical_rate > 0)
        {
            power += equipmentData.critical_rate / 5; // クリティカル率は5分の1で戦闘力に換算
        }

        if (equipmentData.critical_damage_rate > 100)
        {
            power += (equipmentData.critical_damage_rate - 100) / 10; // 基準値100%を超えた分の10分の1
        }

        return power;
    }

    /// <summary>
    /// 装備アイコン読み込み
    /// </summary>
    private Sprite LoadEquipmentIcon(int equipmentId)
    {
        try
        {
            // Unity上のマスターデータからアイコンを読み込み（IDベース）
            return Resources.Load<Sprite>($"Icons/Equipments/equipment_{equipmentId:D3}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EquipmentListItemUI] アイコン読み込み失敗: equipment_{equipmentId:D3}, {e.Message}");
            return null;
        }
    }

    #endregion

    #region Unity Lifecycle

    private void OnDestroy()
    {
        // ボタンイベントクリア
        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
        }
    }

    #endregion

    #region Debug Methods

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogEquipmentDetails()
    {
        if (equipmentData != null)
        {
            Debug.Log($"[EquipmentListItemUI] 装備詳細: ID={equipmentData.equipment_id}, 強化値=+{equipmentData.current_enhanced_value}, 戦闘力={CalculateEquipmentPower()}");
        }
    }

    #endregion
}