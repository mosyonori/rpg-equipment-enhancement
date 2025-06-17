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

    // 強化によって得られたボーナス（ユーザー固有データ）
    public int bonusAttackPower = 0;
    public int bonusDefensePower = 0;
    public int bonusElementalAttack = 0; // ★既存：汎用属性攻撃（互換性維持）
    public int bonusHP = 0;
    public float bonusCriticalRate = 0f;

    // ★追加：4種類の属性攻撃
    public int bonusFireAttack = 0;      // 火属性攻撃
    public int bonusWaterAttack = 0;     // 水属性攻撃
    public int bonusWindAttack = 0;      // 風属性攻撃
    public int bonusEarthAttack = 0;     // 土属性攻撃

    // 現在のステータス（計算用、保存されない）
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

    // 解放されたスキル
    public List<int> unlockedSkills = new List<int>();
    public int equippedSkillId = -1; // 装備中のスキルID

    // ★追加: 総合ステータス計算メソッド（マスターデータ + ボーナス）
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

    // ★追加：4種類の属性攻撃取得メソッド
    public float GetTotalFireAttack()
    {
        var masterData = DataManager.Instance.GetEquipmentData(equipmentId);
        float baseFire = masterData != null ? masterData.stats.baseFireAttack : 0f;
        float total = baseFire + bonusFireAttack;

        // ★デバッグログ追加
        Debug.Log($"GetTotalFireAttack - 装備ID:{equipmentId}, base:{baseFire}, bonus:{bonusFireAttack}, total:{total}");

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

    // ★追加: 強化可能かチェック
    public bool CanEnhance()
    {
        return currentDurability > 0;
    }

    // ★追加: 耐久度減少
    public void ReduceDurability(int amount)
    {
        currentDurability = Mathf.Max(0, currentDurability - amount);
    }

    // ★追加: 耐久度回復
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
    [Header("基本情報")]
    public string userId;
    public string playerName;
    public int playerLevel;
    public int experience;
    public int currency; // ゲーム内通貨
    public int premiumCurrency; // 課金通貨

    [Header("装備関連")]
    public List<UserEquipment> equipments = new List<UserEquipment>();
    public int maxEquipmentSlots = 10; // 装備保管枠
    public int[] equippedEquipmentIds = new int[3]; // 装備中の装備ID（武器3種）

    [Header("アイテム")]
    public List<UserItem> enhancementItems = new List<UserItem>();
    public List<UserItem> supportMaterials = new List<UserItem>();
    public List<UserItem> specialItems = new List<UserItem>();

    [Header("クエスト進行")]
    public List<QuestProgress> questProgress = new List<QuestProgress>();
    public int currentStage = 1;
    public int highestStage = 1;

    [Header("時間管理")]
    public DateTime lastLoginTime;
    public DateTime lastQuestTime;
    public DateTime lastOfflineRewardTime;
    public long totalPlayTimeSeconds;

    [Header("ガチャ・課金")]
    public int dailyAdGachaCount;
    public DateTime lastAdGachaResetTime;
    public List<string> purchasedProducts = new List<string>();

    [Header("設定")]
    public UserGameSettings gameSettings = new UserGameSettings();

    [Header("統計")]
    public int totalEnhancementAttempts;
    public int successfulEnhancements;
    public int totalBattles;
    public int totalVictories;

    // ★追加: 装備を取得
    public UserEquipment GetEquipment(int index)
    {
        if (index >= 0 && index < equipments.Count)
        {
            return equipments[index];
        }
        return null;
    }

    // ★追加: 装備を追加
    public void AddEquipment(int equipmentId)
    {
        var masterData = DataManager.Instance.GetEquipmentData(equipmentId);

        var newEquipment = new UserEquipment
        {
            equipmentId = equipmentId,
            enhancementLevel = 0,
            currentDurability = masterData != null ? masterData.stats.baseDurability : 100, // マスターデータから正しい耐久値取得
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

    // ★追加: アイテムを追加
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

    // ★追加: アイテムを消費
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

    // ★追加: アイテム所持数を取得
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
                // 全リストから検索
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

    // ★追加: 通貨を消費
    public bool SpendCurrency(int amount)
    {
        if (currency >= amount)
        {
            currency -= amount;
            return true;
        }
        return false;
    }

    // ★追加: ジェムを消費
    public bool SpendGems(int amount)
    {
        if (premiumCurrency >= amount)
        {
            premiumCurrency -= amount;
            return true;
        }
        return false;
    }

    // コンストラクタ
    public UserData()
    {
        userId = System.Guid.NewGuid().ToString();
        playerName = "Player";
        playerLevel = 1;
        experience = 0;
        currency = 1000; // 初期通貨
        premiumCurrency = 0;
        lastLoginTime = DateTime.Now;
        lastQuestTime = DateTime.Now;
        lastOfflineRewardTime = DateTime.Now;
        lastAdGachaResetTime = DateTime.Now;

        // 初期化時に配列を初期化
        for (int i = 0; i < equippedEquipmentIds.Length; i++)
        {
            equippedEquipmentIds[i] = -1; // 未装備を表す
        }
    }
}