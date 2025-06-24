using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 装備強化ステータス表示・プレビューUI制御クラス
/// 
/// 【責任】
/// - 現在のステータス表示
/// - 強化後のプレビュー表示
/// - 現在値→強化後の変化表示
/// - 装備種類に応じた適切なステータス項目表示
/// 
/// 【主要機能】
/// - 装備の現在ステータス表示
/// - 強化アイテム・補助材料による変化予測
/// - 属性変化の警告表示
/// - リアルタイムUI更新
/// </summary>
public class Enhance_StatusDisplayController : MonoBehaviour
{
    [Header("Current Status Display")]
    [SerializeField] private Transform currentStatusContainer;
    [SerializeField] private GameObject statusItemPrefab;

    [Header("Preview Status Display")]
    [SerializeField] private Transform previewStatusContainer;
    [SerializeField] private GameObject previewItemPrefab;

    [Header("Change Arrow")]
    [SerializeField] private GameObject changeArrowContainer;
    [SerializeField] private Image changeArrowImage;

    [Header("Equipment Info")]
    [SerializeField] private Text equipmentNameText;
    [SerializeField] private Text equipmentLevelText;
    [SerializeField] private Image equipmentIconImage;

    [Header("Colors")]
    [SerializeField] private Color increaseColor = Color.green;
    [SerializeField] private Color decreaseColor = Color.red;
    [SerializeField] private Color noChangeColor = Color.gray;
    [SerializeField] private Color normalTextColor = Color.white;

    // Service層
    private EnhanceDataService dataService = new EnhanceDataService();

    // 現在の表示状態
    private UserEquipment currentEquipment;
    private EnhanceItemMasterData currentEnhanceItem;
    private SupportItemMasterData currentSupportItem;

    #region Public Methods

    /// <summary>
    /// ステータス表示を更新
    /// 外部のEnhanceUIControllerから呼び出される
    /// </summary>
    public void UpdateDisplay(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        currentEquipment = equipment;
        currentEnhanceItem = enhanceItem;
        currentSupportItem = supportItem;

        if (equipment == null)
        {
            // 装備未選択時は表示をクリア
            ClearAllDisplays();
            return;
        }

        // 装備情報表示
        UpdateEquipmentInfo(equipment);

        // 現在ステータス表示
        UpdateCurrentStatusDisplay(equipment);

        if (enhanceItem != null)
        {
            // プレビュー表示
            UpdatePreviewDisplay(equipment, enhanceItem, supportItem);
            ShowChangeArrow(true);
        }
        else
        {
            // 強化アイテム未選択時はプレビューを非表示
            ClearPreviewDisplay();
            ShowChangeArrow(false);
        }
    }

    /// <summary>
    /// 表示を完全にリセット
    /// </summary>
    public void ResetDisplay()
    {
        currentEquipment = null;
        currentEnhanceItem = null;
        currentSupportItem = null;
        ClearAllDisplays();
    }

    #endregion

    #region Equipment Info Display

    /// <summary>
    /// 装備基本情報表示更新
    /// </summary>
    private void UpdateEquipmentInfo(UserEquipment equipment)
    {
        EquipmentMasterData masterData = dataService.GetEquipmentMaster(equipment.equipment_id);

        if (masterData != null)
        {
            // 装備名表示
            if (equipmentNameText != null)
            {
                equipmentNameText.text = masterData.equipment_name;
            }

            // 強化レベル表示
            if (equipmentLevelText != null)
            {
                equipmentLevelText.text = $"+{equipment.current_enhanced_value}";
            }

            // アイコン表示
            if (equipmentIconImage != null)
            {
                equipmentIconImage.sprite = LoadEquipmentIcon(masterData.equipment_icon_path);
            }
        }
    }

    #endregion

    #region Current Status Display

