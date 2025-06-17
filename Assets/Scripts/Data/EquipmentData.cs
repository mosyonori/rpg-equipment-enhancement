using UnityEngine;

[System.Serializable]
public class EquipmentSkill
{
    [Header("�X�L�����")]
    public int skillId;
    public string skillName;
    public string description;
    public int requiredEnhancementLevel; // ����ɕK�v�ȋ����l
    public float cooldown; // �N�[���_�E������
    public int damage; // �X�L���_���[�W
    public float effect; // ���ʒl�i�񕜗ʁA�o�t���ʂȂǁj
}

[System.Serializable]
public class EquipmentStats
{
    [Header("��{�X�e�[�^�X")]
    public int baseAttackPower;
    public int baseDefensePower;
    public int baseElementalAttack;  // �ėp�����U���i�݊����ێ��j
    public int baseHP;
    public float baseCriticalRate;
    public int baseDurability;

    [Header("4��ނ̑����U��")]
    public int baseFireAttack = 0;      // �Α����U��
    public int baseWaterAttack = 0;     // �������U��
    public int baseWindAttack = 0;      // �������U��
    public int baseEarthAttack = 0;     // �y�����U��

    [Header("������������")]
    public int attackPowerPerLevel;
    public int defensePowerPerLevel;
    public int elementalAttackPerLevel;
    public int hpPerLevel;
    public float criticalRatePerLevel;

    [Header("�����U��������")]
    public int fireAttackPerLevel = 0;   // �Α����U���̐�����
    public int waterAttackPerLevel = 0;  // �������U���̐�����
    public int windAttackPerLevel = 0;   // �������U���̐�����
    public int earthAttackPerLevel = 0;  // �y�����U���̐�����
}

[CreateAssetMenu(fileName = "NewEquipment", menuName = "GameData/Equipment")]
public class EquipmentData : ScriptableObject
{
    [Header("��{���")]
    public int equipmentId;
    public string equipmentName;
    public string description;
    public Sprite icon;
    public Sprite enhancedIcon; // �������̃A�C�R��

    [Header("�����^�C�v")]
    public EquipmentType equipmentType;

    [Header("�X�e�[�^�X")]
    public EquipmentStats stats;

    [Header("�X�L��")]
    public EquipmentSkill[] skills;

    [Header("�����ݒ�")]
    [Range(0f, 1f)]
    public float baseSuccessRate = 0.8f; // ��{������
    public float successRateDecreasePerLevel = 0.01f; // ���x�����Ƃ̐���������

    [Header("�����ڕω�")]
    public EnhancementVisual[] visualChanges;
}

[System.Serializable]
public class EnhancementVisual
{
    public int enhancementLevel; // ���̋����l�Ō����ڕω�
    public Sprite newIcon;
    public Color glowColor;
    public GameObject effectPrefab;
}

public enum EquipmentType
{
    Weapon,
    Armor,
    Accessory
}