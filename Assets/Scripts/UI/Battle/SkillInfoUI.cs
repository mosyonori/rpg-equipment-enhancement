using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// スキル情報詳細表示UI制御
/// 役割：戦闘中のスキル詳細情報の表示・更新制御
/// 機能：スキル名・威力・CT・効果詳細・使用可能性の表示
/// データアクセス統一ルール: UI層 → Manager層 → データ層
/// </summary>
public class SkillInfoUI : MonoBehaviour
{
    [Header("スキル情報パネル")]
    [SerializeField] private GameObject skillInfoPanel;
    [SerializeField] private CanvasGroup skillInfoCanvasGroup;

    [Header("基本情報表示")]
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillTypeText;
    [SerializeField] private TextMeshProUGUI skillAttributeText;
    [SerializeField] private Image skillAttributeIcon;

    [Header("威力・コスト表示")]
    [SerializeField] private TextMeshProUGUI skillPowerText;
    [SerializeField] private TextMeshProUGUI skillHpCostText;
    [SerializeField] private TextMeshProUGUI skillMpCostText;
    [SerializeField] private GameObject hpCostObject;
    [SerializeField] private GameObject mpCostObject;

    [Header("クールタイム表示")]
    [SerializeField] private TextMeshProUGUI currentCoolTimeText;
    [SerializeField] private TextMeshProUGUI maxCoolTimeText;
    [SerializeField] private Slider coolTimeSlider;
    [SerializeField] private Image coolTimeProgressImage;
    [SerializeField] private TextMeshProUGUI coolTimeStatusText;

    [Header("ターゲット・範囲表示")]
    [SerializeField] private TextMeshProUGUI targetTypeText;
    [SerializeField] private TextMeshProUGUI skillRangeText;

    [Header("スキル効果表示")]
    [SerializeField] private TextMeshProUGUI skillDescriptionText;
    [SerializeField] private TextMeshProUGUI effectChanceText;
    [SerializeField] private TextMeshProUGUI effectDurationText;
    [SerializeField] private GameObject effectInfoObject;

    [Header("使用可能性表示")]
    [SerializeField] private GameObject usableIndicator;
    [SerializeField] private GameObject unusableIndicator;
    [SerializeField] private TextMeshProUGUI usabilityReasonText;
    [SerializeField] private Image skillAvailabilityBackground;

    [Header("色設定")]
    [SerializeField] private Color usableColor = Color.green;
    [SerializeField] private Color unusableColor = Color.red;
    [SerializeField] private Color cooldownColor = Color.yellow;
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color disabledTextColor = Color.gray;

