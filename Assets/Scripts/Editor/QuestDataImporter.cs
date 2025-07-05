using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// クエストシステム専用CSVインポーター
/// Quest、Monster、DropTableデータをCSVからScriptableObjectに変換
/// </summary>
public class QuestDataImporter : EditorWindow
{
    // === 1. パス定義 ===
    private static readonly string QUEST_CSV_PATH = "Assets/CSV/m_quest_data.csv";
    private static readonly string MONSTER_CSV_PATH = "Assets/CSV/m_monster_data.csv";
    private static readonly string DROP_TABLE_CSV_FOLDER = "Assets/CSV/m_drop_table/";

    private static readonly string QUEST_OUTPUT_PATH = "Assets/GameData/Quest/";
    private static readonly string MONSTER_OUTPUT_PATH = "Assets/GameData/Monster/";
    private static readonly string DROP_TABLE_OUTPUT_PATH = "Assets/GameData/DropTable/";

    // === 2. メニューアイテム ===
    [MenuItem("Tools/Quest CSV Import/Import All Quest System Data")]
    public static void ImportAllQuestSystemData()
    {
        Debug.Log("=== クエストシステムデータインポート開始 ===");

        ImportQuestData();
        ImportMonsterData();
        ImportDropTableData();

        Debug.Log("=== クエストシステムデータインポート完了 ===");
    }

    [MenuItem("Tools/Quest CSV Import/Import Quest Data")]
    public static void ImportQuestData()
    {
        ImportCSVData<QuestMasterData>(QUEST_CSV_PATH, QUEST_OUTPUT_PATH,
            "Quest", ParseQuestLine, UpdateQuestAsset);
    }

    [MenuItem("Tools/Quest CSV Import/Import Monster Data")]
    public static void ImportMonsterData()
    {
        ImportCSVData<MonsterMasterData>(MONSTER_CSV_PATH, MONSTER_OUTPUT_PATH,
            "Monster", ParseMonsterLine, UpdateMonsterAsset);
    }

    [MenuItem("Tools/Quest CSV Import/Import Drop Table Data")]
    public static void ImportDropTableData()
    {
        // m_drop_table/フォルダ内のCSVファイルを自動検出してインポート
        if (!Directory.Exists(DROP_TABLE_CSV_FOLDER))
        {
            Debug.LogError($"ドロップテーブルフォルダが見つかりません: {DROP_TABLE_CSV_FOLDER}");
            return;
        }

        string[] dropTableFiles = Directory.GetFiles(DROP_TABLE_CSV_FOLDER, "*.csv");

        if (dropTableFiles.Length == 0)
        {
            Debug.LogWarning($"ドロップテーブルCSVファイルが見つかりません: {DROP_TABLE_CSV_FOLDER}*.csv");
            return;
        }

        Debug.Log($"ドロップテーブルフォルダで{dropTableFiles.Length}個のCSVファイルを検出しました");

        foreach (string filePath in dropTableFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            Debug.Log($"ドロップテーブルインポート: {fileName} ({filePath})");
            ImportDropTableFile(filePath, fileName);
        }
    }

    // === 3. 共通インポート処理 ===
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

