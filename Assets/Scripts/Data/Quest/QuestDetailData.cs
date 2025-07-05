using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// クエスト詳細データクラス
/// クエスト詳細画面での情報表示に使用
/// </summary>
[System.Serializable]
public class QuestDetailData
{
    [Header("マスターデータ")]
    public QuestMasterData questMaster;

    [Header("ユーザーデータ")]
    public UserQuestData userQuestData;
    public QuestDisplayData displayData;

    [Header("関連データ")]
    public List<MonsterMasterData> spawnMonsters;
    public DropTableMasterData dropTable;

    [Header("利用可能性")]
    public bool isAvailable;
    public string availabilityReason;

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public QuestDetailData()
    {
        questMaster = null;
        userQuestData = null;
        displayData = null;

        spawnMonsters = new List<MonsterMasterData>();
        dropTable = null;

        isAvailable = false;
        availabilityReason = "";
    }
}