using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 戦闘ログの表示制御UI管理
/// 役割：戦闘中の行動ログ・ダメージログの表示管理
/// 機能：行動ログ追加表示、ダメージ数値ログ、自動スクロール制御
/// データアクセス統一ルール: UI層 → Manager層 → データ層
/// </summary>
public class BattleLogUI : MonoBehaviour
{
    [Header("ログウィンドウ全体")]
    [SerializeField] private GameObject logWindowRoot;
    [SerializeField] private CanvasGroup logCanvasGroup;
    [SerializeField] private Button toggleLogButton;

    [Header("ログリスト表示")]
    [SerializeField] private ScrollRect logScrollRect;
    [SerializeField] private Transform logContentParent;
    [SerializeField] private RectTransform logContentRectTransform;

    [Header("ログエントリープレハブ")]
    [SerializeField] private GameObject normalLogEntryPrefab;
    [SerializeField] private GameObject damageLogEntryPrefab;
    [SerializeField] private GameObject criticalLogEntryPrefab;
    [SerializeField] private GameObject statusEffectLogEntryPrefab;

    [Header("ログ表示設定")]
    [SerializeField] private int maxLogEntries = 50;
    [SerializeField] private float logEntryFadeInDuration = 0.3f;
    [SerializeField] private float autoScrollDelay = 0.1f;
    [SerializeField] private bool enableAutoScroll = true;
    [SerializeField] private bool showDetailedDamage = true;

    [Header("色設定")]
    [SerializeField] private Color playerActionColor = new Color(0.2f, 0.6f, 1f, 1f);     // 青
    [SerializeField] private Color enemyActionColor = new Color(1f, 0.4f, 0.2f, 1f);      // 赤
    [SerializeField] private Color damageColor = new Color(1f, 0.3f, 0.3f, 1f);           // ダメージ赤
    [SerializeField] private Color healColor = new Color(0.3f, 1f, 0.3f, 1f);             // 回復緑
    [SerializeField] private Color criticalColor = new Color(1f, 0.8f, 0f, 1f);           // クリティカル黄
    [SerializeField] private Color statusEffectColor = new Color(0.8f, 0.2f, 1f, 1f);     // 状態異常紫

    [Header("フィルター設定")]
    [SerializeField] private Toggle showPlayerActionsToggle;
    [SerializeField] private Toggle showEnemyActionsToggle;
    [SerializeField] private Toggle showDamageToggle;
    [SerializeField] private Toggle showStatusEffectsToggle;
    [SerializeField] private Button clearLogButton;

    [Header("アニメーション設定")]
    [SerializeField] private float windowFadeInDuration = 0.3f;
    [SerializeField] private float windowFadeOutDuration = 0.2f;
    [SerializeField] private AnimationCurve fadeEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // イベント
    public static event Action<bool> OnLogVisibilityChanged;

    // 内部状態
    private bool isInitialized = false;
    private bool isLogVisible = false;
    private List<GameObject> logEntryObjects;
    private Queue<ActionData> pendingLogEntries;
    private Coroutine autoScrollCoroutine;
    private Coroutine fadeCoroutine;

