using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 単体モンスターの戦闘UI制御
/// 役割：モンスター画像・名前表示、HPバー・状態表示、撃破時のアニメーション
/// データアクセス統一ルール: UI層 → Manager層 → データ層
/// </summary>
public class MonsterBattleUI : MonoBehaviour
{
    [Header("モンスター基本情報")]
    [SerializeField] private Image monsterImage;
    [SerializeField] private TextMeshProUGUI monsterNameText;
    [SerializeField] private Button monsterButton;

    [Header("HPバー")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Color hpNormalColor = Color.green;
    [SerializeField] private Color hpWarningColor = Color.yellow;
    [SerializeField] private Color hpDangerColor = Color.red;

    [Header("行動順位表示")]
    [SerializeField] private GameObject turnOrderIndicator;
    [SerializeField] private TextMeshProUGUI turnOrderText;
    [SerializeField] private Image turnOrderBackground;
    [SerializeField] private Color activeTurnColor = Color.yellow;
    [SerializeField] private Color inactiveTurnColor = Color.gray;

    [Header("状態効果表示")]
    [SerializeField] private Transform statusEffectParent;
    [SerializeField] private GameObject statusEffectIconPrefab;

    [Header("撃破演出")]
    [SerializeField] private CanvasGroup monsterCanvasGroup;
    [SerializeField] private GameObject defeatEffectPrefab;
    [SerializeField] private float defeatAnimationDuration = 1.0f;
    [SerializeField] private AnimationCurve defeatFadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("ダメージエフェクト")]
    [SerializeField] private float damageShakeStrength = 5f;
    [SerializeField] private float damageShakeDuration = 0.3f;
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private Color damageFlashColor = Color.red;

    [Header("アニメーション設定")]
    [SerializeField] private float hpAnimationDuration = 0.3f;
    [SerializeField] private float turnIndicatorScale = 1.2f;

    // イベント
    public static event Action<string> OnMonsterSelected;
    public static event Action<string> OnMonsterDefeated;

    // 内部状態
    private bool isInitialized = false;
    private BattleCharacterData currentMonsterData;
    private float targetHpRatio = 1f;
    private Coroutine hpAnimationCoroutine;
    private bool isDefeated = false;

    // インスタンス管理用リスト（プレハブエラー対策）
    private List<GameObject> statusEffectInstances = new List<GameObject>();

