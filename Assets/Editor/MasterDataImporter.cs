using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class MasterDataImporter : EditorWindow
{
    private Vector2 scrollPosition;

    [MenuItem("Window/Game Data/Master Data Importer")]
    public static void ShowWindow()
    {
        GetWindow<MasterDataImporter>("Master Data Importer");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Master Data Importer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Quest Data (既存)
        DrawImportSection("Quest Data", "Assets/CSV/QuestData.csv", "Assets/GameData/Quests/", ImportQuestData);

        GUILayout.Space(5);

        // Monster Data (新規)
        DrawImportSection("Monster Data", "Assets/CSV/MonsterData.csv", "Assets/GameData/Monsters/", ImportMonsterData);

        GUILayout.Space(5);

        // Skill Data (新規)
        DrawImportSection("Skill Data", "Assets/CSV/SkillData.csv", "Assets/GameData/Skills/", ImportSkillData);

        GUILayout.Space(5);

        // Status Effect Data (新規)
        DrawImportSection("Status Effect Data", "Assets/CSV/StatusEffectData.csv", "Assets/GameData/StatusEffects/", ImportStatusEffectData);

        GUILayout.Space(20);

        // 全インポート
        if (GUILayout.Button("Import All Data", GUILayout.Height(30)))
        {
            ImportAllData();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawImportSection(string dataName, string csvPath, string outputPath, System.Action importAction)
    {
        EditorGUILayout.BeginVertical("box");

        GUILayout.Label(dataName, EditorStyles.boldLabel);

        // ファイル存在チェック
        bool csvExists = File.Exists(csvPath);
        GUI.color = csvExists ? Color.green : Color.red;
        GUILayout.Label($"CSV: {(csvExists ? "Found" : "Not Found")}", EditorStyles.miniLabel);
        GUI.color = Color.white;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button($"Import {dataName}"))
        {
            if (csvExists)
            {
                importAction.Invoke();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", $"CSV file not found: {csvPath}", "OK");
            }
        }

        if (GUILayout.Button("Open Folder"))
        {
            if (Directory.Exists(outputPath))
            {
                EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                Directory.CreateDirectory(outputPath);
                AssetDatabase.Refresh();
                EditorUtility.RevealInFinder(outputPath);
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void ImportAllData()
    {
        ImportQuestData();
        ImportMonsterData();
        ImportSkillData();
        ImportStatusEffectData();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Import Complete", "All master data has been imported successfully!", "OK");
    }

    private void ImportQuestData()
    {
        // 既存のQuest Data Importerの処理を呼び出す
        // または既存のロジックをここに移植
        Debug.Log("Importing Quest Data...");
    }

    private void ImportMonsterData()
    {
        string csvPath = "Assets/CSV/MonsterData.csv";
        string outputDir = "Assets/GameData/Monsters/";

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSV file not found: {csvPath}");
            return;
        }

        // ディレクトリ作成
        Directory.CreateDirectory(outputDir);

        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1) return;

        // ヘッダー行をスキップ
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < 18) continue;

            // MonsterMasterData作成
            MonsterMasterData monster = CreateInstance<MonsterMasterData>();

            // データ設定
            monster.monsterId = int.Parse(values[0]);
            monster.monsterName = values[1];
            monster.monsterDescription = values[2];
            monster.level = int.Parse(values[3]);
            monster.maxHP = int.Parse(values[4]);
            monster.attackPower = int.Parse(values[5]);
            monster.defensePower = int.Parse(values[6]);
            monster.speed = int.Parse(values[7]);
            monster.criticalRate = float.Parse(values[8]);
            monster.fireAttack = int.Parse(values[9]);
            monster.waterAttack = int.Parse(values[10]);
            monster.windAttack = int.Parse(values[11]);
            monster.earthAttack = int.Parse(values[12]);
            monster.skill1Id = values[13];
            monster.skill2Id = values[14];
            monster.iconId = values[15];
            monster.rarity = values[16];
            monster.monsterType = values[17];

            // ファイル保存
            string assetPath = $"{outputDir}Monster_{monster.monsterId:000}_{monster.monsterName}.asset";
            AssetDatabase.CreateAsset(monster, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Monster Data imported to {outputDir}");
    }

    private void ImportSkillData()
    {
        string csvPath = "Assets/CSV/SkillData.csv";
        string outputDir = "Assets/GameData/Skills/";

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSV file not found: {csvPath}");
            return;
        }

        Directory.CreateDirectory(outputDir);

        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1) return;

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < 17) continue;

            SkillMasterData skill = CreateInstance<SkillMasterData>();

            skill.skillId = values[0];
            skill.skillName = values[1];
            skill.skillDescription = values[2];
            skill.skillType = (SkillType)System.Enum.Parse(typeof(SkillType), values[3]);
            skill.targetType = (TargetType)System.Enum.Parse(typeof(TargetType), values[4]);
            skill.damageMultiplier = float.Parse(values[5]);
            skill.maxCoolTime = int.Parse(values[6]);
            skill.mpCost = int.Parse(values[7]);
            skill.skillElement = (SkillElement)System.Enum.Parse(typeof(SkillElement), values[8]);
            skill.statusEffectId = values[9];
            skill.statusEffectChance = float.Parse(values[10]);
            skill.statusEffectDuration = int.Parse(values[11]);
            skill.iconId = values[12];
            skill.animationId = values[13];
            skill.soundId = values[14];
            skill.rarity = (SkillRarity)System.Enum.Parse(typeof(SkillRarity), values[15]);
            skill.skillCategory = (SkillCategory)System.Enum.Parse(typeof(SkillCategory), values[16]);

            string assetPath = $"{outputDir}Skill_{skill.skillId}.asset";
            AssetDatabase.CreateAsset(skill, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Skill Data imported to {outputDir}");
    }

    private void ImportStatusEffectData()
    {
        string csvPath = "Assets/CSV/StatusEffectData.csv";
        string outputDir = "Assets/GameData/StatusEffects/";

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSV file not found: {csvPath}");
            return;
        }

        Directory.CreateDirectory(outputDir);

        string[] lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1) return;

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length < 19) continue;

            StatusEffectMasterData effect = CreateInstance<StatusEffectMasterData>();

            effect.statusEffectId = values[0];
            effect.statusEffectName = values[1];
            effect.statusEffectDescription = values[2];
            effect.effectType = (StatusEffectType)System.Enum.Parse(typeof(StatusEffectType), values[3]);
            effect.isStackable = bool.Parse(values[4]);
            effect.attackModifier = int.Parse(values[5]);
            effect.defenseModifier = int.Parse(values[6]);
            effect.attackMultiplier = float.Parse(values[7]);
            effect.defenseMultiplier = float.Parse(values[8]);
            effect.fireAttackMultiplier = float.Parse(values[9]);
            effect.waterAttackMultiplier = float.Parse(values[10]);
            effect.windAttackMultiplier = float.Parse(values[11]);
            effect.earthAttackMultiplier = float.Parse(values[12]);
            effect.preventAction = bool.Parse(values[13]);
            effect.turnStartDamagePercent = float.Parse(values[14]);
            effect.turnStartHealPercent = float.Parse(values[15]);
            effect.iconId = values[16];
            effect.colorCode = values[17];
            effect.priority = int.Parse(values[18]);

            string assetPath = $"{outputDir}StatusEffect_{effect.statusEffectId}.asset";
            AssetDatabase.CreateAsset(effect, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Status Effect Data imported to {outputDir}");
    }

    private string[] ParseCSVLine(string line)
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
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        result.Add(currentField);
        return result.ToArray();
    }
}