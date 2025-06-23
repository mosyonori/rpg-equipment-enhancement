using System;
using UnityEngine;

/// <summary>
/// 強化成功率の計算専用サービス（バランス版）
/// - 責任：成功率計算ロジックのみ
/// - 修正時：成功率の問題はここだけチェック
/// - 強化値による段階的減少、補助材料効果を管理
/// </summary>
public class SuccessRateService
{
    /// <summary>
    /// 最終成功率計算（メインメソッド）
    /// </summary>
    public float CalculateFinalSuccessRate(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        try
        {
            if (equipment == null || enhanceItem == null)
            {
                Debug.LogWarning("SuccessRateService: 装備または強化アイテムがnullです");
                return 0f;
            }

            // 1. 基本成功率
            float baseRate = enhanceItem.enhance_success_rate;

            // 2. 強化値による成功率減少
            float enhanceValuePenalty = CalculateEnhanceValuePenalty(equipment.current_enhanced_value);

            // 3. 補助材料による成功率増減
            float supportItemModifier = CalculateSupportItemModifier(supportItem);

            // 4. 最終成功率計算
            float finalRate = baseRate - enhanceValuePenalty + supportItemModifier;

            // 5. 0-100%の範囲に制限
            finalRate = Mathf.Clamp(finalRate, 0f, 100f);

            Debug.Log($"SuccessRateService: 成功率計算 基本{baseRate}% - 減少{enhanceValuePenalty}% + 補助{supportItemModifier}% = {finalRate}%");

            return finalRate;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SuccessRateService: 成功率計算エラー - {ex.Message}");
            return 0f; // エラー時は安全のため0%
        }
    }

    /// <summary>
    /// 強化値による成功率減少計算
    /// 仕様：+5される度に1%ずつ減少
    /// +5→+6から-1%、+10→+11から-2%、+50→+51から-10%、+100→+101から-20%
    /// </summary>
    private float CalculateEnhanceValuePenalty(int currentEnhanceValue)
    {
        try
        {
            if (currentEnhanceValue < 5)
            {
                return 0f; // +5未満は減少なし
            }

            // 減少段階の計算（5で割った商）
            int penaltyStage = currentEnhanceValue / 5;

            // 段階に応じた減少率
            float penalty = penaltyStage * 1f; // 1段階につき1%減少

            Debug.Log($"SuccessRateService: 強化値{currentEnhanceValue} → 段階{penaltyStage} → 減少率{penalty}%");

            return penalty;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SuccessRateService: 強化値減少計算エラー - {ex.Message}");
            return 0f;
        }
    }

    /// <summary>
    /// 補助材料による成功率増減計算
    /// </summary>
    private float CalculateSupportItemModifier(SupportItemMasterData supportItem)
    {
        try
        {
            if (supportItem == null)
            {
                return 0f; // 補助材料なしの場合は変化なし
            }

            float modifier = 0f;

            // 成功率増加効果
            if (supportItem.add_enhance_success_rate > 0)
            {
                modifier += supportItem.add_enhance_success_rate;
                Debug.Log($"SuccessRateService: 補助材料による成功率増加 +{supportItem.add_enhance_success_rate}%");
            }

            // 成功率減少効果（デバフ効果のある補助材料用）
            if (supportItem.reduce_enhance_success_rate > 0)
            {
                modifier -= supportItem.reduce_enhance_success_rate;
                Debug.Log($"SuccessRateService: 補助材料による成功率減少 -{supportItem.reduce_enhance_success_rate}%");
            }

            return modifier;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SuccessRateService: 補助材料効果計算エラー - {ex.Message}");
            return 0f;
        }
    }

    /// <summary>
    /// UI表示用の成功率テキスト取得
    /// </summary>
    public string GetSuccessRateDisplayText(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        try
        {
            if (equipment == null || enhanceItem == null)
            {
                return "--";
            }

            float successRate = CalculateFinalSuccessRate(equipment, enhanceItem, supportItem);
            return $"{successRate:F1}";
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SuccessRateService: 表示テキスト生成エラー - {ex.Message}");
            return "ERROR";
        }
    }

    /// <summary>
    /// 成功率の詳細情報取得（デバッグ用）
    /// </summary>
    public SuccessRateBreakdown GetSuccessRateBreakdown(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        try
        {
            if (equipment == null || enhanceItem == null)
            {
                return new SuccessRateBreakdown();
            }

            SuccessRateBreakdown breakdown = new SuccessRateBreakdown();

            breakdown.BaseRate = enhanceItem.enhance_success_rate;
            breakdown.EnhanceValuePenalty = CalculateEnhanceValuePenalty(equipment.current_enhanced_value);
            breakdown.SupportItemModifier = CalculateSupportItemModifier(supportItem);
            breakdown.FinalRate = CalculateFinalSuccessRate(equipment, enhanceItem, supportItem);

            return breakdown;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SuccessRateService: 詳細情報取得エラー - {ex.Message}");
            return new SuccessRateBreakdown();
        }
    }

    /// <summary>
    /// 強化可能な最低成功率チェック
    /// </summary>
    public bool IsSuccessRateAcceptable(float successRate, float minimumRate = 1f)
    {
        return successRate >= minimumRate;
    }

    /// <summary>
    /// 推奨補助材料の判定
    /// </summary>
    public bool ShouldRecommendSupportItem(UserEquipment equipment, EnhanceItemMasterData enhanceItem, float targetSuccessRate = 50f)
    {
        try
        {
            float currentRate = CalculateFinalSuccessRate(equipment, enhanceItem, null);
            return currentRate < targetSuccessRate;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SuccessRateService: 推奨判定エラー - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 成功率段階の説明テキスト取得
    /// </summary>
    public string GetSuccessRateStageDescription(int enhanceValue)
    {
        try
        {
            if (enhanceValue < 5)
            {
                return "成功率減少なし";
            }

            int stage = enhanceValue / 5;
            float penalty = stage * 1f;

            return $"段階{stage} (基本成功率から-{penalty}%)";
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SuccessRateService: 段階説明取得エラー - {ex.Message}");
            return "不明";
        }
    }
}

/// <summary>
/// 成功率の詳細内訳データ（デバッグ・UI表示用）
/// </summary>
[System.Serializable]
public class SuccessRateBreakdown
{
    public float BaseRate;              // 基本成功率
    public float EnhanceValuePenalty;   // 強化値による減少
    public float SupportItemModifier;   // 補助材料による増減
    public float FinalRate;             // 最終成功率

    /// <summary>
    /// デバッグ用文字列表現
    /// </summary>
    public override string ToString()
    {
        return $"基本{BaseRate}% - 減少{EnhanceValuePenalty}% + 補助{SupportItemModifier}% = {FinalRate}%";
    }
}