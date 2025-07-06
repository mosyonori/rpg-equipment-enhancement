using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 装備サマリーデータクラス
/// ホーム画面で表示する装備情報をまとめたデータクラス
/// 修正版：装備編集画面と同じ戦闘力計算ロジックを使用
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
    /// 修正版：MasterDataManagerを使用した正確な計算
    /// </summary>
    /// <param name="saveData">ユーザーセーブデータ</param>
    /// <returns>装備サマリーデータ</returns>
    public static EquipmentSummaryData CreateFromSaveData(UserSaveData saveData)
    {
        if (saveData?.equipments == null) return new EquipmentSummaryData();

        var summary = new EquipmentSummaryData();

        // MasterDataManagerの準備状況確認
        if (MasterDataManager.Instance == null || !MasterDataManager.Instance.IsDataLoaded)
        {
            Debug.LogWarning("[EquipmentSummaryData] MasterDataManagerが利用できません。簡易計算を使用します。");
            return CreateFromSaveDataFallback(saveData);
        }

        var masterDataDict = MasterDataManager.Instance.GetEquipmentDataDict();
        if (masterDataDict == null)
        {
            Debug.LogWarning("[EquipmentSummaryData] 装備マスターデータが取得できません。簡易計算を使用します。");
            return CreateFromSaveDataFallback(saveData);
        }

        // 装備中のアイテムを取得
        var equippedItems = saveData.equipments.Where(e => e.isEquipped).ToList();

        foreach (var equipment in equippedItems)
        {
            // MasterDataから装備タイプを正確に判定
            if (masterDataDict.ContainsKey(equipment.equipmentMasterId))
            {
                var masterData = masterDataDict[equipment.equipmentMasterId];

                switch (masterData.equipmentType)
                {
                    case EquipmentType.Weapon:
                        summary.equippedWeapon = equipment;
                        summary.hasEquippedWeapon = true;
                        summary.weaponCombatPower = CalculateEquipmentPowerAccurate(equipment, masterData);
                        break;
                    case EquipmentType.Armor:
                        summary.equippedArmor = equipment;
                        summary.hasEquippedArmor = true;
                        summary.armorCombatPower = CalculateEquipmentPowerAccurate(equipment, masterData);
                        break;
                    case EquipmentType.Accessory:
                        summary.equippedAccessory = equipment;
                        summary.hasEquippedAccessory = true;
                        summary.accessoryCombatPower = CalculateEquipmentPowerAccurate(equipment, masterData);
                        break;
                }
            }
        }

        // 総戦闘力計算（装備編集画面と同じロジック）
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
    /// MasterDataManagerが利用できない場合のフォールバック処理
    /// 既存システムとの互換性を保つため
    /// </summary>
    /// <param name="saveData">ユーザーセーブデータ</param>
    /// <returns>装備サマリーデータ（簡易計算版）</returns>
    private static EquipmentSummaryData CreateFromSaveDataFallback(UserSaveData saveData)
    {
        var summary = new EquipmentSummaryData();
        var equippedItems = saveData.equipments.Where(e => e.isEquipped).ToList();

        foreach (var equipment in equippedItems)
        {
            // 既存の簡易判定を使用（後方互換性のため）
            if (IsWeaponFallback(equipment))
            {
                summary.equippedWeapon = equipment;
                summary.hasEquippedWeapon = true;
                summary.weaponCombatPower = CalculateEquipmentPowerFallback(equipment);
            }
            else if (IsArmorFallback(equipment))
            {
                summary.equippedArmor = equipment;
                summary.hasEquippedArmor = true;
                summary.armorCombatPower = CalculateEquipmentPowerFallback(equipment);
            }
            else if (IsAccessoryFallback(equipment))
            {
                summary.equippedAccessory = equipment;
                summary.hasEquippedAccessory = true;
                summary.accessoryCombatPower = CalculateEquipmentPowerFallback(equipment);
            }
        }

        summary.totalCombatPower = summary.weaponCombatPower +
                                   summary.armorCombatPower +
                                   summary.accessoryCombatPower;

        summary.CalculateRecommendations(saveData.equipments);
        summary.CalculateStatistics(saveData.equipments);

        return summary;
    }

    /// <summary>
    /// 装備の戦闘力を正確に計算（装備編集画面と同じロジック）
    /// </summary>
    /// <param name="equipment">装備データ</param>
    /// <param name="masterData">装備マスターデータ</param>
    /// <returns>戦闘力</returns>
    private static int CalculateEquipmentPowerAccurate(UserEquipmentData equipment, EquipmentMasterData masterData)
    {
        if (equipment == null || masterData == null) return 0;

        // UserDataUtility.CalculateTotalPower()と同じ計算式を使用
        var totalStats = equipment.CalculateTotalStats(masterData);

        int equipmentPower = 0;

        // 装備編集画面と同じ戦闘力計算ロジック
        equipmentPower += totalStats.hp / 10;
        equipmentPower += totalStats.offense * 2;
        equipmentPower += totalStats.defense;
        equipmentPower += totalStats.speed;
        equipmentPower += totalStats.criticalRate / 5;
        equipmentPower += totalStats.criticalDamageRate / 10;

        // 属性攻撃力も追加
        equipmentPower += totalStats.fireOffence;
        equipmentPower += totalStats.waterOffence;
        equipmentPower += totalStats.windOffence;
        equipmentPower += totalStats.earthOffence;

        return equipmentPower;
    }

    /// <summary>
    /// 装備タイプ判定：武器（MasterDataManager使用）
    /// </summary>
    /// <param name="equipment">装備データ</param>
    /// <returns>武器の場合true</returns>
    private static bool IsWeapon(UserEquipmentData equipment)
    {
        if (MasterDataManager.Instance?.IsDataLoaded != true) return IsWeaponFallback(equipment);

        var masterData = MasterDataManager.Instance.GetEquipmentData(equipment.equipmentMasterId);
        return masterData?.equipmentType == EquipmentType.Weapon;
    }

    /// <summary>
    /// 装備タイプ判定：防具（MasterDataManager使用）
    /// </summary>
    /// <param name="equipment">装備データ</param>
    /// <returns>防具の場合true</returns>
    private static bool IsArmor(UserEquipmentData equipment)
    {
        if (MasterDataManager.Instance?.IsDataLoaded != true) return IsArmorFallback(equipment);

        var masterData = MasterDataManager.Instance.GetEquipmentData(equipment.equipmentMasterId);
        return masterData?.equipmentType == EquipmentType.Armor;
    }

    /// <summary>
    /// 装備タイプ判定：アクセサリー（MasterDataManager使用）
    /// </summary>
    /// <param name="equipment">装備データ</param>
    /// <returns>アクセサリーの場合true</returns>
    private static bool IsAccessory(UserEquipmentData equipment)
    {
        if (MasterDataManager.Instance?.IsDataLoaded != true) return IsAccessoryFallback(equipment);

        var masterData = MasterDataManager.Instance.GetEquipmentData(equipment.equipmentMasterId);
        return masterData?.equipmentType == EquipmentType.Accessory;
    }

    /// <summary>
    /// フォールバック：装備タイプ判定（武器）
    /// MasterDataManagerが利用できない場合の既存ロジック
    /// </summary>
    private static bool IsWeaponFallback(UserEquipmentData equipment)
    {
        return equipment.equipmentMasterId >= 1 && equipment.equipmentMasterId <= 1000;
    }

    /// <summary>
    /// フォールバック：装備タイプ判定（防具）
    /// </summary>
    private static bool IsArmorFallback(UserEquipmentData equipment)
    {
        return equipment.equipmentMasterId >= 1001 && equipment.equipmentMasterId <= 2000;
    }

    /// <summary>
    /// フォールバック：装備タイプ判定（アクセサリー）
    /// </summary>
    private static bool IsAccessoryFallback(UserEquipmentData equipment)
    {
        return equipment.equipmentMasterId >= 2001 && equipment.equipmentMasterId <= 3000;
    }

    /// <summary>
    /// フォールバック：装備の戦闘力を計算（既存ロジック）
    /// MasterDataManagerが利用できない場合の簡易計算
    /// </summary>
    /// <param name="equipment">装備データ</param>
    /// <returns>戦闘力</returns>
    private static int CalculateEquipmentPowerFallback(UserEquipmentData equipment)
    {
        if (equipment == null) return 0;

        // 既存の簡易計算ロジック（後方互換性のため保持）
        int basePower = equipment.equipmentMasterId * 10;
        int enhanceBonus = equipment.currentEnhancedValue * 50;
        float durabilityRatio = (float)equipment.currentEnhanceStamina / 100f;

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