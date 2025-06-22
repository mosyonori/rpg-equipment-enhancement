using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// モンスターデータ（ScriptableObject版）
/// </summary>
[CreateAssetMenu(fileName = "MonsterData", menuName = "GameData/MonsterData")]
public class MonsterData : ScriptableObject
{
    [Header("基本情報")]
    public int monsterId;
    public string monsterType;
    public string attributeType;
    public string rarity;
    public string monsterName;
    public string description;

    [Header("ステータス")]
    public int hp;
    public int offense;
    public int defense;
    public int speed;
    public int criticalRate;
    public int criticalDamageRate;  // 🔴 追加: クリティカルダメージレート

    [Header("属性攻撃")]
    public int fireOffence;
    public int waterOffence;
    public int windOffence;
    public int earthOffence;

    [Header("スキル")]
    public int usedSkill1;
    public string usedSkill2;
    public string usedSkill3;

    [Header("Unityアセット（手動割り当て）")]
    public Sprite monsterIcon;           // Inspector上で手動割り当て
    public GameObject monsterModel;      // Inspector上で手動割り当て
    public AnimationClip[] animations;   // Inspector上で手動割り当て

    [Header("フラグ")]
    public bool completionFlag;
    public bool collectionFlag;
}

/// <summary>
/// モンスターデータCSVインポーター
/// </summary>
#if UNITY_EDITOR
public class MonsterCSVImporter
{
    /// <summary>
    /// m_monster_data.csvからMonsterDataアセットを生成
    /// </summary>
    [MenuItem("Tools/CSV Import/Import Monster Data")]
    public static void ImportMonsterData()
    {
        string csvPath = "Assets/CSV/m_monster_data.csv";

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {csvPath}");
            return;
        }

        Debug.Log("🔄 モンスターデータインポート開始...");

        List<MonsterData> monsterList = new List<MonsterData>();
        string[] lines = File.ReadAllLines(csvPath);

        if (lines.Length < 2)
        {
            Debug.LogError("CSVファイルが空か、ヘッダー行のみです");
            return;
        }

        // ヘッダー行をスキップして処理
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCSVLine(lines[i]);

            // 必要な列数をチェック（23列想定 - クリティカルダメージレート追加）
            if (values.Length < 23)
            {
                Debug.LogWarning($"行 {i + 1}: 列数が不足しています（{values.Length}/23列）");
                continue;
            }

            try
            {
                // ScriptableObjectを作成
                MonsterData monster = ScriptableObject.CreateInstance<MonsterData>();

                // CSVデータを設定
                monster.monsterId = ParseInt(values[0], $"行{i + 1} monsterId");
                monster.monsterType = values[1];
                monster.attributeType = values[2];
                monster.rarity = values[3];
                monster.monsterName = values[4];
                monster.hp = ParseInt(values[5], $"行{i + 1} hp");
                monster.offense = ParseInt(values[6], $"行{i + 1} offense");
                monster.defense = ParseInt(values[7], $"行{i + 1} defense");
                monster.speed = ParseInt(values[8], $"行{i + 1} speed");
                monster.criticalRate = ParseInt(values[9], $"行{i + 1} criticalRate");
                monster.criticalDamageRate = ParseInt(values[10], $"行{i + 1} criticalDamageRate");  // 🔴 追加
                monster.fireOffence = ParseInt(values[11], $"行{i + 1} fireOffence");  // 🔴 インデックス修正
                monster.waterOffence = ParseInt(values[12], $"行{i + 1} waterOffence");  // 🔴 インデックス修正
                monster.windOffence = ParseInt(values[13], $"行{i + 1} windOffence");  // 🔴 インデックス修正
                monster.earthOffence = ParseInt(values[14], $"行{i + 1} earthOffence");  // 🔴 インデックス修正
                monster.usedSkill1 = ParseInt(values[15], $"行{i + 1} usedSkill1");  // 🔴 インデックス修正
                monster.usedSkill2 = values[16];  // 🔴 インデックス修正
                monster.usedSkill3 = values[17];  // 🔴 インデックス修正
                // values[18] = monster_icon_path (空欄)  // 🔴 インデックス修正
                // values[19] = monster_animation_path (空欄)  // 🔴 インデックス修正
                monster.description = values[20];  // 🔴 インデックス修正
                monster.completionFlag = values[21] == "1";  // 🔴 インデックス修正
                monster.collectionFlag = values[22] == "1";  // 🔴 インデックス修正

                // アセットとして保存
                string folderPath = "Assets/GameData/Monsters";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string assetPath = $"{folderPath}/Monster_{monster.monsterId:000}_{monster.monsterName}.asset";

                // 既存アセットがある場合は上書き確認
                if (File.Exists(assetPath))
                {
                    MonsterData existingMonster = AssetDatabase.LoadAssetAtPath<MonsterData>(assetPath);
                    if (existingMonster != null)
                    {
                        // 既存のUnityアセット参照を保持
                        monster.monsterIcon = existingMonster.monsterIcon;
                        monster.monsterModel = existingMonster.monsterModel;
                        monster.animations = existingMonster.animations;
                    }
                }

                AssetDatabase.CreateAsset(monster, assetPath);
                monsterList.Add(monster);

                Debug.Log($"✅ インポート完了: {monster.monsterName} (ID:{monster.monsterId})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"行 {i + 1} の処理中にエラー: {e.Message}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"🎉 モンスターデータインポート完了！合計 {monsterList.Count} 体");
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
}

/// <summary>
/// 全モンスターデータ管理用コレクション
/// </summary>
[CreateAssetMenu(fileName = "MonsterDataCollection", menuName = "GameData/MonsterDataCollection")]
public class MonsterDataCollection : ScriptableObject
{
    [Header("全モンスターデータ")]
    public List<MonsterData> monsters = new List<MonsterData>();

    /// <summary>
    /// 全MonsterDataアセットを自動収集
    /// </summary>
    [ContextMenu("Collect All Monster Data")]
    public void CollectAllMonsterData()
    {
        monsters.Clear();

        // Assets内の全MonsterDataアセットを検索
        string[] guids = AssetDatabase.FindAssets("t:MonsterData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MonsterData monster = AssetDatabase.LoadAssetAtPath<MonsterData>(path);

            if (monster != null)
            {
                monsters.Add(monster);
            }
        }

        // IDでソート
        monsters.Sort((a, b) => a.monsterId.CompareTo(b.monsterId));

        Debug.Log($"📦 {monsters.Count} 体のモンスターデータを収集しました");

        // アセットを更新
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// IDでモンスターデータを取得
    /// </summary>
    public MonsterData GetMonsterData(int monsterId)
    {
        return monsters.Find(m => m.monsterId == monsterId);
    }
}
#endif