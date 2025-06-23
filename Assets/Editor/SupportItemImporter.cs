using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 補助材料データ（ScriptableObject版）
/// </summary>
[CreateAssetMenu(fileName = "SupportItemData", menuName = "GameData/SupportItemData")]
public class SupportItemImporter : ScriptableObject
{
    [Header("基本情報")]
    public int supportItemId;
    public string supportItemName;
    public string attributeType;
    public string rarity;
    public string description;

    [Header("スタック・効果設定")]
    public bool infiniteUse;            // 無限使用可能
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
    public Sprite supportItemIcon;      // Inspector上で手動割り当て

    [Header("フラグ")]
    public bool completionFlag;
    public bool collectionFlag;
}

/// <summary>
/// 補助材料データCSVインポーター
/// </summary>
#if UNITY_EDITOR
public class SupportItemCSVImporter
{
    /// <summary>
    /// m_support_item_data.csvからSupportItemDataアセットを生成
    /// </summary>
    [MenuItem("Tools/CSV Import/Import Support Item Data")]
    public static void ImportSupportItemData()
    {
        string csvPath = "Assets/CSV/m_support_item_data.csv";

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {csvPath}");
            return;
        }

        Debug.Log("🔄 補助材料データインポート開始...");

        List<SupportItemData> itemList = new List<SupportItemData>();
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

            // 必要な列数をチェック（28列想定 - infinite_use追加）
            if (values.Length < 28)
            {
                Debug.LogWarning($"行 {i + 1}: 列数が不足しています（{values.Length}/28列）");
                continue;
            }

            try
            {
                // ScriptableObjectを作成
                SupportItemData item = ScriptableObject.CreateInstance<SupportItemData>();

                // CSVデータを設定
                item.supportItemId = ParseInt(values[0], $"行{i + 1} supportItemId");
                item.supportItemName = values[1];
                item.attributeType = values[2];
                item.rarity = values[3];
                item.infiniteUse = values[4] == "1";  // infinite_use追加
                item.maxStackValue = ParseInt(values[5], $"行{i + 1} maxStackValue");
                item.addEnhancedValue = ParseInt(values[6], $"行{i + 1} addEnhancedValue");
                item.multiplEnhancedValue = ParseInt(values[7], $"行{i + 1} multiplEnhancedValue");
                item.reduceEnhancedValue = ParseInt(values[8], $"行{i + 1} reduceEnhancedValue");
                item.addEnhanceStamina = ParseInt(values[9], $"行{i + 1} addEnhanceStamina");
                item.reduceEnhanceStamina = ParseInt(values[10], $"行{i + 1} reduceEnhanceStamina");
                item.addEnhanceSuccessRate = ParseInt(values[11], $"行{i + 1} addEnhanceSuccessRate");
                item.reduceEnhanceSuccessRate = ParseInt(values[12], $"行{i + 1} reduceEnhanceSuccessRate");
                item.multiplStatusUp = ParseInt(values[13], $"行{i + 1} multiplStatusUp");
                item.hp = ParseInt(values[14], $"行{i + 1} hp");
                item.offense = ParseInt(values[15], $"行{i + 1} offense");
                item.defense = ParseInt(values[16], $"行{i + 1} defense");
                item.speed = ParseInt(values[17], $"行{i + 1} speed");
                item.criticalRate = ParseInt(values[18], $"行{i + 1} criticalRate");
                item.criticalDamageRate = ParseInt(values[19], $"行{i + 1} criticalDamageRate");
                item.fireOffence = ParseInt(values[20], $"行{i + 1} fireOffence");
                item.waterOffence = ParseInt(values[21], $"行{i + 1} waterOffence");
                item.windOffence = ParseInt(values[22], $"行{i + 1} windOffence");
                item.earthOffence = ParseInt(values[23], $"行{i + 1} earthOffence");
                // values[24] = enhance_item_icon_path (空欄)
                item.description = values[25];
                item.completionFlag = values[26] == "1";
                item.collectionFlag = values[27] == "1";

                // アセットとして保存
                string folderPath = "Assets/GameData/SupportItems";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string assetPath = $"{folderPath}/SupportItem_{item.supportItemId:000}_{item.supportItemName}.asset";

                // 既存アセットがある場合は上書き確認
                if (File.Exists(assetPath))
                {
                    SupportItemData existingItem = AssetDatabase.LoadAssetAtPath<SupportItemData>(assetPath);
                    if (existingItem != null)
                    {
                        // 既存のUnityアセット参照を保持
                        item.supportItemIcon = existingItem.supportItemIcon;
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

        Debug.Log($"🎉 補助材料データインポート完了！合計 {itemList.Count} 個");
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
/// 全補助材料データ管理用コレクション
/// </summary>
[CreateAssetMenu(fileName = "SupportItemDataCollection", menuName = "GameData/SupportItemDataCollection")]
public class SupportItemDataCollection : ScriptableObject
{
    [Header("全補助材料データ")]
    public List<SupportItemData> supportItems = new List<SupportItemData>();

    /// <summary>
    /// 全SupportItemDataアセットを自動収集
    /// </summary>
    [ContextMenu("Collect All Support Item Data")]
    public void CollectAllSupportItemData()
    {
        supportItems.Clear();

        // Assets内の全SupportItemDataアセットを検索
        string[] guids = AssetDatabase.FindAssets("t:SupportItemData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SupportItemData item = AssetDatabase.LoadAssetAtPath<SupportItemData>(path);

            if (item != null)
            {
                supportItems.Add(item);
            }
        }

        // IDでソート
        supportItems.Sort((a, b) => a.supportItemId.CompareTo(b.supportItemId));

        Debug.Log($"📦 {supportItems.Count} 個の補助材料データを収集しました");

        // アセットを更新
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// IDで補助材料データを取得
    /// </summary>
    public SupportItemData GetSupportItemData(int itemId)
    {
        return supportItems.Find(i => i.supportItemId == itemId);
    }
}
#endif