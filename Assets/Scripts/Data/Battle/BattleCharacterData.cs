using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戦闘中のキャラクター状態データ
/// プレイヤー・モンスター共通で使用
/// </summary>
[System.Serializable]
public class BattleCharacterData
{
    [Header("基本情報")]
    public string characterId;              // キャラクター識別ID
    public string characterName;            // キャラクター名
    public bool isPlayer;                   // プレイヤーキャラクターか
    public bool isAlive;                    // 生存フラグ

    [Header("現在ステータス")]
    public int currentHp;                   // 現在HP
    public int maxHp;                       // 最大HP
    public int currentMp;                   // 現在MP（将来拡張用）
    public int maxMp;                       // 最大MP（将来拡張用）

    [Header("基本能力値")]
    public int offense;                     // 攻撃力
    public int defense;                     // 防御力
    public int speed;                       // 速度
    public int criticalRate;                // クリティカル率
    public int criticalDamageRate;          // クリティカルダメージ率

    [Header("属性攻撃力")]
    public int fireOffence;                 // 火属性攻撃力
    public int waterOffence;                // 水属性攻撃力
    public int windOffence;                 // 風属性攻撃力
    public int earthOffence;                // 土属性攻撃力

    [Header("キャラクター属性")]
    public AttributeType characterAttribute; // キャラクター自身の属性

    [Header("使用可能スキル")]
    public List<BattleSkillData> availableSkills; // 使用可能スキルリスト

    [Header("状態効果")]
    public List<StatusEffectData> statusEffects;  // 現在の状態効果リスト

    [Header("戦闘統計")]
    public int damageDealt;                 // 与えたダメージ累計
    public int damageReceived;              // 受けたダメージ累計
    public int skillsUsed;                  // 使用スキル回数

    [Header("既存データ参照用")]
    public int masterDataId;                // CharacterMasterData or MonsterMasterDataのID

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public BattleCharacterData()
    {
        availableSkills = new List<BattleSkillData>();
        statusEffects = new List<StatusEffectData>();
        isAlive = true;
    }

    /// <summary>
    /// 既存CharacterMasterDataからBattleCharacterDataを作成
    /// </summary>
    public static BattleCharacterData CreateFromCharacterMaster(CharacterMasterData masterData, EquipmentTotalStats equipmentStats)
    {
        var battleChar = new BattleCharacterData
        {
            characterId = "player",
            characterName = masterData.CharacterName,
            isPlayer = true,
            isAlive = true,
            masterDataId = masterData.CharacterId,

            // 装備込みステータス設定
            maxHp = masterData.Hp + equipmentStats.hp,
            currentHp = masterData.Hp + equipmentStats.hp,
            offense = masterData.Offense + equipmentStats.offense,
            defense = masterData.Defense + equipmentStats.defense,
            speed = masterData.Speed + equipmentStats.speed,
            criticalRate = masterData.CriticalRate + equipmentStats.criticalRate,
            criticalDamageRate = masterData.CriticalDamageRate + equipmentStats.criticalDamageRate,

            // 属性攻撃力
            fireOffence = masterData.FireOffence + equipmentStats.fireOffence,
            waterOffence = masterData.WaterOffence + equipmentStats.waterOffence,
            windOffence = masterData.WindOffence + equipmentStats.windOffence,
            earthOffence = masterData.EarthOffence + equipmentStats.earthOffence,

            // キャラクター属性（属性攻撃力から判定）
            characterAttribute = GetDominantAttribute(
                masterData.FireOffence + equipmentStats.fireOffence,
                masterData.WaterOffence + equipmentStats.waterOffence,
                masterData.WindOffence + equipmentStats.windOffence,
                masterData.EarthOffence + equipmentStats.earthOffence
            )
        };

        // スキル設定
        battleChar.availableSkills = new List<BattleSkillData>();
        if (masterData.DefaultSkillId > 0)
        {
            battleChar.availableSkills.Add(new BattleSkillData
            {
                skillId = masterData.DefaultSkillId,
                skillName = "デフォルトスキル",
                currentCoolTime = 0,
                maxCoolTime = 0,
                isUsable = true
            });
        }

        battleChar.statusEffects = new List<StatusEffectData>();
        return battleChar;
    }

