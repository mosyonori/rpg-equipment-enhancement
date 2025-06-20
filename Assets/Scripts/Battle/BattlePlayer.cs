using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BattlePlayer : BattleCharacter
{
    [Header("�v���C���[�ŗL�f�[�^")]
    public int playerId;
    public int level;
    public int experience;

    [Header("�������")]
    public List<int> equippedItems = new List<int>();

    // �v���C���[��p���\�b�h
    public void ApplyEquipmentStats(UserEquipment equipment)
    {
        if (equipment == null) return;

        // ��������X�e�[�^�X���v�Z����BattleCharacter�̃v���p�e�B�ɐݒ�
        var equipmentData = DataManager.Instance?.GetEquipmentData(equipment.equipmentId);
        if (equipmentData == null) return;

        // ��{�X�e�[�^�X�K�p�ifloat����int�ɃL���X�g�j
        attackPower = (int)equipment.GetTotalAttack();
        defensePower = (int)equipment.GetTotalDefense();
        maxHP = (int)equipment.GetTotalHP();
        currentHP = maxHP;
        criticalRate = equipment.GetTotalCriticalRate(); // float�̂܂�

        // �����U���͓K�p�ifloat����int�ɃL���X�g�j
        fireAttack = (int)equipment.GetTotalFireAttack();
        waterAttack = (int)equipment.GetTotalWaterAttack();
        windAttack = (int)equipment.GetTotalWindAttack();
        earthAttack = (int)equipment.GetTotalEarthAttack();

        // �X�L���ݒ�i���������ŉ�������X�L���j
        InitializePlayerSkills(equipment);
    }

    private void InitializePlayerSkills(UserEquipment equipment)
    {
        // �����̋����l�ɉ����ăX�L����ݒ�
        if (equipment.enhancementLevel >= 5)
        {
            // �����l+5�ŃX�L��1���
            skill1 = new BattleSkill
            {
                skillId = "skill_player_attack",
                skillName = "�����U��",
                maxCoolTime = 3,
                currentCoolTime = 0
            };
        }

        if (equipment.enhancementLevel >= 10)
        {
            // �����l+10�ŃX�L��2���
            skill2 = new BattleSkill
            {
                skillId = "skill_player_heal",
                skillName = "��",
                maxCoolTime = 5,
                currentCoolTime = 0
            };
        }
    }

    // �v���C���[�̊�{����ݒ�
    public void InitializePlayerData()
    {
        characterName = "�v���C���[";
        playerId = 1;
        level = 1;
        position = 1;

        // ���[�U�[�f�[�^���瑕�������擾���ăX�e�[�^�X�K�p
        var userEquipment = DataManager.Instance?.GetUserEquipment(0); // ���ōŏ��̑������擾
        if (userEquipment != null)
        {
            ApplyEquipmentStats(userEquipment);
        }
        else
        {
            // �f�t�H���g�X�e�[�^�X
            maxHP = currentHP = 100;
            attackPower = 25;
            defensePower = 15;
            speed = 10;
            criticalRate = 5.0f;
        }
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        // �v���C���[���S���̓��ʏ���������Βǉ�
        Debug.Log("�v���C���[���|��܂���...");
    }
}