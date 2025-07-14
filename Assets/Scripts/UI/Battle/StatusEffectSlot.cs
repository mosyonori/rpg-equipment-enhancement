using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 個別の状態効果スロット制御クラス
/// 責任範囲：
/// - 状態効果アイコン表示
/// - 残りターン数表示
/// - バフ・デバフの色分け表示
/// - StatusEffectData との連携
/// データアクセス統一ルール: UI層指定用コンポーネント（StatusEffectDataを受け取り表示のみ）
/// </summary>
public class StatusEffectSlot : MonoBehaviour
{
    #region デバッグ・ログ

    /// <summary>
    /// デバッグログ出力
    /// </summary>
    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[StatusEffectSlot] {message}");
        }
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    private void LogError(string message)
    {
        Debug.LogError($"[StatusEffectSlot] {message}");
    }

    /// <summary>
    /// 警告ログ出力
    /// </summary>
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[StatusEffectSlot] {message}");
    }

    #endregion

    #region フィールド

    [Header("スロットコンポーネント")]
    [SerializeField] private Image effectIcon;           // 効果アイコン表示用
    [SerializeField] private Image effectBackground;     // 背景色表示用
    [SerializeField] private TextMeshProUGUI turnCountText; // ターン数テキスト

    [Header("表示設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showTurnCount = true;

    [Header("デフォルト色設定")]
    [SerializeField] private Color defaultBuffColor = new Color(1f, 0.4f, 0.3f);
    [SerializeField] private Color defaultDebuffColor = new Color(0.25f, 0.41f, 0.88f);

    // 内部状態
    private StatusEffectData currentEffectData;
    private bool isInitialized = false;
    private Color currentBackgroundColor;
    private Sprite currentIconSprite;

    #endregion

    #region プロパティ

    /// <summary>
    /// 初期化完了状態
    /// </summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// 現在の効果データ
    /// </summary>
    public StatusEffectData CurrentEffectData => currentEffectData;

    /// <summary>
    /// 残りターン数
    /// </summary>
    public int RemainingTurns => currentEffectData?.remainingTurns ?? 0;

    /// <summary>
    /// 効果がアクティブか
    /// </summary>
    public bool IsActive => currentEffectData?.IsActive() ?? false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        Log("StatusEffectSlot Awake開始");
        ValidateComponents();
    }

    private void Start()
    {
        Log("StatusEffectSlot Start開始");
        InitializeSlot();
    }

    private void OnDestroy()
    {
        Log("StatusEffectSlot OnDestroy開始");
        CleanupSlot();
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
            if (effectBackground == null)
            {
                LogError("effectBackgroundが設定されていません");
                return;
            }

            if (turnCountText == null)
            {
                LogError("turnCountTextが設定されていません");
                return;
            }

            // オプショナルコンポーネント確認
            if (effectIcon == null)
            {
                LogWarning("effectIconが設定されていません（アイコン表示は無効になります）");
            }

            Log("コンポーネント検証完了");
        }
        catch (Exception e)
        {
            LogError($"コンポーネント検証エラー: {e.Message}");
        }
    }

    /// <summary>
    /// スロット初期化
    /// </summary>
    private void InitializeSlot()
    {
        try
        {
            Log("StatusEffectSlot初期化開始");

            // 初期状態設定
            SetVisible(false);

            // デフォルト色設定
            currentBackgroundColor = defaultDebuffColor;

            isInitialized = true;
            Log("StatusEffectSlot初期化完了");
        }
        catch (Exception e)
        {
            LogError($"StatusEffectSlot初期化エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - データ設定

    /// <summary>
    /// 効果データ設定（StatusEffectUIから呼び出される）
    /// </summary>
    /// <param name="effectData">状態効果データ</param>
    /// <param name="effectColor">効果の色</param>
    public void SetEffectData(StatusEffectData effectData, Color effectColor)
    {
        if (effectData == null)
        {
            LogError("StatusEffectDataがnullです");
            return;
        }

        try
        {
            currentEffectData = effectData;
            currentBackgroundColor = effectColor;

            // UI更新
            UpdateEffectDisplay();

            // 表示状態設定
            SetVisible(true);

            Log($"効果データ設定: {effectData.effectName} (残り{effectData.remainingTurns}ターン)");
        }
        catch (Exception e)
        {
            LogError($"効果データ設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ターン数更新（StatusEffectUIから呼び出される）
    /// </summary>
    /// <param name="remainingTurns">残りターン数</param>
    public void UpdateTurnCount(int remainingTurns)
    {
        try
        {
            if (currentEffectData != null)
            {
                currentEffectData.remainingTurns = remainingTurns;
            }

            // ターン数表示更新
            UpdateTurnCountDisplay();

            Log($"ターン数更新: {remainingTurns}");
        }
        catch (Exception e)
        {
            LogError($"ターン数更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 効果データクリア
    /// </summary>
    public void ClearEffectData()
    {
        try
        {
            currentEffectData = null;
            currentIconSprite = null;

            // 表示状態設定
            SetVisible(false);

            Log("効果データクリア完了");
        }
        catch (Exception e)
        {
            LogError($"効果データクリアエラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - 表示制御

    /// <summary>
    /// スロット表示切替
    /// </summary>
    /// <param name="visible">表示するか</param>
    public void SetVisible(bool visible)
    {
        try
        {
            gameObject.SetActive(visible);
            Log($"StatusEffectSlot表示切替: {visible}");
        }
        catch (Exception e)
        {
            LogError($"StatusEffectSlot表示切替エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ターン数表示設定
    /// </summary>
    /// <param name="show">ターン数を表示するか</param>
    public void SetShowTurnCount(bool show)
    {
        try
        {
            showTurnCount = show;
            UpdateTurnCountDisplay();

            Log($"ターン数表示設定: {show}");
        }
        catch (Exception e)
        {
            LogError($"ターン数表示設定エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - 表示更新

    /// <summary>
    /// 効果表示更新
    /// </summary>
    private void UpdateEffectDisplay()
    {
        try
        {
            if (currentEffectData == null) return;

            // 背景色設定
            UpdateBackgroundColor();

            // アイコン設定
            UpdateEffectIcon();

            // ターン数表示更新
            UpdateTurnCountDisplay();

            Log($"効果表示更新完了: {currentEffectData.effectName}");
        }
        catch (Exception e)
        {
            LogError($"効果表示更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 背景色更新
    /// </summary>
    private void UpdateBackgroundColor()
    {
        try
        {
            if (effectBackground != null)
            {
                effectBackground.color = currentBackgroundColor;
                Log($"背景色設定: {currentBackgroundColor}");
            }
        }
        catch (Exception e)
        {
            LogError($"背景色更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 効果アイコン更新
    /// </summary>
    private void UpdateEffectIcon()
    {
        try
        {
            if (effectIcon == null) return;

            if (currentEffectData != null && !string.IsNullOrEmpty(currentEffectData.iconPath))
            {
                // アイコンパスからスプライト読み込み
                LoadEffectIcon(currentEffectData.iconPath);
            }
            else
            {
                // デフォルトアイコンまたは非表示
                effectIcon.gameObject.SetActive(false);
                Log("アイコンパスが設定されていないため、アイコンを非表示にしました");
            }
        }
        catch (Exception e)
        {
            LogError($"効果アイコン更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ターン数表示更新
    /// </summary>
    private void UpdateTurnCountDisplay()
    {
        try
        {
            if (turnCountText == null) return;

            if (showTurnCount && currentEffectData != null)
            {
                turnCountText.text = currentEffectData.remainingTurns.ToString();
                turnCountText.gameObject.SetActive(true);
            }
            else
            {
                turnCountText.gameObject.SetActive(false);
            }

            Log($"ターン数表示更新: {currentEffectData?.remainingTurns ?? 0}");
        }
        catch (Exception e)
        {
            LogError($"ターン数表示更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 効果アイコン読み込み
    /// </summary>
    /// <param name="iconPath">アイコンパス</param>
    private void LoadEffectIcon(string iconPath)
    {
        try
        {
            if (effectIcon == null) return;

            // Resourcesからスプライト読み込み
            Sprite iconSprite = Resources.Load<Sprite>(iconPath);

            if (iconSprite != null)
            {
                effectIcon.sprite = iconSprite;
                effectIcon.gameObject.SetActive(true);
                currentIconSprite = iconSprite;

                Log($"アイコン読み込み成功: {iconPath}");
            }
            else
            {
                LogWarning($"アイコンが見つかりません: {iconPath}");
                effectIcon.gameObject.SetActive(false);
            }
        }
        catch (Exception e)
        {
            LogError($"アイコン読み込みエラー: {e.Message}");
            if (effectIcon != null)
            {
                effectIcon.gameObject.SetActive(false);
            }
        }
    }

    #endregion

    #region 内部メソッド - 色判定

    /// <summary>
    /// バフ効果かどうか判定
    /// </summary>
    /// <param name="effectData">状態効果データ</param>
    /// <returns>バフ効果かどうか</returns>
    private bool IsBuffEffect(StatusEffectData effectData)
    {
        try
        {
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
    /// 効果の色取得
    /// </summary>
    /// <param name="effectData">状態効果データ</param>
    /// <returns>効果の色</returns>
    private Color GetEffectColor(StatusEffectData effectData)
    {
        try
        {
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

    #endregion

    #region クリーンアップ

    /// <summary>
    /// StatusEffectSlotクリーンアップ
    /// </summary>
    private void CleanupSlot()
    {
        try
        {
            // データクリア
            currentEffectData = null;
            currentIconSprite = null;

            Log("StatusEffectSlotクリーンアップ完了");
        }
        catch (Exception e)
        {
            LogError($"StatusEffectSlotクリーンアップエラー: {e.Message}");
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
        Log("=== StatusEffectSlot状態情報 ===");
        Log($"初期化完了: {isInitialized}");
        Log($"アクティブ: {gameObject.activeInHierarchy}");
        Log($"ターン数表示: {showTurnCount}");

        if (currentEffectData != null)
        {
            Log($"効果名: {currentEffectData.effectName}");
            Log($"効果ID: {currentEffectData.effectId}");
            Log($"残りターン: {currentEffectData.remainingTurns}");
            Log($"効果タイプ: {currentEffectData.effectType}");
            Log($"ポジティブ: {currentEffectData.isPositive}");
        }
        else
        {
            Log("現在の効果データ: なし");
        }

        Log($"背景色: {currentBackgroundColor}");
        Log($"アイコンスプライト: {(currentIconSprite != null ? currentIconSprite.name : "なし")}");
        Log("==============================");
    }

    /// <summary>
    /// デバッグ用：テスト効果設定
    /// </summary>
    [ContextMenu("デバッグ：テスト効果設定")]
    public void DebugSetTestEffect()
    {
        Log("デバッグ：テスト効果設定実行");

        var testEffect = new StatusEffectData
        {
            effectId = 999,
            effectName = "テスト効果",
            remainingTurns = 5,
            effectType = StatusEffectType.AttackUp,
            isPositive = true,
            colorCode = "#ff6347",
            offenseMultiplier = 1.5f
        };

        SetEffectData(testEffect, Color.red);
    }

    /// <summary>
    /// デバッグ用：コンポーネント接続確認
    /// </summary>
    [ContextMenu("デバッグ：コンポーネント接続確認")]
    public void DebugCheckComponents()
    {
        Log("=== コンポーネント接続確認 ===");
        Log($"effectIcon: {(effectIcon != null ? "接続済み" : "未接続")}");
        Log($"effectBackground: {(effectBackground != null ? "接続済み" : "未接続")}");
        Log($"turnCountText: {(turnCountText != null ? "接続済み" : "未接続")}");
        Log("=============================");
    }

    #endregion
}