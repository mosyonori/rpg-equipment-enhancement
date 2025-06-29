using System.Collections;
using UnityEngine;

/// <summary>
/// 装備強化システムのテストクラス
/// Step 1-4: データアクセス統合テスト用
/// エディター実行時またはPlayモードで動作確認が可能
/// </summary>
public class EnhanceSystemTest : MonoBehaviour
{
    [Header("テスト設定")]
    [SerializeField] private bool runTestOnStart = true;
    [SerializeField] private bool enableDetailedLog = true;

    [Header("テスト用データ")]
    [SerializeField] private string testEquipmentId = "";
    [SerializeField] private int testEnhanceItemId = 1; // 低級強化石
    [SerializeField] private int testSupportItemId = 0; // 使用しない

    private void Start()
    {
        if (runTestOnStart)
        {
            StartCoroutine(RunSystemTest());
        }
    }

    /// <summary>
    /// システムテストのメイン処理
    /// </summary>
    private IEnumerator RunSystemTest()
    {
        Debug.Log("=== 装備強化システムテスト開始 ===");

        // マネージャーの初期化を待機
        yield return StartCoroutine(WaitForManagersInitialization());

        // テスト用装備データを準備
        PrepareTestEquipment();

        // 1. 基本機能テスト
        yield return StartCoroutine(TestBasicFunctionality());

        // 2. プレビュー機能テスト
        yield return StartCoroutine(TestPreviewFunctionality());

        // 3. 計算精度テスト
        yield return StartCoroutine(TestCalculationAccuracy());

        // 4. エラーハンドリングテスト
        yield return StartCoroutine(TestErrorHandling());

        Debug.Log("=== 装備強化システムテスト完了 ===");
    }

