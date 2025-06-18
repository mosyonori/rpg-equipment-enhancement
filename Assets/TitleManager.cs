using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class TitleManager : MonoBehaviour
{
    [Header("UI�v�f")]
    public Image titleLogo;                 // �^�C�g�����S�摜
    public Button settingsButton;           // �ݒ�{�^��
    public Button startButton;              // START�{�^��
    public Image characterImage;            // �L�����N�^�[�摜�i�\���̂݁j
    public GameObject[] decorationImages;   // �����摜�z��i�\���̂݁j

    [Header("�w�i�E�^�b�v�G���A")]
    public GameObject backgroundTapArea;    // �w�i�^�b�v�G���A
    public CanvasGroup mainUI;

    [Header("�Ó]�pUI")]
    public Image fadeOverlay;               // �Ó]�p�̃I�[�o�[���C�摜�i�����摜�j
    public CanvasGroup fadeCanvasGroup;     // �Ó]�p��CanvasGroup

    [Header("�A�j���[�V�����ݒ�")]
    public float titleFadeInDuration = 2.0f;   // �^�C�g���t�F�[�h�C������
    [Space]
    [Header("START�{�^���_�Őݒ�")]
    public bool enableStartButtonBlink = true;  // START�{�^���_�ł̗L��/����
    public float blinkInterval = 1.0f;          // �_�ŊԊu�i�b�j
    public float blinkFadeDuration = 0.3f;      // �t�F�[�h�C��/�A�E�g����
    public float minAlpha = 0.3f;               // �ŏ������x
    public float maxAlpha = 1.0f;               // �ő哧���x
    [Space]
    [Header("��ʑJ�ڐݒ�")]
    public float fadeToBlackDuration = 1.0f;   // �Ó]����

    [Header("�����ݒ�")]
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
    /// �^�C�g����ʂ̏�����
    /// </summary>
    private void InitializeTitle()
    {
        // �^�C�g�����S���\���ɐݒ�
        if (titleLogo != null)
        {
            titleLogo.gameObject.SetActive(false);
        }

        // ���̑��̗v�f�͒ʏ�ʂ�\��
        if (characterImage != null)
        {
            characterImage.gameObject.SetActive(true);
        }

        // �����摜���\��
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

        // �Ó]�pUI�̏�����
        InitializeFadeOverlay();

        // �{�^���C�x���g�̐ݒ�
        SetupButtons();

        // �w�i�^�b�v�G���A�̐ݒ�
        SetupBackgroundTap();

        // BGM�Đ�
        PlayTitleBGM();

        // �V���v���ȃ^�C�g���\���A�j���[�V�����J�n
        StartCoroutine(ShowTitleLogo());

        Debug.Log("�V���v���^�C�g����ʂ����������܂���");
    }

    /// <summary>
    /// �Ó]�pUI�̏�����
    /// </summary>
    private void InitializeFadeOverlay()
    {
        if (fadeOverlay != null)
        {
            // �Ó]�p�摜�����F�ɐݒ�
            fadeOverlay.color = Color.black;
            fadeOverlay.gameObject.SetActive(true);
        }

        if (fadeCanvasGroup != null)
        {
            // ������Ԃł͓����i�Ó]���Ă��Ȃ���ԁj
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(true);
            // ���C�L���X�g�𖳌��ɂ��āA����UI�̎ז������Ȃ��悤�ɂ���
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// �{�^���C�x���g�̐ݒ�
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
    /// �w�i�^�b�v�G���A�̐ݒ�
    /// </summary>
    private void SetupBackgroundTap()
    {
        if (backgroundTapArea != null)
        {
            // EventTrigger �R���|�[�l���g��ǉ�
            EventTrigger trigger = backgroundTapArea.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = backgroundTapArea.AddComponent<EventTrigger>();
            }

            // PointerClick �C�x���g��ǉ�
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { OnBackgroundTapped(); });
            trigger.triggers.Add(entry);

            Debug.Log("�w�i�^�b�v�G���A��ݒ肵�܂���");
        }
    }

    /// <summary>
    /// �^�C�g�����S���㕔����t�F�[�h�C���ŕ\��
    /// </summary>
    private IEnumerator ShowTitleLogo()
    {
        if (titleLogo == null) yield break;

        // �^�C�g�����S��\����Ԃɂ��āA�㕔�ɔz�u
        titleLogo.gameObject.SetActive(true);

        Vector3 originalPos = titleLogo.transform.localPosition;
        Vector3 startPos = originalPos + new Vector3(0, 200f, 0); // �㕔����X�^�[�g

        // ������Ԑݒ�
        titleLogo.transform.localPosition = startPos;
        titleLogo.color = new Color(titleLogo.color.r, titleLogo.color.g, titleLogo.color.b, 0f); // ����

        float elapsed = 0f;
        while (elapsed < titleFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / titleFadeInDuration;

            // �ʒu�̕�ԁi�ォ�牺�ցj
            titleLogo.transform.localPosition = Vector3.Lerp(startPos, originalPos, t);

            // �A���t�@�l�̕�ԁi��������s�����ցj
            Color currentColor = titleLogo.color;
            currentColor.a = Mathf.Lerp(0f, 1f, t);
            titleLogo.color = currentColor;

            yield return null;
        }

        // �ŏI��Ԃ��m��
        titleLogo.transform.localPosition = originalPos;
        Color finalColor = titleLogo.color;
        finalColor.a = 1f;
        titleLogo.color = finalColor;

        Debug.Log("�^�C�g�����S�̃t�F�[�h�C������");

        // �^�C�g�����S�\��������ASTART�{�^���̓_�ł��J�n
        StartStartButtonBlink();
    }

    #region START�{�^���_�ŃA�j���[�V����

    /// <summary>
    /// START�{�^���̓_�ŃA�j���[�V�����J�n
    /// </summary>
    private void StartStartButtonBlink()
    {
        if (!enableStartButtonBlink || startButton == null) return;

        // �����̓_�ŃR���[�`�����~
        if (startButtonBlinkCoroutine != null)
        {
            StopCoroutine(startButtonBlinkCoroutine);
        }

        // �V�����_�ŃR���[�`�����J�n
        startButtonBlinkCoroutine = StartCoroutine(StartButtonBlinkLoop());
        Debug.Log("START�{�^���̓_�ŃA�j���[�V�������J�n���܂���");
    }

    /// <summary>
    /// START�{�^���̓_�ŃA�j���[�V������~
    /// </summary>
    private void StopStartButtonBlink()
    {
        if (startButtonBlinkCoroutine != null)
        {
            StopCoroutine(startButtonBlinkCoroutine);
            startButtonBlinkCoroutine = null;
        }

        // �{�^�������S�ɕs�����ɖ߂�
        if (startButton != null)
        {
            var buttonImage = startButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color color = buttonImage.color;
                color.a = maxAlpha;
                buttonImage.color = color;
            }

            // �{�^���e�L�X�g�����ɖ߂�
            var buttonText = startButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                Color textColor = buttonText.color;
                textColor.a = maxAlpha;
                buttonText.color = textColor;
            }
        }

        Debug.Log("START�{�^���̓_�ŃA�j���[�V�������~���܂���");
    }

    /// <summary>
    /// START�{�^���_�ł̃��C�����[�v
    /// </summary>
    private IEnumerator StartButtonBlinkLoop()
    {
        while (true)
        {
            // �t�F�[�h�A�E�g�i���邢 �� �Â��j
            yield return StartCoroutine(FadeStartButton(maxAlpha, minAlpha, blinkFadeDuration));

            // �Â���Ԃŏ����ҋ@
            yield return new WaitForSeconds(blinkInterval - blinkFadeDuration * 2f);

            // �t�F�[�h�C���i�Â� �� ���邢�j
            yield return StartCoroutine(FadeStartButton(minAlpha, maxAlpha, blinkFadeDuration));

            // ���邢��Ԃŏ����ҋ@
            yield return new WaitForSeconds(blinkInterval - blinkFadeDuration * 2f);
        }
    }

    /// <summary>
    /// START�{�^���̃t�F�[�h����
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

            // �{�^���摜�̃A���t�@�l��ύX
            if (buttonImage != null)
            {
                Color imageColor = buttonImage.color;
                imageColor.a = currentAlpha;
                buttonImage.color = imageColor;
            }

            // �{�^���e�L�X�g�̃A���t�@�l��ύX
            if (buttonText != null)
            {
                Color textColor = buttonText.color;
                textColor.a = currentAlpha;
                buttonText.color = textColor;
            }

            yield return null;
        }

        // �ŏI�I�ȃA���t�@�l���m��
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

    #region �{�^���E�^�b�v�C�x���g����

    /// <summary>
    /// �ݒ�{�^���N���b�N��̏���
    /// </summary>
    private void OnSettingsButtonClicked()
    {
        PlayButtonClickSE();
        Debug.Log("�ݒ��ʂ��J���i�������j");

        // �����ݒ��ʂ���������ۂɎg�p
        // SettingsManager.Instance.OpenSettings();
    }

    /// <summary>
    /// START�{�^���N���b�N��̏���
    /// </summary>
    private void OnStartButtonClicked()
    {
        if (isTransitioning) return;

        PlayButtonClickSE();
        Debug.Log("START�{�^���Ńz�[����ʂɑJ��");

        StartCoroutine(TransitionToHomeWithFade());
    }

    /// <summary>
    /// �w�i�^�b�v��̏���
    /// </summary>
    private void OnBackgroundTapped()
    {
        if (isTransitioning) return;

        PlayTapSE();
        Debug.Log("�w�i�^�b�v�Ńz�[����ʂɑJ��");

        StartCoroutine(TransitionToHomeWithFade());
    }

    #endregion

    #region �V�[���J��

    /// <summary>
    /// �Ó]���ʕt���Ńz�[����ʂւ̑J��
    /// </summary>
    private IEnumerator TransitionToHomeWithFade()
    {
        isTransitioning = true;

        // START�{�^���̓_�ł��~
        StopStartButtonBlink();

        // �Ó]�G�t�F�N�g���s
        yield return StartCoroutine(FadeToBlack());

        // �z�[����ʂ����[�h
        GameSceneManager.Instance.LoadHomeScene();
    }

    /// <summary>
    /// ��ʂ������Ó]������
    /// </summary>
    private IEnumerator FadeToBlack()
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogWarning("fadeCanvasGroup���ݒ肳��Ă��܂���B�Ó]���ʂ��X�L�b�v���܂��B");
            yield return new WaitForSeconds(fadeToBlackDuration);
            yield break;
        }

        // ���C�L���X�g��L���ɂ��āA����UI�̑����h��
        fadeCanvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < fadeToBlackDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeToBlackDuration);
            fadeCanvasGroup.alpha = alpha;
            yield return null;
        }

        // ���S�ɍ�������
        fadeCanvasGroup.alpha = 1f;

        Debug.Log("�Ó]���ʊ���");
    }

    /// <summary>
    /// UI�S�̂̃t�F�[�h�A�E�g�i���o�[�W�����E�Q�l�p�j
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

    #region ��������

    /// <summary>
    /// �^�C�g��BGM�Đ�
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
    /// �{�^���N���b�N���ʉ��Đ�
    /// </summary>
    private void PlayButtonClickSE()
    {
        if (audioSource != null && buttonClickSE != null)
        {
            audioSource.PlayOneShot(buttonClickSE);
        }
    }

    /// <summary>
    /// �^�b�v���ʉ��Đ�
    /// </summary>
    private void PlayTapSE()
    {
        if (audioSource != null && tapSE != null)
        {
            audioSource.PlayOneShot(tapSE);
        }
    }

    #endregion

    #region Unity Editor�p

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

    #region Unity ���C�t�T�C�N��

    private void OnDestroy()
    {
        // �R���[�`���̈��S�Ȓ�~
        if (startButtonBlinkCoroutine != null)
        {
            StopCoroutine(startButtonBlinkCoroutine);
            startButtonBlinkCoroutine = null;
        }
    }

    private void OnDisable()
    {
        // �_�ŃA�j���[�V�������~
        StopStartButtonBlink();
    }

    #endregion
}