using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UserEquipment
{
    public int equipmentId;
    public int enhancementLevel;
    public int currentDurability;
    public bool isEquipped;
    public DateTime acquiredDate;

    // �����ɂ���ē���ꂽ�{�[�i�X�i���[�U�[�ŗL�f�[�^�j
    public int bonusAttackPower = 0;
    public int bonusDefensePower = 0;
    public int bonusElementalAttack = 0; // �������F�ėp�����U���i�݊����ێ��j
    public int bonusHP = 0;
    public float bonusCriticalRate = 0f;

    // ���ǉ��F4��ނ̑����U��
    public int bonusFireAttack = 0;      // �Α����U��
    public int bonusWaterAttack = 0;     // �������U��
    public int bonusWindAttack = 0;      // �������U��
    public int bonusEarthAttack = 0;     // �y�����U��

    // ���݂̃X�e�[�^�X�i�v�Z�p�A�ۑ�����Ȃ��j
    [System.NonSerialized]
    public int currentAttackPower;
    [System.NonSerialized]
    public int currentDefensePower;
    [System.NonSerialized]
    public int currentElementalAttack;
    [System.NonSerialized]
    public int currentHP;
    [System.NonSerialized]
    public float currentCriticalRate;

    // ������ꂽ�X�L��
    public List<int> unlockedSkills = new List<int>();
    public int equippedSkillId = -1; // �������̃X�L��ID

    // ���ǉ�: �����X�e�[�^�X�v�Z���\�b�h�i�}�X�^�[�f�[�^ + �{�[�i�X�j
    public float GetTotalAttack()
    {
        var masterData = DataManager.Instance.GetEquipmentData(equipmentId);
        float baseAttack = masterData != null ? masterData.stats.baseAttackPower : 0f;
        return baseAttack + bonusAttackPower;
    }

    public float GetTotalDefense()
    {
        var masterData = DataManager.Instance.GetEquipmentData(equipmentId);
        float baseDefense = masterData != null ? masterData.stats.baseDefensePower : 0f;
        return baseDefense + bonusDefensePower;
    }

    public float GetTotalCriticalRate()
    {
        var masterData = DataManager.Instance.GetEquipmentData(equipmentId);
        float baseCritical = masterData != null ? masterData.stats.baseCriticalRate : 0f;
        return baseCritical + bonusCriticalRate;
    }

    public float GetTotalHP()
    {
        var masterData = DataManager.Instance.GetEquipmentData(equipmentId);
        float baseHP = masterData != null ? masterData.stats.baseHP : 0f;
        return baseHP + bonusHP;
    }

    public float GetTotalElementalAttack()
    {
        var masterData = DataManager.Instance.GetEquipmentData(equipmentId);
        float baseElemental = masterData != null ? masterData.stats.baseElementalAttack : 0f;
        return baseElemental + bonusElementalAttack;
    }

    // ���ǉ��F4��ނ̑����U���擾���\�b�h
    public float GetTotalFireAttack()
    {
        var masterData = DataManager.Instance.GetEquipmentData(equipmentId);
        float baseFire = masterData != null ? masterData.stats.baseFireAttack : 0f;
        float total = baseFire + bonusFireAttack;

        // ���f�o�b�O���O�ǉ�
        Debug.Log($"GetTotalFireAttack - ����ID:{equipmentId}, base:{baseFire}, bonus:{bonusFireAttack}, total:{total}");

        return total;
    }

    public float GetTotalWaterAttack()
    {
        var masterData = DataManager.Instance.GetEquipmentData(equipmentId);
        float baseWater = masterData != null ? masterData.stats.baseWaterAttack : 0f;
        return baseWater + bonusWaterAttack;
    }

    public float GetTotalWindAttack()
    {
        var masterData = DataManager.Instance.GetEquipmentData(equipmentId);
        float baseWind = masterData != null ? masterData.stats.baseWindAttack : 0f;
        return baseWind + bonusWindAttack;
    }

    public float GetTotalEarthAttack()
    {
        var masterData = DataManager.Instance.GetEquipmentData(equipmentId);
        float baseEarth = masterData != null ? masterData.stats.baseEarthAttack : 0f;
        return baseEarth + bonusEarthAttack;
    }

    // ���ǉ�: �����\���`�F�b�N
    public bool CanEnhance()
    {
        return currentDurability > 0;
    }

    // ���ǉ�: �ϋv�x����
    public void ReduceDurability(int amount)
    {
        currentDurability = Mathf.Max(0, currentDurability - amount);
    }

    // ���ǉ�: �ϋv�x��
    public void RestoreDurability(int amount)
    {
        var masterData = DataManager.Instance.GetEquipmentData(equipmentId);
        int maxDurability = masterData != null ? masterData.stats.baseDurability : 100;
        currentDurability = Mathf.Min(maxDurability, currentDurability + amount);
    }
}

[System.Serializable]
public class UserItem
{
    public int itemId;
    public int quantity;
    public DateTime lastUsed;
}

[System.Serializable]
public class QuestProgress
{
    public int questId;
    public bool isCleared;
    public int clearCount;
    public DateTime lastClearTime;
    public DateTime firstClearTime;
}

[System.Serializable]
public class UserGameSettings
{
    public bool soundEnabled = true;
    public bool musicEnabled = true;
    public float soundVolume = 1.0f;
    public float musicVolume = 1.0f;
    public bool notificationEnabled = true;
}

[System.Serializable]
public class UserData
{
    [Header("��{���")]
    public string userId;
    public string playerName;
    public int playerLevel;
    public int experience;
    public int currency; // �Q�[�����ʉ�
    public int premiumCurrency; // �ۋ��ʉ�

    [Header("�����֘A")]
    public List<UserEquipment> equipments = new List<UserEquipment>();
    public int maxEquipmentSlots = 10; // �����ۊǘg
    public int[] equippedEquipmentIds = new int[3]; // �������̑���ID�i����3��j

    [Header("�A�C�e��")]
    public List<UserItem> enhancementItems = new List<UserItem>();
    public List<UserItem> supportMaterials = new List<UserItem>();
    public List<UserItem> specialItems = new List<UserItem>();

    [Header("�N�G�X�g�i�s")]
    public List<QuestProgress> questProgress = new List<QuestProgress>();
    public int currentStage = 1;
    public int highestStage = 1;

    [Header("���ԊǗ�")]
    public DateTime lastLoginTime;
    public DateTime lastQuestTime;
    public DateTime lastOfflineRewardTime;
    public long totalPlayTimeSeconds;

    [Header("�K�`���E�ۋ�")]
    public int dailyAdGachaCount;
    public DateTime lastAdGachaResetTime;
    public List<string> purchasedProducts = new List<string>();

    [Header("�ݒ�")]
    public UserGameSettings gameSettings = new UserGameSettings();

    [Header("���v")]
    public int totalEnhancementAttempts;
    public int successfulEnhancements;
    public int totalBattles;
    public int totalVictories;

    // ���ǉ�: �������擾
    public UserEquipment GetEquipment(int index)
    {
        if (index >= 0 && index < equipments.Count)
        {
            return equipments[index];
        }
        return null;
    }

    // ���ǉ�: ������ǉ�
    public void AddEquipment(int equipmentId)
    {
        var masterData = DataManager.Instance.GetEquipmentData(equipmentId);

        var newEquipment = new UserEquipment
        {
            equipmentId = equipmentId,
            enhancementLevel = 0,
            currentDurability = masterData != null ? masterData.stats.baseDurability : 100, // �}�X�^�[�f�[�^���琳�����ϋv�l�擾
            isEquipped = false,
            acquiredDate = DateTime.Now,
            bonusAttackPower = 0,
            bonusDefensePower = 0,
            bonusElementalAttack = 0,
            bonusHP = 0,
            bonusCriticalRate = 0f
        };

        equipments.Add(newEquipment);
    }

    // ���ǉ�: �A�C�e����ǉ�
    public void AddItem(int itemId, int quantity, string itemType = "enhancement")
    {
        List<UserItem> targetList = null;

        switch (itemType)
        {
            case "enhancement":
                targetList = enhancementItems;
                break;
            case "support":
                targetList = supportMaterials;
                break;
            case "special":
                targetList = specialItems;
                break;
            default:
                targetList = enhancementItems;
                break;
        }

        var existingItem = targetList.Find(item => item.itemId == itemId);
        if (existingItem != null)
        {
            existingItem.quantity += quantity;
        }
        else
        {
            var newItem = new UserItem
            {
                itemId = itemId,
                quantity = quantity,
                lastUsed = DateTime.Now
            };
            targetList.Add(newItem);
        }
    }

    // ���ǉ�: �A�C�e��������
    public bool ConsumeItem(int itemId, int quantity, string itemType = "enhancement")
    {
        List<UserItem> targetList = null;

        switch (itemType)
        {
            case "enhancement":
                targetList = enhancementItems;
                break;
            case "support":
                targetList = supportMaterials;
                break;
            case "special":
                targetList = specialItems;
                break;
            default:
                targetList = enhancementItems;
                break;
        }

        var item = targetList.Find(i => i.itemId == itemId);
        if (item != null && item.quantity >= quantity)
        {
            item.quantity -= quantity;
            if (item.quantity <= 0)
            {
                targetList.Remove(item);
            }
            return true;
        }
        return false;
    }

    // ���ǉ�: �A�C�e�����������擾
    public int GetItemQuantity(int itemId, string itemType = "enhancement")
    {
        List<UserItem> targetList = null;

        switch (itemType)
        {
            case "enhancement":
                targetList = enhancementItems;
                break;
            case "support":
                targetList = supportMaterials;
                break;
            case "special":
                targetList = specialItems;
                break;
            default:
                // �S���X�g���猟��
                var item1 = enhancementItems.Find(i => i.itemId == itemId);
                if (item1 != null) return item1.quantity;

                var item2 = supportMaterials.Find(i => i.itemId == itemId);
                if (item2 != null) return item2.quantity;

                var item3 = specialItems.Find(i => i.itemId == itemId);
                if (item3 != null) return item3.quantity;

                return 0;
        }

        var item = targetList.Find(i => i.itemId == itemId);
        return item != null ? item.quantity : 0;
    }

    // ���ǉ�: �ʉ݂�����
    public bool SpendCurrency(int amount)
    {
        if (currency >= amount)
        {
            currency -= amount;
            return true;
        }
        return false;
    }

    // ���ǉ�: �W�F��������
    public bool SpendGems(int amount)
    {
        if (premiumCurrency >= amount)
        {
            premiumCurrency -= amount;
            return true;
        }
        return false;
    }

    // �R���X�g���N�^
    public UserData()
    {
        userId = System.Guid.NewGuid().ToString();
        playerName = "Player";
        playerLevel = 1;
        experience = 0;
        currency = 1000; // �����ʉ�
        premiumCurrency = 0;
        lastLoginTime = DateTime.Now;
        lastQuestTime = DateTime.Now;
        lastOfflineRewardTime = DateTime.Now;
        lastAdGachaResetTime = DateTime.Now;

        // ���������ɔz���������
        for (int i = 0; i < equippedEquipmentIds.Length; i++)
        {
            equippedEquipmentIds[i] = -1; // ��������\��
        }
    }
}