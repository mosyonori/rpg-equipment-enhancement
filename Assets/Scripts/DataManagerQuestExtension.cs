using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// DataManager�̃N�G�X�g�@�\���g������N���X
/// ������DataManager�ɒǉ����郁�\�b�h�Q
/// </summary>
public static class DataManagerQuestExtension
{
    /// <summary>
    /// ���݂̃v���C���[���x�����擾
    /// </summary>
    public static int GetPlayerLevel(this DataManager dataManager)
    {
        return dataManager.currentUserData?.playerLevel ?? 1;
    }

    /// <summary>
    /// ���݂̃X�^�~�i���擾
    /// </summary>
    public static int GetCurrentStamina(this DataManager dataManager)
    {
        // �������F�X�^�~�i�V�X�e�����������̏ꍇ��100��Ԃ�
        return 100;
    }

    /// <summary>
    /// �X�^�~�i������
    /// </summary>
    public static bool ConsumeStamina(this DataManager dataManager, int amount)
    {
        // �������F�X�^�~�i�V�X�e�������������܂ł͏��true��Ԃ�
        Debug.Log($"�X�^�~�i {amount} ������܂���");
        return true;
    }

    /// <summary>
    /// �N�G�X�g���N���A�ς݂��`�F�b�N
    /// </summary>
    public static bool IsQuestCleared(this DataManager dataManager, int questId)
    {
        if (dataManager.currentUserData?.questProgress == null) return false;

        var progress = dataManager.currentUserData.questProgress.Find(q => q.questId == questId);
        return progress != null && progress.isCleared;
    }

    /// <summary>
    /// �N�G�X�g�̎c��N���A�񐔂��擾
    /// </summary>
    public static int GetQuestRemainingClears(this DataManager dataManager, int questId)
    {
        // �N�G�X�g�f�[�^���A�Z�b�g���猟��
        var questData = dataManager.GetQuestDataFromAssets(questId);
        if (questData == null) return 0;

        // �������̏ꍇ
        if (questData.clearLimit == -1) return 999;

        // ���݂̃N���A�񐔂��擾
        var progress = dataManager.currentUserData?.questProgress?.Find(q => q.questId == questId);
        int currentClears = progress?.clearCount ?? 0;

        return Mathf.Max(0, questData.clearLimit - currentClears);
    }

    /// <summary>
    /// �N�G�X�g�f�[�^���擾�i�A�Z�b�g����j
    /// </summary>
    private static QuestData GetQuestDataFromAssets(int questId)
    {
        return DataManager.Instance.GetQuestDataFromAssets(questId);
    }

    /// <summary>
    /// �S�N�G�X�g�f�[�^���擾�iGamedata/Quests�t�H���_����j
    /// </summary>
    public static List<QuestData> GetAllQuestDataFromAssets(this DataManager dataManager)
    {
        var questList = new List<QuestData>();

        // Resources/Gamedata/Quests�t�H���_���炷�ׂĂ�QuestData��ǂݍ���
        QuestData[] questAssets = Resources.LoadAll<QuestData>("Gamedata/Quests");

        if (questAssets == null || questAssets.Length == 0)
        {
            Debug.LogError("Gamedata/Quests�t�H���_��QuestData��������܂���");
            return questList;
        }

        questList.AddRange(questAssets);

        // questId�Ń\�[�g
        questList.Sort((a, b) => a.questId.CompareTo(b.questId));

        Debug.Log($"�N�G�X�g�f�[�^��{questList.Count}���ǂݍ��݂܂���");
        return questList;
    }

    /// <summary>
    /// �w��ID�̃N�G�X�g�f�[�^���擾�iGamedata/Quests�t�H���_����j
    /// </summary>
    public static QuestData GetQuestDataFromAssets(this DataManager dataManager, int questId)
    {
        QuestData[] questAssets = Resources.LoadAll<QuestData>("Gamedata/Quests");

        if (questAssets == null || questAssets.Length == 0)
        {
            Debug.LogError("Gamedata/Quests�t�H���_��QuestData��������܂���");
            return null;
        }

        foreach (var questData in questAssets)
        {
            if (questData.questId == questId)
            {
                return questData;
            }
        }

        Debug.LogWarning($"�N�G�X�gID {questId} ��������܂���");
        return null;
    }

    #region CSV��̓w���p�[���\�b�h

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

            // prerequisiteQuests �̉��
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

            // ����N���A��V�̉�́i������QuestData�̍\���ɍ��킹��j
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
            Debug.LogError($"CSV�̉�͂Ɏ��s���܂���: {e.Message}, Line: {csvLine}");
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

    #region �N�G�X�g���s�@�\

    /// <summary>
    /// �N�G�X�g���J�n
    /// </summary>
    public static bool StartQuest(this DataManager dataManager, int questId)
    {
        var questData = GetQuestDataFromAssets(questId);
        if (questData == null)
        {
            Debug.LogError($"�N�G�X�gID {questId} ��������܂���");
            return false;
        }

        // �O������`�F�b�N
        if (!CanStartQuest(dataManager, questData))
        {
            return false;
        }

        // �X�^�~�i����
        if (!dataManager.ConsumeStamina(questData.requiredStamina))
        {
            Debug.LogWarning("�X�^�~�i���s�����Ă��܂�");
            return false;
        }

        Debug.Log($"�N�G�X�g�J�n: {questData.questName}");

        // ���ۂ̃N�G�X�g���s��QuestManager�ōs��
        // �����ł͊J�n�̏����̂�

        return true;
    }

    /// <summary>
    /// �N�G�X�g�J�n�\���`�F�b�N
    /// </summary>
    public static bool CanStartQuest(this DataManager dataManager, QuestData questData)
    {
        // ���x���`�F�b�N
        if (dataManager.GetPlayerLevel() < questData.requiredLevel)
        {
            Debug.LogWarning($"���x�����s�����Ă��܂��B�K�v���x��: {questData.requiredLevel}");
            return false;
        }

        // �O��N�G�X�g�`�F�b�N�i������QuestData�̍\���ɍ��킹��j
        if (questData.prerequisiteQuestIds != null)
        {
            foreach (int prereqId in questData.prerequisiteQuestIds)
            {
                if (!dataManager.IsQuestCleared(prereqId))
                {
                    Debug.LogWarning($"�O��N�G�X�g {prereqId} ���N���A����Ă��܂���");
                    return false;
                }
            }
        }

        // �N���A�����`�F�b�N
        if (questData.clearLimit > 0)
        {
            int remaining = dataManager.GetQuestRemainingClears(questData.questId);
            if (remaining <= 0)
            {
                Debug.LogWarning("�N���A�񐔐����ɒB���Ă��܂�");
                return false;
            }
        }

        // �X�^�~�i�`�F�b�N
        if (dataManager.GetCurrentStamina() < questData.requiredStamina)
        {
            Debug.LogWarning("�X�^�~�i���s�����Ă��܂�");
            return false;
        }

        return true;
    }

    /// <summary>
    /// �N�G�X�g�N���A����
    /// </summary>
    public static void CompleteQuest(this DataManager dataManager, int questId, bool isFirstClear = false)
    {
        if (dataManager.currentUserData == null) return;

        // �N�G�X�g�i�s�f�[�^���X�V
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

        Debug.Log($"�N�G�X�g {questId} ���N���A���܂����i{progress.clearCount}��ځj");

        // �Z�[�u�f�[�^�X�V
        dataManager.SaveUserData();
    }

    #endregion
}