    /// <summary>
    /// 既存MonsterMasterDataからBattleCharacterDataを作成
    /// </summary>
    public static BattleCharacterData CreateFromMonsterMaster(MonsterMasterData masterData)
    {
        var battleChar = new BattleCharacterData
        {
            characterId = $"monster_{masterData.monsterId}",
            characterName = masterData.monsterName,
            isPlayer = false,
            isAlive = true,
            masterDataId = masterData.monsterId,

            // ステータス設定
            maxHp = masterData.hp,
            currentHp = masterData.hp,
            offense = masterData.offense,
            defense = masterData.defense,
            speed = masterData.speed,
            criticalRate = masterData.criticalRate,
            criticalDamageRate = masterData.criticalDamageRate,

            // 属性攻撃力
            fireOffence = masterData.fireOffence,
            waterOffence = masterData.waterOffence,
            windOffence = masterData.windOffence,
            earthOffence = masterData.earthOffence,

            // モンスター属性
            characterAttribute = masterData.attributeType
        };

        // スキル設定
        battleChar.availableSkills = new List<BattleSkillData>();
        var monsterSkills = masterData.GetUsedSkills();
        foreach (var skillId in monsterSkills)
        {
            battleChar.availableSkills.Add(new BattleSkillData
            {
                skillId = skillId,
                skillName = $"モンスタースキル_{skillId}",
                currentCoolTime = 0,
                maxCoolTime = 3, // デフォルトCT
                isUsable = true
            });
        }

        battleChar.statusEffects = new List<StatusEffectData>();
        return battleChar;
    }

    /// <summary>
    /// HP割合を取得
    /// </summary>
    public float GetHpRatio()
    {
        return maxHp > 0 ? (float)currentHp / maxHp : 0f;
    }

    /// <summary>
    /// HP満タンかチェック
    /// </summary>
    public bool IsHpFull()
    {
        return currentHp >= maxHp;
    }

    /// <summary>
    /// 死亡判定
    /// </summary>
    public bool IsDead()
    {
        return currentHp <= 0 || !isAlive;
    }

    /// <summary>
    /// 行動可能かチェック
    /// </summary>
    public bool CanAct()
    {
        if (IsDead()) return false;

        // スタン等の行動阻害効果をチェック
        foreach (var effect in statusEffects)
        {
            if (effect.preventAction) return false;
        }

        return true;
    }

    /// <summary>
    /// 最も高い属性攻撃力を取得
    /// </summary>
    public int GetHighestElementalAttack()
    {
        return Mathf.Max(fireOffence, waterOffence, windOffence, earthOffence);
    }

    /// <summary>
    /// 最も高い属性攻撃力の属性タイプを取得
    /// </summary>
    public AttributeType GetHighestElementalAttackType()
    {
        int maxAttack = GetHighestElementalAttack();
        if (maxAttack == 0) return AttributeType.None;

        if (fireOffence == maxAttack) return AttributeType.Fire;
        if (waterOffence == maxAttack) return AttributeType.Water;
        if (windOffence == maxAttack) return AttributeType.Wind;
        if (earthOffence == maxAttack) return AttributeType.Earth;

        return AttributeType.None;
    }

    /// <summary>
    /// 属性攻撃力から主要属性を判定
    /// </summary>
    private static AttributeType GetDominantAttribute(int fire, int water, int wind, int earth)
    {
        int maxAttack = Mathf.Max(fire, water, wind, earth);
        if (maxAttack == 0) return AttributeType.None;

        if (fire == maxAttack) return AttributeType.Fire;
        if (water == maxAttack) return AttributeType.Water;
        if (wind == maxAttack) return AttributeType.Wind;
        if (earth == maxAttack) return AttributeType.Earth;

        return AttributeType.None;
    }
}