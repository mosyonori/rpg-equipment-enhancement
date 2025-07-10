using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// バフ・デバフの視覚的表現UI管理（統一プレハブ版）
/// 役割：状態異常・強化効果の表示制御
/// 機能：バフ・デバフアイコン表示、残りターン数表示、効果発動エフェクト
/// データアクセス統一ルール: UI層 → Manager層 → データ層
/// </summary>
public class StatusEffectUI : MonoBehaviour
{
    [Header("状態異常表示エリア")]
    [SerializeField] private Transform statusEffectContainer;
    [SerializeField] private GridLayoutGroup statusEffectGrid;
    [SerializeField] private ScrollRect statusEffectScrollRect;

    [Header("ステータス効果アイコンプレハブ（統一）")]
    [SerializeField] private GameObject statusEffectIconPrefab; // 単一プレハブのみ

    [Header("表示設定")]
    [SerializeField] private int maxVisibleEffects = 8;
    [SerializeField] private Vector2 iconSize = new Vector2(50f, 50f);
    [SerializeField] private Vector2 iconSpacing = new Vector2(5f, 5f);
    [SerializeField] private bool showEffectNames = true;
    [SerializeField] private bool showRemainingTurns = true;

    [Header("色設定")]
    [SerializeField] private Color buffColor = new Color(0.2f, 0.8f, 0.2f, 1f);        // バフ緑
    [SerializeField] private Color debuffColor = new Color(0.8f, 0.2f, 0.2f, 1f);      // デバフ赤
    [SerializeField] private Color neutralColor = new Color(0.6f, 0.6f, 0.6f, 1f);    // 中性グレー
    [SerializeField] private Color expiringColor = new Color(1f, 0.8f, 0f, 1f);       // 期限切れ間近黄

