using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ユーザーデータ操作のユーティリティクラス
/// </summary>
public static class UserDataUtility
{
    /// <summary>
    /// 新規ユーザーデータを作成（初期装備・アイテム・スキル付き）
    /// </summary>
    public static UserSaveData CreateNewUserData(string playerName = "新規プレイヤー")
    {
        UserSaveData userData = new UserSaveData
        {
            playerName = playerName
        };

        // 初期装備を追加（装備ID 1-3の基本装備）
        AddInitialEquipments(userData);

        // 初期アイテムを追加
        AddInitialItems(userData);

        // 初期スキルを追加
        AddInitialSkills(userData);

        return userData;
    }

    /// <summary>
    /// 初期装備を追加
    /// </summary>
    private static void AddInitialEquipments(UserSaveData userData)
    {
        // 初心者の剣（装備ID:1）
        var beginnerSword = new UserEquipmentData
        {
            equipmentMasterId = 1,
            currentEnhancedValue = 0,
            currentEnhanceStamina = 100,
            currentAttributeType = AttributeType.None
        };
        userData.AddEquipment(beginnerSword);

        // 初心者の鎧（装備ID:2）
        var beginnerArmor = new UserEquipmentData
        {
            equipmentMasterId = 2,
            currentEnhancedValue = 0,
            currentEnhanceStamina = 100,
            currentAttributeType = AttributeType.None
        };
        userData.AddEquipment(beginnerArmor);

        // 古びた首飾り（装備ID:3）
        var oldNecklace = new UserEquipmentData
        {
            equipmentMasterId = 3,
            currentEnhancedValue = 0,
            currentEnhanceStamina = 100,
            currentAttributeType = AttributeType.Fire  // 火属性攻撃+5
        };
        userData.AddEquipment(oldNecklace);
    }

    /// <summary>
    /// 初期アイテムを追加
    /// </summary>
    private static void AddInitialItems(UserSaveData userData)
    {
        // 基本的な強化石を数個追加（実際のマスターデータIDに合わせて調整）
        var enhanceStone = new UserItemData
        {
            itemType = ItemType.EnhanceItem,
            itemMasterId = 1,  // 低級強化石のID（仮）
            quantity = 5,
            maxStackQuantity = 99
        };
        userData.AddItem(enhanceStone);

        // 補助材料を追加
        var supportItem = new UserItemData
        {
            itemType = ItemType.SupportItem,
            itemMasterId = 1,  // 基本補助材料のID（仮）
            quantity = 3,
            maxStackQuantity = 50
        };
        userData.AddItem(supportItem);
    }

    /// <summary>
    /// 初期スキルを追加
    /// </summary>
    private static void AddInitialSkills(UserSaveData userData)
    {
        // 基本攻撃スキル（スキルID:1）
        var basicAttackSkill = new UserSkillData(skillMasterId: 1);
        userData.AddSkill(basicAttackSkill);

        // 基本防御スキル（スキルID:2）
        var basicDefenseSkill = new UserSkillData(skillMasterId: 2);
        userData.AddSkill(basicDefenseSkill);
    }

    /// <summary>
    /// 装備をレアリティでフィルタリング
    /// </summary>
    public static List<UserEquipmentData> FilterEquipmentsByRarity(List<UserEquipmentData> equipments, RarityType rarity, Dictionary<int, EquipmentMasterData> masterDataDict)
    {
        return equipments.Where(eq =>
            masterDataDict.ContainsKey(eq.equipmentMasterId) &&
            masterDataDict[eq.equipmentMasterId].rarity == rarity
        ).ToList();
    }

    /// <summary>
    /// 装備を装備タイプでフィルタリング
    /// </summary>
    public static List<UserEquipmentData> FilterEquipmentsByType(List<UserEquipmentData> equipments, EquipmentType equipmentType, Dictionary<int, EquipmentMasterData> masterDataDict)
    {
        return equipments.Where(eq =>
            masterDataDict.ContainsKey(eq.equipmentMasterId) &&
            masterDataDict[eq.equipmentMasterId].equipmentType == equipmentType
        ).ToList();
    }

    /// <summary>
    /// 装備を強化値でソート
    /// </summary>
    public static List<UserEquipmentData> SortEquipmentsByEnhancement(List<UserEquipmentData> equipments, bool descending = true)
    {
        return descending
            ? equipments.OrderByDescending(eq => eq.currentEnhancedValue).ToList()
            : equipments.OrderBy(eq => eq.currentEnhancedValue).ToList();
    }

    /// <summary>
    /// 装備を取得日でソート
    /// </summary>
    public static List<UserEquipmentData> SortEquipmentsByAcquiredDate(List<UserEquipmentData> equipments, bool descending = true)
    {
        return descending
            ? equipments.OrderByDescending(eq => eq.acquiredDate).ToList()
            : equipments.OrderBy(eq => eq.acquiredDate).ToList();
    }

