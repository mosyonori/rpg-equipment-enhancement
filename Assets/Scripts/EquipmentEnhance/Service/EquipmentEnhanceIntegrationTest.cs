using System;
using UnityEngine;
using TMPro;

/// <summary>
/// 装備強化Service層の統合テスト
/// - 全Serviceの連携動作を検証
/// - 実際の強化処理フローをテスト
/// - エラーハンドリングの確認
/// </summary>
public class EquipmentEnhanceIntegrationTest : MonoBehaviour
{
    [Header("テスト設定")]
    [SerializeField] private bool autoRunOnStart = true;
    [SerializeField] private bool enableDetailedLogs = true;

    private EquipmentEnhanceService enhanceService;
    private EnhanceDataService dataService;
    private SuccessRateService successRateService;
    private AttributeManagementService attributeService;

    private void Start()
    {
        if (autoRunOnStart)
        {
            StartIntegrationTest();
        }
    }

    /// <summary>
    /// 統合テスト開始
    /// </summary>
    public void StartIntegrationTest()
    {
        Debug.Log("=== 装備強化Service層 統合テスト開始 ===");

        // Service初期化
        InitializeServices();

        // DataManager確認
        if (!ValidateDataManagerStatus())
        {
            Debug.LogError("DataManager未初期化のため、モックデータでテストを実行します");
            RunMockDataTest();
        }
        else
        {
            Debug.Log("DataManager正常動作確認 - 実データでテストを実行します");
            RunRealDataTest();
        }

        Debug.Log("=== 装備強化Service層 統合テスト完了 ===");
    }

