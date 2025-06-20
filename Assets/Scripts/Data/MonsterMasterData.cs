using UnityEngine;

[CreateAssetMenu(fileName = "NewMonster", menuName = "GameData/MonsterData")]
public class MonsterMasterData : ScriptableObject
{
    [Header("��{���")]
    public int monsterId;
    public string monsterName;
    [TextArea(3, 5)]
    public string monsterDescription;
    public int level;

    [Header("�X�e�[�^�X")]
    public int maxHP;
    public int attackPower;
    public int defensePower;
    public int speed;
    public float criticalRate;

    [Header("�����U����")]
    public int fireAttack;
    public int waterAttack;
    public int windAttack;
    public int earthAttack;

    [Header("�X�L��")]
    public string skill1Id;
    public string skill2Id;

    [Header("�\���ݒ�")]
    public string iconId;
    public string rarity;
    public string monsterType;
}