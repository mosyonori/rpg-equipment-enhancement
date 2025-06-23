using System;
using UnityEngine;

/// <summary>
/// 装備の属性管理を担当するサービス
/// - 属性判定、属性上書き処理、属性変更警告
/// </summary>
public class AttributeManagementService
{
    /// <summary>
    /// 装備に属性を適用（同属性は加算、異属性は上書き）
    /// </summary>
    public void ApplyAttributeEnhancement(UserEquipment equipment, EnhanceItemMasterData enhanceItem)
    {
        AttributeType equipmentAttribute = GetEquipmentCurrentAttribute(equipment);
        AttributeType enhanceItemAttribute = GetEnhanceItemAttribute(enhanceItem);

        if (enhanceItemAttribute == AttributeType.Normal)
        {
            return; // 無属性アイテムは何もしない
        }

        if (equipmentAttribute == AttributeType.Normal || equipmentAttribute == enhanceItemAttribute)
        {
            // 無属性または同属性の場合は加算
            AddAttributeValue(equipment, enhanceItemAttribute, enhanceItem);
        }
        else
        {
            // 異なる属性の場合は上書き
            OverwriteAttribute(equipment, enhanceItemAttribute, enhanceItem);
        }
    }

    private void AddAttributeValue(UserEquipment equipment, AttributeType attribute, EnhanceItemMasterData enhanceItem)
    {
        int value = GetEnhanceItemAttributeValue(enhanceItem, attribute);

        switch (attribute)
        {
            case AttributeType.Fire:
                equipment.fire_offence += value;
                break;
            case AttributeType.Water:
                equipment.water_offence += value;
                break;
            case AttributeType.Wind:
                equipment.wind_offence += value;
                break;
            case AttributeType.Earth:
                equipment.earth_offence += value;
                break;
        }
    }

    private void OverwriteAttribute(UserEquipment equipment, AttributeType newAttribute, EnhanceItemMasterData enhanceItem)
    {
        // 全ての属性攻撃を0にリセット
        ResetAllAttributeAttacks(equipment);

        // 新しい属性攻撃のみを設定
        SetAttributeAttack(equipment, newAttribute, GetEnhanceItemAttributeValue(enhanceItem, newAttribute));
    }

    private void ResetAllAttributeAttacks(UserEquipment equipment)
    {
        equipment.fire_offence = 0;
        equipment.water_offence = 0;
        equipment.wind_offence = 0;
        equipment.earth_offence = 0;
    }

    private void SetAttributeAttack(UserEquipment equipment, AttributeType attribute, int value)
    {
        switch (attribute)
        {
            case AttributeType.Fire:
                equipment.fire_offence = value;
                break;
            case AttributeType.Water:
                equipment.water_offence = value;
                break;
            case AttributeType.Wind:
                equipment.wind_offence = value;
                break;
            case AttributeType.Earth:
                equipment.earth_offence = value;
                break;
        }
    }

    private AttributeType GetEnhanceItemAttribute(EnhanceItemMasterData enhanceItem)
    {
        // 強化アイテムの武器の属性を判定
        if (enhanceItem.weapon_fire_offence > 0) return AttributeType.Fire;
        if (enhanceItem.weapon_water_offence > 0) return AttributeType.Water;
        if (enhanceItem.weapon_wind_offence > 0) return AttributeType.Wind;
        if (enhanceItem.weapon_earth_offence > 0) return AttributeType.Earth;

        // 強化アイテムの防具の属性を判定
        if (enhanceItem.armor_fire_offence > 0) return AttributeType.Fire;
        if (enhanceItem.armor_water_offence > 0) return AttributeType.Water;
        if (enhanceItem.armor_wind_offence > 0) return AttributeType.Wind;
        if (enhanceItem.armor_earth_offence > 0) return AttributeType.Earth;

        // 強化アイテムのアクセサリーの属性を判定
        if (enhanceItem.accessory_fire_offence > 0) return AttributeType.Fire;
        if (enhanceItem.accessory_water_offence > 0) return AttributeType.Water;
        if (enhanceItem.accessory_wind_offence > 0) return AttributeType.Wind;
        if (enhanceItem.accessory_earth_offence > 0) return AttributeType.Earth;
        return AttributeType.Normal;
    }

    private AttributeType GetEquipmentCurrentAttribute(UserEquipment equipment)
    {
        // 装備の現在の属性を判定
        if (equipment.fire_offence > 0) return AttributeType.Fire;
        if (equipment.water_offence > 0) return AttributeType.Water;
        if (equipment.wind_offence > 0) return AttributeType.Wind;
        if (equipment.earth_offence > 0) return AttributeType.Earth;

        return AttributeType.Normal;
    }

    private int GetEnhanceItemAttributeValue(EnhanceItemMasterData enhanceItem, AttributeType attribute)
    {
        switch (attribute)
        {
            case AttributeType.Fire: return enhanceItem.weapon_fire_offence ;
            case AttributeType.Water: return enhanceItem.weapon_water_offence;
            case AttributeType.Wind: return enhanceItem.weapon_wind_offence;
            case AttributeType.Earth: return enhanceItem.weapon_earth_offence;
            default: return 0;
        }
    }

    /// <summary>
    /// 属性変更の警告メッセージ取得
    /// </summary>
    public string GetAttributeChangeWarning(UserEquipment equipment, EnhanceItemMasterData enhanceItem)
    {
        AttributeType equipmentAttribute = GetEquipmentCurrentAttribute(equipment);
        AttributeType enhanceItemAttribute = GetEnhanceItemAttribute(enhanceItem);

        if (enhanceItemAttribute == AttributeType.Normal)
        {
            return ""; // 無属性アイテムは警告なし
        }

        if (equipmentAttribute == AttributeType.Normal)
        {
            return ""; // 無属性装備への属性付与は警告なし
        }

        if (equipmentAttribute != enhanceItemAttribute)
        {
            return "属性攻撃が上書きされます";
        }

        return ""; // 同じ属性は警告なし
    }
}

// 属性タイプの定義
public enum AttributeType
{
    Normal,  // 無属性
    Fire,    // 火
    Water,   // 水
    Wind,    // 風
    Earth    // 土
}