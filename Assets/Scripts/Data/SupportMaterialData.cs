using UnityEngine;

[System.Serializable]
public class SupportBonus
{
    [Header("�ǉ���������")]
    public int bonusAttackPower;
    public int bonusDefensePower;
    public int bonusElementalAttack;
    public int bonusHP;
    public float bonusCriticalRate;

    [Header("�ϋv�ی�")]
    public float durabilityProtection; // 0.0f�`1.0f�i0%�`100%�ی�j
}

[CreateAssetMenu(fileName = "NewSupportMaterial", menuName = "GameData/SupportMaterial")]
public class SupportMaterialData : ScriptableObject
{
    [Header("��{���")]
    public int materialId;
    public string materialName;
    public string description;
    public Sprite icon;

    [Header("���ʃ^�C�v")]
    public SupportType supportType;

    [Header("�ޗ��^�C�v�i������j")]
    public string materialType; // ���ǉ�: DataManager.cs�Ŏg�p����镶����^�C�v

    [Header("�������ւ̉e��")]
    public float successRateModifier; // +0.2f = +20%, -0.1f = -10%

    [Header("�ǉ���������")]
    public SupportBonus bonus;

    [Header("�������")]
    public bool preventFailurePenalty; // ���s���̃y�i���e�B��h��
    public bool guaranteeSuccess; // ������ۏ؂���i���A�f�ޗp�j

    // ���ǉ�: ���ʐ������擾���郁�\�b�h
    public string GetEffectDescription()
    {
        switch (materialType)
        {
            case "lucky_stone":
                return $"��������{successRateModifier * 100:F0}%�㏸";
            case "protection":
                return "���s���̃y�i���e�B��h��";
            case "durability_restore":
                return $"�ϋv�x��{successRateModifier * 100:F0}��";
            default:
                return description;
        }
    }

    // ���ǉ�: �������Ɏg�p�\���`�F�b�N
    public bool CanUseWithEnhancement()
    {
        return materialType == "lucky_stone" || materialType == "protection";
    }

    // ���ǉ�: �C���x���g���Œ��ڎg�p�\���`�F�b�N
    public bool CanUseDirectly()
    {
        return materialType == "durability_restore";
    }

    // ���ǉ�: OnValidate��supportType����materialType�������ݒ�
    private void OnValidate()
    {
        // supportType����materialType�������ݒ�
        switch (supportType)
        {
            case SupportType.LuckyStone:
                materialType = "lucky_stone";
                if (successRateModifier == 0f) successRateModifier = 0.2f; // �f�t�H���g20%
                break;
            case SupportType.ProtectionTicket:
                materialType = "protection";
                preventFailurePenalty = true;
                break;
            case SupportType.DurabilityStone:
                materialType = "durability_restore";
                if (successRateModifier == 0f) successRateModifier = 0.5f; // �f�t�H���g50��
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
    LuckyStone,        // �K�^�΁i�������A�b�v�j
    PowerStone,        // �͂̐΁i�U���͒ǉ��A�������_�E���j
    DefenseStone,      // ���̐΁i�h��͒ǉ��A�������_�E���j
    ElementalStone,    // �����΁i�����U���ǉ��A�������_�E���j
    VitalityStone,     // �����΁iHP�ǉ��A�������_�E���j
    CriticalStone,     // ��S�΁i�N���e�B�J�����ǉ��A�������_�E���j
    ProtectionTicket,  // �����ی�`�P�b�g
    DurabilityStone    // �ϋv�ی��
}