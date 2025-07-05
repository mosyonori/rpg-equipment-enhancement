using System;
using UnityEngine;

/// <summary>
/// ホーム画面管理マネージャー
/// 責任範囲：
/// - ホーム画面で必要なデータの集計・管理
/// - 各Manager間の連携調整
/// - ホーム画面特有の処理ロジック
/// - データアクセス統一ルール: UI層 → HomeManager → SaveDataManager → データ層
/// </summary>
public class HomeManager : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private float dataRefreshInterval = 60f; // データ更新間隔（秒）

    // イベント
    public static event Action<PlayerSummaryData> OnPlayerDataUpdated;
    public static event Action<EquipmentSummaryData> OnEquipmentDataUpdated;
    public static event Action<string> OnNotificationReceived;
    public static event Action OnHomeDataRefreshed;

    // プロパティ
    public static HomeManager Instance { get; private set; }
    public bool IsInitialized { get; private set; }

    // 内部状態
    private PlayerSummaryData currentPlayerSummary;
    private EquipmentSummaryData currentEquipmentSummary;
    private DateTime lastDataRefreshTime;

    #region Unity Lifecycle

    private void Awake()
    {
        // シングルトン設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (autoInitialize)
            {
                Initialize();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 依存関係確認を遅延実行で開始
        StartCoroutine(InitializeWithDependencyCheck());
    }

    /// <summary>
    /// 依存関係確認付きの初期化（コルーチン）
    /// </summary>
    private System.Collections.IEnumerator InitializeWithDependencyCheck()
    {
        Log("依存関係の確認を開始します...");

        // SaveDataManagerの初期化を待機
        yield return StartCoroutine(WaitForSaveDataManager());

        // その他の依存関係を確認
        yield return StartCoroutine(WaitForOtherDependencies());

        // 全ての依存関係が確認できたら初期化実行
        if (ValidateDependencies())
        {
            Log("全ての依存関係が確認できました。初期化を続行します。");

            // 定期更新開始
            InvokeRepeating(nameof(RefreshHomeData), 1f, dataRefreshInterval);
        }
        else
        {
            LogError("依存関係の確認に失敗しました。初期化を中断します。");
        }
    }

    /// <summary>
    /// SaveDataManagerの初期化完了を待機
    /// </summary>
    private System.Collections.IEnumerator WaitForSaveDataManager()
    {
        Log("SaveDataManagerの初期化を待機中...");

        float timeout = 10f; // 10秒でタイムアウト
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (SaveDataManager.Instance != null && SaveDataManager.Instance.IsDataLoaded)
            {
                Log("SaveDataManagerの初期化が完了しました");
                yield break;
            }

            // SaveDataManagerが存在しない場合は作成を試行
            if (SaveDataManager.Instance == null)
            {
                Log("SaveDataManagerが見つかりません。初期化を試行します...");

                // SaveDataManagerのGameObjectを探すか作成
                var saveDataManagerObj = UnityEngine.Object.FindFirstObjectByType<SaveDataManager>();
                if (saveDataManagerObj == null)
                {
                    Log("SaveDataManagerを新規作成します");
                    var newSaveDataManager = new GameObject("SaveDataManager");
                    newSaveDataManager.AddComponent<SaveDataManager>();
                }
            }
            // SaveDataManagerは存在するがデータ未読み込みの場合
            else if (!SaveDataManager.Instance.IsDataLoaded)
            {
                Log("SaveDataManagerのデータ読み込みを開始します");

                // データ読み込みを手動で開始
                if (SaveDataManager.Instance.LoadSaveData())
                {
                    Log("SaveDataManagerのデータ読み込みが開始されました");
                }
                else
                {
                    Log("SaveDataManagerのデータ読み込み開始に失敗しました");
                }
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        LogError($"SaveDataManagerの初期化待機がタイムアウトしました（{timeout}秒）");
    }

    /// <summary>
    /// その他の依存関係の初期化完了を待機
    /// </summary>
    private System.Collections.IEnumerator WaitForOtherDependencies()
    {
        Log("その他の依存関係を確認中...");

        float timeout = 5f; // 5秒でタイムアウト
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            // QuestDataManagerなど他の依存関係をチェック
            bool allReady = true;

            // QuestDataManagerが必要な場合の確認（オプショナル）
            if (QuestDataManager.Instance != null)
            {
                if (!QuestDataManager.Instance.IsDataLoaded)
                {
                    Log("QuestDataManagerのデータ読み込みを待機中...");
                    allReady = false;
                }
            }

            if (allReady)
            {
                Log("その他の依存関係の確認が完了しました");
                yield break;
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        Log("その他の依存関係の確認を完了しました（一部未確認の可能性があります）");
    }

    #endregion

    #region 初期化

    /// <summary>
    /// HomeManagerを初期化
    /// </summary>
    public bool Initialize()
    {
        try
        {
            Log("HomeManager初期化開始");

            // 初期データ設定
            currentPlayerSummary = new PlayerSummaryData();
            currentEquipmentSummary = new EquipmentSummaryData();
            lastDataRefreshTime = DateTime.Now;

            // 他のManagerからのイベント登録
            RegisterManagerEvents();

            // 初期データ読み込み
            RefreshHomeData();

            IsInitialized = true;
            Log("HomeManager初期化完了");
            return true;
        }
        catch (Exception e)
        {
            LogError($"HomeManager初期化エラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 依存関係の検証（改良版）
    /// </summary>
    private bool ValidateDependencies()
    {
        bool isValid = true;

        // SaveDataManagerの確認
        if (SaveDataManager.Instance == null)
        {
            LogError("SaveDataManagerが見つかりません");
            isValid = false;
        }
        else if (!SaveDataManager.Instance.IsDataLoaded)
        {
            LogError("SaveDataManagerのデータが読み込まれていません");
            isValid = false;
        }
        else
        {
            Log("SaveDataManager: 正常");
        }

        // CurrentSaveDataの確認
        if (SaveDataManager.Instance?.CurrentSaveData == null)
        {
            LogError("CurrentSaveDataがnullです");
            isValid = false;
        }
        else
        {
            Log("CurrentSaveData: 正常");
        }

        // MasterDataManagerの確認（オプショナル）
        if (MasterDataManager.Instance == null)
        {
            Log("MasterDataManagerが見つかりません（オプショナル）");
        }
        else
        {
            Log("MasterDataManager: 確認済み");
        }

        return isValid;
    }

    /// <summary>
    /// 他のManagerからのイベント登録
    /// </summary>
    private void RegisterManagerEvents()
    {
        // SaveDataManagerからのイベント
        if (SaveDataManager.Instance != null)
        {
            SaveDataManager.OnDataLoaded += OnSaveDataLoaded;
            SaveDataManager.OnDataSaved += OnSaveDataSaved;
        }

        // QuestListManagerからのイベント
        if (QuestListManager.Instance != null)
        {
            QuestListManager.OnQuestStarted += OnQuestStarted;
            QuestListManager.OnQuestCompleted += OnQuestCompleted;
        }
    }

    /// <summary>
    /// イベント登録解除
    /// </summary>
    private void UnregisterManagerEvents()
    {
        if (SaveDataManager.Instance != null)
        {
            SaveDataManager.OnDataLoaded -= OnSaveDataLoaded;
            SaveDataManager.OnDataSaved -= OnSaveDataSaved;
        }

        if (QuestListManager.Instance != null)
        {
            QuestListManager.OnQuestStarted -= OnQuestStarted;
            QuestListManager.OnQuestCompleted -= OnQuestCompleted;
        }
    }

    private void OnDestroy()
    {
        UnregisterManagerEvents();
    }

    /// <summary>
    /// SaveDataManagerの強制初期化
    /// </summary>
    public static void EnsureSaveDataManager()
    {
        if (SaveDataManager.Instance == null)
        {
            Debug.Log("[HomeManager] SaveDataManagerを強制作成します");

            var saveDataManagerObj = new GameObject("SaveDataManager");
            var saveDataManager = saveDataManagerObj.AddComponent<SaveDataManager>();

            // 明示的に初期化実行
            if (saveDataManager != null)
            {
                // SaveDataManagerの初期化メソッドが公開されている場合は呼び出し
                Debug.Log("[HomeManager] SaveDataManagerの初期化を開始");
            }
        }
        else if (!SaveDataManager.Instance.IsDataLoaded)
        {
            Debug.Log("[HomeManager] SaveDataManagerのデータ読み込みを開始");
            SaveDataManager.Instance.LoadSaveData();
        }
    }

    /// <summary>
    /// 緊急時の依存関係修復
    /// </summary>
    [ContextMenu("依存関係を修復")]
    public void RepairDependencies()
    {
        Log("依存関係の修復を開始します");

        // SaveDataManagerの確保
        EnsureSaveDataManager();

        // 少し待ってから再初期化
        StartCoroutine(DelayedInitialize());
    }

    /// <summary>
    /// 遅延初期化
    /// </summary>
    private System.Collections.IEnumerator DelayedInitialize()
    {
        yield return new WaitForSeconds(1f);

        if (ValidateDependencies())
        {
            Log("依存関係修復後の初期化成功");
            RefreshHomeData();
        }
        else
        {
            LogError("依存関係修復後も問題が残っています");
        }
    }

    #endregion

    #region 公開メソッド - データ取得

    /// <summary>
    /// プレイヤーサマリーデータを取得
    /// </summary>
    /// <returns>プレイヤーサマリーデータ</returns>
    public PlayerSummaryData GetPlayerSummary()
    {
        if (!IsInitialized)
        {
            LogError("HomeManagerが初期化されていません");
            return new PlayerSummaryData();
        }

        return currentPlayerSummary ?? new PlayerSummaryData();
    }

    /// <summary>
    /// 装備サマリーデータを取得
    /// </summary>
    /// <returns>装備サマリーデータ</returns>
    public EquipmentSummaryData GetEquipmentSummary()
    {
        if (!IsInitialized)
        {
            LogError("HomeManagerが初期化されていません");
            return new EquipmentSummaryData();
        }

        return currentEquipmentSummary ?? new EquipmentSummaryData();
    }

    /// <summary>
    /// 最新のセーブデータを取得
    /// </summary>
    /// <returns>セーブデータ</returns>
    public UserSaveData GetCurrentSaveData()
    {
        return SaveDataManager.Instance?.CurrentSaveData;
    }

    #endregion

    #region 公開メソッド - データ更新

    /// <summary>
    /// ホームデータを更新
    /// </summary>
    public void RefreshHomeData()
    {
        if (!IsInitialized) return;

        try
        {
            Log("ホームデータ更新開始");

            // 依存関係を再確認
            if (!ValidateDependencies())
            {
                LogError("データ更新時に依存関係の問題を検出しました");
                return;
            }

            var saveData = GetCurrentSaveData();
            if (saveData == null)
            {
                LogError("セーブデータが取得できません");
                return;
            }

            // プレイヤーサマリー更新
            var newPlayerSummary = UpdatePlayerSummary(saveData);

            // 装備サマリー更新
            var newEquipmentSummary = UpdateEquipmentSummary(saveData);

            // 戦闘力を装備サマリーからプレイヤーサマリーに反映
            newPlayerSummary.totalCombatPower = newEquipmentSummary.totalCombatPower;
            newPlayerSummary.weaponPower = newEquipmentSummary.weaponCombatPower;
            newPlayerSummary.armorPower = newEquipmentSummary.armorCombatPower;
            newPlayerSummary.accessoryPower = newEquipmentSummary.accessoryCombatPower;

            // 進行中クエスト数を更新
            newPlayerSummary.ongoingQuestCount = GetOngoingQuestCount();

            // データ更新
            currentPlayerSummary = newPlayerSummary;
            currentEquipmentSummary = newEquipmentSummary;
            lastDataRefreshTime = DateTime.Now;

            // イベント通知
            OnPlayerDataUpdated?.Invoke(currentPlayerSummary);
            OnEquipmentDataUpdated?.Invoke(currentEquipmentSummary);
            OnHomeDataRefreshed?.Invoke();

            Log("ホームデータ更新完了");
        }
        catch (Exception e)
        {
            LogError($"ホームデータ更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// プレイヤーサマリーを更新
    /// </summary>
    /// <param name="saveData">セーブデータ</param>
    /// <returns>更新されたプレイヤーサマリー</returns>
    private PlayerSummaryData UpdatePlayerSummary(UserSaveData saveData)
    {
        var summary = PlayerSummaryData.CreateFromSaveData(saveData);

        // スタミナ回復処理
        saveData.RecoverStamina();
        summary.currentStamina = saveData.currentStamina;
        summary.staminaRecoveryRemaining = saveData.GetTimeToNextStaminaRecovery();

        // 新アイテム・通知チェック
        summary.hasNewItems = CheckForNewItems(saveData);
        summary.hasCompletedQuests = CheckForCompletedQuests(saveData);
        summary.hasNewNotifications = CheckForNewNotifications();

        return summary;
    }

    /// <summary>
    /// 装備サマリーを更新
    /// </summary>
    /// <param name="saveData">セーブデータ</param>
    /// <returns>更新された装備サマリー</returns>
    private EquipmentSummaryData UpdateEquipmentSummary(UserSaveData saveData)
    {
        return EquipmentSummaryData.CreateFromSaveData(saveData);
    }

    #endregion

    #region 公開メソッド - 状態チェック

    /// <summary>
    /// デイリーボーナスをチェック
    /// </summary>
    /// <returns>デイリーボーナスがある場合true</returns>
    public bool CheckDailyBonus()
    {
        var saveData = GetCurrentSaveData();
        if (saveData == null) return false;

        var today = DateTime.Now.Date;
        var lastLogin = saveData.lastLoginDate.Date;

        bool hasDailyBonus = today > lastLogin;

        if (hasDailyBonus)
        {
            Log("デイリーボーナス利用可能");
            OnNotificationReceived?.Invoke("デイリーボーナスを受け取れます！");
        }

        return hasDailyBonus;
    }

    /// <summary>
    /// 新しい通知があるかチェック
    /// </summary>
    /// <returns>新しい通知がある場合true</returns>
    public bool HasNewNotifications()
    {
        if (currentPlayerSummary == null) return false;
        return currentPlayerSummary.HasAnyNewNotifications();
    }

    /// <summary>
    /// スタミナが満タンかチェック
    /// </summary>
    /// <returns>満タンの場合true</returns>
    public bool IsStaminaFull()
    {
        if (currentPlayerSummary == null) return false;
        return currentPlayerSummary.IsStaminaFull();
    }

    /// <summary>
    /// 装備に警告があるかチェック
    /// </summary>
    /// <returns>警告がある場合true</returns>
    public bool HasEquipmentWarnings()
    {
        if (currentEquipmentSummary == null) return false;
        return currentEquipmentSummary.HasWarnings();
    }

    #endregion

    #region 公開メソッド - アクション

    /// <summary>
    /// ログイン処理を実行
    /// </summary>
    public void ProcessLogin()
    {
        try
        {
            Log("ログイン処理開始");

            var saveData = GetCurrentSaveData();
            if (saveData == null) return;

            // ログイン日時更新
            saveData.UpdateLastLoginDate();

            // デイリーボーナスチェック
            CheckDailyBonus();

            // データ保存
            SaveDataManager.Instance.MarkDataDirty();
            SaveDataManager.Instance.SaveSaveData();

            // データ更新
            RefreshHomeData();

            Log("ログイン処理完了");
            OnNotificationReceived?.Invoke("おかえりなさい！");
        }
        catch (Exception e)
        {
            LogError($"ログイン処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// スタミナ回復を強制実行
    /// </summary>
    public void ForceStaminaRecovery()
    {
        try
        {
            var saveData = GetCurrentSaveData();
            if (saveData == null) return;

            saveData.RecoverStamina();
            SaveDataManager.Instance.MarkDataDirty();
            RefreshHomeData();

            Log("スタミナ回復実行");
        }
        catch (Exception e)
        {
            LogError($"スタミナ回復エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - チェック処理

    /// <summary>
    /// 新アイテムがあるかチェック
    /// </summary>
    /// <param name="saveData">セーブデータ</param>
    /// <returns>新アイテムがある場合true</returns>
    private bool CheckForNewItems(UserSaveData saveData)
    {
        // TODO: 新アイテム判定ロジックを実装
        // 例：最後のログイン以降に取得したアイテムがあるかチェック
        return false;
    }

    /// <summary>
    /// 完了クエストがあるかチェック
    /// </summary>
    /// <param name="saveData">セーブデータ</param>
    /// <returns>完了クエストがある場合true</returns>
    private bool CheckForCompletedQuests(UserSaveData saveData)
    {
        // TODO: 完了クエスト判定ロジックを実装
        return false;
    }

    /// <summary>
    /// 新しい通知があるかチェック
    /// </summary>
    /// <returns>新しい通知がある場合true</returns>
    private bool CheckForNewNotifications()
    {
        // TODO: 通知システムの実装
        return false;
    }

    /// <summary>
    /// 進行中クエスト数を取得
    /// </summary>
    /// <returns>進行中クエスト数</returns>
    private int GetOngoingQuestCount()
    {
        // TODO: QuestListManagerから進行中クエスト数を取得
        return 0;
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// セーブデータ読み込み完了イベント
    /// </summary>
    /// <param name="saveData">読み込まれたセーブデータ</param>
    private void OnSaveDataLoaded(UserSaveData saveData)
    {
        Log("セーブデータ読み込み完了 - ホームデータ更新");
        RefreshHomeData();
    }

    /// <summary>
    /// セーブデータ保存完了イベント
    /// </summary>
    /// <param name="saveData">保存されたセーブデータ</param>
    private void OnSaveDataSaved(UserSaveData saveData)
    {
        Log("セーブデータ保存完了");
    }

    /// <summary>
    /// クエスト開始イベント
    /// </summary>
    /// <param name="result">クエスト開始結果</param>
    private void OnQuestStarted(QuestStartResult result)
    {
        if (result.isSuccess)
        {
            Log($"クエスト開始: {result.questId}");
            RefreshHomeData(); // スタミナ消費を反映
        }
    }

    /// <summary>
    /// クエスト完了イベント
    /// </summary>
    /// <param name="questId">完了したクエストID</param>
    /// <param name="questData">クエストデータ</param>
    private void OnQuestCompleted(int questId, QuestDisplayData questData)
    {
        Log($"クエスト完了: {questId}");
        RefreshHomeData(); // 報酬を反映
        OnNotificationReceived?.Invoke("クエストを完了しました！");
    }

    #endregion

    #region ユーティリティ

    /// <summary>
    /// データの最終更新時刻を取得
    /// </summary>
    /// <returns>最終更新時刻</returns>
    public DateTime GetLastDataRefreshTime()
    {
        return lastDataRefreshTime;
    }

    /// <summary>
    /// データ更新が必要かチェック
    /// </summary>
    /// <returns>更新が必要な場合true</returns>
    public bool NeedsDataRefresh()
    {
        return (DateTime.Now - lastDataRefreshTime).TotalSeconds >= dataRefreshInterval;
    }

    /// <summary>
    /// 強制的にデータ更新間隔をリセット
    /// </summary>
    public void ResetDataRefreshTimer()
    {
        lastDataRefreshTime = DateTime.Now;
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[HomeManager] {message}");
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[HomeManager] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("ホームデータを手動更新")]
    private void ManualRefreshHomeData()
    {
        RefreshHomeData();
    }

    [ContextMenu("プレイヤー情報をログ出力")]
    private void LogPlayerInfo()
    {
        if (currentPlayerSummary != null)
        {
            Log(currentPlayerSummary.ToString());
        }
    }

    [ContextMenu("装備情報をログ出力")]
    private void LogEquipmentInfo()
    {
        if (currentEquipmentSummary != null)
        {
            Log(currentEquipmentSummary.ToString());
        }
    }

    [ContextMenu("スタミナを強制回復")]
    private void EditorForceStaminaRecovery()
    {
        ForceStaminaRecovery();
    }

    [ContextMenu("ログイン処理を実行")]
    private void EditorProcessLogin()
    {
        ProcessLogin();
    }
#endif

    #endregion
}