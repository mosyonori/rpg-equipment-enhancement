using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    [Header("�}�X�^�[�f�[�^")]
    public EquipmentData[] equipmentDatabase;
    public EnhancementItemData[] enhancementItemDatabase;
    public SupportMaterialData[] supportMaterialDatabase;

    [Header("���݂̃��[�U�[�f�[�^")]
    public UserData currentUserData;

    [Header("�f�o�b�O�ݒ�")]
    public bool useTemporaryDataForTesting = true; // �e�X�g�p: true�ɂ���ƃZ�[�u�f�[�^���g��Ȃ�

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
        if (pauseStatus && !useTemporaryDataForTesting)
        {
            SaveUserData();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && !useTemporaryDataForTesting)
        {
            SaveUserData();
        }
    }

    void OnDestroy()
    {
        if (!useTemporaryDataForTesting)
        {
            SaveUserData();
        }
    }

    #region �Z�[�u�E���[�h

    public void SaveUserData()
    {
        if (useTemporaryDataForTesting)
        {
            return;
        }

        try
        {
            string jsonData = JsonUtility.ToJson(currentUserData, true);
            string encryptedData = EncryptString(jsonData);
            PlayerPrefs.SetString(SAVE_KEY, encryptedData);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"�f�[�^�ۑ��G���[: {e.Message}");
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
            Debug.LogError($"�f�[�^�ǂݍ��݃G���[: {e.Message}");
            currentUserData = new UserData();
            SaveUserData();
        }
    }

    [ContextMenu("Reset User Data for Testing")]
    public void ResetUserDataForTesting()
    {
        currentUserData = new UserData();
        ValidateUserData();
    }

    public void ResetUserData()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        currentUserData = new UserData();
        ValidateUserData();
        if (!useTemporaryDataForTesting)
        {
            SaveUserData();
        }
    }

    #endregion

    #region �Í����E������

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

    #region �}�X�^�[�f�[�^�擾

    public EquipmentData GetEquipmentData(int equipmentId)
    {
        foreach (var equipment in equipmentDatabase)
        {
            if (equipment.equipmentId == equipmentId)
                return equipment;
        }

        Debug.LogWarning($"����ID {equipmentId} ��������܂���");
        return null;
    }

    public EnhancementItemData GetEnhancementItemData(int itemId)
    {
        foreach (var item in enhancementItemDatabase)
        {
            if (item.itemId == itemId)
                return item;
        }

        Debug.LogWarning($"�����A�C�e��ID {itemId} ��������܂���");
        return null;
    }

    public SupportMaterialData GetSupportMaterialData(int materialId)
    {
        foreach (var material in supportMaterialDatabase)
        {
            if (material.materialId == materialId)
                return material;
        }

        Debug.LogWarning($"�⏕�ޗ�ID {materialId} ��������܂���");
        return null;
    }

    #endregion

    #region ���������V�X�e��

    /// <summary>
    /// �����������l�������������s
    /// </summary>
    public bool EnhanceEquipmentWithElementalCheck(int equipmentIndex, int enhancementItemId, int supportItemId = -1)
    {
        var userEquipment = GetUserEquipment(equipmentIndex);
        var enhancementItem = GetEnhancementItemData(enhancementItemId);

        if (userEquipment == null || enhancementItem == null)
        {
            Debug.LogError("�����܂��͋����A�C�e����������܂���");
            return false;
        }

        // ���������`�F�b�N
        if (!enhancementItem.CanUseOnEquipment(userEquipment))
        {
            string reason = enhancementItem.GetRestrictionReason(userEquipment);
            Debug.LogWarning($"���������ɂ�苭���ł��܂���: {reason}");
            return false;
        }

        // �����̋������������s
        return EnhanceEquipment(equipmentIndex, enhancementItemId, supportItemId);
    }

    /// <summary>
    /// �g�p�\�ȋ����A�C�e���̃��X�g���擾�i���������l���j
    /// </summary>
    public List<EnhancementItemData> GetAvailableEnhancementItems(int equipmentIndex)
    {
        var userEquipment = GetUserEquipment(equipmentIndex);
        if (userEquipment == null) return new List<EnhancementItemData>();

        var availableItems = new List<EnhancementItemData>();

        foreach (var itemData in enhancementItemDatabase)
        {
            // �������`�F�b�N
            int quantity = GetItemQuantity(itemData.itemId);
            if (quantity <= 0) continue;

            // ���������`�F�b�N
            if (!itemData.CanUseOnEquipment(userEquipment)) continue;

            availableItems.Add(itemData);
        }

        return availableItems;
    }

    /// <summary>
    /// �������������܂ދ����A�C�e�������擾
    /// </summary>
    public List<EnhancementItemInfo> GetEnhancementItemsWithRestrictionInfo(int equipmentIndex)
    {
        var userEquipment = GetUserEquipment(equipmentIndex);
        var itemInfoList = new List<EnhancementItemInfo>();

        foreach (var itemData in enhancementItemDatabase)
        {
            int quantity = GetItemQuantity(itemData.itemId);
            if (quantity <= 0) continue;

            bool canUse = userEquipment != null ? itemData.CanUseOnEquipment(userEquipment) : true;
            string restrictionReason = userEquipment != null ? itemData.GetRestrictionReason(userEquipment) : "";

            itemInfoList.Add(new EnhancementItemInfo
            {
                itemData = itemData,
                quantity = quantity,
                canUse = canUse,
                restrictionReason = restrictionReason
            });
        }

        return itemInfoList;
    }

    /// <summary>
    /// �����̑��������擾�i�f�o�b�O�p�j
    /// </summary>
    public string GetEquipmentElementalInfo(int equipmentIndex)
    {
        var userEquipment = GetUserEquipment(equipmentIndex);
        if (userEquipment == null) return "������������܂���";

        ElementalType currentType = userEquipment.GetCurrentElementalType();
        string typeName = UserEquipment.GetElementalTypeName(currentType);

        return $"���݂̑���: {typeName}����\n" +
               $"�΍U��: {userEquipment.GetTotalFireAttack()}\n" +
               $"���U��: {userEquipment.GetTotalWaterAttack()}\n" +
               $"���U��: {userEquipment.GetTotalWindAttack()}\n" +
               $"�y�U��: {userEquipment.GetTotalEarthAttack()}";
    }

    #endregion

    #region ���[�U�[�f�[�^����i�������\�b�h + �����Ή������j

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

    /// <summary>
    /// �����̋������\�b�h�i���������Ȃ��A����݊����̂��ߕی�j
    /// </summary>
    public bool EnhanceEquipment(int equipmentIndex, int enhancementItemId, int supportItemId = -1)
    {
        var userEquipment = GetUserEquipment(equipmentIndex);
        if (userEquipment == null || !userEquipment.CanEnhance())
        {
            Debug.LogError("�����������ł��܂���");
            return false;
        }

        var enhancementItem = GetEnhancementItemData(enhancementItemId);
        if (enhancementItem == null)
        {
            Debug.LogError("�����A�C�e����������܂���");
            return false;
        }

        if (GetItemQuantity(enhancementItemId) <= 0)
        {
            Debug.LogError("�����A�C�e�����s�����Ă��܂�");
            return false;
        }

        SupportMaterialData supportItem = null;
        if (supportItemId >= 0)
        {
            supportItem = GetSupportMaterialData(supportItemId);
            if (supportItem != null && GetItemQuantity(supportItemId) <= 0)
            {
                Debug.LogError("�⏕�A�C�e�����s�����Ă��܂�");
                return false;
            }
        }

        // �����m���v�Z
        float successRate = enhancementItem.GetAdjustedSuccessRate(userEquipment.enhancementLevel);

        // �⏕�A�C�e���̌��ʓK�p
        if (supportItem != null)
        {
            // �������C��
            successRate += supportItem.successRateModifier;

            // �����ۏ،���
            if (supportItem.guaranteeSuccess)
            {
                successRate = 1.0f;
            }
        }

        successRate = Mathf.Clamp01(successRate);

        // ��������
        bool isSuccess = UnityEngine.Random.Range(0f, 1f) <= successRate;
        bool isGreatSuccess = isSuccess && UnityEngine.Random.Range(0f, 1f) <= 0.1f;

        // ���d�v: �A�C�e���͋������s�Ɠ����ɕK�������
        ConsumeItem(enhancementItemId, 1);
        if (supportItem != null)
        {
            ConsumeItem(supportItemId, 1);
        }

        // �ϋv�x�����̌v�Z�i������ނ��l���j
        var equipmentMasterData = GetEquipmentData(userEquipment.equipmentId);
        EquipmentType currentEquipmentType = equipmentMasterData?.equipmentType ?? EquipmentType.Weapon;
        int baseDurabilityReduction = enhancementItem.GetDurabilityReduction(currentEquipmentType);
        int finalDurabilityReduction = baseDurabilityReduction;

        // ���C���_�F�⏕�A�C�e���ɂ��ϋv�x�C����K�p
        if (supportItem != null)
        {
            // �V�����V�X�e��: ����/���s�ɉ������ϋv�x�v�Z
            finalDurabilityReduction = supportItem.CalculateDurabilityReduction(isSuccess, baseDurabilityReduction);
            Debug.Log($"�⏕�A�C�e�����ʓK�p: ��{����{baseDurabilityReduction} �� �ŏI����{finalDurabilityReduction}");
        }

        // �ϋv�x������0�����ɂȂ�Ȃ��悤��
        finalDurabilityReduction = Mathf.Max(0, finalDurabilityReduction);

        // ���C��: �����l�̐L�т������A�C�e������擾
        int enhancementValueIncrease = enhancementItem.GetEnhancementValue(currentEquipmentType);

        // ���ʏ���
        if (isGreatSuccess)
        {
            ApplyEnhancementBonus(userEquipment, enhancementItem, 2.0f);
            // ���C��: �听�����͋����l��2�{�ǉ�
            int greatSuccessEnhancementIncrease = enhancementValueIncrease * 2;
            userEquipment.enhancementLevel += greatSuccessEnhancementIncrease;
            Debug.Log($"�听���I �����l�ǉ�: +{greatSuccessEnhancementIncrease}�i�ʏ�{enhancementValueIncrease}��2�{�j, ���݂̋������x��: +{userEquipment.enhancementLevel}");
        }
        else if (isSuccess)
        {
            ApplyEnhancementBonus(userEquipment, enhancementItem, 1.0f);
            userEquipment.enhancementLevel += enhancementValueIncrease;
            userEquipment.ReduceDurability(finalDurabilityReduction);
            Debug.Log($"�����I �����l�ǉ�: +{enhancementValueIncrease}, ���݂̋������x��: +{userEquipment.enhancementLevel}, �ϋv����: {finalDurabilityReduction}");
        }
        else
        {
            // ���s��̑ϋv�x����
            userEquipment.ReduceDurability(finalDurabilityReduction);
            Debug.Log($"���s... �ϋv����: {finalDurabilityReduction}");
        }

        currentUserData.totalEnhancementAttempts++;
        if (isSuccess)
        {
            currentUserData.successfulEnhancements++;
        }

        if (!useTemporaryDataForTesting) SaveUserData();

        return isSuccess;
    }

    /// <summary>
    /// �C����: ������ނ��l�����������U���{�[�i�X�K�p
    /// </summary>
    private void ApplyEnhancementBonus(UserEquipment userEquipment, EnhancementItemData enhancementItem, float multiplier)
    {
        // �����̃}�X�^�[�f�[�^���擾���đ�����ނ��m�F
        var equipmentData = GetEquipmentData(userEquipment.equipmentId);
        EquipmentType equipmentType = equipmentData?.equipmentType ?? EquipmentType.Weapon;

        // ������ނɉ������{�[�i�X�l���擾
        userEquipment.bonusAttackPower += Mathf.RoundToInt(enhancementItem.GetAttackPowerBonus(equipmentType) * multiplier);
        userEquipment.bonusDefensePower += Mathf.RoundToInt(enhancementItem.GetDefensePowerBonus(equipmentType) * multiplier);
        userEquipment.bonusElementalAttack += Mathf.RoundToInt(enhancementItem.GetElementalAttackBonus(equipmentType) * multiplier);
        userEquipment.bonusHP += Mathf.RoundToInt(enhancementItem.GetHPBonus(equipmentType) * multiplier);
        userEquipment.bonusCriticalRate += enhancementItem.GetCriticalRateBonus(equipmentType) * multiplier;

        // �d�v: 4��ނ̑����U���{�[�i�X�K�p�i������ލl���j
        userEquipment.bonusFireAttack += Mathf.RoundToInt(enhancementItem.GetFireAttackBonus(equipmentType) * multiplier);
        userEquipment.bonusWaterAttack += Mathf.RoundToInt(enhancementItem.GetWaterAttackBonus(equipmentType) * multiplier);
        userEquipment.bonusWindAttack += Mathf.RoundToInt(enhancementItem.GetWindAttackBonus(equipmentType) * multiplier);
        userEquipment.bonusEarthAttack += Mathf.RoundToInt(enhancementItem.GetEarthAttackBonus(equipmentType) * multiplier);

        // �f�o�b�O���O�ǉ��i������ޏ����܂ށj
        Debug.Log($"�����{�[�i�X�K�p[{GetEquipmentTypeName(equipmentType)}]: " +
                  $"�U��+{Mathf.RoundToInt(enhancementItem.GetAttackPowerBonus(equipmentType) * multiplier)}, " +
                  $"�h��+{Mathf.RoundToInt(enhancementItem.GetDefensePowerBonus(equipmentType) * multiplier)}, " +
                  $"��+{Mathf.RoundToInt(enhancementItem.GetFireAttackBonus(equipmentType) * multiplier)}, " +
                  $"��+{Mathf.RoundToInt(enhancementItem.GetWaterAttackBonus(equipmentType) * multiplier)}, " +
                  $"��+{Mathf.RoundToInt(enhancementItem.GetWindAttackBonus(equipmentType) * multiplier)}, " +
                  $"�y+{Mathf.RoundToInt(enhancementItem.GetEarthAttackBonus(equipmentType) * multiplier)}");
    }

    /// <summary>
    /// ������ނ̓��{�ꖼ���擾
    /// </summary>
    private string GetEquipmentTypeName(EquipmentType type)
    {
        switch (type)
        {
            case EquipmentType.Weapon: return "����";
            case EquipmentType.Armor: return "�h��";
            case EquipmentType.Accessory: return "�A�N�Z�T���[";
            default: return "�s��";
        }
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

    #region �f�[�^�������`�F�b�N

    private void ValidateUserData()
    {
        if (currentUserData.maxEquipmentSlots < 10)
        {
            currentUserData.maxEquipmentSlots = 10;
        }

        // �C��: ���������𖈉񃊃Z�b�g�i�e�X�g�p�j
        if (useTemporaryDataForTesting || currentUserData.equipments.Count == 0)
        {
            currentUserData.equipments.Clear(); // �����f�[�^���N���A

            // ���ǉ�: ID 3, 4�̐V�����f�[�^��ǉ�
            // ����ID 3�̃e�X�g�f�[�^
            var equipment3Data = GetEquipmentData(3);
            if (equipment3Data != null)
            {
                var equipment3 = new UserEquipment
                {
                    equipmentId = 3,
                    enhancementLevel = 0,
                    currentDurability = equipment3Data.stats.baseDurability,
                    isEquipped = false,
                    acquiredDate = DateTime.Now,
                    bonusAttackPower = 0,
                    bonusDefensePower = 0,
                    bonusElementalAttack = 0,
                    bonusHP = 0,
                    bonusCriticalRate = 0f,
                    bonusFireAttack = 0,
                    bonusWaterAttack = 0,
                    bonusWindAttack = 0,
                    bonusEarthAttack = 0
                };
                currentUserData.equipments.Add(equipment3);
                Debug.Log($"�e�X�g�f�[�^�ɑ���ID 3��ǉ�: {equipment3Data.equipmentName}");
            }

            // ����ID 4�̃e�X�g�f�[�^
            var equipment4Data = GetEquipmentData(4);
            if (equipment4Data != null)
            {
                var equipment4 = new UserEquipment
                {
                    equipmentId = 4,
                    enhancementLevel = 0,
                    currentDurability = equipment4Data.stats.baseDurability,
                    isEquipped = false,
                    acquiredDate = DateTime.Now,
                    bonusAttackPower = 0,
                    bonusDefensePower = 0,
                    bonusElementalAttack = 0,
                    bonusHP = 0,
                    bonusCriticalRate = 0f,
                    bonusFireAttack = 0,
                    bonusWaterAttack = 0,
                    bonusWindAttack = 0,
                    bonusEarthAttack = 0
                };
                currentUserData.equipments.Add(equipment4);
                Debug.Log($"�e�X�g�f�[�^�ɑ���ID 4��ǉ�: {equipment4Data.equipmentName}");
            }

            // �V�����ǉ�: �΂̃_�K�[
            var fireKnifeData = GetEquipmentData(2); // ID 2���΂̃_�K�[�̏ꍇ
            if (fireKnifeData != null)
            {
                var fireKnife = new UserEquipment
                {
                    equipmentId = 2,
                    enhancementLevel = 0,
                    currentDurability = fireKnifeData.stats.baseDurability,
                    isEquipped = false,
                    acquiredDate = DateTime.Now,
                    bonusAttackPower = 0,
                    bonusDefensePower = 0,
                    bonusElementalAttack = 0,
                    bonusHP = 0,
                    bonusCriticalRate = 0f,
                    bonusFireAttack = 0,
                    bonusWaterAttack = 0,
                    bonusWindAttack = 0,
                    bonusEarthAttack = 0
                };
                currentUserData.equipments.Add(fireKnife);
            }

            // ���S�җp������ǉ��i�}�X�^�[�f�[�^���琳�����ϋv�l���擾�j
            var starterEquipData = GetEquipmentData(1);
            if (starterEquipData != null)
            {
                var starterWeapon = new UserEquipment
                {
                    equipmentId = 1,
                    enhancementLevel = 0,
                    currentDurability = starterEquipData.stats.baseDurability,
                    isEquipped = true,
                    acquiredDate = DateTime.Now,
                    bonusAttackPower = 0,
                    bonusDefensePower = 0,
                    bonusElementalAttack = 0,
                    bonusHP = 0,
                    bonusCriticalRate = 0f,
                    bonusFireAttack = 0,
                    bonusWaterAttack = 0,
                    bonusWindAttack = 0,
                    bonusEarthAttack = 0
                };

                currentUserData.equipments.Add(starterWeapon);
                currentUserData.equippedEquipmentIds[0] = 1;
            }
        }

        // �����A�C�e���ǉ��i�e�X�g�p�j
        if (useTemporaryDataForTesting || currentUserData.enhancementItems.Count == 0)
        {
            currentUserData.enhancementItems.Clear();
            currentUserData.supportMaterials.Clear();

            // �����̋����A�C�e��
            AddItem(1, 15); // ��{������x15
            AddItem(2, 80);  // ����������x8
            AddItem(3, 80);  // ����������x8

            // �V�������������A�C�e��
            AddItem(4, 50);  // �΂̃��r�[x5
            AddItem(5, 50);  // ���̃A�N�A�}����x5
            AddItem(6, 50);  // ���̃I�p�[��x5
            AddItem(7, 50);  // �y�̃g�p�[�Yx5

            // �����̕⏕�ޗ�
            currentUserData.AddItem(1, 50, "support"); // �K�^��x5

            // �V�����⏕�ޗ�
            currentUserData.AddItem(2, 30, "support"); // �����ϋv�ی�`�P�b�gx3
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

// �����A�C�e�����N���X�iUI�\���p�j
[System.Serializable]
public class EnhancementItemInfo
{
    public EnhancementItemData itemData;
    public int quantity;
    public bool canUse;
    public string restrictionReason;
}