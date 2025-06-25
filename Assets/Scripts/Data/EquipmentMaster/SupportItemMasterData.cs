using UnityEngine;

[CreateAssetMenu(fileName = "SupportItem_", menuName = "GameData/Support Item Master Data")]
public class SupportItemMasterData : ScriptableObject
{
    [Header("基本情報")]
    public int supportItemId;
    public string supportItemName;
    public AttributeType attributeType;
    public RarityType rarity;

    [Header("使用設定")]
    [Tooltip("無限使用可能かどうか")]
    public bool infiniteUse;
    public int maxStackValue;

    [Header("強化値効果")]
    [Tooltip("強化値を加算する数値")]
    public int addEnhancedValue;
    [Tooltip("強化値を乗算する倍率")]
    public int multiplEnhancedValue;
    [Tooltip("強化値を減算する数値")]
    public int reduceEnhancedValue;

    [Header("強化耐久値効果")]
    [Tooltip("強化耐久値を増加させる数値")]
    public int addEnhanceStamina;
    [Tooltip("強化耐久値を減少させる数値")]
    public int reduceEnhanceStamina;

    [Header("強化成功率効果")]
    [Tooltip("強化成功率を増加させる数値（%）")]
    public int addEnhanceSuccessRate;
    [Tooltip("強化成功率を減少させる数値（%）")]
    public int reduceEnhanceSuccessRate;

    [Header("ステータス効果")]
    [Tooltip("ステータス上昇効果の倍率")]
    public int multiplStatusUp;
    public int hp;
    public int offense;
    public int defense;
    public int speed;
    public int criticalRate;
    public int criticalDamageRate;
    public int fireOffence;
    public int waterOffence;
    public int windOffence;
    public int earthOffence;

    [Header("表示設定")]
    public Sprite supportItemIcon;
    public string supportItemIconPath;
    [TextArea(3, 5)]
    public string description;

    [Header("フラグ")]
    public bool completionFlag;
    public bool collectionFlag;

    /// <summary>
    /// 強化アイテムの効果にこの補助アイテムの効果を適用
    /// </summary>
    public EnhanceEffect ApplyEffect(EnhanceEffect baseEffect)
    {
        EnhanceEffect result = baseEffect;

        // 乗算効果を適用
        if (multiplStatusUp > 1)
        {
            result.hp *= multiplStatusUp;
            result.offense *= multiplStatusUp;
            result.defense *= multiplStatusUp;
            result.speed *= multiplStatusUp;
            result.criticalRate *= multiplStatusUp;
            result.criticalDamageRate *= multiplStatusUp;
            result.fireOffence *= multiplStatusUp;
            result.waterOffence *= multiplStatusUp;
            result.windOffence *= multiplStatusUp;
            result.earthOffence *= multiplStatusUp;
        }

        // 加算効果を適用
        result.hp += hp;
        result.offense += offense;
        result.defense += defense;
        result.speed += speed;
        result.criticalRate += criticalRate;
        result.criticalDamageRate += criticalDamageRate;
        result.fireOffence += fireOffence;
        result.waterOffence += waterOffence;
        result.windOffence += windOffence;
        result.earthOffence += earthOffence;

        return result;
    }

    /// <summary>
    /// 強化値への効果を計算
    /// </summary>
    public int CalculateEnhancedValueEffect(int baseEnhancedValue)
    {
        int result = baseEnhancedValue;

        // 乗算効果を適用
        if (multiplEnhancedValue > 1)
        {
            result *= multiplEnhancedValue;
        }

        // 加算・減算効果を適用
        result += addEnhancedValue;
        result -= reduceEnhancedValue;

        return result;
    }

    /// <summary>
    /// 強化成功率への効果を計算
    /// </summary>
    public int CalculateSuccessRateEffect(int baseSuccessRate)
    {
        int result = baseSuccessRate;
        result += addEnhanceSuccessRate;
        result -= reduceEnhanceSuccessRate;
        return Mathf.Clamp(result, 0, 100);
    }

    /// <summary>
    /// 強化耐久値への効果を計算
    /// </summary>
    public int CalculateStaminaEffect(int baseStaminaChange)
    {
        int result = baseStaminaChange;
        result += addEnhanceStamina;
        result -= reduceEnhanceStamina;
        return result;
    }
}