    [Header("アニメーション設定")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private AnimationCurve fadeEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // イベント
    public static event Action<string> OnSkillInfoRequested;

    // 内部状態
    private bool isInitialized = false;
    private bool isVisible = false;
    private string currentDisplayedSkillId = "";
    private BattleSkillData currentSkillData;
    private Coroutine fadeCoroutine;

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
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
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
            Log("SkillInfoUI初期化開始");

            // 初期状態設定
            if (skillInfoPanel != null)
                skillInfoPanel.SetActive(false);

            if (skillInfoCanvasGroup != null)
            {
                skillInfoCanvasGroup.alpha = 0f;
                skillInfoCanvasGroup.interactable = false;
                skillInfoCanvasGroup.blocksRaycasts = false;
            }

            // クールタイムスライダー初期化
            if (coolTimeSlider != null)
            {
                coolTimeSlider.minValue = 0f;
                coolTimeSlider.maxValue = 1f;
                coolTimeSlider.value = 0f;
                coolTimeSlider.interactable = false;
            }

            // 初期表示クリア
            ClearSkillInfo();

            isVisible = false;
            currentDisplayedSkillId = "";
            currentSkillData = null;

            isInitialized = true;
            Log("SkillInfoUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"SkillInfoUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// コンポーネント検証
    /// </summary>
    private void ValidateComponents()
    {
        if (skillInfoPanel == null)
            LogWarning("skillInfoPanelが設定されていません");

        if (skillNameText == null)
            LogWarning("skillNameTextが設定されていません");

        if (coolTimeSlider == null)
            LogWarning("coolTimeSliderが設定されていません");
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
            Log("戦闘開始 - スキル情報UI初期化");

            // 表示状態をリセット
            HideSkillInfo();
            ClearSkillInfo();

            Log("スキル情報UI準備完了");
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
            // 現在表示中のスキル情報がある場合は更新
            if (isVisible && !string.IsNullOrEmpty(currentDisplayedSkillId))
            {
                RefreshCurrentSkillInfo();
            }
        }
        catch (Exception e)
        {
            LogError($"ターン開始処理エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - スキル情報表示

    /// <summary>
    /// スキル情報を表示
    /// </summary>
    public void ShowSkillInfo(string skillId, BattleSkillData skillData)
    {
        if (!isInitialized)
        {
            LogWarning("未初期化状態でのスキル情報表示要求");
            return;
        }

        if (string.IsNullOrEmpty(skillId) || skillData == null)
        {
            LogWarning("無効なスキルデータでの表示要求");
            HideSkillInfo();
            return;
        }

        try
        {
            Log($"スキル情報表示: {skillData.skillName}");

            currentDisplayedSkillId = skillId;
            currentSkillData = skillData;

            // スキル情報を更新
            UpdateSkillInfo(skillData);

            // パネルを表示
            ShowPanel();
        }
        catch (Exception e)
        {
            LogError($"スキル情報表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// スキル情報を非表示
    /// </summary>
    public void HideSkillInfo()
    {
        try
        {
            Log("スキル情報非表示");

            currentDisplayedSkillId = "";
            currentSkillData = null;

            // パネルを非表示
            HidePanel();
        }
        catch (Exception e)
        {
            LogError($"スキル情報非表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 現在表示中のスキル情報を更新
    /// </summary>
    public void RefreshCurrentSkillInfo()
    {
        if (!isVisible || string.IsNullOrEmpty(currentDisplayedSkillId))
            return;

        try
        {
            // BattleManagerから最新のスキル情報を取得
            if (BattleManager.Instance != null)
            {
                var allCharacters = BattleManager.Instance.GetAllCharacters();
                var character = allCharacters.Find(c => c.characterId == currentDisplayedSkillId);

                if (character != null && currentSkillData != null)
                {
                    var updatedSkillData = character.availableSkills.Find(s => s.skillId == currentSkillData.skillId);
                    if (updatedSkillData != null)
                    {
                        currentSkillData = updatedSkillData;
                        UpdateSkillInfo(updatedSkillData);
                    }
                }
            }
        }
        catch (Exception e)
        {
            LogError($"スキル情報更新エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - 表示更新

    /// <summary>
    /// スキル情報の詳細更新
    /// </summary>
    private void UpdateSkillInfo(BattleSkillData skillData)
    {
        if (skillData == null) return;

        try
        {
            // 基本情報更新
            UpdateBasicInfo(skillData);

            // 威力・コスト更新
            UpdatePowerAndCost(skillData);

            // クールタイム更新
            UpdateCoolTimeInfo(skillData);

            // ターゲット・範囲更新
            UpdateTargetInfo(skillData);

            // スキル効果更新
            UpdateEffectInfo(skillData);

            // 使用可能性更新
            UpdateUsabilityInfo(skillData);

            Log($"スキル情報更新完了: {skillData.skillName}");
        }
        catch (Exception e)
        {
            LogError($"スキル情報更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 基本情報更新
    /// </summary>
    private void UpdateBasicInfo(BattleSkillData skillData)
    {
        if (skillNameText != null)
            skillNameText.text = skillData.skillName;

        if (skillTypeText != null)
            skillTypeText.text = GetSkillTypeText(skillData.skillType);

        if (skillAttributeText != null)
            skillAttributeText.text = GetAttributeText(skillData.attributeType);

        // 属性アイコンの色設定（将来的に実装）
        if (skillAttributeIcon != null)
        {
            skillAttributeIcon.color = GetAttributeColor(skillData.attributeType);
        }
    }

    /// <summary>
    /// 威力・コスト更新
    /// </summary>
    private void UpdatePowerAndCost(BattleSkillData skillData)
    {
        // 威力表示
        if (skillPowerText != null)
        {
            if (skillData.damageMultiplier > 0f)
            {
                skillPowerText.text = $"威力: {skillData.damageMultiplier:F1}倍";
                skillPowerText.color = normalTextColor;
            }
            else
            {
                skillPowerText.text = "威力: なし";
                skillPowerText.color = disabledTextColor;
            }
        }

        // HP消費表示
        if (hpCostObject != null && skillHpCostText != null)
        {
            if (skillData.hpCost > 0)
            {
                hpCostObject.SetActive(true);
                skillHpCostText.text = $"HP: {skillData.hpCost}";
            }
            else
            {
                hpCostObject.SetActive(false);
            }
        }

        // MP消費表示
        if (mpCostObject != null && skillMpCostText != null)
        {
            if (skillData.mpCost > 0)
            {
                mpCostObject.SetActive(true);
                skillMpCostText.text = $"MP: {skillData.mpCost}";
            }
            else
            {
                mpCostObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// クールタイム情報更新
    /// </summary>
    private void UpdateCoolTimeInfo(BattleSkillData skillData)
    {
        if (currentCoolTimeText != null)
            currentCoolTimeText.text = skillData.currentCoolTime.ToString();

        if (maxCoolTimeText != null)
            maxCoolTimeText.text = skillData.maxCoolTime.ToString();

        // クールタイムスライダー更新
        if (coolTimeSlider != null)
        {
            if (skillData.maxCoolTime > 0)
            {
                float progress = 1f - ((float)skillData.currentCoolTime / skillData.maxCoolTime);
                coolTimeSlider.value = progress;

                // プログレスバーの色設定
                if (coolTimeProgressImage != null)
                {
                    if (skillData.currentCoolTime <= 0)
                        coolTimeProgressImage.color = usableColor;
                    else
                        coolTimeProgressImage.color = cooldownColor;
                }
            }
            else
            {
                coolTimeSlider.value = 1f;
                if (coolTimeProgressImage != null)
                    coolTimeProgressImage.color = usableColor;
            }
        }

        // クールタイム状態テキスト
        if (coolTimeStatusText != null)
        {
            if (skillData.currentCoolTime <= 0)
            {
                coolTimeStatusText.text = "使用可能";
                coolTimeStatusText.color = usableColor;
            }
            else
            {
                coolTimeStatusText.text = $"CT中（残り{skillData.currentCoolTime}ターン）";
                coolTimeStatusText.color = cooldownColor;
            }
        }
    }

    /// <summary>
    /// ターゲット・範囲情報更新
    /// </summary>
    private void UpdateTargetInfo(BattleSkillData skillData)
    {
        if (targetTypeText != null)
            targetTypeText.text = GetTargetTypeText(skillData.targetType);

        if (skillRangeText != null)
        {
            // ターゲットタイプに基づく範囲説明
            skillRangeText.text = GetRangeDescription(skillData.targetType);
        }
    }

    /// <summary>
    /// スキル効果情報更新
    /// </summary>
    private void UpdateEffectInfo(BattleSkillData skillData)
    {
        // スキルマスターデータから効果説明を取得
        var skillMaster = MasterDataManager.Instance?.GetSkillData(skillData.skillId);

        if (skillDescriptionText != null)
        {
            if (skillMaster != null)
            {
                skillDescriptionText.text = SkillUtility.GetSkillEffectDescription(skillMaster);
            }
            else
            {
                skillDescriptionText.text = "効果情報を取得できません";
            }
        }

        // 効果発動率表示
        if (effectChanceText != null && effectInfoObject != null)
        {
            if (skillData.statusEffectChance > 0)
            {
                effectInfoObject.SetActive(true);
                effectChanceText.text = $"発動率: {skillData.statusEffectChance}%";
            }
            else
            {
                effectInfoObject.SetActive(false);
            }
        }

        // 効果継続時間（将来的にBattleSkillDataに追加予定）
        if (effectDurationText != null && skillMaster != null)
        {
            if (skillMaster.skillEffectDuration > 0)
            {
                effectDurationText.text = $"継続: {skillMaster.skillEffectDuration}ターン";
            }
            else
            {
                effectDurationText.text = "";
            }
        }
    }

    /// <summary>
    /// 使用可能性情報更新
    /// </summary>
    private void UpdateUsabilityInfo(BattleSkillData skillData)
    {
        // 現在のプレイヤー状態を取得
        int currentHp = 100; // デフォルト値
        int currentMp = 100; // デフォルト値

        // BattleManagerからプレイヤーキャラクター情報を取得
        if (BattleManager.Instance != null)
        {
            var playerCharacter = BattleManager.Instance.GetPlayerCharacter();
            if (playerCharacter != null)
            {
                currentHp = playerCharacter.currentHp;
                currentMp = playerCharacter.currentMp;
            }
        }

        // スキルマスターデータから使用可能性判定
        var skillMaster = MasterDataManager.Instance?.GetSkillData(skillData.skillId);
        if (skillMaster != null)
        {
            var usageResult = SkillUtility.CanUseSkill(
                null, // UserSkillDataは戦闘中は不要
                skillMaster,
                currentHp,
                currentMp,
                skillData.currentCoolTime
            );

            UpdateUsabilityDisplay(usageResult.canUse, usageResult.message);
        }
        else
        {
            UpdateUsabilityDisplay(false, "スキル情報が見つかりません");
        }
    }

    /// <summary>
    /// 使用可能性表示更新
    /// </summary>
    private void UpdateUsabilityDisplay(bool canUse, string reason)
    {
        if (usableIndicator != null)
            usableIndicator.SetActive(canUse);

        if (unusableIndicator != null)
            unusableIndicator.SetActive(!canUse);

        if (usabilityReasonText != null)
        {
            usabilityReasonText.text = reason;
            usabilityReasonText.color = canUse ? usableColor : unusableColor;
        }

        if (skillAvailabilityBackground != null)
        {
            skillAvailabilityBackground.color = canUse ? usableColor : unusableColor;
        }
    }

    #endregion

    #region 内部メソッド - パネル表示制御

    /// <summary>
    /// パネル表示
    /// </summary>
    private void ShowPanel()
    {
        if (isVisible) return;

        try
        {
            if (skillInfoPanel != null)
                skillInfoPanel.SetActive(true);

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeInCoroutine());
            isVisible = true;
        }
        catch (Exception e)
        {
            LogError($"パネル表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// パネル非表示
    /// </summary>
    private void HidePanel()
    {
        if (!isVisible) return;

        try
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeOutCoroutine());
            isVisible = false;
        }
        catch (Exception e)
        {
            LogError($"パネル非表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// フェードイン処理
    /// </summary>
    private System.Collections.IEnumerator FadeInCoroutine()
    {
        if (skillInfoCanvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = skillInfoCanvasGroup.alpha;

        skillInfoCanvasGroup.interactable = true;
        skillInfoCanvasGroup.blocksRaycasts = true;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            float curveValue = fadeEasing.Evaluate(t);
            skillInfoCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, curveValue);
            yield return null;
        }

        skillInfoCanvasGroup.alpha = 1f;
        fadeCoroutine = null;
    }

    /// <summary>
    /// フェードアウト処理
    /// </summary>
    private System.Collections.IEnumerator FadeOutCoroutine()
    {
        if (skillInfoCanvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = skillInfoCanvasGroup.alpha;

        skillInfoCanvasGroup.interactable = false;
        skillInfoCanvasGroup.blocksRaycasts = false;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            float curveValue = fadeEasing.Evaluate(t);
            skillInfoCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, curveValue);
            yield return null;
        }

        skillInfoCanvasGroup.alpha = 0f;

        if (skillInfoPanel != null)
            skillInfoPanel.SetActive(false);

        fadeCoroutine = null;
    }

    #endregion

    #region 内部メソッド - ユーティリティ

    /// <summary>
    /// スキル情報表示をクリア
    /// </summary>
    private void ClearSkillInfo()
    {
        if (skillNameText != null)
            skillNameText.text = "";

        if (skillDescriptionText != null)
            skillDescriptionText.text = "";

        if (coolTimeSlider != null)
            coolTimeSlider.value = 0f;

        if (usableIndicator != null)
            usableIndicator.SetActive(false);

        if (unusableIndicator != null)
            unusableIndicator.SetActive(false);
    }

    /// <summary>
    /// スキルタイプテキスト取得
    /// </summary>
    private string GetSkillTypeText(SkillType skillType)
    {
        return skillType switch
        {
            SkillType.Attack => "攻撃",
            SkillType.Heal => "回復",
            SkillType.Buff => "バフ",
            SkillType.Debuff => "デバフ",
            SkillType.Support => "補助",
            _ => "不明"
        };
    }

    /// <summary>
    /// 属性テキスト取得
    /// </summary>
    private string GetAttributeText(AttributeType attributeType)
    {
        return attributeType switch
        {
            AttributeType.Fire => "火",
            AttributeType.Water => "水",
            AttributeType.Wind => "風",
            AttributeType.Earth => "土",
            AttributeType.None => "無",
            _ => "不明"
        };
    }

    /// <summary>
    /// 属性色取得
    /// </summary>
    private Color GetAttributeColor(AttributeType attributeType)
    {
        return attributeType switch
        {
            AttributeType.Fire => Color.red,
            AttributeType.Water => Color.blue,
            AttributeType.Wind => Color.green,
            AttributeType.Earth => new Color(0.6f, 0.4f, 0.2f), // 茶色
            AttributeType.None => Color.gray,
            _ => Color.white
        };
    }

    /// <summary>
    /// ターゲットタイプテキスト取得
    /// </summary>
    private string GetTargetTypeText(TargetType targetType)
    {
        return targetType switch
        {
            TargetType.Self => "自分",
            TargetType.EnemySingle => "敵単体",
            TargetType.EnemyAll => "敵全体",
            TargetType.AllySingle => "味方単体",
            TargetType.AllyAll => "味方全体",
            TargetType.Random => "ランダム",
            _ => "不明"
        };
    }

    /// <summary>
    /// 範囲説明取得
    /// </summary>
    private string GetRangeDescription(TargetType targetType)
    {
        return targetType switch
        {
            TargetType.Self => "自分のみに効果",
            TargetType.EnemySingle => "敵1体を対象",
            TargetType.EnemyAll => "敵全体を対象",
            TargetType.AllySingle => "味方1体を対象",
            TargetType.AllyAll => "味方全体を対象",
            TargetType.Random => "ランダム対象",
            _ => "対象範囲不明"
        };
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        Debug.Log($"[SkillInfoUI] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[SkillInfoUI] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SkillInfoUI] {message}");
    }

    #endregion

    #region エディタ用ツール

#if UNITY_EDITOR
    [ContextMenu("スキル情報表示テスト")]
    private void TestShowSkillInfo()
    {
        // テスト用のスキルデータを作成
        var testSkillData = new BattleSkillData
        {
            skillId = 1,
            skillName = "テストスキル",
            skillType = SkillType.Attack,
            attributeType = AttributeType.Fire,
            currentCoolTime = 2,
            maxCoolTime = 5,
            isUsable = false,
            damageMultiplier = 1.5f,
            targetType = TargetType.EnemySingle,
            statusEffectChance = 30,
            hpCost = 10,
            mpCost = 5
        };

        ShowSkillInfo("test_character", testSkillData);
        Log("スキル情報表示テスト実行");
    }

    [ContextMenu("スキル情報非表示テスト")]
    private void TestHideSkillInfo()
    {
        HideSkillInfo();
        Log("スキル情報非表示テスト実行");
    }
#endif

    #endregion
}