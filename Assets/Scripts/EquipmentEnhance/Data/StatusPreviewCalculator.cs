using System.Collections.Generic;

/// <summary>
/// ステータスプレビュー計算クラス
/// 
/// 【責任】
/// - 装備強化後のステータス予想値を計算
/// - 装備種類別の強化効果を適用
/// - 補助材料による倍率効果を計算
/// - 属性攻撃の上書きルールを適用
/// 
/// 【使用箇所】
/// - Enhance_StatusDisplayController（プレビュー表示計算）
/// 
/// 【設計原則】
/// - Data層：計算ロジックのみを担当
/// - Stateless：状態を持たない計算クラス
/// - 装備種類別の処理を明確に分離
/// </summary>
public class StatusPreviewCalculator
{
    #region Fields

    private readonly UserEquipment equipment;
    private readonly EnhanceItemMasterData enhanceItem;
    private readonly SupportItemMasterData supportItem;
    private readonly EquipmentMasterData equipmentMaster;

    #endregion

    #region Constructor

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="equipment">対象装備</param>
    /// <param name="enhanceItem">強化アイテム</param>
    /// <param name="supportItem">補助材料（nullの場合は「使用しない」）</param>
    /// <param name="equipmentMaster">装備マスターデータ</param>
    public StatusPreviewCalculator(UserEquipment equipment, EnhanceItemMasterData enhanceItem,
        SupportItemMasterData supportItem, EquipmentMasterData equipmentMaster)
    {
        this.equipment = equipment ?? throw new System.ArgumentNullException(nameof(equipment));
        this.enhanceItem = enhanceItem ?? throw new System.ArgumentNullException(nameof(enhanceItem));
        this.supportItem = supportItem; // nullの場合は「使用しない」
        this.equipmentMaster = equipmentMaster ?? throw new System.ArgumentNullException(nameof(equipmentMaster));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// プレビューリスト計算（メインメソッド）
    /// </summary>
    /// <returns>ステータスプレビューデータのリスト</returns>
    public List<StatusPreviewData> CalculatePreviewList()
    {
        List<StatusPreviewData> previewList = new List<StatusPreviewData>();

        // 基本ステータス（強化値）
        AddEnhanceValuePreview(previewList);

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

    #endregion

    #region Basic Status Preview

    /// <summary>
    /// 強化値プレビュー追加
    /// </summary>
    private void AddEnhanceValuePreview(List<StatusPreviewData> previewList)
    {
        int enhanceValueIncrease = enhanceItem.add_enhanced_value;

        // 補助材料による強化値増加効果
        if (supportItem != null && supportItem.add_enhanced_value > 0)
        {
            enhanceValueIncrease += supportItem.add_enhanced_value;
        }

        previewList.Add(new StatusPreviewData("強化値",
            equipment.current_enhanced_value + enhanceValueIncrease, enhanceValueIncrease));
    }

    #endregion

    #region Equipment Type Specific Previews

    /// <summary>
    /// 武器用プレビューリスト追加
    /// </summary>
    private void AddWeaponPreviewList(List<StatusPreviewData> previewList)
    {
        AddStatusIfNotZero(previewList, "HP", equipment.hp, enhanceItem.weapon_hp);
        AddStatusIfNotZero(previewList, "攻撃力", equipment.offense, enhanceItem.weapon_offense);
        AddStatusIfNotZero(previewList, "防御力", equipment.defense, enhanceItem.weapon_defense);
        AddStatusIfNotZero(previewList, "速度", equipment.speed, enhanceItem.weapon_speed);
        AddStatusIfNotZero(previewList, "クリティカル率", equipment.critical_rate, enhanceItem.weapon_critical_rate);
        AddStatusIfNotZero(previewList, "クリティカルダメージ", equipment.critical_damage_rate, enhanceItem.weapon_critical_damage_rate);
    }

    /// <summary>
    /// 防具用プレビューリスト追加
    /// </summary>
    private void AddArmorPreviewList(List<StatusPreviewData> previewList)
    {
        // 防具のHPは3倍効果
        int armorHpIncrease = enhanceItem.armor_hp * 3;
        AddStatusIfNotZero(previewList, "HP", equipment.hp, armorHpIncrease);

        AddStatusIfNotZero(previewList, "防御力", equipment.defense, enhanceItem.armor_defense);
        AddStatusIfNotZero(previewList, "攻撃力", equipment.offense, enhanceItem.armor_offense);
        AddStatusIfNotZero(previewList, "速度", equipment.speed, enhanceItem.armor_speed);
        AddStatusIfNotZero(previewList, "クリティカル率", equipment.critical_rate, enhanceItem.armor_critical_rate);
        AddStatusIfNotZero(previewList, "クリティカルダメージ", equipment.critical_damage_rate, enhanceItem.armor_critical_damage_rate);
    }

    /// <summary>
    /// アクセサリ用プレビューリスト追加
    /// </summary>
    private void AddAccessoryPreviewList(List<StatusPreviewData> previewList)
    {
        AddStatusIfNotZero(previewList, "HP", equipment.hp, enhanceItem.accessory_hp);
        AddStatusIfNotZero(previewList, "攻撃力", equipment.offense, enhanceItem.accessory_offense);
        AddStatusIfNotZero(previewList, "防御力", equipment.defense, enhanceItem.accessory_defense);
        AddStatusIfNotZero(previewList, "速度", equipment.speed, enhanceItem.accessory_speed);
        AddStatusIfNotZero(previewList, "クリティカル率", equipment.critical_rate, enhanceItem.accessory_critical_rate);
        AddStatusIfNotZero(previewList, "クリティカルダメージ", equipment.critical_damage_rate, enhanceItem.accessory_critical_damage_rate);
    }

    #endregion

    #region Attribute Attack Preview

    /// <summary>
    /// 属性攻撃プレビューリスト追加
    /// </summary>
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

    #endregion

    #region Helper Methods

    /// <summary>
    /// ステータスが0でない場合にプレビューリストに追加
    /// </summary>
    private void AddStatusIfNotZero(List<StatusPreviewData> previewList, string name, int currentValue, int increase)
    {
        int finalIncrease = CalculateIncrease(increase);
        if (currentValue > 0 || finalIncrease > 0)
        {
            previewList.Add(new StatusPreviewData(name, currentValue + finalIncrease, finalIncrease));
        }
    }

    /// <summary>
    /// 属性攻撃ステータスの特殊処理
    /// 属性上書きルールを適用
    /// </summary>
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

    /// <summary>
    /// いずれかの属性に増加があるかチェック
    /// </summary>
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

    /// <summary>
    /// 補助材料効果を考慮した増加量計算
    /// </summary>
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

            // 補助材料による直接ステータス増加（該当する場合）
            // 注意：この部分は補助材料の仕様により調整が必要
        }

        return result;
    }

    #endregion

    #region Validation

    /// <summary>
    /// 計算に必要なデータの妥当性検証
    /// </summary>
    /// <returns>妥当な場合true</returns>
    public bool IsValidForCalculation()
    {
        return equipment != null &&
               enhanceItem != null &&
               equipmentMaster != null &&
               equipment.equipment_id == equipmentMaster.equipment_id;
    }

    #endregion
}