    /// <summary>
    /// アイテムをタイプでフィルタリング
    /// </summary>
    public static List<UserItemData> FilterItemsByType(List<UserItemData> items, ItemType itemType)
    {
        return items.Where(item => item.itemType == itemType).ToList();
    }

    /// <summary>
    /// アイテムをレアリティでフィルタリング
    /// </summary>
    public static List<UserItemData> FilterItemsByRarity(List<UserItemData> items, RarityType rarity, Dictionary<int, EnhanceItemMasterData> enhanceItemDict, Dictionary<int, SupportItemMasterData> supportItemDict)
    {
        return items.Where(item =>
        {
            if (item.itemType == ItemType.EnhanceItem && enhanceItemDict.ContainsKey(item.itemMasterId))
                return enhanceItemDict[item.itemMasterId].rarity == rarity;
            else if (item.itemType == ItemType.SupportItem && supportItemDict.ContainsKey(item.itemMasterId))
                return supportItemDict[item.itemMasterId].rarity == rarity;
            return false;
        }).ToList();
    }

    /// <summary>
    /// アイテムを数量でソート
    /// </summary>
    public static List<UserItemData> SortItemsByQuantity(List<UserItemData> items, bool descending = true)
    {
        return descending
            ? items.OrderByDescending(item => item.quantity).ToList()
            : items.OrderBy(item => item.quantity).ToList();
    }

    /// <summary>
    /// 新規取得アイテムのみを取得
    /// </summary>
    public static List<UserItemData> GetNewItems(List<UserItemData> items)
    {
        return items.Where(item => item.isNew).ToList();
    }

    /// <summary>
    /// スキルをレアリティでフィルタリング
    /// </summary>
    public static List<UserSkillData> FilterSkillsByRarity(List<UserSkillData> skills, RarityType rarity, Dictionary<int, SkillMasterData> skillMasterDict)
    {
        return skills.Where(skill =>
            skillMasterDict.ContainsKey(skill.skillMasterId) &&
            skillMasterDict[skill.skillMasterId].rarity == rarity
        ).ToList();
    }

    /// <summary>
    /// スキルを属性でフィルタリング
    /// </summary>
    public static List<UserSkillData> FilterSkillsByAttribute(List<UserSkillData> skills, AttributeType attributeType, Dictionary<int, SkillMasterData> skillMasterDict)
    {
        return skills.Where(skill =>
            skillMasterDict.ContainsKey(skill.skillMasterId) &&
            skillMasterDict[skill.skillMasterId].attributeType == attributeType
        ).ToList();
    }

    /// <summary>
    /// スキルをターゲットタイプでフィルタリング
    /// </summary>
    public static List<UserSkillData> FilterSkillsByTargetType(List<UserSkillData> skills, TargetType targetType, Dictionary<int, SkillMasterData> skillMasterDict)
    {
        return skills.Where(skill =>
            skillMasterDict.ContainsKey(skill.skillMasterId) &&
            skillMasterDict[skill.skillMasterId].skillTargetType == targetType
        ).ToList();
    }

    /// <summary>
    /// スキルを取得日でソート
    /// </summary>
    public static List<UserSkillData> SortSkillsByAcquiredDate(List<UserSkillData> skills, bool descending = true)
    {
        return descending
            ? skills.OrderByDescending(skill => skill.acquiredDate).ToList()
            : skills.OrderBy(skill => skill.acquiredDate).ToList();
    }

    /// <summary>
    /// 新規取得スキルのみを取得
    /// </summary>
    public static List<UserSkillData> GetNewSkills(List<UserSkillData> skills)
    {
        return skills.Where(skill => skill.isNew).ToList();
    }

    /// <summary>
    /// 装備可能な装備リストを取得
    /// </summary>
    public static List<UserEquipmentData> GetEquippableItems(List<UserEquipmentData> equipments, EquipmentType equipmentType, Dictionary<int, EquipmentMasterData> masterDataDict)
    {
        return equipments.Where(eq =>
            !eq.isEquipped &&
            masterDataDict.ContainsKey(eq.equipmentMasterId) &&
            masterDataDict[eq.equipmentMasterId].equipmentType == equipmentType
        ).ToList();
    }

    /// <summary>
    /// キャラクターの装備中アイテムを取得
    /// </summary>
    public static List<UserEquipmentData> GetEquippedItems(List<UserEquipmentData> equipments, string characterId)
    {
        return equipments.Where(eq => eq.isEquipped && eq.equippedCharacterId == characterId).ToList();
    }

