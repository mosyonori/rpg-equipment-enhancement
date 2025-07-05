using System;
using System.Collections.Generic;
using UnityEngine;

// === Enum定義 ===

/// <summary>
/// クエストの種類
/// </summary>
public enum QuestType
{
    Story = 0,      // ストーリー
    Daily = 1,      // デイリー
    Weekly = 2,     // ウィークリー
    Event = 3       // イベント
}

/// <summary>
/// モンスターの種類
/// </summary>
public enum MonsterType
{
    Normal = 0,     // ノーマル
    Boss = 1        // ボス
}

// === データクラス定義 ===

/// <summary>
/// ドロップアイテムデータ
/// </summary>
[System.Serializable]
public class DropItemData
{
    [Header("ドロップテーブル情報")]
    public int itemTableId;        // ドロップテーブル内のアイテムID
    public string itemType;        // アイテムの種類 (Equipment/EnhanceItem/SupportItem)
    public int itemId;             // アイテムのマスターID
    public string itemName;        // アイテム名（表示用）

    [Header("ドロップ設定")]
    public int quantity;           // 1回のドロップでの個数
    public int dropRate;           // ドロップ確率（％）

    public DropItemData()
    {
        itemTableId = 0;
        itemType = "";
        itemId = 0;
        itemName = "";
        quantity = 1;
        dropRate = 0;
    }
}

// === ScriptableObject定義 ===

/// <summary>
/// クエストマスターデータ
/// CSVから読み込んだクエスト情報を管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "QuestMasterData", menuName = "GameData/Quest")]
public class QuestMasterData : ScriptableObject
{
    [Header("基本情報")]
    public int questId;                 // クエストの一意識別子
    public string questName;            // クエスト名
    public int sortOrder;               // UI表示時の並び順
    [TextArea(3, 5)]
    public string description;          // クエストの説明文
    public QuestType questType;         // クエストの種類

    [Header("参加条件")]
    public int needLevel;               // 参加に必要なプレイヤーレベル
    public int requiredClearQuest;      // 前提クエストID（-1=前提なし）
    public int requiredStamina;         // 挑戦に必要なスタミナ
    public int recommendedPower;        // 推奨戦闘力

    [Header("制限")]
    public int dailyClearLimit;         // 1日あたりのクリア制限回数（-1=無制限）
    public bool isRepeatable;           // 繰り返し挑戦可能か
    public int turnLimit;               // 戦闘のターン制限（-1=無制限）

    [Header("出現モンスター")]
    public int spawnMonsterId1;         // 出現モンスター1のID
    public int spawnMonsterId2;         // 出現モンスター2のID（-1=出現なし）
    public int spawnMonsterId3;         // 出現モンスター3のID（-1=出現なし）

    [Header("報酬")]
    public int rewardExp;               // クリア時の経験値報酬
    public int rewardGold;              // クリア時のゴールド報酬
    public int itemDropQuantity;        // ドロップテーブル参照回数
    public string dropItemTable;       // 参照するドロップテーブル名

    [Header("初回クリア報酬")]
    public string firstClearItemType;   // 初回クリア報酬のアイテム種別
    public int firstClearItemId;        // 初回クリア報酬のアイテムID
    public int firstClearItemQuantity;  // 初回クリア報酬の個数

    [Header("開催期間")]
    public string questOpenDay;         // クエスト開始日（YYYY-MM-DD）
    public string questOpenTime;        // クエスト開始時刻（HH:MM）
    public string questEndDay;          // クエスト終了日（YYYY-MM-DD）
    public string questEndTime;         // クエスト終了時刻（HH:MM）

    [Header("UI・演出")]
    public string questIconPath;        // クエストアイコンリソースパス
    public string backgroundPath;       // 戦闘背景リソースパス
    public string bgmPath;              // BGMリソースパス

    // === パブリックメソッド ===

    /// <summary>
    /// 出現モンスターIDのリストを取得
    /// -1のモンスターは除外される
    /// </summary>
    /// <returns>有効なモンスターIDのリスト</returns>
    public List<int> GetSpawnMonsterIds()
    {
        var monsters = new List<int>();

        if (spawnMonsterId1 > 0) monsters.Add(spawnMonsterId1);
        if (spawnMonsterId2 > 0) monsters.Add(spawnMonsterId2);
        if (spawnMonsterId3 > 0) monsters.Add(spawnMonsterId3);

        return monsters;
    }

    /// <summary>
    /// 前提クエストが存在するかチェック
    /// </summary>
    /// <returns>前提クエストがある場合true</returns>
    public bool HasRequiredQuest()
    {
        return requiredClearQuest > 0;
    }

    /// <summary>
    /// 無制限にクリア可能かチェック
    /// </summary>
    /// <returns>無制限の場合true</returns>
    public bool IsUnlimitedClear()
    {
        return dailyClearLimit == -1;
    }

