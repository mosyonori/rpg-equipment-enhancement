using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ドロップアイテムの表示制御UI
/// ドロップアイテムリスト表示、アイテム獲得アニメーション、レアアイテム特別演出を管理
/// データアクセス統一ルール: UI層 → Manager層 → Data層
/// </summary>
public class DropItemUI : MonoBehaviour
{
    [Header("UI設定")]
    [SerializeField] private bool enableDebugLog = false;
    [SerializeField] private GameObject dropItemPanel;
    [SerializeField] private CanvasGroup dropItemCanvasGroup;
    [SerializeField] private Button backgroundButton; // 背景クリックで閉じる用

    [Header("ヘッダー")]
    [SerializeField] private GameObject headerSection;
    [SerializeField] private TextMeshProUGUI headerTitleText;
    [SerializeField] private TextMeshProUGUI totalItemsText;

    [Header("アイテムリスト")]
    [SerializeField] private ScrollRect itemScrollRect;
    [SerializeField] private Transform itemListParent;
    [SerializeField] private GridLayoutGroup itemGridLayout;
    [SerializeField] private GameObject itemSlotPrefab;

    [Header("レアアイテム演出")]
    [SerializeField] private GameObject rareItemEffectPrefab;
    [SerializeField] private Transform rareEffectParent;
    [SerializeField] private float rareItemAnimationDuration = 2.0f;
    [SerializeField] private AnimationCurve rareItemScaleCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1.3f), new Keyframe(1, 1));

    [Header("アニメーション設定")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float itemAppearDelay = 0.1f;
    [SerializeField] private float itemAppearDuration = 0.3f;
    [SerializeField] private float totalAnimationDuration = 3.0f;

    [Header("Audio")]
    [SerializeField] private AudioSource dropItemAudioSource;
    [SerializeField] private AudioClip itemDropSound;
    [SerializeField] private AudioClip rareItemSound;
    [SerializeField] private AudioClip allItemsCompleteSound;

    [Header("操作UI")]
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI continueButtonText;
    [SerializeField] private Button skipButton;
    [SerializeField] private TextMeshProUGUI skipButtonText;

    [Header("エフェクト設定")]
    [SerializeField] private ParticleSystem commonItemEffect;
    [SerializeField] private ParticleSystem rareItemEffect;
    [SerializeField] private ParticleSystem epicItemEffect;
    [SerializeField] private ParticleSystem legendaryItemEffect;

    // プライベートフィールド
    private List<DropResult> currentDropItems;
    private List<GameObject> instantiatedItemSlots;
    private bool isAnimating;
    private Coroutine dropAnimationCoroutine;

    // イベント
    public static event Action OnDropItemAnimationCompleted;
    public static event Action OnContinueRequested;
    public static event Action<DropResult> OnItemDisplayed;

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
        StopDropAnimation();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネント初期化
    /// </summary>
    private void InitializeComponent()
    {
        // パネルを非表示で初期化
        if (dropItemPanel != null)
            dropItemPanel.SetActive(false);

        // CanvasGroupの初期化
        if (dropItemCanvasGroup != null)
        {
            dropItemCanvasGroup.alpha = 0f;
            dropItemCanvasGroup.interactable = false;
            dropItemCanvasGroup.blocksRaycasts = false;
        }

        // ボタンの初期化
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            continueButton.gameObject.SetActive(false);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
            skipButton.gameObject.SetActive(false);
        }

        if (backgroundButton != null)
        {
            backgroundButton.onClick.AddListener(OnBackgroundClicked);
        }

        // テキスト初期化
        if (continueButtonText != null)
            continueButtonText.text = "続行";

        if (skipButtonText != null)
            skipButtonText.text = "スキップ";

        if (headerTitleText != null)
            headerTitleText.text = "ドロップアイテム";

        // データ初期化
        currentDropItems = new List<DropResult>();
        instantiatedItemSlots = new List<GameObject>();
        isAnimating = false;

        DebugLog("DropItemUI初期化完了");
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

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }

        if (backgroundButton != null)
        {
            backgroundButton.onClick.RemoveListener(OnBackgroundClicked);
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// ドロップアイテムUIを表示
    /// </summary>
    public void ShowDropItems(List<DropResult> dropItems)
    {
        if (dropItems == null || dropItems.Count == 0)
        {
            DebugLog("ドロップアイテムがないため表示をスキップ");
            return;
        }

        if (isAnimating)
        {
            DebugLog("既にアニメーション中のためドロップアイテム表示をスキップ");
            return;
        }

        currentDropItems = new List<DropResult>(dropItems);

        if (dropItemPanel != null)
            dropItemPanel.SetActive(true);

        dropAnimationCoroutine = StartCoroutine(PlayDropItemAnimation());
    }

    /// <summary>
    /// ドロップアイテムUIを非表示
    /// </summary>
    public void HideDropItems()
    {
        StopDropAnimation();

        if (dropItemPanel != null)
            dropItemPanel.SetActive(false);

        if (dropItemCanvasGroup != null)
        {
            dropItemCanvasGroup.alpha = 0f;
            dropItemCanvasGroup.interactable = false;
            dropItemCanvasGroup.blocksRaycasts = false;
        }

        ClearItemSlots();
        isAnimating = false;
    }

    /// <summary>
    /// アニメーションをスキップ
    /// </summary>
    public void SkipAnimation()
    {
        if (!isAnimating) return;

        StopDropAnimation();

        // 最終状態を即座に表示
        DisplayAllItemsImmediately();
        ShowContinueButton();

        PlaySound(allItemsCompleteSound);
        DebugLog("ドロップアイテムアニメーションをスキップしました");
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// 戦闘完了イベント処理
    /// </summary>
    private void OnBattleCompleted(BattleResultData battleResult)
    {
        if (battleResult == null || !battleResult.isVictory) return;

        // ドロップアイテムが存在する場合のみ表示
        if (battleResult.dropItems != null && battleResult.dropItems.Count > 0)
        {
            ShowDropItems(battleResult.dropItems);
        }
    }

    /// <summary>
    /// 続行ボタンクリック処理
    /// </summary>
    private void OnContinueButtonClicked()
    {
        PlaySound(allItemsCompleteSound);
        OnContinueRequested?.Invoke();
        HideDropItems();
    }

    /// <summary>
    /// スキップボタンクリック処理
    /// </summary>
    private void OnSkipButtonClicked()
    {
        SkipAnimation();
    }

    /// <summary>
    /// 背景クリック処理
    /// </summary>
    private void OnBackgroundClicked()
    {
        if (isAnimating)
        {
            SkipAnimation();
        }
        else
        {
            OnContinueButtonClicked();
        }
    }

    #endregion

    #region アニメーション処理

    /// <summary>
    /// ドロップアイテムアニメーション実行
    /// </summary>
    private IEnumerator PlayDropItemAnimation()
    {
        isAnimating = true;

        // ヘッダー情報更新
        UpdateHeaderInfo();

        // フェードイン
        yield return StartCoroutine(FadeInDropItemPanel());

        // スキップボタン表示
        if (skipButton != null)
            skipButton.gameObject.SetActive(true);

        // アイテムを順次表示
        for (int i = 0; i < currentDropItems.Count; i++)
        {
            var dropItem = currentDropItems[i];
            yield return StartCoroutine(AnimateItemDrop(dropItem, i));
            yield return new WaitForSeconds(itemAppearDelay);
        }

        // スキップボタン非表示
        if (skipButton != null)
            skipButton.gameObject.SetActive(false);

        // 続行ボタン表示
        ShowContinueButton();

        isAnimating = false;
        OnDropItemAnimationCompleted?.Invoke();

        DebugLog("ドロップアイテムアニメーション完了");
    }

    /// <summary>
    /// パネルフェードイン
    /// </summary>
    private IEnumerator FadeInDropItemPanel()
    {
        if (dropItemCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            dropItemCanvasGroup.alpha = alpha;
            yield return null;
        }

        dropItemCanvasGroup.alpha = 1f;
        dropItemCanvasGroup.interactable = true;
        dropItemCanvasGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// 個別アイテムドロップアニメーション
    /// </summary>
    private IEnumerator AnimateItemDrop(DropResult dropItem, int index)
    {
        // アイテムスロット作成
        var itemSlot = CreateItemSlot(dropItem, index);
        if (itemSlot == null) yield break;

        // 初期状態設定（非表示）
        var canvasGroup = itemSlot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = itemSlot.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        itemSlot.transform.localScale = Vector3.zero;

        // アイテムの希少度に応じた音声・エフェクト
        var rarity = GetItemRarity(dropItem);
        PlayItemDropEffect(itemSlot.transform.position, rarity);
        PlayItemDropSound(rarity);

        // フェードイン & スケールアニメーション
        yield return StartCoroutine(AnimateItemAppear(itemSlot, canvasGroup));

        // レアアイテムの場合は特別演出
        if (IsRareItem(rarity))
        {
            yield return StartCoroutine(PlayRareItemEffect(itemSlot));
        }

        OnItemDisplayed?.Invoke(dropItem);
        DebugLog($"アイテム表示完了: {dropItem.itemName} x{dropItem.quantity}");
    }

    /// <summary>
    /// アイテム出現アニメーション
    /// </summary>
    private IEnumerator AnimateItemAppear(GameObject itemSlot, CanvasGroup canvasGroup)
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;

        while (elapsed < itemAppearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / itemAppearDuration;

            // フェードイン
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            // スケールアニメーション（少しバウンス）
            float scaleT = Mathf.Sin(t * Mathf.PI * 0.5f);
            if (t > 0.7f)
            {
                scaleT = 1f + Mathf.Sin((t - 0.7f) * Mathf.PI * 10f) * 0.1f * (1f - t);
            }
            itemSlot.transform.localScale = Vector3.Lerp(startScale, endScale, scaleT);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        itemSlot.transform.localScale = endScale;
    }

    /// <summary>
    /// レアアイテム特別演出
    /// </summary>
    private IEnumerator PlayRareItemEffect(GameObject itemSlot)
    {
        // レアアイテムエフェクト生成
        if (rareItemEffectPrefab != null && rareEffectParent != null)
        {
            var effect = Instantiate(rareItemEffectPrefab, itemSlot.transform.position, Quaternion.identity, rareEffectParent);
            Destroy(effect, rareItemAnimationDuration);
        }

        // レアアイテム専用スケールアニメーション
        Vector3 originalScale = itemSlot.transform.localScale;
        float elapsed = 0f;

        while (elapsed < rareItemAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rareItemAnimationDuration;
            float curveValue = rareItemScaleCurve.Evaluate(t);

            itemSlot.transform.localScale = originalScale * curveValue;
            yield return null;
        }

        itemSlot.transform.localScale = originalScale;
    }

    #endregion

    #region ヘルパーメソッド

    /// <summary>
    /// ヘッダー情報更新
    /// </summary>
    private void UpdateHeaderInfo()
    {
        if (totalItemsText != null)
        {
            int totalItems = 0;
            foreach (var item in currentDropItems)
            {
                totalItems += item.quantity;
            }
            totalItemsText.text = $"獲得アイテム: {currentDropItems.Count}種類 ({totalItems}個)";
        }
    }

    /// <summary>
    /// アイテムスロット作成
    /// </summary>
    private GameObject CreateItemSlot(DropResult dropItem, int index)
    {
        if (itemSlotPrefab == null || itemListParent == null) return null;

        var itemSlot = Instantiate(itemSlotPrefab, itemListParent);
        instantiatedItemSlots.Add(itemSlot);

        // アイテム情報設定
        SetupItemSlotData(itemSlot, dropItem);

        return itemSlot;
    }

    /// <summary>
    /// アイテムスロットデータ設定
    /// </summary>
    private void SetupItemSlotData(GameObject itemSlot, DropResult dropItem)
    {
        // アイテム名設定
        var nameText = itemSlot.transform.Find("ItemName")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
            nameText.text = dropItem.itemName;

        // 数量設定
        var quantityText = itemSlot.transform.Find("Quantity")?.GetComponent<TextMeshProUGUI>();
        if (quantityText != null)
            quantityText.text = dropItem.quantity > 1 ? $"x{dropItem.quantity}" : "";

        // アイテムアイコン設定（マスターデータから取得）
        var itemIcon = itemSlot.transform.Find("ItemIcon")?.GetComponent<Image>();
        if (itemIcon != null)
        {
            var sprite = GetItemSprite(dropItem);
            if (sprite != null)
                itemIcon.sprite = sprite;
        }

        // 背景色設定（希少度による）
        var background = itemSlot.transform.Find("Background")?.GetComponent<Image>();
        if (background != null)
        {
            var rarity = GetItemRarity(dropItem);
            background.color = GetRarityColor(rarity);
        }

        // レアリティ表示
        var rarityText = itemSlot.transform.Find("Rarity")?.GetComponent<TextMeshProUGUI>();
        if (rarityText != null)
        {
            var rarity = GetItemRarity(dropItem);
            rarityText.text = GetRarityDisplayText(rarity);
            rarityText.color = GetRarityColor(rarity);
        }
    }

    /// <summary>
    /// 全アイテムを即座に表示
    /// </summary>
    private void DisplayAllItemsImmediately()
    {
        ClearItemSlots();

        for (int i = 0; i < currentDropItems.Count; i++)
        {
            var dropItem = currentDropItems[i];
            var itemSlot = CreateItemSlot(dropItem, i);
            if (itemSlot != null)
            {
                var canvasGroup = itemSlot.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = itemSlot.AddComponent<CanvasGroup>();

                canvasGroup.alpha = 1f;
                itemSlot.transform.localScale = Vector3.one;
            }
        }

        if (dropItemCanvasGroup != null)
        {
            dropItemCanvasGroup.alpha = 1f;
            dropItemCanvasGroup.interactable = true;
            dropItemCanvasGroup.blocksRaycasts = true;
        }

        UpdateHeaderInfo();
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
    /// アイテムスロットをクリア
    /// </summary>
    private void ClearItemSlots()
    {
        foreach (var slot in instantiatedItemSlots)
        {
            if (slot != null)
                Destroy(slot);
        }
        instantiatedItemSlots.Clear();
    }

    /// <summary>
    /// アニメーション停止
    /// </summary>
    private void StopDropAnimation()
    {
        if (dropAnimationCoroutine != null)
        {
            StopCoroutine(dropAnimationCoroutine);
            dropAnimationCoroutine = null;
        }
    }

    /// <summary>
    /// アイテムスプライトを取得
    /// </summary>
    private Sprite GetItemSprite(DropResult dropItem)
    {
        // マスターデータからアイコンを取得
        if (MasterDataManager.Instance != null)
        {
            if (dropItem.itemType == "EnhanceItem")
            {
                var enhanceData = MasterDataManager.Instance.GetEnhanceItemData(dropItem.itemId);
                // アイコンスプライトの取得ロジックを実装
                // 現在は省略
            }
            else if (dropItem.itemType == "SupportItem")
            {
                var supportData = MasterDataManager.Instance.GetSupportItemData(dropItem.itemId);
                // アイコンスプライトの取得ロジックを実装
                // 現在は省略
            }
        }
        return null; // デフォルトアイコンまたはnull
    }

    /// <summary>
    /// アイテム希少度を取得
    /// </summary>
    private ItemRarity GetItemRarity(DropResult dropItem)
    {
        // マスターデータから希少度を取得
        if (MasterDataManager.Instance != null)
        {
            if (dropItem.itemType == "EnhanceItem")
            {
                var enhanceData = MasterDataManager.Instance.GetEnhanceItemData(dropItem.itemId);
                if (enhanceData != null)
                {
                    // 希少度判定ロジック（強化値や効果から判定）
                    return ItemRarity.Common; // 仮の実装
                }
            }
            else if (dropItem.itemType == "SupportItem")
            {
                var supportData = MasterDataManager.Instance.GetSupportItemData(dropItem.itemId);
                if (supportData != null)
                {
                    // 希少度判定ロジック
                    return ItemRarity.Common; // 仮の実装
                }
            }
        }
        return ItemRarity.Common;
    }

    /// <summary>
    /// 希少度カラーを取得
    /// </summary>
    private Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => Color.white,
            ItemRarity.Rare => Color.blue,
            ItemRarity.Epic => Color.magenta,
            ItemRarity.Legendary => Color.yellow,
            _ => Color.white
        };
    }

    /// <summary>
    /// 希少度表示テキストを取得
    /// </summary>
    private string GetRarityDisplayText(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => "コモン",
            ItemRarity.Rare => "レア",
            ItemRarity.Epic => "エピック",
            ItemRarity.Legendary => "レジェンダリー",
            _ => ""
        };
    }

    /// <summary>
    /// レアアイテムかどうか判定
    /// </summary>
    private bool IsRareItem(ItemRarity rarity)
    {
        return rarity >= ItemRarity.Rare;
    }

    /// <summary>
    /// アイテムドロップエフェクトを再生
    /// </summary>
    private void PlayItemDropEffect(Vector3 position, ItemRarity rarity)
    {
        ParticleSystem effect = rarity switch
        {
            ItemRarity.Rare => rareItemEffect,
            ItemRarity.Epic => epicItemEffect,
            ItemRarity.Legendary => legendaryItemEffect,
            _ => commonItemEffect
        };

        if (effect != null)
        {
            effect.transform.position = position;
            effect.Play();
        }
    }

    /// <summary>
    /// アイテムドロップ音声を再生
    /// </summary>
    private void PlayItemDropSound(ItemRarity rarity)
    {
        AudioClip clip = IsRareItem(rarity) ? rareItemSound : itemDropSound;
        PlaySound(clip);
    }

    /// <summary>
    /// 音声再生
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (dropItemAudioSource != null && clip != null)
        {
            dropItemAudioSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region ログ・デバッグ

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[DropItemUI] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[DropItemUI] {message}");
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("ドロップアイテムテスト表示")]
    private void TestDropItemDisplay()
    {
        // テスト用のダミーデータ作成
        var testDropItems = new List<DropResult>
        {
            new DropResult { itemId = 1, itemType = "EnhanceItem", itemName = "強化石", quantity = 3 },
            new DropResult { itemId = 2, itemType = "EnhanceItem", itemName = "高級強化石", quantity = 1 },
            new DropResult { itemId = 3, itemType = "SupportItem", itemName = "回復薬", quantity = 5 }
        };

        ShowDropItems(testDropItems);
    }

    [ContextMenu("ドロップアイテムUI設定確認")]
    private void ValidateDropItemSetup()
    {
        DebugLog("=== ドロップアイテムUI設定確認 ===");
        DebugLog($"ドロップアイテムパネル: {(dropItemPanel != null ? "設定済み" : "未設定")}");
        DebugLog($"アイテムリスト親: {(itemListParent != null ? "設定済み" : "未設定")}");
        DebugLog($"アイテムスロットプレハブ: {(itemSlotPrefab != null ? "設定済み" : "未設定")}");
        DebugLog($"レアアイテムエフェクト: {(rareItemEffectPrefab != null ? "設定済み" : "未設定")}");
        DebugLog($"続行ボタン: {(continueButton != null ? "設定済み" : "未設定")}");
        DebugLog($"スキップボタン: {(skipButton != null ? "設定済み" : "未設定")}");
        DebugLog($"オーディオソース: {(dropItemAudioSource != null ? "設定済み" : "未設定")}");
        DebugLog($"コモンエフェクト: {(commonItemEffect != null ? "設定済み" : "未設定")}");
        DebugLog($"レアエフェクト: {(rareItemEffect != null ? "設定済み" : "未設定")}");
    }
#endif

    #endregion
}

#region データ構造

/// <summary>
/// アイテム希少度
/// </summary>
public enum ItemRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

#endregion