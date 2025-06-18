using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    [Header("クエストデータベース")]
    public QuestData[] questDatabase;

    [Header("プレイヤー進行状況")]
    public List<int> clearedQuestIds = new List<int>();
    public Dictionary<int, int> questClearCounts = new Dictionary<int, int>();

    private static QuestManager instance;
    public static QuestManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<QuestManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 利用可能なクエストを取得
    /// </summary>
    public List<QuestData> GetAvailableQuests(int playerLevel)
    {
        var availableQuests = new List<QuestData>();

        foreach (var quest in questDatabase)
        {
            if (quest.IsAvailable(playerLevel, clearedQuestIds))
            {
                int clearCount = GetQuestClearCount(quest.questId);
                if (quest.CanChallenge(clearCount))
                {
                    availableQuests.Add(quest);
                }
            }
        }

        // クエストIDでソート
        return availableQuests.OrderBy(q => q.questId).ToList();
    }

    /// <summary>
    /// 特定のクエストデータを取得
    /// </summary>
    public QuestData GetQuestData(int questId)
    {
        return questDatabase.FirstOrDefault(q => q.questId == questId);
    }

    /// <summary>
    /// クエストのクリア回数を取得
    /// </summary>
    public int GetQuestClearCount(int questId)
    {
        return questClearCounts.ContainsKey(questId) ? questClearCounts[questId] : 0;
    }

    /// <summary>
    /// クエストクリア処理
    /// </summary>
    public void CompleteQuest(int questId)
    {
        var quest = GetQuestData(questId);
        if (quest == null) return;

        // 初回クリア
        if (!clearedQuestIds.Contains(questId))
        {
            clearedQuestIds.Add(questId);

            // 初回クリア報酬
            if (quest.hasFirstClearReward)
            {
                GrantFirstClearReward(quest);
            }
        }

        // クリア回数更新
        if (questClearCounts.ContainsKey(questId))
        {
            questClearCounts[questId]++;
        }
        else
        {
            questClearCounts[questId] = 1;
        }

        // 通常報酬
        GrantQuestRewards(quest);
    }

    /// <summary>
    /// クエスト報酬を付与
    /// </summary>
    private void GrantQuestRewards(QuestData quest)
    {
        // 経験値とゴールド
        if (DataManager.Instance != null)
        {
            DataManager.Instance.currentUserData.experience += quest.rewardExp;
            DataManager.Instance.currentUserData.currency += quest.rewardGold;
        }

        // ドロップアイテム
        var drops = quest.RollDropItems();
        foreach (var (type, id, quantity) in drops)
        {
            GrantItem(type, id, quantity);
        }
    }

    /// <summary>
    /// 初回クリア報酬を付与
    /// </summary>
    private void GrantFirstClearReward(QuestData quest)
    {
        if (quest.hasFirstClearReward)
        {
            GrantItem(quest.firstClearItemType, quest.firstClearItemId, quest.firstClearItemQuantity);
        }
    }

    /// <summary>
    /// アイテムを付与
    /// </summary>
    private void GrantItem(ItemType type, int itemId, int quantity)
    {
        if (DataManager.Instance == null) return;

        // DataManagerのAddItemメソッドを使用
        // ItemTypeに応じて適切なタイプ文字列に変換
        string itemTypeStr = type switch
        {
            ItemType.Equipment => "equipment",
            ItemType.Enhancement => "enhancement",
            ItemType.Support => "support",
            _ => "enhancement"
        };

        if (type == ItemType.Equipment)
        {
            // 装備の場合は個別に追加
            for (int i = 0; i < quantity; i++)
            {
                DataManager.Instance.AddEquipment(itemId);
            }
        }
        else
        {
            DataManager.Instance.AddItem(itemId, quantity);
        }

        Debug.Log($"アイテム付与: {type} ID:{itemId} x{quantity}");
    }

    /// <summary>
    /// スタミナ消費
    /// </summary>
    public bool ConsumeStamina(int questId)
    {
        var quest = GetQuestData(questId);
        if (quest == null) return false;

        // スタミナ消費処理（実装はゲームの仕様に応じて）
        // 例: DataManager.Instance.ConsumeStamina(quest.requiredStamina);

        return true;
    }
}

// クエスト選択UI用のヘルパークラス
[System.Serializable]
public class QuestUIData
{
    public QuestData questData;
    public bool isUnlocked;
    public bool canChallenge;
    public int clearCount;
    public string lockReason;

    public QuestUIData(QuestData quest, int playerLevel, List<int> clearedQuests, int clearCount)
    {
        questData = quest;
        this.clearCount = clearCount;

        // 解放状態チェック
        isUnlocked = quest.IsAvailable(playerLevel, clearedQuests);
        canChallenge = isUnlocked && quest.CanChallenge(clearCount);

        // ロック理由
        if (!isUnlocked)
        {
            if (playerLevel < quest.requiredLevel)
            {
                lockReason = $"レベル{quest.requiredLevel}で解放";
            }
            else if (quest.prerequisiteQuestIds != null && quest.prerequisiteQuestIds.Length > 0)
            {
                lockReason = "前提クエストをクリアする必要があります";
            }
        }
        else if (!canChallenge)
        {
            lockReason = "クリア回数上限に達しています";
        }
    }
}