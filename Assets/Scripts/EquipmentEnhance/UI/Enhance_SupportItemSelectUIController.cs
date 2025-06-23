using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 補助材料選択UI制御 - CS0162警告修正版
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

    private EnhanceDataService dataService = new EnhanceDataService();
    private SupportItemMasterData selectedSupportItem;

    public event System.Action<SupportItemMasterData> OnSupportItemSelected;

    private void Start()
    {
        SetupButtons();
        ResetSelection();
    }

    private void SetupButtons()
    {
        if (supportItemSelectButton != null)
        {
            supportItemSelectButton.onClick.AddListener(OnSupportItemSelectButtonClicked);
        }
    }

    public void OnSupportItemSelectButtonClicked()
    {
        ShowSupportItemList();
    }

    private void ShowSupportItemList()
    {
        List<SupportItemDisplayData> supportItems = GetOwnedSupportItemsWithNone();

        // 既存のアイテムを削除
        foreach (Transform child in supportItemListContent)
        {
            Destroy(child.gameObject);
        }

        // 補助材料アイテム生成（「使用しない」含む）
        foreach (var item in supportItems)
        {
            GameObject itemObj = Instantiate(supportItemPrefab, supportItemListContent);
            SupportItemListItemUI itemUI = itemObj.GetComponent<SupportItemListItemUI>();

            if (itemUI != null)
            {
                itemUI.Setup(item, OnSupportItemClicked);
            }
        }

        supportItemListPanel.SetActive(true);
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
        List<UserItem> ownedItems = dataService.GetOwnedSupportItems();
        foreach (var item in ownedItems)
        {
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

        return result;
    }

    private void OnSupportItemClicked(SupportItemDisplayData item)
    {
        if (item.isNoneOption)
        {
            selectedSupportItem = null; // 「使用しない」を選択
        }
        else
        {
            selectedSupportItem = dataService.GetSupportItemMaster(item.support_item_id);
        }

        // UI更新
        UpdateSupportItemDisplay();
        UpdateSupportEffect();

        // パネルを閉じる
        supportItemListPanel.SetActive(false);

        // イベント通知
        OnSupportItemSelected?.Invoke(selectedSupportItem);
    }

    private void UpdateSupportItemDisplay()
    {
        if (selectedSupportItem != null)
        {
            // アイコン表示（CSVに合わせてプロパティ名修正）
            supportItemIconImage.sprite = LoadSupportItemIcon(selectedSupportItem.enhance_item_icon_path);
            supportItemIconImage.color = Color.white;
        }
        else
        {
            // 「使用しない」の場合
            supportItemIconImage.sprite = null;
            supportItemIconImage.color = Color.gray;
        }
    }

    private void UpdateSupportEffect()
    {
        // 効果表示をクリア
        foreach (Transform child in supportEffectDisplay)
        {
            Destroy(child.gameObject);
        }

        if (selectedSupportItem != null)
        {
            DisplaySupportEffects();
        }
        else
        {
            CreateEffectText("効果なし");
        }
    }

    private void DisplaySupportEffects()
    {
        if (selectedSupportItem.add_enhance_success_rate > 0)
            CreateEffectText($"成功率: +{selectedSupportItem.add_enhance_success_rate}%");

        if (selectedSupportItem.reduce_enhance_success_rate > 0)
            CreateEffectText($"成功率: -{selectedSupportItem.reduce_enhance_success_rate}%");

        if (selectedSupportItem.multipl_enhanced_value > 1)
            CreateEffectText($"強化値: x{selectedSupportItem.multipl_enhanced_value}");

        if (selectedSupportItem.add_enhanced_value > 0)
            CreateEffectText($"強化値: +{selectedSupportItem.add_enhanced_value}");

        if (selectedSupportItem.multipl_status_up > 1)
            CreateEffectText($"ステータス: x{selectedSupportItem.multipl_status_up}");

        if (selectedSupportItem.add_enhance_stamina > 0)
            CreateEffectText($"強化耐久: +{selectedSupportItem.add_enhance_stamina}");
    }

    private void CreateEffectText(string effect)
    {
        if (supportEffectTextPrefab != null && supportEffectDisplay != null)
        {
            GameObject textObj = Instantiate(supportEffectTextPrefab, supportEffectDisplay);
            Text textComponent = textObj.GetComponent<Text>();
            if (textComponent != null)
            {
                textComponent.text = effect;
            }
        }
    }

    public void ResetSelection()
    {
        selectedSupportItem = null;

        if (supportItemIconImage != null)
        {
            supportItemIconImage.sprite = null;
            supportItemIconImage.color = Color.gray;
        }

        // 効果表示をクリア
        if (supportEffectDisplay != null)
        {
            foreach (Transform child in supportEffectDisplay)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private Sprite LoadSupportItemIcon(string iconPath)
    {
        // アイコン読み込み実装
        if (string.IsNullOrEmpty(iconPath))
            return null;

        // Resources フォルダからアイコンを読み込み
        return Resources.Load<Sprite>($"Icons/SupportItems/{iconPath}");
    }

    public void SetInteractable(bool interactable)
    {
        if (supportItemSelectButton != null)
        {
            supportItemSelectButton.interactable = interactable;
        }
    }
}

/// <summary>
/// 補助材料リストアイテムUI - CS0162警告修正版
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

    public void Setup(SupportItemDisplayData data, System.Action<SupportItemDisplayData> onSelected)
    {
        itemData = data;
        onItemSelected = onSelected;

        // UI更新
        UpdateDisplay();

        // ボタンイベント設定
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }
    }

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
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = Color.gray;
            }

            if (quantityText != null)
            {
                quantityText.text = "";
            }
            return; // 「使用しない」の場合はここで処理終了
        }

        // 通常の補助材料の場合
        DisplayNormalSupportItem();
    }

    private void DisplayNormalSupportItem()
    {
        // アイコン表示
        if (iconImage != null)
        {
            SupportItemMasterData masterData = GetSupportItemMasterData();
            if (masterData != null)
            {
                // CSVに合わせてプロパティ名修正
                iconImage.sprite = LoadIcon(masterData.enhance_item_icon_path);
                iconImage.color = Color.white;
            }
        }

        // 数量表示
        if (quantityText != null)
        {
            quantityText.text = $"x{itemData.quantity}";
        }
    }

    private SupportItemMasterData GetSupportItemMasterData()
    {
        // ⚠️ 修正前の問題箇所：return null; の後に到達不可能なコードがあった
        // 修正後：条件分岐を明確にして到達不可能なコードを排除

        if (itemData == null || itemData.isNoneOption)
        {
            return null;
        }

        // EnhanceDataServiceを使用してマスターデータを取得
        EnhanceDataService dataService = new EnhanceDataService();
        return dataService.GetSupportItemMaster(itemData.support_item_id);
    }

    private Sprite LoadIcon(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath))
            return null;

        return Resources.Load<Sprite>($"Icons/SupportItems/{iconPath}");
    }

    private void OnSelectButtonClicked()
    {
        onItemSelected?.Invoke(itemData);
    }
}

/// <summary>
/// 補助材料表示用データクラス
/// </summary>
[System.Serializable]
public class SupportItemDisplayData
{
    public int support_item_id;
    public string support_item_name;
    public int quantity;
    public bool isNoneOption; // 「使用しない」フラグ
}