    /// <summary>
    /// Service初期化
    /// </summary>
    private void InitializeServices()
    {
        try
        {
            enhanceService = new EquipmentEnhanceService();
            dataService = new EnhanceDataService();
            successRateService = new SuccessRateService();
            attributeService = new AttributeManagementService();

            Debug.Log("✅ 全Service初期化成功");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Service初期化失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// DataManager状態確認
    /// </summary>
    private bool ValidateDataManagerStatus()
    {
        try
        {
            if (DataManager.Instance == null)
            {
                Debug.LogWarning("DataManager.Instance がnullです");
                return false;
            }

            // 基本的な動作確認
            var equipments = dataService.GetOwnedEquipments();
            var enhanceItems = dataService.GetOwnedEnhanceItems();
            var supportItems = dataService.GetOwnedSupportItems();

            Debug.Log($"データ確認 - 装備:{equipments.Count}件, 強化アイテム:{enhanceItems.Count}件, 補助材料:{supportItems.Count}件");

            return equipments.Count > 0 && enhanceItems.Count > 0;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DataManager確認エラー: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 実データでの統合テスト
    /// </summary>
    private void RunRealDataTest()
    {
        Debug.Log("=== 実データ統合テスト開始 ===");

        try
        {
            // 1. データ取得テスト
            TestDataRetrieval();

            // 2. 成功率計算テスト
            TestSuccessRateCalculation();

            // 3. プレビュー生成テスト
            TestPreviewGeneration();

            // 4. 強化実行テスト（成功パターン）
            TestEnhanceExecution();

            // 5. 属性管理テスト
            TestAttributeManagement();

            // 6. エラーハンドリングテスト
            TestErrorHandling();

            Debug.Log("✅ 実データ統合テスト完了");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ 実データテストエラー: {ex.Message}");
        }
    }

    /// <summary>
    /// モックデータでの統合テスト
    /// </summary>
    private void RunMockDataTest()
    {
        Debug.Log("=== モックデータ統合テスト開始 ===");

        try
        {
            // モックデータ作成
            UserEquipment mockEquipment = CreateMockEquipment();
            EnhanceItemMasterData mockEnhanceItem = CreateMockEnhanceItem();
            SupportItemMasterData mockSupportItem = CreateMockSupportItem();

            // 各Serviceの個別テスト
            TestCalculationService(mockEquipment, mockEnhanceItem, mockSupportItem);
            TestSuccessRateService(mockEquipment, mockEnhanceItem, mockSupportItem);
            TestAttributeService(mockEquipment, mockEnhanceItem);

            Debug.Log("✅ モックデータ統合テスト完了");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ モックデータテストエラー: {ex.Message}");
        }
    }

    /// <summary>
    /// データ取得テスト
    /// </summary>
    private void TestDataRetrieval()
    {
        Debug.Log("--- データ取得テスト ---");

        var equipments = dataService.GetOwnedEquipments();
        var enhanceItems = dataService.GetOwnedEnhanceItems();
        var supportItems = dataService.GetOwnedSupportItems();

        Debug.Log($"装備取得: {equipments.Count}件");
        Debug.Log($"強化アイテム取得: {enhanceItems.Count}件");
        Debug.Log($"補助材料取得: {supportItems.Count}件");

        if (equipments.Count > 0)
        {
            var firstEquipment = equipments[0];
            Debug.Log($"テスト装備: {firstEquipment.unique_id}, 強化値:{firstEquipment.current_enhanced_value}");
        }

        Debug.Log("✅ データ取得テスト完了");
    }

    /// <summary>
    /// 成功率計算テスト
    /// </summary>
    private void TestSuccessRateCalculation()
    {
        Debug.Log("--- 成功率計算テスト ---");

        var equipments = dataService.GetOwnedEquipments();
        var enhanceItems = dataService.GetOwnedEnhanceItems();

        if (equipments.Count > 0 && enhanceItems.Count > 0)
        {
            var equipment = equipments[0];
            var enhanceItem = dataService.GetEnhanceItemMaster(enhanceItems[0].item_id);

            if (enhanceItem != null)
            {
                // 補助材料なしでの成功率
                float baseRate = enhanceService.GetSuccessRate(equipment.unique_id, enhanceItem.enhance_item_id);
                Debug.Log($"基本成功率: {baseRate}%");

                // 補助材料ありでの成功率テスト
                var supportItems = dataService.GetOwnedSupportItems();
                if (supportItems.Count > 0)
                {
                    float withSupportRate = enhanceService.GetSuccessRate(equipment.unique_id, enhanceItem.enhance_item_id, supportItems[0].item_id);
                    Debug.Log($"補助材料使用時成功率: {withSupportRate}%");
                }
            }
        }

        Debug.Log("✅ 成功率計算テスト完了");
    }

    /// <summary>
    /// プレビュー生成テスト
    /// </summary>
    private void TestPreviewGeneration()
    {
        Debug.Log("--- プレビュー生成テスト ---");

        var equipments = dataService.GetOwnedEquipments();
        var enhanceItems = dataService.GetOwnedEnhanceItems();

        if (equipments.Count > 0 && enhanceItems.Count > 0)
        {
            var equipment = equipments[0];
            var enhanceItem = enhanceItems[0];

            EnhancePreviewData preview = enhanceService.GenerateEnhancePreview(
                equipment.unique_id,
                enhanceItem.item_id
            );

            Debug.Log($"プレビュー生成成功:");
            Debug.Log($"  現在強化値: {preview.CurrentEnhanceValue} → 予想: {preview.AfterEnhanceValue}");
            Debug.Log($"  HP変化: {preview.CurrentHP} → {preview.AfterHP} (+{preview.HPIncrease})");
            Debug.Log($"  攻撃変化: {preview.CurrentOffense} → {preview.AfterOffense} (+{preview.OffenseIncrease})");
        }

        Debug.Log("✅ プレビュー生成テスト完了");
    }

    /// <summary>
    /// 強化実行テスト
    /// </summary>
    private void TestEnhanceExecution()
    {
        Debug.Log("--- 強化実行テスト ---");

        var equipments = dataService.GetOwnedEquipments();
        var enhanceItems = dataService.GetOwnedEnhanceItems();

        if (equipments.Count > 0 && enhanceItems.Count > 0)
        {
            var equipment = equipments[0];
            var enhanceItem = enhanceItems[0];

            // 強化前の状態を記録
            int beforeEnhanceValue = equipment.current_enhanced_value;
            int beforeHP = equipment.hp;
            int beforeStamina = equipment.current_enhance_stamina;

            Debug.Log($"強化前状態: 強化値{beforeEnhanceValue}, HP{beforeHP}, 耐久{beforeStamina}");

            // 強化可能性チェック
            bool canEnhance = enhanceService.CanExecuteEnhance(equipment.unique_id, enhanceItem.item_id);
            Debug.Log($"強化可能性: {canEnhance}");

            if (canEnhance)
            {
                // 強化実行
                EnhanceResultData result = enhanceService.ExecuteEnhance(
                    equipment.unique_id,
                    enhanceItem.item_id
                );

                Debug.Log($"強化結果: {result}");
                Debug.Log($"強化後状態: 強化値{result.EnhancedEquipment.current_enhanced_value}, HP{result.EnhancedEquipment.hp}, 耐久{result.EnhancedEquipment.current_enhance_stamina}");
            }
        }

        Debug.Log("✅ 強化実行テスト完了");
    }

    /// <summary>
    /// 属性管理テスト
    /// </summary>
    private void TestAttributeManagement()
    {
        Debug.Log("--- 属性管理テスト ---");

        var equipments = dataService.GetOwnedEquipments();
        var enhanceItems = dataService.GetOwnedEnhanceItems();

        if (equipments.Count > 0 && enhanceItems.Count > 0)
        {
            var equipment = equipments[0];
            var enhanceItem = enhanceItems[0];

            // 属性警告メッセージテスト
            string warning = enhanceService.GetAttributeWarning(equipment.unique_id, enhanceItem.item_id);
            Debug.Log($"属性警告: {(string.IsNullOrEmpty(warning) ? "なし" : warning)}");

            // 装備の現在属性確認
            var currentAttribute = attributeService.GetEquipmentCurrentAttribute(equipment);
            Debug.Log($"装備の現在属性: {attributeService.GetAttributeDisplayName(currentAttribute)}");

            // 強化アイテムの属性確認
            var enhanceItemMaster = dataService.GetEnhanceItemMaster(enhanceItem.item_id);
            if (enhanceItemMaster != null)
            {
                var itemAttribute = attributeService.GetEnhanceItemAttribute(enhanceItemMaster);
                Debug.Log($"強化アイテムの属性: {attributeService.GetAttributeDisplayName(itemAttribute)}");
            }
        }

        Debug.Log("✅ 属性管理テスト完了");
    }

    /// <summary>
    /// エラーハンドリングテスト
    /// </summary>
    private void TestErrorHandling()
    {
        Debug.Log("--- エラーハンドリングテスト ---");

        // 存在しない装備IDでのテスト
        EnhanceResultData invalidResult = enhanceService.ExecuteEnhance("invalid_equipment_id", 1);
        Debug.Log($"無効装備ID結果: {invalidResult.ResultMessage}");

        // 存在しない強化アイテムIDでのテスト
        var equipments = dataService.GetOwnedEquipments();
        if (equipments.Count > 0)
        {
            EnhanceResultData invalidItemResult = enhanceService.ExecuteEnhance(equipments[0].unique_id, -999);
            Debug.Log($"無効アイテムID結果: {invalidItemResult.ResultMessage}");
        }

        // 成功率が0%の場合のテスト（デバッグ用）
        float zeroRate = enhanceService.GetSuccessRate("invalid_id", -999);
        Debug.Log($"無効データでの成功率: {zeroRate}%");

        Debug.Log("✅ エラーハンドリングテスト完了");
    }

    /// <summary>
    /// 計算サービステスト（モックデータ用）
    /// </summary>
    private void TestCalculationService(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        Debug.Log("--- 計算サービステスト ---");

        var calculationService = new EnhanceCalculationService();

        // 強化値増加計算
        int enhanceIncrease = calculationService.CalculateEnhanceValueIncrease(enhanceItem, null);
        Debug.Log($"強化値増加（補助材料なし）: +{enhanceIncrease}");

        int enhanceIncreaseWithSupport = calculationService.CalculateEnhanceValueIncrease(enhanceItem, supportItem);
        Debug.Log($"強化値増加（補助材料あり）: +{enhanceIncreaseWithSupport}");

        // プレビュー生成
        var preview = calculationService.GenerateEnhancePreview(equipment, enhanceItem, supportItem);
        Debug.Log($"モックプレビュー: 強化値+{preview.EnhanceValueIncrease}, HP+{preview.HPIncrease}");

        Debug.Log("✅ 計算サービステスト完了");
    }

    /// <summary>
    /// 成功率サービステスト（モックデータ用）
    /// </summary>
    private void TestSuccessRateService(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        Debug.Log("--- 成功率サービステスト ---");

        var successRateService = new SuccessRateService();

        float baseRate = successRateService.CalculateFinalSuccessRate(equipment, enhanceItem, null);
        Debug.Log($"基本成功率: {baseRate}%");

        float withSupportRate = successRateService.CalculateFinalSuccessRate(equipment, enhanceItem, supportItem);
        Debug.Log($"補助材料使用時: {withSupportRate}%");

        // 成功率内訳取得
        var breakdown = successRateService.GetSuccessRateBreakdown(equipment, enhanceItem, supportItem);
        Debug.Log($"成功率詳細: {breakdown}");

        Debug.Log("✅ 成功率サービステスト完了");
    }

    /// <summary>
    /// 属性サービステスト（モックデータ用）
    /// </summary>
    private void TestAttributeService(UserEquipment equipment, EnhanceItemMasterData enhanceItem)
    {
        Debug.Log("--- 属性サービステスト ---");

        var attributeService = new AttributeManagementService();

        var equipmentAttribute = attributeService.GetEquipmentCurrentAttribute(equipment);
        var itemAttribute = attributeService.GetEnhanceItemAttribute(enhanceItem);

        Debug.Log($"装備属性: {attributeService.GetAttributeDisplayName(equipmentAttribute)}");
        Debug.Log($"アイテム属性: {attributeService.GetAttributeDisplayName(itemAttribute)}");

        string warning = attributeService.GetAttributeChangeWarning(equipment, enhanceItem);
        Debug.Log($"属性警告: {(string.IsNullOrEmpty(warning) ? "なし" : warning)}");

        Debug.Log("✅ 属性サービステスト完了");
    }

    /// <summary>
    /// モック装備データ作成
    /// </summary>
    private UserEquipment CreateMockEquipment()
    {
        return new UserEquipment
        {
            unique_id = "mock_equipment_001",
            equipment_id = 1001,
            current_enhanced_value = 5,
            current_enhance_stamina = 80,
            is_equipped = false,
            acquired_time = DateTime.Now,
            hp = 100,
            offense = 50,
            defense = 30,
            speed = 20,
            critical_rate = 5,
            critical_damage_rate = 150,
            fire_offence = 0,
            water_offence = 0,
            wind_offence = 10,
            earth_offence = 0
        };
    }

    /// <summary>
    /// モック強化アイテムデータ作成
    /// </summary>
    private EnhanceItemMasterData CreateMockEnhanceItem()
    {
        return new EnhanceItemMasterData
        {
            enhance_item_id = 2001,
            enhance_item_name = "モック強化石",
            attribute_type = "Wind",
            rarity = "Common",
            max_stack_value = 99,
            add_enhanced_value = 1,
            reduce_enhanced_value = 0,
            add_enhance_stamina = 0,
            reduce_enhance_stamina = 10,
            enhance_success_rate = 80,
            enhance_item_icon_path = "Icons/mock_enhance_stone",
            description = "テスト用の強化石",
            completion_flag = 0,
            collection_flag = 0,

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

    /// <summary>
    /// モック補助材料データ作成
    /// </summary>
    private SupportItemMasterData CreateMockSupportItem()
    {
        return new SupportItemMasterData
        {
            support_item_id = 3001,
            support_item_name = "モック補助材料",
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
            enhance_item_icon_path = "Icons/mock_support_item",
            description = "テスト用の補助材料",
            completion_flag = 0,
            collection_flag = 0,

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

    /// <summary>
    /// 手動テスト実行（Inspector用）
    /// </summary>
    [ContextMenu("手動テスト実行")]
    public void RunManualTest()
    {
        StartIntegrationTest();
    }

    /// <summary>
    /// 詳細ログ出力（Inspector用）
    /// </summary>
    [ContextMenu("詳細デバッグ情報出力")]
    public void OutputDetailedDebugInfo()
    {
        if (enhanceService != null)
        {
            var equipments = dataService.GetOwnedEquipments();
            var enhanceItems = dataService.GetOwnedEnhanceItems();

            if (equipments.Count > 0 && enhanceItems.Count > 0)
            {
                enhanceService.LogEnhanceDebugInfo(
                    equipments[0].unique_id,
                    enhanceItems[0].item_id
                );
            }
        }
    }
}