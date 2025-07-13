using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 戦闘画面全体の制御・Manager層との連携
/// データアクセス統一ルール: UI層 → Manager層 → データ層
/// </summary>
public class BattleUI : MonoBehaviour
{
    [Header("戦闘画面全体")]
    [SerializeField] private GameObject battleRoot;
    [SerializeField] private CanvasGroup battleCanvasGroup;

    [Header("戦闘情報表示")]
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI currentTurnText;
    [SerializeField] private TextMeshProUGUI maxTurnText;

    [Header("戦闘制御ボタン")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button speedButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private TextMeshProUGUI speedButtonText;

    [Header("UIコンポーネント参照")]
    [SerializeField] private PlayerBattleUI playerBattleUI;
    [SerializeField] private MonsterAreaManager monsterAreaManager;
    [SerializeField] private BattleInfoUI battleInfoUI;
    [SerializeField] private SkillInfoUI skillInfoUI;
    [SerializeField] private BattleLogUI battleLogUI;
    [SerializeField] private BattleResultUI battleResultUI;

    [Header("スキルボタン")]
    [SerializeField] private Button normalAttackButton;
    [SerializeField] private Button skill1Button;
    [SerializeField] private Button skill2Button;
    [SerializeField] private TextMeshProUGUI normalAttackText;
    [SerializeField] private TextMeshProUGUI skill1Text;
    [SerializeField] private TextMeshProUGUI skill2Text;

    [Header("戦闘設定")]
    [SerializeField] private float[] battleSpeeds = { 1.0f, 2.0f, 4.0f };
    [SerializeField] private string[] speedTexts = { "1倍速", "2倍速", "4倍速" };

    [Header("修正: UI準備完了待機設定")]
    [SerializeField] private float uiSetupTimeout = 5.0f;
    [SerializeField] private float uiSetupCheckInterval = 0.1f;
    [SerializeField] private int maxDataDistributionRetries = 3;

    // 内部状態
    private BattleState currentBattleState;
    private int currentSpeedIndex = 0;
    private bool isPaused = false;
    private bool isInitialized = false;

    // 修正: キャラクターデータ保持と状態管理
    private BattleCharacterData currentPlayerData;
    private System.Collections.Generic.List<BattleCharacterData> currentEnemyData;
    private bool isDataDistributionComplete = false;
    private bool isUISetupComplete = false;

    // イベント
    public static event Action OnBattleUIReady;
    public static event Action OnSkipRequested;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeButtons();
    }

    private void Start()
    {
        RegisterBattleManagerEvents();
        InitializeUI();
    }

