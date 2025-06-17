using UnityEngine;

[System.Serializable]
public class EnhancementBonus
{
    [Header("強化後の追加量")]
    public int enhancementValue = 1; // ★重要: 強化値の増加量（デフォルト1）
    public int attackPower;
    public int defensePower;
    public int elementalAttack;  // 汎用属性攻撃（互換性保持）
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

/// <summary>
/// ★新機能：装備種類別の強化効果
/// </summary>
[System.Serializable]
public class EquipmentTypeBonus
{
    [Header("武器用効果")]
    public EnhancementBonus weaponBonus;

    [Header("防具用効果")]
    public EnhancementBonus armorBonus;

    [Header("アクセサリー用効果")]
    public EnhancementBonus accessoryBonus;
}

[CreateAssetMenu(fileName = "NewEnhancementItem", menuName = "GameData/EnhancementItem")]
public class EnhancementItemData : ScriptableObject
{
    [Header("基本情報")]
    public int itemId;
    public string itemName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;

    [Header("アイテムタイプ")]
    public EnhancementItemType itemType;

    [Header("強化設定")]
    [Range(0f, 1f)]
    public float successRate = 0.5f; // このアイテム固有の成功率

    [Range(0f, 1f)]
    public float greatSuccessRate = 0.1f; // 大成功確率

    [Header("★新機能：装備種類別効果設定")]
    [Tooltip("ONにすると装備種類によって異なる効果を適用します")]
    public bool useEquipmentTypeSpecificBonus = false;

    [Header("装備種類別強化効果")]
    public EquipmentTypeBonus equipmentTypeBonus;

    [Header("従来の強化効果（装備種類別がOFFの場合使用）")]
    public EnhancementBonus bonus;

    [Header("レアリティ")]
    public ItemRarity rarity;

    [Header("UI表示用テキスト")]
    [Tooltip("効果内容の表示テキスト（空の場合は自動生成）")]
    [TextArea(2, 4)]
    public string effectDisplayText;

    [Header("エフェクト設定")]
    public GameObject enhanceEffectPrefab; // 強化エフェクト
    public AudioClip enhanceSound; // 強化音

    #region ★新機能：装備種類別効果取得メソッド

    /// <summary>
    /// 指定された装備種類に対応する強化効果を取得
    /// </summary>
    public EnhancementBonus GetBonusForEquipmentType(EquipmentType equipmentType)
    {
        if (!useEquipmentTypeSpecificBonus)
        {
            // 従来の単一効果を使用
            return bonus;
        }

        switch (equipmentType)
        {
            case EquipmentType.Weapon:
                return equipmentTypeBonus.weaponBonus;
            case EquipmentType.Armor:
                return equipmentTypeBonus.armorBonus;
            case EquipmentType.Accessory:
                return equipmentTypeBonus.accessoryBonus;
            default:
                Debug.LogWarning($"未対応の装備種類: {equipmentType}");
                return bonus; // フォールバック
        }
    }

    /// <summary>
    /// 装備種類別の効果説明を取得
    /// </summary>
    public string GetEffectDescriptionForEquipmentType(EquipmentType equipmentType)
    {
        var targetBonus = GetBonusForEquipmentType(equipmentType);
        return GenerateEffectDescription(targetBonus, equipmentType);
    }

