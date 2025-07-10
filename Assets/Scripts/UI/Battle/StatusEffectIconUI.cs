using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// 個別ステータス効果アイコンUI（統一プレハブ版）
/// バフ・デバフの個別表示、残りターン表示を管理
/// 7種類のスキル効果に対応した統一プレハブ
/// データアクセス統一ルール: UI層 → Manager層 → データ層
/// </summary>
public class StatusEffectIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI要素")]
    [SerializeField] private Image effectIcon;
    [SerializeField] private TextMeshProUGUI turnCountText;
    [SerializeField] private Image effectBackground;
    [SerializeField] private Button iconButton;

    [Header("スキル効果アイコン画像（7種類）")]
    [SerializeField] private Sprite attackUpIcon;      // 攻撃強化
    [SerializeField] private Sprite attackDownIcon;    // 攻撃弱化
    [SerializeField] private Sprite defenseUpIcon;     // 防御強化
    [SerializeField] private Sprite defenseDownIcon;   // 防御弱化
    [SerializeField] private Sprite poisonIcon;        // 毒
    [SerializeField] private Sprite regenIcon;         // 継続回復
    [SerializeField] private Sprite stunIcon;          // 気絶

    [Header("視覚効果")]
    [SerializeField] private ParticleSystem effectParticle;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("色設定")]
    [SerializeField] private Color buffBackgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);     // バフ背景色
    [SerializeField] private Color debuffBackgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);   // デバフ背景色
    [SerializeField] private Color neutralBackgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);  // 中性背景色

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = false;

    // イベント
    public System.Action<StatusEffectData> OnIconHovered;
    public System.Action<StatusEffectData> OnIconUnhovered;
    public System.Action<StatusEffectData> OnIconClicked;

    // プライベートフィールド
    private StatusEffectData currentStatusEffect;
    private bool isInitialized;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponent();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネント初期化
    /// </summary>
    private void InitializeComponent()
    {
        // CanvasGroup初期化
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Button初期化
        if (iconButton == null)
            iconButton = GetComponent<Button>();

        if (iconButton != null)
            iconButton.onClick.AddListener(OnButtonClicked);

        isInitialized = true;
        DebugLog("StatusEffectIconUI初期化完了");
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// ステータス効果データを設定
    /// </summary>
    public void SetStatusEffect(StatusEffectData statusEffect)
    {
        if (statusEffect == null)
        {
            LogError("StatusEffectDataがnullです");
            return;
        }

        currentStatusEffect = statusEffect;
        UpdateIconDisplay();

        DebugLog($"ステータス効果設定: {statusEffect.effectName}");
    }

    /// <summary>
    /// 残りターン数を更新
    /// </summary>
    public void UpdateTurnCount(int remainingTurns)
    {
        if (currentStatusEffect != null)
        {
            currentStatusEffect.remainingTurns = remainingTurns;
            UpdateTurnDisplay();

            // 効果終了チェック
            if (remainingTurns <= 0)
            {
                HideIcon();
            }
        }
    }

    /// <summary>
    /// アイコンを非表示にする
    /// </summary>
    public void HideIcon()
    {
        if (canvasGroup != null)
        {
            StartCoroutine(FadeOut());
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 現在のステータス効果データを取得
    /// </summary>
    public StatusEffectData GetStatusEffect()
    {
        return currentStatusEffect;
    }

    /// <summary>
    /// アイコンが有効かチェック
    /// </summary>
    public bool IsActive()
    {
        return currentStatusEffect != null && currentStatusEffect.IsActive();
    }

    #endregion

    #region UI更新

    /// <summary>
    /// アイコン表示を更新
    /// </summary>
    private void UpdateIconDisplay()
    {
        if (currentStatusEffect == null) return;

        // アイコン画像設定（7種類のスキル効果に対応）
        if (effectIcon != null)
        {
            var sprite = GetEffectSpriteByType(currentStatusEffect.effectType);
            if (sprite != null)
                effectIcon.sprite = sprite;
        }

        // 背景色設定（バフ/デバフで色分け）
        if (effectBackground != null)
        {
            effectBackground.color = GetBackgroundColorByType(currentStatusEffect);
        }

        // ターン数表示更新
        UpdateTurnDisplay();

        // パーティクルエフェクト
        if (effectParticle != null)
        {
            var main = effectParticle.main;
            main.startColor = currentStatusEffect.GetEffectColor();
            effectParticle.Play();
        }
    }

    /// <summary>
    /// ターン数表示を更新
    /// </summary>
    private void UpdateTurnDisplay()
    {
        if (turnCountText != null && currentStatusEffect != null)
        {
            if (currentStatusEffect.remainingTurns > 0)
            {
                turnCountText.text = currentStatusEffect.remainingTurns.ToString();
                turnCountText.gameObject.SetActive(true);

                // 残りターンが少ない場合は警告色
                if (currentStatusEffect.remainingTurns <= 1)
                {
                    turnCountText.color = Color.red;
                }
                else
                {
                    turnCountText.color = Color.white;
                }
            }
            else
            {
                turnCountText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// スキル効果タイプに応じたスプライト取得
    /// </summary>
    private Sprite GetEffectSpriteByType(StatusEffectType effectType)
    {
        return effectType switch
        {
            StatusEffectType.AttackUp => attackUpIcon,
            StatusEffectType.AttackDown => attackDownIcon,
            StatusEffectType.DefenseUp => defenseUpIcon,
            StatusEffectType.DefenseDown => defenseDownIcon,
            StatusEffectType.Poison => poisonIcon,
            StatusEffectType.Regen => regenIcon,
            StatusEffectType.Stun => stunIcon,
            _ => null
        };
    }

    /// <summary>
    /// ステータス効果タイプに応じた背景色取得
    /// </summary>
    private Color GetBackgroundColorByType(StatusEffectData statusEffect)
    {
        if (statusEffect.isPositive)
        {
            return buffBackgroundColor;
        }
        else
        {
            return debuffBackgroundColor;
        }
    }

    #endregion

    #region アニメーション

    /// <summary>
    /// フェードアウト
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        float fadeTime = 0.5f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// ボタンクリック処理
    /// </summary>
    private void OnButtonClicked()
    {
        if (currentStatusEffect != null)
        {
            OnIconClicked?.Invoke(currentStatusEffect);
            DebugLog($"アイコンクリック: {currentStatusEffect.effectName}");
        }
    }

    /// <summary>
    /// ポインターエンター処理
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentStatusEffect != null)
        {
            OnIconHovered?.Invoke(currentStatusEffect);
            DebugLog($"アイコンホバー開始: {currentStatusEffect.effectName}");
        }
    }

    /// <summary>
    /// ポインターエグジット処理
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentStatusEffect != null)
        {
            OnIconUnhovered?.Invoke(currentStatusEffect);
            DebugLog($"アイコンホバー終了: {currentStatusEffect.effectName}");
        }
    }

    #endregion

    #region ログ・デバッグ

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[StatusEffectIconUI] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[StatusEffectIconUI] {message}");
    }

    #endregion

    #region エディタ用ツール

#if UNITY_EDITOR
    [ContextMenu("テスト用ステータス効果設定（攻撃強化）")]
    private void TestAttackUpEffect()
    {
        var testEffect = new StatusEffectData
        {
            effectId = 1,
            effectName = "攻撃力上昇",
            effectType = StatusEffectType.AttackUp,
            remainingTurns = 3,
            isPositive = true
        };

        SetStatusEffect(testEffect);
    }

    [ContextMenu("テスト用ステータス効果設定（毒）")]
    private void TestPoisonEffect()
    {
        var testEffect = new StatusEffectData
        {
            effectId = 2,
            effectName = "毒",
            effectType = StatusEffectType.Poison,
            remainingTurns = 5,
            isPositive = false
        };

        SetStatusEffect(testEffect);
    }

    [ContextMenu("テスト用ステータス効果設定（継続回復）")]
    private void TestRegenEffect()
    {
        var testEffect = new StatusEffectData
        {
            effectId = 3,
            effectName = "継続回復",
            effectType = StatusEffectType.Regen,
            remainingTurns = 4,
            isPositive = true
        };

        SetStatusEffect(testEffect);
    }

    [ContextMenu("アイコン画像確認")]
    private void ValidateIconSprites()
    {
        Debug.Log("=== アイコン画像確認 ===");
        Debug.Log($"攻撃強化: {(attackUpIcon != null ? "設定済み" : "未設定")}");
        Debug.Log($"攻撃弱化: {(attackDownIcon != null ? "設定済み" : "未設定")}");
        Debug.Log($"防御強化: {(defenseUpIcon != null ? "設定済み" : "未設定")}");
        Debug.Log($"防御弱化: {(defenseDownIcon != null ? "設定済み" : "未設定")}");
        Debug.Log($"毒: {(poisonIcon != null ? "設定済み" : "未設定")}");
        Debug.Log($"継続回復: {(regenIcon != null ? "設定済み" : "未設定")}");
        Debug.Log($"気絶: {(stunIcon != null ? "設定済み" : "未設定")}");
    }
#endif

    #endregion
}