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

        // 簡易プレビューテスト
        EnhancePreviewData preview = calcService.GenerateEnhancePreview(
            testEquipment,
            testEnhanceItem,
            null
        );

        Debug.Log($"✅ 強化値増加: +{preview.EnhanceValueIncrease}");
        Debug.Log($"✅ HP増加: +{preview.HPIncrease}");
        Debug.Log($"✅ 攻撃増加: +{preview.OffenseIncrease}");

        Debug.Log("=== 簡易テスト完了 ===");
    }

    private EnhanceItemMasterData CreateTestEnhanceItem()
    {
        throw new NotImplementedException();
    }

    private UserEquipment CreateTestEquipment()
    {
        throw new NotImplementedException();
    }

    private void TestBasicCalculation()
    {
        Debug.Log("=== 通常テスト ===");
        // 元のテストコード...
    }

    // CreateTestEquipment() と CreateTestEnhanceItem() は同じ
}