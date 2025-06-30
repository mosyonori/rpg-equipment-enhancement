using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// シーン遷移時のUI演出制御
/// 左→右の黒パネルスライド演出を管理
/// </summary>
public class TransitionUIController : MonoBehaviour
{
    #region UI References

    [Header("遷移UI要素")]
    [SerializeField] private GameObject transitionCanvas;
    [SerializeField] private RectTransform blackPanel;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Image loadingIcon;

    [Header("遷移設定")]
    [SerializeField] private float slideSpeed = 1000f; // パネル移動速度（pixels/sec）
    [SerializeField] private float minDisplayTime = 0.25f; // 最小表示時間（秒）- デフォルトを半分に

    [Header("テキスト表示設定")]
    [SerializeField] private float textFadeInTime = 0.2f; // テキストフェードイン時間
    [SerializeField] private float textDisplayTime = 0.3f; // テキスト表示時間
    [SerializeField] private float textFadeOutTime = 0.2f; // テキストフェードアウト時間

    [Header("アニメーション設定")]
    [SerializeField] private AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool enableTextAnimation = true; // テキストアニメーション有効/無効

    #endregion

    #region Private Fields

    private bool isTransitioning = false;
    private Canvas transitionCanvasComponent;
    private CanvasGroup loadingTextCanvasGroup;

    #endregion

    #region Singleton Pattern

    private static TransitionUIController _instance;
    public static TransitionUIController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<TransitionUIController>();
                if (_instance == null)
                {
                    var go = new GameObject("TransitionUIController");
                    _instance = go.AddComponent<TransitionUIController>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTransitionUI();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// 遷移UI初期化
    /// </summary>
    private void InitializeTransitionUI()
    {
        // Canvas コンポーネントを取得または作成
        if (transitionCanvas != null)
        {
            transitionCanvasComponent = transitionCanvas.GetComponent<Canvas>();
            if (transitionCanvasComponent == null)
            {
                transitionCanvasComponent = transitionCanvas.AddComponent<Canvas>();
                transitionCanvasComponent.sortingOrder = 1000; // 最前面に表示
                transitionCanvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }

        // テキストアニメーション用のCanvasGroupを設定
        if (loadingText != null && enableTextAnimation)
        {
            loadingTextCanvasGroup = loadingText.GetComponent<CanvasGroup>();
            if (loadingTextCanvasGroup == null)
            {
                loadingTextCanvasGroup = loadingText.gameObject.AddComponent<CanvasGroup>();
            }
        }

        // 初期状態では非表示
        HideTransitionUI();

        Debug.Log("[TransitionUIController] 遷移UI初期化完了");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 遷移演出を開始（左から黒パネルがスライドイン）
    /// </summary>
    /// <param name="targetSceneName">遷移先シーン名</param>
    /// <param name="onTransitionComplete">遷移完了時のコールバック</param>
    public void StartTransition(string targetSceneName, System.Action onTransitionComplete = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[TransitionUIController] 既に遷移中です");
            return;
        }

        StartCoroutine(TransitionCoroutine(targetSceneName, onTransitionComplete));
    }

    /// <summary>
    /// 遷移演出を終了（黒パネルが右にスライドアウト）
    /// </summary>
    /// <param name="onComplete">完了時のコールバック</param>
    public void EndTransition(System.Action onComplete = null)
    {
        if (!isTransitioning)
        {
            Debug.LogWarning("[TransitionUIController] 遷移中ではありません");
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(SlideOutCoroutine(onComplete));
    }

    /// <summary>
    /// 遷移中かどうか
    /// </summary>
    public bool IsTransitioning => isTransitioning;

    #endregion

    #region Private Methods - Transition Control

    /// <summary>
    /// 遷移処理のメインコルーチン
    /// </summary>
    private IEnumerator TransitionCoroutine(string targetSceneName, System.Action onTransitionComplete)
    {
        isTransitioning = true;

        // ローディングテキスト設定
        UpdateLoadingText($"{SceneNames.GetDisplayName(targetSceneName)}に移動中...");

        // 1. スライドイン（左から画面を覆う）
        yield return StartCoroutine(SlideInCoroutine());

        // 2. 最小表示時間待機
        yield return new WaitForSeconds(minDisplayTime);

        // 3. 遷移完了を通知（実際のシーン切り替えはSceneTransitionManagerが行う）
        onTransitionComplete?.Invoke();
    }

    /// <summary>
    /// スライドイン演出（左から右へパネルが移動）
    /// </summary>
    private IEnumerator SlideInCoroutine()
    {
        ShowTransitionUI();

        if (blackPanel == null)
        {
            Debug.LogError("[TransitionUIController] blackPanelが設定されていません");
            yield break;
        }

        // 開始位置：画面左端の外側
        Vector2 startPos = new Vector2(-Screen.width, 0);
        // 終了位置：画面中央
        Vector2 endPos = Vector2.zero;

        blackPanel.anchoredPosition = startPos;

        // テキストフェードイン開始
        if (enableTextAnimation && loadingTextCanvasGroup != null)
        {
            StartCoroutine(FadeInText());
        }

        float elapsed = 0f;
        float duration = Screen.width / slideSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // アニメーションカーブを適用
            float curveValue = slideInCurve.Evaluate(progress);
            blackPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, curveValue);

            yield return null;
        }

        blackPanel.anchoredPosition = endPos;

        // テキスト表示時間
        if (enableTextAnimation)
        {
            yield return new WaitForSeconds(textDisplayTime);
        }
    }

    /// <summary>
    /// スライドアウト演出（左から右へパネルが移動して消える）
    /// </summary>
    private IEnumerator SlideOutCoroutine(System.Action onComplete)
    {
        if (blackPanel == null)
        {
            Debug.LogError("[TransitionUIController] blackPanelが設定されていません");
            onComplete?.Invoke();
            yield break;
        }

        // テキストフェードアウト開始
        if (enableTextAnimation && loadingTextCanvasGroup != null)
        {
            StartCoroutine(FadeOutText());
        }

        // 開始位置：画面中央
        Vector2 startPos = Vector2.zero;
        // 終了位置：画面右端の外側
        Vector2 endPos = new Vector2(Screen.width, 0);

        blackPanel.anchoredPosition = startPos;

        float elapsed = 0f;
        float duration = Screen.width / slideSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // アニメーションカーブを適用
            float curveValue = slideOutCurve.Evaluate(progress);
            blackPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, curveValue);

            yield return null;
        }

