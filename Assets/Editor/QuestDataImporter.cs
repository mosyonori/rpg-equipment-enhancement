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

    // Unity 6.0�Ή��F�����̃��j���[�ʒu�ɔz�u
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

        // CSV�t�@�C���I��
        EditorGUILayout.BeginHorizontal();
        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        // �܂��͒��ڃp�X�w��
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

        // �o�̓t�H���_
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Output Folder:", GUILayout.Width(80));
        outputFolder = EditorGUILayout.TextField(outputFolder);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // �C���|�[�g�{�^��
        if (GUILayout.Button("Import Quest Data", GUILayout.Height(30)))
        {
            ImportQuestData();
        }

        GUILayout.Space(10);

        // �g��������
        EditorGUILayout.HelpBox(
            "�g����:\n" +
            "1. Google�X�v���b�h�V�[�g����CSV���_�E�����[�h\n" +
            "2. CSV�t�@�C����Project�r���[�Ƀh���b�O&�h���b�v\n" +
            "3. ��L��CSV File�t�B�[���h�ɐݒ�\n" +
            "4. Import Quest Data�{�^�����N���b�N",
            MessageType.Info);
    }

    private void ImportQuestData()
    {
        string csvContent = "";

        // CSV�t�@�C���̓��e���擾
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
            EditorUtility.DisplayDialog("Error", "CSV�t�@�C����I�����Ă�������", "OK");
            return;
        }

        // CSV�p�[�X
        var questDataList = ParseCSV(csvContent);

        if (questDataList.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "�L���ȃN�G�X�g�f�[�^��������܂���ł���", "OK");
            return;
        }

        // �o�̓t�H���_�쐬
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // ScriptableObject�Ƃ��ĕۑ�
        int createdCount = 0;
        foreach (var data in questDataList)
        {
            string assetPath = $"{outputFolder}/Quest_{data.questId:D3}_{data.questName}.asset";

            // �����̃A�Z�b�g�����邩�m�F
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

            // �f�[�^���R�s�[
            CopyDataToAsset(data, questAsset);

            createdCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("����",
            $"{createdCount}�̃N�G�X�g�f�[�^���C���|�[�g���܂���\n" +
            $"�ۑ���: {outputFolder}", "OK");
    }

    private List<QuestCSVData> ParseCSV(string csvContent)
    {
        var result = new List<QuestCSVData>();
        var lines = csvContent.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();

        if (lines.Length < 2) return result; // �w�b�_�[�s�ƍŒ�1�s�̃f�[�^���K�v

        // �w�b�_�[�s���X�L�b�v���āA�f�[�^�s������
        for (int i = 1; i < lines.Length; i++)
        {
            var values = SplitCSVLine(lines[i]);
            if (values.Length < 20) continue; // �K�v�ȗ񐔂��`�F�b�N

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
                Debug.LogError($"�s {i + 1} �̃p�[�X�G���[: {e.Message}");
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

        // ����N���A��V
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

    // CSV��͗p�̈ꎞ�f�[�^�\��
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