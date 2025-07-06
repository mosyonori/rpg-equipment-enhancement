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

            Log($"ドロップアイテムスロット初期化完了: {GetItemDisplayName()}");
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

        // アイテム名（修正：MasterDataManagerから取得）
        if (itemNameText != null)
        {
            string displayName = GetItemDisplayName();
            itemNameText.text = displayName;
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
    /// アイテム表示名を取得（修正：MasterDataManagerから実際の名前を取得）
    /// </summary>
    /// <returns>アイテム表示名</returns>
    private string GetItemDisplayName()
    {
        if (dropItemData == null) return "不明なアイテム";

        // MasterDataManagerが利用可能かチェック
        if (MasterDataManager.Instance == null)
        {
            LogError("MasterDataManagerが利用できません");
            return $"{dropItemData.itemType} ID:{dropItemData.itemId}";
        }

        // アイテムタイプに応じてマスターデータから名前を取得
        return dropItemData.itemType?.ToLower() switch
        {
            "equipment" => GetEquipmentName(dropItemData.itemId),
            "enhanceitem" => GetEnhanceItemName(dropItemData.itemId),
            "supportitem" => GetSupportItemName(dropItemData.itemId),
            _ => dropItemData.itemName ?? $"{dropItemData.itemType} ID:{dropItemData.itemId}"
        };
    }

    /// <summary>
    /// 装備名をMasterDataManagerから取得
    /// </summary>
    /// <param name="equipmentId">装備ID</param>
    /// <returns>装備名</returns>
    private string GetEquipmentName(int equipmentId)
    {
        try
        {
            var equipmentData = MasterDataManager.Instance.GetEquipmentData(equipmentId);
            if (equipmentData != null)
            {
                return equipmentData.equipmentName;
            }
            else
            {
                LogError($"装備マスターデータが見つかりません: ID {equipmentId}");
                return $"装備 ID:{equipmentId}";
            }
        }
        catch (Exception e)
        {
            LogError($"装備名取得エラー: {e.Message}");
            return $"装備 ID:{equipmentId}";
        }
    }

    /// <summary>
    /// 強化アイテム名をMasterDataManagerから取得
    /// </summary>
    /// <param name="itemId">強化アイテムID</param>
    /// <returns>強化アイテム名</returns>
    private string GetEnhanceItemName(int itemId)
    {
        try
        {
            var enhanceItemData = MasterDataManager.Instance.GetEnhanceItemData(itemId);
            if (enhanceItemData != null)
            {
                return enhanceItemData.enhanceItemName;
            }
            else
            {
                LogError($"強化アイテムマスターデータが見つかりません: ID {itemId}");
                return $"強化素材 ID:{itemId}";
            }
        }
        catch (Exception e)
        {
            LogError($"強化アイテム名取得エラー: {e.Message}");
            return $"強化素材 ID:{itemId}";
        }
    }

    /// <summary>
    /// 補助アイテム名をMasterDataManagerから取得
    /// </summary>
    /// <param name="itemId">補助アイテムID</param>
    /// <returns>補助アイテム名</returns>
    private string GetSupportItemName(int itemId)
    {
        try
        {
            var supportItemData = MasterDataManager.Instance.GetSupportItemData(itemId);
            if (supportItemData != null)
            {
                return supportItemData.supportItemName;
            }
            else
            {
                LogError($"補助アイテムマスターデータが見つかりません: ID {itemId}");
                return $"補助アイテム ID:{itemId}";
            }
        }
        catch (Exception e)
        {
            LogError($"補助アイテム名取得エラー: {e.Message}");
            return $"補助アイテム ID:{itemId}";
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

    #region アイコン読み込み（修正版）

    /// <summary>
    /// アイテムアイコンを読み込み（修正：正しいフォルダパス対応）
    /// </summary>
    private void LoadItemIcon()
    {
        try
        {
            if (itemIcon == null || dropItemData == null) return;

            string iconPath = GetItemIconPathFromMasterData(dropItemData.itemType, dropItemData.itemId);
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
    /// MasterDataManagerからアイテムアイコンパスを取得（修正：正しいパス生成）
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    /// <param name="itemId">アイテムID</param>
    /// <returns>アイコンパス</returns>
    private string GetItemIconPathFromMasterData(string itemType, int itemId)
    {
        if (MasterDataManager.Instance == null)
        {
            LogError("MasterDataManagerが利用できません - フォールバック処理");
            return GetFallbackIconPath(itemType, itemId);
        }

        try
        {
            string iconPath = itemType?.ToLower() switch
            {
                "equipment" => GetEquipmentIconPath(itemId),
                "enhanceitem" => GetEnhanceItemIconPath(itemId),
                "enhance" => GetEnhanceItemIconPath(itemId), // "enhance"タイプも対応
                "supportitem" => GetSupportItemIconPath(itemId),
                "support" => GetSupportItemIconPath(itemId), // "support"タイプも対応
                _ => GetFallbackIconPath(itemType, itemId)
            };

            Log($"アイコンパス取得: Type={itemType}, ID={itemId}, Path={iconPath}");
            return iconPath;
        }
        catch (Exception e)
        {
            LogError($"マスターデータからのアイコンパス取得エラー: {e.Message}");
            return GetFallbackIconPath(itemType, itemId);
        }
    }

    /// <summary>
    /// 装備アイコンパスを取得（修正：装備タイプ別フォルダ対応）
    /// </summary>
    /// <param name="equipmentId">装備ID</param>
    /// <returns>アイコンパス</returns>
    private string GetEquipmentIconPath(int equipmentId)
    {
        var equipmentData = MasterDataManager.Instance.GetEquipmentData(equipmentId);
        if (equipmentData != null)
        {
            Log($"装備マスターデータ取得成功: ID={equipmentId}, Name={equipmentData.equipmentName}, Type={equipmentData.equipmentType}");

            // 装備タイプに応じたフォルダを決定
            string equipmentTypeFolder = GetEquipmentTypeFolder(equipmentData.equipmentType);

            if (!string.IsNullOrEmpty(equipmentData.equipmentIconPath))
            {
                Log($"装備アイコンパス取得成功: ID={equipmentId}, Path={equipmentData.equipmentIconPath}");
                return equipmentData.equipmentIconPath;
            }
            else
            {
                Log($"装備マスターデータにアイコンパスが設定されていません: ID={equipmentId}");

                // equipmentIconプロパティも確認
                if (equipmentData.equipmentIcon != null)
                {
                    Log($"装備にSpriteが直接設定されています: ID={equipmentId}, SpriteName={equipmentData.equipmentIcon.name}");
                    // 装備タイプ別フォルダにSpriteが配置されている前提
                    return $"Icons/{equipmentTypeFolder}/{equipmentData.equipmentIcon.name}";
                }
                else
                {
                    Log($"装備にSpriteも設定されていません: ID={equipmentId}");
                    // 修正：正しいファイル名パターンを使用
                    return $"Icons/{equipmentTypeFolder}/{equipmentTypeFolder}_{equipmentId}";
                }
            }
        }
        else
        {
            LogError($"装備マスターデータが見つかりません: ID={equipmentId}");
            // フォールバックとして武器フォルダを使用
            return $"Icons/Weapon/Weapon_{equipmentId}";
        }
    }

    /// <summary>
    /// 装備タイプに対応するフォルダ名を取得
    /// </summary>
    /// <param name="equipmentType">装備タイプ</param>
    /// <returns>フォルダ名</returns>
    private string GetEquipmentTypeFolder(EquipmentType equipmentType)
    {
        return equipmentType switch
        {
            EquipmentType.Weapon => "Weapon",
            EquipmentType.Armor => "Armor",
            EquipmentType.Accessory => "Accessory",
            _ => "Weapon" // デフォルトは武器
        };
    }

    /// <summary>
    /// 強化アイテムアイコンパスを取得（修正：武器と同じ手法を適用）
    /// </summary>
    /// <param name="itemId">強化アイテムID</param>
    /// <returns>アイコンパス</returns>
    private string GetEnhanceItemIconPath(int itemId)
    {
        var enhanceItemData = MasterDataManager.Instance.GetEnhanceItemData(itemId);
        if (enhanceItemData != null)
        {
            Log($"強化アイテムマスターデータ取得成功: ID={itemId}, Name={enhanceItemData.enhanceItemName}");

            if (!string.IsNullOrEmpty(enhanceItemData.enhanceItemIconPath))
            {
                Log($"強化アイテムアイコンパス取得成功: ID={itemId}, Path={enhanceItemData.enhanceItemIconPath}");
                return enhanceItemData.enhanceItemIconPath;
            }
            else
            {
                Log($"強化アイテムマスターデータにアイコンパスが設定されていません: ID={itemId}");

                // enhanceItemIconプロパティも確認
                if (enhanceItemData.enhanceItemIcon != null)
                {
                    Log($"強化アイテムにSpriteが直接設定されています: ID={itemId}, SpriteName={enhanceItemData.enhanceItemIcon.name}");
                    // 統一フォルダ構造に対応
                    return $"Icons/EnhanceItem/{enhanceItemData.enhanceItemIcon.name}";
                }
                else
                {
                    Log($"強化アイテムにSpriteも設定されていません: ID={itemId}");
                    // 武器と同じ手法：正しいファイル名パターンを使用
                    return $"Icons/EnhanceItem/Enhance_{itemId}";
                }
            }
        }
        else
        {
            LogError($"強化アイテムマスターデータが見つかりません: ID={itemId}");
        }

        // フォールバック：武器と同じ手法
        string fallbackPath = $"Icons/EnhanceItem/Enhance_{itemId}";
        Log($"強化アイテムアイコンフォールバック: {fallbackPath}");
        return fallbackPath;
    }

    /// <summary>
    /// 補助アイテムアイコンパスを取得（修正：武器と同じ手法を適用）
    /// </summary>
    /// <param name="itemId">補助アイテムID</param>
    /// <returns>アイコンパス</returns>
    private string GetSupportItemIconPath(int itemId)
    {
        var supportItemData = MasterDataManager.Instance.GetSupportItemData(itemId);
        if (supportItemData != null)
        {
            Log($"補助アイテムマスターデータ取得成功: ID={itemId}, Name={supportItemData.supportItemName}");

            if (!string.IsNullOrEmpty(supportItemData.supportItemIconPath))
            {
                Log($"補助アイテムアイコンパス取得成功: ID={itemId}, Path={supportItemData.supportItemIconPath}");
                return supportItemData.supportItemIconPath;
            }
            else
            {
                Log($"補助アイテムマスターデータにアイコンパスが設定されていません: ID={itemId}");

                // supportItemIconプロパティも確認
                if (supportItemData.supportItemIcon != null)
                {
                    Log($"補助アイテムにSpriteが直接設定されています: ID={itemId}, SpriteName={supportItemData.supportItemIcon.name}");
                    // 統一フォルダ構造に対応
                    return $"Icons/SupportItem/{supportItemData.supportItemIcon.name}";
                }
                else
                {
                    Log($"補助アイテムにSpriteも設定されていません: ID={itemId}");
                    // 武器と同じ手法：正しいファイル名パターンを使用
                    return $"Icons/SupportItem/Support_{itemId}";
                }
            }
        }
        else
        {
            LogError($"補助アイテムマスターデータが見つかりません: ID={itemId}");
        }

        // フォールバック：武器と同じ手法
        string fallbackPath = $"Icons/SupportItem/Support_{itemId}";
        Log($"補助アイテムアイコンフォールバック: {fallbackPath}");
        return fallbackPath;
    }

    /// <summary>
    /// フォールバックアイコンパスを取得（修正：正しいファイル名パターン対応）
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    /// <param name="itemId">アイテムID</param>
    /// <returns>アイコンパス</returns>
    private string GetFallbackIconPath(string itemType, int itemId)
    {
        string fallbackPath = itemType?.ToLower() switch
        {
            "equipment" => $"Icons/Weapon/Weapon_{itemId}", // 装備のフォールバックは武器フォルダ
            "enhanceitem" => $"Icons/EnhanceItem/Enhance_{itemId}",
            "enhance" => $"Icons/EnhanceItem/Enhance_{itemId}", // "enhance"タイプも対応
            "supportitem" => $"Icons/SupportItem/Support_{itemId}",
            "support" => $"Icons/SupportItem/Support_{itemId}", // "support"タイプも対応
            "skill" => $"Icons/Skill/skill_{itemId}",
            _ => $"Icons/Item/item_{itemId}"
        };

        Log($"フォールバックパス生成: Type={itemType}, ID={itemId}, Path={fallbackPath}");
        return fallbackPath;
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

        // ドロップ率に応じてレアリティを判定
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

        return $"DropItem[{dropItemData.itemId}] {GetItemDisplayName()} - " +
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