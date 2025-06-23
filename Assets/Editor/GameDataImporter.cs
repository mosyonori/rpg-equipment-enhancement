using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// サポートアイテムデータ（ScriptableObject版）
/// </summary>
[CreateAssetMenu(fileName = "SupportItemData", menuName = "GameData/SupportItemData")]
public class SupportItemData : ScriptableObject
{
    [Header("基本情報")]
    public int supportItemId;
    public string supportItemName;
    public string attributeType;
    public string rarity;
    public string description;

    [Header("スタック・効果設定")]
    public bool infiniteUse;            // 🔴 NEW: 無限使用可能
    public int maxStackValue;
    public int addEnhancedValue;
    public int multiplEnhancedValue;
    public int reduceEnhancedValue;
    public int addEnhanceStamina;
    public int reduceEnhanceStamina;
    public int addEnhanceSuccessRate;
    public int reduceEnhanceSuccessRate;
    public int multiplStatusUp;

    [Header("ステータスボーナス")]
    public int hp;
    public int offense;
    public int defense;
    public int speed;
    public int criticalRate;
    public int criticalDamageRate;

    [Header("属性攻撃ボーナス")]
    public int fireOffence;
    public int waterOffence;
    public int windOffence;
    public int earthOffence;

    [Header("Unityアセット（手動割り当て）")]
    public Sprite supportItemIcon;      // 🔴 NEW: Inspector上で手動割り当て

    [Header("フラグ")]
    public bool completionFlag;
    public bool collectionFlag;
    internal object itemIcon;
}

/// <summary>
/// キャラクターデータ（ScriptableObject版）
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "GameData/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("基本情報")]
    public int characterId;
    public string characterName;
    public string rarity;
    public int baseLevel;
    public int maxLevel;
    public string description;

    [Header("ステータス")]
    public int hp;
    public int offense;
    public int defense;
    public int speed;
    public int criticalRate;
    public int criticalDamageRate;

    [Header("属性攻撃")]
    public int fireOffence;
    public int waterOffence;
    public int windOffence;
    public int earthOffence;

    [Header("スキル")]
    public int defaultSkillId;
    public string usedSkill1;
    public string usedSkill2;

    [Header("Unityアセット（手動割り当て）")]
    public Sprite characterIcon;
    public GameObject characterModel;
    public AnimationClip[] animations;

    [Header("フラグ")]
    public bool completionFlag;
    public bool collectionFlag;
}

/// <summary>
/// キャラクター経験値テーブル（ScriptableObject版）
/// </summary>
[CreateAssetMenu(fileName = "CharacterExperienceTable", menuName = "GameData/CharacterExperienceTable")]
public class CharacterExperienceTable : ScriptableObject
{
    [System.Serializable]
    public class ExperienceEntry
    {
        public int characterLevel;
        public int needExperience;
        public int totalExperience;
    }

    [Header("経験値テーブル")]
    public List<ExperienceEntry> experienceTable = new List<ExperienceEntry>();

    /// <summary>
    /// レベルに必要な経験値を取得
    /// </summary>
    public int GetNeedExperience(int level)
    {
        var entry = experienceTable.Find(e => e.characterLevel == level);
        return entry?.needExperience ?? 0;
    }

    /// <summary>
    /// レベルに必要な累計経験値を取得
    /// </summary>
    public int GetTotalExperience(int level)
    {
        var entry = experienceTable.Find(e => e.characterLevel == level);
        return entry?.totalExperience ?? 0;
    }
}

/// <summary>
/// クエストデータ（ScriptableObject版）
/// </summary>
[CreateAssetMenu(fileName = "QuestData", menuName = "GameData/QuestData")]
public class QuestData : ScriptableObject
{
    [Header("基本情報")]
    public int questId;
    public string questName;
    public string description;
    public string questType;
    public int needLevel;
    public float requiredClearQuest;
    public int clearLimit;
    public int requiredStamina;
    public int recommendedPower;

    [Header("モンスター設定")]
    public int monsterCount;
    public int spawnMonsterId1;
    public int spawnPriorityMonsterId1;
    public float spawnMonsterId2;
    public float spawnPriorityMonsterId2;
    public float spawnMonsterId3;
    public float spawnPriorityMonsterId3;
    public int turnLimit;

