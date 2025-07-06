using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// プレイヤー情報表示UI
/// 責任範囲：
/// - プレイヤー基本情報の表示（名前、レベル、経験値）
/// - 通貨・リソース表示（ゴールド、ジェム、スタミナ）
/// - 戦闘力表示
/// - キャラクター画像表示
/// データアクセス統一ルール: UI層 → HomeManager → SaveDataManager → データ層
/// </summary>
public class PlayerInfoUI : MonoBehaviour
{
    [Header("プレイヤー基本情報")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private Slider expProgressSlider;
    [SerializeField] private TextMeshProUGUI expProgressText;

    [Header("通貨・リソース")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI gemsText;
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private Slider staminaProgressSlider;

    [Header("戦闘力情報")]
    [SerializeField] private TextMeshProUGUI totalCombatPowerText;
    [SerializeField] private TextMeshProUGUI weaponPowerText;
    [SerializeField] private TextMeshProUGUI armorPowerText;
    [SerializeField] private TextMeshProUGUI accessoryPowerText;

    [Header("キャラクター画像")]
    [SerializeField] private Image characterImage;
    [SerializeField] private Button characterChangeButton;

    [Header("スタミナ回復")]
    [SerializeField] private TextMeshProUGUI staminaRecoveryTimeText;
    [SerializeField] private Button staminaRecoveryButton;

    [Header("通知・バッジ")]
    [SerializeField] private GameObject newItemBadge;
    [SerializeField] private GameObject questCompleteBadge;
    [SerializeField] private GameObject notificationBadge;

    [Header("ボタン")]
    [SerializeField] private Button goldButton;
    [SerializeField] private Button gemsButton;
    [SerializeField] private Button profileButton;

    [Header("アニメーション")]
    [SerializeField] private Animator uiAnimator;
    [SerializeField] private bool enableAnimations = true;

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private float updateInterval = 1f; // UI更新間隔（秒）
    [SerializeField] private float maxInitializationWaitTime = 10f; // 最大初期化待機時間

    // イベント
    public event Action OnCharacterChangeRequested;
    public event Action OnGoldButtonClicked;
    public event Action OnGemsButtonClicked;
    public event Action OnProfileButtonClicked;
    public event Action OnStaminaRecoveryRequested;

    // 内部状態
    private PlayerSummaryData currentPlayerData;
    private bool isInitialized = false;
    private DateTime lastUpdateTime;
    private Coroutine initializationCoroutine;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        // **修正点**: コルーチンで依存関係を待機してから初期化
        initializationCoroutine = StartCoroutine(WaitForDependenciesAndInitialize());
    }

    private void OnEnable()
    {
        // **修正点**: 初期化完了後にイベント登録とデータ更新
        if (isInitialized)
        {
            RegisterEvents();
            RefreshPlayerInfo();
        }
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    private void Update()
    {
        if (isInitialized && ShouldUpdateUI())
        {
            UpdateTimeBasedUI();
        }
    }

    private void OnDestroy()
    {
        if (initializationCoroutine != null)
        {
            StopCoroutine(initializationCoroutine);
        }
        UnregisterEvents();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// **新規追加**: 依存関係の初期化完了を待機してから初期化
    /// </summary>
    private IEnumerator WaitForDependenciesAndInitialize()
    {
        Log("PlayerInfoUI初期化開始 - 依存関係チェック中...");

        float elapsed = 0f;
        while (elapsed < maxInitializationWaitTime)
        {
            if (ValidateDependencies())
            {
                Log("依存関係確認完了 - PlayerInfoUI初期化実行");
                Initialize();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        LogError($"依存関係の初期化がタイムアウトしました（{maxInitializationWaitTime}秒）");
        LogError("HomeManagerが正常に初期化されているか確認してください");
    }

    /// <summary>
    /// コンポーネントの初期化
    /// </summary>
    private void InitializeComponents()
    {
        // ボタンイベント設定
        SetupButtons();

        // 初期状態設定
        SetupInitialState();

        // スライダー設定
        SetupSliders();
    }

    /// <summary>
    /// PlayerInfoUIを初期化
    /// </summary>
    public void Initialize()
    {
        try
        {
            Log("PlayerInfoUI初期化開始");

            // 依存関係確認
            if (!ValidateDependencies())
            {
                LogError("必要な依存関係が満たされていません");
                return;
            }

            // **修正点**: イベント登録を初期化時に実行
            RegisterEvents();

            // **修正点**: 初期データ取得（実際のセーブデータから）
            RefreshPlayerInfo();

            isInitialized = true;
            lastUpdateTime = DateTime.Now;

            Log("PlayerInfoUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"PlayerInfoUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ボタン設定
    /// </summary>
    private void SetupButtons()
    {
        if (characterChangeButton != null)
        {
            characterChangeButton.onClick.RemoveAllListeners();
            characterChangeButton.onClick.AddListener(OnCharacterChangeButtonClicked);
        }

        if (staminaRecoveryButton != null)
        {
            staminaRecoveryButton.onClick.RemoveAllListeners();
            staminaRecoveryButton.onClick.AddListener(OnStaminaRecoveryButtonClicked);
        }

        if (goldButton != null)
        {
            goldButton.onClick.RemoveAllListeners();
            goldButton.onClick.AddListener(OnGoldButtonClickedInternal);
        }

        if (gemsButton != null)
        {
            gemsButton.onClick.RemoveAllListeners();
            gemsButton.onClick.AddListener(OnGemsButtonClickedInternal);
        }

        if (profileButton != null)
        {
            profileButton.onClick.RemoveAllListeners();
            profileButton.onClick.AddListener(OnProfileButtonClickedInternal);
        }
    }

    /// <summary>
    /// 初期状態設定
    /// </summary>
    private void SetupInitialState()
    {
        // バッジ非表示
        if (newItemBadge != null) newItemBadge.SetActive(false);
        if (questCompleteBadge != null) questCompleteBadge.SetActive(false);
        if (notificationBadge != null) notificationBadge.SetActive(false);

        // スタミナ回復ボタン初期状態
        if (staminaRecoveryButton != null) staminaRecoveryButton.interactable = false;
    }

    /// <summary>
    /// スライダー設定
    /// </summary>
    private void SetupSliders()
    {
        if (expProgressSlider != null)
        {
            expProgressSlider.minValue = 0f;
            expProgressSlider.maxValue = 1f;
            expProgressSlider.value = 0f;
        }

        if (staminaProgressSlider != null)
        {
            staminaProgressSlider.minValue = 0f;
            staminaProgressSlider.maxValue = 1f;
            staminaProgressSlider.value = 0f;
        }
    }

    /// <summary>
    /// **修正点**: 依存関係の検証（HomeManagerのデータ初期化まで確認）
    /// </summary>
    private bool ValidateDependencies()
    {
        if (HomeManager.Instance == null)
        {
            Log("HomeManager.Instance がnullです");
            return false;
        }

        if (!HomeManager.Instance.IsInitialized)
        {
            Log("HomeManager が初期化されていません");
            return false;
        }

        // **追加**: HomeManagerのデータ初期化確認
        var playerData = HomeManager.Instance.GetPlayerSummary();
        if (playerData == null)
        {
            Log("HomeManager からプレイヤーデータを取得できません");
            return false;
        }

        // **追加**: 実際にデータが設定されているかチェック（空のPlayerSummaryDataでないか）
        if (string.IsNullOrEmpty(playerData.playerName))
        {
            Log("HomeManager のプレイヤーデータがまだ空です（初期化中）");
            return false;
        }

        Log("全ての依存関係が満たされています");
        return true;
    }

    #endregion

    #region イベント処理

    /// <summary>
    /// イベント登録
    /// </summary>
    private void RegisterEvents()
    {
        UnregisterEvents(); // 重複登録防止

        if (HomeManager.Instance != null)
        {
            HomeManager.OnPlayerDataUpdated += OnPlayerDataUpdated;
            Log("HomeManagerイベント登録完了");
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
        }
    }

    /// <summary>
    /// プレイヤーデータ更新イベント
    /// </summary>
    /// <param name="playerData">更新されたプレイヤーデータ</param>
    private void OnPlayerDataUpdated(PlayerSummaryData playerData)
    {
        Log($"プレイヤーデータ更新イベント受信: {playerData?.playerName}");
        currentPlayerData = playerData;
        DisplayPlayerInfo(playerData);
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// **修正点**: プレイヤー情報を更新（データ検証強化）
    /// </summary>
    public void RefreshPlayerInfo()
    {
        if (!isInitialized)
        {
            Log("初期化未完了のためRefreshPlayerInfoをスキップ");
            return;
        }

        try
        {
            Log("プレイヤー情報更新開始");

            var playerData = HomeManager.Instance.GetPlayerSummary();
            if (playerData == null)
            {
                LogError("HomeManagerからプレイヤーデータを取得できませんでした");
                return;
            }

            // **追加**: データが有効かチェック
            if (string.IsNullOrEmpty(playerData.playerName))
            {
                LogError("取得したプレイヤーデータが無効です（名前が空）");
                return;
            }

            Log($"プレイヤーデータ取得成功: {playerData.playerName}, Lv.{playerData.playerLevel}, Gold: {playerData.gold}");

            currentPlayerData = playerData;
            DisplayPlayerInfo(playerData);
        }
        catch (Exception e)
        {
            LogError($"プレイヤー情報更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// アニメーション再生
    /// </summary>
    /// <param name="animationName">アニメーション名</param>
    public void PlayAnimation(string animationName)
    {
        if (!enableAnimations || uiAnimator == null) return;

        try
        {
            uiAnimator.SetTrigger(animationName);
            Log($"アニメーション再生: {animationName}");
        }
        catch (Exception e)
        {
            LogError($"アニメーション再生エラー: {e.Message}");
        }
    }

    #endregion

    #region 表示処理

    /// <summary>
    /// プレイヤー情報を表示
    /// </summary>
    /// <param name="playerData">プレイヤーデータ</param>
    private void DisplayPlayerInfo(PlayerSummaryData playerData)
    {
        if (playerData == null)
        {
            LogError("DisplayPlayerInfo: playerDataがnullです");
            return;
        }

        Log($"プレイヤー情報表示開始: {playerData.playerName}");

        // 基本情報表示
        DisplayBasicInfo(playerData);

        // 通貨・リソース表示
        DisplayCurrencyAndResources(playerData);

        // 戦闘力表示
        DisplayCombatPower(playerData);

        // キャラクター画像表示
        DisplayCharacterImage(playerData);

        // 通知バッジ表示
        DisplayNotificationBadges(playerData);

        // スタミナ回復時間表示
        DisplayStaminaRecovery(playerData);

        Log("プレイヤー情報表示更新完了");
    }

    /// <summary>
    /// 基本情報を表示
    /// </summary>
    /// <param name="playerData">プレイヤーデータ</param>
    private void DisplayBasicInfo(PlayerSummaryData playerData)
    {
        // プレイヤー名
        if (playerNameText != null)
        {
            playerNameText.text = playerData.playerName;
        }

        // レベル
        if (playerLevelText != null)
        {
            playerLevelText.text = $"Lv.{playerData.playerLevel}";
        }

        // 経験値進行度
        if (expProgressSlider != null)
        {
            expProgressSlider.value = playerData.GetExpProgress();
        }

        if (expProgressText != null)
        {
            expProgressText.text = $"{playerData.currentExp:N0} / {playerData.maxExp:N0}";
        }
    }

    /// <summary>
    /// 通貨・リソースを表示
    /// </summary>
    /// <param name="playerData">プレイヤーデータ</param>
    private void DisplayCurrencyAndResources(PlayerSummaryData playerData)
    {
        // ゴールド
        if (goldText != null)
        {
            goldText.text = playerData.GetFormattedGold();
        }

        // ジェム
        if (gemsText != null)
        {
            gemsText.text = playerData.GetFormattedGems();
        }

        // スタミナ
        if (staminaText != null)
        {
            staminaText.text = $"{playerData.currentStamina} / {playerData.maxStamina}";
        }

        // スタミナ進行度
        if (staminaProgressSlider != null)
        {
            staminaProgressSlider.value = playerData.GetStaminaProgress();
        }
    }

    /// <summary>
    /// 戦闘力を表示
    /// </summary>
    /// <param name="playerData">プレイヤーデータ</param>
    private void DisplayCombatPower(PlayerSummaryData playerData)
    {
        // 総戦闘力
        if (totalCombatPowerText != null)
        {
            totalCombatPowerText.text = playerData.GetFormattedCombatPower();
        }

        // 武器戦闘力
        if (weaponPowerText != null)
        {
            weaponPowerText.text = FormatNumber(playerData.weaponPower);
        }

        // 防具戦闘力
        if (armorPowerText != null)
        {
            armorPowerText.text = FormatNumber(playerData.armorPower);
        }

        // アクセサリー戦闘力
        if (accessoryPowerText != null)
        {
            accessoryPowerText.text = FormatNumber(playerData.accessoryPower);
        }
    }

    /// <summary>
    /// キャラクター画像を表示
    /// </summary>
    /// <param name="playerData">プレイヤーデータ</param>
    private void DisplayCharacterImage(PlayerSummaryData playerData)
    {
        if (characterImage == null) return;

        try
        {
            if (!string.IsNullOrEmpty(playerData.characterImagePath))
            {
                LoadCharacterImage(playerData.characterImagePath);
            }
            else
            {
                SetDefaultCharacterImage();
            }
        }
        catch (Exception e)
        {
            LogError($"キャラクター画像表示エラー: {e.Message}");
            SetDefaultCharacterImage();
        }
    }

    /// <summary>
    /// 通知バッジを表示
    /// </summary>
    /// <param name="playerData">プレイヤーデータ</param>
    private void DisplayNotificationBadges(PlayerSummaryData playerData)
    {
        if (newItemBadge != null)
        {
            newItemBadge.SetActive(playerData.hasNewItems);
        }

        if (questCompleteBadge != null)
        {
            questCompleteBadge.SetActive(playerData.hasCompletedQuests);
        }

        if (notificationBadge != null)
        {
            notificationBadge.SetActive(playerData.hasNewNotifications);
        }
    }

    /// <summary>
    /// スタミナ回復時間を表示
    /// </summary>
    /// <param name="playerData">プレイヤーデータ</param>
    private void DisplayStaminaRecovery(PlayerSummaryData playerData)
    {
        if (staminaRecoveryTimeText != null)
        {
            staminaRecoveryTimeText.text = playerData.GetStaminaRecoveryTimeString();
        }

        // スタミナ回復ボタンの状態
        if (staminaRecoveryButton != null)
        {
            bool canRecover = !playerData.IsStaminaFull();
            staminaRecoveryButton.interactable = canRecover;
        }
    }

    #endregion

    #region 時間ベース更新

    /// <summary>
    /// UI更新が必要かチェック
    /// </summary>
    /// <returns>更新が必要な場合true</returns>
    private bool ShouldUpdateUI()
    {
        return (DateTime.Now - lastUpdateTime).TotalSeconds >= updateInterval;
    }

    /// <summary>
    /// 時間ベースのUI要素を更新
    /// </summary>
    private void UpdateTimeBasedUI()
    {
        if (currentPlayerData == null) return;

        // スタミナ回復時間のみリアルタイム更新
        if (staminaRecoveryTimeText != null && !currentPlayerData.IsStaminaFull())
        {
            // データを再取得してスタミナ情報を更新
            var updatedData = HomeManager.Instance.GetPlayerSummary();
            if (updatedData != null)
            {
                currentPlayerData.staminaRecoveryRemaining = updatedData.staminaRecoveryRemaining;
                staminaRecoveryTimeText.text = currentPlayerData.GetStaminaRecoveryTimeString();
            }
        }

        lastUpdateTime = DateTime.Now;
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// キャラクター変更ボタンクリック
    /// </summary>
    private void OnCharacterChangeButtonClicked()
    {
        Log("キャラクター変更ボタンクリック");
        OnCharacterChangeRequested?.Invoke();
    }

    /// <summary>
    /// スタミナ回復ボタンクリック
    /// </summary>
    private void OnStaminaRecoveryButtonClicked()
    {
        Log("スタミナ回復ボタンクリック");
        OnStaminaRecoveryRequested?.Invoke();
    }

    /// <summary>
    /// ゴールドボタンクリック
    /// </summary>
    private void OnGoldButtonClickedInternal()
    {
        Log("ゴールドボタンクリック");
        OnGoldButtonClicked?.Invoke();
    }

    /// <summary>
    /// ジェムボタンクリック
    /// </summary>
    private void OnGemsButtonClickedInternal()
    {
        Log("ジェムボタンクリック");
        OnGemsButtonClicked?.Invoke();
    }

    /// <summary>
    /// プロフィールボタンクリック
    /// </summary>
    private void OnProfileButtonClickedInternal()
    {
        Log("プロフィールボタンクリック");
        OnProfileButtonClicked?.Invoke();
    }

    #endregion

    #region ユーティリティ

    /// <summary>
    /// キャラクター画像を読み込み
    /// </summary>
    /// <param name="imagePath">画像パス</param>
    private void LoadCharacterImage(string imagePath)
    {
        try
        {
            var sprite = Resources.Load<Sprite>(imagePath);
            if (sprite != null)
            {
                characterImage.sprite = sprite;
                Log($"キャラクター画像読み込み成功: {imagePath}");
            }
            else
            {
                Log($"キャラクター画像が見つかりません: {imagePath}");
                SetDefaultCharacterImage();
            }
        }
        catch (Exception e)
        {
            LogError($"キャラクター画像読み込みエラー: {e.Message}");
            SetDefaultCharacterImage();
        }
    }

    /// <summary>
    /// デフォルトキャラクター画像を設定
    /// </summary>
    private void SetDefaultCharacterImage()
    {
        try
        {
            var defaultSprite = Resources.Load<Sprite>("Images/Character/default_character");
            if (defaultSprite != null)
            {
                characterImage.sprite = defaultSprite;
            }
        }
        catch (Exception e)
        {
            LogError($"デフォルトキャラクター画像設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 数値をフォーマット
    /// </summary>
    /// <param name="number">数値</param>
    /// <returns>フォーマット済み文字列</returns>
    private string FormatNumber(int number)
    {
        if (number >= 1000000)
        {
            return $"{number / 1000000.0:F1}M";
        }
        else if (number >= 1000)
        {
            return $"{number / 1000.0:F1}K";
        }
        else
        {
            return number.ToString("N0");
        }
    }

    /// <summary>
    /// 現在のプレイヤーデータを取得
    /// </summary>
    /// <returns>プレイヤーデータ</returns>
    public PlayerSummaryData GetCurrentPlayerData()
    {
        return currentPlayerData;
    }

    /// <summary>
    /// 初期化状態を取得
    /// </summary>
    /// <returns>初期化済みの場合true</returns>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[PlayerInfoUI] {message}");
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[PlayerInfoUI] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("プレイヤー情報を手動更新")]
    private void ManualRefreshPlayerInfo()
    {
        RefreshPlayerInfo();
    }

    [ContextMenu("レベルアップアニメーション再生")]
    private void TestLevelUpAnimation()
    {
        PlayAnimation("LevelUp");
    }

    [ContextMenu("現在のデータをログ出力")]
    private void LogCurrentData()
    {
        if (currentPlayerData != null)
        {
            Log(currentPlayerData.ToString());
        }
        else
        {
            Log("現在のプレイヤーデータはnullです");
        }
    }

    [ContextMenu("依存関係を強制チェック")]
    private void ForceDependencyCheck()
    {
        bool isValid = ValidateDependencies();
        Log($"依存関係チェック結果: {isValid}");
    }
#endif

    #endregion
}