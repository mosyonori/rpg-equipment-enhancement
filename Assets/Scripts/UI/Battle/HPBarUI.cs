using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HPバーの表示・アニメーション制御
/// 役割：HP減少・回復アニメーション、危険域（赤）表示、HP数値表示
/// データアクセス統一ルール: UI層 → Manager層 → データ層
/// </summary>
public class HPBarUI : MonoBehaviour
{
    [Header("HPバー基本UI")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private Image hpBackgroundImage;
    [SerializeField] private TextMeshProUGUI hpValueText;
    [SerializeField] private TextMeshProUGUI hpRatioText;

    [Header("色設定")]
    [SerializeField] private Color hpNormalColor = new Color(0.2f, 0.8f, 0.2f, 1f);    // 緑
    [SerializeField] private Color hpWarningColor = new Color(1f, 0.8f, 0f, 1f);       // 黄
    [SerializeField] private Color hpDangerColor = new Color(0.8f, 0.2f, 0.2f, 1f);    // 赤
    [SerializeField] private Color hpCriticalColor = new Color(1f, 0f, 0f, 1f);        // 真紅
    [SerializeField] private Color hpBackgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f); // グレー

    [Header("危険域しきい値")]
    [SerializeField][Range(0f, 1f)] private float warningThreshold = 0.5f;  // 警告しきい値
    [SerializeField][Range(0f, 1f)] private float dangerThreshold = 0.25f;  // 危険しきい値
    [SerializeField][Range(0f, 1f)] private float criticalThreshold = 0.1f; // 致命的しきい値

