using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 強化アイテムデータ（ScriptableObject版）
/// </summary>
[CreateAssetMenu(fileName = "EnhancementItemData", menuName = "GameData/EnhancementItemData")]
public class EnhancementItemData : ScriptableObject
{
    [Header("基本情報")]
    public int enhanceItemId;
    public string enhanceItemName;
    public string attributeType;
    public string rarity;
    public string description;

    [Header("スタック・使用設定")]
    public int maxStackValue;
    public int addEnhancedValue;
    public int reduceEnhancedValue;
    public int addEnhanceStamina;
    public int reduceEnhanceStamina;
    public int enhanceSuccessRate;

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
    public Sprite enhanceItemIcon;      // Inspector上で手動割り当て

    [Header("フラグ")]
    public bool completionFlag;
    public bool collectionFlag;
}

/// <summary>
/// 強化アイテムデータCSVインポーター
/// </summary>
#if UNITY_EDITOR
public class EnhancementItemCSVImporter
{
    /// <summary>
    /// m_enhance_item_data.csvからEnhancementItemDataアセットを生成
    /// </summary>
    [MenuItem("Tools/CSV Import/Import Enhancement Item Data")]
    public static void ImportEnhancementItemData()
    {
        string csvPath = "Assets/CSV/m_enhance_item_data.csv";

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"CSVファイルが見つかりません: {csvPath}");
            return;
        }

        Debug.Log("🔄 強化アイテムデータインポート開始...");

        List<EnhancementItemData> itemList = new List<EnhancementItemData>();
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

            // 必要な列数をチェック（24列想定）
            if (values.Length < 24)
            {
                Debug.LogWarning($"行 {i + 1}: 列数が不足しています（{values.Length}/24列）");
                continue;
            }

            try
            {
                // ScriptableObjectを作成
                EnhancementItemData item = ScriptableObject.CreateInstance<EnhancementItemData>();

                // CSVデータを設定
                item.enhanceItemId = ParseInt(values[0], $"行{i + 1} enhanceItemId");
                item.enhanceItemName = values[1];
                item.attributeType = values[2];
                item.rarity = values[3];
                item.maxStackValue = ParseInt(values[4], $"行{i + 1} maxStackValue");
                item.addEnhancedValue = ParseInt(values[5], $"行{i + 1} addEnhancedValue");
                item.reduceEnhancedValue = ParseInt(values[6], $"行{i + 1} reduceEnhancedValue");
                item.addEnhanceStamina = ParseInt(values[7], $"行{i + 1} addEnhanceStamina");
                item.reduceEnhanceStamina = ParseInt(values[8], $"行{i + 1} reduceEnhanceStamina");
                item.enhanceSuccessRate = ParseInt(values[9], $"行{i + 1} enhanceSuccessRate");
                item.hp = ParseInt(values[10], $"行{i + 1} hp");
                item.offense = ParseInt(values[11], $"行{i + 1} offense");
                item.defense = ParseInt(values[12], $"行{i + 1} defense");
                item.speed = ParseInt(values[13], $"行{i + 1} speed");
                item.criticalRate = ParseInt(values[14], $"行{i + 1} criticalRate");
                item.criticalDamageRate = ParseInt(values[15], $"行{i + 1} criticalDamageRate");
                item.fireOffence = ParseInt(values[16], $"行{i + 1} fireOffence");
                item.waterOffence = ParseInt(values[17], $"行{i + 1} waterOffence");
                item.windOffence = ParseInt(values[18], $"行{i + 1} windOffence");
                item.earthOffence = ParseInt(values[19], $"行{i + 1} earthOffence");
                // values[20] = enhance_item_icon_path (空欄)
                item.description = values[21];
                item.completionFlag = values[22] == "1";
                item.collectionFlag = values[23] == "1";

                // アセットとして保存
                string folderPath = "Assets/GameData/EnhancementItems";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string assetPath = $"{folderPath}/EnhancementItem_{item.enhanceItemId:000}_{item.enhanceItemName}.asset";

                // 既存アセットがある場合は上書き確認
                if (File.Exists(assetPath))
                {
                    EnhancementItemData existingItem = AssetDatabase.LoadAssetAtPath<EnhancementItemData>(assetPath);
                    if (existingItem != null)
                    {
                        // 既存のUnityアセット参照を保持
                        item.enhanceItemIcon = existingItem.enhanceItemIcon;
                    }
                }

                AssetDatabase.CreateAsset(item, assetPath);
                itemList.Add(item);

                Debug.Log($"✅ インポート完了: {item.enhanceItemName} (ID:{item.enhanceItemId})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"行 {i + 1} の処理中にエラー: {e.Message}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"🎉 強化アイテムデータインポート完了！合計 {itemList.Count} 個");
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
/// 全強化アイテムデータ管理用コレクション
/// </summary>
[CreateAssetMenu(fileName = "EnhancementItemDataCollection", menuName = "GameData/EnhancementItemDataCollection")]
public class EnhancementItemDataCollection : ScriptableObject
{
    [Header("全強化アイテムデータ")]
    public List<EnhancementItemData> enhancementItems = new List<EnhancementItemData>();

    /// <summary>
    /// 全EnhancementItemDataアセットを自動収集
    /// </summary>
    [ContextMenu("Collect All Enhancement Item Data")]
    public void CollectAllEnhancementItemData()
    {
        enhancementItems.Clear();

        // Assets内の全EnhancementItemDataアセットを検索
        string[] guids = AssetDatabase.FindAssets("t:EnhancementItemData");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnhancementItemData item = AssetDatabase.LoadAssetAtPath<EnhancementItemData>(path);

            if (item != null)
            {
                enhancementItems.Add(item);
            }
        }

        // IDでソート
        enhancementItems.Sort((a, b) => a.enhanceItemId.CompareTo(b.enhanceItemId));

        Debug.Log($"📦 {enhancementItems.Count} 個の強化アイテムデータを収集しました");

        // アセットを更新
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// IDで強化アイテムデータを取得
    /// </summary>
    public EnhancementItemData GetEnhancementItemData(int itemId)
    {
        return enhancementItems.Find(i => i.enhanceItemId == itemId);
    }
}
#endif