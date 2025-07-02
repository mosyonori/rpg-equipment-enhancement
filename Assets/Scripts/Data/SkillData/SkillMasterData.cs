using UnityEngine;

[CreateAssetMenu(fileName = "Skill_", menuName = "GameData/Skill Master Data")]
public class SkillMasterData : ScriptableObject
{
    [Header("基本情報")]
    public int skillId;
    public string skillName;
    public SkillType skillType;
    public AttributeType attributeType;
    public RarityType rarity;

    [Header("スキル効果")]
    public float skillDamageMultiplier;
    public TargetType skillTargetType;
    public string skillTargetCharacter;

    [Header("使用制限")]
    public int skillMaxCoolTime;
    public int skillHpCost;
    public int skillMpCost;

    [Header("状態効果")]
    public string skillEffect;
    public string skillEffectTargetCharacter;
    public int skillEffectChance;
    public int skillEffectChanceBoss;
    public int skillEffectDuration;

    [Header("表示設定")]
    public Sprite skillIcon;
    public string skillIconPath;
    public string skillAnimationPath;
    public string skillSoundPath;
    [TextArea(3, 5)]
    public string description;

    [Header("フラグ")]
    public bool completionFlag;
    public bool collectionFlag;

    /// <summary>
    /// スキルが使用可能かチェック
    /// </summary>
    public bool CanUse(int currentHp, int currentMp)
    {
        return currentHp >= skillHpCost && currentMp >= skillMpCost;
    }

    /// <summary>
    /// 基本ダメージ倍率を取得
    /// </summary>
    public float GetDamageMultiplier()
    {
        return skillDamageMultiplier;
    }
}

/// <summary>
/// スキルタイプ
/// </summary>
public enum SkillType
{
    Attack,         // 攻撃系
    Heal,           // 回復系
    Buff,           // バフ系
    Debuff,         // デバフ系
    Support,        // サポート系
    Special         // 特殊系
}

/// <summary>
/// スキルの対象タイプ
/// </summary>
public enum TargetType
{
    Self,           // 自分
    EnemySingle,    // 敵単体
    EnemyAll,       // 敵全体
    AllySingle,     // 味方単体
    AllyAll,        // 味方全体
    Random          // ランダム
}