using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// プレイヤーキャラクターの戦闘UI制御
/// 役割：プレイヤー画像・名前表示、HPバー・ステータス表示、現在の行動順位表示
/// データアクセス統一ルール: UI層 → Manager層 → データ層
/// </summary>
public class PlayerBattleUI : MonoBehaviour
{
    [Header("プレイヤー基本情報")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI characterLevelText;

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

    [Header("スキル表示")]
    [SerializeField] private Transform skillListParent;
    [SerializeField] private GameObject skillSlotPrefab;

    [Header("アニメーション設定")]
    [SerializeField] private float hpAnimationDuration = 0.3f;
    [SerializeField] private float damageShakeStrength = 10f;
    [SerializeField] private float damageShakeDuration = 0.2f;

    // イベント
    public static event Action<string> OnSkillInfoRequested;

    // 内部状態
    private bool isInitialized = false;
    private BattleCharacterData currentPlayerData;
    private float targetHpRatio = 1f;
    private Coroutine hpAnimationCoroutine;

    // インスタンス管理用リスト（プレハブエラー対策）
    private List<GameObject> statusEffectInstances = new List<GameObject>();
    private List<GameObject> skillSlotInstances = new List<GameObject>();

    // HPバー危険度しきい値
    private const float HP_WARNING_THRESHOLD = 0.5f;
    private const float HP_DANGER_THRESHOLD = 0.25f;

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateComponents();
    }

