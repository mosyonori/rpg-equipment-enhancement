using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 戦闘速度制御UI管理
/// 役割：戦闘速度制御UIの管理
/// 機能：1倍・2倍・4倍速切替、一時停止・再開、設定の永続化
/// データアクセス統一ルール: UI層 → Manager層 → データ層
/// </summary>
public class BattleSpeedUI : MonoBehaviour
{
    [Header("速度制御ボタン")]
    [SerializeField] private Button speedToggleButton;
    [SerializeField] private TextMeshProUGUI speedButtonText;
    [SerializeField] private Image speedButtonIcon;

    [Header("一時停止制御")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private TextMeshProUGUI pauseButtonText;
    [SerializeField] private Image pauseButtonIcon;
    [SerializeField] private GameObject pauseIndicator;

    [Header("速度表示")]
    [SerializeField] private GameObject speedDisplayPanel;
    [SerializeField] private TextMeshProUGUI currentSpeedText;
    [SerializeField] private Slider speedIndicatorSlider;

    [Header("速度設定")]
    [SerializeField] private float[] availableSpeeds = { 1.0f, 2.0f, 4.0f };
    [SerializeField] private string[] speedDisplayTexts = { "1x", "2x", "4x" };
    [SerializeField] private Color[] speedColors = { Color.white, Color.yellow, Color.red };

    [Header("アイコン設定")]
    [SerializeField] private Sprite playIcon;
    [SerializeField] private Sprite pauseIcon;
    [SerializeField] private Sprite[] speedIcons;

    [Header("アニメーション設定")]
    [SerializeField] private float buttonAnimationDuration = 0.2f;
    [SerializeField] private float speedChangeAnimationDuration = 0.3f;
    [SerializeField] private AnimationCurve buttonScaleEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("設定保存")]
    [SerializeField] private bool saveSpeedSettings = true;
    [SerializeField] private string speedPrefsKey = "BattleSpeed";
    [SerializeField] private string pausePrefsKey = "BattlePaused";

    // イベント
    public static event Action<float> OnSpeedChanged;
    public static event Action<bool> OnPauseStateChanged;

    // 内部状態
    private bool isInitialized = false;
    private int currentSpeedIndex = 0;
    private bool isPaused = false;
    private float currentSpeed = 1.0f;

