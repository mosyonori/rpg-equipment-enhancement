using UnityEngine;

[System.Serializable]
public class SupportBonus
{
    [Header("追加強化効果")]
    public int bonusAttackPower;
    public int bonusDefensePower;
    public int bonusElementalAttack;
    public int bonusHP;
    public float bonusCriticalRate;

    [Header("耐久保護")]
    public float durabilityProtection; // 0.0f〜1.0f で0%〜100%保護
}

[CreateAssetMenu(fileName = "NewSupportMaterial", menuName = "GameData/SupportMaterial")]
public class SupportMaterialData : ScriptableObject
{
    [Header("基本情報")]
    public int materialId;
    public string materialName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;

    [Header("効果タイプ（参考用）")]
    [Tooltip("効果タイプは参考用です。実際の効果は下記の個別設定で決まります")]
    public SupportType supportType;

    [Header("素材タイプ（文字列）")]
    [Tooltip("DataManager.csで使用される文字列タイプ（自動設定可能）")]
    public string materialType = "custom"; // デフォルトをcustomに

    [Header("成功率への影響")]
    [Range(-1f, 1f)]
    [Tooltip("成功率の増減（+0.1 = +10%, -0.1 = -10%）")]
    public float successRateModifier = 0f;

    [Header("強化耐久への影響")]
    [Tooltip("使用後の強化耐久の増減（負の値で減少軽減、正の値で回復）")]
    public int durabilityModifier = 0;

    [Header("★新機能：失敗時耐久軽減")]
    [Tooltip("失敗時の耐久減少量を軽減する値（例：3の場合、失敗時に通常の耐久減少から3減らす）")]
    public int failureDurabilityReduction = 0;

    [Header("特殊効果（自由設定）")]
    [Tooltip("失敗後のペナルティを防ぐ")]
    public bool preventFailurePenalty = false;

    [Tooltip("成功を保証する（レア素材用）")]
    public bool guaranteeSuccess = false;

    [Header("追加強化効果")]
    public SupportBonus bonus;

    [Header("UI表示用テキスト")]
    [Tooltip("効果内容の表示テキスト（空の場合は自動生成）")]
    [TextArea(2, 4)]
    public string effectDisplayText;

    [Header("自動設定オプション")]
    [Tooltip("ONにすると効果タイプに応じて値を自動設定します")]
    public bool useAutoConfiguration = false;

    /// <summary>
    /// 効果の説明文を取得
    /// effectDisplayTextが設定されていればそれを使用、なければ自動生成
    /// </summary>
    public string GetEffectDescription()
    {
        // カスタムテキストが設定されていればそれを使用
        if (!string.IsNullOrEmpty(effectDisplayText))
            return effectDisplayText;

        // 自動生成
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // 成功率修正
        if (successRateModifier != 0)
        {
            float percentage = successRateModifier * 100f;
            if (percentage > 0)
                sb.AppendLine($"成功率+{percentage:F0}%");
            else
                sb.AppendLine($"成功率{percentage:F0}%");
        }

        // 耐久度修正
        if (durabilityModifier != 0)
        {
            if (durabilityModifier < 0)
                sb.AppendLine($"強化耐久の減少量{-durabilityModifier}");
            else
                sb.AppendLine($"強化耐久+{durabilityModifier}");
        }

        // ★新機能：失敗時耐久軽減
        if (failureDurabilityReduction > 0)
        {
            sb.AppendLine($"失敗時の耐久減少を{failureDurabilityReduction}軽減");
        }

        // 特殊効果
        if (preventFailurePenalty)
            sb.AppendLine("失敗後のペナルティを防ぐ");

        if (guaranteeSuccess)
            sb.AppendLine("成功を保証");

        // 追加ボーナス効果
        if (bonus.bonusAttackPower > 0)
            sb.AppendLine($"攻撃力+{bonus.bonusAttackPower}");

        if (bonus.bonusDefensePower > 0)
            sb.AppendLine($"防御力+{bonus.bonusDefensePower}");

        if (bonus.bonusElementalAttack > 0)
            sb.AppendLine($"属性攻撃+{bonus.bonusElementalAttack}");

        if (bonus.bonusHP > 0)
            sb.AppendLine($"HP+{bonus.bonusHP}");

        if (bonus.bonusCriticalRate > 0)
            sb.AppendLine($"クリティカル率+{bonus.bonusCriticalRate:F1}%");

        // 何も効果がない場合はdescriptionを使用
        if (sb.Length == 0)
            return description;

        return sb.ToString().Trim();
    }

    /// <summary>
    /// ★新機能：失敗時の耐久減少量を計算
    /// </summary>
    public int CalculateDurabilityReduction(bool isSuccess, int baseDurabilityReduction)
    {
        if (isSuccess)
        {
            // 成功時は通常の耐久減少
            return baseDurabilityReduction;
        }
        else
        {
            // 失敗時は軽減効果を適用
            int reducedAmount = baseDurabilityReduction - failureDurabilityReduction;
            return Mathf.Max(0, reducedAmount); // 0未満にならないよう制限
        }
    }

    /// <summary>
    /// 耐久度への影響を取得
    /// </summary>
    public int GetDurabilityEffect()
    {
        return durabilityModifier;
    }

