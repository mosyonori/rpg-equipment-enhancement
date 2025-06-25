using UnityEngine;

[CreateAssetMenu(fileName = "EnhanceItem_", menuName = "GameData/Enhance Item Master Data")]
public class EnhanceItemMasterData : ScriptableObject
{
    [Header("基本情報")]
    public int enhanceItemId;
    public string enhanceItemName;
    public AttributeType attributeType;
    public RarityType rarity;

    [Header("スタック設定")]
    public int maxStackValue;

    [Header("強化値変動")]
    [Tooltip("装備の強化値を増加させる数値")]
    public int addEnhancedValue;
    [Tooltip("装備の強化値を減少させる数値")]
    public int reduceEnhancedValue;

    [Header("強化耐久値変動")]
    [Tooltip("装備の強化耐久値を増加させる数値")]
    public int addEnhanceStamina;
    [Tooltip("装備の強化耐久値を減少させる数値")]
    public int reduceEnhanceStamina;

    [Header("強化成功率")]
    [Tooltip("強化成功率（%）")]
    public int enhanceSuccessRate;

    [Header("武器への効果")]
    public int weaponHp;
    public int weaponOffense;
    public int weaponDefense;
    public int weaponSpeed;
    public int weaponCriticalRate;
    public int weaponCriticalDamageRate;
    public int weaponFireOffence;
    public int weaponWaterOffence;
    public int weaponWindOffence;
    public int weaponEarthOffence;

    [Header("防具への効果")]
    public int armorHp;
    public int armorOffense;
    public int armorDefense;
    public int armorSpeed;
    public int armorCriticalRate;
    public int armorCriticalDamageRate;
    public int armorFireOffence;
    public int armorWaterOffence;
    public int armorWindOffence;
    public int armorEarthOffence;

    [Header("アクセサリーへの効果")]
    public int accessoryHp;
    public int accessoryOffense;
    public int accessoryDefense;
    public int accessorySpeed;
    public int accessoryCriticalRate;
    public int accessoryCriticalDamageRate;
    public int accessoryFireOffence;
    public int accessoryWaterOffence;
    public int accessoryWindOffence;
    public int accessoryEarthOffence;

    [Header("表示設定")]
    public Sprite enhanceItemIcon;
    public string enhanceItemIconPath;
    [TextArea(3, 5)]
    public string description;

    [Header("フラグ")]
    public bool completionFlag;
    public bool collectionFlag;

    /// <summary>
    /// 装備タイプに応じたステータス効果を取得
    /// </summary>
    public EnhanceEffect GetEnhanceEffect(EquipmentType equipmentType)
    {
        return equipmentType switch
        {
            EquipmentType.Weapon => new EnhanceEffect
            {
                hp = weaponHp,
                offense = weaponOffense,
                defense = weaponDefense,
                speed = weaponSpeed,
                criticalRate = weaponCriticalRate,
                criticalDamageRate = weaponCriticalDamageRate,
                fireOffence = weaponFireOffence,
                waterOffence = weaponWaterOffence,
                windOffence = weaponWindOffence,
                earthOffence = weaponEarthOffence
            },
            EquipmentType.Armor => new EnhanceEffect
            {
                hp = armorHp,
                offense = armorOffense,
                defense = armorDefense,
                speed = armorSpeed,
                criticalRate = armorCriticalRate,
                criticalDamageRate = armorCriticalDamageRate,
                fireOffence = armorFireOffence,
                waterOffence = armorWaterOffence,
                windOffence = armorWindOffence,
                earthOffence = armorEarthOffence
            },
            EquipmentType.Accessory => new EnhanceEffect
            {
                hp = accessoryHp,
                offense = accessoryOffense,
                defense = accessoryDefense,
                speed = accessorySpeed,
                criticalRate = accessoryCriticalRate,
                criticalDamageRate = accessoryCriticalDamageRate,
                fireOffence = accessoryFireOffence,
                waterOffence = accessoryWaterOffence,
                windOffence = accessoryWindOffence,
                earthOffence = accessoryEarthOffence
            },
            _ => new EnhanceEffect()
        };
    }

    /// <summary>
    /// 属性攻撃力を取得
    /// </summary>
    public int GetAttributeOffence(EquipmentType equipmentType)
    {
        return equipmentType switch
        {
            EquipmentType.Weapon => attributeType switch
            {
                AttributeType.Fire => weaponFireOffence,
                AttributeType.Water => weaponWaterOffence,
                AttributeType.Wind => weaponWindOffence,
                AttributeType.Earth => weaponEarthOffence,
                _ => 0
            },
            EquipmentType.Armor => attributeType switch
            {
                AttributeType.Fire => armorFireOffence,
                AttributeType.Water => armorWaterOffence,
                AttributeType.Wind => armorWindOffence,
                AttributeType.Earth => armorEarthOffence,
                _ => 0
            },
            EquipmentType.Accessory => attributeType switch
            {
                AttributeType.Fire => accessoryFireOffence,
                AttributeType.Water => accessoryWaterOffence,
                AttributeType.Wind => accessoryWindOffence,
                AttributeType.Earth => accessoryEarthOffence,
                _ => 0
            },
            _ => 0
        };
    }
}

[System.Serializable]
public struct EnhanceEffect
{
    public int hp;
    public int offense;
    public int defense;
    public int speed;
    public int criticalRate;
    public int criticalDamageRate;
    public int fireOffence;
    public int waterOffence;
    public int windOffence;
    public int earthOffence;
}