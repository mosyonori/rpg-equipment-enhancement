using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 戦闘画面全体の統合制御・Manager層との連携窓口
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
    [SerializeField] private GameObject monsterBattleUIPrefab;
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
    /// 修正: UI基本初期化（CanvasGroup表示問題修正）
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

            // 修正: 初期状態を表示状態に変更
            if (battleCanvasGroup != null)
            {
                battleCanvasGroup.alpha = 1f;           // 修正: 1fに変更（非表示問題解決）
                battleCanvasGroup.interactable = true;   // 修正: trueに変更
                battleCanvasGroup.blocksRaycasts = true; // 修正: trueに変更
                Log("CanvasGroup初期設定完了: Alpha=1.0, Interactable=true");
            }

            // UIコンポーネントの有効化確認
            ValidateAndActivateUIComponents();

            Log("UI基本初期化完了");
        }
        catch (Exception e)
        {
            LogError($"UI基本初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: UIコンポーネントの有効化確認（強化版）
    /// </summary>
    private void ValidateAndActivateUIComponents()
    {
        Log("UIコンポーネント有効化確認開始");

        try
        {
            // 自分自身のCanvas・CanvasGroupを確認
            EnsureBattleUIActive();

            // PlayerBattleUIの確認・有効化
            EnsurePlayerBattleUIActive();

            // BattleInfoUIの確認・有効化
            EnsureBattleInfoUIActive();

            // MonsterUIParentの確認・有効化
            EnsureMonsterUIParentActive();

            Log("UIコンポーネント有効化確認完了");
        }
        catch (Exception e)
        {
            LogError($"UIコンポーネント有効化確認エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: BattleUI自体の有効化確認
    /// </summary>
    private void EnsureBattleUIActive()
    {
        try
        {
            // BattleUI自体の有効化
            if (!gameObject.activeInHierarchy)
            {
                Log("BattleUI自体が非アクティブのため、有効化します");
                gameObject.SetActive(true);
            }

            // Canvas有効化
            if (battleCanvas != null && !battleCanvas.gameObject.activeInHierarchy)
            {
                Log("BattleCanvas が非アクティブのため、有効化します");
                battleCanvas.gameObject.SetActive(true);
            }

            // CanvasGroup設定
            if (battleCanvasGroup != null)
            {
                if (!battleCanvasGroup.gameObject.activeInHierarchy)
                {
                    Log("BattleCanvasGroup が非アクティブのため、有効化します");
                    battleCanvasGroup.gameObject.SetActive(true);
                }
            }

            Log("BattleUI基本コンポーネント有効化完了");
        }
        catch (Exception e)
        {
            LogError($"BattleUI基本コンポーネント有効化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: PlayerBattleUIとその子コンポーネントの有効化確認
    /// </summary>
    private void EnsurePlayerBattleUIActive()
    {
        try
        {
            if (playerBattleUI == null)
            {
                LogError("playerBattleUIがnullです");
                return;
            }

            // PlayerBattleUI自体の有効化
            if (!playerBattleUI.gameObject.activeInHierarchy)
            {
                Log("PlayerBattleUIが非アクティブのため、有効化します");
                playerBattleUI.gameObject.SetActive(true);
            }

            // PlayerBattleUIコンポーネント有効化
            if (!playerBattleUI.enabled)
            {
                Log("PlayerBattleUIコンポーネントが無効のため、有効化します");
                playerBattleUI.enabled = true;
            }

            // HPBarUIコンポーネントの確認
            EnsurePlayerHPBarUIActive();

            // StatusEffectUIコンポーネントの確認
            EnsurePlayerStatusEffectUIActive();

            Log("PlayerBattleUI有効化完了");
        }
        catch (Exception e)
        {
            LogError($"PlayerBattleUI有効化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: Player用HPBarUIの徹底的な有効化確認
    /// </summary>
    private void EnsurePlayerHPBarUIActive()
    {
        try
        {
            if (playerBattleUI == null) return;

            // GetComponentsInChildrenで全てのHPBarUIを取得（非アクティブも含む）
            HPBarUI[] hpBarUIs = playerBattleUI.GetComponentsInChildren<HPBarUI>(true);

            if (hpBarUIs.Length == 0)
            {
                LogWarning("PlayerBattleUI配下にHPBarUIが見つかりません");
                return;
            }

            foreach (var hpBarUI in hpBarUIs)
            {
                if (hpBarUI == null) continue;

                // HPBarUI GameObjectの有効化
                if (!hpBarUI.gameObject.activeInHierarchy)
                {
                    Log($"Player HPBarUI({hpBarUI.name})が非アクティブのため、有効化します");
                    hpBarUI.gameObject.SetActive(true);
                }

                // HPBarUIコンポーネントの有効化
                if (!hpBarUI.enabled)
                {
                    Log($"Player HPBarUIコンポーネント({hpBarUI.name})が無効のため、有効化します");
                    hpBarUI.enabled = true;
                }

                // HPBarUIの親階層を再帰的に有効化
                EnsureParentHierarchyActive(hpBarUI.transform, playerBattleUI.transform);
            }

            Log("Player HPBarUI有効化完了");
        }
        catch (Exception e)
        {
            LogError($"Player HPBarUI有効化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: Player用StatusEffectUIの有効化確認
    /// </summary>
    private void EnsurePlayerStatusEffectUIActive()
    {
        try
        {
            if (playerBattleUI == null) return;

            StatusEffectUI[] statusEffectUIs = playerBattleUI.GetComponentsInChildren<StatusEffectUI>(true);

            foreach (var statusEffectUI in statusEffectUIs)
            {
                if (statusEffectUI == null) continue;

                if (!statusEffectUI.gameObject.activeInHierarchy)
                {
                    Log($"Player StatusEffectUI({statusEffectUI.name})が非アクティブのため、有効化します");
                    statusEffectUI.gameObject.SetActive(true);
                }

                if (!statusEffectUI.enabled)
                {
                    Log($"Player StatusEffectUIコンポーネント({statusEffectUI.name})が無効のため、有効化します");
                    statusEffectUI.enabled = true;
                }
            }

            Log("Player StatusEffectUI有効化完了");
        }
        catch (Exception e)
        {
            LogError($"Player StatusEffectUI有効化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: BattleInfoUIの有効化確認
    /// </summary>
    private void EnsureBattleInfoUIActive()
    {
        try
        {
            if (battleInfoUI != null && !battleInfoUI.gameObject.activeInHierarchy)
            {
                Log("BattleInfoUIが非アクティブのため、有効化します");
                battleInfoUI.gameObject.SetActive(true);
            }

            if (battleInfoUI != null && !battleInfoUI.enabled)
            {
                Log("BattleInfoUIコンポーネントが無効のため、有効化します");
                battleInfoUI.enabled = true;
            }

            Log("BattleInfoUI有効化完了");
        }
        catch (Exception e)
        {
            LogError($"BattleInfoUI有効化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: MonsterUIParentの有効化確認
    /// </summary>
    private void EnsureMonsterUIParentActive()
    {
        try
        {
            if (monsterUIParent != null && !monsterUIParent.gameObject.activeInHierarchy)
            {
                Log("MonsterUIParentが非アクティブのため、有効化します");
                monsterUIParent.gameObject.SetActive(true);
            }

            Log("MonsterUIParent有効化完了");
        }
        catch (Exception e)
        {
            LogError($"MonsterUIParent有効化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: 親階層を再帰的に有効化する汎用メソッド
    /// </summary>
    /// <param name="child">子オブジェクトのTransform</param>
    /// <param name="stopAt">停止するTransform（通常は最上位UI）</param>
    private void EnsureParentHierarchyActive(Transform child, Transform stopAt)
    {
        try
        {
            Transform current = child.parent;

            while (current != null && current != stopAt)
            {
                if (!current.gameObject.activeInHierarchy)
                {
                    Log($"親オブジェクト({current.name})を有効化します");
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }
        }
        catch (Exception e)
        {
            LogError($"親階層有効化エラー: {e.Message}");
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

        // 修正: UI表示処理を削除（既に表示状態で初期化済み）
        // yield return StartCoroutine(ShowBattleUI());

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
            // PlayerBattleUI初期化確認
            if (playerBattleUI != null)
            {
                Log("PlayerBattleUI初期化確認完了");
            }

            // BattleInfoUI初期化確認
            if (battleInfoUI != null)
            {
                Log("BattleInfoUI初期化確認完了");
            }

            // BattleSpeedUI初期化確認
            if (battleSpeedUI != null)
            {
                Log("BattleSpeedUI初期化確認完了");
            }

            // DamageTextUI初期化確認
            if (damageTextUI != null)
            {
                Log("DamageTextUI初期化確認完了");
            }

            // BattleResultUI初期化確認
            if (battleResultUI != null)
            {
                Log("BattleResultUI初期化確認完了");
            }

            // RewardUI初期化確認
            if (rewardUI != null)
            {
                Log("RewardUI初期化確認完了");
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
    /// 修正: 戦闘初期化完了イベントハンドラ
    /// </summary>
    private void OnBattleInitialized(BattleSetupData setupData)
    {
        Log($"戦闘初期化完了: {setupData?.questName ?? "クエスト名不明"}");

        try
        {
            // 修正: setupDataの詳細確認
            if (setupData == null)
            {
                LogError("BattleSetupDataがnullです");
                return;
            }

            Log($"受信したBattleSetupData詳細:");
            Log($"  questName: '{setupData.questName}'");
            Log($"  questId: {setupData.questId}");
            Log($"  turnLimit: {setupData.turnLimit}");
            Log($"  spawnMonsterIds数: {setupData.spawnMonsterIds?.Count ?? 0}");

            currentBattleSetup = setupData;

            // 修正: questNameが空の場合の対処
            if (string.IsNullOrEmpty(setupData.questName))
            {
                LogWarning("questNameが空のため、デフォルト名を設定します");
                setupData.questName = "戦闘中";
            }

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

            // キャラクターUI更新（HP・状態異常含む）
            UpdateCharacterDisplay(character);

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

            // 全キャラクターのHP・状態異常更新
            UpdateAllCharacterDisplays();

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
    /// 修正: 戦闘UI表示（削除 - 初期化時点で表示済み）
    /// </summary>
    private IEnumerator ShowBattleUI()
    {
        Log("戦闘UI表示開始");

        // 修正: 既に表示状態で初期化されているため、追加の表示処理は不要
        Log("戦闘UI表示完了（初期化時点で表示済み）");
        yield return null;
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

            // 全キャラクターHP・状態異常更新
            UpdateAllCharacterDisplays();
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
            // BattleInfoUIは自動でBattleManagerのイベントを受信するため、
            // ここでは特別な処理は不要
            Log("戦闘情報更新確認");
        }
        catch (Exception e)
        {
            LogError($"戦闘情報更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 全キャラクター表示更新
    /// </summary>
    private void UpdateAllCharacterDisplays()
    {
        try
        {
            // プレイヤー更新
            UpdatePlayerDisplay();

            // モンスター更新
            UpdateMonsterDisplays();

            Log("全キャラクター表示更新完了");
        }
        catch (Exception e)
        {
            LogError($"全キャラクター表示更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// プレイヤー表示更新
    /// </summary>
    private void UpdatePlayerDisplay()
    {
        try
        {
            if (playerBattleUI != null && battleManager != null)
            {
                var playerCharacter = battleManager.GetPlayerCharacter();
                if (playerCharacter != null)
                {
                    playerBattleUI.UpdateFromCharacterData(playerCharacter);
                }
            }
        }
        catch (Exception e)
        {
            LogError($"プレイヤー表示更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// モンスター表示更新
    /// </summary>
    private void UpdateMonsterDisplays()
    {
        try
        {
            if (battleManager == null) return;

            var enemyCharacters = battleManager.GetEnemyCharacters();
            if (enemyCharacters == null) return;

            // 各モンスターUIを対応するキャラクターデータで更新
            foreach (var enemy in enemyCharacters)
            {
                if (monsterUIMap.TryGetValue(enemy.characterId, out MonsterBattleUI monsterUI))
                {
                    monsterUI.UpdateFromCharacterData(enemy);
                }
            }

            Log($"モンスター表示更新: {enemyCharacters.Count}体");
        }
        catch (Exception e)
        {
            LogError($"モンスター表示更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 特定キャラクター表示更新
    /// </summary>
    /// <param name="characterData">更新対象キャラクター</param>
    private void UpdateCharacterDisplay(BattleCharacterData characterData)
    {
        try
        {
            if (characterData == null) return;

            if (characterData.isPlayer && playerBattleUI != null)
            {
                playerBattleUI.UpdateFromCharacterData(characterData);
            }
            else if (!characterData.isPlayer && monsterUIMap.TryGetValue(characterData.characterId, out MonsterBattleUI monsterUI))
            {
                monsterUI.UpdateFromCharacterData(characterData);
            }

            Log($"キャラクター表示更新: {characterData.characterName}");
        }
        catch (Exception e)
        {
            LogError($"キャラクター表示更新エラー: {e.Message}");
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
                playerBattleUI.SetCharacterData(playerCharacter);
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
    /// 修正: モンスターUI作成（HPBarUI有効化強化版）
    /// </summary>
    private void CreateMonsterUIs(List<BattleCharacterData> enemies)
    {
        if (enemies == null || monsterUIParent == null) return;

        try
        {
            // 既存モンスターUIクリア
            ClearMonsterUIs();

            Log($"モンスターUI作成開始: {enemies.Count}体");

            // プレハブが設定されているかチェック
            if (monsterBattleUIPrefab == null)
            {
                LogError("monsterBattleUIPrefabが設定されていません。Inspectorで設定してください。");
                return;
            }

            // 新規モンスターUI作成
            foreach (var enemy in enemies)
            {
                try
                {
                    // モンスターUIプレハブからインスタンス作成
                    GameObject monsterUIObject = Instantiate(monsterBattleUIPrefab, monsterUIParent);
                    MonsterBattleUI monsterUI = monsterUIObject.GetComponent<MonsterBattleUI>();

                    if (monsterUI == null)
                    {
                        LogError("MonsterBattleUIコンポーネントがプレハブに含まれていません");
                        Destroy(monsterUIObject);
                        continue;
                    }

                    // モンスターUIにデータ設定
                    monsterUI.SetCharacterData(enemy);

                    // 修正: MonsterUI配下のHPBarUIを徹底的に有効化
                    EnsureMonsterHPBarUIActive(monsterUI, enemy);

                    // 修正: MonsterUI配下のStatusEffectUIを有効化
                    EnsureMonsterStatusEffectUIActive(monsterUI, enemy);

                    // リストとマップに追加
                    monsterBattleUIs.Add(monsterUI);
                    monsterUIMap[enemy.characterId] = monsterUI;

                    Log($"モンスターUI作成完了: {enemy.characterName} (ID: {enemy.characterId})");
                }
                catch (Exception e)
                {
                    LogError($"個別モンスターUI作成エラー ({enemy.characterName}): {e.Message}");
                }
            }

            Log($"モンスターUI作成完了: {monsterBattleUIs.Count}体");
        }
        catch (Exception e)
        {
            LogError($"モンスターUI作成エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: Monster用HPBarUIの徹底的な有効化
    /// </summary>
    private void EnsureMonsterHPBarUIActive(MonsterBattleUI monsterUI, BattleCharacterData enemy)
    {
        try
        {
            if (monsterUI == null) return;

            // GetComponentsInChildrenで全てのHPBarUIを取得
            HPBarUI[] hpBarUIs = monsterUI.GetComponentsInChildren<HPBarUI>(true);

            foreach (var hpBarUI in hpBarUIs)
            {
                if (hpBarUI == null) continue;

                // HPBarUI GameObjectの有効化
                if (!hpBarUI.gameObject.activeInHierarchy)
                {
                    Log($"Monster HPBarUI({enemy.characterName})が非アクティブのため、有効化します");
                    hpBarUI.gameObject.SetActive(true);
                }

                // HPBarUIコンポーネントの有効化
                if (!hpBarUI.enabled)
                {
                    Log($"Monster HPBarUIコンポーネント({enemy.characterName})が無効のため、有効化します");
                    hpBarUI.enabled = true;
                }

                // 親階層の有効化
                EnsureParentHierarchyActive(hpBarUI.transform, monsterUI.transform);
            }

            Log($"Monster HPBarUI有効化完了: {enemy.characterName}");
        }
        catch (Exception e)
        {
            LogError($"Monster HPBarUI有効化エラー ({enemy.characterName}): {e.Message}");
        }
    }

    /// <summary>
    /// 修正: Monster用StatusEffectUIの有効化
    /// </summary>
    private void EnsureMonsterStatusEffectUIActive(MonsterBattleUI monsterUI, BattleCharacterData enemy)
    {
        try
        {
            if (monsterUI == null) return;

            StatusEffectUI[] statusEffectUIs = monsterUI.GetComponentsInChildren<StatusEffectUI>(true);

            foreach (var statusEffectUI in statusEffectUIs)
            {
                if (statusEffectUI == null) continue;

                if (!statusEffectUI.gameObject.activeInHierarchy)
                {
                    Log($"Monster StatusEffectUI({enemy.characterName})が非アクティブのため、有効化します");
                    statusEffectUI.gameObject.SetActive(true);
                }

                if (!statusEffectUI.enabled)
                {
                    Log($"Monster StatusEffectUIコンポーネント({enemy.characterName})が無効のため、有効化します");
                    statusEffectUI.enabled = true;
                }
            }

            Log($"Monster StatusEffectUI有効化完了: {enemy.characterName}");
        }
        catch (Exception e)
        {
            LogError($"Monster StatusEffectUI有効化エラー ({enemy.characterName}): {e.Message}");
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
                // DamageTextUIはBattleManagerのイベントを自動受信するため、
                // ここでは特別な処理は不要
                Log($"ダメージ表示確認: {damage.targetName}に{damage.finalDamage}ダメージ");
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
                // BattleResultUIはBattleManagerのイベントを自動受信するため、
                // ここでは特別な処理は不要
                Log("戦闘結果UI表示確認");
            }

            if (rewardUI != null && result.isVictory)
            {
                // RewardUIもBattleManagerのイベントを自動受信するため、
                // ここでは特別な処理は不要
                Log("報酬UI表示確認");
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

    #region 公開メソッド - 状態異常制御

    /// <summary>
    /// 全キャラクターの状態異常を強制更新
    /// </summary>
    public void ForceUpdateAllStatusEffects()
    {
        try
        {
            Log("全キャラクター状態異常強制更新開始");

            // プレイヤー状態異常更新
            if (playerBattleUI != null && battleManager != null)
            {
                var playerCharacter = battleManager.GetPlayerCharacter();
                if (playerCharacter != null)
                {
                    playerBattleUI.UpdateFromCharacterData(playerCharacter);
                }
            }

            // モンスター状態異常更新
            if (battleManager != null)
            {
                var enemyCharacters = battleManager.GetEnemyCharacters();
                if (enemyCharacters != null)
                {
                    foreach (var enemy in enemyCharacters)
                    {
                        if (monsterUIMap.TryGetValue(enemy.characterId, out MonsterBattleUI monsterUI))
                        {
                            monsterUI.UpdateFromCharacterData(enemy);
                        }
                    }
                }
            }

            Log("全キャラクター状態異常強制更新完了");
        }
        catch (Exception e)
        {
            LogError($"状態異常強制更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 特定キャラクターの状態異常を更新
    /// </summary>
    /// <param name="characterId">更新対象のキャラクターID</param>
    public void UpdateCharacterStatusEffects(string characterId)
    {
        try
        {
            if (battleManager == null) return;

            // プレイヤーかチェック
            var playerCharacter = battleManager.GetPlayerCharacter();
            if (playerCharacter != null && playerCharacter.characterId == characterId)
            {
                if (playerBattleUI != null)
                {
                    playerBattleUI.UpdateFromCharacterData(playerCharacter);
                    Log($"プレイヤー状態異常更新: {characterId}");
                }
                return;
            }

            // モンスターかチェック
            if (monsterUIMap.TryGetValue(characterId, out MonsterBattleUI monsterUI))
            {
                var enemyCharacters = battleManager.GetEnemyCharacters();
                if (enemyCharacters != null)
                {
                    var enemy = enemyCharacters.FirstOrDefault(e => e.characterId == characterId);
                    if (enemy != null)
                    {
                        monsterUI.UpdateFromCharacterData(enemy);
                        Log($"モンスター状態異常更新: {characterId}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            LogError($"特定キャラクター状態異常更新エラー: {e.Message}");
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

    #endregion

    #region デバッグ用公開メソッド

    /// <summary>
    /// デバッグ用：現在の状態情報を出力
    /// </summary>
    [ContextMenu("デバッグ：状態情報出力")]
    public void DebugDumpState()
    {
        Log("=== BattleUI状態情報 ===");
        Log($"初期化完了: {isInitialized}");
        Log($"UIセットアップ完了: {isUISetupComplete}");
        Log($"現在の戦闘速度: {currentBattleSpeed}x");
        Log($"一時停止状態: {isPaused}");
        Log($"イベント購読状態: {isEventSubscribed}");
        Log($"トランジション進行中: {isTransitionInProgress}");

        Log($"プレイヤーUI: {(playerBattleUI != null ? "設定済み" : "未設定")}");
        Log($"モンスターUI数: {monsterBattleUIs?.Count ?? 0}体");
        Log($"モンスターUIマップ: {monsterUIMap?.Count ?? 0}エントリ");

        if (currentBattleSetup != null)
        {
            Log($"現在のクエスト: {currentBattleSetup.questName}");
        }
        else
        {
            Log("現在のクエスト: なし");
        }

        Log($"アクティブアニメーション: {activeAnimations?.Count ?? 0}個");
        Log($"実行中コルーチン: {runningCoroutines?.Count ?? 0}個");
        Log($"フレーム更新回数: {frameUpdateCount}");
        Log($"最後のエラー: {(string.IsNullOrEmpty(lastErrorMessage) ? "なし" : lastErrorMessage)}");

        Log("=======================");
    }

    /// <summary>
    /// デバッグ用：UIコンポーネント接続確認
    /// </summary>
    [ContextMenu("デバッグ：UIコンポーネント接続確認")]
    public void DebugCheckUIComponents()
    {
        Log("=== UIコンポーネント接続確認 ===");
        Log($"battleCanvas: {(battleCanvas != null ? "接続済み" : "未接続")}");
        Log($"battleCanvasGroup: {(battleCanvasGroup != null ? "接続済み" : "未接続")}");
        Log($"playerBattleUI: {(playerBattleUI != null ? "接続済み" : "未接続")}");
        Log($"monsterUIParent: {(monsterUIParent != null ? "接続済み" : "未接続")}");
        Log($"monsterBattleUIPrefab: {(monsterBattleUIPrefab != null ? "接続済み" : "未接続")}");
        Log($"battleInfoUI: {(battleInfoUI != null ? "接続済み" : "未接続")}");
        Log($"battleSpeedUI: {(battleSpeedUI != null ? "接続済み" : "未接続")}");
        Log($"damageTextUI: {(damageTextUI != null ? "接続済み" : "未接続")}");
        Log($"battleResultUI: {(battleResultUI != null ? "接続済み" : "未接続")}");
        Log($"rewardUI: {(rewardUI != null ? "接続済み" : "未接続")}");

        // BattleManager接続確認
        Log($"BattleManager: {(battleManager != null ? "接続済み" : "未接続")}");
        if (battleManager != null)
        {
            Log($"BattleManager初期化: {battleManager.IsInitialized}");
        }

        Log("===============================");
    }

    /// <summary>
    /// デバッグ用：CanvasGroup状態確認
    /// </summary>
    [ContextMenu("デバッグ：CanvasGroup状態確認")]
    public void DebugCheckCanvasGroupState()
    {
        if (battleCanvasGroup != null)
        {
            Log($"CanvasGroup Alpha: {battleCanvasGroup.alpha}");
            Log($"CanvasGroup Interactable: {battleCanvasGroup.interactable}");
            Log($"CanvasGroup BlocksRaycasts: {battleCanvasGroup.blocksRaycasts}");
            Log($"CanvasGroup GameObject Active: {battleCanvasGroup.gameObject.activeInHierarchy}");
        }
        else
        {
            Log("CanvasGroupが設定されていません");
        }
    }

    /// <summary>
    /// デバッグ用：UI強制表示
    /// </summary>
    [ContextMenu("デバッグ：UI強制表示")]
    public void DebugForceShowUI()
    {
        if (battleCanvasGroup != null)
        {
            battleCanvasGroup.alpha = 1f;
            battleCanvasGroup.interactable = true;
            battleCanvasGroup.blocksRaycasts = true;
            Log("UI強制表示完了");
        }
        else
        {
            Log("CanvasGroupが設定されていないため、強制表示できません");
        }
    }

    /// <summary>
    /// デバッグ用：全状態異常更新テスト
    /// </summary>
    [ContextMenu("デバッグ：状態異常更新テスト")]
    public void DebugTestStatusEffectUpdate()
    {
        Log("デバッグ：状態異常更新テスト実行");
        ForceUpdateAllStatusEffects();
    }

    #endregion
}