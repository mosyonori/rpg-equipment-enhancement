using UnityEngine;

[System.Serializable]
public class EquipmentSkill
{
    [Header("スキル情報")]
    public int skillId;
    public string skillName;
    public string description;
    public int requiredEnhancementLevel; // 解放に必要な強化値
    public float cooldown; // クールダウン時間
    public int damage; // スキルダメージ
    public float effect; // 効果値（回復量、バフ効果など）
}

[System.Serializable]
public class EquipmentStats
{
    [Header("基本ステータス")]
    public int baseAttackPower;
    public int baseDefensePower;
    public int baseElementalAttack;  // 汎用属性攻撃（互換性維持）
    public int baseHP;
    public float baseCriticalRate;
    public int baseDurability;

    [Header("4種類の属性攻撃")]
    public int baseFireAttack = 0;      // 火属性攻撃
    public int baseWaterAttack = 0;     // 水属性攻撃
    public int baseWindAttack = 0;      // 風属性攻撃
    public int baseEarthAttack = 0;     // 土属性攻撃

    [Header("強化時成長量")]
    public int attackPowerPerLevel;
    public int defensePowerPerLevel;
    public int elementalAttackPerLevel;
    public int hpPerLevel;
    public float criticalRatePerLevel;

    [Header("属性攻撃成長量")]
    public int fireAttackPerLevel = 0;   // 火属性攻撃の成長量
    public int waterAttackPerLevel = 0;  // 水属性攻撃の成長量
    public int windAttackPerLevel = 0;   // 風属性攻撃の成長量
    public int earthAttackPerLevel = 0;  // 土属性攻撃の成長量
}

[CreateAssetMenu(fileName = "NewEquipment", menuName = "GameData/Equipment")]
public class EquipmentData : ScriptableObject
{
    [Header("基本情報")]
    public int equipmentId;
    public string equipmentName;
    public string description;
    public Sprite icon;
    public Sprite enhancedIcon; // 強化時のアイコン

    [Header("装備タイプ")]
    public EquipmentType equipmentType;

    [Header("ステータス")]
    public EquipmentStats stats;

    [Header("スキル")]
    public EquipmentSkill[] skills;

    [Header("強化設定")]
    [Range(0f, 1f)]
    public float baseSuccessRate = 0.8f; // 基本成功率
    public float successRateDecreasePerLevel = 0.01f; // レベルごとの成功率減少

    [Header("見た目変化")]
    public EnhancementVisual[] visualChanges;
}

[System.Serializable]
public class EnhancementVisual
{
    public int enhancementLevel; // この強化値で見た目変化
    public Sprite newIcon;
    public Color glowColor;
    public GameObject effectPrefab;
}

public enum EquipmentType
{
    Weapon,
    Armor,
    Accessory
}