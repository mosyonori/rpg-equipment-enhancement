using UnityEngine;

public enum StatusEffectType
{
    Buff,
    Debuff
}

[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "GameData/StatusEffectData")]
public class StatusEffectMasterData : ScriptableObject
{
    [Header("基本情報")]
    public string statusEffectId;
    public string statusEffectName;
    [TextArea(3, 5)]
    public string statusEffectDescription;

    [Header("効果設定")]
    public StatusEffectType effectType;
    public bool isStackable;

    [Header("ステータス修正（固定値）")]
    public int attackModifier;
    public int defenseModifier;

    [Header("ステータス修正（倍率）")]
    public float attackMultiplier = 1.0f;
    public float defenseMultiplier = 1.0f;

    [Header("属性攻撃力修正（倍率）")]
    public float fireAttackMultiplier = 1.0f;
    public float waterAttackMultiplier = 1.0f;
    public float windAttackMultiplier = 1.0f;
    public float earthAttackMultiplier = 1.0f;

    [Header("特殊効果")]
    public bool preventAction;
    public float turnStartDamagePercent;
    public float turnStartHealPercent;

    [Header("表示設定")]
    public string iconId;
    public string colorCode = "#FFFFFF";
    public int priority = 100;
}