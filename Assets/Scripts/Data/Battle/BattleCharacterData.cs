using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戦闘中のキャラクター状態データ
/// プレイヤー・モンスター共通で使用
/// データ保持専用クラス（データアクセス統一原則に準拠）
/// </summary>
[System.Serializable]
public class BattleCharacterData
{
    [Header("基本情報")]
    public string characterId;              // キャラクター識別ID
    public string characterName;            // キャラクター名
    public bool isPlayer;                   // プレイヤーキャラクターか
    public bool isAlive;                    // 生存フラグ
    public int characterLevel;              // 📝追加: キャラクターレベル
    public Sprite characterSprite;          // キャラクター画像（追加）

    [Header("個体識別・表示制御")]
    public string instanceId;               // 個体識別ID (例: "monster_1_001")
    public string displayName;              // 表示用名前 (例: "スライムA")
    public int positionIndex;               // 配置位置インデックス (0, 1, 2...)
    public Vector3 battlePosition;          // 戦闘画面での配置座標

    [Header("ビジュアル・アニメーション")]
    public string iconPath;                 // アイコンリソースパス
    public string animationPath;            // アニメーションリソースパス

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

    [Header("参照データ用")]
    public int masterDataId;                // CharacterMasterData or MonsterMasterData のID

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public BattleCharacterData()
    {
        availableSkills = new List<BattleSkillData>();
        statusEffects = new List<StatusEffectData>();
        isAlive = true;
        characterLevel = 1; // 📝追加: デフォルトレベル

        // 📝追加: 新フィールドの初期化
        instanceId = "";
        displayName = "";
        positionIndex = 0;
        battlePosition = Vector3.zero;
        iconPath = "";
        animationPath = "";
    }

    /// <summary>
    /// 修正: CharacterMasterData から BattleCharacterData を作成
    /// 正しいプロパティ名を使用
    /// </summary>
    public static BattleCharacterData CreateFromCharacterMaster(CharacterMasterData masterData, EquipmentTotalStats equipmentStats)
    {
        var battleChar = new BattleCharacterData
        {
            characterId = "player",
            characterName = masterData.CharacterName, // 📝修正: 大文字
            isPlayer = true,
            isAlive = true,
            characterLevel = 1, // デフォルトレベル（後でBattleManagerで設定）
            masterDataId = masterData.CharacterId, // 📝修正: 大文字
            characterSprite = masterData.CharacterIcon, // 📝追加: アイコン設定

            // 📝追加: 個体識別・表示制御（プレイヤー用）
            instanceId = "player_001",
            displayName = masterData.CharacterName,
            positionIndex = 0,
            battlePosition = Vector3.zero,

            // 📝追加: ビジュアル・アニメーション（空で初期化、Manager層で設定）
            iconPath = "",
            animationPath = "",

            // 装備込みステータス設定
            maxHp = masterData.Hp + equipmentStats.hp, // 📝修正: 大文字
            currentHp = masterData.Hp + equipmentStats.hp,
            offense = masterData.Offense + equipmentStats.offense, // 📝修正: 大文字
            defense = masterData.Defense + equipmentStats.defense, // 📝修正: 大文字
            speed = masterData.Speed + equipmentStats.speed, // 📝修正: 大文字
            criticalRate = masterData.CriticalRate + equipmentStats.criticalRate, // 📝修正: 大文字
            criticalDamageRate = masterData.CriticalDamageRate + equipmentStats.criticalDamageRate, // 📝修正: 大文字

            // 属性攻撃力
            fireOffence = masterData.FireOffence + equipmentStats.fireOffence, // 📝修正: 大文字
            waterOffence = masterData.WaterOffence + equipmentStats.waterOffence, // 📝修正: 大文字
            windOffence = masterData.WindOffence + equipmentStats.windOffence, // 📝修正: 大文字
            earthOffence = masterData.EarthOffence + equipmentStats.earthOffence, // 📝修正: 大文字

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

        // デフォルトスキル
        if (masterData.DefaultSkillId > 0) // 📝修正: 大文字
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

        // 使用スキル1
        if (masterData.UsedSkill1 > 0) // 📝修正: 大文字
        {
            battleChar.availableSkills.Add(new BattleSkillData
            {
                skillId = masterData.UsedSkill1,
                skillName = $"スキル1_{masterData.UsedSkill1}",
                currentCoolTime = 0,
                maxCoolTime = 3,
                isUsable = true
            });
        }

        // 使用スキル2
        if (masterData.UsedSkill2 > 0) // 📝修正: 大文字
        {
            battleChar.availableSkills.Add(new BattleSkillData
            {
                skillId = masterData.UsedSkill2,
                skillName = $"スキル2_{masterData.UsedSkill2}",
                currentCoolTime = 0,
                maxCoolTime = 5,
                isUsable = true
            });
        }

        battleChar.statusEffects = new List<StatusEffectData>();
        return battleChar;
    }

    /// <summary>
    /// 📝修正: MonsterMasterData から BattleCharacterData を作成
    /// データ保持のみ、リソース読み込みはManager層で実行
    /// </summary>
    /// <param name="masterData">モンスターマスターデータ</param>
    /// <param name="instanceId">個体識別ID</param>
    /// <param name="displayName">表示用名前</param>
    /// <param name="positionIndex">配置位置インデックス</param>
    /// <param name="sprite">読み込み済みSprite（Manager層で取得）</param>
    /// <returns>作成されたBattleCharacterData</returns>
    public static BattleCharacterData CreateFromMonsterMaster(
        MonsterMasterData masterData,
        string instanceId = null,
        string displayName = null,
        int positionIndex = 0,
        Sprite sprite = null)
    {
        if (masterData == null)
        {
            Debug.LogError("[BattleCharacterData] MonsterMasterDataがnullです");
            return null;
        }

        var battleChar = new BattleCharacterData
        {
            // 基本情報
            characterId = $"monster_{masterData.monsterId}",
            characterName = masterData.monsterName,
            isPlayer = false,
            isAlive = true,
            characterLevel = 1,
            masterDataId = masterData.monsterId,
            characterSprite = sprite, // 📝修正: Manager層で読み込まれたSpriteを設定

            // 📝追加: 個体識別・表示制御
            instanceId = instanceId ?? $"monster_{masterData.monsterId}_{DateTime.Now.Ticks}",
            displayName = displayName ?? masterData.monsterName,
            positionIndex = positionIndex,
            battlePosition = Vector3.zero,

            // 📝追加: ビジュアル・アニメーション（パスのみ保持、読み込みはManager層）
            iconPath = masterData.monsterIconPath,
            animationPath = masterData.monsterAnimationPath,

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
            characterAttribute = masterData.attributeType,

            // 戦闘統計初期化
            damageDealt = 0,
            damageReceived = 0,
            skillsUsed = 0
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
                maxCoolTime = 3,
                isUsable = true
            });
        }

        battleChar.statusEffects = new List<StatusEffectData>();

        Debug.Log($"[BattleCharacterData] モンスター作成完了: {battleChar.displayName} (ID: {battleChar.instanceId})");
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
    /// 📝追加: 個体の表示名を取得（フォールバック付き）
    /// </summary>
    /// <returns>表示用の名前</returns>
    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(displayName) ? displayName : characterName;
    }

    /// <summary>
    /// 📝追加: バトルポジションを設定
    /// </summary>
    /// <param name="position">戦闘配置座標</param>
    public void SetBattlePosition(Vector3 position)
    {
        battlePosition = position;
    }

    /// <summary>
    /// 属性攻撃力から優勢属性を判定
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