    // フィルター状態
    private bool showPlayerActions = true;
    private bool showEnemyActions = true;
    private bool showDamageInfo = true;
    private bool showStatusEffectInfo = true;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeCollections();
        ValidateComponents();
    }

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// UIコンポーネントの初期化
    /// </summary>
    public void Initialize()
    {
        try
        {
            Log("BattleLogUI初期化開始");

            // コレクション初期化
            InitializeCollections();

            // ボタンイベント登録
            RegisterButtonEvents();

            // 初期状態設定
            if (logWindowRoot != null)
                logWindowRoot.SetActive(false);

            if (logCanvasGroup != null)
            {
                logCanvasGroup.alpha = 0f;
                logCanvasGroup.interactable = false;
                logCanvasGroup.blocksRaycasts = false;
            }

            // スクロールビューの設定
            SetupScrollView();

            // フィルター初期化
            InitializeFilters();

            isLogVisible = false;
            isInitialized = true;

            Log("BattleLogUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"BattleLogUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// コレクション初期化
    /// </summary>
    private void InitializeCollections()
    {
        logEntryObjects = new List<GameObject>();
        pendingLogEntries = new Queue<ActionData>();
    }

    /// <summary>
    /// コンポーネント検証
    /// </summary>
    private void ValidateComponents()
    {
        if (logScrollRect == null)
            LogWarning("logScrollRectが設定されていません");

        if (logContentParent == null)
            LogWarning("logContentParentが設定されていません");

        if (normalLogEntryPrefab == null)
            LogWarning("normalLogEntryPrefab が設定されていません");
    }

    /// <summary>
    /// ボタンイベント登録
    /// </summary>
    private void RegisterButtonEvents()
    {
        if (toggleLogButton != null)
            toggleLogButton.onClick.AddListener(ToggleLogVisibility);

        if (clearLogButton != null)
            clearLogButton.onClick.AddListener(ClearLog);

        // フィルタートグルイベント登録
        if (showPlayerActionsToggle != null)
            showPlayerActionsToggle.onValueChanged.AddListener(OnPlayerActionsFilterChanged);

        if (showEnemyActionsToggle != null)
            showEnemyActionsToggle.onValueChanged.AddListener(OnEnemyActionsFilterChanged);

        if (showDamageToggle != null)
            showDamageToggle.onValueChanged.AddListener(OnDamageFilterChanged);

        if (showStatusEffectsToggle != null)
            showStatusEffectsToggle.onValueChanged.AddListener(OnStatusEffectsFilterChanged);
    }

    /// <summary>
    /// スクロールビュー設定
    /// </summary>
    private void SetupScrollView()
    {
        if (logScrollRect != null)
        {
            logScrollRect.movementType = ScrollRect.MovementType.Clamped;
            logScrollRect.scrollSensitivity = 20f;
        }

        if (logContentRectTransform == null && logContentParent != null)
            logContentRectTransform = logContentParent.GetComponent<RectTransform>();
    }

    /// <summary>
    /// フィルター初期化
    /// </summary>
    private void InitializeFilters()
    {
        if (showPlayerActionsToggle != null)
            showPlayerActionsToggle.isOn = showPlayerActions;

        if (showEnemyActionsToggle != null)
            showEnemyActionsToggle.isOn = showEnemyActions;

        if (showDamageToggle != null)
            showDamageToggle.isOn = showDamageInfo;

        if (showStatusEffectsToggle != null)
            showStatusEffectsToggle.isOn = showStatusEffectInfo;
    }

    #endregion

    #region 公開メソッド - イベントハンドラ

    /// <summary>
    /// 戦闘開始時の処理
    /// </summary>
    public void OnBattleStart(BattleSetupData setupData)
    {
        if (!isInitialized) Initialize();

        try
        {
            Log("戦闘開始 - 戦闘ログUIクリア");

            // ログをクリア
            ClearLog();

            // 戦闘開始メッセージ追加
            AddBattleStartMessage(setupData);

            Log("戦闘ログUI準備完了");
        }
        catch (Exception e)
        {
            LogError($"戦闘開始処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// アクション実行時の処理
    /// </summary>
    public void OnActionExecuted(ActionData action)
    {
        try
        {
            if (action == null) return;

            // フィルター条件をチェック
            if (!ShouldShowAction(action)) return;

            // ログエントリー追加
            AddLogEntry(action);

            Log($"アクションログ追加: {action.GetActionSummary()}");
        }
        catch (Exception e)
        {
            LogError($"アクション実行処理エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - ログ制御

    /// <summary>
    /// ログウィンドウの表示切替
    /// </summary>
    public void ToggleLogVisibility()
    {
        try
        {
            if (isLogVisible)
                HideLog();
            else
                ShowLog();
        }
        catch (Exception e)
        {
            LogError($"ログ表示切替エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ログウィンドウ表示
    /// </summary>
    public void ShowLog()
    {
        if (isLogVisible) return;

        try
        {
            if (logWindowRoot != null)
                logWindowRoot.SetActive(true);

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeInLogWindow());
            isLogVisible = true;

            OnLogVisibilityChanged?.Invoke(true);
            Log("ログウィンドウ表示");
        }
        catch (Exception e)
        {
            LogError($"ログ表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ログウィンドウ非表示
    /// </summary>
    public void HideLog()
    {
        if (!isLogVisible) return;

        try
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeOutLogWindow());
            isLogVisible = false;

            OnLogVisibilityChanged?.Invoke(false);
            Log("ログウィンドウ非表示");
        }
        catch (Exception e)
        {
            LogError($"ログ非表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ログをクリア
    /// </summary>
    public void ClearLog()
    {
        try
        {
            // 既存のログエントリーを削除
            foreach (var logEntry in logEntryObjects)
            {
                if (logEntry != null)
                    DestroyImmediate(logEntry);
            }

            logEntryObjects.Clear();
            pendingLogEntries.Clear();

            // スクロール位置をリセット
            if (logScrollRect != null)
                logScrollRect.verticalNormalizedPosition = 1f;

            Log("戦闘ログクリア完了");
        }
        catch (Exception e)
        {
            LogError($"ログクリアエラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - ログエントリー管理

    /// <summary>
    /// 戦闘開始メッセージ追加
    /// </summary>
    private void AddBattleStartMessage(BattleSetupData setupData)
    {
        try
        {
            // クエスト名を取得
            var questData = QuestDataManager.Instance?.GetQuestData(setupData.questId);
            string questName = questData?.questName ?? "不明なクエスト";

            // 戦闘開始メッセージ作成
            var startMessage = CreateBattleStartLogEntry($"戦闘開始: {questName}");
            if (startMessage != null)
            {
                AddLogEntryObject(startMessage);
            }
        }
        catch (Exception e)
        {
            LogError($"戦闘開始メッセージ追加エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ログエントリー追加
    /// </summary>
    private void AddLogEntry(ActionData action)
    {
        try
        {
            GameObject logEntry = null;

            // アクションタイプに応じてログエントリー作成
            if (action.IsNormalAttack() || action.IsSkillUse())
            {
                logEntry = CreateActionLogEntry(action);
            }

            if (logEntry != null)
            {
                AddLogEntryObject(logEntry);
            }

            // ダメージ詳細ログも追加
            if (showDetailedDamage && action.damageResults.Count > 0)
            {
                foreach (var damage in action.damageResults)
                {
                    var damageEntry = CreateDamageLogEntry(damage, action.isPlayerAction);
                    if (damageEntry != null)
                    {
                        AddLogEntryObject(damageEntry);
                    }
                }
            }
        }
        catch (Exception e)
        {
            LogError($"ログエントリー追加エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ログエントリーオブジェクト追加
    /// </summary>
    private void AddLogEntryObject(GameObject logEntry)
    {
        if (logEntry == null || logContentParent == null) return;

        try
        {
            // 最大エントリー数チェック
            if (logEntryObjects.Count >= maxLogEntries)
            {
                var oldestEntry = logEntryObjects[0];
                logEntryObjects.RemoveAt(0);
                DestroyImmediate(oldestEntry);
            }

            // 新しいエントリーを追加
            logEntry.transform.SetParent(logContentParent, false);
            logEntryObjects.Add(logEntry);

            // フェードインアニメーション
            StartCoroutine(FadeInLogEntry(logEntry));

            // 自動スクロール
            if (enableAutoScroll)
            {
                if (autoScrollCoroutine != null)
                    StopCoroutine(autoScrollCoroutine);
                autoScrollCoroutine = StartCoroutine(AutoScrollToBottom());
            }
        }
        catch (Exception e)
        {
            LogError($"ログエントリーオブジェクト追加エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - ログエントリー作成

    /// <summary>
    /// 戦闘開始ログエントリー作成
    /// </summary>
    private GameObject CreateBattleStartLogEntry(string message)
    {
        if (normalLogEntryPrefab == null) return null;

        try
        {
            GameObject entry = Instantiate(normalLogEntryPrefab);
            SetupLogEntryText(entry, message, playerActionColor, true);
            return entry;
        }
        catch (Exception e)
        {
            LogError($"戦闘開始ログエントリー作成エラー: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// アクションログエントリー作成
    /// </summary>
    private GameObject CreateActionLogEntry(ActionData action)
    {
        GameObject prefab = null;
        Color textColor = playerActionColor;

        // アクションタイプとプレイヤーかどうかで色とプレハブを決定
        if (action.GetCriticalCount() > 0 && criticalLogEntryPrefab != null)
        {
            prefab = criticalLogEntryPrefab;
            textColor = criticalColor;
        }
        else
        {
            prefab = normalLogEntryPrefab;
            textColor = action.isPlayerAction ? playerActionColor : enemyActionColor;
        }

        if (prefab == null) return null;

        try
        {
            GameObject entry = Instantiate(prefab);
            string logText = action.GetActionSummary();
            SetupLogEntryText(entry, logText, textColor);

            return entry;
        }
        catch (Exception e)
        {
            LogError($"アクションログエントリー作成エラー: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// ダメージログエントリー作成
    /// </summary>
    private GameObject CreateDamageLogEntry(DamageData damage, bool isPlayerAction)
    {
        GameObject prefab = null;
        Color textColor = damageColor;

        // ダメージタイプに応じてプレハブと色を決定
        if (damage.isCritical && criticalLogEntryPrefab != null)
        {
            prefab = criticalLogEntryPrefab;
            textColor = criticalColor;
        }
        else if (damage.IsHealing())
        {
            prefab = normalLogEntryPrefab;
            textColor = healColor;
        }
        else if (damageLogEntryPrefab != null)
        {
            prefab = damageLogEntryPrefab;
            textColor = damageColor;
        }
        else
        {
            prefab = normalLogEntryPrefab;
        }

        if (prefab == null) return null;

        try
        {
            GameObject entry = Instantiate(prefab);
            string logText = $"  → {damage.ToString()}";
            SetupLogEntryText(entry, logText, textColor);

            return entry;
        }
        catch (Exception e)
        {
            LogError($"ダメージログエントリー作成エラー: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// ログエントリーテキスト設定
    /// </summary>
    private void SetupLogEntryText(GameObject entry, string text, Color color, bool isBold = false)
    {
        if (entry == null) return;

        try
        {
            var textComponent = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
                textComponent.color = color;

                if (isBold)
                {
                    textComponent.fontStyle = FontStyles.Bold;
                }
            }

            // タイムスタンプ追加（オプション）
            var timeText = GetTimeStampText();
            if (!string.IsNullOrEmpty(timeText))
            {
                textComponent.text = $"[{timeText}] {text}";
            }
        }
        catch (Exception e)
        {
            LogError($"ログエントリーテキスト設定エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - フィルター処理

    /// <summary>
    /// アクション表示判定
    /// </summary>
    private bool ShouldShowAction(ActionData action)
    {
        if (action.isPlayerAction && !showPlayerActions) return false;
        if (!action.isPlayerAction && !showEnemyActions) return false;

        return true;
    }

    #endregion

    #region 内部メソッド - アニメーション

    /// <summary>
    /// ログウィンドウフェードイン
    /// </summary>
    private IEnumerator FadeInLogWindow()
    {
        if (logCanvasGroup == null) yield break;

        float elapsed = 0f;
        logCanvasGroup.interactable = true;
        logCanvasGroup.blocksRaycasts = true;

        while (elapsed < windowFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / windowFadeInDuration;
            float curveValue = fadeEasing.Evaluate(t);
            logCanvasGroup.alpha = curveValue;
            yield return null;
        }

        logCanvasGroup.alpha = 1f;
        fadeCoroutine = null;
    }

    /// <summary>
    /// ログウィンドウフェードアウト
    /// </summary>
    private IEnumerator FadeOutLogWindow()
    {
        if (logCanvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = logCanvasGroup.alpha;
        logCanvasGroup.interactable = false;
        logCanvasGroup.blocksRaycasts = false;

        while (elapsed < windowFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / windowFadeOutDuration;
            float curveValue = fadeEasing.Evaluate(t);
            logCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, curveValue);
            yield return null;
        }

        logCanvasGroup.alpha = 0f;

        if (logWindowRoot != null)
            logWindowRoot.SetActive(false);

        fadeCoroutine = null;
    }

    /// <summary>
    /// ログエントリーフェードイン
    /// </summary>
    private IEnumerator FadeInLogEntry(GameObject entry)
    {
        if (entry == null) yield break;

        var canvasGroup = entry.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = entry.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < logEntryFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / logEntryFadeInDuration;
            canvasGroup.alpha = t;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 自動スクロール（最下部へ）
    /// </summary>
    private IEnumerator AutoScrollToBottom()
    {
        yield return new WaitForSeconds(autoScrollDelay);

        if (logScrollRect != null)
        {
            // 強制的に最下部にスクロール
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }

        autoScrollCoroutine = null;
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// プレイヤーアクションフィルター変更
    /// </summary>
    private void OnPlayerActionsFilterChanged(bool value)
    {
        showPlayerActions = value;
        RefreshLogDisplay();
    }

    /// <summary>
    /// 敵アクションフィルター変更
    /// </summary>
    private void OnEnemyActionsFilterChanged(bool value)
    {
        showEnemyActions = value;
        RefreshLogDisplay();
    }

    /// <summary>
    /// ダメージフィルター変更
    /// </summary>
    private void OnDamageFilterChanged(bool value)
    {
        showDamageInfo = value;
        RefreshLogDisplay();
    }

    /// <summary>
    /// 状態異常フィルター変更
    /// </summary>
    private void OnStatusEffectsFilterChanged(bool value)
    {
        showStatusEffectInfo = value;
        RefreshLogDisplay();
    }

    /// <summary>
    /// ログ表示更新（フィルター適用）
    /// </summary>
    private void RefreshLogDisplay()
    {
        // 実装が複雑になるため、現在は単純にクリア
        // 将来的にはフィルター条件に応じて表示/非表示を切り替え
        Log("ログフィルター変更 - 表示更新");
    }

    #endregion

    #region ユーティリティ

    /// <summary>
    /// タイムスタンプテキスト取得
    /// </summary>
    private string GetTimeStampText()
    {
        if (BattleManager.Instance != null)
        {
            float elapsedTime = BattleManager.Instance.GetBattleElapsedTime();
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            return $"{minutes:00}:{seconds:00}";
        }
        return "";
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        Debug.Log($"[BattleLogUI] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[BattleLogUI] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[BattleLogUI] {message}");
    }

    #endregion

    #region エディタ用ツール

#if UNITY_EDITOR
    [ContextMenu("ログ表示テスト")]
    private void TestShowLog()
    {
        ShowLog();
        Log("ログ表示テスト実行");
    }

    [ContextMenu("ログ非表示テスト")]
    private void TestHideLog()
    {
        HideLog();
        Log("ログ非表示テスト実行");
    }

    [ContextMenu("ダミーログ追加テスト")]
    private void TestAddDummyLog()
    {
        // テスト用のActionData作成
        var testAction = ActionData.CreateNormalAttack(
            "test_player", "テストプレイヤー", true,
            "test_enemy", "テスト敵", 1
        );

        OnActionExecuted(testAction);
        Log("ダミーログ追加テスト実行");
    }
#endif

    #endregion
}