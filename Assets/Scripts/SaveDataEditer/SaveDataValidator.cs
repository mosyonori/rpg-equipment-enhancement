using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// セーブデータの検証とデバッグ機能
/// </summary>
public static class SaveDataValidator
{
    /// <summary>
    /// セーブデータの詳細検証
    /// </summary>
    public static SaveDataValidationResult ValidateDetailedSaveData(UserSaveData saveData)
    {
        var result = new SaveDataValidationResult();

        if (saveData == null)
        {
            result.AddError("セーブデータがnullです");
            return result;
        }

        // 基本情報検証
        ValidateBasicInfo(saveData, result);

        // 装備データ検証
        ValidateEquipments(saveData.equipments, result);

        // アイテムデータ検証
        ValidateItems(saveData.items, result);

        return result;
    }

    private static void ValidateBasicInfo(UserSaveData saveData, SaveDataValidationResult result)
    {
        if (string.IsNullOrEmpty(saveData.playerName))
            result.AddWarning("プレイヤー名が空です");

        if (saveData.playerLevel < 1)
            result.AddError("プレイヤーレベルが1未満です");

        if (saveData.gold < 0)
            result.AddWarning("ゴールドが負の値です");
    }

    private static void ValidateEquipments(List<UserEquipmentData> equipments, SaveDataValidationResult result)
    {
        if (equipments == null)
        {
            result.AddError("装備リストがnullです");
            return;
        }

        var masterDataManager = MasterDataManager.Instance;
        if (masterDataManager == null)
        {
            result.AddWarning("MasterDataManagerが見つかりません - マスターデータ検証をスキップ");
            return;
        }

        foreach (var equipment in equipments)
        {
            if (equipment == null)
            {
                result.AddError("装備データがnullです");
                continue;
            }

            // マスターデータ存在確認
            var masterData = masterDataManager.GetEquipmentData(equipment.equipmentMasterId);
            if (masterData == null)
            {
                result.AddError($"装備マスターデータが見つかりません: ID={equipment.equipmentMasterId}");
            }
            else
            {
                result.AddInfo($"装備確認: {masterData.equipmentName} (ID:{equipment.equipmentMasterId})");
            }

            // 強化値範囲確認
            if (masterData != null)
            {
                if (equipment.currentEnhancedValue > masterData.maxEnhancedValue)
                    result.AddWarning($"装備 {masterData.equipmentName} の強化値が上限を超えています");

                if (equipment.currentEnhancedValue < masterData.minEnhancedValue)
                    result.AddWarning($"装備 {masterData.equipmentName} の強化値が下限を下回っています");
            }
        }
    }

    private static void ValidateItems(List<UserItemData> items, SaveDataValidationResult result)
    {
        if (items == null)
        {
            result.AddError("アイテムリストがnullです");
            return;
        }

        var masterDataManager = MasterDataManager.Instance;
        if (masterDataManager == null)
        {
            result.AddWarning("MasterDataManagerが見つかりません - マスターデータ検証をスキップ");
            return;
        }

        foreach (var item in items)
        {
            if (item == null)
            {
                result.AddError("アイテムデータがnullです");
                continue;
            }

            // マスターデータ存在確認
            if (item.itemType == ItemType.EnhanceItem)
            {
                var masterData = masterDataManager.GetEnhanceItemData(item.itemMasterId);
                if (masterData == null)
                {
                    result.AddError($"強化アイテムマスターデータが見つかりません: ID={item.itemMasterId}");
                }
                else
                {
                    result.AddInfo($"強化アイテム確認: {masterData.enhanceItemName} (ID:{item.itemMasterId}) x{item.quantity}");
                }
            }
            else if (item.itemType == ItemType.SupportItem)
            {
                var masterData = masterDataManager.GetSupportItemData(item.itemMasterId);
                if (masterData == null)
                {
                    result.AddError($"補助アイテムマスターデータが見つかりません: ID={item.itemMasterId}");
                }
                else
                {
                    result.AddInfo($"補助アイテム確認: {masterData.supportItemName} (ID:{item.itemMasterId}) x{item.quantity}");
                }
            }

            // 数量確認
            if (item.quantity <= 0)
                result.AddWarning($"アイテム {item.itemType}-{item.itemMasterId} の数量が0以下です");

            if (item.quantity > item.maxStackQuantity)
                result.AddWarning($"アイテム {item.itemType}-{item.itemMasterId} の数量がスタック上限を超えています");
        }
    }

