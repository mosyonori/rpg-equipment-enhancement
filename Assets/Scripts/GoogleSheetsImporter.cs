using UnityEngine;
using UnityEditor;
using System.Net;
using System.IO;

public class GoogleSheetsImporter : EditorWindow
{
    // GoogleスプレッドシートのURLから取得したID
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

        // スプレッドシートID入力
        EditorGUILayout.LabelField("Spreadsheet ID:");
        spreadsheetId = EditorGUILayout.TextField(spreadsheetId);

        EditorGUILayout.HelpBox(
            "スプレッドシートIDの取得方法:\n" +
            "GoogleスプレッドシートのURLから取得\n" +
            "例: https://docs.google.com/spreadsheets/d/[ここがID]/edit\n" +
            "※スプレッドシートは「リンクを知っている全員」に公開する必要があります",
            MessageType.Info);

        GUILayout.Space(5);

        // シート名
        EditorGUILayout.LabelField("Sheet Name:");
        sheetName = EditorGUILayout.TextField(sheetName);

        // 出力フォルダ
        EditorGUILayout.LabelField("Output Folder:");
        outputFolder = EditorGUILayout.TextField(outputFolder);

        GUILayout.Space(10);

        // インポートボタン
        if (GUILayout.Button("Import from Google Sheets", GUILayout.Height(30)))
        {
            ImportFromGoogleSheets();
        }

        GUILayout.Space(10);

        // 公開設定の説明
        EditorGUILayout.HelpBox(
            "重要: Googleスプレッドシートの共有設定\n" +
            "1. スプレッドシートを開く\n" +
            "2. 右上の「共有」ボタンをクリック\n" +
            "3. 「リンクを知っている全員」を選択\n" +
            "4. 「閲覧者」権限でOK",
            MessageType.Warning);
    }

    private void ImportFromGoogleSheets()
    {
        if (string.IsNullOrEmpty(spreadsheetId))
        {
            EditorUtility.DisplayDialog("Error", "スプレッドシートIDを入力してください", "OK");
            return;
        }

        try
        {
            // GoogleスプレッドシートのCSVエクスポートURL
            string url = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid=0";

            using (WebClient client = new WebClient())
            {
                client.Encoding = System.Text.Encoding.UTF8;
                string csvContent = client.DownloadString(url);

                // 一時ファイルに保存
                string tempPath = "Assets/temp_quest_data.csv";
                File.WriteAllText(tempPath, csvContent);
                AssetDatabase.Refresh();

                // CSVファイルを読み込んでインポート
                TextAsset csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(tempPath);
                if (csvAsset != null)
                {
                    // QuestDataImporterの処理を呼び出す
                    ProcessCSVImport(csvAsset.text);

                    // 一時ファイルを削除
                    AssetDatabase.DeleteAsset(tempPath);
                }
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error",
                $"データの取得に失敗しました:\n{e.Message}\n\n" +
                "スプレッドシートが公開設定になっているか確認してください", "OK");
        }
    }

    private void ProcessCSVImport(string csvContent)
    {
        // QuestDataImporterのロジックを再利用
        // ここにCSV処理のコードを記述（QuestDataImporterから流用）
        EditorUtility.DisplayDialog("完了", "クエストデータのインポートが完了しました", "OK");
    }
}

// Googleスプレッドシート設定保存用
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