    /// <summary>
    /// 現在ステータス表示更新
    /// </summary>
    private void UpdateCurrentStatusDisplay(UserEquipment equipment)
    {
        // 既存表示をクリア
        ClearContainer(currentStatusContainer);

        EquipmentMasterData masterData = dataService.GetEquipmentMaster(equipment.equipment_id);
        if (masterData == null) return;

        // 装備種類に応じたステータス項目を表示
        List<StatusDisplayData> statusList = GetCurrentStatusList(equipment, masterData);

        foreach (var status in statusList)
        {
            CreateStatusItem(currentStatusContainer, status.name, status.value.ToString(), normalTextColor);
        }
    }

    /// <summary>
    /// 現在ステータス一覧取得
    /// </summary>
    private List<StatusDisplayData> GetCurrentStatusList(UserEquipment equipment, EquipmentMasterData masterData)
    {
        List<StatusDisplayData> statusList = new List<StatusDisplayData>();

        // 基本ステータス（全装備共通）
        statusList.Add(new StatusDisplayData("強化値", equipment.current_enhanced_value));

        // 装備種類別ステータス表示
        switch (masterData.equipment_type)
        {
            case EquipmentType.Weapon:
                AddWeaponStatusList(statusList, equipment);
                break;
            case EquipmentType.Armor:
                AddArmorStatusList(statusList, equipment);
                break;
            case EquipmentType.Accessory:
                AddAccessoryStatusList(statusList, equipment);
                break;
        }

        // 属性攻撃表示
        AddAttributeStatusList(statusList, equipment);

        return statusList;
    }

    private void AddWeaponStatusList(List<StatusDisplayData> statusList, UserEquipment equipment)
    {
        if (equipment.hp > 0)
            statusList.Add(new StatusDisplayData("HP", equipment.hp));
        if (equipment.offense > 0)
            statusList.Add(new StatusDisplayData("攻撃力", equipment.offense));
        if (equipment.defense > 0)
            statusList.Add(new StatusDisplayData("防御力", equipment.defense));
        if (equipment.speed > 0)
            statusList.Add(new StatusDisplayData("速度", equipment.speed));
        if (equipment.critical_rate > 0)
            statusList.Add(new StatusDisplayData("クリティカル率", equipment.critical_rate));
        if (equipment.critical_damage_rate > 0)
            statusList.Add(new StatusDisplayData("クリティカルダメージ", equipment.critical_damage_rate));
    }

    private void AddArmorStatusList(List<StatusDisplayData> statusList, UserEquipment equipment)
    {
        if (equipment.hp > 0)
            statusList.Add(new StatusDisplayData("HP", equipment.hp));
        if (equipment.defense > 0)
            statusList.Add(new StatusDisplayData("防御力", equipment.defense));
        if (equipment.offense > 0)
            statusList.Add(new StatusDisplayData("攻撃力", equipment.offense));
    }

    private void AddAccessoryStatusList(List<StatusDisplayData> statusList, UserEquipment equipment)
    {
        if (equipment.hp > 0)
            statusList.Add(new StatusDisplayData("HP", equipment.hp));
        if (equipment.offense > 0)
            statusList.Add(new StatusDisplayData("攻撃力", equipment.offense));
        if (equipment.defense > 0)
            statusList.Add(new StatusDisplayData("防御力", equipment.defense));
        if (equipment.speed > 0)
            statusList.Add(new StatusDisplayData("速度", equipment.speed));
    }

    private void AddAttributeStatusList(List<StatusDisplayData> statusList, UserEquipment equipment)
    {
        if (equipment.fire_offence > 0)
            statusList.Add(new StatusDisplayData("火属性攻撃", equipment.fire_offence));
        if (equipment.water_offence > 0)
            statusList.Add(new StatusDisplayData("水属性攻撃", equipment.water_offence));
        if (equipment.wind_offence > 0)
            statusList.Add(new StatusDisplayData("風属性攻撃", equipment.wind_offence));
        if (equipment.earth_offence > 0)
            statusList.Add(new StatusDisplayData("土属性攻撃", equipment.earth_offence));
    }

    #endregion

    #region Preview Display

