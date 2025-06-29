using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 装備強化実行前のプレビュー情報を格納するデータクラス
/// 強化実行前にユーザーに表示する予測情報を保持
/// </summary>
[System.Serializable]
public class EnhancePreviewData
{
    [Header("成功率情報")]
    public float finalSuccessRate;                  // 最終的な成功率（%）
    public float baseSuccessRate;                   // 基本成功率（%）
    public float enhanceValuePenalty;               // 強化値によるペナルティ（%）
    public float supportItemBonus;                  // 補助材料によるボーナス（%）

    [Header("予想される変化")]
    public int currentEnhancedValue;                // 現在の強化値
    public int expectedEnhancedValue;               // 成功時の予想強化値
    public AttributeType currentAttributeType;      // 現在の属性
    public AttributeType expectedAttributeType;     // 成功時の予想属性
    public int currentEnhanceStamina;               // 現在の耐久値
    public int expectedEnhanceStamina;              // 強化後の予想耐久値;     // 成功時の予想属性

    [Header("ステータス予測")]
    public Dictionary<string, int> currentStatuses;     // 現在のステータス
    public Dictionary<string, int> expectedStatusIncrease; // 予想ステータス増加量
    public Dictionary<string, int> expectedTotalStatuses;  // 予想総ステータス

    [Header("リスク・警告")]
    public bool canEnhance;                         // 強化実行可能フラグ
    public bool hasRisk;                           // リスクが存在するかどうか
    public List<string> warningMessages;           // 警告メッセージリスト
    public List<string> riskMessages;              // リスクメッセージリスト

    [Header("コスト情報")]
    public List<ItemUsageData> requiredItems;      // 必要なアイテム情報
    public bool hasEnoughItems;                    // 必要アイテムが足りているか

    [Header("特殊効果")]
    public bool willAttributeChange;               // 属性が変化するかどうか
    public bool willStaminaDecrease;               // 耐久値が減少するかどうか
    public bool isStaminaRestoration;              // 耐久値復元かどうか
    public bool isEnhanceReset;                    // 強化値リセットかどうか

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public EnhancePreviewData()
    {
        currentStatuses = new Dictionary<string, int>();
        expectedStatusIncrease = new Dictionary<string, int>();
        expectedTotalStatuses = new Dictionary<string, int>();
        warningMessages = new List<string>();
        riskMessages = new List<string>();
        requiredItems = new List<ItemUsageData>();
        canEnhance = false;
        hasEnoughItems = false;
    }

    /// <summary>
    /// 現在のステータスを設定
    /// </summary>
    /// <param name="statusName">ステータス名</param>
    /// <param name="currentValue">現在の値</param>
    public void SetCurrentStatus(string statusName, int currentValue)
    {
        currentStatuses[statusName] = currentValue;
    }

    /// <summary>
    /// 予想ステータス増加量を設定
    /// </summary>
    /// <param name="statusName">ステータス名</param>
    /// <param name="increaseValue">増加量</param>
    public void SetExpectedStatusIncrease(string statusName, int increaseValue)
    {
        expectedStatusIncrease[statusName] = increaseValue;

        // 総ステータスも計算
        int currentValue = currentStatuses.ContainsKey(statusName) ? currentStatuses[statusName] : 0;
        expectedTotalStatuses[statusName] = currentValue + increaseValue;
    }

    /// <summary>
    /// 警告メッセージを追加
    /// </summary>
    /// <param name="message">警告メッセージ</param>
    public void AddWarningMessage(string message)
    {
        if (!string.IsNullOrEmpty(message) && !warningMessages.Contains(message))
        {
            warningMessages.Add(message);
        }
    }

    /// <summary>
    /// リスクメッセージを追加
    /// </summary>
    /// <param name="message">リスクメッセージ</param>
    public void AddRiskMessage(string message)
    {
        if (!string.IsNullOrEmpty(message) && !riskMessages.Contains(message))
        {
            riskMessages.Add(message);
            hasRisk = true;
        }
    }

