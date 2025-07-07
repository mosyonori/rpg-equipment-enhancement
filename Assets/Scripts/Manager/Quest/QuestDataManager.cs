using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// クエストシステム専用データマネージャー
/// Quest、Monster、DropTableのマスターデータを管理
/// 既存のMasterDataManagerと独立して動作
/// </summary>
public class QuestDataManager : MonoBehaviour
{
    [Header("データ読み込み設定")]
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private bool enableDebugLog = true;

    [Header("データパス設定")]
    [SerializeField] private string questDataPath = "GameData/Quest";
    [SerializeField] private string monsterDataPath = "GameData/Monster";
    [SerializeField] private string dropTableDataPath = "GameData/DropTable";

    // イベント
    public static event System.Action OnQuestDataLoaded;
    public static event System.Action<string> OnQuestDataLoadError;

    // プロパティ
    public static QuestDataManager Instance { get; private set; }
    public bool IsDataLoaded { get; private set; }

    // マスターデータ辞書
    private Dictionary<int, QuestMasterData> questDataDict;
    private Dictionary<int, MonsterMasterData> monsterDataDict;
    private Dictionary<string, DropTableMasterData> dropTableDataDict;

    // リスト形式のデータ（フィルタ・ソート用）
    private List<QuestMasterData> questDataList;
    private List<MonsterMasterData> monsterDataList;
    private List<DropTableMasterData> dropTableDataList;

    // キャッシュ用データ
    private Dictionary<QuestType, List<QuestMasterData>> questsByType;
    private Dictionary<MonsterType, List<MonsterMasterData>> monstersByType;
    private Dictionary<RarityType, List<QuestMasterData>> questsByRarity;
    private Dictionary<RarityType, List<MonsterMasterData>> monstersByRarity;
    private Dictionary<AttributeType, List<MonsterMasterData>> monstersByAttribute;

    #region Unity Lifecycle