    /// <summary>
    /// プレビュー表示更新
    /// </summary>
    private void UpdatePreviewDisplay(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        // 既存表示をクリア
        ClearContainer(previewStatusContainer);

        EquipmentMasterData masterData = dataService.GetEquipmentMaster(equipment.equipment_id);
        if (masterData == null) return;

        // 強化後予測値を計算
        StatusPreviewCalculator calculator = new StatusPreviewCalculator(equipment, enhanceItem, supportItem, masterData);
        List<StatusPreviewData> previewList = calculator.CalculatePreviewList();

        foreach (var status in previewList)
        {
            Color textColor = GetChangeColor(status.change);
            string displayText = status.afterValue.ToString();

            if (status.change != 0)
            {
                string changeText = status.change > 0 ? $"(+{status.change})" : $"({status.change})";
                displayText += " " + changeText;
            }

            CreateStatusItem(previewStatusContainer, status.name, displayText, textColor);
        }
    }

    #endregion

    #region UI Utility Methods

    /// <summary>
    /// ステータス項目UI生成
    /// </summary>
    private void CreateStatusItem(Transform container, string name, string value, Color textColor)
    {
        if (statusItemPrefab == null || container == null) return;

        GameObject itemObj = Instantiate(statusItemPrefab, container);
        StatusDisplayItem displayItem = itemObj.GetComponent<StatusDisplayItem>();

        if (displayItem != null)
        {
            displayItem.Setup(name, value, textColor);
        }
        else
        {
            // フォールバック：直接テキストコンポーネントを探す
            Text[] texts = itemObj.GetComponentsInChildren<Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = name;
                texts[1].text = value;
                texts[1].color = textColor;
            }
        }
    }

    /// <summary>
    /// 変化量に応じた色取得
    /// </summary>
    private Color GetChangeColor(int change)
    {
        if (change > 0) return increaseColor;
        if (change < 0) return decreaseColor;
        return normalTextColor;
    }

    /// <summary>
    /// 変化矢印表示制御
    /// </summary>
    private void ShowChangeArrow(bool show)
    {
        if (changeArrowContainer != null)
        {
            changeArrowContainer.SetActive(show);
        }
    }

    /// <summary>
    /// コンテナ内容クリア
    /// </summary>
    private void ClearContainer(Transform container)
    {
        if (container == null) return;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 全表示クリア
    /// </summary>
    private void ClearAllDisplays()
    {
        ClearContainer(currentStatusContainer);
        ClearPreviewDisplay();
        ShowChangeArrow(false);

        if (equipmentNameText != null) equipmentNameText.text = "";
        if (equipmentLevelText != null) equipmentLevelText.text = "";
        if (equipmentIconImage != null) equipmentIconImage.sprite = null;
    }

    /// <summary>
    /// プレビュー表示クリア
    /// </summary>
    private void ClearPreviewDisplay()
    {
        ClearContainer(previewStatusContainer);
    }

    /// <summary>
    /// 装備アイコン読み込み
    /// </summary>
    private Sprite LoadEquipmentIcon(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath)) return null;
        return Resources.Load<Sprite>($"Icons/Equipment/{iconPath}");
    }

    #endregion
}

#region Data Classes

/// <summary>
/// ステータス表示データ
/// </summary>
[System.Serializable]
public class StatusDisplayData
{
    public string name;
    public int value;

    public StatusDisplayData(string name, int value)
    {
        this.name = name;
        this.value = value;
    }
}

/// <summary>
/// ステータスプレビューデータ
/// </summary>
[System.Serializable]
public class StatusPreviewData
{
    public string name;
    public int afterValue;
    public int change;

    public StatusPreviewData(string name, int afterValue, int change)
    {
        this.name = name;
        this.afterValue = afterValue;
        this.change = change;
    }
}

/// <summary>
/// ステータスプレビュー計算クラス
/// </summary>
public class StatusPreviewCalculator
{
    private UserEquipment equipment;
    private EnhanceItemMasterData enhanceItem;
    private SupportItemMasterData supportItem;
    private EquipmentMasterData equipmentMaster;

    public StatusPreviewCalculator(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem, EquipmentMasterData equipmentMaster)
    {
        this.equipment = equipment;
        this.enhanceItem = enhanceItem;
        this.supportItem = supportItem;
        this.equipmentMaster = equipmentMaster;
    }