    // HPバー危険度しきい値
    private const float HP_WARNING_THRESHOLD = 0.5f;
    private const float HP_DANGER_THRESHOLD = 0.25f;

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateComponents();
        SetupEventListeners();
    }

    private void OnDestroy()
    {
        CleanupEventListeners();
        if (hpAnimationCoroutine != null)
        {
            StopCoroutine(hpAnimationCoroutine);
        }
    }

    #endregion

    #region 初期化

    /// <summary>
    /// UIコンポーネントの初期化
    /// </summary>
    public void Initialize()
    {
        if (!Application.isPlaying)
        {
            Log("エディタモードのため初期化をスキップ");
            return;
        }

        try
        {
            Log("MonsterBattleUI初期化開始");

            // 初期状態設定
            if (hpSlider != null)
            {
                hpSlider.value = 1f;
                hpSlider.maxValue = 1f;
                hpSlider.minValue = 0f;
            }

            // 行動順位表示初期化
            SetTurnOrderActive(false);

            // 状態効果エリアクリア
            ClearStatusEffects();

            // 撃破状態リセット
            isDefeated = false;
            if (monsterCanvasGroup != null)
            {
                monsterCanvasGroup.alpha = 1f;
            }

            // ボタン有効化
            if (monsterButton != null)
            {
                monsterButton.interactable = true;
            }

            isInitialized = true;
            Log("MonsterBattleUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"MonsterBattleUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// コンポーネント検証
    /// </summary>
    private void ValidateComponents()
    {
        if (monsterNameText == null)
            LogWarning("monsterNameTextが設定されていません");

        if (hpSlider == null)
            LogWarning("hpSliderが設定されていません");

        if (hpText == null)
            LogWarning("hpTextが設定されていません");

        if (monsterButton == null)
            LogWarning("monsterButtonが設定されていません");
    }

    /// <summary>
    /// イベントリスナー設定
    /// </summary>
    private void SetupEventListeners()
    {
        if (monsterButton != null)
        {
            monsterButton.onClick.AddListener(OnMonsterButtonClicked);
        }
    }

    /// <summary>
    /// イベントリスナークリーンアップ
    /// </summary>
    private void CleanupEventListeners()
    {
        if (monsterButton != null)
        {
            monsterButton.onClick.RemoveListener(OnMonsterButtonClicked);
        }
    }

    #endregion

    #region 公開メソッド - イベントハンドラ

    /// <summary>
    /// モンスターデータ設定
    /// </summary>
    public void SetMonsterData(BattleCharacterData monsterData)
    {
        if (monsterData == null || monsterData.isPlayer) return;

        try
        {
            currentMonsterData = monsterData;
            UpdateAllMonsterInfo();
            Log($"モンスターデータ設定: {monsterData.characterName}");
        }
        catch (Exception e)
        {
            LogError($"モンスターデータ設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ターン開始時の処理
    /// </summary>
    public void OnTurnStart(BattleCharacterData character)
    {
        if (currentMonsterData == null) return;

        try
        {
            bool isMyTurn = character.characterId == currentMonsterData.characterId;
            SetTurnOrderActive(isMyTurn);

            if (isMyTurn)
            {
                Log($"モンスターターン開始: {character.characterName}");
                StartCoroutine(ActiveMonsterEffect());
            }
        }
        catch (Exception e)
        {
            LogError($"ターン開始処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 行動実行時の処理
    /// </summary>
    public void OnActionExecuted(ActionData action)
    {
        if (currentMonsterData == null) return;

        try
        {
            // ダメージを受けた場合の処理
            foreach (var damage in action.damageResults)
            {
                if (damage.targetId == currentMonsterData.characterId)
                {
                    // HPバー更新
                    UpdateHPDisplay();

                    // ダメージエフェクト表示
                    if (damage.finalDamage > 0)
                    {
                        StartCoroutine(PlayDamageEffect());
                    }

                    // 撃破チェック
                    if (damage.targetDefeated)
                    {
                        StartCoroutine(PlayDefeatAnimation());
                    }

                    Log($"モンスターダメージ: {damage.finalDamage}");
                }
            }
        }
        catch (Exception e)
        {
            LogError($"行動実行処理エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - データ更新

    /// <summary>
    /// モンスターデータ更新
    /// </summary>
    public void UpdateMonsterData()
    {
        if (currentMonsterData == null) return;

        try
        {
            UpdateAllMonsterInfo();
            Log($"モンスターデータ更新: {currentMonsterData.characterName}");
        }
        catch (Exception e)
        {
            LogError($"モンスターデータ更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 撃破状態設定
    /// </summary>
    public void SetDefeated(bool defeated)
    {
        isDefeated = defeated;
        if (monsterButton != null)
        {
            monsterButton.interactable = !defeated;
        }
    }

    #endregion

    #region 内部メソッド - 表示更新

    /// <summary>
    /// モンスター情報全体更新
    /// </summary>
    private void UpdateAllMonsterInfo()
    {
        if (currentMonsterData == null) return;

        UpdateBasicInfo();
        UpdateHPDisplay();
        UpdateStatusEffectDisplay();
    }

    /// <summary>
    /// 基本情報更新
    /// </summary>
    private void UpdateBasicInfo()
    {
        if (currentMonsterData == null) return;

        try
        {
            if (monsterNameText != null)
                monsterNameText.text = currentMonsterData.characterName;

            // モンスター画像設定（スプライトがあれば）
            if (monsterImage != null && currentMonsterData.characterSprite != null)
                monsterImage.sprite = currentMonsterData.characterSprite;
        }
        catch (Exception e)
        {
            LogError($"基本情報更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// HP表示更新
    /// </summary>
    private void UpdateHPDisplay()
    {
        if (currentMonsterData == null) return;

        try
        {
            float newHpRatio = currentMonsterData.GetHpRatio();

            // HPテキスト更新
            if (hpText != null)
                hpText.text = $"{currentMonsterData.currentHp}/{currentMonsterData.maxHp}";

            // HPバー色更新
            UpdateHPBarColor(newHpRatio);

            // HPバーアニメーション
            AnimateHPBar(newHpRatio);
        }
        catch (Exception e)
        {
            LogError($"HP表示更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// HPバー色更新
    /// </summary>
    private void UpdateHPBarColor(float hpRatio)
    {
        if (hpFillImage == null) return;

        Color targetColor;
        if (hpRatio <= HP_DANGER_THRESHOLD)
            targetColor = hpDangerColor;
        else if (hpRatio <= HP_WARNING_THRESHOLD)
            targetColor = hpWarningColor;
        else
            targetColor = hpNormalColor;

        hpFillImage.color = targetColor;
    }

    /// <summary>
    /// HPバーアニメーション
    /// </summary>
    private void AnimateHPBar(float targetRatio)
    {
        if (hpSlider == null) return;

        targetHpRatio = targetRatio;

        if (hpAnimationCoroutine != null)
            StopCoroutine(hpAnimationCoroutine);

        hpAnimationCoroutine = StartCoroutine(HPAnimationCoroutine());
    }

    /// <summary>
    /// HPアニメーションコルーチン
    /// </summary>
    private IEnumerator HPAnimationCoroutine()
    {
        float startValue = hpSlider.value;
        float elapsed = 0f;

        while (elapsed < hpAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / hpAnimationDuration;
            hpSlider.value = Mathf.Lerp(startValue, targetHpRatio, t);
            yield return null;
        }

        hpSlider.value = targetHpRatio;
        hpAnimationCoroutine = null;
    }

    /// <summary>
    /// 状態効果表示更新
    /// </summary>
    private void UpdateStatusEffectDisplay()
    {
        if (currentMonsterData == null || statusEffectParent == null) return;

        try
        {
            // 既存の状態効果アイコンをクリア
            ClearStatusEffects();

            // 現在の状態効果を表示
            foreach (var effect in currentMonsterData.statusEffects)
            {
                if (effect.IsActive())
                {
                    CreateStatusEffectIcon(effect);
                }
            }
        }
        catch (Exception e)
        {
            LogError($"状態効果表示更新エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - エフェクト・アニメーション

    /// <summary>
    /// ダメージエフェクト再生
    /// </summary>
    private IEnumerator PlayDamageEffect()
    {
        // 振動エフェクト
        yield return StartCoroutine(DamageShakeEffect());

        // フラッシュエフェクト
        yield return StartCoroutine(DamageFlashEffect());
    }

    /// <summary>
    /// ダメージ振動エフェクト
    /// </summary>
    private IEnumerator DamageShakeEffect()
    {
        Vector3 originalPosition = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < damageShakeDuration)
        {
            elapsed += Time.deltaTime;
            float intensity = Mathf.Lerp(damageShakeStrength, 0f, elapsed / damageShakeDuration);

            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-intensity, intensity),
                UnityEngine.Random.Range(-intensity, intensity),
                0f
            );

            transform.localPosition = originalPosition + randomOffset;
            yield return null;
        }

        transform.localPosition = originalPosition;
    }

    /// <summary>
    /// ダメージフラッシュエフェクト
    /// </summary>
    private IEnumerator DamageFlashEffect()
    {
        if (monsterImage == null) yield break;

        Color originalColor = monsterImage.color;
        float elapsed = 0f;

        while (elapsed < damageFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / damageFlashDuration;
            monsterImage.color = Color.Lerp(damageFlashColor, originalColor, t);
            yield return null;
        }

        monsterImage.color = originalColor;
    }

    /// <summary>
    /// アクティブモンスターエフェクト
    /// </summary>
    private IEnumerator ActiveMonsterEffect()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * turnIndicatorScale;

        float duration = 0.3f;
        float elapsed = 0f;

        // スケールアップ
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;

        // 少し待つ
        yield return new WaitForSeconds(0.2f);

        // スケールダウン
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// 撃破アニメーション再生
    /// </summary>
    private IEnumerator PlayDefeatAnimation()
    {
        // 撃破エフェクト生成
        if (defeatEffectPrefab != null)
        {
            GameObject effectObj = Instantiate(defeatEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effectObj, defeatAnimationDuration);
        }

        // フェードアウトアニメーション
        yield return StartCoroutine(DefeatFadeOutAnimation());

        // 撃破状態設定
        SetDefeated(true);

        // イベント発行
        if (currentMonsterData != null)
        {
            OnMonsterDefeated?.Invoke(currentMonsterData.characterId);
        }

        Log($"撃破アニメーション完了: {currentMonsterData?.characterName}");
    }

    /// <summary>
    /// 撃破フェードアウトアニメーション
    /// </summary>
    private IEnumerator DefeatFadeOutAnimation()
    {
        if (monsterCanvasGroup == null)
        {
            monsterCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        float elapsed = 0f;
        float startAlpha = monsterCanvasGroup.alpha;

        while (elapsed < defeatAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / defeatAnimationDuration;
            float curveValue = defeatFadeCurve.Evaluate(t);
            monsterCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, curveValue);
            yield return null;
        }

        monsterCanvasGroup.alpha = 0f;
    }

    #endregion

    #region 内部メソッド - 行動順位

    /// <summary>
    /// 行動順位表示設定
    /// </summary>
    private void SetTurnOrderActive(bool isActive)
    {
        try
        {
            if (turnOrderIndicator != null)
                turnOrderIndicator.SetActive(isActive);

            if (turnOrderBackground != null)
                turnOrderBackground.color = isActive ? activeTurnColor : inactiveTurnColor;

            if (turnOrderText != null)
                turnOrderText.text = isActive ? "行動中" : "";
        }
        catch (Exception e)
        {
            LogError($"行動順位表示設定エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - UI要素生成

    /// <summary>
    /// 状態効果アイコン生成
    /// </summary>
    private void CreateStatusEffectIcon(StatusEffectData effect)
    {
        if (statusEffectIconPrefab == null || statusEffectParent == null) return;

        try
        {
            GameObject iconObj = Instantiate(statusEffectIconPrefab, statusEffectParent);
            statusEffectInstances.Add(iconObj);

            // 基本的なテキスト表示のみ実装
            var textComponent = iconObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = effect.remainingTurns.ToString();
            }
        }
        catch (Exception e)
        {
            LogError($"状態効果アイコン生成エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 状態効果クリア（プレハブエラー対策版）
    /// </summary>
    private void ClearStatusEffects()
    {
        if (statusEffectParent == null) return;

        try
        {
            // プレハブモード判定
            bool isPrefabMode = !Application.isPlaying &&
#if UNITY_EDITOR
                UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject);
#else
                false;
#endif

            // インスタンス管理リストからクリア
            foreach (var instance in statusEffectInstances)
            {
                if (instance != null)
                {
                    if (Application.isPlaying && !isPrefabMode)
                    {
                        Destroy(instance);
                    }
                    else
                    {
                        instance.SetActive(false);
                    }
                }
            }
            statusEffectInstances.Clear();

            // 直接の子オブジェクトの処理
            if (Application.isPlaying && !isPrefabMode)
            {
                for (int i = statusEffectParent.childCount - 1; i >= 0; i--)
                {
                    var child = statusEffectParent.GetChild(i);
                    if (child != null && child.gameObject != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
            else
            {
                for (int i = 0; i < statusEffectParent.childCount; i++)
                {
                    var child = statusEffectParent.GetChild(i);
                    if (child != null && child.gameObject != null)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            Log($"状態効果クリア完了 (プレハブモード: {isPrefabMode}, プレイ中: {Application.isPlaying})");
        }
        catch (Exception e)
        {
            LogError($"状態効果クリアエラー: {e.Message}");
        }
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// モンスターボタンクリック処理
    /// </summary>
    private void OnMonsterButtonClicked()
    {
        if (currentMonsterData == null || isDefeated) return;

        try
        {
            OnMonsterSelected?.Invoke(currentMonsterData.characterId);
            Log($"モンスター選択: {currentMonsterData.characterName}");
        }
        catch (Exception e)
        {
            LogError($"モンスターボタンクリックエラー: {e.Message}");
        }
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        Debug.Log($"[MonsterBattleUI] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[MonsterBattleUI] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[MonsterBattleUI] {message}");
    }

    #endregion

    #region エディタ用ツール

#if UNITY_EDITOR
    [ContextMenu("モンスター情報テスト更新")]
    private void TestUpdateMonsterInfo()
    {
        Log("テスト用モンスター情報更新");

        if (currentMonsterData != null)
        {
            UpdateAllMonsterInfo();
        }
        else
        {
            LogWarning("currentMonsterDataがnullです");
        }
    }

    [ContextMenu("撃破アニメーションテスト")]
    private void TestDefeatAnimation()
    {
        Log("撃破アニメーションテスト");
        StartCoroutine(PlayDefeatAnimation());
    }
#endif

    #endregion
}