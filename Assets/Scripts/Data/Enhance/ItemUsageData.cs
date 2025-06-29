using UnityEngine;

/// <summary>
/// アイテム使用情報を格納するデータクラス
/// 強化で使用するアイテムの詳細情報を保持
/// </summary>
[System.Serializable]
public class ItemUsageData
{
    [Header("基本情報")]
    public ItemType itemType;                       // アイテムタイプ（EnhanceItem/SupportItem）
    public int itemId;                              // アイテムのマスターID
    public int usedQuantity;                        // 使用数量

    [Header("アイテム詳細")]
    public string itemName;                         // アイテム名（表示用）
    public RarityType rarity;                       // レアリティ
    public AttributeType attributeType;             // 属性タイプ

    [Header("所持状況")]
    public int currentQuantity;                     // 現在の所持数量
    public int remainingQuantity;                   // 使用後の残り数量
    public bool hasEnoughQuantity;                  // 必要数量が足りているか

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public ItemUsageData()
    {
        itemType = ItemType.EnhanceItem;
        itemId = 0;
        usedQuantity = 0;
        itemName = string.Empty;
        rarity = RarityType.Common;
        attributeType = AttributeType.None;
        currentQuantity = 0;
        remainingQuantity = 0;
        hasEnoughQuantity = false;
    }

    /// <summary>
    /// 強化アイテム用コンストラクタ
    /// </summary>
    /// <param name="enhanceItemId">強化アイテムID</param>
    /// <param name="quantity">使用数量</param>
    /// <param name="currentStock">現在の所持数量</param>
    public ItemUsageData(int enhanceItemId, int quantity, int currentStock)
    {
        itemType = ItemType.EnhanceItem;
        itemId = enhanceItemId;
        usedQuantity = quantity;
        currentQuantity = currentStock;
        remainingQuantity = currentStock - quantity;
        hasEnoughQuantity = currentStock >= quantity;

        // マスターデータから詳細情報を取得
        var masterData = MasterDataManager.Instance?.GetEnhanceItemData(enhanceItemId);
        if (masterData != null)
        {
            itemName = masterData.enhanceItemName;
            rarity = masterData.rarity;
            attributeType = masterData.attributeType;
        }
        else
        {
            itemName = $"UnknownEnhanceItem_{enhanceItemId}";
            rarity = RarityType.Common;
            attributeType = AttributeType.None;
        }
    }

    /// <summary>
    /// 補助材料用コンストラクタ
    /// </summary>
    /// <param name="supportItemId">補助材料ID</param>
    /// <param name="quantity">使用数量</param>
    /// <param name="currentStock">現在の所持数量</param>
    public ItemUsageData(int supportItemId, int quantity, int currentStock, bool isSupportItem)
    {
        if (!isSupportItem)
        {
            Debug.LogError("補助材料用コンストラクタでisSupportItemがfalseです");
            return;
        }

        itemType = ItemType.SupportItem;
        itemId = supportItemId;
        usedQuantity = quantity;
        currentQuantity = currentStock;
        remainingQuantity = currentStock - quantity;
        hasEnoughQuantity = currentStock >= quantity;

        // マスターデータから詳細情報を取得
        var masterData = MasterDataManager.Instance?.GetSupportItemData(supportItemId);
        if (masterData != null)
        {
            itemName = masterData.supportItemName;
            rarity = masterData.rarity;
            attributeType = masterData.attributeType;
        }
        else
        {
            itemName = $"UnknownSupportItem_{supportItemId}";
            rarity = RarityType.Common;
            attributeType = AttributeType.None;
        }
    }

    /// <summary>
    /// 静的ファクトリーメソッド - 強化アイテム用
    /// </summary>
    /// <param name="enhanceItemId">強化アイテムID</param>
    /// <param name="quantity">使用数量</param>
    /// <param name="currentStock">現在の所持数量</param>
    /// <returns>強化アイテム用のItemUsageData</returns>
    public static ItemUsageData CreateForEnhanceItem(int enhanceItemId, int quantity = 1, int currentStock = 0)
    {
        return new ItemUsageData(enhanceItemId, quantity, currentStock);
    }