    public List<StatusPreviewData> CalculatePreviewList()
    {
        List<StatusPreviewData> previewList = new List<StatusPreviewData>();

        // 基本ステータス
        int enhanceValueIncrease = enhanceItem.add_enhanced_value;
        previewList.Add(new StatusPreviewData("強化値",
            equipment.current_enhanced_value + enhanceValueIncrease, enhanceValueIncrease));

        // 装備種類別ステータス
        switch (equipmentMaster.equipment_type)
        {
            case EquipmentType.Weapon:
                AddWeaponPreviewList(previewList);
                break;
            case EquipmentType.Armor:
                AddArmorPreviewList(previewList);
                break;
            case EquipmentType.Accessory:
                AddAccessoryPreviewList(previewList);
                break;
        }

        // 属性攻撃
        AddAttributePreviewList(previewList);

        return previewList;
    }

    private void AddWeaponPreviewList(List<StatusPreviewData> previewList)
    {
        AddStatusIfNotZero(previewList, "HP", equipment.hp, enhanceItem.weapon_hp);
        AddStatusIfNotZero(previewList, "攻撃力", equipment.offense, enhanceItem.weapon_offense);
        AddStatusIfNotZero(previewList, "防御力", equipment.defense, enhanceItem.weapon_defense);
        AddStatusIfNotZero(previewList, "速度", equipment.speed, enhanceItem.weapon_speed);
        AddStatusIfNotZero(previewList, "クリティカル率", equipment.critical_rate, enhanceItem.weapon_critical_rate);
        AddStatusIfNotZero(previewList, "クリティカルダメージ", equipment.critical_damage_rate, enhanceItem.weapon_critical_damage_rate);
    }

    private void AddArmorPreviewList(List<StatusPreviewData> previewList)
    {
        AddStatusIfNotZero(previewList, "HP", equipment.hp, enhanceItem.armor_hp);
        AddStatusIfNotZero(previewList, "防御力", equipment.defense, enhanceItem.armor_defense);
        AddStatusIfNotZero(previewList, "攻撃力", equipment.offense, enhanceItem.armor_offense);
        AddStatusIfNotZero(previewList, "速度", equipment.speed, enhanceItem.armor_speed);
        AddStatusIfNotZero(previewList, "クリティカル率", equipment.critical_rate, enhanceItem.armor_critical_rate);
        AddStatusIfNotZero(previewList, "クリティカルダメージ", equipment.critical_damage_rate, enhanceItem.armor_critical_damage_rate);
    }

    private void AddAccessoryPreviewList(List<StatusPreviewData> previewList)
    {
        AddStatusIfNotZero(previewList, "HP", equipment.hp, enhanceItem.accessory_hp);
        AddStatusIfNotZero(previewList, "攻撃力", equipment.offense, enhanceItem.accessory_offense);
        AddStatusIfNotZero(previewList, "防御力", equipment.defense, enhanceItem.accessory_defense);
        AddStatusIfNotZero(previewList, "速度", equipment.speed, enhanceItem.accessory_speed);
        AddStatusIfNotZero(previewList, "クリティカル率", equipment.critical_rate, enhanceItem.accessory_critical_rate);
        AddStatusIfNotZero(previewList, "クリティカルダメージ", equipment.critical_damage_rate, enhanceItem.accessory_critical_damage_rate);
    }

