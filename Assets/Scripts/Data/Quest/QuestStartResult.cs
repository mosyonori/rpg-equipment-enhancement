using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// クエスト開始結果データクラス
/// クエスト開始処理の結果を格納
/// </summary>
[System.Serializable]
public class QuestStartResult
{
    [Header("結果情報")]
    public bool isSuccess;
    public string message;

    [Header("クエスト情報")]
    public int questId;
    public DateTime startTime;

    [Header("消費・報酬")]
    public int consumedStamina;
    public List<QuestReward> expectedRewards;

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public QuestStartResult()
    {
        isSuccess = false;
        message = "";

        questId = 0;
        startTime = DateTime.MinValue;

        consumedStamina = 0;
        expectedRewards = new List<QuestReward>();
    }
}