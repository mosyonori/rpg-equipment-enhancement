using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class CSVImporter : EditorWindow
{
    // === 1. パス定義 ===
    private static readonly string CHARACTER_CSV_PATH = "Assets/CSV/m_character_data.csv";
    private static readonly string EQUIPMENT_CSV_PATH = "Assets/CSV/m_equipment_data.csv";
    private static readonly string ENHANCE_ITEM_CSV_PATH = "Assets/CSV/m_enhance_item_data.csv";
    private static readonly string SUPPORT_ITEM_CSV_PATH = "Assets/CSV/m_support_item_data.csv";
    private static readonly string SKILL_CSV_PATH = "Assets/CSV/m_skill_data.csv";
    private static readonly string SKILL_EFFECT_CSV_PATH = "Assets/CSV/m_skill_effects_data.csv";

    private static readonly string CHARACTER_OUTPUT_PATH = "Assets/GameData/Character/";
    private static readonly string EQUIPMENT_OUTPUT_PATH = "Assets/GameData/Equipment/";
    private static readonly string ENHANCE_ITEM_OUTPUT_PATH = "Assets/GameData/EnhanceItem/";
    private static readonly string SUPPORT_ITEM_OUTPUT_PATH = "Assets/GameData/SupportItem/";
    private static readonly string SKILL_OUTPUT_PATH = "Assets/GameData/Skill/";
    private static readonly string SKILL_EFFECT_OUTPUT_PATH = "Assets/GameData/SkillEffect/";

    // === 2. メニューアイテム ===
    [MenuItem("Tools/CSV Import/Import All CSV Data")]
    public static void ImportAllCSVData()
    {
        Debug.Log("=== 全CSVデータインポート開始 ===");

        ImportCharacterData();
        ImportEquipmentData();
        ImportEnhanceItemData();
        ImportSupportItemData();
        ImportSkillData();
        ImportSkillEffectData();

        Debug.Log("=== 全CSVデータインポート完了 ===");
    }

    [MenuItem("Tools/CSV Import/Import Character Data")]
    public static void ImportCharacterData()
    {
        ImportCSVData<CharacterMasterData>(CHARACTER_CSV_PATH, CHARACTER_OUTPUT_PATH,
            "Character", ParseCharacterLine, UpdateCharacterAsset);
    }

    [MenuItem("Tools/CSV Import/Import Equipment Data")]
    public static void ImportEquipmentData()
    {
        ImportCSVData<EquipmentMasterData>(EQUIPMENT_CSV_PATH, EQUIPMENT_OUTPUT_PATH,
            "Equipment", ParseEquipmentLine, UpdateEquipmentAsset);
    }

    [MenuItem("Tools/CSV Import/Import Enhance Item Data")]
    public static void ImportEnhanceItemData()
    {
        ImportCSVData<EnhanceItemMasterData>(ENHANCE_ITEM_CSV_PATH, ENHANCE_ITEM_OUTPUT_PATH,
            "EnhanceItem", ParseEnhanceItemLine, UpdateEnhanceItemAsset);
    }

    [MenuItem("Tools/CSV Import/Import Support Item Data")]
    public static void ImportSupportItemData()
    {
        ImportCSVData<SupportItemMasterData>(SUPPORT_ITEM_CSV_PATH, SUPPORT_ITEM_OUTPUT_PATH,
            "SupportItem", ParseSupportItemLine, UpdateSupportItemAsset);
    }

    [MenuItem("Tools/CSV Import/Import Skill Data")]
    public static void ImportSkillData()
    {
        ImportCSVData<SkillMasterData>(SKILL_CSV_PATH, SKILL_OUTPUT_PATH,
            "Skill", ParseSkillLine, UpdateSkillAsset);
    }

    [MenuItem("Tools/CSV Import/Import Skill Effect Data")]
    public static void ImportSkillEffectData()
    {
        ImportCSVData<SkillEffectMasterData>(SKILL_EFFECT_CSV_PATH, SKILL_EFFECT_OUTPUT_PATH,
            "SkillEffect", ParseSkillEffectLine, UpdateSkillEffectAsset);
    }

    // === 3. 汎用インポート処理 ===
    private static void ImportCSVData<T>(string csvPath, string outputPath, string dataTypeName,
        System.Func<string, T> parseFunc, System.Action<T, T> updateFunc) where T : ScriptableObject
    {
        try
        {
            Debug.Log($"=== {dataTypeName}データインポート開始 ===");

            if (!File.Exists(csvPath))
            {
                Debug.LogError($"{dataTypeName} CSV file not found: {csvPath}");
                return;
            }

            // フォルダ作成
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            string csvContent = File.ReadAllText(csvPath);
            string[] lines = csvContent.Split('\n');

            if (lines.Length < 2)
            {
                Debug.LogError($"{dataTypeName} CSV file is empty or has no data");
                return;
            }

            int successCount = 0;
            int errorCount = 0;

            // ヘッダー行をスキップして処理
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    T data = parseFunc(line);
                    if (data != null)
                    {
                        SaveAsset(data, outputPath, GetAssetFileName(data, dataTypeName), updateFunc);
                        successCount++;
                    }
                    else
                    {
                        errorCount++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing {dataTypeName} line {i + 1}: {e.Message}");
                    errorCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"{dataTypeName} data import completed. Success: {successCount}, Errors: {errorCount}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to import {dataTypeName} data: {e.Message}");
        }
    }

    // === 4. アセット保存処理 ===
    private static void SaveAsset<T>(T data, string outputPath, string fileName,
        System.Action<T, T> updateFunc) where T : ScriptableObject
    {
        string fullPath = outputPath + fileName;

        T existingAsset = AssetDatabase.LoadAssetAtPath<T>(fullPath);
        if (existingAsset != null)
        {
            updateFunc(existingAsset, data);
            EditorUtility.SetDirty(existingAsset);
        }
        else
        {
            AssetDatabase.CreateAsset(data, fullPath);
        }
    }

    // === 5. ファイル名生成 ===
    private static string GetAssetFileName(ScriptableObject data, string dataTypeName)
    {
        return dataTypeName switch
        {
            "Character" => $"Character_{((CharacterMasterData)data).CharacterId:000}_{((CharacterMasterData)data).CharacterName}.asset",
            "Equipment" => $"Equipment_{((EquipmentMasterData)data).equipmentId:000}_{((EquipmentMasterData)data).equipmentName}.asset",
            "EnhanceItem" => $"EnhanceItem_{((EnhanceItemMasterData)data).enhanceItemId:000}_{((EnhanceItemMasterData)data).enhanceItemName}.asset",
            "SupportItem" => $"SupportItem_{((SupportItemMasterData)data).supportItemId:000}_{((SupportItemMasterData)data).supportItemName}.asset",
            "Skill" => $"Skill_{((SkillMasterData)data).skillId:000}_{((SkillMasterData)data).skillName}.asset",
            "SkillEffect" => $"SkillEffect_{((SkillEffectMasterData)data).statusEffectId:000}_{((SkillEffectMasterData)data).statusEffectName}.asset",
            _ => "Unknown.asset"
        };
    }

    // === 6. キャラクターデータ解析（既存） ===
    private static CharacterMasterData ParseCharacterLine(string line)
    {
        try
        {
            string[] fields = SplitCSVLine(line);

            if (fields.Length < 22)
            {
                Debug.LogError($"Character CSV line has insufficient fields: {fields.Length}");
                return null;
            }

            CharacterMasterData character = ScriptableObject.CreateInstance<CharacterMasterData>();

            character.SetCharacterId(ParseInt(fields[0]));
            character.SetCharacterName(fields[1]);
            character.SetRarity(ParseRarity(fields[2]));
            character.SetBaseLevel(ParseInt(fields[3]));
            character.SetMaxLevel(ParseInt(fields[4]));
            character.SetHp(ParseInt(fields[5]));
            character.SetOffense(ParseInt(fields[6]));
            character.SetDefense(ParseInt(fields[7]));
            character.SetSpeed(ParseInt(fields[8]));
            character.SetCriticalRate(ParseInt(fields[9]));
            character.SetCriticalDamageRate(ParseInt(fields[10]));
            character.SetFireOffence(ParseInt(fields[11]));
            character.SetWaterOffence(ParseInt(fields[12]));
            character.SetWindOffence(ParseInt(fields[13]));
            character.SetEarthOffence(ParseInt(fields[14]));
            character.SetDefaultSkillId(ParseInt(fields[15]));
            character.SetUsedSkill1(ParseInt(fields[16]));
            character.SetUsedSkill2(ParseInt(fields[17]));
            character.SetCharacterIconPath(fields[18]);
            character.SetCharacterAnimationPath(fields[19]);
            character.SetDescription(fields[20]);
            character.SetCompletionFlag(ParseBool(fields[21]));
            if (fields.Length > 22)
            {
                character.SetCollectionFlag(ParseBool(fields[22]));
            }

            return character;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing character line: {e.Message}\nLine: {line}");
            return null;
        }
    }

    // === 7. 装備データ解析（既存） ===
    private static EquipmentMasterData ParseEquipmentLine(string line)
    {
        try
        {
            string[] fields = SplitCSVLine(line);

            if (fields.Length < 29)
            {
                Debug.LogError($"Equipment CSV line has insufficient fields: {fields.Length}");
                return null;
            }

            EquipmentMasterData equipment = ScriptableObject.CreateInstance<EquipmentMasterData>();

            // 基本情報
            equipment.equipmentId = ParseInt(fields[0]);
            equipment.equipmentName = fields[1];
            equipment.equipmentType = ParseEquipmentType(fields[2]);
            equipment.rarity = ParseRarity(fields[3]);

            // 強化値設定
            equipment.baseEnhancedValue = ParseInt(fields[4]);
            equipment.maxEnhancedValue = ParseInt(fields[5]);
            equipment.minEnhancedValue = ParseInt(fields[6]);

            // 強化耐久値設定
            equipment.baseEnhanceStamina = ParseInt(fields[7]);
            equipment.maxEnhanceStamina = ParseInt(fields[8]);
            equipment.minEnhanceStamina = ParseInt(fields[9]);

            // 強化成功率
            equipment.baseEnhanceSuccessRate = ParseInt(fields[10]);

            // 基本ステータス
            equipment.hp = ParseInt(fields[11]);
            equipment.offense = ParseInt(fields[12]);
            equipment.defense = ParseInt(fields[13]);
            equipment.speed = ParseInt(fields[14]);
            equipment.criticalRate = ParseInt(fields[15]);
            equipment.criticalDamageRate = ParseInt(fields[16]);

            // 属性攻撃力
            equipment.fireOffence = ParseInt(fields[17]);
            equipment.waterOffence = ParseInt(fields[18]);
            equipment.windOffence = ParseInt(fields[19]);
            equipment.earthOffence = ParseInt(fields[20]);

            // 解放コンテンツ
            equipment.equipmentUnlockSkillId = ParseInt(fields[21]);
            equipment.equipmentUnlockSkillEnhancedValue = ParseInt(fields[22]);
            equipment.equipmentUnlockCharacterId = fields[23];
            equipment.equipmentUnlockCharacterEnhancedValue = fields[24];

            // 表示設定
            equipment.equipmentIconPath = fields[25];
            equipment.description = fields[26];

            // フラグ
            equipment.completionFlag = ParseBool(fields[27]);
            equipment.collectionFlag = ParseBool(fields[28]);

            return equipment;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing equipment line: {e.Message}\nLine: {line}");
            return null;
        }
    }

    // === 8. 強化アイテムデータ解析（既存） ===
    private static EnhanceItemMasterData ParseEnhanceItemLine(string line)
    {
        try
        {
            string[] fields = SplitCSVLine(line);

            if (fields.Length < 44)
            {
                Debug.LogError($"EnhanceItem CSV line has insufficient fields: {fields.Length}");
                return null;
            }

            EnhanceItemMasterData enhanceItem = ScriptableObject.CreateInstance<EnhanceItemMasterData>();

            // 基本情報
            enhanceItem.enhanceItemId = ParseInt(fields[0]);
            enhanceItem.enhanceItemName = fields[1];
            enhanceItem.attributeType = ParseAttributeType(fields[2]);
            enhanceItem.rarity = ParseRarity(fields[3]);

            // スタック設定
            enhanceItem.maxStackValue = ParseInt(fields[4]);

            // 強化値変動
            enhanceItem.addEnhancedValue = ParseInt(fields[5]);
            enhanceItem.reduceEnhancedValue = ParseInt(fields[6]);

            // 強化耐久値変動
            enhanceItem.addEnhanceStamina = ParseInt(fields[7]);
            enhanceItem.reduceEnhanceStamina = ParseInt(fields[8]);

            // 強化成功率
            enhanceItem.enhanceSuccessRate = ParseInt(fields[9]);

            // 武器への効果
            enhanceItem.weaponHp = ParseInt(fields[10]);
            enhanceItem.weaponOffense = ParseInt(fields[11]);
            enhanceItem.weaponDefense = ParseInt(fields[12]);
            enhanceItem.weaponSpeed = ParseInt(fields[13]);
            enhanceItem.weaponCriticalRate = ParseInt(fields[14]);
            enhanceItem.weaponCriticalDamageRate = ParseInt(fields[15]);
            enhanceItem.weaponFireOffence = ParseInt(fields[16]);
            enhanceItem.weaponWaterOffence = ParseInt(fields[17]);
            enhanceItem.weaponWindOffence = ParseInt(fields[18]);
            enhanceItem.weaponEarthOffence = ParseInt(fields[19]);

            // 防具への効果
            enhanceItem.armorHp = ParseInt(fields[20]);
            enhanceItem.armorOffense = ParseInt(fields[21]);
            enhanceItem.armorDefense = ParseInt(fields[22]);
            enhanceItem.armorSpeed = ParseInt(fields[23]);
            enhanceItem.armorCriticalRate = ParseInt(fields[24]);
            enhanceItem.armorCriticalDamageRate = ParseInt(fields[25]);
            enhanceItem.armorFireOffence = ParseInt(fields[26]);
            enhanceItem.armorWaterOffence = ParseInt(fields[27]);
            enhanceItem.armorWindOffence = ParseInt(fields[28]);
            enhanceItem.armorEarthOffence = ParseInt(fields[29]);

            // アクセサリーへの効果
            enhanceItem.accessoryHp = ParseInt(fields[30]);
            enhanceItem.accessoryOffense = ParseInt(fields[31]);
            enhanceItem.accessoryDefense = ParseInt(fields[32]);
            enhanceItem.accessorySpeed = ParseInt(fields[33]);
            enhanceItem.accessoryCriticalRate = ParseInt(fields[34]);
            enhanceItem.accessoryCriticalDamageRate = ParseInt(fields[35]);
            enhanceItem.accessoryFireOffence = ParseInt(fields[36]);
            enhanceItem.accessoryWaterOffence = ParseInt(fields[37]);
            enhanceItem.accessoryWindOffence = ParseInt(fields[38]);
            enhanceItem.accessoryEarthOffence = ParseInt(fields[39]);

            // 表示設定
            enhanceItem.enhanceItemIconPath = fields[40];
            enhanceItem.description = fields[41];

            // フラグ
            enhanceItem.completionFlag = ParseBool(fields[42]);
            enhanceItem.collectionFlag = ParseBool(fields[43]);

            return enhanceItem;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing enhance item line: {e.Message}\nLine: {line}");
            return null;
        }
    }

    // === 9. 補助料データ解析（既存） ===
    private static SupportItemMasterData ParseSupportItemLine(string line)
    {
        try
        {
            string[] fields = SplitCSVLine(line);

            if (fields.Length < 28)
            {
                Debug.LogError($"SupportItem CSV line has insufficient fields: {fields.Length}");
                return null;
            }

            SupportItemMasterData supportItem = ScriptableObject.CreateInstance<SupportItemMasterData>();

            // 基本情報
            supportItem.supportItemId = ParseInt(fields[0]);
            supportItem.supportItemName = fields[1];
            supportItem.attributeType = ParseAttributeType(fields[2]);
            supportItem.rarity = ParseRarity(fields[3]);

            // 使用設定
            supportItem.infiniteUse = ParseBool(fields[4]);
            supportItem.maxStackValue = ParseInt(fields[5]);

            // 強化値効果
            supportItem.addEnhancedValue = ParseInt(fields[6]);
            supportItem.multiplEnhancedValue = ParseInt(fields[7]);
            supportItem.reduceEnhancedValue = ParseInt(fields[8]);

            // 強化耐久値効果
            supportItem.addEnhanceStamina = ParseInt(fields[9]);
            supportItem.reduceEnhanceStamina = ParseInt(fields[10]);

            // 強化成功率効果
            supportItem.addEnhanceSuccessRate = ParseInt(fields[11]);
            supportItem.reduceEnhanceSuccessRate = ParseInt(fields[12]);

            // ステータス効果
            supportItem.multiplStatusUp = ParseInt(fields[13]);
            supportItem.hp = ParseInt(fields[14]);
            supportItem.offense = ParseInt(fields[15]);
            supportItem.defense = ParseInt(fields[16]);
            supportItem.speed = ParseInt(fields[17]);
            supportItem.criticalRate = ParseInt(fields[18]);
            supportItem.criticalDamageRate = ParseInt(fields[19]);
            supportItem.fireOffence = ParseInt(fields[20]);
            supportItem.waterOffence = ParseInt(fields[21]);
            supportItem.windOffence = ParseInt(fields[22]);
            supportItem.earthOffence = ParseInt(fields[23]);

            // 表示設定
            supportItem.supportItemIconPath = fields[24];
            supportItem.description = fields[25];

            // フラグ
            supportItem.completionFlag = ParseBool(fields[26]);
            supportItem.collectionFlag = ParseBool(fields[27]);

            return supportItem;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing support item line: {e.Message}\nLine: {line}");
            return null;
        }
    }

    // === 10. スキルデータ解析（新規追加） ===
    private static SkillMasterData ParseSkillLine(string line)
    {
        try
        {
            string[] fields = SplitCSVLine(line);

            if (fields.Length < 19)
            {
                Debug.LogError($"Skill CSV line has insufficient fields: {fields.Length}");
                return null;
            }

            SkillMasterData skill = ScriptableObject.CreateInstance<SkillMasterData>();

            // 基本情報
            skill.skillId = ParseInt(fields[0]);
            skill.skillName = fields[1];
            skill.attributeType = ParseAttributeType(fields[2]);
            skill.rarity = ParseRarity(fields[3]);

            // スキル効果
            skill.skillDamageMultiplier = ParseFloat(fields[4]);
            skill.skillTargetType = ParseTargetType(fields[5]);

            // 使用制限
            skill.skillMaxCoolTime = ParseInt(fields[6]);
            skill.skillHpCost = ParseInt(fields[7]);
            skill.skillMpCost = ParseInt(fields[8]);

            // 状態効果
            skill.skillEffect = fields[9];
            skill.skillEffectChance = ParseInt(fields[10]);
            skill.skillEffectChanceBoss = ParseInt(fields[11]);
            skill.skillEffectDuration = ParseInt(fields[12]);

            // 表示設定
            skill.skillIconPath = fields[13];
            skill.skillAnimationPath = fields[14];
            skill.skillSoundPath = fields[15];
            skill.description = fields[16];

            // フラグ
            skill.completionFlag = ParseBool(fields[17]);
            skill.collectionFlag = ParseBool(fields[18]);

            return skill;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing skill line: {e.Message}\nLine: {line}");
            return null;
        }
    }

    // === 11. スキル効果データ解析（新規追加） ===
    private static SkillEffectMasterData ParseSkillEffectLine(string line)
    {
        try
        {
            string[] fields = SplitCSVLine(line);

            if (fields.Length < 20)
            {
                Debug.LogError($"SkillEffect CSV line has insufficient fields: {fields.Length}");
                return null;
            }

            SkillEffectMasterData skillEffect = ScriptableObject.CreateInstance<SkillEffectMasterData>();

            // 基本情報
            skillEffect.statusEffectId = ParseInt(fields[0]);
            skillEffect.statusEffectName = fields[1];
            skillEffect.statusEffectType = ParseStatusEffectType(fields[2]);
            skillEffect.description = fields[3];

            // 効果設定
            skillEffect.effectType = ParseEffectType(fields[4]);
            skillEffect.stackable = ParseBool(fields[5]);

            // ステータス修正値
            skillEffect.offenseModifier = ParseInt(fields[6]);
            skillEffect.defenseModifier = ParseInt(fields[7]);

            // ステータス倍率
            skillEffect.offenseMultiplier = ParseFloat(fields[8]);
            skillEffect.defenseMultiplier = ParseFloat(fields[9]);
            skillEffect.fireOffenseMultiplier = ParseFloat(fields[10]);
            skillEffect.waterOffenseMultiplier = ParseFloat(fields[11]);
            skillEffect.windOffenseMultiplier = ParseFloat(fields[12]);
            skillEffect.earthOffenseMultiplier = ParseFloat(fields[13]);

            // 特殊効果
            skillEffect.preventAction = ParseBool(fields[14]);
            skillEffect.turnStartDamagePercent = ParseInt(fields[15]);
            skillEffect.turnStartHealPercent = ParseInt(fields[16]);

            // 表示設定
            skillEffect.skillEffectIconId = fields[17];
            skillEffect.colorCode = fields[18];
            skillEffect.skillEffectPriority = ParseInt(fields[19]);

            return skillEffect;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing skill effect line: {e.Message}\nLine: {line}");
            return null;
        }
    }

    // === 12. アセット更新処理 ===
    private static void UpdateCharacterAsset(CharacterMasterData existing, CharacterMasterData newData)
    {
        // 既存のキャラクターアセット更新処理（既存コードと同じ）
        existing.SetCharacterName(newData.CharacterName);
        existing.SetRarity(newData.Rarity);
        existing.SetBaseLevel(newData.BaseLevel);
        existing.SetMaxLevel(newData.MaxLevel);
        existing.SetHp(newData.Hp);
        existing.SetOffense(newData.Offense);
        existing.SetDefense(newData.Defense);
        existing.SetSpeed(newData.Speed);
        existing.SetCriticalRate(newData.CriticalRate);
        existing.SetCriticalDamageRate(newData.CriticalDamageRate);
        existing.SetFireOffence(newData.FireOffence);
        existing.SetWaterOffence(newData.WaterOffence);
        existing.SetWindOffence(newData.WindOffence);
        existing.SetEarthOffence(newData.EarthOffence);
        existing.SetDefaultSkillId(newData.DefaultSkillId);
        existing.SetUsedSkill1(newData.UsedSkill1);
        existing.SetUsedSkill2(newData.UsedSkill2);
        existing.SetCharacterIconPath(newData.CharacterIconPath);
        existing.SetCharacterAnimationPath(newData.CharacterAnimationPath);
        existing.SetDescription(newData.Description);
        existing.SetCompletionFlag(newData.CompletionFlag);
        existing.SetCollectionFlag(newData.CollectionFlag);
    }

    private static void UpdateEquipmentAsset(EquipmentMasterData existing, EquipmentMasterData newData)
    {
        // 基本情報
        existing.equipmentName = newData.equipmentName;
        existing.equipmentType = newData.equipmentType;
        existing.rarity = newData.rarity;

        // 強化値設定
        existing.baseEnhancedValue = newData.baseEnhancedValue;
        existing.maxEnhancedValue = newData.maxEnhancedValue;
        existing.minEnhancedValue = newData.minEnhancedValue;

        // 強化耐久値設定
        existing.baseEnhanceStamina = newData.baseEnhanceStamina;
        existing.maxEnhanceStamina = newData.maxEnhanceStamina;
        existing.minEnhanceStamina = newData.minEnhanceStamina;

        // 強化成功率
        existing.baseEnhanceSuccessRate = newData.baseEnhanceSuccessRate;

        // 基本ステータス
        existing.hp = newData.hp;
        existing.offense = newData.offense;
        existing.defense = newData.defense;
        existing.speed = newData.speed;
        existing.criticalRate = newData.criticalRate;
        existing.criticalDamageRate = newData.criticalDamageRate;

        // 属性攻撃力
        existing.fireOffence = newData.fireOffence;
        existing.waterOffence = newData.waterOffence;
        existing.windOffence = newData.windOffence;
        existing.earthOffence = newData.earthOffence;

        // 解放コンテンツ
        existing.equipmentUnlockSkillId = newData.equipmentUnlockSkillId;
        existing.equipmentUnlockSkillEnhancedValue = newData.equipmentUnlockSkillEnhancedValue;
        existing.equipmentUnlockCharacterId = newData.equipmentUnlockCharacterId;
        existing.equipmentUnlockCharacterEnhancedValue = newData.equipmentUnlockCharacterEnhancedValue;

        // 表示設定
        existing.equipmentIconPath = newData.equipmentIconPath;
        existing.description = newData.description;

        // フラグ
        existing.completionFlag = newData.completionFlag;
        existing.collectionFlag = newData.collectionFlag;
    }

    private static void UpdateEnhanceItemAsset(EnhanceItemMasterData existing, EnhanceItemMasterData newData)
    {
        // 基本情報
        existing.enhanceItemName = newData.enhanceItemName;
        existing.attributeType = newData.attributeType;
        existing.rarity = newData.rarity;

        // スタック設定
        existing.maxStackValue = newData.maxStackValue;

        // 強化値変動
        existing.addEnhancedValue = newData.addEnhancedValue;
        existing.reduceEnhancedValue = newData.reduceEnhancedValue;

        // 強化耐久値変動
        existing.addEnhanceStamina = newData.addEnhanceStamina;
        existing.reduceEnhanceStamina = newData.reduceEnhanceStamina;

        // 強化成功率
        existing.enhanceSuccessRate = newData.enhanceSuccessRate;

        // 武器への効果
        existing.weaponHp = newData.weaponHp;
        existing.weaponOffense = newData.weaponOffense;
        existing.weaponDefense = newData.weaponDefense;
        existing.weaponSpeed = newData.weaponSpeed;
        existing.weaponCriticalRate = newData.weaponCriticalRate;
        existing.weaponCriticalDamageRate = newData.weaponCriticalDamageRate;
        existing.weaponFireOffence = newData.weaponFireOffence;
        existing.weaponWaterOffence = newData.weaponWaterOffence;
        existing.weaponWindOffence = newData.weaponWindOffence;
        existing.weaponEarthOffence = newData.weaponEarthOffence;

        // 防具への効果
        existing.armorHp = newData.armorHp;
        existing.armorOffense = newData.armorOffense;
        existing.armorDefense = newData.armorDefense;
        existing.armorSpeed = newData.armorSpeed;
        existing.armorCriticalRate = newData.armorCriticalRate;
        existing.armorCriticalDamageRate = newData.armorCriticalDamageRate;
        existing.armorFireOffence = newData.armorFireOffence;
        existing.armorWaterOffence = newData.armorWaterOffence;
        existing.armorWindOffence = newData.armorWindOffence;
        existing.armorEarthOffence = newData.armorEarthOffence;

        // アクセサリーへの効果
        existing.accessoryHp = newData.accessoryHp;
        existing.accessoryOffense = newData.accessoryOffense;
        existing.accessoryDefense = newData.accessoryDefense;
        existing.accessorySpeed = newData.accessorySpeed;
        existing.accessoryCriticalRate = newData.accessoryCriticalRate;
        existing.accessoryCriticalDamageRate = newData.accessoryCriticalDamageRate;
        existing.accessoryFireOffence = newData.accessoryFireOffence;
        existing.accessoryWaterOffence = newData.accessoryWaterOffence;
        existing.accessoryWindOffence = newData.accessoryWindOffence;
        existing.accessoryEarthOffence = newData.accessoryEarthOffence;

        // 表示設定
        existing.enhanceItemIconPath = newData.enhanceItemIconPath;
        existing.description = newData.description;

        // フラグ
        existing.completionFlag = newData.completionFlag;
        existing.collectionFlag = newData.collectionFlag;
    }

    private static void UpdateSupportItemAsset(SupportItemMasterData existing, SupportItemMasterData newData)
    {
        // 基本情報
        existing.supportItemName = newData.supportItemName;
        existing.attributeType = newData.attributeType;
        existing.rarity = newData.rarity;

        // 使用設定
        existing.infiniteUse = newData.infiniteUse;
        existing.maxStackValue = newData.maxStackValue;

        // 強化値効果
        existing.addEnhancedValue = newData.addEnhancedValue;
        existing.multiplEnhancedValue = newData.multiplEnhancedValue;
        existing.reduceEnhancedValue = newData.reduceEnhancedValue;

        // 強化耐久値効果
        existing.addEnhanceStamina = newData.addEnhanceStamina;
        existing.reduceEnhanceStamina = newData.reduceEnhanceStamina;

        // 強化成功率効果
        existing.addEnhanceSuccessRate = newData.addEnhanceSuccessRate;
        existing.reduceEnhanceSuccessRate = newData.reduceEnhanceSuccessRate;

        // ステータス効果
        existing.multiplStatusUp = newData.multiplStatusUp;
        existing.hp = newData.hp;
        existing.offense = newData.offense;
        existing.defense = newData.defense;
        existing.speed = newData.speed;
        existing.criticalRate = newData.criticalRate;
        existing.criticalDamageRate = newData.criticalDamageRate;
        existing.fireOffence = newData.fireOffence;
        existing.waterOffence = newData.waterOffence;
        existing.windOffence = newData.windOffence;
        existing.earthOffence = newData.earthOffence;

        // 表示設定
        existing.supportItemIconPath = newData.supportItemIconPath;
        existing.description = newData.description;

        // フラグ
        existing.completionFlag = newData.completionFlag;
        existing.collectionFlag = newData.collectionFlag;
    }

    // === 13. スキルアセット更新処理（新規追加） ===
    private static void UpdateSkillAsset(SkillMasterData existing, SkillMasterData newData)
    {
        // 基本情報
        existing.skillName = newData.skillName;
        existing.attributeType = newData.attributeType;
        existing.rarity = newData.rarity;

        // スキル効果
        existing.skillDamageMultiplier = newData.skillDamageMultiplier;
        existing.skillTargetType = newData.skillTargetType;

        // 使用制限
        existing.skillMaxCoolTime = newData.skillMaxCoolTime;
        existing.skillHpCost = newData.skillHpCost;
        existing.skillMpCost = newData.skillMpCost;

        // 状態効果
        existing.skillEffect = newData.skillEffect;
        existing.skillEffectChance = newData.skillEffectChance;
        existing.skillEffectChanceBoss = newData.skillEffectChanceBoss;
        existing.skillEffectDuration = newData.skillEffectDuration;

        // 表示設定
        existing.skillIconPath = newData.skillIconPath;
        existing.skillAnimationPath = newData.skillAnimationPath;
        existing.skillSoundPath = newData.skillSoundPath;
        existing.description = newData.description;

        // フラグ
        existing.completionFlag = newData.completionFlag;
        existing.collectionFlag = newData.collectionFlag;
    }

    private static void UpdateSkillEffectAsset(SkillEffectMasterData existing, SkillEffectMasterData newData)
    {
        // 基本情報
        existing.statusEffectName = newData.statusEffectName;
        existing.statusEffectType = newData.statusEffectType;
        existing.description = newData.description;

        // 効果設定
        existing.effectType = newData.effectType;
        existing.stackable = newData.stackable;

        // ステータス修正値
        existing.offenseModifier = newData.offenseModifier;
        existing.defenseModifier = newData.defenseModifier;

        // ステータス倍率
        existing.offenseMultiplier = newData.offenseMultiplier;
        existing.defenseMultiplier = newData.defenseMultiplier;
        existing.fireOffenseMultiplier = newData.fireOffenseMultiplier;
        existing.waterOffenseMultiplier = newData.waterOffenseMultiplier;
        existing.windOffenseMultiplier = newData.windOffenseMultiplier;
        existing.earthOffenseMultiplier = newData.earthOffenseMultiplier;

        // 特殊効果
        existing.preventAction = newData.preventAction;
        existing.turnStartDamagePercent = newData.turnStartDamagePercent;
        existing.turnStartHealPercent = newData.turnStartHealPercent;

        // 表示設定
        existing.skillEffectIconId = newData.skillEffectIconId;
        existing.colorCode = newData.colorCode;
        existing.skillEffectPriority = newData.skillEffectPriority;
    }

    // === 14. 新規パース用ユーティリティ（新規追加） ===
    private static TargetType ParseTargetType(string targetStr)
    {
        return targetStr.ToLower() switch
        {
            "self" => TargetType.Self,
            "enemy_single" => TargetType.EnemySingle,
            "enemy_all" => TargetType.EnemyAll,
            "ally_single" => TargetType.AllySingle,
            "ally_all" => TargetType.AllyAll,
            "random" => TargetType.Random,
            "" => TargetType.Self,
            _ => TargetType.Self
        };
    }

    private static StatusEffectType ParseStatusEffectType(string effectStr)
    {
        return effectStr.ToLower() switch
        {
            "attack_down" => StatusEffectType.AttackDown,
            "defense_down" => StatusEffectType.DefenseDown,
            "attack_up" => StatusEffectType.AttackUp,
            "defense_up" => StatusEffectType.DefenseUp,
            "stun" => StatusEffectType.Stun,
            "poison" => StatusEffectType.Poison,
            "regen" => StatusEffectType.Regen,
            "" => StatusEffectType.AttackUp,
            _ => StatusEffectType.AttackUp
        };
    }

    private static EffectType ParseEffectType(string effectStr)
    {
        return effectStr.ToLower() switch
        {
            "damage" => EffectType.Damage,
            "heal" => EffectType.Heal,
            "status_modifier" => EffectType.StatusModifier,
            "action_block" => EffectType.ActionBlock,
            "special" => EffectType.Special,
            "" => EffectType.Damage,
            _ => EffectType.Damage
        };
    }

    private static float ParseFloat(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0f;
        if (float.TryParse(value, out float result)) return result;
        Debug.LogWarning($"Failed to parse float: {value}, defaulting to 0");
        return 0f;
    }

    // === 15. 既存のパース用ユーティリティ ===
    private static EquipmentType ParseEquipmentType(string typeStr)
    {
        return typeStr.ToLower() switch
        {
            "weapon" => EquipmentType.Weapon,
            "armor" => EquipmentType.Armor,
            "accessory" => EquipmentType.Accessory,
            _ => EquipmentType.Weapon
        };
    }

    private static AttributeType ParseAttributeType(string attributeStr)
    {
        return attributeStr.ToLower() switch
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

    private static RarityType ParseRarity(string rarityStr)
    {
        return rarityStr.ToLower() switch
        {
            "common" => RarityType.Common,
            "rare" => RarityType.Rare,
            "epic" => RarityType.Epic,
            "legendary" => RarityType.Legendary,
            _ => RarityType.Common
        };
    }

    private static int ParseInt(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        if (int.TryParse(value, out int result)) return result;
        Debug.LogWarning($"Failed to parse int: {value}, defaulting to 0");
        return 0;
    }

    private static bool ParseBool(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        if (value == "1") return true;
        if (bool.TryParse(value, out bool result)) return result;
        Debug.LogWarning($"Failed to parse bool: {value}, defaulting to false");
        return false;
    }

    private static string[] SplitCSVLine(string line)
    {
        List<string> fields = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"' && !inQuotes)
            {
                inQuotes = true;
            }
            else if (c == '"' && inQuotes)
            {
                inQuotes = false;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.Trim());
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        fields.Add(currentField.Trim());
        return fields.ToArray();
    }

    // === 16. 検証メニュー ===
    [MenuItem("Tools/CSV Import/Validate All Data")]
    public static void ValidateAllData()
    {
        Debug.Log("=== 全データ検証開始 ===");
        ValidateCharacterData();
        ValidateEquipmentData();
        ValidateEnhanceItemData();
        ValidateSupportItemData();
        ValidateSkillData();
        ValidateSkillEffectData();
        Debug.Log("=== 全データ検証完了 ===");
    }

    [MenuItem("Tools/CSV Import/Validate Character Data")]
    public static void ValidateCharacterData()
    {
        ValidateAssets<CharacterMasterData>("Character", "Assets/GameData/Character");
    }

    [MenuItem("Tools/CSV Import/Validate Equipment Data")]
    public static void ValidateEquipmentData()
    {
        ValidateAssets<EquipmentMasterData>("Equipment", "Assets/GameData/Equipment");
    }

    [MenuItem("Tools/CSV Import/Validate Enhance Item Data")]
    public static void ValidateEnhanceItemData()
    {
        ValidateAssets<EnhanceItemMasterData>("EnhanceItem", "Assets/GameData/EnhanceItem");
    }

    [MenuItem("Tools/CSV Import/Validate Support Item Data")]
    public static void ValidateSupportItemData()
    {
        ValidateAssets<SupportItemMasterData>("SupportItem", "Assets/GameData/SupportItem");
    }

    [MenuItem("Tools/CSV Import/Validate Skill Data")]
    public static void ValidateSkillData()
    {
        ValidateAssets<SkillMasterData>("Skill", "Assets/GameData/Skill");
    }

    [MenuItem("Tools/CSV Import/Validate Skill Effect Data")]
    public static void ValidateSkillEffectData()
    {
        ValidateAssets<SkillEffectMasterData>("SkillEffect", "Assets/GameData/SkillEffect");
    }

    // === 17. 検証処理（スキル追加） ===
    private static void ValidateAssets<T>(string dataType, string searchPath) where T : ScriptableObject
    {
        try
        {
            Debug.Log($"=== {dataType} Data Validation ===");

            string[] assets = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { searchPath });

            if (assets.Length == 0)
            {
                Debug.LogWarning($"No {dataType} assets found");
                return;
            }

            int validCount = 0;
            int errorCount = 0;

            foreach (string guid in assets)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);

                if (asset == null) continue;

                List<string> errors = ValidateAsset(asset);

                if (errors.Count > 0)
                {
                    Debug.LogError($"{dataType} {asset.name} has errors:\n" + string.Join("\n", errors));
                    errorCount++;
                }
                else
                {
                    validCount++;
                }
            }

            Debug.Log($"{dataType} validation completed. Valid: {validCount}, Errors: {errorCount}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{dataType} validation failed: {e.Message}");
        }
    }

    private static List<string> ValidateAsset<T>(T asset) where T : ScriptableObject
    {
        List<string> errors = new List<string>();

        switch (asset)
        {
            case CharacterMasterData character:
                if (character.CharacterId <= 0) errors.Add("Invalid character ID");
                if (string.IsNullOrEmpty(character.CharacterName)) errors.Add("Character name is empty");
                if (character.BaseLevel <= 0) errors.Add("Invalid base level");
                if (character.MaxLevel < character.BaseLevel) errors.Add("Max level < base level");
                if (character.Hp < 0) errors.Add("HP is negative");
                break;

            case EquipmentMasterData equipment:
                if (equipment.equipmentId <= 0) errors.Add("Invalid equipment ID");
                if (string.IsNullOrEmpty(equipment.equipmentName)) errors.Add("Equipment name is empty");
                if (equipment.maxEnhancedValue < equipment.minEnhancedValue) errors.Add("Max enhanced value < min enhanced value");
                break;

            case EnhanceItemMasterData enhanceItem:
                if (enhanceItem.enhanceItemId <= 0) errors.Add("Invalid enhance item ID");
                if (string.IsNullOrEmpty(enhanceItem.enhanceItemName)) errors.Add("Enhance item name is empty");
                if (enhanceItem.maxStackValue <= 0) errors.Add("Invalid max stack value");
                break;

            case SupportItemMasterData supportItem:
                if (supportItem.supportItemId <= 0) errors.Add("Invalid support item ID");
                if (string.IsNullOrEmpty(supportItem.supportItemName)) errors.Add("Support item name is empty");
                if (supportItem.maxStackValue <= 0) errors.Add("Invalid max stack value");
                break;

            case SkillMasterData skill:
                if (skill.skillId <= 0) errors.Add("Invalid skill ID");
                if (string.IsNullOrEmpty(skill.skillName)) errors.Add("Skill name is empty");
                if (skill.skillDamageMultiplier < 0) errors.Add("Skill damage multiplier is negative");
                if (skill.skillMaxCoolTime < 0) errors.Add("Skill cool time is negative");
                break;

            case SkillEffectMasterData skillEffect:
                if (skillEffect.statusEffectId <= 0) errors.Add("Invalid skill effect ID");
                if (string.IsNullOrEmpty(skillEffect.statusEffectName)) errors.Add("Skill effect name is empty");
                if (skillEffect.offenseMultiplier < 0) errors.Add("Offense multiplier is negative");
                if (skillEffect.defenseMultiplier < 0) errors.Add("Defense multiplier is negative");
                break;
        }

        return errors;
    }
}