    /// <summary>
    /// マスターデータの利用可能性を確認
    /// </summary>
    public static MasterDataAvailability CheckMasterDataAvailability()
    {
        var availability = new MasterDataAvailability();

        var masterDataManager = MasterDataManager.Instance;
        if (masterDataManager == null)
        {
            availability.isAvailable = false;
            availability.errorMessage = "MasterDataManagerが見つかりません";
            return availability;
        }

        if (!masterDataManager.IsDataLoaded)
        {
            availability.isAvailable = false;
            availability.errorMessage = "マスターデータがロードされていません";
            return availability;
        }

        // 各データの件数確認
        var equipmentData = masterDataManager.GetEquipmentDataList();
        var enhanceItemData = masterDataManager.GetEnhanceItemDataList();
        var supportItemData = masterDataManager.GetSupportItemDataList();

        availability.equipmentCount = equipmentData.Count;
        availability.enhanceItemCount = enhanceItemData.Count;
        availability.supportItemCount = supportItemData.Count;

        availability.isAvailable = equipmentData.Count > 0 && enhanceItemData.Count > 0 && supportItemData.Count > 0;

        if (!availability.isAvailable)
        {
            availability.errorMessage = "一部のマスターデータが空です";
        }

        return availability;
    }

    /// <summary>
    /// 利用可能なマスターデータIDリストを取得
    /// </summary>
    public static AvailableDataIds GetAvailableDataIds()
    {
        var availableIds = new AvailableDataIds();

        var masterDataManager = MasterDataManager.Instance;
        if (masterDataManager == null) return availableIds;

        // 装備ID取得
        var equipments = masterDataManager.GetEquipmentDataList();
        foreach (var equipment in equipments)
        {
            availableIds.equipmentIds.Add(equipment.equipmentId);
        }

        // 強化アイテムID取得
        var enhanceItems = masterDataManager.GetEnhanceItemDataList();
        foreach (var item in enhanceItems)
        {
            availableIds.enhanceItemIds.Add(item.enhanceItemId);
        }

        // 補助アイテムID取得
        var supportItems = masterDataManager.GetSupportItemDataList();
        foreach (var item in supportItems)
        {
            availableIds.supportItemIds.Add(item.supportItemId);
        }

        return availableIds;
    }
}

/// <summary>
/// セーブデータ検証結果
/// </summary>
public class SaveDataValidationResult
{
    public List<string> errors = new List<string>();
    public List<string> warnings = new List<string>();
    public List<string> info = new List<string>();

    public bool HasErrors => errors.Count > 0;
    public bool HasWarnings => warnings.Count > 0;

    public void AddError(string message) => errors.Add(message);
    public void AddWarning(string message) => warnings.Add(message);
    public void AddInfo(string message) => info.Add(message);

    public string GetSummary()
    {
        var summary = $"検証結果: エラー {errors.Count}件, 警告 {warnings.Count}件, 情報 {info.Count}件\n\n";

        if (errors.Count > 0)
        {
            summary += "【エラー】\n";
            foreach (var error in errors)
                summary += $"- {error}\n";
            summary += "\n";
        }

        if (warnings.Count > 0)
        {
            summary += "【警告】\n";
            foreach (var warning in warnings)
                summary += $"- {warning}\n";
            summary += "\n";
        }

        if (info.Count > 0)
        {
            summary += "【情報】\n";
            foreach (var infoItem in info)
                summary += $"- {infoItem}\n";
        }

        return summary;
    }
}

/// <summary>
/// マスターデータ利用可能性
/// </summary>
public class MasterDataAvailability
{
    public bool isAvailable;
    public string errorMessage;
    public int equipmentCount;
    public int enhanceItemCount;
    public int supportItemCount;
}

/// <summary>
/// 利用可能なデータID
/// </summary>
public class AvailableDataIds
{
    public List<int> equipmentIds = new List<int>();
    public List<int> enhanceItemIds = new List<int>();
    public List<int> supportItemIds = new List<int>();
}