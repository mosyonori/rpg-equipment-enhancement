using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HPバーの表示・アニメーション制御
/// 責任範囲：
/// - HP減少・回復アニメーション
/// - HP数値表示（現在HP/最大HP）
/// - HPバーの色変更（残りHP率に応じて）
/// - ダメージ/回復時の視覚的フィードバック
/// データアクセス統一ルール: UI層専用コンポーネント（BattleCharacterDataを受け取り表示のみ）
/// </summary>
public class HPBarUI : MonoBehaviour
{
    #region デバッグ・ログ

    /// <summary>
    /// デバッグログ出力
    /// </summary>
    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[HPBarUI] {message}");
        }
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    private void LogError(string message)
    {
        Debug.LogError($"[HPBarUI] {message}");
    }

    /// <summary>
    /// 警告ログ出力
    /// </summary>
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[HPBarUI] {message}");
    }

    #endregion

    #region フィールド

    [Header("UI設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showHPText = true;
    [SerializeField] private bool showPercentage = true;

    [Header("HPバーコンポーネント")]
    [SerializeField] private Image hpBarFillImage;
    [SerializeField] private Image hpBarBackgroundImage;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("アニメーション設定")]
    [SerializeField] private float hpChangeAnimationDuration = 0.5f;
    [SerializeField] private AnimationCurve hpChangeAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private float healFlashDuration = 0.3f;

    [Header("色設定")]
    [SerializeField] private Color hpFullColor = Color.green;
    [SerializeField] private Color hpMediumColor = Color.yellow;
    [SerializeField] private Color hpLowColor = Color.red;
    [SerializeField] private Color hpCriticalColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private Color healFlashColor = Color.cyan;

    [Header("HP色変更しきい値")]
    [SerializeField] private float mediumHPThreshold = 0.6f;
    [SerializeField] private float lowHPThreshold = 0.3f;
    [SerializeField] private float criticalHPThreshold = 0.1f;

    // 現在の状態
    private int currentHP = 0;
    private int maxHP = 100;
    private float currentHPRatio = 1.0f;
    private bool isInitialized = false;

    // アニメーション制御
    private Coroutine hpAnimationCoroutine;
    private Coroutine flashAnimationCoroutine;
    private bool isAnimating = false;

    // 元の色保存
    private Color originalHPBarColor;
    private Color originalBackgroundColor;

    #endregion

    #region プロパティ

    /// <summary>
    /// 初期化完了状態
    /// </summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// 現在のHP
    /// </summary>
    public int CurrentHP => currentHP;

    /// <summary>
    /// 最大HP
    /// </summary>
    public int MaxHP => maxHP;

    /// <summary>
    /// 現在のHP比率（0.0-1.0）
    /// </summary>
    public float CurrentHPRatio => currentHPRatio;

    /// <summary>
    /// アニメーション実行中フラグ
    /// </summary>
    public bool IsAnimating => isAnimating;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        Log("HPBarUI Awake開始");
        ValidateComponents();
    }

    private void Start()
    {
        Log("HPBarUI Start開始");
        InitializeHPBar();
    }

    private void OnDestroy()
    {
        Log("HPBarUI OnDestroy開始");
        CleanupAnimations();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネント検証
    /// </summary>
    private void ValidateComponents()
    {
        try
        {
            // 必須コンポーネント確認
            if (hpBarFillImage == null)
            {
                LogError("hpBarFillImageが設定されていません");
                return;
            }

            // オプションコンポーネント確認
            if (hpBarBackgroundImage == null)
            {
                LogWarning("hpBarBackgroundImageが設定されていません");
            }

            if (hpText == null && showHPText)
            {
                LogWarning("hpTextが設定されていませんが、showHPTextがtrueです");
            }

            Log("コンポーネント検証完了");
        }
        catch (Exception e)
        {
            LogError($"コンポーネント検証エラー: {e.Message}");
        }
    }

    /// <summary>
    /// HPバー初期化
    /// </summary>
    private void InitializeHPBar()
    {
        try
        {
            Log("HPバー初期化開始");

            // 元の色を保存
            if (hpBarFillImage != null)
            {
                originalHPBarColor = hpBarFillImage.color;
            }

            if (hpBarBackgroundImage != null)
            {
                originalBackgroundColor = hpBarBackgroundImage.color;
            }

            // 初期値設定
            SetHPImmediate(maxHP, maxHP);

            isInitialized = true;
            Log("HPバー初期化完了");
        }
        catch (Exception e)
        {
            LogError($"HPバー初期化エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - HP設定

    /// <summary>
    /// HPを即座に設定（アニメーションなし）
    /// </summary>
    /// <param name="currentHp">現在HP</param>
    /// <param name="maxHp">最大HP</param>
    public void SetHPImmediate(int currentHp, int maxHp)
    {
        try
        {
            if (maxHp <= 0)
            {
                LogError($"無効な最大HP: {maxHp}");
                return;
            }

            this.currentHP = Mathf.Max(0, currentHp);
            this.maxHP = maxHp;
            this.currentHPRatio = (float)this.currentHP / this.maxHP;

            UpdateHPBarDisplay();
            UpdateHPText();
            UpdateHPBarColor();

            Log($"HP即座設定: {this.currentHP}/{this.maxHP} ({this.currentHPRatio:P1})");
        }
        catch (Exception e)
        {
            LogError($"HP即座設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// HPをアニメーション付きで変更
    /// </summary>
    /// <param name="newCurrentHp">新しい現在HP</param>
    /// <param name="newMaxHp">新しい最大HP（省略時は現在の最大HPを使用）</param>
    public void SetHPAnimated(int newCurrentHp, int? newMaxHp = null)
    {
        try
        {
            int targetMaxHp = newMaxHp ?? this.maxHP;

            if (targetMaxHp <= 0)
            {
                LogError($"無効な最大HP: {targetMaxHp}");
                return;
            }

            int targetCurrentHp = Mathf.Max(0, newCurrentHp);

            // アニメーション実行
            if (hpAnimationCoroutine != null)
            {
                StopCoroutine(hpAnimationCoroutine);
            }

            hpAnimationCoroutine = StartCoroutine(AnimateHPChange(targetCurrentHp, targetMaxHp));

            Log($"HPアニメーション開始: {this.currentHP}/{this.maxHP} → {targetCurrentHp}/{targetMaxHp}");
        }
        catch (Exception e)
        {
            LogError($"HPアニメーション設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// BattleCharacterDataからHP設定（アニメーション付き）
    /// </summary>
    /// <param name="character">戦闘キャラクターデータ</param>
    public void UpdateFromCharacterData(BattleCharacterData character)
    {
        if (character == null)
        {
            LogError("BattleCharacterDataがnullです");
            return;
        }

        try
        {
            SetHPAnimated(character.currentHp, character.maxHp);
            Log($"キャラクターデータからHP更新: {character.characterName}");
        }
        catch (Exception e)
        {
            LogError($"キャラクターデータからのHP更新エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - 演出

    /// <summary>
    /// ダメージ受けフラッシュ演出
    /// </summary>
    /// <param name="damageAmount">ダメージ量（演出の強さに影響）</param>
    public void PlayDamageFlash(int damageAmount = 0)
    {
        try
        {
            if (flashAnimationCoroutine != null)
            {
                StopCoroutine(flashAnimationCoroutine);
            }

            flashAnimationCoroutine = StartCoroutine(FlashAnimation(damageFlashColor, damageFlashDuration));
            Log($"ダメージフラッシュ演出再生: {damageAmount}ダメージ");
        }
        catch (Exception e)
        {
            LogError($"ダメージフラッシュ演出エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 回復フラッシュ演出
    /// </summary>
    /// <param name="healAmount">回復量（演出の強さに影響）</param>
    public void PlayHealFlash(int healAmount = 0)
    {
        try
        {
            if (flashAnimationCoroutine != null)
            {
                StopCoroutine(flashAnimationCoroutine);
            }

            flashAnimationCoroutine = StartCoroutine(FlashAnimation(healFlashColor, healFlashDuration));
            Log($"回復フラッシュ演出再生: {healAmount}回復");
        }
        catch (Exception e)
        {
            LogError($"回復フラッシュ演出エラー: {e.Message}");
        }
    }

    /// <summary>
    /// HPバーを強調表示（戦闘中の行動者表示等に使用）
    /// </summary>
    /// <param name="highlight">強調表示するか</param>
    public void SetHighlight(bool highlight)
    {
        try
        {
            if (hpBarBackgroundImage != null)
            {
                Color targetColor = highlight ? Color.white : originalBackgroundColor;
                hpBarBackgroundImage.color = targetColor;
            }

            Log($"HPバー強調表示: {highlight}");
        }
        catch (Exception e)
        {
            LogError($"HPバー強調表示エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - 設定

    /// <summary>
    /// HPテキスト表示切替
    /// </summary>
    /// <param name="show">表示するか</param>
    public void SetHPTextVisible(bool show)
    {
        try
        {
            showHPText = show;

            if (hpText != null)
            {
                hpText.gameObject.SetActive(show);
            }

            Log($"HPテキスト表示切替: {show}");
        }
        catch (Exception e)
        {
            LogError($"HPテキスト表示切替エラー: {e.Message}");
        }
    }

    /// <summary>
    /// パーセンテージ表示切替
    /// </summary>
    /// <param name="show">パーセンテージ表示するか</param>
    public void SetPercentageVisible(bool show)
    {
        try
        {
            showPercentage = show;
            UpdateHPText();

            Log($"パーセンテージ表示切替: {show}");
        }
        catch (Exception e)
        {
            LogError($"パーセンテージ表示切替エラー: {e.Message}");
        }
    }

    /// <summary>
    /// アニメーション速度設定
    /// </summary>
    /// <param name="duration">アニメーション時間（秒）</param>
    public void SetAnimationDuration(float duration)
    {
        try
        {
            hpChangeAnimationDuration = Mathf.Max(0.1f, duration);
            Log($"アニメーション時間設定: {hpChangeAnimationDuration}秒");
        }
        catch (Exception e)
        {
            LogError($"アニメーション時間設定エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - 表示更新

    /// <summary>
    /// HPバー表示更新
    /// </summary>
    private void UpdateHPBarDisplay()
    {
        try
        {
            if (hpBarFillImage != null)
            {
                hpBarFillImage.fillAmount = currentHPRatio;
            }
        }
        catch (Exception e)
        {
            LogError($"HPバー表示更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// HPテキスト更新
    /// </summary>
    private void UpdateHPText()
    {
        try
        {
            if (hpText == null || !showHPText) return;

            string hpTextContent = $"{currentHP}/{maxHP}";

            if (showPercentage)
            {
                hpTextContent += $" ({currentHPRatio:P0})";
            }

            hpText.text = hpTextContent;
        }
        catch (Exception e)
        {
            LogError($"HPテキスト更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// HP比率に応じたバー色更新
    /// </summary>
    private void UpdateHPBarColor()
    {
        try
        {
            if (hpBarFillImage == null) return;

            Color targetColor;

            if (currentHPRatio <= criticalHPThreshold)
            {
                targetColor = hpCriticalColor;
            }
            else if (currentHPRatio <= lowHPThreshold)
            {
                targetColor = hpLowColor;
            }
            else if (currentHPRatio <= mediumHPThreshold)
            {
                targetColor = hpMediumColor;
            }
            else
            {
                targetColor = hpFullColor;
            }

            hpBarFillImage.color = targetColor;
        }
        catch (Exception e)
        {
            LogError($"HPバー色更新エラー: {e.Message}");
        }
    }

    #endregion

    #region アニメーション

    /// <summary>
    /// HP変更アニメーション
    /// </summary>
    private IEnumerator AnimateHPChange(int targetCurrentHp, int targetMaxHp)
    {
        isAnimating = true;

        int startCurrentHp = this.currentHP;
        int startMaxHp = this.maxHP;
        float startRatio = this.currentHPRatio;
        float targetRatio = (float)targetCurrentHp / targetMaxHp;
        float elapsed = 0f;

        while (elapsed < hpChangeAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / hpChangeAnimationDuration;

            // アニメーションカーブを適用
            float curveProgress = hpChangeAnimationCurve.Evaluate(progress);

            // 値を補間
            this.currentHP = Mathf.RoundToInt(Mathf.Lerp(startCurrentHp, targetCurrentHp, curveProgress));
            this.maxHP = Mathf.RoundToInt(Mathf.Lerp(startMaxHp, targetMaxHp, curveProgress));
            this.currentHPRatio = Mathf.Lerp(startRatio, targetRatio, curveProgress);

            try
            {
                UpdateHPBarDisplay();
                UpdateHPText();
                UpdateHPBarColor();
            }
            catch (Exception e)
            {
                LogError($"HPアニメーション更新エラー: {e.Message}");
            }

            yield return null;
        }

        // 最終値を確実に設定
        this.currentHP = targetCurrentHp;
        this.maxHP = targetMaxHp;
        this.currentHPRatio = targetRatio;

        try
        {
            UpdateHPBarDisplay();
            UpdateHPText();
            UpdateHPBarColor();
            Log($"HPアニメーション完了: {this.currentHP}/{this.maxHP}");
        }
        catch (Exception e)
        {
            LogError($"HPアニメーション完了処理エラー: {e.Message}");
        }

        isAnimating = false;
    }

    /// <summary>
    /// フラッシュアニメーション
    /// </summary>
    private IEnumerator FlashAnimation(Color flashColor, float duration)
    {
        if (hpBarFillImage == null) yield break;

        Color originalColor = hpBarFillImage.color;
        float elapsed = 0f;

        // フェードイン（元の色→フラッシュ色）
        while (elapsed < duration * 0.3f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration * 0.3f);

            try
            {
                hpBarFillImage.color = Color.Lerp(originalColor, flashColor, progress);
            }
            catch (Exception e)
            {
                LogError($"フラッシュアニメーション（フェードイン）エラー: {e.Message}");
            }

            yield return null;
        }

        // フェードアウト（フラッシュ色→元の色）
        elapsed = 0f;
        while (elapsed < duration * 0.7f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (duration * 0.7f);

            try
            {
                hpBarFillImage.color = Color.Lerp(flashColor, originalColor, progress);
            }
            catch (Exception e)
            {
                LogError($"フラッシュアニメーション（フェードアウト）エラー: {e.Message}");
            }

            yield return null;
        }

        // 元の色に確実に戻す
        try
        {
            UpdateHPBarColor();
        }
        catch (Exception e)
        {
            LogError($"フラッシュアニメーション（色復元）エラー: {e.Message}");
        }
    }

    #endregion

    #region クリーンアップ

    /// <summary>
    /// アニメーション停止・クリーンアップ
    /// </summary>
    private void CleanupAnimations()
    {
        try
        {
            if (hpAnimationCoroutine != null)
            {
                StopCoroutine(hpAnimationCoroutine);
                hpAnimationCoroutine = null;
            }

            if (flashAnimationCoroutine != null)
            {
                StopCoroutine(flashAnimationCoroutine);
                flashAnimationCoroutine = null;
            }

            isAnimating = false;
            Log("アニメーションクリーンアップ完了");
        }
        catch (Exception e)
        {
            LogError($"アニメーションクリーンアップエラー: {e.Message}");
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
        Log("=== HPBarUI状態情報 ===");
        Log($"初期化完了: {isInitialized}");
        Log($"現在HP: {currentHP}/{maxHP} ({currentHPRatio:P1})");
        Log($"アニメーション実行中: {isAnimating}");
        Log($"HPテキスト表示: {showHPText}");
        Log($"パーセンテージ表示: {showPercentage}");
        Log($"アニメーション時間: {hpChangeAnimationDuration}秒");
        Log("========================");
    }

    /// <summary>
    /// デバッグ用：ダメージテスト
    /// </summary>
    [ContextMenu("デバッグ：ダメージテスト")]
    public void DebugTestDamage()
    {
        int testDamage = UnityEngine.Random.Range(10, 30);
        int newHP = Mathf.Max(0, currentHP - testDamage);

        Log($"デバッグ：ダメージテスト実行 {testDamage}ダメージ");
        SetHPAnimated(newHP);
        PlayDamageFlash(testDamage);
    }

    /// <summary>
    /// デバッグ用：回復テスト
    /// </summary>
    [ContextMenu("デバッグ：回復テスト")]
    public void DebugTestHeal()
    {
        int testHeal = UnityEngine.Random.Range(5, 20);
        int newHP = Mathf.Min(maxHP, currentHP + testHeal);

        Log($"デバッグ：回復テスト実行 {testHeal}回復");
        SetHPAnimated(newHP);
        PlayHealFlash(testHeal);
    }

    /// <summary>
    /// デバッグ用：HPフル回復
    /// </summary>
    [ContextMenu("デバッグ：HPフル回復")]
    public void DebugFullHeal()
    {
        Log("デバッグ：HPフル回復実行");
        SetHPAnimated(maxHP);
        PlayHealFlash(maxHP - currentHP);
    }

    /// <summary>
    /// デバッグ用：強調表示テスト
    /// </summary>
    [ContextMenu("デバッグ：強調表示テスト")]
    public void DebugTestHighlight()
    {
        Log("デバッグ：強調表示テスト実行");
        StartCoroutine(DebugHighlightCoroutine());
    }

    /// <summary>
    /// デバッグ用：強調表示コルーチン
    /// </summary>
    private IEnumerator DebugHighlightCoroutine()
    {
        try
        {
            SetHighlight(true);
        }
        catch (Exception e)
        {
            LogError($"デバッグ強調表示エラー: {e.Message}");
        }

        yield return new WaitForSeconds(1f);

        try
        {
            SetHighlight(false);
        }
        catch (Exception e)
        {
            LogError($"デバッグ強調表示解除エラー: {e.Message}");
        }
    }

    #endregion
}