            // 出力フォルダ作成
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
                Debug.Log($"出力フォルダを作成: {outputPath}");
            }

            string csvContent = File.ReadAllText(csvPath, System.Text.Encoding.UTF8);
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
                        Debug.LogWarning($"{dataTypeName} line {i + 1}: パースに失敗しました");
                        errorCount++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing {dataTypeName} line {i + 1}: {e.Message}\nLine: {line}");
                    errorCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"{dataTypeName} インポート完了. 成功: {successCount}, エラー: {errorCount}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to import {dataTypeName} data: {e.Message}");
        }
    }

    // === 4. ドロップテーブル専用インポート処理 ===
    private static void ImportDropTableFile(string csvPath, string tableId)
    {
        try
        {
            Debug.Log($"=== ドロップテーブル({tableId})インポート開始 ===");

            if (!File.Exists(csvPath))
            {
                Debug.LogError($"Drop table CSV file not found: {csvPath}");
                return;
            }

            // 出力フォルダ作成
            if (!Directory.Exists(DROP_TABLE_OUTPUT_PATH))
            {
                Directory.CreateDirectory(DROP_TABLE_OUTPUT_PATH);
                Debug.Log($"出力フォルダを作成: {DROP_TABLE_OUTPUT_PATH}");
            }

            string csvContent = File.ReadAllText(csvPath, System.Text.Encoding.UTF8);
            string[] lines = csvContent.Split('\n');

            if (lines.Length < 2)
            {
                Debug.LogError($"Drop table CSV file is empty or has no data: {csvPath}");
                return;
            }

            // ドロップテーブル全体を一つのScriptableObjectとして作成
            DropTableMasterData dropTable = ScriptableObject.CreateInstance<DropTableMasterData>();
            dropTable.tableId = tableId;
            dropTable.dropItems = new List<DropItemData>();

            int successCount = 0;
            int errorCount = 0;

            // ヘッダー行をスキップして処理
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    DropItemData dropItem = ParseDropItemLine(line);
                    if (dropItem != null)
                    {
                        dropTable.dropItems.Add(dropItem);
                        successCount++;
                    }
                    else
                    {
                        Debug.LogWarning($"Drop item line {i + 1}: パースに失敗しました");
                        errorCount++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing drop item line {i + 1}: {e.Message}\nLine: {line}");
                    errorCount++;
                }
            }

            // ドロップテーブル全体を保存
            string fileName = $"DropTable_{tableId}.asset";
            string fullPath = DROP_TABLE_OUTPUT_PATH + fileName;

            DropTableMasterData existingAsset = AssetDatabase.LoadAssetAtPath<DropTableMasterData>(fullPath);
            if (existingAsset != null)
            {
                existingAsset.tableId = dropTable.tableId;
                existingAsset.dropItems = dropTable.dropItems;
                EditorUtility.SetDirty(existingAsset);
                Debug.Log($"既存アセットを更新: {fileName}");
            }
            else
            {
                AssetDatabase.CreateAsset(dropTable, fullPath);
                Debug.Log($"新規アセットを作成: {fileName}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"ドロップテーブル ({tableId}) インポート完了. 成功: {successCount}, エラー: {errorCount}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to import drop table {tableId}: {e.Message}");
        }
    }

    // === 5. アセット保存処理 ===
    private static void SaveAsset<T>(T data, string outputPath, string fileName,
        System.Action<T, T> updateFunc) where T : ScriptableObject
    {
        string fullPath = outputPath + fileName;

        T existingAsset = AssetDatabase.LoadAssetAtPath<T>(fullPath);
        if (existingAsset != null)
        {
            updateFunc(existingAsset, data);
            EditorUtility.SetDirty(existingAsset);
            Debug.Log($"既存アセットを更新: {fileName}");
        }
        else
        {
            AssetDatabase.CreateAsset(data, fullPath);
            Debug.Log($"新規アセットを作成: {fileName}");
        }
    }

    // === 6. ファイル名生成 ===
    private static string GetAssetFileName(ScriptableObject data, string dataTypeName)
    {
        return dataTypeName switch
        {
            "Quest" => $"Quest_{((QuestMasterData)data).questId:000}_{SanitizeFileName(((QuestMasterData)data).questName)}.asset",
            "Monster" => $"Monster_{((MonsterMasterData)data).monsterId:000}_{SanitizeFileName(((MonsterMasterData)data).monsterName)}.asset",
            _ => "Unknown.asset"
        };
    }

    // ファイル名に使用できない文字を除去
    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return "Unknown";

        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }

        return fileName.Replace(" ", "_"); // スペースもアンダースコアに変換
    }

    // === 7. クエストデータ解析 ===
    private static QuestMasterData ParseQuestLine(string line)
    {
        try
        {
            string[] fields = SplitCSVLine(line);

            if (fields.Length < 29)
            {
                Debug.LogError($"Quest CSV line has insufficient fields: {fields.Length}");
                return null;
            }

            QuestMasterData quest = ScriptableObject.CreateInstance<QuestMasterData>();

            // 基本情報
            quest.questId = ParseInt(fields[0]);
            quest.questName = fields[1];
            quest.sortOrder = ParseInt(fields[2]);
            quest.description = fields[3];
            quest.questType = ParseQuestType(fields[4]);
            quest.needLevel = ParseInt(fields[5]);
            quest.requiredClearQuest = ParseInt(fields[6]);
            quest.dailyClearLimit = ParseInt(fields[7]);
            quest.isRepeatable = ParseBool(fields[8]);
            quest.requiredStamina = ParseInt(fields[9]);
            quest.recommendedPower = ParseInt(fields[10]);

            // 出現モンスター
            quest.spawnMonsterId1 = ParseInt(fields[11]);
            quest.spawnMonsterId2 = ParseInt(fields[12]);
            quest.spawnMonsterId3 = ParseInt(fields[13]);

            // 制限
            quest.turnLimit = ParseInt(fields[14]);

            // 報酬
            quest.rewardExp = ParseInt(fields[15]);
            quest.rewardGold = ParseInt(fields[16]);
            quest.itemDropQuantity = ParseInt(fields[17]);
            quest.dropItemTable = fields[18];

            // 初回クリア報酬
            quest.firstClearItemType = fields[19];
            quest.firstClearItemId = ParseInt(fields[20]);
            quest.firstClearItemQuantity = ParseInt(fields[21]);

            // 開催期間
            quest.questOpenDay = fields[22];
            quest.questOpenTime = fields[23];
            quest.questEndDay = fields[24];
            quest.questEndTime = fields[25];

            // UI・演出
            quest.backgroundPath = fields[26];
            quest.bgmPath = fields[27];
            quest.questIconPath = fields[28];

            return quest;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing quest line: {e.Message}\nLine: {line}");
            return null;
        }
    }

    // === 8. モンスターデータ解析 ===
    private static MonsterMasterData ParseMonsterLine(string line)
    {
        try
        {
            string[] fields = SplitCSVLine(line);

            if (fields.Length < 23)
            {
                Debug.LogError($"Monster CSV line has insufficient fields: {fields.Length}");
                return null;
            }

            MonsterMasterData monster = ScriptableObject.CreateInstance<MonsterMasterData>();

            // 基本情報
            monster.monsterId = ParseInt(fields[0]);
            monster.monsterType = ParseMonsterType(fields[1]);
            monster.attributeType = ParseAttributeType(fields[2]);
            monster.rarity = ParseRarity(fields[3]);
            monster.monsterName = fields[4];

            // ステータス
            monster.hp = ParseInt(fields[5]);
            monster.offense = ParseInt(fields[6]);
            monster.defense = ParseInt(fields[7]);
            monster.speed = ParseInt(fields[8]);
            monster.criticalRate = ParseInt(fields[9]);
            monster.criticalDamageRate = ParseInt(fields[10]);

            // 属性攻撃
            monster.fireOffence = ParseInt(fields[11]);
            monster.waterOffence = ParseInt(fields[12]);
            monster.windOffence = ParseInt(fields[13]);
            monster.earthOffence = ParseInt(fields[14]);

            // 使用スキル
            monster.usedSkill1 = ParseInt(fields[15]);
            monster.usedSkill2 = ParseInt(fields[16]);
            monster.usedSkill3 = ParseInt(fields[17]);

            // UI・演出
            monster.monsterIconPath = fields[18];
            monster.monsterAnimationPath = fields[19];
            monster.description = fields[20];

            // 図鑑フラグ
            monster.completionFlag = ParseBool(fields[21]);
            monster.collectionFlag = ParseBool(fields[22]);

            return monster;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing monster line: {e.Message}\nLine: {line}");
            return null;
        }
    }

    // === 9. ドロップアイテムデータ解析 ===
    private static DropItemData ParseDropItemLine(string line)
    {
        try
        {
            string[] fields = SplitCSVLine(line);

            if (fields.Length < 6)
            {
                Debug.LogError($"Drop item CSV line has insufficient fields: {fields.Length}");
                return null;
            }

            DropItemData dropItem = new DropItemData
            {
                itemTableId = ParseInt(fields[0]),
                itemType = fields[1],
                itemId = ParseInt(fields[2]),
                itemName = fields[3],
                quantity = ParseInt(fields[4]),
                dropRate = ParseInt(fields[5])
            };

            return dropItem;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing drop item line: {e.Message}\nLine: {line}");
            return null;
        }
    }

    // === 10. アセット更新処理 ===
    private static void UpdateQuestAsset(QuestMasterData existing, QuestMasterData newData)
    {
        // 基本情報
        existing.questName = newData.questName;
        existing.sortOrder = newData.sortOrder;
        existing.description = newData.description;
        existing.questType = newData.questType;
        existing.needLevel = newData.needLevel;
        existing.requiredClearQuest = newData.requiredClearQuest;
        existing.dailyClearLimit = newData.dailyClearLimit;
        existing.isRepeatable = newData.isRepeatable;
        existing.requiredStamina = newData.requiredStamina;
        existing.recommendedPower = newData.recommendedPower;

        // 出現モンスター
        existing.spawnMonsterId1 = newData.spawnMonsterId1;
        existing.spawnMonsterId2 = newData.spawnMonsterId2;
        existing.spawnMonsterId3 = newData.spawnMonsterId3;

        // 制限
        existing.turnLimit = newData.turnLimit;

        // 報酬
        existing.rewardExp = newData.rewardExp;
        existing.rewardGold = newData.rewardGold;
        existing.itemDropQuantity = newData.itemDropQuantity;
        existing.dropItemTable = newData.dropItemTable;

        // 初回クリア報酬
        existing.firstClearItemType = newData.firstClearItemType;
        existing.firstClearItemId = newData.firstClearItemId;
        existing.firstClearItemQuantity = newData.firstClearItemQuantity;

        // 開催期間
        existing.questOpenDay = newData.questOpenDay;
        existing.questOpenTime = newData.questOpenTime;
        existing.questEndDay = newData.questEndDay;
        existing.questEndTime = newData.questEndTime;

        // UI・演出
        existing.backgroundPath = newData.backgroundPath;
        existing.bgmPath = newData.bgmPath;
        existing.questIconPath = newData.questIconPath;
    }

    private static void UpdateMonsterAsset(MonsterMasterData existing, MonsterMasterData newData)
    {
        // 基本情報
        existing.monsterType = newData.monsterType;
        existing.attributeType = newData.attributeType;
        existing.rarity = newData.rarity;
        existing.monsterName = newData.monsterName;

        // ステータス
        existing.hp = newData.hp;
        existing.offense = newData.offense;
        existing.defense = newData.defense;
        existing.speed = newData.speed;
        existing.criticalRate = newData.criticalRate;
        existing.criticalDamageRate = newData.criticalDamageRate;

        // 属性攻撃
        existing.fireOffence = newData.fireOffence;
        existing.waterOffence = newData.waterOffence;
        existing.windOffence = newData.windOffence;
        existing.earthOffence = newData.earthOffence;

        // 使用スキル
        existing.usedSkill1 = newData.usedSkill1;
        existing.usedSkill2 = newData.usedSkill2;
        existing.usedSkill3 = newData.usedSkill3;

        // UI・演出
        existing.monsterIconPath = newData.monsterIconPath;
        existing.monsterAnimationPath = newData.monsterAnimationPath;
        existing.description = newData.description;

        // 図鑑フラグ
        existing.completionFlag = newData.completionFlag;
        existing.collectionFlag = newData.collectionFlag;
    }

    // === 11. パース用ユーティリティ ===
    private static QuestType ParseQuestType(string typeStr)
    {
        return typeStr.ToLower() switch
        {
            "story" => QuestType.Story,
            "daily" => QuestType.Daily,
            "weekly" => QuestType.Weekly,
            "event" => QuestType.Event,
            "" => QuestType.Story,
            _ => QuestType.Story
        };
    }

    private static MonsterType ParseMonsterType(string typeStr)
    {
        return typeStr.ToLower() switch
        {
            "normal" => MonsterType.Normal,
            "boss" => MonsterType.Boss,
            "" => MonsterType.Normal,
            _ => MonsterType.Normal
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
        Debug.LogWarning($"Failed to parse int: '{value}', defaulting to 0");
        return 0;
    }

    private static bool ParseBool(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        if (value == "1") return true;
        if (value.ToLower() == "true") return true;
        if (bool.TryParse(value, out bool result)) return result;
        Debug.LogWarning($"Failed to parse bool: '{value}', defaulting to false");
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

    // === 12. 検証メニュー ===
    [MenuItem("Tools/Quest CSV Import/Validate All Quest Data")]
    public static void ValidateAllQuestData()
    {
        Debug.Log("=== クエストシステムデータ検証開始 ===");
        ValidateQuestData();
        ValidateMonsterData();
        ValidateDropTableData();
        Debug.Log("=== クエストシステムデータ検証完了 ===");
    }

    [MenuItem("Tools/Quest CSV Import/Validate Quest Data")]
    public static void ValidateQuestData()
    {
        ValidateAssets<QuestMasterData>("Quest", "Assets/GameData/Quest");
    }

    [MenuItem("Tools/Quest CSV Import/Validate Monster Data")]
    public static void ValidateMonsterData()
    {
        ValidateAssets<MonsterMasterData>("Monster", "Assets/GameData/Monster");
    }

    [MenuItem("Tools/Quest CSV Import/Validate Drop Table Data")]
    public static void ValidateDropTableData()
    {
        ValidateAssets<DropTableMasterData>("DropTable", "Assets/GameData/DropTable");
    }

    // === 13. 検証処理 ===
    private static void ValidateAssets<T>(string dataType, string searchPath) where T : ScriptableObject
    {
        try
        {
            Debug.Log($"=== {dataType} Data Validation ===");

            if (!Directory.Exists(searchPath))
            {
                Debug.LogWarning($"Search path does not exist: {searchPath}");
                return;
            }

            string[] assets = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { searchPath });

            if (assets.Length == 0)
            {
                Debug.LogWarning($"No {dataType} assets found in {searchPath}");
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
            case QuestMasterData quest:
                if (quest.questId <= 0) errors.Add("Invalid quest ID");
                if (string.IsNullOrEmpty(quest.questName)) errors.Add("Quest name is empty");
                if (quest.needLevel < 0) errors.Add("Need level is negative");
                if (quest.requiredStamina < 0) errors.Add("Required stamina is negative");
                if (quest.recommendedPower < 0) errors.Add("Recommended power is negative");
                if (quest.rewardExp < 0) errors.Add("Reward exp is negative");
                if (quest.rewardGold < 0) errors.Add("Reward gold is negative");
                if (quest.itemDropQuantity < 0) errors.Add("Item drop quantity is negative");
                if (quest.spawnMonsterId1 <= 0) errors.Add("At least one spawn monster must be specified");
                if (quest.firstClearItemQuantity < 0) errors.Add("First clear item quantity is negative");

                // 日付形式の簡易チェック
                if (!string.IsNullOrEmpty(quest.questOpenDay) && !IsValidDateFormat(quest.questOpenDay))
                    errors.Add($"Invalid quest open day format: {quest.questOpenDay}");
                if (!string.IsNullOrEmpty(quest.questEndDay) && !IsValidDateFormat(quest.questEndDay))
                    errors.Add($"Invalid quest end day format: {quest.questEndDay}");

                // 時刻形式の簡易チェック
                if (!string.IsNullOrEmpty(quest.questOpenTime) && !IsValidTimeFormat(quest.questOpenTime))
                    errors.Add($"Invalid quest open time format: {quest.questOpenTime}");
                if (!string.IsNullOrEmpty(quest.questEndTime) && !IsValidTimeFormat(quest.questEndTime))
                    errors.Add($"Invalid quest end time format: {quest.questEndTime}");
                break;

            case MonsterMasterData monster:
                if (monster.monsterId <= 0) errors.Add("Invalid monster ID");
                if (string.IsNullOrEmpty(monster.monsterName)) errors.Add("Monster name is empty");
                if (monster.hp <= 0) errors.Add("HP must be positive");
                if (monster.offense < 0) errors.Add("Offense is negative");
                if (monster.defense < 0) errors.Add("Defense is negative");
                if (monster.speed < 0) errors.Add("Speed is negative");
                if (monster.criticalRate < 0 || monster.criticalRate > 100)
                    errors.Add("Critical rate must be between 0-100");
                if (monster.criticalDamageRate < 0) errors.Add("Critical damage rate is negative");
                if (monster.usedSkill1 <= 0) errors.Add("At least one skill must be specified");
                break;

            case DropTableMasterData dropTable:
                if (string.IsNullOrEmpty(dropTable.tableId)) errors.Add("Table ID is empty");
                if (dropTable.dropItems == null || dropTable.dropItems.Count == 0)
                    errors.Add("Drop items list is empty");

                for (int i = 0; i < dropTable.dropItems?.Count; i++)
                {
                    var item = dropTable.dropItems[i];
                    if (item.itemTableId <= 0) errors.Add($"Drop item {i}: Invalid item table ID");
                    if (string.IsNullOrEmpty(item.itemType)) errors.Add($"Drop item {i}: Item type is empty");
                    if (item.itemId <= 0) errors.Add($"Drop item {i}: Invalid item ID");
                    if (item.quantity <= 0) errors.Add($"Drop item {i}: Quantity must be positive");
                    if (item.dropRate < 0 || item.dropRate > 100)
                        errors.Add($"Drop item {i}: Drop rate must be between 0-100");
                }
                break;
        }

        return errors;
    }

    // === 14. 日付・時刻フォーマット検証 ===
    private static bool IsValidDateFormat(string date)
    {
        if (string.IsNullOrEmpty(date)) return false;
        return System.DateTime.TryParseExact(date, "yyyy-MM-dd", null,
            System.Globalization.DateTimeStyles.None, out _);
    }

    private static bool IsValidTimeFormat(string time)
    {
        if (string.IsNullOrEmpty(time)) return false;
        return System.TimeSpan.TryParseExact(time, @"hh\:mm", null, out _);
    }

    // === 15. デバッグ・ヘルパーメニュー ===
    [MenuItem("Tools/Quest CSV Import/Show Quest Data Info")]
    public static void ShowQuestDataInfo()
    {
        string[] questAssets = AssetDatabase.FindAssets("t:QuestMasterData", new[] { "Assets/GameData/Quest" });
        string[] monsterAssets = AssetDatabase.FindAssets("t:MonsterMasterData", new[] { "Assets/GameData/Monster" });
        string[] dropTableAssets = AssetDatabase.FindAssets("t:DropTableMasterData", new[] { "Assets/GameData/DropTable" });

        Debug.Log($"=== クエストシステムデータ情報 ===");
        Debug.Log($"クエストデータ: {questAssets.Length} 個");
        Debug.Log($"モンスターデータ: {monsterAssets.Length} 個");
        Debug.Log($"ドロップテーブル: {dropTableAssets.Length} 個");
        Debug.Log($"合計: {questAssets.Length + monsterAssets.Length + dropTableAssets.Length} 個");
    }

    [MenuItem("Tools/Quest CSV Import/Clear All Quest Data")]
    public static void ClearAllQuestData()
    {
        if (EditorUtility.DisplayDialog("警告",
            "クエストシステムの全データを削除しますか？\nこの操作は元に戻せません。",
            "削除", "キャンセル"))
        {
            DeleteAssetsInFolder("Assets/GameData/Quest");
            DeleteAssetsInFolder("Assets/GameData/Monster");
            DeleteAssetsInFolder("Assets/GameData/DropTable");

            AssetDatabase.Refresh();
            Debug.Log("クエストシステムの全データを削除しました");
        }
    }

    private static void DeleteAssetsInFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;

        string[] assetPaths = Directory.GetFiles(folderPath, "*.asset");
        foreach (string assetPath in assetPaths)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }
    }

    // === 16. CSV形式チェック ===
    [MenuItem("Tools/Quest CSV Import/Check CSV Format")]
    public static void CheckCSVFormat()
    {
        Debug.Log("=== CSV形式チェック開始 ===");

        CheckCSVFile(QUEST_CSV_PATH, "Quest", 29); // 29カラム期待
        CheckCSVFile(MONSTER_CSV_PATH, "Monster", 23); // 23カラム期待

        // ドロップテーブルファイルをチェック
        if (!Directory.Exists(DROP_TABLE_CSV_FOLDER))
        {
            Debug.LogWarning($"DropTable: フォルダが見つかりません - {DROP_TABLE_CSV_FOLDER}");
            return;
        }

        string[] dropTableFiles = Directory.GetFiles(DROP_TABLE_CSV_FOLDER, "*.csv");

        if (dropTableFiles.Length == 0)
        {
            Debug.LogWarning($"DropTable: CSVファイルが見つかりません - {DROP_TABLE_CSV_FOLDER}*.csv");
        }
        else
        {
            Debug.Log($"DropTable: {dropTableFiles.Length}個のCSVファイルを検出");
            foreach (string filePath in dropTableFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                CheckCSVFile(filePath, $"DropTable({fileName})", 6); // 6カラム期待
            }
        }

        Debug.Log("=== CSV形式チェック完了 ===");
    }

    private static void CheckCSVFile(string csvPath, string dataTypeName, int expectedColumnCount)
    {
        try
        {
            if (!File.Exists(csvPath))
            {
                Debug.LogWarning($"{dataTypeName}: CSVファイルが見つかりません - {csvPath}");
                return;
            }

            string csvContent = File.ReadAllText(csvPath, System.Text.Encoding.UTF8);
            string[] lines = csvContent.Split('\n');

            if (lines.Length < 2)
            {
                Debug.LogError($"{dataTypeName}: CSVファイルが空または無効です");
                return;
            }

            // ヘッダー行のカラム数チェック
            string[] headerFields = SplitCSVLine(lines[0]);
            if (headerFields.Length != expectedColumnCount)
            {
                Debug.LogWarning($"{dataTypeName}: カラム数が予期値と異なります。期待: {expectedColumnCount}, 実際: {headerFields.Length}");
                Debug.Log($"{dataTypeName} ヘッダー: {string.Join(", ", headerFields)}");
            }

            // データ行数とサンプル行チェック
            int dataLineCount = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    dataLineCount++;

                    // 最初のデータ行のカラム数チェック
                    if (dataLineCount == 1)
                    {
                        string[] dataFields = SplitCSVLine(line);
                        if (dataFields.Length != expectedColumnCount)
                        {
                            Debug.LogWarning($"{dataTypeName}: データ行のカラム数が不正です。行{i + 1}: 期待{expectedColumnCount}, 実際{dataFields.Length}");
                        }
                    }
                }
            }

            Debug.Log($"{dataTypeName}: OK - {dataLineCount} データ行, {headerFields.Length} カラム");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{dataTypeName}: CSV形式チェック中にエラー - {e.Message}");
        }
    }
}