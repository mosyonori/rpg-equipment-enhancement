using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戦闘計算管理クラス
/// 戦闘に関わる全ての数値計算処理を担当
/// データアクセス統一ルール: BattleManager → BattleCalculationManager → BattleDataManager → Data層
/// </summary>
public class BattleCalculationManager : MonoBehaviour
{
    [Header("ダメージ計算設定")]
    [SerializeField] private float randomDamageMin = 0.9f;
    [SerializeField] private float randomDamageMax = 1.1f;
    [SerializeField] private int minDamage = 1;

    [Header("属性相性設定")]
    [SerializeField] private float superEffectiveMultiplier = 1.5f;
    [SerializeField] private float notVeryEffectiveMultiplier = 0.75f;

    [Header("デバッグ設定")]
    [SerializeField] private bool enableDebugLog = false;

    // シングルトンパターン
    public static BattleCalculationManager Instance { get; private set; }

    // イベント
    public static event System.Action<DamageData> OnDamageCalculated;

    // 依存Manager参照
    private BattleDataManager battleDataManager;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // BattleDataManagerは同一GameObject内にあることを想定
        battleDataManager = GetComponent<BattleDataManager>();
        if (battleDataManager == null)
        {
            DebugLogError("BattleDataManagerが見つかりません");
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 攻撃ダメージを計算
    /// 戦闘画面フローの仕様に従った計算処理
    /// </summary>
    public DamageData CalculateAttackDamage(
        BattleCharacterData attacker,
        BattleCharacterData defender,
        BattleSkillData skill = null)
    {
        if (attacker == null || defender == null)
        {
            DebugLogError("攻撃者または防御者がnullです");
            return new DamageData();
        }

        // ダメージデータの基本設定
        var damageData = DamageData.CreateDamageResult(attacker, defender, skill);

        // 状態効果適用済みのステータスを取得
        var attackerStats = GetEffectiveStats(attacker);
        var defenderStats = GetEffectiveStats(defender);

        // 攻撃属性の決定
        var attackAttribute = DetermineAttackAttribute(attacker, skill);
        damageData.attackAttribute = attackAttribute;

        // 属性相性の計算
        var attributeMultiplier = CalculateAttributeMultiplier(attackAttribute, defender.characterAttribute);
        damageData.attributeMultiplier = attributeMultiplier;
        damageData.effectiveness = GetDamageEffectiveness(attributeMultiplier);

        // 基本ダメージ計算（戦闘画面フローの仕様に従う）
        int baseDamage = CalculateBaseDamage(
            attackerStats,
            defenderStats,
            attackAttribute,
            attributeMultiplier,
            skill?.damageMultiplier ?? 1.0f);

        damageData.baseDamage = baseDamage;
        damageData.attackerOffense = attackerStats.offense;
        damageData.defenderDefense = defenderStats.defense;
        damageData.skillMultiplier = skill?.damageMultiplier ?? 1.0f;

        // クリティカル判定
        bool isCritical = CalculateCritical(attackerStats.criticalRate);
        damageData.isCritical = isCritical;

        if (isCritical)
        {
            float criticalMultiplier = attackerStats.criticalDamageRate / 100.0f;
            damageData.criticalMultiplier = criticalMultiplier;
            baseDamage = Mathf.RoundToInt(baseDamage * criticalMultiplier);
        }

        // ランダム補正適用
        float randomMultiplier = Random.Range(randomDamageMin, randomDamageMax);
        damageData.randomMultiplier = randomMultiplier;
        baseDamage = Mathf.RoundToInt(baseDamage * randomMultiplier);

        // 最小ダメージ保証
        damageData.finalDamage = Mathf.Max(minDamage, baseDamage);

        DebugLog($"ダメージ計算完了: {attacker.characterName} → {defender.characterName} = {damageData.finalDamage}");

        OnDamageCalculated?.Invoke(damageData);
        return damageData;
    }

    /// <summary>
    /// 回復量を計算
    /// </summary>
    public DamageData CalculateHealAmount(
        BattleCharacterData caster,
        BattleCharacterData target,
        BattleSkillData skill)
    {
        if (caster == null || target == null || skill == null)
        {
            DebugLogError("回復計算でnullパラメータが渡されました");
            return new DamageData();
        }

        var damageData = DamageData.CreateDamageResult(caster, target, skill);
        var casterStats = GetEffectiveStats(caster);

        // 回復量 = 攻撃力 * スキル倍率
        int healAmount = Mathf.RoundToInt(casterStats.offense * skill.damageMultiplier);

        // ランダム補正
        float randomMultiplier = Random.Range(randomDamageMin, randomDamageMax);
        healAmount = Mathf.RoundToInt(healAmount * randomMultiplier);

        // 回復は負の値で表現
        damageData.finalDamage = -Mathf.Max(1, healAmount);
        damageData.randomMultiplier = randomMultiplier;
        damageData.skillMultiplier = skill.damageMultiplier;

        DebugLog($"回復計算完了: {caster.characterName} → {target.characterName} = {-damageData.finalDamage}");

        OnDamageCalculated?.Invoke(damageData);
        return damageData;
    }

    /// <summary>
    /// ターン開始時の状態効果ダメージ/回復を計算
    /// </summary>
    public List<DamageData> CalculateTurnStartEffects(BattleCharacterData character)
    {
        var results = new List<DamageData>();

        if (battleDataManager == null || character == null) return results;

        // BattleDataManager経由で状態効果を取得
        var statusEffects = battleDataManager.GetCharacterStatusEffects(character.characterId);

        foreach (var effect in statusEffects)
        {
            if (!effect.IsActive() || !effect.HasTurnStartEffect()) continue;

            var damageData = new DamageData
            {
                targetId = character.characterId,
                targetName = character.characterName,
                targetHpBefore = character.currentHp,
                attackAttribute = AttributeType.None,
                defenderAttribute = character.characterAttribute
            };

            // ターン開始時の効果量を計算
            int hpChange = effect.CalculateTurnStartHpChange(character.maxHp);
            damageData.finalDamage = -hpChange; // 負の値は回復、正の値はダメージ

            if (hpChange != 0)
            {
                results.Add(damageData);
                DebugLog($"ターン開始効果: {character.characterName} {effect.effectName} = {hpChange}");
            }
        }

        return results;
    }

    /// <summary>
    /// クリティカル率を計算
    /// </summary>
    public bool CalculateCritical(int criticalRate)
    {
        if (criticalRate <= 0) return false;
        return Random.Range(0, 100) < criticalRate;
    }

    /// <summary>
    /// 状態効果の発動判定
    /// </summary>
    public bool CalculateStatusEffectChance(int chance, bool isBossTarget = false)
    {
        if (chance <= 0) return false;

        // ボス相手の場合は効果が低下する可能性がある
        int effectiveChance = isBossTarget ? chance / 2 : chance;
        return Random.Range(0, 100) < effectiveChance;
    }

    /// <summary>
    /// ActionDataに基づくダメージ計算（BattleManager連携用）
    /// </summary>
    public List<DamageData> CalculateActionDamage(ActionData action, List<BattleCharacterData> allCharacters)
    {
        var results = new List<DamageData>();

        if (action == null || allCharacters == null || battleDataManager == null) return results;

        // 行動者を取得
        var actor = allCharacters.Find(c => c.characterId == action.actorId);
        if (actor == null) return results;

        // 対象キャラクターを取得
        foreach (var targetId in action.targetIds)
        {
            var target = allCharacters.Find(c => c.characterId == targetId);
            if (target == null) continue;

            DamageData damageData;

            if (action.IsSkillUse())
            {
                // スキル使用の場合
                var skill = GetSkillData(actor.characterId, action.skillId);
                if (skill != null && skill.IsHealSkill())
                {
                    damageData = CalculateHealAmount(actor, target, skill);
                }
                else
                {
                    damageData = CalculateAttackDamage(actor, target, skill);
                }
            }
            else
            {
                // 通常攻撃の場合
                damageData = CalculateAttackDamage(actor, target);
            }

            results.Add(damageData);
        }

        return results;
    }

    #endregion

    #region 内部メソッド

    /// <summary>
    /// スキルデータを取得（BattleDataManager経由）
    /// </summary>
    private BattleSkillData GetSkillData(string characterId, int skillId)
    {
        if (battleDataManager == null) return null;
        return battleDataManager.GetCharacterSkill(characterId, skillId);
    }

    /// <summary>
    /// 攻撃属性を決定
    /// </summary>
    private AttributeType DetermineAttackAttribute(BattleCharacterData attacker, BattleSkillData skill)
    {
        // スキルに属性が設定されている場合はスキル属性を使用
        if (skill != null && skill.attributeType != AttributeType.None)
        {
            return skill.attributeType;
        }

        // 攻撃者の最も高い属性攻撃力の属性を使用
        return attacker.GetHighestElementalAttackType();
    }

    /// <summary>
    /// 属性相性倍率を計算
    /// </summary>
    private float CalculateAttributeMultiplier(AttributeType attackAttribute, AttributeType defenderAttribute)
    {
        if (attackAttribute == AttributeType.None || defenderAttribute == AttributeType.None)
        {
            return 1.0f;
        }

        // MonsterMasterDataの属性相性計算を活用
        return GetAttributeCompatibility(attackAttribute, defenderAttribute);
    }

    /// <summary>
    /// 属性相性を取得（MonsterMasterDataの仕様に準拠）
    /// </summary>
    private float GetAttributeCompatibility(AttributeType attackerAttribute, AttributeType defenderAttribute)
    {
        return (attackerAttribute, defenderAttribute) switch
        {
            // 有利な相性（1.5倍）
            (AttributeType.Fire, AttributeType.Wind) => superEffectiveMultiplier,
            (AttributeType.Wind, AttributeType.Earth) => superEffectiveMultiplier,
            (AttributeType.Earth, AttributeType.Water) => superEffectiveMultiplier,
            (AttributeType.Water, AttributeType.Fire) => superEffectiveMultiplier,

            // 不利な相性（0.75倍）
            (AttributeType.Wind, AttributeType.Fire) => notVeryEffectiveMultiplier,
            (AttributeType.Earth, AttributeType.Wind) => notVeryEffectiveMultiplier,
            (AttributeType.Water, AttributeType.Earth) => notVeryEffectiveMultiplier,
            (AttributeType.Fire, AttributeType.Water) => notVeryEffectiveMultiplier,

            // 同属性または相性なしは等倍
            _ => 1.0f
        };
    }

    /// <summary>
    /// 基本ダメージを計算（戦闘画面フローの仕様）
    /// </summary>
    private int CalculateBaseDamage(
        EffectiveStats attackerStats,
        EffectiveStats defenderStats,
        AttributeType attackAttribute,
        float attributeMultiplier,
        float skillMultiplier)
    {
        int damage;

        if (attackAttribute == AttributeType.None)
        {
            // 無属性攻撃: d=((a*s1)-(b*s2))*sp
            damage = Mathf.RoundToInt(((attackerStats.offense) - (defenderStats.defense)) * skillMultiplier);
        }
        else
        {
            // 属性攻撃力を取得
            int elementalAttack = GetElementalAttack(attackerStats, attackAttribute);

            if (Mathf.Approximately(attributeMultiplier, superEffectiveMultiplier))
            {
                // 属性攻撃（有利）: d=((ea+a)*s1)*sp （防御力無視）
                damage = Mathf.RoundToInt(((elementalAttack + attackerStats.offense)) * skillMultiplier);
            }
            else if (Mathf.Approximately(attributeMultiplier, notVeryEffectiveMultiplier))
            {
                // 属性攻撃（不利）: d=(((ea*s1)/5+(a*s1))-(b*s2))*sp
                damage = Mathf.RoundToInt((((elementalAttack) / 5.0f + (attackerStats.offense)) - (defenderStats.defense)) * skillMultiplier);
            }
            else
            {
                // 属性攻撃（通常）: d=(((ea*s1)/2+(a*s1))-(b*s2))*sp
                damage = Mathf.RoundToInt((((elementalAttack) / 2.0f + (attackerStats.offense)) - (defenderStats.defense)) * skillMultiplier);
            }
        }

        return Mathf.Max(0, damage);
    }

    /// <summary>
    /// 指定属性の攻撃力を取得
    /// </summary>
    private int GetElementalAttack(EffectiveStats stats, AttributeType attribute)
    {
        return attribute switch
        {
            AttributeType.Fire => stats.fireOffense,
            AttributeType.Water => stats.waterOffense,
            AttributeType.Wind => stats.windOffense,
            AttributeType.Earth => stats.earthOffense,
            _ => 0
        };
    }

    /// <summary>
    /// 状態効果適用済みの実効ステータスを取得
    /// </summary>
    private EffectiveStats GetEffectiveStats(BattleCharacterData character)
    {
        var stats = new EffectiveStats
        {
            offense = character.offense,
            defense = character.defense,
            criticalRate = character.criticalRate,
            criticalDamageRate = character.criticalDamageRate,
            fireOffense = character.fireOffence,
            waterOffense = character.waterOffence,
            windOffense = character.windOffence,
            earthOffense = character.earthOffence
        };

        // 状態効果を適用（BattleDataManager経由）
        ApplyStatusEffectsToStats(character, ref stats);

        return stats;
    }

    /// <summary>
    /// 状態効果による影響を計算（BattleDataManager経由）
    /// </summary>
    public void ApplyStatusEffectsToStats(BattleCharacterData character, ref EffectiveStats stats)
    {
        if (battleDataManager == null || character == null) return;

        // BattleDataManager経由で状態効果を取得
        var statusEffects = battleDataManager.GetCharacterStatusEffects(character.characterId);

        foreach (var effect in statusEffects)
        {
            if (!effect.IsActive()) continue;

            // ステータス効果を適用
            effect.ApplyToStats(
                ref stats.offense,
                ref stats.defense,
                ref stats.fireOffense,
                ref stats.waterOffense,
                ref stats.windOffense,
                ref stats.earthOffense);
        }
    }

    /// <summary>
    /// ダメージ効果を判定
    /// </summary>
    private DamageEffectiveness GetDamageEffectiveness(float multiplier)
    {
        if (Mathf.Approximately(multiplier, superEffectiveMultiplier))
            return DamageEffectiveness.SuperEffective;
        else if (Mathf.Approximately(multiplier, notVeryEffectiveMultiplier))
            return DamageEffectiveness.NotVeryEffective;
        else if (multiplier == 0f)
            return DamageEffectiveness.NoEffect;
        else
            return DamageEffectiveness.Normal;
    }

    #endregion

    #region ログ・デバッグ

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[BattleCalculationManager] {message}");
        }
    }

    private void DebugLogError(string message)
    {
        Debug.LogError($"[BattleCalculationManager] {message}");
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("計算設定を表示")]
    private void ShowCalculationSettings()
    {
        Debug.Log($"=== 戦闘計算設定 ===");
        Debug.Log($"ランダム補正範囲: {randomDamageMin:F2} ~ {randomDamageMax:F2}");
        Debug.Log($"最小ダメージ: {minDamage}");
        Debug.Log($"有利属性倍率: {superEffectiveMultiplier:F2}");
        Debug.Log($"不利属性倍率: {notVeryEffectiveMultiplier:F2}");
        Debug.Log($"デバッグログ: {(enableDebugLog ? "有効" : "無効")}");
    }
#endif

    #endregion
}

/// <summary>
/// 状態効果適用済みの実効ステータス
/// </summary>
public struct EffectiveStats
{
    public int offense;
    public int defense;
    public int criticalRate;
    public int criticalDamageRate;
    public int fireOffense;
    public int waterOffense;
    public int windOffense;
    public int earthOffense;
}