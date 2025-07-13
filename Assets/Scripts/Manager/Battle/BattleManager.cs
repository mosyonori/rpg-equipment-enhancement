using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 戦闘システム全体の統合制御
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

    [Header("修正: UI待機設定")]
    [SerializeField] private float uiReadyTimeout = 10.0f;
    [SerializeField] private float uiReadyCheckInterval = 0.2f;
    [SerializeField] private float battleStartDelay = 1.0f;

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

    // 修正: UI層参照をコメントアウト（一時的）
    // private BattleUI battleUI;

    #region Unity Lifecycle

    private void Awake()
    {
        // 修正: 自身が非アクティブの場合、強制的に有効化
        if (!gameObject.activeInHierarchy)
        {
            Debug.Log("[BattleManager] BattleManagerが非アクティブのため、有効化します");
            gameObject.SetActive(true);
        }

        // シングルトン設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[BattleManager] BattleManager Awake - シングルトン設定完了");
            InitializeManager();
        }
        else
        {
            Debug.Log("[BattleManager] BattleManager重複インスタンス検出 - 削除");
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
    /// 修正: 依存する全Managerの存在確認
    /// </summary>
    private bool CheckDependencies()
    {
        // 必須Manager確認
        if (SaveDataManager.Instance == null || !SaveDataManager.Instance.IsDataLoaded)
        {
            Log("SaveDataManager未初期化");
            return false;
        }

        if (MasterDataManager.Instance == null || !MasterDataManager.Instance.IsDataLoaded)
        {
            Log("MasterDataManager未初期化");
            return false;
        }

        // QuestDataManagerの確認を追加
        if (QuestDataManager.Instance == null || !QuestDataManager.Instance.IsDataLoaded)
        {
            Log("QuestDataManager未初期化");
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

        // 修正: BattleUI関連のチェックを一時的にコメントアウト
        // battleUI = FindAnyObjectByType<BattleUI>(); 
        // if (battleUI == null)
        // {
        //     Log("BattleUIが見つかりません");
        //     return false;
        // }

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

            // 修正: 装備データの詳細ログ出力
            LogPlayerEquipmentDetails(userData);

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
    /// 修正: プレイヤーの装備データの詳細ログ出力（HomeManagerパターンを参考）
    /// </summary>
    private void LogPlayerEquipmentDetails(UserSaveData userData)
    {
        Log("=== プレイヤー装備データ詳細確認 ===");

        Log($"全装備数: {userData.equipments?.Count ?? 0}");
        Log($"装備武器ID数: {userData.equippedWeaponIds?.Count ?? 0}");
        Log($"装備防具ID数: {userData.equippedArmorIds?.Count ?? 0}");
        Log($"装備アクセサリID数: {userData.equippedAccessoryIds?.Count ?? 0}");
        Log($"戦闘スキル1: {userData.battleSkill1Id}");
        Log($"戦闘スキル2: {userData.battleSkill2Id}");

        // 装備中のアイテム詳細確認（HomeManager のUpdateEquipmentSummary相当）
        if (userData.equipments != null)
        {
            var equippedItems = userData.equipments.FindAll(e => e.isEquipped);
            Log($"装備中アイテム数: {equippedItems.Count}");

            int totalPower = 0;
            foreach (var item in equippedItems)
            {
                var masterData = MasterDataManager.Instance?.GetEquipmentData(item.equipmentMasterId);
                if (masterData != null)
                {
                    var totalStats = item.CalculateTotalStats(masterData);
                    Log($"装備: {masterData.equipmentName} - HP:{totalStats.hp}, ATK:{totalStats.offense}, DEF:{totalStats.defense}");

                    // 戦闘力計算（UserDataUtility のCalculateTotalPowerと同じロジック）
                    totalPower += totalStats.hp / 10;
                    totalPower += totalStats.offense * 2;
                    totalPower += totalStats.defense;
                    totalPower += totalStats.speed;
                    totalPower += totalStats.fireOffence;
                    totalPower += totalStats.waterOffence;
                    totalPower += totalStats.windOffence;
                    totalPower += totalStats.earthOffence;
                }
                else
                {
                    LogError($"装備ID {item.equipmentMasterId} のマスターデータが見つかりません");
                }
            }

            Log($"合計戦闘力: {totalPower}");
        }

        Log("=== 装備データ詳細確認終了 ===");
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

        // TimeScaleは使わず、アニメーション・処理待機系を調整
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
    /// 修正: 生存している敵キャラクター一覧を取得
    /// </summary>
    public List<BattleCharacterData> GetAliveEnemies()
    {
        return allCharacters?.FindAll(c => !c.isPlayer && c.isAlive) ?? new List<BattleCharacterData>();
    }

    /// <summary>
    /// 修正: 生存しているプレイヤーキャラクター一覧を取得
    /// </summary>
    public List<BattleCharacterData> GetAlivePlayers()
    {
        return allCharacters?.FindAll(c => c.isPlayer && c.isAlive) ?? new List<BattleCharacterData>();
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
    /// 修正: 戦闘初期化処理（エラー修正版）
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

            // 修正: allCharactersの内容を詳細確認
            Log($"作成されたキャラクター総数: {allCharacters?.Count ?? 0}");
            if (allCharacters != null)
            {
                foreach (var character in allCharacters)
                {
                    Log($"キャラクター: {character.characterName} (プレイヤー:{character.isPlayer}, 生存:{character.isAlive}, HP:{character.currentHp}/{character.maxHp})");
                }
            }

            // 戦闘データManager初期化
            Log("BattleDataManager初期化開始");
            battleDataManager.InitializeBattle(allCharacters, currentBattleSetup);
            Log("BattleDataManager初期化完了");

            // BattleTurnManager の依存関係設定
            Log("BattleTurnManager.SetDataManager呼び出し");
            battleTurnManager.SetDataManager(battleDataManager);
            Log("BattleTurnManager.SetDataManager完了");

            Log($"BattleTurnManager.SetTurnLimit呼び出し: {currentBattleSetup.turnLimit}");
            battleTurnManager.SetTurnLimit(currentBattleSetup.turnLimit);
            Log("BattleTurnManager.SetTurnLimit完了");

            Log("BattleTurnManager.InitializeTurnOrder呼び出し");
            battleTurnManager.InitializeTurnOrder();
            Log("BattleTurnManager.InitializeTurnOrder完了");

            // 修正: BattleTurnManagerの状態を確認
            Log($"BattleTurnManager.Instance存在確認: {(BattleTurnManager.Instance != null ? "存在" : "null")}");
            Log($"battleTurnManager参照確認: {(battleTurnManager != null ? "存在" : "null")}");
            Log($"battleTurnManager == BattleTurnManager.Instance: {(battleTurnManager == BattleTurnManager.Instance)}");

            // BattleCalculationManager にも BattleDataManager の参照を設定
            SetCalculationManagerDependencies();

            // 戦闘結果データ初期化
            currentBattleResult = new BattleResultData();

            // 初期化完了通知
            OnBattleInitialized?.Invoke(currentBattleSetup);
            ChangeState(BattleState.InProgress);

            // 戦闘開始
            Log("BattleMainLoop開始前");
            battleCoroutine = StartCoroutine(BattleMainLoop());
            Log("BattleMainLoop開始後");

            Log("戦闘初期化処理完了");
        }
        catch (Exception e)
        {
            LogError($"戦闘初期化エラー: {e.Message}");
            LogError($"スタックトレース: {e.StackTrace}");
            OnBattleError?.Invoke("戦闘の初期化に失敗しました");
            ChangeState(BattleState.Idle);
        }
    }

    /// <summary>
    /// 戦闘参加キャラクター作成（修正版）
    /// </summary>
    private void CreateBattleCharacters()
    {
        allCharacters.Clear();
        Log("=== 戦闘キャラクター作成開始 ===");

        // プレイヤーキャラクター作成
        try
        {
            var playerCharacterMaster = MasterDataManager.Instance.GetCharacterData(1);
            if (playerCharacterMaster != null)
            {
                Log($"プレイヤーキャラクターマスター取得: {playerCharacterMaster.CharacterName}");

                // 装備込みステータス計算
                var userData = SaveDataManager.Instance.CurrentSaveData;
                var equipmentStats = CalculatePlayerEquipmentStats(userData);

                Log($"計算済み装備ステータス: HP+{equipmentStats.hp}, ATK+{equipmentStats.offense}, DEF+{equipmentStats.defense}");

                var playerChar = BattleCharacterData.CreateFromCharacterMaster(
                    playerCharacterMaster,
                    equipmentStats
                );

                // プレイヤーレベル反映
                playerChar.characterLevel = userData.playerLevel;

                allCharacters.Add(playerChar);
                Log($"プレイヤーキャラクター作成完了: {playerChar.characterName} (最終HP:{playerChar.maxHp}, ATK:{playerChar.offense}, Level:{playerChar.characterLevel})");
            }
            else
            {
                LogError("プレイヤーキャラクターマスターデータが見つかりません！");

                // フォールバック: 最小限のプレイヤーキャラクター作成
                var fallbackPlayer = CreateFallbackPlayer();
                allCharacters.Add(fallbackPlayer);
                LogError("フォールバックプレイヤーキャラクターを作成しました");
            }
        }
        catch (Exception e)
        {
            LogError($"プレイヤーキャラクター作成エラー: {e.Message}");

            // フォールバック: 最小限のプレイヤーキャラクター作成
            var fallbackPlayer = CreateFallbackPlayer();
            allCharacters.Add(fallbackPlayer);
            LogError("例外によりフォールバックプレイヤーキャラクターを作成しました");
        }

        // 敵モンスター作成（詳細ログ付き）
        Log($"敵モンスター作成開始: {currentBattleSetup.spawnMonsterIds.Count}体");

        foreach (var monsterId in currentBattleSetup.spawnMonsterIds)
        {
            Log($"モンスターID {monsterId} の作成を実行");

            try
            {
                var monsterMaster = QuestDataManager.Instance.GetMonsterData(monsterId);
                if (monsterMaster != null)
                {
                    var monsterChar = BattleCharacterData.CreateFromMonsterMaster(monsterMaster);
                    allCharacters.Add(monsterChar);
                    Log($"敵モンスター作成成功: {monsterChar.characterName} (HP:{monsterChar.maxHp}, ATK:{monsterChar.offense})");
                }
                else
                {
                    LogError($"モンスターID {monsterId} のマスターデータが見つかりません！");
                    LogQuestDataManagerStatus(); // デバッグ情報出力

                    // フォールバック: デフォルトモンスター作成
                    var fallbackMonster = CreateFallbackMonster(monsterId);
                    allCharacters.Add(fallbackMonster);
                    LogError($"フォールバックモンスター{monsterId}を作成しました");
                }
            }
            catch (Exception e)
            {
                LogError($"モンスターID {monsterId} 作成エラー: {e.Message}");

                // フォールバック: デフォルトモンスター作成
                var fallbackMonster = CreateFallbackMonster(monsterId);
                allCharacters.Add(fallbackMonster);
                LogError($"例外によりフォールバックモンスター{monsterId}を作成しました");
            }
        }

        // 修正: メソッド追加後なので正常に動作
        int playerCount = GetAlivePlayers().Count;
        int enemyCount = GetAliveEnemies().Count;

        Log($"戦闘キャラクター作成完了: 合計{allCharacters.Count}体 (プレイヤー:{playerCount}体, 敵:{enemyCount}体)");

        // 作成されたキャラクターの詳細情報
        foreach (var character in allCharacters)
        {
            Log($"  作成済み: {character.characterName} ({(character.isPlayer ? "プレイヤー" : "敵")}) HP:{character.maxHp} Level:{character.characterLevel}");
        }

        Log("=== 戦闘キャラクター作成終了 ===");
    }

    /// <summary>
    /// フォールバックプレイヤーキャラクター作成
    /// </summary>
    private BattleCharacterData CreateFallbackPlayer()
    {
        var userData = SaveDataManager.Instance.CurrentSaveData;

        return new BattleCharacterData
        {
            characterId = "player",
            characterName = "プレイヤー",
            isPlayer = true,
            isAlive = true,
            characterLevel = userData?.playerLevel ?? 1,
            maxHp = 100,
            currentHp = 100,
            offense = 20,
            defense = 15,
            speed = 10,
            criticalRate = 5,
            criticalDamageRate = 150,
            availableSkills = new List<BattleSkillData>
        {
            new BattleSkillData
            {
                skillId = 1,
                skillName = "通常攻撃",
                currentCoolTime = 0,
                maxCoolTime = 0,
                isUsable = true
            }
        },
            statusEffects = new List<StatusEffectData>()
        };
    }

    /// <summary>
    /// フォールバックモンスター作成
    /// </summary>
    private BattleCharacterData CreateFallbackMonster(int monsterId)
    {
        return new BattleCharacterData
        {
            characterId = $"monster_{monsterId}",
            characterName = $"敵モンスター{monsterId}",
            isPlayer = false,
            isAlive = true,
            characterLevel = 1,
            maxHp = 80,
            currentHp = 80,
            offense = 15,
            defense = 10,
            speed = 8,
            criticalRate = 3,
            criticalDamageRate = 130,
            availableSkills = new List<BattleSkillData>
        {
            new BattleSkillData
            {
                skillId = 1,
                skillName = "モンスター攻撃",
                currentCoolTime = 0,
                maxCoolTime = 2,
                isUsable = true
            }
        },
            statusEffects = new List<StatusEffectData>()
        };
    }

    /// <summary>
    /// QuestDataManagerの状態をログ出力（デバッグ用）
    /// </summary>
    private void LogQuestDataManagerStatus()
    {
        if (QuestDataManager.Instance == null)
        {
            LogError("QuestDataManager.Instance が null です");
            return;
        }

        if (!QuestDataManager.Instance.IsDataLoaded)
        {
            LogError("QuestDataManager.IsDataLoaded が false です");
            return;
        }

        var allMonsters = QuestDataManager.Instance.GetMonsterDataList();
        LogError($"利用可能なモンスター数: {allMonsters.Count}");

        if (allMonsters.Count > 0)
        {
            LogError($"登録済みモンスターID例: {string.Join(", ", allMonsters.Take(5).Select(m => m.monsterId))}");
        }
    }

    /// <summary>
    /// 修正: プレイヤーの装備ステータス計算（HomeManager のEquipmentSummaryData.CreateFromSaveDataパターンを参考）
    /// </summary>
    private EquipmentTotalStats CalculatePlayerEquipmentStats(UserSaveData userData)
    {
        var totalStats = new EquipmentTotalStats();

        if (userData?.equipments == null)
        {
            Log("装備データが存在しません");
            return totalStats;
        }

        Log("=== 装備ステータス計算開始 ===");

        // 装備中のアイテムのみを対象にステータス計算
        var equippedItems = userData.equipments.FindAll(e => e.isEquipped);
        Log($"装備中アイテム数: {equippedItems.Count}");

        foreach (var equipment in equippedItems)
        {
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

                Log($"装備 {masterData.equipmentName}: HP+{equipStats.hp}, ATK+{equipStats.offense}, DEF+{equipStats.defense}");
            }
            else
            {
                LogError($"装備ID {equipment.equipmentMasterId} のマスターデータが見つかりません");
            }
        }

        Log($"=== 装備ステータス合計: HP+{totalStats.hp}, ATK+{totalStats.offense}, DEF+{totalStats.defense} ===");
        return totalStats;
    }

    /// <summary>
    /// 修正: 戦闘メインループに自動開始機能を追加
    /// </summary>
    private IEnumerator BattleMainLoop()
    {
        Log("戦闘メインループ開始");

        // 修正: 戦闘開始前の準備完了確認
        yield return StartCoroutine(WaitForBattleReady());

        // 修正: 戦闘自動開始
        yield return StartCoroutine(AutoStartBattle());

        // ターン処理開始、BattleTurnManager が自動でターン処理を行うため、
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
    /// 修正: 戦闘準備完了まで待機
    /// </summary>
    private IEnumerator WaitForBattleReady()
    {
        Log("戦闘準備完了を待機中...");

        float elapsed = 0f;

        while (elapsed < uiReadyTimeout)
        {
            // 修正: より確実な条件チェック
            bool hasValidData = allCharacters != null && allCharacters.Count > 0;
            bool hasPlayer = false;
            bool hasEnemies = false;
            bool uiReady = false;

            if (hasValidData)
            {
                // 修正: 直接リストから確認
                hasPlayer = allCharacters.Any(c => c.isPlayer && c.isAlive);
                hasEnemies = allCharacters.Any(c => !c.isPlayer && c.isAlive);
            }

            // 修正: UI関連のチェックを一時的にコメントアウト
            // if (battleUI != null && battleUI.IsInitialized())
            // {
            //     // UI層のデータ設定完了を確認
            //     uiReady = battleUI.IsUISetupComplete();
            // }
            uiReady = true; // 

            Log($"準備状況チェック: データ有効={hasValidData}, プレイヤー={hasPlayer}, 敵={hasEnemies}, UI準備={uiReady}");

            if (hasValidData && hasPlayer && hasEnemies && uiReady)
            {
                Log($"戦闘準備完了: プレイヤー{GetAlivePlayers().Count}体, 敵{GetAliveEnemies().Count}体, UI準備完了");
                yield break;
            }

            elapsed += uiReadyCheckInterval;
            yield return new WaitForSeconds(uiReadyCheckInterval);
        }

        LogError($"戦闘準備がタイムアウトしました。プレイヤー:{GetAlivePlayers().Count}体, 敵:{GetAliveEnemies().Count}体");
    }

    /// <summary>
    /// 修正: 戦闘自動開始処理
    /// </summary>
    private IEnumerator AutoStartBattle()
    {
        Log("戦闘自動開始処理");

        // UI に戦闘開始を通知
        var playerChar = GetPlayerCharacter();
        var enemyChars = GetEnemyCharacters();

        if (playerChar != null && enemyChars.Count > 0)
        {
            Log($"戦闘開始: {playerChar.characterName} vs {string.Join(", ", enemyChars.ConvertAll(e => e.characterName))}");

            // 修正: BattleTurnManager にターン処理開始を具体的に指示
            if (battleTurnManager != null)
            {
                Log("BattleTurnManager にターン処理開始を指示");

                // 修正: 初期化を確実に実行してからターン処理開始
                battleTurnManager.InitializeBattle(allCharacters, currentBattleSetup.turnLimit);

                yield return new WaitForSeconds(battleStartDelay); // 開始前の待機時間

                battleTurnManager.StartTurnProcessing();

                Log("BattleTurnManager ターン処理開始完了");
            }
            else
            {
                LogError("BattleTurnManager が null です");
            }

            yield return new WaitForSeconds(1f); // 戦闘開始演出時間
            Log("戦闘自動開始完了");
        }
        else
        {
            LogError($"戦闘開始失敗: プレイヤー:{(playerChar != null ? "存在" : "なし")}, 敵:{enemyChars.Count}体");
        }
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

            // 勝利後の報酬処理
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
    /// 勝利後報酬処理
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

    #region エディタ用ツール

#if UNITY_EDITOR
    [ContextMenu("戦闘状態をログ出力")]
    private void LogBattleState()
    {
        Log($"現在の戦闘状態: {CurrentState}");
        Log($"キャラクター数: {allCharacters?.Count ?? 0}");
        Log($"現在ターン: {currentTurnNumber}");
    }

    [ContextMenu("戦闘を強制終了")]
    private void EditorForceEndBattle()
    {
        ForceEndBattle();
    }

    [ContextMenu("装備ステータス計算テスト")]
    private void TestEquipmentCalculation()
    {
        var userData = SaveDataManager.Instance?.CurrentSaveData;
        if (userData != null)
        {
            var equipmentStats = CalculatePlayerEquipmentStats(userData);
            Log($"テスト結果 - HP:{equipmentStats.hp}, ATK:{equipmentStats.offense}, DEF:{equipmentStats.defense}");
        }
        else
        {
            LogError("UserSaveDataが取得できません");
        }
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