    /// <summary>
    /// 装備選択に関係なく全装備種類の効果を表示（管理画面用）
    /// </summary>
    public string GetAllEquipmentTypesEffectDescription()
    {
        if (!useEquipmentTypeSpecificBonus)
        {
            return GetEffectDescription();
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine("【武器用効果】");
        sb.AppendLine(GenerateEffectDescription(equipmentTypeBonus.weaponBonus, EquipmentType.Weapon));

        sb.AppendLine("\n【防具用効果】");
        sb.AppendLine(GenerateEffectDescription(equipmentTypeBonus.armorBonus, EquipmentType.Armor));

        sb.AppendLine("\n【アクセサリー用効果】");
        sb.AppendLine(GenerateEffectDescription(equipmentTypeBonus.accessoryBonus, EquipmentType.Accessory));

        return sb.ToString().Trim();
    }

    /// <summary>
    /// 指定されたボーナスから効果説明を生成
    /// </summary>
    private string GenerateEffectDescription(EnhancementBonus targetBonus, EquipmentType equipmentType)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // ★重要: 強化値の表示
        if (targetBonus.enhancementValue > 0)
            sb.AppendLine($"強化値 +{targetBonus.enhancementValue}");

        if (targetBonus.attackPower > 0)
            sb.AppendLine($"攻撃力 +{targetBonus.attackPower}");

        if (targetBonus.defensePower > 0)
            sb.AppendLine($"防御力 +{targetBonus.defensePower}");

        if (targetBonus.elementalAttack > 0)
            sb.AppendLine($"属性攻撃 +{targetBonus.elementalAttack}");

        // 4種類の属性攻撃
        if (targetBonus.fireAttack > 0)
            sb.AppendLine($"火属性攻撃 +{targetBonus.fireAttack}");

        if (targetBonus.waterAttack > 0)
            sb.AppendLine($"水属性攻撃 +{targetBonus.waterAttack}");

        if (targetBonus.windAttack > 0)
            sb.AppendLine($"風属性攻撃 +{targetBonus.windAttack}");

        if (targetBonus.earthAttack > 0)
            sb.AppendLine($"土属性攻撃 +{targetBonus.earthAttack}");

        if (targetBonus.hp > 0)
            sb.AppendLine($"HP +{targetBonus.hp}");

        if (targetBonus.criticalRate > 0)
            sb.AppendLine($"クリティカル率 +{targetBonus.criticalRate:F1}%");

        if (sb.Length == 0)
            return "効果なし";

        return sb.ToString().Trim();
    }

    #endregion

    #region 属性判定システム（装備種類対応版）

    /// <summary>
    /// この強化アイテムが持つ属性タイプを取得（装備種類考慮）
    /// </summary>
    public ElementalType GetElementalType(EquipmentType equipmentType = EquipmentType.Weapon)
    {
        var targetBonus = GetBonusForEquipmentType(equipmentType);

        // 属性攻撃ボーナスがある場合、その属性を返す
        if (targetBonus.fireAttack > 0) return ElementalType.Fire;
        if (targetBonus.waterAttack > 0) return ElementalType.Water;
        if (targetBonus.windAttack > 0) return ElementalType.Wind;
        if (targetBonus.earthAttack > 0) return ElementalType.Earth;

        return ElementalType.None; // 属性攻撃を持たない強化アイテム
    }

    /// <summary>
    /// 指定した装備に対してこの強化アイテムが使用可能かチェック（装備種類考慮）
    /// </summary>
    public bool CanUseOnEquipment(UserEquipment equipment)
    {
        if (equipment == null) return false;

        // 装備のマスターデータを取得して装備種類を確認
        var equipmentData = DataManager.Instance?.GetEquipmentData(equipment.equipmentId);
        if (equipmentData == null) return false;

        ElementalType itemType = GetElementalType(equipmentData.equipmentType);
        return equipment.CanUseElementalEnhancement(itemType);
    }

    /// <summary>
    /// 装備に使用できない理由を取得（UI表示用）
    /// </summary>
    public string GetRestrictionReason(UserEquipment equipment)
    {
        if (equipment == null) return "装備が選択されていません";

        if (CanUseOnEquipment(equipment)) return "";

        var equipmentData = DataManager.Instance?.GetEquipmentData(equipment.equipmentId);
        ElementalType itemType = GetElementalType(equipmentData?.equipmentType ?? EquipmentType.Weapon);
        return equipment.GetElementalRestrictionReason(itemType);
    }

    #endregion

    #region 既存のメソッド（装備種類対応版に修正）

    /// <summary>
    /// 強化レベルに応じた成功確率を計算
    /// </summary>
    public float GetAdjustedSuccessRate(int enhancementLevel)
    {
        // 強化レベルが上がるほど成功確率が微量に下がる
        float penalty = enhancementLevel * 0.01f; // 1レベルごとに1%減少
        return Mathf.Max(0.1f, successRate - penalty); // 最低10%は保証
    }

