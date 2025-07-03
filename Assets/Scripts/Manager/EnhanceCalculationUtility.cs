using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 装備強化計算専用ユーティリティクラス
/// 強化に関する全ての計算ロジックを集約
/// </summary>
public static class EnhanceCalculationUtility
{
    /// <summary>
    /// 強化成功率を計算
    /// 仕様: 強化値が+5される度に1%ずつ成功率が減少
    /// </summary>
    /// <param name="equipment">対象装備</param>
    /// <param name="enhanceItem">強化アイテム</param>
    /// <param name="supportItem">補助材料（null可）</param>
    /// <returns>最終的な成功率（0-100）</returns>
    public static float CalculateSuccessRate(UserEquipmentData equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem = null)
    {
        if (equipment == null || enhanceItem == null)
        {
            Debug.LogError("CalculateSuccessRate: 必要なデータがnullです");
            return 0f;
        }

        // 基本成功率を取得
        float baseSuccessRate = enhanceItem.enhanceSuccessRate;

        // 強化値による基本ペナルティを計算
        float penalty = CalculateEnhanceValuePenalty(equipment.currentEnhancedValue);

        // 補助材料による成功率ボーナスを計算
        float supportBonus = supportItem != null ? CalculateSupportItemBonus(supportItem) : 0f;

        // 最終成功率を計算（0-100の範囲でクランプ）
        float finalSuccessRate = baseSuccessRate - penalty + supportBonus;
        return Mathf.Clamp(finalSuccessRate, 0f, 100f);
    }

    /// <summary>
    /// 強化値によるペナルティを計算
    /// 仕様: +5→+6から-1%、+10→+11から-2%...
    /// </summary>
    /// <param name="currentEnhanceValue">現在の強化値</param>
    /// <returns>ペナルティ（%）</returns>
    public static float CalculateEnhanceValuePenalty(int currentEnhanceValue)
    {
        if (currentEnhanceValue < 5)
        {
            return 0f;
        }

        // 強化値を5で割った商がペナルティの回数
        int penaltyLevel = currentEnhanceValue / 5;
        return penaltyLevel * 1f; // 1%ずつ減少
    }

    /// <summary>
    /// 補助材料による成功率ボーナスを計算
    /// </summary>
    /// <param name="supportItem">補助材料</param>
    /// <returns>ボーナス（%）</returns>
    public static float CalculateSupportItemBonus(SupportItemMasterData supportItem)
    {
        if (supportItem == null)
        {
            return 0f;
        }

        // SupportItemMasterDataの addEnhanceSuccessRate プロパティから成功率ボーナスを取得
        float successRateBonus = supportItem.addEnhanceSuccessRate;

        // 成功率減少効果も考慮（珍しい物などで）
        float successRatePenalty = supportItem.reduceEnhanceSuccessRate;

        // 最終的なボーナス = 増加量 - 減少量
        float finalBonus = successRateBonus - successRatePenalty;

        return finalBonus;
    }

    /// <summary>
    /// 補助材料による強化耐久値への効果を計算（修正版：単純加算）
    /// </summary>
    /// <param name="supportItem">補助材料</param>
    /// <param name="baseStaminaChange">基本の耐久値変化</param>
    /// <returns>補助材料効果を適用した耐久値変化</returns>
    public static int CalculateSupportItemStaminaEffect(SupportItemMasterData supportItem, int baseStaminaChange)
    {
        if (supportItem == null)
        {
            return baseStaminaChange;
        }

        // 耐久保護スクロールなどの効果：成功失敗に関わらず耐久値を増加
        return baseStaminaChange + supportItem.addEnhanceStamina;
    }