    private void OnDestroy()
    {
        UnregisterBattleManagerEvents();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// UI初期化
    /// </summary>
    private void InitializeUI()
    {
        if (!Application.isPlaying)
        {
            DebugLog("エディタモード中のため初期化をスキップ");
            return;
        }

        try
        {
            // 初期状態設定
            currentBattleState = BattleState.Idle;
            battleRoot.SetActive(false);

            // 各UIコンポーネント初期化
            InitializeUIComponents();

            // 戦闘制御UI初期化
            UpdateSpeedButtonText();
            UpdatePauseButtonState();

            // 修正: 状態フラグ初期化
            isDataDistributionComplete = false;
            isUISetupComplete = false;

            isInitialized = true;
            OnBattleUIReady?.Invoke();

            DebugLog("BattleUI初期化完了");
        }
        catch (Exception e)
        {
            DebugLogError($"BattleUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// UIコンポーネント初期化
    /// </summary>
    private void InitializeUIComponents()
    {
        // 各UIコンポーネントの初期化
        if (playerBattleUI != null)
            playerBattleUI.Initialize();

        if (monsterAreaManager != null)
            monsterAreaManager.Initialize();

        if (battleInfoUI != null)
            battleInfoUI.Initialize();

        if (skillInfoUI != null)
            skillInfoUI.Initialize();

        if (battleLogUI != null)
            battleLogUI.Initialize();

        if (battleResultUI != null)
            battleResultUI.Initialize();
    }

    /// <summary>
    /// ボタンイベント初期化
    /// </summary>
    private void InitializeButtons()
    {
        // 戦闘制御ボタン
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (skipButton != null)
            skipButton.onClick.AddListener(OnSkipClicked);

        if (speedButton != null)
            speedButton.onClick.AddListener(OnSpeedClicked);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);

        // スキルボタン（現在は表示のみ、オートバトルのため操作無効）
        if (normalAttackButton != null)
        {
            normalAttackButton.onClick.AddListener(() => OnSkillButtonClicked("通常攻撃"));
            normalAttackButton.interactable = false; // オートバトルのため無効
        }

        if (skill1Button != null)
        {
            skill1Button.onClick.AddListener(() => OnSkillButtonClicked("スキル1"));
            skill1Button.interactable = false; // オートバトルのため無効
        }

        if (skill2Button != null)
        {
            skill2Button.onClick.AddListener(() => OnSkillButtonClicked("スキル2"));
            skill2Button.interactable = false; // オートバトルのため無効
        }
    }

    #endregion

    #region Manager層イベント登録

    /// <summary>
    /// BattleManagerイベント登録
    /// </summary>
    private void RegisterBattleManagerEvents()
    {
        BattleManager.OnBattleStateChanged += OnBattleStateChanged;
        BattleManager.OnBattleInitialized += OnBattleInitialized;
        BattleManager.OnCharacterTurnStart += OnCharacterTurnStart;
        BattleManager.OnActionExecuted += OnActionExecuted;
        BattleManager.OnBattleCompleted += OnBattleCompleted;
        BattleManager.OnBattleError += OnBattleError;
    }

    /// <summary>
    /// BattleManagerイベント登録解除
    /// </summary>
    private void UnregisterBattleManagerEvents()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.OnBattleStateChanged -= OnBattleStateChanged;
            BattleManager.OnBattleInitialized -= OnBattleInitialized;
            BattleManager.OnCharacterTurnStart -= OnCharacterTurnStart;
            BattleManager.OnActionExecuted -= OnActionExecuted;
            BattleManager.OnBattleCompleted -= OnBattleCompleted;
            BattleManager.OnBattleError -= OnBattleError;
        }
    }

    #endregion

    #region Manager層イベントハンドラ

    /// <summary>
    /// 戦闘状態変更イベント
    /// </summary>
    private void OnBattleStateChanged(BattleState newState)
    {
        currentBattleState = newState;
        UpdateUIBasedOnBattleState(newState);
        DebugLog($"戦闘状態変更: {newState}");
    }

    /// <summary>
    /// 修正: 戦闘初期化完了イベント - 非同期データ配布処理
    /// </summary>
    private void OnBattleInitialized(BattleSetupData setupData)
    {
        try
        {
            DebugLog("戦闘初期化完了 - UI準備開始");

            // 戦闘画面表示
            ShowBattleUI();

            // 戦闘情報更新
            UpdateBattleInfo(setupData);

            // 修正: 非同期でキャラクターデータ配布を開始
            StartCoroutine(WaitForDataAndDistribute(setupData));
        }
        catch (Exception e)
        {
            DebugLogError($"戦闘初期化処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// キャラクターターン開始イベント
    /// </summary>
    private void OnCharacterTurnStart(BattleCharacterData character)
    {
        // ターン情報更新
        UpdateTurnInfo();

        // 各UIコンポーネントにターン開始を通知
        NotifyTurnStartToComponents(character);

        DebugLog($"ターン開始: {character.characterName}");
    }

    /// <summary>
    /// 行動実行イベント
    /// </summary>
    private void OnActionExecuted(ActionData action)
    {
        // 各UIコンポーネントに行動実行を通知
        NotifyActionExecutedToComponents(action);

        DebugLog($"行動実行: {action.GetActionSummary()}");
    }

    /// <summary>
    /// 戦闘完了イベント
    /// </summary>
    private void OnBattleCompleted(BattleResultData result)
    {
        // 戦闘結果UI表示
        if (battleResultUI != null)
            battleResultUI.ShowResult(result);

        DebugLog($"戦闘完了: {(result.isVictory ? "勝利" : "敗北")}");
    }

    /// <summary>
    /// 戦闘エラーイベント
    /// </summary>
    private void OnBattleError(string errorMessage)
    {
        DebugLogError($"戦闘エラー: {errorMessage}");
        // エラー後は強制的にホーム画面に戻る等の処理を実装
    }

    #endregion

    #region 修正: 非同期データ配布処理

    /// <summary>
    /// 修正: データ準備完了を待機してからキャラクターデータを配布
    /// </summary>
    private IEnumerator WaitForDataAndDistribute(BattleSetupData setupData)
    {
        DebugLog("データ準備完了待機開始");

        float elapsed = 0f;
        int retryCount = 0;

        while (elapsed < uiSetupTimeout && retryCount < maxDataDistributionRetries)
        {
            // BattleManagerからキャラクターデータを取得を試行
            if (BattleManager.Instance != null)
            {
                var allCharacters = BattleManager.Instance.GetAllCharacters();

                if (allCharacters != null && allCharacters.Count > 0)
                {
                    DebugLog($"キャラクターデータ取得成功: {allCharacters.Count}体");

                    // データ配布を実行
                    yield return StartCoroutine(DistributeCharacterDataAsync(allCharacters, setupData));

                    // 配布結果を確認
                    if (isDataDistributionComplete)
                    {
                        DebugLog("データ配布完了 - UI準備完了通知");
                        NotifyUISetupComplete();
                        yield break;
                    }
                    else
                    {
                        retryCount++;
                        DebugLogError($"データ配布失敗 - リトライ {retryCount}/{maxDataDistributionRetries}");
                    }
                }
                else
                {
                    DebugLog($"キャラクターデータ未準備 - 待機中... ({elapsed:F1}秒)");
                }
            }

            elapsed += uiSetupCheckInterval;
            yield return new WaitForSeconds(uiSetupCheckInterval);
        }

        if (elapsed >= uiSetupTimeout || retryCount >= maxDataDistributionRetries)
        {
            DebugLogError($"データ配布タイムアウトまたは最大リトライ到達: 経過時間={elapsed:F1}秒, リトライ={retryCount}");
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.ForceEndBattle();
            }
        }
    }

    /// <summary>
    /// 修正: 非同期キャラクターデータ配布
    /// </summary>
    private IEnumerator DistributeCharacterDataAsync(System.Collections.Generic.List<BattleCharacterData> allCharacters, BattleSetupData setupData)
    {
        DebugLog("非同期キャラクターデータ配布開始");

        if (allCharacters == null || allCharacters.Count == 0)
        {
            DebugLogError("配布対象のキャラクターデータが空です");
            isDataDistributionComplete = false;
            yield break;
        }

        // プレイヤーデータと敵データを分離
        currentPlayerData = allCharacters.Find(c => c.isPlayer);
        currentEnemyData = allCharacters.FindAll(c => !c.isPlayer);

        DebugLog($"データ分離完了: プレイヤー={currentPlayerData?.characterName ?? "null"}, 敵={currentEnemyData?.Count ?? 0}体");

        // 修正: PlayerBattleUIにデータ設定
        bool playerUISuccess = false;
        if (playerBattleUI != null && currentPlayerData != null)
        {
            playerBattleUI.OnBattleStart(setupData);
            yield return new WaitForSeconds(0.1f); // UI準備時間

            playerBattleUI.UpdatePlayerData(currentPlayerData);
            playerUISuccess = true;
            DebugLog($"PlayerBattleUIデータ設定完了: {currentPlayerData.characterName}");
        }
        else
        {
            DebugLogError($"PlayerBattleUIデータ設定失敗: playerBattleUI={playerBattleUI != null}, currentPlayerData={currentPlayerData != null}");
        }

        // 修正: MonsterAreaManagerにデータ設定（UI作成も含む）
        bool monsterUISuccess = false;
        if (monsterAreaManager != null && currentEnemyData != null && currentEnemyData.Count > 0)
        {
            monsterAreaManager.OnBattleStart(setupData);
            yield return new WaitForSeconds(0.1f); // UI準備時間

            monsterAreaManager.UpdateMonstersData(currentEnemyData);

            // 修正: UI作成完了を確認
            yield return StartCoroutine(WaitForMonsterUISetup());

            if (monsterAreaManager.IsUISetupComplete())
            {
                monsterUISuccess = true;
                DebugLog($"MonsterAreaManagerデータ設定完了: {currentEnemyData.Count}体");
            }
            else
            {
                DebugLogError("MonsterAreaManagerのUI設定が完了していません");
            }
        }
        else
        {
            DebugLogError($"MonsterAreaManagerデータ設定失敗: monsterAreaManager={monsterAreaManager != null}, currentEnemyData={currentEnemyData?.Count ?? 0}");
        }

        // 他のUIコンポーネントにも基本的な戦闘開始通知
        battleInfoUI?.OnBattleStart(setupData);
        skillInfoUI?.OnBattleStart(setupData);
        battleLogUI?.OnBattleStart(setupData);

        // 修正: 成功判定
        bool overallSuccess = playerUISuccess && monsterUISuccess;

        if (overallSuccess)
        {
            isDataDistributionComplete = true;
            DebugLog("全キャラクターデータ配布完了");
        }
        else
        {
            isDataDistributionComplete = false;
            DebugLogError($"データ配布部分失敗: プレイヤーUI={playerUISuccess}, モンスターUI={monsterUISuccess}");
        }
    }

    /// <summary>
    /// 修正: モンスターUI作成完了待機
    /// </summary>
    private IEnumerator WaitForMonsterUISetup()
    {
        float elapsed = 0f;
        float timeout = 2f; // モンスターUI作成のタイムアウト

        while (elapsed < timeout)
        {
            if (monsterAreaManager != null && monsterAreaManager.IsUISetupComplete())
            {
                DebugLog($"モンスターUI作成完了確認: {elapsed:F2}秒で完了");
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        DebugLogError($"モンスターUI作成タイムアウト: {timeout}秒");
    }

    /// <summary>
    /// 修正: UI準備完了通知
    /// </summary>
    private void NotifyUISetupComplete()
    {
        try
        {
            isUISetupComplete = true;
            DebugLog("=== UI準備完了通知 ===");

            // BattleManagerに準備完了を通知（将来的な拡張用）
            // 現在はログ出力のみ

            DebugLog($"最終状態確認:");
            DebugLog($"- プレイヤーデータ: {currentPlayerData?.characterName ?? "null"}");
            DebugLog($"- 敵データ数: {currentEnemyData?.Count ?? 0}");
            DebugLog($"- データ配布完了: {isDataDistributionComplete}");
            DebugLog($"- UI設定完了: {isUISetupComplete}");
        }
        catch (Exception e)
        {
            DebugLogError($"UI準備完了通知エラー: {e.Message}");
        }
    }

    #endregion

    #region UI状態更新

    /// <summary>
    /// 戦闘状態に応じたUI更新
    /// </summary>
    private void UpdateUIBasedOnBattleState(BattleState state)
    {
        switch (state)
        {
            case BattleState.Idle:
                battleRoot.SetActive(false);
                break;

            case BattleState.Initializing:
                battleRoot.SetActive(true);
                SetUIInteractable(false);
                break;

            case BattleState.InProgress:
                battleRoot.SetActive(true);
                SetUIInteractable(true);
                break;

            case BattleState.Completed:
                SetUIInteractable(false);
                break;
        }
    }

    /// <summary>
    /// 戦闘画面表示
    /// </summary>
    private void ShowBattleUI()
    {
        battleRoot.SetActive(true);
        if (battleCanvasGroup != null)
        {
            battleCanvasGroup.alpha = 1f;
            battleCanvasGroup.interactable = true;
        }
    }

    /// <summary>
    /// UI操作可能性設定
    /// </summary>
    private void SetUIInteractable(bool interactable)
    {
        if (battleCanvasGroup != null)
            battleCanvasGroup.interactable = interactable;
    }

    /// <summary>
    /// 戦闘情報更新
    /// </summary>
    private void UpdateBattleInfo(BattleSetupData setupData)
    {
        if (questNameText != null)
        {
            // QuestMasterDataから名前を取得
            var questData = QuestDataManager.Instance?.GetQuestData(setupData.questId);
            questNameText.text = questData?.questName ?? "不明なクエスト";
        }

        if (maxTurnText != null)
            maxTurnText.text = setupData.turnLimit > 0 ? setupData.turnLimit.ToString() : "無制限";
    }

    /// <summary>
    /// ターン情報更新
    /// </summary>
    private void UpdateTurnInfo()
    {
        if (currentTurnText != null && BattleManager.Instance != null)
            currentTurnText.text = BattleManager.Instance.GetCurrentTurnNumber().ToString();
    }

    #endregion

    #region UIコンポーネント通知

    /// <summary>
    /// 各UIコンポーネントにターン開始を通知
    /// </summary>
    private void NotifyTurnStartToComponents(BattleCharacterData character)
    {
        playerBattleUI?.OnTurnStart(character);
        monsterAreaManager?.OnTurnStart(character);
        battleInfoUI?.OnTurnStart(character);
        skillInfoUI?.OnTurnStart(character);
    }

    /// <summary>
    /// 各UIコンポーネントに行動実行を通知
    /// </summary>
    private void NotifyActionExecutedToComponents(ActionData action)
    {
        playerBattleUI?.OnActionExecuted(action);
        monsterAreaManager?.OnActionExecuted(action);
        battleLogUI?.OnActionExecuted(action);
    }

    #endregion

    #region ボタンイベントハンドラ

    /// <summary>
    /// 設定ボタンクリック
    /// </summary>
    private void OnSettingsClicked()
    {
        DebugLog("設定ボタンクリック");
        // 設定画面表示等の処理を実装
    }

    /// <summary>
    /// スキップボタンクリック
    /// </summary>
    private void OnSkipClicked()
    {
        DebugLog("スキップボタンクリック");
        OnSkipRequested?.Invoke();

        // 戦闘強制終了
        if (BattleManager.Instance != null)
            BattleManager.Instance.ForceEndBattle();
    }

    /// <summary>
    /// 倍速ボタンクリック
    /// </summary>
    private void OnSpeedClicked()
    {
        // 次の倍速に切り替え
        currentSpeedIndex = (currentSpeedIndex + 1) % battleSpeeds.Length;
        float newSpeed = battleSpeeds[currentSpeedIndex];

        // BattleManagerに倍速設定を適用
        if (BattleManager.Instance != null)
            BattleManager.Instance.SetBattleSpeed(newSpeed);

        UpdateSpeedButtonText();
        DebugLog($"戦闘速度変更: {speedTexts[currentSpeedIndex]}");
    }

    /// <summary>
    /// 一時停止ボタンクリック
    /// </summary>
    private void OnPauseClicked()
    {
        isPaused = !isPaused;

        // BattleManagerに一時停止設定を適用
        if (BattleManager.Instance != null)
            BattleManager.Instance.SetBattlePause(isPaused);

        UpdatePauseButtonState();
        DebugLog($"戦闘{(isPaused ? "一時停止" : "再開")}");
    }

    /// <summary>
    /// スキルボタンクリック（表示のみ）
    /// </summary>
    private void OnSkillButtonClicked(string skillName)
    {
        DebugLog($"{skillName}ボタンクリック（オートバトルのため無効）");
    }

    #endregion

    #region UI更新メソッド

    /// <summary>
    /// 倍速ボタンテキスト更新
    /// </summary>
    private void UpdateSpeedButtonText()
    {
        if (speedButtonText != null)
            speedButtonText.text = speedTexts[currentSpeedIndex];
    }

    /// <summary>
    /// 一時停止ボタン状態更新
    /// </summary>
    private void UpdatePauseButtonState()
    {
        // ボタンの見た目を一時停止状態に応じて変更
        if (pauseButton != null)
        {
            var buttonText = pauseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = isPaused ? "再開" : "一時停止";
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 初期化完了確認
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    /// <summary>
    /// 現在の戦闘状態取得
    /// </summary>
    public BattleState GetCurrentBattleState()
    {
        return currentBattleState;
    }

    /// <summary>
    /// MonsterAreaManager取得
    /// </summary>
    public MonsterAreaManager GetMonsterAreaManager()
    {
        return monsterAreaManager;
    }

    /// <summary>
    /// PlayerBattleUI取得
    /// </summary>
    public PlayerBattleUI GetPlayerBattleUI()
    {
        return playerBattleUI;
    }

    /// <summary>
    /// 修正: 現在のキャラクターデータ取得メソッド追加
    /// </summary>
    public BattleCharacterData GetCurrentPlayerData()
    {
        return currentPlayerData;
    }

    /// <summary>
    /// 修正: 現在のモンスターデータ取得メソッド追加
    /// </summary>
    public System.Collections.Generic.List<BattleCharacterData> GetCurrentEnemyData()
    {
        return currentEnemyData;
    }

    /// <summary>
    /// 修正: UI準備完了状態確認
    /// </summary>
    public bool IsUISetupComplete()
    {
        return isUISetupComplete && isDataDistributionComplete;
    }

    #endregion

    #region デバッグ

    private void DebugLog(string message)
    {
        Debug.Log($"[BattleUI] {message}");
    }

    private void DebugLogError(string message)
    {
        Debug.LogError($"[BattleUI] {message}");
    }

    #endregion
}