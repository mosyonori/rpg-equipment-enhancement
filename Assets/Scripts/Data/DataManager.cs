using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    [Header("マスターデータ")]
    public EquipmentData[] equipmentDatabase;
    public EnhancementItemData[] enhancementItemDatabase;
    public SupportMaterialData[] supportMaterialDatabase;
    public QuestData[] questDatabase;

    [Header("現在のユーザーデータ")]
    public UserData currentUserData;

    [Header("デバッグ設定")]
    public bool useTemporaryDataForTesting = true;

    private static DataManager instance;
    public static DataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DataManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("DataManager");
                    instance = go.AddComponent<DataManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private const string SAVE_KEY = "UserGameData";
    private const string ENCRYPTION_KEY = "YourGameEncryptionKey2024";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadUserData();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && !useTemporaryDataForTesting) SaveUserData();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && !useTemporaryDataForTesting) SaveUserData();
    }

    void OnDestroy()
    {
        if (!useTemporaryDataForTesting) SaveUserData();
    }

    #region セーブ・ロード

    public void SaveUserData()
    {
        if (useTemporaryDataForTesting) return;

        try
        {
            string jsonData = JsonUtility.ToJson(currentUserData, true);
            string encryptedData = EncryptString(jsonData);
            PlayerPrefs.SetString(SAVE_KEY, encryptedData);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"データ保存エラー: {e.Message}");
        }
    }

    public void LoadUserData()
    {
        if (useTemporaryDataForTesting)
        {
            currentUserData = new UserData();
            ValidateUserData();
            return;
        }

        try
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string encryptedData = PlayerPrefs.GetString(SAVE_KEY);
                string jsonData = DecryptString(encryptedData);
                currentUserData = JsonUtility.FromJson<UserData>(jsonData);
            }
            else
            {
                currentUserData = new UserData();
                SaveUserData();
            }

            ValidateUserData();
        }
        catch (Exception e)
        {
            Debug.LogError($"データ読み込みエラー: {e.Message}");
            currentUserData = new UserData();
            SaveUserData();
        }
    }

    public void ResetUserData()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        currentUserData = new UserData();
        ValidateUserData();
        if (!useTemporaryDataForTesting) SaveUserData();
    }

    #endregion

    #region 暗号化・復号化

    private string EncryptString(string plainText)
    {
        byte[] data = Encoding.UTF8.GetBytes(plainText);
        byte[] key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY);

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(data[i] ^ key[i % key.Length]);
        }

        return Convert.ToBase64String(data);
    }

    private string DecryptString(string encryptedText)
    {
        byte[] data = Convert.FromBase64String(encryptedText);
        byte[] key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY);

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(data[i] ^ key[i % key.Length]);
        }

        return Encoding.UTF8.GetString(data);
    }

    #endregion

    #region マスターデータ取得

    public EquipmentData GetEquipmentData(int equipmentId)
    {
        foreach (var equipment in equipmentDatabase)
        {
            if (equipment.equipmentId == equipmentId)
                return equipment;
        }
        return null;
    }

    public EnhancementItemData GetEnhancementItemData(int itemId)
    {
        foreach (var item in enhancementItemDatabase)
        {
            if (item.itemId == itemId)
                return item;
        }
        return null;
    }

    public SupportMaterialData GetSupportMaterialData(int materialId)
    {
        foreach (var material in supportMaterialDatabase)
        {
            if (material.materialId == materialId)
                return material;
        }
        return null;
    }

    #endregion

    #region クエストデータ取得

    public List<QuestData> GetAllQuestData()
    {
        var questList = new List<QuestData>();

        if (questDatabase != null && questDatabase.Length > 0)
        {
            questList.AddRange(questDatabase);
            questList.Sort((a, b) => a.questId.CompareTo(b.questId));
        }
        else
        {
            Debug.LogError("questDatabaseが設定されていません");
        }

        return questList;
    }

    public QuestData GetQuestData(int questId)
    {
        if (questDatabase != null)
        {
            foreach (var questData in questDatabase)
            {
                if (questData.questId == questId)
                    return questData;
            }
        }
        return null;
    }

    #endregion

    #region クエスト機能

    public int GetPlayerLevel()
    {
        return currentUserData?.playerLevel ?? 1;
    }

    public int GetCurrentStamina()
    {
        return 100; // 仮実装
    }

    public bool ConsumeStamina(int amount)
    {
        Debug.Log($"スタミナ {amount} を消費しました");
        return true; // 仮実装
    }

    public bool IsQuestCleared(int questId)
    {
        if (currentUserData?.questProgress == null) return false;
        var progress = currentUserData.questProgress.Find(q => q.questId == questId);
        return progress != null && progress.isCleared;
    }

    public int GetQuestRemainingClears(int questId)
    {
        var questData = GetQuestData(questId);
        if (questData == null) return 0;

        if (questData.clearLimit == -1) return 999;

        var progress = currentUserData?.questProgress?.Find(q => q.questId == questId);
        int currentClears = progress?.clearCount ?? 0;
        return Mathf.Max(0, questData.clearLimit - currentClears);
    }

    public bool StartQuest(int questId)
    {
        var questData = GetQuestData(questId);
        if (questData == null) return false;

        if (GetPlayerLevel() < questData.requiredLevel) return false;
        if (questData.prerequisiteQuestIds != null)
        {
            foreach (int prereqId in questData.prerequisiteQuestIds)
            {
                if (!IsQuestCleared(prereqId)) return false;
            }
        }
        if (questData.clearLimit > 0 && GetQuestRemainingClears(questId) <= 0) return false;
        if (GetCurrentStamina() < questData.requiredStamina) return false;

        if (!ConsumeStamina(questData.requiredStamina)) return false;

        Debug.Log($"クエスト開始: {questData.questName}");
        return true;
    }

    public void CompleteQuest(int questId)
    {
        if (currentUserData == null) return;

        var progress = currentUserData.questProgress.Find(q => q.questId == questId);
        if (progress == null)
        {
            progress = new QuestProgress
            {
                questId = questId,
                isCleared = true,
                clearCount = 1,
                firstClearTime = DateTime.Now,
                lastClearTime = DateTime.Now
            };
            currentUserData.questProgress.Add(progress);
        }
        else
        {
            progress.isCleared = true;
            progress.clearCount++;
            progress.lastClearTime = DateTime.Now;
        }

        SaveUserData();
    }

    #endregion

    #region 属性制限システム

    public bool EnhanceEquipmentWithElementalCheck(int equipmentIndex, int enhancementItemId, int supportItemId = -1)
    {
        var userEquipment = GetUserEquipment(equipmentIndex);
        var enhancementItem = GetEnhancementItemData(enhancementItemId);

        if (userEquipment == null || enhancementItem == null) return false;
        if (!enhancementItem.CanUseOnEquipment(userEquipment)) return false;

        return EnhanceEquipment(equipmentIndex, enhancementItemId, supportItemId);
    }

    #endregion

    #region ユーザーデータ操作

    public UserEquipment GetUserEquipment(int index)
    {
        return currentUserData.GetEquipment(index);
    }

    public List<UserEquipment> GetAllUserEquipments()
    {
        return currentUserData.equipments;
    }

    public int GetItemQuantity(int itemId)
    {
        return currentUserData.GetItemQuantity(itemId);
    }

    public bool ConsumeItem(int itemId, int quantity = 1)
    {
        bool result = currentUserData.ConsumeItem(itemId, quantity, "enhancement");
        if (result)
        {
            if (!useTemporaryDataForTesting) SaveUserData();
            return true;
        }

        result = currentUserData.ConsumeItem(itemId, quantity, "support");
        if (result)
        {
            if (!useTemporaryDataForTesting) SaveUserData();
            return true;
        }

        return false;
    }

    public void AddItem(int itemId, int quantity)
    {
        var enhancementItem = GetEnhancementItemData(itemId);
        if (enhancementItem != null)
        {
            currentUserData.AddItem(itemId, quantity, "enhancement");
            if (!useTemporaryDataForTesting) SaveUserData();
            return;
        }

        var supportItem = GetSupportMaterialData(itemId);
        if (supportItem != null)
        {
            currentUserData.AddItem(itemId, quantity, "support");
            if (!useTemporaryDataForTesting) SaveUserData();
            return;
        }

        currentUserData.AddItem(itemId, quantity, "enhancement");
        if (!useTemporaryDataForTesting) SaveUserData();
    }

    public bool EnhanceEquipment(int equipmentIndex, int enhancementItemId, int supportItemId = -1)
    {
        var userEquipment = GetUserEquipment(equipmentIndex);
        if (userEquipment == null || !userEquipment.CanEnhance()) return false;

        var enhancementItem = GetEnhancementItemData(enhancementItemId);
        if (enhancementItem == null) return false;
        if (GetItemQuantity(enhancementItemId) <= 0) return false;

        SupportMaterialData supportItem = null;
        if (supportItemId >= 0)
        {
            supportItem = GetSupportMaterialData(supportItemId);
            if (supportItem != null && GetItemQuantity(supportItemId) <= 0) return false;
        }

        float successRate = enhancementItem.GetAdjustedSuccessRate(userEquipment.enhancementLevel);

        if (supportItem != null)
        {
            successRate += supportItem.successRateModifier;
            if (supportItem.guaranteeSuccess) successRate = 1.0f;
        }

        successRate = Mathf.Clamp01(successRate);

        bool isSuccess = UnityEngine.Random.Range(0f, 1f) <= successRate;
        bool isGreatSuccess = isSuccess && UnityEngine.Random.Range(0f, 1f) <= 0.1f;

        ConsumeItem(enhancementItemId, 1);
        if (supportItem != null) ConsumeItem(supportItemId, 1);

        var equipmentMasterData = GetEquipmentData(userEquipment.equipmentId);
        EquipmentType currentEquipmentType = equipmentMasterData?.equipmentType ?? EquipmentType.Weapon;
        int baseDurabilityReduction = enhancementItem.GetDurabilityReduction(currentEquipmentType);
        int finalDurabilityReduction = baseDurabilityReduction;

        if (supportItem != null)
        {
            finalDurabilityReduction = supportItem.CalculateDurabilityReduction(isSuccess, baseDurabilityReduction);
        }

        finalDurabilityReduction = Mathf.Max(0, finalDurabilityReduction);
        int enhancementValueIncrease = enhancementItem.GetEnhancementValue(currentEquipmentType);

        if (isGreatSuccess)
        {
            ApplyEnhancementBonus(userEquipment, enhancementItem, 2.0f);
            int greatSuccessEnhancementIncrease = enhancementValueIncrease * 2;
            userEquipment.enhancementLevel += greatSuccessEnhancementIncrease;
        }
        else if (isSuccess)
        {
            ApplyEnhancementBonus(userEquipment, enhancementItem, 1.0f);
            userEquipment.enhancementLevel += enhancementValueIncrease;
            userEquipment.ReduceDurability(finalDurabilityReduction);
        }
        else
        {
            userEquipment.ReduceDurability(finalDurabilityReduction);
        }

        currentUserData.totalEnhancementAttempts++;
        if (isSuccess) currentUserData.successfulEnhancements++;

        if (!useTemporaryDataForTesting) SaveUserData();
        return isSuccess;
    }

    private void ApplyEnhancementBonus(UserEquipment userEquipment, EnhancementItemData enhancementItem, float multiplier)
    {
        var equipmentData = GetEquipmentData(userEquipment.equipmentId);
        EquipmentType equipmentType = equipmentData?.equipmentType ?? EquipmentType.Weapon;

        userEquipment.bonusAttackPower += Mathf.RoundToInt(enhancementItem.GetAttackPowerBonus(equipmentType) * multiplier);
        userEquipment.bonusDefensePower += Mathf.RoundToInt(enhancementItem.GetDefensePowerBonus(equipmentType) * multiplier);
        userEquipment.bonusElementalAttack += Mathf.RoundToInt(enhancementItem.GetElementalAttackBonus(equipmentType) * multiplier);
        userEquipment.bonusHP += Mathf.RoundToInt(enhancementItem.GetHPBonus(equipmentType) * multiplier);
        userEquipment.bonusCriticalRate += enhancementItem.GetCriticalRateBonus(equipmentType) * multiplier;

        userEquipment.bonusFireAttack += Mathf.RoundToInt(enhancementItem.GetFireAttackBonus(equipmentType) * multiplier);
        userEquipment.bonusWaterAttack += Mathf.RoundToInt(enhancementItem.GetWaterAttackBonus(equipmentType) * multiplier);
        userEquipment.bonusWindAttack += Mathf.RoundToInt(enhancementItem.GetWindAttackBonus(equipmentType) * multiplier);
        userEquipment.bonusEarthAttack += Mathf.RoundToInt(enhancementItem.GetEarthAttackBonus(equipmentType) * multiplier);
    }

    public void AddEquipment(int equipmentId)
    {
        currentUserData.AddEquipment(equipmentId);
        if (!useTemporaryDataForTesting) SaveUserData();
    }

    public bool UseItem(int itemId, int quantity = 1)
    {
        return ConsumeItem(itemId, quantity);
    }

    #endregion

    #region データ整合性チェック

    private void ValidateUserData()
    {
        if (currentUserData.maxEquipmentSlots < 10)
        {
            currentUserData.maxEquipmentSlots = 10;
        }

        if (useTemporaryDataForTesting || currentUserData.equipments.Count == 0)
        {
            currentUserData.equipments.Clear();

            var equipment3Data = GetEquipmentData(3);
            if (equipment3Data != null)
            {
                var equipment3 = new UserEquipment
                {
                    equipmentId = 3,
                    enhancementLevel = 0,
                    currentDurability = equipment3Data.stats.baseDurability,
                    isEquipped = false,
                    acquiredDate = DateTime.Now
                };
                currentUserData.equipments.Add(equipment3);
            }

            var equipment4Data = GetEquipmentData(4);
            if (equipment4Data != null)
            {
                var equipment4 = new UserEquipment
                {
                    equipmentId = 4,
                    enhancementLevel = 0,
                    currentDurability = equipment4Data.stats.baseDurability,
                    isEquipped = false,
                    acquiredDate = DateTime.Now
                };
                currentUserData.equipments.Add(equipment4);
            }

            var fireKnifeData = GetEquipmentData(2);
            if (fireKnifeData != null)
            {
                var fireKnife = new UserEquipment
                {
                    equipmentId = 2,
                    enhancementLevel = 0,
                    currentDurability = fireKnifeData.stats.baseDurability,
                    isEquipped = false,
                    acquiredDate = DateTime.Now
                };
                currentUserData.equipments.Add(fireKnife);
            }

            var starterEquipData = GetEquipmentData(1);
            if (starterEquipData != null)
            {
                var starterWeapon = new UserEquipment
                {
                    equipmentId = 1,
                    enhancementLevel = 0,
                    currentDurability = starterEquipData.stats.baseDurability,
                    isEquipped = true,
                    acquiredDate = DateTime.Now
                };

                currentUserData.equipments.Add(starterWeapon);
                currentUserData.equippedEquipmentIds[0] = 1;
            }
        }

        if (useTemporaryDataForTesting || currentUserData.enhancementItems.Count == 0)
        {
            currentUserData.enhancementItems.Clear();
            currentUserData.supportMaterials.Clear();

            AddItem(1, 15);
            AddItem(2, 80);
            AddItem(3, 80);
            AddItem(4, 50);
            AddItem(5, 50);
            AddItem(6, 50);
            AddItem(7, 50);

            currentUserData.AddItem(1, 50, "support");
            currentUserData.AddItem(2, 30, "support");
        }

        if (currentUserData.lastLoginTime > DateTime.Now)
        {
            currentUserData.lastLoginTime = DateTime.Now;
        }

        if (DateTime.Now.Date > currentUserData.lastAdGachaResetTime.Date)
        {
            currentUserData.dailyAdGachaCount = 0;
            currentUserData.lastAdGachaResetTime = DateTime.Now;
        }
    }

    #endregion
}