using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 戦闘速度制御UI
/// 責任範囲：
/// - 1倍・2倍・4倍速切替
/// - 一時停止・再開
/// - 設定の永続化
/// データアクセス統一ルール: UI層 → BattleManager → Data層
/// </summary>
public class BattleSpeedUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button speedToggleButton;
    [SerializeField] private Button pauseResumeButton;
    [SerializeField] private TextMeshProUGUI speedDisplayText;
    [SerializeField] private TextMeshProUGUI pauseButtonText;
    [SerializeField] private Image pauseButtonIcon;

    [Header("速度設定")]
    [SerializeField] private float[] speedOptions = { 1f, 2f, 4f };
    [SerializeField] private int defaultSpeedIndex = 0;

    [Header("アイコン設定（オプション）")]
    [SerializeField] private Sprite playIcon;
    [SerializeField] private Sprite pauseIcon;

    [Header("表示設定")]
    [SerializeField] private string speedDisplayFormat = "{0}x";
    [SerializeField] private string pauseText = "停止";
    [SerializeField] private string resumeText = "再開";

    [Header("設定永続化")]
    [SerializeField] private bool enablePersistentSettings = true;
    [SerializeField] private string speedPrefsKey = "BattleSpeed";
    [SerializeField] private string pausePrefsKey = "BattlePauseOnStart";

    [Header("デバッグ設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool enableOperationRestriction = true;

    // 内部状態
    private bool isInitialized = false;
    private int currentSpeedIndex = 0;
    private bool isPaused = false;
    private bool canOperate = false;

    // 連続クリック防止
    private float lastClickTime = 0f;
    private const float CLICK_COOLDOWN = 0.2f;

    #region Unity Lifecycle

    void Start()
    {
        InitializeUI();
        LoadSettings();
        SetupEventListeners();
        Log("BattleSpeedUI初期化完了");
    }

    void OnDestroy()
    {
        CleanupEventListeners();
        SaveSettings();
    }

    #endregion

    #region 初期化・終了処理

    /// <summary>
    /// UI初期化
    /// </summary>
    private void InitializeUI()
    {
        // コンポーネント存在確認
        if (speedToggleButton == null)
        {
            LogError("speedToggleButtonが設定されていません。Inspectorで設定してください。");
        }
        else
        {
            speedToggleButton.onClick.AddListener(OnSpeedToggleClicked);
        }

        if (pauseResumeButton == null)
        {
            LogError("pauseResumeButtonが設定されていません。Inspectorで設定してください。");
        }
        else
        {
            pauseResumeButton.onClick.AddListener(OnPauseResumeClicked);
        }

        // 速度オプション検証
        if (speedOptions == null || speedOptions.Length == 0)
        {
            LogError("speedOptionsが空です。デフォルト値を設定します。");
            speedOptions = new float[] { 1f, 2f, 4f };
        }

        // 初期状態設定
        currentSpeedIndex = Mathf.Clamp(defaultSpeedIndex, 0, speedOptions.Length - 1);
        isPaused = false;
        UpdateSpeedDisplay();
        UpdatePauseDisplay();

        // 操作可能状態の初期設定
        SetOperationEnabled(false);

        isInitialized = true;
        Log("BattleSpeedUI初期化処理完了");
    }

    /// <summary>
    /// イベントリスナー設定
    /// </summary>
    private void SetupEventListeners()
    {
        // BattleManagerのイベントに登録
        BattleManager.OnBattleStateChanged += OnBattleStateChanged;
        BattleManager.OnBattleInitialized += OnBattleInitialized;
        BattleManager.OnBattleCompleted += OnBattleCompleted;
        BattleManager.OnBattleError += OnBattleError;

        Log("BattleManagerイベントリスナー設定完了");
    }

    /// <summary>
    /// イベントリスナー解除
    /// </summary>
    private void CleanupEventListeners()
    {
        // BattleManagerのイベントから解除
        BattleManager.OnBattleStateChanged -= OnBattleStateChanged;
        BattleManager.OnBattleInitialized -= OnBattleInitialized;
        BattleManager.OnBattleCompleted -= OnBattleCompleted;
        BattleManager.OnBattleError -= OnBattleError;

        Log("BattleManagerイベントリスナー解除完了");
    }

    #endregion

    #region 設定永続化

    /// <summary>
    /// 設定読み込み
    /// </summary>
    private void LoadSettings()
    {
        if (!enablePersistentSettings) return;

        try
        {
            // 戦闘速度設定読み込み
            if (PlayerPrefs.HasKey(speedPrefsKey))
            {
                float savedSpeed = PlayerPrefs.GetFloat(speedPrefsKey, speedOptions[defaultSpeedIndex]);
                currentSpeedIndex = GetSpeedIndex(savedSpeed);
                Log($"戦闘速度設定読み込み: {savedSpeed}x (インデックス: {currentSpeedIndex})");
            }

            // 一時停止設定読み込み（オプション）
            if (PlayerPrefs.HasKey(pausePrefsKey))
            {
                bool savedPauseOnStart = PlayerPrefs.GetInt(pausePrefsKey, 0) == 1;
                if (savedPauseOnStart)
                {
                    Log("開始時一時停止設定が有効です");
                    // 必要に応じて開始時一時停止を適用
                }
            }

            Log("設定読み込み完了");
        }
        catch (Exception e)
        {
            LogError($"設定読み込みエラー: {e.Message}");
        }
    }

    /// <summary>
    /// 設定保存
    /// </summary>
    private void SaveSettings()
    {
        if (!enablePersistentSettings) return;

        try
        {
            // 戦闘速度設定保存
            float currentSpeed = GetCurrentSpeed();
            PlayerPrefs.SetFloat(speedPrefsKey, currentSpeed);

            // 一時停止設定保存（必要に応じて）
            PlayerPrefs.SetInt(pausePrefsKey, isPaused ? 1 : 0);

            PlayerPrefs.Save();
            Log($"設定保存完了: 速度={currentSpeed}x, 一時停止={isPaused}");
        }
        catch (Exception e)
        {
            LogError($"設定保存エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 速度値からインデックスを取得
    /// </summary>
    /// <param name="speed">速度値</param>
    /// <returns>対応するインデックス</returns>
    private int GetSpeedIndex(float speed)
    {
        for (int i = 0; i < speedOptions.Length; i++)
        {
            if (Mathf.Approximately(speedOptions[i], speed))
            {
                return i;
            }
        }
        return defaultSpeedIndex; // 見つからない場合はデフォルト
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// 戦闘状態変更イベントハンドラ
    /// </summary>
    /// <param name="newState">新しい戦闘状態</param>
    private void OnBattleStateChanged(BattleState newState)
    {
        Log($"戦闘状態変更: {newState}");

        switch (newState)
        {
            case BattleState.Idle:
                SetOperationEnabled(false);
                ResetToInitialState();
                break;

            case BattleState.Initializing:
                SetOperationEnabled(false);
                Log("戦闘初期化中 - 操作無効");
                break;

            case BattleState.InProgress:
                SetOperationEnabled(true);
                ApplyCurrentSettings();
                Log("戦闘開始 - 操作有効");
                break;

            case BattleState.Completed:
                SetOperationEnabled(false);
                SaveSettings();
                Log("戦闘完了 - 設定保存");
                break;
        }
    }

    /// <summary>
    /// 戦闘初期化イベントハンドラ
    /// </summary>
    /// <param name="setupData">戦闘セットアップデータ</param>
    private void OnBattleInitialized(BattleSetupData setupData)
    {
        try
        {
            Log("戦闘初期化 - 設定を適用");
            ApplyCurrentSettings();
        }
        catch (Exception e)
        {
            LogError($"戦闘初期化処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘完了イベントハンドラ
    /// </summary>
    /// <param name="resultData">戦闘結果データ</param>
    private void OnBattleCompleted(BattleResultData resultData)
    {
        try
        {
            Log("戦闘完了 - UI状態をリセット");
            SetOperationEnabled(false);
            SaveSettings();
        }
        catch (Exception e)
        {
            LogError($"戦闘完了処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘エラーイベントハンドラ
    /// </summary>
    /// <param name="errorMessage">エラーメッセージ</param>
    private void OnBattleError(string errorMessage)
    {
        LogError($"戦闘エラー受信: {errorMessage}");
        SetOperationEnabled(false);
        ResetToInitialState();
    }

    #endregion

    #region ボタンイベント

    /// <summary>
    /// 速度切り替えボタンクリック
    /// </summary>
    private void OnSpeedToggleClicked()
    {
        if (!CanPerformOperation()) return;

        try
        {
            // 次の速度インデックスに切り替え
            currentSpeedIndex = (currentSpeedIndex + 1) % speedOptions.Length;
            float newSpeed = GetCurrentSpeed();

            // BattleManagerに速度変更を通知
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.SetBattleSpeed(newSpeed);
                Log($"戦闘速度変更: {newSpeed}x");
            }
            else
            {
                LogError("BattleManager.Instanceがnullです");
            }

            // UI更新
            UpdateSpeedDisplay();
            UpdateLastClickTime();
        }
        catch (Exception e)
        {
            LogError($"速度切り替えエラー: {e.Message}");
        }
    }

    /// <summary>
    /// 一時停止・再開ボタンクリック
    /// </summary>
    private void OnPauseResumeClicked()
    {
        if (!CanPerformOperation()) return;

        try
        {
            // 一時停止状態を切り替え
            isPaused = !isPaused;

            // BattleManagerに一時停止状態を通知
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.SetBattlePause(isPaused);
                Log($"戦闘{(isPaused ? "一時停止" : "再開")}");
            }
            else
            {
                LogError("BattleManager.Instanceがnullです");
            }

            // UI更新
            UpdatePauseDisplay();
            UpdateLastClickTime();
        }
        catch (Exception e)
        {
            LogError($"一時停止切り替えエラー: {e.Message}");
        }
    }

    #endregion

    #region UI更新

    /// <summary>
    /// 速度表示更新
    /// </summary>
    private void UpdateSpeedDisplay()
    {
        if (speedDisplayText != null)
        {
            float currentSpeed = GetCurrentSpeed();
            speedDisplayText.text = string.Format(speedDisplayFormat, currentSpeed);
        }

        // 速度切り替えボタンのテキスト更新（ボタンにテキストがある場合）
        if (speedToggleButton != null)
        {
            var buttonText = speedToggleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                float currentSpeed = GetCurrentSpeed();
                buttonText.text = string.Format(speedDisplayFormat, currentSpeed);
            }
        }
    }

    /// <summary>
    /// 一時停止表示更新
    /// </summary>
    private void UpdatePauseDisplay()
    {
        // ボタンテキスト更新
        if (pauseButtonText != null)
        {
            pauseButtonText.text = isPaused ? resumeText : pauseText;
        }

        // ボタンアイコン更新
        if (pauseButtonIcon != null)
        {
            if (isPaused && playIcon != null)
            {
                pauseButtonIcon.sprite = playIcon;
            }
            else if (!isPaused && pauseIcon != null)
            {
                pauseButtonIcon.sprite = pauseIcon;
            }
        }

        // 一時停止・再開ボタンのテキスト更新（ボタンにテキストがある場合）
        if (pauseResumeButton != null)
        {
            var buttonText = pauseResumeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isPaused ? resumeText : pauseText;
            }
        }
    }

    /// <summary>
    /// 操作可能状態設定
    /// </summary>
    /// <param name="enabled">操作可能かどうか</param>
    private void SetOperationEnabled(bool enabled)
    {
        canOperate = enabled;

        if (speedToggleButton != null)
        {
            speedToggleButton.interactable = enabled;
        }

        if (pauseResumeButton != null)
        {
            pauseResumeButton.interactable = enabled;
        }

        Log($"操作状態変更: {(enabled ? "有効" : "無効")}");
    }

    #endregion

    #region 内部メソッド

    /// <summary>
    /// 現在の速度取得
    /// </summary>
    /// <returns>現在の戦闘速度</returns>
    private float GetCurrentSpeed()
    {
        if (currentSpeedIndex >= 0 && currentSpeedIndex < speedOptions.Length)
        {
            return speedOptions[currentSpeedIndex];
        }
        return speedOptions[defaultSpeedIndex];
    }

    /// <summary>
    /// 操作可能かチェック
    /// </summary>
    /// <returns>操作可能かどうか</returns>
    private bool CanPerformOperation()
    {
        // 初期化チェック
        if (!isInitialized)
        {
            LogError("BattleSpeedUIが初期化されていません");
            return false;
        }

        // 操作制限チェック
        if (enableOperationRestriction && !canOperate)
        {
            Log("現在操作が無効化されています");
            return false;
        }

        // 連続クリック防止
        if (Time.time - lastClickTime < CLICK_COOLDOWN)
        {
            Log("連続クリック防止中");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 最後のクリック時間更新
    /// </summary>
    private void UpdateLastClickTime()
    {
        lastClickTime = Time.time;
    }

    /// <summary>
    /// 現在の設定をBattleManagerに適用
    /// </summary>
    private void ApplyCurrentSettings()
    {
        if (BattleManager.Instance == null)
        {
            LogError("BattleManager.Instanceがnullのため設定を適用できません");
            return;
        }

        try
        {
            // 速度設定適用
            float currentSpeed = GetCurrentSpeed();
            BattleManager.Instance.SetBattleSpeed(currentSpeed);

            // 一時停止設定適用
            BattleManager.Instance.SetBattlePause(isPaused);

            Log($"設定適用完了: 速度={currentSpeed}x, 一時停止={isPaused}");
        }
        catch (Exception e)
        {
            LogError($"設定適用エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 初期状態にリセット
    /// </summary>
    private void ResetToInitialState()
    {
        currentSpeedIndex = defaultSpeedIndex;
        isPaused = false;
        UpdateSpeedDisplay();
        UpdatePauseDisplay();
        Log("UI状態を初期状態にリセット");
    }

    /// <summary>
    /// BattleManagerとの同期
    /// </summary>
    private void SyncWithBattleManager()
    {
        if (BattleManager.Instance == null) return;

        try
        {
            // BattleManagerから現在の設定を取得して同期
            float managerSpeed = BattleManager.Instance.BattleSpeedMultiplier;
            bool managerPaused = BattleManager.Instance.IsPaused;

            // UI状態を同期
            int syncedSpeedIndex = GetSpeedIndex(managerSpeed);
            if (syncedSpeedIndex != currentSpeedIndex)
            {
                currentSpeedIndex = syncedSpeedIndex;
                UpdateSpeedDisplay();
                Log($"速度設定を同期: {managerSpeed}x");
            }

            if (managerPaused != isPaused)
            {
                isPaused = managerPaused;
                UpdatePauseDisplay();
                Log($"一時停止設定を同期: {managerPaused}");
            }
        }
        catch (Exception e)
        {
            LogError($"BattleManagerとの同期エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 手動で設定をBattleManagerに適用（外部から呼び出し可能）
    /// </summary>
    public void ApplySettings()
    {
        ApplyCurrentSettings();
    }

    /// <summary>
    /// 速度を直接設定（外部から呼び出し可能）
    /// </summary>
    /// <param name="speed">設定する速度</param>
    public void SetSpeed(float speed)
    {
        int targetIndex = GetSpeedIndex(speed);
        if (targetIndex != currentSpeedIndex)
        {
            currentSpeedIndex = targetIndex;
            UpdateSpeedDisplay();

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.SetBattleSpeed(speed);
            }

            Log($"速度を直接設定: {speed}x");
        }
    }

    /// <summary>
    /// 一時停止状態を直接設定（外部から呼び出し可能）
    /// </summary>
    /// <param name="pause">一時停止するかどうか</param>
    public void SetPause(bool pause)
    {
        if (pause != isPaused)
        {
            isPaused = pause;
            UpdatePauseDisplay();

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.SetBattlePause(pause);
            }

            Log($"一時停止状態を直接設定: {pause}");
        }
    }

    /// <summary>
    /// BattleManagerとの同期を手動実行
    /// </summary>
    public void SyncWithManager()
    {
        SyncWithBattleManager();
    }

    /// <summary>
    /// 初期化状態確認
    /// </summary>
    /// <returns>初期化済みかどうか</returns>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    /// <summary>
    /// 現在の設定情報取得
    /// </summary>
    /// <returns>設定情報文字列</returns>
    public string GetCurrentSettingsInfo()
    {
        float currentSpeed = GetCurrentSpeed();
        return $"速度: {currentSpeed}x, 一時停止: {isPaused}, 操作可能: {canOperate}";
    }

    #endregion

    #region ログ・デバッグ機能

    /// <summary>
    /// ログ出力
    /// </summary>
    /// <param name="message">ログメッセージ</param>
    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[BattleSpeedUI] {message}");
        }
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    private void LogError(string message)
    {
        Debug.LogError($"[BattleSpeedUI] {message}");
    }

    /// <summary>
    /// デバッグ情報出力
    /// </summary>
    [ContextMenu("デバッグ情報出力")]
    private void DumpDebugInfo()
    {
        Log("=== BattleSpeedUI デバッグ情報 ===");
        Log($"初期化状態: {isInitialized}");
        Log($"現在の設定: {GetCurrentSettingsInfo()}");
        Log($"速度オプション: [{string.Join(", ", speedOptions)}]");
        Log($"BattleManager存在: {BattleManager.Instance != null}");

        if (BattleManager.Instance != null)
        {
            Log($"BattleManager速度: {BattleManager.Instance.BattleSpeedMultiplier}");
            Log($"BattleManager一時停止: {BattleManager.Instance.IsPaused}");
            Log($"BattleManager状態: {BattleManager.Instance.CurrentState}");
        }

        Log($"UI要素確認:");
        Log($"  speedToggleButton: {speedToggleButton != null}");
        Log($"  pauseResumeButton: {pauseResumeButton != null}");
        Log($"  speedDisplayText: {speedDisplayText != null}");
        Log($"  pauseButtonText: {pauseButtonText != null}");
    }

    #endregion
}