using UnityEngine;
using UnityEditor;
using System.Net;
using System.IO;

public class GoogleSheetsImporter : EditorWindow
{
    // Google�X�v���b�h�V�[�g��URL����擾����ID
    private string spreadsheetId = "YOUR_SPREADSHEET_ID_HERE";
    private string sheetName = "QuestMaster";
    private string outputFolder = "Assets/GameData/Quests";

    [MenuItem("Tools/Google Sheets Quest Importer")]
    public static void ShowWindow()
    {
        GetWindow<GoogleSheetsImporter>("Google Sheets Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Google Sheets Quest Importer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // �X�v���b�h�V�[�gID����
        EditorGUILayout.LabelField("Spreadsheet ID:");
        spreadsheetId = EditorGUILayout.TextField(spreadsheetId);

        EditorGUILayout.HelpBox(
            "�X�v���b�h�V�[�gID�̎擾���@:\n" +
            "Google�X�v���b�h�V�[�g��URL����擾\n" +
            "��: https://docs.google.com/spreadsheets/d/[������ID]/edit\n" +
            "���X�v���b�h�V�[�g�́u�����N��m���Ă���S���v�Ɍ��J����K�v������܂�",
            MessageType.Info);

        GUILayout.Space(5);

        // �V�[�g��
        EditorGUILayout.LabelField("Sheet Name:");
        sheetName = EditorGUILayout.TextField(sheetName);

        // �o�̓t�H���_
        EditorGUILayout.LabelField("Output Folder:");
        outputFolder = EditorGUILayout.TextField(outputFolder);

        GUILayout.Space(10);

        // �C���|�[�g�{�^��
        if (GUILayout.Button("Import from Google Sheets", GUILayout.Height(30)))
        {
            ImportFromGoogleSheets();
        }

        GUILayout.Space(10);

        // ���J�ݒ�̐���
        EditorGUILayout.HelpBox(
            "�d�v: Google�X�v���b�h�V�[�g�̋��L�ݒ�\n" +
            "1. �X�v���b�h�V�[�g���J��\n" +
            "2. �E��́u���L�v�{�^�����N���b�N\n" +
            "3. �u�����N��m���Ă���S���v��I��\n" +
            "4. �u�{���ҁv������OK",
            MessageType.Warning);
    }

    private void ImportFromGoogleSheets()
    {
        if (string.IsNullOrEmpty(spreadsheetId))
        {
            EditorUtility.DisplayDialog("Error", "�X�v���b�h�V�[�gID����͂��Ă�������", "OK");
            return;
        }

        try
        {
            // Google�X�v���b�h�V�[�g��CSV�G�N�X�|�[�gURL
            string url = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid=0";

            using (WebClient client = new WebClient())
            {
                client.Encoding = System.Text.Encoding.UTF8;
                string csvContent = client.DownloadString(url);

                // �ꎞ�t�@�C���ɕۑ�
                string tempPath = "Assets/temp_quest_data.csv";
                File.WriteAllText(tempPath, csvContent);
                AssetDatabase.Refresh();

                // CSV�t�@�C����ǂݍ���ŃC���|�[�g
                TextAsset csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(tempPath);
                if (csvAsset != null)
                {
                    // QuestDataImporter�̏������Ăяo��
                    ProcessCSVImport(csvAsset.text);

                    // �ꎞ�t�@�C�����폜
                    AssetDatabase.DeleteAsset(tempPath);
                }
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error",
                $"�f�[�^�̎擾�Ɏ��s���܂���:\n{e.Message}\n\n" +
                "�X�v���b�h�V�[�g�����J�ݒ�ɂȂ��Ă��邩�m�F���Ă�������", "OK");
        }
    }

    private void ProcessCSVImport(string csvContent)
    {
        // QuestDataImporter�̃��W�b�N���ė��p
        // ������CSV�����̃R�[�h���L�q�iQuestDataImporter���痬�p�j
        EditorUtility.DisplayDialog("����", "�N�G�X�g�f�[�^�̃C���|�[�g���������܂���", "OK");
    }
}

// Google�X�v���b�h�V�[�g�ݒ�ۑ��p
[System.Serializable]
public class GoogleSheetsSettings : ScriptableObject
{
    public string questSpreadsheetId;
    public string monsterSpreadsheetId;
    public string itemSpreadsheetId;

    private static GoogleSheetsSettings instance;

    public static GoogleSheetsSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<GoogleSheetsSettings>("GoogleSheetsSettings");
                if (instance == null)
                {
                    instance = CreateInstance<GoogleSheetsSettings>();

                    if (!Directory.Exists("Assets/Resources"))
                    {
                        Directory.CreateDirectory("Assets/Resources");
                    }

                    AssetDatabase.CreateAsset(instance, "Assets/Resources/GoogleSheetsSettings.asset");
                    AssetDatabase.SaveAssets();
                }
            }
            return instance;
        }
    }
}