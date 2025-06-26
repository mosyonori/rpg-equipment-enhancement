using UnityEngine;
using UnityEditor;
using System.IO;

#if UNITY_EDITOR
public class JsonSaveDataImporter : EditorWindow
{
    [MenuItem("Tools/Save Data/JSON Importer")]
    public static void ShowWindow()
    {
        GetWindow<JsonSaveDataImporter>("JSON Save Data Importer");
    }

    private string jsonFilePath = "";
    private string jsonContent = "";
    private Vector2 scrollPosition;
    private bool showJsonPreview = false;

    private void OnGUI()
    {
        GUILayout.Label("JSONセーブデータインポーター", EditorStyles.boldLabel);
        GUILayout.Space(10);

        DrawFileSelection();
        GUILayout.Space(10);

        DrawJsonPreview();
        GUILayout.Space(10);

        DrawImportButtons();
        GUILayout.Space(10);

        DrawQuickJsonCreator();
    }

    private void DrawFileSelection()
    {
        GUILayout.Label("ファイル選択", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ファイルパス:", jsonFilePath);
        if (GUILayout.Button("参照", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("JSONファイル選択", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                jsonFilePath = path;
                LoadJsonFile();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("現在のセーブファイルを読み込み"))
        {
            LoadCurrentSaveFile();
        }
    }

    private void DrawJsonPreview()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("JSON プレビュー", EditorStyles.boldLabel);
        showJsonPreview = EditorGUILayout.Toggle("表示", showJsonPreview);
        EditorGUILayout.EndHorizontal();

        if (showJsonPreview && !string.IsNullOrEmpty(jsonContent))
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            EditorGUILayout.TextArea(jsonContent, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawImportButtons()
    {
        GUILayout.Label("インポート操作", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("データ検証"))
        {
            ValidateJsonData();
        }

        if (GUILayout.Button("利用可能ID確認"))
        {
            ShowAvailableIds();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("JSONからインポート"))
        {
            ImportFromJson();
        }

        if (GUILayout.Button("バックアップ作成後インポート"))
        {
            ImportWithBackup();
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("手動JSON入力"))
        {
            ShowManualInputDialog();
        }

        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("インポート後確認"))
        {
            CheckImportResult();
        }

        if (GUILayout.Button("強制データ再読み込み"))
        {
            ForceReloadData();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void CheckImportResult()
    {
        if (SaveDataManager.Instance?.CurrentSaveData == null)
        {
            EditorUtility.DisplayDialog("状態確認", "セーブデータが読み込まれていません", "OK");
            return;
        }

        var saveData = SaveDataManager.Instance.CurrentSaveData;
        string message = $@"現在のセーブデータ:
プレイヤー名: {saveData.playerName}
レベル: {saveData.playerLevel}
装備数: {saveData.equipments.Count}
アイテム数: {saveData.items.Count}
ゴールド: {saveData.gold:N0}";

        EditorUtility.DisplayDialog("インポート結果確認", message, "OK");
        Debug.Log("=== インポート結果確認 ===\n" + message);
    }

    private void ForceReloadData()
    {
        if (SaveDataManager.Instance != null)
        {
            SaveDataManager.Instance.LoadSaveData();

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RefreshCache();
            }

            EditorUtility.DisplayDialog("完了", "データを強制再読み込みしました", "OK");
            Debug.Log("データを強制再読み込みしました");
        }
    }

    private void ValidateJsonData()
    {
        if (string.IsNullOrEmpty(jsonContent))
        {
            EditorUtility.DisplayDialog("エラー", "JSONデータがありません", "OK");
            return;
        }

        try
        {
            var saveData = JsonUtility.FromJson<UserSaveData>(jsonContent);
            var result = SaveDataValidator.ValidateDetailedSaveData(saveData);

            EditorUtility.DisplayDialog("データ検証結果", result.GetSummary(), "OK");
            Debug.Log("=== セーブデータ検証結果 ===\n" + result.GetSummary());
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("エラー", $"JSON解析エラー: {e.Message}", "OK");
        }
    }

    private void ShowAvailableIds()
    {
        var availability = SaveDataValidator.CheckMasterDataAvailability();

        if (!availability.isAvailable)
        {
            EditorUtility.DisplayDialog("エラー", availability.errorMessage, "OK");
            return;
        }

        var availableIds = SaveDataValidator.GetAvailableDataIds();

        string message = $@"利用可能なマスターデータID:

【装備】({availableIds.equipmentIds.Count}件)
{string.Join(", ", availableIds.equipmentIds)}

【強化アイテム】({availableIds.enhanceItemIds.Count}件)
{string.Join(", ", availableIds.enhanceItemIds)}

【補助アイテム】({availableIds.supportItemIds.Count}件)
{string.Join(", ", availableIds.supportItemIds)}";

        EditorUtility.DisplayDialog("利用可能ID一覧", message, "OK");
        Debug.Log("=== 利用可能マスターデータID ===\n" + message);
    }

    private void DrawQuickJsonCreator()
    {
        GUILayout.Label("クイック作成", EditorStyles.boldLabel);

        if (GUILayout.Button("サンプルJSONを作成"))
        {
            CreateSampleJson();
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("リッチプレイヤー"))
        {
            CreateRichPlayerJson();
        }

        if (GUILayout.Button("初心者プレイヤー"))
        {
            CreateBeginnerPlayerJson();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void LoadJsonFile()
    {
        try
        {
            if (File.Exists(jsonFilePath))
            {
                jsonContent = File.ReadAllText(jsonFilePath);
                Debug.Log($"JSONファイルを読み込みました: {jsonFilePath}");
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("エラー", $"ファイル読み込みエラー: {e.Message}", "OK");
        }
    }

    private void LoadCurrentSaveFile()
    {
        if (SaveDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("エラー", "SaveDataManagerが見つかりません", "OK");
            return;
        }

        string savePath = SaveDataManager.Instance.SaveFilePath;
        if (File.Exists(savePath))
        {
            jsonFilePath = savePath;
            LoadJsonFile();
        }
        else
        {
            EditorUtility.DisplayDialog("エラー", "セーブファイルが見つかりません", "OK");
        }
    }

    private void ImportFromJson()
    {
        if (string.IsNullOrEmpty(jsonContent))
        {
            EditorUtility.DisplayDialog("エラー", "JSONデータがありません", "OK");
            return;
        }

        try
        {
            var saveData = JsonUtility.FromJson<UserSaveData>(jsonContent);

            if (saveData == null)
            {
                EditorUtility.DisplayDialog("エラー", "JSONの解析に失敗しました", "OK");
                return;
            }

            // データ整合性をチェック
            var errors = UserDataUtility.ValidateUserData(saveData);
            if (errors.Count > 0)
            {
                string errorMessage = "データに問題があります:\n" + string.Join("\n", errors.ToArray());
                bool proceed = EditorUtility.DisplayDialog("警告", errorMessage + "\n\n続行しますか？", "はい", "いいえ");
                if (!proceed) return;
            }

            // セーブデータを設定
            if (SaveDataManager.Instance != null)
            {
                SaveDataManager.Instance.SetSaveData(saveData);
                SaveDataManager.Instance.SaveSaveData();

                // InventoryManagerのキャッシュを更新
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.RefreshCache();
                    Debug.Log("InventoryManagerのキャッシュを手動更新しました");
                }

                // データを再読み込みして確実に反映
                SaveDataManager.Instance.LoadSaveData();
                Debug.Log("セーブデータを再読み込みしました");

                EditorUtility.DisplayDialog("完了", "JSONからセーブデータをインポートしました", "OK");
                Debug.Log("JSONからセーブデータをインポートしました");
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("エラー", $"インポートエラー: {e.Message}", "OK");
        }
    }

    private void ImportWithBackup()
    {
        if (SaveDataManager.Instance != null && SaveDataManager.Instance.SaveFileExists())
        {
            // バックアップ作成
            SaveDataManager.Instance.SaveSaveData(); // これによりバックアップが作成される
        }

        ImportFromJson();
    }

    private void ShowManualInputDialog()
    {
        // 手動JSON入力用のウィンドウを表示
        ManualJsonInputWindow.ShowWindow();
    }

    private void CreateSampleJson()
    {
        var sampleData = UserDataUtility.CreateNewUserData("サンプルプレイヤー");

        // サンプル装備追加
        var sampleEquipment = new UserEquipmentData
        {
            equipmentMasterId = 1,
            currentEnhancedValue = 3,
            currentEnhanceStamina = 97,
            isLocked = true,
            isFavorite = true
        };
        sampleData.AddEquipment(sampleEquipment);

        jsonContent = JsonUtility.ToJson(sampleData, true);
        Debug.Log("サンプルJSONを作成しました");
    }

    private void CreateRichPlayerJson()
    {
        var richPlayer = UserDataUtility.CreateNewUserData("リッチプレイヤー");
        richPlayer.playerLevel = 50;
        richPlayer.gold = 1000000;
        richPlayer.gems = 5000;
        richPlayer.totalExp = 500000;

        // 高級装備を追加
        for (int i = 1; i <= 5; i++)
        {
            var equipment = new UserEquipmentData
            {
                equipmentMasterId = i,
                currentEnhancedValue = 15,
                currentEnhanceStamina = 85,
                enhancedOffense = 30,
                enhancedHp = 75,
                enhancedDefense = 15,
                isFavorite = true
            };
            richPlayer.AddEquipment(equipment);
        }

        jsonContent = JsonUtility.ToJson(richPlayer, true);
        Debug.Log("リッチプレイヤーJSONを作成しました");
    }

    private void CreateBeginnerPlayerJson()
    {
        var beginner = UserDataUtility.CreateNewUserData("初心者プレイヤー");
        beginner.playerLevel = 1;
        beginner.gold = 500;
        beginner.gems = 10;

        // 基本装備のみ
        var basicEquipment = new UserEquipmentData
        {
            equipmentMasterId = 1,
            currentEnhancedValue = 0,
            currentEnhanceStamina = 100
        };
        beginner.AddEquipment(basicEquipment);

        jsonContent = JsonUtility.ToJson(beginner, true);
        Debug.Log("初心者プレイヤーJSONを作成しました");
    }
}

public class ManualJsonInputWindow : EditorWindow
{
    private string manualJsonInput = "";
    private Vector2 scrollPosition;

    public static void ShowWindow()
    {
        GetWindow<ManualJsonInputWindow>("Manual JSON Input");
    }

    private void OnGUI()
    {
        GUILayout.Label("手動JSON入力", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("JSONを直接入力してください:");
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        manualJsonInput = EditorGUILayout.TextArea(manualJsonInput, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("インポート"))
        {
            ImportManualJson();
        }

        if (GUILayout.Button("キャンセル"))
        {
            Close();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void ImportManualJson()
    {
        if (string.IsNullOrEmpty(manualJsonInput))
        {
            EditorUtility.DisplayDialog("エラー", "JSONが入力されていません", "OK");
            return;
        }

        try
        {
            var saveData = JsonUtility.FromJson<UserSaveData>(manualJsonInput);

            if (SaveDataManager.Instance != null)
            {
                SaveDataManager.Instance.SetSaveData(saveData);
                SaveDataManager.Instance.SaveSaveData();

                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.RefreshCache();
                }

                // データを再読み込みして確実に反映
                SaveDataManager.Instance.LoadSaveData();

                EditorUtility.DisplayDialog("完了", "手動JSONからセーブデータをインポートしました", "OK");
                Close();
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("エラー", $"JSON解析エラー: {e.Message}", "OK");
        }
    }
}
#endif