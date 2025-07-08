#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// 戦闘テスト用UIを自動生成するエディタスクリプト
/// メニューから実行してテスト用UIを作成
/// </summary>
public class BattleTestUIPrefabCreator : EditorWindow
{
    [MenuItem("Tools/Battle System/Create Battle Test UI")]
    public static void CreateBattleTestUI()
    {
        // Canvas作成または取得
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // EventSystem作成
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // メインパネル作成
        GameObject mainPanel = CreatePanel("BattleTestPanel", canvas.transform);

        // ヘッダーパネル作成
        GameObject headerPanel = CreatePanel("HeaderPanel", mainPanel.transform);
        SetLayoutGroup(headerPanel, typeof(HorizontalLayoutGroup));

        CreateText("TitleText", "戦闘システムテスト", headerPanel.transform, 24, Color.white);
        CreateText("BattleStateText", "戦闘待機中", headerPanel.transform, 18, Color.yellow);

        // 情報パネル作成
        GameObject infoPanel = CreatePanel("InfoPanel", mainPanel.transform);
        SetLayoutGroup(infoPanel, typeof(VerticalLayoutGroup));

        CreateText("TurnInfoText", "ターン: 0", infoPanel.transform, 16, Color.white);
        CreateText("PlayerHPText", "プレイヤーHP: ---", infoPanel.transform, 16, Color.green);
        CreateText("EnemyHPText", "敵HP: ---", infoPanel.transform, 16, Color.red);

        // コントロールパネル作成
        GameObject controlPanel = CreatePanel("ControlPanel", mainPanel.transform);
        SetLayoutGroup(controlPanel, typeof(HorizontalLayoutGroup));

        CreateButton("StartBattleButton", "戦闘開始", controlPanel.transform);
        CreateButton("SpeedToggleButton", "速度変更", controlPanel.transform);
        CreateText("SpeedText", "速度: 1x", controlPanel.transform, 14, Color.cyan);

        // ログパネル作成
        GameObject logPanel = CreatePanel("LogPanel", mainPanel.transform);
        CreateText("LogText", "コンソールでログを確認してください", logPanel.transform, 12, Color.gray);

        // BattleTestUIコンポーネント追加とアサイン
        SetupBattleTestUI(mainPanel);

        Debug.Log("戦闘テスト用UIを作成しました！");
    }

