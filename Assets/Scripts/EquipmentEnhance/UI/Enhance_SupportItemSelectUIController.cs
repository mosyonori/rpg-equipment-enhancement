using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 補助材料選択UI制御クラス - プロパティ名修正版
/// 
/// 【責任】
/// - 補助材料選択UI制御
/// - CSV構造に対応したプロパティアクセス
/// - Service層との正確な連携
/// - 「使用しない」オプション対応
/// 
/// 【重要機能】
/// - 所持補助材料一覧表示（「使用しない」含む）
/// - 補助材料選択処理  
/// - 補助材料効果表示
/// - リアルタイムUI更新
/// </summary>
public class Enhance_SupportItemSelectUIController : MonoBehaviour
{
    [Header("UI Elements")]
    public Button supportItemSelectButton;
    public Image supportItemIconImage;
    public GameObject supportItemListPanel;
    public Transform supportItemListContent;
    public GameObject supportItemPrefab;
    public Transform supportEffectDisplay;
    public GameObject supportEffectTextPrefab;

    [Header("Button State Colors")]
    public Color enabledButtonColor = Color.white;
    public Color disabledButtonColor = Color.gray;

    // Service層
    private EnhanceDataService dataService = new EnhanceDataService();

    // 選択状態
    private SupportItemMasterData selectedSupportItem;

    // イベント
    public event System.Action<SupportItemMasterData> OnSupportItemSelected;

    #region Unity Lifecycle

    private void Start()
    {
        SetupButtons();
        ResetSelection();
    }