    /// <summary>
    /// 補助材料によるステータス増加量への倍率効果を計算（修正版：CSVのmultipl_status_upを使用＋追加ステータス）
    /// </summary>
    /// <param name="supportItem">補助材料</param>
    /// <param name="baseStatusIncrease">基本のステータス増加量</param>
    /// <returns>補助材料効果を適用したステータス増加量</returns>
    public static Dictionary<string, int> CalculateSupportItemStatusEffect(SupportItemMasterData supportItem, Dictionary<string, int> baseStatusIncrease)
    {
        if (supportItem == null || baseStatusIncrease == null)
        {
            return baseStatusIncrease ?? new Dictionary<string, int>();
        }

        var modifiedStatusIncrease = new Dictionary<string, int>();

        // CSV の multipl_status_up フィールドから倍率を取得
        int multiplier = GetStatusMultiplier(supportItem);

        // 1. 基本効果に倍率を適用
        foreach (var kvp in baseStatusIncrease)
        {
            modifiedStatusIncrease[kvp.Key] = kvp.Value * multiplier;
        }

        // === 修正版：2. 補助材料のCSVに設定された追加ステータス値を加算 ===
        AddSupportItemBonusStatuses(supportItem, modifiedStatusIncrease);

        return modifiedStatusIncrease;
    }

    /// <summary>
    /// 【新規追加】補助材料のCSVに設定された追加ステータス値を加算
    /// </summary>
    /// <param name="supportItem">補助材料</param>
    /// <param name="statusIncrease">ステータス増加量の辞書（変更される）</param>
    private static void AddSupportItemBonusStatuses(SupportItemMasterData supportItem, Dictionary<string, int> statusIncrease)
    {
        if (supportItem == null || statusIncrease == null)
        {
            return;
        }

        // 補助材料のCSVに設定された各ステータス値を追加で加算
        AddBonusIfPositive(statusIncrease, "hp", supportItem.hp);
        AddBonusIfPositive(statusIncrease, "offense", supportItem.offense);
        AddBonusIfPositive(statusIncrease, "defense", supportItem.defense);
        AddBonusIfPositive(statusIncrease, "speed", supportItem.speed);
        AddBonusIfPositive(statusIncrease, "criticalRate", supportItem.criticalRate);
        AddBonusIfPositive(statusIncrease, "criticalDamageRate", supportItem.criticalDamageRate);
        AddBonusIfPositive(statusIncrease, "fireOffence", supportItem.fireOffence);
        AddBonusIfPositive(statusIncrease, "waterOffence", supportItem.waterOffence);
        AddBonusIfPositive(statusIncrease, "windOffence", supportItem.windOffence);
        AddBonusIfPositive(statusIncrease, "earthOffence", supportItem.earthOffence);
    }

    /// <summary>
    /// 【新規追加】正の値のみをボーナスとして加算するヘルパーメソッド
    /// </summary>
    /// <param name="statusIncrease">ステータス増加量の辞書</param>
    /// <param name="key">ステータス名</param>
    /// <param name="bonusValue">補助材料のボーナス値</param>
    private static void AddBonusIfPositive(Dictionary<string, int> statusIncrease, string key, int bonusValue)
    {
        if (bonusValue > 0)
        {
            // 既存の値に加算（存在しない場合は新規追加）
            if (statusIncrease.ContainsKey(key))
            {
                statusIncrease[key] += bonusValue;
            }
            else
            {
                statusIncrease[key] = bonusValue;
            }
        }
    }

    /// <summary>
    /// 補助材料による強化値への倍率効果を計算（修正版：add_enhanced_value追加効果を含む）
    /// </summary>
    /// <param name="supportItem">補助材料</param>
    /// <param name="baseEnhancedValue">基本の強化値増加量</param>
    /// <returns>補助材料効果を適用した強化値増加量</returns>
    public static int CalculateSupportItemEnhanceValueEffect(SupportItemMasterData supportItem, int baseEnhancedValue)
    {
        if (supportItem == null)
        {
            return baseEnhancedValue;
        }

        // 1. CSV の multipl_enhanced_value による倍率効果を適用
        int result = baseEnhancedValue;
        if (supportItem.multiplEnhancedValue > 1)
        {
            result *= supportItem.multiplEnhancedValue;
        }

        // === 修正版：2. CSVの add_enhanced_value による追加効果を加算 ===
        result += supportItem.addEnhancedValue;

        // 3. reduce_enhanced_value による減少効果を適用（珍しいケース）
        result -= supportItem.reduceEnhancedValue;

        // 結果が負の値にならないように保護
        return Mathf.Max(0, result);
    }

