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
            Debug.Log("�e�X�g���[�h���̂��߁A�f�[�^��ۑ����܂���");
            return;
        }

        try
        {
            string jsonData = JsonUtility.ToJson(currentUserData, true);
            string encryptedData = EncryptString(jsonData);
            PlayerPrefs.SetString(SAVE_KEY, encryptedData);
            PlayerPrefs.Save();

            Debug.Log("���[�U�[�f�[�^��ۑ����܂���");
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
            Debug.Log("�e�X�g���[�h: �V�K�f�[�^���쐬���܂�");
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

                Debug.Log("���[�U�[�f�[�^��ǂݍ��݂܂���");
            }
            else
            {
                currentUserData = new UserData();
                SaveUserData();
                Debug.Log("�V�K���[�U�[�f�[�^���쐬���܂���");
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

    // �e�X�g�p: �f�[�^���Z�b�g�@�\
    [ContextMenu("Reset User Data for Testing")]
    public void ResetUserDataForTesting()
    {
        Debug.Log("�e�X�g�p�f�[�^���Z�b�g���s");
        currentUserData = new UserData();
        ValidateUserData();
        Debug.Log("�f�[�^���Z�b�g����");
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
        Debug.Log("���[�U�[�f�[�^�����Z�b�g���܂���");
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

    #region ���[�U�[�f�[�^����

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
        if (userEquipment == null || !userEquipment.CanEnhance())
        {
            Debug.LogError("�����������ł��܂���");
            return false;
        }

        Debug.Log($"=== �����J�n ===");
        Debug.Log($"�����C���f�b�N�X: {equipmentIndex}");
        Debug.Log($"����ID: {userEquipment.equipmentId}");
        Debug.Log($"�����O�̉Α����{�[�i�X: {userEquipment.bonusFireAttack}");
        Debug.Log($"�����I�u�W�F�N�g�n�b�V��: {userEquipment.GetHashCode()}");

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

        if (supportItem != null && supportItem.materialType == "lucky_stone")
        {
            successRate += supportItem.successRateModifier;
        }

        successRate = Mathf.Clamp01(successRate);

        // ��������
        bool isSuccess = UnityEngine.Random.Range(0f, 1f) <= successRate;
        bool isGreatSuccess = isSuccess && UnityEngine.Random.Range(0f, 1f) <= 0.1f;

        // �A�C�e������
        ConsumeItem(enhancementItemId, 1);
        if (supportItem != null)
        {
            ConsumeItem(supportItemId, 1);
        }

        // �f�o�b�O: �����A�C�e���̑����{�[�i�X�m�F
        Debug.Log($"=== �����A�C�e���ڍ�: {enhancementItem.itemName} ===");
        Debug.Log($"  �U���̓{�[�i�X: {enhancementItem.bonus.attackPower}");
        Debug.Log($"  �Α����{�[�i�X: {enhancementItem.bonus.fireAttack}");
        Debug.Log($"  �������{�[�i�X: {enhancementItem.bonus.waterAttack}");
        Debug.Log($"  �������{�[�i�X: {enhancementItem.bonus.windAttack}");
        Debug.Log($"  �y�����{�[�i�X: {enhancementItem.bonus.earthAttack}");
        Debug.Log($"========================================");

        // ���ʏ���
        if (isGreatSuccess)
        {
            ApplyEnhancementBonus(userEquipment, enhancementItem, 2.0f);
            userEquipment.enhancementLevel++;
            Debug.Log("�听���I");
        }
        else if (isSuccess)
        {
            ApplyEnhancementBonus(userEquipment, enhancementItem, 1.0f);
            userEquipment.enhancementLevel++;

            bool hasProtection = supportItem != null && supportItem.materialType == "protection";
            if (!hasProtection)
            {
                userEquipment.ReduceDurability(enhancementItem.GetDurabilityReduction());
            }
            Debug.Log("�����I");
        }
        else
        {
            bool hasProtection = supportItem != null && supportItem.materialType == "protection";
            if (!hasProtection)
            {
                userEquipment.ReduceDurability(enhancementItem.GetDurabilityReduction());
            }
            Debug.Log("���s...");
        }

        currentUserData.totalEnhancementAttempts++;
        if (isSuccess)
        {
            currentUserData.successfulEnhancements++;
        }

        if (!useTemporaryDataForTesting) SaveUserData();

        return isSuccess;
    }

    // �� �C����: �����U���{�[�i�X�K�p��ǉ�
    private void ApplyEnhancementBonus(UserEquipment userEquipment, EnhancementItemData enhancementItem, float multiplier)
    {
        // �����̊�{�X�e�[�^�X����
        userEquipment.bonusAttackPower += Mathf.RoundToInt(enhancementItem.GetAttackPowerBonus() * multiplier);
        userEquipment.bonusDefensePower += Mathf.RoundToInt(enhancementItem.GetDefensePowerBonus() * multiplier);
        userEquipment.bonusElementalAttack += Mathf.RoundToInt(enhancementItem.GetElementalAttackBonus() * multiplier);
        userEquipment.bonusHP += Mathf.RoundToInt(enhancementItem.GetHPBonus() * multiplier);
        userEquipment.bonusCriticalRate += enhancementItem.GetCriticalRateBonus() * multiplier;

        // �� �d�v: 4��ނ̑����U���{�[�i�X�K�p��ǉ�
        userEquipment.bonusFireAttack += Mathf.RoundToInt(enhancementItem.GetFireAttackBonus() * multiplier);
        userEquipment.bonusWaterAttack += Mathf.RoundToInt(enhancementItem.GetWaterAttackBonus() * multiplier);
        userEquipment.bonusWindAttack += Mathf.RoundToInt(enhancementItem.GetWindAttackBonus() * multiplier);
        userEquipment.bonusEarthAttack += Mathf.RoundToInt(enhancementItem.GetEarthAttackBonus() * multiplier);

        // �� �f�o�b�O���O�ǉ�
        Debug.Log($"=== �����{�[�i�X�K�p���� ===");
        Debug.Log($"�K�p�{��: {multiplier}");
        Debug.Log($"Fire Attack �{�[�i�X�K�p: +{Mathf.RoundToInt(enhancementItem.GetFireAttackBonus() * multiplier)}");
        Debug.Log($"�K�p��� Fire Attack �{�[�i�X���v: {userEquipment.bonusFireAttack}");
        Debug.Log($"Water Attack �{�[�i�X�K�p: +{Mathf.RoundToInt(enhancementItem.GetWaterAttackBonus() * multiplier)}");
        Debug.Log($"Wind Attack �{�[�i�X�K�p: +{Mathf.RoundToInt(enhancementItem.GetWindAttackBonus() * multiplier)}");
        Debug.Log($"Earth Attack �{�[�i�X�K�p: +{Mathf.RoundToInt(enhancementItem.GetEarthAttackBonus() * multiplier)}");
        Debug.Log($"================================");
    }

    public void AddEquipment(int equipmentId)
    {
        currentUserData.AddEquipment(equipmentId);
        if (!useTemporaryDataForTesting) SaveUserData();
        Debug.Log($"�����ǉ�: ID={equipmentId}");
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
                Debug.Log($"�΂̃_�K�[�ǉ�: �ϋv�l = {fireKnife.currentDurability}, �����Α����U�� = {fireKnife.bonusFireAttack}");
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

                Debug.Log($"���������쐬: �ϋv�l = {starterWeapon.currentDurability}");
            }
        }

        // �����A�C�e���ǉ��i�e�X�g�p�j
        if (useTemporaryDataForTesting || currentUserData.enhancementItems.Count == 0)
        {
            currentUserData.enhancementItems.Clear();
            currentUserData.supportMaterials.Clear();

            // �����̋����A�C�e��
            AddItem(1, 15); // ��{������x15
            AddItem(2, 8);  // �㋉������x8

            // �V�������������A�C�e��
            AddItem(3, 5);  // �΂̃��r�[x5
            AddItem(4, 5);  // ���̃A�N�A�}����x5
            AddItem(5, 5);  // ���̃I�p�[��x5
            AddItem(6, 5);  // �y�̃g�p�[�Yx5

            // �����̕⏕�ޗ�
            currentUserData.AddItem(1, 5, "support"); // �K�^��x5

            // �V�����⏕�ޗ�
            currentUserData.AddItem(2, 3, "support"); // �����ϋv�ی�`�P�b�gx3

            Debug.Log("�L�x�ȏ����A�C�e����ǉ����܂���");
            Debug.Log($"�����A�C�e����: {currentUserData.enhancementItems.Count}");
            Debug.Log($"�⏕�ޗ���: {currentUserData.supportMaterials.Count}");
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