using UnityEngine;

/// <summary>
/// クエスト報酬データクラス
/// クエスト報酬の情報を格納
/// </summary>
[System.Serializable]
public class QuestReward
{
    [Header("アイテム情報")]
    public string itemType;
    public int itemId;
    public int quantity;

    [Header("報酬種別")]
    public bool isDropReward;

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public QuestReward()
    {
        itemType = "";
        itemId = 0;
        quantity = 0;
        isDropReward = false;
    }

    /// <summary>
    /// パラメータ付きコンストラクタ
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    /// <param name="itemId">アイテムID</param>
    /// <param name="quantity">数量</param>
    /// <param name="isDropReward">ドロップ報酬かどうか</param>
    public QuestReward(string itemType, int itemId, int quantity, bool isDropReward = false)
    {
        this.itemType = itemType ?? "";
        this.itemId = itemId;
        this.quantity = quantity;
        this.isDropReward = isDropReward;
    }
}