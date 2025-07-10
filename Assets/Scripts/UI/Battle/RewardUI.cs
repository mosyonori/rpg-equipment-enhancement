using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 獲得報酬の表示制御UI
/// 獲得経験値アニメーション、レベルアップ演出、獲得ゴールド表示を管理
/// データアクセス統一ルール: UI層 → Manager層 → Data層
/// </summary>
public class RewardUI : MonoBehaviour
{
    [Header("UI設定")]
    [SerializeField] private bool enableDebugLog = false;
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private CanvasGroup rewardCanvasGroup;

    [Header("経験値表示")]
    [SerializeField] private GameObject expRewardSection;
    [SerializeField] private TextMeshProUGUI gainedExpText;
    [SerializeField] private TextMeshProUGUI totalExpText;
    [SerializeField] private Slider expProgressSlider;
    [SerializeField] private TextMeshProUGUI expProgressText;

    [Header("レベルアップ演出")]
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private TextMeshProUGUI levelUpText;
    [SerializeField] private TextMeshProUGUI newLevelText;
    [SerializeField] private GameObject levelUpEffectPrefab;
    [SerializeField] private Transform levelUpEffectParent;

    [Header("ゴールド表示")]
    [SerializeField] private GameObject goldRewardSection;
    [SerializeField] private TextMeshProUGUI gainedGoldText;
    [SerializeField] private TextMeshProUGUI totalGoldText;

    [Header("プレイヤー情報表示")]
    [SerializeField] private GameObject playerInfoSection;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private Image playerIcon;

    [Header("アニメーション設定")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float expAnimationDuration = 2.0f;
    [SerializeField] private float goldAnimationDuration = 1.5f;
    [SerializeField] private float levelUpAnimationDuration = 3.0f;
    [SerializeField] private float delayBetweenAnimations = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource rewardAudioSource;
    [SerializeField] private AudioClip expGainSound;
    [SerializeField] private AudioClip goldGainSound;
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private AudioClip rewardCompleteSound;

    [Header("操作UI")]
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI continueButtonText;

    // プライベートフィールド
    private BattleResultData currentBattleResult;
    private UserSaveData currentUserData;
    private bool isAnimating;
    private Coroutine rewardAnimationCoroutine;

    // 元の値（アニメーション用）
    private long originalExp;
    private long originalGold;
    private int originalLevel;

    // イベント
    public static event Action OnRewardAnimationCompleted;
    public static event Action OnContinueRequested;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponent();
    }

    private void Start()
    {
        RegisterEvents();
    }

    private void OnDestroy()
    {
        UnregisterEvents();
        StopRewardAnimation();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネント初期化
    /// </summary>
    private void InitializeComponent()
    {
        // パネルを非表示で初期化
        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        // CanvasGroupの初期化
        if (rewardCanvasGroup != null)
        {
            rewardCanvasGroup.alpha = 0f;
            rewardCanvasGroup.interactable = false;
            rewardCanvasGroup.blocksRaycasts = false;
        }

        // 続行ボタンの初期化
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            continueButton.gameObject.SetActive(false);
        }

        if (continueButtonText != null)
            continueButtonText.text = "続行";

        isAnimating = false;

        DebugLog("RewardUI初期化完了");
    }

    /// <summary>
    /// イベント登録
    /// </summary>
    private void RegisterEvents()
    {
        // Manager層からのイベント受信
        if (BattleManager.Instance != null)
        {
            BattleManager.OnBattleCompleted += OnBattleCompleted;
        }
    }

