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

    [Header("状態異常表示")]
    [SerializeField] private Transform statusEffectParent;
    [SerializeField] private GameObject statusEffectIconPrefab;

    [Header("スキル表示")]
    [SerializeField] private Transform skillListParent;
    [SerializeField] private GameObject skillSlotPrefab;

    [Header("アニメーション設定")]
    [SerializeField] private float hpAnimationDuration = 0.3f;
    [SerializeField] private float damageShakeStrength = 10f;
    [SerializeField] private float damageShakeDuration = 0.2f;

    [Header("デバッグ設定")]
    [SerializeField] private bool enableDebugLog = true;

    // イベント
    public static event Action<string> OnSkillInfoRequested;

    // 内部状態
    private bool isInitialized = false;
    private BattleCharacterData currentPlayerData;
    private float targetHpRatio = 1f;
    private Coroutine hpAnimationCoroutine;

    // インスタンス管理用リスト（プレハブエラー対処）
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

            // 状態異常エリアクリア
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
    /// 修正: 戦闘開始後の処理 - 基本情報のみ設定
    /// </summary>
    public void OnBattleStart(BattleSetupData setupData)
    {
        if (!isInitialized) Initialize();

        try
        {
            Log("戦闘開始 - プレイヤーUI初期化");

            // 修正: BattleSetupDataから基本情報を設定
            if (characterNameText != null)
                characterNameText.text = setupData.playerName;

            if (characterLevelText != null)
                characterLevelText.text = $"Lv.{setupData.playerLevel}";

            Log($"プレイヤー基本情報設定完了: {setupData.playerName} Lv.{setupData.playerLevel}");

            // 修正: 実際のBattleCharacterDataはUpdatePlayerData()で後から受け取る
        }
        catch (Exception e)
        {
            LogError($"戦闘開始処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ターン開始後の処理
    /// </summary>
    public void OnTurnStart(BattleCharacterData character)
    {
        if (character == null) return;

        try
        {
            if (character.isPlayer)
            {
                // プレイヤーのターン開始
                SetTurnOrderActive(true);

                // 修正: データが更新されている可能性があるため、最新データで更新
                if (currentPlayerData != null && character.characterId == currentPlayerData.characterId)
                {
                    currentPlayerData = character;
                    UpdateAllPlayerInfo();
                }

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
    /// 行動実行後の処理
    /// </summary>
    public void OnActionExecuted(ActionData action)
    {
        if (action == null || currentPlayerData == null) return;

        try
        {
            // ダメージを受けた場合の処理
            foreach (var damage in action.damageResults)
            {
                if (damage.targetId == currentPlayerData.characterId)
                {
                    // 修正: BattleManagerから最新のプレイヤーデータを取得
                    RefreshPlayerData();

                    // ダメージエフェクト表示
                    if (damage.finalDamage > 0)
                    {
                        PlayDamageEffect();
                    }

                    Log($"プレイヤーダメージ: {damage.finalDamage} (残りHP: {currentPlayerData.currentHp})");
                    break;
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
    /// 修正: プレイヤーデータ設定（BattleUIから呼び出される）
    /// </summary>
    public void UpdatePlayerData(BattleCharacterData playerData)
    {
        if (playerData == null)
        {
            LogError("UpdatePlayerData: playerDataがnullです");
            return;
        }

        if (!playerData.isPlayer)
        {
            LogError($"UpdatePlayerData: プレイヤーではないデータが渡されました: {playerData.characterName}");
            return;
        }

        try
        {
            Log($"プレイヤーデータ更新開始: {playerData.characterName}");

            currentPlayerData = playerData;
            UpdateAllPlayerInfo();

            Log($"プレイヤーデータ更新完了: {playerData.characterName} (HP: {playerData.currentHp}/{playerData.maxHp})");
        }
        catch (Exception e)
        {
            LogError($"プレイヤーデータ更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: 現在のプレイヤーデータ取得
    /// </summary>
    public BattleCharacterData GetCurrentPlayerData()
    {
        return currentPlayerData;
    }

    /// <summary>
    /// 修正: プレイヤーが生存しているかチェック
    /// </summary>
    public bool IsPlayerAlive()
    {
        return currentPlayerData != null && currentPlayerData.isAlive && currentPlayerData.currentHp > 0;
    }

    #endregion

    #region 内部メソッド - 表示更新

    /// <summary>
    /// プレイヤー情報全体更新
    /// </summary>
    private void UpdateAllPlayerInfo()
    {
        if (currentPlayerData == null) return;

        try
        {
            UpdateBasicInfo();
            UpdateHPDisplay();
            UpdateStatusEffectDisplay();
            UpdateSkillDisplay();

            Log($"プレイヤー情報全体更新完了: {currentPlayerData.characterName}");
        }
        catch (Exception e)
        {
            LogError($"プレイヤー情報全体更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: 基本情報更新
    /// </summary>
    private void UpdateBasicInfo()
    {
        if (currentPlayerData == null) return;

        try
        {
            // 名前とレベルを更新（すでにOnBattleStartで設定済みだが、最新情報で上書き）
            if (characterNameText != null)
                characterNameText.text = currentPlayerData.characterName;

            if (characterLevelText != null)
                characterLevelText.text = $"Lv.{currentPlayerData.characterLevel}";

            // キャラクター画像設定（スプライトがあれば）
            if (characterImage != null && currentPlayerData.characterSprite != null)
                characterImage.sprite = currentPlayerData.characterSprite;

            Log($"基本情報更新: {currentPlayerData.characterName} Lv.{currentPlayerData.characterLevel}");
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

            Log($"HP表示更新: {currentPlayerData.currentHp}/{currentPlayerData.maxHp} ({newHpRatio:F2})");
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
    /// 状態異常表示更新
    /// </summary>
    private void UpdateStatusEffectDisplay()
    {
        if (currentPlayerData == null || statusEffectParent == null) return;

        try
        {
            // 既存の状態異常アイコンをクリア
            ClearStatusEffects();

            // 現在の状態異常を表示
            if (currentPlayerData.statusEffects != null)
            {
                foreach (var effect in currentPlayerData.statusEffects)
                {
                    if (effect.IsActive())
                    {
                        CreateStatusEffectIcon(effect);
                    }
                }

                Log($"状態異常表示更新: {currentPlayerData.statusEffects.Count}個");
            }
        }
        catch (Exception e)
        {
            LogError($"状態異常表示更新エラー: {e.Message}");
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
            if (currentPlayerData.availableSkills != null)
            {
                foreach (var skill in currentPlayerData.availableSkills)
                {
                    CreateSkillSlot(skill);
                }

                Log($"スキル表示更新: {currentPlayerData.availableSkills.Count}個");
            }
        }
        catch (Exception e)
        {
            LogError($"スキル表示更新エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - データ同期

    /// <summary>
    /// 修正: BattleManagerから最新のプレイヤーデータを取得
    /// </summary>
    private void RefreshPlayerData()
    {
        if (currentPlayerData == null) return;

        try
        {
            if (BattleManager.Instance != null)
            {
                var playerData = BattleManager.Instance.GetPlayerCharacter();
                if (playerData != null && playerData.characterId == currentPlayerData.characterId)
                {
                    currentPlayerData = playerData;
                    UpdateAllPlayerInfo();
                    Log($"プレイヤーデータ同期完了: {playerData.characterName}");
                }
            }
        }
        catch (Exception e)
        {
            LogError($"プレイヤーデータ同期エラー: {e.Message}");
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

            Log($"行動順位表示設定: {(isActive ? "アクティブ" : "非アクティブ")}");
        }
        catch (Exception e)
        {
            LogError($"行動順位表示設定エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - UI要素生成

    /// <summary>
    /// 状態異常アイコン生成
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

            Log($"状態異常アイコン生成: {effect.effectName}");
        }
        catch (Exception e)
        {
            LogError($"状態異常アイコン生成エラー: {e.Message}");
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

            Log($"スキルスロット生成: {skill.skillName}");
        }
        catch (Exception e)
        {
            LogError($"スキルスロット生成エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 状態異常クリア（プレハブエラー対処版）
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

            Log("状態異常クリア完了");
        }
        catch (Exception e)
        {
            LogError($"状態異常クリアエラー: {e.Message}");
        }
    }

    /// <summary>
    /// スキルリストクリア（プレハブエラー対処版）
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

            Log("スキルリストクリア完了");
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
        if (enableDebugLog)
        {
            Debug.Log($"[PlayerBattleUI] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogWarning($"[PlayerBattleUI] {message}");
        }
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

    [ContextMenu("現在の状態を表示")]
    private void ShowCurrentStatus()
    {
        Log($"=== PlayerBattleUI現在の状態 ===");
        Log($"初期化済み: {isInitialized}");
        Log($"プレイヤーデータ: {(currentPlayerData != null ? currentPlayerData.characterName : "null")}");
        if (currentPlayerData != null)
        {
            Log($"  HP: {currentPlayerData.currentHp}/{currentPlayerData.maxHp}");
            Log($"  レベル: {currentPlayerData.characterLevel}");
            Log($"  生存: {currentPlayerData.isAlive}");
        }
        Log($"状態異常インスタンス数: {statusEffectInstances.Count}");
        Log($"スキルインスタンス数: {skillSlotInstances.Count}");
    }
#endif

    #endregion
}