    /// <summary>
    /// 強化レベルに応じた大成功確率を計算
    /// </summary>
    public float GetAdjustedGreatSuccessRate(int enhancementLevel)
    {
        // 大成功確率は強化レベルの影響を受けにくい
        float penalty = enhancementLevel * 0.005f; // 1レベルごとに0.5%減少
        return Mathf.Max(0.01f, greatSuccessRate - penalty); // 最低1%は保証
    }

    /// <summary>
    /// ボーナス値を取得（装備種類指定版）
    /// </summary>
    public int GetEnhancementValue(EquipmentType equipmentType = EquipmentType.Weapon)
    {
        return GetBonusForEquipmentType(equipmentType).enhancementValue;
    }

    public int GetAttackPowerBonus(EquipmentType equipmentType = EquipmentType.Weapon)
    {
        return GetBonusForEquipmentType(equipmentType).attackPower;
    }

    public int GetDefensePowerBonus(EquipmentType equipmentType = EquipmentType.Weapon)
    {
        return GetBonusForEquipmentType(equipmentType).defensePower;
    }

    public int GetElementalAttackBonus(EquipmentType equipmentType = EquipmentType.Weapon)
    {
        return GetBonusForEquipmentType(equipmentType).elementalAttack;
    }

    public int GetHPBonus(EquipmentType equipmentType = EquipmentType.Weapon)
    {
        return GetBonusForEquipmentType(equipmentType).hp;
    }

    public float GetCriticalRateBonus(EquipmentType equipmentType = EquipmentType.Weapon)
    {
        return GetBonusForEquipmentType(equipmentType).criticalRate;
    }

    public int GetDurabilityReduction(EquipmentType equipmentType = EquipmentType.Weapon)
    {
        return GetBonusForEquipmentType(equipmentType).durabilityReduction;
    }

    /// <summary>
    /// 属性攻撃ボーナス取得メソッド（装備種類指定版）
    /// </summary>
    public int GetFireAttackBonus(EquipmentType equipmentType = EquipmentType.Weapon)
    {
        return GetBonusForEquipmentType(equipmentType).fireAttack;
    }

    public int GetWaterAttackBonus(EquipmentType equipmentType = EquipmentType.Weapon)
    {
        return GetBonusForEquipmentType(equipmentType).waterAttack;
    }

    public int GetWindAttackBonus(EquipmentType equipmentType = EquipmentType.Weapon)
    {
        return GetBonusForEquipmentType(equipmentType).windAttack;
    }

    public int GetEarthAttackBonus(EquipmentType equipmentType = EquipmentType.Weapon)
    {
        return GetBonusForEquipmentType(equipmentType).earthAttack;
    }

    /// <summary>
    /// 従来互換性のためのメソッド（武器として扱う）
    /// </summary>
    public int GetEnhancementValue() => GetEnhancementValue(EquipmentType.Weapon);
    public int GetAttackPowerBonus() => GetAttackPowerBonus(EquipmentType.Weapon);
    public int GetDefensePowerBonus() => GetDefensePowerBonus(EquipmentType.Weapon);
    public int GetElementalAttackBonus() => GetElementalAttackBonus(EquipmentType.Weapon);
    public int GetHPBonus() => GetHPBonus(EquipmentType.Weapon);
    public float GetCriticalRateBonus() => GetCriticalRateBonus(EquipmentType.Weapon);
    public int GetDurabilityReduction() => GetDurabilityReduction(EquipmentType.Weapon);
    public int GetFireAttackBonus() => GetFireAttackBonus(EquipmentType.Weapon);
    public int GetWaterAttackBonus() => GetWaterAttackBonus(EquipmentType.Weapon);
    public int GetWindAttackBonus() => GetWindAttackBonus(EquipmentType.Weapon);
    public int GetEarthAttackBonus() => GetEarthAttackBonus(EquipmentType.Weapon);

