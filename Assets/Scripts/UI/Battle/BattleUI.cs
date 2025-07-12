using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 戦闘画面全体の制御・Manager層との連携窓口
/// データアクセス統一ルール: UI層 → Manager層 → Data層
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

    // 内部状態
    private BattleState currentBattleState;
    private int currentSpeedIndex = 0;
    private bool isPaused = false;
    private bool isInitialized = false;

    // 修正: キャラクターデータ保持
    private BattleCharacterData currentPlayerData;
    private System.Collections.Generic.List<BattleCharacterData> currentEnemyData;

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
    /// 修正: 戦闘初期化完了イベント - キャラクターデータを取得・配布
    /// </summary>
    private void OnBattleInitialized(BattleSetupData setupData)
    {
        try
        {
            DebugLog("戦闘初期化完了 - キャラクターデータ取得開始");

            // 戦闘画面表示
            ShowBattleUI();

            // 戦闘情報更新
            UpdateBattleInfo(setupData);

            // 修正: BattleManagerからキャラクターデータを取得
            if (BattleManager.Instance != null)
            {
                var allCharacters = BattleManager.Instance.GetAllCharacters();
                DebugLog($"BattleManagerから取得したキャラクター数: {allCharacters?.Count ?? 0}");

                if (allCharacters != null && allCharacters.Count > 0)
                {
                    // プレイヤーデータとモンスターデータを分離
                    currentPlayerData = allCharacters.Find(c => c.isPlayer);
                    currentEnemyData = allCharacters.FindAll(c => !c.isPlayer);

                    DebugLog($"プレイヤーデータ: {(currentPlayerData != null ? currentPlayerData.characterName : "null")}");
                    DebugLog($"モンスターデータ数: {currentEnemyData?.Count ?? 0}");

                    // 各UIコンポーネントにデータを配布
                    DistributeCharacterDataToComponents(setupData);
                }
                else
                {
                    DebugLogError("BattleManagerからキャラクターデータを取得できませんでした");
                }
            }
            else
            {
                DebugLogError("BattleManager.Instanceがnullです");
            }

            DebugLog($"戦闘初期化完了: {setupData.questId}");
        }
        catch (Exception e)
        {
            DebugLogError($"戦闘初期化処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: キャラクターデータを各UIコンポーネントに配布
    /// </summary>
    private void DistributeCharacterDataToComponents(BattleSetupData setupData)
    {
        try
        {
            DebugLog("キャラクターデータ配布開始");

            // PlayerBattleUIにプレイヤーデータを設定
            if (playerBattleUI != null && currentPlayerData != null)
            {
                playerBattleUI.OnBattleStart(setupData);
                playerBattleUI.UpdatePlayerData(currentPlayerData);
                DebugLog($"PlayerBattleUIにデータ設定: {currentPlayerData.characterName}");
            }
            else
            {
                DebugLogError($"PlayerBattleUI設定失敗: playerBattleUI={playerBattleUI != null}, currentPlayerData={currentPlayerData != null}");
            }

            // MonsterAreaManagerにモンスターデータを設定
            if (monsterAreaManager != null && currentEnemyData != null && currentEnemyData.Count > 0)
            {
                monsterAreaManager.OnBattleStart(setupData);
                // 修正: 直接モンスターデータを渡す
                monsterAreaManager.UpdateMonstersData(currentEnemyData);
                DebugLog($"MonsterAreaManagerにデータ設定: {currentEnemyData.Count}体");
            }
            else
            {
                DebugLogError($"MonsterAreaManager設定失敗: monsterAreaManager={monsterAreaManager != null}, currentEnemyData={currentEnemyData?.Count ?? 0}");
            }

            // 他のUIコンポーネントにも基本的な戦闘開始通知
            battleInfoUI?.OnBattleStart(setupData);
            skillInfoUI?.OnBattleStart(setupData);
            battleLogUI?.OnBattleStart(setupData);

            DebugLog("キャラクターデータ配布完了");
        }
        catch (Exception e)
        {
            DebugLogError($"キャラクターデータ配布エラー: {e.Message}");
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