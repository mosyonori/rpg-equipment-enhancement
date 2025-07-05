using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// メインボタン群とパネル制御UI
/// 責任範囲：
/// - メインボタンのUI制御とイベント発火
/// - 既存SceneTransitionManagerを使用したシーン遷移
/// - ボタン状態管理（有効/無効、通知バッジ等）
/// - パネル表示要求のイベント発火（実際のパネル制御はHomeUIが担当）
/// データアクセス統一ルール: UI層 → HomeManager → SaveDataManager → データ層
/// </summary>
public class MainButtonPanelUI : MonoBehaviour
{
    [Header("シーン遷移ボタン")]
    [SerializeField] private Button equipmentEditButton;
    [SerializeField] private Button equipmentEnhanceButton;
    [SerializeField] private Button battleButton;
    [SerializeField] private Button gachaButton;

    [Header("パネル表示ボタン")]
    [SerializeField] private Button questButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button dailyQuestButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button characterSettingButton;

    [Header("将来拡張用ボタン")]
    [SerializeField] private Button miningButton;           // 将来実装：採掘システム
    [SerializeField] private Button dailyMissionButton;     // 将来実装：デイリーミッション
    [SerializeField] private Button announcementButton;     // 将来実装：アナウンス

    [Header("通知バッジ")]
    [SerializeField] private GameObject questNotificationBadge;
    [SerializeField] private GameObject shopNotificationBadge;
    [SerializeField] private GameObject dailyQuestNotificationBadge;
    [SerializeField] private GameObject characterNotificationBadge;
    [SerializeField] private GameObject announcementNotificationBadge;

    [Header("ボタン無効化表示")]
    [SerializeField] private GameObject battleDisabledOverlay;      // 戦闘ボタン無効化オーバーレイ
    [SerializeField] private GameObject gachaDisabledOverlay;       // ガチャボタン無効化オーバーレイ
    [SerializeField] private GameObject miningDisabledOverlay;      // 採掘ボタン無効化オーバーレイ

    [Header("アニメーション")]
    [SerializeField] private Animator buttonAnimator;
    [SerializeField] private bool enableButtonAnimations = true;

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;

    // シーン遷移要求イベント（静的）
    public static event Action OnEquipmentEditRequested;
    public static event Action OnEquipmentEnhanceRequested;
    public static event Action OnBattleRequested;
    public static event Action OnGachaRequested;

    // パネル表示要求イベント（インスタンス）
    public event Action OnQuestButtonClicked;
    public event Action OnShopButtonClicked;
    public event Action OnDailyQuestButtonClicked;
    public event Action OnSettingsButtonClicked;
    public event Action OnCharacterButtonClicked;

    // 将来拡張用イベント（インスタンス）
    public event Action OnMiningButtonClicked;
    public event Action OnDailyMissionButtonClicked;
    public event Action OnAnnouncementButtonClicked;

    // プロパティ
    public bool IsInitialized { get; private set; }

    // 内部状態
    private bool isAnyButtonDisabled = false;

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

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネントの初期化
    /// </summary>
    private void InitializeComponents()
    {
        // ボタンイベント設定
        SetupButtonEvents();

        // 初期状態設定
        SetupInitialState();
    }