    // アニメーション用
    private Coroutine buttonAnimationCoroutine;
    private Coroutine speedChangeCoroutine;

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateComponents();
    }

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        SaveCurrentSettings();
        StopAllCoroutines();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// UIコンポーネントの初期化
    /// </summary>
    public void Initialize()
    {
        try
        {
            Log("BattleSpeedUI初期化開始");

            // ボタンイベント登録
            RegisterButtonEvents();

            // 保存済み設定の読み込み
            LoadSavedSettings();

            // UI初期状態設定
            UpdateSpeedDisplay();
            UpdatePauseDisplay();

            // 速度インジケーター設定
            SetupSpeedIndicator();

            isInitialized = true;
            Log("BattleSpeedUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"BattleSpeedUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// コンポーネント検証
    /// </summary>
    private void ValidateComponents()
    {
        if (speedToggleButton == null)
            LogWarning("speedToggleButtonが設定されていません");

        if (pauseButton == null)
            LogWarning("pauseButtonが設定されていません");

        if (speedButtonText == null)
            LogWarning("speedButtonTextが設定されていません");

        if (availableSpeeds.Length != speedDisplayTexts.Length)
            LogWarning("availableSpeedsとspeedDisplayTextsの配列長が一致しません");
    }

    /// <summary>
    /// ボタンイベント登録
    /// </summary>
    private void RegisterButtonEvents()
    {
        if (speedToggleButton != null)
            speedToggleButton.onClick.AddListener(OnSpeedToggleClicked);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseToggleClicked);
    }

    /// <summary>
    /// 速度インジケーター設定
    /// </summary>
    private void SetupSpeedIndicator()
    {
        if (speedIndicatorSlider != null)
        {
            speedIndicatorSlider.minValue = 0f;
            speedIndicatorSlider.maxValue = availableSpeeds.Length - 1;
            speedIndicatorSlider.wholeNumbers = true;
            speedIndicatorSlider.interactable = false;
            speedIndicatorSlider.value = currentSpeedIndex;
        }
    }

    #endregion

    #region 公開メソッド - 速度制御

    /// <summary>
    /// 戦闘速度を設定
    /// </summary>
    /// <param name="speedMultiplier">速度倍率</param>
    public void SetBattleSpeed(float speedMultiplier)
    {
        try
        {
            // 対応する速度インデックスを検索
            int newSpeedIndex = Array.IndexOf(availableSpeeds, speedMultiplier);
            if (newSpeedIndex >= 0)
            {
                currentSpeedIndex = newSpeedIndex;
                currentSpeed = speedMultiplier;
                UpdateSpeedDisplay();
                ApplySpeedToBattleManager();

                Log($"戦闘速度設定: {speedMultiplier}倍速");
            }
            else
            {
                LogWarning($"無効な速度値: {speedMultiplier}");
            }
        }
        catch (Exception e)
        {
            LogError($"戦闘速度設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 一時停止状態を設定
    /// </summary>
    /// <param name="paused">一時停止するか</param>
    public void SetPauseState(bool paused)
    {
        try
        {
            isPaused = paused;
            UpdatePauseDisplay();
            ApplyPauseToBattleManager();

            Log($"一時停止状態設定: {(paused ? "一時停止" : "再開")}");
        }
        catch (Exception e)
        {
            LogError($"一時停止状態設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 次の速度に切り替え
    /// </summary>
    public void CycleToNextSpeed()
    {
        try
        {
            currentSpeedIndex = (currentSpeedIndex + 1) % availableSpeeds.Length;
            currentSpeed = availableSpeeds[currentSpeedIndex];

            UpdateSpeedDisplay();
            ApplySpeedToBattleManager();

            // アニメーション再生
            PlaySpeedChangeAnimation();

            Log($"速度切替: {speedDisplayTexts[currentSpeedIndex]}");
        }
        catch (Exception e)
        {
            LogError($"速度切替エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 一時停止切替
    /// </summary>
    public void TogglePause()
    {
        try
        {
            isPaused = !isPaused;
            UpdatePauseDisplay();
            ApplyPauseToBattleManager();

            // ボタンアニメーション再生
            PlayButtonPressAnimation(pauseButton);

            Log($"一時停止切替: {(isPaused ? "一時停止" : "再開")}");
        }
        catch (Exception e)
        {
            LogError($"一時停止切替エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - 状態取得

    /// <summary>
    /// 現在の戦闘速度を取得
    /// </summary>
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    /// <summary>
    /// 現在の一時停止状態を取得
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }

    /// <summary>
    /// 現在の速度表示テキストを取得
    /// </summary>
    public string GetCurrentSpeedText()
    {
        if (currentSpeedIndex >= 0 && currentSpeedIndex < speedDisplayTexts.Length)
            return speedDisplayTexts[currentSpeedIndex];
        return "1x";
    }

    #endregion

    #region 内部メソッド - UI更新

    /// <summary>
    /// 速度表示の更新
    /// </summary>
    private void UpdateSpeedDisplay()
    {
        try
        {
            // 速度ボタンテキスト更新
            if (speedButtonText != null && currentSpeedIndex < speedDisplayTexts.Length)
            {
                speedButtonText.text = speedDisplayTexts[currentSpeedIndex];
            }

            // 速度表示パネル更新
            if (currentSpeedText != null && currentSpeedIndex < speedDisplayTexts.Length)
            {
                currentSpeedText.text = $"速度: {speedDisplayTexts[currentSpeedIndex]}";
            }

            // 速度インジケーター更新
            if (speedIndicatorSlider != null)
            {
                speedIndicatorSlider.value = currentSpeedIndex;
            }

            // ボタン色更新
            if (speedButtonText != null && currentSpeedIndex < speedColors.Length)
            {
                speedButtonText.color = speedColors[currentSpeedIndex];
            }

            // アイコン更新
            if (speedButtonIcon != null && speedIcons != null && currentSpeedIndex < speedIcons.Length)
            {
                speedButtonIcon.sprite = speedIcons[currentSpeedIndex];
            }
        }
        catch (Exception e)
        {
            LogError($"速度表示更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 一時停止表示の更新
    /// </summary>
    private void UpdatePauseDisplay()
    {
        try
        {
            // 一時停止ボタンテキスト更新
            if (pauseButtonText != null)
            {
                pauseButtonText.text = isPaused ? "再開" : "一時停止";
            }

            // 一時停止アイコン更新
            if (pauseButtonIcon != null)
            {
                pauseButtonIcon.sprite = isPaused ? playIcon : pauseIcon;
            }

            // 一時停止インジケーター表示
            if (pauseIndicator != null)
            {
                pauseIndicator.SetActive(isPaused);
            }

            // 速度制御ボタンの有効/無効
            if (speedToggleButton != null)
            {
                speedToggleButton.interactable = !isPaused;
            }
        }
        catch (Exception e)
        {
            LogError($"一時停止表示更新エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - BattleManager連携

    /// <summary>
    /// BattleManagerに速度を適用
    /// </summary>
    private void ApplySpeedToBattleManager()
    {
        try
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.SetBattleSpeed(currentSpeed);
                OnSpeedChanged?.Invoke(currentSpeed);
            }
            else
            {
                LogWarning("BattleManagerが見つかりません");
            }
        }
        catch (Exception e)
        {
            LogError($"BattleManager速度適用エラー: {e.Message}");
        }
    }

    /// <summary>
    /// BattleManagerに一時停止を適用
    /// </summary>
    private void ApplyPauseToBattleManager()
    {
        try
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.SetBattlePause(isPaused);
                OnPauseStateChanged?.Invoke(isPaused);
            }
            else
            {
                LogWarning("BattleManagerが見つかりません");
            }
        }
        catch (Exception e)
        {
            LogError($"BattleManager一時停止適用エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - 設定保存/読み込み

    /// <summary>
    /// 設定保存
    /// </summary>
    private void SaveCurrentSettings()
    {
        if (!saveSpeedSettings) return;

        try
        {
            PlayerPrefs.SetInt(speedPrefsKey, currentSpeedIndex);
            PlayerPrefs.SetInt(pausePrefsKey, isPaused ? 1 : 0);
            PlayerPrefs.Save();

            Log("戦闘速度設定を保存しました");
        }
        catch (Exception e)
        {
            LogError($"設定保存エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 保存済み設定読み込み
    /// </summary>
    private void LoadSavedSettings()
    {
        if (!saveSpeedSettings) return;

        try
        {
            // 速度設定読み込み
            if (PlayerPrefs.HasKey(speedPrefsKey))
            {
                int savedSpeedIndex = PlayerPrefs.GetInt(speedPrefsKey, 0);
                if (savedSpeedIndex >= 0 && savedSpeedIndex < availableSpeeds.Length)
                {
                    currentSpeedIndex = savedSpeedIndex;
                    currentSpeed = availableSpeeds[currentSpeedIndex];
                }
            }

            // 一時停止設定読み込み（戦闘開始時は常に非一時停止）
            isPaused = false;

            Log($"保存済み設定読み込み: 速度={speedDisplayTexts[currentSpeedIndex]}");
        }
        catch (Exception e)
        {
            LogError($"設定読み込みエラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - アニメーション

    /// <summary>
    /// 速度変更アニメーション
    /// </summary>
    private void PlaySpeedChangeAnimation()
    {
        if (speedChangeCoroutine != null)
            StopCoroutine(speedChangeCoroutine);

        speedChangeCoroutine = StartCoroutine(SpeedChangeAnimationCoroutine());
    }

    /// <summary>
    /// 速度変更アニメーションコルーチン
    /// </summary>
    private System.Collections.IEnumerator SpeedChangeAnimationCoroutine()
    {
        Transform speedButtonTransform = speedToggleButton?.transform;
        if (speedButtonTransform == null) yield break;

        Vector3 originalScale = speedButtonTransform.localScale;
        Vector3 targetScale = originalScale * 1.2f;

        float elapsed = 0f;

        // スケールアップ
        while (elapsed < speedChangeAnimationDuration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (speedChangeAnimationDuration * 0.5f);
            float curveValue = buttonScaleEasing.Evaluate(t);
            speedButtonTransform.localScale = Vector3.Lerp(originalScale, targetScale, curveValue);
            yield return null;
        }

        // スケールダウン
        elapsed = 0f;
        while (elapsed < speedChangeAnimationDuration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (speedChangeAnimationDuration * 0.5f);
            float curveValue = buttonScaleEasing.Evaluate(t);
            speedButtonTransform.localScale = Vector3.Lerp(targetScale, originalScale, curveValue);
            yield return null;
        }

        speedButtonTransform.localScale = originalScale;
        speedChangeCoroutine = null;
    }

    /// <summary>
    /// ボタン押下アニメーション
    /// </summary>
    private void PlayButtonPressAnimation(Button button)
    {
        if (button == null) return;

        if (buttonAnimationCoroutine != null)
            StopCoroutine(buttonAnimationCoroutine);

        buttonAnimationCoroutine = StartCoroutine(ButtonPressAnimationCoroutine(button.transform));
    }

    /// <summary>
    /// ボタン押下アニメーションコルーチン
    /// </summary>
    private System.Collections.IEnumerator ButtonPressAnimationCoroutine(Transform buttonTransform)
    {
        Vector3 originalScale = buttonTransform.localScale;
        Vector3 pressedScale = originalScale * 0.9f;

        float elapsed = 0f;

        // スケールダウン
        while (elapsed < buttonAnimationDuration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (buttonAnimationDuration * 0.5f);
            buttonTransform.localScale = Vector3.Lerp(originalScale, pressedScale, t);
            yield return null;
        }

        // スケールアップ
        elapsed = 0f;
        while (elapsed < buttonAnimationDuration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (buttonAnimationDuration * 0.5f);
            buttonTransform.localScale = Vector3.Lerp(pressedScale, originalScale, t);
            yield return null;
        }

        buttonTransform.localScale = originalScale;
        buttonAnimationCoroutine = null;
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// 速度切替ボタンクリック
    /// </summary>
    private void OnSpeedToggleClicked()
    {
        try
        {
            PlayButtonPressAnimation(speedToggleButton);
            CycleToNextSpeed();
            SaveCurrentSettings();
        }
        catch (Exception e)
        {
            LogError($"速度切替ボタンクリックエラー: {e.Message}");
        }
    }

    /// <summary>
    /// 一時停止ボタンクリック
    /// </summary>
    private void OnPauseToggleClicked()
    {
        try
        {
            TogglePause();
            SaveCurrentSettings();
        }
        catch (Exception e)
        {
            LogError($"一時停止ボタンクリックエラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - 外部連携

    /// <summary>
    /// 戦闘開始時の初期化
    /// </summary>
    public void OnBattleStart()
    {
        try
        {
            // 一時停止状態をリセット
            isPaused = false;
            UpdatePauseDisplay();

            // 現在の設定をBattleManagerに適用
            ApplySpeedToBattleManager();
            ApplyPauseToBattleManager();

            Log("戦闘開始 - 速度制御UI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"戦闘開始処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘終了時の処理
    /// </summary>
    public void OnBattleEnd()
    {
        try
        {
            // 設定保存
            SaveCurrentSettings();

            // 一時停止状態をリセット
            isPaused = false;
            UpdatePauseDisplay();

            Log("戦闘終了 - 速度制御UI終了処理完了");
        }
        catch (Exception e)
        {
            LogError($"戦闘終了処理エラー: {e.Message}");
        }
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        Debug.Log($"[BattleSpeedUI] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[BattleSpeedUI] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[BattleSpeedUI] {message}");
    }

    #endregion

    #region エディタ用ツール

#if UNITY_EDITOR
    [ContextMenu("速度切替テスト")]
    private void TestSpeedCycle()
    {
        CycleToNextSpeed();
        Log($"速度切替テスト実行: {GetCurrentSpeedText()}");
    }

    [ContextMenu("一時停止切替テスト")]
    private void TestPauseToggle()
    {
        TogglePause();
        Log($"一時停止切替テスト実行: {(isPaused ? "一時停止" : "再開")}");
    }

    [ContextMenu("設定保存テスト")]
    private void TestSaveSettings()
    {
        SaveCurrentSettings();
        Log("設定保存テスト実行");
    }

    [ContextMenu("設定読み込みテスト")]
    private void TestLoadSettings()
    {
        LoadSavedSettings();
        UpdateSpeedDisplay();
        UpdatePauseDisplay();
        Log("設定読み込みテスト実行");
    }

    [ContextMenu("現在の設定表示")]
    private void ShowCurrentSettings()
    {
        Log($"現在の設定 - 速度: {GetCurrentSpeedText()}, 一時停止: {isPaused}");
    }
#endif

    #endregion
}