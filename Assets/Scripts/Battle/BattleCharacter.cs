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

// StatusEffectType��StatusEffectMasterData.cs�Œ�`�ς݂Œ�`�ς�

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

    // ��{���ʒl
    public int attackModifier = 0;
    public int defenseModifier = 0;
    public float attackMultiplier = 1.0f;
    public float defenseMultiplier = 1.0f;

    // �����C��
    public float fireAttackMultiplier = 1.0f;
    public float waterAttackMultiplier = 1.0f;
    public float windAttackMultiplier = 1.0f;
    public float earthAttackMultiplier = 1.0f;

    // �������
    public bool preventAction = false;
    public float turnStartDamagePercent = 0f;
    public float turnStartHealPercent = 0f;

    // �p���_���[�W/�񕜂̌Œ�l�Łi�ǉ��j
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
    [Header("��{���")]
    public string characterName;
    public int position;
    public bool isAlive = true;

    [Header("�X�e�[�^�X")]
    public int currentHP;
    public int maxHP;
    public int attackPower;
    public int defensePower;
    public float criticalRate;
    public int speed;

    [Header("�����U����")]
    public int fireAttack;
    public int waterAttack;
    public int windAttack;
    public int earthAttack;

    [Header("�X�L��")]
    public BattleSkill skill1;
    public BattleSkill skill2;

    [Header("��Ԉُ�")]
    public List<StatusEffect> activeEffects = new List<StatusEffect>();

    [Header("�s������")]
    public bool hasActedThisTurn = false;

    // �v�Z�v���p�e�B
    public ElementType PrimaryElement => GetPrimaryElement();
    public int EffectiveAttackPower => CalculateEffectiveAttack();
    public int EffectiveDefensePower => CalculateEffectiveDefense();

    protected virtual void Awake()
    {
        // ���������̈��S����
        if (activeEffects == null)
            activeEffects = new List<StatusEffect>();

        if (currentHP <= 0 && maxHP > 0)
            currentHP = maxHP;

        if (string.IsNullOrEmpty(characterName))
            characterName = gameObject.name;
    }

    // AI�s�����W�b�N�i���ʁj
    public virtual BattleSkill GetNextAction()
    {
        // �X�L��1���g�p�\�ȏꍇ
        if (skill1 != null && skill1.CanUse)
            return skill1;

        // �X�L��2���g�p�\�ȏꍇ
        if (skill2 != null && skill2.CanUse)
            return skill2;

        // �ʏ�U���inull��Ԃ��Ēʏ�U���������j
        return null;
    }

    // �^�[�Q�b�g�I�����W�b�N�i���ʁj
    public virtual BattleCharacter SelectTarget(List<BattleCharacter> enemies)
    {
        if (enemies == null || enemies.Count == 0)
            return null;

        var aliveEnemies = enemies.Where(e => e != null && e.isAlive).ToList();
        if (!aliveEnemies.Any()) return null;

        // 1. �L���ȑ����̑����D��
        var advantageousTargets = GetAdvantageousTargets(aliveEnemies);
        if (advantageousTargets.Any())
        {
            return SelectBySecondaryPriority(advantageousTargets);
        }

        // �L���ȑ������Ȃ��ꍇ�͑S�̂���I��
        return SelectBySecondaryPriority(aliveEnemies);
    }

    // �_���[�W���󂯂�
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

    // ��
    public virtual void Heal(int amount)
    {
        if (!isAlive) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }

    // ��Ԉُ�K�p
    public virtual void ApplyStatusEffect(StatusEffect effect)
    {
        if (effect == null) return;

        if (activeEffects == null)
            activeEffects = new List<StatusEffect>();

        // �����̓������ʂ������i�d���s�̏ꍇ�j
        activeEffects.RemoveAll(e => e.effectId == effect.effectId);
        activeEffects.Add(effect);
    }

    // �^�[���J�n����
    public virtual void OnTurnStart()
    {
        if (activeEffects == null) return;

        // ��Ԉُ�̌��ʓK�p
        foreach (var effect in activeEffects.ToList())
        {
            if (!isAlive) break;

            // �_���[�W���ʁi�p�[�Z���g�Łj
            if (effect.turnStartDamagePercent > 0)
            {
                int damage = Mathf.RoundToInt(maxHP * effect.turnStartDamagePercent / 100f);
                TakeDamage(damage);
                Debug.Log($"{characterName}��{effect.effectName}�ɂ��{damage}�̃_���[�W���󂯂��I");
            }

            // �_���[�W���ʁi�Œ�l�Łj
            if (effect.damagePerTurn > 0)
            {
                TakeDamage(effect.damagePerTurn);
                Debug.Log($"{characterName}��{effect.effectName}�ɂ��{effect.damagePerTurn}�̃_���[�W���󂯂��I");
            }

            // �񕜌��ʁi�p�[�Z���g�Łj
            if (effect.turnStartHealPercent > 0)
            {
                int healAmount = Mathf.RoundToInt(maxHP * effect.turnStartHealPercent / 100f);
                Heal(healAmount);
                Debug.Log($"{characterName}��{effect.effectName}�ɂ��{healAmount}�񕜂����I");
            }

            // �񕜌��ʁi�Œ�l�Łj
            if (effect.healPerTurn > 0)
            {
                Heal(effect.healPerTurn);
                Debug.Log($"{characterName}��{effect.effectName}�ɂ��{effect.healPerTurn}�񕜂����I");
            }
        }
    }

    // �^�[���I������
    public virtual void OnTurnEnd()
    {
        if (activeEffects == null) return;

        // ��Ԉُ�̃^�[��������
        foreach (var effect in activeEffects.ToList())
        {
            effect.ReduceTurn();
            if (effect.IsExpired)
            {
                activeEffects.Remove(effect);
                Debug.Log($"{characterName}��{effect.effectName}���������ꂽ�B");
            }
        }

        // �X�L���N�[���^�C������
        skill1?.ReduceCoolTime();
        skill2?.ReduceCoolTime();

        hasActedThisTurn = false;
    }

    // ���S������
    protected virtual void OnDeath()
    {
        Debug.Log($"{characterName}���|�ꂽ�I");
    }

    // �呮���擾
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

    // �L���ȑ����̑�����擾
    private List<BattleCharacter> GetAdvantageousTargets(List<BattleCharacter> targets)
    {
        ElementType advantageousElement = GetAdvantageousElement(PrimaryElement);
        if (advantageousElement == ElementType.None) return new List<BattleCharacter>();

        return targets.Where(t => t.PrimaryElement == advantageousElement).ToList();
    }

    // ���������ɂ��L���ȑ������擾
    private ElementType GetAdvantageousElement(ElementType myElement)
    {
        return myElement switch
        {
            ElementType.Fire => ElementType.Wind,   // �΂͕��ɗL��
            ElementType.Wind => ElementType.Earth,  // ���͓y�ɗL��
            ElementType.Earth => ElementType.Water, // �y�͐��ɗL��
            ElementType.Water => ElementType.Fire,  // ���͉΂ɗL��
            _ => ElementType.None
        };
    }

    // 2���D��x�Ń^�[�Q�b�g�I��
    private BattleCharacter SelectBySecondaryPriority(List<BattleCharacter> targets)
    {
        if (targets == null || targets.Count == 0) return null;

        // 2. HP���ł��Ⴂ����
        int minHP = targets.Min(t => t.currentHP);
        var lowHPTargets = targets.Where(t => t.currentHP == minHP).ToList();

        if (lowHPTargets.Count == 1)
            return lowHPTargets[0];

        // 3. �X�L����CT���łȂ�����i�ǂ��炩�̃X�L�����g�p�\�j
        var skillReadyTargets = lowHPTargets.Where(t =>
            (t.skill1?.CanUse == true) || (t.skill2?.CanUse == true)).ToList();

        if (skillReadyTargets.Any())
        {
            if (skillReadyTargets.Count == 1)
                return skillReadyTargets[0];

            // 4. �ʒu���߂���
            return skillReadyTargets.OrderBy(t => t.position).First();
        }

        // �X�L�����S��CT���̏ꍇ���ʒu���őI��
        return lowHPTargets.OrderBy(t => t.position).First();
    }

    // ���ʓK�p��̃X�e�[�^�X�v�Z
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