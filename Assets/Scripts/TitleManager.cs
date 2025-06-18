using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class TitleManager : MonoBehaviour
{
    [Header("UI要素")]
    public Image titleLogo;                 // タイトルロゴ画像
    public Button settingsButton;           // 設定ボタン
    public Button startButton;              // STARTボタン
    public Image characterImage;            // キャラクター画像（表示のみ）
    public GameObject[] decorationImages;   // 装飾画像配列（表示のみ）

    [Header("背景・タップエリア")]
    public GameObject backgroundTapArea;    // 背景タップエリア
    public CanvasGroup mainUI;

    [Header("暗転用UI")]
    public Image fadeOverlay;               // 暗転用のオーバーレイ画像（黒い画像）
    public CanvasGroup fadeCanvasGroup;     // 暗転用のCanvasGroup

    [Header("アニメーション設定")]
    public float titleFadeInDuration = 2.0f;   // タイトルフェードイン時間
    [Space]
    [Header("STARTボタン点滅設定")]
    public bool enableStartButtonBlink = true;  // STARTボタン点滅の有効/無効
    public float blinkInterval = 1.0f;          // 点滅間隔（秒）
    public float blinkFadeDuration = 0.3f;      // フェードイン/アウト時間
    public float minAlpha = 0.3f;               // 最小透明度
    public float maxAlpha = 1.0f;               // 最大透明度
    [Space]
    [Header("画面遷移設定")]
    public float fadeToBlackDuration = 1.0f;   // 暗転時間

    [Header("音響設定")]
    public AudioSource audioSource;
    public AudioClip titleBGM;
    public AudioClip buttonClickSE;
    public AudioClip tapSE;

    private bool isTransitioning = false;
    private Coroutine startButtonBlinkCoroutine;

    private void Start()
    {
        InitializeTitle();
    }

    /// <summary>
    /// タイトル画面の初期化
    /// </summary>
    private void InitializeTitle()
    {
        // タイトルロゴを非表示に設定
        if (titleLogo != null)
        {
            titleLogo.gameObject.SetActive(false);
        }

        // その他の要素は通常通り表示
        if (characterImage != null)
        {
            characterImage.gameObject.SetActive(true);
        }

        // 装飾画像も表示
        if (decorationImages != null)
        {
            foreach (var decoration in decorationImages)
            {
                if (decoration != null)
                {
                    decoration.SetActive(true);
                }
            }
        }

        // 暗転用UIの初期化
        InitializeFadeOverlay();

        // ボタンイベントの設定
        SetupButtons();

        // 背景タップエリアの設定
        SetupBackgroundTap();

        // BGM再生
        PlayTitleBGM();

        // シンプルなタイトル表示アニメーション開始
        StartCoroutine(ShowTitleLogo());

        Debug.Log("シンプルタイトル画面を初期化しました");
    }

    /// <summary>
    /// 暗転用UIの初期化
    /// </summary>
    private void InitializeFadeOverlay()
    {
        if (fadeOverlay != null)
        {
            // 暗転用画像を黒色に設定
            fadeOverlay.color = Color.black;
            fadeOverlay.gameObject.SetActive(true);
        }

        if (fadeCanvasGroup != null)
        {
            // 初期状態では透明（暗転していない状態）
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(true);
            // レイキャストを無効にして、他のUIの邪魔をしないようにする
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// ボタンイベントの設定
    /// </summary>
    private void SetupButtons()
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
    }

    /// <summary>
    /// 背景タップエリアの設定
    /// </summary>
    private void SetupBackgroundTap()
    {
        if (backgroundTapArea != null)
        {
            // EventTrigger コンポーネントを追加
            EventTrigger trigger = backgroundTapArea.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = backgroundTapArea.AddComponent<EventTrigger>();
            }

            // PointerClick イベントを追加
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { OnBackgroundTapped(); });
            trigger.triggers.Add(entry);

            Debug.Log("背景タップエリアを設定しました");
        }
    }

    /// <summary>
    /// タイトルロゴを上部からフェードインで表示
    /// </summary>
    private IEnumerator ShowTitleLogo()
    {
        if (titleLogo == null) yield break;

        // タイトルロゴを表示状態にして、上部に配置
        titleLogo.gameObject.SetActive(true);

        Vector3 originalPos = titleLogo.transform.localPosition;
        Vector3 startPos = originalPos + new Vector3(0, 200f, 0); // 上部からスタート

        // 初期状態設定
        titleLogo.transform.localPosition = startPos;
        titleLogo.color = new Color(titleLogo.color.r, titleLogo.color.g, titleLogo.color.b, 0f); // 透明

        float elapsed = 0f;
        while (elapsed < titleFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / titleFadeInDuration;

            // 位置の補間（上から下へ）
            titleLogo.transform.localPosition = Vector3.Lerp(startPos, originalPos, t);

            // アルファ値の補間（透明から不透明へ）
            Color currentColor = titleLogo.color;
            currentColor.a = Mathf.Lerp(0f, 1f, t);
            titleLogo.color = currentColor;

            yield return null;
        }

        // 最終状態を確定
        titleLogo.transform.localPosition = originalPos;
        Color finalColor = titleLogo.color;
        finalColor.a = 1f;
        titleLogo.color = finalColor;

        Debug.Log("タイトルロゴのフェードイン完了");

        // タイトルロゴ表示完了後、STARTボタンの点滅を開始
        StartStartButtonBlink();
    }

    #region STARTボタン点滅アニメーション

    /// <summary>
    /// STARTボタンの点滅アニメーション開始
    /// </summary>
    private void StartStartButtonBlink()
    {
        if (!enableStartButtonBlink || startButton == null) return;

        // 既存の点滅コルーチンを停止
        if (startButtonBlinkCoroutine != null)
        {
            StopCoroutine(startButtonBlinkCoroutine);
        }

        // 新しい点滅コルーチンを開始
        startButtonBlinkCoroutine = StartCoroutine(StartButtonBlinkLoop());
        Debug.Log("STARTボタンの点滅アニメーションを開始しました");
    }

    /// <summary>
    /// STARTボタンの点滅アニメーション停止
    /// </summary>
    private void StopStartButtonBlink()
    {
        if (startButtonBlinkCoroutine != null)
        {
            StopCoroutine(startButtonBlinkCoroutine);
            startButtonBlinkCoroutine = null;
        }

        // ボタンを完全に不透明に戻す
        if (startButton != null)
        {
            var buttonImage = startButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color color = buttonImage.color;
                color.a = maxAlpha;
                buttonImage.color = color;
            }

            // ボタンテキストも元に戻す
            var buttonText = startButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                Color textColor = buttonText.color;
                textColor.a = maxAlpha;
                buttonText.color = textColor;
            }
        }

        Debug.Log("STARTボタンの点滅アニメーションを停止しました");
    }

    /// <summary>
    /// STARTボタン点滅のメインループ
    /// </summary>
    private IEnumerator StartButtonBlinkLoop()
    {
        while (true)
        {
            // フェードアウト（明るい → 暗い）
            yield return StartCoroutine(FadeStartButton(maxAlpha, minAlpha, blinkFadeDuration));

            // 暗い状態で少し待機
            yield return new WaitForSeconds(blinkInterval - blinkFadeDuration * 2f);

            // フェードイン（暗い → 明るい）
            yield return StartCoroutine(FadeStartButton(minAlpha, maxAlpha, blinkFadeDuration));

            // 明るい状態で少し待機
            yield return new WaitForSeconds(blinkInterval - blinkFadeDuration * 2f);
        }
    }

    /// <summary>
    /// STARTボタンのフェード処理
    /// </summary>
    private IEnumerator FadeStartButton(float fromAlpha, float toAlpha, float duration)
    {
        if (startButton == null) yield break;

        var buttonImage = startButton.GetComponent<Image>();
        var buttonText = startButton.GetComponentInChildren<TextMeshProUGUI>();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentAlpha = Mathf.Lerp(fromAlpha, toAlpha, t);

            // ボタン画像のアルファ値を変更
            if (buttonImage != null)
            {
                Color imageColor = buttonImage.color;
                imageColor.a = currentAlpha;
                buttonImage.color = imageColor;
            }

            // ボタンテキストのアルファ値を変更
            if (buttonText != null)
            {
                Color textColor = buttonText.color;
                textColor.a = currentAlpha;
                buttonText.color = textColor;
            }

            yield return null;
        }

        // 最終的なアルファ値を確定
        if (buttonImage != null)
        {
            Color finalImageColor = buttonImage.color;
            finalImageColor.a = toAlpha;
            buttonImage.color = finalImageColor;
        }

        if (buttonText != null)
        {
            Color finalTextColor = buttonText.color;
            finalTextColor.a = toAlpha;
            buttonText.color = finalTextColor;
        }
    }

    #endregion

    #region ボタン・タップイベント処理

    /// <summary>
    /// 設定ボタンクリック後の処理
    /// </summary>
    private void OnSettingsButtonClicked()
    {
        PlayButtonClickSE();
        Debug.Log("設定画面を開く（未実装）");

        // 将来設定画面を実装する際に使用
        // SettingsManager.Instance.OpenSettings();
    }

    /// <summary>
    /// STARTボタンクリック後の処理
    /// </summary>
    private void OnStartButtonClicked()
    {
        if (isTransitioning) return;

        PlayButtonClickSE();
        Debug.Log("STARTボタンでホーム画面に遷移");

        StartCoroutine(TransitionToHomeWithFade());
    }

    /// <summary>
    /// 背景タップ後の処理
    /// </summary>
    private void OnBackgroundTapped()
    {
        if (isTransitioning) return;

        PlayTapSE();
        Debug.Log("背景タップでホーム画面に遷移");

        StartCoroutine(TransitionToHomeWithFade());
    }

    #endregion

    #region シーン遷移

    /// <summary>
    /// 暗転効果付きでホーム画面への遷移
    /// </summary>
    private IEnumerator TransitionToHomeWithFade()
    {
        isTransitioning = true;

        // STARTボタンの点滅を停止
        StopStartButtonBlink();

        // 暗転エフェクト実行
        yield return StartCoroutine(FadeToBlack());

        // ホーム画面をロード
        GameSceneManager.Instance.LoadHomeScene();
    }

    /// <summary>
    /// 画面を黒く暗転させる
    /// </summary>
    private IEnumerator FadeToBlack()
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("fadeCanvasGroupが設定されていません。暗転効果をスキップします。");
            yield return new WaitForSeconds(fadeToBlackDuration);
            yield break;
        }

        // レイキャストを有効にして、他のUIの操作を防ぐ
        fadeCanvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < fadeToBlackDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeToBlackDuration);
            fadeCanvasGroup.alpha = alpha;
            yield return null;
        }

        // 完全に黒くする
        fadeCanvasGroup.alpha = 1f;

        Debug.Log("暗転効果完了");
    }

    /// <summary>
    /// UI全体のフェードアウト（旧バージョン・参考用）
    /// </summary>
    private IEnumerator FadeOutUI()
    {
        if (mainUI == null) yield break;

        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            mainUI.alpha = alpha;
            yield return null;
        }

        mainUI.alpha = 0f;
    }

    #endregion

    #region 音響処理

    /// <summary>
    /// タイトルBGM再生
    /// </summary>
    private void PlayTitleBGM()
    {
        if (audioSource != null && titleBGM != null)
        {
            audioSource.clip = titleBGM;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    /// <summary>
    /// ボタンクリック効果音再生
    /// </summary>
    private void PlayButtonClickSE()
    {
        if (audioSource != null && buttonClickSE != null)
        {
            audioSource.PlayOneShot(buttonClickSE);
        }
    }

    /// <summary>
    /// タップ効果音再生
    /// </summary>
    private void PlayTapSE()
    {
        if (audioSource != null && tapSE != null)
        {
            audioSource.PlayOneShot(tapSE);
        }
    }

    #endregion

    #region Unity Editor用

    [ContextMenu("Test Background Tap")]
    private void TestBackgroundTap()
    {
        OnBackgroundTapped();
    }

    [ContextMenu("Test Settings Button")]
    private void TestSettingsButton()
    {
        OnSettingsButtonClicked();
    }

    [ContextMenu("Test START Button Blink")]
    private void TestStartButtonBlink()
    {
        if (enableStartButtonBlink)
        {
            StopStartButtonBlink();
        }
        else
        {
            StartStartButtonBlink();
        }
        enableStartButtonBlink = !enableStartButtonBlink;
    }

    [ContextMenu("Test Fade to Black")]
    private void TestFadeToBlack()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(FadeToBlack());
        }
    }

    #endregion

    #region Unity ライフサイクル

    private void OnDestroy()
    {
        // コルーチンの安全な停止
        if (startButtonBlinkCoroutine != null)
        {
            StopCoroutine(startButtonBlinkCoroutine);
            startButtonBlinkCoroutine = null;
        }
    }

    private void OnDisable()
    {
        // 点滅アニメーションを停止
        StopStartButtonBlink();
    }

    #endregion
}