    /// <summary>
    /// MainButtonPanelUIを初期化
    /// </summary>
    public void Initialize()
    {
        try
        {
            Log("MainButtonPanelUI初期化開始");

            // 依存関係確認
            if (!ValidateDependencies())
            {
                LogError("必要な依存関係が満たされていません");
                return;
            }

            // ボタン状態更新
            UpdateAllButtonStates();

            IsInitialized = true;
            Log("MainButtonPanelUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"MainButtonPanelUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ボタンイベント設定
    /// </summary>
    private void SetupButtonEvents()
    {
        // シーン遷移ボタン
        if (equipmentEditButton != null)
        {
            equipmentEditButton.onClick.RemoveAllListeners();
            equipmentEditButton.onClick.AddListener(OnEquipmentEditButtonClicked);
        }

        if (equipmentEnhanceButton != null)
        {
            equipmentEnhanceButton.onClick.RemoveAllListeners();
            equipmentEnhanceButton.onClick.AddListener(OnEquipmentEnhanceButtonClicked);
        }

        if (battleButton != null)
        {
            battleButton.onClick.RemoveAllListeners();
            battleButton.onClick.AddListener(OnBattleButtonClicked);
        }

        if (gachaButton != null)
        {
            gachaButton.onClick.RemoveAllListeners();
            gachaButton.onClick.AddListener(OnGachaButtonClicked);
        }

        // パネル表示ボタン
        if (questButton != null)
        {
            questButton.onClick.RemoveAllListeners();
            questButton.onClick.AddListener(OnQuestButtonClickedInternal);
        }

        if (shopButton != null)
        {
            shopButton.onClick.RemoveAllListeners();
            shopButton.onClick.AddListener(OnShopButtonClickedInternal);
        }

        if (dailyQuestButton != null)
        {
            dailyQuestButton.onClick.RemoveAllListeners();
            dailyQuestButton.onClick.AddListener(OnDailyQuestButtonClickedInternal);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OnSettingsButtonClickedInternal);
        }

        if (characterSettingButton != null)
        {
            characterSettingButton.onClick.RemoveAllListeners();
            characterSettingButton.onClick.AddListener(OnCharacterButtonClickedInternal);
        }

        // 将来拡張用ボタン（現在は機能なし）
        if (miningButton != null)
        {
            miningButton.onClick.RemoveAllListeners();
            miningButton.onClick.AddListener(OnMiningButtonClickedInternal);
        }

        if (dailyMissionButton != null)
        {
            dailyMissionButton.onClick.RemoveAllListeners();
            dailyMissionButton.onClick.AddListener(OnDailyMissionButtonClickedInternal);
        }

        if (announcementButton != null)
        {
            announcementButton.onClick.RemoveAllListeners();
            announcementButton.onClick.AddListener(OnAnnouncementButtonClickedInternal);
        }
    }

    /// <summary>
    /// 初期状態設定
    /// </summary>
    private void SetupInitialState()
    {
        // 通知バッジ初期状態（非表示）
        HideAllNotificationBadges();

        // 将来実装機能のボタンを無効化
        SetFutureFeatureButtonStates(false);

        // 無効化オーバーレイ初期状態
        if (battleDisabledOverlay != null) battleDisabledOverlay.SetActive(false);
        if (gachaDisabledOverlay != null) gachaDisabledOverlay.SetActive(true);  // ガチャは将来実装のため無効
        if (miningDisabledOverlay != null) miningDisabledOverlay.SetActive(true); // 採掘は将来実装のため無効
    }

    /// <summary>
    /// 依存関係の確認
    /// </summary>
    private bool ValidateDependencies()
    {
        if (SceneTransitionManager.Instance == null)
        {
            LogError("SceneTransitionManagerが見つかりません");
            return false;
        }

        return true;
    }

    #endregion

    #region イベント処理

    /// <summary>
    /// イベント登録
    /// </summary>
    private void RegisterEvents()
    {
        // HomeManagerからの通知を受信
        if (HomeManager.Instance != null)
        {
            HomeManager.OnPlayerDataUpdated += OnPlayerDataUpdated;
            HomeManager.OnEquipmentDataUpdated += OnEquipmentDataUpdated;
            HomeManager.OnNotificationReceived += OnNotificationReceived;
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
        }
    }

    #endregion

    #region 公開メソッド - ボタン状態制御

    /// <summary>
    /// 全ボタン状態を更新
    /// </summary>
    public void UpdateAllButtonStates()
    {
        if (!IsInitialized) return;

        try
        {
            // プレイヤーデータに基づくボタン状態更新
            UpdateButtonInteractability();

            // 通知バッジ状態更新
            UpdateNotificationBadges();

            Log("全ボタン状態更新完了");
        }
        catch (Exception e)
        {
            LogError($"ボタン状態更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 通知バッジ表示設定
    /// </summary>
    /// <param name="badgeType">バッジタイプ</param>
    /// <param name="visible">表示するかどうか</param>
    public void SetNotificationBadge(string badgeType, bool visible)
    {
        try
        {
            var badge = GetNotificationBadge(badgeType);
            if (badge != null)
            {
                badge.SetActive(visible);
                Log($"通知バッジ更新: {badgeType} = {visible}");
            }
        }
        catch (Exception e)
        {
            LogError($"通知バッジ設定エラー ({badgeType}): {e.Message}");
        }
    }

    /// <summary>
    /// ボタンアニメーション再生
    /// </summary>
    /// <param name="animationName">アニメーション名</param>
    public void PlayButtonAnimation(string animationName)
    {
        if (!enableButtonAnimations || buttonAnimator == null) return;

        try
        {
            buttonAnimator.SetTrigger(animationName);
            Log($"ボタンアニメーション再生: {animationName}");
        }
        catch (Exception e)
        {
            LogError($"ボタンアニメーション再生エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - ボタン状態管理

    /// <summary>
    /// ボタンのインタラクト可能状態を更新
    /// </summary>
    private void UpdateButtonInteractability()
    {
        if (HomeManager.Instance == null) return;

        var playerData = HomeManager.Instance.GetPlayerSummary();
        var equipmentData = HomeManager.Instance.GetEquipmentSummary();

        // 戦闘ボタン：スタミナとクエスト進行状況で判定
        bool canBattle = playerData.currentStamina > 0 && playerData.playerLevel >= 1;
        SetButtonInteractable(battleButton, canBattle);
        if (battleDisabledOverlay != null) battleDisabledOverlay.SetActive(!canBattle);

        // 装備編集ボタン：装備を所持している場合のみ有効
        bool hasEquipment = equipmentData.totalEquipmentCount > 0;
        SetButtonInteractable(equipmentEditButton, hasEquipment);

        // 装備強化ボタン：強化可能な装備がある場合のみ有効
        bool canEnhance = equipmentData.hasRecommendedEnhancements;
        SetButtonInteractable(equipmentEnhanceButton, true); // 常に有効（装備がなくても説明表示）

        // その他のボタンは基本的に常に有効
        SetButtonInteractable(questButton, true);
        SetButtonInteractable(shopButton, true);
        SetButtonInteractable(dailyQuestButton, true);
        SetButtonInteractable(settingsButton, true);
        SetButtonInteractable(characterSettingButton, true);
    }

    /// <summary>
    /// 通知バッジ状態を更新
    /// </summary>
    private void UpdateNotificationBadges()
    {
        if (HomeManager.Instance == null) return;

        var playerData = HomeManager.Instance.GetPlayerSummary();
        var equipmentData = HomeManager.Instance.GetEquipmentSummary();

        // クエスト通知：完了済みクエストがある場合
        SetNotificationBadge("Quest", playerData.hasCompletedQuests);

        // ショップ通知：新アイテムがある場合（将来実装）
        SetNotificationBadge("Shop", false);

        // デイリークエスト通知：未完了のデイリークエストがある場合（将来実装）
        SetNotificationBadge("DailyQuest", false);

        // キャラクター通知：装備推奨がある場合
        SetNotificationBadge("Character", equipmentData.hasRecommendedEnhancements);

        // アナウンス通知：新しいお知らせがある場合（将来実装）
        SetNotificationBadge("Announcement", false);
    }

    /// <summary>
    /// 将来実装機能のボタン状態設定
    /// </summary>
    /// <param name="enabled">有効にするかどうか</param>
    private void SetFutureFeatureButtonStates(bool enabled)
    {
        // ガチャ（将来実装）
        SetButtonInteractable(gachaButton, enabled);

        // 採掘（将来実装）
        SetButtonInteractable(miningButton, enabled);

        // デイリーミッション（将来実装）
        SetButtonInteractable(dailyMissionButton, enabled);

        // アナウンス（将来実装）
        SetButtonInteractable(announcementButton, enabled);
    }

    /// <summary>
    /// 全通知バッジを非表示
    /// </summary>
    private void HideAllNotificationBadges()
    {
        var badges = new[]
        {
            questNotificationBadge, shopNotificationBadge, dailyQuestNotificationBadge,
            characterNotificationBadge, announcementNotificationBadge
        };

        foreach (var badge in badges)
        {
            if (badge != null) badge.SetActive(false);
        }
    }

    /// <summary>
    /// 指定されたボタンのインタラクト状態を設定
    /// </summary>
    /// <param name="button">対象ボタン</param>
    /// <param name="interactable">インタラクト可能かどうか</param>
    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    /// <summary>
    /// 通知バッジオブジェクトを取得
    /// </summary>
    /// <param name="badgeType">バッジタイプ</param>
    /// <returns>バッジオブジェクト</returns>
    private GameObject GetNotificationBadge(string badgeType)
    {
        return badgeType switch
        {
            "Quest" => questNotificationBadge,
            "Shop" => shopNotificationBadge,
            "DailyQuest" => dailyQuestNotificationBadge,
            "Character" => characterNotificationBadge,
            "Announcement" => announcementNotificationBadge,
            _ => null
        };
    }

    #endregion

    #region イベントハンドラ - HomeManager

    /// <summary>
    /// プレイヤーデータ更新イベント
    /// </summary>
    /// <param name="playerData">更新されたプレイヤーデータ</param>
    private void OnPlayerDataUpdated(PlayerSummaryData playerData)
    {
        UpdateButtonInteractability();
        UpdateNotificationBadges();
    }

    /// <summary>
    /// 装備データ更新イベント
    /// </summary>
    /// <param name="equipmentData">更新された装備データ</param>
    private void OnEquipmentDataUpdated(EquipmentSummaryData equipmentData)
    {
        UpdateButtonInteractability();
        UpdateNotificationBadges();
    }

    /// <summary>
    /// 通知受信イベント
    /// </summary>
    /// <param name="message">通知メッセージ</param>
    private void OnNotificationReceived(string message)
    {
        // 通知に応じてバッジ状態を更新
        UpdateNotificationBadges();
    }

    #endregion

    #region イベントハンドラ - ボタンクリック（シーン遷移）

    /// <summary>
    /// 装備編集ボタンクリック
    /// </summary>
    private void OnEquipmentEditButtonClicked()
    {
        try
        {
            Log("装備編集ボタンクリック");
            PlayButtonAnimation("ClickEquipmentEdit");

            // 既存SceneTransitionManagerを使用
            SceneTransitionManager.Instance.TransitionToEquipmentEdit();
            OnEquipmentEditRequested?.Invoke();
        }
        catch (Exception e)
        {
            LogError($"装備編集画面遷移エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 装備強化ボタンクリック
    /// </summary>
    private void OnEquipmentEnhanceButtonClicked()
    {
        try
        {
            Log("装備強化ボタンクリック");
            PlayButtonAnimation("ClickEquipmentEnhance");

            // 既存SceneTransitionManagerを使用
            SceneTransitionManager.Instance.TransitionToEquipmentEnhance();
            OnEquipmentEnhanceRequested?.Invoke();
        }
        catch (Exception e)
        {
            LogError($"装備強化画面遷移エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘ボタンクリック
    /// </summary>
    private void OnBattleButtonClicked()
    {
        try
        {
            Log("戦闘ボタンクリック");
            PlayButtonAnimation("ClickBattle");

            // スタミナチェック
            var playerData = HomeManager.Instance.GetPlayerSummary();
            if (playerData.currentStamina <= 0)
            {
                LogError("スタミナが不足しています");
                return;
            }

            // TODO: 戦闘画面遷移（将来実装）
            // SceneTransitionManager.Instance.TransitionToBattle();
            Log("戦闘画面は将来実装予定です");
            OnBattleRequested?.Invoke();
        }
        catch (Exception e)
        {
            LogError($"戦闘画面遷移エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ガチャボタンクリック
    /// </summary>
    private void OnGachaButtonClicked()
    {
        try
        {
            Log("ガチャボタンクリック");
            PlayButtonAnimation("ClickGacha");

            // TODO: ガチャ画面遷移（将来実装）
            // SceneTransitionManager.Instance.TransitionToGacha();
            Log("ガチャ画面は将来実装予定です");
            OnGachaRequested?.Invoke();
        }
        catch (Exception e)
        {
            LogError($"ガチャ画面遷移エラー: {e.Message}");
        }
    }

    #endregion

    #region イベントハンドラ - ボタンクリック（パネル表示）

    /// <summary>
    /// クエストボタンクリック
    /// </summary>
    private void OnQuestButtonClickedInternal()
    {
        Log("クエストボタンクリック");
        PlayButtonAnimation("ClickQuest");
        OnQuestButtonClicked?.Invoke();
    }

    /// <summary>
    /// ショップボタンクリック
    /// </summary>
    private void OnShopButtonClickedInternal()
    {
        Log("ショップボタンクリック");
        PlayButtonAnimation("ClickShop");
        OnShopButtonClicked?.Invoke();
    }

    /// <summary>
    /// デイリークエストボタンクリック
    /// </summary>
    private void OnDailyQuestButtonClickedInternal()
    {
        Log("デイリークエストボタンクリック");
        PlayButtonAnimation("ClickDailyQuest");
        OnDailyQuestButtonClicked?.Invoke();
    }

    /// <summary>
    /// 設定ボタンクリック
    /// </summary>
    private void OnSettingsButtonClickedInternal()
    {
        Log("設定ボタンクリック");
        PlayButtonAnimation("ClickSettings");
        OnSettingsButtonClicked?.Invoke();
    }

    /// <summary>
    /// キャラクター設定ボタンクリック
    /// </summary>
    private void OnCharacterButtonClickedInternal()
    {
        Log("キャラクター設定ボタンクリック");
        PlayButtonAnimation("ClickCharacter");
        OnCharacterButtonClicked?.Invoke();
    }

    /// <summary>
    /// 採掘ボタンクリック（将来実装）
    /// </summary>
    private void OnMiningButtonClickedInternal()
    {
        Log("採掘ボタンクリック（将来実装）");
        PlayButtonAnimation("ClickMining");
        OnMiningButtonClicked?.Invoke();
    }

    /// <summary>
    /// デイリーミッションボタンクリック（将来実装）
    /// </summary>
    private void OnDailyMissionButtonClickedInternal()
    {
        Log("デイリーミッションボタンクリック（将来実装）");
        PlayButtonAnimation("ClickDailyMission");
        OnDailyMissionButtonClicked?.Invoke();
    }

    /// <summary>
    /// アナウンスボタンクリック（将来実装）
    /// </summary>
    private void OnAnnouncementButtonClickedInternal()
    {
        Log("アナウンスボタンクリック（将来実装）");
        PlayButtonAnimation("ClickAnnouncement");
        OnAnnouncementButtonClicked?.Invoke();
    }

    #endregion

    #region ユーティリティ

    /// <summary>
    /// 現在の状態情報を取得
    /// </summary>
    /// <returns>状態情報</returns>
    public string GetCurrentStatus()
    {
        return $"MainButtonPanelUI - Initialized:{IsInitialized}, AnyButtonDisabled:{isAnyButtonDisabled}";
    }

    /// <summary>
    /// 初期化状態を取得
    /// </summary>
    /// <returns>初期化済みの場合true</returns>
    public bool GetIsInitialized()
    {
        return IsInitialized;
    }

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MainButtonPanelUI] {message}");
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[MainButtonPanelUI] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("全ボタン状態を手動更新")]
    private void ManualUpdateAllButtonStates()
    {
        UpdateAllButtonStates();
    }

    [ContextMenu("全通知バッジ表示テスト")]
    private void TestShowAllNotificationBadges()
    {
        SetNotificationBadge("Quest", true);
        SetNotificationBadge("Shop", true);
        SetNotificationBadge("DailyQuest", true);
        SetNotificationBadge("Character", true);
        SetNotificationBadge("Announcement", true);
    }

    [ContextMenu("全通知バッジ非表示テスト")]
    private void TestHideAllNotificationBadges()
    {
        HideAllNotificationBadges();
    }

    [ContextMenu("現在の状態をログ出力")]
    private void LogCurrentStatus()
    {
        Log(GetCurrentStatus());
    }

    [ContextMenu("ボタンアニメーションテスト")]
    private void TestButtonAnimation()
    {
        PlayButtonAnimation("TestAnimation");
    }
#endif

    #endregion
}