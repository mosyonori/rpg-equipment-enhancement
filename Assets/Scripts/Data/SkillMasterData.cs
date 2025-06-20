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
    [Header("��{���")]
    public string skillId;
    public string skillName;
    [TextArea(3, 5)]
    public string skillDescription;

    [Header("�X�L������")]
    public SkillType skillType;
    public TargetType targetType;
    public float damageMultiplier = 1.0f;
    public int maxCoolTime;
    public int mpCost;

    [Header("�����Ə�Ԉُ�")]
    public SkillElement skillElement;
    public string statusEffectId;
    public float statusEffectChance;
    public int statusEffectDuration;

    [Header("�\���E���o")]
    public string iconId;
    public string animationId;
    public string soundId;
    public SkillRarity rarity;
    public SkillCategory skillCategory;
}