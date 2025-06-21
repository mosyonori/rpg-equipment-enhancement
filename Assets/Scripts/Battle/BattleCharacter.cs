using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public enum ElementType
{
    None,
    Fire,
    Water,
    Wind,
    Earth
}

public enum BattleState
{
    Initializing,
    InProgress,
    Victory,
    Defeat,
    TimeUp
}

public enum BattleResult
{
    None,
    Victory,
    Defeat,
    TimeUp
}

// StatusEffectTypeはStatusEffectMasterData.csで定義済みで定義済み

[System.Serializable]
public class BattleSkill
{
    public string skillId;
    public string skillName;
    public int maxCoolTime;
    public int currentCoolTime;

    public bool CanUse => currentCoolTime <= 0;

    public void Use()
    {
        currentCoolTime = maxCoolTime;
    }

    public void ReduceCoolTime()
    {
        if (currentCoolTime > 0)
            currentCoolTime--;
    }
}

[System.Serializable]
public class StatusEffect
{
    public string effectId;
    public string effectName;
    public int remainingTurns;
    public StatusEffectType effectType;

    // 基本効果値
    public int attackModifier = 0;
    public int defenseModifier = 0;
    public float attackMultiplier = 1.0f;
    public float defenseMultiplier = 1.0f;

    // 属性修正
    public float fireAttackMultiplier = 1.0f;
    public float waterAttackMultiplier = 1.0f;
    public float windAttackMultiplier = 1.0f;
    public float earthAttackMultiplier = 1.0f;

    // 特殊効果
    public bool preventAction = false;
    public float turnStartDamagePercent = 0f;
    public float turnStartHealPercent = 0f;

    // 継続ダメージ/回復の固定値版（追加）
    public int damagePerTurn = 0;
    public int healPerTurn = 0;

    public bool IsExpired => remainingTurns <= 0;

    public void ReduceTurn()
    {
        if (remainingTurns > 0)
            remainingTurns--;
    }
}

public abstract class BattleCharacter : MonoBehaviour
{
    [Header("基本情報")]
    public string characterName;
    public int position;
    public bool isAlive = true;

    [Header("ステータス")]
    public int currentHP;
    public int maxHP;
    public int attackPower;
    public int defensePower;
    public float criticalRate;
    public int speed;

    [Header("属性攻撃力")]
    public int fireAttack;
    public int waterAttack;
    public int windAttack;
    public int earthAttack;

    [Header("スキル")]
    public BattleSkill skill1;
    public BattleSkill skill2;

    [Header("状態異常")]
    public List<StatusEffect> activeEffects = new List<StatusEffect>();

    [Header("行動制御")]
    public bool hasActedThisTurn = false;

    // 計算プロパティ
    public ElementType PrimaryElement => GetPrimaryElement();
    public int EffectiveAttackPower => CalculateEffectiveAttack();
    public int EffectiveDefensePower => CalculateEffectiveDefense();

    protected virtual void Awake()
    {
        // 初期化時の安全処理
        if (activeEffects == null)
            activeEffects = new List<StatusEffect>();

        if (currentHP <= 0 && maxHP > 0)
            currentHP = maxHP;

        if (string.IsNullOrEmpty(characterName))
            characterName = gameObject.name;
    }

    // AI行動ロジック（共通）
    public virtual BattleSkill GetNextAction()
    {
        // スキル1が使用可能な場合
        if (skill1 != null && skill1.CanUse)
            return skill1;

        // スキル2が使用可能な場合
        if (skill2 != null && skill2.CanUse)
            return skill2;

        // 通常攻撃（nullを返して通常攻撃を示す）
        return null;
    }

    // ターゲット選択ロジック（共通）
    public virtual BattleCharacter SelectTarget(List<BattleCharacter> enemies)
    {
        if (enemies == null || enemies.Count == 0)
            return null;

        var aliveEnemies = enemies.Where(e => e != null && e.isAlive).ToList();
        if (!aliveEnemies.Any()) return null;

        // 1. 有利な属性の相手を優先
        var advantageousTargets = GetAdvantageousTargets(aliveEnemies);
        if (advantageousTargets.Any())
        {
            return SelectBySecondaryPriority(advantageousTargets);
        }

        // 有利な属性がない場合は全体から選択
        return SelectBySecondaryPriority(aliveEnemies);
    }

    // ダメージを受ける
    public virtual void TakeDamage(int damage)
    {
        if (!isAlive) return;

        currentHP = Mathf.Max(0, currentHP - damage);

        if (currentHP <= 0)
        {
            isAlive = false;
            OnDeath();
        }
    }

    // 回復
    public virtual void Heal(int amount)
    {
        if (!isAlive) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }

    // 状態異常適用
    public virtual void ApplyStatusEffect(StatusEffect effect)
    {
        if (effect == null) return;

        if (activeEffects == null)
            activeEffects = new List<StatusEffect>();

        // 既存の同じ効果を除去（重複不可の場合）
        activeEffects.RemoveAll(e => e.effectId == effect.effectId);
        activeEffects.Add(effect);
    }

