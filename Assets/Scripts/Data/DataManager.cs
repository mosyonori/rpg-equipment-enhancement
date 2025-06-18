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

    #region 属性制限システム

    /// <summary>
    /// 属性制限を考慮した強化実行
    /// </summary>
    public bool EnhanceEquipmentWithElementalCheck(int equipmentIndex, int enhancementItemId, int supportItemId = -1)
    {
        var userEquipment = GetUserEquipment(equipmentIndex);
        var enhancementItem = GetEnhancementItemData(enhancementItemId);

        if (userEquipment == null || enhancementItem == null)
        {
            Debug.LogError("装備または強化アイテムが見つかりません");
            return false;
        }

        // 属性制限チェック
        if (!enhancementItem.CanUseOnEquipment(userEquipment))
        {
            string reason = enhancementItem.GetRestrictionReason(userEquipment);
            Debug.LogWarning($"属性制限により強化できません: {reason}");
            return false;
        }

        // 既存の強化処理を実行
        return EnhanceEquipment(equipmentIndex, enhancementItemId, supportItemId);
    }

    /// <summary>
    /// 使用可能な強化アイテムのリストを取得（属性制限考慮）
    /// </summary>
    public List<EnhancementItemData> GetAvailableEnhancementItems(int equipmentIndex)
    {
        var userEquipment = GetUserEquipment(equipmentIndex);
        if (userEquipment == null) return new List<EnhancementItemData>();

        var availableItems = new List<EnhancementItemData>();

        foreach (var itemData in enhancementItemDatabase)
        {
            // 所持数チェック
            int quantity = GetItemQuantity(itemData.itemId);
            if (quantity <= 0) continue;

            // 属性制限チェック
            if (!itemData.CanUseOnEquipment(userEquipment)) continue;

            availableItems.Add(itemData);
        }

        return availableItems;
    }

    /// <summary>
    /// 属性制限情報を含む強化アイテム情報を取得
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
    /// 装備の属性情報を取得（デバッグ用）
    /// </summary>
    public string GetEquipmentElementalInfo(int equipmentIndex)
    {
        var userEquipment = GetUserEquipment(equipmentIndex);
        if (userEquipment == null) return "装備が見つかりません";

        ElementalType currentType = userEquipment.GetCurrentElementalType();
        string typeName = UserEquipment.GetElementalTypeName(currentType);

        return $"現在の属性: {typeName}属性\n" +
               $"火攻撃: {userEquipment.GetTotalFireAttack()}\n" +
               $"水攻撃: {userEquipment.GetTotalWaterAttack()}\n" +
               $"風攻撃: {userEquipment.GetTotalWindAttack()}\n" +
               $"土攻撃: {userEquipment.GetTotalEarthAttack()}";
    }

    #endregion

    #region ユーザーデータ操作（既存メソッド + 属性対応強化）

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
    /// 既存の強化メソッド（属性制限なし、後方互換性のため保護）
    /// </summary>
    public bool EnhanceEquipment(int equipmentIndex, int enhancementItemId, int supportItemId = -1)
    {
        var userEquipment = GetUserEquipment(equipmentIndex);
        if (userEquipment == null || !userEquipment.CanEnhance())
        {
            Debug.LogError("装備を強化できません");
            return false;
        }

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

        // 補助アイテムの効果適用
        if (supportItem != null)
        {
            // 成功率修正
            successRate += supportItem.successRateModifier;

            // 成功保証効果
            if (supportItem.guaranteeSuccess)
            {
                successRate = 1.0f;
            }
        }

        successRate = Mathf.Clamp01(successRate);

        // 強化判定
        bool isSuccess = UnityEngine.Random.Range(0f, 1f) <= successRate;
        bool isGreatSuccess = isSuccess && UnityEngine.Random.Range(0f, 1f) <= 0.1f;

        // ★重要: アイテムは強化実行と同時に必ず消費する
        ConsumeItem(enhancementItemId, 1);
        if (supportItem != null)
        {
            ConsumeItem(supportItemId, 1);
        }

        // 耐久度減少の計算（装備種類を考慮）
        var equipmentMasterData = GetEquipmentData(userEquipment.equipmentId);
        EquipmentType currentEquipmentType = equipmentMasterData?.equipmentType ?? EquipmentType.Weapon;
        int baseDurabilityReduction = enhancementItem.GetDurabilityReduction(currentEquipmentType);
        int finalDurabilityReduction = baseDurabilityReduction;

        // ★修正点：補助アイテムによる耐久度修正を適用
        if (supportItem != null)
        {
            // 新しいシステム: 成功/失敗に応じた耐久度計算
            finalDurabilityReduction = supportItem.CalculateDurabilityReduction(isSuccess, baseDurabilityReduction);
            Debug.Log($"補助アイテム効果適用: 基本減少{baseDurabilityReduction} → 最終減少{finalDurabilityReduction}");
        }

        // 耐久度減少は0未満にならないように
        finalDurabilityReduction = Mathf.Max(0, finalDurabilityReduction);

        // ★修正: 強化値の伸びを強化アイテムから取得
        int enhancementValueIncrease = enhancementItem.GetEnhancementValue(currentEquipmentType);

        // 結果処理
        if (isGreatSuccess)
        {
            ApplyEnhancementBonus(userEquipment, enhancementItem, 2.0f);
            // ★修正: 大成功時は強化値も2倍追加
            int greatSuccessEnhancementIncrease = enhancementValueIncrease * 2;
            userEquipment.enhancementLevel += greatSuccessEnhancementIncrease;
            Debug.Log($"大成功！ 強化値追加: +{greatSuccessEnhancementIncrease}（通常{enhancementValueIncrease}の2倍）, 現在の強化レベル: +{userEquipment.enhancementLevel}");
        }
        else if (isSuccess)
        {
            ApplyEnhancementBonus(userEquipment, enhancementItem, 1.0f);
            userEquipment.enhancementLevel += enhancementValueIncrease;
            userEquipment.ReduceDurability(finalDurabilityReduction);
            Debug.Log($"成功！ 強化値追加: +{enhancementValueIncrease}, 現在の強化レベル: +{userEquipment.enhancementLevel}, 耐久減少: {finalDurabilityReduction}");
        }
        else
        {
            // 失敗後の耐久度処理
            userEquipment.ReduceDurability(finalDurabilityReduction);
            Debug.Log($"失敗... 耐久減少: {finalDurabilityReduction}");
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
    /// 修正版: 装備種類を考慮した属性攻撃ボーナス適用
    /// </summary>
    private void ApplyEnhancementBonus(UserEquipment userEquipment, EnhancementItemData enhancementItem, float multiplier)
    {
        // 装備のマスターデータを取得して装備種類を確認
        var equipmentData = GetEquipmentData(userEquipment.equipmentId);
        EquipmentType equipmentType = equipmentData?.equipmentType ?? EquipmentType.Weapon;

        // 装備種類に応じたボーナス値を取得
        userEquipment.bonusAttackPower += Mathf.RoundToInt(enhancementItem.GetAttackPowerBonus(equipmentType) * multiplier);
        userEquipment.bonusDefensePower += Mathf.RoundToInt(enhancementItem.GetDefensePowerBonus(equipmentType) * multiplier);
        userEquipment.bonusElementalAttack += Mathf.RoundToInt(enhancementItem.GetElementalAttackBonus(equipmentType) * multiplier);
        userEquipment.bonusHP += Mathf.RoundToInt(enhancementItem.GetHPBonus(equipmentType) * multiplier);
        userEquipment.bonusCriticalRate += enhancementItem.GetCriticalRateBonus(equipmentType) * multiplier;

        // 重要: 4種類の属性攻撃ボーナス適用（装備種類考慮）
        userEquipment.bonusFireAttack += Mathf.RoundToInt(enhancementItem.GetFireAttackBonus(equipmentType) * multiplier);
        userEquipment.bonusWaterAttack += Mathf.RoundToInt(enhancementItem.GetWaterAttackBonus(equipmentType) * multiplier);
        userEquipment.bonusWindAttack += Mathf.RoundToInt(enhancementItem.GetWindAttackBonus(equipmentType) * multiplier);
        userEquipment.bonusEarthAttack += Mathf.RoundToInt(enhancementItem.GetEarthAttackBonus(equipmentType) * multiplier);

        // デバッグログ追加（装備種類情報も含む）
        Debug.Log($"強化ボーナス適用[{GetEquipmentTypeName(equipmentType)}]: " +
                  $"攻撃+{Mathf.RoundToInt(enhancementItem.GetAttackPowerBonus(equipmentType) * multiplier)}, " +
                  $"防御+{Mathf.RoundToInt(enhancementItem.GetDefensePowerBonus(equipmentType) * multiplier)}, " +
                  $"火+{Mathf.RoundToInt(enhancementItem.GetFireAttackBonus(equipmentType) * multiplier)}, " +
                  $"水+{Mathf.RoundToInt(enhancementItem.GetWaterAttackBonus(equipmentType) * multiplier)}, " +
                  $"風+{Mathf.RoundToInt(enhancementItem.GetWindAttackBonus(equipmentType) * multiplier)}, " +
                  $"土+{Mathf.RoundToInt(enhancementItem.GetEarthAttackBonus(equipmentType) * multiplier)}");
    }

    /// <summary>
    /// 装備種類の日本語名を取得
    /// </summary>
    private string GetEquipmentTypeName(EquipmentType type)
    {
        switch (type)
        {
            case EquipmentType.Weapon: return "武器";
            case EquipmentType.Armor: return "防具";
            case EquipmentType.Accessory: return "アクセサリー";
            default: return "不明";
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

            // ★追加: ID 3, 4の新装備データを追加
            // 装備ID 3のテストデータ
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
                Debug.Log($"テストデータに装備ID 3を追加: {equipment3Data.equipmentName}");
            }

            // 装備ID 4のテストデータ
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
                Debug.Log($"テストデータに装備ID 4を追加: {equipment4Data.equipmentName}");
            }

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
            }
        }

        // 初期アイテム追加（テスト用）
        if (useTemporaryDataForTesting || currentUserData.enhancementItems.Count == 0)
        {
            currentUserData.enhancementItems.Clear();
            currentUserData.supportMaterials.Clear();

            // 既存の強化アイテム
            AddItem(1, 15); // 基本強化石x15
            AddItem(2, 80);  // 高級強化石x8
            AddItem(3, 80);  // 高級強化石x8

            // 新しい属性強化アイテム
            AddItem(4, 50);  // 火のルビーx5
            AddItem(5, 50);  // 水のアクアマリンx5
            AddItem(6, 50);  // 風のオパールx5
            AddItem(7, 50);  // 土のトパーズx5

            // 既存の補助材料
            currentUserData.AddItem(1, 50, "support"); // 幸運石x5

            // 新しい補助材料
            currentUserData.AddItem(2, 30, "support"); // 強化耐久保護チケットx3
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

// 強化アイテム情報クラス（UI表示用）
[System.Serializable]
public class EnhancementItemInfo
{
    public EnhancementItemData itemData;
    public int quantity;
    public bool canUse;
    public string restrictionReason;
}