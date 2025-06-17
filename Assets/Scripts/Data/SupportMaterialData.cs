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
    public float durabilityProtection; // 0.0f～1.0f（0%～100%保護）
}

[CreateAssetMenu(fileName = "NewSupportMaterial", menuName = "GameData/SupportMaterial")]
public class SupportMaterialData : ScriptableObject
{
    [Header("基本情報")]
    public int materialId;
    public string materialName;
    public string description;
    public Sprite icon;

    [Header("効果タイプ")]
    public SupportType supportType;

    [Header("材料タイプ（文字列）")]
    public string materialType; // ★追加: DataManager.csで使用される文字列タイプ

    [Header("成功率への影響")]
    public float successRateModifier; // +0.2f = +20%, -0.1f = -10%

    [Header("追加強化効果")]
    public SupportBonus bonus;

    [Header("特殊効果")]
    public bool preventFailurePenalty; // 失敗時のペナルティを防ぐ
    public bool guaranteeSuccess; // 成功を保証する（レア素材用）

    // ★追加: 効果説明を取得するメソッド
    public string GetEffectDescription()
    {
        switch (materialType)
        {
            case "lucky_stone":
                return $"成功率を{successRateModifier * 100:F0}%上昇";
            case "protection":
                return "失敗時のペナルティを防ぐ";
            case "durability_restore":
                return $"耐久度を{successRateModifier * 100:F0}回復";
            default:
                return description;
        }
    }

    // ★追加: 強化時に使用可能かチェック
    public bool CanUseWithEnhancement()
    {
        return materialType == "lucky_stone" || materialType == "protection";
    }

    // ★追加: インベントリで直接使用可能かチェック
    public bool CanUseDirectly()
    {
        return materialType == "durability_restore";
    }

    // ★追加: OnValidateでsupportTypeからmaterialTypeを自動設定
    private void OnValidate()
    {
        // supportTypeからmaterialTypeを自動設定
        switch (supportType)
        {
            case SupportType.LuckyStone:
                materialType = "lucky_stone";
                if (successRateModifier == 0f) successRateModifier = 0.2f; // デフォルト20%
                break;
            case SupportType.ProtectionTicket:
                materialType = "protection";
                preventFailurePenalty = true;
                break;
            case SupportType.DurabilityStone:
                materialType = "durability_restore";
                if (successRateModifier == 0f) successRateModifier = 0.5f; // デフォルト50回復
                break;
            case SupportType.PowerStone:
                materialType = "power_boost";
                break;
            case SupportType.DefenseStone:
                materialType = "defense_boost";
                break;
            case SupportType.ElementalStone:
                materialType = "elemental_boost";
                break;
            case SupportType.VitalityStone:
                materialType = "vitality_boost";
                break;
            case SupportType.CriticalStone:
                materialType = "critical_boost";
                break;
            default:
                materialType = "unknown";
                break;
        }
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
    DurabilityStone    // 耐久保護石
}