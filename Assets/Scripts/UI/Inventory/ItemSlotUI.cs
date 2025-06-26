using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// アイテムスロットUI表示コンポーネント
/// </summary>
public class ItemSlotUI : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Button slotButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image rarityFrame;
    [SerializeField] private Image selectionFrame; // 追加: 選択フレーム
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI stackText;
    [SerializeField] private GameObject lockMark;
    [SerializeField] private GameObject newMark;
    [SerializeField] private GameObject attributeIcon;

    [Header("レアリティ色設定")]
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = Color.magenta;
    [SerializeField] private Color legendaryColor = Color.yellow;

    [Header("選択状態設定")]
    [SerializeField] private Color selectedFrameColor = Color.cyan;
    [SerializeField] private Color normalFrameColor = Color.clear;

    [Header("属性アイコン")]
    [SerializeField] private Sprite fireIcon;
    [SerializeField] private Sprite waterIcon;
    [SerializeField] private Sprite windIcon;
    [SerializeField] private Sprite earthIcon;
    [SerializeField] private Image attributeIconImage;

    // イベント
    public System.Action<UserItemData> OnSlotClicked;
    public System.Action<UserItemData> OnSlotLongPressed;

    // データ
    private UserItemData itemData;
    private object masterData; // EnhanceItemMasterData または SupportItemMasterData

    #region Unity Lifecycle

    private void Awake()
    {
        // ボタンイベント設定
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClick);
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// アイテムデータを設定して表示更新
    /// </summary>
    public void SetItemData(UserItemData item)
    {
        itemData = item;

        if (item == null)
        {
            SetEmpty();
            return;
        }

        // マスターデータ取得
        LoadMasterData();

        if (masterData == null)
        {
            SetEmpty();
            Debug.LogError($"アイテムマスターデータが見つかりません: Type={item.itemType}, ID={item.itemMasterId}");
            return;
        }

        UpdateDisplay();
    }

    /// <summary>
    /// 空のスロット表示
    /// </summary>
    public void SetEmpty()
    {
        itemData = null;
        masterData = null;

        // UI要素を非表示/初期化
        if (iconImage != null) iconImage.sprite = null;
        if (nameText != null) nameText.text = "";
        if (quantityText != null) quantityText.text = "";
        if (stackText != null) stackText.text = "";
        if (lockMark != null) lockMark.SetActive(false);
        if (newMark != null) newMark.SetActive(false);
        if (attributeIcon != null) attributeIcon.SetActive(false);
        if (backgroundImage != null) backgroundImage.color = Color.gray;
        if (rarityFrame != null) rarityFrame.color = commonColor;

        // 選択フレームを非表示
        SetSelected(false);

        // ボタンを無効化
        if (slotButton != null) slotButton.interactable = false;
    }

    /// <summary>
    /// アイテムデータを取得
    /// </summary>
    public UserItemData GetItemData()
    {
        return itemData;
    }

    /// <summary>
    /// 選択状態を設定
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (selectionFrame != null)
        {
            selectionFrame.gameObject.SetActive(selected);
            if (selected)
            {
                selectionFrame.color = selectedFrameColor;
            }
        }

        // 背景色も変更（フォールバック）
        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? selectedFrameColor : Color.white;
        }
    }

    #endregion

    #region 内部メソッド

    private void LoadMasterData()
    {
        var masterDataManager = MasterDataManager.Instance;
        if (masterDataManager == null) return;

        if (itemData.itemType == ItemType.EnhanceItem)
        {
            masterData = masterDataManager.GetEnhanceItemData(itemData.itemMasterId);
        }
        else if (itemData.itemType == ItemType.SupportItem)
        {
            masterData = masterDataManager.GetSupportItemData(itemData.itemMasterId);
        }
    }

    private void UpdateDisplay()
    {
        // ボタンを有効化
        if (slotButton != null) slotButton.interactable = true;

        // アイコン設定
        UpdateIcon();

        // 名前設定
        if (nameText != null)
        {
            nameText.text = GetItemName();
        }

        // 数量表示
        if (quantityText != null)
        {
            quantityText.text = itemData.quantity.ToString();
            quantityText.gameObject.SetActive(itemData.quantity > 1);
        }

        // スタック情報表示
        if (stackText != null)
        {
            if (itemData.quantity >= itemData.maxStackQuantity)
            {
                stackText.text = "MAX";
                stackText.color = Color.red;
            }
            else
            {
                stackText.text = $"{itemData.quantity}/{itemData.maxStackQuantity}";
                stackText.color = Color.white;
            }
        }

        // レアリティフレーム
        if (rarityFrame != null)
        {
            rarityFrame.color = GetRarityColor(GetItemRarity());
        }

        // 属性アイコン
        UpdateAttributeIcon();

        // 状態マーク更新
        UpdateStatusMarks();

        // 背景色
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.white;
        }
    }

    private void UpdateIcon()
    {
        if (iconImage == null) return;

        Sprite icon = null;

        if (masterData is EnhanceItemMasterData enhanceItem)
        {
            icon = enhanceItem.enhanceItemIcon;
        }
        else if (masterData is SupportItemMasterData supportItem)
        {
            icon = supportItem.supportItemIcon;
        }

        iconImage.sprite = icon;
        iconImage.gameObject.SetActive(icon != null);
    }

    private void UpdateAttributeIcon()
    {
        if (attributeIcon == null || attributeIconImage == null) return;

        AttributeType attribute = GetItemAttribute();

        if (attribute == AttributeType.None)
        {
            attributeIcon.SetActive(false);
            return;
        }

        Sprite attributeSprite = attribute switch
        {
            AttributeType.Fire => fireIcon,
            AttributeType.Water => waterIcon,
            AttributeType.Wind => windIcon,
            AttributeType.Earth => earthIcon,
            _ => null
        };

        if (attributeSprite != null)
        {
            attributeIconImage.sprite = attributeSprite;
            attributeIcon.SetActive(true);
        }
        else
        {
            attributeIcon.SetActive(false);
        }
    }

    private void UpdateStatusMarks()
    {
        // ロックマーク
        if (lockMark != null)
        {
            lockMark.SetActive(itemData.isLocked);
        }

        // 新規マーク
        if (newMark != null)
        {
            newMark.SetActive(itemData.isNew);
        }
    }

    private string GetItemName()
    {
        if (masterData is EnhanceItemMasterData enhanceItem)
        {
            return enhanceItem.enhanceItemName;
        }
        else if (masterData is SupportItemMasterData supportItem)
        {
            return supportItem.supportItemName;
        }
        return "Unknown Item";
    }

    private RarityType GetItemRarity()
    {
        if (masterData is EnhanceItemMasterData enhanceItem)
        {
            return enhanceItem.rarity;
        }
        else if (masterData is SupportItemMasterData supportItem)
        {
            return supportItem.rarity;
        }
        return RarityType.Common;
    }

    private AttributeType GetItemAttribute()
    {
        if (masterData is EnhanceItemMasterData enhanceItem)
        {
            return enhanceItem.attributeType;
        }
        else if (masterData is SupportItemMasterData supportItem)
        {
            return supportItem.attributeType;
        }
        return AttributeType.None;
    }

    private Color GetRarityColor(RarityType rarity)
    {
        return rarity switch
        {
            RarityType.Common => commonColor,
            RarityType.Rare => rareColor,
            RarityType.Epic => epicColor,
            RarityType.Legendary => legendaryColor,
            _ => commonColor
        };
    }

    private void OnSlotClick()
    {
        if (itemData != null)
        {
            OnSlotClicked?.Invoke(itemData);
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("テストデータ設定")]
    private void SetTestData()
    {
        // テスト用のダミーデータを設定
        var testItem = new UserItemData
        {
            itemType = ItemType.EnhanceItem,
            itemMasterId = 1,
            quantity = 5,
            maxStackQuantity = 99,
            isLocked = false,
            isNew = true
        };

        SetItemData(testItem);
    }

    [ContextMenu("空スロット設定")]
    private void SetEmptySlot()
    {
        SetEmpty();
    }
#endif

    #endregion
}