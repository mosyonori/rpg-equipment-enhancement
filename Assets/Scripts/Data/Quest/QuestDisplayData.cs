using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// クエスト表示用データクラス
/// UI層でのクエストリスト表示に使用
/// </summary>
[System.Serializable]
public class QuestDisplayData
{
    [Header("基本情報")]
    public int questId;
    public string questName;
    public string shortDescription;
    public QuestType questType;
    public QuestStatus status;

    [Header("表示制御")]
    public bool isAvailable;
    public bool isNew;
    public int sortOrder;

    [Header("条件・要求")]
    public int needLevel;
    public int requiredStamina;
    public int recommendedPower;

    [Header("進行状況")]
    public int clearCount;
    public int maxClearCount; // -1で無制限

    [Header("報酬・UI")]
    public List<QuestReward> rewards;
    public string questIconPath;

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public QuestDisplayData()
    {
        questId = 0;
        questName = "";
        shortDescription = "";
        questType = QuestType.Story;
        status = QuestStatus.Locked;

        isAvailable = false;
        isNew = false;
        sortOrder = 0;

        needLevel = 1;
        requiredStamina = 0;
        recommendedPower = 0;

        clearCount = 0;
        maxClearCount = -1;

        rewards = new List<QuestReward>();
        questIconPath = "";
    }
}