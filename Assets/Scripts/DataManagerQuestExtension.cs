using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// DataManagerのクエスト機能を拡張するクラス
/// 既存のDataManagerに追加するメソッド群
/// </summary>
public static class DataManagerQuestExtension
{
    /// <summary>
    /// 現在のプレイヤーレベルを取得
    /// </summary>
    public static int GetPlayerLevel(this DataManager dataManager)
    {
        return dataManager.currentUserData?.playerLevel ?? 1;
    }

    /// <summary>
    /// 現在のスタミナを取得
    /// </summary>
    public static int GetCurrentStamina(this DataManager dataManager)
    {
        // 仮実装：スタミナシステムが未実装の場合は100を返す
        return 100;
    }

    /// <summary>
    /// スタミナを消費
    /// </summary>
    public static bool ConsumeStamina(this DataManager dataManager, int amount)
    {
        // 仮実装：スタミナシステムが実装されるまでは常にtrueを返す
        Debug.Log($"スタミナ {amount} を消費しました");
        return true;
    }

    /// <summary>
    /// クエストがクリア済みかチェック
    /// </summary>
    public static bool IsQuestCleared(this DataManager dataManager, int questId)
    {
        if (dataManager.currentUserData?.questProgress == null) return false;

        var progress = dataManager.currentUserData.questProgress.Find(q => q.questId == questId);
        return progress != null && progress.isCleared;
    }

    /// <summary>
    /// クエストの残りクリア回数を取得
    /// </summary>
    public static int GetQuestRemainingClears(this DataManager dataManager, int questId)
    {
        // クエストデータをアセットから検索
        var questData = dataManager.GetQuestDataFromAssets(questId);
        if (questData == null) return 0;

        // 無制限の場合
        if (questData.clearLimit == -1) return 999;

        // 現在のクリア回数を取得
        var progress = dataManager.currentUserData?.questProgress?.Find(q => q.questId == questId);
        int currentClears = progress?.clearCount ?? 0;

        return Mathf.Max(0, questData.clearLimit - currentClears);
    }

    /// <summary>
    /// クエストデータを取得（アセットから）
    /// </summary>
    private static QuestData GetQuestDataFromAssets(int questId)
    {
        return DataManager.Instance.GetQuestDataFromAssets(questId);
    }

    /// <summary>
    /// 全クエストデータを取得（Gamedata/Questsフォルダから）
    /// </summary>
    public static List<QuestData> GetAllQuestDataFromAssets(this DataManager dataManager)
    {
        var questList = new List<QuestData>();

        // Resources/Gamedata/QuestsフォルダからすべてのQuestDataを読み込み
        QuestData[] questAssets = Resources.LoadAll<QuestData>("Gamedata/Quests");

        if (questAssets == null || questAssets.Length == 0)
        {
            Debug.LogError("Gamedata/QuestsフォルダにQuestDataが見つかりません");
            return questList;
        }

        questList.AddRange(questAssets);

        // questIdでソート
        questList.Sort((a, b) => a.questId.CompareTo(b.questId));

        Debug.Log($"クエストデータを{questList.Count}件読み込みました");
        return questList;
    }

    /// <summary>
    /// 指定IDのクエストデータを取得（Gamedata/Questsフォルダから）
    /// </summary>
    public static QuestData GetQuestDataFromAssets(this DataManager dataManager, int questId)
    {
        QuestData[] questAssets = Resources.LoadAll<QuestData>("Gamedata/Quests");

        if (questAssets == null || questAssets.Length == 0)
        {
            Debug.LogError("Gamedata/QuestsフォルダにQuestDataが見つかりません");
            return null;
        }

        foreach (var questData in questAssets)
        {
            if (questData.questId == questId)
            {
                return questData;
            }
        }

        Debug.LogWarning($"クエストID {questId} が見つかりません");
        return null;
    }

    #region CSV解析ヘルパーメソッド

