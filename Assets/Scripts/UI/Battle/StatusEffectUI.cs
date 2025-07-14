using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// バフ・デバフ状況の統合的表示
/// 責任範囲：
/// - 状態異常アイコン表示
/// - 残りターン数表示
/// - バフ・デバフの色分け表示
/// - 複数効果の動的管理
/// データアクセス統一ルール: UI層専用コンポーネント（StatusEffectDataリストを受け取り表示のみ）
/// </summary>
public class StatusEffectUI : MonoBehaviour
{
    #region デバッグ・ログ

    /// <summary>
    /// デバッグログ出力
    /// </summary>
    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[StatusEffectUI] {message}");
        }
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    private void LogError(string message)
    {
        Debug.LogError($"[StatusEffectUI] {message}");
    }

    /// <summary>
    /// 警告ログ出力
    /// </summary>
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[StatusEffectUI] {message}");
    }

    #endregion

    #region フィールド

    [Header("UI設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showWhenEmpty = false;

    [Header("エフェクト表示")]
    [SerializeField] private Transform effectListParent;
    [SerializeField] private GameObject effectSlotPrefab;
    [SerializeField] private int maxDisplayEffects = 6;

    [Header("レイアウト設定")]
    [SerializeField] private bool useHorizontalLayout = true;
    [SerializeField] private float effectSlotSpacing = 5f;
    [SerializeField] private Vector2 effectSlotSize = new Vector2(32f, 32f);

    [Header("色設定")]
    [SerializeField] private Color defaultBuffColor = new Color(1f, 0.4f, 0.3f); // #ff6347
    [SerializeField] private Color defaultDebuffColor = new Color(0.25f, 0.41f, 0.88f); // #4169e1
    [SerializeField] private Color turnTextColor = Color.white;

    [Header("アニメーション設定")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;

    // 内部状態
    private List<StatusEffectData> currentEffects;
    private List<StatusEffectSlot> effectSlots;
    private bool isInitialized = false;

    #endregion

    #region プロパティ

    /// <summary>
    /// 初期化完了状態
    /// </summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// 現在表示中の効果数
    /// </summary>
    public int CurrentEffectCount => currentEffects?.Count ?? 0;

    /// <summary>
    /// 表示可能な最大効果数
    /// </summary>
    public int MaxDisplayEffects => maxDisplayEffects;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        Log("StatusEffectUI Awake開始");
        ValidateComponents();
    }

    private void Start()
    {
        Log("StatusEffectUI Start開始");
        InitializeStatusEffectUI();
    }

    private void OnDestroy()
    {
        Log("StatusEffectUI OnDestroy開始");
        CleanupStatusEffectUI();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// 修正: コンポーネント検証（安全性向上）
    /// </summary>
    private void ValidateComponents()
    {
        try
        {
            // 必須コンポーネント確認
            if (effectListParent == null)
            {
                LogError("effectListParent が設定されていません。自動的に自身のTransformを使用します。");
                effectListParent = this.transform;
            }

            if (effectSlotPrefab == null)
            {
                LogError("effectSlotPrefab が設定されていません。Inspectorで設定してください。");
                return;
            }

            // プレハブの検証
            var slotComponent = effectSlotPrefab.GetComponent<StatusEffectSlot>();
            if (slotComponent == null)
            {
                LogError("effectSlotPrefab にStatusEffectSlotコンポーネントがありません");
            }

            Log("コンポーネント検証完了");
        }
        catch (Exception e)
        {
            LogError($"コンポーネント検証エラー: {e.Message}");
        }
    }

    /// <summary>
    /// StatusEffectUI初期化
    /// </summary>
    private void InitializeStatusEffectUI()
    {
        try
        {
            Log("StatusEffectUI初期化開始");

            // リスト初期化
            currentEffects = new List<StatusEffectData>();
            effectSlots = new List<StatusEffectSlot>();

            // レイアウト設定
            SetupLayout();

            // 初期状態設定
            SetVisible(showWhenEmpty);

            isInitialized = true;
            Log("StatusEffectUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"StatusEffectUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: レイアウト設定（エラー対策）
    /// </summary>
    private void SetupLayout()
    {
        try
        {
            if (effectListParent == null)
            {
                LogError("effectListParent が null のため、レイアウト設定をスキップします");
                return;
            }

            // 既存のLayoutGroupを削除
            var existingLayouts = effectListParent.GetComponents<LayoutGroup>();
            for (int i = existingLayouts.Length - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(existingLayouts[i]);
                }
                else
                {
                    DestroyImmediate(existingLayouts[i]);
                }
            }

            // レイアウトグループ追加
            if (useHorizontalLayout)
            {
                var hlg = effectListParent.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlHeight = false;
                hlg.childControlWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.spacing = effectSlotSpacing;
            }
            else
            {
                var vlg = effectListParent.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlHeight = false;
                vlg.childControlWidth = false;
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = false;
                vlg.spacing = effectSlotSpacing;
            }

            // ContentSizeFitterで自動サイズ調整
            var csf = effectListParent.gameObject.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = effectListParent.gameObject.AddComponent<ContentSizeFitter>();
            }

            if (useHorizontalLayout)
            {
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.verticalFit = ContentSizeFitter.FitMode.MinSize;
            }
            else
            {
                csf.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            Log("レイアウト設定完了");
        }
        catch (Exception e)
        {
            LogError($"レイアウト設定エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - 状態異常更新

    /// <summary>
    /// キャラクターデータから状態異常更新
    /// </summary>
    /// <param name="characterData">キャラクターデータ</param>
    public void UpdateFromCharacterData(BattleCharacterData characterData)
    {
        if (characterData == null)
        {
            LogError("BattleCharacterDataがnullです");
            return;
        }

        try
        {
            UpdateStatusEffects(characterData.statusEffects);
            Log($"キャラクターデータから状態異常更新: {characterData.characterName}");
        }
        catch (Exception e)
        {
            LogError($"キャラクターデータからの状態異常更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: 状態異常リスト更新（安全性向上）
    /// </summary>
    /// <param name="statusEffects">状態異常データリスト</param>
    public void UpdateStatusEffects(List<StatusEffectData> statusEffects)
    {
        try
        {
            // null や空リストの処理
            if (statusEffects == null)
            {
                statusEffects = new List<StatusEffectData>();
            }

            // 有効な効果のみフィルタリング（残りターン数が0より大きい）
            var validEffects = statusEffects
                .Where(effect => effect != null && effect.remainingTurns > 0)
                .OrderByDescending(effect => effect.displayPriority) // 優先順位順
                .Take(maxDisplayEffects) // 最大表示数まで
                .ToList();

            // 現在の効果と比較して更新が必要かチェック
            if (!AreEffectsEqual(currentEffects, validEffects))
            {
                currentEffects = validEffects;
                RefreshEffectDisplay();

                Log($"状態異常更新: {validEffects.Count}個の効果を表示");
            }

            // 表示状態更新
            bool shouldShow = validEffects.Count > 0 || showWhenEmpty;
            SetVisible(shouldShow);
        }
        catch (Exception e)
        {
            LogError($"状態異常更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: 状態異常クリア（安全性向上）
    /// </summary>
    public void ClearStatusEffects()
    {
        try
        {
            Log("状態異常クリア開始");

            // currentEffectsが null でないことを確認
            if (currentEffects == null)
            {
                currentEffects = new List<StatusEffectData>();
            }
            else
            {
                currentEffects.Clear();
            }

            RefreshEffectDisplay();
            SetVisible(showWhenEmpty);

            Log("状態異常クリア完了");
        }
        catch (Exception e)
        {
            LogError($"効果スロットクリアエラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - 表示制御

    /// <summary>
    /// StatusEffectUI表示切替
    /// </summary>
    /// <param name="visible">表示するか</param>
    public void SetVisible(bool visible)
    {
        try
        {
            if (gameObject != null)
            {
                gameObject.SetActive(visible);
                Log($"StatusEffectUI表示切替: {visible}");
            }
        }
        catch (Exception e)
        {
            LogError($"StatusEffectUI表示切替エラー: {e.Message}");
        }
    }

    /// <summary>
    /// レイアウト方向設定
    /// </summary>
    /// <param name="horizontal">水平レイアウトにするか</param>
    public void SetLayoutHorizontal(bool horizontal)
    {
        try
        {
            if (useHorizontalLayout != horizontal)
            {
                useHorizontalLayout = horizontal;
                SetupLayout();
                RefreshEffectDisplay();

                Log($"レイアウト方向変更: {(horizontal ? "水平" : "垂直")}");
            }
        }
        catch (Exception e)
        {
            LogError($"レイアウト方向設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 最大表示効果数設定
    /// </summary>
    /// <param name="maxEffects">最大表示数</param>
    public void SetMaxDisplayEffects(int maxEffects)
    {
        try
        {
            maxDisplayEffects = Mathf.Max(1, maxEffects);

            // 現在の効果が最大数を超えている場合は再表示
            if (currentEffects != null && currentEffects.Count > maxDisplayEffects)
            {
                RefreshEffectDisplay();
            }

            Log($"最大表示効果数設定: {maxDisplayEffects}");
        }
        catch (Exception e)
        {
            LogError($"最大表示効果数設定エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - 表示更新

    /// <summary>
    /// 修正: 効果表示更新（安全性向上）
    /// </summary>
    private void RefreshEffectDisplay()
    {
        try
        {
            Log("効果表示更新開始");

            // 既存スロットをクリア
            ClearEffectSlots();

            // 現在の効果に応じてスロット作成
            if (currentEffects != null && currentEffects.Count > 0)
            {
                foreach (var effect in currentEffects)
                {
                    if (effect != null)
                    {
                        CreateEffectSlot(effect);
                    }
                }
            }

            Log($"効果表示更新完了: {effectSlots?.Count ?? 0}個のスロット作成");
        }
        catch (Exception e)
        {
            LogError($"効果表示更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: 効果スロット作成（安全性向上）
    /// </summary>
    /// <param name="effectData">状態異常データ</param>
    private void CreateEffectSlot(StatusEffectData effectData)
    {
        try
        {
            if (effectSlotPrefab == null || effectListParent == null || effectData == null)
            {
                LogWarning("効果スロット作成に必要な要素が不足しています");
                return;
            }

            // スロットインスタンス作成
            GameObject slotObject = Instantiate(effectSlotPrefab, effectListParent);
            StatusEffectSlot slot = slotObject.GetComponent<StatusEffectSlot>();

            if (slot == null)
            {
                LogError("StatusEffectSlotコンポーネントが見つかりません");
                if (slotObject != null)
                {
                    Destroy(slotObject);
                }
                return;
            }

            // スロットサイズ設定
            var rectTransform = slotObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = effectSlotSize;
            }

            // スロットデータ設定
            slot.SetEffectData(effectData, GetEffectColor(effectData));

            // リストに追加
            if (effectSlots == null)
            {
                effectSlots = new List<StatusEffectSlot>();
            }
            effectSlots.Add(slot);

            Log($"効果スロット作成: {effectData.effectName} (残り{effectData.remainingTurns}ターン)");
        }
        catch (Exception e)
        {
            LogError($"効果スロット作成エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: 効果スロットクリア（安全性向上）
    /// </summary>
    private void ClearEffectSlots()
    {
        try
        {
            if (effectSlots != null)
            {
                foreach (var slot in effectSlots)
                {
                    if (slot != null && slot.gameObject != null)
                    {
                        Destroy(slot.gameObject);
                    }
                }
                effectSlots.Clear();
            }
            else
            {
                effectSlots = new List<StatusEffectSlot>();
            }

            Log("効果スロットクリア完了");
        }
        catch (Exception e)
        {
            LogError($"効果スロットクリアエラー: {e.Message}");
        }
    }

    /// <summary>
    /// 効果の色取得
    /// </summary>
    /// <param name="effectData">状態異常データ</param>
    /// <returns>効果の色</returns>
    private Color GetEffectColor(StatusEffectData effectData)
    {
        try
        {
            if (effectData == null) return defaultDebuffColor;

            // colorCodeが設定されている場合はそちらを使用
            if (!string.IsNullOrEmpty(effectData.colorCode))
            {
                if (ColorUtility.TryParseHtmlString(effectData.colorCode, out Color parsedColor))
                {
                    return parsedColor;
                }
            }

            // 効果タイプに応じてデフォルト色を返す
            return IsBuffEffect(effectData) ? defaultBuffColor : defaultDebuffColor;
        }
        catch (Exception e)
        {
            LogError($"効果色取得エラー: {e.Message}");
            return defaultDebuffColor;
        }
    }

    /// <summary>
    /// バフ効果かどうか判定
    /// </summary>
    /// <param name="effectData">状態異常データ</param>
    /// <returns>バフ効果かどうか</returns>
    private bool IsBuffEffect(StatusEffectData effectData)
    {
        try
        {
            if (effectData == null) return false;

            // 基本的な判定ロジック
            // 攻撃力・防御力・回復効果がプラスならバフ
            bool hasPositiveEffect = effectData.offenseMultiplier > 1.0f ||
                                   effectData.defenseMultiplier > 1.0f ||
                                   effectData.turnStartHealPercent > 0;

            // ダメージやアクション阻害があればデバフ
            bool hasNegativeEffect = effectData.turnStartDamagePercent > 0 ||
                                    effectData.preventAction ||
                                    effectData.offenseMultiplier < 1.0f ||
                                    effectData.defenseMultiplier < 1.0f;

            return hasPositiveEffect && !hasNegativeEffect;
        }
        catch (Exception e)
        {
            LogError($"バフ効果判定エラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 効果リストの比較
    /// </summary>
    /// <param name="list1">リスト1</param>
    /// <param name="list2">リスト2</param>
    /// <returns>同じかどうか</returns>
    private bool AreEffectsEqual(List<StatusEffectData> list1, List<StatusEffectData> list2)
    {
        if (list1 == null && list2 == null) return true;
        if (list1 == null || list2 == null) return false;
        if (list1.Count != list2.Count) return false;

        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i]?.effectId != list2[i]?.effectId ||
                list1[i]?.remainingTurns != list2[i]?.remainingTurns)
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region クリーンアップ

    /// <summary>
    /// StatusEffectUIクリーンアップ
    /// </summary>
    private void CleanupStatusEffectUI()
    {
        try
        {
            // スロットクリア
            ClearEffectSlots();

            // リストクリア
            currentEffects?.Clear();
            effectSlots?.Clear();

            Log("StatusEffectUIクリーンアップ完了");
        }
        catch (Exception e)
        {
            LogError($"StatusEffectUIクリーンアップエラー: {e.Message}");
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
        Log("=== StatusEffectUI状態情報 ===");
        Log($"初期化完了: {isInitialized}");
        Log($"現在の効果数: {CurrentEffectCount}");
        Log($"最大表示数: {maxDisplayEffects}");
        Log($"レイアウト: {(useHorizontalLayout ? "水平" : "垂直")}");
        Log($"空時表示: {showWhenEmpty}");
        Log($"スロット数: {effectSlots?.Count ?? 0}");

        if (currentEffects != null && currentEffects.Count > 0)
        {
            Log("現在の効果:");
            foreach (var effect in currentEffects)
            {
                Log($"  - {effect.effectName} (ID:{effect.effectId}, 残り:{effect.remainingTurns}ターン)");
            }
        }
        else
        {
            Log("現在の効果: なし");
        }

        Log("==============================");
    }

    /// <summary>
    /// デバッグ用：テスト効果追加
    /// </summary>
    [ContextMenu("デバッグ：テスト効果追加")]
    public void DebugAddTestEffects()
    {
        Log("デバッグ：テスト効果追加実行");

        var testEffects = new List<StatusEffectData>
        {
            new StatusEffectData
            {
                effectId = 3,
                effectName = "攻撃力増加",
                remainingTurns = 3,
                displayPriority = 100,
                colorCode = "#ff6347",
                offenseMultiplier = 1.5f,
                isPositive = true
            },
            new StatusEffectData
            {
                effectId = 1,
                effectName = "攻撃力低下",
                remainingTurns = 2,
                displayPriority = 100,
                colorCode = "#4169e1",
                offenseMultiplier = 0.7f,
                isPositive = false
            },
            new StatusEffectData
            {
                effectId = 8,
                effectName = "持続回復",
                remainingTurns = 5,
                displayPriority = 100,
                colorCode = "#ff6347",
                turnStartHealPercent = 10,
                isPositive = true
            }
        };

        UpdateStatusEffects(testEffects);
    }

    /// <summary>
    /// デバッグ用：効果クリア
    /// </summary>
    [ContextMenu("デバッグ：効果クリア")]
    public void DebugClearEffects()
    {
        Log("デバッグ：効果クリア実行");
        ClearStatusEffects();
    }

    /// <summary>
    /// デバッグ用：コンポーネント接続確認
    /// </summary>
    [ContextMenu("デバッグ：コンポーネント接続確認")]
    public void DebugCheckComponents()
    {
        Log("=== コンポーネント接続確認 ===");
        Log($"effectListParent: {(effectListParent != null ? "接続済み" : "未接続")}");
        Log($"effectSlotPrefab: {(effectSlotPrefab != null ? "接続済み" : "未接続")}");

        if (effectSlotPrefab != null)
        {
            var slotComponent = effectSlotPrefab.GetComponent<StatusEffectSlot>();
            Log($"StatusEffectSlotコンポーネント: {(slotComponent != null ? "存在" : "なし")}");
        }

        Log("=============================");
    }

    #endregion
}