using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 戦闘システムの動作確認用の簡易テストUI
/// 修正版：BattleManagerの非同期初期化に対応
/// </summary>
public class BattleTestUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button startBattleButton;
    [SerializeField] private Button speedToggleButton;
    [SerializeField] private TextMeshProUGUI battleStateText;
    [SerializeField] private TextMeshProUGUI turnInfoText;
    [SerializeField] private TextMeshProUGUI playerHPText;
    [SerializeField] private TextMeshProUGUI enemyHPText;
    [SerializeField] private TextMeshProUGUI speedText;

    [Header("Test Settings")]
    [SerializeField] private int testQuestId = 2;
    [SerializeField] private float initializationTimeout = 15.0f;  // 📝追加: 初期化待機タイムアウト

    private BattleManager battleManager;
    private int currentSpeedIndex = 0;
    private float[] speedOptions = { 1f, 2f, 4f };
    private Coroutine initializationCheckCoroutine;  // 📝追加: 初期化チェック用コルーチン

    void Start()
    {
        InitializeUI();
        // 📝修正: SetupEventListenersをコルーチンで実行
        StartCoroutine(WaitForManagersAndSetupEvents());
        Debug.Log("[BattleTestUI] 戦闘テストUI初期化開始");
    }

    void OnDestroy()
    {
        CleanupEventListeners();

        // 📝追加: コルーチンのクリーンアップ
        if (initializationCheckCoroutine != null)
        {
            StopCoroutine(initializationCheckCoroutine);
        }
    }

    /// <summary>
    /// UI初期化
    /// </summary>
    private void InitializeUI()
    {
        // 初期状態設定
        if (battleStateText != null)
            battleStateText.text = "戦闘待機中";

        if (turnInfoText != null)
            turnInfoText.text = "ターン: 0";

        if (playerHPText != null)
            playerHPText.text = "プレイヤーHP: ---";

        if (enemyHPText != null)
            enemyHPText.text = "敵HP: ---";

        if (speedText != null)
            speedText.text = $"速度: {speedOptions[currentSpeedIndex]}x";

        // ボタン初期化
        if (startBattleButton != null)
        {
            startBattleButton.onClick.AddListener(StartBattleTest);
            // 📝修正: 初期化完了まで無効化
            startBattleButton.interactable = false;
        }

        if (speedToggleButton != null)
        {
            speedToggleButton.onClick.AddListener(ToggleSpeed);
            speedToggleButton.interactable = true;
        }
    }

    /// <summary>
    /// 📝新規追加: Managerの初期化を待ってからイベント設定
    /// </summary>
    private IEnumerator WaitForManagersAndSetupEvents()
    {
        Debug.Log("[BattleTestUI] Manager初期化待機開始");

        float elapsed = 0f;
        bool managersReady = false;

        // UI状態更新
        if (battleStateText != null)
            battleStateText.text = "Manager初期化待機中...";

        while (elapsed < initializationTimeout && !managersReady)
        {
            managersReady = CheckAllManagersReady();

            if (managersReady)
            {
                Debug.Log("[BattleTestUI] 全Manager初期化完了");
                break;
            }

            // 進行状況をUIに表示
            if (battleStateText != null)
                battleStateText.text = $"初期化中... ({elapsed:F1}s)";

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        if (managersReady)
        {
            SetupEventListeners();

            // UI状態更新
            if (battleStateText != null)
                battleStateText.text = "戦闘待機中";

            if (startBattleButton != null)
                startBattleButton.interactable = true;

            Debug.Log("[BattleTestUI] 戦闘テストUI初期化完了");
        }
        else
        {
            Debug.LogError($"[BattleTestUI] Manager初期化がタイムアウトしました ({initializationTimeout}秒)");

            if (battleStateText != null)
                battleStateText.text = "初期化失敗";
        }
    }

    /// <summary>
    /// 📝新規追加: 全Managerの準備状況をチェック
    /// </summary>
    private bool CheckAllManagersReady()
    {
        // BattleManager確認
        if (BattleManager.Instance == null || !BattleManager.Instance.IsInitialized)
        {
            return false;
        }

        // SaveDataManager確認
        if (SaveDataManager.Instance == null || !SaveDataManager.Instance.IsDataLoaded)
        {
            return false;
        }

        // QuestDataManager確認
        if (QuestDataManager.Instance == null || !QuestDataManager.Instance.IsDataLoaded)
        {
            return false;
        }

        // MasterDataManager確認
        if (MasterDataManager.Instance == null || !MasterDataManager.Instance.IsDataLoaded)
        {
            return false;
        }

        // BattleTurnManager確認
        if (BattleTurnManager.Instance == null)
        {
            return false;
        }

        // BattleCalculationManager確認
        if (BattleCalculationManager.Instance == null)
        {
            return false;
        }

        // 📝追加: BattleMonsterManager確認
        if (BattleMonsterManager.Instance == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 📝修正: BattleManagerのイベント登録（初期化完了後に実行）
    /// </summary>
    private void SetupEventListeners()
    {
        Debug.Log("[BattleTestUI] イベントリスナー設定開始");

        if (BattleManager.Instance == null)
        {
            Debug.LogError("[BattleTestUI] BattleManager.Instanceがnullです - BattleManagerがシーンに配置されていません");
            return;
        }

        battleManager = BattleManager.Instance;
        Debug.Log("[BattleTestUI] BattleManager.Instance取得成功");

        try
        {
            // 戦闘状態変更イベント (static)
            BattleManager.OnBattleStateChanged += OnBattleStateChanged;
            Debug.Log("[BattleTestUI] OnBattleStateChangedイベント登録完了");

            // キャラクターターン開始イベント (static)
            BattleManager.OnCharacterTurnStart += OnCharacterTurnStart;
            Debug.Log("[BattleTestUI] OnCharacterTurnStartイベント登録完了");

            // 行動実行イベント (static)
            BattleManager.OnActionExecuted += OnActionExecuted;
            Debug.Log("[BattleTestUI] OnActionExecutedイベント登録完了");

            // 戦闘完了イベント (static)
            BattleManager.OnBattleCompleted += OnBattleCompleted;
            Debug.Log("[BattleTestUI] OnBattleCompletedイベント登録完了");

            // 📝追加: BattleManagerの初期化完了イベント
            BattleManager.OnBattleInitialized += OnBattleInitialized;
            Debug.Log("[BattleTestUI] OnBattleInitializedイベント登録完了");

            // 📝追加: エラーイベント
            BattleManager.OnBattleError += OnBattleError;
            Debug.Log("[BattleTestUI] OnBattleErrorイベント登録完了");

            Debug.Log("[BattleTestUI] BattleManagerイベント登録完了");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BattleTestUI] イベント登録エラー: {e.Message}");
        }
    }

    /// <summary>
    /// イベントリスナーのクリーンアップ
    /// </summary>
    private void CleanupEventListeners()
    {
        // staticイベントなので型名で直接アクセス
        BattleManager.OnBattleStateChanged -= OnBattleStateChanged;
        BattleManager.OnCharacterTurnStart -= OnCharacterTurnStart;
        BattleManager.OnActionExecuted -= OnActionExecuted;
        BattleManager.OnBattleCompleted -= OnBattleCompleted;
        BattleManager.OnBattleInitialized -= OnBattleInitialized;  // 📝追加
        BattleManager.OnBattleError -= OnBattleError;  // 📝追加
    }

    /// <summary>
    /// 📝修正: 戦闘開始テスト（初期化状態チェック追加）
    /// </summary>
    private void StartBattleTest()
    {
        Debug.Log($"[BattleTestUI] ========== 戦闘テスト開始 (Quest ID: {testQuestId}) ==========");

        try
        {
            // 📝追加: BattleManager初期化状態確認
            if (battleManager == null || !battleManager.IsInitialized)
            {
                Debug.LogError("[BattleTestUI] BattleManagerが初期化されていません");

                // 📝追加: 初期化待機を再実行
                if (initializationCheckCoroutine != null)
                {
                    StopCoroutine(initializationCheckCoroutine);
                }
                initializationCheckCoroutine = StartCoroutine(WaitForInitializationAndRetry());
                return;
            }

            // 詳細なnullチェック
            Debug.Log("[BattleTestUI] SaveDataManagerインスタンス確認中...");
            if (SaveDataManager.Instance == null)
            {
                Debug.LogError("[BattleTestUI] SaveDataManager.Instanceがnullです");
                return;
            }

            Debug.Log("[BattleTestUI] UserSaveData取得中...");
            var userData = SaveDataManager.Instance.CurrentSaveData;
            if (userData == null)
            {
                Debug.LogError("[BattleTestUI] UserSaveDataがnullです - SaveDataManagerが初期化されていない可能性があります");
                return;
            }
            Debug.Log($"[BattleTestUI] UserSaveData取得成功: プレイヤー名={userData.playerName}, レベル={userData.playerLevel}");

            Debug.Log("[BattleTestUI] QuestDataManagerインスタンス確認中...");
            if (QuestDataManager.Instance == null)
            {
                Debug.LogError("[BattleTestUI] QuestDataManager.Instanceがnullです");
                return;
            }

            Debug.Log($"[BattleTestUI] QuestMasterData取得中... (ID: {testQuestId})");
            var questData = QuestDataManager.Instance.GetQuestData(testQuestId);
            if (questData == null)
            {
                Debug.LogError($"[BattleTestUI] QuestMasterData (ID: {testQuestId}) がnullです - クエストデータが存在しない可能性があります");
                return;
            }
            Debug.Log($"[BattleTestUI] QuestMasterData取得成功: {questData.questName}");

            Debug.Log("[BattleTestUI] 戦闘開始処理実行中...");
            bool battleStarted = battleManager.StartBattle(userData, questData);

            if (battleStarted)
            {
                // UI状態更新
                if (startBattleButton != null)
                    startBattleButton.interactable = false;

                Debug.Log("[BattleTestUI] 戦闘開始処理完了");
            }
            else
            {
                Debug.LogError("[BattleTestUI] 戦闘開始に失敗しました - StartBattleがfalseを返しました");
            }
        }
        catch (System.NullReferenceException e)
        {
            Debug.LogError($"[BattleTestUI] Null参照エラー: {e.Message}\nStackTrace: {e.StackTrace}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BattleTestUI] 戦闘開始エラー: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// 📝新規追加: 初期化待機してからリトライ
    /// </summary>
    private IEnumerator WaitForInitializationAndRetry()
    {
        Debug.Log("[BattleTestUI] BattleManager初期化完了を待機してリトライします");

        if (battleStateText != null)
            battleStateText.text = "BattleManager初期化待機中...";

        float elapsed = 0f;
        while (elapsed < initializationTimeout)
        {
            if (battleManager != null && battleManager.IsInitialized)
            {
                Debug.Log("[BattleTestUI] BattleManager初期化完了 - 戦闘開始をリトライ");

                if (battleStateText != null)
                    battleStateText.text = "戦闘待機中";

                if (startBattleButton != null)
                    startBattleButton.interactable = true;

                yield break;
            }

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        Debug.LogError("[BattleTestUI] BattleManager初期化待機がタイムアウトしました");

        if (battleStateText != null)
            battleStateText.text = "初期化タイムアウト";
    }

    /// <summary>
    /// 戦闘速度切り替え
    /// </summary>
    private void ToggleSpeed()
    {
        currentSpeedIndex = (currentSpeedIndex + 1) % speedOptions.Length;
        float newSpeed = speedOptions[currentSpeedIndex];

        if (battleManager != null)
        {
            battleManager.SetBattleSpeed(newSpeed);
        }

        if (speedText != null)
            speedText.text = $"速度: {newSpeed}x";

        Debug.Log($"[BattleTestUI] 戦闘速度変更: {newSpeed}x");
    }

    /// <summary>
    /// 戦闘状態変更イベントハンドラ
    /// </summary>
    private void OnBattleStateChanged(BattleState newState)
    {
        Debug.Log($"[BattleTestUI] 戦闘状態変更: {newState}");

        if (battleStateText != null)
        {
            battleStateText.text = GetBattleStateText(newState);
        }

        // 状態に応じたUI制御
        switch (newState)
        {
            case BattleState.Idle:
                if (startBattleButton != null) startBattleButton.interactable = true;
                break;

            case BattleState.InProgress:
                if (startBattleButton != null) startBattleButton.interactable = false;
                break;

            case BattleState.Completed:
                if (startBattleButton != null) startBattleButton.interactable = true;
                break;
        }
    }

    /// <summary>
    /// 📝新規追加: 戦闘初期化完了イベントハンドラ
    /// </summary>
    private void OnBattleInitialized(BattleSetupData setupData)
    {
        Debug.Log($"[BattleTestUI] 戦闘初期化完了: {setupData.questName}");
        Debug.Log($"[BattleTestUI] 出現モンスター数: {setupData.spawnMonsterIds?.Count ?? 0}");

        if (setupData.spawnMonsterIds != null)
        {
            foreach (var monsterId in setupData.spawnMonsterIds)
            {
                var monster = QuestDataManager.Instance?.GetMonsterData(monsterId);
                if (monster != null)
                {
                    Debug.Log($"[BattleTestUI] 出現モンスター: {monster.monsterName} (ID: {monsterId})");
                }
            }
        }
    }

    /// <summary>
    /// 📝新規追加: 戦闘エラーイベントハンドラ
    /// </summary>
    private void OnBattleError(string errorMessage)
    {
        Debug.LogError($"[BattleTestUI] 戦闘エラー: {errorMessage}");

        if (battleStateText != null)
            battleStateText.text = $"エラー: {errorMessage}";

        if (startBattleButton != null)
            startBattleButton.interactable = true;
    }

    /// <summary>
    /// ターン情報更新
    /// </summary>
    private void OnCharacterTurnStart(BattleCharacterData character)
    {
        Debug.Log($"[BattleTestUI] ターン開始: {character.characterName} (HP: {character.currentHp}/{character.maxHp})");

        // ターン情報更新
        if (turnInfoText != null && battleManager != null)
        {
            int currentTurn = battleManager.GetCurrentTurnNumber();
            turnInfoText.text = $"ターン: {currentTurn}";
        }

        // HP情報更新
        UpdateHPDisplays();
    }

    /// <summary>
    /// 行動実行イベントハンドラ
    /// </summary>
    private void OnActionExecuted(ActionData action)
    {
        string actionType = action.IsNormalAttack() ? "通常攻撃" : "スキル使用";
        string skillName = action.IsSkillUse() ? action.skillName : "通常攻撃";

        Debug.Log($"[BattleTestUI] 行動実行: {action.actorName} -> {actionType}({skillName})");

        // ダメージ情報の表示
        if (action.damageResults != null && action.damageResults.Count > 0)
        {
            foreach (var damage in action.damageResults)
            {
                Debug.Log($"[BattleTestUI] ダメージ: {damage.targetName} に {damage.finalDamage} ダメージ");
            }
        }

        // HP表示更新
        UpdateHPDisplays();
    }

    /// <summary>
    /// 戦闘完了イベントハンドラ
    /// </summary>
    private void OnBattleCompleted(BattleResultData result)
    {
        string resultText = result.isVictory ? "勝利" : "敗北";
        Debug.Log($"[BattleTestUI] ========== 戦闘終了: {resultText} (ターン数: {result.totalTurns}) ==========");

        if (result.isVictory)
        {
            Debug.Log($"[BattleTestUI] 獲得経験値: {result.gainedExp}");
            Debug.Log($"[BattleTestUI] 獲得ゴールド: {result.gainedGold}");

            if (result.dropItems != null && result.dropItems.Count > 0)
            {
                Debug.Log($"[BattleTestUI] ドロップアイテム数: {result.dropItems.Count}");
            }
        }
    }

    /// <summary>
    /// HP表示更新
    /// </summary>
    private void UpdateHPDisplays()
    {
        if (battleManager == null) return;

        var playerCharacter = battleManager.GetPlayerCharacter();
        var enemyCharacters = battleManager.GetEnemyCharacters();

        // プレイヤーHP表示
        if (playerHPText != null && playerCharacter != null)
        {
            playerHPText.text = $"プレイヤーHP: {playerCharacter.currentHp}/{playerCharacter.maxHp}";
        }

        // 敵HP表示（複数敵対応）
        if (enemyHPText != null && enemyCharacters != null && enemyCharacters.Count > 0)
        {
            if (enemyCharacters.Count == 1)
            {
                var enemy = enemyCharacters[0];
                enemyHPText.text = $"敵HP: {enemy.currentHp}/{enemy.maxHp}";
            }
            else
            {
                // 📝修正: 複数敵の場合は生存数を表示
                var aliveEnemies = enemyCharacters.FindAll(e => e.isAlive);
                enemyHPText.text = $"敵: {aliveEnemies.Count}/{enemyCharacters.Count}体生存";
            }
        }
    }

    /// <summary>
    /// 戦闘状態テキスト取得
    /// </summary>
    private string GetBattleStateText(BattleState state)
    {
        switch (state)
        {
            case BattleState.Idle:
                return "戦闘待機中";
            case BattleState.Initializing:
                return "戦闘初期化中";
            case BattleState.InProgress:
                return "戦闘中";
            case BattleState.Completed:
                return "戦闘完了";
            default:
                return "不明な状態";
        }
    }

    /// <summary>
    /// Update処理でリアルタイム情報更新
    /// </summary>
    void Update()
    {
        // 戦闘中のみHP表示を定期更新
        if (battleManager != null && battleManager.CurrentState == BattleState.InProgress)
        {
            // 1秒に1回程度の頻度で更新
            if (Time.frameCount % 60 == 0)
            {
                UpdateHPDisplays();

                // 戦闘進行状況のデバッグ出力
                DebugBattleProgress();
            }
        }
    }

    /// <summary>
    /// 📝修正: 戦闘進行状況をデバッグ出力（BattleMonsterManager対応）
    /// </summary>
    private void DebugBattleProgress()
    {
        if (battleManager == null) return;

        try
        {
            Debug.Log($"[BattleTestUI] === 戦闘進行状況 ===");
            Debug.Log($"[BattleTestUI] 戦闘状態: {battleManager.CurrentState}");
            Debug.Log($"[BattleTestUI] 現在ターン: {battleManager.GetCurrentTurnNumber()}");

            // キャラクター状況確認
            var playerChar = battleManager.GetPlayerCharacter();
            var enemies = battleManager.GetEnemyCharacters();

            if (playerChar != null)
            {
                Debug.Log($"[BattleTestUI] プレイヤー: {playerChar.characterName} HP:{playerChar.currentHp}/{playerChar.maxHp} 生存:{playerChar.isAlive}");
            }
            else
            {
                Debug.LogWarning("[BattleTestUI] プレイヤーキャラクターがnullです");
            }

            if (enemies != null && enemies.Count > 0)
            {
                foreach (var enemy in enemies)
                {
                    // 📝修正: displayNameとinstanceIdも表示
                    string displayInfo = !string.IsNullOrEmpty(enemy.displayName) ? enemy.displayName : enemy.characterName;
                    Debug.Log($"[BattleTestUI] 敵: {displayInfo} (ID:{enemy.characterId}) HP:{enemy.currentHp}/{enemy.maxHp} 生存:{enemy.isAlive}");
                }
            }
            else
            {
                Debug.LogWarning("[BattleTestUI] 敵キャラクターが存在しません");
            }

            // BattleTurnManagerの状態確認
            if (BattleTurnManager.Instance != null)
            {
                Debug.Log($"[BattleTestUI] BattleTurnManager - ターン進行中: {BattleTurnManager.Instance.IsTurnInProgress}");
                Debug.Log($"[BattleTestUI] BattleTurnManager - 現在のアクター: {BattleTurnManager.Instance.CurrentActorId}");

                // ターン進行が停止している場合、自動で開始を試行
                if (!BattleTurnManager.Instance.IsTurnInProgress && battleManager.CurrentState == BattleState.InProgress)
                {
                    Debug.LogWarning("[BattleTestUI] ターン進行が停止しています。自動で開始を試行します...");
                    ForceStartTurnProcessing();
                }

                var currentActor = BattleTurnManager.Instance.GetCurrentActor();
                if (currentActor != null)
                {
                    string actorDisplayInfo = !string.IsNullOrEmpty(currentActor.displayName) ? currentActor.displayName : currentActor.characterName;
                    Debug.Log($"[BattleTestUI] 現在の行動者: {actorDisplayInfo} (ID:{currentActor.characterId})");
                }
                else
                {
                    Debug.LogWarning("[BattleTestUI] 現在の行動者がnullです");
                }
            }
            else
            {
                Debug.LogError("[BattleTestUI] BattleTurnManager.Instanceがnullです");
            }

            // 📝追加: BattleMonsterManagerの状態確認
            if (BattleMonsterManager.Instance != null)
            {
                var allMonsters = BattleMonsterManager.Instance.GetAllMonsters();
                var aliveMonsters = BattleMonsterManager.Instance.GetAliveMonsters();
                Debug.Log($"[BattleTestUI] BattleMonsterManager - 総モンスター数: {allMonsters.Count}, 生存数: {aliveMonsters.Count}");
            }
            else
            {
                Debug.LogWarning("[BattleTestUI] BattleMonsterManager.Instanceがnullです");
            }

            Debug.Log($"[BattleTestUI] === 戦闘進行状況終了 ===");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BattleTestUI] 戦闘進行状況確認エラー: {e.Message}");
        }
    }

    /// <summary>
    /// テスト用メソッド：手動で戦闘ログ出力
    /// </summary>
    [ContextMenu("戦闘ログ出力")]
    public void DumpBattleLog()
    {
        if (battleManager == null) return;

        var battleLog = battleManager.GetBattleHistory();
        if (battleLog != null && battleLog.Count > 0)
        {
            Debug.Log($"[BattleTestUI] ========== 戦闘ログ (総数: {battleLog.Count}) ==========");
            foreach (var action in battleLog)
            {
                Debug.Log($"[BattleTestUI] {action.GetActionSummary()}");
            }
        }
        else
        {
            Debug.Log("[BattleTestUI] 戦闘ログが空です");
        }
    }

    /// <summary>
    /// 📝修正: 戦闘システム状態を詳細出力（BattleMonsterManager対応）
    /// </summary>
    [ContextMenu("戦闘システム状態確認")]
    public void DumpBattleSystemState()
    {
        Debug.Log("=== 戦闘システム状態確認 ===");

        // BattleManager確認
        if (BattleManager.Instance != null)
        {
            Debug.Log($"✓ BattleManager: 初期化済み={BattleManager.Instance.IsInitialized}, 状態={BattleManager.Instance.CurrentState}");
        }
        else
        {
            Debug.LogError("✗ BattleManager: Instanceがnull");
        }

        // BattleTurnManager確認
        if (BattleTurnManager.Instance != null)
        {
            Debug.Log($"✓ BattleTurnManager: ターン進行中={BattleTurnManager.Instance.IsTurnInProgress}");
            Debug.Log($"  現在ターン={BattleTurnManager.Instance.CurrentTurnNumber}");
            Debug.Log($"  現在アクター={BattleTurnManager.Instance.CurrentActorId}");
        }
        else
        {
            Debug.LogError("✗ BattleTurnManager: Instanceがnull");
        }

        // BattleCalculationManager確認
        if (BattleCalculationManager.Instance != null)
        {
            Debug.Log("✓ BattleCalculationManager: 利用可能");
        }
        else
        {
            Debug.LogError("✗ BattleCalculationManager: Instanceがnull");
        }

        // 📝追加: BattleMonsterManager確認
        if (BattleMonsterManager.Instance != null)
        {
            var allMonsters = BattleMonsterManager.Instance.GetAllMonsters();
            var aliveMonsters = BattleMonsterManager.Instance.GetAliveMonsters();
            Debug.Log($"✓ BattleMonsterManager: 総モンスター数={allMonsters.Count}, 生存数={aliveMonsters.Count}");
        }
        else
        {
            Debug.LogError("✗ BattleMonsterManager: Instanceがnull");
        }

        // SaveDataManager確認
        if (SaveDataManager.Instance != null)
        {
            Debug.Log($"✓ SaveDataManager: データ読み込み済み={SaveDataManager.Instance.IsDataLoaded}");
        }
        else
        {
            Debug.LogError("✗ SaveDataManager: Instanceがnull");
        }

        // QuestDataManager確認
        if (QuestDataManager.Instance != null)
        {
            Debug.Log($"✓ QuestDataManager: データ読み込み済み={QuestDataManager.Instance.IsDataLoaded}");
        }
        else
        {
            Debug.LogError("✗ QuestDataManager: Instanceがnull");
        }

        Debug.Log("=== 戦闘システム状態確認終了 ===");
    }

    /// <summary>
    /// テスト用メソッド：ターン処理を手動開始
    /// </summary>
    [ContextMenu("ターン処理開始")]
    public void ForceStartTurnProcessing()
    {
        if (BattleTurnManager.Instance != null)
        {
            Debug.Log("[BattleTestUI] 手動でターン処理を開始します");
            try
            {
                // まず行動順序を初期化
                BattleTurnManager.Instance.InitializeTurnOrder();
                Debug.Log("[BattleTestUI] 行動順序初期化完了");

                // ターン処理開始
                BattleTurnManager.Instance.StartTurnProcessing();
                Debug.Log("[BattleTestUI] ターン処理開始完了");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BattleTestUI] ターン処理開始エラー: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("[BattleTestUI] BattleTurnManager.Instanceがnullです");
        }
    }

    /// <summary>
    /// 📝修正: プレイヤーステータス詳細確認（BattleMonsterManager対応）
    /// </summary>
    [ContextMenu("プレイヤーステータス確認")]
    public void DumpPlayerStats()
    {
        Debug.Log("=== プレイヤーステータス詳細確認 ===");

        try
        {
            // SaveDataからユーザーデータ確認
            var userData = SaveDataManager.Instance?.CurrentSaveData;
            if (userData != null)
            {
                Debug.Log($"[PlayerStats] ユーザーレベル: {userData.playerLevel}");
                Debug.Log($"[PlayerStats] 装備武器数: {userData.equippedWeaponIds?.Count ?? 0}");
                Debug.Log($"[PlayerStats] 装備防具数: {userData.equippedArmorIds?.Count ?? 0}");
                Debug.Log($"[PlayerStats] 装備アクセサリ数: {userData.equippedAccessoryIds?.Count ?? 0}");
                Debug.Log($"[PlayerStats] 戦闘スキル1: {userData.battleSkill1Id}");
                Debug.Log($"[PlayerStats] 戦闘スキル2: {userData.battleSkill2Id}");

                // 装備詳細
                if (userData.equippedWeaponIds != null)
                {
                    foreach (var weaponId in userData.equippedWeaponIds)
                    {
                        Debug.Log($"[PlayerStats] 装備武器ID: {weaponId}");
                    }
                }

                if (userData.equippedArmorIds != null)
                {
                    foreach (var armorId in userData.equippedArmorIds)
                    {
                        Debug.Log($"[PlayerStats] 装備防具ID: {armorId}");
                    }
                }
            }
            else
            {
                Debug.LogError("[PlayerStats] UserSaveDataがnullです");
            }

            // 戦闘中のプレイヤーステータス確認
            if (battleManager != null)
            {
                var playerChar = battleManager.GetPlayerCharacter();
                if (playerChar != null)
                {
                    Debug.Log($"[BattleStats] プレイヤー名: {playerChar.characterName}");
                    Debug.Log($"[BattleStats] HP: {playerChar.currentHp}/{playerChar.maxHp}");
                    Debug.Log($"[BattleStats] 攻撃力: {playerChar.offense}");
                    Debug.Log($"[BattleStats] 防御力: {playerChar.defense}");
                    Debug.Log($"[BattleStats] 速度: {playerChar.speed}");
                    Debug.Log($"[BattleStats] クリティカル率: {playerChar.criticalRate}");
                    Debug.Log($"[BattleStats] 火属性攻撃: {playerChar.fireOffence}");
                    Debug.Log($"[BattleStats] 水属性攻撃: {playerChar.waterOffence}");
                    Debug.Log($"[BattleStats] 風属性攻撃: {playerChar.windOffence}");
                    Debug.Log($"[BattleStats] 土属性攻撃: {playerChar.earthOffence}");
                    Debug.Log($"[BattleStats] 利用可能スキル数: {playerChar.availableSkills?.Count ?? 0}");

                    if (playerChar.availableSkills != null)
                    {
                        foreach (var skill in playerChar.availableSkills)
                        {
                            Debug.Log($"[BattleStats] スキル: {skill.skillName} (ID: {skill.skillId})");
                        }
                    }
                }
                else
                {
                    Debug.LogError("[BattleStats] プレイヤーキャラクターがnullです");
                }
            }
            else
            {
                Debug.LogError("[BattleStats] BattleManagerがnullです");
            }

            // BattleSetupDataの確認
            var setupData = battleManager?.GetCurrentBattleSetup();
            if (setupData != null)
            {
                Debug.Log($"[SetupData] プレイヤーレベル: {setupData.playerLevel}");
                Debug.Log($"[SetupData] 装備アイテム数: {setupData.playerEquipmentIds?.Count ?? 0}");
                Debug.Log($"[SetupData] 戦闘スキル数: {setupData.playerSkillIds?.Count ?? 0}");

                // 修正: EquipmentTotalStats は構造体なので null チェック不要
                // setupData.playerStats は常に有効な構造体インスタンス
                Debug.Log($"[SetupData] 合計HP: {setupData.playerStats.hp}");
                Debug.Log($"[SetupData] 合計攻撃力: {setupData.playerStats.offense}");
                Debug.Log($"[SetupData] 合計防御力: {setupData.playerStats.defense}");
                Debug.Log($"[SetupData] 合計速度: {setupData.playerStats.speed}");
                Debug.Log($"[SetupData] 合計火属性: {setupData.playerStats.fireOffence}");
            }
            else
            {
                Debug.LogError("[SetupData] BattleSetupDataがnullです");
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerStats] ステータス確認エラー: {e.Message}");
        }

        Debug.Log("=== プレイヤーステータス確認終了 ===");
    }

    /// <summary>
    /// テスト用メソッド：装備ステータス計算テスト
    /// </summary>
    [ContextMenu("装備ステータス計算テスト")]
    public void TestEquipmentCalculation()
    {
        Debug.Log("=== 装備ステータス計算テスト ===");

        try
        {
            var userData = SaveDataManager.Instance?.CurrentSaveData;
            if (userData == null)
            {
                Debug.LogError("[EquipTest] UserSaveDataがnullです");
                return;
            }

            // 装備データの確認
            Debug.Log($"[EquipTest] 全装備数: {userData.equipments?.Count ?? 0}");
            Debug.Log($"[EquipTest] 装備武器ID数: {userData.equippedWeaponIds?.Count ?? 0}");
            Debug.Log($"[EquipTest] 装備防具ID数: {userData.equippedArmorIds?.Count ?? 0}");
            Debug.Log($"[EquipTest] 装備アクセサリID数: {userData.equippedAccessoryIds?.Count ?? 0}");

            // 装備中のアイテム詳細確認
            if (userData.equipments != null)
            {
                var equippedItems = userData.equipments.FindAll(e => e.isEquipped);
                Debug.Log($"[EquipTest] 装備中アイテム数: {equippedItems.Count}");

                foreach (var item in equippedItems)
                {
                    Debug.Log($"[EquipTest] 装備中: {item.userEquipmentId} - キャラ:{item.equippedCharacterId}");

                    // 修正: EquipmentMasterDataを取得してCalculateTotalStatsに渡す
                    var masterData = MasterDataManager.Instance?.GetEquipmentData(item.equipmentMasterId);
                    if (masterData != null)
                    {
                        var totalStats = item.CalculateTotalStats(masterData);
                        Debug.Log($"[EquipTest] ステータス - HP:{totalStats.hp}, ATK:{totalStats.offense}, DEF:{totalStats.defense}");
                    }
                    else
                    {
                        Debug.LogError($"[EquipTest] 装備ID {item.equipmentMasterId} のマスターデータが見つかりません");
                    }
                }
            }

            // 手動で装備合計ステータス計算（UserSaveDataに該当メソッドがあるかチェック）
            // ※ UserSaveDataに装備ステータス合計計算メソッドが見当たらないため、手動計算
            Debug.Log("[EquipTest] 手動装備ステータス合計計算を実行します");
            CalculateManualEquipmentStats(userData);

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EquipTest] 装備計算テストエラー: {e.Message}");
        }

        Debug.Log("=== 装備ステータス計算テスト終了 ===");
    }

    /// <summary>
    /// 手動で装備ステータス合計を計算
    /// </summary>
    private void CalculateManualEquipmentStats(UserSaveData userData)
    {
        var totalStats = new EquipmentTotalStats();

        if (userData.equipments != null)
        {
            foreach (var equipment in userData.equipments)
            {
                if (equipment.isEquipped)
                {
                    // 修正: EquipmentMasterDataを取得してCalculateTotalStatsに渡す
                    var masterData = MasterDataManager.Instance?.GetEquipmentData(equipment.equipmentMasterId);
                    if (masterData != null)
                    {
                        var equipStats = equipment.CalculateTotalStats(masterData);
                        totalStats.hp += equipStats.hp;
                        totalStats.offense += equipStats.offense;
                        totalStats.defense += equipStats.defense;
                        totalStats.speed += equipStats.speed;
                        totalStats.criticalRate += equipStats.criticalRate;
                        totalStats.criticalDamageRate += equipStats.criticalDamageRate;
                        totalStats.fireOffence += equipStats.fireOffence;
                        totalStats.waterOffence += equipStats.waterOffence;
                        totalStats.windOffence += equipStats.windOffence;
                        totalStats.earthOffence += equipStats.earthOffence;

                        Debug.Log($"[ManualCalc] 装備 {equipment.userEquipmentId}: HP+{equipStats.hp}, ATK+{equipStats.offense}, DEF+{equipStats.defense}");
                    }
                    else
                    {
                        Debug.LogError($"[ManualCalc] 装備ID {equipment.equipmentMasterId} のマスターデータが見つかりません");
                    }
                }
            }
        }

        Debug.Log($"[ManualCalc] 装備合計ステータス:");
        Debug.Log($"[ManualCalc] - HP: +{totalStats.hp}");
        Debug.Log($"[ManualCalc] - 攻撃力: +{totalStats.offense}");
        Debug.Log($"[ManualCalc] - 防御力: +{totalStats.defense}");
        Debug.Log($"[ManualCalc] - 速度: +{totalStats.speed}");
        Debug.Log($"[ManualCalc] - 火属性: +{totalStats.fireOffence}");
        Debug.Log($"[ManualCalc] - 水属性: +{totalStats.waterOffence}");
        Debug.Log($"[ManualCalc] - 風属性: +{totalStats.windOffence}");
        Debug.Log($"[ManualCalc] - 土属性: +{totalStats.earthOffence}");
    }

    /// <summary>
    /// 📝新規追加: モンスター詳細確認テスト
    /// </summary>
    [ContextMenu("モンスター詳細確認")]
    public void DumpMonsterDetails()
    {
        Debug.Log("=== モンスター詳細確認 ===");

        try
        {
            // BattleManagerからモンスター情報取得
            if (battleManager != null)
            {
                var enemies = battleManager.GetEnemyCharacters();
                Debug.Log($"[MonsterTest] BattleManagerから取得した敵数: {enemies?.Count ?? 0}");

                if (enemies != null)
                {
                    foreach (var enemy in enemies)
                    {
                        Debug.Log($"[MonsterTest] 敵情報:");
                        Debug.Log($"  characterId: {enemy.characterId}");
                        Debug.Log($"  instanceId: {enemy.instanceId}");
                        Debug.Log($"  characterName: {enemy.characterName}");
                        Debug.Log($"  displayName: {enemy.displayName}");
                        Debug.Log($"  positionIndex: {enemy.positionIndex}");
                        Debug.Log($"  HP: {enemy.currentHp}/{enemy.maxHp}");
                        Debug.Log($"  生存: {enemy.isAlive}");
                        Debug.Log($"  battlePosition: {enemy.battlePosition}");
                        Debug.Log("");
                    }
                }
            }

            // BattleMonsterManagerから情報取得
            if (BattleMonsterManager.Instance != null)
            {
                var allMonsters = BattleMonsterManager.Instance.GetAllMonsters();
                var aliveMonsters = BattleMonsterManager.Instance.GetAliveMonsters();

                Debug.Log($"[MonsterManager] BattleMonsterManagerから取得:");
                Debug.Log($"  総モンスター数: {allMonsters.Count}");
                Debug.Log($"  生存モンスター数: {aliveMonsters.Count}");

                foreach (var monster in allMonsters)
                {
                    Debug.Log($"[MonsterManager] モンスター情報:");
                    Debug.Log($"  characterId: {monster.characterId}");
                    Debug.Log($"  instanceId: {monster.instanceId}");
                    Debug.Log($"  displayName: {monster.displayName}");
                    Debug.Log($"  masterDataId: {monster.masterDataId}");
                    Debug.Log($"  生存: {monster.isAlive}");
                    Debug.Log("");
                }
            }
            else
            {
                Debug.LogError("[MonsterManager] BattleMonsterManager.Instanceがnullです");
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MonsterTest] モンスター確認エラー: {e.Message}");
        }

        Debug.Log("=== モンスター詳細確認終了 ===");
    }

    /// <summary>
    /// テスト用メソッド：クエストデータの詳細確認
    /// </summary>
    [ContextMenu("クエストデータ詳細確認")]
    public void DumpQuestDataDetails()
    {
        Debug.Log($"=== クエストデータ詳細確認 (現在のテストID: {testQuestId}) ===");

        try
        {
            // 現在のテストクエストの詳細確認
            var questData = QuestDataManager.Instance.GetQuestData(testQuestId);
            if (questData != null)
            {
                Debug.Log($"選択されたクエスト:");
                Debug.Log($"  ID: {questData.questId}");
                Debug.Log($"  名前: {questData.questName}");
                Debug.Log($"  spawnMonsterId1: {questData.spawnMonsterId1}");
                Debug.Log($"  spawnMonsterId2: {questData.spawnMonsterId2}");
                Debug.Log($"  spawnMonsterId3: {questData.spawnMonsterId3}");

                // GetSpawnMonsterIds()の結果確認
                var monsterIds = questData.GetSpawnMonsterIds();
                Debug.Log($"  GetSpawnMonsterIds()結果: [{string.Join(", ", monsterIds)}]");
            }
            else
            {
                Debug.LogError($"クエストID {testQuestId} のデータが見つかりません");
            }

            Debug.Log("");
            Debug.Log("=== 全クエストの比較 ===");

            // 複数のクエストを比較確認
            for (int questId = 1; questId <= 10; questId++)
            {
                var quest = QuestDataManager.Instance.GetQuestData(questId);
                if (quest != null)
                {
                    var monsters = quest.GetSpawnMonsterIds();
                    Debug.Log($"Quest {questId}: {quest.questName}");
                    Debug.Log($"  Raw IDs: [{quest.spawnMonsterId1}, {quest.spawnMonsterId2}, {quest.spawnMonsterId3}]");
                    Debug.Log($"  Filtered: [{string.Join(", ", monsters)}]");

                    // 各モンスターの名前も確認
                    foreach (var monsterId in monsters)
                    {
                        var monsterData = QuestDataManager.Instance.GetMonsterData(monsterId);
                        if (monsterData != null)
                        {
                            Debug.Log($"    Monster {monsterId}: {monsterData.monsterName}");
                        }
                        else
                        {
                            Debug.LogError($"    Monster {monsterId}: データが見つかりません");
                        }
                    }
                    Debug.Log("");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"クエストデータ確認エラー: {e.Message}");
            Debug.LogError($"スタックトレース: {e.StackTrace}");
        }

        Debug.Log("=== クエストデータ詳細確認終了 ===");
    }

    /// <summary>
    /// テスト用メソッド：BattleSetupDataの詳細確認
    /// </summary>
    [ContextMenu("BattleSetupData詳細確認")]
    public void DumpBattleSetupData()
    {
        Debug.Log($"=== BattleSetupData詳細確認 ===");

        try
        {
            var userData = SaveDataManager.Instance.CurrentSaveData;
            var questData = QuestDataManager.Instance.GetQuestData(testQuestId);

            if (userData == null)
            {
                Debug.LogError("UserSaveDataがnullです");
                return;
            }

            if (questData == null)
            {
                Debug.LogError($"QuestMasterData (ID: {testQuestId}) がnullです");
                return;
            }

            // BattleSetupData作成前のデータ確認
            Debug.Log("=== 作成前のデータ確認 ===");
            Debug.Log($"QuestID: {questData.questId}");
            Debug.Log($"Quest名: {questData.questName}");

            var rawMonsterIds = questData.GetSpawnMonsterIds();
            Debug.Log($"Quest出現モンスターID: [{string.Join(", ", rawMonsterIds)}]");

            // BattleSetupData作成
            Debug.Log("=== BattleSetupData作成中 ===");
            var battleSetup = BattleSetupData.CreateFromUserData(userData, questData);

            if (battleSetup != null)
            {
                Debug.Log("BattleSetupData作成成功");
                Debug.Log($"  questId: {battleSetup.questId}");
                Debug.Log($"  questName: {battleSetup.questName}");
                Debug.Log($"  spawnMonsterIds: [{string.Join(", ", battleSetup.spawnMonsterIds)}]");
                Debug.Log($"  spawnMonsterIds.Count: {battleSetup.spawnMonsterIds.Count}");

                // 各モンスターIDの詳細確認
                for (int i = 0; i < battleSetup.spawnMonsterIds.Count; i++)
                {
                    int monsterId = battleSetup.spawnMonsterIds[i];
                    var monsterData = QuestDataManager.Instance.GetMonsterData(monsterId);
                    if (monsterData != null)
                    {
                        Debug.Log($"    Monster[{i}]: ID={monsterId}, Name={monsterData.monsterName}");
                    }
                    else
                    {
                        Debug.LogError($"    Monster[{i}]: ID={monsterId}, データが見つかりません");
                    }
                }
            }
            else
            {
                Debug.LogError("BattleSetupDataの作成に失敗しました");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BattleSetupData確認エラー: {e.Message}");
            Debug.LogError($"スタックトレース: {e.StackTrace}");
        }

        Debug.Log("=== BattleSetupData詳細確認終了 ===");
    }
}