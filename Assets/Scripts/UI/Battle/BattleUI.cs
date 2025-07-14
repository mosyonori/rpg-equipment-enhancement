using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 戦闘画面全体の統括制御・Manager層との連携窓口
/// 責任範囲：
/// - 戦闘画面全体のUI制御
/// - Manager層からのイベント受信
/// - 各UIコンポーネント間の連携
/// - 戦闘設定UI制御
/// データアクセス統一ルール: UI層 → BattleManager → 各Manager → Data層
/// </summary>
public class BattleUI : MonoBehaviour
{
    #region フィールド

    [Header("UI設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private float uiInitializationTimeout = 10.0f;
    [SerializeField] private float uiUpdateInterval = 0.1f;

    [Header("UIコンポーネント参照")]
    [SerializeField] private Canvas battleCanvas;
    [SerializeField] private CanvasGroup battleCanvasGroup;

    [Header("キャラクター表示UI")]
    [SerializeField] private PlayerBattleUI playerBattleUI;
    [SerializeField] private Transform monsterUIParent;
    private List<MonsterBattleUI> monsterBattleUIs;

    [Header("戦闘情報・制御UI")]
    [SerializeField] private BattleInfoUI battleInfoUI;
    [SerializeField] private BattleSpeedUI battleSpeedUI;
    [SerializeField] private DamageTextUI damageTextUI;

    [Header("結果表示UI")]
    [SerializeField] private BattleResultUI battleResultUI;
    [SerializeField] private RewardUI rewardUI;

    [Header("演出設定")]
    [SerializeField] private float uiTransitionDuration = 0.3f;
    [SerializeField] private float damageDisplayDuration = 1.5f;

    // Manager層参照
    private BattleManager battleManager;

    // UI状態管理
    private bool isInitialized = false;
    private bool isUISetupComplete = false;
    private BattleSetupData currentBattleSetup;
    private List<BattleCharacterData> currentCharacters;
    private Dictionary<string, MonsterBattleUI> monsterUIMap;

    // 戦闘制御状態
    private bool isPaused = false;
    private float currentBattleSpeed = 1.0f;
    private bool isEventSubscribed = false;

    // アニメーション・演出制御
    private bool isTransitionInProgress = false;
    private List<Coroutine> activeAnimations;

    // エラー・デバッグ管理
    private string lastErrorMessage = "";
    private DateTime lastErrorTime;
    private int frameUpdateCount = 0;
    private float lastUpdateTime = 0f;

    // コルーチン管理
    private Coroutine initializationCoroutine;
    private Coroutine battleFlowCoroutine;
    private List<Coroutine> runningCoroutines;

    #endregion

    #region プロパティ

    /// <summary>
    /// UI初期化完了状態
    /// </summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// UIセットアップ完了状態
    /// </summary>
    public bool IsUISetupComplete => isUISetupComplete;

    /// <summary>
    /// 現在の戦闘速度
    /// </summary>
    public float CurrentBattleSpeed => currentBattleSpeed;

    /// <summary>
    /// 一時停止状態
    /// </summary>
    public bool IsPaused => isPaused;

    #endregion

    #region デバッグ・ログ

    /// <summary>
    /// デバッグログ出力
    /// </summary>
    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[BattleUI] {message}");
        }
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    private void LogError(string message)
    {
        Debug.LogError($"[BattleUI] {message}");
    }

    /// <summary>
    /// 警告ログ出力
    /// </summary>
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[BattleUI] {message}");
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        Log("BattleUI Awake開始");
        InitializeUI();
    }

    private void Start()
    {
        Log("BattleUI Start開始");
        initializationCoroutine = StartCoroutine(InitializeBattleUICoroutine());
    }

    private void Update()
    {
        if (!isInitialized) return;

        // 定期的な更新処理
        frameUpdateCount++;
        if (Time.time - lastUpdateTime >= uiUpdateInterval)
        {
            UpdateUI();
            lastUpdateTime = Time.time;
        }
    }

