using UnityEngine;

[System.Serializable]
public class EnhancementBonus
{
    [Header("強化時の増加量")]
    public int enhancementValue;
    public int attackPower;
    public int defensePower;
    public int elementalAttack;  // 汎用属性攻撃（互換性維持）
    public int hp;
    public float criticalRate;

    [Header("属性攻撃")]
    public int fireAttack;       // 火属性攻撃
    public int waterAttack;      // 水属性攻撃  
    public int windAttack;       // 風属性攻撃
    public int earthAttack;      // 土属性攻撃

    [Header("コスト")]
    public int durabilityReduction = 1; // 強化耐久減少量（デフォルト1）
}

[CreateAssetMenu(fileName = "NewEnhancementItem", menuName = "GameData/EnhancementItem")]
public class EnhancementItemData : ScriptableObject
{
    [Header("基本情報")]
    public int itemId;
    public string itemName;
    public string description;
    public Sprite icon;

    [Header("アイテムタイプ")]
    public EnhancementItemType itemType;

    [Header("強化設定")]
    [Range(0f, 1f)]
    public float successRate = 0.5f; // このアイテム固有の成功率

    [Range(0f, 1f)]
    public float greatSuccessRate = 0.1f; // 大成功確率

    [Header("強化効果")]
    public EnhancementBonus bonus;

    [Header("レアリティ")]
    public ItemRarity rarity;

    [Header("エフェクト設定")]
    public GameObject enhanceEffectPrefab; // 強化エフェクト
    public AudioClip enhanceSound; // 強化音

    // ★追加: 強化レベルに応じた成功確率を計算
    public float GetAdjustedSuccessRate(int enhancementLevel)
    {
        // 強化レベルが上がるほど成功確率が微量に下がる
        float penalty = enhancementLevel * 0.01f; // 1レベルごとに1%減少
        return Mathf.Max(0.1f, successRate - penalty); // 最低10%は保証
    }

    // ★追加: 強化レベルに応じた大成功確率を計算
    public float GetAdjustedGreatSuccessRate(int enhancementLevel)
    {
        // 大成功確率は強化レベルの影響を受けにくい
        float penalty = enhancementLevel * 0.005f; // 1レベルごとに0.5%減少
        return Mathf.Max(0.01f, greatSuccessRate - penalty); // 最低1%は保証
    }

    // ★追加: ボーナス値を取得（互換性維持用）
    public int GetAttackPowerBonus()
    {
        return bonus.attackPower;
    }

    public int GetDefensePowerBonus()
    {
        return bonus.defensePower;
    }

    public int GetElementalAttackBonus()
    {
        return bonus.elementalAttack;
    }

    public int GetHPBonus()
    {
        return bonus.hp;
    }

    public float GetCriticalRateBonus()
    {
        return bonus.criticalRate;
    }

    public int GetDurabilityReduction()
    {
        return bonus.durabilityReduction;
    }

    // ★追加: 属性攻撃ボーナス取得メソッド
    public int GetFireAttackBonus()
    {
        return bonus.fireAttack;
    }

    public int GetWaterAttackBonus()
    {
        return bonus.waterAttack;
    }

    public int GetWindAttackBonus()
    {
        return bonus.windAttack;
    }

    public int GetEarthAttackBonus()
    {
        return bonus.earthAttack;
    }

    // ★追加: 効果説明を生成
    public string GetEffectDescription()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (bonus.attackPower > 0)
            sb.AppendLine($"攻撃力 +{bonus.attackPower}");

        if (bonus.defensePower > 0)
            sb.AppendLine($"防御力 +{bonus.defensePower}");

        if (bonus.elementalAttack > 0)
            sb.AppendLine($"属性攻撃 +{bonus.elementalAttack}");

        // ★追加：4種類の属性攻撃
        if (bonus.fireAttack > 0)
            sb.AppendLine($"火属性攻撃 +{bonus.fireAttack}");

        if (bonus.waterAttack > 0)
            sb.AppendLine($"水属性攻撃 +{bonus.waterAttack}");

        if (bonus.windAttack > 0)
            sb.AppendLine($"風属性攻撃 +{bonus.windAttack}");

        if (bonus.earthAttack > 0)
            sb.AppendLine($"土属性攻撃 +{bonus.earthAttack}");

        if (bonus.hp > 0)
            sb.AppendLine($"HP +{bonus.hp}");

        if (bonus.criticalRate > 0)
            sb.AppendLine($"クリティカル率 +{bonus.criticalRate:F1}%");

        if (sb.Length == 0)
            return "効果なし";

        return sb.ToString().Trim();
    }

    // ★追加: レアリティに応じた色を取得
    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return Color.white;
            case ItemRarity.Uncommon:
                return Color.green;
            case ItemRarity.Rare:
                return Color.blue;
            case ItemRarity.Epic:
                return Color.magenta;
            case ItemRarity.Legendary:
                return Color.yellow;
            default:
                return Color.white;
        }
    }

    // ★追加: アイテムタイプに応じたボーナス調整
    private void OnValidate()
    {
        // アイテムタイプに応じてデフォルト値を調整
        switch (itemType)
        {
            case EnhancementItemType.BasicStone:
                if (successRate == 0f) successRate = 0.8f;
                if (greatSuccessRate == 0f) greatSuccessRate = 0.1f;
                break;
            case EnhancementItemType.ElementalStone:
                if (successRate == 0f) successRate = 0.7f;
                if (greatSuccessRate == 0f) greatSuccessRate = 0.15f;
                break;
            case EnhancementItemType.SpecialStone:
                if (successRate == 0f) successRate = 0.6f;
                if (greatSuccessRate == 0f) greatSuccessRate = 0.2f;
                break;
        }

        // レアリティに応じてボーナス値を調整
        float rarityMultiplier = GetRarityMultiplier();
        if (rarityMultiplier > 1f)
        {
            // レアリティが高い場合は効果を強化
            // ただし、既に設定済みの値は変更しない
        }
    }

    private float GetRarityMultiplier()
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return 1f;
            case ItemRarity.Uncommon:
                return 1.2f;
            case ItemRarity.Rare:
                return 1.5f;
            case ItemRarity.Epic:
                return 2f;
            case ItemRarity.Legendary:
                return 3f;
            default:
                return 1f;
        }
    }
}

public enum EnhancementItemType
{
    BasicStone,      // 基本強化石
    ElementalStone,  // 属性強化石
    SpecialStone     // 特殊強化石
}

public enum ItemRarity
{
    Common,    // 白
    Uncommon,  // 緑
    Rare,      // 青
    Epic,      // 紫
    Legendary  // 金
}