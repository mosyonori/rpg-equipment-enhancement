using UnityEngine;

public class BattleEnemy : BattleCharacter
{
    [Header("�G�ŗL�f�[�^")]
    public int enemyId;
    public int spawnOrder;

    // �G��p���\�b�h
    public void InitializeFromMasterData(MonsterMasterData masterData)
    {
        if (masterData == null) return;

        // �}�X�^�[�f�[�^����X�e�[�^�X��������
        characterName = masterData.monsterName;
        enemyId = masterData.monsterId;
        maxHP = currentHP = masterData.maxHP;
        attackPower = masterData.attackPower;
        defensePower = masterData.defensePower;
        speed = masterData.speed;
        criticalRate = masterData.criticalRate;

        // �����U����
        fireAttack = masterData.fireAttack;
        waterAttack = masterData.waterAttack;
        windAttack = masterData.windAttack;
        earthAttack = masterData.earthAttack;

        // �X�L��������
        InitializeSkill(ref skill1, masterData.skill1Id);
        InitializeSkill(ref skill2, masterData.skill2Id);

        Debug.Log($"�G '{characterName}' �����������܂����BHP:{maxHP} ATK:{attackPower} DEF:{defensePower}");
    }

    private void InitializeSkill(ref BattleSkill skill, string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return;

        // �X�L���}�X�^�[�f�[�^����X�L�������擾
        SkillMasterData skillData = GetSkillMasterData(skillId);
        if (skillData != null)
        {
            skill = new BattleSkill
            {
                skillId = skillData.skillId,
                skillName = skillData.skillName,
                maxCoolTime = skillData.maxCoolTime,
                currentCoolTime = 0
            };

            Debug.Log($"�X�L�� '{skill.skillName}' ��ݒ肵�܂��� (CT:{skill.maxCoolTime})");
        }
        else
        {
            // �t�H�[���o�b�N: �f�t�H���g�X�L���ݒ�
            skill = new BattleSkill
            {
                skillId = skillId,
                skillName = GetDefaultSkillName(skillId),
                maxCoolTime = GetDefaultCoolTime(skillId),
                currentCoolTime = 0
            };
        }
    }

    private SkillMasterData GetSkillMasterData(string skillId)
    {
        // TODO: DataManager����X�L���}�X�^�[�f�[�^���擾
        // ���݂̓}�X�^�[�f�[�^�����ړǂݍ��߂Ȃ����߁A�X�L��ID���琄��

        // Assets/GameData/Skills/ �t�H���_����X�L���f�[�^������
        var skillAssets = Resources.LoadAll<SkillMasterData>("GameData/Skills");
        foreach (var skillData in skillAssets)
        {
            if (skillData.skillId == skillId)
            {
                return skillData;
            }
        }

        return null;
    }

    private string GetDefaultSkillName(string skillId)
    {
        // �X�L��ID���疼�O�𐄑��i�t�H�[���o�b�N�p�j
        return skillId switch
        {
            "skill_fire_ball" => "�t�@�C�A�{�[��",
            "skill_quick_strike" => "�N�C�b�N�X�g���C�N",
            "skill_guard" => "�K�[�h",
            "skill_wind_claw" => "�E�B���h�N���[",
            "skill_howl" => "�n�E��",
            "skill_earthquake" => "�A�[�X�N�G�C�N",
            "skill_stone_armor" => "�X�g�[���A�[�}�[",
            "skill_heal_small" => "�X���[���q�[��",
            _ => "�s���ȃX�L��"
        };
    }

    private int GetDefaultCoolTime(string skillId)
    {
        // �X�L��ID����N�[���^�C���𐄑��i�t�H�[���o�b�N�p�j
        return skillId switch
        {
            "skill_fire_ball" => 3,
            "skill_quick_strike" => 2,
            "skill_guard" => 4,
            "skill_wind_claw" => 4,
            "skill_howl" => 5,
            "skill_earthquake" => 6,
            "skill_stone_armor" => 8,
            "skill_heal_small" => 3,
            _ => 3
        };
    }

    // �G�̃��x���ɉ������X�e�[�^�X����
    public void AdjustStatsForLevel(int targetLevel)
    {
        if (targetLevel <= 1) return;

        float levelMultiplier = 1.0f + (targetLevel - 1) * 0.1f; // ���x��1�オ�邲�Ƃ�10%����

        maxHP = Mathf.RoundToInt(maxHP * levelMultiplier);
        currentHP = maxHP;
        attackPower = Mathf.RoundToInt(attackPower * levelMultiplier);
        defensePower = Mathf.RoundToInt(defensePower * levelMultiplier);

        // �����U���͂�����
        fireAttack = Mathf.RoundToInt(fireAttack * levelMultiplier);
        waterAttack = Mathf.RoundToInt(waterAttack * levelMultiplier);
        windAttack = Mathf.RoundToInt(windAttack * levelMultiplier);
        earthAttack = Mathf.RoundToInt(earthAttack * levelMultiplier);

        Debug.Log($"�G���x�� {targetLevel} �ɒ���: HP:{maxHP} ATK:{attackPower} DEF:{defensePower}");
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        // �G���S���̌o���l�E�h���b�v����������Βǉ�
        Debug.Log($"{characterName}��|���܂����I");
    }
}