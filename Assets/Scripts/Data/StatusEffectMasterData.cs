using UnityEngine;

public enum StatusEffectType
{
    Buff,
    Debuff
}

[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "GameData/StatusEffectData")]
public class StatusEffectMasterData : ScriptableObject
{
    [Header("��{���")]
    public string statusEffectId;
    public string statusEffectName;
    [TextArea(3, 5)]
    public string statusEffectDescription;

    [Header("���ʐݒ�")]
    public StatusEffectType effectType;
    public bool isStackable;

    [Header("�X�e�[�^�X�C���i�Œ�l�j")]
    public int attackModifier;
    public int defenseModifier;

    [Header("�X�e�[�^�X�C���i�{���j")]
    public float attackMultiplier = 1.0f;
    public float defenseMultiplier = 1.0f;

    [Header("�����U���͏C���i�{���j")]
    public float fireAttackMultiplier = 1.0f;
    public float waterAttackMultiplier = 1.0f;
    public float windAttackMultiplier = 1.0f;
    public float earthAttackMultiplier = 1.0f;

    [Header("�������")]
    public bool preventAction;
    public float turnStartDamagePercent;
    public float turnStartHealPercent;

    [Header("�\���ݒ�")]
    public string iconId;
    public string colorCode = "#FFFFFF";
    public int priority = 100;
}