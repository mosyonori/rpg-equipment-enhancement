using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ダメージ計算結果データ
/// 1回の攻撃によるダメージ情報を詳細に保持
/// </summary>
[System.Serializable]
public class DamageData
{
    [Header("ダメージ情報")]
    public int baseDamage;                  // 基本ダメージ
    public float attributeMultiplier;       // 属性相性倍率
    public float randomMultiplier;          // ランダム補正倍率
    public bool isCritical;                 // クリティカルフラグ
    public int finalDamage;                 // 最終ダメージ

    [Header("属性情報")]
    public AttributeType attackAttribute;   // 攻撃属性
    public AttributeType defenderAttribute; // 防御側属性
    public DamageEffectiveness effectiveness; // 効果

    [Header("対象情報")]
    public string targetId;                 // 対象ID
    public string targetName;               // 対象名
    public int targetHpBefore;              // ダメージ前HP
    public int targetHpAfter;               // ダメージ後HP

    [Header("特殊効果")]
    public bool targetDefeated;             // 対象撃破フラグ
    public List<StatusEffectData> appliedEffects; // 付与された状態効果

    [Header("計算詳細")]
    public int attackerOffense;             // 攻撃者の攻撃力
    public int defenderDefense;             // 防御者の防御力
    public float skillMultiplier;           // スキル倍率
    public float criticalMultiplier;        // クリティカル倍率

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public DamageData()
    {
        appliedEffects = new List<StatusEffectData>();
        attributeMultiplier = 1.0f;
        randomMultiplier = 1.0f;
        skillMultiplier = 1.0f;
        criticalMultiplier = 1.0f;
        effectiveness = DamageEffectiveness.Normal;
    }

    /// <summary>
    /// ダメージ計算結果を作成
    /// </summary>
    public static DamageData CreateDamageResult(
        BattleCharacterData attacker,
        BattleCharacterData defender,
        BattleSkillData skill = null)
    {
        var damageData = new DamageData
        {
            targetId = defender.characterId,
            targetName = defender.characterName,
            targetHpBefore = defender.currentHp,
            attackerOffense = attacker.offense,
            defenderDefense = defender.defense
        };

        // 攻撃属性決定
        if (skill != null && skill.attributeType != AttributeType.None)
        {
            // スキルに属性が設定されている場合はスキル属性を使用
            damageData.attackAttribute = skill.attributeType;
        }
        else
        {
            // 攻撃者の最も高い属性攻撃力の属性を使用
            damageData.attackAttribute = attacker.GetHighestElementalAttackType();
        }

        damageData.defenderAttribute = defender.characterAttribute;

        // スキル倍率設定
        if (skill != null)
        {
            damageData.skillMultiplier = skill.damageMultiplier;
        }

        return damageData;
    }

    /// <summary>
    /// ダメージが有効かチェック
    /// </summary>
    public bool IsEffective()
    {
        return finalDamage > 0;
    }

    /// <summary>
    /// 回復かチェック
    /// </summary>
    public bool IsHealing()
    {
        return finalDamage < 0;
    }

    /// <summary>
    /// 無効化されたかチェック
    /// </summary>
    public bool IsNullified()
    {
        return finalDamage == 0 && baseDamage > 0;
    }

    /// <summary>
    /// HPダメージを適用して撃破判定
    /// </summary>
    public void ApplyDamageToTarget(BattleCharacterData target)
    {
        targetHpBefore = target.currentHp;

        if (IsHealing())
        {
            // 回復処理
            target.currentHp = Mathf.Min(target.maxHp, target.currentHp - finalDamage);
        }
        else
        {
            // ダメージ処理
            target.currentHp = Mathf.Max(0, target.currentHp - finalDamage);
            target.damageReceived += finalDamage;
        }

        targetHpAfter = target.currentHp;

        // 撃破判定
        if (target.currentHp <= 0)
        {
            targetDefeated = true;
            target.isAlive = false;
        }
    }

    /// <summary>
    /// ダメージ効果の文字列を取得
    /// </summary>
    public string GetEffectivenessText()
    {
        return effectiveness switch
        {
            DamageEffectiveness.SuperEffective => "効果抜群！",
            DamageEffectiveness.NotVeryEffective => "効果今ひとつ...",
            DamageEffectiveness.NoEffect => "効果なし",
            _ => ""
        };
    }

    /// <summary>
    /// ダメージの色情報を取得（UI表示用）
    /// </summary>
    public Color GetDamageColor()
    {
        if (IsHealing())
        {
            return Color.green; // 回復は緑
        }
        else if (isCritical)
        {
            return Color.red; // クリティカルは赤
        }
        else if (effectiveness == DamageEffectiveness.SuperEffective)
        {
            return Color.yellow; // 効果抜群は黄色
        }
        else if (effectiveness == DamageEffectiveness.NotVeryEffective)
        {
            return Color.gray; // 効果今ひとつは灰色
        }
        else if (IsNullified())
        {
            return Color.blue; // 無効化は青
        }
        else
        {
            return Color.white; // 通常は白
        }
    }

    /// <summary>
    /// ダメージ計算の詳細情報を取得
    /// </summary>
    public string GetCalculationDetails()
    {
        string details = $"基本ダメージ: {baseDamage}\n";

        if (skillMultiplier != 1.0f)
        {
            details += $"スキル倍率: x{skillMultiplier:F2}\n";
        }

        if (attributeMultiplier != 1.0f)
        {
            details += $"属性相性: x{attributeMultiplier:F2}\n";
        }

        if (isCritical)
        {
            details += $"クリティカル: x{criticalMultiplier:F2}\n";
        }

        if (randomMultiplier != 1.0f)
        {
            details += $"ランダム補正: x{randomMultiplier:F2}\n";
        }

        details += $"最終ダメージ: {finalDamage}";

        return details;
    }

    /// <summary>
    /// HPバー表示用の変化割合を取得
    /// </summary>
    public float GetHpChangeRatio(int maxHp)
    {
        if (maxHp <= 0) return 0f;
        return (float)(targetHpBefore - targetHpAfter) / maxHp;
    }

    /// <summary>
    /// ダメージデータの文字列表現
    /// </summary>
    public override string ToString()
    {
        string damageType = IsHealing() ? "回復" : "ダメージ";
        string critical = isCritical ? " [クリティカル]" : "";
        string effectiveness = GetEffectivenessText();
        string effectivenessText = !string.IsNullOrEmpty(effectiveness) ? $" [{effectiveness}]" : "";

        return $"{targetName}に{Mathf.Abs(finalDamage)}{damageType}{critical}{effectivenessText}";
    }
}

/// <summary>
/// ダメージ効果
/// </summary>
public enum DamageEffectiveness
{
    Normal,             // 通常
    SuperEffective,     // 効果抜群
    NotVeryEffective,   // 効果今ひとつ
    NoEffect            // 効果なし
}