    /// <summary>
    /// ターン制限があるかチェック
    /// </summary>
    /// <returns>ターン制限がある場合true</returns>
    public bool HasTurnLimit()
    {
        return turnLimit > 0;
    }

    /// <summary>
    /// 初回クリア報酬があるかチェック
    /// </summary>
    /// <returns>初回クリア報酬がある場合true</returns>
    public bool HasFirstClearReward()
    {
        return !string.IsNullOrEmpty(firstClearItemType) &&
               firstClearItemId > 0 &&
               firstClearItemQuantity > 0;
    }

    /// <summary>
    /// 期間限定クエストかチェック
    /// </summary>
    /// <returns>期間限定の場合true</returns>
    public bool IsTimeLimited()
    {
        return !string.IsNullOrEmpty(questOpenDay) || !string.IsNullOrEmpty(questEndDay);
    }

    /// <summary>
    /// 現在時刻でクエストが有効かチェック
    /// </summary>
    /// <returns>有効な場合true</returns>
    public bool IsQuestActive()
    {
        if (!IsTimeLimited()) return true;

        DateTime now = DateTime.Now;

        // 開始日時チェック
        if (!string.IsNullOrEmpty(questOpenDay))
        {
            if (DateTime.TryParseExact($"{questOpenDay} {questOpenTime}",
                "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime startTime))
            {
                if (now < startTime) return false;
            }
        }

        // 終了日時チェック
        if (!string.IsNullOrEmpty(questEndDay))
        {
            if (DateTime.TryParseExact($"{questEndDay} {questEndTime}",
                "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime endTime))
            {
                if (now > endTime) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// クエスト情報の文字列表現を取得（デバッグ用）
    /// </summary>
    /// <returns>クエスト情報の文字列</returns>
    public override string ToString()
    {
        return $"Quest[{questId}] {questName} (Type: {questType}, Level: {needLevel}+, Stamina: {requiredStamina})";
    }

    /// <summary>
    /// データの妥当性をチェック
    /// </summary>
    /// <returns>エラーメッセージのリスト（空の場合は正常）</returns>
    public List<string> ValidateData()
    {
        var errors = new List<string>();

        // 基本情報のチェック
        if (questId <= 0)
            errors.Add("questId must be positive");

        if (string.IsNullOrEmpty(questName))
            errors.Add("questName cannot be empty");

        if (needLevel < 0)
            errors.Add("needLevel cannot be negative");

        if (requiredStamina < 0)
            errors.Add("requiredStamina cannot be negative");

        if (recommendedPower < 0)
            errors.Add("recommendedPower cannot be negative");

        // 報酬のチェック
        if (rewardExp < 0)
            errors.Add("rewardExp cannot be negative");

        if (rewardGold < 0)
            errors.Add("rewardGold cannot be negative");

        if (itemDropQuantity < 0)
            errors.Add("itemDropQuantity cannot be negative");

        // モンスターのチェック
        if (spawnMonsterId1 <= 0)
            errors.Add("At least spawnMonsterId1 must be specified");

        // 初回報酬のチェック
        if (HasFirstClearReward())
        {
            if (firstClearItemQuantity <= 0)
                errors.Add("firstClearItemQuantity must be positive when first clear reward is set");
        }

        // 日付形式のチェック
        if (!string.IsNullOrEmpty(questOpenDay))
        {
            if (!DateTime.TryParseExact(questOpenDay, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out _))
                errors.Add($"Invalid questOpenDay format: {questOpenDay}");
        }

        if (!string.IsNullOrEmpty(questEndDay))
        {
            if (!DateTime.TryParseExact(questEndDay, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out _))
                errors.Add($"Invalid questEndDay format: {questEndDay}");
        }

        // 時刻形式のチェック
        if (!string.IsNullOrEmpty(questOpenTime))
        {
            if (!TimeSpan.TryParseExact(questOpenTime, @"hh\:mm", null, out _))
                errors.Add($"Invalid questOpenTime format: {questOpenTime}");
        }

        if (!string.IsNullOrEmpty(questEndTime))
        {
            if (!TimeSpan.TryParseExact(questEndTime, @"hh\:mm", null, out _))
                errors.Add($"Invalid questEndTime format: {questEndTime}");
        }

        return errors;
    }

#if UNITY_EDITOR
    /// <summary>
    /// エディター用：インスペクターでの表示名をカスタマイズ
    /// </summary>
    [UnityEditor.MenuItem("CONTEXT/QuestMasterData/Validate Data")]
    private static void ValidateDataContext(UnityEditor.MenuCommand command)
    {
        QuestMasterData quest = (QuestMasterData)command.context;
        var errors = quest.ValidateData();

        if (errors.Count == 0)
        {
            Debug.Log($"Quest '{quest.questName}' validation passed!");
        }
        else
        {
            Debug.LogError($"Quest '{quest.questName}' validation failed:\n" + string.Join("\n", errors));
        }
    }
#endif
}