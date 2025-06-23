using System;
using UnityEngine;

/// <summary>
/// 装備強化の計算処理を担当するサービス
/// - 強化値計算、ステータス増加計算、耐久減少処理
/// </summary>
public class EnhanceCalculationService
{
    /// <summary>
    /// 強化値の増加量を計算
    /// </summary>
    public int CalculateEnhanceValueIncrease(EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        int baseIncrease = enhanceItem.add_enhanced_value;

        if (supportItem != null && supportItem.add_enhanced_value > 0)
        {
            baseIncrease += supportItem.add_enhanced_value;
        }

        return baseIncrease;
    }

    /// <summary>
    /// 装備種類に応じた強化処理を実行
    /// </summary>
    public void ApplyEnhancement(UserEquipment equipment, EquipmentType equipmentType,
        EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        // 強化値を増加
        equipment.current_enhanced_value += CalculateEnhanceValueIncrease(enhanceItem, supportItem);

        // 装備種類別の強化処理
        switch (equipmentType)
        {
            case EquipmentType.Weapon:
                ApplyWeaponEnhance(equipment, enhanceItem, supportItem);
                break;
            case EquipmentType.Armor:
                ApplyArmorEnhance(equipment, enhanceItem, supportItem);
                break;
            case EquipmentType.Accessory:
                ApplyAccessoryEnhance(equipment, enhanceItem, supportItem);
                break;
        }
    }

    private void ApplyWeaponEnhance(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        // 武器：強化値+1、攻撃+1、クリティカルダメージ+1%
        equipment.offense += CalculateStatusIncrease(enhanceItem.weapon_offense, supportItem);
        equipment.critical_rate += CalculateStatusIncrease(enhanceItem.weapon_critical_rate, supportItem);
        equipment.critical_damage_rate += CalculateStatusIncrease(enhanceItem.weapon_critical_damage_rate, supportItem);


        // 属性攻撃も適用
        equipment.fire_offence += CalculateStatusIncrease(enhanceItem.weapon_fire_offence, supportItem);
        equipment.water_offence += CalculateStatusIncrease(enhanceItem.weapon_water_offence, supportItem);
        equipment.wind_offence += CalculateStatusIncrease(enhanceItem.weapon_wind_offence, supportItem);
        equipment.earth_offence += CalculateStatusIncrease(enhanceItem.weapon_earth_offence, supportItem);
    }

    private void ApplyArmorEnhance(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        // 防具：強化値+1、HP+3、防御+1
        equipment.hp += CalculateStatusIncrease(enhanceItem.armor_hp * 3, supportItem); // HP は3倍
        equipment.defense += CalculateStatusIncrease(enhanceItem.armor_defense, supportItem);

        // 属性攻撃も適用
        equipment.fire_offence += CalculateStatusIncrease(enhanceItem.armor_fire_offence, supportItem);
        equipment.water_offence += CalculateStatusIncrease(enhanceItem.armor_water_offence, supportItem);
        equipment.wind_offence += CalculateStatusIncrease(enhanceItem.armor_wind_offence, supportItem);
        equipment.earth_offence += CalculateStatusIncrease(enhanceItem.armor_earth_offence, supportItem);
    }

    private void ApplyAccessoryEnhance(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        // アクセサリ：強化値+1、HP+1、攻撃+1、防御+1
        equipment.hp += CalculateStatusIncrease(enhanceItem.accessory_hp, supportItem);
        equipment.offense += CalculateStatusIncrease(enhanceItem.accessory_offense, supportItem);
        equipment.defense += CalculateStatusIncrease(enhanceItem.accessory_defense, supportItem);
        equipment.speed += CalculateStatusIncrease(enhanceItem.accessory_speed, supportItem);

        // 属性攻撃も適用
        equipment.fire_offence += CalculateStatusIncrease(enhanceItem.accessory_fire_offence, supportItem);
        equipment.water_offence += CalculateStatusIncrease(enhanceItem.accessory_water_offence, supportItem);
        equipment.wind_offence += CalculateStatusIncrease(enhanceItem.accessory_wind_offence, supportItem);
        equipment.earth_offence += CalculateStatusIncrease(enhanceItem.accessory_earth_offence, supportItem);
    }


    private int CalculateStatusIncrease(int baseValue, SupportItemMasterData supportItem)
    {
        if (supportItem == null) return baseValue;

        int result = baseValue;

        // 補助材料の乗算効果
        if (supportItem.multipl_status_up > 1)
        {
            result *= supportItem.multipl_status_up;
        }

        return result;
    }

    /// <summary>
    /// 強化耐久減少処理
    /// </summary>
    public void ApplyStaminaDecrease(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        int staminaDecrease = enhanceItem.reduce_enhance_stamina;

        if (supportItem != null)
        {
            // 補助材料による耐久増加効果
            staminaDecrease -= supportItem.add_enhance_stamina;
            staminaDecrease = Math.Max(0, staminaDecrease); // 0以下にはならない
        }

        equipment.current_enhance_stamina = Math.Max(0, equipment.current_enhance_stamina - staminaDecrease);
    }
}