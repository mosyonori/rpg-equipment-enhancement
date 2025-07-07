using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戦闘システム全体の統括制御
/// 責任範囲：
/// - 戦闘開始・終了フロー制御
/// - 各Manager間の連携調整
/// - UI層への状態変更通知
/// - 戦闘設定（倍速等）の管理
/// データアクセス統一ルール: UI層 → BattleManager → 各Manager → Data層
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private float battleSpeedMultiplier = 1.0f;
    [SerializeField] private bool isPaused = false;

    // イベント
    public static event Action<BattleState> OnBattleStateChanged;
    public static event Action<BattleSetupData> OnBattleInitialized;
    public static event Action<BattleCharacterData> OnCharacterTurnStart;
    public static event Action<ActionData> OnActionExecuted;
    public static event Action<BattleResultData> OnBattleCompleted;
    public static event Action<string> OnBattleError;

    // プロパティ
    public static BattleManager Instance { get; private set; }
    public BattleState CurrentState { get; private set; }
    public bool IsInitialized { get; private set; }
    public float BattleSpeedMultiplier => battleSpeedMultiplier;
    public bool IsPaused => isPaused;

    // 内部状態
    private BattleSetupData currentBattleSetup;
    private BattleResultData currentBattleResult;
    private List<BattleCharacterData> allCharacters;
    private List<ActionData> battleHistory;
    private DateTime battleStartTime;
    private int currentTurnNumber;
    private Coroutine battleCoroutine;

    // 依存Manager
    private BattleDataManager battleDataManager;
    private BattleCalculationManager battleCalculationManager;
    private BattleTurnManager battleTurnManager;

    #region Unity Lifecycle

    private void Awake()
    {
        // シングルトン設定
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

    private void Start()
    {
        StartCoroutine(WaitForDependenciesAndInitialize());
    }

    private void OnDestroy()
    {
        if (battleCoroutine != null)
        {
            StopCoroutine(battleCoroutine);
        }
        UnregisterEvents();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// BattleManager基本初期化
    /// </summary>
    private void InitializeManager()
    {
        Log("BattleManager基本初期化開始");

        CurrentState = BattleState.Idle;
        allCharacters = new List<BattleCharacterData>();
        battleHistory = new List<ActionData>();
        currentTurnNumber = 0;

        Log("BattleManager基本初期化完了");
    }

    /// <summary>
    /// 依存関係の初期化完了を待機してから完全初期化
    /// </summary>
    private IEnumerator WaitForDependenciesAndInitialize()
    {
        Log("BattleManager依存関係チェック開始");

        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (CheckDependencies())
            {
                Log("依存関係確認完了 - BattleManager完全初期化実行");
                CompleteInitialization();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        LogError($"依存関係の初期化がタイムアウトしました（{timeout}秒）");
    }

    /// <summary>
    /// 依存するManagerの存在確認
    /// </summary>
    private bool CheckDependencies()
    {
        // 必須Manager確認
        if (SaveDataManager.Instance == null || !SaveDataManager.Instance.IsDataLoaded)
        {
            Log("SaveDataManagerが未初期化");
            return false;
        }

        if (MasterDataManager.Instance == null || !MasterDataManager.Instance.IsDataLoaded)
        {
            Log("MasterDataManagerが未初期化");
            return false;
        }

        // QuestDataManagerの確認を追加
        if (QuestDataManager.Instance == null || !QuestDataManager.Instance.IsDataLoaded)
        {
            Log("QuestDataManagerが未初期化");
            return false;
        }

        // 戦闘用Manager取得（同一GameObject内を想定）
        battleDataManager = GetComponent<BattleDataManager>();
        battleCalculationManager = GetComponent<BattleCalculationManager>();
        battleTurnManager = GetComponent<BattleTurnManager>();

        if (battleDataManager == null)
        {
            Log("BattleDataManagerが見つかりません");
            return false;
        }

        if (battleCalculationManager == null)
        {
            Log("BattleCalculationManagerが見つかりません");
            return false;
        }

        if (battleTurnManager == null)
        {
            Log("BattleTurnManagerが見つかりません");
            return false;
        }

        Log("全ての依存関係が満たされています");
        return true;
    }

    /// <summary>
    /// 完全初期化処理
    /// </summary>
    private void CompleteInitialization()
    {
        Log("BattleManager完全初期化開始");

        // イベント登録
        RegisterEvents();

        IsInitialized = true;
        Log("BattleManager完全初期化完了");
    }

    /// <summary>
    /// イベント登録
    /// </summary>
    private void RegisterEvents()
    {
        // BattleTurnManagerからのイベント受信
        if (battleTurnManager != null)
        {
            BattleTurnManager.OnTurnCompleted += OnTurnCompleted;
            BattleTurnManager.OnAllEnemiesDefeated += OnAllEnemiesDefeated;
            BattleTurnManager.OnPlayerDefeated += OnPlayerDefeated;
            BattleTurnManager.OnTurnLimitReached += OnTurnLimitReached;
            BattleTurnManager.OnTurnStart += OnTurnStart;
            BattleTurnManager.OnActionExecuted += OnActionExecutedFromTurnManager;
        }
    }

    /// <summary>
    /// イベント登録解除
    /// </summary>
    private void UnregisterEvents()
    {
        if (battleTurnManager != null)
        {
            BattleTurnManager.OnTurnCompleted -= OnTurnCompleted;
            BattleTurnManager.OnAllEnemiesDefeated -= OnAllEnemiesDefeated;
            BattleTurnManager.OnPlayerDefeated -= OnPlayerDefeated;
            BattleTurnManager.OnTurnLimitReached -= OnTurnLimitReached;
            BattleTurnManager.OnTurnStart -= OnTurnStart;
            BattleTurnManager.OnActionExecuted -= OnActionExecutedFromTurnManager;
        }
    }

    #endregion

    #region 公開メソッド - 戦闘制御

    /// <summary>
    /// 戦闘開始
    /// </summary>
    /// <param name="userData">ユーザーセーブデータ</param>
    /// <param name="questData">クエストマスターデータ</param>
    public bool StartBattle(UserSaveData userData, QuestMasterData questData)
    {
        try
        {
            Log($"戦闘開始要求: Quest[{questData.questId}] {questData.questName}");

            if (!IsInitialized)
            {
                LogError("BattleManagerが初期化されていません");
                return false;
            }

            if (CurrentState != BattleState.Idle)
            {
                LogError($"戦闘開始不可: 現在の状態 = {CurrentState}");
                return false;
            }

            // 戦闘セットアップデータ作成
            currentBattleSetup = BattleSetupData.CreateFromUserData(userData, questData);
            if (!currentBattleSetup.IsValid())
            {
                LogError("無効な戦闘セットアップデータ");
                return false;
            }

            // スタミナ消費チェック・実行
            if (!userData.ConsumeStamina(questData.requiredStamina))
            {
                LogError($"スタミナ不足: 必要{questData.requiredStamina}, 現在{userData.currentStamina}");
                OnBattleError?.Invoke("スタミナが不足しています");
                return false;
            }

            // 戦闘初期化開始
            ChangeState(BattleState.Initializing);
            InitializeBattle();

            return true;
        }
        catch (Exception e)
        {
            LogError($"戦闘開始エラー: {e.Message}");
            OnBattleError?.Invoke("戦闘開始に失敗しました");
            return false;
        }
    }

    /// <summary>
    /// 戦闘速度設定
    /// </summary>
    /// <param name="speedMultiplier">速度倍率（1.0=通常, 2.0=2倍速, 4.0=4倍速）</param>
    public void SetBattleSpeed(float speedMultiplier)
    {
        speedMultiplier = Mathf.Clamp(speedMultiplier, 0.5f, 4.0f);
        battleSpeedMultiplier = speedMultiplier;
        Log($"戦闘速度設定: {speedMultiplier}倍速");

        // TimeScaleは使わず、アニメーション・処理待機時間を調整
        if (battleTurnManager != null)
        {
            battleTurnManager.SetBattleSpeed(speedMultiplier);
        }
    }

    /// <summary>
    /// 戦闘一時停止・再開
    /// </summary>
    /// <param name="pause">一時停止するか</param>
    public void SetBattlePause(bool pause)
    {
        isPaused = pause;
        Log($"戦闘{(pause ? "一時停止" : "再開")}");

        if (battleTurnManager != null)
        {
            battleTurnManager.SetPaused(pause);
        }
    }

    /// <summary>
    /// 戦闘強制終了
    /// </summary>
    public void ForceEndBattle()
    {
        Log("戦闘強制終了要求");

        if (CurrentState == BattleState.Idle) return;

        if (battleCoroutine != null)
        {
            StopCoroutine(battleCoroutine);
            battleCoroutine = null;
        }

        // 敗北として処理
        CompleteBattle(false, BattleEndReason.Disconnect);
    }

    #endregion

    #region 公開メソッド - 情報取得

    /// <summary>
    /// 現在の戦闘セットアップデータを取得
    /// </summary>
    public BattleSetupData GetCurrentBattleSetup()
    {
        return currentBattleSetup;
    }

    /// <summary>
    /// 現在の戦闘結果データを取得
    /// </summary>
    public BattleResultData GetCurrentBattleResult()
    {
        return currentBattleResult;
    }

    /// <summary>
    /// 全キャラクターデータを取得
    /// </summary>
    public List<BattleCharacterData> GetAllCharacters()
    {
        return new List<BattleCharacterData>(allCharacters);
    }

    /// <summary>
    /// プレイヤーキャラクターを取得
    /// </summary>
    public BattleCharacterData GetPlayerCharacter()
    {
        return allCharacters.Find(c => c.isPlayer);
    }

    /// <summary>
    /// 敵キャラクター一覧を取得
    /// </summary>
    public List<BattleCharacterData> GetEnemyCharacters()
    {
        return allCharacters.FindAll(c => !c.isPlayer);
    }

    /// <summary>
    /// 戦闘履歴を取得
    /// </summary>
    public List<ActionData> GetBattleHistory()
    {
        return new List<ActionData>(battleHistory);
    }

    /// <summary>
    /// 現在のターン数を取得
    /// </summary>
    public int GetCurrentTurnNumber()
    {
        return currentTurnNumber;
    }

    /// <summary>
    /// 戦闘経過時間を取得
    /// </summary>
    public float GetBattleElapsedTime()
    {
        return CurrentState != BattleState.Idle ?
            (float)(DateTime.Now - battleStartTime).TotalSeconds : 0f;
    }

    #endregion

    #region 内部メソッド - Manager依存関係設定

    /// <summary>
    /// BattleCalculationManagerの依存関係設定
    /// </summary>
    private void SetCalculationManagerDependencies()
    {
        // BattleCalculationManagerがBattleDataManagerにアクセスできるよう設定
        // 現在のBattleCalculationManagerは直接参照を持たないため、
        // 将来の拡張に備えてメソッドを用意
        Log("BattleCalculationManagerの依存関係設定完了");
    }

    #endregion

    #region 内部メソッド - 戦闘フロー

    /// <summary>
    /// 戦闘初期化処理
    /// </summary>
    private void InitializeBattle()
    {
        Log("戦闘初期化処理開始");

        try
        {
            // 時間記録
            battleStartTime = DateTime.Now;
            currentTurnNumber = 1;

            // キャラクターデータ構築
            CreateBattleCharacters();

            // 戦闘データManager初期化
            battleDataManager.InitializeBattle(allCharacters, currentBattleSetup);

            // BattleTurnManagerの依存関係設定（メモ手順に従って実行）
            // 1. BattleDataManagerの参照を設定
            battleTurnManager.SetDataManager(battleDataManager);

            // 2. ターン制限を設定（必要に応じて）
            battleTurnManager.SetTurnLimit(currentBattleSetup.turnLimit);

            // 3. 行動順序を初期化
            battleTurnManager.InitializeTurnOrder();

            // BattleCalculationManagerにもBattleDataManagerの参照を設定
            SetCalculationManagerDependencies();

            // 戦闘結果データ初期化
            currentBattleResult = new BattleResultData();

            // 初期化完了通知
            OnBattleInitialized?.Invoke(currentBattleSetup);
            ChangeState(BattleState.InProgress);

            // 戦闘開始
            battleCoroutine = StartCoroutine(BattleMainLoop());

            Log("戦闘初期化処理完了");
        }
        catch (Exception e)
        {
            LogError($"戦闘初期化エラー: {e.Message}");
            OnBattleError?.Invoke("戦闘の初期化に失敗しました");
            ChangeState(BattleState.Idle);
        }
    }

    /// <summary>
    /// 戦闘参加キャラクター作成
    /// </summary>
    private void CreateBattleCharacters()
    {
        allCharacters.Clear();

        // プレイヤーキャラクター作成
        var playerCharacterMaster = MasterDataManager.Instance.GetCharacterData(1); // プレイヤーキャラクターID=1と仮定
        if (playerCharacterMaster != null)
        {
            var playerChar = BattleCharacterData.CreateFromCharacterMaster(
                playerCharacterMaster,
                currentBattleSetup.playerStats
            );
            allCharacters.Add(playerChar);
            Log($"プレイヤーキャラクター作成: {playerChar.characterName}");
        }

        // 敵モンスター作成（QuestDataManagerを使用）
        foreach (var monsterId in currentBattleSetup.spawnMonsterIds)
        {
            var monsterMaster = QuestDataManager.Instance.GetMonsterData(monsterId);
            if (monsterMaster != null)
            {
                var monsterChar = BattleCharacterData.CreateFromMonsterMaster(monsterMaster);
                allCharacters.Add(monsterChar);
                Log($"敵モンスター作成: {monsterChar.characterName}");
            }
        }

        Log($"戦闘キャラクター作成完了: 合計{allCharacters.Count}体");
    }

    /// <summary>
    /// 戦闘メインループ
    /// </summary>
    private IEnumerator BattleMainLoop()
    {
        Log("戦闘メインループ開始");

        // 4. ターン処理開始（メモ手順の最後のステップ）
        // BattleTurnManagerは自動でターン処理を行うため、
        // ここでは戦闘状態の監視のみ行う
        while (CurrentState == BattleState.InProgress)
        {
            // 一時停止チェック
            yield return new WaitUntil(() => !isPaused);

            // 戦闘終了条件チェック
            if (CheckBattleEndConditions())
            {
                break;
            }

            // 速度調整
            yield return new WaitForSeconds(0.1f / battleSpeedMultiplier);
        }

        Log("戦闘メインループ終了");
    }

    /// <summary>
    /// 戦闘終了条件をチェック
    /// </summary>
    private bool CheckBattleEndConditions()
    {
        // 全滅チェックはBattleTurnManagerのイベントで処理されるため、
        // ここでは特別な処理は不要
        return false;
    }

    /// <summary>
    /// キャラクターのターン実行
    /// </summary>
    private IEnumerator ExecuteCharacterTurn(BattleCharacterData character)
    {
        // この処理はBattleTurnManagerが自動で行うため、
        // BattleManagerでは個別のターン実行は不要
        // イベントベースでUI通知のみ行う
        yield return null;
    }

    /// <summary>
    /// 行動実行処理
    /// </summary>
    private IEnumerator ExecuteAction(ActionData action)
    {
        // この処理もBattleTurnManagerが自動で行うため、
        // BattleManagerでは直接実行せず、イベント経由で結果を受け取る
        Log($"行動実行（イベント経由）: {action.GetActionSummary()}");
        yield return null;
    }

    /// <summary>
    /// 戦闘完了処理
    /// </summary>
    private void CompleteBattle(bool isVictory, BattleEndReason endReason)
    {
        Log($"戦闘完了: {(isVictory ? "勝利" : "敗北")} ({endReason})");

        if (battleCoroutine != null)
        {
            StopCoroutine(battleCoroutine);
            battleCoroutine = null;
        }

        try
        {
            // 戦闘結果作成
            CreateBattleResult(isVictory, endReason);

            // 勝利時の報酬処理
            if (isVictory)
            {
                ProcessVictoryRewards();
            }

            // 結果をセーブデータに反映
            var userData = SaveDataManager.Instance.CurrentSaveData;
            if (userData != null)
            {
                currentBattleResult.ApplyResultToUserData(userData);
                SaveDataManager.Instance.MarkDataDirty();
            }

            ChangeState(BattleState.Completed);
            OnBattleCompleted?.Invoke(currentBattleResult);

            Log("戦闘完了処理終了");
        }
        catch (Exception e)
        {
            LogError($"戦闘完了処理エラー: {e.Message}");
            OnBattleError?.Invoke("戦闘結果の処理に失敗しました");
        }
    }

    /// <summary>
    /// 戦闘結果データ作成
    /// </summary>
    private void CreateBattleResult(bool isVictory, BattleEndReason endReason)
    {
        currentBattleResult.isVictory = isVictory;
        currentBattleResult.endReason = endReason;
        currentBattleResult.totalTurns = currentTurnNumber;
        currentBattleResult.battleDuration = GetBattleElapsedTime();

        // 統計情報集計
        var playerChar = GetPlayerCharacter();
        if (playerChar != null)
        {
            currentBattleResult.totalDamageDealt = playerChar.damageDealt;
            currentBattleResult.totalDamageReceived = playerChar.damageReceived;
            currentBattleResult.skillsUsed = playerChar.skillsUsed;
        }

        // クリティカル回数集計
        foreach (var action in battleHistory)
        {
            currentBattleResult.criticalHits += action.GetCriticalCount();
        }
    }

    /// <summary>
    /// 勝利時報酬処理
    /// </summary>
    private void ProcessVictoryRewards()
    {
        // 基本報酬
        currentBattleResult.gainedExp = currentBattleSetup.baseRewardExp;
        currentBattleResult.gainedGold = currentBattleSetup.baseRewardGold;

        // ドロップアイテム処理（QuestDataManagerを使用）
        if (!string.IsNullOrEmpty(currentBattleSetup.dropTableId))
        {
            var dropTable = QuestDataManager.Instance.GetDropTableData(currentBattleSetup.dropTableId);
            if (dropTable != null)
            {
                var dropResults = dropTable.SimulateDrop(1);
                currentBattleResult.dropItems.AddRange(dropResults);
            }
        }

        Log($"報酬処理完了: Exp={currentBattleResult.gainedExp}, Gold={currentBattleResult.gainedGold}, DropItems={currentBattleResult.dropItems.Count}");
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// ターン開始イベント（BattleTurnManagerから）
    /// </summary>
    private void OnTurnStart(string characterId, int turnNumber)
    {
        currentTurnNumber = turnNumber;
        var character = battleDataManager.GetCharacter(characterId);
        if (character != null)
        {
            OnCharacterTurnStart?.Invoke(character);
            Log($"ターン{turnNumber}: {character.characterName}の行動開始");
        }
    }

    /// <summary>
    /// 行動実行イベント（BattleTurnManagerから）
    /// </summary>
    private void OnActionExecutedFromTurnManager(ActionData action)
    {
        // 戦闘履歴に追加
        battleHistory.Add(action);

        // UI層に通知
        OnActionExecuted?.Invoke(action);

        Log($"行動実行完了: {action.GetActionSummary()}");
    }

    /// <summary>
    /// ターン完了イベント
    /// </summary>
    private void OnTurnCompleted(int turnNumber)
    {
        currentTurnNumber = turnNumber;
        Log($"ターン{turnNumber}完了");
    }

    /// <summary>
    /// 敵全滅イベント
    /// </summary>
    private void OnAllEnemiesDefeated()
    {
        Log("敵全滅 - 勝利");
        CompleteBattle(true, BattleEndReason.Victory);
    }

    /// <summary>
    /// プレイヤー敗北イベント
    /// </summary>
    private void OnPlayerDefeated()
    {
        Log("プレイヤー敗北");
        CompleteBattle(false, BattleEndReason.Defeat);
    }

    /// <summary>
    /// ターン制限到達イベント
    /// </summary>
    private void OnTurnLimitReached()
    {
        Log("ターン制限到達 - 敗北");
        CompleteBattle(false, BattleEndReason.TurnLimit);
    }

    #endregion

    #region 内部メソッド - ユーティリティ

    /// <summary>
    /// 戦闘状態変更
    /// </summary>
    private void ChangeState(BattleState newState)
    {
        if (CurrentState != newState)
        {
            var oldState = CurrentState;
            CurrentState = newState;
            OnBattleStateChanged?.Invoke(newState);
            Log($"戦闘状態変更: {oldState} → {newState}");
        }
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[BattleManager] {message}");
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[BattleManager] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("戦闘状態をログ出力")]
    private void LogBattleState()
    {
        Log($"現在の戦闘状態: {CurrentState}");
        Log($"キャラクター数: {allCharacters?.Count ?? 0}");
        Log($"現在ターン: {currentTurnNumber}");
        Log($"戦闘時間: {GetBattleElapsedTime():F1}秒");
    }

    [ContextMenu("戦闘を強制終了")]
    private void EditorForceEndBattle()
    {
        ForceEndBattle();
    }
#endif

    #endregion
}

/// <summary>
/// 戦闘状態列挙型
/// </summary>
public enum BattleState
{
    Idle,           // 待機中
    Initializing,   // 初期化中
    InProgress,     // 戦闘中
    Completed       // 完了
}