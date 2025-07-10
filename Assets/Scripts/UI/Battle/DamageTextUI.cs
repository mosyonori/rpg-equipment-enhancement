using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ダメージ数値の表示演出UI制御
/// 役割：ダメージ数値ポップアップ・クリティカル時の特別演出・数値色分け表示
/// 機能：ダメージ数値ポップアップ、クリティカル演出、回復・無効化演出、複数同時表示管理
/// データアクセス統一ルール: UI層 → Manager層 → データ層
/// </summary>
public class DamageTextUI : MonoBehaviour
{
    [Header("ダメージテキストプレハブ")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private GameObject criticalDamageTextPrefab;
    [SerializeField] private GameObject healTextPrefab;
    [SerializeField] private GameObject nullifyTextPrefab;

    [Header("表示位置設定")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private float yOffset = 1.0f;
    [SerializeField] private float randomXRange = 0.5f;
    [SerializeField] private float randomYRange = 0.3f;

    [Header("アニメーション設定")]
    [SerializeField] private float popupDuration = 1.2f;
    [SerializeField] private float moveDistance = 100f;
    [SerializeField] private AnimationCurve popupEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve fadeEasing = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("クリティカル専用設定")]
    [SerializeField] private float criticalScaleMultiplier = 1.5f;
    [SerializeField] private float criticalDuration = 1.5f;
    [SerializeField] private float criticalShakeStrength = 5f;
    [SerializeField] private int criticalShakeCount = 3;

    [Header("色設定")]
    [SerializeField] private Color normalDamageColor = Color.white;
    [SerializeField] private Color criticalDamageColor = Color.red;
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private Color nullifyColor = Color.blue;
    [SerializeField] private Color superEffectiveColor = Color.yellow;
    [SerializeField] private Color notVeryEffectiveColor = Color.gray;

    [Header("フォント設定")]
    [SerializeField] private int normalFontSize = 24;
    [SerializeField] private int criticalFontSize = 36;
    [SerializeField] private int healFontSize = 20;
    [SerializeField] private int nullifyFontSize = 18;

    [Header("多重表示制御")]
    [SerializeField] private int maxSimultaneousTexts = 5;
    [SerializeField] private float textSpacing = 0.3f;

    // イベント
    public static event Action<DamageData> OnDamageTextShown;

    // 内部状態
    private bool isInitialized = false;
    private List<DamageTextInstance> activeDamageTexts;
    private Queue<Vector3> availablePositions;

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
        ClearAllDamageTexts();
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
            Log("DamageTextUI初期化開始");

            // コレクション初期化
            InitializeCollections();

            // ワールドキャンバス設定
            SetupWorldCanvas();

            // 利用可能位置の初期化
            InitializeAvailablePositions();

            isInitialized = true;
            Log("DamageTextUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"DamageTextUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// コレクション初期化
    /// </summary>
    private void InitializeCollections()
    {
        activeDamageTexts = new List<DamageTextInstance>();
        availablePositions = new Queue<Vector3>();
    }

    /// <summary>
    /// コンポーネント検証
    /// </summary>
    private void ValidateComponents()
    {
        if (damageTextPrefab == null)
            LogWarning("damageTextPrefabが設定されていません");

        if (worldCanvas == null)
        {
            worldCanvas = FindFirstObjectByType<Canvas>();
            if (worldCanvas == null)
                LogWarning("worldCanvasが見つかりません");
        }
    }

    /// <summary>
    /// ワールドキャンバス設定
    /// </summary>
    private void SetupWorldCanvas()
    {
        if (worldCanvas == null) return;

        // キャンバス設定の確認・調整
        if (worldCanvas.renderMode == RenderMode.WorldSpace)
        {
            Log("ワールドスペースキャンバス設定確認済み");
        }
    }

    /// <summary>
    /// 利用可能位置の初期化
    /// </summary>
    private void InitializeAvailablePositions()
    {
        availablePositions.Clear();

        // 複数の表示位置を事前計算
        for (int i = 0; i < maxSimultaneousTexts; i++)
        {
            float yPos = i * textSpacing;
            availablePositions.Enqueue(new Vector3(0f, yPos, 0f));
        }
    }

    #endregion

    #region 公開メソッド - ダメージテキスト表示

    /// <summary>
    /// ダメージテキストを表示
    /// </summary>
    /// <param name="damageData">ダメージデータ</param>
    /// <param name="worldPosition">ワールド座標での表示位置</param>
    public void ShowDamageText(DamageData damageData, Vector3 worldPosition)
    {
        if (!isInitialized) Initialize();

        if (damageData == null)
        {
            LogWarning("無効なダメージデータでの表示要求");
            return;
        }

        try
        {
            // 表示位置の計算
            Vector3 displayPosition = CalculateDisplayPosition(worldPosition);

            // ダメージタイプに応じたテキスト生成
            GameObject textInstance = CreateDamageTextInstance(damageData, displayPosition);

            if (textInstance != null)
            {
                // アニメーション開始
                StartCoroutine(PlayDamageTextAnimation(textInstance, damageData));

                // イベント発行
                OnDamageTextShown?.Invoke(damageData);

                Log($"ダメージテキスト表示: {damageData.targetName} - {damageData.finalDamage}");
            }
        }
        catch (Exception e)
        {
            LogError($"ダメージテキスト表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 複数ダメージの同時表示
    /// </summary>
    /// <param name="damageDataList">ダメージデータリスト</param>
    /// <param name="baseWorldPosition">基準ワールド座標</param>
    public void ShowMultipleDamageTexts(List<DamageData> damageDataList, Vector3 baseWorldPosition)
    {
        if (damageDataList == null || damageDataList.Count == 0) return;

        try
        {
            for (int i = 0; i < damageDataList.Count; i++)
            {
                var damageData = damageDataList[i];

                // 各ダメージに対して少しずつ位置をずらして表示
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-randomXRange, randomXRange),
                    i * textSpacing,
                    0f
                );

                Vector3 displayPosition = baseWorldPosition + offset;

                // 少し時間差をつけて表示
                StartCoroutine(DelayedDamageTextShow(damageData, displayPosition, i * 0.1f));
            }

            Log($"複数ダメージテキスト表示: {damageDataList.Count}個");
        }
        catch (Exception e)
        {
            LogError($"複数ダメージテキスト表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 全てのダメージテキストをクリア
    /// </summary>
    public void ClearAllDamageTexts()
    {
        try
        {
            StopAllCoroutines();

            foreach (var textInstance in activeDamageTexts)
            {
                if (textInstance.gameObject != null)
                {
                    DestroyImmediate(textInstance.gameObject);
                }
            }

            activeDamageTexts.Clear();
            InitializeAvailablePositions();

            Log("全ダメージテキストクリア完了");
        }
        catch (Exception e)
        {
            LogError($"ダメージテキストクリアエラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - テキスト生成

    /// <summary>
    /// ダメージテキストインスタンス作成
    /// </summary>
    private GameObject CreateDamageTextInstance(DamageData damageData, Vector3 position)
    {
        GameObject prefab = GetAppropriateTextPrefab(damageData);
        if (prefab == null || worldCanvas == null) return null;

        try
        {
            GameObject instance = Instantiate(prefab, worldCanvas.transform);
            instance.transform.position = position;

            // テキスト内容設定
            var textComponent = instance.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                SetupDamageText(textComponent, damageData);
            }

            // インスタンス管理に追加
            var damageTextInstance = new DamageTextInstance
            {
                gameObject = instance,
                damageData = damageData,
                startTime = Time.time
            };
            activeDamageTexts.Add(damageTextInstance);

            return instance;
        }
        catch (Exception e)
        {
            LogError($"ダメージテキストインスタンス作成エラー: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 適切なテキストプレハブを取得
    /// </summary>
    private GameObject GetAppropriateTextPrefab(DamageData damageData)
    {
        if (damageData.IsHealing() && healTextPrefab != null)
            return healTextPrefab;
        else if (damageData.IsNullified() && nullifyTextPrefab != null)
            return nullifyTextPrefab;
        else if (damageData.isCritical && criticalDamageTextPrefab != null)
            return criticalDamageTextPrefab;
        else
            return damageTextPrefab;
    }

    /// <summary>
    /// ダメージテキストの内容設定
    /// </summary>
    private void SetupDamageText(TextMeshProUGUI textComponent, DamageData damageData)
    {
        if (textComponent == null) return;

        try
        {
            // テキスト内容設定
            string displayText = GetDamageDisplayText(damageData);
            textComponent.text = displayText;

            // 色設定
            Color textColor = GetDamageTextColor(damageData);
            textComponent.color = textColor;

            // フォントサイズ設定
            int fontSize = GetDamageFontSize(damageData);
            textComponent.fontSize = fontSize;

            // フォントスタイル設定
            if (damageData.isCritical)
            {
                textComponent.fontStyle = FontStyles.Bold;
            }
        }
        catch (Exception e)
        {
            LogError($"ダメージテキスト設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ダメージ表示テキスト取得
    /// </summary>
    private string GetDamageDisplayText(DamageData damageData)
    {
        if (damageData.IsHealing())
        {
            return $"+{Mathf.Abs(damageData.finalDamage)}";
        }
        else if (damageData.IsNullified())
        {
            return "無効化";
        }
        else if (damageData.finalDamage == 0)
        {
            return "MISS";
        }
        else
        {
            string baseText = damageData.finalDamage.ToString();

            if (damageData.isCritical)
            {
                baseText = $"CRITICAL!\n{baseText}";
            }

            // 属性効果表示
            if (damageData.effectiveness == DamageEffectiveness.SuperEffective)
            {
                baseText += "\n効果抜群！";
            }
            else if (damageData.effectiveness == DamageEffectiveness.NotVeryEffective)
            {
                baseText += "\n効果いまひとつ...";
            }

            return baseText;
        }
    }

    /// <summary>
    /// ダメージテキスト色取得
    /// </summary>
    private Color GetDamageTextColor(DamageData damageData)
    {
        if (damageData.IsHealing())
            return healColor;
        else if (damageData.IsNullified())
            return nullifyColor;
        else if (damageData.isCritical)
            return criticalDamageColor;
        else if (damageData.effectiveness == DamageEffectiveness.SuperEffective)
            return superEffectiveColor;
        else if (damageData.effectiveness == DamageEffectiveness.NotVeryEffective)
            return notVeryEffectiveColor;
        else
            return normalDamageColor;
    }

    /// <summary>
    /// ダメージフォントサイズ取得
    /// </summary>
    private int GetDamageFontSize(DamageData damageData)
    {
        if (damageData.IsHealing())
            return healFontSize;
        else if (damageData.IsNullified())
            return nullifyFontSize;
        else if (damageData.isCritical)
            return criticalFontSize;
        else
            return normalFontSize;
    }

    #endregion

    #region 内部メソッド - 位置計算

    /// <summary>
    /// 表示位置計算
    /// </summary>
    private Vector3 CalculateDisplayPosition(Vector3 worldPosition)
    {
        Vector3 basePosition = worldPosition + new Vector3(0f, yOffset, 0f);

        // 利用可能位置がある場合は使用
        if (availablePositions.Count > 0)
        {
            Vector3 offset = availablePositions.Dequeue();
            return basePosition + offset;
        }

        // 利用可能位置がない場合はランダムオフセット
        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-randomXRange, randomXRange),
            UnityEngine.Random.Range(-randomYRange, randomYRange),
            0f
        );

        return basePosition + randomOffset;
    }

    #endregion

    #region 内部メソッド - アニメーション

    /// <summary>
    /// ダメージテキストアニメーション実行
    /// </summary>
    private IEnumerator PlayDamageTextAnimation(GameObject textInstance, DamageData damageData)
    {
        if (textInstance == null) yield break;

        Transform textTransform = textInstance.transform;
        Vector3 startPosition = textTransform.position;
        Vector3 targetPosition = startPosition + new Vector3(0f, moveDistance, 0f);

        CanvasGroup canvasGroup = textInstance.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = textInstance.AddComponent<CanvasGroup>();

        Vector3 originalScale = textTransform.localScale;
        float duration = damageData.isCritical ? criticalDuration : popupDuration;

        float elapsed = 0f;

        // クリティカル用の特殊処理
        if (damageData.isCritical)
        {
            yield return StartCoroutine(PlayCriticalEffect(textTransform, canvasGroup));
        }

        // メインアニメーション
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 位置アニメーション
            float moveT = popupEasing.Evaluate(t);
            textTransform.position = Vector3.Lerp(startPosition, targetPosition, moveT);

            // フェードアニメーション
            float fadeT = fadeEasing.Evaluate(t);
            canvasGroup.alpha = fadeT;

            // スケールアニメーション（クリティカル時）
            if (damageData.isCritical)
            {
                float scaleT = Mathf.Sin(t * Mathf.PI);
                float scaleMultiplier = 1f + (criticalScaleMultiplier - 1f) * scaleT;
                textTransform.localScale = originalScale * scaleMultiplier;
            }

            yield return null;
        }

        // アニメーション完了後の処理
        OnDamageTextAnimationComplete(textInstance);
    }

    /// <summary>
    /// クリティカル専用エフェクト
    /// </summary>
    private IEnumerator PlayCriticalEffect(Transform textTransform, CanvasGroup canvasGroup)
    {
        Vector3 originalPosition = textTransform.position;

        // シェイクエフェクト
        for (int i = 0; i < criticalShakeCount; i++)
        {
            Vector3 shakeOffset = new Vector3(
                UnityEngine.Random.Range(-criticalShakeStrength, criticalShakeStrength),
                UnityEngine.Random.Range(-criticalShakeStrength, criticalShakeStrength),
                0f
            );

            textTransform.position = originalPosition + shakeOffset;
            yield return new WaitForSeconds(0.05f);

            textTransform.position = originalPosition;
            yield return new WaitForSeconds(0.05f);
        }
    }

    /// <summary>
    /// 遅延ダメージテキスト表示
    /// </summary>
    private IEnumerator DelayedDamageTextShow(DamageData damageData, Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowDamageText(damageData, position);
    }

    /// <summary>
    /// ダメージテキストアニメーション完了処理
    /// </summary>
    private void OnDamageTextAnimationComplete(GameObject textInstance)
    {
        try
        {
            // アクティブリストから削除
            var instanceToRemove = activeDamageTexts.Find(x => x.gameObject == textInstance);
            if (instanceToRemove != null)
            {
                activeDamageTexts.Remove(instanceToRemove);
            }

            // 位置を利用可能位置に戻す
            if (availablePositions.Count < maxSimultaneousTexts)
            {
                availablePositions.Enqueue(Vector3.zero); // 簡略化
            }

            // オブジェクト削除
            DestroyImmediate(textInstance);
        }
        catch (Exception e)
        {
            LogError($"ダメージテキスト完了処理エラー: {e.Message}");
        }
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        Debug.Log($"[DamageTextUI] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[DamageTextUI] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[DamageTextUI] {message}");
    }

    #endregion

    #region エディタ用ツール

#if UNITY_EDITOR
    [ContextMenu("ダメージテキストテスト")]
    private void TestDamageText()
    {
        // テスト用のダメージデータ作成
        var testDamage = new DamageData
        {
            targetName = "テストターゲット",
            finalDamage = 999,
            isCritical = true,
            effectiveness = DamageEffectiveness.SuperEffective
        };

        Vector3 testPosition = transform.position;
        ShowDamageText(testDamage, testPosition);

        Log("ダメージテキストテスト実行");
    }

    [ContextMenu("回復テキストテスト")]
    private void TestHealText()
    {
        var testHeal = new DamageData
        {
            targetName = "テストターゲット",
            finalDamage = -150 // 負の値で回復
        };

        Vector3 testPosition = transform.position;
        ShowDamageText(testHeal, testPosition);

        Log("回復テキストテスト実行");
    }

    [ContextMenu("無効化テキストテスト")]
    private void TestNullifyText()
    {
        var testNullify = new DamageData
        {
            targetName = "テストターゲット",
            baseDamage = 100,
            finalDamage = 0 // 基本ダメージあり、最終ダメージ0で無効化
        };

        Vector3 testPosition = transform.position;
        ShowDamageText(testNullify, testPosition);

        Log("無効化テキストテスト実行");
    }
#endif

    #endregion
}

/// <summary>
/// ダメージテキストインスタンス管理用クラス
/// </summary>
[System.Serializable]
public class DamageTextInstance
{
    public GameObject gameObject;
    public DamageData damageData;
    public float startTime;
}