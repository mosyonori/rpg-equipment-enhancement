using UnityEngine;

public static class DamageCalculator
{
    /// <summary>
    /// メインのダメージ計算メソッド
    /// </summary>
    public static int CalculateDamage(BattleCharacter attacker, BattleCharacter target, BattleSkill skill = null)
    {
        // スキル倍率取得
        float skillMultiplier = GetSkillMultiplier(skill);

        // 攻撃者の実効ステータス取得
        int effectiveAttack = attacker.EffectiveAttackPower;
        int targetDefense = target.EffectiveDefensePower;

        // 属性攻撃力と相性を考慮したダメージ計算
        float damage = CalculateElementalDamage(attacker, target, effectiveAttack, targetDefense, skillMultiplier);

        // ランダム補正 (0.9~1.1倍)
        damage *= Random.Range(0.9f, 1.1f);

        // クリティカル判定
        if (IsCriticalHit(attacker.criticalRate))
        {
            damage *= 1.5f;
            Debug.Log("💥 クリティカルヒット！");
        }

        // 最低ダメージ保証
        damage = Mathf.Max(1, damage);

        return Mathf.RoundToInt(damage);
    }

    /// <summary>
    /// 属性を考慮したダメージ計算
    /// </summary>
    private static float CalculateElementalDamage(BattleCharacter attacker, BattleCharacter target,
                                                 int baseAttack, int defense, float skillMultiplier)
    {
        // 攻撃者の最大属性攻撃力を取得
        int maxElementalAttack = GetMaxElementalAttack(attacker);
        ElementType attackerElement = attacker.PrimaryElement;
        ElementType targetElement = target.PrimaryElement;

        // 無属性攻撃の場合
        if (attackerElement == ElementType.None || maxElementalAttack == 0)
        {
            return CalculateNonElementalDamage(baseAttack, defense, skillMultiplier);
        }

        // 属性攻撃の場合
        return CalculateElementalAttackDamage(baseAttack, maxElementalAttack, defense,
                                            attackerElement, targetElement, skillMultiplier);
    }

    /// <summary>
    /// 無属性攻撃のダメージ計算
    /// </summary>
    private static float CalculateNonElementalDamage(int attack, int defense, float skillMultiplier)
    {
        // d=((a*s1)-(b*s2))*sp
        float damage = (attack - defense) * skillMultiplier;
        return Mathf.Max(0, damage);
    }

    /// <summary>
    /// 属性攻撃のダメージ計算
    /// </summary>
    private static float CalculateElementalAttackDamage(int baseAttack, int elementalAttack, int defense,
                                                      ElementType attackerElement, ElementType targetElement,
                                                      float skillMultiplier)
    {
        // 属性相性判定
        ElementalAdvantage advantage = GetElementalAdvantage(attackerElement, targetElement);

        switch (advantage)
        {
            case ElementalAdvantage.Advantage:
                // パターン1：相手が有利な属性の場合 - 防御力無視
                // d=((ea+a)*s1)*sp
                return (elementalAttack + baseAttack) * skillMultiplier;

            case ElementalAdvantage.Neutral:
                // パターン2：有利でも不利でもない場合
                // d=(((ea*s1)/2+(a*s1))-(b*s2))*sp
                float neutralDamage = ((elementalAttack / 2f) + baseAttack - defense) * skillMultiplier;
                return Mathf.Max(0, neutralDamage);

            case ElementalAdvantage.Disadvantage:
                // パターン3：相手が不利な属性の場合
                // d=(((ea*s1)/5+(a*s1))-(b*s2))*sp
                float disadvantageDamage = ((elementalAttack / 5f) + baseAttack - defense) * skillMultiplier;
                return Mathf.Max(0, disadvantageDamage);

            default:
                return CalculateNonElementalDamage(baseAttack, defense, skillMultiplier);
        }
    }

