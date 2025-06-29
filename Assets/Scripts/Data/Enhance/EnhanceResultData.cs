using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 装備強化実行結果を格納するデータクラス
/// 強化実行後の結果情報を保持し、UIへの結果表示やログ記録に使用される
/// </summary>
[System.Serializable]
public class EnhanceResultData
{
    [Header("基本情報")]
    public bool isSuccess;                          // 強化成功フラグ
    public string equipmentId;                      // 対象装備のユーザーID
    public int enhanceItemId;                       // 使用した強化アイテムのマスターID
    public int supportItemId;                       // 使用した補助材料のマスターID（0の場合は未使用）

    [Header("強化前後の状態")]
    public int previousEnhancedValue;               // 強化前の強化値（currentEnhancedValue）
    public int newEnhancedValue;                    // 強化後の強化値（currentEnhancedValue）
    public AttributeType previousAttributeType;     // 強化前の属性（currentAttributeType）
    public AttributeType newAttributeType;          // 強化後の属性（currentAttributeType）
    public int previousEnhanceStamina;              // 強化前の耐久値（currentEnhanceStamina）
    public int newEnhanceStamina;                   // 強化後の耐久値（currentEnhanceStamina）

    [Header("ステータス変化")]
    public Dictionary<string, int> statusChanges;   // ステータス変化量（キー: ステータス名, 値: 変化量）
    public Dictionary<string, int> totalStatuses;   // 強化後の総ステータス

    [Header("アイテム使用情報")]
    public List<ItemUsageData> usedItems;           // 使用したアイテムの詳細情報

    [Header("メタ情報")]
    public DateTime enhanceDateTime;                // 強化実行日時
    public float actualSuccessRate;                 // 実際に計算された成功率
    public string resultMessage;                    // 結果メッセージ（成功/失敗の詳細）

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public EnhanceResultData()
    {
        statusChanges = new Dictionary<string, int>();
        totalStatuses = new Dictionary<string, int>();
        usedItems = new List<ItemUsageData>();
        enhanceDateTime = DateTime.Now;
        resultMessage = string.Empty;
    }

    /// <summary>
    /// 成功時の結果データを作成
    /// </summary>
    /// <param name="equipmentId">装備ID</param>
    /// <param name="enhanceItemId">強化アイテムID</param>
    /// <param name="supportItemId">補助材料ID</param>
    /// <param name="previousValue">強化前の強化値</param>
    /// <param name="newValue">強化後の強化値</param>
    /// <param name="previousAttribute">強化前の属性</param>
    /// <param name="newAttribute">強化後の属性</param>
    /// <param name="previousStamina">強化前の耐久値</param>
    /// <param name="newStamina">強化後の耐久値</param>
    /// <param name="successRate">成功率</param>
    /// <returns>成功時の結果データ</returns>
    public static EnhanceResultData CreateSuccessResult(
        string equipmentId,
        int enhanceItemId,
        int supportItemId,
        int previousValue,
        int newValue,
        AttributeType previousAttribute,
        AttributeType newAttribute,
        int previousStamina,
        int newStamina,
        float successRate)
    {
        var result = new EnhanceResultData
        {
            isSuccess = true,
            equipmentId = equipmentId,
            enhanceItemId = enhanceItemId,
            supportItemId = supportItemId,
            previousEnhancedValue = previousValue,
            newEnhancedValue = newValue,
            previousAttributeType = previousAttribute,
            newAttributeType = newAttribute,
            previousEnhanceStamina = previousStamina,
            newEnhanceStamina = newStamina,
            actualSuccessRate = successRate,
            resultMessage = "強化に成功しました！"
        };

        return result;
    }

    /// <summary>
    /// 失敗時の結果データを作成
    /// </summary>
    /// <param name="equipmentId">装備ID</param>
    /// <param name="enhanceItemId">強化アイテムID</param>
    /// <param name="supportItemId">補助材料ID</param>
    /// <param name="currentValue">現在の強化値</param>
    /// <param name="currentAttribute">現在の属性</param>
    /// <param name="previousStamina">強化前の耐久値</param>
    /// <param name="newStamina">強化後の耐久値</param>
    /// <param name="successRate">成功率</param>
    /// <returns>失敗時の結果データ</returns>
    public static EnhanceResultData CreateFailureResult(
        string equipmentId,
        int enhanceItemId,
        int supportItemId,
        int currentValue,
        AttributeType currentAttribute,
        int previousStamina,
        int newStamina,
        float successRate)
    {
        var result = new EnhanceResultData
        {
            isSuccess = false,
            equipmentId = equipmentId,
            enhanceItemId = enhanceItemId,
            supportItemId = supportItemId,
            previousEnhancedValue = currentValue,
            newEnhancedValue = currentValue, // 失敗時は変化なし
            previousAttributeType = currentAttribute,
            newAttributeType = currentAttribute, // 失敗時は変化なし
            previousEnhanceStamina = previousStamina,
            newEnhanceStamina = newStamina,
            actualSuccessRate = successRate,
            resultMessage = "強化に失敗しました..."
        };

        return result;
    }

    /// <summary>
    /// ステータス変化を追加
    /// </summary>
    /// <param name="statusName">ステータス名</param>
    /// <param name="changeAmount">変化量</param>
    /// <param name="totalAmount">変化後の総量</param>
    public void AddStatusChange(string statusName, int changeAmount, int totalAmount)
    {
        if (statusChanges.ContainsKey(statusName))
        {
            statusChanges[statusName] += changeAmount;
        }
        else
        {
            statusChanges[statusName] = changeAmount;
        }

        totalStatuses[statusName] = totalAmount;
    }

    /// <summary>
    /// 使用アイテム情報を追加
    /// </summary>
    /// <param name="itemUsage">使用アイテム情報</param>
    public void AddUsedItem(ItemUsageData itemUsage)
    {
        if (itemUsage != null)
        {
            usedItems.Add(itemUsage);
        }
    }

    /// <summary>
    /// 強化値が変化したかどうか
    /// </summary>
    /// <returns>強化値に変化があった場合true</returns>
    public bool HasEnhanceValueChanged()
    {
        return previousEnhancedValue != newEnhancedValue;
    }

    /// <summary>
    /// 属性が変化したかどうか
    /// </summary>
    /// <returns>属性に変化があった場合true</returns>
    public bool HasAttributeChanged()
    {
        return previousAttributeType != newAttributeType;
    }

    /// <summary>
    /// 耐久値が変化したかどうか
    /// </summary>
    /// <returns>耐久値に変化があった場合true</returns>
    public bool HasStaminaChanged()
    {
        return previousEnhanceStamina != newEnhanceStamina;
    }

    /// <summary>
    /// デバッグ用の文字列表現
    /// </summary>
    /// <returns>結果の詳細情報</returns>
    public override string ToString()
    {
        return $"EnhanceResult: {(isSuccess ? "Success" : "Failure")} | " +
               $"Equipment: {equipmentId} | " +
               $"EnhanceValue: {previousEnhancedValue} → {newEnhancedValue} | " +
               $"Attribute: {previousAttributeType} → {newAttributeType} | " +
               $"Stamina: {previousEnhanceStamina} → {newEnhanceStamina} | " +
               $"SuccessRate: {actualSuccessRate:F1}%";
    }
}