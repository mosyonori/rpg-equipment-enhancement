using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

/// <summary>
/// 装備強化関連のデータ取得・保存専用サービス（Phase 1: 基本機能のみ）
/// - 修正時：データアクセスの問題はここだけチェック
/// - 役割：DataManagerとの橋渡し、基本的なデータ取得
/// </summary>
public class EnhanceDataService



{/// <summary>
 /// 装備アイコン取得（マスターデータ優先、フォールバック付き）
 /// </summary>
    public Sprite GetEquipmentIcon(int equipmentId)
    {
        try
        {
            // 1. マスターデータからパス取得
            EquipmentMasterData masterData = GetEquipmentMaster(equipmentId);
            if (masterData != null && !string.IsNullOrEmpty(masterData.equipment_icon_path))
            {
                Sprite sprite = Resources.Load<Sprite>(masterData.equipment_icon_path);
                if (sprite != null)
                {
                    Debug.Log($"✅ 装備アイコン読み込み成功(マスターデータ): {masterData.equipment_icon_path}");
                    return sprite;
                }
            }

            // 2. フォールバック: IDベース読み込み
            string fallbackPath = $"Icons/Equipments/equipment_{equipmentId:D3}";
            Sprite fallbackSprite = Resources.Load<Sprite>(fallbackPath);
            if (fallbackSprite != null)
            {
                Debug.Log($"✅ 装備アイコン読み込み成功(フォールバック): {fallbackPath}");
                return fallbackSprite;
            }

            Debug.LogWarning($"⚠️ 装備アイコン読み込み失敗: equipment_id={equipmentId}");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 装備アイコン読み込みエラー: equipment_id={equipmentId}, {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 強化アイテムアイコン取得
    /// </summary>
    public Sprite GetEnhanceItemIcon(int enhanceItemId)
    {
        try
        {
            // 1. マスターデータからパス取得
            EnhanceItemMasterData masterData = GetEnhanceItemMaster(enhanceItemId);
            if (masterData != null && !string.IsNullOrEmpty(masterData.enhance_item_icon_path))
            {
                Sprite sprite = Resources.Load<Sprite>(masterData.enhance_item_icon_path);
                if (sprite != null)
                {
                    Debug.Log($"✅ 強化アイテムアイコン読み込み成功(マスターデータ): {masterData.enhance_item_icon_path}");
                    return sprite;
                }
            }

            // 2. フォールバック: IDベース読み込み
            string fallbackPath = $"Icons/EnhanceItems/enhance_item_{enhanceItemId:D3}";
            Sprite fallbackSprite = Resources.Load<Sprite>(fallbackPath);
            if (fallbackSprite != null)
            {
                Debug.Log($"✅ 強化アイテムアイコン読み込み成功(フォールバック): {fallbackPath}");
                return fallbackSprite;
            }

            Debug.LogWarning($"⚠️ 強化アイテムアイコン読み込み失敗: enhance_item_id={enhanceItemId}");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 強化アイテムアイコン読み込みエラー: enhance_item_id={enhanceItemId}, {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 補助材料アイコン取得
    /// </summary>
    public Sprite GetSupportItemIcon(int supportItemId)
    {
        try
        {
            // 1. マスターデータからパス取得
            SupportItemMasterData masterData = GetSupportItemMaster(supportItemId);
            if (masterData != null && !string.IsNullOrEmpty(masterData.support_item_icon_path))
            {
                Sprite sprite = Resources.Load<Sprite>(masterData.support_item_icon_path);
                if (sprite != null)
                {
                    Debug.Log($"✅ 補助材料アイコン読み込み成功(マスターデータ): {masterData.support_item_icon_path}");
                    return sprite;
                }
            }

            // 2. フォールバック: IDベース読み込み
            string fallbackPath = $"Icons/SupportItems/support_item_{supportItemId:D3}";
            Sprite fallbackSprite = Resources.Load<Sprite>(fallbackPath);
            if (fallbackSprite != null)
            {
                Debug.Log($"✅ 補助材料アイコン読み込み成功(フォールバック): {fallbackPath}");
                return fallbackSprite;
            }

            Debug.LogWarning($"⚠️ 補助材料アイコン読み込み失敗: support_item_id={supportItemId}");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 補助材料アイコン読み込みエラー: support_item_id={supportItemId}, {e.Message}");
            return null;
        }
    }
    /// <summary>
    /// 所持している装備一覧を取得
    /// </summary>
    /// 

    public List<UserEquipment> GetOwnedEquipments()
    {
        try
        {
            List<UserEquipment> allEquipments = DataManager.Instance.GetUserEquipments();

            // 所持している装備のみを返す（将来的な拡張用）
            return allEquipments.Where(equipment => equipment != null).ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"装備データ取得エラー: {ex.Message}");
            return new List<UserEquipment>(); // 空のリストを返す
        }
    }

    /// <summary>
    /// 所持している強化アイテム一覧を取得
    /// </summary>
    public List<UserItem> GetOwnedEnhanceItems()
    {
        try
        {
            List<UserItem> allItems = DataManager.Instance.GetUserItems();

            // 強化アイテムで、かつ所持数が1以上のもののみ
            return allItems.Where(item =>
                item.item_type == ItemType.EnhanceItem &&
                item.quantity > 0
            ).ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"強化アイテムデータ取得エラー: {ex.Message}");
            return new List<UserItem>(); // 空のリストを返す
        }
    }

    /// <summary>
    /// 指定したユニークIDの装備データを取得
    /// </summary>
    public UserEquipment GetUserEquipment(string uniqueId)
    {
        try
        {
            List<UserEquipment> allEquipments = GetOwnedEquipments();

            UserEquipment equipment = allEquipments.FirstOrDefault(eq => eq.unique_id == uniqueId);

            if (equipment == null)
            {
                Debug.LogWarning($"装備が見つかりません: {uniqueId}");
            }

            return equipment;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"装備データ取得エラー (ID: {uniqueId}): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 装備マスターデータを取得
    /// </summary>
    public EquipmentMasterData GetEquipmentMaster(int equipmentId)
    {
        try
        {
            EquipmentMasterData masterData = DataManager.Instance.GetEquipmentMasterData(equipmentId);

            if (masterData == null)
            {
                Debug.LogWarning($"装備マスターデータが見つかりません: {equipmentId}");
            }

            return masterData;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"装備マスターデータ取得エラー (ID: {equipmentId}): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 強化アイテムマスターデータを取得
    /// </summary>
    public EnhanceItemMasterData GetEnhanceItemMaster(int enhanceItemId)
    {
        try
        {
            EnhanceItemMasterData masterData = DataManager.Instance.GetEnhanceItemMasterData(enhanceItemId);

            if (masterData == null)
            {
                Debug.LogWarning($"強化アイテムマスターデータが見つかりません: {enhanceItemId}");
            }

            return masterData;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"強化アイテムマスターデータ取得エラー (ID: {enhanceItemId}): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 補助材料マスターデータを取得
    /// </summary>
    public SupportItemMasterData GetSupportItemMaster(int supportItemId)
    {
        try
        {
            SupportItemMasterData masterData = DataManager.Instance.GetSupportItemMasterData(supportItemId);

            if (masterData == null)
            {
                Debug.LogWarning($"補助材料マスターデータが見つかりません: {supportItemId}");
            }

            return masterData;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"補助材料マスターデータ取得エラー (ID: {supportItemId}): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 装備種類に応じた強化アイテムの効果を取得
    /// </summary>
    public EnhanceStatusValues GetEnhanceEffectByEquipmentType(int enhanceItemId, EquipmentType equipmentType)
    {
        try
        {
            EnhanceItemMasterData enhanceItem = GetEnhanceItemMaster(enhanceItemId);

            if (enhanceItem == null)
            {
                Debug.LogWarning($"強化アイテムが見つかりません: {enhanceItemId}");
                return new EnhanceStatusValues();
            }

            return enhanceItem.GetStatusValuesByEquipmentType(equipmentType);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"装備種類別強化効果取得エラー: {ex.Message}");
            return new EnhanceStatusValues();
        }
    }

    /// <summary>
    /// 所持している補助材料一覧を取得
    /// </summary>
    public List<UserItem> GetOwnedSupportItems()
    {
        try
        {
            List<UserItem> allItems = DataManager.Instance.GetUserItems();

            // 補助材料で、かつ所持数が1以上のもののみ
            return allItems.Where(item =>
                item.item_type == ItemType.SupportItem &&
                item.quantity > 0
            ).ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"補助材料データ取得エラー: {ex.Message}");
            return new List<UserItem>(); // 空のリストを返す
        }
    }

    /// <summary>
    /// 補助材料を消費
    /// </summary>
    public bool ConsumeSupportItem(int supportItemId, int quantity)
    {
        try
        {
            if (quantity <= 0)
            {
                Debug.LogWarning("消費量は1以上である必要があります");
                return false;
            }

            // 無限使用アイテムの場合は消費しない
            SupportItemMasterData masterData = GetSupportItemMaster(supportItemId);
            if (masterData != null && masterData.infinite_use == 1)
            {
                Debug.Log($"無限使用アイテムのため消費しません: {masterData.support_item_name}");
                return true;
            }

            // 所持確認
            UserItem item = GetOwnedSupportItems().FirstOrDefault(i => i.item_id == supportItemId);
            if (item == null || item.quantity < quantity)
            {
                Debug.LogWarning($"補助材料が不足しています: ID={supportItemId}, 必要={quantity}, 所持={item?.quantity ?? 0}");
                return false;
            }

            // アイテム消費
            DataManager.Instance.ConsumeUserItem(supportItemId, quantity);
            Debug.Log($"補助材料を消費しました: ID={supportItemId}, 消費量={quantity}");

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"補助材料消費エラー: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 強化アイテムを消費
    /// </summary>
    public bool ConsumeEnhanceItem(int enhanceItemId, int quantity)
    {
        try
        {
            if (quantity <= 0)
            {
                Debug.LogWarning("消費量は1以上である必要があります");
                return false;
            }

            // 所持確認
            UserItem item = GetOwnedEnhanceItems().FirstOrDefault(i => i.item_id == enhanceItemId);
            if (item == null || item.quantity < quantity)
            {
                Debug.LogWarning($"強化アイテムが不足しています: ID={enhanceItemId}, 必要={quantity}, 所持={item?.quantity ?? 0}");
                return false;
            }

            // アイテム消費
            DataManager.Instance.ConsumeUserItem(enhanceItemId, quantity);
            Debug.Log($"強化アイテムを消費しました: ID={enhanceItemId}, 消費量={quantity}");

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"強化アイテム消費エラー: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 装備が強化可能かチェック
    /// </summary>
    public bool CanEnhanceEquipment(UserEquipment equipment)
    {
        try
        {
            if (equipment == null)
            {
                return false;
            }

            // 強化耐久が0以下の場合は強化不可
            if (equipment.current_enhance_stamina <= 0)
            {
                Debug.Log($"装備の強化耐久が不足しています: {equipment.unique_id}");
                return false;
            }

            // 最大強化値に達している場合は強化不可
            EquipmentMasterData masterData = GetEquipmentMaster(equipment.equipment_id);
            if (masterData != null && equipment.current_enhanced_value >= masterData.max_enhanced_value)
            {
                Debug.Log($"装備が最大強化値に達しています: {equipment.unique_id}");
                return false;
            }

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"装備強化可能チェックエラー: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// デバッグ用：データの整合性チェック（CSV対応版）
    /// </summary>
    public bool ValidateData()
    {
        try
        {
            Debug.Log("=== EnhanceDataService データ整合性チェック（CSV対応版） ===");

            // 1. 装備データチェック
            List<UserEquipment> equipments = GetOwnedEquipments();
            Debug.Log($"所持装備数: {equipments.Count}");

            foreach (var equipment in equipments.Take(3)) // 最初の3つだけ表示
            {
                EquipmentMasterData master = GetEquipmentMaster(equipment.equipment_id);
                if (master != null)
                {
                    Debug.Log($"装備: {master.equipment_name} (種類: {master.equipment_type}, 強化値: {equipment.current_enhanced_value})");
                }
            }

            // 2. 強化アイテムデータチェック
            List<UserItem> enhanceItems = GetOwnedEnhanceItems();
            Debug.Log($"所持強化アイテム種類数: {enhanceItems.Count}");

            foreach (var item in enhanceItems.Take(3)) // 最初の3つだけ表示
            {
                EnhanceItemMasterData master = GetEnhanceItemMaster(item.item_id);
                if (master != null)
                {
                    Debug.Log($"強化アイテム: {master.enhance_item_name} (所持数: {item.quantity})");

                    // 装備種類別効果のテスト表示
                    Debug.Log($"  武器への効果 - HP: +{master.weapon_hp}, 攻撃: +{master.weapon_offense}");
                    Debug.Log($"  防具への効果 - HP: +{master.armor_hp}, 防御: +{master.armor_defense}");
                    Debug.Log($"  アクセサリへの効果 - HP: +{master.accessory_hp}, 攻撃: +{master.accessory_offense}");
                }
            }

            // 3. 補助材料データチェック
            List<UserItem> supportItems = GetOwnedSupportItems();
            Debug.Log($"所持補助材料種類数: {supportItems.Count}");

            foreach (var item in supportItems.Take(3)) // 最初の3つだけ表示
            {
                SupportItemMasterData master = GetSupportItemMaster(item.item_id);
                if (master != null)
                {
                    string infiniteText = master.infinite_use == 1 ? " (無限使用)" : "";
                    Debug.Log($"補助材料: {master.support_item_name}{infiniteText} (所持数: {item.quantity})");
                    Debug.Log($"  効果 - 成功率: +{master.add_enhance_success_rate}%, ステータス倍率: x{master.multipl_status_up}");
                }
            }

            // 4. 装備種類別効果テスト
            Debug.Log("=== 装備種類別効果テスト ===");
            if (enhanceItems.Count > 0)
            {
                int testEnhanceItemId = enhanceItems[0].item_id;

                EnhanceStatusValues weaponEffect = GetEnhanceEffectByEquipmentType(testEnhanceItemId, EquipmentType.Weapon);
                EnhanceStatusValues armorEffect = GetEnhanceEffectByEquipmentType(testEnhanceItemId, EquipmentType.Armor);
                EnhanceStatusValues accessoryEffect = GetEnhanceEffectByEquipmentType(testEnhanceItemId, EquipmentType.Accessory);

                Debug.Log($"テストアイテムID {testEnhanceItemId} の効果:");
                Debug.Log($"  武器: HP+{weaponEffect.hp}, 攻撃+{weaponEffect.offense}, 防御+{weaponEffect.defense}");
                Debug.Log($"  防具: HP+{armorEffect.hp}, 攻撃+{armorEffect.offense}, 防御+{armorEffect.defense}");
                Debug.Log($"  アクセサリ: HP+{accessoryEffect.hp}, 攻撃+{accessoryEffect.offense}, 防御+{accessoryEffect.defense}");
            }

            Debug.Log("=== データ整合性チェック完了 ===");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"データ整合性チェックエラー: {ex.Message}");
            return false;
        }
    }
}

// ===== データクラス定義（Phase 1で必要な基本クラス） =====

/// <summary>
/// アイテムタイプ列挙型
/// </summary>
public enum ItemType
{
    EnhanceItem,    // 強化アイテム
    SupportItem,    // 補助材料
    Equipment,      // 装備
    Consumable      // 消費アイテム
}

/// <summary>
/// 装備タイプ列挙型
/// </summary>
public enum EquipmentType
{
    Weapon,     // 武器
    Armor,      // 防具
    Accessory   // アクセサリ
}

/// <summary>
/// ユーザー装備データ（セーブデータ）
/// </summary>
[System.Serializable]
public class UserEquipment
{
    public string unique_id;                    // ユニークID
    public int equipment_id;                    // 装備マスターID
    public int current_enhanced_value;          // 現在の強化値
    public int current_enhance_stamina;         // 現在の強化耐久
    public bool is_equipped;                    // 装備中フラグ
    public System.DateTime acquired_time;       // 取得日時

    // 現在のステータス（強化により変動）
    public int hp;
    public int offense;
    public int defense;
    public int speed;
    public int critical_rate;
    public int critical_damage_rate;
    public int fire_offence;
    public int water_offence;
    public int wind_offence;
    public int earth_offence;
}

/// <summary>
/// ユーザーアイテムデータ（セーブデータ）
/// </summary>
[System.Serializable]
public class UserItem
{
    public int item_id;         // アイテムマスターID
    public ItemType item_type;  // アイテムタイプ
    public int quantity;        // 所持数
}

/// <summary>
/// 装備マスターデータ
/// </summary>
[System.Serializable]
public class EquipmentMasterData
{
    public int equipment_id;
    public string equipment_name;
    public EquipmentType equipment_type;
    public string rarity;
    public int base_enhanced_value;
    public int max_enhanced_value;
    public int min_enhanced_value;
    public int base_enhance_stamina;
    public string equipment_icon_path;
    public string description;

    // 基本ステータス
    public int hp;
    public int offense;
    public int defense;
    public int speed;
    public int critical_rate;
    public int critical_damage_rate;
    public int fire_offence;
    public int water_offence;
    public int wind_offence;
    public int earth_offence;
}

/// <summary>
/// 強化アイテムマスターデータ（CSV対応版）
/// </summary>
[System.Serializable]
public class EnhanceItemMasterData
{
    public int enhance_item_id;
    public string enhance_item_name;
    public string attribute_type;
    public string rarity;
    public int max_stack_value;
    public int add_enhanced_value;
    public int reduce_enhanced_value;
    public int add_enhance_stamina;
    public int reduce_enhance_stamina;
    public int enhance_success_rate;
    public string enhance_item_icon_path;
    public string description;
    public int completion_flag;
    public int collection_flag;

    // 武器用ステータス増加値
    public int weapon_hp;
    public int weapon_offense;
    public int weapon_defense;
    public int weapon_speed;
    public int weapon_critical_rate;
    public int weapon_critical_damage_rate;
    public int weapon_fire_offence;
    public int weapon_water_offence;
    public int weapon_wind_offence;
    public int weapon_earth_offence;

    // 防具用ステータス増加値
    public int armor_hp;
    public int armor_offense;
    public int armor_defense;
    public int armor_speed;
    public int armor_critical_rate;
    public int armor_critical_damage_rate;
    public int armor_fire_offence;
    public int armor_water_offence;
    public int armor_wind_offence;
    public int armor_earth_offence;

    // アクセサリ用ステータス増加値
    public int accessory_hp;
    public int accessory_offense;
    public int accessory_defense;
    public int accessory_speed;
    public int accessory_critical_rate;
    public int accessory_critical_damage_rate;
    public int accessory_fire_offence;
    public int accessory_water_offence;
    public int accessory_wind_offence;
    public int accessory_earth_offence;

    /// <summary>
    /// 装備タイプに応じたステータス増加値を取得
    /// </summary>
    public EnhanceStatusValues GetStatusValuesByEquipmentType(EquipmentType equipmentType)
    {
        switch (equipmentType)
        {
            case EquipmentType.Weapon:
                return new EnhanceStatusValues
                {
                    hp = weapon_hp,
                    offense = weapon_offense,
                    defense = weapon_defense,
                    speed = weapon_speed,
                    critical_rate = weapon_critical_rate,
                    critical_damage_rate = weapon_critical_damage_rate,
                    fire_offence = weapon_fire_offence,
                    water_offence = weapon_water_offence,
                    wind_offence = weapon_wind_offence,
                    earth_offence = weapon_earth_offence
                };

            case EquipmentType.Armor:
                return new EnhanceStatusValues
                {
                    hp = armor_hp,
                    offense = armor_offense,
                    defense = armor_defense,
                    speed = armor_speed,
                    critical_rate = armor_critical_rate,
                    critical_damage_rate = armor_critical_damage_rate,
                    fire_offence = armor_fire_offence,
                    water_offence = armor_water_offence,
                    wind_offence = armor_wind_offence,
                    earth_offence = armor_earth_offence
                };

            case EquipmentType.Accessory:
                return new EnhanceStatusValues
                {
                    hp = accessory_hp,
                    offense = accessory_offense,
                    defense = accessory_defense,
                    speed = accessory_speed,
                    critical_rate = accessory_critical_rate,
                    critical_damage_rate = accessory_critical_damage_rate,
                    fire_offence = accessory_fire_offence,
                    water_offence = accessory_water_offence,
                    wind_offence = accessory_wind_offence,
                    earth_offence = accessory_earth_offence
                };

            default:
                Debug.LogWarning($"EnhanceItemMasterData: 未対応の装備タイプです {equipmentType}");
                return new EnhanceStatusValues();
        }
    }
}

/// <summary>
/// 補助材料マスターデータ（CSV対応版）
/// </summary>
[System.Serializable]
public class SupportItemMasterData
{
    public int support_item_id;
    public string support_item_name;
    public string attribute_type;
    public string rarity;
    public int infinite_use;                    // 無限使用フラグ
    public int max_stack_value;
    public int add_enhanced_value;
    public int multipl_enhanced_value;
    public int reduce_enhanced_value;
    public int add_enhance_stamina;
    public int reduce_enhance_stamina;
    public int add_enhance_success_rate;
    public int reduce_enhance_success_rate;
    public int multipl_status_up;
    public string support_item_icon_path;
    public string description;
    public int completion_flag;
    public int collection_flag;

    // 補助材料の直接ステータス効果
    public int hp;
    public int offense;
    public int defense;
    public int speed;
    public int critical_rate;
    public int critical_damage_rate;
    public int fire_offence;
    public int water_offence;
    public int wind_offence;
    public int earth_offence;
}

/// <summary>
/// 強化ステータス値（装備種類別の値を格納）
/// </summary>
[System.Serializable]
public class EnhanceStatusValues
{
    public int hp;
    public int offense;
    public int defense;
    public int speed;
    public int critical_rate;
    public int critical_damage_rate;
    public int fire_offence;
    public int water_offence;
    public int wind_offence;
    public int earth_offence;

    /// <summary>
    /// デバッグ用文字列表現
    /// </summary>
    public override string ToString()
    {
        return $"HP:{hp}, 攻撃:{offense}, 防御:{defense}, 速度:{speed}, " +
               $"クリ率:{critical_rate}, クリダメ:{critical_damage_rate}, " +
               $"火:{fire_offence}, 水:{water_offence}, 風:{wind_offence}, 土:{earth_offence}";
    }
}

/// <summary>
/// ユーザープロフィール
/// </summary>
[System.Serializable]
public class UserProfile
{
    public int userId;
    public int level;
    public int experience;
    public int stamina;
    public int maxStamina;
    public int gold;
    public int gems;
    public System.DateTime lastLoginTime;
    public System.DateTime lastStaminaRecoveryTime;
    public int mainCharacterId;
}

/// <summary>
/// ユーザースキル
/// </summary>
[System.Serializable]
public class UserSkill
{
    public int skill_id;
    public System.DateTime acquired_time;
    public string unlock_source;
}

/// <summary>
/// 音響設定データ
/// </summary>
[System.Serializable]
public class AudioSettingsData
{
    public float bgmVolume = 1.0f;
    public float seVolume = 1.0f;
}

/// <summary>
/// クエストマスターデータ
/// </summary>
[System.Serializable]
public class QuestMasterData
{
    public int questId;
    public string questName;
    public string description;
    public QuestType questType;
    public int needLevel;
    public int requiredClearQuest;
    public int clearLimit;
    public int requiredStamina;
    public int recommendedPower;
}

/// <summary>
/// クエストタイプ
/// </summary>
public enum QuestType
{
    Normal,
    Daily,
    Event
}