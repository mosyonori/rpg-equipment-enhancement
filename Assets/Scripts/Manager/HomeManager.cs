using System;
using UnityEngine;

/// <summary>
/// ホーム画面管理マネージャー
/// 責任範囲：
/// - ホーム画面で必要なデータの集約・管理
/// - 各Manager間の連携調整
/// - ホーム画面固有の処理ロジック
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

            // **修正**: UIコンポーネントより先に基本初期化を実行
            InitializeImmediate();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 依存関係確認後にデータ初期化
        StartCoroutine(WaitForDependenciesAndInitialize());
    }

    // **削除**: Start()メソッドを削除（重複初期化の原因）

    /// <summary>
    /// **新規追加**: 即座に基本初期化（UIコンポーネントの参照エラー回避）
    /// </summary>
    private void InitializeImmediate()
    {
        Log("HomeManager基本初期化開始（即座実行）");

        // 基本状態設定
        lastDataRefreshTime = DateTime.Now;

        // 初期データを空で設定（UIエラー回避）
        currentPlayerSummary = new PlayerSummaryData();
        currentEquipmentSummary = new EquipmentSummaryData();

        // 基本初期化完了フラグ（データ初期化とは別）
        IsInitialized = true;

        Log("HomeManager基本初期化完了 - UIコンポーネント参照可能");
    }

    /// <summary>
    /// 依存関係の初期化完了を待機してからデータ初期化
    /// </summary>
    private System.Collections.IEnumerator WaitForDependenciesAndInitialize()
    {
        Log("HomeManagerデータ初期化開始 - 依存関係チェック中...");

        float timeout = 15f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (CheckDependencies())
            {
                Log("依存関係確認完了 - データ初期化実行");
                InitializeData();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        LogError($"依存関係の初期化がタイムアウトしました（{timeout}秒）");
        LogError("MasterDataManagerとSaveDataManagerがシーンに配置され、正常に初期化されているか確認してください");
    }

    /// <summary>
    /// 依存するマネージャーの初期化チェック
    /// </summary>
    /// <returns>全ての依存関係が満たされている場合true</returns>
    private bool CheckDependencies()
    {
        // MasterDataManagerチェック
        if (MasterDataManager.Instance == null)
        {
            Log("MasterDataManager.Instanceがnullです");
            return false;
        }

        if (!MasterDataManager.Instance.IsDataLoaded)
        {
            Log($"MasterDataManagerのデータ読み込みが未完了です (IsDataLoaded: {MasterDataManager.Instance.IsDataLoaded})");
            return false;
        }

        // SaveDataManagerチェック
        if (SaveDataManager.Instance == null)
        {
            Log("SaveDataManager.Instanceがnullです");
            return false;
        }

        if (!SaveDataManager.Instance.IsDataLoaded)
        {
            Log($"SaveDataManagerのデータ読み込みが未完了です (IsDataLoaded: {SaveDataManager.Instance.IsDataLoaded})");
            return false;
        }

        Log("全ての依存関係が満たされています");
        return true;
    }

    /// <summary>
    /// **修正**: データ初期化（依存関係確認後）
    /// </summary>
    private void InitializeData()
    {
        Log("HomeManagerデータ初期化実行");

        // 依存するマネージャーの最終チェック
        if (!CheckDependencies())
        {
            LogError("データ初期化時に依存関係チェックに失敗しました");
            return;
        }

        // イベント購読を行う
        RegisterManagerEvents();

        // 実際のデータでサマリーを更新
        RefreshHomeData();

        // 定期更新開始
        InvokeRepeating(nameof(RefreshHomeData), 1f, dataRefreshInterval);

        Log("HomeManagerデータ初期化完了");
    }

    /// <summary>
    /// **既存維持**: 外部からの初期化呼び出し用
    /// </summary>
    public bool Initialize()
    {
        try
        {
            Log("HomeManager.Initialize()が呼び出されました");

            // 既に基本初期化済みの場合はデータ初期化のみ
            if (IsInitialized && CheckDependencies())
            {
                InitializeData();
                return true;
            }
            else if (!IsInitialized)
            {
                Log("基本初期化が未完了のため、即座実行");
                InitializeImmediate();

                if (CheckDependencies())
                {
                    InitializeData();
                    return true;
                }
                else
                {
                    Log("依存関係未準備のため、コルーチンで待機中");
                    return false;
                }
            }

            return false;
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

        // QuestListManagerからのイベント（存在する場合のみ）
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
        // **修正点**: 初期化前でも最低限のデータは設定
        if (!IsInitialized && currentPlayerSummary == null)
        {
            currentPlayerSummary = new PlayerSummaryData();
            currentEquipmentSummary = new EquipmentSummaryData();
        }

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

            // **修正点**: 実際のセーブデータからサマリーを作成
            Log($"セーブデータ取得成功 - プレイヤー名: {saveData.playerName}, レベル: {saveData.playerLevel}");

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

            Log($"ホームデータ更新完了 - プレイヤー名: {currentPlayerSummary.playerName}, ゴールド: {currentPlayerSummary.gold}");
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
        // **修正点**: PlayerSummaryData.CreateFromSaveData()を使用
        var summary = PlayerSummaryData.CreateFromSaveData(saveData);

        // スタミナ回復処理
        saveData.RecoverStamina();
        summary.currentStamina = saveData.currentStamina;
        summary.staminaRecoveryRemaining = saveData.GetTimeToNextStaminaRecovery();

        // 新アイテム・通知チェック
        summary.hasNewItems = CheckForNewItems(saveData);
        summary.hasCompletedQuests = CheckForCompletedQuests(saveData);
        summary.hasNewNotifications = CheckForNewNotifications();

        Log($"プレイヤーサマリー作成: {summary.playerName}, Lv.{summary.playerLevel}, ゴールド: {summary.gold}");

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
    [ContextMenu("ホームデータを強制更新")]
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