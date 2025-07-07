using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// スキル関連のユーティリティ関数
/// </summary>
public static class SkillUtility
{
    /// <summary>
    /// スキル威力を計算
    /// </summary>
    public static float CalculateSkillPower(UserSkillData userSkill, SkillMasterData masterData)
    {
        if (userSkill == null || masterData == null || userSkill.skillMasterId != masterData.skillId)
            return 0f;

        // 基本威力はマスターデータの値をそのまま使用
        return masterData.skillDamageMultiplier;
    }

    /// <summary>
    /// スキル効果発動率を取得
    /// </summary>
    public static int GetSkillEffectChance(UserSkillData userSkill, SkillMasterData masterData, bool isBoss = false)
    {
        if (userSkill == null || masterData == null || userSkill.skillMasterId != masterData.skillId)
            return 0;

        // ボス戦かどうかで発動率を変える
        return isBoss ? masterData.skillEffectChanceBoss : masterData.skillEffectChance;
    }

    /// <summary>
    /// スキルのクールタイムを取得
    /// </summary>
    public static int GetSkillCoolTime(UserSkillData userSkill, SkillMasterData masterData)
    {
        if (userSkill == null || masterData == null || userSkill.skillMasterId != masterData.skillId)
            return 0;

        return masterData.skillMaxCoolTime;
    }

    /// <summary>
    /// スキルのHP消費量を取得
    /// </summary>
    public static int GetSkillHpCost(UserSkillData userSkill, SkillMasterData masterData)
    {
        if (userSkill == null || masterData == null || userSkill.skillMasterId != masterData.skillId)
            return 0;

        return masterData.skillHpCost;
    }

    /// <summary>
    /// スキルのMP消費量を取得
    /// </summary>
    public static int GetSkillMpCost(UserSkillData userSkill, SkillMasterData masterData)
    {
        if (userSkill == null || masterData == null || userSkill.skillMasterId != masterData.skillId)
            return 0;

        return masterData.skillMpCost;
    }

    /// <summary>
    /// スキルが装備可能かチェック
    /// </summary>
    public static bool CanEquipSkill(UserEquipmentData equipment, UserSkillData skill)
    {
        if (equipment == null || skill == null)
            return false;

        // ロックされたスキルは装備不可
        if (skill.isLocked)
            return false;

        // 装備がロックされている場合は装備不可
        if (equipment.isLocked)
            return false;

        // 既に別のスキルが装着されている場合は装備可能（上書きする）
        // その他の制限があれば追加

        return true;
    }

    /// <summary>
    /// スキル効果の説明文を取得
    /// </summary>
    public static string GetSkillEffectDescription(SkillMasterData masterData)
    {
        if (masterData == null)
            return "スキル情報がありません";

        string description = masterData.description;

        // 基本情報を追加
        if (masterData.skillDamageMultiplier > 0)
            description += $"\n威力: {masterData.skillDamageMultiplier:F1}倍";

        if (masterData.skillHpCost > 0)
            description += $"\nHP消費: {masterData.skillHpCost}";

        if (masterData.skillMpCost > 0)
            description += $"\nMP消費: {masterData.skillMpCost}";

        if (masterData.skillMaxCoolTime > 0)
            description += $"\nクールタイム: {masterData.skillMaxCoolTime}ターン";

        // 効果発動率
        if (masterData.skillEffectChance > 0)
        {
            description += $"\n効果発動率: {masterData.skillEffectChance}%";
            if (masterData.skillEffectChanceBoss != masterData.skillEffectChance)
                description += $" (ボス戦: {masterData.skillEffectChanceBoss}%)";
        }

        // 効果継続時間
        if (masterData.skillEffectDuration > 0)
            description += $"\n効果時間: {masterData.skillEffectDuration}ターン";

        return description;
    }

