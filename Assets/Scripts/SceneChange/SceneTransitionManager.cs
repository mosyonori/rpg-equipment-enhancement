using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーン遷移の統合管理クラス
/// データアクセス統一ルール: UI層 → SceneTransitionManager → データ層
/// 自動保存、遷移バリデーション、エラーハンドリングを提供
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    #region Events

    /// <summary>
    /// 遷移開始時のイベント
    /// </summary>
    public static event Action<string, string> OnTransitionStarted; // (from, to)

    /// <summary>
    /// 遷移完了時のイベント
    /// </summary>
    public static event Action<string> OnTransitionCompleted;

    /// <summary>
    /// 遷移エラー時のイベント
    /// </summary>
    public static event Action<string, string> OnTransitionError; // (sceneName, errorMessage)

    #endregion

    #region Properties

    public static SceneTransitionManager Instance { get; private set; }

    /// <summary>
    /// 遷移中かどうか
    /// </summary>
    public bool IsTransitioning { get; private set; }

    /// <summary>
    /// 自動保存が有効かどうか
    /// </summary>
    public bool AutoSaveEnabled { get; set; } = true;

    #endregion

    #region Private Fields

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private float transitionTimeout = 30f; // 遷移タイムアウト時間
    [SerializeField] private bool validateDependencies = true; // 依存関係チェック

    private Coroutine currentTransitionCoroutine;

    #endregion

    #region Singleton Pattern

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// マネージャー初期化
    /// </summary>
    private void InitializeManager()
    {
        LogDebug("SceneTransitionManager初期化完了");
    }

    #endregion

    #region Public Methods - Scene Transition

    /// <summary>
    /// 指定シーンに遷移
    /// </summary>
    /// <param name="targetSceneName">遷移先シーン名</param>
    /// <param name="onComplete">遷移完了時のコールバック</param>
    public void TransitionToScene(string targetSceneName, Action onComplete = null)
    {
        if (IsTransitioning)
        {
            LogWarning("既に遷移中です");
            return;
        }

        if (!ValidateSceneTransition(targetSceneName))
        {
            return;
        }

        currentTransitionCoroutine = StartCoroutine(TransitionCoroutine(targetSceneName, onComplete));
    }

    /// <summary>
    /// ホームシーンに遷移
    /// </summary>
    public void TransitionToHome()
    {
        TransitionToScene(SceneNames.HOME);
    }

    /// <summary>
    /// タイトルシーンに遷移
    /// </summary>
    public void TransitionToTitle()
    {
        TransitionToScene(SceneNames.TITLE);
    }

    /// <summary>
    /// 装備編集シーンに遷移
    /// </summary>
    public void TransitionToEquipmentEdit()
    {
        TransitionToScene(SceneNames.EQUIPMENT_EDIT);
    }

    /// <summary>
    /// 装備強化シーンに遷移
    /// </summary>
    public void TransitionToEquipmentEnhance()
    {
        TransitionToScene(SceneNames.EQUIPMENT_ENHANCE);
    }

    /// <summary>
    /// クエスト戦闘シーンに遷移
    /// </summary>
    public void TransitionToQuestBattle()
    {
        LogDebug("クエスト戦闘シーンに遷移します");

        // 修正: より詳細なデバッグログ
        int selectedQuestId = QuestSelectionData.GetSelectedQuestId();
        bool hasValidQuest = QuestSelectionData.HasValidQuest();

        LogDebug($"選択されたクエストID: {selectedQuestId}");
        LogDebug($"有効なクエストが選択されているか: {hasValidQuest}");

        // 選択されたクエストが有効かチェック
        if (!hasValidQuest)
        {
            LogError($"有効なクエストが選択されていません - questId={selectedQuestId}");
            LogError("QuestDetailUIでSetSelectedQuest()が正しく呼ばれているか確認してください");

            // 修正: デバッグ情報を追加
            LogError($"QuestSelectionData.GetSelectedQuestId() = {QuestSelectionData.GetSelectedQuestId()}");
            LogError($"QuestSelectionData.HasValidQuest() = {QuestSelectionData.HasValidQuest()}");

            return;
        }

        LogDebug($"選択されたクエストID: {selectedQuestId}");

        // 実際のシーン名「BattleScene」に修正
        TransitionToScene("BattleScene");
    }

    /// <summary>
    /// ガチャシーンに遷移（未実装）
    /// </summary>
    public void TransitionToGacha()
    {
        if (!SceneNames.IsSceneImplemented(SceneNames.GACHA))
        {
            ShowNotImplementedMessage("ガチャ");
            return;
        }
        TransitionToScene(SceneNames.GACHA);
    }

    /// <summary>
    /// 前のシーンに戻る
    /// </summary>
    public void GoBack()
    {
        if (GameSceneManager.Instance != null && GameSceneManager.Instance.CanGoBack())
        {
            GameSceneManager.Instance.GoBackToPreviousScene();
        }
        else
        {
            LogWarning("前のシーンに戻れません - ホームに移動します");
            TransitionToHome();
        }
    }

    #endregion

    #region Private Methods - Transition Logic

    /// <summary>
    /// 遷移処理のメインコルーチン
    /// </summary>
    private IEnumerator TransitionCoroutine(string targetSceneName, Action onComplete)
    {
        IsTransitioning = true;
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool hasError = false;
        string errorMessage = "";

        LogDebug($"シーン遷移開始: {currentSceneName} → {targetSceneName}");

        // 1. 遷移開始イベント発火
        OnTransitionStarted?.Invoke(currentSceneName, targetSceneName);
        GameSceneManager.Instance?.RecordSceneTransition(currentSceneName, targetSceneName);

        // 2. データ自動保存
        if (AutoSaveEnabled)
        {
            yield return StartCoroutine(AutoSaveCoroutine());
        }

        // 3. 遷移UI演出開始
        TransitionUIController.Instance?.StartTransition(targetSceneName, () => {
            // UI演出中にシーンロード開始
            StartCoroutine(LoadSceneCoroutine(targetSceneName));
        });

        // 4. シーンロード完了を待機（タイムアウト付き）
        var waitCoroutine = StartCoroutine(WaitForSceneLoadWithTimeout(targetSceneName));
        yield return waitCoroutine;

        // エラーチェック（WaitForSceneLoadWithTimeoutからのエラー検知）
        if (SceneManager.GetActiveScene().name != targetSceneName)
        {
            hasError = true;
            errorMessage = $"シーンロードがタイムアウトまたは失敗しました: {targetSceneName}";
        }

        // 5. エラーハンドリング
        if (hasError)
        {
            HandleTransitionError(targetSceneName, errorMessage);
            yield break;
        }

        // 6. 遷移UI演出終了
        if (TransitionUIController.Instance != null)
        {
            TransitionUIController.Instance.EndTransition(() => {
                // 遷移完了処理
                CompleteTransition(targetSceneName, onComplete);
            });
        }
        else
        {
            // TransitionUIControllerがない場合は即座に完了
            CompleteTransition(targetSceneName, onComplete);
        }
    }

    /// <summary>
    /// シーンロード処理
    /// </summary>
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        LogDebug($"シーンロード完了: {sceneName}");
    }

    /// <summary>
    /// シーンロード完了をタイムアウト付きで待機
    /// </summary>
    private IEnumerator WaitForSceneLoadWithTimeout(string sceneName)
    {
        float elapsed = 0f;

        // 期待されるシーン名を計算（Build Settingsでの登録名 vs 実行時のシーン名の違いに対応）
        string expectedSceneName = SceneNames.GetActualSceneName(sceneName);

        LogDebug($"シーンロード待機: 要求シーン='{sceneName}', 期待ファイル名='{expectedSceneName}'");

        while (elapsed < transitionTimeout)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            // 複数パターンでチェック
            if (currentSceneName == expectedSceneName ||
                currentSceneName == sceneName ||
                currentSceneName == SceneNames.GetSceneFileName(sceneName))
            {
                LogDebug($"シーンロード完了: 現在シーン='{currentSceneName}'");
                yield break; // ロード完了
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // タイムアウト時の詳細情報
        string currentScene = SceneManager.GetActiveScene().name;
        LogError($"シーンロードがタイムアウトしました: 要求='{sceneName}', 期待='{expectedSceneName}', 現在='{currentScene}', 経過時間={elapsed:F1}秒");
    }

    /// <summary>
    /// 遷移完了処理
    /// </summary>
    private void CompleteTransition(string targetSceneName, Action onComplete)
    {
        IsTransitioning = false;
        currentTransitionCoroutine = null;

        // 遷移完了イベント発火
        OnTransitionCompleted?.Invoke(targetSceneName);
        GameSceneManager.Instance?.RecordSceneTransitionCompleted(targetSceneName);

        // コールバック実行
        onComplete?.Invoke();

        LogDebug($"シーン遷移完了: {targetSceneName}");
    }

    #endregion

    #region Private Methods - Validation & Error Handling

    /// <summary>
    /// シーン遷移の妥当性チェック
    /// </summary>
    private bool ValidateSceneTransition(string targetSceneName)
    {
        // シーン名の妥当性チェック
        if (string.IsNullOrEmpty(targetSceneName))
        {
            LogError("遷移先シーン名が空です");
            return false;
        }

        if (!SceneNames.IsValidSceneName(targetSceneName))
        {
            LogError($"無効なシーン名です: {targetSceneName}");
            return false;
        }

        // Build Settingsでの存在確認
        string expectedSceneName = SceneNames.GetActualSceneName(targetSceneName);
        if (!IsSceneInBuildSettings(targetSceneName))
        {
            LogError($"シーンがBuild Settingsに登録されていません: {targetSceneName} (期待ファイル名: {expectedSceneName})");
            LogError("Build Settings → Scenes In Build で以下を確認してください:");
            LogError($"  - {targetSceneName}");
            LogError($"  - Assets/Scenes/{expectedSceneName}.unity");
            return false;
        }

        // 現在のシーンと同じ場合はスキップ
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == expectedSceneName ||
            currentSceneName == SceneNames.GetSceneFileName(targetSceneName))
        {
            LogWarning($"既に同じシーンにいます: {currentSceneName}");
            return false;
        }

        // 依存関係チェック
        if (validateDependencies && !ValidateSceneDependencies(targetSceneName))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// シーンがBuild Settingsに登録されているかチェック
    /// </summary>
    private bool IsSceneInBuildSettings(string sceneName)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            // 複数パターンでチェック
            if (scenePath.Contains(sceneName) ||
                sceneNameFromPath == SceneNames.GetActualSceneName(sceneName) ||
                scenePath.EndsWith($"{sceneName}.unity"))
            {
                LogDebug($"Build Settingsでシーン確認: {scenePath}");
                return true;
            }
        }

        LogWarning($"Build Settingsでシーンが見つかりません: {sceneName}");
        LogWarning("登録されているシーン一覧:");
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            LogWarning($"  [{i}] {scenePath}");
        }

        return false;
    }

    /// <summary>
    /// シーンの依存関係チェック（デバッグ強化版）
    /// </summary>
    private bool ValidateSceneDependencies(string sceneName)
    {
        // 依存関係チェックを無効化している場合はスキップ
        if (!validateDependencies)
        {
            LogDebug("依存関係チェックが無効化されています");
            return true;
        }

        LogDebug($"シーン依存関係チェック開始: {sceneName}");

        switch (sceneName)
        {
            case SceneNames.QUEST_BATTLE:
                // 戦闘シーンの場合（デバッグ強化版）
                LogDebug("戦闘シーン依存関係チェック開始");

                // 必須マネージャーチェック
                if (SaveDataManager.Instance == null || !SaveDataManager.Instance.IsDataLoaded)
                {
                    LogError("SaveDataManagerが未初期化 - 戦闘シーンに遷移できません");
                    return false;
                }
                LogDebug("SaveDataManager: OK");

                if (MasterDataManager.Instance == null || !MasterDataManager.Instance.IsDataLoaded)
                {
                    LogError("MasterDataManagerが未初期化 - 戦闘シーンに遷移できません");
                    return false;
                }
                LogDebug("MasterDataManager: OK");

                if (QuestDataManager.Instance == null || !QuestDataManager.Instance.IsDataLoaded)
                {
                    LogError("QuestDataManagerが未初期化 - 戦闘シーンに遷移できません");
                    return false;
                }
                LogDebug("QuestDataManager: OK");

               

                if (!QuestSelectionData.HasValidQuest())
                {
                    int currentQuestId = QuestSelectionData.GetSelectedQuestId();
                    LogError($"有効なクエストが選択されていません - questId={currentQuestId}");
                    LogError("QuestDetailUIでSetSelectedQuest()が正しく呼ばれているか確認してください");
                    return false;
                }

                int questId = QuestSelectionData.GetSelectedQuestId();
                LogDebug($"戦闘シーン依存関係チェック完了 - 選択クエストID: {questId}");
                break;

            // 他のケース...
            default:
                LogDebug($"{sceneName}は依存関係チェック対象外");
                break;
        }

        LogDebug($"シーン依存関係チェック完了: {sceneName}");
        return true;
    }

    /// <summary>
    /// 遷移エラーハンドリング
    /// </summary>
    private void HandleTransitionError(string sceneName, string errorMessage)
    {
        IsTransitioning = false;
        currentTransitionCoroutine = null;

        LogError($"シーン遷移エラー [{sceneName}]: {errorMessage}");
        OnTransitionError?.Invoke(sceneName, errorMessage);

        // UI演出を強制終了
        if (TransitionUIController.Instance != null && TransitionUIController.Instance.IsTransitioning)
        {
            TransitionUIController.Instance.EndTransition();
        }
    }

    /// <summary>
    /// 未実装シーンメッセージ表示
    /// </summary>
    private void ShowNotImplementedMessage(string sceneName)
    {
        LogWarning($"{sceneName}は未実装です");

        // TODO: 未実装メッセージのUI表示
        /*
        // 実装例（コメントアウト）:
        // MessageBox.Show($"{sceneName}機能は現在開発中です。\n今後のアップデートをお待ちください。");
        */

        Debug.LogWarning($"[未実装] {sceneName}機能は現在開発中です");
    }

    #endregion

    #region Private Methods - Auto Save

    /// <summary>
    /// 自動保存処理
    /// </summary>
    private IEnumerator AutoSaveCoroutine()
    {
        LogDebug("シーン遷移前の自動保存開始");

        bool saveSuccess = true;

        // SaveDataManagerの自動保存
        if (SaveDataManager.Instance != null)
        {
            // エラーハンドリングを別メソッドに分離
            saveSuccess = PerformAutoSave();

            if (saveSuccess)
            {
                // 保存完了を少し待機
                yield return new WaitForSeconds(0.1f);
                LogDebug("自動保存完了");
            }
            else
            {
                LogError("自動保存でエラーが発生しましたが、遷移を続行します");
                // 保存エラーでも遷移は続行
            }
        }
    }

    /// <summary>
    /// 自動保存実行（エラーハンドリング分離）
    /// </summary>
    private bool PerformAutoSave()
    {
        try
        {
            SaveDataManager.Instance.MarkDataDirty();
            SaveDataManager.Instance.SaveSaveData();
            return true;
        }
        catch (System.Exception e)
        {
            LogError($"自動保存エラー: {e.Message}");
            return false;
        }
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// 遷移をキャンセル
    /// </summary>
    public void CancelTransition()
    {
        if (!IsTransitioning)
        {
            LogWarning("遷移中ではありません");
            return;
        }

        if (currentTransitionCoroutine != null)
        {
            StopCoroutine(currentTransitionCoroutine);
            currentTransitionCoroutine = null;
        }

        IsTransitioning = false;

        // UI演出を停止
        if (TransitionUIController.Instance != null && TransitionUIController.Instance.IsTransitioning)
        {
            TransitionUIController.Instance.EndTransition();
        }

        LogDebug("シーン遷移をキャンセルしました");
    }

    /// <summary>
    /// 現在のシーンから遷移可能なシーン一覧を取得
    /// </summary>
    public string[] GetAvailableTransitions()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        return currentScene switch
        {
            SceneNames.TITLE => new[] { SceneNames.HOME },
            SceneNames.HOME => new[] { SceneNames.TITLE, SceneNames.EQUIPMENT_EDIT, SceneNames.EQUIPMENT_ENHANCE, SceneNames.QUEST_BATTLE, SceneNames.GACHA },
            SceneNames.EQUIPMENT_EDIT => new[] { SceneNames.HOME, SceneNames.EQUIPMENT_ENHANCE },
            SceneNames.EQUIPMENT_ENHANCE => new[] { SceneNames.HOME, SceneNames.EQUIPMENT_EDIT },
            SceneNames.QUEST_BATTLE => new[] { SceneNames.HOME },
            SceneNames.GACHA => new[] { SceneNames.HOME },
            _ => new[] { SceneNames.HOME } // 不明なシーンの場合はホームのみ
        };
    }

    #endregion

    #region Debug Methods

    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SceneTransitionManager] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[SceneTransitionManager] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SceneTransitionManager] {message}");
    }

    #endregion

    #region Inspector Context Menu

#if UNITY_EDITOR
    [ContextMenu("利用可能な遷移先を表示")]
    private void ShowAvailableTransitions()
    {
        var transitions = GetAvailableTransitions();
        LogDebug($"現在のシーン ({SceneManager.GetActiveScene().name}) から遷移可能:");
        foreach (var scene in transitions)
        {
            LogDebug($"  - {scene} ({SceneNames.GetDisplayName(scene)})");
        }
    }

    [ContextMenu("遷移状態を表示")]
    private void ShowTransitionState()
    {
        LogDebug($"=== 遷移状態 ===");
        LogDebug($"遷移中: {IsTransitioning}");
        LogDebug($"自動保存有効: {AutoSaveEnabled}");
        LogDebug($"依存関係チェック有効: {validateDependencies}");
        LogDebug($"遷移タイムアウト: {transitionTimeout}秒");
    }

    [ContextMenu("ホームに遷移テスト")]
    private void TestTransitionToHome()
    {
        if (Application.isPlaying)
        {
            TransitionToHome();
        }
    }
#endif

    #endregion
}