    private static string[] ParseCSVLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current.Trim());
        return result.ToArray();
    }

    private static QuestData ParseCSVLineToQuestData(string csvLine)
    {
        try
        {
            string[] values = ParseCSVLine(csvLine);
            if (values.Length < 20) return null;

            var questData = ScriptableObject.CreateInstance<QuestData>();

            questData.questId = int.Parse(values[0]);
            questData.questName = values[1];
            questData.questDescription = values[2];
            questData.questType = ParseQuestType(values[3]);
            questData.requiredLevel = int.Parse(values[4]);

            // prerequisiteQuests の解析
            if (!string.IsNullOrEmpty(values[5]))
            {
                var prerequisites = values[5].Split(',');
                questData.prerequisiteQuestIds = new int[prerequisites.Length];
                for (int j = 0; j < prerequisites.Length; j++)
                {
                    if (int.TryParse(prerequisites[j].Trim(), out int prereqId))
                    {
                        questData.prerequisiteQuestIds[j] = prereqId;
                    }
                }
            }
            else
            {
                questData.prerequisiteQuestIds = new int[0];
            }

            questData.clearLimit = int.Parse(values[6]);
            questData.requiredStamina = int.Parse(values[7]);
            questData.recommendedPower = int.Parse(values[8]);
            questData.monsterSpawnCSV = values[9];
            questData.monsterCount = int.Parse(values[10]);
            questData.turnLimit = int.Parse(values[11]);
            questData.rewardExp = int.Parse(values[12]);
            questData.rewardGold = int.Parse(values[13]);
            questData.itemDropCSV = values[14];

            // 初回クリア報酬の解析（既存のQuestDataの構造に合わせる）
            if (!string.IsNullOrEmpty(values[15]))
            {
                questData.hasFirstClearReward = true;
                questData.firstClearItemType = ParseItemType(values[15]);
                if (values.Length > 16 && !string.IsNullOrEmpty(values[16]))
                    questData.firstClearItemId = int.Parse(values[16]);
                if (values.Length > 17 && !string.IsNullOrEmpty(values[17]))
                    questData.firstClearItemQuantity = int.Parse(values[17]);
            }
            else
            {
                questData.hasFirstClearReward = false;
            }

            questData.backgroundId = int.Parse(values[18]);
            questData.bgmId = int.Parse(values[19]);

            return questData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CSVの解析に失敗しました: {e.Message}, Line: {csvLine}");
            return null;
        }
    }

    private static QuestType ParseQuestType(string typeString)
    {
        switch (typeString)
        {
            case "Normal": return QuestType.Normal;
            case "Daily": return QuestType.Daily;
            case "Event": return QuestType.Event;
            case "Tutorial": return QuestType.Tutorial;
            case "Boss": return QuestType.Boss;
            default: return QuestType.Normal;
        }
    }

    private static ItemType ParseItemType(string typeString)
    {
        switch (typeString.ToLower())
        {
            case "enhancement": return ItemType.Enhancement;
            case "equipment": return ItemType.Equipment;
            case "support": return ItemType.Support;
            default: return ItemType.Enhancement;
        }
    }

    private static int[] ParsePrerequisiteQuests(string prerequisiteString)
    {
        if (string.IsNullOrEmpty(prerequisiteString))
            return new int[0];

        string[] parts = prerequisiteString.Split(',');
        var prerequisites = new List<int>();

        foreach (string part in parts)
        {
            if (int.TryParse(part.Trim(), out int questId))
            {
                prerequisites.Add(questId);
            }
        }

        return prerequisites.ToArray();
    }

    #endregion

    #region クエスト実行機能

    /// <summary>
    /// クエストを開始
    /// </summary>
    public static bool StartQuest(this DataManager dataManager, int questId)
    {
        var questData = GetQuestDataFromAssets(questId);
        if (questData == null)
        {
            Debug.LogError($"クエストID {questId} が見つかりません");
            return false;
        }

        // 前提条件チェック
        if (!CanStartQuest(dataManager, questData))
        {
            return false;
        }

        // スタミナ消費
        if (!dataManager.ConsumeStamina(questData.requiredStamina))
        {
            Debug.LogWarning("スタミナが不足しています");
            return false;
        }

        Debug.Log($"クエスト開始: {questData.questName}");

        // 実際のクエスト実行はQuestManagerで行う
        // ここでは開始の準備のみ

        return true;
    }

    /// <summary>
    /// クエスト開始可能かチェック
    /// </summary>
    public static bool CanStartQuest(this DataManager dataManager, QuestData questData)
    {
        // レベルチェック
        if (dataManager.GetPlayerLevel() < questData.requiredLevel)
        {
            Debug.LogWarning($"レベルが不足しています。必要レベル: {questData.requiredLevel}");
            return false;
        }

        // 前提クエストチェック（既存のQuestDataの構造に合わせる）
        if (questData.prerequisiteQuestIds != null)
        {
            foreach (int prereqId in questData.prerequisiteQuestIds)
            {
                if (!dataManager.IsQuestCleared(prereqId))
                {
                    Debug.LogWarning($"前提クエスト {prereqId} がクリアされていません");
                    return false;
                }
            }
        }

        // クリア制限チェック
        if (questData.clearLimit > 0)
        {
            int remaining = dataManager.GetQuestRemainingClears(questData.questId);
            if (remaining <= 0)
            {
                Debug.LogWarning("クリア回数制限に達しています");
                return false;
            }
        }

        // スタミナチェック
        if (dataManager.GetCurrentStamina() < questData.requiredStamina)
        {
            Debug.LogWarning("スタミナが不足しています");
            return false;
        }

        return true;
    }

    /// <summary>
    /// クエストクリア処理
    /// </summary>
    public static void CompleteQuest(this DataManager dataManager, int questId, bool isFirstClear = false)
    {
        if (dataManager.currentUserData == null) return;

        // クエスト進行データを更新
        var progress = dataManager.currentUserData.questProgress.Find(q => q.questId == questId);
        if (progress == null)
        {
            progress = new QuestProgress
            {
                questId = questId,
                isCleared = true,
                clearCount = 1,
                firstClearTime = System.DateTime.Now,
                lastClearTime = System.DateTime.Now
            };
            dataManager.currentUserData.questProgress.Add(progress);
        }
        else
        {
            progress.isCleared = true;
            progress.clearCount++;
            progress.lastClearTime = System.DateTime.Now;
        }

        Debug.Log($"クエスト {questId} をクリアしました（{progress.clearCount}回目）");

        // セーブデータ更新
        dataManager.SaveUserData();
    }

    #endregion
}