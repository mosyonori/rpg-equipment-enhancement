using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    [Header("�N�G�X�g�f�[�^�x�[�X")]
    public QuestData[] questDatabase;

    [Header("�v���C���[�i�s��")]
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
    /// ���p�\�ȃN�G�X�g���擾
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

        // �N�G�X�gID�Ń\�[�g
        return availableQuests.OrderBy(q => q.questId).ToList();
    }

    /// <summary>
    /// ����̃N�G�X�g�f�[�^���擾
    /// </summary>
    public QuestData GetQuestData(int questId)
    {
        return questDatabase.FirstOrDefault(q => q.questId == questId);
    }

    /// <summary>
    /// �N�G�X�g�̃N���A�񐔂��擾
    /// </summary>
    public int GetQuestClearCount(int questId)
    {
        return questClearCounts.ContainsKey(questId) ? questClearCounts[questId] : 0;
    }

    /// <summary>
    /// �N�G�X�g�N���A����
    /// </summary>
    public void CompleteQuest(int questId)
    {
        var quest = GetQuestData(questId);
        if (quest == null) return;

        // ����N���A
        if (!clearedQuestIds.Contains(questId))
        {
            clearedQuestIds.Add(questId);

            // ����N���A��V
            if (quest.hasFirstClearReward)
            {
                GrantFirstClearReward(quest);
            }
        }

        // �N���A�񐔍X�V
        if (questClearCounts.ContainsKey(questId))
        {
            questClearCounts[questId]++;
        }
        else
        {
            questClearCounts[questId] = 1;
        }

        // �ʏ��V
        GrantQuestRewards(quest);
    }

    /// <summary>
    /// �N�G�X�g��V��t�^
    /// </summary>
    private void GrantQuestRewards(QuestData quest)
    {
        // �o���l�ƃS�[���h
        if (DataManager.Instance != null)
        {
            DataManager.Instance.currentUserData.experience += quest.rewardExp;
            DataManager.Instance.currentUserData.currency += quest.rewardGold;
        }

        // �h���b�v�A�C�e��
        var drops = quest.RollDropItems();
        foreach (var (type, id, quantity) in drops)
        {
            GrantItem(type, id, quantity);
        }
    }

    /// <summary>
    /// ����N���A��V��t�^
    /// </summary>
    private void GrantFirstClearReward(QuestData quest)
    {
        if (quest.hasFirstClearReward)
        {
            GrantItem(quest.firstClearItemType, quest.firstClearItemId, quest.firstClearItemQuantity);
        }
    }

    /// <summary>
    /// �A�C�e����t�^
    /// </summary>
    private void GrantItem(ItemType type, int itemId, int quantity)
    {
        if (DataManager.Instance == null) return;

        // DataManager��AddItem���\�b�h���g�p
        // ItemType�ɉ����ēK�؂ȃ^�C�v������ɕϊ�
        string itemTypeStr = type switch
        {
            ItemType.Equipment => "equipment",
            ItemType.Enhancement => "enhancement",
            ItemType.Support => "support",
            _ => "enhancement"
        };

        if (type == ItemType.Equipment)
        {
            // �����̏ꍇ�͌ʂɒǉ�
            for (int i = 0; i < quantity; i++)
            {
                DataManager.Instance.AddEquipment(itemId);
            }
        }
        else
        {
            DataManager.Instance.AddItem(itemId, quantity);
        }

        Debug.Log($"�A�C�e���t�^: {type} ID:{itemId} x{quantity}");
    }

    /// <summary>
    /// �X�^�~�i����
    /// </summary>
    public bool ConsumeStamina(int questId)
    {
        var quest = GetQuestData(questId);
        if (quest == null) return false;

        // �X�^�~�i������i�����̓Q�[���̎d�l�ɉ����āj
        // ��: DataManager.Instance.ConsumeStamina(quest.requiredStamina);

        return true;
    }
}

// �N�G�X�g�I��UI�p�̃w���p�[�N���X
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

        // �����ԃ`�F�b�N
        isUnlocked = quest.IsAvailable(playerLevel, clearedQuests);
        canChallenge = isUnlocked && quest.CanChallenge(clearCount);

        // ���b�N���R
        if (!isUnlocked)
        {
            if (playerLevel < quest.requiredLevel)
            {
                lockReason = $"���x��{quest.requiredLevel}�ŉ��";
            }
            else if (quest.prerequisiteQuestIds != null && quest.prerequisiteQuestIds.Length > 0)
            {
                lockReason = "�O��N�G�X�g���N���A����K�v������܂�";
            }
        }
        else if (!canChallenge)
        {
            lockReason = "�N���A�񐔏���ɒB���Ă��܂�";
        }
    }
}