    private void OnDestroy()
    {
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
        try
        {
            Log("PlayerBattleUI初期化開始");

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

            // スキルエリアクリア
            ClearSkillList();

            isInitialized = true;
            Log("PlayerBattleUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"PlayerBattleUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// コンポーネント検証
    /// </summary>
    private void ValidateComponents()
    {
        if (characterNameText == null)
            LogWarning("characterNameTextが設定されていません");

        if (hpSlider == null)
            LogWarning("hpSliderが設定されていません");

        if (hpText == null)
            LogWarning("hpTextが設定されていません");

        if (turnOrderIndicator == null)
            LogWarning("turnOrderIndicatorが設定されていません");
    }

    #endregion

    #region 公開メソッド - イベントハンドラ

    /// <summary>
    /// 戦闘開始時の処理
    /// </summary>
    public void OnBattleStart(BattleSetupData setupData)
    {
        if (!isInitialized) Initialize();

        try
        {
            Log("戦闘開始 - プレイヤーUI初期化");

            // プレイヤー基本情報表示
            if (characterNameText != null)
                characterNameText.text = setupData.playerName;

            if (characterLevelText != null)
                characterLevelText.text = $"Lv.{setupData.playerLevel}";

            Log("プレイヤー戦闘UI準備完了");
        }
        catch (Exception e)
        {
            LogError($"戦闘開始処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ターン開始時の処理
    /// </summary>
    public void OnTurnStart(BattleCharacterData character)
    {
        try
        {
            if (character.isPlayer)
            {
                // プレイヤーのターン開始
                SetTurnOrderActive(true);
                currentPlayerData = character;
                UpdateAllPlayerInfo();
                Log($"プレイヤーターン開始: {character.characterName}");
            }
            else
            {
                // 敵のターン
                SetTurnOrderActive(false);
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
        try
        {
            // ダメージを受けた場合の処理
            foreach (var damage in action.damageResults)
            {
                if (currentPlayerData != null && damage.targetId == currentPlayerData.characterId)
                {
                    // HPバー更新
                    UpdateHPDisplay();

                    // ダメージエフェクト表示
                    if (damage.finalDamage > 0)
                    {
                        PlayDamageEffect();
                    }

                    Log($"プレイヤーダメージ: {damage.finalDamage}");
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
    /// プレイヤーデータ更新
    /// </summary>
    public void UpdatePlayerData(BattleCharacterData playerData)
    {
        if (playerData == null || !playerData.isPlayer) return;

        try
        {
            currentPlayerData = playerData;
            UpdateAllPlayerInfo();
            Log($"プレイヤーデータ更新: {playerData.characterName}");
        }
        catch (Exception e)
        {
            LogError($"プレイヤーデータ更新エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - 表示更新

    /// <summary>
    /// プレイヤー情報全体更新
    /// </summary>
    private void UpdateAllPlayerInfo()
    {
        if (currentPlayerData == null) return;

        UpdateHPDisplay();
        UpdateStatusEffectDisplay();
        UpdateSkillDisplay();
    }

    /// <summary>
    /// HP表示更新
    /// </summary>
    private void UpdateHPDisplay()
    {
        if (currentPlayerData == null) return;

        try
        {
            float newHpRatio = currentPlayerData.GetHpRatio();

            // HPテキスト更新
            if (hpText != null)
                hpText.text = $"{currentPlayerData.currentHp}/{currentPlayerData.maxHp}";

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
        if (currentPlayerData == null || statusEffectParent == null) return;

        try
        {
            // 既存の状態効果アイコンをクリア
            ClearStatusEffects();

            // 現在の状態効果を表示
            foreach (var effect in currentPlayerData.statusEffects)
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

    /// <summary>
    /// スキル表示更新
    /// </summary>
    private void UpdateSkillDisplay()
    {
        if (currentPlayerData == null || skillListParent == null) return;

        try
        {
            // 既存のスキルスロットをクリア
            ClearSkillList();

            // 使用可能スキルを表示
            foreach (var skill in currentPlayerData.availableSkills)
            {
                CreateSkillSlot(skill);
            }
        }
        catch (Exception e)
        {
            LogError($"スキル表示更新エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - エフェクト

    /// <summary>
    /// ダメージエフェクト再生
    /// </summary>
    private void PlayDamageEffect()
    {
        try
        {
            // 画面振動エフェクト
            if (transform != null)
            {
                StartCoroutine(DamageShakeCoroutine());
            }
        }
        catch (Exception e)
        {
            LogError($"ダメージエフェクトエラー: {e.Message}");
        }
    }

    /// <summary>
    /// ダメージ振動コルーチン
    /// </summary>
    private IEnumerator DamageShakeCoroutine()
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

            // 基本的なテキスト表示のみ実装（詳細UIは別途作成予定）
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
    /// スキルスロット生成
    /// </summary>
    private void CreateSkillSlot(BattleSkillData skill)
    {
        if (skillSlotPrefab == null || skillListParent == null) return;

        try
        {
            GameObject slotObj = Instantiate(skillSlotPrefab, skillListParent);
            skillSlotInstances.Add(slotObj);

            // 基本的なテキスト表示のみ実装（詳細UIは別途作成予定）
            var textComponents = slotObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (textComponents.Length > 0)
            {
                textComponents[0].text = skill.skillName;
            }
            if (textComponents.Length > 1)
            {
                textComponents[1].text = skill.currentCoolTime > 0 ?
                    $"CT:{skill.currentCoolTime}" : "使用可能";
            }
        }
        catch (Exception e)
        {
            LogError($"スキルスロット生成エラー: {e.Message}");
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
            // インスタンス管理リストからクリア
            foreach (var instance in statusEffectInstances)
            {
                if (instance != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(instance);
                    }
                    else
                    {
                        // エディタモードでは非表示にする
                        instance.SetActive(false);
                    }
                }
            }
            statusEffectInstances.Clear();

            // 念のため直接の子オブジェクトもチェック
            if (Application.isPlaying)
            {
                for (int i = statusEffectParent.childCount - 1; i >= 0; i--)
                {
                    var child = statusEffectParent.GetChild(i);
                    if (child != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }
        catch (Exception e)
        {
            LogError($"状態効果クリアエラー: {e.Message}");
        }
    }

    /// <summary>
    /// スキルリストクリア（プレハブエラー対策版）
    /// </summary>
    private void ClearSkillList()
    {
        if (skillListParent == null) return;

        try
        {
            // インスタンス管理リストからクリア
            foreach (var instance in skillSlotInstances)
            {
                if (instance != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(instance);
                    }
                    else
                    {
                        // エディタモードでは非表示にする
                        instance.SetActive(false);
                    }
                }
            }
            skillSlotInstances.Clear();

            // 念のため直接の子オブジェクトもチェック
            if (Application.isPlaying)
            {
                for (int i = skillListParent.childCount - 1; i >= 0; i--)
                {
                    var child = skillListParent.GetChild(i);
                    if (child != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }
        catch (Exception e)
        {
            LogError($"スキルリストクリアエラー: {e.Message}");
        }
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// スキル情報要求ハンドラ（将来の拡張用）
    /// </summary>
    private void OnSkillInfoRequestedHandler(string skillName)
    {
        OnSkillInfoRequested?.Invoke(skillName);
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        Debug.Log($"[PlayerBattleUI] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[PlayerBattleUI] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[PlayerBattleUI] {message}");
    }

    #endregion

    #region エディタ用ツール

#if UNITY_EDITOR
    [ContextMenu("プレイヤー情報テスト更新")]
    private void TestUpdatePlayerInfo()
    {
        Log("テスト用プレイヤー情報更新");

        if (currentPlayerData != null)
        {
            UpdateAllPlayerInfo();
        }
        else
        {
            LogWarning("currentPlayerDataがnullです");
        }
    }

    [ContextMenu("HPバーテストアニメーション")]
    private void TestHPBarAnimation()
    {
        float testRatio = UnityEngine.Random.Range(0.1f, 1.0f);
        Log($"HPバーテストアニメーション: {testRatio:F2}");
        AnimateHPBar(testRatio);
    }
#endif

    #endregion
}