using UnityEngine;

/// <summary>
/// 戦闘用スキルデータ
/// 戦闘中のスキル状態（CT、使用可能性等）を管理
/// </summary>
[System.Serializable]
public class BattleSkillData
{
    [Header("スキル基本情報")]
    public int skillId;                     // スキルID（SkillMasterDataのID）
    public string skillName;                // スキル名
    public SkillType skillType;             // スキルタイプ
    public AttributeType attributeType;     // スキル属性

    [Header("クールタイム管理")]
    public int currentCoolTime;             // 現在のCT
    public int maxCoolTime;                 // 最大CT
    public bool isUsable;                   // 使用可能フラグ

    [Header("スキル効果")]
    public float damageMultiplier;          // ダメージ倍率
    public TargetType targetType;           // 対象タイプ
    public int statusEffectId;              // 付与する状態効果ID（0=なし）
    public int statusEffectChance;          // 状態効果発動確率（%）

    [Header("使用制限")]
    public int hpCost;                      // HP消費量
    public int mpCost;                      // MP消費量

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public BattleSkillData()
    {
        isUsable = true;
        damageMultiplier = 1.0f;
        targetType = TargetType.EnemySingle;
    }

    /// <summary>
    /// 既存SkillMasterDataからBattleSkillDataを作成
    /// </summary>
    public static BattleSkillData CreateFromSkillMaster(SkillMasterData masterData)
    {
        var battleSkill = new BattleSkillData
        {
            skillId = masterData.skillId,
            skillName = masterData.skillName,
            skillType = masterData.skillType,
            attributeType = masterData.attributeType,
            currentCoolTime = 0, // 戦闘開始時は使用可能
            maxCoolTime = masterData.skillMaxCoolTime,
            isUsable = true,
            damageMultiplier = masterData.skillDamageMultiplier,
            targetType = masterData.skillTargetType,
            hpCost = masterData.skillHpCost,
            mpCost = masterData.skillMpCost
        };

        // 状態効果設定（修正箇所）
        if (!string.IsNullOrEmpty(masterData.skillEffect))
        {
            // 文字列を状態効果IDに変換
            int effectId = ConvertSkillEffectNameToId(masterData.skillEffect);
            if (effectId > 0)
            {
                battleSkill.statusEffectId = effectId;
                battleSkill.statusEffectChance = masterData.skillEffectChance;
            }
        }

        return battleSkill;
    }

    /// <summary>
    /// スキル効果名を状態効果IDに変換
    /// CSVの文字列データを対応する数値IDに変換
    /// </summary>
    private static int ConvertSkillEffectNameToId(string effectName)
    {
        return effectName switch
        {
            "attack_down" => 1,    // 攻撃力低下
            "defense_down" => 2,   // 防御力低下
            "attack_up" => 3,      // 攻撃力増加
            "defense_up" => 4,     // 防御力増加
            "stun" => 6,           // 気絶
            "poison" => 7,         // 毒
            "regen" => 8,          // 継続回復
            _ => 0                 // 不明な効果名は0（効果なし）
        };
    }

    /// <summary>
    /// スキル使用可能かチェック
    /// </summary>
    public bool CanUse(int currentHp, int currentMp)
    {
        return isUsable &&
               currentCoolTime <= 0 &&
               currentHp >= hpCost &&
               currentMp >= mpCost;
    }

    /// <summary>
    /// CTを減算
    /// </summary>
    public void ReduceCoolTime()
    {
        currentCoolTime = Mathf.Max(0, currentCoolTime - 1);
    }

    /// <summary>
    /// CTをリセット（スキル使用後）
    /// </summary>
    public void ResetCoolTime()
    {
        currentCoolTime = maxCoolTime;
    }

    /// <summary>
    /// スキルが攻撃系かチェック
    /// </summary>
    public bool IsAttackSkill()
    {
        return skillType == SkillType.Attack || damageMultiplier > 0f;
    }

    /// <summary>
    /// スキルが回復系かチェック
    /// </summary>
    public bool IsHealSkill()
    {
        return skillType == SkillType.Heal;
    }

    /// <summary>
    /// スキルがバフ系かチェック
    /// </summary>
    public bool IsBuffSkill()
    {
        return skillType == SkillType.Buff || skillType == SkillType.Support;
    }

    /// <summary>
    /// スキルがデバフ系かチェック
    /// </summary>
    public bool IsDebuffSkill()
    {
        return skillType == SkillType.Debuff;
    }

    /// <summary>
    /// 敵対象のスキルかチェック
    /// </summary>
    public bool IsEnemyTargetSkill()
    {
        return targetType == TargetType.EnemySingle ||
               targetType == TargetType.EnemyAll;
    }

    /// <summary>
    /// 味方対象のスキルかチェック
    /// </summary>
    public bool IsAllyTargetSkill()
    {
        return targetType == TargetType.AllySingle ||
               targetType == TargetType.AllyAll ||
               targetType == TargetType.Self;
    }

    /// <summary>
    /// 状態効果を付与するスキルかチェック
    /// </summary>
    public bool HasStatusEffect()
    {
        return statusEffectId > 0 && statusEffectChance > 0;
    }

    /// <summary>
    /// CTの残り割合を取得
    /// </summary>
    public float GetCoolTimeRatio()
    {
        return maxCoolTime > 0 ? (float)currentCoolTime / maxCoolTime : 0f;
    }

    /// <summary>
    /// スキル情報の文字列表現
    /// </summary>
    public override string ToString()
    {
        string ctInfo = maxCoolTime > 0 ? $" CT:{currentCoolTime}/{maxCoolTime}" : "";
        return $"Skill[{skillId}] {skillName} ({skillType}){ctInfo}";
    }
}