    [Header("アニメーション設定")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool enableSmoothAnimation = true;
    [SerializeField] private float colorTransitionDuration = 0.3f;

    [Header("ダメージエフェクト")]
    [SerializeField] private bool enableDamageFlash = true;
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private int damageFlashCount = 2;

    [Header("回復エフェクト")]
    [SerializeField] private bool enableHealGlow = true;
    [SerializeField] private Color healGlowColor = Color.green;
    [SerializeField] private float healGlowDuration = 0.4f;
    [SerializeField] private float healGlowIntensity = 1.5f;

    [Header("危険域エフェクト")]
    [SerializeField] private bool enableCriticalPulse = true;
    [SerializeField] private float criticalPulseSpeed = 2f;
    [SerializeField] private float criticalPulseIntensity = 0.3f;

    [Header("テキスト表示設定")]
    [SerializeField] private bool showHPValue = true;
    [SerializeField] private bool showHPRatio = true;
    [SerializeField] private string hpValueFormat = "{0}/{1}";
    [SerializeField] private string hpRatioFormat = "{0:P0}";

    // イベント
    public static event Action<float> OnHPRatioChanged;
    public static event Action OnHPCritical;
    public static event Action OnHPRecovered;

    // 内部状態
    private bool isInitialized = false;
    private float currentHPRatio = 1f;
    private float targetHPRatio = 1f;
    private int currentHP = 0;
    private int maxHP = 100;
    private Color currentHPColor;
    private Coroutine hpAnimationCoroutine;
    private Coroutine colorAnimationCoroutine;
    private Coroutine damageEffectCoroutine;
    private Coroutine healEffectCoroutine;
    private Coroutine criticalPulseCoroutine;
    private bool wasCritical = false;

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateComponents();
        InitializeHPBar();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// HPバー初期化
    /// </summary>
    public void Initialize()
    {
        try
        {
            Log("HPBarUI初期化開始");
            InitializeHPBar();
            isInitialized = true;
            Log("HPBarUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"HPBarUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// コンポーネント検証
    /// </summary>
    private void ValidateComponents()
    {
        if (hpSlider == null)
            LogWarning("hpSliderが設定されていません");

        if (hpFillImage == null && hpSlider != null)
        {
            hpFillImage = hpSlider.fillRect?.GetComponent<Image>();
            if (hpFillImage == null)
                LogWarning("hpFillImageが見つかりません");
        }
    }

    /// <summary>
    /// HPバー基本設定
    /// </summary>
    private void InitializeHPBar()
    {
        try
        {
            // スライダー設定
            if (hpSlider != null)
            {
                hpSlider.minValue = 0f;
                hpSlider.maxValue = 1f;
                hpSlider.value = 1f;
                hpSlider.interactable = false; // ユーザー操作無効
            }

            // 初期色設定
            currentHPColor = hpNormalColor;
            if (hpFillImage != null)
                hpFillImage.color = currentHPColor;

            if (hpBackgroundImage != null)
                hpBackgroundImage.color = hpBackgroundColor;

            // 初期値設定
            currentHPRatio = 1f;
            targetHPRatio = 1f;
            UpdateHPText();

            Log("HPバー基本設定完了");
        }
        catch (Exception e)
        {
            LogError($"HPバー初期化エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - HP更新

    /// <summary>
    /// HP値とMaxHP設定
    /// </summary>
    /// <param name="newCurrentHP">現在HP</param>
    /// <param name="newMaxHP">最大HP</param>
    /// <param name="playAnimation">アニメーション再生するか</param>
    public void SetHP(int newCurrentHP, int newMaxHP, bool playAnimation = true)
    {
        try
        {
            int oldHP = currentHP;
            currentHP = Mathf.Max(0, newCurrentHP);
            maxHP = Mathf.Max(1, newMaxHP);

            float newRatio = (float)currentHP / maxHP;
            SetHPRatio(newRatio, playAnimation);

            // ダメージ・回復判定
            if (oldHP > currentHP && currentHP > 0)
            {
                PlayDamageEffect();
            }
            else if (oldHP < currentHP)
            {
                PlayHealEffect();
                OnHPRecovered?.Invoke();
            }

            Log($"HP設定: {currentHP}/{maxHP} (比率: {newRatio:P1})");
        }
        catch (Exception e)
        {
            LogError($"HP設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// HP比率設定（0.0～1.0）
    /// </summary>
    /// <param name="ratio">HP比率</param>
    /// <param name="playAnimation">アニメーション再生するか</param>
    public void SetHPRatio(float ratio, bool playAnimation = true)
    {
        try
        {
            float oldRatio = currentHPRatio;
            targetHPRatio = Mathf.Clamp01(ratio);

            // 危険域チェック
            CheckCriticalState(oldRatio, targetHPRatio);

            if (playAnimation && enableSmoothAnimation)
            {
                StartHPAnimation();
            }
            else
            {
                currentHPRatio = targetHPRatio;
                ApplyHPRatio();
            }

            // イベント発行
            OnHPRatioChanged?.Invoke(targetHPRatio);
        }
        catch (Exception e)
        {
            LogError($"HP比率設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// HP増減（相対値）
    /// </summary>
    /// <param name="changeAmount">変化量（正=回復、負=ダメージ）</param>
    /// <param name="playAnimation">アニメーション再生するか</param>
    public void ChangeHP(int changeAmount, bool playAnimation = true)
    {
        int newHP = currentHP + changeAmount;
        SetHP(newHP, maxHP, playAnimation);
    }

    /// <summary>
    /// HP比率増減（相対値）
    /// </summary>
    /// <param name="changeRatio">変化比率</param>
    /// <param name="playAnimation">アニメーション再生するか</param>
    public void ChangeHPRatio(float changeRatio, bool playAnimation = true)
    {
        float newRatio = currentHPRatio + changeRatio;
        SetHPRatio(newRatio, playAnimation);
    }

    #endregion

    #region 公開メソッド - 状態取得

    /// <summary>
    /// 現在のHP比率取得
    /// </summary>
    public float GetCurrentHPRatio()
    {
        return currentHPRatio;
    }

    /// <summary>
    /// 現在のHP値取得
    /// </summary>
    public int GetCurrentHP()
    {
        return currentHP;
    }

    /// <summary>
    /// 最大HP取得
    /// </summary>
    public int GetMaxHP()
    {
        return maxHP;
    }

    /// <summary>
    /// 危険状態かどうか
    /// </summary>
    public bool IsCritical()
    {
        return currentHPRatio <= criticalThreshold;
    }

    /// <summary>
    /// 危険域状態かどうか
    /// </summary>
    public bool IsDanger()
    {
        return currentHPRatio <= dangerThreshold;
    }

    #endregion

    #region 内部メソッド - アニメーション

    /// <summary>
    /// HPアニメーション開始
    /// </summary>
    private void StartHPAnimation()
    {
        if (hpAnimationCoroutine != null)
            StopCoroutine(hpAnimationCoroutine);

        hpAnimationCoroutine = StartCoroutine(HPAnimationCoroutine());
    }

    /// <summary>
    /// HPアニメーションコルーチン
    /// </summary>
    private IEnumerator HPAnimationCoroutine()
    {
        float startRatio = currentHPRatio;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float curveValue = animationCurve.Evaluate(t);

            currentHPRatio = Mathf.Lerp(startRatio, targetHPRatio, curveValue);
            ApplyHPRatio();

            yield return null;
        }

        currentHPRatio = targetHPRatio;
        ApplyHPRatio();
        hpAnimationCoroutine = null;
    }

    /// <summary>
    /// HP比率をUIに適用
    /// </summary>
    private void ApplyHPRatio()
    {
        // スライダー値更新
        if (hpSlider != null)
            hpSlider.value = currentHPRatio;

        // 色更新
        UpdateHPColor();

        // テキスト更新
        UpdateHPText();
    }

    /// <summary>
    /// HP色更新
    /// </summary>
    private void UpdateHPColor()
    {
        Color targetColor = GetHPColor(currentHPRatio);

        if (colorAnimationCoroutine != null)
            StopCoroutine(colorAnimationCoroutine);

        colorAnimationCoroutine = StartCoroutine(ColorTransitionCoroutine(targetColor));
    }

    /// <summary>
    /// 色遷移コルーチン
    /// </summary>
    private IEnumerator ColorTransitionCoroutine(Color targetColor)
    {
        Color startColor = currentHPColor;
        float elapsed = 0f;

        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / colorTransitionDuration;

            currentHPColor = Color.Lerp(startColor, targetColor, t);
            if (hpFillImage != null)
                hpFillImage.color = currentHPColor;

            yield return null;
        }

        currentHPColor = targetColor;
        if (hpFillImage != null)
            hpFillImage.color = currentHPColor;

        colorAnimationCoroutine = null;
    }

    /// <summary>
    /// HP比率に応じた色取得
    /// </summary>
    private Color GetHPColor(float ratio)
    {
        if (ratio <= criticalThreshold)
            return hpCriticalColor;
        else if (ratio <= dangerThreshold)
            return hpDangerColor;
        else if (ratio <= warningThreshold)
            return hpWarningColor;
        else
            return hpNormalColor;
    }

    #endregion

    #region 内部メソッド - エフェクト

    /// <summary>
    /// ダメージエフェクト再生
    /// </summary>
    private void PlayDamageEffect()
    {
        if (!enableDamageFlash || hpFillImage == null) return;

        if (damageEffectCoroutine != null)
            StopCoroutine(damageEffectCoroutine);

        damageEffectCoroutine = StartCoroutine(DamageFlashCoroutine());
    }

    /// <summary>
    /// ダメージフラッシュコルーチン
    /// </summary>
    private IEnumerator DamageFlashCoroutine()
    {
        Color originalColor = hpFillImage.color;

        for (int i = 0; i < damageFlashCount; i++)
        {
            // 白フラッシュ
            hpFillImage.color = damageFlashColor;
            yield return new WaitForSeconds(damageFlashDuration * 0.3f);

            // 元の色
            hpFillImage.color = originalColor;
            yield return new WaitForSeconds(damageFlashDuration * 0.7f);
        }

        damageEffectCoroutine = null;
    }

    /// <summary>
    /// 回復エフェクト再生
    /// </summary>
    private void PlayHealEffect()
    {
        if (!enableHealGlow || hpFillImage == null) return;

        if (healEffectCoroutine != null)
            StopCoroutine(healEffectCoroutine);

        healEffectCoroutine = StartCoroutine(HealGlowCoroutine());
    }

    /// <summary>
    /// 回復グローコルーチン
    /// </summary>
    private IEnumerator HealGlowCoroutine()
    {
        Color originalColor = hpFillImage.color;
        Color glowColor = healGlowColor * healGlowIntensity;

        float elapsed = 0f;
        while (elapsed < healGlowDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / healGlowDuration;
            float intensity = Mathf.Sin(t * Mathf.PI);

            hpFillImage.color = Color.Lerp(originalColor, glowColor, intensity * 0.5f);
            yield return null;
        }

        hpFillImage.color = originalColor;
        healEffectCoroutine = null;
    }

    /// <summary>
    /// 危険域状態チェック
    /// </summary>
    private void CheckCriticalState(float oldRatio, float newRatio)
    {
        bool nowCritical = newRatio <= criticalThreshold;

        // 危険域に入った
        if (!wasCritical && nowCritical)
        {
            OnHPCritical?.Invoke();
            if (enableCriticalPulse)
                StartCriticalPulse();
        }
        // 危険域から脱出
        else if (wasCritical && !nowCritical)
        {
            StopCriticalPulse();
        }

        wasCritical = nowCritical;
    }

    /// <summary>
    /// 危険域パルス開始
    /// </summary>
    private void StartCriticalPulse()
    {
        if (criticalPulseCoroutine != null)
            StopCoroutine(criticalPulseCoroutine);

        criticalPulseCoroutine = StartCoroutine(CriticalPulseCoroutine());
    }

    /// <summary>
    /// 危険域パルス停止
    /// </summary>
    private void StopCriticalPulse()
    {
        if (criticalPulseCoroutine != null)
        {
            StopCoroutine(criticalPulseCoroutine);
            criticalPulseCoroutine = null;
        }
    }

    /// <summary>
    /// 危険域パルスコルーチン
    /// </summary>
    private IEnumerator CriticalPulseCoroutine()
    {
        while (currentHPRatio <= criticalThreshold)
        {
            float pulse = Mathf.Sin(Time.time * criticalPulseSpeed) * criticalPulseIntensity;
            Color pulseColor = Color.Lerp(hpCriticalColor, Color.white, Mathf.Abs(pulse));

            if (hpFillImage != null)
                hpFillImage.color = pulseColor;

            yield return null;
        }

        criticalPulseCoroutine = null;
    }

    #endregion

    #region 内部メソッド - テキスト表示

    /// <summary>
    /// HPテキスト更新
    /// </summary>
    private void UpdateHPText()
    {
        try
        {
            // HP数値表示
            if (showHPValue && hpValueText != null)
            {
                hpValueText.text = string.Format(hpValueFormat, currentHP, maxHP);
            }

            // HP比率表示
            if (showHPRatio && hpRatioText != null)
            {
                hpRatioText.text = string.Format(hpRatioFormat, currentHPRatio);
            }
        }
        catch (Exception e)
        {
            LogError($"HPテキスト更新エラー: {e.Message}");
        }
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        Debug.Log($"[HPBarUI] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[HPBarUI] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[HPBarUI] {message}");
    }

    #endregion

    #region エディタ用ツール

#if UNITY_EDITOR
    [ContextMenu("HPテストアニメーション（50%）")]
    private void TestHP50()
    {
        SetHPRatio(0.5f, true);
        Log("HPテスト: 50%に設定");
    }

    [ContextMenu("HPテストアニメーション（25%）")]
    private void TestHP25()
    {
        SetHPRatio(0.25f, true);
        Log("HPテスト: 25%に設定（危険域）");
    }

    [ContextMenu("HPテストアニメーション（10%）")]
    private void TestHP10()
    {
        SetHPRatio(0.1f, true);
        Log("HPテスト: 10%に設定（危機的）");
    }

    [ContextMenu("HPテストアニメーション（回復）")]
    private void TestHPRecover()
    {
        SetHPRatio(1.0f, true);
        Log("HPテスト: 100%に回復");
    }

    [ContextMenu("ダメージエフェクトテスト")]
    private void TestDamageEffect()
    {
        PlayDamageEffect();
        Log("ダメージエフェクトテスト実行");
    }

    [ContextMenu("回復エフェクトテスト")]
    private void TestHealEffect()
    {
        PlayHealEffect();
        Log("回復エフェクトテスト実行");
    }
#endif

    #endregion
}