    /// <summary>
    /// 効果説明を生成（装備選択状態に応じて適応的に表示）
    /// </summary>
    public string GetEffectDescription()
    {
        // カスタムテキストが設定されていればそれを使用
        if (!string.IsNullOrEmpty(effectDisplayText))
            return effectDisplayText;

        // 装備種類別効果が有効でない場合は従来の説明を返す
        if (!useEquipmentTypeSpecificBonus)
        {
            return GenerateEffectDescription(bonus, EquipmentType.Weapon);
        }

        // 装備種類別効果が有効な場合は基本説明（description）を返す
        // UI側で装備選択時に適切な効果を表示する
        return description;
    }

    /// <summary>
    /// 詳細な効果説明を取得（装備種類指定版）
    /// </summary>
    public string GetDetailedEffectDescription(UserEquipment targetEquipment = null)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (targetEquipment != null)
        {
            var equipmentData = DataManager.Instance?.GetEquipmentData(targetEquipment.equipmentId);
            if (equipmentData != null)
            {
                // 装備種類に応じた効果を表示
                sb.AppendLine($"【{GetEquipmentTypeName(equipmentData.equipmentType)}用効果】");
                sb.AppendLine(GetEffectDescriptionForEquipmentType(equipmentData.equipmentType));
            }
            else
            {
                sb.AppendLine(GetEffectDescription());
            }

            // 属性制限情報
            string restriction = GetRestrictionReason(targetEquipment);
            if (!string.IsNullOrEmpty(restriction))
            {
                sb.AppendLine($"⚠️ {restriction}");
            }

            // 成功率情報
            float adjustedRate = GetAdjustedSuccessRate(targetEquipment.enhancementLevel);
            sb.AppendLine($"成功率: {adjustedRate * 100:F1}%");

            // 耐久消費
            var equipmentData2 = DataManager.Instance?.GetEquipmentData(targetEquipment.equipmentId);
            int durabilityReduction = GetDurabilityReduction(equipmentData2?.equipmentType ?? EquipmentType.Weapon);
            sb.AppendLine($"消費耐久: {durabilityReduction}");
        }
        else
        {
            sb.AppendLine(GetEffectDescription());
            sb.AppendLine($"基本成功率: {successRate * 100:F0}%");
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// 装備種類の日本語名を取得
    /// </summary>
    private string GetEquipmentTypeName(EquipmentType type)
    {
        switch (type)
        {
            case EquipmentType.Weapon: return "武器";
            case EquipmentType.Armor: return "防具";
            case EquipmentType.Accessory: return "アクセサリー";
            default: return "不明";
        }
    }

    /// <summary>
    /// レアリティに応じた色を取得
    /// </summary>
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

    #endregion

    #region Unity Editor用

    /// <summary>
    /// アイテムタイプに応じたボーナス値調整
    /// </summary>
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

        // 装備種類別効果の整合性チェック
        if (useEquipmentTypeSpecificBonus)
        {
            ValidateEquipmentTypeEffects();
        }
    }

    /// <summary>
    /// 装備種類別効果の整合性をチェック
    /// </summary>
    private void ValidateEquipmentTypeEffects()
    {
        // 各装備種類に対して属性攻撃の整合性をチェック
        ValidateElementalAttacks(equipmentTypeBonus.weaponBonus, "武器");
        ValidateElementalAttacks(equipmentTypeBonus.armorBonus, "防具");
        ValidateElementalAttacks(equipmentTypeBonus.accessoryBonus, "アクセサリー");
    }

    /// <summary>
    /// 属性攻撃の整合性をチェック
    /// </summary>
    private void ValidateElementalAttacks(EnhancementBonus targetBonus, string equipmentTypeName)
    {
        int elementalCount = 0;
        if (targetBonus.fireAttack > 0) elementalCount++;
        if (targetBonus.waterAttack > 0) elementalCount++;
        if (targetBonus.windAttack > 0) elementalCount++;
        if (targetBonus.earthAttack > 0) elementalCount++;

        // 複数の属性攻撃が設定されている場合は警告
        if (elementalCount > 1)
        {
            Debug.LogWarning($"[{itemName}] {equipmentTypeName}用効果で複数の属性攻撃が設定されています。最初に見つかった属性のみが有効になります。", this);
        }
    }

    #endregion
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