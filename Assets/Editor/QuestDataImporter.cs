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

        if (lines.Length < 2)
        {
            Debug.LogError("CSVファイルにデータ行がありません。ヘッダー行と最低1行のデータが必要です。");
            return result;
        }

        // デバッグ用：ヘッダー行を表示
        Debug.Log($"ヘッダー行: {lines[0]}");

        // ヘッダー行をスキップして、データ行を処理
        for (int i = 1; i < lines.Length; i++)
        {
            var values = SplitCSVLine(lines[i]);

            // デバッグ用：各行の列数を表示
            Debug.Log($"行 {i + 1}: {values.Length} 列のデータ");

            if (values.Length < 20)
            {
                Debug.LogWarning($"行 {i + 1}: 列数が不足しています（{values.Length}/20）。スキップします。");
                continue;
            }

            try
            {
                var data = new QuestCSVData
                {
                    questId = ParseInt(values[0], "questId", i + 1),
                    questName = values[1].Trim(),
                    questDescription = values[2].Trim(),
                    questType = ParseQuestType(values[3]),
                    requiredLevel = ParseInt(values[4], "requiredLevel", i + 1, 1),
                    prerequisiteQuests = ParseIntArray(values[5]),
                    clearLimit = ParseInt(values[6], "clearLimit", i + 1, -1),
                    requiredStamina = ParseInt(values[7], "requiredStamina", i + 1, 5),
                    recommendedPower = ParseInt(values[8], "recommendedPower", i + 1, 100),
                    monsterSpawnCSV = values[9].Trim(),
                    monsterCount = ParseInt(values[10], "monsterCount", i + 1, 1),
                    turnLimit = ParseInt(values[11], "turnLimit", i + 1, 0),
                    rewardExp = ParseInt(values[12], "rewardExp", i + 1, 0),
                    rewardGold = ParseInt(values[13], "rewardGold", i + 1, 0),
                    itemDropCSV = values[14].Trim(),
                    firstClearItemType = values[15].Trim(),
                    firstClearItemId = ParseInt(values[16], "firstClearItemId", i + 1, 0),
                    firstClearItemQuantity = ParseInt(values[17], "firstClearItemQuantity", i + 1, 0),
                    backgroundId = ParseInt(values[18], "backgroundId", i + 1, 1),
                    bgmId = ParseInt(values[19], "bgmId", i + 1, 1)
                };

                // 基本的な検証
                if (data.questId <= 0)
                {
                    Debug.LogError($"行 {i + 1}: 無効なクエストID ({data.questId})");
                    continue;
                }

                if (string.IsNullOrEmpty(data.questName))
                {
                    Debug.LogError($"行 {i + 1}: クエスト名が空です");
                    continue;
                }

                result.Add(data);
                Debug.Log($"行 {i + 1}: クエスト '{data.questName}' (ID: {data.questId}) を正常に読み込みました");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"行 {i + 1} のパースエラー: {e.Message}\n{e.StackTrace}");
            }
        }

        Debug.Log($"合計 {result.Count} 個のクエストデータを読み込みました");
        return result;
    }

    // 安全な整数パース関数
    private int ParseInt(string value, string fieldName, int lineNumber, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.Log($"行 {lineNumber}: {fieldName} が空です。デフォルト値 {defaultValue} を使用します。");
            return defaultValue;
        }

        value = value.Trim();
        if (int.TryParse(value, out int result))
        {
            return result;
        }
        else
        {
            Debug.LogWarning($"行 {lineNumber}: {fieldName} の値 '{value}' を整数に変換できません。デフォルト値 {defaultValue} を使用します。");
            return defaultValue;
        }
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
                // フィールドの処理
                currentField = ProcessField(currentField);
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        // 最後のフィールド
        currentField = ProcessField(currentField);
        result.Add(currentField);
        return result.ToArray();
    }

    // フィールドの前処理
    private string ProcessField(string field)
    {
        // 前後の空白とクォートを削除
        field = field.Trim();
        if (field.StartsWith("\"") && field.EndsWith("\""))
        {
            field = field.Substring(1, field.Length - 2);
        }

        // アポストロフィがある場合は削除（Googleスプレッドシート対策）
        if (field.StartsWith("'"))
        {
            field = field.Substring(1);
        }

        return field;
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