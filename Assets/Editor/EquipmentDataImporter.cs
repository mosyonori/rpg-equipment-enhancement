using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 装備データ（ScriptableObject版）
/// </summary>
[CreateAssetMenu(fileName = "EquipmentData", menuName = "GameData/EquipmentData")]
public class EquipmentData : ScriptableObject
{
    [Header("基本情報")]
    public int equipmentId;
    public string equipmentName;
    public string equipmentType;
    public string rarity;
    public string description;

    [Header("強化設定")]
    public int baseEnhancedValue;
    public int maxEnhancedValue;
    public int minEnhancedValue;
    public int baseEnhanceStamina;
    public int maxEnhanceStamina;
    public int minEnhanceStamina;
    public int baseEnhanceSuccessRate;

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

    [Header("開放要素")]
    public float equipmentOpenSkillId;
    public string equipmentOpenCharacterId;

    [Header("Unityアセット（手動割り当て）")]
    public Sprite equipmentIcon;        // Inspector上で手動割り当て
    public GameObject equipmentModel;   // Inspector上で手動割り当て

    [Header("フラグ")]
    public bool completionFlag;
    public bool collectionFlag;
}

/// <summary>
/// 装備データCSVインポーター
/// </summary>
#if UNITY_EDITOR
public class EquipmentCSVImporter
{
    /// <summary>
    /// m_equipment_data.csvからEquipmentDataアセットを生成
    /// </summary>
    [MenuItem("Tools/CSV Import/Import Equipment Data")]
    public static void ImportEquipmentData()
    {
        string csvPath = "Assets/CSV/m_equipment_data.csv";

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {csvPath}");
            return;
        }

        Debug.Log("🔄 装備データインポート開始...");

        List<EquipmentData> equipmentList = new List<EquipmentData>();
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

            // 必要な列数をチェック（27列想定）
            if (values.Length < 27)
            {
                Debug.LogWarning($"行 {i + 1}: 列数が不足しています（{values.Length}/27列）");
                continue;
            }

            try
            {
                // ScriptableObjectを作成
                EquipmentData equipment = ScriptableObject.CreateInstance<EquipmentData>();

                // CSVデータを設定
                equipment.equipmentId = ParseInt(values[0], $"行{i + 1} equipmentId");
                equipment.equipmentName = values[1];
                equipment.equipmentType = values[2];
                equipment.rarity = values[3];
                equipment.baseEnhancedValue = ParseInt(values[4], $"行{i + 1} baseEnhancedValue");
                equipment.maxEnhancedValue = ParseInt(values[5], $"行{i + 1} maxEnhancedValue");
                equipment.minEnhancedValue = ParseInt(values[6], $"行{i + 1} minEnhancedValue");
                equipment.baseEnhanceStamina = ParseInt(values[7], $"行{i + 1} baseEnhanceStamina");
                equipment.maxEnhanceStamina = ParseInt(values[8], $"行{i + 1} maxEnhanceStamina");
                equipment.minEnhanceStamina = ParseInt(values[9], $"行{i + 1} minEnhanceStamina");
                equipment.baseEnhanceSuccessRate = ParseInt(values[10], $"行{i + 1} baseEnhanceSuccessRate");
                equipment.hp = ParseInt(values[11], $"行{i + 1} hp");
                equipment.offense = ParseInt(values[12], $"行{i + 1} offense");
                equipment.defense = ParseInt(values[13], $"行{i + 1} defense");
                equipment.speed = ParseInt(values[14], $"行{i + 1} speed");
                equipment.criticalRate = ParseInt(values[15], $"行{i + 1} criticalRate");
                equipment.criticalDamageRate = ParseInt(values[16], $"行{i + 1} criticalDamageRate");
                equipment.fireOffence = ParseInt(values[17], $"行{i + 1} fireOffence");
                equipment.waterOffence = ParseInt(values[18], $"行{i + 1} waterOffence");
                equipment.windOffence = ParseInt(values[19], $"行{i + 1} windOffence");
                equipment.earthOffence = ParseInt(values[20], $"行{i + 1} earthOffence");
                equipment.equipmentOpenSkillId = ParseFloat(values[21], $"行{i + 1} equipmentOpenSkillId");
                equipment.equipmentOpenCharacterId = values[22];
                // values[23] = equipment_icon_path (空欄)
                equipment.description = values[24];
                equipment.completionFlag = values[25] == "1";
                equipment.collectionFlag = values[26] == "1";

                // アセットとして保存
                string folderPath = "Assets/GameData/Equipment";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string assetPath = $"{folderPath}/Equipment_{equipment.equipmentId:000}_{equipment.equipmentName}.asset";

                // 既存アセットがある場合は上書き確認
                if (File.Exists(assetPath))
                {
                    EquipmentData existingEquipment = AssetDatabase.LoadAssetAtPath<EquipmentData>(assetPath);
                    if (existingEquipment != null)
                    {
                        // 既存のUnityアセット参照を保持
                        equipment.equipmentIcon = existingEquipment.equipmentIcon;
                        equipment.equipmentModel = existingEquipment.equipmentModel;
                    }
                }

                AssetDatabase.CreateAsset(equipment, assetPath);
                equipmentList.Add(equipment);

                Debug.Log($"✅ インポート完了: {equipment.equipmentName} (ID:{equipment.equipmentId})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"行 {i + 1} の処理中にエラー: {e.Message}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"🎉 装備データインポート完了！合計 {equipmentList.Count} 個");
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
/// 全装備データ管理用コレクション
/// </summary>
[CreateAssetMenu(fileName = "EquipmentDataCollection", menuName = "GameData/EquipmentDataCollection")]
public class EquipmentDataCollection : ScriptableObject
{
    [Header("全装備データ")]
    public List<EquipmentData> equipment = new List<EquipmentData>();

    /// <summary>
    /// 全EquipmentDataアセットを自動収集
    /// </summary>
    [ContextMenu("Collect All Equipment Data")]
    public void CollectAllEquipmentData()
    {
        equipment.Clear();

        // Assets内の全EquipmentDataアセットを検索
        string[] guids = AssetDatabase.FindAssets("t:EquipmentData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EquipmentData equip = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);

            if (equip != null)
            {
                equipment.Add(equip);
            }
        }

        // IDでソート
        equipment.Sort((a, b) => a.equipmentId.CompareTo(b.equipmentId));

        Debug.Log($"📦 {equipment.Count} 個の装備データを収集しました");

        // アセットを更新
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// IDで装備データを取得
    /// </summary>
    public EquipmentData GetEquipmentData(int equipmentId)
    {
        return equipment.Find(e => e.equipmentId == equipmentId);
    }
}
#endif