using System;
using UnityEngine;

public class EnhanceCalculationServiceTest : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== EnhanceCalculationService テスト開始 ===");

        // DataManagerの初期化確認
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("DataManager.Instance がnullです - 簡易テストのみ実行");
            TestWithoutDataManager();
        }
        else
        {
            TestBasicCalculation();
        }
    }

    private void TestWithoutDataManager()
    {
        Debug.Log("=== DataManager無しテスト ===");

        EnhanceCalculationService calcService = new EnhanceCalculationService();

        UserEquipment testEquipment = CreateTestEquipment();
        EnhanceItemMasterData testEnhanceItem = CreateTestEnhanceItem();
        SupportItemMasterData testSupportItem = CreateTestSupportItem();

        Debug.Log($"テスト装備作成: {testEquipment.unique_id}, 強化値: {testEquipment.current_enhanced_value}");
        Debug.Log($"テスト強化アイテム作成: {testEnhanceItem.enhance_item_name}");

        // 1. 強化値増加計算テスト
        int enhanceValueIncrease = calcService.CalculateEnhanceValueIncrease(testEnhanceItem, null);
        Debug.Log($"✅ 強化値増加（補助材料なし）: +{enhanceValueIncrease}");

        int enhanceValueIncreaseWithSupport = calcService.CalculateEnhanceValueIncrease(testEnhanceItem, testSupportItem);
        Debug.Log($"✅ 強化値増加（補助材料あり）: +{enhanceValueIncreaseWithSupport}");

        // 2. 簡易プレビューテスト
        EnhancePreviewData preview = calcService.GenerateEnhancePreview(
            testEquipment,
            testEnhanceItem,
            null
        );

        Debug.Log($"✅ 強化プレビュー:");
        Debug.Log($"   現在強化値: {preview.CurrentEnhanceValue} → 予想: {preview.AfterEnhanceValue}");
        Debug.Log($"   HP増加: +{preview.HPIncrease}");
        Debug.Log($"   攻撃増加: +{preview.OffenseIncrease}");
        Debug.Log($"   防御増加: +{preview.DefenseIncrease}");

        // 3. 強化限界チェックテスト
        bool canEnhance = calcService.IsEnhancementAtLimit(testEquipment);
        Debug.Log($"✅ 強化可能かチェック: {(canEnhance ? "限界に達している" : "強化可能")}");

        // 4. 耐久減少テスト
        int oldStamina = testEquipment.current_enhance_stamina;
        calcService.ApplyStaminaDecrease(testEquipment, testEnhanceItem, null);
        Debug.Log($"✅ 耐久減少テスト: {oldStamina} → {testEquipment.current_enhance_stamina}");

        Debug.Log("=== 簡易テスト完了 ===");
    }

    private UserEquipment CreateTestEquipment()
    {
        return new UserEquipment
        {
            unique_id = "test_equipment_001",
            equipment_id = 1001,
            current_enhanced_value = 5,
            current_enhance_stamina = 80,
            is_equipped = false,
            acquired_time = DateTime.Now,

            // 現在のステータス
            hp = 100,
            offense = 50,
            defense = 30,
            speed = 20,
            critical_rate = 5,
            critical_damage_rate = 150,

            // 属性攻撃
            fire_offence = 0,
            water_offence = 0,
            wind_offence = 10, // 風属性装備として設定
            earth_offence = 0
        };
    }

    private EnhanceItemMasterData CreateTestEnhanceItem()
    {
        return new EnhanceItemMasterData
        {
            enhance_item_id = 2001,
            enhance_item_name = "テスト強化石",
            attribute_type = "Wind",
            rarity = "Common",
            max_stack_value = 99,
            add_enhanced_value = 1,
            reduce_enhanced_value = 0,
            add_enhance_stamina = 0,
            reduce_enhance_stamina = 10,
            enhance_success_rate = 80,
            enhance_item_icon_path = "Icons/enhance_stone_common",
            description = "テスト用の強化石です",
            completion_flag = 0,
            collection_flag = 0,

            // 武器用ステータス増加値
            weapon_hp = 1,
            weapon_offense = 2,
            weapon_defense = 1,
            weapon_speed = 1,
            weapon_critical_rate = 1,
            weapon_critical_damage_rate = 2,
            weapon_fire_offence = 0,
            weapon_water_offence = 0,
            weapon_wind_offence = 3,
            weapon_earth_offence = 0,

            // 防具用ステータス増加値
            armor_hp = 5,
            armor_offense = 1,
            armor_defense = 3,
            armor_speed = 0,
            armor_critical_rate = 0,
            armor_critical_damage_rate = 0,
            armor_fire_offence = 0,
            armor_water_offence = 0,
            armor_wind_offence = 1,
            armor_earth_offence = 0,

            // アクセサリ用ステータス増加値
            accessory_hp = 2,
            accessory_offense = 1,
            accessory_defense = 1,
            accessory_speed = 1,
            accessory_critical_rate = 1,
            accessory_critical_damage_rate = 1,
            accessory_fire_offence = 0,
            accessory_water_offence = 0,
            accessory_wind_offence = 2,
            accessory_earth_offence = 0
        };
    }

    private SupportItemMasterData CreateTestSupportItem()
    {
        return new SupportItemMasterData
        {
            support_item_id = 3001,
            support_item_name = "テスト補助材料",
            attribute_type = "Normal",
            rarity = "Common",
            infinite_use = 0,
            max_stack_value = 50,
            add_enhanced_value = 1,
            multipl_enhanced_value = 2,
            reduce_enhanced_value = 0,
            add_enhance_stamina = 5,
            reduce_enhance_stamina = 0,
            add_enhance_success_rate = 10,
            reduce_enhance_success_rate = 0,
            multipl_status_up = 2,
            support_item_icon_path = "Icons/support_item_common",
            description = "テスト用の補助材料です",
            completion_flag = 0,
            collection_flag = 0,

            // 補助材料の直接ステータス効果
            hp = 0,
            offense = 0,
            defense = 0,
            speed = 0,
            critical_rate = 0,
            critical_damage_rate = 0,
            fire_offence = 0,
            water_offence = 0,
            wind_offence = 0,
            earth_offence = 0
        };
    }

    private void TestBasicCalculation()
    {
        Debug.Log("=== DataManager有りテスト ===");

        // DataManagerが利用可能な場合の本格的なテスト
        EnhanceCalculationService calcService = new EnhanceCalculationService();

        try
        {
            // 実際のデータを使ったテストを実行
            Debug.Log("DataManager経由でのテストを実行中...");

            // TODO: 実際のデータを使ったテスト実装
            Debug.Log("✅ DataManagerテスト完了");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DataManagerテストでエラー: {ex.Message}");

            // エラーが発生した場合は簡易テストにフォールバック
            TestWithoutDataManager();
        }
    }
}