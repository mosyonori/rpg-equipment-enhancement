using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 戦闘結果表示UI制御
/// 役割：戦闘結果の表示制御・ユーザー操作受付
/// 機能：勝利・敗北画面表示、戦闘ターン数・時間表示、ホーム画面復帰ボタン
/// データアクセス統一ルール: UI層 → Manager層 → データ層
/// </summary>
public class BattleResultUI : MonoBehaviour
{
    [Header("結果画面全体")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private CanvasGroup resultCanvasGroup;
    [SerializeField] private GameObject backgroundDim;

    [Header("勝利・敗北表示")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private Image resultBackgroundImage;
    [SerializeField] private Image resultIconImage;

    [Header("戦闘統計表示")]
    [SerializeField] private TextMeshProUGUI battleTurnsText;
    [SerializeField] private TextMeshProUGUI battleTimeText;
    [SerializeField] private TextMeshProUGUI totalDamageDealtText;
    [SerializeField] private TextMeshProUGUI totalDamageReceivedText;
    [SerializeField] private TextMeshProUGUI skillsUsedText;
    [SerializeField] private TextMeshProUGUI criticalHitsText;

    [Header("報酬情報表示")]
    [SerializeField] private GameObject rewardSection;
    [SerializeField] private TextMeshProUGUI gainedExpText;
    [SerializeField] private TextMeshProUGUI gainedGoldText;
    [SerializeField] private TextMeshProUGUI levelUpText;
    [SerializeField] private GameObject levelUpPanel;

    [Header("ドロップアイテム表示")]
    [SerializeField] private GameObject dropItemSection;
    [SerializeField] private Transform dropItemGridParent;
    [SerializeField] private GameObject dropItemSlotPrefab;
    [SerializeField] private TextMeshProUGUI noDropItemsText;

    [Header("操作ボタン")]
    [SerializeField] private Button homeButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextQuestButton;
    [SerializeField] private TextMeshProUGUI homeButtonText;
    [SerializeField] private TextMeshProUGUI retryButtonText;

    [Header("色設定")]
    [SerializeField] private Color victoryColor = new Color(1f, 0.8f, 0f, 1f); // 金色
    [SerializeField] private Color defeatColor = new Color(0.8f, 0.2f, 0.2f, 1f); // 赤色
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color highlightTextColor = Color.yellow;

    [Header("アニメーション設定")]
    [SerializeField] private float showDelay = 1.0f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float resultTextAnimationDelay = 0.8f;
    [SerializeField] private float statisticsAnimationDelay = 1.2f;
    [SerializeField] private float buttonAnimationDelay = 1.8f;
    [SerializeField] private AnimationCurve fadeEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // イベント
    public static event Action OnHomeButtonClicked;
    public static event Action OnRetryButtonClicked;
    public static event Action OnNextQuestButtonClicked;

    // 内部状態
    private bool isInitialized = false;
    private bool isVisible = false;
    private BattleResultData currentResultData;
    private List<GameObject> dropItemSlots;
    private Coroutine showResultCoroutine;

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateComponents();
        InitializeCollections();
    }

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (showResultCoroutine != null)
        {
            StopCoroutine(showResultCoroutine);
        }
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
            Log("BattleResultUI初期化開始");

            // コレクション初期化
            InitializeCollections();

            // ボタンイベント登録
            RegisterButtonEvents();

            // 初期状態設定
            if (resultPanel != null)
                resultPanel.SetActive(false);

            if (resultCanvasGroup != null)
            {
                resultCanvasGroup.alpha = 0f;
                resultCanvasGroup.interactable = false;
                resultCanvasGroup.blocksRaycasts = false;
            }

            // 各パネルを非表示
            if (victoryPanel != null)
                victoryPanel.SetActive(false);

            if (defeatPanel != null)
                defeatPanel.SetActive(false);

            if (levelUpPanel != null)
                levelUpPanel.SetActive(false);

            // 初期表示クリア
            ClearResultDisplay();

            isVisible = false;
            currentResultData = null;

            isInitialized = true;
            Log("BattleResultUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"BattleResultUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// コレクション初期化
    /// </summary>
    private void InitializeCollections()
    {
        dropItemSlots = new List<GameObject>();
    }

    /// <summary>
    /// コンポーネント検証
    /// </summary>
    private void ValidateComponents()
    {
        if (resultPanel == null)
            LogWarning("resultPanelが設定されていません");

        if (homeButton == null)
            LogWarning("homeButtonが設定されていません");

        if (resultTitleText == null)
            LogWarning("resultTitleTextが設定されていません");
    }

    /// <summary>
    /// ボタンイベント登録
    /// </summary>
    private void RegisterButtonEvents()
    {
        if (homeButton != null)
            homeButton.onClick.AddListener(OnHomeButtonClick);

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryButtonClick);

        if (nextQuestButton != null)
            nextQuestButton.onClick.AddListener(OnNextQuestButtonClick);
    }

    /// <summary>
    /// イベント登録解除
    /// </summary>
    private void UnregisterEvents()
    {
        if (homeButton != null)
            homeButton.onClick.RemoveAllListeners();

        if (retryButton != null)
            retryButton.onClick.RemoveAllListeners();

        if (nextQuestButton != null)
            nextQuestButton.onClick.RemoveAllListeners();
    }

    #endregion

    #region 公開メソッド - 結果表示

    /// <summary>
    /// 戦闘結果を表示
    /// </summary>
    public void ShowResult(BattleResultData resultData)
    {
        if (!isInitialized)
        {
            LogWarning("未初期化状態での結果表示要求");
            return;
        }

        if (resultData == null)
        {
            LogWarning("無効な結果データでの表示要求");
            return;
        }

        try
        {
            Log($"戦闘結果表示: {(resultData.isVictory ? "勝利" : "敗北")}");

            currentResultData = resultData;

            // 結果表示アニメーション開始
            if (showResultCoroutine != null)
                StopCoroutine(showResultCoroutine);

            showResultCoroutine = StartCoroutine(ShowResultCoroutine());
        }
        catch (Exception e)
        {
            LogError($"戦闘結果表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 結果画面を非表示
    /// </summary>
    public void HideResult()
    {
        try
        {
            Log("戦闘結果非表示");

            if (showResultCoroutine != null)
            {
                StopCoroutine(showResultCoroutine);
                showResultCoroutine = null;
            }

            // パネルを非表示
            if (resultPanel != null)
                resultPanel.SetActive(false);

            isVisible = false;
            currentResultData = null;

            // ドロップアイテムスロットをクリア
            ClearDropItemSlots();
        }
        catch (Exception e)
        {
            LogError($"戦闘結果非表示エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - 結果表示アニメーション

    /// <summary>
    /// 結果表示アニメーションコルーチン
    /// </summary>
    private IEnumerator ShowResultCoroutine()
    {
        // 表示準備
        PrepareResultDisplay();

        // 初期遅延
        yield return new WaitForSeconds(showDelay);

        // パネル表示・フェードイン
        yield return StartCoroutine(FadeInPanel());

        // 結果タイトル表示
        yield return new WaitForSeconds(resultTextAnimationDelay);
        yield return StartCoroutine(ShowResultTitle());

        // 統計情報表示
        yield return new WaitForSeconds(statisticsAnimationDelay - resultTextAnimationDelay);
        yield return StartCoroutine(ShowStatistics());

        // 報酬情報表示（勝利時のみ）
        if (currentResultData.isVictory)
        {
            yield return StartCoroutine(ShowRewards());
            yield return StartCoroutine(ShowDropItems());
        }

        // ボタン表示
        yield return new WaitForSeconds(buttonAnimationDelay - statisticsAnimationDelay);
        yield return StartCoroutine(ShowButtons());

        showResultCoroutine = null;
    }

    /// <summary>
    /// 結果表示準備
    /// </summary>
    private void PrepareResultDisplay()
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        // 勝利・敗北パネル設定
        bool isVictory = currentResultData.isVictory;

        if (victoryPanel != null)
            victoryPanel.SetActive(isVictory);

        if (defeatPanel != null)
            defeatPanel.SetActive(!isVictory);

        // 背景色設定
        if (resultBackgroundImage != null)
        {
            resultBackgroundImage.color = isVictory ? victoryColor : defeatColor;
        }

        // 初期状態で非表示にする要素
        SetUIElementsAlpha(0f);
    }

    /// <summary>
    /// パネルフェードイン
    /// </summary>
    private IEnumerator FadeInPanel()
    {
        if (resultCanvasGroup == null) yield break;

        float elapsed = 0f;
        resultCanvasGroup.interactable = false;
        resultCanvasGroup.blocksRaycasts = false;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            float curveValue = fadeEasing.Evaluate(t);
            resultCanvasGroup.alpha = curveValue;
            yield return null;
        }

        resultCanvasGroup.alpha = 1f;
        isVisible = true;
    }

    /// <summary>
    /// 結果タイトル表示
    /// </summary>
    private IEnumerator ShowResultTitle()
    {
        if (resultTitleText != null)
        {
            string titleText = currentResultData.isVictory ? "VICTORY!" : "DEFEAT...";
            resultTitleText.text = titleText;
            resultTitleText.color = currentResultData.isVictory ? victoryColor : defeatColor;

            yield return StartCoroutine(FadeInText(resultTitleText, 0.3f));
        }

        if (resultIconImage != null)
        {
            yield return StartCoroutine(FadeInImage(resultIconImage, 0.2f));
        }
    }

    /// <summary>
    /// 統計情報表示
    /// </summary>
    private IEnumerator ShowStatistics()
    {
        // 戦闘ターン数
        if (battleTurnsText != null)
        {
            battleTurnsText.text = $"戦闘ターン数: {currentResultData.totalTurns}";
            yield return StartCoroutine(FadeInText(battleTurnsText, 0.2f));
        }

        // 戦闘時間
        if (battleTimeText != null)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(currentResultData.battleDuration);
            battleTimeText.text = $"戦闘時間: {timeSpan:mm\\:ss}";
            yield return StartCoroutine(FadeInText(battleTimeText, 0.2f));
        }

        // 与えたダメージ
        if (totalDamageDealtText != null)
        {
            totalDamageDealtText.text = $"与えたダメージ: {currentResultData.totalDamageDealt:N0}";
            yield return StartCoroutine(FadeInText(totalDamageDealtText, 0.2f));
        }

        // 受けたダメージ
        if (totalDamageReceivedText != null)
        {
            totalDamageReceivedText.text = $"受けたダメージ: {currentResultData.totalDamageReceived:N0}";
            yield return StartCoroutine(FadeInText(totalDamageReceivedText, 0.2f));
        }

        // 使用スキル数
        if (skillsUsedText != null)
        {
            skillsUsedText.text = $"使用スキル数: {currentResultData.skillsUsed}";
            yield return StartCoroutine(FadeInText(skillsUsedText, 0.2f));
        }

        // クリティカル回数
        if (criticalHitsText != null)
        {
            criticalHitsText.text = $"クリティカル回数: {currentResultData.criticalHits}";
            yield return StartCoroutine(FadeInText(criticalHitsText, 0.2f));
        }
    }

    /// <summary>
    /// 報酬情報表示
    /// </summary>
    private IEnumerator ShowRewards()
    {
        if (rewardSection != null)
            rewardSection.SetActive(true);

        // 獲得経験値
        if (gainedExpText != null)
        {
            gainedExpText.text = $"獲得経験値: +{currentResultData.gainedExp:N0}";
            yield return StartCoroutine(FadeInText(gainedExpText, 0.2f));
        }

        // 獲得ゴールド
        if (gainedGoldText != null)
        {
            gainedGoldText.text = $"獲得ゴールド: +{currentResultData.gainedGold:N0}G";
            yield return StartCoroutine(FadeInText(gainedGoldText, 0.2f));
        }

        // レベルアップ表示
        if (currentResultData.leveledUp)
        {
            if (levelUpPanel != null)
                levelUpPanel.SetActive(true);

            if (levelUpText != null)
            {
                levelUpText.text = $"LEVEL UP! Lv.{currentResultData.newLevel}";
                levelUpText.color = highlightTextColor;
                yield return StartCoroutine(FadeInText(levelUpText, 0.3f));
            }
        }
    }

    /// <summary>
    /// ドロップアイテム表示
    /// </summary>
    private IEnumerator ShowDropItems()
    {
        if (dropItemSection != null)
            dropItemSection.SetActive(true);

        if (currentResultData.dropItems == null || currentResultData.dropItems.Count == 0)
        {
            // ドロップアイテムなし
            if (noDropItemsText != null)
            {
                noDropItemsText.text = "ドロップアイテムなし";
                yield return StartCoroutine(FadeInText(noDropItemsText, 0.2f));
            }
        }
        else
        {
            // ドロップアイテムあり
            if (noDropItemsText != null)
                noDropItemsText.gameObject.SetActive(false);

            // アイテムスロット作成
            yield return StartCoroutine(CreateDropItemSlots());
        }
    }

    /// <summary>
    /// ボタン表示
    /// </summary>
    private IEnumerator ShowButtons()
    {
        if (resultCanvasGroup != null)
        {
            resultCanvasGroup.interactable = true;
            resultCanvasGroup.blocksRaycasts = true;
        }

        // ホームボタン
        if (homeButton != null)
        {
            yield return StartCoroutine(FadeInButton(homeButton, 0.2f));
        }

        // リトライボタン（敗北時のみ）
        if (retryButton != null && !currentResultData.isVictory)
        {
            yield return StartCoroutine(FadeInButton(retryButton, 0.2f));
        }

        // 次のクエストボタン（勝利時のみ）
        if (nextQuestButton != null && currentResultData.isVictory)
        {
            yield return StartCoroutine(FadeInButton(nextQuestButton, 0.2f));
        }
    }

    #endregion

    #region 内部メソッド - ドロップアイテム処理

    /// <summary>
    /// ドロップアイテムスロット作成
    /// </summary>
    private IEnumerator CreateDropItemSlots()
    {
        if (dropItemSlotPrefab == null || dropItemGridParent == null) yield break;

        ClearDropItemSlots();

        foreach (var dropItem in currentResultData.dropItems)
        {
            yield return StartCoroutine(CreateSingleDropItemSlot(dropItem));
            yield return new WaitForSeconds(0.1f); // スロット間の間隔
        }
    }

    /// <summary>
    /// 単一ドロップアイテムスロット作成
    /// </summary>
    private IEnumerator CreateSingleDropItemSlot(DropResult dropItem)
    {
        GameObject slotObj = CreateDropItemSlotObject(dropItem);

        if (slotObj != null)
        {
            // フェードイン演出
            var canvasGroup = slotObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = slotObj.AddComponent<CanvasGroup>();

            yield return StartCoroutine(FadeInCanvasGroup(canvasGroup, 0.3f));
        }
    }

    /// <summary>
    /// ドロップアイテムスロットオブジェクト作成
    /// </summary>
    private GameObject CreateDropItemSlotObject(DropResult dropItem)
    {
        try
        {
            GameObject slotObj = Instantiate(dropItemSlotPrefab, dropItemGridParent);
            dropItemSlots.Add(slotObj);

            // アイテム情報設定（基本的なテキスト表示のみ実装）
            var textComponents = slotObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (textComponents.Length > 0)
            {
                // アイテム名を表示
                textComponents[0].text = GetItemDisplayName(dropItem);
            }
            if (textComponents.Length > 1)
            {
                // 数量を表示
                textComponents[1].text = $"x{dropItem.quantity}";
            }

            return slotObj;
        }
        catch (Exception e)
        {
            LogError($"ドロップアイテムスロット作成エラー: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// ドロップアイテムスロットクリア
    /// </summary>
    private void ClearDropItemSlots()
    {
        foreach (var slot in dropItemSlots)
        {
            if (slot != null)
                DestroyImmediate(slot);
        }
        dropItemSlots.Clear();
    }

    #endregion

    #region 内部メソッド - アニメーション

    /// <summary>
    /// UI要素のアルファ値設定
    /// </summary>
    private void SetUIElementsAlpha(float alpha)
    {
        // 各テキストコンポーネントのアルファ値を設定
        SetTextAlpha(resultTitleText, alpha);
        SetTextAlpha(battleTurnsText, alpha);
        SetTextAlpha(battleTimeText, alpha);
        SetTextAlpha(totalDamageDealtText, alpha);
        SetTextAlpha(totalDamageReceivedText, alpha);
        SetTextAlpha(skillsUsedText, alpha);
        SetTextAlpha(criticalHitsText, alpha);
        SetTextAlpha(gainedExpText, alpha);
        SetTextAlpha(gainedGoldText, alpha);
        SetTextAlpha(levelUpText, alpha);

        // ボタンのアルファ値設定
        SetButtonAlpha(homeButton, alpha);
        SetButtonAlpha(retryButton, alpha);
        SetButtonAlpha(nextQuestButton, alpha);
    }

    /// <summary>
    /// テキストフェードイン
    /// </summary>
    private IEnumerator FadeInText(TextMeshProUGUI textComponent, float duration)
    {
        if (textComponent == null) yield break;

        float elapsed = 0f;
        Color startColor = textComponent.color;
        startColor.a = 0f;
        Color targetColor = textComponent.color;
        targetColor.a = 1f;

        textComponent.color = startColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            textComponent.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        textComponent.color = targetColor;
    }

    /// <summary>
    /// イメージフェードイン
    /// </summary>
    private IEnumerator FadeInImage(Image imageComponent, float duration)
    {
        if (imageComponent == null) yield break;

        float elapsed = 0f;
        Color startColor = imageComponent.color;
        startColor.a = 0f;
        Color targetColor = imageComponent.color;
        targetColor.a = 1f;

        imageComponent.color = startColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            imageComponent.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        imageComponent.color = targetColor;
    }

    /// <summary>
    /// ボタンフェードイン
    /// </summary>
    private IEnumerator FadeInButton(Button button, float duration)
    {
        if (button == null) yield break;

        var canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();

        yield return StartCoroutine(FadeInCanvasGroup(canvasGroup, duration));
    }

    /// <summary>
    /// CanvasGroupフェードイン
    /// </summary>
    private IEnumerator FadeInCanvasGroup(CanvasGroup canvasGroup, float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            canvasGroup.alpha = t;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    #endregion

    #region 内部メソッド - ユーティリティ

    /// <summary>
    /// 結果表示をクリア
    /// </summary>
    private void ClearResultDisplay()
    {
        if (resultTitleText != null)
            resultTitleText.text = "";

        // 統計情報クリア
        if (battleTurnsText != null)
            battleTurnsText.text = "";

        if (battleTimeText != null)
            battleTimeText.text = "";

        // 報酬情報クリア
        if (gainedExpText != null)
            gainedExpText.text = "";

        if (gainedGoldText != null)
            gainedGoldText.text = "";

        // ドロップアイテムクリア
        ClearDropItemSlots();

        // レベルアップパネル非表示
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
    }

    /// <summary>
    /// テキストのアルファ値設定
    /// </summary>
    private void SetTextAlpha(TextMeshProUGUI textComponent, float alpha)
    {
        if (textComponent == null) return;
        Color color = textComponent.color;
        color.a = alpha;
        textComponent.color = color;
    }

    /// <summary>
    /// ボタンのアルファ値設定
    /// </summary>
    private void SetButtonAlpha(Button button, float alpha)
    {
        if (button == null) return;
        var canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = alpha;
    }

    /// <summary>
    /// アイテム表示名取得
    /// </summary>
    private string GetItemDisplayName(DropResult dropItem)
    {
        // 実際の実装では、MasterDataManagerからアイテム名を取得
        if (MasterDataManager.Instance != null)
        {
            if (dropItem.itemType == "EnhanceItem")
            {
                var enhanceItem = MasterDataManager.Instance.GetEnhanceItemData(dropItem.itemId);
                return enhanceItem?.enhanceItemName ?? $"強化アイテム{dropItem.itemId}";
            }
            else if (dropItem.itemType == "SupportItem")
            {
                var supportItem = MasterDataManager.Instance.GetSupportItemData(dropItem.itemId);
                return supportItem?.supportItemName ?? $"補助アイテム{dropItem.itemId}";
            }
        }

        return $"アイテム{dropItem.itemId}";
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// ホームボタンクリック
    /// </summary>
    private void OnHomeButtonClick()
    {
        try
        {
            Log("ホームボタンクリック");
            OnHomeButtonClicked?.Invoke();
        }
        catch (Exception e)
        {
            LogError($"ホームボタンクリックエラー: {e.Message}");
        }
    }

    /// <summary>
    /// リトライボタンクリック
    /// </summary>
    private void OnRetryButtonClick()
    {
        try
        {
            Log("リトライボタンクリック");
            OnRetryButtonClicked?.Invoke();
        }
        catch (Exception e)
        {
            LogError($"リトライボタンクリックエラー: {e.Message}");
        }
    }

    /// <summary>
    /// 次のクエストボタンクリック
    /// </summary>
    private void OnNextQuestButtonClick()
    {
        try
        {
            Log("次のクエストボタンクリック");
            OnNextQuestButtonClicked?.Invoke();
        }
        catch (Exception e)
        {
            LogError($"次のクエストボタンクリックエラー: {e.Message}");
        }
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        Debug.Log($"[BattleResultUI] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[BattleResultUI] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[BattleResultUI] {message}");
    }

    #endregion

    #region エディタ用ツール

#if UNITY_EDITOR
    [ContextMenu("勝利結果表示テスト")]
    private void TestShowVictoryResult()
    {
        var testResult = new BattleResultData
        {
            isVictory = true,
            totalTurns = 8,
            battleDuration = 45.5f,
            gainedExp = 120,
            gainedGold = 350,
            totalDamageDealt = 2450,
            totalDamageReceived = 890,
            skillsUsed = 5,
            criticalHits = 3,
            leveledUp = true,
            newLevel = 5,
            dropItems = new List<DropResult>
            {
                new DropResult { itemType = "EnhanceItem", itemId = 1, quantity = 2 },
                new DropResult { itemType = "SupportItem", itemId = 3, quantity = 1 }
            }
        };

        ShowResult(testResult);
        Log("勝利結果表示テスト実行");
    }

    [ContextMenu("敗北結果表示テスト")]
    private void TestShowDefeatResult()
    {
        var testResult = new BattleResultData
        {
            isVictory = false,
            totalTurns = 12,
            battleDuration = 67.8f,
            gainedExp = 0,
            gainedGold = 0,
            totalDamageDealt = 1820,
            totalDamageReceived = 1950,
            skillsUsed = 7,
            criticalHits = 1,
            leveledUp = false,
            dropItems = new List<DropResult>()
        };

        ShowResult(testResult);
        Log("敗北結果表示テスト実行");
    }

    [ContextMenu("結果非表示テスト")]
    private void TestHideResult()
    {
        HideResult();
        Log("結果非表示テスト実行");
    }
#endif

    #endregion
}