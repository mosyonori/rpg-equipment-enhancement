using UnityEngine;

[CreateAssetMenu(fileName = "NewMonster", menuName = "GameData/MonsterData")]
public class MonsterMasterData : ScriptableObject
{
    [Header("基本情報")]
    public int monsterId;
    public string monsterName;
    [TextArea(3, 5)]
    public string monsterDescription;
    public int level;

    [Header("ステータス")]
    public int maxHP;
    public int attackPower;
    public int defensePower;
    public int speed;
    public float criticalRate;

    [Header("属性攻撃力")]
    public int fireAttack;
    public int waterAttack;
    public int windAttack;
    public int earthAttack;

    [Header("スキル")]
    public string skill1Id;
    public string skill2Id;

    [Header("表示設定")]
    public string iconId;
    public string rarity;
    public string monsterType;
}