using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 装備サマリーデータクラス
/// ホーム画面で表示する装備情報をまとめたデータクラス
/// </summary>
[System.Serializable]
public class EquipmentSummaryData
{
    [Header("装備中アイテム")]
    public UserEquipmentData equippedWeapon;
    public UserEquipmentData equippedArmor;
    public UserEquipmentData equippedAccessory;

    [Header("戦闘力情報")]
    public int totalCombatPower;
    public int weaponCombatPower;
    public int armorCombatPower;
    public int accessoryCombatPower;

    [Header("装備状態")]
    public bool hasEquippedWeapon;
    public bool hasEquippedArmor;
    public bool hasEquippedAccessory;

    [Header("推奨・警告")]
    public List<UserEquipmentData> lowDurabilityEquipments;
    public List<UserEquipmentData> enhanceableEquipments;
    public bool hasRecommendedEnhancements;
    public bool hasLowDurabilityWarning;

    [Header("統計情報")]
    public int totalEquipmentCount;
    public int maxEnhanceLevel;
    public float averageEnhanceLevel;

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public EquipmentSummaryData()
    {
        equippedWeapon = null;
        equippedArmor = null;
        equippedAccessory = null;
        totalCombatPower = 0;
        weaponCombatPower = 0;
        armorCombatPower = 0;
        accessoryCombatPower = 0;
        hasEquippedWeapon = false;
        hasEquippedArmor = false;
        hasEquippedAccessory = false;
        lowDurabilityEquipments = new List<UserEquipmentData>();
        enhanceableEquipments = new List<UserEquipmentData>();
        hasRecommendedEnhancements = false;
        hasLowDurabilityWarning = false;
        totalEquipmentCount = 0;
        maxEnhanceLevel = 0;
        averageEnhanceLevel = 0f;
    }

    /// <summary>
    /// UserSaveDataからEquipmentSummaryDataを作成
    /// </summary>
    /// <param name="saveData">ユーザーセーブデータ</param>
    /// <returns>装備サマリーデータ</returns>
    public static EquipmentSummaryData CreateFromSaveData(UserSaveData saveData)
    {
        if (saveData?.equipments == null) return new EquipmentSummaryData();

        var summary = new EquipmentSummaryData();

        // 装備中のアイテムを取得
        var equippedItems = saveData.equipments.Where(e => e.isEquipped).ToList();

        foreach (var equipment in equippedItems)
        {
            // TODO: MasterDataManagerから装備タイプを取得
            // 仮実装として文字列比較で判定
            if (IsWeapon(equipment))
            {
                summary.equippedWeapon = equipment;
                summary.hasEquippedWeapon = true;
                summary.weaponCombatPower = CalculateEquipmentPower(equipment);
            }
            else if (IsArmor(equipment))
            {
                summary.equippedArmor = equipment;
                summary.hasEquippedArmor = true;
                summary.armorCombatPower = CalculateEquipmentPower(equipment);
            }
            else if (IsAccessory(equipment))
            {
                summary.equippedAccessory = equipment;
                summary.hasEquippedAccessory = true;
                summary.accessoryCombatPower = CalculateEquipmentPower(equipment);
            }
        }

        // 総戦闘力計算
        summary.totalCombatPower = summary.weaponCombatPower +
                                   summary.armorCombatPower +
                                   summary.accessoryCombatPower;

        // 推奨・警告情報の計算
        summary.CalculateRecommendations(saveData.equipments);

        // 統計情報の計算
        summary.CalculateStatistics(saveData.equipments);

        return summary;
    }

    /// <summary>
    /// 装備タイプ判定：武器
    /// </summary>
    /// <param name="equipment">装備データ</param>
    /// <returns>武器の場合true</returns>
    private static bool IsWeapon(UserEquipmentData equipment)
    {
        // TODO: MasterDataManagerを使用して正確な判定を実装
        // 仮実装として装備IDの範囲で判定
        return equipment.equipmentMasterId >= 1 && equipment.equipmentMasterId <= 1000;
    }

    /// <summary>
    /// 装備タイプ判定：防具
    /// </summary>
    /// <param name="equipment">装備データ</param>
    /// <returns>防具の場合true</returns>
    private static bool IsArmor(UserEquipmentData equipment)
    {
        // TODO: MasterDataManagerを使用して正確な判定を実装
        return equipment.equipmentMasterId >= 1001 && equipment.equipmentMasterId <= 2000;
    }

    /// <summary>
    /// 装備タイプ判定：アクセサリー
    /// </summary>
    /// <param name="equipment">装備データ</param>
    /// <returns>アクセサリーの場合true</returns>
    private static bool IsAccessory(UserEquipmentData equipment)
    {
        // TODO: MasterDataManagerを使用して正確な判定を実装
        return equipment.equipmentMasterId >= 2001 && equipment.equipmentMasterId <= 3000;
    }

    /// <summary>
    /// 装備の戦闘力を計算
    /// </summary>
    /// <param name="equipment">装備データ</param>
    /// <returns>戦闘力</returns>
    private static int CalculateEquipmentPower(UserEquipmentData equipment)
    {
        if (equipment == null) return 0;

        // TODO: より正確な戦闘力計算ロジックを実装
        // 仮実装として強化値と耐久度を考慮
        int basePower = equipment.equipmentMasterId * 10; // 基礎値
        int enhanceBonus = equipment.currentEnhancedValue * 50; // 強化ボーナス
        float durabilityRatio = (float)equipment.currentEnhanceStamina / 100f; // 仮の最大耐久度100として計算

        return Mathf.RoundToInt((basePower + enhanceBonus) * durabilityRatio);
    }