    [Header("アニメーション設定")]
    [SerializeField] private float iconAppearDuration = 0.4f;
    [SerializeField] private float iconDisappearDuration = 0.3f;
    [SerializeField] private float effectPulseDuration = 1.0f;
    [SerializeField] private AnimationCurve appearEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve pulseEasing = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);

    [Header("エフェクト設定")]
    [SerializeField] private GameObject buffApplyEffectPrefab;
    [SerializeField] private GameObject debuffApplyEffectPrefab;
    [SerializeField] private GameObject effectExpireEffectPrefab;
    [SerializeField] private float effectParticleDuration = 1.5f;

    [Header("ツールチップ設定")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipNameText;
    [SerializeField] private TextMeshProUGUI tooltipDescriptionText;
    [SerializeField] private TextMeshProUGUI tooltipRemainingText;
    [SerializeField] private float tooltipDelay = 0.5f;

    // イベント
    public static event Action<StatusEffectData> OnStatusEffectHovered;
    public static event Action OnStatusEffectUnhovered;

    // 内部状態
    private bool isInitialized = false;
    private Dictionary<int, StatusEffectIconUI> activeEffectIcons;
    private string currentCharacterId = "";
    private Coroutine tooltipCoroutine;
    private List<StatusEffectData> cachedEffects;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeCollections();
        ValidateComponents();
    }

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        UnregisterEvents();
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
            Log("StatusEffectUI初期化開始");

            // コレクション初期化
            InitializeCollections();

            // グリッドレイアウト設定
            SetupGridLayout();

            // ツールチップ初期化
            InitializeTooltip();

            // イベント登録
            RegisterEvents();

            isInitialized = true;
            Log("StatusEffectUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"StatusEffectUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// コレクション初期化
    /// </summary>
    private void InitializeCollections()
    {
        activeEffectIcons = new Dictionary<int, StatusEffectIconUI>();
        cachedEffects = new List<StatusEffectData>();
    }

    /// <summary>
    /// コンポーネント検証
    /// </summary>
    private void ValidateComponents()
    {
        if (statusEffectContainer == null)
            LogWarning("statusEffectContainerが設定されていません");

        if (statusEffectIconPrefab == null)
            LogWarning("statusEffectIconPrefabが設定されていません");

        if (statusEffectGrid == null)
            LogWarning("statusEffectGridが設定されていません");
    }

    /// <summary>
    /// グリッドレイアウト設定
    /// </summary>
    private void SetupGridLayout()
    {
        if (statusEffectGrid == null) return;

        try
        {
            statusEffectGrid.cellSize = iconSize;
            statusEffectGrid.spacing = iconSpacing;
            statusEffectGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            statusEffectGrid.constraintCount = Mathf.Min(maxVisibleEffects, 4);
            statusEffectGrid.childAlignment = TextAnchor.MiddleCenter;

            Log("グリッドレイアウト設定完了");
        }
        catch (Exception e)
        {
            LogError($"グリッドレイアウト設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ツールチップ初期化
    /// </summary>
    private void InitializeTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    /// <summary>
    /// イベント登録
    /// </summary>
    private void RegisterEvents()
    {
        // BattleDataManagerからの状態異常変更イベントを受信
        BattleDataManager.OnStatusEffectApplied += OnStatusEffectApplied;
        BattleDataManager.OnStatusEffectRemoved += OnStatusEffectRemoved;
    }

    /// <summary>
    /// イベント登録解除
    /// </summary>
    private void UnregisterEvents()
    {
        BattleDataManager.OnStatusEffectApplied -= OnStatusEffectApplied;
        BattleDataManager.OnStatusEffectRemoved -= OnStatusEffectRemoved;
    }

    #endregion

    #region 公開メソッド - 状態異常表示管理

    /// <summary>
    /// キャラクターの状態異常を表示
    /// </summary>
    /// <param name="characterId">キャラクターID</param>
    public void DisplayStatusEffects(string characterId)
    {
        if (!isInitialized) Initialize();

        if (string.IsNullOrEmpty(characterId)) return;

        try
        {
            currentCharacterId = characterId;

            // BattleManagerから状態異常リストを取得
            List<StatusEffectData> effects = new List<StatusEffectData>();

            if (BattleManager.Instance != null)
            {
                var battleDataManager = BattleManager.Instance.GetComponent<BattleDataManager>();
                if (battleDataManager != null)
                {
                    effects = battleDataManager.GetCharacterStatusEffects(characterId);
                }
            }

            UpdateStatusEffectDisplay(effects);

            Log($"状態異常表示更新: {characterId} - {effects.Count}個");
        }
        catch (Exception e)
        {
            LogError($"状態異常表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 状態異常表示をクリア
    /// </summary>
    public void ClearStatusEffects()
    {
        try
        {
            // 既存のアイコンを削除
            foreach (var icon in activeEffectIcons.Values)
            {
                if (icon != null && icon.gameObject != null)
                {
                    StartCoroutine(RemoveEffectIcon(icon));
                }
            }

            activeEffectIcons.Clear();
            cachedEffects.Clear();
            currentCharacterId = "";

            Log("状態異常表示クリア完了");
        }
        catch (Exception e)
        {
            LogError($"状態異常表示クリアエラー: {e.Message}");
        }
    }

    /// <summary>
    /// 状態異常の更新
    /// </summary>
    public void RefreshStatusEffects()
    {
        if (!string.IsNullOrEmpty(currentCharacterId))
        {
            DisplayStatusEffects(currentCharacterId);
        }
    }

    #endregion

    #region 内部メソッド - 表示更新

    /// <summary>
    /// 状態異常表示の更新
    /// </summary>
    private void UpdateStatusEffectDisplay(List<StatusEffectData> newEffects)
    {
        if (newEffects == null) return;

        try
        {
            // アクティブな効果のみをフィルタ
            var activeEffects = newEffects.Where(e => e.IsActive()).ToList();

            // 効果の優先順位でソート（表示優先順）
            activeEffects = activeEffects.OrderByDescending(e => e.displayPriority)
                                       .ThenByDescending(e => e.isPositive)
                                       .Take(maxVisibleEffects)
                                       .ToList();

            // 削除された効果を特定
            var effectsToRemove = activeEffectIcons.Keys
                .Where(id => !activeEffects.Any(e => e.effectId == id))
                .ToList();

            // 効果削除
            foreach (var effectId in effectsToRemove)
            {
                RemoveStatusEffectIcon(effectId);
            }

            // 新規効果追加・既存効果更新
            foreach (var effect in activeEffects)
            {
                if (activeEffectIcons.ContainsKey(effect.effectId))
                {
                    // 既存効果の更新
                    UpdateStatusEffectIcon(effect);
                }
                else
                {
                    // 新規効果の追加
                    AddStatusEffectIcon(effect);
                }
            }

            cachedEffects = new List<StatusEffectData>(activeEffects);
        }
        catch (Exception e)
        {
            LogError($"状態異常表示更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 状態異常アイコン追加
    /// </summary>
    private void AddStatusEffectIcon(StatusEffectData effect)
    {
        if (effect == null || statusEffectContainer == null || statusEffectIconPrefab == null) return;

        try
        {
            // アイコン作成
            GameObject iconObj = Instantiate(statusEffectIconPrefab, statusEffectContainer);
            var iconComponent = iconObj.GetComponent<StatusEffectIconUI>();

            if (iconComponent == null)
            {
                LogError("StatusEffectIconUIコンポーネントが見つかりません");
                DestroyImmediate(iconObj);
                return;
            }

            // アイコン設定
            iconComponent.SetStatusEffect(effect);
            iconComponent.OnIconHovered += OnEffectIconHovered;
            iconComponent.OnIconUnhovered += OnEffectIconUnhovered;
            iconComponent.OnIconClicked += OnEffectIconClicked;

            // 辞書に登録
            activeEffectIcons[effect.effectId] = iconComponent;

            // 出現アニメーション
            StartCoroutine(PlayAppearAnimation(iconComponent));

            // エフェクト再生
            PlayStatusEffectAppliedEffect(effect);

            Log($"状態異常アイコン追加: {effect.effectName}");
        }
        catch (Exception e)
        {
            LogError($"状態異常アイコン追加エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 状態異常アイコン更新
    /// </summary>
    private void UpdateStatusEffectIcon(StatusEffectData effect)
    {
        if (effect == null || !activeEffectIcons.ContainsKey(effect.effectId))
            return;

        try
        {
            var iconComponent = activeEffectIcons[effect.effectId];
            iconComponent.UpdateTurnCount(effect.remainingTurns);

            // 期限切れ間近の場合はパルスエフェクト
            if (effect.remainingTurns <= 1 && effect.remainingTurns > 0)
            {
                StartCoroutine(PlayPulseAnimation(iconComponent));
            }
        }
        catch (Exception e)
        {
            LogError($"状態異常アイコン更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 状態異常アイコン削除
    /// </summary>
    private void RemoveStatusEffectIcon(int effectId)
    {
        if (!activeEffectIcons.ContainsKey(effectId)) return;

        try
        {
            var iconComponent = activeEffectIcons[effectId];

            // イベント解除
            iconComponent.OnIconHovered -= OnEffectIconHovered;
            iconComponent.OnIconUnhovered -= OnEffectIconUnhovered;
            iconComponent.OnIconClicked -= OnEffectIconClicked;

            // 消滅アニメーション
            StartCoroutine(RemoveEffectIcon(iconComponent));

            activeEffectIcons.Remove(effectId);

            Log($"状態異常アイコン削除: ID={effectId}");
        }
        catch (Exception e)
        {
            LogError($"状態異常アイコン削除エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - アニメーション

    /// <summary>
    /// アイコン出現アニメーション
    /// </summary>
    private IEnumerator PlayAppearAnimation(StatusEffectIconUI iconComponent)
    {
        if (iconComponent == null) yield break;

        Transform iconTransform = iconComponent.transform;
        Vector3 originalScale = iconTransform.localScale;
        iconTransform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < iconAppearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / iconAppearDuration;
            float curveValue = appearEasing.Evaluate(t);
            iconTransform.localScale = Vector3.Lerp(Vector3.zero, originalScale, curveValue);
            yield return null;
        }

        iconTransform.localScale = originalScale;
    }

    /// <summary>
    /// アイコン消滅アニメーション
    /// </summary>
    private IEnumerator RemoveEffectIcon(StatusEffectIconUI iconComponent)
    {
        if (iconComponent == null || iconComponent.gameObject == null) yield break;

        Transform iconTransform = iconComponent.transform;
        Vector3 originalScale = iconTransform.localScale;

        float elapsed = 0f;
        while (elapsed < iconDisappearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / iconDisappearDuration;
            iconTransform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);

            // フェードアウト
            var canvasGroup = iconComponent.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - t;
            }

            yield return null;
        }

        // エフェクト再生
        PlayStatusEffectExpiredEffect(iconComponent.transform.position);

        // オブジェクト削除
        DestroyImmediate(iconComponent.gameObject);
    }

    /// <summary>
    /// パルスアニメーション
    /// </summary>
    private IEnumerator PlayPulseAnimation(StatusEffectIconUI iconComponent)
    {
        if (iconComponent == null) yield break;

        Transform iconTransform = iconComponent.transform;
        Vector3 originalScale = iconTransform.localScale;
        Vector3 pulseScale = originalScale * 1.2f;

        float elapsed = 0f;
        while (elapsed < effectPulseDuration && iconComponent != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / effectPulseDuration;
            float pulseValue = pulseEasing.Evaluate(t);
            iconTransform.localScale = Vector3.Lerp(originalScale, pulseScale, pulseValue);
            yield return null;
        }

        if (iconComponent != null)
        {
            iconTransform.localScale = originalScale;
        }
    }

    #endregion

    #region 内部メソッド - エフェクト

    /// <summary>
    /// 状態異常適用エフェクト再生
    /// </summary>
    private void PlayStatusEffectAppliedEffect(StatusEffectData effect)
    {
        try
        {
            GameObject effectPrefab = effect.isPositive ? buffApplyEffectPrefab : debuffApplyEffectPrefab;

            if (effectPrefab != null && statusEffectContainer != null)
            {
                GameObject effectObj = Instantiate(effectPrefab, statusEffectContainer);
                Destroy(effectObj, effectParticleDuration);
            }
        }
        catch (Exception e)
        {
            LogError($"状態異常適用エフェクトエラー: {e.Message}");
        }
    }

    /// <summary>
    /// 状態異常期限切れエフェクト再生
    /// </summary>
    private void PlayStatusEffectExpiredEffect(Vector3 position)
    {
        try
        {
            if (effectExpireEffectPrefab != null)
            {
                GameObject effectObj = Instantiate(effectExpireEffectPrefab);
                effectObj.transform.position = position;
                Destroy(effectObj, effectParticleDuration);
            }
        }
        catch (Exception e)
        {
            LogError($"状態異常期限切れエフェクトエラー: {e.Message}");
        }
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// 状態異常適用イベントハンドラ
    /// </summary>
    private void OnStatusEffectApplied(BattleCharacterData character, StatusEffectData effect)
    {
        try
        {
            if (character != null && character.characterId == currentCharacterId)
            {
                RefreshStatusEffects();
            }
        }
        catch (Exception e)
        {
            LogError($"状態異常適用イベントエラー: {e.Message}");
        }
    }

    /// <summary>
    /// 状態異常除去イベントハンドラ
    /// </summary>
    private void OnStatusEffectRemoved(BattleCharacterData character, StatusEffectData effect)
    {
        try
        {
            if (character != null && character.characterId == currentCharacterId)
            {
                RemoveStatusEffectIcon(effect.effectId);
            }
        }
        catch (Exception e)
        {
            LogError($"状態異常除去イベントエラー: {e.Message}");
        }
    }

    /// <summary>
    /// エフェクトアイコンホバーイベント
    /// </summary>
    private void OnEffectIconHovered(StatusEffectData effect)
    {
        try
        {
            ShowTooltip(effect);
            OnStatusEffectHovered?.Invoke(effect);
        }
        catch (Exception e)
        {
            LogError($"エフェクトアイコンホバーエラー: {e.Message}");
        }
    }

    /// <summary>
    /// エフェクトアイコンホバー解除イベント
    /// </summary>
    private void OnEffectIconUnhovered(StatusEffectData effect)
    {
        try
        {
            HideTooltip();
            OnStatusEffectUnhovered?.Invoke();
        }
        catch (Exception e)
        {
            LogError($"エフェクトアイコンホバー解除エラー: {e.Message}");
        }
    }

    /// <summary>
    /// エフェクトアイコンクリックイベント
    /// </summary>
    private void OnEffectIconClicked(StatusEffectData effect)
    {
        try
        {
            Log($"状態異常アイコンクリック: {effect.effectName}");
            // 将来的に詳細情報表示などを実装予定
        }
        catch (Exception e)
        {
            LogError($"エフェクトアイコンクリックエラー: {e.Message}");
        }
    }

    #endregion

    #region ツールチップ

    /// <summary>
    /// ツールチップ表示
    /// </summary>
    private void ShowTooltip(StatusEffectData effect)
    {
        if (tooltipPanel == null || effect == null) return;

        try
        {
            if (tooltipCoroutine != null)
                StopCoroutine(tooltipCoroutine);

            tooltipCoroutine = StartCoroutine(ShowTooltipCoroutine(effect));
        }
        catch (Exception e)
        {
            LogError($"ツールチップ表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ツールチップ非表示
    /// </summary>
    private void HideTooltip()
    {
        try
        {
            if (tooltipCoroutine != null)
            {
                StopCoroutine(tooltipCoroutine);
                tooltipCoroutine = null;
            }

            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }
        catch (Exception e)
        {
            LogError($"ツールチップ非表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ツールチップ表示コルーチン
    /// </summary>
    private IEnumerator ShowTooltipCoroutine(StatusEffectData effect)
    {
        yield return new WaitForSeconds(tooltipDelay);

        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(true);

            if (tooltipNameText != null)
                tooltipNameText.text = effect.effectName;

            if (tooltipDescriptionText != null)
                tooltipDescriptionText.text = effect.GetDescription();

            if (tooltipRemainingText != null)
                tooltipRemainingText.text = $"残り{effect.remainingTurns}ターン";
        }

        tooltipCoroutine = null;
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        Debug.Log($"[StatusEffectUI] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[StatusEffectUI] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[StatusEffectUI] {message}");
    }

    #endregion

    #region エディタ用ツール

#if UNITY_EDITOR
    [ContextMenu("状態異常表示テスト")]
    private void TestStatusEffectDisplay()
    {
        Log("状態異常表示テスト実行");

        if (BattleManager.Instance != null)
        {
            var playerCharacter = BattleManager.Instance.GetPlayerCharacter();
            if (playerCharacter != null)
            {
                DisplayStatusEffects(playerCharacter.characterId);
            }
        }
        else
        {
            LogWarning("BattleManagerが見つかりません");
        }
    }

    [ContextMenu("状態異常クリアテスト")]
    private void TestClearStatusEffects()
    {
        ClearStatusEffects();
        Log("状態異常クリアテスト実行");
    }
#endif

    #endregion
}