    /// <summary>
    /// スキルの詳細情報を取得
    /// </summary>
    public static string GetSkillDetailInfo(UserSkillData userSkill, SkillMasterData masterData)
    {
        if (userSkill == null || masterData == null)
            return "情報がありません";

        string info = $"【{masterData.skillName}】\n";
        info += $"レアリティ: {GetRarityText(masterData.rarity)}\n";
        info += $"属性: {GetAttributeText(masterData.attributeType)}\n";
        info += $"対象: {GetTargetTypeText(masterData.skillTargetType)}\n";

        // 取得日時
        info += $"取得日: {userSkill.acquiredDate:yyyy/MM/dd}\n";

        // 状態表示
        List<string> statusList = new List<string>();
        if (userSkill.isNew) statusList.Add("NEW");
        if (userSkill.isLocked) statusList.Add("LOCKED");

        if (statusList.Count > 0)
            info += $"状態: {string.Join(", ", statusList)}\n";

        // 効果説明
        info += "\n" + GetSkillEffectDescription(masterData);

        return info;
    }

    /// <summary>
    /// スキルの簡易情報を取得
    /// </summary>
    public static string GetSkillShortInfo(UserSkillData userSkill, SkillMasterData masterData)
    {
        if (userSkill == null || masterData == null)
            return "---";

        string info = masterData.skillName;

        // 状態表示
        if (userSkill.isNew) info += " [NEW]";
        if (userSkill.isLocked) info += " [🔒]";

        return info;
    }

    /// <summary>
    /// 装備とスキルの相性をチェック
    /// </summary>
    public static SkillCompatibility CheckSkillCompatibility(UserEquipmentData equipment, UserSkillData skill,
        EquipmentMasterData equipmentMaster, SkillMasterData skillMaster)
    {
        if (equipment == null || skill == null || equipmentMaster == null || skillMaster == null)
            return SkillCompatibility.Incompatible;

        // 基本的な装備可能チェック
        if (!CanEquipSkill(equipment, skill))
            return SkillCompatibility.Incompatible;

        // 属性の相性チェック
        if (equipmentMaster.GetAttributeType() == skillMaster.attributeType &&
            skillMaster.attributeType != AttributeType.None)
        {
            return SkillCompatibility.Perfect; // 属性が一致
        }

        // レアリティの相性チェック
        if (equipmentMaster.rarity == skillMaster.rarity)
        {
            return SkillCompatibility.Good; // レアリティが一致
        }

        return SkillCompatibility.Normal; // 通常の相性
    }

    /// <summary>
    /// スキル使用の前提条件をチェック
    /// </summary>
    public static SkillUsageResult CanUseSkill(UserSkillData userSkill, SkillMasterData masterData,
        int currentHp, int currentMp, int currentCooldown = 0)
    {
        if (userSkill == null || masterData == null)
            return new SkillUsageResult(false, "スキル情報がありません");

        // HP消費チェック
        if (masterData.skillHpCost > 0 && currentHp < masterData.skillHpCost)
            return new SkillUsageResult(false, $"HPが不足しています（必要: {masterData.skillHpCost}）");

        // MP消費チェック
        if (masterData.skillMpCost > 0 && currentMp < masterData.skillMpCost)
            return new SkillUsageResult(false, $"MPが不足しています（必要: {masterData.skillMpCost}）");

        // クールダウンチェック
        if (currentCooldown > 0)
            return new SkillUsageResult(false, $"クールダウン中です（残り: {currentCooldown}ターン）");

        return new SkillUsageResult(true, "使用可能");
    }

    /// <summary>
    /// スキルリストをソート
    /// </summary>
    public static List<UserSkillData> SortSkills(List<UserSkillData> skills, SkillSortType sortType,
        Dictionary<int, SkillMasterData> masterDataDict, bool descending = true)
    {
        if (skills == null || skills.Count == 0)
            return new List<UserSkillData>();

        return sortType switch
        {
            SkillSortType.Name => SortByName(skills, masterDataDict, descending),
            SkillSortType.Rarity => SortByRarity(skills, masterDataDict, descending),
            SkillSortType.Attribute => SortByAttribute(skills, masterDataDict, descending),
            SkillSortType.AcquiredDate => SortByAcquiredDate(skills, descending),
            SkillSortType.Power => SortByPower(skills, masterDataDict, descending),
            _ => skills.ToList()
        };
    }