    /// <summary>
    /// マネージャーの初期化完了を待機
    /// </summary>
    private IEnumerator WaitForManagersInitialization()
    {
        Debug.Log("マネージャー初期化待機中...");

        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (MasterDataManager.Instance != null && MasterDataManager.Instance.IsDataLoaded &&
                SaveDataManager.Instance != null && SaveDataManager.Instance.IsDataLoaded &&
                EquipmentEnhanceManager.Instance != null && EquipmentEnhanceManager.Instance.IsInitialized)
            {
                Debug.Log("✅ 全マネージャーの初期化完了");
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogError("❌ マネージャーの初期化がタイムアウトしました");
    }

    /// <summary>
    /// テスト用装備データを準備
    /// </summary>
    private void PrepareTestEquipment()
    {
        Debug.Log("--- テスト用装備データ準備 ---");

        // 既存の装備データを取得するか、新規作成
        var saveData = SaveDataManager.Instance.CurrentSaveData;
        if (saveData.equipments.Count == 0)
        {
            Debug.Log("装備データが存在しないため、テスト用装備を作成します");
            CreateTestEquipment();
        }
        else
        {
            testEquipmentId = saveData.equipments[0].userEquipmentId;
            Debug.Log($"既存装備を使用: {testEquipmentId}");
        }

        LogEquipmentStatus(testEquipmentId, "テスト開始時");
    }

    /// <summary>
    /// テスト用装備を作成
    /// </summary>
    private void CreateTestEquipment()
    {
        var masterData = MasterDataManager.Instance.GetEquipmentData(1); // 初心者の剣
        if (masterData != null)
        {
            var testEquipment = new UserEquipmentData(masterData);
            var saveData = SaveDataManager.Instance.CurrentSaveData;
            saveData.equipments.Add(testEquipment);

            testEquipmentId = testEquipment.userEquipmentId;
            SaveDataManager.Instance.MarkDataDirty();
            SaveDataManager.Instance.SaveSaveData();

            Debug.Log($"テスト用装備作成完了: {testEquipmentId}");
        }
        else
        {
            Debug.LogError("テスト用装備のマスターデータが見つかりません");
        }
    }

    /// <summary>
    /// 基本機能テスト
    /// </summary>
    private IEnumerator TestBasicFunctionality()
    {
        Debug.Log("--- 基本機能テスト ---");

        // 1. 強化実行可能性チェックテスト
        bool canEnhance = EquipmentEnhanceManager.Instance.CanExecuteEnhance(testEquipmentId, testEnhanceItemId);
        Debug.Log($"強化実行可能性: {canEnhance}");

        if (!canEnhance)
        {
            Debug.LogWarning("強化実行不可のため、基本機能テストをスキップします");
            yield break;
        }

        // 2. 利用可能アイテム一覧テスト
        var availableItems = EquipmentEnhanceManager.Instance.GetAvailableEnhanceItems();
        Debug.Log($"利用可能な強化アイテム数: {availableItems.Count}");

        // 3. 強化実行テスト
        LogEquipmentStatus(testEquipmentId, "強化実行前");

        var result = EquipmentEnhanceManager.Instance.ExecuteEnhance(testEquipmentId, testEnhanceItemId, testSupportItemId);

        if (result != null)
        {
            Debug.Log($"強化実行結果: {(result.isSuccess ? "成功" : "失敗")}");
            Debug.Log($"成功率: {result.actualSuccessRate:F1}%");

            if (enableDetailedLog)
            {
                Debug.Log($"詳細結果: {result}");
            }

            LogEquipmentStatus(testEquipmentId, "強化実行後");
        }
        else
        {
            Debug.LogError("❌ 強化実行が失敗しました");
        }

        yield return new WaitForSeconds(1f);
    }

    /// <summary>
    /// プレビュー機能テスト
    /// </summary>
    private IEnumerator TestPreviewFunctionality()
    {
        Debug.Log("--- プレビュー機能テスト ---");

        var preview = EquipmentEnhanceManager.Instance.GetEnhancePreview(testEquipmentId, testEnhanceItemId, testSupportItemId);

        if (preview != null)
        {
            Debug.Log($"✅ プレビュー取得成功");
            Debug.Log($"最終成功率: {preview.finalSuccessRate:F1}%");
            Debug.Log($"基本成功率: {preview.baseSuccessRate:F1}%");
            Debug.Log($"ペナルティ: -{preview.enhanceValuePenalty:F1}%");
            Debug.Log($"ボーナス: +{preview.supportItemBonus:F1}%");
            Debug.Log($"強化可能: {preview.CanExecuteEnhance()}");

            if (enableDetailedLog)
            {
                Debug.Log($"プレビュー詳細: {preview}");
                Debug.Log($"成功率詳細: {preview.GetSuccessRateDetails()}");
                Debug.Log($"ステータス変化: {preview.GetStatusChangeDetails()}");
            }
        }
        else
        {
            Debug.LogError("❌ プレビュー取得が失敗しました");
        }

        yield return new WaitForSeconds(1f);
    }

    /// <summary>
    /// 計算精度テスト
    /// </summary>
    private IEnumerator TestCalculationAccuracy()
    {
        Debug.Log("--- 計算精度テスト ---");

        var equipment = GetTestEquipment();
        var enhanceItem = MasterDataManager.Instance.GetEnhanceItemData(testEnhanceItemId);

        if (equipment != null && enhanceItem != null)
        {
            // 成功率計算テスト
            float calculatedRate = EnhanceCalculationUtility.CalculateSuccessRate(equipment, enhanceItem);
            float expectedPenalty = EnhanceCalculationUtility.CalculateEnhanceValuePenalty(equipment.currentEnhancedValue);

            Debug.Log($"成功率計算結果: {calculatedRate:F1}%");
            Debug.Log($"強化値ペナルティ: -{expectedPenalty:F1}%");
            Debug.Log($"ペナルティ詳細: {EnhanceCalculationUtility.GetPenaltyDescription(equipment.currentEnhancedValue)}");

            // ステータス増加計算テスト
            var masterData = MasterDataManager.Instance.GetEquipmentData(equipment.equipmentMasterId);
            if (masterData != null)
            {
                var statusIncrease = EnhanceCalculationUtility.CalculateStatusIncrease(masterData.equipmentType, enhanceItem);
                Debug.Log($"ステータス増加計算: {statusIncrease.Count}項目");

                if (enableDetailedLog)
                {
                    foreach (var kvp in statusIncrease)
                    {
                        Debug.Log($"  {kvp.Key}: +{kvp.Value}");
                    }
                }
            }

            // 属性変更計算テスト
            var newAttribute = EnhanceCalculationUtility.CalculateAttributeChange(equipment.currentAttributeType, enhanceItem.attributeType);
            Debug.Log($"属性変更: {equipment.currentAttributeType} → {newAttribute}");

            // 耐久値変化計算テスト
            int newStamina = EnhanceCalculationUtility.CalculateStaminaChange(equipment.currentEnhanceStamina, enhanceItem);
            Debug.Log($"耐久値変化: {equipment.currentEnhanceStamina} → {newStamina}");
        }
        else
        {
            Debug.LogError("❌ 計算精度テスト用のデータが不足しています");
        }

        yield return new WaitForSeconds(1f);
    }

    /// <summary>
    /// エラーハンドリングテスト
    /// </summary>
    private IEnumerator TestErrorHandling()
    {
        Debug.Log("--- エラーハンドリングテスト ---");

        // 1. 無効な装備IDテスト
        var result1 = EquipmentEnhanceManager.Instance.ExecuteEnhance("invalid_equipment_id", testEnhanceItemId);
        Debug.Log($"無効装備IDテスト: {(result1 == null ? "正常にエラー処理" : "予期しない成功")}");

        // 2. 無効な強化アイテムIDテスト
        var result2 = EquipmentEnhanceManager.Instance.ExecuteEnhance(testEquipmentId, 9999);
        Debug.Log($"無効アイテムIDテスト: {(result2 == null ? "正常にエラー処理" : "予期しない成功")}");

        // 3. 空文字列テスト
        var result3 = EquipmentEnhanceManager.Instance.ExecuteEnhance("", testEnhanceItemId);
        Debug.Log($"空文字列テスト: {(result3 == null ? "正常にエラー処理" : "予期しない成功")}");

        // 4. プレビューエラーテスト
        var preview = EquipmentEnhanceManager.Instance.GetEnhancePreview("invalid_id", 9999);
        Debug.Log($"プレビューエラーテスト: {(!preview.canEnhance ? "正常にエラー処理" : "予期しない成功")}");

        yield return new WaitForSeconds(1f);
    }

    /// <summary>
    /// テスト用装備データを取得
    /// </summary>
    private UserEquipmentData GetTestEquipment()
    {
        var saveData = SaveDataManager.Instance.CurrentSaveData;
        return saveData?.equipments?.Find(e => e.userEquipmentId == testEquipmentId);
    }

    /// <summary>
    /// 装備の現在状態をログ出力
    /// </summary>
    private void LogEquipmentStatus(string equipmentId, string timing)
    {
        var equipment = GetTestEquipment();
        if (equipment != null)
        {
            Debug.Log($"[{timing}] 装備状態:");
            Debug.Log($"  強化値: {equipment.currentEnhancedValue}");
            Debug.Log($"  属性: {equipment.currentAttributeType}");
            Debug.Log($"  耐久値: {equipment.currentEnhanceStamina}");
            Debug.Log($"  強化ステータス - HP:{equipment.enhancedHp}, 攻撃:{equipment.enhancedOffense}, 防御:{equipment.enhancedDefense}");
        }
        else
        {
            Debug.LogWarning($"[{timing}] 装備データが見つかりません: {equipmentId}");
        }
    }

    /// <summary>
    /// 手動テスト実行（Inspector用）
    /// </summary>
    [ContextMenu("Run Manual Test")]
    public void RunManualTest()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(RunSystemTest());
        }
        else
        {
            Debug.LogWarning("手動テストはPlayモードで実行してください");
        }
    }

    /// <summary>
    /// 強化実行テスト（Inspector用）
    /// </summary>
    [ContextMenu("Test Single Enhance")]
    public void TestSingleEnhance()
    {
        if (Application.isPlaying && !string.IsNullOrEmpty(testEquipmentId))
        {
            var result = EquipmentEnhanceManager.Instance.ExecuteEnhance(testEquipmentId, testEnhanceItemId);
            Debug.Log($"単発強化テスト結果: {(result?.isSuccess == true ? "成功" : "失敗")}");
        }
        else
        {
            Debug.LogWarning("Playモードで実行するか、testEquipmentIdを設定してください");
        }
    }

    /// <summary>
    /// プレビューテスト（Inspector用）
    /// </summary>
    [ContextMenu("Test Preview")]
    public void TestPreview()
    {
        if (Application.isPlaying && !string.IsNullOrEmpty(testEquipmentId))
        {
            var preview = EquipmentEnhanceManager.Instance.GetEnhancePreview(testEquipmentId, testEnhanceItemId);
            Debug.Log($"プレビューテスト - 成功率: {preview?.finalSuccessRate:F1}%, 実行可能: {preview?.CanExecuteEnhance()}");
        }
        else
        {
            Debug.LogWarning("Playモードで実行するか、testEquipmentIdを設定してください");
        }
    }
}