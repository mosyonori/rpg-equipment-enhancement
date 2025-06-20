using UnityEngine;

public enum SkillType
{
    Attack,
    Support,
    Heal,
    Debuff
}

public enum TargetType
{
    SingleEnemy,
    AllEnemies,
    Self,
    AllAllies
}

public enum SkillElement
{
    None,
    Fire,
    Water,
    Wind,
    Earth
}

public enum SkillRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum SkillCategory
{
    Physical,
    Magic,
    Special
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "GameData/SkillData")]
public class SkillMasterData : ScriptableObject
{
    [Header("基本情報")]
    public string skillId;
    public string skillName;
    [TextArea(3, 5)]
    public string skillDescription;

    [Header("スキル効果")]
    public SkillType skillType;
    public TargetType targetType;
    public float damageMultiplier = 1.0f;
    public int maxCoolTime;
    public int mpCost;

    [Header("属性と状態異常")]
    public SkillElement skillElement;
    public string statusEffectId;
    public float statusEffectChance;
    public int statusEffectDuration;

    [Header("表示・演出")]
    public string iconId;
    public string animationId;
    public string soundId;
    public SkillRarity rarity;
    public SkillCategory skillCategory;
}