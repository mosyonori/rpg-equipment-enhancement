using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class MonsterSpawnInfo
{
    public int monsterId;
    public float spawnRate; // 出現率（%）
}

[System.Serializable]
public class ItemDropInfo
{
    public ItemType itemType;
    public int itemId;
    public float dropRate; // ドロップ率（%）
}

[CreateAssetMenu(fileName = "NewQuest", menuName = "GameData/Quest")]
public class QuestData : ScriptableObject
{
    [Header("基本情報")]
    public int questId;
    public string questName;
    [TextArea(3, 5)]
    public string questDescription;

    [Header("解放条件")]
    public int requiredLevel = 1;
    public int[] prerequisiteQuestIds; // 前提クエストID

    [Header("クエストタイプ")]
    public QuestType questType = QuestType.Normal;
    public int clearLimit = -1; // -1は無限、0以上は回数制限

    [Header("消費リソース")]
    public int requiredStamina = 10;

    [Header("戦闘設定")]
    [Tooltip("推奨戦闘力")]
    public int recommendedPower;

    [Tooltip("モンスター出現設定（CSV形式: monsterId:rate,monsterId:rate）")]
    public string monsterSpawnCSV = "1:100";

    [Tooltip("出現モンスター数")]
    public int monsterCount = 3;

    [Tooltip("上限ターン数（0は無制限）")]
    public int turnLimit = 30;

    [Header("報酬設定")]
    public int rewardExp;
    public int rewardGold;

    [Tooltip("ドロップアイテム設定（CSV形式: type_id:rate）")]
    public string itemDropCSV = "enhancement_1:30,equipment_1:10";

    [Header("初回クリア報酬")]
    public bool hasFirstClearReward;
    public ItemType firstClearItemType;
    public int firstClearItemId;
    public int firstClearItemQuantity;

    [Header("演出設定")]
    public int backgroundId;
    public int bgmId;

    // パース済みデータのキャッシュ
    private List<MonsterSpawnInfo> _cachedMonsterSpawns;
    private List<ItemDropInfo> _cachedItemDrops;

    /// <summary>
    /// モンスター出現情報を取得
    /// </summary>
    public List<MonsterSpawnInfo> GetMonsterSpawnInfo()
    {
        if (_cachedMonsterSpawns == null)
        {
            _cachedMonsterSpawns = ParseMonsterSpawnCSV(monsterSpawnCSV);
        }
        return _cachedMonsterSpawns;
    }

    /// <summary>
    /// アイテムドロップ情報を取得
    /// </summary>
    public List<ItemDropInfo> GetItemDropInfo()
    {
        if (_cachedItemDrops == null)
        {
            _cachedItemDrops = ParseItemDropCSV(itemDropCSV);
        }
        return _cachedItemDrops;
    }

    /// <summary>
    /// モンスター出現CSV形式をパース
    /// </summary>
    private List<MonsterSpawnInfo> ParseMonsterSpawnCSV(string csv)
    {
        var result = new List<MonsterSpawnInfo>();
        if (string.IsNullOrEmpty(csv)) return result;

        var entries = csv.Split(',');
        foreach (var entry in entries)
        {
            var parts = entry.Trim().Split(':');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out int id) &&
                    float.TryParse(parts[1], out float rate))
                {
                    result.Add(new MonsterSpawnInfo
                    {
                        monsterId = id,
                        spawnRate = rate
                    });
                }
            }
        }

        // 出現率の正規化（合計100%になるように調整）
        float totalRate = result.Sum(x => x.spawnRate);
        if (totalRate > 0)
        {
            foreach (var spawn in result)
            {
                spawn.spawnRate = (spawn.spawnRate / totalRate) * 100f;
            }
        }

        return result;
    }

    /// <summary>
    /// アイテムドロップCSV形式をパース
    /// </summary>
    private List<ItemDropInfo> ParseItemDropCSV(string csv)
    {
        var result = new List<ItemDropInfo>();
        if (string.IsNullOrEmpty(csv)) return result;

        var entries = csv.Split(',');
        foreach (var entry in entries)
        {
            var parts = entry.Trim().Split(':');
            if (parts.Length == 2)
            {
                var itemParts = parts[0].Split('_');
                if (itemParts.Length == 2)
                {
                    ItemType itemType = ParseItemType(itemParts[0]);
                    if (int.TryParse(itemParts[1], out int id) &&
                        float.TryParse(parts[1], out float rate))
                    {
                        result.Add(new ItemDropInfo
                        {
                            itemType = itemType,
                            itemId = id,
                            dropRate = rate
                        });
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// アイテムタイプ文字列をenumに変換
    /// </summary>
    private ItemType ParseItemType(string typeStr)
    {
        switch (typeStr.ToLower())
        {
            case "equipment":
                return ItemType.Equipment;
            case "enhancement":
                return ItemType.Enhancement;
            case "support":
                return ItemType.Support;
            default:
                return ItemType.Enhancement;
        }
    }

    /// <summary>
    /// クエストが受注可能かチェック
    /// </summary>
    public bool IsAvailable(int playerLevel, List<int> clearedQuestIds)
    {
        // レベルチェック
        if (playerLevel < requiredLevel)
            return false;

        // 前提クエストチェック
        if (prerequisiteQuestIds != null && prerequisiteQuestIds.Length > 0)
        {
            foreach (var questId in prerequisiteQuestIds)
            {
                if (!clearedQuestIds.Contains(questId))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// クエストがクリア可能かチェック（回数制限）
    /// </summary>
    public bool CanChallenge(int currentClearCount)
    {
        if (clearLimit < 0) return true; // 無限
        return currentClearCount < clearLimit;
    }

    /// <summary>
    /// ランダムにモンスターを選出
    /// </summary>
    public List<int> GetRandomMonsters()
    {
        var spawnInfo = GetMonsterSpawnInfo();
        var result = new List<int>();

        for (int i = 0; i < monsterCount; i++)
        {
            float random = Random.Range(0f, 100f);
            float cumulative = 0f;

            foreach (var spawn in spawnInfo)
            {
                cumulative += spawn.spawnRate;
                if (random <= cumulative)
                {
                    result.Add(spawn.monsterId);
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// ドロップアイテムを判定
    /// </summary>
    public List<(ItemType type, int id, int quantity)> RollDropItems()
    {
        var drops = new List<(ItemType, int, int)>();
        var dropInfo = GetItemDropInfo();

        foreach (var item in dropInfo)
        {
            float roll = Random.Range(0f, 100f);
            if (roll <= item.dropRate)
            {
                // 基本的に1個ドロップ（必要に応じて個数設定を追加可能）
                drops.Add((item.itemType, item.itemId, 1));
            }
        }

        return drops;
    }

    #region Unity Editor用

    /// <summary>
    /// Inspector上でのデータ検証
    /// </summary>
    private void OnValidate()
    {
        // CSVフォーマットの簡易チェック
        if (!string.IsNullOrEmpty(monsterSpawnCSV))
        {
            _cachedMonsterSpawns = null; // キャッシュクリア
        }

        if (!string.IsNullOrEmpty(itemDropCSV))
        {
            _cachedItemDrops = null; // キャッシュクリア
        }

        // 基本値の範囲チェック
        requiredLevel = Mathf.Max(1, requiredLevel);
        monsterCount = Mathf.Max(1, monsterCount);
        turnLimit = Mathf.Max(0, turnLimit);
        requiredStamina = Mathf.Max(0, requiredStamina);
        rewardExp = Mathf.Max(0, rewardExp);
        rewardGold = Mathf.Max(0, rewardGold);
    }

    #endregion
}