    [Header("報酬")]
    public int rewardExp;
    public int rewardGold;
    public int itemDropQuantity;

    [Header("ドロップアイテム1")]
    public string dropItemType1;
    public int itemId1;
    public int itemDropPriority1;

    [Header("ドロップアイテム2")]
    public string dropItemType2;
    public int itemId2;
    public int itemDropPriority2;

    [Header("ドロップアイテム3")]
    public string dropItemType3;
    public int itemDropId3;
    public int itemDropPriority3;

    [Header("ドロップアイテム4")]
    public string dropItemType4;
    public int itemId4;
    public int itemDropPriority4;

    [Header("ドロップアイテム5")]
    public string dropItemType5;
    public int itemId5;
    public int itemDropPriority5;

    [Header("ドロップアイテム6")]
    public string dropItemType6;
    public int itemId6;
    public int itemDropPriority6;

    [Header("初回クリア報酬")]
    public int firstClearItemId;
    public string firstClearItemType;
    public int firstClearItemIdAlt;
    public int firstClearItemQuantity;

    [Header("環境設定")]
    public int backgroundPath;
    public int bgmPath;
}

/// <summary>
/// スキルデータ（ScriptableObject版）
/// </summary>
[CreateAssetMenu(fileName = "SkillData", menuName = "GameData/SkillData")]
public class SkillData : ScriptableObject
{
    [Header("基本情報")]
    public int skillId;
    public string skillType;
    public string attributeType;
    public string rarity;
    public string skillName;
    public string description;

    [Header("スキル設定")]
    public float skillDamageMultiplier;
    public string skillTargetType;
    public string skillTargetCharacter;
    public int skillMaxCoolTime;
    public int skillHpCost;
    public int skillMpCost;

    [Header("スキル効果")]
    public string skillEffect;
    public string skillEffectTargetCharacter;
    public int skillEffectChance;
    public int skillEffectChanceBoss;
    public int skillEffectDuration;

    [Header("Unityアセット（手動割り当て）")]
    public Sprite skillIcon;
    public AnimationClip skillAnimation;
    public AudioClip skillSound;

    [Header("フラグ")]
    public bool completionFlag;
    public bool collectionFlag;
}

/// <summary>
/// スキル効果データ（ScriptableObject版）
/// </summary>
[CreateAssetMenu(fileName = "SkillEffectData", menuName = "GameData/SkillEffectData")]
public class SkillEffectData : ScriptableObject
{
    [Header("基本情報")]
    public int statusEffectId;
    public string statusEffectType;
    public string statusEffectName;
    public string description;
    public string effectType;
    public int stackable;

    [Header("ステータス効果")]
    public int offenseModifier;
    public int defenseModifier;
    public float offenseMultiplier;
    public float defenseMultiplier;

    [Header("属性効果")]
    public float fireOffenseMultiplier;
    public float waterOffenseMultiplier;
    public float windOffenseMultiplier;
    public float earthOffenseMultiplier;

    [Header("特殊効果")]
    public int preventAction;
    public int turnStartDamagePercent;
    public int turnStartHealPercent;

    [Header("表示設定")]
    public string skillEffectIconId;
    public string colorCode;
    public int skillEffectPriority;
}

#if UNITY_EDITOR
/// <summary>
/// 各種データCSVインポーター
/// </summary>
public class GameDataCSVImporter
{
    /// <summary>
    /// サポートアイテムデータをインポート
    /// </summary>
    [MenuItem("Tools/CSV Import/Import Support Item Data")]
    public static void ImportSupportItemData()
    {
        string csvPath = "Assets/CSV/m_support_item_data.csv";
        ImportSupportItemDataFromCSV(csvPath);
    }

    private static void ImportSupportItemDataFromCSV(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {csvPath}");
            return;
        }

        Debug.Log("🔄 サポートアイテムデータインポート開始...");