    /// <summary>
    /// 総合戦闘力を計算（簡易版）
    /// </summary>
    public static int CalculateTotalPower(List<UserEquipmentData> equippedItems, Dictionary<int, EquipmentMasterData> masterDataDict)
    {
        int totalPower = 0;

        foreach (var equipment in equippedItems)
        {
            if (masterDataDict.ContainsKey(equipment.equipmentMasterId))
            {
                var masterData = masterDataDict[equipment.equipmentMasterId];
                var totalStats = equipment.CalculateTotalStats(masterData);

                // 簡易的な戦闘力計算
                totalPower += totalStats.hp / 10;
                totalPower += totalStats.offense * 2;
                totalPower += totalStats.defense;
                totalPower += totalStats.speed;
                totalPower += totalStats.criticalRate / 5;
                totalPower += totalStats.criticalDamageRate / 10;

                // 属性攻撃力も追加
                totalPower += totalStats.fireOffence;
                totalPower += totalStats.waterOffence;
                totalPower += totalStats.windOffence;
                totalPower += totalStats.earthOffence;
            }
        }

        return totalPower;
    }

    /// <summary>
    /// 装備の強化可能性をチェック
    /// </summary>
    public static bool CanEnhanceEquipment(UserEquipmentData equipment, UserItemData enhanceItem, Dictionary<int, EquipmentMasterData> masterDataDict)
    {
        if (!masterDataDict.ContainsKey(equipment.equipmentMasterId))
            return false;

        var masterData = masterDataDict[equipment.equipmentMasterId];

        // 装備が強化可能状態かチェック
        if (!equipment.CanEnhance(masterData))
            return false;

        // 強化アイテムが使用可能かチェック
        if (enhanceItem == null || !enhanceItem.CanUse(1))
            return false;

        return true;
    }

    /// <summary>
    /// インベントリの空き容量を計算
    /// </summary>
    public static int CalculateInventorySpace(List<UserEquipmentData> equipments, int maxEquipmentSlots)
    {
        return maxEquipmentSlots - equipments.Count;
    }

    /// <summary>
    /// デバッグ用：ユーザーデータの概要を文字列で取得
    /// </summary>
    public static string GetUserDataSummary(UserSaveData userData)
    {
        var itemSummary = userData.GetItemSummary();

        return $@"=== ユーザーデータ概要 ===
プレイヤー名: {userData.playerName}
レベル: {userData.playerLevel}
ゴールド: {userData.gold:N0}
ジェム: {userData.gems}
装備数: {userData.equipments.Count}
スキル数: {userData.skills.Count}
強化アイテム: {itemSummary.totalEnhanceItems}種類 {itemSummary.totalEnhanceQuantity}個
補助アイテム: {itemSummary.totalSupportItems}種類 {itemSummary.totalSupportQuantity}個
新規アイテム: {itemSummary.newItemCount}個
新規スキル: {userData.skills.Count(s => s.isNew)}個
最終ログイン: {userData.lastLoginDate:yyyy/MM/dd HH:mm}";
    }

    /// <summary>
    /// 指定された条件でアイテムを検索
    /// </summary>
    public static List<T> SearchItems<T>(List<T> items, Func<T, bool> predicate)
    {
        return items.Where(predicate).ToList();
    }

    /// <summary>
    /// ユーザーデータの整合性をチェック
    /// </summary>
    public static List<string> ValidateUserData(UserSaveData userData)
    {
        List<string> errors = new List<string>();

        // 基本データチェック
        if (string.IsNullOrEmpty(userData.playerId))
            errors.Add("プレイヤーIDが設定されていません");

        if (userData.playerLevel < 1)
            errors.Add("プレイヤーレベルが無効です");

        // 装備データチェック
        var duplicateEquipmentIds = userData.equipments
            .GroupBy(eq => eq.userEquipmentId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateId in duplicateEquipmentIds)
            errors.Add($"重複する装備ID: {duplicateId}");

        // アイテムデータチェック
        foreach (var item in userData.items)
        {
            if (item.quantity < 0)
                errors.Add($"アイテム数量が負の値: {item.userItemId}");

            if (item.quantity > item.maxStackQuantity)
                errors.Add($"アイテム数量がスタック上限を超過: {item.userItemId}");
        }

        // スキルデータチェック
        var duplicateSkillIds = userData.skills
            .GroupBy(skill => skill.userSkillId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateId in duplicateSkillIds)
            errors.Add($"重複するスキルID: {duplicateId}");

        // 装備とスキルの関連性チェック
        foreach (var equipment in userData.equipments)
        {
            if (!string.IsNullOrEmpty(equipment.equippedSkillId))
            {
                var skill = userData.GetSkill(equipment.equippedSkillId);
                if (skill == null)
                    errors.Add($"装備 {equipment.userEquipmentId} に存在しないスキル {equipment.equippedSkillId} が装着されています");
            }
        }

        return errors;
    }
}