    /// <summary>
    /// 強化後に使用可能かチェック
    /// </summary>
    public bool CanUseWithEnhancement()
    {
        return materialType == "lucky_stone" ||
               materialType == "protection" ||
               materialType == "durability_protection" ||
               materialType == "custom" ||
               successRateModifier != 0 ||
               durabilityModifier != 0 ||
               failureDurabilityReduction > 0 ||
               preventFailurePenalty ||
               guaranteeSuccess;
    }

    /// <summary>
    /// インベントリで直接使用可能かチェック
    /// </summary>
    public bool CanUseDirectly()
    {
        return materialType == "durability_restore";
    }

    /// <summary>
    /// 自動設定を適用（手動で呼び出し可能）
    /// </summary>
    [ContextMenu("Apply Auto Configuration")]
    public void ApplyAutoConfiguration()
    {
        if (!useAutoConfiguration) return;

        switch (supportType)
        {
            case SupportType.LuckyStone:
                materialType = "lucky_stone";
                if (successRateModifier == 0f)
                    successRateModifier = 0.1f; // デフォルト10%
                break;

            case SupportType.ProtectionTicket:
                materialType = "durability_protection";
                if (failureDurabilityReduction == 0)
                    failureDurabilityReduction = 3; // デフォルト3軽減
                break;

            case SupportType.DurabilityStone:
                materialType = "durability_restore";
                if (durabilityModifier == 0)
                    durabilityModifier = 50; // デフォルト50回復
                break;

            case SupportType.PowerStone:
                materialType = "power_boost";
                if (bonus.bonusAttackPower == 0)
                    bonus.bonusAttackPower = 10;
                if (successRateModifier == 0f)
                    successRateModifier = -0.1f; // 成功率-10%
                break;

            case SupportType.DefenseStone:
                materialType = "defense_boost";
                if (bonus.bonusDefensePower == 0)
                    bonus.bonusDefensePower = 10;
                if (successRateModifier == 0f)
                    successRateModifier = -0.1f; // 成功率-10%
                break;

            case SupportType.ElementalStone:
                materialType = "elemental_boost";
                if (bonus.bonusElementalAttack == 0)
                    bonus.bonusElementalAttack = 15;
                if (successRateModifier == 0f)
                    successRateModifier = -0.05f; // 成功率-5%
                break;

            case SupportType.VitalityStone:
                materialType = "vitality_boost";
                if (bonus.bonusHP == 0)
                    bonus.bonusHP = 20;
                if (successRateModifier == 0f)
                    successRateModifier = -0.05f; // 成功率-5%
                break;

            case SupportType.CriticalStone:
                materialType = "critical_boost";
                if (bonus.bonusCriticalRate == 0f)
                    bonus.bonusCriticalRate = 5f;
                if (successRateModifier == 0f)
                    successRateModifier = -0.1f; // 成功率-10%
                break;

            default:
                materialType = "custom";
                break;
        }
    }

    /// <summary>
    /// OnValidateでは自動設定をオプション化
    /// </summary>
    private void OnValidate()
    {
        // 自動設定がONの場合のみ適用
        if (useAutoConfiguration)
        {
            ApplyAutoConfiguration();
        }

        // materialTypeが空の場合のみcustomを設定
        if (string.IsNullOrEmpty(materialType))
        {
            materialType = "custom";
        }
    }

    /// <summary>
    /// プリセット設定用メソッド群
    /// </summary>
    [ContextMenu("Preset: Lucky Stone")]
    public void SetAsLuckyStone()
    {
        supportType = SupportType.LuckyStone;
        materialType = "lucky_stone";
        successRateModifier = 0.1f;
        durabilityModifier = 0;
        failureDurabilityReduction = 0;
        preventFailurePenalty = false;
        guaranteeSuccess = false;
        effectDisplayText = "成功率+10%";
    }

    [ContextMenu("Preset: Durability Protection")]
    public void SetAsDurabilityProtection()
    {
        supportType = SupportType.ProtectionTicket;
        materialType = "durability_protection";
        successRateModifier = 0f;
        durabilityModifier = 0;
        failureDurabilityReduction = 3;
        preventFailurePenalty = false;
        guaranteeSuccess = false;
        effectDisplayText = "失敗時の耐久減少を3軽減";
    }

    [ContextMenu("Preset: Failure Protection")]
    public void SetAsFailureProtection()
    {
        supportType = SupportType.ProtectionTicket;
        materialType = "protection";
        successRateModifier = 0f;
        durabilityModifier = 0;
        failureDurabilityReduction = 0;
        preventFailurePenalty = true;
        guaranteeSuccess = false;
        effectDisplayText = "失敗後のペナルティを防ぐ";
    }

    [ContextMenu("Reset to Custom")]
    public void ResetToCustom()
    {
        materialType = "custom";
        successRateModifier = 0f;
        durabilityModifier = 0;
        failureDurabilityReduction = 0;
        preventFailurePenalty = false;
        guaranteeSuccess = false;
        useAutoConfiguration = false;
        effectDisplayText = "";
    }
}

public enum SupportType
{
    LuckyStone,        // 幸運石（成功率アップ）
    PowerStone,        // 力の石（攻撃力追加、成功率ダウン）
    DefenseStone,      // 守りの石（防御力追加、成功率ダウン）
    ElementalStone,    // 属性石（属性攻撃追加、成功率ダウン）
    VitalityStone,     // 生命石（HP追加、成功率ダウン）
    CriticalStone,     // 会心石（クリティカル率追加、成功率ダウン）
    ProtectionTicket,  // 強化保護チケット
    DurabilityStone,   // 耐久保護石
    Custom             // カスタム（自由設定）
}