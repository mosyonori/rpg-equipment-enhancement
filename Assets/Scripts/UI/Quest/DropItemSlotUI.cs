using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ドロップアイテムスロットプレハブ制御クラス
/// 責任範囲：
/// - ドロップアイテムの基本情報表示
/// - アイテムアイコン・名前・数量表示
/// </summary>
public class DropItemSlotUI : MonoBehaviour
{
    [Header("基本情報表示")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI quantityText;

    [Header("確率表示")]
    [SerializeField] private TextMeshProUGUI dropRateText;
    [SerializeField] private Slider dropRateSlider;
    [SerializeField] private Image dropRateBackground;

    [Header("アイテムタイプ表示")]
    [SerializeField] private Image typeIcon;
    [SerializeField] private TextMeshProUGUI typeText;

    [Header("レアリティ表現")]
    [SerializeField] private Image rarityBorder;
    [SerializeField] private Image rarityBackground;

    [Header("カラー設定")]
    [SerializeField] private Color highDropRateColor = Color.green;
    [SerializeField] private Color mediumDropRateColor = Color.yellow;
    [SerializeField] private Color lowDropRateColor = Color.red;

    [Header("アイテムタイプカラー")]
    [SerializeField] private Color equipmentColor = Color.blue;
    [SerializeField] private Color enhanceItemColor = Color.yellow;
    [SerializeField] private Color supportItemColor = Color.red;
    [SerializeField] private Color defaultColor = Color.gray;

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showDropRate = true;
    [SerializeField] private bool showItemType = true;

    // 内部状態
    private DropItemData dropItemData;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネントの初期化
    /// </summary>
    private void InitializeComponents()
    {
        // 初期状態設定
        if (dropRateSlider != null)
        {
            dropRateSlider.minValue = 0f;
            dropRateSlider.maxValue = 100f;
            dropRateSlider.value = 0f;
        }

        // 非表示設定に応じてUI要素を制御
        if (!showDropRate)
        {
            if (dropRateText != null) dropRateText.gameObject.SetActive(false);
            if (dropRateSlider != null) dropRateSlider.gameObject.SetActive(false);
        }

        if (!showItemType)
        {
            if (typeIcon != null) typeIcon.gameObject.SetActive(false);
            if (typeText != null) typeText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ドロップアイテムスロットを初期化
    /// </summary>
    /// <param name="dropItem">ドロップアイテムデータ</param>
    public void Initialize(DropItemData dropItem)
    {
        try
        {
            if (dropItem == null)
            {
                LogError("DropItemDataがnullです");
                return;
            }

            this.dropItemData = dropItem;

            // 基本情報表示
            DisplayBasicInfo();

            // ドロップ率表示
            DisplayDropRate();

            // アイテムタイプ表示
            DisplayItemType();

            // アイコン読み込み
            LoadItemIcon();

            // レアリティ表現
            DisplayRarity();

            Log($"ドロップアイテムスロット初期化完了: {dropItemData.itemName}");
        }
        catch (Exception e)
        {
            LogError($"ドロップアイテムスロット初期化エラー: {e.Message}");
        }
    }

    #endregion

    #region 基本情報表示

    /// <summary>
    /// 基本情報を表示
    /// </summary>
    private void DisplayBasicInfo()
    {
        if (dropItemData == null) return;

        // アイテム名
        if (itemNameText != null)
        {
            itemNameText.text = dropItemData.itemName;
        }

        // 数量
        if (quantityText != null)
        {
            if (dropItemData.quantity > 1)
            {
                quantityText.text = $"x{dropItemData.quantity}";
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// ドロップ率を表示
    /// </summary>
    private void DisplayDropRate()
    {
        if (dropItemData == null || !showDropRate) return;

        // ドロップ率テキスト
        if (dropRateText != null)
        {
            dropRateText.text = $"{dropItemData.dropRate}%";
            dropRateText.color = GetDropRateColor(dropItemData.dropRate);
        }

        // ドロップ率スライダー
        if (dropRateSlider != null)
        {
            dropRateSlider.value = dropItemData.dropRate;

            // スライダーの色設定
            var fillImage = dropRateSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = GetDropRateColor(dropItemData.dropRate);
            }
        }

        // 背景色設定
        if (dropRateBackground != null)
        {
            Color bgColor = GetDropRateColor(dropItemData.dropRate);
            bgColor.a = 0.2f; // 透明度調整
            dropRateBackground.color = bgColor;
        }
    }

    /// <summary>
    /// ドロップ率に対応する色を取得
    /// </summary>
    /// <param name="dropRate">ドロップ率</param>
    /// <returns>対応する色</returns>
    private Color GetDropRateColor(int dropRate)
    {
        if (dropRate >= 70)
        {
            return highDropRateColor;
        }
        else if (dropRate >= 30)
        {
            return mediumDropRateColor;
        }
        else
        {
            return lowDropRateColor;
        }
    }

    /// <summary>
    /// アイテムタイプを表示
    /// </summary>
    private void DisplayItemType()
    {
        if (dropItemData == null || !showItemType) return;

        // タイプテキスト
        if (typeText != null)
        {
            string typeDisplayName = GetItemTypeDisplayName(dropItemData.itemType);
            typeText.text = typeDisplayName;
            typeText.color = GetItemTypeColor(dropItemData.itemType);
        }

        // タイプアイコン
        if (typeIcon != null)
        {
            LoadItemTypeIcon(dropItemData.itemType);
        }
    }

    /// <summary>
    /// アイテムタイプの表示名を取得
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    /// <returns>表示名</returns>
    private string GetItemTypeDisplayName(string itemType)
    {
        return itemType?.ToLower() switch
        {
            "equipment" => "装備",
            "enhanceitem" => "強化素材",
            "supportitem" => "補助アイテム",
            _ => "アイテム"
        };
    }

    /// <summary>
    /// アイテムタイプに対応する色を取得
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    /// <returns>対応する色</returns>
    private Color GetItemTypeColor(string itemType)
    {
        return itemType?.ToLower() switch
        {
            "equipment" => equipmentColor,
            "enhanceitem" => enhanceItemColor,
            "supportitem" => supportItemColor,
            _ => defaultColor
        };
    }

    #endregion

    #region アイコン読み込み

    /// <summary>
    /// アイテムアイコンを読み込み
    /// </summary>
    private void LoadItemIcon()
    {
        try
        {
            if (itemIcon == null || dropItemData == null) return;

            string iconPath = GetItemIconPath(dropItemData.itemType, dropItemData.itemId);
            var sprite = Resources.Load<Sprite>(iconPath);

            if (sprite != null)
            {
                itemIcon.sprite = sprite;
                itemIcon.gameObject.SetActive(true);
                Log($"アイテムアイコン読み込み成功: {iconPath}");
            }
            else
            {
                Log($"アイテムアイコンが見つかりません: {iconPath}");
                SetDefaultItemIcon();
            }
        }
        catch (Exception e)
        {
            LogError($"アイテムアイコン読み込みエラー: {e.Message}");
            SetDefaultItemIcon();
        }
    }

    /// <summary>
    /// アイテムアイコンパスを取得
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    /// <param name="itemId">アイテムID</param>
    /// <returns>アイコンパス</returns>
    private string GetItemIconPath(string itemType, int itemId)
    {
        string typeFolder = itemType?.ToLower() switch
        {
            "equipment" => "Equipment",
            "enhanceitem" => "EnhanceItem",
            "supportitem" => "SupportItem",
            _ => "Item"
        };

        return $"Icons/{typeFolder}/item_{itemId}";
    }

    /// <summary>
    /// タイプアイコンを読み込み
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    private void LoadItemTypeIcon(string itemType)
    {
        try
        {
            string iconPath = GetItemTypeIconPath(itemType);
            var sprite = Resources.Load<Sprite>(iconPath);

            if (sprite != null)
            {
                typeIcon.sprite = sprite;
                typeIcon.color = GetItemTypeColor(itemType);
                typeIcon.gameObject.SetActive(true);
            }
            else
            {
                typeIcon.gameObject.SetActive(false);
            }
        }
        catch (Exception e)
        {
            LogError($"タイプアイコン読み込みエラー: {e.Message}");
            typeIcon.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// アイテムタイプアイコンパスを取得
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    /// <returns>アイコンパス</returns>
    private string GetItemTypeIconPath(string itemType)
    {
        return itemType?.ToLower() switch
        {
            "equipment" => "Icons/ItemType/equipment_icon",
            "enhanceitem" => "Icons/ItemType/enhance_icon",
            "supportitem" => "Icons/ItemType/support_icon",
            _ => "Icons/ItemType/default_icon"
        };
    }

    /// <summary>
    /// デフォルトアイテムアイコンを設定
    /// </summary>
    private void SetDefaultItemIcon()
    {
        try
        {
            var defaultSprite = Resources.Load<Sprite>("Icons/Item/default_item");
            if (defaultSprite != null)
            {
                itemIcon.sprite = defaultSprite;
                itemIcon.gameObject.SetActive(true);
            }
            else
            {
                itemIcon.gameObject.SetActive(false);
            }
        }
        catch (Exception e)
        {
            LogError($"デフォルトアイコン設定エラー: {e.Message}");
            itemIcon.gameObject.SetActive(false);
        }
    }

    #endregion

    #region レアリティ表現

    /// <summary>
    /// レアリティを表現
    /// </summary>
    private void DisplayRarity()
    {
        if (dropItemData == null) return;

        // ドロップ率に基づいてレアリティを判定
        RarityLevel rarity = DetermineRarityFromDropRate(dropItemData.dropRate);
        Color rarityColor = GetRarityColorFromLevel(rarity);

        // レアリティ枠
        if (rarityBorder != null)
        {
            rarityBorder.color = rarityColor;
        }

        // レアリティ背景
        if (rarityBackground != null)
        {
            Color bgColor = rarityColor;
            bgColor.a = 0.3f; // 透明度調整
            rarityBackground.color = bgColor;
        }
    }

    /// <summary>
    /// ドロップ率からレアリティレベルを判定
    /// </summary>
    /// <param name="dropRate">ドロップ率</param>
    /// <returns>レアリティレベル</returns>
    private RarityLevel DetermineRarityFromDropRate(int dropRate)
    {
        if (dropRate >= 80)
        {
            return RarityLevel.Common;
        }
        else if (dropRate >= 50)
        {
            return RarityLevel.Uncommon;
        }
        else if (dropRate >= 20)
        {
            return RarityLevel.Rare;
        }
        else if (dropRate >= 5)
        {
            return RarityLevel.Epic;
        }
        else
        {
            return RarityLevel.Legendary;
        }
    }

    /// <summary>
    /// レアリティレベルに対応する色を取得
    /// </summary>
    /// <param name="rarity">レアリティレベル</param>
    /// <returns>レアリティカラー</returns>
    private Color GetRarityColorFromLevel(RarityLevel rarity)
    {
        return rarity switch
        {
            RarityLevel.Common => Color.white,
            RarityLevel.Uncommon => Color.green,
            RarityLevel.Rare => Color.blue,
            RarityLevel.Epic => Color.magenta,
            RarityLevel.Legendary => Color.yellow,
            _ => Color.gray
        };
    }

    /// <summary>
    /// レアリティレベル列挙型
    /// </summary>
    private enum RarityLevel
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    #endregion

    #region ユーティリティ

    /// <summary>
    /// ドロップアイテムデータを取得
    /// </summary>
    /// <returns>ドロップアイテムデータ</returns>
    public DropItemData GetDropItemData()
    {
        return dropItemData;
    }

    /// <summary>
    /// スロットの有効性をチェック
    /// </summary>
    /// <returns>有効な場合true</returns>
    public bool IsValidSlot()
    {
        return dropItemData != null && dropItemData.itemId > 0;
    }

    /// <summary>
    /// アイテム情報の更新
    /// </summary>
    /// <param name="updatedDropItem">更新されたドロップアイテムデータ</param>
    public void UpdateDropItemData(DropItemData updatedDropItem)
    {
        if (updatedDropItem == null || updatedDropItem.itemId != dropItemData?.itemId)
        {
            LogError("無効なドロップアイテムデータ更新要求");
            return;
        }

        Initialize(updatedDropItem);
    }

    /// <summary>
    /// 表示設定を変更
    /// </summary>
    /// <param name="showDropRateParam">ドロップ率表示フラグ</param>
    /// <param name="showItemTypeParam">アイテムタイプ表示フラグ</param>
    public void UpdateDisplaySettings(bool showDropRateParam, bool showItemTypeParam)
    {
        showDropRate = showDropRateParam;
        showItemType = showItemTypeParam;

        // UI要素の表示/非表示を更新
        if (dropRateText != null) dropRateText.gameObject.SetActive(showDropRate);
        if (dropRateSlider != null) dropRateSlider.gameObject.SetActive(showDropRate);
        if (typeIcon != null) typeIcon.gameObject.SetActive(showItemType);
        if (typeText != null) typeText.gameObject.SetActive(showItemType);

        // 表示を再構成
        if (dropItemData != null)
        {
            DisplayDropRate();
            DisplayItemType();
        }
    }

    /// <summary>
    /// ドロップ期待値を計算
    /// </summary>
    /// <returns>ドロップ期待値</returns>
    public float CalculateExpectedDrops()
    {
        if (dropItemData == null) return 0f;
        return (dropItemData.dropRate / 100f) * dropItemData.quantity;
    }

    /// <summary>
    /// デバッグ情報を取得
    /// </summary>
    /// <returns>デバッグ用文字列</returns>
    public string GetDebugInfo()
    {
        if (dropItemData == null) return "DropItemData: null";

        return $"DropItem[{dropItemData.itemId}] {dropItemData.itemName} - " +
               $"Type: {dropItemData.itemType}, Quantity: {dropItemData.quantity}, " +
               $"DropRate: {dropItemData.dropRate}%, Expected: {CalculateExpectedDrops():F2}";
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[DropItemSlotUI] {message}");
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[DropItemSlotUI] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("ドロップアイテム情報をログ出力")]
    private void LogDropItemInfo()
    {
        Log(GetDebugInfo());
    }

    [ContextMenu("ドロップ期待値を計算")]
    private void CalculateAndLogExpectedDrops()
    {
        float expected = CalculateExpectedDrops();
        Log($"ドロップ期待値: {expected:F2}");
    }

    [ContextMenu("レアリティ表示をテスト")]
    private void TestRarityDisplay()
    {
        if (dropItemData != null)
        {
            DisplayRarity();
            Log($"レアリティ表示テスト: ドロップ率{dropItemData.dropRate}%");
        }
    }

    [ContextMenu("表示設定をトグル")]
    private void ToggleDisplaySettings()
    {
        UpdateDisplaySettings(!showDropRate, !showItemType);
        Log($"表示設定変更: DropRate={showDropRate}, ItemType={showItemType}");
    }

    private void OnValidate()
    {
        // エディター上での設定変更を即座に反映
        if (Application.isPlaying && dropItemData != null)
        {
            DisplayDropRate();
            DisplayItemType();
            DisplayRarity();
        }
    }
#endif

    #endregion
}