        List<SupportItemData> itemList = new List<SupportItemData>();
        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length < 2)
        {
            Debug.LogError("CSVファイルが空か、ヘッダー行のみです");
            return;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);

            if (values.Length < 27)
            {
                Debug.LogWarning($"行 {i + 1}: 列数が不足しています（{values.Length}/27列）");
                continue;
            }

            try
            {
                SupportItemData item = ScriptableObject.CreateInstance<SupportItemData>();

                item.supportItemId = ParseInt(values[0], $"行{i + 1} supportItemId");
                item.supportItemName = values[1];
                item.attributeType = values[2];
                item.rarity = values[3];
                item.maxStackValue = ParseInt(values[4], $"行{i + 1} maxStackValue");
                item.addEnhancedValue = ParseInt(values[5], $"行{i + 1} addEnhancedValue");
                item.multiplEnhancedValue = ParseInt(values[6], $"行{i + 1} multiplEnhancedValue");
                item.reduceEnhancedValue = ParseInt(values[7], $"行{i + 1} reduceEnhancedValue");
                item.addEnhanceStamina = ParseInt(values[8], $"行{i + 1} addEnhanceStamina");
                item.reduceEnhanceStamina = ParseInt(values[9], $"行{i + 1} reduceEnhanceStamina");
                item.addEnhanceSuccessRate = ParseInt(values[10], $"行{i + 1} addEnhanceSuccessRate");
                item.reduceEnhanceSuccessRate = ParseInt(values[11], $"行{i + 1} reduceEnhanceSuccessRate");
                item.multiplStatusUp = ParseInt(values[12], $"行{i + 1} multiplStatusUp");
                item.hp = ParseInt(values[13], $"行{i + 1} hp");
                item.offense = ParseInt(values[14], $"行{i + 1} offense");
                item.defense = ParseInt(values[15], $"行{i + 1} defense");
                item.speed = ParseInt(values[16], $"行{i + 1} speed");
                item.criticalRate = ParseInt(values[17], $"行{i + 1} criticalRate");
                item.criticalDamageRate = ParseInt(values[18], $"行{i + 1} criticalDamageRate");
                item.fireOffence = ParseInt(values[19], $"行{i + 1} fireOffence");
                item.waterOffence = ParseInt(values[20], $"行{i + 1} waterOffence");
                item.windOffence = ParseInt(values[21], $"行{i + 1} windOffence");
                item.earthOffence = ParseInt(values[22], $"行{i + 1} earthOffence");
                // values[23] = enhance_item_icon_path (空欄)
                item.description = values[24];
                item.completionFlag = values[25] == "1";
                item.collectionFlag = values[26] == "1";

                string folderPath = "Assets/GameData/SupportItems";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string assetPath = $"{folderPath}/SupportItem_{item.supportItemId:000}_{item.supportItemName}.asset";

                if (File.Exists(assetPath))
                {
                    SupportItemData existingItem = AssetDatabase.LoadAssetAtPath<SupportItemData>(assetPath);
                    if (existingItem != null)
                    {
                        item.itemIcon = existingItem.itemIcon;
                    }
                }

                AssetDatabase.CreateAsset(item, assetPath);
                itemList.Add(item);

                Debug.Log($"✅ インポート完了: {item.supportItemName} (ID:{item.supportItemId})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"行 {i + 1} の処理中にエラー: {e.Message}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"🎉 サポートアイテムデータインポート完了！合計 {itemList.Count} 個");
    }

    /// <summary>
    /// キャラクターデータをインポート
    /// </summary>
    [MenuItem("Tools/CSV Import/Import Character Data")]
    public static void ImportCharacterData()
    {
        string csvPath = "Assets/CSV/m_character_data.csv";
        ImportCharacterDataFromCSV(csvPath);
    }

    private static void ImportCharacterDataFromCSV(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {csvPath}");
            return;
        }

        Debug.Log("🔄 キャラクターデータインポート開始...");

        List<CharacterData> characterList = new List<CharacterData>();
        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length < 2)
        {
            Debug.LogError("CSVファイルが空か、ヘッダー行のみです");
            return;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);

            if (values.Length < 23)
            {
                Debug.LogWarning($"行 {i + 1}: 列数が不足しています（{values.Length}/23列）");
                continue;
            }

            try
            {
                CharacterData character = ScriptableObject.CreateInstance<CharacterData>();

                character.characterId = ParseInt(values[0], $"行{i + 1} characterId");
                character.characterName = values[1];
                character.rarity = values[2];
                character.baseLevel = ParseInt(values[3], $"行{i + 1} baseLevel");
                character.maxLevel = ParseInt(values[4], $"行{i + 1} maxLevel");
                character.hp = ParseInt(values[5], $"行{i + 1} hp");
                character.offense = ParseInt(values[6], $"行{i + 1} offense");
                character.defense = ParseInt(values[7], $"行{i + 1} defense");
                character.speed = ParseInt(values[8], $"行{i + 1} speed");
                character.criticalRate = ParseInt(values[9], $"行{i + 1} criticalRate");
                character.criticalDamageRate = ParseInt(values[10], $"行{i + 1} criticalDamageRate");
                character.fireOffence = ParseInt(values[11], $"行{i + 1} fireOffence");
                character.waterOffence = ParseInt(values[12], $"行{i + 1} waterOffence");
                character.windOffence = ParseInt(values[13], $"行{i + 1} windOffence");
                character.earthOffence = ParseInt(values[14], $"行{i + 1} earthOffence");
                character.defaultSkillId = ParseInt(values[15], $"行{i + 1} defaultSkillId");
                character.usedSkill1 = values[16];
                character.usedSkill2 = values[17];
                // values[18] = character_icon_path (空欄)
                // values[19] = character_animation_path (空欄)
                character.description = values[20];
                character.completionFlag = values[21] == "1";
                character.collectionFlag = values[22] == "1";

                string folderPath = "Assets/GameData/Characters";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string assetPath = $"{folderPath}/Character_{character.characterId:000}_{character.characterName}.asset";

                if (File.Exists(assetPath))
                {
                    CharacterData existingCharacter = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);
                    if (existingCharacter != null)
                    {
                        character.characterIcon = existingCharacter.characterIcon;
                        character.characterModel = existingCharacter.characterModel;
                        character.animations = existingCharacter.animations;
                    }
                }

                AssetDatabase.CreateAsset(character, assetPath);
                characterList.Add(character);

                Debug.Log($"✅ インポート完了: {character.characterName} (ID:{character.characterId})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"行 {i + 1} の処理中にエラー: {e.Message}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"🎉 キャラクターデータインポート完了！合計 {characterList.Count} 体");
    }

    /// <summary>
    /// キャラクター経験値テーブルをインポート
    /// </summary>
    [MenuItem("Tools/CSV Import/Import Character Experience Table")]
    public static void ImportCharacterExperienceTable()
    {
        string csvPath = "Assets/CSV/m_character_experiece_table.csv";
        ImportCharacterExperienceTableFromCSV(csvPath);
    }

    private static void ImportCharacterExperienceTableFromCSV(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {csvPath}");
            return;
        }

        Debug.Log("🔄 キャラクター経験値テーブルインポート開始...");

        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length < 2)
        {
            Debug.LogError("CSVファイルが空か、ヘッダー行のみです");
            return;
        }

        CharacterExperienceTable expTable = ScriptableObject.CreateInstance<CharacterExperienceTable>();
        expTable.experienceTable.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);

            if (values.Length < 3)
            {
                Debug.LogWarning($"行 {i + 1}: 列数が不足しています（{values.Length}/3列）");
                continue;
            }

            try
            {
                CharacterExperienceTable.ExperienceEntry entry = new CharacterExperienceTable.ExperienceEntry();
                entry.characterLevel = ParseInt(values[0], $"行{i + 1} characterLevel");
                entry.needExperience = ParseInt(values[1], $"行{i + 1} needExperience");
                entry.totalExperience = ParseInt(values[2], $"行{i + 1} totalExperience");

                expTable.experienceTable.Add(entry);

                Debug.Log($"✅ レベル {entry.characterLevel}: 必要経験値 {entry.needExperience}, 累計 {entry.totalExperience}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"行 {i + 1} の処理中にエラー: {e.Message}");
            }
        }

        string folderPath = "Assets/GameData";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string assetPath = $"{folderPath}/CharacterExperienceTable.asset";
        AssetDatabase.CreateAsset(expTable, assetPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"🎉 キャラクター経験値テーブルインポート完了！合計 {expTable.experienceTable.Count} レベル");
    }

    /// <summary>
    /// クエストデータをインポート
    /// </summary>
    [MenuItem("Tools/CSV Import/Import Quest Data")]
    public static void ImportQuestData()
    {
        string csvPath = "Assets/CSV/m_quest_data.csv";
        ImportQuestDataFromCSV(csvPath);
    }

    private static void ImportQuestDataFromCSV(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {csvPath}");
            return;
        }

        Debug.Log("🔄 クエストデータインポート開始...");

        List<QuestData> questList = new List<QuestData>();
        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length < 2)
        {
            Debug.LogError("CSVファイルが空か、ヘッダー行のみです");
            return;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);

            if (values.Length < 44)
            {
                Debug.LogWarning($"行 {i + 1}: 列数が不足しています（{values.Length}/44列）");
                continue;
            }

            try
            {
                QuestData quest = ScriptableObject.CreateInstance<QuestData>();

                quest.questId = ParseInt(values[0], $"行{i + 1} questId");
                quest.questName = values[1];
                quest.description = values[2];
                quest.questType = values[3];
                quest.needLevel = ParseInt(values[4], $"行{i + 1} needLevel");
                quest.requiredClearQuest = ParseFloat(values[5], $"行{i + 1} requiredClearQuest");
                quest.clearLimit = ParseInt(values[6], $"行{i + 1} clearLimit");
                quest.requiredStamina = ParseInt(values[7], $"行{i + 1} requiredStamina");
                quest.recommendedPower = ParseInt(values[8], $"行{i + 1} recommendedPower");
                quest.monsterCount = ParseInt(values[9], $"行{i + 1} monsterCount");
                quest.spawnMonsterId1 = ParseInt(values[10], $"行{i + 1} spawnMonsterId1");
                quest.spawnPriorityMonsterId1 = ParseInt(values[11], $"行{i + 1} spawnPriorityMonsterId1");
                quest.spawnMonsterId2 = ParseFloat(values[12], $"行{i + 1} spawnMonsterId2");
                quest.spawnPriorityMonsterId2 = ParseFloat(values[13], $"行{i + 1} spawnPriorityMonsterId2");
                quest.spawnMonsterId3 = ParseFloat(values[14], $"行{i + 1} spawnMonsterId3");
                quest.spawnPriorityMonsterId3 = ParseFloat(values[15], $"行{i + 1} spawnPriorityMonsterId3");
                quest.turnLimit = ParseInt(values[16], $"行{i + 1} turnLimit");
                quest.rewardExp = ParseInt(values[17], $"行{i + 1} rewardExp");
                quest.rewardGold = ParseInt(values[18], $"行{i + 1} rewardGold");
                quest.itemDropQuantity = ParseInt(values[19], $"行{i + 1} itemDropQuantity");
                quest.dropItemType1 = values[20];
                quest.itemId1 = ParseInt(values[21], $"行{i + 1} itemId1");
                quest.itemDropPriority1 = ParseInt(values[22], $"行{i + 1} itemDropPriority1");
                quest.dropItemType2 = values[23];
                quest.itemId2 = ParseInt(values[24], $"行{i + 1} itemId2");
                quest.itemDropPriority2 = ParseInt(values[25], $"行{i + 1} itemDropPriority2");
                quest.dropItemType3 = values[26];
                quest.itemDropId3 = ParseInt(values[27], $"行{i + 1} itemDropId3");
                quest.itemDropPriority3 = ParseInt(values[28], $"行{i + 1} itemDropPriority3");
                quest.dropItemType4 = values[29];
                quest.itemId4 = ParseInt(values[30], $"行{i + 1} itemId4");
                quest.itemDropPriority4 = ParseInt(values[31], $"行{i + 1} itemDropPriority4");
                quest.dropItemType5 = values[32];
                quest.itemId5 = ParseInt(values[33], $"行{i + 1} itemId5");
                quest.itemDropPriority5 = ParseInt(values[34], $"行{i + 1} itemDropPriority5");
                quest.dropItemType6 = values[35];
                quest.itemId6 = ParseInt(values[36], $"行{i + 1} itemId6");
                quest.itemDropPriority6 = ParseInt(values[37], $"行{i + 1} itemDropPriority6");
                quest.firstClearItemId = ParseInt(values[38], $"行{i + 1} firstClearItemId");
                quest.firstClearItemType = values[39];
                quest.firstClearItemIdAlt = ParseInt(values[40], $"行{i + 1} firstClearItemIdAlt");
                quest.firstClearItemQuantity = ParseInt(values[41], $"行{i + 1} firstClearItemQuantity");
                quest.backgroundPath = ParseInt(values[42], $"行{i + 1} backgroundPath");
                quest.bgmPath = ParseInt(values[43], $"行{i + 1} bgmPath");

                string folderPath = "Assets/GameData/Quests";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string assetPath = $"{folderPath}/Quest_{quest.questId:000}_{quest.questName}.asset";
                AssetDatabase.CreateAsset(quest, assetPath);
                questList.Add(quest);

                Debug.Log($"✅ インポート完了: {quest.questName} (ID:{quest.questId})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"行 {i + 1} の処理中にエラー: {e.Message}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"🎉 クエストデータインポート完了！合計 {questList.Count} 個");
    }

    /// <summary>
    /// スキルデータをインポート
    /// </summary>
    [MenuItem("Tools/CSV Import/Import Skill Data")]
    public static void ImportSkillData()
    {
        string csvPath = "Assets/CSV/m_skill_data.csv";
        ImportSkillDataFromCSV(csvPath);
    }

    private static void ImportSkillDataFromCSV(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {csvPath}");
            return;
        }

        Debug.Log("🔄 スキルデータインポート開始...");

        List<SkillData> skillList = new List<SkillData>();
        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length < 2)
        {
            Debug.LogError("CSVファイルが空か、ヘッダー行のみです");
            return;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);

            if (values.Length < 22)
            {
                Debug.LogWarning($"行 {i + 1}: 列数が不足しています（{values.Length}/22列）");
                continue;
            }

            try
            {
                SkillData skill = ScriptableObject.CreateInstance<SkillData>();

                skill.skillId = ParseInt(values[0], $"行{i + 1} skillId");
                skill.skillType = values[1];
                skill.attributeType = values[2];
                skill.rarity = values[3];
                skill.skillName = values[4];
                skill.skillDamageMultiplier = ParseFloat(values[5], $"行{i + 1} skillDamageMultiplier");
                skill.skillTargetType = values[6];
                skill.skillTargetCharacter = values[7];
                skill.skillMaxCoolTime = ParseInt(values[8], $"行{i + 1} skillMaxCoolTime");
                skill.skillHpCost = ParseInt(values[9], $"行{i + 1} skillHpCost");
                skill.skillMpCost = ParseInt(values[10], $"行{i + 1} skillMpCost");
                skill.skillEffect = values[11];
                skill.skillEffectTargetCharacter = values[12];
                skill.skillEffectChance = ParseInt(values[13], $"行{i + 1} skillEffectChance");
                skill.skillEffectChanceBoss = ParseInt(values[14], $"行{i + 1} skillEffectChanceBoss");
                skill.skillEffectDuration = ParseInt(values[15], $"行{i + 1} skillEffectDuration");
                // values[16] = skill_icon_path (空欄)
                // values[17] = skill_animation_path (空欄)
                // values[18] = skill_sound_path (空欄)
                skill.description = values[19];
                skill.completionFlag = values[20] == "1";
                skill.collectionFlag = values[21] == "1";

                string folderPath = "Assets/GameData/Skills";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string assetPath = $"{folderPath}/Skill_{skill.skillId:000}_{skill.skillName}.asset";

                if (File.Exists(assetPath))
                {
                    SkillData existingSkill = AssetDatabase.LoadAssetAtPath<SkillData>(assetPath);
                    if (existingSkill != null)
                    {
                        skill.skillIcon = existingSkill.skillIcon;
                        skill.skillAnimation = existingSkill.skillAnimation;
                        skill.skillSound = existingSkill.skillSound;
                    }
                }

                AssetDatabase.CreateAsset(skill, assetPath);
                skillList.Add(skill);

                Debug.Log($"✅ インポート完了: {skill.skillName} (ID:{skill.skillId})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"行 {i + 1} の処理中にエラー: {e.Message}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"🎉 スキルデータインポート完了！合計 {skillList.Count} 個");
    }

    /// <summary>
    /// スキル効果データをインポート
    /// </summary>
    [MenuItem("Tools/CSV Import/Import Skill Effect Data")]
    public static void ImportSkillEffectData()
    {
        string csvPath = "Assets/CSV/m_skill_effects_data.csv";
        ImportSkillEffectDataFromCSV(csvPath);
    }

    private static void ImportSkillEffectDataFromCSV(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {csvPath}");
            return;
        }

        Debug.Log("🔄 スキル効果データインポート開始...");

        List<SkillEffectData> effectList = new List<SkillEffectData>();
        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length < 2)
        {
            Debug.LogError("CSVファイルが空か、ヘッダー行のみです");
            return;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);

            if (values.Length < 20)
            {
                Debug.LogWarning($"行 {i + 1}: 列数が不足しています（{values.Length}/20列）");
                continue;
            }

            try
            {
                SkillEffectData effect = ScriptableObject.CreateInstance<SkillEffectData>();

                effect.statusEffectId = ParseInt(values[0], $"行{i + 1} statusEffectId");
                effect.statusEffectType = values[1];
                effect.statusEffectName = values[2];
                effect.description = values[3];
                effect.effectType = values[4];
                effect.stackable = ParseInt(values[5], $"行{i + 1} stackable");
                effect.offenseModifier = ParseInt(values[6], $"行{i + 1} offenseModifier");
                effect.defenseModifier = ParseInt(values[7], $"行{i + 1} defenseModifier");
                effect.offenseMultiplier = ParseFloat(values[8], $"行{i + 1} offenseMultiplier");
                effect.defenseMultiplier = ParseFloat(values[9], $"行{i + 1} defenseMultiplier");
                effect.fireOffenseMultiplier = ParseFloat(values[10], $"行{i + 1} fireOffenseMultiplier");
                effect.waterOffenseMultiplier = ParseFloat(values[11], $"行{i + 1} waterOffenseMultiplier");
                effect.windOffenseMultiplier = ParseFloat(values[12], $"行{i + 1} windOffenseMultiplier");
                effect.earthOffenseMultiplier = ParseFloat(values[13], $"行{i + 1} earthOffenseMultiplier");
                effect.preventAction = ParseInt(values[14], $"行{i + 1} preventAction");
                effect.turnStartDamagePercent = ParseInt(values[15], $"行{i + 1} turnStartDamagePercent");
                effect.turnStartHealPercent = ParseInt(values[16], $"行{i + 1} turnStartHealPercent");
                effect.skillEffectIconId = values[17];
                effect.colorCode = values[18];
                effect.skillEffectPriority = ParseInt(values[19], $"行{i + 1} skillEffectPriority");

                string folderPath = "Assets/GameData/SkillEffects";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string assetPath = $"{folderPath}/SkillEffect_{effect.statusEffectId:000}_{effect.statusEffectName}.asset";
                AssetDatabase.CreateAsset(effect, assetPath);
                effectList.Add(effect);

                Debug.Log($"✅ インポート完了: {effect.statusEffectName} (ID:{effect.statusEffectId})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"行 {i + 1} の処理中にエラー: {e.Message}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"🎉 スキル効果データインポート完了！合計 {effectList.Count} 個");
    }

    /// <summary>
    /// 全データを一括インポート
    /// </summary>
    [MenuItem("Tools/CSV Import/Import All Game Data")]
    public static void ImportAllGameData()
    {
        Debug.Log("🔄 全データ一括インポート開始...");

        ImportSupportItemData();
        ImportCharacterData();
        ImportCharacterExperienceTable();
        ImportQuestData();
        ImportSkillData();
        ImportSkillEffectData();

        Debug.Log("🎉 全データ一括インポート完了！");
    }

    /// <summary>
    /// CSV行をパース（カンマ区切り、クォート対応）
    /// </summary>
    private static string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField.Trim());
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        result.Add(currentField.Trim()); // 最後のフィールド
        return result.ToArray();
    }

    /// <summary>
    /// 安全なint変換
    /// </summary>
    private static int ParseInt(string value, string fieldName)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        if (int.TryParse(value, out int result))
        {
            return result;
        }

        Debug.LogWarning($"{fieldName}: '{value}' を数値に変換できません。0を使用します。");
        return 0;
    }

    /// <summary>
    /// 安全なfloat変換
    /// </summary>
    private static float ParseFloat(string value, string fieldName)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0f;
        }

        if (float.TryParse(value, out float result))
        {
            return result;
        }

        Debug.LogWarning($"{fieldName}: '{value}' を数値に変換できません。0を使用します。");
        return 0f;
    }
}

