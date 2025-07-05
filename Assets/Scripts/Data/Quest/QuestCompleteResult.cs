using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// クエスト完了結果データクラス
/// クエスト完了処理の結果を格納
/// </summary>
[System.Serializable]
public class QuestCompleteResult
{
    [Header("結果情報")]
    public bool isSuccess;
    public string message;

    [Header("クエスト情報")]
    public int questId;
    public DateTime completedTime;

    [Header("報酬・進行")]
    public List<QuestReward> rewards;
    public bool isFirstClear;
    public int totalClearCount;

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public QuestCompleteResult()
    {
        isSuccess = false;
        message = "";

        questId = 0;
        completedTime = DateTime.MinValue;

        rewards = new List<QuestReward>();
        isFirstClear = false;
        totalClearCount = 0;
    }
}