using UnityEngine;

[System.Serializable]
public class EnhancementBonus
{
    [Header("�������̑�����")]
    public int enhancementValue;
    public int attackPower;
    public int defensePower;
    public int elementalAttack;  // �ėp�����U���i�݊����ێ��j
    public int hp;
    public float criticalRate;

    [Header("�����U��")]
    public int fireAttack;       // �Α����U��
    public int waterAttack;      // �������U��  
    public int windAttack;       // �������U��
    public int earthAttack;      // �y�����U��

    [Header("�R�X�g")]
    public int durabilityReduction = 1; // �����ϋv�����ʁi�f�t�H���g1�j
}

[CreateAssetMenu(fileName = "NewEnhancementItem", menuName = "GameData/EnhancementItem")]
public class EnhancementItemData : ScriptableObject
{
    [Header("��{���")]
    public int itemId;
    public string itemName;
    public string description;
    public Sprite icon;

    [Header("�A�C�e���^�C�v")]
    public EnhancementItemType itemType;

    [Header("�����ݒ�")]
    [Range(0f, 1f)]
    public float successRate = 0.5f; // ���̃A�C�e���ŗL�̐�����

    [Range(0f, 1f)]
    public float greatSuccessRate = 0.1f; // �听���m��

    [Header("��������")]
    public EnhancementBonus bonus;

    [Header("���A���e�B")]
    public ItemRarity rarity;

    [Header("�G�t�F�N�g�ݒ�")]
    public GameObject enhanceEffectPrefab; // �����G�t�F�N�g
    public AudioClip enhanceSound; // ������

    // ���ǉ�: �������x���ɉ����������m�����v�Z
    public float GetAdjustedSuccessRate(int enhancementLevel)
    {
        // �������x�����オ��قǐ����m�������ʂɉ�����
        float penalty = enhancementLevel * 0.01f; // 1���x�����Ƃ�1%����
        return Mathf.Max(0.1f, successRate - penalty); // �Œ�10%�͕ۏ�
    }

    // ���ǉ�: �������x���ɉ������听���m�����v�Z
    public float GetAdjustedGreatSuccessRate(int enhancementLevel)
    {
        // �听���m���͋������x���̉e�����󂯂ɂ���
        float penalty = enhancementLevel * 0.005f; // 1���x�����Ƃ�0.5%����
        return Mathf.Max(0.01f, greatSuccessRate - penalty); // �Œ�1%�͕ۏ�
    }

    // ���ǉ�: �{�[�i�X�l���擾�i�݊����ێ��p�j
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

    // ���ǉ�: �����U���{�[�i�X�擾���\�b�h
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

    // ���ǉ�: ���ʐ����𐶐�
    public string GetEffectDescription()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (bonus.attackPower > 0)
            sb.AppendLine($"�U���� +{bonus.attackPower}");

        if (bonus.defensePower > 0)
            sb.AppendLine($"�h��� +{bonus.defensePower}");

        if (bonus.elementalAttack > 0)
            sb.AppendLine($"�����U�� +{bonus.elementalAttack}");

        // ���ǉ��F4��ނ̑����U��
        if (bonus.fireAttack > 0)
            sb.AppendLine($"�Α����U�� +{bonus.fireAttack}");

        if (bonus.waterAttack > 0)
            sb.AppendLine($"�������U�� +{bonus.waterAttack}");

        if (bonus.windAttack > 0)
            sb.AppendLine($"�������U�� +{bonus.windAttack}");

        if (bonus.earthAttack > 0)
            sb.AppendLine($"�y�����U�� +{bonus.earthAttack}");

        if (bonus.hp > 0)
            sb.AppendLine($"HP +{bonus.hp}");

        if (bonus.criticalRate > 0)
            sb.AppendLine($"�N���e�B�J���� +{bonus.criticalRate:F1}%");

        if (sb.Length == 0)
            return "���ʂȂ�";

        return sb.ToString().Trim();
    }

    // ���ǉ�: ���A���e�B�ɉ������F���擾
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

    // ���ǉ�: �A�C�e���^�C�v�ɉ������{�[�i�X����
    private void OnValidate()
    {
        // �A�C�e���^�C�v�ɉ����ăf�t�H���g�l�𒲐�
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

        // ���A���e�B�ɉ����ă{�[�i�X�l�𒲐�
        float rarityMultiplier = GetRarityMultiplier();
        if (rarityMultiplier > 1f)
        {
            // ���A���e�B�������ꍇ�͌��ʂ�����
            // �������A���ɐݒ�ς݂̒l�͕ύX���Ȃ�
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
    BasicStone,      // ��{������
    ElementalStone,  // ����������
    SpecialStone     // ���ꋭ����
}

public enum ItemRarity
{
    Common,    // ��
    Uncommon,  // ��
    Rare,      // ��
    Epic,      // ��
    Legendary  // ��
}