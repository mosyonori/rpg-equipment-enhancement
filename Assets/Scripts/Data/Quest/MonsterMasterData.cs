using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// モンスターマスターデータ
/// CSVから読み込んだモンスター情報を管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "MonsterMasterData", menuName = "GameData/Monster")]
public class MonsterMasterData : ScriptableObject
{
    [Header("基本情報")]
    public int monsterId;               // モンスターの一意識別子
    public MonsterType monsterType;     // モンスターの種類（Normal/Boss）
    public AttributeType attributeType; // 属性
    public RarityType rarity;           // レアリティ
    public string monsterName;          // モンスター名

    [Header("ステータス")]
    public int hp;                      // ヒットポイント
    public int offense;                 // 攻撃力
    public int defense;                 // 防御力
    public int speed;                   // 速度
    [Range(0, 100)]
    public int criticalRate;            // クリティカル率（％）
    public int criticalDamageRate;      // クリティカルダメージ率（％）

    [Header("属性攻撃")]
    public int fireOffence;             // 火属性攻撃力
    public int waterOffence;            // 水属性攻撃力
    public int windOffence;             // 風属性攻撃力
    public int earthOffence;            // 土属性攻撃力

    [Header("使用スキル")]
    public int usedSkill1;              // 使用スキル1のID
    public int usedSkill2;              // 使用スキル2のID（-1=使用なし）
    public int usedSkill3;              // 使用スキル3のID（-1=使用なし）

    [Header("UI・演出")]
    public string monsterIconPath;      // モンスターアイコンリソースパス
    public string monsterAnimationPath; // モンスターアニメーションリソースパス
    [TextArea(3, 5)]
    public string description;          // モンスターの説明

    [Header("図鑑フラグ（将来用）")]
    public bool completionFlag;         // 図鑑完了フラグ
    public bool collectionFlag;         // 図鑑収集フラグ

    // === パブリックメソッド ===

    /// <summary>
    /// 使用スキルIDのリストを取得
    /// -1のスキルは除外される
    /// </summary>
    /// <returns>有効なスキルIDのリスト</returns>
    public List<int> GetUsedSkills()
    {
        var skills = new List<int>();

        if (usedSkill1 > 0) skills.Add(usedSkill1);
        if (usedSkill2 > 0) skills.Add(usedSkill2);
        if (usedSkill3 > 0) skills.Add(usedSkill3);

        return skills;
    }

    /// <summary>
    /// ボスモンスターかどうかをチェック
    /// </summary>
    /// <returns>ボスの場合true</returns>
    public bool IsBoss()
    {
        return monsterType == MonsterType.Boss;
    }

    /// <summary>
    /// 無属性モンスターかどうかをチェック
    /// </summary>
    /// <returns>無属性の場合true</returns>
    public bool IsNonAttribute()
    {
        return attributeType == AttributeType.None;
    }

    /// <summary>
    /// 属性攻撃力を持っているかチェック
    /// </summary>
    /// <returns>いずれかの属性攻撃力が0より大きい場合true</returns>
    public bool HasElementalAttack()
    {
        return fireOffence > 0 || waterOffence > 0 || windOffence > 0 || earthOffence > 0;
    }

    /// <summary>
    /// 指定された属性の攻撃力を取得
    /// </summary>
    /// <param name="attributeType">取得したい属性</param>
    /// <returns>指定属性の攻撃力</returns>
    public int GetElementalAttack(AttributeType attributeType)
    {
        return attributeType switch
        {
            AttributeType.Fire => fireOffence,
            AttributeType.Water => waterOffence,
            AttributeType.Wind => windOffence,
            AttributeType.Earth => earthOffence,
            _ => 0
        };
    }

    /// <summary>
    /// 最も高い属性攻撃力を取得
    /// </summary>
    /// <returns>最高の属性攻撃力</returns>
    public int GetHighestElementalAttack()
    {
        return Mathf.Max(fireOffence, waterOffence, windOffence, earthOffence);
    }

    /// <summary>
    /// 最も高い属性攻撃力の属性タイプを取得
    /// </summary>
    /// <returns>最高属性攻撃力の属性タイプ</returns>
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
    /// 総合戦闘力を計算
    /// </summary>
    /// <returns>総合戦闘力</returns>
    public int CalculateTotalPower()
    {
        // 基本ステータスの重み付け合計
        int basePower = hp / 10 + offense * 2 + defense + speed;

        // 属性攻撃力の加算
        int elementalPower = fireOffence + waterOffence + windOffence + earthOffence;

        // クリティカル補正
        float criticalBonus = 1.0f + (criticalRate / 100.0f) * (criticalDamageRate / 100.0f);

        // ボス補正
        float bossMultiplier = IsBoss() ? 1.5f : 1.0f;

        return Mathf.RoundToInt((basePower + elementalPower) * criticalBonus * bossMultiplier);
    }

