using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ドロップテーブルマスターデータ
/// CSVから読み込んだドロップアイテム情報を管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "DropTableMasterData", menuName = "GameData/DropTable")]
public class DropTableMasterData : ScriptableObject
{
    [Header("テーブル基本情報")]
    public string tableId;                      // ドロップテーブルID（例："m_drop_table_1"）

    [Header("ドロップアイテムリスト")]
    public List<DropItemData> dropItems;        // ドロップアイテムのリスト

    [Header("テーブル設定")]
    [Range(1, 10)]
    public int maxDropCount = 3;                // 最大ドロップ数（デフォルト3個）
    [Range(0, 100)]
    public int guaranteedDropRate = 100;        // 必ずドロップする確率（％）

    // === 初期化 ===
    private void OnEnable()
    {
        if (dropItems == null)
        {
            dropItems = new List<DropItemData>();
        }
    }

    // === パブリックメソッド ===

    /// <summary>
    /// ドロップアイテム数を取得
    /// </summary>
    /// <returns>ドロップテーブルに登録されているアイテム数</returns>
    public int GetDropItemCount()
    {
        return dropItems?.Count ?? 0;
    }

    /// <summary>
    /// 指定されたアイテムタイプのドロップアイテムを取得
    /// </summary>
    /// <param name="itemType">アイテムタイプ（Equipment/EnhanceItem/SupportItem）</param>
    /// <returns>指定タイプのドロップアイテムリスト</returns>
    public List<DropItemData> GetDropItemsByType(string itemType)
    {
        if (dropItems == null) return new List<DropItemData>();

        return dropItems.Where(item =>
            string.Equals(item.itemType, itemType, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    /// <summary>
    /// 指定されたドロップ率以上のアイテムを取得
    /// </summary>
    /// <param name="minDropRate">最小ドロップ率</param>
    /// <returns>指定ドロップ率以上のアイテムリスト</returns>
    public List<DropItemData> GetDropItemsByMinRate(int minDropRate)
    {
        if (dropItems == null) return new List<DropItemData>();

        return dropItems.Where(item => item.dropRate >= minDropRate).ToList();
    }

    /// <summary>
    /// 最もドロップ率の高いアイテムを取得
    /// </summary>
    /// <returns>最高ドロップ率のアイテム</returns>
    public DropItemData GetHighestDropRateItem()
    {
        if (dropItems == null || dropItems.Count == 0) return null;

        return dropItems.OrderByDescending(item => item.dropRate).First();
    }

    /// <summary>
    /// 最もドロップ率の低いアイテムを取得
    /// </summary>
    /// <returns>最低ドロップ率のアイテム</returns>
    public DropItemData GetLowestDropRateItem()
    {
        if (dropItems == null || dropItems.Count == 0) return null;

        return dropItems.OrderBy(item => item.dropRate).First();
    }

    /// <summary>
    /// ドロップシミュレーションを実行
    /// 実際のゲームロジックで使用するドロップ処理
    /// </summary>
    /// <param name="dropCount">ドロップ試行回数</param>
    /// <returns>ドロップしたアイテムのリスト</returns>
    public List<DropResult> SimulateDrop(int dropCount)
    {
        var results = new List<DropResult>();

        if (dropItems == null || dropItems.Count == 0) return results;

        for (int i = 0; i < dropCount; i++)
        {
            foreach (var item in dropItems)
            {
                // ドロップ判定
                if (UnityEngine.Random.Range(0, 100) < item.dropRate)
                {
                    results.Add(new DropResult
                    {
                        itemType = item.itemType,
                        itemId = item.itemId,
                        itemName = item.itemName,
                        quantity = item.quantity,
                        dropRate = item.dropRate
                    });
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 確定ドロップシミュレーション（テスト用）
    /// 確率に関係なく全アイテムを返す
    /// </summary>
    /// <returns>全ドロップアイテムのリスト</returns>
    public List<DropResult> SimulateGuaranteedDrop()
    {
        var results = new List<DropResult>();

        if (dropItems == null) return results;

        foreach (var item in dropItems)
        {
            results.Add(new DropResult
            {
                itemType = item.itemType,
                itemId = item.itemId,
                itemName = item.itemName,
                quantity = item.quantity,
                dropRate = 100 // 確定ドロップなので100%
            });
        }

        return results;
    }

    /// <summary>
    /// アイテムタイプ別の統計情報を取得
    /// </summary>
    /// <returns>アイテムタイプ別統計</returns>
    public Dictionary<string, DropTableStatistics> GetStatistics()
    {
        var stats = new Dictionary<string, DropTableStatistics>();

        if (dropItems == null) return stats;

        var groupedItems = dropItems.GroupBy(item => item.itemType);

        foreach (var group in groupedItems)
        {
            var items = group.ToList();
            stats[group.Key] = new DropTableStatistics
            {
                itemType = group.Key,
                itemCount = items.Count,
                averageDropRate = items.Average(item => item.dropRate),
                minDropRate = items.Min(item => item.dropRate),
                maxDropRate = items.Max(item => item.dropRate),
                totalQuantity = items.Sum(item => item.quantity)
            };
        }

        return stats;
    }

    /// <summary>
    /// 指定されたアイテムIDがテーブルに存在するかチェック
    /// </summary>
    /// <param name="itemId">アイテムID</param>
    /// <param name="itemType">アイテムタイプ（省略可）</param>
    /// <returns>存在する場合true</returns>
    public bool ContainsItem(int itemId, string itemType = null)
    {
        if (dropItems == null) return false;

        return dropItems.Any(item =>
            item.itemId == itemId &&
            (string.IsNullOrEmpty(itemType) ||
             string.Equals(item.itemType, itemType, StringComparison.OrdinalIgnoreCase))
        );
    }

    /// <summary>
    /// ドロップテーブルの期待値を計算
    /// </summary>
    /// <returns>1回のドロップ試行での期待ドロップ数</returns>
    public float CalculateExpectedDropCount()
    {
        if (dropItems == null) return 0f;

        return dropItems.Sum(item => (item.dropRate / 100f) * item.quantity);
    }

    /// <summary>
    /// レアアイテム（低ドロップ率）のリストを取得
    /// </summary>
    /// <param name="rareThreshold">レア判定の閾値（デフォルト20%以下）</param>
    /// <returns>レアアイテムのリスト</returns>
    public List<DropItemData> GetRareItems(int rareThreshold = 20)
    {
        if (dropItems == null) return new List<DropItemData>();

        return dropItems.Where(item => item.dropRate <= rareThreshold).ToList();
    }

    /// <summary>
    /// ドロップテーブル情報の文字列表現を取得（デバッグ用）
    /// </summary>
    /// <returns>ドロップテーブル情報の文字列</returns>
    public override string ToString()
    {
        int itemCount = GetDropItemCount();
        float expectedDrop = CalculateExpectedDropCount();
        return $"DropTable[{tableId}] Items: {itemCount}, Expected: {expectedDrop:F2} per drop";
    }

    /// <summary>
    /// データの妥当性をチェック
    /// </summary>
    /// <returns>エラーメッセージのリスト（空の場合は正常）</returns>
    public List<string> ValidateData()
    {
        var errors = new List<string>();

        // 基本情報のチェック
        if (string.IsNullOrEmpty(tableId))
            errors.Add("tableId cannot be empty");

        if (dropItems == null || dropItems.Count == 0)
            errors.Add("dropItems list cannot be empty");

        if (maxDropCount <= 0)
            errors.Add("maxDropCount must be positive");

        if (guaranteedDropRate < 0 || guaranteedDropRate > 100)
            errors.Add("guaranteedDropRate must be between 0-100");

        // 各ドロップアイテムのチェック
        if (dropItems != null)
        {
            for (int i = 0; i < dropItems.Count; i++)
            {
                var item = dropItems[i];
                string prefix = $"dropItems[{i}]";

                if (item.itemTableId <= 0)
                    errors.Add($"{prefix}: itemTableId must be positive");

                if (string.IsNullOrEmpty(item.itemType))
                    errors.Add($"{prefix}: itemType cannot be empty");

                if (item.itemId <= 0)
                    errors.Add($"{prefix}: itemId must be positive");

                if (string.IsNullOrEmpty(item.itemName))
                    errors.Add($"{prefix}: itemName cannot be empty");

                if (item.quantity <= 0)
                    errors.Add($"{prefix}: quantity must be positive");

                if (item.dropRate < 0 || item.dropRate > 100)
                    errors.Add($"{prefix}: dropRate must be between 0-100");
            }

            // 重複チェック
            var duplicates = dropItems
                .GroupBy(item => new { item.itemType, item.itemId })
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

            foreach (var duplicate in duplicates)
            {
                errors.Add($"Duplicate item found: {duplicate.itemType} ID {duplicate.itemId}");
            }
        }

        return errors;
    }

    /// <summary>
    /// ドロップテーブルの最適化提案を取得
    /// </summary>
    /// <returns>最適化提案のリスト</returns>
    public List<string> GetOptimizationSuggestions()
    {
        var suggestions = new List<string>();

        if (dropItems == null) return suggestions;

        // ドロップ率0%のアイテムチェック
        var zeroRateItems = dropItems.Where(item => item.dropRate == 0).ToList();
        if (zeroRateItems.Count > 0)
        {
            suggestions.Add($"{zeroRateItems.Count} items have 0% drop rate and can be removed");
        }

        // 非常に低いドロップ率のアイテムチェック
        var veryLowRateItems = dropItems.Where(item => item.dropRate > 0 && item.dropRate < 5).ToList();
        if (veryLowRateItems.Count > 0)
        {
            suggestions.Add($"{veryLowRateItems.Count} items have very low drop rates (<5%)");
        }

        // ドロップ率100%のアイテムチェック
        var guaranteedItems = dropItems.Where(item => item.dropRate == 100).ToList();
        if (guaranteedItems.Count > 0)
        {
            suggestions.Add($"{guaranteedItems.Count} items are guaranteed drops (100% rate)");
        }

        // 期待値チェック
        float expectedDrop = CalculateExpectedDropCount();
        if (expectedDrop > maxDropCount * 2)
        {
            suggestions.Add("Expected drop count is very high, consider reducing drop rates");
        }
        else if (expectedDrop < 0.5f)
        {
            suggestions.Add("Expected drop count is very low, consider increasing drop rates");
        }

        return suggestions;
    }

#if UNITY_EDITOR
    /// <summary>
    /// エディター用：インスペクターでの表示名をカスタマイズ
    /// </summary>
    [UnityEditor.MenuItem("CONTEXT/DropTableMasterData/Validate Data")]
    private static void ValidateDataContext(UnityEditor.MenuCommand command)
    {
        DropTableMasterData dropTable = (DropTableMasterData)command.context;
        var errors = dropTable.ValidateData();

        if (errors.Count == 0)
        {
            Debug.Log($"DropTable '{dropTable.tableId}' validation passed!");
        }
        else
        {
            Debug.LogError($"DropTable '{dropTable.tableId}' validation failed:\n" + string.Join("\n", errors));
        }
    }

    [UnityEditor.MenuItem("CONTEXT/DropTableMasterData/Show Statistics")]
    private static void ShowStatisticsContext(UnityEditor.MenuCommand command)
    {
        DropTableMasterData dropTable = (DropTableMasterData)command.context;
        var stats = dropTable.GetStatistics();

        Debug.Log($"=== DropTable '{dropTable.tableId}' Statistics ===");
        foreach (var stat in stats)
        {
            Debug.Log($"{stat.Key}: {stat.Value.itemCount} items, Avg rate: {stat.Value.averageDropRate:F1}%");
        }
        Debug.Log($"Expected drops per try: {dropTable.CalculateExpectedDropCount():F2}");
    }

    [UnityEditor.MenuItem("CONTEXT/DropTableMasterData/Simulate Drop")]
    private static void SimulateDropContext(UnityEditor.MenuCommand command)
    {
        DropTableMasterData dropTable = (DropTableMasterData)command.context;
        var results = dropTable.SimulateDrop(1);

        Debug.Log($"=== DropTable '{dropTable.tableId}' Drop Simulation ===");
        if (results.Count == 0)
        {
            Debug.Log("No items dropped");
        }
        else
        {
            foreach (var result in results)
            {
                Debug.Log($"Dropped: {result.itemName} x{result.quantity} ({result.itemType})");
            }
        }
    }

    [UnityEditor.MenuItem("CONTEXT/DropTableMasterData/Get Optimization Suggestions")]
    private static void GetOptimizationSuggestionsContext(UnityEditor.MenuCommand command)
    {
        DropTableMasterData dropTable = (DropTableMasterData)command.context;
        var suggestions = dropTable.GetOptimizationSuggestions();

        Debug.Log($"=== DropTable '{dropTable.tableId}' Optimization Suggestions ===");
        if (suggestions.Count == 0)
        {
            Debug.Log("No optimization suggestions - table looks good!");
        }
        else
        {
            foreach (var suggestion in suggestions)
            {
                Debug.Log($"• {suggestion}");
            }
        }
    }
#endif
}

// === 補助クラス定義 ===

/// <summary>
/// ドロップ結果データ
/// </summary>
[System.Serializable]
public class DropResult
{
    public string itemType;         // アイテムタイプ
    public int itemId;              // アイテムID
    public string itemName;         // アイテム名
    public int quantity;            // ドロップ数量
    public int dropRate;            // 使用されたドロップ率

    public override string ToString()
    {
        return $"{itemName} x{quantity} ({itemType})";
    }
}

/// <summary>
/// ドロップテーブル統計情報
/// </summary>
[System.Serializable]
public class DropTableStatistics
{
    public string itemType;         // アイテムタイプ
    public int itemCount;           // アイテム数
    public double averageDropRate;  // 平均ドロップ率
    public int minDropRate;         // 最小ドロップ率
    public int maxDropRate;         // 最大ドロップ率
    public int totalQuantity;       // 合計数量

    public override string ToString()
    {
        return $"{itemType}: {itemCount} items, Rate: {minDropRate}-{maxDropRate}% (avg: {averageDropRate:F1}%)";
    }
}