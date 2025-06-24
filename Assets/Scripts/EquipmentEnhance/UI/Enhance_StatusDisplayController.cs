using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 装備強化ステータス表示・プレビューUIコントロールクラス - IDベース修正版
/// 
/// 【責任】
/// - 現在のステータス表示
/// - 強化後のプレビュー表示
/// - 現在値・強化後の変化表示
/// - 装備種類に応じた適切なステータス項目表示
/// 
/// 【重要機能】
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
    [SerializeField] private TextMeshProUGUI equipmentNameText;
    [SerializeField] private TextMeshProUGUI equipmentLevelText;
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
    /// 装備基本情報表示更新 - IDベース修正版
    /// </summary>
    private void UpdateEquipmentInfo(UserEquipment equipment)
    {
        try
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

                // ✅ アイコン表示 - IDベース読み込み
                if (equipmentIconImage != null)
                {
                    equipmentIconImage.sprite = LoadEquipmentIcon(masterData.equipment_id);
                    equipmentIconImage.color = equipmentIconImage.sprite != null ? Color.white : Color.gray;
                }
            }
            else
            {
                Debug.LogWarning($"[Enhance_StatusDisplayController] 装備マスターデータが見つかりません: ID={equipment.equipment_id}");
                ClearEquipmentInfo();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Enhance_StatusDisplayController] 装備情報更新エラー: {e.Message}");
            ClearEquipmentInfo();
        }
    }

    /// <summary>
    /// 装備情報をクリア
    /// </summary>
    private void ClearEquipmentInfo()
    {
        if (equipmentNameText != null) equipmentNameText.text = "";
        if (equipmentLevelText != null) equipmentLevelText.text = "";
        if (equipmentIconImage != null)
        {
            equipmentIconImage.sprite = null;
            equipmentIconImage.color = Color.gray;
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

        try
        {
            EquipmentMasterData masterData = dataService.GetEquipmentMaster(equipment.equipment_id);
            if (masterData == null) return;

            // 装備種類に応じたステータス項目を表示
            List<StatusDisplayData> statusList = GetCurrentStatusList(equipment, masterData);

            foreach (var status in statusList)
            {
                CreateStatusItem(currentStatusContainer, status.name, status.value.ToString(), normalTextColor);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Enhance_StatusDisplayController] 現在ステータス表示エラー: {e.Message}");
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

        try
        {
            EquipmentMasterData masterData = dataService.GetEquipmentMaster(equipment.equipment_id);
            if (masterData == null) return;

            // 強化後予測値を計算
            List<StatusPreviewData> previewList = CalculateStatusPreview(equipment, enhanceItem, supportItem, masterData);

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
        catch (System.Exception e)
        {
            Debug.LogError($"[Enhance_StatusDisplayController] プレビュー表示エラー: {e.Message}");
        }
    }

    #endregion

    #region UI Utility Methods

    /// <summary>
    /// ステータス項目UI作成
    /// </summary>
    private void CreateStatusItem(Transform container, string name, string value, Color textColor)
    {
        if (statusItemPrefab == null || container == null) return;

        try
        {
            GameObject itemObj = Instantiate(statusItemPrefab, container);
            StatusDisplayItemUI displayItem = itemObj.GetComponent<StatusDisplayItemUI>();

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
                else if (texts.Length == 1)
                {
                    texts[0].text = $"{name}: {value}";
                    texts[0].color = textColor;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Enhance_StatusDisplayController] ステータス項目作成エラー: {e.Message}");
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
        ClearEquipmentInfo();
    }

    /// <summary>
    /// プレビュー表示クリア
    /// </summary>
    private void ClearPreviewDisplay()
    {
        ClearContainer(previewStatusContainer);
    }

    /// <summary>
    /// 装備アイコン読み込み - IDベース修正版
    /// ✅ Phase 1パターン適用：CSVパス依存からIDベースに変更
    /// </summary>
    private Sprite LoadEquipmentIcon(int equipmentId)
    {
        try
        {
            return Resources.Load<Sprite>($"Icons/Equipments/equipment_{equipmentId:D3}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Enhance_StatusDisplayController] アイコン読み込み失敗: equipment_{equipmentId:D3}, {e.Message}");
            return null;
        }
    }

    #endregion

    #region Status Preview Calculation

    /// <summary>
    /// ステータスプレビュー計算
    /// 強化アイテムと補助材料による変化を予測計算
    /// </summary>
    private List<StatusPreviewData> CalculateStatusPreview(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem, EquipmentMasterData equipmentMaster)
    {
        List<StatusPreviewData> previewList = new List<StatusPreviewData>();

        // 強化値は必ず+1
        int newEnhanceValue = equipment.current_enhanced_value + (enhanceItem?.add_enhanced_value ?? 1);
        previewList.Add(new StatusPreviewData("強化値", equipment.current_enhanced_value, newEnhanceValue));

        if (enhanceItem == null) return previewList;

        // 装備種類別のステータス変化計算
        switch (equipmentMaster.equipment_type)
        {
            case EquipmentType.Weapon:
                CalculateWeaponStatusPreview(previewList, equipment, enhanceItem);
                break;
            case EquipmentType.Armor:
                CalculateArmorStatusPreview(previewList, equipment, enhanceItem);
                break;
            case EquipmentType.Accessory:
                CalculateAccessoryStatusPreview(previewList, equipment, enhanceItem);
                break;
        }

        // 属性攻撃の変化計算
        CalculateAttributeStatusPreview(previewList, equipment, enhanceItem, equipmentMaster);

        return previewList;
    }

    private void CalculateWeaponStatusPreview(List<StatusPreviewData> previewList, UserEquipment equipment, EnhanceItemMasterData enhanceItem)
    {
        // 武器用ステータス変化
        AddPreviewIfChanged(previewList, "HP", equipment.hp, equipment.hp + enhanceItem.weapon_hp);
        AddPreviewIfChanged(previewList, "攻撃力", equipment.offense, equipment.offense + enhanceItem.weapon_offense);
        AddPreviewIfChanged(previewList, "防御力", equipment.defense, equipment.defense + enhanceItem.weapon_defense);
        AddPreviewIfChanged(previewList, "速度", equipment.speed, equipment.speed + enhanceItem.weapon_speed);
        AddPreviewIfChanged(previewList, "クリティカル率", equipment.critical_rate, equipment.critical_rate + enhanceItem.weapon_critical_rate);
        AddPreviewIfChanged(previewList, "クリティカルダメージ", equipment.critical_damage_rate, equipment.critical_damage_rate + enhanceItem.weapon_critical_damage_rate);
    }

    private void CalculateArmorStatusPreview(List<StatusPreviewData> previewList, UserEquipment equipment, EnhanceItemMasterData enhanceItem)
    {
        // 防具用ステータス変化
        AddPreviewIfChanged(previewList, "HP", equipment.hp, equipment.hp + enhanceItem.armor_hp);
        AddPreviewIfChanged(previewList, "攻撃力", equipment.offense, equipment.offense + enhanceItem.armor_offense);
        AddPreviewIfChanged(previewList, "防御力", equipment.defense, equipment.defense + enhanceItem.armor_defense);
        AddPreviewIfChanged(previewList, "速度", equipment.speed, equipment.speed + enhanceItem.armor_speed);
    }

    private void CalculateAccessoryStatusPreview(List<StatusPreviewData> previewList, UserEquipment equipment, EnhanceItemMasterData enhanceItem)
    {
        // アクセサリー用ステータス変化
        AddPreviewIfChanged(previewList, "HP", equipment.hp, equipment.hp + enhanceItem.accessory_hp);
        AddPreviewIfChanged(previewList, "攻撃力", equipment.offense, equipment.offense + enhanceItem.accessory_offense);
        AddPreviewIfChanged(previewList, "防御力", equipment.defense, equipment.defense + enhanceItem.accessory_defense);
        AddPreviewIfChanged(previewList, "速度", equipment.speed, equipment.speed + enhanceItem.accessory_speed);
    }

    private void CalculateAttributeStatusPreview(List<StatusPreviewData> previewList, UserEquipment equipment, EnhanceItemMasterData enhanceItem, EquipmentMasterData equipmentMaster)
    {
        // 属性攻撃変化（装備種類別プロパティを使用）
        int fireIncrease = GetAttributeIncreaseValue("fire", enhanceItem, equipmentMaster);
        int waterIncrease = GetAttributeIncreaseValue("water", enhanceItem, equipmentMaster);
        int windIncrease = GetAttributeIncreaseValue("wind", enhanceItem, equipmentMaster);
        int earthIncrease = GetAttributeIncreaseValue("earth", enhanceItem, equipmentMaster);

        AddPreviewIfChanged(previewList, "火属性攻撃", equipment.fire_offence, equipment.fire_offence + fireIncrease);
        AddPreviewIfChanged(previewList, "水属性攻撃", equipment.water_offence, equipment.water_offence + waterIncrease);
        AddPreviewIfChanged(previewList, "風属性攻撃", equipment.wind_offence, equipment.wind_offence + windIncrease);
        AddPreviewIfChanged(previewList, "土属性攻撃", equipment.earth_offence, equipment.earth_offence + earthIncrease);
    }

    private int GetAttributeIncreaseValue(string attributeType, EnhanceItemMasterData enhanceItem, EquipmentMasterData equipmentMaster)
    {
        switch (equipmentMaster.equipment_type)
        {
            case EquipmentType.Weapon:
                switch (attributeType)
                {
                    case "fire": return enhanceItem.weapon_fire_offence;
                    case "water": return enhanceItem.weapon_water_offence;
                    case "wind": return enhanceItem.weapon_wind_offence;
                    case "earth": return enhanceItem.weapon_earth_offence;
                }
                break;
            case EquipmentType.Armor:
                switch (attributeType)
                {
                    case "fire": return enhanceItem.armor_fire_offence;
                    case "water": return enhanceItem.armor_water_offence;
                    case "wind": return enhanceItem.armor_wind_offence;
                    case "earth": return enhanceItem.armor_earth_offence;
                }
                break;
            case EquipmentType.Accessory:
                switch (attributeType)
                {
                    case "fire": return enhanceItem.accessory_fire_offence;
                    case "water": return enhanceItem.accessory_water_offence;
                    case "wind": return enhanceItem.accessory_wind_offence;
                    case "earth": return enhanceItem.accessory_earth_offence;
                }
                break;
        }
        return 0;
    }

    private void AddPreviewIfChanged(List<StatusPreviewData> previewList, string name, int before, int after)
    {
        if (before > 0 || after > 0) // どちらかが0より大きい場合に表示
        {
            previewList.Add(new StatusPreviewData(name, before, after));
        }
    }

    #endregion

    #region Data Classes

    /// <summary>
    /// ステータス表示用データクラス
    /// </summary>
    [System.Serializable]
    public class StatusDisplayData
    {
        public string name;
        public int value;

        public StatusDisplayData(string statusName, int statusValue)
        {
            name = statusName;
            value = statusValue;
        }
    }

    /// <summary>
    /// ステータスプレビュー用データクラス
    /// </summary>
    [System.Serializable]
    public class StatusPreviewData
    {
        public string name;
        public int beforeValue;
        public int afterValue;
        public int change;

        public StatusPreviewData(string statusName, int before, int after)
        {
            name = statusName;
            beforeValue = before;
            afterValue = after;
            change = after - before;
        }
    }

    #endregion
}

#region Supporting Classes

/// <summary>
/// ステータス表示項目UI（プレハブ用）
/// </summary>
public class StatusDisplayItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Text statusNameText;
    public Text statusValueText;
    public Image statusIcon;

    public void Setup(string name, string value, Color textColor)
    {
        if (statusNameText != null)
        {
            statusNameText.text = name;
        }

        if (statusValueText != null)
        {
            statusValueText.text = value;
            statusValueText.color = textColor;
        }
    }
}

#endregion