using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class MonsterSpawnInfo
{
    public int monsterId;
    public float spawnRate; // �o�����i%�j
}

[System.Serializable]
public class ItemDropInfo
{
    public ItemType itemType;
    public int itemId;
    public float dropRate; // �h���b�v���i%�j
}

[CreateAssetMenu(fileName = "NewQuest", menuName = "GameData/Quest")]
public class QuestData : ScriptableObject
{
    [Header("��{���")]
    public int questId;
    public string questName;
    [TextArea(3, 5)]
    public string questDescription;

    [Header("�������")]
    public int requiredLevel = 1;
    public int[] prerequisiteQuestIds; // �O��N�G�X�gID

    [Header("�N�G�X�g�^�C�v")]
    public QuestType questType = QuestType.Normal;
    public int clearLimit = -1; // -1�͖����A0�ȏ�͉񐔐���

    [Header("����\�[�X")]
    public int requiredStamina = 10;

    [Header("�퓬�ݒ�")]
    [Tooltip("�����퓬��")]
    public int recommendedPower;

    [Tooltip("�����X�^�[�o���ݒ�iCSV�`��: monsterId:rate,monsterId:rate�j")]
    public string monsterSpawnCSV = "1:100";

    [Tooltip("�o�������X�^�[��")]
    public int monsterCount = 3;

    [Tooltip("����^�[�����i0�͖������j")]
    public int turnLimit = 30;

    [Header("��V�ݒ�")]
    public int rewardExp;
    public int rewardGold;

    [Tooltip("�h���b�v�A�C�e���ݒ�iCSV�`��: type_id:rate�j")]
    public string itemDropCSV = "enhancement_1:30,equipment_1:10";

    [Header("����N���A��V")]
    public bool hasFirstClearReward;
    public ItemType firstClearItemType;
    public int firstClearItemId;
    public int firstClearItemQuantity;

    [Header("���o�ݒ�")]
    public int backgroundId;
    public int bgmId;

    // �p�[�X�ς݃f�[�^�̃L���b�V��
    private List<MonsterSpawnInfo> _cachedMonsterSpawns;
    private List<ItemDropInfo> _cachedItemDrops;

    /// <summary>
    /// �����X�^�[�o�������擾
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
    /// �A�C�e���h���b�v�����擾
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
    /// �����X�^�[�o��CSV�`�����p�[�X
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

        // �o�����̐��K���i���v100%�ɂȂ�悤�ɒ����j
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
    /// �A�C�e���h���b�vCSV�`�����p�[�X
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
    /// �A�C�e���^�C�v�������enum�ɕϊ�
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
    /// �N�G�X�g���󒍉\���`�F�b�N
    /// </summary>
    public bool IsAvailable(int playerLevel, List<int> clearedQuestIds)
    {
        // ���x���`�F�b�N
        if (playerLevel < requiredLevel)
            return false;

        // �O��N�G�X�g�`�F�b�N
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
    /// �N�G�X�g���N���A�\���`�F�b�N�i�񐔐����j
    /// </summary>
    public bool CanChallenge(int currentClearCount)
    {
        if (clearLimit < 0) return true; // ����
        return currentClearCount < clearLimit;
    }

    /// <summary>
    /// �����_���Ƀ����X�^�[��I�o
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
    /// �h���b�v�A�C�e���𔻒�
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
                // ��{�I��1�h���b�v�i�K�v�ɉ����Č��ݒ��ǉ��\�j
                drops.Add((item.itemType, item.itemId, 1));
            }
        }

        return drops;
    }

    #region Unity Editor�p

    /// <summary>
    /// Inspector��ł̃f�[�^����
    /// </summary>
    private void OnValidate()
    {
        // CSV�t�H�[�}�b�g�̊ȈՃ`�F�b�N
        if (!string.IsNullOrEmpty(monsterSpawnCSV))
        {
            _cachedMonsterSpawns = null; // �L���b�V���N���A
        }

        if (!string.IsNullOrEmpty(itemDropCSV))
        {
            _cachedItemDrops = null; // �L���b�V���N���A
        }

        // ��{�l�͈̔̓`�F�b�N
        requiredLevel = Mathf.Max(1, requiredLevel);
        monsterCount = Mathf.Max(1, monsterCount);
        turnLimit = Mathf.Max(0, turnLimit);
        requiredStamina = Mathf.Max(0, requiredStamina);
        rewardExp = Mathf.Max(0, rewardExp);
        rewardGold = Mathf.Max(0, rewardGold);
    }

    #endregion
}