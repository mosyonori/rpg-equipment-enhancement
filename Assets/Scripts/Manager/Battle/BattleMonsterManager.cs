using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 戦闘中のモンスター管理専用クラス
/// 責任範囲：
/// - 複数同種モンスターの個体識別管理
/// - モンスター配置・位置制御
/// - UI表示用のモンスターデータ提供
/// データアクセス統一ルール: BattleManager → BattleMonsterManager → Data層
/// </summary>
public class BattleMonsterManager : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private float monsterSpacing = 2.0f;  // モンスター間隔
    [SerializeField] private Vector3 baseMonsterPosition = new Vector3(5f, 0f, 0f);  // モンスター基準位置

    [Header("表示名生成設定")]
    [SerializeField] private string[] displaySuffixes = { "A", "B", "C", "D", "E", "F" };  // 表示名接尾詞

    // シングルトンパターン
    public static BattleMonsterManager Instance { get; private set; }

    // イベント
    public static event System.Action<List<BattleCharacterData>> OnMonstersGenerated;
    public static event System.Action<string, Vector3> OnMonsterPositionUpdated;

    // 内部データ
    private Dictionary<int, List<BattleCharacterData>> monstersByMasterId;  // マスターID別モンスター管理
    private Dictionary<string, BattleCharacterData> monstersById;           // ID別モンスター管理
    private List<BattleCharacterData> allMonsters;                         // 全モンスターリスト
    private Dictionary<int, int> monsterInstanceCounter;                   // 同種モンスター個体カウンター

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManager();
            DebugLog("BattleMonsterManager Awake - シングルトン設定完了");
        }
        else
        {
            DebugLog("BattleMonsterManager重複インスタンス検出 - 削除");
            Destroy(gameObject);
        }
    }

    #endregion

    #region 初期化

    /// <summary>
    /// マネージャー初期化
    /// </summary>
    private void InitializeManager()
    {
        monstersByMasterId = new Dictionary<int, List<BattleCharacterData>>();
        monstersById = new Dictionary<string, BattleCharacterData>();
        allMonsters = new List<BattleCharacterData>();
        monsterInstanceCounter = new Dictionary<int, int>();

        DebugLog("BattleMonsterManager初期化完了");
    }

    /// <summary>
    /// 戦闘開始時のクリア処理
    /// </summary>
    public void ClearBattleData()
    {
        monstersByMasterId.Clear();
        monstersById.Clear();
        allMonsters.Clear();
        monsterInstanceCounter.Clear();

        DebugLog("戦闘データクリア完了");
    }

    #endregion

    #region 公開メソッド - モンスター生成・管理

    /// <summary>
    /// 戦闘用モンスターリストを生成
    /// BattleManagerのCreateBattleCharacters()から呼び出される
    /// </summary>
    /// <param name="spawnMonsterIds">出現モンスターIDリスト</param>
    /// <returns>生成されたBattleCharacterDataリスト</returns>
    public List<BattleCharacterData> GenerateBattleMonsters(List<int> spawnMonsterIds)
    {
        if (spawnMonsterIds == null || spawnMonsterIds.Count == 0)
        {
            DebugLogError("出現モンスターIDリストが空です");
            return new List<BattleCharacterData>();
        }

        DebugLog($"=== 戦闘モンスター生成開始: {spawnMonsterIds.Count}体 ===");

        // 戦闘開始時のデータクリア
        ClearBattleData();

        var generatedMonsters = new List<BattleCharacterData>();
        int overallPositionIndex = 0;

        // 各モンスターIDに対して個体を生成
        foreach (var monsterId in spawnMonsterIds)
        {
            var monster = CreateMonsterInstance(monsterId, overallPositionIndex);
            if (monster != null)
            {
                generatedMonsters.Add(monster);
                RegisterMonster(monster);
                overallPositionIndex++;
            }
        }

        // 配置位置を計算・設定
        CalculateMonsterPositions(generatedMonsters);

        DebugLog($"=== 戦闘モンスター生成完了: {generatedMonsters.Count}体 ===");
        OnMonstersGenerated?.Invoke(generatedMonsters);

        return generatedMonsters;
    }

    /// <summary>
    /// モンスターの個体を取得
    /// </summary>
    /// <param name="instanceId">個体識別ID</param>
    /// <returns>対応するBattleCharacterData</returns>
    public BattleCharacterData GetMonsterById(string instanceId)
    {
        return monstersById.TryGetValue(instanceId, out var monster) ? monster : null;
    }

    /// <summary>
    /// 同種モンスターのリストを取得
    /// </summary>
    /// <param name="masterId">モンスターマスターID</param>
    /// <returns>同種モンスターのリスト</returns>
    public List<BattleCharacterData> GetMonstersByMasterId(int masterId)
    {
        return monstersByMasterId.TryGetValue(masterId, out var monsters)
            ? new List<BattleCharacterData>(monsters)
            : new List<BattleCharacterData>();
    }

    /// <summary>
    /// 全モンスターリストを取得
    /// </summary>
    /// <returns>全モンスターのリスト</returns>
    public List<BattleCharacterData> GetAllMonsters()
    {
        return new List<BattleCharacterData>(allMonsters);
    }

    /// <summary>
    /// 生存中のモンスターリストを取得
    /// </summary>
    /// <returns>生存中のモンスターリスト</returns>
    public List<BattleCharacterData> GetAliveMonsters()
    {
        return allMonsters.Where(m => m.isAlive).ToList();
    }

    /// <summary>
    /// モンスターの撃破処理
    /// </summary>
    /// <param name="instanceId">撃破されたモンスターの個体ID</param>
    public void OnMonsterDefeated(string instanceId)
    {
        var monster = GetMonsterById(instanceId);
        if (monster != null)
        {
            monster.isAlive = false;
            DebugLog($"モンスター撃破: {monster.displayName} (ID: {instanceId})");

            // 配置の再計算が必要な場合はここで実行
            RecalculateAliveMonsterPositions();
        }
    }

    #endregion

    #region 公開メソッド - 配置制御

    /// <summary>
    /// モンスターの配置位置を更新
    /// </summary>
    /// <param name="instanceId">モンスター個体ID</param>
    /// <param name="newPosition">新しい配置位置</param>
    public void UpdateMonsterPosition(string instanceId, Vector3 newPosition)
    {
        var monster = GetMonsterById(instanceId);
        if (monster != null)
        {
            monster.SetBattlePosition(newPosition);
            OnMonsterPositionUpdated?.Invoke(instanceId, newPosition);
            DebugLog($"モンスター位置更新: {monster.displayName} → {newPosition}");
        }
    }

    /// <summary>
    /// 全モンスターの配置位置を再計算
    /// </summary>
    public void RecalculateAllMonsterPositions()
    {
        CalculateMonsterPositions(allMonsters);
    }

    /// <summary>
    /// 生存モンスターのみの配置位置を再計算
    /// </summary>
    public void RecalculateAliveMonsterPositions()
    {
        var aliveMonsters = GetAliveMonsters();
        CalculateMonsterPositions(aliveMonsters);
    }

    #endregion

    #region 内部メソッド - モンスター生成

    /// <summary>
    /// モンスター個体を生成
    /// </summary>
    /// <param name="masterId">モンスターマスターID</param>
    /// <param name="overallPosition">全体配置インデックス</param>
    /// <returns>生成されたBattleCharacterData</returns>
    private BattleCharacterData CreateMonsterInstance(int masterId, int overallPosition)
    {
        try
        {
            // モンスターマスターデータ取得
            var monsterMaster = QuestDataManager.Instance?.GetMonsterData(masterId);
            if (monsterMaster == null)
            {
                DebugLogError($"モンスターマスターデータが見つかりません: ID={masterId}");
                return CreateFallbackMonster(masterId, overallPosition);
            }

            // 同種モンスターの個体番号を取得・更新
            if (!monsterInstanceCounter.ContainsKey(masterId))
            {
                monsterInstanceCounter[masterId] = 0;
            }
            int instanceNumber = ++monsterInstanceCounter[masterId];

            // 一意の個体識別ID生成
            string instanceId = GenerateUniqueInstanceId(masterId, instanceNumber);

            // 表示用名前生成
            string displayName = GenerateDisplayName(monsterMaster.monsterName, instanceNumber);

            // スプライト読み込み（将来的にはResourceManager等で管理）
            Sprite monsterSprite = LoadMonsterSprite(monsterMaster.monsterIconPath);

            // BattleCharacterData生成
            var monster = BattleCharacterData.CreateFromMonsterMaster(
                monsterMaster,
                instanceId,
                displayName,
                overallPosition,
                monsterSprite
            );

            // 重要：characterIdにも一意のinstanceIdを設定
            monster.characterId = instanceId;

            DebugLog($"モンスター個体生成成功: {displayName} (ID: {instanceId})");
            return monster;

        }
        catch (Exception e)
        {
            DebugLogError($"モンスター個体生成エラー (ID={masterId}): {e.Message}");
            return CreateFallbackMonster(masterId, overallPosition);
        }
    }

    /// <summary>
    /// 一意の個体識別ID生成
    /// </summary>
    /// <param name="masterId">モンスターマスターID</param>
    /// <param name="instanceNumber">個体番号</param>
    /// <returns>一意の個体識別ID</returns>
    private string GenerateUniqueInstanceId(int masterId, int instanceNumber)
    {
        // 形式: "monster_{masterId}_{instanceNumber:D3}_{timestamp}"
        long timestamp = DateTime.Now.Ticks;
        return $"monster_{masterId}_{instanceNumber:D3}_{timestamp}";
    }

    /// <summary>
    /// 表示用名前生成
    /// </summary>
    /// <param name="baseName">基本名前</param>
    /// <param name="instanceNumber">個体番号</param>
    /// <returns>表示用名前</returns>
    private string GenerateDisplayName(string baseName, int instanceNumber)
    {
        if (instanceNumber == 1)
        {
            // 1体目は元の名前をそのまま使用
            return baseName;
        }
        else if (instanceNumber <= displaySuffixes.Length + 1)
        {
            // 2体目以降は接尾詞を付加 (A, B, C...)
            string suffix = displaySuffixes[instanceNumber - 2];
            return $"{baseName}{suffix}";
        }
        else
        {
            // 接尾詞を超える場合は数字を使用
            return $"{baseName}{instanceNumber}";
        }
    }

    /// <summary>
    /// モンスターを内部管理システムに登録
    /// </summary>
    /// <param name="monster">登録するモンスター</param>
    private void RegisterMonster(BattleCharacterData monster)
    {
        // ID別管理に追加
        monstersById[monster.instanceId] = monster;

        // 全モンスターリストに追加
        allMonsters.Add(monster);

        // マスターID別管理に追加
        if (!monstersByMasterId.ContainsKey(monster.masterDataId))
        {
            monstersByMasterId[monster.masterDataId] = new List<BattleCharacterData>();
        }
        monstersByMasterId[monster.masterDataId].Add(monster);

        DebugLog($"モンスター登録完了: {monster.displayName} (個体ID: {monster.instanceId})");
    }

    /// <summary>
    /// フォールバックモンスター生成
    /// </summary>
    /// <param name="masterId">モンスターマスターID</param>
    /// <param name="overallPosition">配置位置</param>
    /// <returns>フォールバックモンスター</returns>
    private BattleCharacterData CreateFallbackMonster(int masterId, int overallPosition)
    {
        if (!monsterInstanceCounter.ContainsKey(masterId))
        {
            monsterInstanceCounter[masterId] = 0;
        }
        int instanceNumber = ++monsterInstanceCounter[masterId];

        string instanceId = GenerateUniqueInstanceId(masterId, instanceNumber);
        string displayName = GenerateDisplayName($"未知モンスター{masterId}", instanceNumber);

        var fallbackMonster = new BattleCharacterData
        {
            characterId = instanceId,  // 重要: characterIdに一意のinstanceIdを設定
            instanceId = instanceId,
            characterName = $"未知モンスター{masterId}",
            displayName = displayName,
            positionIndex = overallPosition,
            isPlayer = false,
            isAlive = true,
            characterLevel = 1,
            masterDataId = masterId,

            // デフォルトステータス
            maxHp = 50,
            currentHp = 50,
            offense = 10,
            defense = 8,
            speed = 5,
            criticalRate = 5,
            criticalDamageRate = 150,

            availableSkills = new List<BattleSkillData>
            {
                new BattleSkillData
                {
                    skillId = 1,
                    skillName = "通常攻撃",
                    currentCoolTime = 0,
                    maxCoolTime = 0,
                    isUsable = true
                }
            },
            statusEffects = new List<StatusEffectData>()
        };

        DebugLogError($"フォールバックモンスター生成: {displayName} (ID: {instanceId})");
        return fallbackMonster;
    }

    #endregion

    #region 内部メソッド - 配置計算

    /// <summary>
    /// モンスターの配置位置を計算
    /// </summary>
    /// <param name="monsters">配置するモンスターリスト</param>
    private void CalculateMonsterPositions(List<BattleCharacterData> monsters)
    {
        if (monsters == null || monsters.Count == 0) return;

        DebugLog($"モンスター配置位置計算開始: {monsters.Count}体");

        for (int i = 0; i < monsters.Count; i++)
        {
            // 横一列に配置（将来的にはより複雑な配置ロジックに拡張可能）
            Vector3 position = baseMonsterPosition + new Vector3(0, i * monsterSpacing, 0);

            monsters[i].positionIndex = i;
            monsters[i].SetBattlePosition(position);

            DebugLog($"配置設定: {monsters[i].displayName} → 位置{i} {position}");
        }

        DebugLog("モンスター配置位置計算完了");
    }

    /// <summary>
    /// モンスタースプライト読み込み
    /// </summary>
    /// <param name="iconPath">アイコンパス</param>
    /// <returns>読み込まれたSprite</returns>
    private Sprite LoadMonsterSprite(string iconPath)
    {
        // 将来的にはResourceManagerやAddressableAssetSystem等で管理
        // 現在は簡易実装
        if (!string.IsNullOrEmpty(iconPath))
        {
            var sprite = Resources.Load<Sprite>(iconPath);
            if (sprite != null)
            {
                DebugLog($"モンスタースプライト読み込み成功: {iconPath}");
                return sprite;
            }
            else
            {
                DebugLogWarning($"モンスタースプライト読み込み失敗: {iconPath}");
            }
        }

        return null;
    }

    #endregion

    #region デバッグ・ログ

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[BattleMonsterManager] {message}");
        }
    }

    private void DebugLogWarning(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogWarning($"[BattleMonsterManager] {message}");
        }
    }

    private void DebugLogError(string message)
    {
        Debug.LogError($"[BattleMonsterManager] {message}");
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("現在の管理状況を表示")]
    private void ShowManagementStatus()
    {
        DebugLog($"=== モンスター管理状況 ===");
        DebugLog($"総モンスター数: {allMonsters?.Count ?? 0}");
        DebugLog($"生存モンスター数: {GetAliveMonsters().Count}");
        DebugLog($"管理中のマスターID数: {monstersByMasterId?.Keys.Count ?? 0}");

        if (monstersByMasterId != null)
        {
            foreach (var kvp in monstersByMasterId)
            {
                DebugLog($"  マスターID {kvp.Key}: {kvp.Value.Count}体");
                foreach (var monster in kvp.Value)
                {
                    DebugLog($"    - {monster.displayName} (ID: {monster.instanceId}, 生存: {monster.isAlive})");
                }
            }
        }
    }

    [ContextMenu("配置位置を再計算")]
    private void EditorRecalculatePositions()
    {
        RecalculateAllMonsterPositions();
        DebugLog("エディターから配置位置再計算を実行しました");
    }
#endif

    #endregion
}