    /// <summary>
    /// 必要アイテムを追加
    /// </summary>
    /// <param name="itemUsage">必要なアイテム情報</param>
    public void AddRequiredItem(ItemUsageData itemUsage)
    {
        if (itemUsage != null)
        {
            requiredItems.Add(itemUsage);
        }
    }

    /// <summary>
    /// 成功率の詳細情報を取得
    /// </summary>
    /// <returns>成功率の詳細文字列</returns>
    public string GetSuccessRateDetails()
    {
        var details = $"基本成功率: {baseSuccessRate:F1}%";

        if (enhanceValuePenalty > 0)
        {
            details += $"\n強化値ペナルティ: -{enhanceValuePenalty:F1}%";
        }

        if (supportItemBonus > 0)
        {
            details += $"\n補助材料ボーナス: +{supportItemBonus:F1}%";
        }

        details += $"\n最終成功率: {finalSuccessRate:F1}%";

        return details;
    }

    /// <summary>
    /// ステータス変化の詳細情報を取得
    /// </summary>
    /// <returns>ステータス変化の詳細文字列</returns>
    public string GetStatusChangeDetails()
    {
        var details = "";

        foreach (var kvp in expectedStatusIncrease)
        {
            if (kvp.Value != 0)
            {
                var currentValue = currentStatuses.ContainsKey(kvp.Key) ? currentStatuses[kvp.Key] : 0;
                var newValue = expectedTotalStatuses.ContainsKey(kvp.Key) ? expectedTotalStatuses[kvp.Key] : currentValue;

                details += $"{kvp.Key}: {currentValue} → {newValue} (+{kvp.Value})\n";
            }
        }

        return details.TrimEnd('\n');
    }

    /// <summary>
    /// 属性変化があるかどうか
    /// </summary>
    /// <returns>属性変化がある場合true</returns>
    public bool HasAttributeChange()
    {
        return currentAttributeType != expectedAttributeType;
    }

    /// <summary>
    /// 強化値変化があるかどうか
    /// </summary>
    /// <returns>強化値変化がある場合true</returns>
    public bool HasEnhanceValueChange()
    {
        return currentEnhancedValue != expectedEnhancedValue;
    }

    /// <summary>
    /// 耐久値変化があるかどうか
    /// </summary>
    /// <returns>耐久値変化がある場合true</returns>
    public bool HasStaminaChange()
    {
        return currentEnhanceStamina != expectedEnhanceStamina;
    }

    /// <summary>
    /// 強化実行可能かどうかの総合判定
    /// </summary>
    /// <returns>強化実行可能な場合true</returns>
    public bool CanExecuteEnhance()
    {
        return canEnhance && hasEnoughItems && currentEnhanceStamina > 0;
    }

    /// <summary>
    /// プレビューデータの妥当性をチェック
    /// </summary>
    /// <returns>データが妥当な場合true</returns>
    public bool IsValid()
    {
        return finalSuccessRate >= 0 && finalSuccessRate <= 100 &&
               currentEnhancedValue >= 0 &&
               expectedEnhancedValue >= 0 &&
               currentEnhanceStamina >= 0 &&
               expectedEnhanceStamina >= 0;
    }

    /// <summary>
    /// デバッグ用の文字列表現
    /// </summary>
    /// <returns>プレビューの詳細情報</returns>
    public override string ToString()
    {
        return $"EnhancePreview: " +
               $"SuccessRate: {finalSuccessRate:F1}% | " +
               $"EnhanceValue: {currentEnhancedValue} → {expectedEnhancedValue} | " +
               $"Attribute: {currentAttributeType} → {expectedAttributeType} | " +
               $"Stamina: {currentEnhanceStamina} → {expectedEnhanceStamina} | " +
               $"CanEnhance: {CanExecuteEnhance()}";
    }
}