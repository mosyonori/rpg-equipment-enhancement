// QuestEnums.cs
// Assets/Scripts/Data/ フォルダに配置してください

using UnityEngine;

/// <summary>
/// クエストタイプの定義
/// </summary>
public enum QuestType
{
    Normal,     // 通常クエスト
    Event,      // イベントクエスト
    Daily,      // デイリークエスト
    Tutorial,   // チュートリアルクエスト
    Boss        // ボスクエスト
}

/// <summary>
/// アイテムタイプの定義
/// </summary>
public enum ItemType
{
    Equipment,    // 装備
    Enhancement,  // 強化アイテム
    Support      // 補助材料
}