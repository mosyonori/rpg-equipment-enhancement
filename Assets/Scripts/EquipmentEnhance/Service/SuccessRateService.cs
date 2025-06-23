using System;
using UnityEngine;

/// <summary>
/// 強化成功率の計算のみを担当するサービス
/// </summary>
public class SuccessRateService
{
    /// <summary>
    /// 最終成功率計算
    /// </summary>
    public float CalculateFinalSuccessRate(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        // 1. 基本成功率
        float baseRate = enhanceItem.enhance_success_rate;

        // 2. 強化値による成功率減少
        float enhanceValuePenalty = CalculateEnhanceValuePenalty(equipment.current_enhanced_value);

        // 3. 補助材料による成功率増減
        float supportItemModifier = CalculateSupportItemModifier(supportItem);

        // 4. 最終成功率計算
        float finalRate = baseRate - enhanceValuePenalty + supportItemModifier;

        // 5. 範囲制限（0%～100%）
        return Mathf.Clamp(finalRate, 0f, 100f);
    }

    /// <summary>
    /// 強化値による成功率減少計算
    /// </summary>
    private float CalculateEnhanceValuePenalty(int currentEnhanceValue)
    {
        // 段階的な成功率減少
        if (currentEnhanceValue < 5)
        {
            return 0f; // +4まではペナルティなし
        }
        else if (currentEnhanceValue < 10)
        {
            return (currentEnhanceValue - 4) * 5f; // +5から1段階につき-5%
        }
        else if (currentEnhanceValue < 15)
        {
            return 25f + (currentEnhanceValue - 9) * 10f; // +10から1段階につき-10%
        }
        else
        {
            return 75f + (currentEnhanceValue - 14) * 15f; // +15から1段階につき-15%
        }
    }

    /// <summary>
    /// 補助材料による成功率修正
    /// </summary>
    private float CalculateSupportItemModifier(SupportItemMasterData supportItem)
    {
        if (supportItem == null)
        {
            return 0f;
        }

        return supportItem.add_enhance_success_rate;
    }

    /// <summary>
    /// 成功率の表示用テキスト取得
    /// </summary>
    public string GetSuccessRateDisplayText(UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        float successRate = CalculateFinalSuccessRate(equipment, enhanceItem, supportItem);
        return $"{successRate:F1}%";
    }

    /// <summary>
    /// 成功判定
    /// </summary>
    public bool DetermineSuccess(float successRate)
    {
        float randomValue = UnityEngine.Random.Range(0f, 100f);
        return randomValue <= successRate;
    }
}