    /// <summary>
    /// 補助材料のステータス倍率を取得（修正版：正しいプロパティ名を使用）
    /// </summary>
    /// <param name="supportItem">補助材料</param>
    /// <returns>ステータス倍率（CSVのmultipl_status_up値、デフォルト1）</returns>
    public static int GetStatusMultiplier(SupportItemMasterData supportItem)
    {
        if (supportItem == null)
        {
            return 1;
        }

        // SupportItemMasterDataのmultiplStatusUpフィールドの値を使用
        // 0の場合は倍率なし（1倍）として扱う
        return supportItem.multiplStatusUp > 0 ? supportItem.multiplStatusUp : 1;
    }

    /// <summary>
    /// 補助材料がステータス増加量を倍にする効果を持つかチェック（修正版：CSVベース）
    /// </summary>
    /// <param name="supportItem">補助材料</param>
    /// <returns>倍率効果があるかどうか</returns>
    public static bool IsDoubleStatusEffect(SupportItemMasterData supportItem)
    {
        return GetStatusMultiplier(supportItem) > 1;
    }

    /// <summary>
    /// 装備種別のステータス増加量を計算
    /// </summary>
    /// <param name="equipmentType">装備タイプ</param>
    /// <param name="enhanceItem">強化アイテム</param>
    /// <returns>ステータス増加量の辞書</returns>
    public static Dictionary<string, int> CalculateStatusIncrease(EquipmentType equipmentType, EnhanceItemMasterData enhanceItem)
    {
        var statusIncrease = new Dictionary<string, int>();

        if (enhanceItem == null)
        {
            Debug.LogError("CalculateStatusIncrease: enhanceItemがnullです");
            return statusIncrease;
        }

        switch (equipmentType)
        {
            case EquipmentType.Weapon:
                AddIfPositive(statusIncrease, "hp", enhanceItem.weaponHp);
                AddIfPositive(statusIncrease, "offense", enhanceItem.weaponOffense);
                AddIfPositive(statusIncrease, "defense", enhanceItem.weaponDefense);
                AddIfPositive(statusIncrease, "speed", enhanceItem.weaponSpeed);
                AddIfPositive(statusIncrease, "criticalRate", enhanceItem.weaponCriticalRate);
                AddIfPositive(statusIncrease, "criticalDamageRate", enhanceItem.weaponCriticalDamageRate);
                AddIfPositive(statusIncrease, "fireOffence", enhanceItem.weaponFireOffence);
                AddIfPositive(statusIncrease, "waterOffence", enhanceItem.weaponWaterOffence);
                AddIfPositive(statusIncrease, "windOffence", enhanceItem.weaponWindOffence);
                AddIfPositive(statusIncrease, "earthOffence", enhanceItem.weaponEarthOffence);
                break;

            case EquipmentType.Armor:
                AddIfPositive(statusIncrease, "hp", enhanceItem.armorHp);
                AddIfPositive(statusIncrease, "offense", enhanceItem.armorOffense);
                AddIfPositive(statusIncrease, "defense", enhanceItem.armorDefense);
                AddIfPositive(statusIncrease, "speed", enhanceItem.armorSpeed);
                AddIfPositive(statusIncrease, "criticalRate", enhanceItem.armorCriticalRate);
                AddIfPositive(statusIncrease, "criticalDamageRate", enhanceItem.armorCriticalDamageRate);
                AddIfPositive(statusIncrease, "fireOffence", enhanceItem.armorFireOffence);
                AddIfPositive(statusIncrease, "waterOffence", enhanceItem.armorWaterOffence);
                AddIfPositive(statusIncrease, "windOffence", enhanceItem.armorWindOffence);
                AddIfPositive(statusIncrease, "earthOffence", enhanceItem.armorEarthOffence);
                break;

            case EquipmentType.Accessory:
                AddIfPositive(statusIncrease, "hp", enhanceItem.accessoryHp);
                AddIfPositive(statusIncrease, "offense", enhanceItem.accessoryOffense);
                AddIfPositive(statusIncrease, "defense", enhanceItem.accessoryDefense);
                AddIfPositive(statusIncrease, "speed", enhanceItem.accessorySpeed);
                AddIfPositive(statusIncrease, "criticalRate", enhanceItem.accessoryCriticalRate);
                AddIfPositive(statusIncrease, "criticalDamageRate", enhanceItem.accessoryCriticalDamageRate);
                AddIfPositive(statusIncrease, "fireOffence", enhanceItem.accessoryFireOffence);
                AddIfPositive(statusIncrease, "waterOffence", enhanceItem.accessoryWaterOffence);
                AddIfPositive(statusIncrease, "windOffence", enhanceItem.accessoryWindOffence);
                AddIfPositive(statusIncrease, "earthOffence", enhanceItem.accessoryEarthOffence);
                break;

            default:
                Debug.LogWarning($"未対応の装備タイプ: {equipmentType}");
                break;
        }

        return statusIncrease;
    }