    // ターン開始処理
    public virtual void OnTurnStart()
    {
        if (activeEffects == null) return;

        // 状態異常の効果適用
        foreach (var effect in activeEffects.ToList())
        {
            if (!isAlive) break;

            // ダメージ効果（パーセント版）
            if (effect.turnStartDamagePercent > 0)
            {
                int damage = Mathf.RoundToInt(maxHP * effect.turnStartDamagePercent / 100f);
                TakeDamage(damage);
                Debug.Log($"{characterName}は{effect.effectName}により{damage}のダメージを受けた！");
            }

            // ダメージ効果（固定値版）
            if (effect.damagePerTurn > 0)
            {
                TakeDamage(effect.damagePerTurn);
                Debug.Log($"{characterName}は{effect.effectName}により{effect.damagePerTurn}のダメージを受けた！");
            }

            // 回復効果（パーセント版）
            if (effect.turnStartHealPercent > 0)
            {
                int healAmount = Mathf.RoundToInt(maxHP * effect.turnStartHealPercent / 100f);
                Heal(healAmount);
                Debug.Log($"{characterName}は{effect.effectName}により{healAmount}回復した！");
            }

            // 回復効果（固定値版）
            if (effect.healPerTurn > 0)
            {
                Heal(effect.healPerTurn);
                Debug.Log($"{characterName}は{effect.effectName}により{effect.healPerTurn}回復した！");
            }
        }
    }

    // ターン終了処理
    public virtual void OnTurnEnd()
    {
        if (activeEffects == null) return;

        // 状態異常のターン数減少
        foreach (var effect in activeEffects.ToList())
        {
            effect.ReduceTurn();
            if (effect.IsExpired)
            {
                activeEffects.Remove(effect);
                Debug.Log($"{characterName}の{effect.effectName}が解除された。");
            }
        }

        // スキルクールタイム減少
        skill1?.ReduceCoolTime();
        skill2?.ReduceCoolTime();

        hasActedThisTurn = false;
    }

    // 死亡時処理
    protected virtual void OnDeath()
    {
        Debug.Log($"{characterName}が倒れた！");
    }

    // 主属性取得
    private ElementType GetPrimaryElement()
    {
        int maxAttack = Mathf.Max(Mathf.Max(fireAttack, waterAttack),
                                Mathf.Max(windAttack, earthAttack));

        if (maxAttack == 0) return ElementType.None;

        if (fireAttack == maxAttack) return ElementType.Fire;
        if (waterAttack == maxAttack) return ElementType.Water;
        if (windAttack == maxAttack) return ElementType.Wind;
        if (earthAttack == maxAttack) return ElementType.Earth;

        return ElementType.None;
    }

    // 有利な属性の相手を取得
    private List<BattleCharacter> GetAdvantageousTargets(List<BattleCharacter> targets)
    {
        ElementType advantageousElement = GetAdvantageousElement(PrimaryElement);
        if (advantageousElement == ElementType.None) return new List<BattleCharacter>();

        return targets.Where(t => t.PrimaryElement == advantageousElement).ToList();
    }

    // 属性相性による有利な属性を取得
    private ElementType GetAdvantageousElement(ElementType myElement)
    {
        return myElement switch
        {
            ElementType.Fire => ElementType.Wind,   // 火は風に有利
            ElementType.Wind => ElementType.Earth,  // 風は土に有利
            ElementType.Earth => ElementType.Water, // 土は水に有利
            ElementType.Water => ElementType.Fire,  // 水は火に有利
            _ => ElementType.None
        };
    }

    // 2次優先度でターゲット選択
    private BattleCharacter SelectBySecondaryPriority(List<BattleCharacter> targets)
    {
        if (targets == null || targets.Count == 0) return null;

        // 2. HPが最も低い相手
        int minHP = targets.Min(t => t.currentHP);
        var lowHPTargets = targets.Where(t => t.currentHP == minHP).ToList();

        if (lowHPTargets.Count == 1)
            return lowHPTargets[0];

        // 3. スキルがCT中でない相手（どちらかのスキルが使用可能）
        var skillReadyTargets = lowHPTargets.Where(t =>
            (t.skill1?.CanUse == true) || (t.skill2?.CanUse == true)).ToList();

        if (skillReadyTargets.Any())
        {
            if (skillReadyTargets.Count == 1)
                return skillReadyTargets[0];

            // 4. 位置が近い順
            return skillReadyTargets.OrderBy(t => t.position).First();
        }

        // スキルが全てCT中の場合も位置順で選択
        return lowHPTargets.OrderBy(t => t.position).First();
    }

    // 効果適用後のステータス計算
    private int CalculateEffectiveAttack()
    {
        if (activeEffects == null) return attackPower;

        int baseValue = attackPower;
        float multiplier = 1.0f;

        foreach (var effect in activeEffects)
        {
            baseValue += effect.attackModifier;
            multiplier *= effect.attackMultiplier;
        }

        return Mathf.RoundToInt(baseValue * multiplier);
    }

    private int CalculateEffectiveDefense()
    {
        if (activeEffects == null) return defensePower;

        int baseValue = defensePower;
        float multiplier = 1.0f;

        foreach (var effect in activeEffects)
        {
            baseValue += effect.defenseModifier;
            multiplier *= effect.defenseMultiplier;
        }

        return Mathf.RoundToInt(baseValue * multiplier);
    }
}