/// <summary>
/// データコレクション管理クラス
/// </summary>
[CreateAssetMenu(fileName = "GameDataCollection", menuName = "GameData/GameDataCollection")]
public class GameDataCollection : ScriptableObject
{
    [Header("全データコレクション")]
    public List<SupportItemData> supportItems = new List<SupportItemData>();
    public List<CharacterData> characters = new List<CharacterData>();
    public CharacterExperienceTable experienceTable;
    public List<QuestData> quests = new List<QuestData>();
    public List<SkillData> skills = new List<SkillData>();
    public List<SkillEffectData> skillEffects = new List<SkillEffectData>();

    /// <summary>
    /// 全データを自動収集
    /// </summary>
    [ContextMenu("Collect All Game Data")]
    public void CollectAllGameData()
    {
        CollectSupportItems();
        CollectCharacters();
        CollectExperienceTable();
        CollectQuests();
        CollectSkills();
        CollectSkillEffects();

        Debug.Log("📦 全ゲームデータの収集完了");
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    private void CollectSupportItems()
    {
        supportItems.Clear();
        string[] guids = AssetDatabase.FindAssets("t:SupportItemData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SupportItemData item = AssetDatabase.LoadAssetAtPath<SupportItemData>(path);
            if (item != null) supportItems.Add(item);
        }
        supportItems.Sort((a, b) => a.supportItemId.CompareTo(b.supportItemId));
        Debug.Log($"📦 サポートアイテム: {supportItems.Count} 個");
    }

    private void CollectCharacters()
    {
        characters.Clear();
        string[] guids = AssetDatabase.FindAssets("t:CharacterData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (character != null) characters.Add(character);
        }
        characters.Sort((a, b) => a.characterId.CompareTo(b.characterId));
        Debug.Log($"📦 キャラクター: {characters.Count} 体");
    }

    private void CollectExperienceTable()
    {
        string[] guids = AssetDatabase.FindAssets("t:CharacterExperienceTable");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            experienceTable = AssetDatabase.LoadAssetAtPath<CharacterExperienceTable>(path);
            Debug.Log($"📦 経験値テーブル: {experienceTable?.experienceTable.Count ?? 0} レベル");
        }
    }