        blackPanel.anchoredPosition = endPos;

        // 演出完了
        HideTransitionUI();
        isTransitioning = false;
        onComplete?.Invoke();
    }

    /// <summary>
    /// テキストフェードイン
    /// </summary>
    private IEnumerator FadeInText()
    {
        if (loadingTextCanvasGroup == null) yield break;

        loadingTextCanvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < textFadeInTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / textFadeInTime;
            loadingTextCanvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
            yield return null;
        }

        loadingTextCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// テキストフェードアウト
    /// </summary>
    private IEnumerator FadeOutText()
    {
        if (loadingTextCanvasGroup == null) yield break;

        loadingTextCanvasGroup.alpha = 1f;
        float elapsed = 0f;

        while (elapsed < textFadeOutTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / textFadeOutTime;
            loadingTextCanvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
            yield return null;
        }

        loadingTextCanvasGroup.alpha = 0f;
    }

    #endregion

    #region Private Methods - UI Control

    /// <summary>
    /// 遷移UIを表示
    /// </summary>
    private void ShowTransitionUI()
    {
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(true);
        }
    }

    /// <summary>
    /// 遷移UIを非表示
    /// </summary>
    private void HideTransitionUI()
    {
        if (transitionCanvas != null)
        {
            transitionCanvas.SetActive(false);
        }
    }

    /// <summary>
    /// ローディングテキストを更新
    /// </summary>
    private void UpdateLoadingText(string text)
    {
        if (loadingText != null)
        {
            loadingText.text = text;
        }
    }

    #endregion

    #region Public Properties - Inspector Settings

    /// <summary>
    /// スライド速度設定（Inspector用）
    /// </summary>
    public float SlideSpeed
    {
        get => slideSpeed;
        set => slideSpeed = Mathf.Max(100f, value); // 最小値100
    }

    /// <summary>
    /// 最小表示時間設定（Inspector用）
    /// </summary>
    public float MinDisplayTime
    {
        get => minDisplayTime;
        set => minDisplayTime = Mathf.Max(0.1f, value); // 最小値0.1秒
    }

    /// <summary>
    /// テキストフェードイン時間（Inspector用）
    /// </summary>
    public float TextFadeInTime
    {
        get => textFadeInTime;
        set => textFadeInTime = Mathf.Max(0.05f, value);
    }

    /// <summary>
    /// テキスト表示時間（Inspector用）
    /// </summary>
    public float TextDisplayTime
    {
        get => textDisplayTime;
        set => textDisplayTime = Mathf.Max(0.1f, value);
    }

    /// <summary>
    /// テキストフェードアウト時間（Inspector用）
    /// </summary>
    public float TextFadeOutTime
    {
        get => textFadeOutTime;
        set => textFadeOutTime = Mathf.Max(0.05f, value);
    }

    #endregion

    #region Inspector Context Menu

#if UNITY_EDITOR
    [ContextMenu("遷移演出テスト")]
    private void TestTransition()
    {
        if (Application.isPlaying)
        {
            StartTransition("TestScene", () => {
                Debug.Log("遷移演出テスト完了");
                EndTransition(() => Debug.Log("スライドアウト完了"));
            });
        }
    }

    [ContextMenu("設定をデフォルト値にリセット")]
    private void ResetToDefaultSettings()
    {
        slideSpeed = 1000f;
        minDisplayTime = 0.25f;
        textFadeInTime = 0.2f;
        textDisplayTime = 0.3f;
        textFadeOutTime = 0.2f;
        enableTextAnimation = true;

        Debug.Log("TransitionUIController設定をデフォルト値にリセットしました");
    }

    [ContextMenu("高速設定（開発用）")]
    private void SetFastSettings()
    {
        slideSpeed = 2000f;
        minDisplayTime = 0.1f;
        textFadeInTime = 0.1f;
        textDisplayTime = 0.1f;
        textFadeOutTime = 0.1f;

        Debug.Log("TransitionUIController設定を高速モードに変更しました");
    }
#endif

    #endregion

    #region Validation

    /// <summary>
    /// 設定の妥当性チェック
    /// </summary>
    private void OnValidate()
    {
        // 最小値制限
        slideSpeed = Mathf.Max(100f, slideSpeed);
        minDisplayTime = Mathf.Max(0.1f, minDisplayTime);
        textFadeInTime = Mathf.Max(0.05f, textFadeInTime);
        textDisplayTime = Mathf.Max(0.1f, textDisplayTime);
        textFadeOutTime = Mathf.Max(0.05f, textFadeOutTime);

        // エディタでの設定変更時に自動適用
        if (Application.isPlaying && isInitialized)
        {
            // 実行時設定変更への対応（必要に応じて）
        }
    }

    private bool isInitialized = false;
    private void Start()
    {
        isInitialized = true;
    }

    #endregion
}