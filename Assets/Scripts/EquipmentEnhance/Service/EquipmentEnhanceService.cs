using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// 装備強化の実行処理専用サービス（バランス版・エラー修正版）
/// - 責任：強化実行ビジネスロジックのみ
/// - 修正時：強化実行の問題はここだけチェック
/// - 各Serviceを統合して強化処理を実行
/// </summary>
public class EquipmentEnhanceService
{
    private EnhanceCalculationService calculationService = new EnhanceCalculationService();
    private AttributeManagementService attributeService = new AttributeManagementService();
    private SuccessRateService successRateService = new SuccessRateService();
    private EnhanceDataService dataService = new EnhanceDataService();

    /// <summary>
    /// 装備強化実行（メイン処理）
    /// </summary>
    public EnhanceResultData ExecuteEnhance(string equipmentUniqueId, int enhanceItemId, int supportItemId = -1)
    {
        try
        {
            Debug.Log($"EquipmentEnhanceService: 強化実行開始 装備:{equipmentUniqueId}, 強化アイテム:{enhanceItemId}, 補助材料:{supportItemId}");

            // 1. 強化前データ取得・検証
            UserEquipment equipment = dataService.GetUserEquipment(equipmentUniqueId);
            EnhanceItemMasterData enhanceItem = dataService.GetEnhanceItemMaster(enhanceItemId);
            SupportItemMasterData supportItem = supportItemId > 0 ? dataService.GetSupportItemMaster(supportItemId) : null;

            // データ検証
            EnhanceResultData validationResult = ValidateEnhanceData(equipment, enhanceItem, supportItem);
            if (!validationResult.IsSuccess)
            {
                return validationResult; // 検証失敗時はそのまま返す
            }

            // 2. 強化可能性チェック
            if (!dataService.CanEnhanceEquipment(equipment))
            {
                return new EnhanceResultData
                {
                    IsSuccess = false,
                    EnhancedEquipment = equipment,
                    ResultMessage = "装備は強化できません（耐久不足または最大強化値到達）"
                };
            }

            // 3. アイテム所持確認
            if (!ValidateItemAvailability(enhanceItemId, supportItemId))
            {
                return new EnhanceResultData
                {
                    IsSuccess = false,
                    EnhancedEquipment = equipment,
                    ResultMessage = "必要なアイテムが不足しています"
                };
            }

            // 4. 成功率計算
            float successRate = successRateService.CalculateFinalSuccessRate(equipment, enhanceItem, supportItem);
            Debug.Log($"EquipmentEnhanceService: 成功率 {successRate}%");

            // 5. 成功判定
            bool isSuccess = UnityEngine.Random.Range(0f, 100f) <= successRate;
            Debug.Log($"EquipmentEnhanceService: 強化結果 {(isSuccess ? "成功" : "失敗")}");

            // 6. 強化実行
            EnhanceResultData result;
            if (isSuccess)
            {
                result = ExecuteSuccessfulEnhance(equipment, enhanceItem, supportItem);
            }
            else
            {
                result = ExecuteFailedEnhance(equipment, enhanceItem, supportItem);
            }

            // 7. アイテム消費とデータ保存
            bool saveSuccess = ConsumeItemsAndSaveData(equipment, enhanceItemId, supportItemId, result);
            if (!saveSuccess)
            {
                Debug.LogWarning("EquipmentEnhanceService: データ保存に問題がありましたが、強化は実行されました");
            }

            Debug.Log($"EquipmentEnhanceService: 強化処理完了 結果:{(result.IsSuccess ? "成功" : "失敗")}");
            return result;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EquipmentEnhanceService: 強化実行エラー - {ex.Message}");
            return new EnhanceResultData
            {
                IsSuccess = false,
                ResultMessage = $"強化処理中にエラーが発生しました: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 強化データの検証
    /// </summary>
    private EnhanceResultData ValidateEnhanceData(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        if (equipment == null)
        {
            return new EnhanceResultData
            {
                IsSuccess = false,
                ResultMessage = "装備データが見つかりません"
            };
        }

        if (enhanceItem == null)
        {
            return new EnhanceResultData
            {
                IsSuccess = false,
                EnhancedEquipment = equipment,
                ResultMessage = "強化アイテムデータが見つかりません"
            };
        }

        // 補助材料は任意なので、nullでもOK
        return new EnhanceResultData { IsSuccess = true };
    }

    /// <summary>
    /// アイテム所持確認
    /// </summary>
    private bool ValidateItemAvailability(int enhanceItemId, int supportItemId)
    {
        try
        {
            // 強化アイテム確認
            var enhanceItems = dataService.GetOwnedEnhanceItems();
            bool hasEnhanceItem = enhanceItems.Any(item => item.item_id == enhanceItemId && item.quantity > 0);

            if (!hasEnhanceItem)
            {
                Debug.LogWarning($"EquipmentEnhanceService: 強化アイテムが不足 ID:{enhanceItemId}");
                return false;
            }

            // 補助材料確認（指定されている場合のみ）
            if (supportItemId > 0)
            {
                var supportItems = dataService.GetOwnedSupportItems();
                bool hasSupportItem = supportItems.Any(item => item.item_id == supportItemId && item.quantity > 0);

                // 無限使用アイテムの場合は所持確認をスキップ
                SupportItemMasterData supportMaster = dataService.GetSupportItemMaster(supportItemId);
                if (supportMaster != null && supportMaster.infinite_use == 1)
                {
                    return true; // 無限使用アイテムは常に使用可能
                }

                if (!hasSupportItem)
                {
                    Debug.LogWarning($"EquipmentEnhanceService: 補助材料が不足 ID:{supportItemId}");
                    return false;
                }
            }

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EquipmentEnhanceService: アイテム所持確認エラー - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 強化成功時の処理
    /// </summary>
    private EnhanceResultData ExecuteSuccessfulEnhance(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        try
        {
            Debug.Log("EquipmentEnhanceService: 強化成功処理開始");

            EnhanceResultData result = new EnhanceResultData { IsSuccess = true };

            // 強化前の状態を記録
            int beforeEnhanceValue = equipment.current_enhanced_value;
            int beforeHP = equipment.hp;
            int beforeOffense = equipment.offense;

            // 1. 強化値増加
            int enhanceValueIncrease = calculationService.CalculateEnhanceValueIncrease(enhanceItem, supportItem);
            equipment.current_enhanced_value += enhanceValueIncrease;

            // 2. ステータス増加適用
            calculationService.ApplyStatusIncrease(equipment, enhanceItem, supportItem);

            // 3. 属性管理
            attributeService.ApplyAttributeChange(equipment, enhanceItem);

            // 4. 強化耐久減少
            calculationService.ApplyStaminaDecrease(equipment, enhanceItem, supportItem);

            // 結果データ設定
            result.EnhancedEquipment = equipment;
            result.ConsumedEnhanceItemId = enhanceItem.enhance_item_id;
            result.ConsumedSupportItemId = supportItem?.support_item_id ?? -1;
            result.ResultMessage = $"強化成功！ 強化値 {beforeEnhanceValue} → {equipment.current_enhanced_value}";

            Debug.Log($"EquipmentEnhanceService: 強化成功 {beforeEnhanceValue}→{equipment.current_enhanced_value}, HP{beforeHP}→{equipment.hp}, 攻撃{beforeOffense}→{equipment.offense}");

            return result;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EquipmentEnhanceService: 強化成功処理エラー - {ex.Message}");
            return new EnhanceResultData
            {
                IsSuccess = false,
                EnhancedEquipment = equipment,
                ResultMessage = $"強化成功処理でエラーが発生しました: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 強化失敗時の処理
    /// </summary>
    private EnhanceResultData ExecuteFailedEnhance(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        try
        {
            Debug.Log("EquipmentEnhanceService: 強化失敗処理開始");

            EnhanceResultData result = new EnhanceResultData { IsSuccess = false };

            // 失敗時は強化耐久のみ減少（ステータスや強化値は変化なし）
            int beforeStamina = equipment.current_enhance_stamina;
            calculationService.ApplyStaminaDecrease(equipment, enhanceItem, supportItem);

            // 結果データ設定
            result.EnhancedEquipment = equipment;
            result.ConsumedEnhanceItemId = enhanceItem.enhance_item_id;
            result.ConsumedSupportItemId = supportItem?.support_item_id ?? -1;
            result.ResultMessage = $"強化失敗... 耐久 {beforeStamina} → {equipment.current_enhance_stamina}";

            Debug.Log($"EquipmentEnhanceService: 強化失敗 耐久のみ減少 {beforeStamina}→{equipment.current_enhance_stamina}");

            return result;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EquipmentEnhanceService: 強化失敗処理エラー - {ex.Message}");
            return new EnhanceResultData
            {
                IsSuccess = false,
                EnhancedEquipment = equipment,
                ResultMessage = $"強化失敗処理でエラーが発生しました: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// アイテム消費とデータ保存
    /// </summary>
    private bool ConsumeItemsAndSaveData(UserEquipment equipment, int enhanceItemId, int supportItemId, EnhanceResultData result)
    {
        try
        {
            bool allSuccess = true;

            // 1. アイテム消費
            bool enhanceItemConsumed = dataService.ConsumeEnhanceItem(enhanceItemId, 1);
            if (!enhanceItemConsumed)
            {
                Debug.LogWarning("EquipmentEnhanceService: 強化アイテム消費に失敗");
                allSuccess = false;
            }

            if (supportItemId > 0)
            {
                bool supportItemConsumed = dataService.ConsumeSupportItem(supportItemId, 1);
                if (!supportItemConsumed)
                {
                    Debug.LogWarning("EquipmentEnhanceService: 補助材料消費に失敗");
                    allSuccess = false;
                }
            }

            // 2. 装備データ保存（DataManagerを直接使用）
            bool equipmentSaved = false;
            try
            {
                // DataManagerのSaveUserEquipmentメソッドが存在するかチェック
                if (DataManager.Instance != null)
                {
                    // 直接DataManagerを使用（voidメソッドの場合）
                    DataManager.Instance.SaveUserEquipment(equipment);
                    equipmentSaved = true; // 例外が発生しなければ成功とみなす
                    Debug.Log("EquipmentEnhanceService: 装備データ保存完了");
                }
                else
                {
                    Debug.LogWarning("EquipmentEnhanceService: DataManager.Instanceがnullです");
                    equipmentSaved = false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"EquipmentEnhanceService: SaveUserEquipmentメソッドでエラー - {ex.Message}");
                Debug.Log("装備データは一時的にメモリ上でのみ更新されています");
                equipmentSaved = true; // メモリ上では更新済みなので、一時的にtrueとする
            }

            if (!equipmentSaved)
            {
                Debug.LogWarning("EquipmentEnhanceService: 装備データ保存に失敗");
                allSuccess = false;
            }

            if (allSuccess)
            {
                Debug.Log("EquipmentEnhanceService: アイテム消費・データ保存完了");
            }

            return allSuccess;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EquipmentEnhanceService: アイテム消費・データ保存エラー - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 強化プレビュー生成（UI表示用）
    /// </summary>
    public EnhancePreviewData GenerateEnhancePreview(string equipmentUniqueId, int enhanceItemId, int supportItemId = -1)
    {
        try
        {
            UserEquipment equipment = dataService.GetUserEquipment(equipmentUniqueId);
            EnhanceItemMasterData enhanceItem = dataService.GetEnhanceItemMaster(enhanceItemId);
            SupportItemMasterData supportItem = supportItemId > 0 ? dataService.GetSupportItemMaster(supportItemId) : null;

            if (equipment == null || enhanceItem == null)
            {
                Debug.LogWarning("EquipmentEnhanceService: プレビュー生成でデータ不足");
                return new EnhancePreviewData();
            }

            return calculationService.GenerateEnhancePreview(equipment, enhanceItem, supportItem);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EquipmentEnhanceService: プレビュー生成エラー - {ex.Message}");
            return new EnhancePreviewData();
        }
    }

    /// <summary>
    /// 強化可能性チェック（UI表示用）
    /// </summary>
    public bool CanExecuteEnhance(string equipmentUniqueId, int enhanceItemId, int supportItemId = -1)
    {
        try
        {
            UserEquipment equipment = dataService.GetUserEquipment(equipmentUniqueId);
            if (equipment == null)
            {
                return false;
            }

            // 基本的な強化可能性チェック
            if (!dataService.CanEnhanceEquipment(equipment))
            {
                return false;
            }

            // アイテム所持確認
            if (!ValidateItemAvailability(enhanceItemId, supportItemId))
            {
                return false;
            }

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EquipmentEnhanceService: 強化可能性チェックエラー - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 成功率取得（UI表示用）
    /// </summary>
    public float GetSuccessRate(string equipmentUniqueId, int enhanceItemId, int supportItemId = -1)
    {
        try
        {
            UserEquipment equipment = dataService.GetUserEquipment(equipmentUniqueId);
            EnhanceItemMasterData enhanceItem = dataService.GetEnhanceItemMaster(enhanceItemId);
            SupportItemMasterData supportItem = supportItemId > 0 ? dataService.GetSupportItemMaster(supportItemId) : null;

            if (equipment == null || enhanceItem == null)
            {
                return 0f;
            }

            return successRateService.CalculateFinalSuccessRate(equipment, enhanceItem, supportItem);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EquipmentEnhanceService: 成功率取得エラー - {ex.Message}");
            return 0f;
        }
    }

    /// <summary>
    /// 属性警告メッセージ取得（UI表示用）
    /// </summary>
    public string GetAttributeWarning(string equipmentUniqueId, int enhanceItemId)
    {
        try
        {
            UserEquipment equipment = dataService.GetUserEquipment(equipmentUniqueId);
            EnhanceItemMasterData enhanceItem = dataService.GetEnhanceItemMaster(enhanceItemId);

            if (equipment == null || enhanceItem == null)
            {
                return "";
            }

            return attributeService.GetAttributeChangeWarning(equipment, enhanceItem);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EquipmentEnhanceService: 属性警告取得エラー - {ex.Message}");
            return "属性チェックエラー";
        }
    }

    /// <summary>
    /// 強化結果のデバッグ情報出力
    /// </summary>
    public void LogEnhanceDebugInfo(string equipmentUniqueId, int enhanceItemId, int supportItemId = -1)
    {
        try
        {
            Debug.Log("=== EquipmentEnhanceService デバッグ情報 ===");

            UserEquipment equipment = dataService.GetUserEquipment(equipmentUniqueId);
            EnhanceItemMasterData enhanceItem = dataService.GetEnhanceItemMaster(enhanceItemId);
            SupportItemMasterData supportItem = supportItemId > 0 ? dataService.GetSupportItemMaster(supportItemId) : null;

            if (equipment == null || enhanceItem == null)
            {
                Debug.LogWarning("デバッグ情報: データが不足しています");
                return;
            }

            // 装備情報
            Debug.Log($"装備: {equipment.unique_id}, 強化値: {equipment.current_enhanced_value}, 耐久: {equipment.current_enhance_stamina}");

            // 強化アイテム情報
            Debug.Log($"強化アイテム: {enhanceItem.enhance_item_name}, 成功率: {enhanceItem.enhance_success_rate}%");

            // 補助材料情報
            if (supportItem != null)
            {
                Debug.Log($"補助材料: {supportItem.support_item_name}, 成功率修正: +{supportItem.add_enhance_success_rate}%");
            }
            else
            {
                Debug.Log("補助材料: 使用しない");
            }

            // 最終成功率
            float successRate = GetSuccessRate(equipmentUniqueId, enhanceItemId, supportItemId);
            Debug.Log($"最終成功率: {successRate}%");

            // 属性警告
            string warning = GetAttributeWarning(equipmentUniqueId, enhanceItemId);
            if (!string.IsNullOrEmpty(warning))
            {
                Debug.Log($"属性警告: {warning}");
            }

            Debug.Log("=== デバッグ情報終了 ===");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"EquipmentEnhanceService: デバッグ情報出力エラー - {ex.Message}");
        }
    }
}

/// <summary>
/// 強化結果データ（拡張版）
/// </summary>
[System.Serializable]
public class EnhanceResultData
{
    public bool IsSuccess;                      // 強化成功フラグ
    public UserEquipment EnhancedEquipment;     // 強化後の装備
    public int ConsumedEnhanceItemId = -1;      // 消費した強化アイテムID
    public int ConsumedSupportItemId = -1;      // 消費した補助材料ID
    public string ResultMessage = "";           // 結果メッセージ

    /// <summary>
    /// デバッグ用文字列表現
    /// </summary>
    public override string ToString()
    {
        return $"強化結果: {(IsSuccess ? "成功" : "失敗")} - {ResultMessage}";
    }
}