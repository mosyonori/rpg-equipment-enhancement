using UnityEngine;

[CreateAssetMenu(fileName = "SkillEffect_", menuName = "GameData/Skill Effect Master Data")]
public class SkillEffectMasterData : ScriptableObject
{
    [Header("基本情報")]
    public int statusEffectId;
    public StatusEffectType statusEffectType;
    public string statusEffectName;
    [TextArea(3, 5)]
    public string description;

    [Header("効果設定")]
    public EffectType effectType;
    public bool stackable;

    [Header("ステータス修正値")]
    public int offenseModifier;
    public int defenseModifier;

    [Header("ステータス倍率")]
    public float offenseMultiplier = 1.0f;
    public float defenseMultiplier = 1.0f;
    public float fireOffenseMultiplier = 1.0f;
    public float waterOffenseMultiplier = 1.0f;
    public float windOffenseMultiplier = 1.0f;
    public float earthOffenseMultiplier = 1.0f;

    [Header("特殊効果")]
    public bool preventAction;
    public int turnStartDamagePercent;
    public int turnStartHealPercent;

    [Header("表示設定")]
    public string skillEffectIconId;
    public string colorCode;
    public int skillEffectPriority;

    /// <summary>
    /// ステータスへの効果を適用
    /// </summary>
    public EquipmentTotalStats ApplyEffect(EquipmentTotalStats baseStats)
    {
        EquipmentTotalStats result = baseStats;

        // 加算効果
        result.offense += offenseModifier;
        result.defense += defenseModifier;

        // 乗算効果
        result.offense = Mathf.RoundToInt(result.offense * offenseMultiplier);
        result.defense = Mathf.RoundToInt(result.defense * defenseMultiplier);
        result.fireOffence = Mathf.RoundToInt(result.fireOffence * fireOffenseMultiplier);
        result.waterOffence = Mathf.RoundToInt(result.waterOffence * waterOffenseMultiplier);
        result.windOffence = Mathf.RoundToInt(result.windOffence * windOffenseMultiplier);
        result.earthOffence = Mathf.RoundToInt(result.earthOffence * earthOffenseMultiplier);

        return result;
    }

    /// <summary>
    /// ターン開始時の処理が必要かチェック
    /// </summary>
    public bool HasTurnStartEffect()
    {
        return turnStartDamagePercent > 0 || turnStartHealPercent > 0;
    }
}

/// <summary>
/// 状態効果タイプ
/// </summary>
public enum StatusEffectType
{
    AttackDown,     // 攻撃力低下
    DefenseDown,    // 防御力低下
    AttackUp,       // 攻撃力上昇
    DefenseUp,      // 防御力上昇
    Stun,          // スタン
    Poison,        // 毒
    Regen          // 再生
}

/// <summary>
/// 効果タイプ
/// </summary>
public enum EffectType
{
    Damage,         // ダメージ
    Heal,           // 回復
    StatusModifier, // ステータス修正
    ActionBlock,    // 行動阻害
    Special         // 特殊効果
}