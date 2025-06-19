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

        if (lines.Length < 2)
        {
            Debug.LogError("CSV�t�@�C���Ƀf�[�^�s������܂���B�w�b�_�[�s�ƍŒ�1�s�̃f�[�^���K�v�ł��B");
            return result;
        }

        // �f�o�b�O�p�F�w�b�_�[�s��\��
        Debug.Log($"�w�b�_�[�s: {lines[0]}");

        // �w�b�_�[�s���X�L�b�v���āA�f�[�^�s������
        for (int i = 1; i < lines.Length; i++)
        {
            var values = SplitCSVLine(lines[i]);

            // �f�o�b�O�p�F�e�s�̗񐔂�\��
            Debug.Log($"�s {i + 1}: {values.Length} ��̃f�[�^");

            if (values.Length < 20)
            {
                Debug.LogWarning($"�s {i + 1}: �񐔂��s�����Ă��܂��i{values.Length}/20�j�B�X�L�b�v���܂��B");
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

                // ��{�I�Ȍ���
                if (data.questId <= 0)
                {
                    Debug.LogError($"�s {i + 1}: �����ȃN�G�X�gID ({data.questId})");
                    continue;
                }

                if (string.IsNullOrEmpty(data.questName))
                {
                    Debug.LogError($"�s {i + 1}: �N�G�X�g������ł�");
                    continue;
                }

                result.Add(data);
                Debug.Log($"�s {i + 1}: �N�G�X�g '{data.questName}' (ID: {data.questId}) �𐳏�ɓǂݍ��݂܂���");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"�s {i + 1} �̃p�[�X�G���[: {e.Message}\n{e.StackTrace}");
            }
        }

        Debug.Log($"���v {result.Count} �̃N�G�X�g�f�[�^��ǂݍ��݂܂���");
        return result;
    }

    // ���S�Ȑ����p�[�X�֐�
    private int ParseInt(string value, string fieldName, int lineNumber, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.Log($"�s {lineNumber}: {fieldName} ����ł��B�f�t�H���g�l {defaultValue} ���g�p���܂��B");
            return defaultValue;
        }

        value = value.Trim();
        if (int.TryParse(value, out int result))
        {
            return result;
        }
        else
        {
            Debug.LogWarning($"�s {lineNumber}: {fieldName} �̒l '{value}' �𐮐��ɕϊ��ł��܂���B�f�t�H���g�l {defaultValue} ���g�p���܂��B");
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
                // �t�B�[���h�̏���
                currentField = ProcessField(currentField);
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        // �Ō�̃t�B�[���h
        currentField = ProcessField(currentField);
        result.Add(currentField);
        return result.ToArray();
    }

    // �t�B�[���h�̑O����
    private string ProcessField(string field)
    {
        // �O��̋󔒂ƃN�H�[�g���폜
        field = field.Trim();
        if (field.StartsWith("\"") && field.EndsWith("\""))
        {
            field = field.Substring(1, field.Length - 2);
        }

        // �A�|�X�g���t�B������ꍇ�͍폜�iGoogle�X�v���b�h�V�[�g�΍�j
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