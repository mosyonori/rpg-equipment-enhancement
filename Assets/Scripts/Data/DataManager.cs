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

    [Header("現在のユーザーデータ")]
    public UserData currentUserData;

    [Header("デバッグ設定")]
    public bool useTemporaryDataForTesting = true; // テスト用: trueにするとセーブデータを使わない

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

    #region セーブ・ロード

    public void SaveUserData()
    {
        if (useTemporaryDataForTesting)
        {
            Debug.Log("テストモード中のため、データを保存しません");
            return;
        }

        try
        {
            string jsonData = JsonUtility.ToJson(currentUserData, true);
            string encryptedData = EncryptString(jsonData);
            PlayerPrefs.SetString(SAVE_KEY, encryptedData);
            PlayerPrefs.Save();

            Debug.Log("ユーザーデータを保存しました");
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
            Debug.Log("テストモード: 新規データを作成します");
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

                Debug.Log("ユーザーデータを読み込みました");
            }
            else
            {
                currentUserData = new UserData();
                SaveUserData();
                Debug.Log("新規ユーザーデータを作成しました");
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

    // テスト用: データリセット機能
    [ContextMenu("Reset User Data for Testing")]
    public void ResetUserDataForTesting()
    {
        Debug.Log("テスト用データリセット実行");
        currentUserData = new UserData();
        ValidateUserData();
        Debug.Log("データリセット完了");
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
        Debug.Log("ユーザーデータをリセットしました");
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

        Debug.LogWarning($"装備ID {equipmentId} が見つかりません");
        return null;
    }

    public EnhancementItemData GetEnhancementItemData(int itemId)
    {
        foreach (var item in enhancementItemDatabase)
        {
            if (item.itemId == itemId)
                return item;
        }

        Debug.LogWarning($"強化アイテムID {itemId} が見つかりません");
        return null;
    }

    public SupportMaterialData GetSupportMaterialData(int materialId)
    {
        foreach (var material in supportMaterialDatabase)
        {
            if (material.materialId == materialId)
                return material;
        }

        Debug.LogWarning($"補助材料ID {materialId} が見つかりません");
        return null;
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
        if (userEquipment == null || !userEquipment.CanEnhance())
        {
            Debug.LogError("装備を強化できません");
            return false;
        }

        Debug.Log($"=== 強化開始 ===");
        Debug.Log($"装備インデックス: {equipmentIndex}");
        Debug.Log($"装備ID: {userEquipment.equipmentId}");
        Debug.Log($"強化前の火属性ボーナス: {userEquipment.bonusFireAttack}");
        Debug.Log($"装備オブジェクトハッシュ: {userEquipment.GetHashCode()}");

        var enhancementItem = GetEnhancementItemData(enhancementItemId);
        if (enhancementItem == null)
        {
            Debug.LogError("強化アイテムが見つかりません");
            return false;
        }

        if (GetItemQuantity(enhancementItemId) <= 0)
        {
            Debug.LogError("強化アイテムが不足しています");
            return false;
        }

        SupportMaterialData supportItem = null;
        if (supportItemId >= 0)
        {
            supportItem = GetSupportMaterialData(supportItemId);
            if (supportItem != null && GetItemQuantity(supportItemId) <= 0)
            {
                Debug.LogError("補助アイテムが不足しています");
                return false;
            }
        }

        // 成功確率計算
        float successRate = enhancementItem.GetAdjustedSuccessRate(userEquipment.enhancementLevel);

        if (supportItem != null && supportItem.materialType == "lucky_stone")
        {
            successRate += supportItem.successRateModifier;
        }

        successRate = Mathf.Clamp01(successRate);

        // 強化判定
        bool isSuccess = UnityEngine.Random.Range(0f, 1f) <= successRate;
        bool isGreatSuccess = isSuccess && UnityEngine.Random.Range(0f, 1f) <= 0.1f;

        // アイテム消費
        ConsumeItem(enhancementItemId, 1);
        if (supportItem != null)
        {
            ConsumeItem(supportItemId, 1);
        }

        // デバッグ: 強化アイテムの属性ボーナス確認
        Debug.Log($"=== 強化アイテム詳細: {enhancementItem.itemName} ===");
        Debug.Log($"  攻撃力ボーナス: {enhancementItem.bonus.attackPower}");
        Debug.Log($"  火属性ボーナス: {enhancementItem.bonus.fireAttack}");
        Debug.Log($"  水属性ボーナス: {enhancementItem.bonus.waterAttack}");
        Debug.Log($"  風属性ボーナス: {enhancementItem.bonus.windAttack}");
        Debug.Log($"  土属性ボーナス: {enhancementItem.bonus.earthAttack}");
        Debug.Log($"========================================");

        // 結果処理
        if (isGreatSuccess)
        {
            ApplyEnhancementBonus(userEquipment, enhancementItem, 2.0f);
            userEquipment.enhancementLevel++;
            Debug.Log("大成功！");
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
            Debug.Log("成功！");
        }
        else
        {
            bool hasProtection = supportItem != null && supportItem.materialType == "protection";
            if (!hasProtection)
            {
                userEquipment.ReduceDurability(enhancementItem.GetDurabilityReduction());
            }
            Debug.Log("失敗...");
        }

        currentUserData.totalEnhancementAttempts++;
        if (isSuccess)
        {
            currentUserData.successfulEnhancements++;
        }

        if (!useTemporaryDataForTesting) SaveUserData();

        return isSuccess;
    }

    // ★ 修正版: 属性攻撃ボーナス適用を追加
    private void ApplyEnhancementBonus(UserEquipment userEquipment, EnhancementItemData enhancementItem, float multiplier)
    {
        // 既存の基本ステータス強化
        userEquipment.bonusAttackPower += Mathf.RoundToInt(enhancementItem.GetAttackPowerBonus() * multiplier);
        userEquipment.bonusDefensePower += Mathf.RoundToInt(enhancementItem.GetDefensePowerBonus() * multiplier);
        userEquipment.bonusElementalAttack += Mathf.RoundToInt(enhancementItem.GetElementalAttackBonus() * multiplier);
        userEquipment.bonusHP += Mathf.RoundToInt(enhancementItem.GetHPBonus() * multiplier);
        userEquipment.bonusCriticalRate += enhancementItem.GetCriticalRateBonus() * multiplier;

        // ★ 重要: 4種類の属性攻撃ボーナス適用を追加
        userEquipment.bonusFireAttack += Mathf.RoundToInt(enhancementItem.GetFireAttackBonus() * multiplier);
        userEquipment.bonusWaterAttack += Mathf.RoundToInt(enhancementItem.GetWaterAttackBonus() * multiplier);
        userEquipment.bonusWindAttack += Mathf.RoundToInt(enhancementItem.GetWindAttackBonus() * multiplier);
        userEquipment.bonusEarthAttack += Mathf.RoundToInt(enhancementItem.GetEarthAttackBonus() * multiplier);

        // ★ デバッグログ追加
        Debug.Log($"=== 強化ボーナス適用完了 ===");
        Debug.Log($"適用倍率: {multiplier}");
        Debug.Log($"Fire Attack ボーナス適用: +{Mathf.RoundToInt(enhancementItem.GetFireAttackBonus() * multiplier)}");
        Debug.Log($"適用後の Fire Attack ボーナス合計: {userEquipment.bonusFireAttack}");
        Debug.Log($"Water Attack ボーナス適用: +{Mathf.RoundToInt(enhancementItem.GetWaterAttackBonus() * multiplier)}");
        Debug.Log($"Wind Attack ボーナス適用: +{Mathf.RoundToInt(enhancementItem.GetWindAttackBonus() * multiplier)}");
        Debug.Log($"Earth Attack ボーナス適用: +{Mathf.RoundToInt(enhancementItem.GetEarthAttackBonus() * multiplier)}");
        Debug.Log($"================================");
    }

    public void AddEquipment(int equipmentId)
    {
        currentUserData.AddEquipment(equipmentId);
        if (!useTemporaryDataForTesting) SaveUserData();
        Debug.Log($"装備追加: ID={equipmentId}");
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

        // 修正: 初期装備を毎回リセット（テスト用）
        if (useTemporaryDataForTesting || currentUserData.equipments.Count == 0)
        {
            currentUserData.equipments.Clear(); // 既存データをクリア

            // 新装備追加: 火のダガー
            var fireKnifeData = GetEquipmentData(2); // ID 2が火のダガーの場合
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
                Debug.Log($"火のダガー追加: 耐久値 = {fireKnife.currentDurability}, 初期火属性攻撃 = {fireKnife.bonusFireAttack}");
            }

            // 初心者用装備を追加（マスターデータから正しい耐久値を取得）
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

                Debug.Log($"初期装備作成: 耐久値 = {starterWeapon.currentDurability}");
            }
        }

        // 初期アイテム追加（テスト用）
        if (useTemporaryDataForTesting || currentUserData.enhancementItems.Count == 0)
        {
            currentUserData.enhancementItems.Clear();
            currentUserData.supportMaterials.Clear();

            // 既存の強化アイテム
            AddItem(1, 15); // 基本強化石x15
            AddItem(2, 8);  // 上級強化石x8

            // 新しい属性強化アイテム
            AddItem(3, 5);  // 火のルビーx5
            AddItem(4, 5);  // 水のアクアマランx5
            AddItem(5, 5);  // 風のオパールx5
            AddItem(6, 5);  // 土のトパーズx5

            // 既存の補助材料
            currentUserData.AddItem(1, 5, "support"); // 幸運石x5

            // 新しい補助材料
            currentUserData.AddItem(2, 3, "support"); // 強化耐久保護チケットx3

            Debug.Log("豊富な初期アイテムを追加しました");
            Debug.Log($"強化アイテム数: {currentUserData.enhancementItems.Count}");
            Debug.Log($"補助材料数: {currentUserData.supportMaterials.Count}");
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