    /// <summary>
    /// 攻撃者の最大属性攻撃力を取得
    /// </summary>
    private static int GetMaxElementalAttack(BattleCharacter character)
    {
        return Mathf.Max(
            Mathf.Max(character.fireAttack, character.waterAttack),
            Mathf.Max(character.windAttack, character.earthAttack)
        );
    }

    /// <summary>
    /// スキル倍率を取得
    /// </summary>
    private static float GetSkillMultiplier(BattleSkill skill)
    {
        if (skill == null) return 1.0f; // 通常攻撃

        // スキルIDに基づいた倍率設定（マスターデータから取得できない場合のフォールバック）
        return skill.skillId switch
        {
            "skill_fire_ball" => 1.3f,
            "skill_quick_strike" => 1.1f,
            "skill_guard" => 0f, // 攻撃スキルではない
            "skill_wind_claw" => 1.4f,
            "skill_howl" => 0f, // デバフスキル
            "skill_earthquake" => 1.2f,
            "skill_stone_armor" => 0f, // バフスキル
            "skill_heal_small" => 0.3f, // 回復スキル（回復量計算用）
            "skill_player_attack" => 1.3f,
            "skill_player_heal" => 0.3f,
            _ => 1.0f
        };
    }

    /// <summary>
    /// 属性相性を判定
    /// </summary>
    private static ElementalAdvantage GetElementalAdvantage(ElementType attackerElement, ElementType targetElement)
    {
        if (targetElement == ElementType.None) return ElementalAdvantage.Neutral;

        // 火→風→土→水→火 の相性チェーン
        bool isAdvantage = (attackerElement == ElementType.Fire && targetElement == ElementType.Wind) ||
                          (attackerElement == ElementType.Wind && targetElement == ElementType.Earth) ||
                          (attackerElement == ElementType.Earth && targetElement == ElementType.Water) ||
                          (attackerElement == ElementType.Water && targetElement == ElementType.Fire);

        bool isDisadvantage = (targetElement == ElementType.Fire && attackerElement == ElementType.Wind) ||
                             (targetElement == ElementType.Wind && attackerElement == ElementType.Earth) ||
                             (targetElement == ElementType.Earth && attackerElement == ElementType.Water) ||
                             (targetElement == ElementType.Water && attackerElement == ElementType.Fire);

        if (isAdvantage) return ElementalAdvantage.Advantage;
        if (isDisadvantage) return ElementalAdvantage.Disadvantage;
        return ElementalAdvantage.Neutral;
    }

    /// <summary>
    /// クリティカル判定
    /// </summary>
    private static bool IsCriticalHit(float criticalRate)
    {
        return Random.Range(0f, 100f) < criticalRate;
    }

    /// <summary>
    /// 回復量計算（スキル用）
    /// </summary>
    public static int CalculateHealAmount(BattleCharacter healer, BattleSkill skill)
    {
        if (skill == null) return 0;

        float skillMultiplier = GetSkillMultiplier(skill);

        // 回復スキルの場合は最大HPの一定割合を回復
        if (skill.skillId.Contains("heal"))
        {
            return Mathf.RoundToInt(healer.maxHP * skillMultiplier);
        }

        return 0;
    }

    /// <summary>
    /// 状態異常効果のダメージ計算（毒など）
    /// </summary>
    public static int CalculateStatusEffectDamage(BattleCharacter target, float damagePercent)
    {
        return Mathf.RoundToInt(target.maxHP * damagePercent / 100f);
    }

    /// <summary>
    /// 状態異常効果の回復量計算（継続回復など）
    /// </summary>
    public static int CalculateStatusEffectHeal(BattleCharacter target, float healPercent)
    {
        return Mathf.RoundToInt(target.maxHP * healPercent / 100f);
    }
}

/// <summary>
/// 属性相性の結果
/// </summary>
public enum ElementalAdvantage
{
    Advantage,      // 有利
    Neutral,        // 普通
    Disadvantage    // 不利
}