    /// <summary>
    /// 属性変更を計算
    /// 仕様: 強化アイテムに属性があるか場合、装備の属性を上書き
    /// </summary>
    /// <param name="currentAttribute">現在の属性</param>
    /// <param name="enhanceItemAttribute">強化アイテムの属性</param>
    /// <returns>変更後の属性</returns>
    public static AttributeType CalculateAttributeChange(AttributeType currentAttribute, AttributeType enhanceItemAttribute)
    {
        // 強化アイテムがNone（無属性）の場合は属性変更なし
        if (enhanceItemAttribute == AttributeType.None)
        {
            return currentAttribute;
        }

        // それ以外の場合は強化アイテムの属性で上書き
        return enhanceItemAttribute;
    }

    /// <summary>
    /// 属性攻撃力の変更を計算（修正版：同じ属性の場合は加算、異なる属性の場合はリセット）
    /// 仕様: 属性変更後に他属性を強制的に0にリセット、同じ属性の場合は既存値に加算
    /// </summary>
    /// <param name="equipment">対象装備</param>
    /// <param name="enhanceItem">強化アイテム</param>
    /// <param name="newAttribute">変更後の属性</param>
    /// <returns>属性攻撃力の変更辞書（キー: 属性名、値: 新しい絶対値または変更量）</returns>
    public static Dictionary<string, int> CalculateAttributeAttackChange(UserEquipmentData equipment, EnhanceItemMasterData enhanceItem, AttributeType newAttribute)
    {
        var attributeChanges = new Dictionary<string, int>();

        if (equipment == null || enhanceItem == null)
        {
            return attributeChanges;
        }

        var masterData = MasterDataManager.Instance?.GetEquipmentData(equipment.equipmentMasterId);
        if (masterData == null)
        {
            return attributeChanges;
        }

        // 強化アイテムに属性が設定されている場合（属性変更が発生する場合）
        if (newAttribute != AttributeType.None && enhanceItem.attributeType != AttributeType.None)
        {
            // 現在の装備の属性と強化アイテムの属性を比較
            bool isAttributeChanging = equipment.currentAttributeType != newAttribute;

            if (isAttributeChanging)
            {
                // 属性が変更される場合：全ての属性攻撃力を0にリセット
                attributeChanges["fireOffence"] = 0;
                attributeChanges["waterOffence"] = 0;
                attributeChanges["windOffence"] = 0;
                attributeChanges["earthOffence"] = 0;

                // 新しい属性の攻撃力のみを設定（強化アイテムの効果量）
                var statusIncrease = CalculateStatusIncrease(masterData.equipmentType, enhanceItem);

                switch (newAttribute)
                {
                    case AttributeType.Fire:
                        if (statusIncrease.ContainsKey("fireOffence"))
                            attributeChanges["fireOffence"] = statusIncrease["fireOffence"];
                        break;
                    case AttributeType.Water:
                        if (statusIncrease.ContainsKey("waterOffence"))
                            attributeChanges["waterOffence"] = statusIncrease["waterOffence"];
                        break;
                    case AttributeType.Wind:
                        if (statusIncrease.ContainsKey("windOffence"))
                            attributeChanges["windOffence"] = statusIncrease["windOffence"];
                        break;
                    case AttributeType.Earth:
                        if (statusIncrease.ContainsKey("earthOffence"))
                            attributeChanges["earthOffence"] = statusIncrease["earthOffence"];
                        break;
                }
            }
            else
            {
                // 同じ属性で強化する場合：該当属性の攻撃力を加算
                var statusIncrease = CalculateStatusIncrease(masterData.equipmentType, enhanceItem);

                switch (newAttribute)
                {
                    case AttributeType.Fire:
                        if (statusIncrease.ContainsKey("fireOffence"))
                        {
                            // 現在の強化済み火属性攻撃力 + 強化アイテムの効果
                            int currentTotal = masterData.fireOffence + equipment.enhancedFireOffence;
                            int newTotal = currentTotal + statusIncrease["fireOffence"];
                            attributeChanges["fireOffence"] = newTotal;
                        }
                        break;
                    case AttributeType.Water:
                        if (statusIncrease.ContainsKey("waterOffence"))
                        {
                            int currentTotal = masterData.waterOffence + equipment.enhancedWaterOffence;
                            int newTotal = currentTotal + statusIncrease["waterOffence"];
                            attributeChanges["waterOffence"] = newTotal;
                        }
                        break;
                    case AttributeType.Wind:
                        if (statusIncrease.ContainsKey("windOffence"))
                        {
                            int currentTotal = masterData.windOffence + equipment.enhancedWindOffence;
                            int newTotal = currentTotal + statusIncrease["windOffence"];
                            attributeChanges["windOffence"] = newTotal;
                        }
                        break;
                    case AttributeType.Earth:
                        if (statusIncrease.ContainsKey("earthOffence"))
                        {
                            int currentTotal = masterData.earthOffence + equipment.enhancedEarthOffence;
                            int newTotal = currentTotal + statusIncrease["earthOffence"];
                            attributeChanges["earthOffence"] = newTotal;
                        }
                        break;
                }
            }
        }

        return attributeChanges;
    }

