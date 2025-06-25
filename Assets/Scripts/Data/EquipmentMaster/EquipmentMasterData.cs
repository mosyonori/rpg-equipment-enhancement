using UnityEngine;

[CreateAssetMenu(fileName = "Equipment_", menuName = "GameData/Equipment Master Data")]
public class EquipmentMasterData : ScriptableObject
{
    [Header("基本情報")]
    public int equipmentId;
    public string equipmentName;
    public EquipmentType equipmentType;
    public RarityType rarity;

    [Header("強化値設定")]
    public int baseEnhancedValue;
    public int maxEnhancedValue;
    public int minEnhancedValue;

    [Header("強化耐久値設定")]
    public int baseEnhanceStamina;
    public int maxEnhanceStamina;
    public int minEnhanceStamina;

    [Header("強化成功率")]
    public int baseEnhanceSuccessRate;

    [Header("基本ステータス")]
    public int hp;
    public int offense;
    public int defense;
    public int speed;
    public int criticalRate;
    public int criticalDamageRate;

    [Header("属性攻撃力")]
    public int fireOffence;
    public int waterOffence;
    public int windOffence;
    public int earthOffence;

    [Header("解放コンテンツ")]
    public int equipmentUnlockSkillId;
    public int equipmentUnlockSkillEnhancedValue;
    public string equipmentUnlockCharacterId;
    public string equipmentUnlockCharacterEnhancedValue;

    [Header("表示設定")]
    public Sprite equipmentIcon;
    public string equipmentIconPath;
    [TextArea(3, 5)]
    public string description;

    [Header("フラグ")]
    public bool completionFlag;
    public bool collectionFlag;

    /// <summary>
    /// 装備の属性を取得（属性攻撃力が設定されている属性を返す）
    /// </summary>
    public AttributeType GetAttributeType()
    {
        if (fireOffence > 0) return AttributeType.Fire;
        if (waterOffence > 0) return AttributeType.Water;
        if (windOffence > 0) return AttributeType.Wind;
        if (earthOffence > 0) return AttributeType.Earth;
        return AttributeType.None;
    }

    /// <summary>
    /// 指定された属性の攻撃力を取得
    /// </summary>
    public int GetAttributeOffence(AttributeType attributeType)
    {
        return attributeType switch
        {
            AttributeType.Fire => fireOffence,
            AttributeType.Water => waterOffence,
            AttributeType.Wind => windOffence,
            AttributeType.Earth => earthOffence,
            _ => 0
        };
    }
}

public enum EquipmentType
{
    Weapon,     // 武器
    Armor,      // 防具
    Accessory   // アクセサリー
}

public enum RarityType
{
    Common,     // コモン
    Rare,       // レア
    Epic,       // エピック
    Legendary   // レジェンダリー
}

public enum AttributeType
{
    None,   // 無属性
    Fire,   // 火
    Water,  // 水
    Wind,   // 風
    Earth   // 土
}