    /// <summary>
    /// スキル使用可能数を取得
    /// </summary>
    /// <returns>使用可能なスキル数</returns>
    public int GetAvailableSkillCount()
    {
        return GetUsedSkills().Count;
    }

    /// <summary>
    /// 図鑑に登録されているかチェック
    /// </summary>
    /// <returns>図鑑登録済みの場合true</returns>
    public bool IsRegisteredInCollection()
    {
        return completionFlag && collectionFlag;
    }

    /// <summary>
    /// レアリティに応じた基本経験値を取得
    /// </summary>
    /// <returns>撃破時の基本経験値</returns>
    public int GetBaseExperience()
    {
        int baseExp = rarity switch
        {
            RarityType.Common => 10,
            RarityType.Rare => 25,
            RarityType.Epic => 50,
            RarityType.Legendary => 100,
            _ => 5
        };

        // ボス補正
        return IsBoss() ? baseExp * 3 : baseExp;
    }

    /// <summary>
    /// モンスター情報の文字列表現を取得（デバッグ用）
    /// </summary>
    /// <returns>モンスター情報の文字列</returns>
    public override string ToString()
    {
        return $"Monster[{monsterId}] {monsterName} ({monsterType}, {attributeType}, {rarity}) HP:{hp} ATK:{offense} DEF:{defense} SPD:{speed}";
    }

    /// <summary>
    /// データの妥当性をチェック
    /// </summary>
    /// <returns>エラーメッセージのリスト（空の場合は正常）</returns>
    public List<string> ValidateData()
    {
        var errors = new List<string>();

        // 基本情報のチェック
        if (monsterId <= 0)
            errors.Add("monsterId must be positive");

        if (string.IsNullOrEmpty(monsterName))
            errors.Add("monsterName cannot be empty");

        // ステータスのチェック
        if (hp <= 0)
            errors.Add("hp must be positive");

        if (offense < 0)
            errors.Add("offense cannot be negative");

        if (defense < 0)
            errors.Add("defense cannot be negative");

        if (speed < 0)
            errors.Add("speed cannot be negative");

        if (criticalRate < 0 || criticalRate > 100)
            errors.Add("criticalRate must be between 0-100");

        if (criticalDamageRate < 0)
            errors.Add("criticalDamageRate cannot be negative");

        // 属性攻撃力のチェック
        if (fireOffence < 0)
            errors.Add("fireOffence cannot be negative");

        if (waterOffence < 0)
            errors.Add("waterOffence cannot be negative");

        if (windOffence < 0)
            errors.Add("windOffence cannot be negative");

        if (earthOffence < 0)
            errors.Add("earthOffence cannot be negative");

        // スキルのチェック
        if (usedSkill1 <= 0)
            errors.Add("At least usedSkill1 must be specified");

        // スキル重複チェック
        var skills = GetUsedSkills();
        var uniqueSkills = new HashSet<int>(skills);
        if (skills.Count != uniqueSkills.Count)
            errors.Add("Duplicate skills are not allowed");

        return errors;
    }

    /// <summary>
    /// 戦闘での有効性をチェック
    /// </summary>
    /// <returns>戦闘で使用可能な場合true</returns>
    public bool IsValidForBattle()
    {
        return hp > 0 && GetAvailableSkillCount() > 0;
    }

    // NOTE: 属性相性によるダメージ倍率計算は削除
    // → BattleCalculationManager で実装するように変更

#if UNITY_EDITOR
    /// <summary>
    /// エディター用：インスペクターでの表示名をカスタマイズ
    /// </summary>
    [UnityEditor.MenuItem("CONTEXT/MonsterMasterData/Validate Data")]
    private static void ValidateDataContext(UnityEditor.MenuCommand command)
    {
        MonsterMasterData monster = (MonsterMasterData)command.context;
        var errors = monster.ValidateData();

        if (errors.Count == 0)
        {
            Debug.Log($"Monster '{monster.monsterName}' validation passed!");
        }
        else
        {
            Debug.LogError($"Monster '{monster.monsterName}' validation failed:\n" + string.Join("\n", errors));
        }
    }

    [UnityEditor.MenuItem("CONTEXT/MonsterMasterData/Calculate Power")]
    private static void CalculatePowerContext(UnityEditor.MenuCommand command)
    {
        MonsterMasterData monster = (MonsterMasterData)command.context;
        int power = monster.CalculateTotalPower();
        Debug.Log($"Monster '{monster.monsterName}' Total Power: {power}");
    }

    [UnityEditor.MenuItem("CONTEXT/MonsterMasterData/Show Skills")]
    private static void ShowSkillsContext(UnityEditor.MenuCommand command)
    {
        MonsterMasterData monster = (MonsterMasterData)command.context;
        var skills = monster.GetUsedSkills();
        Debug.Log($"Monster '{monster.monsterName}' Skills: [{string.Join(", ", skills)}] ({skills.Count} skills)");
    }
#endif
}