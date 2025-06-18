using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class QuestDataImporter : EditorWindow
{
    private string csvFilePath = "";
    private string outputFolder = "Assets/GameData/Quests";
    private TextAsset csvFile;

    // Unity 6.0対応：複数のメニュー位置に配置
    [MenuItem("Window/Game Data/Quest Data Importer")]
    [MenuItem("Assets/Import Quest Data", false, 100)]
    public static void ShowWindow()
    {
        var window = GetWindow<QuestDataImporter>("Quest Data Importer");
        window.minSize = new Vector2(400, 300);
    }

    void OnGUI()
    {
        GUILayout.Label("Quest Data CSV Importer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // CSVファイル選択
        EditorGUILayout.BeginHorizontal();
        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        // または直接パス指定
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Or File Path:", GUILayout.Width(80));
        csvFilePath = EditorGUILayout.TextField(csvFilePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("Select CSV file", "", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                csvFilePath = path;
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        // 出力フォルダ
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Output Folder:", GUILayout.Width(80));
        outputFolder = EditorGUILayout.TextField(outputFolder);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // インポートボタン
        if (GUILayout.Button("Import Quest Data", GUILayout.Height(30)))
        {
            ImportQuestData();
        }

        GUILayout.Space(10);

        // 使い方説明
        EditorGUILayout.HelpBox(
            "使い方:\n" +
            "1. GoogleスプレッドシートからCSVをダウンロード\n" +
            "2. CSVファイルをProjectビューにドラッグ&ドロップ\n" +
            "3. 上記のCSV Fileフィールドに設定\n" +
            "4. Import Quest Dataボタンをクリック",
            MessageType.Info);
    }

    private void ImportQuestData()
    {
        string csvContent = "";

        // CSVファイルの内容を取得
        if (csvFile != null)
        {
            csvContent = csvFile.text;
        }
        else if (!string.IsNullOrEmpty(csvFilePath) && File.Exists(csvFilePath))
        {
            csvContent = File.ReadAllText(csvFilePath);
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "CSVファイルを選択してください", "OK");
            return;
        }

        // CSVパース
        var questDataList = ParseCSV(csvContent);

        if (questDataList.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "有効なクエストデータが見つかりませんでした", "OK");
            return;
        }

        // 出力フォルダ作成
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // ScriptableObjectとして保存
        int createdCount = 0;
        foreach (var data in questDataList)
        {
            string assetPath = $"{outputFolder}/Quest_{data.questId:D3}_{data.questName}.asset";

            // 既存のアセットがあるか確認
            QuestData existingAsset = AssetDatabase.LoadAssetAtPath<QuestData>(assetPath);
            QuestData questAsset;

            if (existingAsset != null)
            {
                questAsset = existingAsset;
                EditorUtility.SetDirty(questAsset);
            }
            else
            {
                questAsset = ScriptableObject.CreateInstance<QuestData>();
                AssetDatabase.CreateAsset(questAsset, assetPath);
            }

            // データをコピー
            CopyDataToAsset(data, questAsset);

            createdCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完了",
            $"{createdCount}個のクエストデータをインポートしました\n" +
            $"保存先: {outputFolder}", "OK");
    }

    private List<QuestCSVData> ParseCSV(string csvContent)
    {
        var result = new List<QuestCSVData>();
        var lines = csvContent.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();

        if (lines.Length < 2) return result; // ヘッダー行と最低1行のデータが必要

        // ヘッダー行をスキップして、データ行を処理
        for (int i = 1; i < lines.Length; i++)
        {
            var values = SplitCSVLine(lines[i]);
            if (values.Length < 20) continue; // 必要な列数をチェック

            try
            {
                var data = new QuestCSVData
                {
                    questId = int.Parse(values[0]),
                    questName = values[1],
                    questDescription = values[2],
                    questType = ParseQuestType(values[3]),
                    requiredLevel = int.Parse(values[4]),
                    prerequisiteQuests = ParseIntArray(values[5]),
                    clearLimit = int.Parse(values[6]),
                    requiredStamina = int.Parse(values[7]),
                    recommendedPower = int.Parse(values[8]),
                    monsterSpawnCSV = values[9],
                    monsterCount = int.Parse(values[10]),
                    turnLimit = int.Parse(values[11]),
                    rewardExp = int.Parse(values[12]),
                    rewardGold = int.Parse(values[13]),
                    itemDropCSV = values[14],
                    firstClearItemType = values[15],
                    firstClearItemId = string.IsNullOrWhiteSpace(values[16]) ? 0 : int.Parse(values[16]),
                    firstClearItemQuantity = string.IsNullOrWhiteSpace(values[17]) ? 0 : int.Parse(values[17]),
                    backgroundId = int.Parse(values[18]),
                    bgmId = int.Parse(values[19])
                };

                result.Add(data);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"行 {i + 1} のパースエラー: {e.Message}");
            }
        }

        return result;
    }

    private string[] SplitCSVLine(string line)
    {
        var result = new List<string>();
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
                result.Add(currentField.Trim());
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        result.Add(currentField.Trim());
        return result.ToArray();
    }

    private QuestType ParseQuestType(string type)
    {
        return type.ToLower() switch
        {
            "normal" => QuestType.Normal,
            "event" => QuestType.Event,
            "daily" => QuestType.Daily,
            "tutorial" => QuestType.Tutorial,
            "boss" => QuestType.Boss,
            _ => QuestType.Normal
        };
    }

    private int[] ParseIntArray(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return new int[0];

        return str.Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => int.TryParse(s, out int val) ? val : 0)
            .Where(val => val > 0)
            .ToArray();
    }

    private void CopyDataToAsset(QuestCSVData source, QuestData target)
    {
        target.questId = source.questId;
        target.questName = source.questName;
        target.questDescription = source.questDescription;
        target.questType = source.questType;
        target.requiredLevel = source.requiredLevel;
        target.prerequisiteQuestIds = source.prerequisiteQuests;
        target.clearLimit = source.clearLimit;
        target.requiredStamina = source.requiredStamina;
        target.recommendedPower = source.recommendedPower;
        target.monsterSpawnCSV = source.monsterSpawnCSV;
        target.monsterCount = source.monsterCount;
        target.turnLimit = source.turnLimit;
        target.rewardExp = source.rewardExp;
        target.rewardGold = source.rewardGold;
        target.itemDropCSV = source.itemDropCSV;

        // 初回クリア報酬
        if (!string.IsNullOrWhiteSpace(source.firstClearItemType))
        {
            target.hasFirstClearReward = true;
            target.firstClearItemType = ParseItemType(source.firstClearItemType);
            target.firstClearItemId = source.firstClearItemId;
            target.firstClearItemQuantity = source.firstClearItemQuantity;
        }
        else
        {
            target.hasFirstClearReward = false;
        }

        target.backgroundId = source.backgroundId;
        target.bgmId = source.bgmId;
    }

    private ItemType ParseItemType(string type)
    {
        return type.ToLower() switch
        {
            "equipment" => ItemType.Equipment,
            "enhancement" => ItemType.Enhancement,
            "support" => ItemType.Support,
            _ => ItemType.Enhancement
        };
    }

    // CSV解析用の一時データ構造
    private class QuestCSVData
    {
        public int questId;
        public string questName;
        public string questDescription;
        public QuestType questType;
        public int requiredLevel;
        public int[] prerequisiteQuests;
        public int clearLimit;
        public int requiredStamina;
        public int recommendedPower;
        public string monsterSpawnCSV;
        public int monsterCount;
        public int turnLimit;
        public int rewardExp;
        public int rewardGold;
        public string itemDropCSV;
        public string firstClearItemType;
        public int firstClearItemId;
        public int firstClearItemQuantity;
        public int backgroundId;
        public int bgmId;
    }
}