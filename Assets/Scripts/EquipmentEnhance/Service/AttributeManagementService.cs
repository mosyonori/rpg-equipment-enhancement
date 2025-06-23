using UnityEngine;

/// <summary>
/// 装備の属性管理専用サービス（バランス版）
/// - 責任：属性変更ロジックのみ
/// - 修正時：属性関連の問題はここだけチェック
/// - 属性判定、上書きルール、警告メッセージを管理
/// </summary>
public class AttributeManagementService
{
    /// <summary>
    /// 属性タイプ列挙型
    /// </summary>
    public enum AttributeType
    {
        Normal,  // 無属性
        Fire,    // 火属性
        Water,   // 水属性
        Wind,    // 風属性
        Earth    // 土属性
    }

    /// <summary>
    /// 属性攻撃の上書き処理（メインメソッド）
    /// 仕様：異なる属性で上書き時は他の属性攻撃を0にリセット
    /// </summary>
    public void ApplyAttributeChange(UserEquipment equipment, EnhanceItemMasterData enhanceItem)
    {
        try
        {
            if (equipment == null || enhanceItem == null)
            {
                Debug.LogWarning("AttributeManagementService: 装備または強化アイテムがnullです");
                return;
            }

            AttributeType enhanceItemAttribute = GetEnhanceItemAttribute(enhanceItem);

            if (enhanceItemAttribute == AttributeType.Normal)
            {
                // 無属性強化アイテムの場合は属性変更なし
                Debug.Log("AttributeManagementService: 無属性強化アイテムのため属性変更なし");
                return;
            }

            AttributeType currentEquipmentAttribute = GetEquipmentCurrentAttribute(equipment);

            if (currentEquipmentAttribute == AttributeType.Normal)
            {
                // 無属性装備に属性付与
                ApplyNewAttribute(equipment, enhanceItemAttribute, enhanceItem);
                Debug.Log($"AttributeManagementService: 無属性装備に{enhanceItemAttribute}属性を付与");
            }
            else if (currentEquipmentAttribute != enhanceItemAttribute)
            {
                // 異なる属性で上書き
                OverwriteAttribute(equipment, enhanceItemAttribute, enhanceItem);
                Debug.Log($"AttributeManagementService: {currentEquipmentAttribute}属性を{enhanceItemAttribute}属性で上書き");
            }
            else
            {
                // 同じ属性の場合は上書きせず加算のみ
                Debug.Log($"AttributeManagementService: 同じ{enhanceItemAttribute}属性のため加算のみ");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AttributeManagementService: 属性変更エラー - {ex.Message}");
        }
    }

    /// <summary>
    /// 無属性装備への属性付与
    /// </summary>
    private void ApplyNewAttribute(UserEquipment equipment, AttributeType newAttribute, EnhanceItemMasterData enhanceItem)
    {
        try
        {
            // 既存の属性攻撃をクリア（無属性なので元々0のはず）
            ResetAllAttributeAttacks(equipment);

            // 新しい属性攻撃を設定
            int attributeValue = GetEnhanceItemAttributeValue(enhanceItem, newAttribute);
            SetAttributeAttack(equipment, newAttribute, attributeValue);

            Debug.Log($"AttributeManagementService: 新属性付与 {newAttribute} +{attributeValue}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AttributeManagementService: 新属性付与エラー - {ex.Message}");
        }
    }

    /// <summary>
    /// 既存属性の上書き処理
    /// 仕様：他の属性攻撃を0にリセットし、新しい属性攻撃のみ設定
    /// </summary>
    private void OverwriteAttribute(UserEquipment equipment, AttributeType newAttribute, EnhanceItemMasterData enhanceItem)
    {
        try
        {
            // 全ての属性攻撃をリセット
            ResetAllAttributeAttacks(equipment);

            // 新しい属性攻撃のみ設定
            int attributeValue = GetEnhanceItemAttributeValue(enhanceItem, newAttribute);
            SetAttributeAttack(equipment, newAttribute, attributeValue);

            Debug.Log($"AttributeManagementService: 属性上書き {newAttribute} = {attributeValue}（他の属性攻撃は0にリセット）");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AttributeManagementService: 属性上書きエラー - {ex.Message}");
        }
    }

    /// <summary>
    /// 全ての属性攻撃をリセット
    /// </summary>
    private void ResetAllAttributeAttacks(UserEquipment equipment)
    {
        equipment.fire_offence = 0;
        equipment.water_offence = 0;
        equipment.wind_offence = 0;
        equipment.earth_offence = 0;
    }

    /// <summary>
    /// 指定属性の攻撃力を設定
    /// </summary>
    private void SetAttributeAttack(UserEquipment equipment, AttributeType attribute, int value)
    {
        switch (attribute)
        {
            case AttributeType.Fire:
                equipment.fire_offence = value;
                break;
            case AttributeType.Water:
                equipment.water_offence = value;
                break;
            case AttributeType.Wind:
                equipment.wind_offence = value;
                break;
            case AttributeType.Earth:
                equipment.earth_offence = value;
                break;
            default:
                Debug.LogWarning($"AttributeManagementService: 不明な属性タイプ {attribute}");
                break;
        }
    }

    /// <summary>
    /// 装備の現在の属性を判定
    /// </summary>
    public AttributeType GetEquipmentCurrentAttribute(UserEquipment equipment)
    {
        try
        {
            if (equipment == null)
            {
                return AttributeType.Normal;
            }

            // 属性攻撃が設定されている属性を返す（複数ある場合は最初に見つかったもの）
            if (equipment.fire_offence > 0) return AttributeType.Fire;
            if (equipment.water_offence > 0) return AttributeType.Water;
            if (equipment.wind_offence > 0) return AttributeType.Wind;
            if (equipment.earth_offence > 0) return AttributeType.Earth;

            return AttributeType.Normal;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AttributeManagementService: 装備属性判定エラー - {ex.Message}");
            return AttributeType.Normal;
        }
    }

    /// <summary>
    /// 強化アイテムの属性を判定
    /// </summary>
    public AttributeType GetEnhanceItemAttribute(EnhanceItemMasterData enhanceItem)
    {
        try
        {
            if (enhanceItem == null)
            {
                return AttributeType.Normal;
            }

            // 属性攻撃が設定されている属性を返す
            if (enhanceItem.weapon_fire_offence > 0 || enhanceItem.armor_fire_offence > 0 || enhanceItem.accessory_fire_offence > 0)
                return AttributeType.Fire;
            if (enhanceItem.weapon_water_offence > 0 || enhanceItem.armor_water_offence > 0 || enhanceItem.accessory_water_offence > 0)
                return AttributeType.Water;
            if (enhanceItem.weapon_wind_offence > 0 || enhanceItem.armor_wind_offence > 0 || enhanceItem.accessory_wind_offence > 0)
                return AttributeType.Wind;
            if (enhanceItem.weapon_earth_offence > 0 || enhanceItem.armor_earth_offence > 0 || enhanceItem.accessory_earth_offence > 0)
                return AttributeType.Earth;

            return AttributeType.Normal;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AttributeManagementService: 強化アイテム属性判定エラー - {ex.Message}");
            return AttributeType.Normal;
        }
    }

    /// <summary>
    /// 強化アイテムの属性攻撃値を取得（装備種類別）
    /// </summary>
    private int GetEnhanceItemAttributeValue(EnhanceItemMasterData enhanceItem, AttributeType attribute)
    {
        try
        {
            // 装備種類に関係なく、最大の属性攻撃値を返す
            // （実際の適用は装備種類に応じて EnhanceCalculationService で処理される）
            switch (attribute)
            {
                case AttributeType.Fire:
                    return Mathf.Max(enhanceItem.weapon_fire_offence,
                                   enhanceItem.armor_fire_offence,
                                   enhanceItem.accessory_fire_offence);
                case AttributeType.Water:
                    return Mathf.Max(enhanceItem.weapon_water_offence,
                                   enhanceItem.armor_water_offence,
                                   enhanceItem.accessory_water_offence);
                case AttributeType.Wind:
                    return Mathf.Max(enhanceItem.weapon_wind_offence,
                                   enhanceItem.armor_wind_offence,
                                   enhanceItem.accessory_wind_offence);
                case AttributeType.Earth:
                    return Mathf.Max(enhanceItem.weapon_earth_offence,
                                   enhanceItem.armor_earth_offence,
                                   enhanceItem.accessory_earth_offence);
                default:
                    return 0;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AttributeManagementService: 属性攻撃値取得エラー - {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 属性変更の警告メッセージ取得（UI表示用）
    /// </summary>
    public string GetAttributeChangeWarning(UserEquipment equipment, EnhanceItemMasterData enhanceItem)
    {
        try
        {
            if (equipment == null || enhanceItem == null)
            {
                return "";
            }

            AttributeType equipmentAttribute = GetEquipmentCurrentAttribute(equipment);
            AttributeType enhanceItemAttribute = GetEnhanceItemAttribute(enhanceItem);

            if (enhanceItemAttribute == AttributeType.Normal)
            {
                return ""; // 無属性アイテムは警告なし
            }

            if (equipmentAttribute == AttributeType.Normal)
            {
                return ""; // 無属性装備への属性付与は警告なし
            }

            if (equipmentAttribute != enhanceItemAttribute)
            {
                return "属性攻撃が上書きされます";
            }

            return ""; // 同じ属性は警告なし
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AttributeManagementService: 警告メッセージ生成エラー - {ex.Message}");
            return "属性チェックエラー";
        }
    }

    /// <summary>
    /// 属性名の表示用文字列取得
    /// </summary>
    public string GetAttributeDisplayName(AttributeType attribute)
    {
        switch (attribute)
        {
            case AttributeType.Normal: return "無属性";
            case AttributeType.Fire: return "火属性";
            case AttributeType.Water: return "水属性";
            case AttributeType.Wind: return "風属性";
            case AttributeType.Earth: return "土属性";
            default: return "不明";
        }
    }

    /// <summary>
    /// 装備の属性情報取得（デバッグ用）
    /// </summary>
    public AttributeInfo GetEquipmentAttributeInfo(UserEquipment equipment)
    {
        try
        {
            if (equipment == null)
            {
                return new AttributeInfo();
            }

            AttributeInfo info = new AttributeInfo();
            info.CurrentAttribute = GetEquipmentCurrentAttribute(equipment);
            info.FireOffence = equipment.fire_offence;
            info.WaterOffence = equipment.water_offence;
            info.WindOffence = equipment.wind_offence;
            info.EarthOffence = equipment.earth_offence;

            return info;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"AttributeManagementService: 属性情報取得エラー - {ex.Message}");
            return new AttributeInfo();
        }
    }
}

/// <summary>
/// 属性情報データクラス（デバッグ・UI表示用）
/// </summary>
[System.Serializable]
public class AttributeInfo
{
    public AttributeManagementService.AttributeType CurrentAttribute;
    public int FireOffence;
    public int WaterOffence;
    public int WindOffence;
    public int EarthOffence;

    /// <summary>
    /// デバッグ用文字列表現
    /// </summary>
    public override string ToString()
    {
        return $"属性:{CurrentAttribute}, 火:{FireOffence}, 水:{WaterOffence}, 風:{WindOffence}, 土:{EarthOffence}";
    }
}