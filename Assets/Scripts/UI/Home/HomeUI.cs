using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ホーム画面全体統括UI
/// 責任範囲：
/// - ホーム画面全体のUI制御とパネル管理
/// - MainButtonPanelUIとPlayerInfoUIの統合制御
/// - 各種パネル（クエスト、ショップ等）の表示/非表示管理
/// - HomeManagerとの連携によるデータ表示統合
/// データアクセス統一ルール: UI層 → HomeManager → SaveDataManager → データ層
/// </summary>
public class HomeUI : MonoBehaviour
{
    [Header("メインUI参照")]
    [SerializeField] private PlayerInfoUI playerInfoUI;
    [SerializeField] private MainButtonPanelUI mainButtonPanelUI;

    [Header("パネル参照")]
    [SerializeField] private GameObject questListPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject dailyQuestPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject characterSelectionPanel;
    [SerializeField] private GameObject miningPanel;           // 将来拡張用
    [SerializeField] private GameObject dailyMissionPanel;     // 将来拡張用
    [SerializeField] private GameObject announcementPanel;     // 将来拡張用

    [Header("背景・オーバーレイ")]
    [SerializeField] private GameObject panelOverlay;          // パネル表示時の背景オーバーレイ
    [SerializeField] private Button overlayCloseButton;       // オーバーレイタップでパネルを閉じる

    [Header("通知・ポップアップ")]
    [SerializeField] private GameObject notificationPopup;     // 通知ポップアップ
    [SerializeField] private GameObject dailyBonusPopup;      // デイリーボーナスポップアップ

    [Header("アニメーション")]
    [SerializeField] private Animator homeAnimator;
    [SerializeField] private bool enableAnimations = true;

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private float autoSaveInterval = 60f;    // 自動保存間隔（秒）

    // イベント
    public static event Action OnHomeUIInitialized;
    public static event Action<string> OnPanelOpened;
    public static event Action<string> OnPanelClosed;

    // プロパティ
    public bool IsInitialized { get; private set; }
    public string CurrentOpenPanel { get; private set; } = "";

    // 内部状態
    private bool isAnyPanelOpen = false;
    private DateTime lastAutoSaveTime;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        RegisterEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    private void Update()
    {
        if (IsInitialized)
        {
            UpdateAutoSave();
        }
    }

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネントの初期化
    /// </summary>
    private void InitializeComponents()
    {
        // オーバーレイボタン設定
        if (overlayCloseButton != null)
        {
            overlayCloseButton.onClick.RemoveAllListeners();
            overlayCloseButton.onClick.AddListener(CloseCurrentPanel);
        }

        // 初期状態設定
        SetupInitialState();
    }