    private void AddAttributePreviewList(List<StatusPreviewData> previewList)
    {
        // 装備種類に応じた属性攻撃の処理
        switch (equipmentMaster.equipment_type)
        {
            case EquipmentType.Weapon:
                AddAttributeStatusIfNotZero(previewList, "火属性攻撃", equipment.fire_offence, enhanceItem.weapon_fire_offence);
                AddAttributeStatusIfNotZero(previewList, "水属性攻撃", equipment.water_offence, enhanceItem.weapon_water_offence);
                AddAttributeStatusIfNotZero(previewList, "風属性攻撃", equipment.wind_offence, enhanceItem.weapon_wind_offence);
                AddAttributeStatusIfNotZero(previewList, "土属性攻撃", equipment.earth_offence, enhanceItem.weapon_earth_offence);
                break;
            case EquipmentType.Armor:
                AddAttributeStatusIfNotZero(previewList, "火属性攻撃", equipment.fire_offence, enhanceItem.armor_fire_offence);
                AddAttributeStatusIfNotZero(previewList, "水属性攻撃", equipment.water_offence, enhanceItem.armor_water_offence);
                AddAttributeStatusIfNotZero(previewList, "風属性攻撃", equipment.wind_offence, enhanceItem.armor_wind_offence);
                AddAttributeStatusIfNotZero(previewList, "土属性攻撃", equipment.earth_offence, enhanceItem.armor_earth_offence);
                break;
            case EquipmentType.Accessory:
                AddAttributeStatusIfNotZero(previewList, "火属性攻撃", equipment.fire_offence, enhanceItem.accessory_fire_offence);
                AddAttributeStatusIfNotZero(previewList, "水属性攻撃", equipment.water_offence, enhanceItem.accessory_water_offence);
                AddAttributeStatusIfNotZero(previewList, "風属性攻撃", equipment.wind_offence, enhanceItem.accessory_wind_offence);
                AddAttributeStatusIfNotZero(previewList, "土属性攻撃", equipment.earth_offence, enhanceItem.accessory_earth_offence);
                break;
        }
    }

    private void AddStatusIfNotZero(List<StatusPreviewData> previewList, string name, int currentValue, int increase)
    {
        int finalIncrease = CalculateIncrease(increase);
        if (currentValue > 0 || finalIncrease > 0)
        {
            previewList.Add(new StatusPreviewData(name, currentValue + finalIncrease, finalIncrease));
        }
    }

    private void AddAttributeStatusIfNotZero(List<StatusPreviewData> previewList, string name, int currentValue, int enhanceValue)
    {
        if (enhanceValue > 0)
        {
            // この属性が強化される場合（上書き）
            int finalIncrease = CalculateIncrease(enhanceValue);
            int change = finalIncrease - currentValue;
            previewList.Add(new StatusPreviewData(name, finalIncrease, change));
        }
        else if (currentValue > 0 && HasAnyAttributeIncrease())
        {
            // 他の属性が強化される場合、この属性は0になる
            previewList.Add(new StatusPreviewData(name, 0, -currentValue));
        }
        else if (currentValue > 0)
        {
            // 属性強化がない場合は現在値維持
            previewList.Add(new StatusPreviewData(name, currentValue, 0));
        }
    }

    private bool HasAnyAttributeIncrease()
    {
        switch (equipmentMaster.equipment_type)
        {
            case EquipmentType.Weapon:
                return enhanceItem.weapon_fire_offence > 0 || enhanceItem.weapon_water_offence > 0 ||
                       enhanceItem.weapon_wind_offence > 0 || enhanceItem.weapon_earth_offence > 0;
            case EquipmentType.Armor:
                return enhanceItem.armor_fire_offence > 0 || enhanceItem.armor_water_offence > 0 ||
                       enhanceItem.armor_wind_offence > 0 || enhanceItem.armor_earth_offence > 0;
            case EquipmentType.Accessory:
                return enhanceItem.accessory_fire_offence > 0 || enhanceItem.accessory_water_offence > 0 ||
                       enhanceItem.accessory_wind_offence > 0 || enhanceItem.accessory_earth_offence > 0;
            default:
                return false;
        }
    }

    private int CalculateIncrease(int baseValue)
    {
        if (baseValue == 0) return 0;

        int result = baseValue;

        if (supportItem != null)
        {
            // 補助材料による倍率効果
            if (supportItem.multipl_status_up > 1)
            {
                result *= supportItem.multipl_status_up;
            }
        }

        return result;
    }
}

/// <summary>
/// ステータス表示項目UI（プレハブ用）
/// </summary>
public class StatusDisplayItem : MonoBehaviour
{
    [Header("UI Elements")]
    public Text nameText;
    public Text valueText;

    public void Setup(string name, string value, Color textColor)
    {
        if (nameText != null) nameText.text = name;
        if (valueText != null)
        {
            valueText.text = value;
            valueText.color = textColor;
        }
    }
}

#endregion