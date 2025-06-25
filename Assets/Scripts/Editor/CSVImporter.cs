using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class CSVImporter
{
    private const string CSV_FOLDER_PATH = "Assets/CSV/";
    private const string GAME_DATA_FOLDER_PATH = "Assets/GameData/";
    private const string EQUIPMENT_FOLDER_PATH = GAME_DATA_FOLDER_PATH + "Equipment/";
    private const string ENHANCE_ITEM_FOLDER_PATH = GAME_DATA_FOLDER_PATH + "EnhanceItem/";
    private const string SUPPORT_ITEM_FOLDER_PATH = GAME_DATA_FOLDER_PATH + "SupportItem/";

    [MenuItem("Tools/CSV Import/Import All CSV Data")]
    public static void ImportAllCSVData()
    {
        CreateFolders();
        ImportEquipmentData();
        ImportEnhanceItemData();
        ImportSupportItemData();
        AssetDatabase.Refresh();
        Debug.Log("全てのCSVデータのインポートが完了しました。");
    }

    [MenuItem("Tools/CSV Import/Import Equipment Data")]
    public static void ImportEquipmentData()
    {
        string csvPath = CSV_FOLDER_PATH + "m_equipment_data.csv";
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {csvPath}");
            return;
        }

        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2)
        {
            Debug.LogError("CSVファイルが空です。");
            return;
        }

        // ヘッダー行をスキップして処理
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < 29) continue;

            EquipmentMasterData equipment = ScriptableObject.CreateInstance<EquipmentMasterData>();

            // 基本情報
            equipment.equipmentId = ParseInt(values[0]);
            equipment.equipmentName = values[1];
            equipment.equipmentType = ParseEquipmentType(values[2]);
            equipment.rarity = ParseRarityType(values[3]);

            // 強化値設定
            equipment.baseEnhancedValue = ParseInt(values[4]);
            equipment.maxEnhancedValue = ParseInt(values[5]);
            equipment.minEnhancedValue = ParseInt(values[6]);

            // 強化耐久値設定
            equipment.baseEnhanceStamina = ParseInt(values[7]);
            equipment.maxEnhanceStamina = ParseInt(values[8]);
            equipment.minEnhanceStamina = ParseInt(values[9]);

            // 強化成功率
            equipment.baseEnhanceSuccessRate = ParseInt(values[10]);

            // 基本ステータス
            equipment.hp = ParseInt(values[11]);
            equipment.offense = ParseInt(values[12]);
            equipment.defense = ParseInt(values[13]);
            equipment.speed = ParseInt(values[14]);
            equipment.criticalRate = ParseInt(values[15]);
            equipment.criticalDamageRate = ParseInt(values[16]);

            // 属性攻撃力
            equipment.fireOffence = ParseInt(values[17]);
            equipment.waterOffence = ParseInt(values[18]);
            equipment.windOffence = ParseInt(values[19]);
            equipment.earthOffence = ParseInt(values[20]);

            // 解放コンテンツ
            equipment.equipmentUnlockSkillId = ParseInt(values[21]);
            equipment.equipmentUnlockSkillEnhancedValue = ParseInt(values[22]);
            equipment.equipmentUnlockCharacterId = values[23];
            equipment.equipmentUnlockCharacterEnhancedValue = values[24];

            // 表示設定
            equipment.equipmentIconPath = values[25];
            equipment.description = values[26];

            // フラグ
            equipment.completionFlag = ParseBool(values[27]);
            equipment.collectionFlag = ParseBool(values[28]);

            // アセットとして保存
            string assetPath = EQUIPMENT_FOLDER_PATH + $"Equipment_{equipment.equipmentId:D3}_{equipment.equipmentName}.asset";
            AssetDatabase.CreateAsset(equipment, assetPath);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"装備データのインポートが完了しました。({lines.Length - 1}件)");
    }

    [MenuItem("Tools/CSV Import/Import Enhance Item Data")]
    public static void ImportEnhanceItemData()
    {
        string csvPath = CSV_FOLDER_PATH + "m_enhance_item_data.csv";
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {csvPath}");
            return;
        }

        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2) return;

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < 44) continue;

            EnhanceItemMasterData enhanceItem = ScriptableObject.CreateInstance<EnhanceItemMasterData>();

            // 基本情報
            enhanceItem.enhanceItemId = ParseInt(values[0]);
            enhanceItem.enhanceItemName = values[1];
            enhanceItem.attributeType = ParseAttributeType(values[2]);
            enhanceItem.rarity = ParseRarityType(values[3]);

            // スタック設定
            enhanceItem.maxStackValue = ParseInt(values[4]);

            // 強化値変動
            enhanceItem.addEnhancedValue = ParseInt(values[5]);
            enhanceItem.reduceEnhancedValue = ParseInt(values[6]);

            // 強化耐久値変動
            enhanceItem.addEnhanceStamina = ParseInt(values[7]);
            enhanceItem.reduceEnhanceStamina = ParseInt(values[8]);

            // 強化成功率
            enhanceItem.enhanceSuccessRate = ParseInt(values[9]);

            // 武器への効果
            enhanceItem.weaponHp = ParseInt(values[10]);
            enhanceItem.weaponOffense = ParseInt(values[11]);
            enhanceItem.weaponDefense = ParseInt(values[12]);
            enhanceItem.weaponSpeed = ParseInt(values[13]);
            enhanceItem.weaponCriticalRate = ParseInt(values[14]);
            enhanceItem.weaponCriticalDamageRate = ParseInt(values[15]);
            enhanceItem.weaponFireOffence = ParseInt(values[16]);
            enhanceItem.weaponWaterOffence = ParseInt(values[17]);
            enhanceItem.weaponWindOffence = ParseInt(values[18]);
            enhanceItem.weaponEarthOffence = ParseInt(values[19]);

            // 防具への効果
            enhanceItem.armorHp = ParseInt(values[20]);
            enhanceItem.armorOffense = ParseInt(values[21]);
            enhanceItem.armorDefense = ParseInt(values[22]);
            enhanceItem.armorSpeed = ParseInt(values[23]);
            enhanceItem.armorCriticalRate = ParseInt(values[24]);
            enhanceItem.armorCriticalDamageRate = ParseInt(values[25]);
            enhanceItem.armorFireOffence = ParseInt(values[26]);
            enhanceItem.armorWaterOffence = ParseInt(values[27]);
            enhanceItem.armorWindOffence = ParseInt(values[28]);
            enhanceItem.armorEarthOffence = ParseInt(values[29]);

            // アクセサリーへの効果
            enhanceItem.accessoryHp = ParseInt(values[30]);
            enhanceItem.accessoryOffense = ParseInt(values[31]);
            enhanceItem.accessoryDefense = ParseInt(values[32]);
            enhanceItem.accessorySpeed = ParseInt(values[33]);
            enhanceItem.accessoryCriticalRate = ParseInt(values[34]);
            enhanceItem.accessoryCriticalDamageRate = ParseInt(values[35]);
            enhanceItem.accessoryFireOffence = ParseInt(values[36]);
            enhanceItem.accessoryWaterOffence = ParseInt(values[37]);
            enhanceItem.accessoryWindOffence = ParseInt(values[38]);
            enhanceItem.accessoryEarthOffence = ParseInt(values[39]);

            // 表示設定
            enhanceItem.enhanceItemIconPath = values[40];
            enhanceItem.description = values[41];

            // フラグ
            enhanceItem.completionFlag = ParseBool(values[42]);
            enhanceItem.collectionFlag = ParseBool(values[43]);

            // アセットとして保存
            string assetPath = ENHANCE_ITEM_FOLDER_PATH + $"EnhanceItem_{enhanceItem.enhanceItemId:D3}_{enhanceItem.enhanceItemName}.asset";
            AssetDatabase.CreateAsset(enhanceItem, assetPath);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"強化アイテムデータのインポートが完了しました。({lines.Length - 1}件)");
    }

    [MenuItem("Tools/CSV Import/Import Support Item Data")]
    public static void ImportSupportItemData()
    {
        string csvPath = CSV_FOLDER_PATH + "m_support_item_data.csv";
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {csvPath}");
            return;
        }

        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2) return;

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < 28) continue;

            SupportItemMasterData supportItem = ScriptableObject.CreateInstance<SupportItemMasterData>();

            // 基本情報
            supportItem.supportItemId = ParseInt(values[0]);
            supportItem.supportItemName = values[1];
            supportItem.attributeType = ParseAttributeType(values[2]);
            supportItem.rarity = ParseRarityType(values[3]);

            // 使用設定
            supportItem.infiniteUse = ParseBool(values[4]);
            supportItem.maxStackValue = ParseInt(values[5]);

            // 強化値効果
            supportItem.addEnhancedValue = ParseInt(values[6]);
            supportItem.multiplEnhancedValue = ParseInt(values[7]);
            supportItem.reduceEnhancedValue = ParseInt(values[8]);

            // 強化耐久値効果
            supportItem.addEnhanceStamina = ParseInt(values[9]);
            supportItem.reduceEnhanceStamina = ParseInt(values[10]);

            // 強化成功率効果
            supportItem.addEnhanceSuccessRate = ParseInt(values[11]);
            supportItem.reduceEnhanceSuccessRate = ParseInt(values[12]);

            // ステータス効果
            supportItem.multiplStatusUp = ParseInt(values[13]);
            supportItem.hp = ParseInt(values[14]);
            supportItem.offense = ParseInt(values[15]);
            supportItem.defense = ParseInt(values[16]);
            supportItem.speed = ParseInt(values[17]);
            supportItem.criticalRate = ParseInt(values[18]);
            supportItem.criticalDamageRate = ParseInt(values[19]);
            supportItem.fireOffence = ParseInt(values[20]);
            supportItem.waterOffence = ParseInt(values[21]);
            supportItem.windOffence = ParseInt(values[22]);
            supportItem.earthOffence = ParseInt(values[23]);

            // 表示設定
            supportItem.supportItemIconPath = values[24];
            supportItem.description = values[25];

            // フラグ
            supportItem.completionFlag = ParseBool(values[26]);
            supportItem.collectionFlag = ParseBool(values[27]);

            // アセットとして保存
            string assetPath = SUPPORT_ITEM_FOLDER_PATH + $"SupportItem_{supportItem.supportItemId:D3}_{supportItem.supportItemName}.asset";
            AssetDatabase.CreateAsset(supportItem, assetPath);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"補助アイテムデータのインポートが完了しました。({lines.Length - 1}件)");
    }

    private static void CreateFolders()
    {
        if (!AssetDatabase.IsValidFolder(GAME_DATA_FOLDER_PATH))
            AssetDatabase.CreateFolder("Assets", "GameData");

        if (!AssetDatabase.IsValidFolder(EQUIPMENT_FOLDER_PATH))
            AssetDatabase.CreateFolder(GAME_DATA_FOLDER_PATH.TrimEnd('/'), "Equipment");

        if (!AssetDatabase.IsValidFolder(ENHANCE_ITEM_FOLDER_PATH))
            AssetDatabase.CreateFolder(GAME_DATA_FOLDER_PATH.TrimEnd('/'), "EnhanceItem");

        if (!AssetDatabase.IsValidFolder(SUPPORT_ITEM_FOLDER_PATH))
            AssetDatabase.CreateFolder(GAME_DATA_FOLDER_PATH.TrimEnd('/'), "SupportItem");
    }

    private static string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        result.Add(currentField);
        return result.ToArray();
    }

    private static int ParseInt(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        return int.TryParse(value, out int result) ? result : 0;
    }

    private static bool ParseBool(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        return value == "1" || value.ToLower() == "true";
    }

    private static EquipmentType ParseEquipmentType(string value)
    {
        return value?.ToLower() switch
        {
            "weapon" => EquipmentType.Weapon,
            "armor" => EquipmentType.Armor,
            "accessory" => EquipmentType.Accessory,
            _ => EquipmentType.Weapon
        };
    }

    private static RarityType ParseRarityType(string value)
    {
        return value?.ToLower() switch
        {
            "common" => RarityType.Common,
            "rare" => RarityType.Rare,
            "epic" => RarityType.Epic,
            "legendary" => RarityType.Legendary,
            _ => RarityType.Common
        };
    }

    private static AttributeType ParseAttributeType(string value)
    {
        return value?.ToLower() switch
        {
            "fire" => AttributeType.Fire,
            "water" => AttributeType.Water,
            "wind" => AttributeType.Wind,
            "earth" => AttributeType.Earth,
            "none" => AttributeType.None,
            "" => AttributeType.None,
            _ => AttributeType.None
        };
    }
}