    /// <summary>
    /// 静的ファクトリーメソッド - 補助材料用
    /// </summary>
    /// <param name="supportItemId">補助材料ID</param>
    /// <param name="quantity">使用数量</param>
    /// <param name="currentStock">現在の所持数量</param>
    /// <returns>補助材料用のItemUsageData</returns>
    public static ItemUsageData CreateForSupportItem(int supportItemId, int quantity = 1, int currentStock = 0)
    {
        return new ItemUsageData(supportItemId, quantity, currentStock, true);
    }

    /// <summary>
    /// アイテムが使用可能かどうかを判定
    /// </summary>
    /// <returns>使用可能な場合true</returns>
    public bool CanUse()
    {
        return hasEnoughQuantity && usedQuantity > 0 && itemId > 0;
    }

    /// <summary>
    /// 使用後の所持数量を更新
    /// </summary>
    /// <param name="newCurrentQuantity">新しい現在所持数量</param>
    public void UpdateCurrentQuantity(int newCurrentQuantity)
    {
        currentQuantity = newCurrentQuantity;
        remainingQuantity = currentQuantity - usedQuantity;
        hasEnoughQuantity = currentQuantity >= usedQuantity;
    }

    /// <summary>
    /// アイテムの表示名を取得（レアリティ付き）
    /// </summary>
    /// <returns>表示用のアイテム名</returns>
    public string GetDisplayName()
    {
        var rarityPrefix = rarity switch
        {
            RarityType.Common => "[C]",
            RarityType.Rare => "[R]",
            RarityType.Epic => "[E]",
            RarityType.Legendary => "[L]",
            _ => ""
        };

        var attributePrefix = attributeType switch
        {
            AttributeType.Fire => "[火]",
            AttributeType.Water => "[水]",
            AttributeType.Wind => "[風]",
            AttributeType.Earth => "[土]",
            _ => ""
        };

        return $"{rarityPrefix}{attributePrefix}{itemName}";
    }

    /// <summary>
    /// 数量情報を取得
    /// </summary>
    /// <returns>数量情報の文字列</returns>
    public string GetQuantityInfo()
    {
        return $"{usedQuantity}個使用 (所持: {currentQuantity}個)";
    }

    /// <summary>
    /// アイテムが足りない場合のメッセージを取得
    /// </summary>
    /// <returns>不足メッセージ</returns>
    public string GetShortageMessage()
    {
        if (hasEnoughQuantity)
        {
            return string.Empty;
        }

        int shortage = usedQuantity - currentQuantity;
        return $"{itemName}が{shortage}個不足しています";
    }

    /// <summary>
    /// デバッグ用の文字列表現
    /// </summary>
    /// <returns>アイテム使用情報の詳細</returns>
    public override string ToString()
    {
        return $"ItemUsage: {itemType} | " +
               $"ID: {itemId} | " +
               $"Name: {itemName} | " +
               $"Use: {usedQuantity} | " +
               $"Stock: {currentQuantity} | " +
               $"CanUse: {CanUse()}";
    }

    /// <summary>
    /// オブジェクトの等価性をチェック
    /// </summary>
    /// <param name="obj">比較対象</param>
    /// <returns>等価な場合true</returns>
    public override bool Equals(object obj)
    {
        if (obj is ItemUsageData other)
        {
            return itemType == other.itemType &&
                   itemId == other.itemId &&
                   usedQuantity == other.usedQuantity;
        }
        return false;
    }

    /// <summary>
    /// ハッシュコードを取得
    /// </summary>
    /// <returns>ハッシュコード</returns>
    public override int GetHashCode()
    {
        return System.HashCode.Combine(itemType, itemId, usedQuantity);
    }
}