    private void CollectQuests()
    {
        quests.Clear();
        string[] guids = AssetDatabase.FindAssets("t:QuestData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            QuestData quest = AssetDatabase.LoadAssetAtPath<QuestData>(path);
            if (quest != null) quests.Add(quest);
        }
        quests.Sort((a, b) => a.questId.CompareTo(b.questId));
        Debug.Log($"📦 クエスト: {quests.Count} 個");
    }

    private void CollectSkills()
    {
        skills.Clear();
        string[] guids = AssetDatabase.FindAssets("t:SkillData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill != null) skills.Add(skill);
        }
        skills.Sort((a, b) => a.skillId.CompareTo(b.skillId));
        Debug.Log($"📦 スキル: {skills.Count} 個");
    }

    private void CollectSkillEffects()
    {
        skillEffects.Clear();
        string[] guids = AssetDatabase.FindAssets("t:SkillEffectData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillEffectData effect = AssetDatabase.LoadAssetAtPath<SkillEffectData>(path);
            if (effect != null) skillEffects.Add(effect);
        }
        skillEffects.Sort((a, b) => a.statusEffectId.CompareTo(b.statusEffectId));
        Debug.Log($"📦 スキル効果: {skillEffects.Count} 個");
    }

    /// <summary>
    /// データ取得メソッド群
    /// </summary>
    public SupportItemData GetSupportItem(int id) => supportItems.Find(item => item.supportItemId == id);
    public CharacterData GetCharacter(int id) => characters.Find(character => character.characterId == id);
    public QuestData GetQuest(int id) => quests.Find(quest => quest.questId == id);
    public SkillData GetSkill(int id) => skills.Find(skill => skill.skillId == id);
    public SkillEffectData GetSkillEffect(int id) => skillEffects.Find(effect => effect.statusEffectId == id);
}
#endif