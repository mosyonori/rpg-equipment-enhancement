using System;
using UnityEngine;

/// <summary>
/// 装備強化の計算処理専用サービス（バランス版）
/// - 責任：強化関連の計算ロジックのみ
/// - 修正時：計算ロジックの問題はここだけチェック
/// - 装備種類別の強化効果計算を管理
/// </summary>
public class EnhanceCalculationService
{
    /// <summary>
    /// 強化値増加量計算
    /// </summary>
    public int CalculateEnhanceValueIncrease(EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        try
        {
            if (enhanceItem == null)
            {
                Debug.LogWarning("EnhanceCalculationService: 強化アイテムがnullです");
                return 0;
            }

            int baseIncrease = enhanceItem.add_enhanced_value;

            if (supportItem != null)
            {
                // 乗算効果（例：怪しい薬 = 2倍）
                if (supportItem.multipl_enhanced_value > 1)
                {
                    baseIncrease *= supportItem.multipl_enhanced_value;
                    Debug.Log($"EnhanceCalculationService: 強化値乗算適用 {enhanceItem.add_enhanced_value} x {supportItem.multipl_enhanced_value} = {baseIncrease}");
                }

                // 加算効果
                baseIncrease += supportItem.add_enhanced_value;

                if (supportItem.add_enhanced_value > 0)
                {
                    Debug.Log($"EnhanceCalculationService: 強化値加算適用 +{supportItem.add_enhanced_value}");
                }
            }

            Debug.Log($"EnhanceCalculationService: 最終強化値増加量 = {baseIncrease}");
            return Math.Max(0, baseIncrease); // 負の値は0にする
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EnhanceCalculationService: 強化値計算エラー - {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 装備種類別のステータス増加適用（メイン処理）
    /// </summary>
    public void ApplyStatusIncrease(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        try
        {
            if (equipment == null || enhanceItem == null)
            {
                Debug.LogError("EnhanceCalculationService: 装備または強化アイテムがnullです");
                return;
            }

            EquipmentMasterData equipmentMaster = DataManager.Instance.GetEquipmentMasterData(equipment.equipment_id);
            if (equipmentMaster == null)
            {
                Debug.LogError($"EnhanceCalculationService: 装備マスターデータが見つかりません ID:{equipment.equipment_id}");
                return;
            }

            Debug.Log($"EnhanceCalculationService: {equipmentMaster.equipment_type} 用の強化効果を適用開始");

            // 装備種類によって強化内容が変わる
            switch (equipmentMaster.equipment_type)
            {
                case EquipmentType.Weapon:
                    ApplyWeaponEnhance(equipment, enhanceItem, supportItem);
                    break;
                case EquipmentType.Armor:
                    ApplyArmorEnhance(equipment, enhanceItem, supportItem);
                    break;
                case EquipmentType.Accessory:
                    ApplyAccessoryEnhance(equipment, enhanceItem, supportItem);
                    break;
                default:
                    Debug.LogWarning($"EnhanceCalculationService: 未対応の装備タイプです {equipmentMaster.equipment_type}");
                    break;
            }

            Debug.Log("EnhanceCalculationService: ステータス増加適用完了");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EnhanceCalculationService: ステータス増加適用エラー - {ex.Message}");
        }
    }

    /// <summary>
    /// 武器強化効果適用
    /// 仕様：武器：強化値+1、攻撃+1、クリティカルダメージ+1%
    /// </summary>
    private void ApplyWeaponEnhance(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        Debug.Log("EnhanceCalculationService: 武器強化効果適用開始");

        // 武器用のステータス増加値を取得
        EnhanceStatusValues weaponValues = enhanceItem.GetStatusValuesByEquipmentType(EquipmentType.Weapon);

        // 各ステータスに補助材料効果を適用して増加
        int hpIncrease = CalculateStatusIncrease(weaponValues.hp, supportItem);
        int offenseIncrease = CalculateStatusIncrease(weaponValues.offense, supportItem);
        int defenseIncrease = CalculateStatusIncrease(weaponValues.defense, supportItem);
        int speedIncrease = CalculateStatusIncrease(weaponValues.speed, supportItem);
        int criticalRateIncrease = CalculateStatusIncrease(weaponValues.critical_rate, supportItem);
        int criticalDamageIncrease = CalculateStatusIncrease(weaponValues.critical_damage_rate, supportItem);

        // ステータス適用
        equipment.hp += hpIncrease;
        equipment.offense += offenseIncrease;
        equipment.defense += defenseIncrease;
        equipment.speed += speedIncrease;
        equipment.critical_rate += criticalRateIncrease;
        equipment.critical_damage_rate += criticalDamageIncrease;

        Debug.Log($"武器強化適用: HP+{hpIncrease}, 攻撃+{offenseIncrease}, 防御+{defenseIncrease}");
        Debug.Log($"             速度+{speedIncrease}, クリ率+{criticalRateIncrease}, クリダメ+{criticalDamageIncrease}%");

        // 属性攻撃も適用
        ApplyAttributeStatusIncrease(equipment, weaponValues, supportItem);
    }

    /// <summary>
    /// 防具強化効果適用
    /// 仕様：防具：強化値+1、HP+3、防御+1
    /// </summary>
    private void ApplyArmorEnhance(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        Debug.Log("EnhanceCalculationService: 防具強化効果適用開始");

        // 防具用のステータス増加値を取得
        EnhanceStatusValues armorValues = enhanceItem.GetStatusValuesByEquipmentType(EquipmentType.Armor);

        // 各ステータスに補助材料効果を適用して増加
        int hpIncrease = CalculateStatusIncrease(armorValues.hp, supportItem);
        int offenseIncrease = CalculateStatusIncrease(armorValues.offense, supportItem);
        int defenseIncrease = CalculateStatusIncrease(armorValues.defense, supportItem);
        int speedIncrease = CalculateStatusIncrease(armorValues.speed, supportItem);
        int criticalRateIncrease = CalculateStatusIncrease(armorValues.critical_rate, supportItem);
        int criticalDamageIncrease = CalculateStatusIncrease(armorValues.critical_damage_rate, supportItem);

        // ステータス適用
        equipment.hp += hpIncrease;
        equipment.offense += offenseIncrease;
        equipment.defense += defenseIncrease;
        equipment.speed += speedIncrease;
        equipment.critical_rate += criticalRateIncrease;
        equipment.critical_damage_rate += criticalDamageIncrease;

        Debug.Log($"防具強化適用: HP+{hpIncrease}, 攻撃+{offenseIncrease}, 防御+{defenseIncrease}");
        Debug.Log($"             速度+{speedIncrease}, クリ率+{criticalRateIncrease}, クリダメ+{criticalDamageIncrease}%");

        // 属性攻撃も適用
        ApplyAttributeStatusIncrease(equipment, armorValues, supportItem);
    }

    /// <summary>
    /// アクセサリ強化効果適用
    /// 仕様：アクセ：強化値+1、HP+1、攻撃+1、防御+1
    /// </summary>
    private void ApplyAccessoryEnhance(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        Debug.Log("EnhanceCalculationService: アクセサリ強化効果適用開始");

        // アクセサリ用のステータス増加値を取得
        EnhanceStatusValues accessoryValues = enhanceItem.GetStatusValuesByEquipmentType(EquipmentType.Accessory);

        // 各ステータスに補助材料効果を適用して増加
        int hpIncrease = CalculateStatusIncrease(accessoryValues.hp, supportItem);
        int offenseIncrease = CalculateStatusIncrease(accessoryValues.offense, supportItem);
        int defenseIncrease = CalculateStatusIncrease(accessoryValues.defense, supportItem);
        int speedIncrease = CalculateStatusIncrease(accessoryValues.speed, supportItem);
        int criticalRateIncrease = CalculateStatusIncrease(accessoryValues.critical_rate, supportItem);
        int criticalDamageIncrease = CalculateStatusIncrease(accessoryValues.critical_damage_rate, supportItem);

        // ステータス適用
        equipment.hp += hpIncrease;
        equipment.offense += offenseIncrease;
        equipment.defense += defenseIncrease;
        equipment.speed += speedIncrease;
        equipment.critical_rate += criticalRateIncrease;
        equipment.critical_damage_rate += criticalDamageIncrease;

        Debug.Log($"アクセサリ強化適用: HP+{hpIncrease}, 攻撃+{offenseIncrease}, 防御+{defenseIncrease}");
        Debug.Log($"                   速度+{speedIncrease}, クリ率+{criticalRateIncrease}, クリダメ+{criticalDamageIncrease}%");

        // 属性攻撃も適用
        ApplyAttributeStatusIncrease(equipment, accessoryValues, supportItem);
    }

    /// <summary>
    /// 属性攻撃ステータス増加適用
    /// </summary>
    private void ApplyAttributeStatusIncrease(UserEquipment equipment, EnhanceStatusValues statusValues, SupportItemMasterData supportItem)
    {
        int fireIncrease = CalculateStatusIncrease(statusValues.fire_offence, supportItem);
        int waterIncrease = CalculateStatusIncrease(statusValues.water_offence, supportItem);
        int windIncrease = CalculateStatusIncrease(statusValues.wind_offence, supportItem);
        int earthIncrease = CalculateStatusIncrease(statusValues.earth_offence, supportItem);

        equipment.fire_offence += fireIncrease;
        equipment.water_offence += waterIncrease;
        equipment.wind_offence += windIncrease;
        equipment.earth_offence += earthIncrease;

        if (fireIncrease > 0 || waterIncrease > 0 || windIncrease > 0 || earthIncrease > 0)
        {
            Debug.Log($"属性攻撃増加: 火+{fireIncrease}, 水+{waterIncrease}, 風+{windIncrease}, 土+{earthIncrease}");
        }
    }

    /// <summary>
    /// 補助材料効果を考慮したステータス増加値計算
    /// </summary>
    private int CalculateStatusIncrease(int baseValue, SupportItemMasterData supportItem)
    {
        if (baseValue == 0) return 0; // 元の値が0なら増加なし

        int result = baseValue;

        if (supportItem != null)
        {
            // 補助材料の乗算効果（例：怪しい薬でステータス2倍）
            if (supportItem.multipl_status_up > 1)
            {
                result *= supportItem.multipl_status_up;
            }

            // 補助材料の直接ステータス効果は別途適用
            // （これは強化アイテムの効果とは別の、補助材料自体の効果）
        }

        return Math.Max(0, result); // 負の値は0にする
    }

    /// <summary>
    /// 強化耐久減少処理
    /// </summary>
    public void ApplyStaminaDecrease(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        try
        {
            if (equipment == null || enhanceItem == null)
            {
                Debug.LogError("EnhanceCalculationService: 装備または強化アイテムがnullです");
                return;
            }

            int staminaDecrease = enhanceItem.reduce_enhance_stamina;

            if (supportItem != null)
            {
                // 補助材料による耐久増加効果（例：保護スクロール）
                if (supportItem.add_enhance_stamina > 0)
                {
                    staminaDecrease -= supportItem.add_enhance_stamina;
                    Debug.Log($"EnhanceCalculationService: 補助材料による耐久減少軽減 -{supportItem.add_enhance_stamina}");
                }

                // 補助材料による耐久減少増加効果
                if (supportItem.reduce_enhance_stamina > 0)
                {
                    staminaDecrease += supportItem.reduce_enhance_stamina;
                    Debug.Log($"EnhanceCalculationService: 補助材料による耐久減少増加 +{supportItem.reduce_enhance_stamina}");
                }
            }

            // 強化耐久は0以下にはならない
            staminaDecrease = Math.Max(0, staminaDecrease);

            int oldStamina = equipment.current_enhance_stamina;
            equipment.current_enhance_stamina = Math.Max(0, equipment.current_enhance_stamina - staminaDecrease);

            Debug.Log($"EnhanceCalculationService: 強化耐久変化 {oldStamina} → {equipment.current_enhance_stamina} (減少: {staminaDecrease})");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EnhanceCalculationService: 強化耐久減少エラー - {ex.Message}");
        }
    }

    /// <summary>
    /// 装備が強化限界に達しているかチェック
    /// </summary>
    public bool IsEnhancementAtLimit(UserEquipment equipment)
    {
        try
        {
            if (equipment == null)
            {
                Debug.LogWarning("EnhanceCalculationService: 装備がnullです");
                return true; // 安全のためtrue
            }

            EquipmentMasterData masterData = DataManager.Instance.GetEquipmentMasterData(equipment.equipment_id);
            if (masterData == null)
            {
                Debug.LogWarning($"EnhanceCalculationService: 装備マスターデータが見つかりません ID:{equipment.equipment_id}");
                return true; // 安全のためtrue
            }

            bool isAtMaxEnhanceValue = equipment.current_enhanced_value >= masterData.max_enhanced_value;
            bool isStaminaExhausted = equipment.current_enhance_stamina <= 0;

            if (isAtMaxEnhanceValue)
            {
                Debug.Log($"EnhanceCalculationService: 最大強化値に達しています {equipment.current_enhanced_value}/{masterData.max_enhanced_value}");
            }

            if (isStaminaExhausted)
            {
                Debug.Log("EnhanceCalculationService: 強化耐久が不足しています");
            }

            return isAtMaxEnhanceValue || isStaminaExhausted;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EnhanceCalculationService: 強化限界チェックエラー - {ex.Message}");
            return true; // エラー時は安全のためtrue
        }
    }

    /// <summary>
    /// 強化プレビューデータ生成
    /// </summary>
    public EnhancePreviewData GenerateEnhancePreview(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        try
        {
            if (equipment == null || enhanceItem == null)
            {
                Debug.LogError("EnhanceCalculationService: プレビュー生成でnullパラメータ");
                return new EnhancePreviewData();
            }

            // DataManager の存在確認
            if (DataManager.Instance == null)
            {
                Debug.LogError("EnhanceCalculationService: DataManager.Instance がnullです");
                return GenerateSimplePreview(equipment, enhanceItem, supportItem);
            }

            EquipmentMasterData equipmentMaster = DataManager.Instance.GetEquipmentMasterData(equipment.equipment_id);
            if (equipmentMaster == null)
            {
                Debug.LogWarning($"EnhanceCalculationService: 装備マスターデータが見つかりません ID:{equipment.equipment_id} - 簡易プレビューを生成します");
                return GenerateSimplePreview(equipment, enhanceItem, supportItem);
            }

            EnhancePreviewData preview = new EnhancePreviewData();

            // 現在の値
            preview.CurrentEnhanceValue = equipment.current_enhanced_value;
            preview.CurrentHP = equipment.hp;
            preview.CurrentOffense = equipment.offense;
            preview.CurrentDefense = equipment.defense;
            preview.CurrentSpeed = equipment.speed;
            preview.CurrentCriticalRate = equipment.critical_rate;
            preview.CurrentCriticalDamage = equipment.critical_damage_rate;
            preview.CurrentStamina = equipment.current_enhance_stamina;

            // 強化値増加量
            preview.EnhanceValueIncrease = CalculateEnhanceValueIncrease(enhanceItem, supportItem);

            // 装備種類に応じた変化量を計算
            switch (equipmentMaster.equipment_type)
            {
                case EquipmentType.Weapon:
                    CalculateWeaponPreview(preview, enhanceItem, supportItem);
                    break;
                case EquipmentType.Armor:
                    CalculateArmorPreview(preview, enhanceItem, supportItem);
                    break;
                case EquipmentType.Accessory:
                    CalculateAccessoryPreview(preview, enhanceItem, supportItem);
                    break;
                default:
                    Debug.LogWarning($"EnhanceCalculationService: 不明な装備タイプ {equipmentMaster.equipment_type} - 簡易計算を使用");
                    CalculateSimpleStatusPreview(preview, enhanceItem, supportItem);
                    break;
            }

            // 強化耐久減少量
            preview.StaminaDecrease = Math.Max(0, enhanceItem.reduce_enhance_stamina -
                (supportItem?.add_enhance_stamina ?? 0) +
                (supportItem?.reduce_enhance_stamina ?? 0));

            Debug.Log($"EnhanceCalculationService: プレビュー生成完了 - 強化値+{preview.EnhanceValueIncrease}, HP+{preview.HPIncrease}");

            return preview;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EnhanceCalculationService: プレビュー生成エラー - {ex.Message}");
            return GenerateSimplePreview(equipment, enhanceItem, supportItem);
        }
    }

    /// <summary>
    /// 簡易プレビューデータ生成（エラー回避用）
    /// </summary>
    private EnhancePreviewData GenerateSimplePreview(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        try
        {
            Debug.Log("EnhanceCalculationService: 簡易プレビューモードで生成");

            EnhancePreviewData preview = new EnhancePreviewData();

            // 現在の値（安全な取得）
            preview.CurrentEnhanceValue = equipment?.current_enhanced_value ?? 0;
            preview.CurrentHP = equipment?.hp ?? 0;
            preview.CurrentOffense = equipment?.offense ?? 0;
            preview.CurrentDefense = equipment?.defense ?? 0;
            preview.CurrentSpeed = equipment?.speed ?? 0;
            preview.CurrentCriticalRate = equipment?.critical_rate ?? 0;
            preview.CurrentCriticalDamage = equipment?.critical_damage_rate ?? 0;
            preview.CurrentStamina = equipment?.current_enhance_stamina ?? 0;

            // 基本的な増加値のみ計算
            preview.EnhanceValueIncrease = CalculateEnhanceValueIncrease(enhanceItem, supportItem);

            // 装備タイプが不明なので、アイテムの基本値を使用
            if (enhanceItem != null)
            {
                // 仮の計算（装備タイプ判定なし）
                CalculateSimpleStatusPreview(preview, enhanceItem, supportItem);
            }

            // 強化耐久減少量
            preview.StaminaDecrease = enhanceItem != null ? Math.Max(0, enhanceItem.reduce_enhance_stamina -
                (supportItem?.add_enhance_stamina ?? 0) +
                (supportItem?.reduce_enhance_stamina ?? 0)) : 0;

            Debug.Log($"EnhanceCalculationService: 簡易プレビュー生成完了 - 強化値+{preview.EnhanceValueIncrease}");

            return preview;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EnhanceCalculationService: 簡易プレビュー生成でもエラー - {ex.Message}");
            return new EnhancePreviewData(); // 空のプレビューを返す
        }
    }

    /// <summary>
    /// 簡易ステータス計算（装備タイプ不明時）
    /// </summary>
    private void CalculateSimpleStatusPreview(EnhancePreviewData preview, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        // GetStatusValuesByEquipmentType が使えない場合の代替処理
        // 武器の値を基準として使用（仮）
        if (enhanceItem != null)
        {
            preview.HPIncrease = CalculateStatusIncrease(enhanceItem.weapon_hp, supportItem);
            preview.OffenseIncrease = CalculateStatusIncrease(enhanceItem.weapon_offense, supportItem);
            preview.DefenseIncrease = CalculateStatusIncrease(enhanceItem.weapon_defense, supportItem);
            preview.SpeedIncrease = CalculateStatusIncrease(enhanceItem.weapon_speed, supportItem);
            preview.CriticalRateIncrease = CalculateStatusIncrease(enhanceItem.weapon_critical_rate, supportItem);
            preview.CriticalDamageIncrease = CalculateStatusIncrease(enhanceItem.weapon_critical_damage_rate, supportItem);

            preview.FireOffenceIncrease = CalculateStatusIncrease(enhanceItem.weapon_fire_offence, supportItem);
            preview.WaterOffenceIncrease = CalculateStatusIncrease(enhanceItem.weapon_water_offence, supportItem);
            preview.WindOffenceIncrease = CalculateStatusIncrease(enhanceItem.weapon_wind_offence, supportItem);
            preview.EarthOffenceIncrease = CalculateStatusIncrease(enhanceItem.weapon_earth_offence, supportItem);

            Debug.Log("EnhanceCalculationService: 武器データを基準とした簡易計算を使用");
        }
    }

    /// <summary>
    /// 武器用プレビュー計算
    /// </summary>
    private void CalculateWeaponPreview(EnhancePreviewData preview, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        EnhanceStatusValues weaponValues = enhanceItem.GetStatusValuesByEquipmentType(EquipmentType.Weapon);

        preview.HPIncrease = CalculateStatusIncrease(weaponValues.hp, supportItem);
        preview.OffenseIncrease = CalculateStatusIncrease(weaponValues.offense, supportItem);
        preview.DefenseIncrease = CalculateStatusIncrease(weaponValues.defense, supportItem);
        preview.SpeedIncrease = CalculateStatusIncrease(weaponValues.speed, supportItem);
        preview.CriticalRateIncrease = CalculateStatusIncrease(weaponValues.critical_rate, supportItem);
        preview.CriticalDamageIncrease = CalculateStatusIncrease(weaponValues.critical_damage_rate, supportItem);

        preview.FireOffenceIncrease = CalculateStatusIncrease(weaponValues.fire_offence, supportItem);
        preview.WaterOffenceIncrease = CalculateStatusIncrease(weaponValues.water_offence, supportItem);
        preview.WindOffenceIncrease = CalculateStatusIncrease(weaponValues.wind_offence, supportItem);
        preview.EarthOffenceIncrease = CalculateStatusIncrease(weaponValues.earth_offence, supportItem);
    }

    /// <summary>
    /// 防具用プレビュー計算
    /// </summary>
    private void CalculateArmorPreview(EnhancePreviewData preview, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        EnhanceStatusValues armorValues = enhanceItem.GetStatusValuesByEquipmentType(EquipmentType.Armor);

        preview.HPIncrease = CalculateStatusIncrease(armorValues.hp, supportItem);
        preview.OffenseIncrease = CalculateStatusIncrease(armorValues.offense, supportItem);
        preview.DefenseIncrease = CalculateStatusIncrease(armorValues.defense, supportItem);
        preview.SpeedIncrease = CalculateStatusIncrease(armorValues.speed, supportItem);
        preview.CriticalRateIncrease = CalculateStatusIncrease(armorValues.critical_rate, supportItem);
        preview.CriticalDamageIncrease = CalculateStatusIncrease(armorValues.critical_damage_rate, supportItem);

        preview.FireOffenceIncrease = CalculateStatusIncrease(armorValues.fire_offence, supportItem);
        preview.WaterOffenceIncrease = CalculateStatusIncrease(armorValues.water_offence, supportItem);
        preview.WindOffenceIncrease = CalculateStatusIncrease(armorValues.wind_offence, supportItem);
        preview.EarthOffenceIncrease = CalculateStatusIncrease(armorValues.earth_offence, supportItem);
    }

    /// <summary>
    /// アクセサリ用プレビュー計算
    /// </summary>
    private void CalculateAccessoryPreview(EnhancePreviewData preview, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        EnhanceStatusValues accessoryValues = enhanceItem.GetStatusValuesByEquipmentType(EquipmentType.Accessory);

        preview.HPIncrease = CalculateStatusIncrease(accessoryValues.hp, supportItem);
        preview.OffenseIncrease = CalculateStatusIncrease(accessoryValues.offense, supportItem);
        preview.DefenseIncrease = CalculateStatusIncrease(accessoryValues.defense, supportItem);
        preview.SpeedIncrease = CalculateStatusIncrease(accessoryValues.speed, supportItem);
        preview.CriticalRateIncrease = CalculateStatusIncrease(accessoryValues.critical_rate, supportItem);
        preview.CriticalDamageIncrease = CalculateStatusIncrease(accessoryValues.critical_damage_rate, supportItem);

        preview.FireOffenceIncrease = CalculateStatusIncrease(accessoryValues.fire_offence, supportItem);
        preview.WaterOffenceIncrease = CalculateStatusIncrease(accessoryValues.water_offence, supportItem);
        preview.WindOffenceIncrease = CalculateStatusIncrease(accessoryValues.wind_offence, supportItem);
        preview.EarthOffenceIncrease = CalculateStatusIncrease(accessoryValues.earth_offence, supportItem);
    }
}

/// <summary>
/// 強化プレビューデータ（拡張版）
/// </summary>
[System.Serializable]
public class EnhancePreviewData
{
    // 現在の値
    public int CurrentEnhanceValue;
    public int CurrentHP;
    public int CurrentOffense;
    public int CurrentDefense;
    public int CurrentSpeed;
    public int CurrentCriticalRate;
    public int CurrentCriticalDamage;
    public int CurrentStamina;

    // 予想される変化
    public int EnhanceValueIncrease;
    public int HPIncrease;
    public int OffenseIncrease;
    public int DefenseIncrease;
    public int SpeedIncrease;
    public int CriticalRateIncrease;
    public int CriticalDamageIncrease;
    public int StaminaDecrease;

    // 属性攻撃の変化
    public int FireOffenceIncrease;
    public int WaterOffenceIncrease;
    public int WindOffenceIncrease;
    public int EarthOffenceIncrease;

    // 強化後の値（プロパティ）
    public int AfterEnhanceValue => CurrentEnhanceValue + EnhanceValueIncrease;
    public int AfterHP => CurrentHP + HPIncrease;
    public int AfterOffense => CurrentOffense + OffenseIncrease;
    public int AfterDefense => CurrentDefense + DefenseIncrease;
    public int AfterSpeed => CurrentSpeed + SpeedIncrease;
    public int AfterCriticalRate => CurrentCriticalRate + CriticalRateIncrease;
    public int AfterCriticalDamage => CurrentCriticalDamage + CriticalDamageIncrease;
    public int AfterStamina => Math.Max(0, CurrentStamina - StaminaDecrease);
}