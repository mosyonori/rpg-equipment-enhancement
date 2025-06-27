using UnityEngine;

/// <summary>
/// キャラクターのマスターデータ
/// </summary>
[CreateAssetMenu(fileName = "Character_", menuName = "MasterData/Character")]
public class CharacterMasterData : ScriptableObject
{
    [Header("基本情報")]
    [SerializeField] private int characterId;
    [SerializeField] private string characterName;
    [SerializeField] private RarityType rarity;
    [SerializeField] private int baseLevel;
    [SerializeField] private int maxLevel;

    [Header("基本ステータス")]
    [SerializeField] private int hp;
    [SerializeField] private int offense;
    [SerializeField] private int defense;
    [SerializeField] private int speed;
    [SerializeField] private int criticalRate;
    [SerializeField] private int criticalDamageRate;

    [Header("属性攻撃")]
    [SerializeField] private int fireOffence;
    [SerializeField] private int waterOffence;
    [SerializeField] private int windOffence;
    [SerializeField] private int earthOffence;

    [Header("スキル")]
    [SerializeField] private int defaultSkillId;
    [SerializeField] private int usedSkill1;
    [SerializeField] private int usedSkill2;

    [Header("UI・表示")]
    [SerializeField] private Sprite characterIcon;
    [SerializeField] private string characterIconPath;
    [SerializeField] private string characterAnimationPath;
    [SerializeField] private string description;

    [Header("収集要素")]
    [SerializeField] private bool completionFlag;
    [SerializeField] private bool collectionFlag;

    // プロパティ
    public int CharacterId => characterId;
    public string CharacterName => characterName;
    public RarityType Rarity => rarity;
    public int BaseLevel => baseLevel;
    public int MaxLevel => maxLevel;
    public int Hp => hp;
    public int Offense => offense;
    public int Defense => defense;
    public int Speed => speed;
    public int CriticalRate => criticalRate;
    public int CriticalDamageRate => criticalDamageRate;
    public int FireOffence => fireOffence;
    public int WaterOffence => waterOffence;
    public int WindOffence => windOffence;
    public int EarthOffence => earthOffence;
    public int DefaultSkillId => defaultSkillId;
    public int UsedSkill1 => usedSkill1;
    public int UsedSkill2 => usedSkill2;
    public Sprite CharacterIcon => characterIcon;
    public string CharacterIconPath => characterIconPath;
    public string CharacterAnimationPath => characterAnimationPath;
    public string Description => description;
    public bool CompletionFlag => completionFlag;
    public bool CollectionFlag => collectionFlag;

    // === CSVImporter用のSetterメソッド ===

#if UNITY_EDITOR
    public void SetCharacterId(int value) => characterId = value;
    public void SetCharacterName(string value) => characterName = value;
    public void SetRarity(RarityType value) => rarity = value;
    public void SetBaseLevel(int value) => baseLevel = value;
    public void SetMaxLevel(int value) => maxLevel = value;
    public void SetHp(int value) => hp = value;
    public void SetOffense(int value) => offense = value;
    public void SetDefense(int value) => defense = value;
    public void SetSpeed(int value) => speed = value;
    public void SetCriticalRate(int value) => criticalRate = value;
    public void SetCriticalDamageRate(int value) => criticalDamageRate = value;
    public void SetFireOffence(int value) => fireOffence = value;
    public void SetWaterOffence(int value) => waterOffence = value;
    public void SetWindOffence(int value) => windOffence = value;
    public void SetEarthOffence(int value) => earthOffence = value;
    public void SetDefaultSkillId(int value) => defaultSkillId = value;
    public void SetUsedSkill1(int value) => usedSkill1 = value;
    public void SetUsedSkill2(int value) => usedSkill2 = value;
    public void SetCharacterIcon(Sprite value) => characterIcon = value;
    public void SetCharacterIconPath(string value) => characterIconPath = value;
    public void SetCharacterAnimationPath(string value) => characterAnimationPath = value;
    public void SetDescription(string value) => description = value;
    public void SetCompletionFlag(bool value) => completionFlag = value;
    public void SetCollectionFlag(bool value) => collectionFlag = value;
#endif
}