    private void Awake()
    {
        // シングルトン設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCollections();

            if (loadOnAwake)
            {
                LoadAllQuestData();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region 初期化

    private void InitializeCollections()
    {
        questDataDict = new Dictionary<int, QuestMasterData>();
        monsterDataDict = new Dictionary<int, MonsterMasterData>();
        dropTableDataDict = new Dictionary<string, DropTableMasterData>();

        questDataList = new List<QuestMasterData>();
        monsterDataList = new List<MonsterMasterData>();
        dropTableDataList = new List<DropTableMasterData>();

        questsByType = new Dictionary<QuestType, List<QuestMasterData>>();
        monstersByType = new Dictionary<MonsterType, List<MonsterMasterData>>();
        questsByRarity = new Dictionary<RarityType, List<QuestMasterData>>();
        monstersByRarity = new Dictionary<RarityType, List<MonsterMasterData>>();
        monstersByAttribute = new Dictionary<AttributeType, List<MonsterMasterData>>();
    }

    #endregion

    #region 公開メソッド - データ読み込み

    /// <summary>
    /// 全クエストシステムデータを読み込み
    /// </summary>
    public bool LoadAllQuestData()
    {
        try
        {
            DebugLog("クエストシステムデータの読み込みを開始します");

            bool success = true;
            success &= LoadQuestData();
            success &= LoadMonsterData();
            success &= LoadDropTableData();

            if (success)
            {
                BuildCacheData();
                IsDataLoaded = true;
                DebugLog("全クエストシステムデータの読み込みが完了しました");
                OnQuestDataLoaded?.Invoke();
            }
            else
            {
                string error = "一部のクエストシステムデータの読み込みに失敗しました";
                DebugLogError(error);
                OnQuestDataLoadError?.Invoke(error);
            }

            return success;
        }
        catch (Exception e)
        {
            string error = $"クエストシステムデータ読み込み中にエラーが発生: {e.Message}";
            DebugLogError(error);
            OnQuestDataLoadError?.Invoke(error);
            return false;
        }
    }

    /// <summary>
    /// クエストデータを読み込み
    /// </summary>
    public bool LoadQuestData()
    {
        try
        {
            var questAssets = Resources.LoadAll<QuestMasterData>(questDataPath);

            questDataDict.Clear();
            questDataList.Clear();

            foreach (var quest in questAssets)
            {
                if (quest == null) continue;

                if (questDataDict.ContainsKey(quest.questId))
                {
                    DebugLogError($"重複するクエストID: {quest.questId} ({quest.questName})");
                    continue;
                }

                questDataDict[quest.questId] = quest;
                questDataList.Add(quest);
            }

            DebugLog($"クエストデータを{questDataList.Count}個読み込みました");
            return true;
        }
        catch (Exception e)
        {
            DebugLogError($"クエストデータ読み込みエラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// モンスターデータを読み込み
    /// </summary>
    public bool LoadMonsterData()
    {
        try
        {
            var monsterAssets = Resources.LoadAll<MonsterMasterData>(monsterDataPath);

            monsterDataDict.Clear();
            monsterDataList.Clear();

            foreach (var monster in monsterAssets)
            {
                if (monster == null) continue;

                if (monsterDataDict.ContainsKey(monster.monsterId))
                {
                    DebugLogError($"重複するモンスターID: {monster.monsterId} ({monster.monsterName})");
                    continue;
                }

                monsterDataDict[monster.monsterId] = monster;
                monsterDataList.Add(monster);
            }

            DebugLog($"モンスターデータを{monsterDataList.Count}個読み込みました");
            return true;
        }
        catch (Exception e)
        {
            DebugLogError($"モンスターデータ読み込みエラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// ドロップテーブルデータを読み込み
    /// </summary>
    public bool LoadDropTableData()
    {
        try
        {
            var dropTableAssets = Resources.LoadAll<DropTableMasterData>(dropTableDataPath);

            dropTableDataDict.Clear();
            dropTableDataList.Clear();

            foreach (var dropTable in dropTableAssets)
            {
                if (dropTable == null) continue;

                if (dropTableDataDict.ContainsKey(dropTable.tableId))
                {
                    DebugLogError($"重複するドロップテーブルID: {dropTable.tableId}");
                    continue;
                }

                dropTableDataDict[dropTable.tableId] = dropTable;
                dropTableDataList.Add(dropTable);
            }

            DebugLog($"ドロップテーブルデータを{dropTableDataList.Count}個読み込みました");
            return true;
        }
        catch (Exception e)
        {
            DebugLogError($"ドロップテーブルデータ読み込みエラー: {e.Message}");
            return false;
        }
    }

    #endregion

    #region 公開メソッド - データ取得（単体）

    /// <summary>
    /// クエストデータを取得
    /// </summary>
    public QuestMasterData GetQuestData(int questId)
    {
        return questDataDict.TryGetValue(questId, out var data) ? data : null;
    }

    /// <summary>
    /// モンスターデータを取得
    /// </summary>
    public MonsterMasterData GetMonsterData(int monsterId)
    {
        return monsterDataDict.TryGetValue(monsterId, out var data) ? data : null;
    }

    /// <summary>
    /// ドロップテーブルデータを取得
    /// </summary>
    public DropTableMasterData GetDropTableData(string tableId)
    {
        return dropTableDataDict.TryGetValue(tableId, out var data) ? data : null;
    }

    #endregion

    #region 公開メソッド - データ取得（辞書・リスト）

    /// <summary>
    /// クエストデータ辞書を取得
    /// </summary>
    public Dictionary<int, QuestMasterData> GetQuestDataDict()
    {
        return new Dictionary<int, QuestMasterData>(questDataDict);
    }

    /// <summary>
    /// モンスターデータ辞書を取得
    /// </summary>
    public Dictionary<int, MonsterMasterData> GetMonsterDataDict()
    {
        return new Dictionary<int, MonsterMasterData>(monsterDataDict);
    }

    /// <summary>
    /// ドロップテーブルデータ辞書を取得
    /// </summary>
    public Dictionary<string, DropTableMasterData> GetDropTableDataDict()
    {
        return new Dictionary<string, DropTableMasterData>(dropTableDataDict);
    }

    /// <summary>
    /// クエストデータリストを取得
    /// </summary>
    public List<QuestMasterData> GetQuestDataList()
    {
        return new List<QuestMasterData>(questDataList);
    }

    /// <summary>
    /// モンスターデータリストを取得
    /// </summary>
    public List<MonsterMasterData> GetMonsterDataList()
    {
        return new List<MonsterMasterData>(monsterDataList);
    }

    /// <summary>
    /// ドロップテーブルデータリストを取得
    /// </summary>
    public List<DropTableMasterData> GetDropTableDataList()
    {
        return new List<DropTableMasterData>(dropTableDataList);
    }

    #endregion

    #region 公開メソッド - フィルタ取得

    /// <summary>
    /// クエストタイプ別のクエストデータを取得
    /// </summary>
    public List<QuestMasterData> GetQuestsByType(QuestType questType)
    {
        if (questsByType.TryGetValue(questType, out var list))
        {
            return new List<QuestMasterData>(list);
        }
        return new List<QuestMasterData>();
    }

    /// <summary>
    /// モンスタータイプ別のモンスターデータを取得
    /// </summary>
    public List<MonsterMasterData> GetMonstersByType(MonsterType monsterType)
    {
        if (monstersByType.TryGetValue(monsterType, out var list))
        {
            return new List<MonsterMasterData>(list);
        }
        return new List<MonsterMasterData>();
    }

    /// <summary>
    /// 属性別のモンスターデータを取得
    /// </summary>
    public List<MonsterMasterData> GetMonstersByAttribute(AttributeType attributeType)
    {
        if (monstersByAttribute.TryGetValue(attributeType, out var list))
        {
            return new List<MonsterMasterData>(list);
        }
        return new List<MonsterMasterData>();
    }

    /// <summary>
    /// レアリティ別のクエストデータを取得
    /// </summary>
    public List<QuestMasterData> GetQuestsByRarity(RarityType rarity)
    {
        if (questsByRarity.TryGetValue(rarity, out var list))
        {
            return new List<QuestMasterData>(list);
        }
        return new List<QuestMasterData>();
    }

    /// <summary>
    /// レアリティ別のモンスターデータを取得
    /// </summary>
    public List<MonsterMasterData> GetMonstersByRarity(RarityType rarity)
    {
        if (monstersByRarity.TryGetValue(rarity, out var list))
        {
            return new List<MonsterMasterData>(list);
        }
        return new List<MonsterMasterData>();
    }

    #endregion

    #region 公開メソッド - 検索・条件指定

    /// <summary>
    /// クエストデータを条件で検索
    /// </summary>
    public List<QuestMasterData> SearchQuests(System.Func<QuestMasterData, bool> predicate)
    {
        return questDataList.Where(predicate).ToList();
    }

    /// <summary>
    /// モンスターデータを条件で検索
    /// </summary>
    public List<MonsterMasterData> SearchMonsters(System.Func<MonsterMasterData, bool> predicate)
    {
        return monsterDataList.Where(predicate).ToList();
    }

    /// <summary>
    /// ドロップテーブルデータを条件で検索
    /// </summary>
    public List<DropTableMasterData> SearchDropTables(System.Func<DropTableMasterData, bool> predicate)
    {
        return dropTableDataList.Where(predicate).ToList();
    }

    /// <summary>
    /// 名前でクエストを検索
    /// </summary>
    public List<QuestMasterData> SearchQuestsByName(string name)
    {
        return questDataList.Where(quest => quest.questName.Contains(name)).ToList();
    }

    /// <summary>
    /// 名前でモンスターを検索
    /// </summary>
    public List<MonsterMasterData> SearchMonstersByName(string name)
    {
        return monsterDataList.Where(monster => monster.monsterName.Contains(name)).ToList();
    }

    /// <summary>
    /// 必要レベル範囲でクエストを検索
    /// </summary>
    public List<QuestMasterData> GetQuestsByLevelRange(int minLevel, int maxLevel)
    {
        return questDataList.Where(quest =>
            quest.needLevel >= minLevel && quest.needLevel <= maxLevel).ToList();
    }

    /// <summary>
    /// 推奨戦闘力範囲でクエストを検索
    /// </summary>
    public List<QuestMasterData> GetQuestsByPowerRange(int minPower, int maxPower)
    {
        return questDataList.Where(quest =>
            quest.recommendedPower >= minPower && quest.recommendedPower <= maxPower).ToList();
    }

    /// <summary>
    /// 有効なクエスト（期間内）を取得
    /// </summary>
    public List<QuestMasterData> GetActiveQuests()
    {
        return questDataList.Where(quest => quest.IsQuestActive()).ToList();
    }

    /// <summary>
    /// 繰り返し可能なクエストを取得
    /// </summary>
    public List<QuestMasterData> GetRepeatableQuests()
    {
        return questDataList.Where(quest => quest.isRepeatable).ToList();
    }

    /// <summary>
    /// ボスモンスターを取得
    /// </summary>
    public List<MonsterMasterData> GetBossMonsters()
    {
        return monsterDataList.Where(monster => monster.IsBoss()).ToList();
    }

    #endregion

    #region 公開メソッド - 統計・検証

    /// <summary>
    /// クエストシステムデータの統計情報を取得
    /// </summary>
    public QuestDataStatistics GetStatistics()
    {
        return new QuestDataStatistics
        {
            totalQuests = questDataList.Count,
            totalMonsters = monsterDataList.Count,
            totalDropTables = dropTableDataList.Count,
            questsByType = questsByType.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            monstersByType = monstersByType.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            monstersByAttribute = monstersByAttribute.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            questsByRarity = questsByRarity.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            monstersByRarity = monstersByRarity.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            activeQuestCount = GetActiveQuests().Count,
            repeatableQuestCount = GetRepeatableQuests().Count,
            bossMonsterCount = GetBossMonsters().Count
        };
    }

    /// <summary>
    /// クエストシステムデータの整合性をチェック
    /// </summary>
    public List<string> ValidateQuestSystemData()
    {
        List<string> errors = new List<string>();

        // クエストデータの検証
        foreach (var quest in questDataList)
        {
            var questErrors = quest.ValidateData();
            foreach (var error in questErrors)
            {
                errors.Add($"Quest[{quest.questId}] {quest.questName}: {error}");
            }

            // 出現モンスターの存在チェック
            var monsterIds = quest.GetSpawnMonsterIds();
            foreach (var monsterId in monsterIds)
            {
                if (!monsterDataDict.ContainsKey(monsterId))
                {
                    errors.Add($"Quest[{quest.questId}] {quest.questName}: 存在しないモンスターID {monsterId}");
                }
            }

            // ドロップテーブルの存在チェック
            if (!string.IsNullOrEmpty(quest.dropItemTable))
            {
                if (!dropTableDataDict.ContainsKey(quest.dropItemTable))
                {
                    errors.Add($"Quest[{quest.questId}] {quest.questName}: 存在しないドロップテーブル {quest.dropItemTable}");
                }
            }
        }

        // モンスターデータの検証
        foreach (var monster in monsterDataList)
        {
            var monsterErrors = monster.ValidateData();
            foreach (var error in monsterErrors)
            {
                errors.Add($"Monster[{monster.monsterId}] {monster.monsterName}: {error}");
            }
        }

        // ドロップテーブルデータの検証
        foreach (var dropTable in dropTableDataList)
        {
            var dropTableErrors = dropTable.ValidateData();
            foreach (var error in dropTableErrors)
            {
                errors.Add($"DropTable[{dropTable.tableId}]: {error}");
            }
        }

        return errors;
    }

    /// <summary>
    /// データが存在するかチェック
    /// </summary>
    public bool HasData()
    {
        return IsDataLoaded &&
               questDataList.Count > 0 &&
               monsterDataList.Count > 0 &&
               dropTableDataList.Count > 0;
    }

    #endregion

    #region 内部メソッド

    /// <summary>
    /// キャッシュデータを構築
    /// </summary>
    private void BuildCacheData()
    {
        // クエストタイプ別キャッシュ
        questsByType.Clear();
        foreach (QuestType type in Enum.GetValues(typeof(QuestType)))
        {
            questsByType[type] = questDataList.Where(quest => quest.questType == type).ToList();
        }

        // モンスタータイプ別キャッシュ
        monstersByType.Clear();
        foreach (MonsterType type in Enum.GetValues(typeof(MonsterType)))
        {
            monstersByType[type] = monsterDataList.Where(monster => monster.monsterType == type).ToList();
        }

        // モンスター属性別キャッシュ
        monstersByAttribute.Clear();
        foreach (AttributeType attribute in Enum.GetValues(typeof(AttributeType)))
        {
            monstersByAttribute[attribute] = monsterDataList.Where(monster => monster.attributeType == attribute).ToList();
        }

        // クエストレアリティ別キャッシュ（仮想的に推奨戦闘力でレアリティ判定）
        questsByRarity.Clear();
        foreach (RarityType rarity in Enum.GetValues(typeof(RarityType)))
        {
            questsByRarity[rarity] = GetQuestsByVirtualRarity(rarity);
        }

        // モンスターレアリティ別キャッシュ
        monstersByRarity.Clear();
        foreach (RarityType rarity in Enum.GetValues(typeof(RarityType)))
        {
            monstersByRarity[rarity] = monsterDataList.Where(monster => monster.rarity == rarity).ToList();
        }

        DebugLog("キャッシュデータの構築が完了しました");
    }

    /// <summary>
    /// 推奨戦闘力からクエストの仮想レアリティを判定
    /// </summary>
    private List<QuestMasterData> GetQuestsByVirtualRarity(RarityType rarity)
    {
        return rarity switch
        {
            RarityType.Common => questDataList.Where(q => q.recommendedPower < 100).ToList(),
            RarityType.Rare => questDataList.Where(q => q.recommendedPower >= 100 && q.recommendedPower < 500).ToList(),
            RarityType.Epic => questDataList.Where(q => q.recommendedPower >= 500 && q.recommendedPower < 1000).ToList(),
            RarityType.Legendary => questDataList.Where(q => q.recommendedPower >= 1000).ToList(),
            _ => new List<QuestMasterData>()
        };
    }

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[QuestDataManager] {message}");
        }
    }

    private void DebugLogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[QuestDataManager] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("クエストシステムデータを再読み込み")]
    private void ReloadQuestSystemData()
    {
        LoadAllQuestData();
    }

    [ContextMenu("クエストシステムデータ統計を表示")]
    private void ShowStatistics()
    {
        var stats = GetStatistics();
        Debug.Log(stats.ToString());
    }

    [ContextMenu("クエストシステムデータを検証")]
    private void ValidateData()
    {
        var errors = ValidateQuestSystemData();
        if (errors.Count == 0)
        {
            Debug.Log("クエストシステムデータに問題はありません");
        }
        else
        {
            Debug.LogError($"クエストシステムデータに{errors.Count}個の問題があります:\n" + string.Join("\n", errors));
        }
    }
#endif

    #endregion
}

/// <summary>
/// クエストシステムデータの統計情報
/// </summary>
[System.Serializable]
public class QuestDataStatistics
{
    public int totalQuests;
    public int totalMonsters;
    public int totalDropTables;
    public Dictionary<QuestType, int> questsByType;
    public Dictionary<MonsterType, int> monstersByType;
    public Dictionary<AttributeType, int> monstersByAttribute;
    public Dictionary<RarityType, int> questsByRarity;
    public Dictionary<RarityType, int> monstersByRarity;
    public int activeQuestCount;
    public int repeatableQuestCount;
    public int bossMonsterCount;

    public override string ToString()
    {
        var result = $@"=== クエストシステムデータ統計 ===
クエスト数: {totalQuests}
モンスター数: {totalMonsters}
ドロップテーブル数: {totalDropTables}
有効クエスト数: {activeQuestCount}
繰り返し可能クエスト数: {repeatableQuestCount}
ボスモンスター数: {bossMonsterCount}

クエストタイプ別:";

        foreach (var kv in questsByType)
        {
            result += $"\n  {kv.Key}: {kv.Value}個";
        }

        result += "\n\nモンスタータイプ別:";
        foreach (var kv in monstersByType)
        {
            result += $"\n  {kv.Key}: {kv.Value}個";
        }

        result += "\n\nモンスター属性別:";
        foreach (var kv in monstersByAttribute)
        {
            result += $"\n  {kv.Key}: {kv.Value}個";
        }

        return result;
    }
}