    /// <summary>
    /// 強化耐久値の変化を計算
    /// </summary>
    /// <param name="currentStamina">現在の耐久値</param>
    /// <param name="enhanceItem">強化アイテム</param>
    /// <returns>変化後の耐久値</returns>
    public static int CalculateStaminaChange(int currentStamina, EnhanceItemMasterData enhanceItem)
    {
        if (enhanceItem == null)
        {
            Debug.LogError("CalculateStaminaChange: enhanceItemがnullです");
            return currentStamina;
        }

        // 耐久値の増減を計算
        int staminaChange = enhanceItem.addEnhanceStamina - enhanceItem.reduceEnhanceStamina;
        int newStamina = currentStamina + staminaChange;

        // 0-100の範囲でクランプ
        return Mathf.Clamp(newStamina, 0, 100);
    }

    /// <summary>
    /// 強化可能かどうかをチェック
    /// </summary>
    /// <param name="equipment">対象装備</param>
    /// <param name="enhanceItem">強化アイテム</param>
    /// <returns>強化可能な場合true</returns>
    public static bool CanEnhance(UserEquipmentData equipment, EnhanceItemMasterData enhanceItem)
    {
        if (equipment == null || enhanceItem == null)
        {
            return false;
        }

        // 耐久値チェック：耐久減少アイテムの場合、耐久値が0以下では使用不可
        if (enhanceItem.reduceEnhanceStamina > 0 && equipment.currentEnhanceStamina <= 0)
        {
            return false;
        }

        // その他の基本的なチェック
        if (equipment.currentEnhancedValue < 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 強化実行の抽選を行う
    /// </summary>
    /// <param name="successRate">成功率（0-100）</param>
    /// <returns>成功した場合true</returns>
    public static bool RollEnhanceSuccess(float successRate)
    {
        if (successRate <= 0f)
        {
            return false;
        }

        if (successRate >= 100f)
        {
            return true;
        }

        float roll = UnityEngine.Random.Range(0f, 100f);
        return roll < successRate;
    }

    /// <summary>
    /// 装備の総戦闘力を計算
    /// </summary>
    /// <param name="equipment">装備データ</param>
    /// <param name="masterData">マスターデータ</param>
    /// <returns>総戦闘力</returns>
    public static int CalculateTotalPower(UserEquipmentData equipment, EquipmentMasterData masterData)
    {
        if (equipment == null || masterData == null)
        {
            return 0;
        }

        int totalPower = 0;

        // 基本ステータス + 強化ステータス
        totalPower += masterData.hp + equipment.enhancedHp;
        totalPower += masterData.offense + equipment.enhancedOffense;
        totalPower += masterData.defense + equipment.enhancedDefense;
        totalPower += masterData.speed + equipment.enhancedSpeed;

        // クリティカル系は重み付けして計算
        totalPower += (masterData.criticalRate + equipment.enhancedCriticalRate) / 10;
        totalPower += (masterData.criticalDamageRate + equipment.enhancedCriticalDamageRate) / 10;

        // 属性攻撃力
        totalPower += equipment.enhancedFireOffence;
        totalPower += equipment.enhancedWaterOffence;
        totalPower += equipment.enhancedWindOffence;
        totalPower += equipment.enhancedEarthOffence;

        return totalPower;
    }

    /// <summary>
    /// プラスの値のみを辞書に追加するヘルパーメソッド
    /// </summary>
    /// <param name="dictionary">対象辞書</param>
    /// <param name="key">キー</param>
    /// <param name="value">値</param>
    private static void AddIfPositive(Dictionary<string, int> dictionary, string key, int value)
    {
        if (value > 0)
        {
            dictionary[key] = value;
        }
    }

    /// <summary>
    /// 強化値によるペナルティの詳細情報を取得
    /// </summary>
    /// <param name="enhanceValue">強化値</param>
    /// <returns>ペナルティの詳細説明</returns>
    public static string GetPenaltyDescription(int enhanceValue)
    {
        if (enhanceValue < 5)
        {
            return "ペナルティなし";
        }

        int penaltyLevel = enhanceValue / 5;
        int nextThreshold = (penaltyLevel + 1) * 5;

        return $"強化値+{enhanceValue}: -{penaltyLevel}% (次のペナルティ増加: +{nextThreshold})";
    }

    /// <summary>
    /// デバッグ用：計算結果の詳細ログを出力
    /// </summary>
    /// <param name="equipment">装備</param>
    /// <param name="enhanceItem">強化アイテム</param>
    /// <param name="supportItem">補助材料</param>
    public static void LogCalculationDetails(UserEquipmentData equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem = null)
    {
        if (equipment == null || enhanceItem == null)
        {
            Debug.LogError("LogCalculationDetails: 必要なデータがnullです");
            return;
        }

        float baseRate = enhanceItem.enhanceSuccessRate;
        float penalty = CalculateEnhanceValuePenalty(equipment.currentEnhancedValue);
        float bonus = CalculateSupportItemBonus(supportItem);
        float finalRate = CalculateSuccessRate(equipment, enhanceItem, supportItem);

        string supportInfo = "";
        if (supportItem != null)
        {
            supportInfo = $"\n補助材料: {supportItem.supportItemName}" +
                         $"\n　成功率ボーナス: +{supportItem.addEnhanceSuccessRate}%" +
                         $"\n　ステータス倍率: {GetStatusMultiplier(supportItem)}倍" +
                         $"\n　強化値追加: +{supportItem.addEnhancedValue}";
        }

        Debug.Log($"=== 強化計算詳細 ===\n" +
                  $"装備: {equipment.userEquipmentId}\n" +
                  $"強化アイテム: {enhanceItem.enhanceItemName}\n" +
                  $"基本成功率: {baseRate}%\n" +
                  $"ペナルティ: -{penalty}%\n" +
                  $"ボーナス: +{bonus}%\n" +
                  $"最終成功率: {finalRate}%" +
                  supportInfo);
    }
}