    private static GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.1f); // 半透明背景

        return panel;
    }

    private static GameObject CreateText(string name, string text, Transform parent, int fontSize = 16, Color? color = null)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);

        RectTransform rect = textGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 30);

        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = color ?? Color.white;
        textComponent.alignment = TextAlignmentOptions.Left;

        return textGO;
    }

    private static GameObject CreateButton(string name, string buttonText, Transform parent)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 40);

        Image image = buttonGO.AddComponent<Image>();
        image.color = new Color(0.2f, 0.3f, 0.5f, 1f);

        Button button = buttonGO.AddComponent<Button>();

        // ボタンテキスト作成
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = buttonText;
        textComponent.fontSize = 14;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;

        return buttonGO;
    }

    private static void SetLayoutGroup(GameObject target, System.Type layoutType)
    {
        if (layoutType == typeof(HorizontalLayoutGroup))
        {
            var hlg = target.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlHeight = false;
            hlg.childControlWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.spacing = 10;
            hlg.padding = new RectOffset(10, 10, 5, 5);
        }
        else if (layoutType == typeof(VerticalLayoutGroup))
        {
            var vlg = target.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.spacing = 5;
            vlg.padding = new RectOffset(10, 10, 5, 5);
        }

        ContentSizeFitter fitter = target.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void SetupBattleTestUI(GameObject mainPanel)
    {
        // BattleTestUIコンポーネント追加
        BattleTestUI testUI = mainPanel.AddComponent<BattleTestUI>();

        // UI要素の参照をアサイン
        Transform headerPanel = mainPanel.transform.Find("HeaderPanel");
        Transform infoPanel = mainPanel.transform.Find("InfoPanel");
        Transform controlPanel = mainPanel.transform.Find("ControlPanel");

        if (headerPanel != null)
        {
            var battleStateText = headerPanel.transform.Find("BattleStateText")?.GetComponent<TextMeshProUGUI>();
            if (battleStateText != null)
            {
                SetPrivateField(testUI, "battleStateText", battleStateText);
            }
        }

        if (infoPanel != null)
        {
            var turnInfoText = infoPanel.transform.Find("TurnInfoText")?.GetComponent<TextMeshProUGUI>();
            var playerHPText = infoPanel.transform.Find("PlayerHPText")?.GetComponent<TextMeshProUGUI>();
            var enemyHPText = infoPanel.transform.Find("EnemyHPText")?.GetComponent<TextMeshProUGUI>();

            if (turnInfoText != null) SetPrivateField(testUI, "turnInfoText", turnInfoText);
            if (playerHPText != null) SetPrivateField(testUI, "playerHPText", playerHPText);
            if (enemyHPText != null) SetPrivateField(testUI, "enemyHPText", enemyHPText);
        }

        if (controlPanel != null)
        {
            var startButton = controlPanel.transform.Find("StartBattleButton")?.GetComponent<Button>();
            var speedButton = controlPanel.transform.Find("SpeedToggleButton")?.GetComponent<Button>();
            var speedText = controlPanel.transform.Find("SpeedText")?.GetComponent<TextMeshProUGUI>();

            if (startButton != null) SetPrivateField(testUI, "startBattleButton", startButton);
            if (speedButton != null) SetPrivateField(testUI, "speedToggleButton", speedButton);
            if (speedText != null) SetPrivateField(testUI, "speedText", speedText);
        }

        // testQuestIdのデフォルト値設定
        SetPrivateField(testUI, "testQuestId", 1);

        Debug.Log("BattleTestUIコンポーネントの設定完了");
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(target, value);
        }
        else
        {
            Debug.LogWarning($"フィールド '{fieldName}' が見つかりませんでした");
        }
    }

    [MenuItem("Tools/Battle System/Create Test Quest Data")]
    public static void CreateTestQuestData()
    {
        // テスト用のクエストデータ作成
        Debug.Log("テスト用クエストデータの作成を開始します");
        Debug.Log("MasterDataManagerでテスト用データを設定してください：");
        Debug.Log("- QuestMasterData (ID: 1)");
        Debug.Log("- MonsterMasterData (基本的なスライムなど)");
        Debug.Log("- CharacterMasterData (プレイヤーキャラクター)");
        Debug.Log("- SkillMasterData (基本的な攻撃スキル)");

        EditorUtility.DisplayDialog("テストデータ作成",
            "MasterDataManagerでテスト用データを設定してください\n" +
            "詳細はコンソールログを確認してください", "OK");
    }

    [MenuItem("Tools/Battle System/Validate Battle System")]
    public static void ValidateBattleSystem()
    {
        Debug.Log("========== 戦闘システム検証開始 ==========");

        bool isValid = true;

        // 必要なManagerの存在確認
        if (FindFirstObjectByType<BattleManager>() == null)
        {
            Debug.LogError("BattleManagerが見つかりません");
            isValid = false;
        }
        else
        {
            Debug.Log("✓ BattleManager確認済み");
        }

        if (FindFirstObjectByType<SaveDataManager>() == null)
        {
            Debug.LogError("SaveDataManagerが見つかりません");
            isValid = false;
        }
        else
        {
            Debug.Log("✓ SaveDataManager確認済み");
        }

        if (FindFirstObjectByType<MasterDataManager>() == null)
        {
            Debug.LogError("MasterDataManagerが見つかりません");
            isValid = false;
        }
        else
        {
            Debug.Log("✓ MasterDataManager確認済み");
        }

        // BattleTestUIの存在確認
        if (FindFirstObjectByType<BattleTestUI>() == null)
        {
            Debug.LogWarning("BattleTestUIが見つかりません（まだ作成されていない可能性があります）");
        }
        else
        {
            Debug.Log("✓ BattleTestUI確認済み");
        }

        if (isValid)
        {
            Debug.Log("========== 戦闘システム検証完了: 正常 ==========");
            EditorUtility.DisplayDialog("検証完了", "戦闘システムの基盤は正常に設定されています", "OK");
        }
        else
        {
            Debug.LogError("========== 戦闘システム検証完了: エラーあり ==========");
            EditorUtility.DisplayDialog("検証エラー", "戦闘システムに問題があります。コンソールを確認してください", "OK");
        }
    }
}
#endif