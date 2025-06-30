using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム全体のシーン状態管理
/// 現在のシーン情報、遷移履歴、戻る機能を提供
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    #region Events

    /// <summary>
    /// シーン遷移開始時のイベント
    /// </summary>
    public static event Action<string> OnSceneTransitionStarted;

    /// <summary>
    /// シーン遷移完了時のイベント
    /// </summary>
    public static event Action<string> OnSceneTransitionCompleted;

    /// <summary>
    /// シーン初期化完了時のイベント
    /// </summary>
    public static event Action<string> OnSceneInitialized;

    #endregion

    #region Properties

    public static GameSceneManager Instance { get; private set; }

    /// <summary>
    /// 現在のシーン名
    /// </summary>
    public string CurrentSceneName { get; private set; }

    /// <summary>
    /// 前のシーン名
    /// </summary>
    public string PreviousSceneName { get; private set; }

    /// <summary>
    /// シーン初期化中かどうか
    /// </summary>
    public bool IsInitializing { get; private set; }

    #endregion

    #region Private Fields

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private int maxSceneHistory = 10; // 履歴の最大保持数

    // シーン遷移履歴
    private Queue<string> sceneHistory = new Queue<string>();

    // シーン別の初期化ハンドラー
    private Dictionary<string, Action> sceneInitializers = new Dictionary<string, Action>();

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
        // 現在のシーン名を取得
        CurrentSceneName = SceneManager.GetActiveScene().name;

        // Unity のシーン読み込み完了イベントに登録
        SceneManager.sceneLoaded += OnSceneLoaded;

        // シーン初期化ハンドラーを登録
        RegisterSceneInitializers();

        LogDebug($"GameSceneManager初期化完了 - 現在のシーン: {CurrentSceneName}");
    }

    /// <summary>
    /// シーン別初期化ハンドラーを登録
    /// </summary>
    private void RegisterSceneInitializers()
    {
        // Unity SceneManager.GetActiveScene().name で取得される実際のファイル名で登録

        // タイトルシーン初期化
        sceneInitializers["TitleScene"] = InitializeTitleScene;

        // ホームシーン初期化
        sceneInitializers["HomeScene"] = InitializeHomeScene;

        // 装備編集シーン初期化（InventoryScene）
        sceneInitializers["InventoryScene"] = InitializeEquipmentEditScene;

        // 装備強化シーン初期化（EquipmentScene）
        sceneInitializers["EquipmentScene"] = InitializeEquipmentEnhanceScene;

        // クエスト戦闘シーン初期化（未実装）
        sceneInitializers["QuestBattleScene"] = InitializeQuestBattleScene;

        // ガチャシーン初期化（未実装）
        sceneInitializers["GachaScene"] = InitializeGachaScene;
    }

    #endregion

    #region Scene Transition Tracking

    /// <summary>
    /// シーン遷移を記録
    /// </summary>
    /// <param name="fromScene">遷移元シーン</param>
    /// <param name="toScene">遷移先シーン</param>
    public void RecordSceneTransition(string fromScene, string toScene)
    {
        LogDebug($"シーン遷移記録: {fromScene} → {toScene}");

        PreviousSceneName = fromScene;
        CurrentSceneName = toScene;

        // 履歴に追加
        AddToHistory(fromScene);

        // イベント発火
        OnSceneTransitionStarted?.Invoke(toScene);
    }

    /// <summary>
    /// シーン遷移完了を記録
    /// </summary>
    /// <param name="sceneName">完了したシーン名</param>
    public void RecordSceneTransitionCompleted(string sceneName)
    {
        LogDebug($"シーン遷移完了: {sceneName}");
        OnSceneTransitionCompleted?.Invoke(sceneName);
    }

    /// <summary>
    /// 履歴にシーンを追加
    /// </summary>
    private void AddToHistory(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        sceneHistory.Enqueue(sceneName);

        // 履歴が上限を超えた場合は古いものを削除
        while (sceneHistory.Count > maxSceneHistory)
        {
            sceneHistory.Dequeue();
        }
    }

    #endregion

    #region Scene Back Navigation

    /// <summary>
    /// 前のシーンに戻れるかチェック
    /// </summary>
    /// <returns>戻れる場合true</returns>
    public bool CanGoBack()
    {
        return !string.IsNullOrEmpty(PreviousSceneName) &&
               SceneNames.IsValidSceneName(PreviousSceneName);
    }

    /// <summary>
    /// 前のシーンに戻る
    /// </summary>
    public void GoBackToPreviousScene()
    {
        if (!CanGoBack())
        {
            LogWarning("前のシーンに戻れません");
            return;
        }

        LogDebug($"前のシーンに戻ります: {PreviousSceneName}");
        SceneTransitionManager.Instance.TransitionToScene(PreviousSceneName);
    }

    /// <summary>
    /// ホームシーンに戻る
    /// </summary>
    public void GoToHome()
    {
        LogDebug("ホームシーンに移動します");
        SceneTransitionManager.Instance.TransitionToScene(SceneNames.HOME);
    }

    #endregion

    #region Scene Initialization Handlers

    /// <summary>
    /// Unity のシーン読み込み完了時のハンドラー
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        LogDebug($"シーン読み込み完了: {sceneName}");

        // シーン別初期化を実行
        InitializeScene(sceneName);
    }

    /// <summary>
    /// シーン別初期化を実行
    /// </summary>
    private void InitializeScene(string sceneName)
    {
        IsInitializing = true;

        try
        {
            if (sceneInitializers.ContainsKey(sceneName))
            {
                LogDebug($"シーン初期化開始: {sceneName}");
                sceneInitializers[sceneName]?.Invoke();
                LogDebug($"シーン初期化完了: {sceneName}");
            }
            else
            {
                LogWarning($"未登録のシーンです: {sceneName}");
            }
        }
        catch (Exception e)
        {
            LogError($"シーン初期化エラー [{sceneName}]: {e.Message}");
        }
        finally
        {
            IsInitializing = false;
            OnSceneInitialized?.Invoke(sceneName);
        }
    }

    #endregion

    #region Scene-Specific Initialization

    /// <summary>
    /// タイトルシーン初期化
    /// </summary>
    private void InitializeTitleScene()
    {
        LogDebug("タイトルシーン初期化実行");

        // TODO: タイトルシーン固有の初期化処理
        // 例：BGM再生、セーブデータの存在チェック、etc.

        /*
        // 実装例（コメントアウト）:
        // - BGMManager.Instance.PlayBGM("TitleBGM");
        // - UIManager.Instance.ShowTitleUI();
        // - SaveDataManager.Instance.CheckSaveDataExists();
        */
    }

    /// <summary>
    /// ホームシーン初期化
    /// </summary>
    private void InitializeHomeScene()
    {
        LogDebug("ホームシーン初期化実行");

        // TODO: ホームシーン固有の初期化処理
        // 例：プレイヤー情報表示、通知チェック、etc.

        /*
        // 実装例（コメントアウト）:
        // - HomeUIManager.Instance.UpdatePlayerInfo();
        // - NotificationManager.Instance.CheckNotifications();
        // - MissionManager.Instance.UpdateMissionProgress();
        */
    }

    /// <summary>
    /// 装備編集シーン初期化
    /// </summary>
    private void InitializeEquipmentEditScene()
    {
        LogDebug("装備編集シーン初期化実行");

        // 装備編集に必要なマネージャーの初期化確認
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsInitialized)
        {
            LogDebug("InventoryManager初期化済み - 装備編集シーン準備完了");
        }
        else
        {
            LogWarning("InventoryManagerが初期化されていません");
        }
    }

    /// <summary>
    /// 装備強化シーン初期化
    /// </summary>
    private void InitializeEquipmentEnhanceScene()
    {
        LogDebug("装備強化シーン初期化実行");

        // 装備強化に必要なマネージャーの初期化確認
        if (EquipmentEnhanceManager.Instance != null && EquipmentEnhanceManager.Instance.IsInitialized)
        {
            LogDebug("EquipmentEnhanceManager初期化済み - 装備強化シーン準備完了");
        }
        else
        {
            LogWarning("EquipmentEnhanceManagerが初期化されていません");
        }
    }

    /// <summary>
    /// クエスト戦闘シーン初期化（未実装）
    /// </summary>
    private void InitializeQuestBattleScene()
    {
        LogDebug("クエスト戦闘シーン初期化実行（未実装）");

        // TODO: 将来実装予定
        /*
        // 実装予定例（コメントアウト）:
        // - BattleManager.Instance.InitializeBattle();
        // - EnemyManager.Instance.LoadEnemyData();
        // - PlayerPartyManager.Instance.SetupBattleParty();
        */
    }

    /// <summary>
    /// ガチャシーン初期化（未実装）
    /// </summary>
    private void InitializeGachaScene()
    {
        LogDebug("ガチャシーン初期化実行（未実装）");

        // TODO: 将来実装予定
        /*
        // 実装予定例（コメントアウト）:
        // - GachaManager.Instance.LoadGachaData();
        // - GachaUIManager.Instance.UpdateGachaUI();
        // - PlayerCurrencyManager.Instance.UpdateCurrencyDisplay();
        */
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 現在のシーンが指定したシーンかチェック
    /// </summary>
    /// <param name="sceneName">チェック対象のシーン名（SceneNames定数またはファイル名）</param>
    /// <returns>一致する場合true</returns>
    public bool IsCurrentScene(string sceneName)
    {
        string currentSceneName = CurrentSceneName;

        // 完全一致チェック
        if (currentSceneName == sceneName)
            return true;

        // SceneNames定数での照合（Scenes/付きの場合）
        if (sceneName.StartsWith("Scenes/"))
        {
            string fileName = sceneName.Substring(7); // "Scenes/"を除去
            return currentSceneName == fileName;
        }

        return false;
    }

    /// <summary>
    /// シーン履歴を取得
    /// </summary>
    /// <returns>シーン履歴の配列</returns>
    public string[] GetSceneHistory()
    {
        return sceneHistory.ToArray();
    }

    /// <summary>
    /// シーン履歴をクリア
    /// </summary>
    public void ClearSceneHistory()
    {
        sceneHistory.Clear();
        LogDebug("シーン履歴をクリアしました");
    }

    #endregion

    #region Debug Methods

    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[GameSceneManager] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[GameSceneManager] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[GameSceneManager] {message}");
    }

    #endregion

    #region Inspector Context Menu

#if UNITY_EDITOR
    [ContextMenu("現在の状態を表示")]
    private void ShowCurrentState()
    {
        LogDebug($"=== 現在の状態 ===");
        LogDebug($"現在のシーン: {CurrentSceneName}");
        LogDebug($"前のシーン: {PreviousSceneName}");
        LogDebug($"初期化中: {IsInitializing}");
        LogDebug($"履歴数: {sceneHistory.Count}");

        if (sceneHistory.Count > 0)
        {
            LogDebug($"履歴: [{string.Join(", ", GetSceneHistory())}]");
        }
    }

    [ContextMenu("履歴をクリア")]
    private void ClearHistoryEditor()
    {
        ClearSceneHistory();
    }
#endif

    #endregion

    #region Unity Lifecycle

    private void OnDestroy()
    {
        // Unity のイベントから登録解除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion
}