    /// <summary>
    /// イベント登録解除
    /// </summary>
    private void UnregisterEvents()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.OnBattleCompleted -= OnBattleCompleted;
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueButtonClicked);
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 報酬UIを表示
    /// </summary>
    public void ShowRewards(BattleResultData battleResult, UserSaveData userData)
    {
        if (battleResult == null || userData == null)
        {
            LogError("報酬データまたはユーザーデータがnullです");
            return;
        }

        if (isAnimating)
        {
            DebugLog("既にアニメーション中のため報酬表示をスキップ");
            return;
        }

        currentBattleResult = battleResult;
        currentUserData = userData;

        // 元の値を保存（アニメーション開始前の値）
        originalExp = userData.currentExp - battleResult.gainedExp;
        originalGold = userData.gold - battleResult.gainedGold;
        originalLevel = userData.playerLevel;

        if (rewardPanel != null)
            rewardPanel.SetActive(true);

        rewardAnimationCoroutine = StartCoroutine(PlayRewardAnimation());
    }

    /// <summary>
    /// 報酬UIを非表示
    /// </summary>
    public void HideRewards()
    {
        StopRewardAnimation();

        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        if (rewardCanvasGroup != null)
        {
            rewardCanvasGroup.alpha = 0f;
            rewardCanvasGroup.interactable = false;
            rewardCanvasGroup.blocksRaycasts = false;
        }

        isAnimating = false;
    }

    /// <summary>
    /// アニメーションをスキップ
    /// </summary>
    public void SkipAnimation()
    {
        if (!isAnimating) return;

        StopRewardAnimation();

        // 最終状態を即座に表示
        DisplayFinalRewardState();
        ShowContinueButton();

        PlaySound(rewardCompleteSound);
        DebugLog("報酬アニメーションをスキップしました");
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// 戦闘完了イベント処理
    /// </summary>
    private void OnBattleCompleted(BattleResultData battleResult)
    {
        if (battleResult == null || !battleResult.isVictory) return;

        // SaveDataManagerからユーザーデータを取得
        var userData = SaveDataManager.Instance?.CurrentSaveData;
        if (userData != null)
        {
            ShowRewards(battleResult, userData);
        }
    }

    /// <summary>
    /// 続行ボタンクリック処理
    /// </summary>
    private void OnContinueButtonClicked()
    {
        PlaySound(rewardCompleteSound);
        OnContinueRequested?.Invoke();
        HideRewards();
    }

    #endregion

    #region アニメーション処理

    /// <summary>
    /// 報酬アニメーション実行
    /// </summary>
    private IEnumerator PlayRewardAnimation()
    {
        isAnimating = true;

        // フェードイン
        yield return StartCoroutine(FadeInRewardPanel());

        // プレイヤー情報更新
        UpdatePlayerInfo();

        // 経験値アニメーション
        if (currentBattleResult.gainedExp > 0)
        {
            yield return StartCoroutine(AnimateExpGain());
            yield return new WaitForSeconds(delayBetweenAnimations);
        }

        // レベルアップチェック・アニメーション
        if (currentBattleResult.leveledUp)
        {
            yield return StartCoroutine(AnimateLevelUp());
            yield return new WaitForSeconds(delayBetweenAnimations);
        }

        // ゴールドアニメーション
        if (currentBattleResult.gainedGold > 0)
        {
            yield return StartCoroutine(AnimateGoldGain());
            yield return new WaitForSeconds(delayBetweenAnimations);
        }

        // 続行ボタン表示
        ShowContinueButton();

        isAnimating = false;
        OnRewardAnimationCompleted?.Invoke();

        DebugLog("報酬アニメーション完了");
    }

    /// <summary>
    /// パネルフェードイン
    /// </summary>
    private IEnumerator FadeInRewardPanel()
    {
        if (rewardCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            rewardCanvasGroup.alpha = alpha;
            yield return null;
        }

        rewardCanvasGroup.alpha = 1f;
        rewardCanvasGroup.interactable = true;
        rewardCanvasGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// 経験値獲得アニメーション
    /// </summary>
    private IEnumerator AnimateExpGain()
    {
        if (expRewardSection != null)
            expRewardSection.SetActive(true);

        PlaySound(expGainSound);

        // 獲得経験値テキスト更新
        if (gainedExpText != null)
            gainedExpText.text = $"+{currentBattleResult.gainedExp} EXP";

        // 経験値プログレスバーアニメーション
        yield return StartCoroutine(AnimateExpProgress());

        DebugLog($"経験値アニメーション完了: +{currentBattleResult.gainedExp}");
    }

    /// <summary>
    /// 経験値プログレスアニメーション
    /// </summary>
    private IEnumerator AnimateExpProgress()
    {
        if (expProgressSlider == null) yield break;

        var userData = currentUserData;
        long requiredExp = GetRequiredExpForLevel(userData.playerLevel);
        long maxExp = GetRequiredExpForLevel(userData.playerLevel + 1);

        float startProgress = (float)originalExp / maxExp;
        float endProgress = (float)userData.currentExp / maxExp;

        float elapsed = 0f;
        while (elapsed < expAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / expAnimationDuration;

            float currentProgress = Mathf.Lerp(startProgress, endProgress, t);
            expProgressSlider.value = currentProgress;

            // 現在の経験値表示を更新
            long currentExp = (long)Mathf.Lerp(originalExp, userData.currentExp, t);
            UpdateExpProgressText(currentExp, maxExp);

            yield return null;
        }

        expProgressSlider.value = endProgress;
        UpdateExpProgressText(userData.currentExp, maxExp);
    }

    /// <summary>
    /// レベルアップアニメーション
    /// </summary>
    private IEnumerator AnimateLevelUp()
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);

        PlaySound(levelUpSound);

        // レベルアップエフェクト生成
        if (levelUpEffectPrefab != null && levelUpEffectParent != null)
        {
            var effect = Instantiate(levelUpEffectPrefab, levelUpEffectParent);
            Destroy(effect, levelUpAnimationDuration);
        }

        // レベルアップテキスト更新
        if (levelUpText != null)
            levelUpText.text = "LEVEL UP!";

        if (newLevelText != null)
            newLevelText.text = $"Lv.{currentUserData.playerLevel}";

        // プレイヤーレベル表示更新
        if (playerLevelText != null)
            playerLevelText.text = $"Lv.{currentUserData.playerLevel}";

        yield return new WaitForSeconds(levelUpAnimationDuration);

        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        DebugLog($"レベルアップアニメーション完了: Lv.{currentUserData.playerLevel}");
    }

    /// <summary>
    /// ゴールド獲得アニメーション
    /// </summary>
    private IEnumerator AnimateGoldGain()
    {
        if (goldRewardSection != null)
            goldRewardSection.SetActive(true);

        PlaySound(goldGainSound);

        // 獲得ゴールドテキスト更新
        if (gainedGoldText != null)
            gainedGoldText.text = $"+{currentBattleResult.gainedGold:N0} G";

        // 総ゴールドアニメーション
        yield return StartCoroutine(AnimateGoldCount());

        DebugLog($"ゴールドアニメーション完了: +{currentBattleResult.gainedGold}");
    }

    /// <summary>
    /// ゴールドカウントアニメーション
    /// </summary>
    private IEnumerator AnimateGoldCount()
    {
        if (totalGoldText == null) yield break;

        float elapsed = 0f;
        while (elapsed < goldAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / goldAnimationDuration;

            long currentGold = (long)Mathf.Lerp(originalGold, currentUserData.gold, t);
            totalGoldText.text = $"{currentGold:N0} G";

            yield return null;
        }

        totalGoldText.text = $"{currentUserData.gold:N0} G";
    }

    #endregion

    #region ヘルパーメソッド

    /// <summary>
    /// プレイヤー情報更新
    /// </summary>
    private void UpdatePlayerInfo()
    {
        if (playerNameText != null)
            playerNameText.text = currentUserData.playerName;

        if (playerLevelText != null)
            playerLevelText.text = $"Lv.{originalLevel}";

        // プレイヤーアイコンは既存のスプライト設定があれば使用
        // 今回は省略
    }

    /// <summary>
    /// 経験値プログレステキスト更新
    /// </summary>
    private void UpdateExpProgressText(long currentExp, long maxExp)
    {
        if (expProgressText != null)
            expProgressText.text = $"{currentExp:N0} / {maxExp:N0}";

        if (totalExpText != null)
            totalExpText.text = $"{currentExp:N0} EXP";
    }

    /// <summary>
    /// 最終報酬状態を表示
    /// </summary>
    private void DisplayFinalRewardState()
    {
        // 経験値表示
        if (expRewardSection != null)
            expRewardSection.SetActive(currentBattleResult.gainedExp > 0);

        if (gainedExpText != null)
            gainedExpText.text = $"+{currentBattleResult.gainedExp} EXP";

        if (expProgressSlider != null)
        {
            long maxExp = GetRequiredExpForLevel(currentUserData.playerLevel + 1);
            expProgressSlider.value = (float)currentUserData.currentExp / maxExp;
        }

        UpdateExpProgressText(currentUserData.currentExp, GetRequiredExpForLevel(currentUserData.playerLevel + 1));

        // ゴールド表示
        if (goldRewardSection != null)
            goldRewardSection.SetActive(currentBattleResult.gainedGold > 0);

        if (gainedGoldText != null)
            gainedGoldText.text = $"+{currentBattleResult.gainedGold:N0} G";

        if (totalGoldText != null)
            totalGoldText.text = $"{currentUserData.gold:N0} G";

        // プレイヤー情報最終更新
        if (playerLevelText != null)
            playerLevelText.text = $"Lv.{currentUserData.playerLevel}";

        // CanvasGroup設定
        if (rewardCanvasGroup != null)
        {
            rewardCanvasGroup.alpha = 1f;
            rewardCanvasGroup.interactable = true;
            rewardCanvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// 続行ボタンを表示
    /// </summary>
    private void ShowContinueButton()
    {
        if (continueButton != null)
            continueButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// レベルに必要な経験値を取得
    /// </summary>
    private long GetRequiredExpForLevel(int level)
    {
        // 簡単な経験値計算式（実際の仕様に合わせて調整）
        return level * 100L;
    }

    /// <summary>
    /// アニメーション停止
    /// </summary>
    private void StopRewardAnimation()
    {
        if (rewardAnimationCoroutine != null)
        {
            StopCoroutine(rewardAnimationCoroutine);
            rewardAnimationCoroutine = null;
        }
    }

    /// <summary>
    /// 音声再生
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (rewardAudioSource != null && clip != null)
        {
            rewardAudioSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region ログ・デバッグ

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[RewardUI] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[RewardUI] {message}");
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("報酬テスト表示")]
    private void TestRewardDisplay()
    {
        // テスト用のダミーデータ作成
        var testBattleResult = new BattleResultData
        {
            isVictory = true,
            gainedExp = 150,
            gainedGold = 500,
            leveledUp = true
        };

        var testUserData = SaveDataManager.Instance?.CurrentSaveData;
        if (testUserData != null)
        {
            ShowRewards(testBattleResult, testUserData);
        }
        else
        {
            DebugLog("テスト用ユーザーデータが見つかりません");
        }
    }

    [ContextMenu("報酬UI設定確認")]
    private void ValidateRewardSetup()
    {
        DebugLog("=== 報酬UI設定確認 ===");
        DebugLog($"報酬パネル: {(rewardPanel != null ? "設定済み" : "未設定")}");
        DebugLog($"経験値セクション: {(expRewardSection != null ? "設定済み" : "未設定")}");
        DebugLog($"ゴールドセクション: {(goldRewardSection != null ? "設定済み" : "未設定")}");
        DebugLog($"レベルアップパネル: {(levelUpPanel != null ? "設定済み" : "未設定")}");
        DebugLog($"続行ボタン: {(continueButton != null ? "設定済み" : "未設定")}");
        DebugLog($"オーディオソース: {(rewardAudioSource != null ? "設定済み" : "未設定")}");
    }
#endif

    #endregion
}