    private void OnDestroy()
    {
        RemoveEventListeners();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// ボタンイベント設定
    /// </summary>
    private void SetupButtons()
    {
        if (supportItemSelectButton != null)
        {
            supportItemSelectButton.onClick.AddListener(OnSupportItemSelectButtonClicked);
        }
    }

    /// <summary>
    /// イベントリスナー削除
    /// </summary>
    private void RemoveEventListeners()
    {
        if (supportItemSelectButton != null)
        {
            supportItemSelectButton.onClick.RemoveAllListeners();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 補助材料選択ボタンクリック処理
    /// </summary>
    public void OnSupportItemSelectButtonClicked()
    {
        ShowSupportItemList();
    }

    /// <summary>
    /// 操作可能状態設定
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (supportItemSelectButton != null)
        {
            supportItemSelectButton.interactable = interactable;

            // ボタンの色も変更
            ColorBlock colors = supportItemSelectButton.colors;
            colors.normalColor = interactable ? enabledButtonColor : disabledButtonColor;
            supportItemSelectButton.colors = colors;
        }
    }

    /// <summary>
    /// 選択状態リセット
    /// </summary>
    public void ResetSelection()
    {
        selectedSupportItem = null;
        UpdateSupportItemDisplay();
        UpdateSupportEffect();
    }

    /// <summary>
    /// 現在選択中の補助材料取得
    /// </summary>
    public SupportItemMasterData GetSelectedSupportItem()
    {
        return selectedSupportItem;
    }

    /// <summary>
    /// 補助材料が選択されているかどうか
    /// </summary>
    public bool HasSelectedItem()
    {
        return selectedSupportItem != null;
    }

    #endregion

    #region Support Item List Management

    /// <summary>
    /// 補助材料一覧表示
    /// </summary>
    private void ShowSupportItemList()
    {
        try
        {
            List<SupportItemDisplayData> supportItems = GetOwnedSupportItemsWithNone();

            // 既存のアイテムを削除
            ClearSupportItemList();

            // 補助材料アイテム生成（「使用しない」含む）
            CreateSupportItemListItems(supportItems);

            // パネル表示
            if (supportItemListPanel != null)
            {
                supportItemListPanel.SetActive(true);
            }

            Debug.Log($"[Enhance_SupportItemSelectUIController] 補助材料一覧表示: {supportItems.Count}個");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Enhance_SupportItemSelectUIController] 補助材料一覧表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 補助材料リストアイテム生成
    /// </summary>
    private void CreateSupportItemListItems(List<SupportItemDisplayData> supportItems)
    {
        foreach (var item in supportItems)
        {
            try
            {
                GameObject itemObj = Instantiate(supportItemPrefab, supportItemListContent);
                SupportItemListItemUI itemUI = itemObj.GetComponent<SupportItemListItemUI>();

                if (itemUI != null)
                {
                    itemUI.Setup(item, OnSupportItemClicked);
                }
                else
                {
                    Debug.LogWarning($"[Enhance_SupportItemSelectUIController] SupportItemListItemUIコンポーネントが見つかりません");

                    // フォールバック: 基本的なボタン設定
                    Button itemButton = itemObj.GetComponent<Button>();
                    if (itemButton != null)
                    {
                        itemButton.onClick.AddListener(() => OnSupportItemClicked(item));
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Enhance_SupportItemSelectUIController] 補助材料アイテム生成エラー: {item.support_item_name}, {e.Message}");
            }
        }
    }

    /// <summary>
    /// 補助材料リストクリア
    /// </summary>
    private void ClearSupportItemList()
    {
        if (supportItemListContent == null) return;

        foreach (Transform child in supportItemListContent)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 所持補助材料一覧取得（「使用しない」含む）
    /// </summary>
    private List<SupportItemDisplayData> GetOwnedSupportItemsWithNone()
    {
        List<SupportItemDisplayData> result = new List<SupportItemDisplayData>();

        // 「使用しない」オプションを最初に追加
        result.Add(new SupportItemDisplayData
        {
            support_item_id = -1,
            support_item_name = "使用しない",
            quantity = 1,
            isNoneOption = true
        });

        // 実際の補助材料を追加
        try
        {
            List<UserItem> ownedItems = dataService.GetOwnedSupportItems();

            if (ownedItems != null)
            {
                foreach (var item in ownedItems)
                {
                    if (item.quantity <= 0) continue; // 所持数0のアイテムはスキップ

                    SupportItemMasterData masterData = dataService.GetSupportItemMaster(item.item_id);
                    if (masterData != null)
                    {
                        result.Add(new SupportItemDisplayData
                        {
                            support_item_id = item.item_id,
                            support_item_name = masterData.support_item_name,
                            quantity = item.quantity,
                            isNoneOption = false
                        });
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Enhance_SupportItemSelectUIController] 補助材料データ取得エラー: {e.Message}");
        }

        return result;
    }

    #endregion

    #region Support Item Selection

    /// <summary>
    /// 補助材料クリック処理
    /// </summary>
    private void OnSupportItemClicked(SupportItemDisplayData item)
    {
        if (item == null)
        {
            Debug.LogWarning("[Enhance_SupportItemSelectUIController] 無効な補助材料が選択されました");
            return;
        }

        if (item.isNoneOption)
        {
            selectedSupportItem = null; // 「使用しない」を選択
            Debug.Log("[Enhance_SupportItemSelectUIController] 「使用しない」を選択");
        }
        else
        {
            selectedSupportItem = dataService.GetSupportItemMaster(item.support_item_id);
            Debug.Log($"[Enhance_SupportItemSelectUIController] 補助材料選択: {item.support_item_name}");
        }

        // UI更新
        UpdateSupportItemDisplay();
        UpdateSupportEffect();

        // パネルを閉じる
        if (supportItemListPanel != null)
        {
            supportItemListPanel.SetActive(false);
        }

        // イベント通知
        OnSupportItemSelected?.Invoke(selectedSupportItem);
    }

    #endregion

    #region Display Update

    /// <summary>
    /// 補助材料表示更新
    /// </summary>
    private void UpdateSupportItemDisplay()
    {
        if (supportItemIconImage == null) return;

        if (selectedSupportItem != null)
        {
            // Unity上のマスターデータからアイコンを取得
            supportItemIconImage.sprite = LoadSupportItemIcon(selectedSupportItem.support_item_id);
            supportItemIconImage.color = Color.white;
        }
        else
        {
            // 「使用しない」の場合
            supportItemIconImage.sprite = null;
            supportItemIconImage.color = Color.gray;
        }
    }

    /// <summary>
    /// 補助材料効果表示更新
    /// </summary>
    private void UpdateSupportEffect()
    {
        // 効果表示をクリア
        ClearSupportEffectDisplay();

        if (selectedSupportItem != null)
        {
            DisplaySupportEffects();
        }
        else
        {
            CreateEffectText("効果なし");
        }
    }

    /// <summary>
    /// 補助材料効果詳細表示
    /// </summary>
    private void DisplaySupportEffects()
    {
        bool hasAnyEffect = false;

        // 成功率増加効果
        if (selectedSupportItem.add_enhance_success_rate > 0)
        {
            CreateEffectText($"成功率: +{selectedSupportItem.add_enhance_success_rate}%");
            hasAnyEffect = true;
        }

        // 成功率減少効果
        if (selectedSupportItem.reduce_enhance_success_rate > 0)
        {
            CreateEffectText($"成功率: -{selectedSupportItem.reduce_enhance_success_rate}%");
            hasAnyEffect = true;
        }

        // 強化値倍率効果
        if (selectedSupportItem.multipl_enhanced_value > 1)
        {
            CreateEffectText($"強化値: ×{selectedSupportItem.multipl_enhanced_value}");
            hasAnyEffect = true;
        }

        // 強化値加算効果
        if (selectedSupportItem.add_enhanced_value > 0)
        {
            CreateEffectText($"強化値: +{selectedSupportItem.add_enhanced_value}");
            hasAnyEffect = true;
        }

        // ステータス倍率効果
        if (selectedSupportItem.multipl_status_up > 1)
        {
            CreateEffectText($"ステータス: ×{selectedSupportItem.multipl_status_up}");
            hasAnyEffect = true;
        }

        // 強化耐久増加効果
        if (selectedSupportItem.add_enhance_stamina > 0)
        {
            CreateEffectText($"強化耐久: +{selectedSupportItem.add_enhance_stamina}");
            hasAnyEffect = true;
        }

        // 強化耐久減少効果
        if (selectedSupportItem.reduce_enhance_stamina > 0)
        {
            CreateEffectText($"強化耐久: -{selectedSupportItem.reduce_enhance_stamina}");
            hasAnyEffect = true;
        }

        // 効果がない場合
        if (!hasAnyEffect)
        {
            CreateEffectText("特殊効果なし");
        }
    }

    /// <summary>
    /// 効果テキスト生成
    /// </summary>
    private void CreateEffectText(string effect)
    {
        if (supportEffectTextPrefab == null || supportEffectDisplay == null) return;

        try
        {
            GameObject textObj = Instantiate(supportEffectTextPrefab, supportEffectDisplay);
            Text textComponent = textObj.GetComponent<Text>();

            if (textComponent != null)
            {
                textComponent.text = effect;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Enhance_SupportItemSelectUIController] 効果テキスト生成エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 補助材料効果表示クリア
    /// </summary>
    private void ClearSupportEffectDisplay()
    {
        if (supportEffectDisplay == null) return;

        foreach (Transform child in supportEffectDisplay)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 補助材料アイコン読み込み
    /// </summary>
    private Sprite LoadSupportItemIcon(int supportItemId)
    {
        try
        {
            EnhanceDataService dataService = new EnhanceDataService();
            return dataService.GetSupportItemIcon(supportItemId);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Enhance_SupportItemSelectUIController] アイコン読み込み失敗: {e.Message}");
            return null;
        }
    }

    #endregion
}

/// <summary>
/// 補助材料リストアイテムUI - プロパティ名修正版
/// </summary>
public class SupportItemListItemUI : MonoBehaviour
{
    [Header("UI Components")]
    public Image iconImage;
    public Text nameText;
    public Text quantityText;
    public Button selectButton;

    private SupportItemDisplayData itemData;
    private System.Action<SupportItemDisplayData> onItemSelected;

    #region Public Methods

    /// <summary>
    /// 補助材料アイテムセットアップ
    /// </summary>
    public void Setup(SupportItemDisplayData data, System.Action<SupportItemDisplayData> onSelected)
    {
        itemData = data;
        onItemSelected = onSelected;

        // UI更新
        UpdateDisplay();

        // ボタンイベント設定
        SetupButton();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 表示更新
    /// </summary>
    private void UpdateDisplay()
    {
        if (itemData == null) return;

        // 名前表示
        if (nameText != null)
        {
            nameText.text = itemData.support_item_name;
        }

        // 「使用しない」オプションの場合
        if (itemData.isNoneOption)
        {
            SetupNoneOptionDisplay();
            return;
        }

        // 通常の補助材料の場合
        SetupNormalSupportItemDisplay();
    }

    /// <summary>
    /// 「使用しない」オプション表示設定
    /// </summary>
    private void SetupNoneOptionDisplay()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.color = Color.gray;
        }

        if (quantityText != null)
        {
            quantityText.text = "";
        }
    }

    /// <summary>
    /// 通常補助材料表示設定
    /// </summary>
    private void SetupNormalSupportItemDisplay()
    {
        // アイコン表示
        if (iconImage != null)
        {
            SupportItemMasterData masterData = GetSupportItemMasterData();
            if (masterData != null)
            {
                // Unity上のマスターデータからアイコンを取得（IDベース）
                iconImage.sprite = LoadIcon(masterData.support_item_id);
                iconImage.color = Color.white;
            }
        }

        // 数量表示
        if (quantityText != null)
        {
            quantityText.text = $"×{itemData.quantity}";
        }
    }

    /// <summary>
    /// 補助材料マスターデータ取得
    /// </summary>
    private SupportItemMasterData GetSupportItemMasterData()
    {
        if (itemData == null || itemData.isNoneOption)
        {
            return null;
        }

        try
        {
            // EnhanceDataServiceを使用してマスターデータを取得
            EnhanceDataService dataService = new EnhanceDataService();
            return dataService.GetSupportItemMaster(itemData.support_item_id);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SupportItemListItemUI] マスターデータ取得エラー: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// アイコン読み込み
    /// </summary>
    private Sprite LoadIcon(int supportItemId)
    {
        try
        {
            // Unity上のマスターデータからアイコンを読み込み（IDベース）
            return Resources.Load<Sprite>($"Icons/SupportItems/support_item_{supportItemId:D3}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SupportItemListItemUI] アイコン読み込み失敗: support_item_{supportItemId:D3}, {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// ボタン設定
    /// </summary>
    private void SetupButton()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }
    }

    /// <summary>
    /// 選択ボタンクリック処理
    /// </summary>
    private void OnSelectButtonClicked()
    {
        onItemSelected?.Invoke(itemData);
    }

    #endregion
}

/// <summary>
/// 補助材料表示用データクラス - プロパティ名修正版
/// </summary>
[System.Serializable]
public class SupportItemDisplayData
{
    public int support_item_id;
    public string support_item_name;
    public int quantity;
    public bool isNoneOption; // 「使用しない」フラグ
}