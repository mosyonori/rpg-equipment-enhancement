// 既存のCSVImporter.csに以下の内容を追加してください
// エラーとなっている ImportAllCSVData メソッドを修正します

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class CSVImporter : EditorWindow
{
    // === 1. キャラクターデータ用の定数を追加 ===
    private static readonly string CHARACTER_CSV_PATH = "Assets/CSV/m_character_data.csv";
    private static readonly string CHARACTER_OUTPUT_PATH = "Assets/GameData/Character/";

    // === 2. キャラクターデータインポートメソッド ===
    [MenuItem("Tools/CSV Import/Import Character Data")]
    public static void ImportCharacterData()
    {
        try
        {
            if (!File.Exists(CHARACTER_CSV_PATH))
            {
                Debug.LogError($"Character CSV file not found: {CHARACTER_CSV_PATH}");
                return;
            }

            // フォルダ作成
            if (!Directory.Exists(CHARACTER_OUTPUT_PATH))
            {
                Directory.CreateDirectory(CHARACTER_OUTPUT_PATH);
            }

            string csvContent = File.ReadAllText(CHARACTER_CSV_PATH);
            string[] lines = csvContent.Split('\n');

            if (lines.Length < 2)
            {
                Debug.LogError("Character CSV file is empty or has no data");
                return;
            }

            // ヘッダー行をスキップして処理
            int successCount = 0;
            int errorCount = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    CharacterMasterData characterData = ParseCharacterLine(line);
                    if (characterData != null)
                    {
                        SaveCharacterAsset(characterData);
                        successCount++;
                    }
                    else
                    {
                        errorCount++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing character line {i + 1}: {e.Message}");
                    errorCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Character data import completed. Success: {successCount}, Errors: {errorCount}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to import character data: {e.Message}");
        }
    }

    // === 3. Import All CSV Data メソッドを修正（既存のメソッドがある場合は置き換え） ===
    [MenuItem("Tools/CSV Import/Import All CSV Data")]
    public static void ImportAllCSVData()
    {
        Debug.Log("Starting import of all CSV data...");

        // キャラクターデータのみ追加
        ImportCharacterData();

        Debug.Log("All CSV data import completed!");
    }

    // === 4. キャラクターデータ専用のメソッド群 ===

    /// <summary>
    /// キャラクターCSV行をパース
    /// </summary>
    private static CharacterMasterData ParseCharacterLine(string line)
    {
        try
        {
            string[] fields = SplitCSVLine(line);

            if (fields.Length < 22) // 最低限必要なフィールド数
            {
                Debug.LogError($"Character CSV line has insufficient fields: {fields.Length}");
                return null;
            }

            CharacterMasterData character = ScriptableObject.CreateInstance<CharacterMasterData>();

            // 基本情報
            character.SetCharacterId(ParseInt(fields[0]));
            character.SetCharacterName(fields[1]);
            character.SetRarity(ParseRarity(fields[2]));
            character.SetBaseLevel(ParseInt(fields[3]));
            character.SetMaxLevel(ParseInt(fields[4]));

            // 基本ステータス
            character.SetHp(ParseInt(fields[5]));
            character.SetOffense(ParseInt(fields[6]));
            character.SetDefense(ParseInt(fields[7]));
            character.SetSpeed(ParseInt(fields[8]));
            character.SetCriticalRate(ParseInt(fields[9]));
            character.SetCriticalDamageRate(ParseInt(fields[10]));

            // 属性攻撃
            character.SetFireOffence(ParseInt(fields[11]));
            character.SetWaterOffence(ParseInt(fields[12]));
            character.SetWindOffence(ParseInt(fields[13]));
            character.SetEarthOffence(ParseInt(fields[14]));

            // スキル
            character.SetDefaultSkillId(ParseInt(fields[15]));
            character.SetUsedSkill1(ParseInt(fields[16]));
            character.SetUsedSkill2(ParseInt(fields[17]));

            // UI・表示
            character.SetCharacterIconPath(fields[18]);
            character.SetCharacterAnimationPath(fields[19]);
            character.SetDescription(fields[20]);

            // 収集要素
            character.SetCompletionFlag(ParseBool(fields[21]));
            if (fields.Length > 22) // collection_flagがある場合のみ
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

    /// <summary>
    /// キャラクターアセットを保存
    /// </summary>
    private static void SaveCharacterAsset(CharacterMasterData characterData)
    {
        string fileName = $"Character_{characterData.CharacterId:000}_{characterData.CharacterName}.asset";
        string fullPath = CHARACTER_OUTPUT_PATH + fileName;

        // 既存アセットをチェック
        CharacterMasterData existingAsset = AssetDatabase.LoadAssetAtPath<CharacterMasterData>(fullPath);
        if (existingAsset != null)
        {
            // 既存アセットを更新
            UpdateCharacterAsset(existingAsset, characterData);
            EditorUtility.SetDirty(existingAsset);
        }
        else
        {
            // 新規アセット作成
            AssetDatabase.CreateAsset(characterData, fullPath);
        }
    }

    /// <summary>
    /// 既存キャラクターアセットを更新
    /// </summary>
    private static void UpdateCharacterAsset(CharacterMasterData existing, CharacterMasterData newData)
    {
        // 基本情報
        existing.SetCharacterName(newData.CharacterName);
        existing.SetRarity(newData.Rarity);
        existing.SetBaseLevel(newData.BaseLevel);
        existing.SetMaxLevel(newData.MaxLevel);

        // 基本ステータス
        existing.SetHp(newData.Hp);
        existing.SetOffense(newData.Offense);
        existing.SetDefense(newData.Defense);
        existing.SetSpeed(newData.Speed);
        existing.SetCriticalRate(newData.CriticalRate);
        existing.SetCriticalDamageRate(newData.CriticalDamageRate);

        // 属性攻撃
        existing.SetFireOffence(newData.FireOffence);
        existing.SetWaterOffence(newData.WaterOffence);
        existing.SetWindOffence(newData.WindOffence);
        existing.SetEarthOffence(newData.EarthOffence);

        // スキル
        existing.SetDefaultSkillId(newData.DefaultSkillId);
        existing.SetUsedSkill1(newData.UsedSkill1);
        existing.SetUsedSkill2(newData.UsedSkill2);

        // UI・表示
        existing.SetCharacterIconPath(newData.CharacterIconPath);
        existing.SetCharacterAnimationPath(newData.CharacterAnimationPath);
        existing.SetDescription(newData.Description);

        // 収集要素
        existing.SetCompletionFlag(newData.CompletionFlag);
        existing.SetCollectionFlag(newData.CollectionFlag);
    }

    /// <summary>
    /// レアリティをパース
    /// </summary>
    private static RarityType ParseRarity(string rarityStr)
    {
        switch (rarityStr.ToLower())
        {
            case "common": return RarityType.Common;
            case "rare": return RarityType.Rare;
            case "epic": return RarityType.Epic;
            case "legendary": return RarityType.Legendary;
            default:
                Debug.LogWarning($"Unknown rarity type: {rarityStr}, defaulting to Common");
                return RarityType.Common;
        }
    }

    /// <summary>
    /// 整数をパース
    /// </summary>
    private static int ParseInt(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        if (int.TryParse(value, out int result)) return result;
        Debug.LogWarning($"Failed to parse int: {value}, defaulting to 0");
        return 0;
    }

    /// <summary>
    /// ブール値をパース
    /// </summary>
    private static bool ParseBool(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        if (value == "1") return true;
        if (bool.TryParse(value, out bool result)) return result;
        Debug.LogWarning($"Failed to parse bool: {value}, defaulting to false");
        return false;
    }

    /// <summary>
    /// CSV行を分割（カンマ区切り、クォート対応）
    /// </summary>
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

        // 最後のフィールドを追加
        fields.Add(currentField.Trim());

        return fields.ToArray();
    }

    /// <summary>
    /// キャラクターデータ検証
    /// </summary>
    [MenuItem("Tools/CSV Import/Validate Character Data")]
    public static void ValidateCharacterData()
    {
        try
        {
            Debug.Log("=== Character Data Validation ===");

            string[] characterAssets = AssetDatabase.FindAssets("t:CharacterMasterData", new[] { "Assets/GameData/Character" });

            if (characterAssets.Length == 0)
            {
                Debug.LogWarning("No character assets found");
                return;
            }

            int validCount = 0;
            int errorCount = 0;

            foreach (string guid in characterAssets)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CharacterMasterData character = AssetDatabase.LoadAssetAtPath<CharacterMasterData>(path);

                if (character == null) continue;

                List<string> errors = new List<string>();

                // 基本検証
                if (character.CharacterId <= 0)
                    errors.Add("Invalid character ID");

                if (string.IsNullOrEmpty(character.CharacterName))
                    errors.Add("Character name is empty");

                if (character.BaseLevel <= 0)
                    errors.Add("Invalid base level");

                if (character.MaxLevel < character.BaseLevel)
                    errors.Add("Max level is less than base level");

                if (character.Hp < 0)
                    errors.Add("HP is negative");

                if (errors.Count > 0)
                {
                    Debug.LogError($"Character {character.CharacterName} (ID:{character.CharacterId}) has errors:\n" +
                                 string.Join("\n", errors));
                    errorCount++;
                }
                else
                {
                    validCount++;
                }
            }

            Debug.Log($"Character validation completed. Valid: {validCount}, Errors: {errorCount}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Character validation failed: {e.Message}");
        }
    }
}