    #region プライベートメソッド - ソート関連

    private static List<UserSkillData> SortByName(List<UserSkillData> skills,
        Dictionary<int, SkillMasterData> masterDataDict, bool descending)
    {
        var sorted = skills.OrderBy(skill =>
        {
            if (masterDataDict.TryGetValue(skill.skillMasterId, out var masterData))
                return masterData.skillName;
            return "";
        });

        return descending ? sorted.Reverse().ToList() : sorted.ToList();
    }

    private static List<UserSkillData> SortByRarity(List<UserSkillData> skills,
        Dictionary<int, SkillMasterData> masterDataDict, bool descending)
    {
        var sorted = skills.OrderBy(skill =>
        {
            if (masterDataDict.TryGetValue(skill.skillMasterId, out var masterData))
                return (int)masterData.rarity;
            return 0;
        });

        return descending ? sorted.Reverse().ToList() : sorted.ToList();
    }

    private static List<UserSkillData> SortByAttribute(List<UserSkillData> skills,
        Dictionary<int, SkillMasterData> masterDataDict, bool descending)
    {
        var sorted = skills.OrderBy(skill =>
        {
            if (masterDataDict.TryGetValue(skill.skillMasterId, out var masterData))
                return (int)masterData.attributeType;
            return 0;
        });

        return descending ? sorted.Reverse().ToList() : sorted.ToList();
    }

    private static List<UserSkillData> SortByAcquiredDate(List<UserSkillData> skills, bool descending)
    {
        var sorted = skills.OrderBy(skill => skill.acquiredDate);
        return descending ? sorted.Reverse().ToList() : sorted.ToList();
    }

    private static List<UserSkillData> SortByPower(List<UserSkillData> skills,
        Dictionary<int, SkillMasterData> masterDataDict, bool descending)
    {
        var sorted = skills.OrderBy(skill =>
        {
            if (masterDataDict.TryGetValue(skill.skillMasterId, out var masterData))
                return masterData.skillDamageMultiplier;
            return 0f;
        });

        return descending ? sorted.Reverse().ToList() : sorted.ToList();
    }

    #endregion

    #region プライベートメソッド - 表示用テキスト

    private static string GetRarityText(RarityType rarity)
    {
        return rarity switch
        {
            RarityType.Common => "コモン",
            RarityType.Rare => "レア",
            RarityType.Epic => "エピック",
            RarityType.Legendary => "レジェンダリー",
            _ => "不明"
        };
    }

    private static string GetAttributeText(AttributeType attribute)
    {
        return attribute switch
        {
            AttributeType.Fire => "火",
            AttributeType.Water => "水",
            AttributeType.Wind => "風",
            AttributeType.Earth => "土",
            AttributeType.None => "無",
            _ => "不明"
        };
    }

    private static string GetTargetTypeText(TargetType targetType)
    {
        return targetType switch
        {
            TargetType.Self => "自分",
            TargetType.EnemySingle => "敵単体",
            TargetType.EnemyAll => "敵全体",
            TargetType.AllySingle => "味方単体",
            TargetType.AllyAll => "味方全体",
            TargetType.Random => "ランダム",
            _ => "不明"
        };
    }

    #endregion
}

/// <summary>
/// スキルの相性
/// </summary>
public enum SkillCompatibility
{
    Incompatible,   // 装備不可
    Normal,         // 通常
    Good,           // 良い
    Perfect         // 最適
}

/// <summary>
/// スキル使用結果
/// </summary>
public struct SkillUsageResult
{
    public bool canUse;
    public string message;

    public SkillUsageResult(bool canUse, string message)
    {
        this.canUse = canUse;
        this.message = message;
    }
}

/// <summary>
/// スキルソートタイプ
/// </summary>
public enum SkillSortType
{
    Name,           // 名前順
    Rarity,         // レアリティ順
    Attribute,      // 属性順
    AcquiredDate,   // 取得日順
    Power           // 威力順
}