    /// <summary>
    /// 推奨・警告情報を計算
    /// </summary>
    /// <param name="equipments">全装備リスト</param>
    private void CalculateRecommendations(List<UserEquipmentData> equipments)
    {
        lowDurabilityEquipments = equipments
            .Where(e => e.isEquipped && GetDurabilityRatio(e) < 0.3f)
            .ToList();

        enhanceableEquipments = equipments
            .Where(e => e.isEquipped && e.currentEnhancedValue < 100) // 最大強化値を100と仮定
            .ToList();

        hasLowDurabilityWarning = lowDurabilityEquipments.Count > 0;
        hasRecommendedEnhancements = enhanceableEquipments.Count > 0;
    }

    /// <summary>
    /// 統計情報を計算
    /// </summary>
    /// <param name="equipments">全装備リスト</param>
    private void CalculateStatistics(List<UserEquipmentData> equipments)
    {
        totalEquipmentCount = equipments.Count;

        if (equipments.Count > 0)
        {
            maxEnhanceLevel = equipments.Max(e => e.currentEnhancedValue);
            averageEnhanceLevel = (float)equipments.Average(e => e.currentEnhancedValue);
        }
    }

    /// <summary>
    /// 装備の耐久度比率を取得
    /// </summary>
    /// <param name="equipment">装備データ</param>
    /// <returns>耐久度比率（0.0～1.0）</returns>
    public static float GetDurabilityRatio(UserEquipmentData equipment)
    {
        if (equipment == null || equipment.currentEnhanceStamina <= 0) return 0f;
        // 仮の最大耐久度100として計算
        return Mathf.Clamp01((float)equipment.currentEnhanceStamina / 100f);
    }

    /// <summary>
    /// 指定タイプの装備が装備されているかチェック
    /// </summary>
    /// <param name="equipmentType">装備タイプ</param>
    /// <returns>装備されている場合true</returns>
    public bool IsEquipmentTypeEquipped(string equipmentType)
    {
        return equipmentType?.ToLower() switch
        {
            "weapon" => hasEquippedWeapon,
            "armor" => hasEquippedArmor,
            "accessory" => hasEquippedAccessory,
            _ => false
        };
    }

    /// <summary>
    /// 指定タイプの装備データを取得
    /// </summary>
    /// <param name="equipmentType">装備タイプ</param>
    /// <returns>装備データ</returns>
    public UserEquipmentData GetEquippedItem(string equipmentType)
    {
        return equipmentType?.ToLower() switch
        {
            "weapon" => equippedWeapon,
            "armor" => equippedArmor,
            "accessory" => equippedAccessory,
            _ => null
        };
    }

    /// <summary>
    /// 指定タイプの戦闘力を取得
    /// </summary>
    /// <param name="equipmentType">装備タイプ</param>
    /// <returns>戦闘力</returns>
    public int GetEquipmentTypePower(string equipmentType)
    {
        return equipmentType?.ToLower() switch
        {
            "weapon" => weaponCombatPower,
            "armor" => armorCombatPower,
            "accessory" => accessoryCombatPower,
            _ => 0
        };
    }

    /// <summary>
    /// 装備スロット数を取得
    /// </summary>
    /// <returns>装備中のスロット数</returns>
    public int GetEquippedSlotCount()
    {
        int count = 0;
        if (hasEquippedWeapon) count++;
        if (hasEquippedArmor) count++;
        if (hasEquippedAccessory) count++;
        return count;
    }

    /// <summary>
    /// 空きスロット数を取得
    /// </summary>
    /// <returns>空きスロット数</returns>
    public int GetEmptySlotCount()
    {
        return 3 - GetEquippedSlotCount(); // 3つの装備スロットを想定
    }

    /// <summary>
    /// すべてのスロットが装備されているかチェック
    /// </summary>
    /// <returns>全装備されている場合true</returns>
    public bool IsAllSlotsEquipped()
    {
        return hasEquippedWeapon && hasEquippedArmor && hasEquippedAccessory;
    }

    /// <summary>
    /// 警告が必要な状態かチェック
    /// </summary>
    /// <returns>警告が必要な場合true</returns>
    public bool HasWarnings()
    {
        return hasLowDurabilityWarning || !IsAllSlotsEquipped();
    }

    /// <summary>
    /// 推奨アクションがあるかチェック
    /// </summary>
    /// <returns>推奨アクションがある場合true</returns>
    public bool HasRecommendations()
    {
        return hasRecommendedEnhancements || GetEmptySlotCount() > 0;
    }

    /// <summary>
    /// 装備サマリーの状態文字列を取得
    /// </summary>
    /// <returns>状態文字列</returns>
    public string GetStatusString()
    {
        if (HasWarnings())
        {
            return "要注意";
        }
        else if (HasRecommendations())
        {
            return "改善可能";
        }
        else
        {
            return "良好";
        }
    }

    /// <summary>
    /// デバッグ用文字列表現
    /// </summary>
    /// <returns>デバッグ情報</returns>
    public override string ToString()
    {
        return $"Equipment Summary - Power:{totalCombatPower} " +
               $"Equipped:{GetEquippedSlotCount()}/3 " +
               $"Warnings:{HasWarnings()} Recommendations:{HasRecommendations()}";
    }

    /// <summary>
    /// データの妥当性チェック
    /// </summary>
    /// <returns>データが有効な場合true</returns>
    public bool IsValid()
    {
        return totalCombatPower >= 0 &&
               weaponCombatPower >= 0 &&
               armorCombatPower >= 0 &&
               accessoryCombatPower >= 0 &&
               totalEquipmentCount >= 0 &&
               maxEnhanceLevel >= 0 &&
               averageEnhanceLevel >= 0;
    }
}