    private void OnDestroy()
    {
        Log("BattleUI OnDestroy開始");
        CleanupUI();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// UI基本初期化
    /// </summary>
    private void InitializeUI()
    {
        Log("UI基本初期化開始");

        try
        {
            // リスト初期化
            monsterBattleUIs = new List<MonsterBattleUI>();
            monsterUIMap = new Dictionary<string, MonsterBattleUI>();
            activeAnimations = new List<Coroutine>();
            runningCoroutines = new List<Coroutine>();
            currentCharacters = new List<BattleCharacterData>();

            // Canvas設定
            if (battleCanvas == null)
                battleCanvas = GetComponent<Canvas>();

            if (battleCanvasGroup == null)
                battleCanvasGroup = GetComponent<CanvasGroup>();

            // 初期状態設定
            if (battleCanvasGroup != null)
            {
                battleCanvasGroup.alpha = 0f;
                battleCanvasGroup.interactable = false;
                battleCanvasGroup.blocksRaycasts = false;
            }

            Log("UI基本初期化完了");
        }
        catch (Exception e)
        {
            LogError($"UI基本初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// BattleUI完全初期化コルーチン
    /// </summary>
    private IEnumerator InitializeBattleUICoroutine()
    {
        Log("BattleUI完全初期化開始");

        float elapsed = 0f;
        bool managerReady = false;

        // BattleManager初期化待機
        while (elapsed < uiInitializationTimeout && !managerReady)
        {
            managerReady = CheckBattleManagerReady();

            if (managerReady)
            {
                Log("BattleManager初期化完了確認");
                break;
            }

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        if (!managerReady)
        {
            LogError($"BattleManager初期化タイムアウト ({uiInitializationTimeout}秒)");
            yield break;
        }

        // Manager参照取得
        battleManager = BattleManager.Instance;

        // イベント購読
        SubscribeToEvents();

        // 子UIコンポーネント初期化
        yield return StartCoroutine(InitializeChildUIComponents());

        // UI表示
        yield return StartCoroutine(ShowBattleUI());

        isInitialized = true;
        isUISetupComplete = true;

        Log("BattleUI完全初期化完了");
    }

    /// <summary>
    /// BattleManager準備状況確認
    /// </summary>
    private bool CheckBattleManagerReady()
    {
        return BattleManager.Instance != null && BattleManager.Instance.IsInitialized;
    }

    /// <summary>
    /// 子UIコンポーネント初期化
    /// </summary>
    private IEnumerator InitializeChildUIComponents()
    {
        Log("子UIコンポーネント初期化開始");

        try
        {
            // PlayerBattleUI初期化
            if (playerBattleUI != null)
            {
                // 将来の実装：playerBattleUI.Initialize();
                Log("PlayerBattleUI初期化完了");
            }

            // BattleInfoUI初期化
            if (battleInfoUI != null)
            {
                // 将来の実装：battleInfoUI.Initialize();
                Log("BattleInfoUI初期化完了");
            }

            // BattleSpeedUI初期化
            if (battleSpeedUI != null)
            {
                // 将来の実装：battleSpeedUI.Initialize();
                Log("BattleSpeedUI初期化完了");
            }

            // DamageTextUI初期化
            if (damageTextUI != null)
            {
                // 将来の実装：damageTextUI.Initialize();
                Log("DamageTextUI初期化完了");
            }

            // BattleResultUI初期化
            if (battleResultUI != null)
            {
                // 将来の実装：battleResultUI.Initialize();
                Log("BattleResultUI初期化完了");
            }

            // RewardUI初期化
            if (rewardUI != null)
            {
                // 将来の実装：rewardUI.Initialize();
                Log("RewardUI初期化完了");
            }

            Log("子UIコンポーネント初期化完了");
        }
        catch (Exception e)
        {
            LogError($"子UIコンポーネント初期化エラー: {e.Message}");
        }

        yield return null; // 1フレーム待機
    }

    #endregion

    #region イベント管理

    /// <summary>
    /// イベント購読
    /// </summary>
    private void SubscribeToEvents()
    {
        if (isEventSubscribed) return;

        Log("BattleManagerイベント購読開始");

        try
        {
            BattleManager.OnBattleStateChanged += OnBattleStateChanged;
            BattleManager.OnBattleInitialized += OnBattleInitialized;
            BattleManager.OnCharacterTurnStart += OnCharacterTurnStart;
            BattleManager.OnActionExecuted += OnActionExecuted;
            BattleManager.OnBattleCompleted += OnBattleCompleted;
            BattleManager.OnBattleError += OnBattleError;

            isEventSubscribed = true;
            Log("BattleManagerイベント購読完了");
        }
        catch (Exception e)
        {
            LogError($"イベント購読エラー: {e.Message}");
        }
    }

    /// <summary>
    /// イベント購読解除
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (!isEventSubscribed) return;

        Log("BattleManagerイベント購読解除開始");

        try
        {
            BattleManager.OnBattleStateChanged -= OnBattleStateChanged;
            BattleManager.OnBattleInitialized -= OnBattleInitialized;
            BattleManager.OnCharacterTurnStart -= OnCharacterTurnStart;
            BattleManager.OnActionExecuted -= OnActionExecuted;
            BattleManager.OnBattleCompleted -= OnBattleCompleted;
            BattleManager.OnBattleError -= OnBattleError;

            isEventSubscribed = false;
            Log("BattleManagerイベント購読解除完了");
        }
        catch (Exception e)
        {
            LogError($"イベント購読解除エラー: {e.Message}");
        }
    }

    #endregion

    #region BattleManagerイベントハンドラ

    /// <summary>
    /// 戦闘状態変更イベントハンドラ
    /// </summary>
    private void OnBattleStateChanged(BattleState newState)
    {
        Log($"戦闘状態変更: {newState}");

        try
        {
            switch (newState)
            {
                case BattleState.Idle:
                    HandleIdleState();
                    break;

                case BattleState.Initializing:
                    HandleInitializingState();
                    break;

                case BattleState.InProgress:
                    HandleInProgressState();
                    break;

                case BattleState.Completed:
                    HandleCompletedState();
                    break;

                default:
                    LogError($"未対応の戦闘状態: {newState}");
                    break;
            }
        }
        catch (Exception e)
        {
            LogError($"戦闘状態変更処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘初期化完了イベントハンドラ
    /// </summary>
    private void OnBattleInitialized(BattleSetupData setupData)
    {
        Log($"戦闘初期化完了: {setupData.questName}");

        try
        {
            currentBattleSetup = setupData;

            // BattleInfoUI更新
            UpdateBattleInfo();

            // キャラクターUI作成
            CreateCharacterUIs();

            Log("戦闘初期化UI反映完了");
        }
        catch (Exception e)
        {
            LogError($"戦闘初期化UI反映エラー: {e.Message}");
        }
    }

    /// <summary>
    /// キャラクターターン開始イベントハンドラ
    /// </summary>
    private void OnCharacterTurnStart(BattleCharacterData character)
    {
        Log($"ターン開始: {character.characterName}");

        try
        {
            // ターン情報更新
            UpdateBattleInfo();

            // キャラクターUIハイライト
            HighlightCurrentActor(character);

            Log("ターン開始UI反映完了");
        }
        catch (Exception e)
        {
            LogError($"ターン開始UI反映エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 行動実行イベントハンドラ
    /// </summary>
    private void OnActionExecuted(ActionData action)
    {
        Log($"行動実行: {action.GetActionSummary()}");

        try
        {
            // ダメージ表示
            ShowDamageDisplay(action);

            // HPバー更新
            UpdateCharacterHPBars();

            Log("行動実行UI反映完了");
        }
        catch (Exception e)
        {
            LogError($"行動実行UI反映エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘完了イベントハンドラ
    /// </summary>
    private void OnBattleCompleted(BattleResultData result)
    {
        Log($"戦闘完了: {(result.isVictory ? "勝利" : "敗北")}");

        try
        {
            // 結果画面表示
            ShowBattleResult(result);

            Log("戦闘完了UI反映完了");
        }
        catch (Exception e)
        {
            LogError($"戦闘完了UI反映エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘エラーイベントハンドラ
    /// </summary>
    private void OnBattleError(string errorMessage)
    {
        LogError($"戦闘エラー受信: {errorMessage}");

        lastErrorMessage = errorMessage;
        lastErrorTime = DateTime.Now;

        // エラー表示UI（将来実装）
        ShowErrorMessage(errorMessage);
    }

    #endregion

    #region UI表示制御

    /// <summary>
    /// 戦闘UI表示
    /// </summary>
    private IEnumerator ShowBattleUI()
    {
        Log("戦闘UI表示開始");

        if (battleCanvasGroup != null)
        {
            isTransitionInProgress = true;

            float elapsed = 0f;
            float startAlpha = battleCanvasGroup.alpha;

            while (elapsed < uiTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / uiTransitionDuration;
                battleCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, progress);
                yield return null;
            }

            battleCanvasGroup.alpha = 1f;
            battleCanvasGroup.interactable = true;
            battleCanvasGroup.blocksRaycasts = true;

            isTransitionInProgress = false;
        }

        Log("戦闘UI表示完了");
    }

    /// <summary>
    /// 戦闘UI非表示
    /// </summary>
    private IEnumerator HideBattleUI()
    {
        Log("戦闘UI非表示開始");

        if (battleCanvasGroup != null)
        {
            isTransitionInProgress = true;
            battleCanvasGroup.interactable = false;
            battleCanvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            float startAlpha = battleCanvasGroup.alpha;

            while (elapsed < uiTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / uiTransitionDuration;
                battleCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
                yield return null;
            }

            battleCanvasGroup.alpha = 0f;
            isTransitionInProgress = false;
        }

        Log("戦闘UI非表示完了");
    }

    #endregion

    #region 戦闘状態処理

    /// <summary>
    /// 待機状態処理
    /// </summary>
    private void HandleIdleState()
    {
        Log("待機状態UI処理");
        // 将来の実装
    }

    /// <summary>
    /// 初期化中状態処理
    /// </summary>
    private void HandleInitializingState()
    {
        Log("初期化中状態UI処理");
        // 将来の実装
    }

    /// <summary>
    /// 戦闘中状態処理
    /// </summary>
    private void HandleInProgressState()
    {
        Log("戦闘中状態UI処理");
        // 将来の実装
    }

    /// <summary>
    /// 完了状態処理
    /// </summary>
    private void HandleCompletedState()
    {
        Log("完了状態UI処理");
        // 将来の実装
    }

    #endregion

    #region UI更新処理

    /// <summary>
    /// UI定期更新
    /// </summary>
    private void UpdateUI()
    {
        if (!isInitialized || battleManager == null) return;

        try
        {
            // 戦闘情報更新
            UpdateBattleInfo();

            // キャラクターHP更新
            UpdateCharacterHPBars();
        }
        catch (Exception e)
        {
            LogError($"UI更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘情報更新
    /// </summary>
    private void UpdateBattleInfo()
    {
        if (battleInfoUI == null || battleManager == null) return;

        try
        {
            // 将来の実装：battleInfoUI.UpdateTurnInfo(battleManager.GetCurrentTurnNumber());
            // 将来の実装：battleInfoUI.UpdateQuestInfo(currentBattleSetup);
        }
        catch (Exception e)
        {
            LogError($"戦闘情報更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// キャラクターHPバー更新
    /// </summary>
    private void UpdateCharacterHPBars()
    {
        try
        {
            // プレイヤーHP更新
            if (playerBattleUI != null && battleManager != null)
            {
                var playerCharacter = battleManager.GetPlayerCharacter();
                if (playerCharacter != null)
                {
                    // 将来の実装：playerBattleUI.UpdateHP(playerCharacter);
                }
            }

            // モンスターHP更新
            foreach (var monsterUI in monsterBattleUIs)
            {
                if (monsterUI != null)
                {
                    // 将来の実装：monsterUI.UpdateHP();
                }
            }
        }
        catch (Exception e)
        {
            LogError($"キャラクターHPバー更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 現在行動者ハイライト
    /// </summary>
    private void HighlightCurrentActor(BattleCharacterData character)
    {
        try
        {
            if (character.isPlayer && playerBattleUI != null)
            {
                // 将来の実装：playerBattleUI.SetHighlight(true);
            }
            else if (!character.isPlayer && monsterUIMap.ContainsKey(character.characterId))
            {
                // 将来の実装：monsterUIMap[character.characterId].SetHighlight(true);
            }
        }
        catch (Exception e)
        {
            LogError($"現在行動者ハイライトエラー: {e.Message}");
        }
    }

    #endregion

    #region キャラクターUI管理

    /// <summary>
    /// キャラクターUI作成
    /// </summary>
    private void CreateCharacterUIs()
    {
        if (battleManager == null) return;

        try
        {
            Log("キャラクターUI作成開始");

            // プレイヤーUI設定
            var playerCharacter = battleManager.GetPlayerCharacter();
            if (playerCharacter != null && playerBattleUI != null)
            {
                // 将来の実装：playerBattleUI.SetCharacterData(playerCharacter);
                Log("プレイヤーUI設定完了");
            }

            // モンスターUI作成
            var enemyCharacters = battleManager.GetEnemyCharacters();
            CreateMonsterUIs(enemyCharacters);

            Log("キャラクターUI作成完了");
        }
        catch (Exception e)
        {
            LogError($"キャラクターUI作成エラー: {e.Message}");
        }
    }

    /// <summary>
    /// モンスターUI作成
    /// </summary>
    private void CreateMonsterUIs(List<BattleCharacterData> enemies)
    {
        if (enemies == null || monsterUIParent == null) return;

        try
        {
            // 既存モンスターUIクリア
            ClearMonsterUIs();

            // 新規モンスターUI作成
            foreach (var enemy in enemies)
            {
                // 将来の実装：MonsterBattleUIプレハブからインスタンス作成
                // GameObject monsterUIObject = Instantiate(monsterBattleUIPrefab, monsterUIParent);
                // MonsterBattleUI monsterUI = monsterUIObject.GetComponent<MonsterBattleUI>();
                // monsterUI.SetCharacterData(enemy);
                // monsterBattleUIs.Add(monsterUI);
                // monsterUIMap[enemy.characterId] = monsterUI;

                Log($"モンスターUI作成: {enemy.characterName}");
            }

            Log($"モンスターUI作成完了: {enemies.Count}体");
        }
        catch (Exception e)
        {
            LogError($"モンスターUI作成エラー: {e.Message}");
        }
    }

    /// <summary>
    /// モンスターUIクリア
    /// </summary>
    private void ClearMonsterUIs()
    {
        try
        {
            foreach (var monsterUI in monsterBattleUIs)
            {
                if (monsterUI != null)
                {
                    Destroy(monsterUI.gameObject);
                }
            }

            monsterBattleUIs.Clear();
            monsterUIMap.Clear();

            Log("モンスターUIクリア完了");
        }
        catch (Exception e)
        {
            LogError($"モンスターUIクリアエラー: {e.Message}");
        }
    }

    #endregion

    #region 演出・表示処理

    /// <summary>
    /// ダメージ表示
    /// </summary>
    private void ShowDamageDisplay(ActionData action)
    {
        if (damageTextUI == null || action.damageResults == null) return;

        try
        {
            foreach (var damage in action.damageResults)
            {
                // 将来の実装：damageTextUI.ShowDamage(damage);
                Log($"ダメージ表示: {damage.targetName}に{damage.finalDamage}ダメージ");
            }
        }
        catch (Exception e)
        {
            LogError($"ダメージ表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘結果表示
    /// </summary>
    private void ShowBattleResult(BattleResultData result)
    {
        try
        {
            if (battleResultUI != null)
            {
                // 将来の実装：battleResultUI.ShowResult(result);
                Log("戦闘結果UI表示");
            }

            if (rewardUI != null && result.isVictory)
            {
                // 将来の実装：rewardUI.ShowRewards(result);
                Log("報酬UI表示");
            }
        }
        catch (Exception e)
        {
            LogError($"戦闘結果表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// エラーメッセージ表示
    /// </summary>
    private void ShowErrorMessage(string message)
    {
        try
        {
            // 将来の実装：エラー表示UI
            Log($"エラーメッセージ表示: {message}");
        }
        catch (Exception e)
        {
            LogError($"エラーメッセージ表示エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - 戦闘制御

    /// <summary>
    /// 戦闘速度変更
    /// </summary>
    public void SetBattleSpeed(float speed)
    {
        if (battleManager == null) return;

        try
        {
            currentBattleSpeed = speed;
            battleManager.SetBattleSpeed(speed);
            Log($"戦闘速度変更: {speed}x");
        }
        catch (Exception e)
        {
            LogError($"戦闘速度変更エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘一時停止
    /// </summary>
    public void PauseBattle()
    {
        if (battleManager == null) return;

        try
        {
            isPaused = true;
            battleManager.SetBattlePause(true);
            Log("戦闘一時停止");
        }
        catch (Exception e)
        {
            LogError($"戦闘一時停止エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘再開
    /// </summary>
    public void ResumeBattle()
    {
        if (battleManager == null) return;

        try
        {
            isPaused = false;
            battleManager.SetBattlePause(false);
            Log("戦闘再開");
        }
        catch (Exception e)
        {
            LogError($"戦闘再開エラー: {e.Message}");
        }
    }

    #endregion

    #region クリーンアップ

    /// <summary>
    /// UIクリーンアップ
    /// </summary>
    private void CleanupUI()
    {
        Log("UIクリーンアップ開始");

        try
        {
            // イベント購読解除
            UnsubscribeFromEvents();

            // コルーチン停止
            StopAllRunningCoroutines();

            // モンスターUIクリア
            ClearMonsterUIs();

            // アニメーション停止
            StopAllAnimations();

            Log("UIクリーンアップ完了");
        }
        catch (Exception e)
        {
            LogError($"UIクリーンアップエラー: {e.Message}");
        }
    }

    /// <summary>
    /// 実行中コルーチン停止
    /// </summary>
    private void StopAllRunningCoroutines()
    {
        try
        {
            if (initializationCoroutine != null)
            {
                StopCoroutine(initializationCoroutine);
                initializationCoroutine = null;
            }

            if (battleFlowCoroutine != null)
            {
                StopCoroutine(battleFlowCoroutine);
                battleFlowCoroutine = null;
            }

            foreach (var coroutine in runningCoroutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            runningCoroutines.Clear();

            Log("実行中コルーチン停止完了");
        }
        catch (Exception e)
        {
            LogError($"コルーチン停止エラー: {e.Message}");
        }
    }

    /// <summary>
    /// アニメーション停止
    /// </summary>
    private void StopAllAnimations()
    {
        try
        {
            foreach (var animation in activeAnimations)
            {
                if (animation != null)
                {
                    StopCoroutine(animation);
                }
            }
            activeAnimations.Clear();

            Log("アニメーション停止完了");
        }
        catch (Exception e)
        {
            LogError($"アニメーション停止エラー: {e.Message}");
        }
    }
}
#endregion