    /// <summary>
    /// HomeUIを初期化
    /// </summary>
    public void Initialize()
    {
        try
        {
            Log("HomeUI初期化開始");

            // 依存関係確認
            if (!ValidateDependencies())
            {
                LogError("必要な依存関係が満たされていません");
                return;
            }

            // 子UIコンポーネントの初期化
            InitializeChildComponents();

            // 初期データ取得・表示
            RefreshAllData();

            // ログイン処理実行
            ProcessLoginSequence();

            IsInitialized = true;
            lastAutoSaveTime = DateTime.Now;

            Log("HomeUI初期化完了");
            OnHomeUIInitialized?.Invoke();
        }
        catch (Exception e)
        {
            LogError($"HomeUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 初期状態設定
    /// </summary>
    private void SetupInitialState()
    {
        // 全パネルを非表示
        CloseAllPanels();

        // オーバーレイ非表示
        if (panelOverlay != null) panelOverlay.SetActive(false);

        // ポップアップ非表示
        if (notificationPopup != null) notificationPopup.SetActive(false);
        if (dailyBonusPopup != null) dailyBonusPopup.SetActive(false);

        CurrentOpenPanel = "";
        isAnyPanelOpen = false;
    }

    /// <summary>
    /// 依存関係の確認
    /// </summary>
    private bool ValidateDependencies()
    {
        if (HomeManager.Instance == null)
        {
            LogError("HomeManagerが見つかりません");
            return false;
        }

        if (!HomeManager.Instance.IsInitialized)
        {
            LogError("HomeManagerが初期化されていません");
            return false;
        }

        if (playerInfoUI == null)
        {
            LogError("PlayerInfoUIが設定されていません");
            return false;
        }

        if (mainButtonPanelUI == null)
        {
            LogError("MainButtonPanelUIが設定されていません");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 子UIコンポーネントの初期化
    /// </summary>
    private void InitializeChildComponents()
    {
        // PlayerInfoUIの初期化確認
        if (playerInfoUI != null && !playerInfoUI.IsInitialized())
        {
            playerInfoUI.Initialize();
        }

        // MainButtonPanelUIの初期化確認
        if (mainButtonPanelUI != null && !mainButtonPanelUI.GetIsInitialized())
        {
            mainButtonPanelUI.Initialize();
        }
    }

    #endregion

    #region イベント処理

    /// <summary>
    /// イベント登録
    /// </summary>
    private void RegisterEvents()
    {
        // HomeManagerイベント
        if (HomeManager.Instance != null)
        {
            HomeManager.OnPlayerDataUpdated += OnPlayerDataUpdated;
            HomeManager.OnEquipmentDataUpdated += OnEquipmentDataUpdated;
            HomeManager.OnNotificationReceived += OnNotificationReceived;
            HomeManager.OnHomeDataRefreshed += OnHomeDataRefreshed;
        }

        // MainButtonPanelUIイベント
        if (mainButtonPanelUI != null)
        {
            mainButtonPanelUI.OnQuestButtonClicked += OnQuestButtonClicked;
            mainButtonPanelUI.OnShopButtonClicked += OnShopButtonClicked;
            mainButtonPanelUI.OnDailyQuestButtonClicked += OnDailyQuestButtonClicked;
            mainButtonPanelUI.OnSettingsButtonClicked += OnSettingsButtonClicked;
            mainButtonPanelUI.OnCharacterButtonClicked += OnCharacterButtonClicked;
        }

        // PlayerInfoUIイベント
        if (playerInfoUI != null)
        {
            playerInfoUI.OnCharacterChangeRequested += OnCharacterChangeRequested;
            playerInfoUI.OnGoldButtonClicked += OnGoldButtonClicked;
            playerInfoUI.OnGemsButtonClicked += OnGemsButtonClicked;
            playerInfoUI.OnProfileButtonClicked += OnProfileButtonClicked;
            playerInfoUI.OnStaminaRecoveryRequested += OnStaminaRecoveryRequested;
        }
    }

    /// <summary>
    /// イベント解除
    /// </summary>
    private void UnregisterEvents()
    {
        if (HomeManager.Instance != null)
        {
            HomeManager.OnPlayerDataUpdated -= OnPlayerDataUpdated;
            HomeManager.OnEquipmentDataUpdated -= OnEquipmentDataUpdated;
            HomeManager.OnNotificationReceived -= OnNotificationReceived;
            HomeManager.OnHomeDataRefreshed -= OnHomeDataRefreshed;
        }

        if (mainButtonPanelUI != null)
        {
            mainButtonPanelUI.OnQuestButtonClicked -= OnQuestButtonClicked;
            mainButtonPanelUI.OnShopButtonClicked -= OnShopButtonClicked;
            mainButtonPanelUI.OnDailyQuestButtonClicked -= OnDailyQuestButtonClicked;
            mainButtonPanelUI.OnSettingsButtonClicked -= OnSettingsButtonClicked;
            mainButtonPanelUI.OnCharacterButtonClicked -= OnCharacterButtonClicked;
        }

        if (playerInfoUI != null)
        {
            playerInfoUI.OnCharacterChangeRequested -= OnCharacterChangeRequested;
            playerInfoUI.OnGoldButtonClicked -= OnGoldButtonClicked;
            playerInfoUI.OnGemsButtonClicked -= OnGemsButtonClicked;
            playerInfoUI.OnProfileButtonClicked -= OnProfileButtonClicked;
            playerInfoUI.OnStaminaRecoveryRequested -= OnStaminaRecoveryRequested;
        }
    }

    #endregion

    #region 公開メソッド - パネル制御

    /// <summary>
    /// クエストリストパネルを開く
    /// </summary>
    public void OpenQuestListPanel()
    {
        OpenPanel("Quest", questListPanel);
    }

    /// <summary>
    /// ショップパネルを開く
    /// </summary>
    public void OpenShopPanel()
    {
        OpenPanel("Shop", shopPanel);
    }

    /// <summary>
    /// デイリークエストパネルを開く
    /// </summary>
    public void OpenDailyQuestPanel()
    {
        OpenPanel("DailyQuest", dailyQuestPanel);
    }

    /// <summary>
    /// 設定パネルを開く
    /// </summary>
    public void OpenSettingsPanel()
    {
        OpenPanel("Settings", settingsPanel);
    }

    /// <summary>
    /// キャラクター選択パネルを開く
    /// </summary>
    public void OpenCharacterSelectionPanel()
    {
        OpenPanel("CharacterSelection", characterSelectionPanel);
    }

    /// <summary>
    /// 現在開いているパネルを閉じる
    /// </summary>
    public void CloseCurrentPanel()
    {
        if (!isAnyPanelOpen) return;

        ClosePanel(CurrentOpenPanel);
    }

    /// <summary>
    /// 全てのパネルを閉じる
    /// </summary>
    public void CloseAllPanels()
    {
        var panels = new[]
        {
            questListPanel, shopPanel, dailyQuestPanel,
            settingsPanel, characterSelectionPanel,
            miningPanel, dailyMissionPanel, announcementPanel
        };

        foreach (var panel in panels)
        {
            if (panel != null) panel.SetActive(false);
        }

        if (panelOverlay != null) panelOverlay.SetActive(false);

        CurrentOpenPanel = "";
        isAnyPanelOpen = false;
    }

    /// <summary>
    /// 指定パネルが開いているかチェック
    /// </summary>
    /// <param name="panelName">パネル名</param>
    /// <returns>開いている場合true</returns>
    public bool IsPanelOpen(string panelName)
    {
        return CurrentOpenPanel == panelName && isAnyPanelOpen;
    }

    #endregion

    #region 公開メソッド - データ更新

    /// <summary>
    /// 全データを更新
    /// </summary>
    public void RefreshAllData()
    {
        if (!IsInitialized) return;

        try
        {
            // HomeManagerからデータ更新を要求
            HomeManager.Instance.RefreshHomeData();

            Log("全データ更新完了");
        }
        catch (Exception e)
        {
            LogError($"全データ更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// アニメーション再生
    /// </summary>
    /// <param name="animationName">アニメーション名</param>
    public void PlayAnimation(string animationName)
    {
        if (!enableAnimations || homeAnimator == null) return;

        try
        {
            homeAnimator.SetTrigger(animationName);
            Log($"アニメーション再生: {animationName}");
        }
        catch (Exception e)
        {
            LogError($"アニメーション再生エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - パネル制御

    /// <summary>
    /// パネルを開く
    /// </summary>
    /// <param name="panelName">パネル名</param>
    /// <param name="panelObject">パネルオブジェクト</param>
    private void OpenPanel(string panelName, GameObject panelObject)
    {
        if (panelObject == null)
        {
            LogError($"パネルが設定されていません: {panelName}");
            return;
        }

        try
        {
            // 他のパネルを閉じる
            CloseAllPanels();

            // パネルとオーバーレイを表示
            panelObject.SetActive(true);
            if (panelOverlay != null) panelOverlay.SetActive(true);

            CurrentOpenPanel = panelName;
            isAnyPanelOpen = true;

            Log($"パネル開く: {panelName}");
            OnPanelOpened?.Invoke(panelName);

            // パネル用アニメーション再生
            PlayAnimation($"Open{panelName}Panel");
        }
        catch (Exception e)
        {
            LogError($"パネル表示エラー ({panelName}): {e.Message}");
        }
    }

    /// <summary>
    /// パネルを閉じる
    /// </summary>
    /// <param name="panelName">パネル名</param>
    private void ClosePanel(string panelName)
    {
        try
        {
            // パネルとオーバーレイを非表示
            var panelObject = GetPanelByName(panelName);
            if (panelObject != null) panelObject.SetActive(false);

            if (panelOverlay != null) panelOverlay.SetActive(false);

            CurrentOpenPanel = "";
            isAnyPanelOpen = false;

            Log($"パネル閉じる: {panelName}");
            OnPanelClosed?.Invoke(panelName);

            // パネル用アニメーション再生
            PlayAnimation($"Close{panelName}Panel");
        }
        catch (Exception e)
        {
            LogError($"パネル非表示エラー ({panelName}): {e.Message}");
        }
    }

    /// <summary>
    /// パネル名からパネルオブジェクトを取得
    /// </summary>
    /// <param name="panelName">パネル名</param>
    /// <returns>パネルオブジェクト</returns>
    private GameObject GetPanelByName(string panelName)
    {
        return panelName switch
        {
            "Quest" => questListPanel,
            "Shop" => shopPanel,
            "DailyQuest" => dailyQuestPanel,
            "Settings" => settingsPanel,
            "CharacterSelection" => characterSelectionPanel,
            "Mining" => miningPanel,
            "DailyMission" => dailyMissionPanel,
            "Announcement" => announcementPanel,
            _ => null
        };
    }

    #endregion

    #region 内部メソッド - 特殊処理

    /// <summary>
    /// ログイン処理シーケンス
    /// </summary>
    private void ProcessLoginSequence()
    {
        try
        {
            Log("ログイン処理開始");

            // HomeManagerでログイン処理実行
            HomeManager.Instance.ProcessLogin();

            // デイリーボーナスチェック
            if (HomeManager.Instance.CheckDailyBonus())
            {
                ShowDailyBonusPopup();
            }

            Log("ログイン処理完了");
        }
        catch (Exception e)
        {
            LogError($"ログイン処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// デイリーボーナスポップアップ表示
    /// </summary>
    private void ShowDailyBonusPopup()
    {
        if (dailyBonusPopup != null)
        {
            dailyBonusPopup.SetActive(true);
            PlayAnimation("ShowDailyBonus");
            Log("デイリーボーナスポップアップ表示");
        }
    }

    /// <summary>
    /// 通知ポップアップ表示
    /// </summary>
    /// <param name="message">通知メッセージ</param>
    private void ShowNotificationPopup(string message)
    {
        if (notificationPopup != null)
        {
            notificationPopup.SetActive(true);
            // TODO: 通知メッセージテキストの設定
            PlayAnimation("ShowNotification");
            Log($"通知ポップアップ表示: {message}");
        }
    }

    /// <summary>
    /// 自動保存処理
    /// </summary>
    private void UpdateAutoSave()
    {
        if ((DateTime.Now - lastAutoSaveTime).TotalSeconds >= autoSaveInterval)
        {
            SaveDataManager.Instance.SaveSaveData();
            lastAutoSaveTime = DateTime.Now;
            Log("自動保存実行");
        }
    }

    #endregion

    #region イベントハンドラ - HomeManager

    /// <summary>
    /// プレイヤーデータ更新イベント
    /// </summary>
    /// <param name="playerData">更新されたプレイヤーデータ</param>
    private void OnPlayerDataUpdated(PlayerSummaryData playerData)
    {
        Log("プレイヤーデータ更新通知受信");
        // PlayerInfoUIが自動的に更新される
    }

    /// <summary>
    /// 装備データ更新イベント
    /// </summary>
    /// <param name="equipmentData">更新された装備データ</param>
    private void OnEquipmentDataUpdated(EquipmentSummaryData equipmentData)
    {
        Log("装備データ更新通知受信");
        // 装備関連UIが自動的に更新される
    }

    /// <summary>
    /// 通知受信イベント
    /// </summary>
    /// <param name="message">通知メッセージ</param>
    private void OnNotificationReceived(string message)
    {
        ShowNotificationPopup(message);
    }

    /// <summary>
    /// ホームデータ更新完了イベント
    /// </summary>
    private void OnHomeDataRefreshed()
    {
        Log("ホームデータ更新完了");
    }

    #endregion

    #region イベントハンドラ - UI操作

    /// <summary>
    /// クエストボタンクリック
    /// </summary>
    private void OnQuestButtonClicked()
    {
        Log("クエストボタンクリック");
        OpenQuestListPanel();
    }

    /// <summary>
    /// ショップボタンクリック
    /// </summary>
    private void OnShopButtonClicked()
    {
        Log("ショップボタンクリック");
        OpenShopPanel();
    }

    /// <summary>
    /// デイリークエストボタンクリック
    /// </summary>
    private void OnDailyQuestButtonClicked()
    {
        Log("デイリークエストボタンクリック");
        OpenDailyQuestPanel();
    }

    /// <summary>
    /// 設定ボタンクリック
    /// </summary>
    private void OnSettingsButtonClicked()
    {
        Log("設定ボタンクリック");
        OpenSettingsPanel();
    }

    /// <summary>
    /// キャラクターボタンクリック
    /// </summary>
    private void OnCharacterButtonClicked()
    {
        Log("キャラクターボタンクリック");
        OpenCharacterSelectionPanel();
    }

    /// <summary>
    /// キャラクター変更要求
    /// </summary>
    private void OnCharacterChangeRequested()
    {
        Log("キャラクター変更要求");
        OpenCharacterSelectionPanel();
    }

    /// <summary>
    /// ゴールドボタンクリック
    /// </summary>
    private void OnGoldButtonClicked()
    {
        Log("ゴールドボタンクリック");
        OpenShopPanel(); // ショップでゴールド購入
    }

    /// <summary>
    /// ジェムボタンクリック
    /// </summary>
    private void OnGemsButtonClicked()
    {
        Log("ジェムボタンクリック");
        OpenShopPanel(); // ショップでジェム購入
    }

    /// <summary>
    /// プロフィールボタンクリック
    /// </summary>
    private void OnProfileButtonClicked()
    {
        Log("プロフィールボタンクリック");
        // TODO: プロフィール画面への遷移（将来実装）
    }

    /// <summary>
    /// スタミナ回復要求
    /// </summary>
    private void OnStaminaRecoveryRequested()
    {
        Log("スタミナ回復要求");
        HomeManager.Instance.ForceStaminaRecovery();
    }

    #endregion

    #region ユーティリティ

    /// <summary>
    /// 現在の状態情報を取得
    /// </summary>
    /// <returns>状態情報</returns>
    public string GetCurrentStatus()
    {
        return $"HomeUI - Initialized:{IsInitialized}, OpenPanel:{CurrentOpenPanel}, AnyPanelOpen:{isAnyPanelOpen}";
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[HomeUI] {message}");
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[HomeUI] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("全データを手動更新")]
    private void ManualRefreshAllData()
    {
        RefreshAllData();
    }

    [ContextMenu("全パネルを閉じる")]
    private void ManualCloseAllPanels()
    {
        CloseAllPanels();
    }

    [ContextMenu("現在の状態をログ出力")]
    private void LogCurrentStatus()
    {
        Log(GetCurrentStatus());
    }

    [ContextMenu("デイリーボーナステスト")]
    private void TestDailyBonus()
    {
        ShowDailyBonusPopup();
    }
#endif

    #endregion
}