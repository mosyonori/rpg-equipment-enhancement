using System;
using UnityEngine;

/// <summary>
/// ユーザーが所持するアイテムデータ（強化アイテム・補助アイテム共通）
/// </summary>
[System.Serializable]
public class UserItemData
{
    [Header("基本情報")]
    public string userItemId;       // ユーザー固有のアイテムID
    public ItemType itemType;       // アイテムタイプ（強化アイテム/補助アイテム）
    public int itemMasterId;        // マスターデータのID

    [Header("所持状況")]
    public int quantity;            // 所持数量
    public int maxStackQuantity;    // 最大スタック数（マスターデータから取得）

    [Header("管理情報")]
    public DateTime firstAcquiredDate;  // 初回取得日時
    public DateTime lastAcquiredDate;   // 最終取得日時
    public bool isLocked;               // ロック状態
    public bool isNew;                  // 新規取得フラグ

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public UserItemData()
    {
        userItemId = Guid.NewGuid().ToString();
        quantity = 0;
        maxStackQuantity = 1;
        firstAcquiredDate = DateTime.Now;
        lastAcquiredDate = DateTime.Now;
        isLocked = false;
        isNew = true;
    }

    /// <summary>
    /// 強化アイテムから作成
    /// </summary>
    public UserItemData(EnhanceItemMasterData masterData, int initialQuantity = 1) : this()
    {
        itemType = ItemType.EnhanceItem;
        itemMasterId = masterData.enhanceItemId;
        quantity = initialQuantity;
        maxStackQuantity = masterData.maxStackValue;
    }

    /// <summary>
    /// 補助アイテムから作成
    /// </summary>
    public UserItemData(SupportItemMasterData masterData, int initialQuantity = 1) : this()
    {
        itemType = ItemType.SupportItem;
        itemMasterId = masterData.supportItemId;
        quantity = initialQuantity;
        maxStackQuantity = masterData.maxStackValue;
    }

    /// <summary>
    /// アイテムを追加
    /// </summary>
    public bool AddItem(int addQuantity)
    {
        if (addQuantity <= 0) return false;

        // スタック上限チェック
        if (quantity + addQuantity > maxStackQuantity)
        {
            // 上限まで追加
            int actualAddQuantity = maxStackQuantity - quantity;
            quantity = maxStackQuantity;
            lastAcquiredDate = DateTime.Now;
            isNew = true;

            return actualAddQuantity > 0;
        }
        else
        {
            quantity += addQuantity;
            lastAcquiredDate = DateTime.Now;
            isNew = true;
            return true;
        }
    }

    /// <summary>
    /// アイテムを使用（減算）
    /// </summary>
    public bool UseItem(int useQuantity)
    {
        if (useQuantity <= 0) return false;
        if (quantity < useQuantity) return false;

        quantity -= useQuantity;
        isNew = false;
        return true;
    }

    /// <summary>
    /// 指定数量使用可能かチェック
    /// </summary>
    public bool CanUse(int useQuantity)
    {
        return quantity >= useQuantity && useQuantity > 0;
    }

    /// <summary>
    /// スタックが満杯かチェック
    /// </summary>
    public bool IsStackFull()
    {
        return quantity >= maxStackQuantity;
    }

    /// <summary>
    /// スタックの空き容量を取得
    /// </summary>
    public int GetStackSpace()
    {
        return maxStackQuantity - quantity;
    }

    /// <summary>
    /// アイテムが空かチェック
    /// </summary>
    public bool IsEmpty()
    {
        return quantity <= 0;
    }

    /// <summary>
    /// 新規フラグをリセット
    /// </summary>
    public void ClearNewFlag()
    {
        isNew = false;
    }

    /// <summary>
    /// デバッグ用文字列
    /// </summary>
    public override string ToString()
    {
        return $"UserItem[ID:{userItemId}, Type:{itemType}, MasterID:{itemMasterId}, Quantity:{quantity}/{maxStackQuantity}, New:{isNew}]";
    }
}

/// <summary>
/// アイテムタイプ
/// </summary>
public enum ItemType
{
    EnhanceItem,    // 強化アイテム
    SupportItem,    // 補助アイテム
    Skill           // スキル（新規追加）
}

/// <summary>
/// アイテム所持状況の集計データ
/// </summary>
[System.Serializable]
public class ItemInventorySummary
{
    public int totalEnhanceItems;       // 強化アイテム総種類数
    public int totalSupportItems;       // 補助アイテム総種類数
    public int totalEnhanceQuantity;    // 強化アイテム総個数
    public int totalSupportQuantity;    // 補助アイテム総個数
    public int newItemCount;            // 新規取得アイテム数

    /// <summary>
    /// ユーザーアイテムリストから集計データを作成
    /// </summary>
    public static ItemInventorySummary CreateFromUserItems(System.Collections.Generic.List<UserItemData> userItems)
    {
        ItemInventorySummary summary = new ItemInventorySummary();

        foreach (UserItemData item in userItems)
        {
            if (item.IsEmpty()) continue;

            switch (item.itemType)
            {
                case ItemType.EnhanceItem:
                    summary.totalEnhanceItems++;
                    summary.totalEnhanceQuantity += item.quantity;
                    break;
                case ItemType.SupportItem:
                    summary.totalSupportItems++;
                    summary.totalSupportQuantity += item.quantity;
                    break;
            }

            if (item.isNew)
                summary.newItemCount++;
        }

        return summary;
    }
}