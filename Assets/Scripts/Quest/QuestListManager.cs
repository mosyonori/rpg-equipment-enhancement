using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// クエスト管理のコアManager
/// クエストの開始、進行、完了処理を管理
/// UI層からのリクエストを受けてビジネスロジックを実行
/// </summary>
public class QuestListManager : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool autoInitialize = true;

    [Header("制限設定")]
    [SerializeField] private int maxConcurrentQuests = 10; // 同時実行可能クエスト数

    // イベント
    public static event Action<QuestStartResult> OnQuestStarted;
    public static event Action<int, QuestDisplayData> OnQuestCompleted;
    public static event Action OnQuestListUpdated;
    public static event Action<string> OnQuestError;

    // プロパティ
    public static QuestListManager Instance { get; private set; }
    public bool IsInitialized { get; private set; }

    // 内部状態
    private Dictionary<int, QuestDisplayData> availableQuests;
    private HashSet<int> ongoingQuestIds;
    private DateTime lastUpdateTime;

    #region Unity Lifecycle

    private void Awake()
    {
        // シングルトン設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (autoInitialize)
            {
                Initialize();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 依存関係の確認
        if (!ValidateDependencies())
        {
            LogError("必要な依存関係が満たされていません");
            return;
        }

        // 定期更新開始
        InvokeRepeating(nameof(UpdateQuestStates), 1f, 30f); // 30秒ごとに更新
    }

    #endregion

    #region 初期化

    /// <summary>
    /// QuestListManagerを初期化
    /// </summary>
    public bool Initialize()
    {
        try
        {
            Log("QuestListManager初期化開始");

            availableQuests = new Dictionary<int, QuestDisplayData>();
            ongoingQuestIds = new HashSet<int>();
            lastUpdateTime = DateTime.Now;

            // クエストリストの初期構築
            RefreshQuestList();

            IsInitialized = true;
            Log("QuestListManager初期化完了");
            return true;
        }
        catch (Exception e)
        {
            LogError($"QuestListManager初期化エラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 依存関係の検証
    /// </summary>
    private bool ValidateDependencies()
    {
        if (QuestDataManager.Instance == null)
        {
            LogError("QuestDataManagerが見つかりません");
            return false;
        }

        if (!QuestDataManager.Instance.IsDataLoaded)
        {
            LogError("QuestDataManagerのデータが読み込まれていません");
            return false;
        }

        if (SaveDataManager.Instance == null)
        {
            LogError("SaveDataManagerが見つかりません");
            return false;
        }

        if (!SaveDataManager.Instance.IsDataLoaded)
        {
            LogError("SaveDataManagerのデータが読み込まれていません");
            return false;
        }

        return true;
    }

    #endregion

    #region 公開メソッド - クエストリスト取得

    /// <summary>
    /// 利用可能なクエストリストを取得
    /// </summary>
    /// <returns>利用可能なクエストのリスト</returns>
    public List<QuestDisplayData> GetAvailableQuests()
    {
        if (!IsInitialized) return new List<QuestDisplayData>();

        return availableQuests.Values
            .Where(quest => quest.isAvailable)
            .OrderBy(quest => quest.sortOrder)
            .ToList();
    }

    /// <summary>
    /// 指定タイプのクエストリストを取得
    /// </summary>
    /// <param name="questType">クエストタイプ</param>
    /// <returns>指定タイプのクエストリスト</returns>
    public List<QuestDisplayData> GetQuestsByType(QuestType questType)
    {
        if (!IsInitialized) return new List<QuestDisplayData>();

        return availableQuests.Values
            .Where(quest => quest.questType == questType && quest.isAvailable)
            .OrderBy(quest => quest.sortOrder)
            .ToList();
    }

    /// <summary>
    /// プレイヤーレベルに適したクエストを取得
    /// </summary>
    /// <param name="playerLevel">プレイヤーレベル</param>
    /// <returns>適したクエストのリスト</returns>
    public List<QuestDisplayData> GetQuestsForPlayerLevel(int playerLevel)
    {
        if (!IsInitialized) return new List<QuestDisplayData>();

        return availableQuests.Values
            .Where(quest => quest.isAvailable && quest.needLevel <= playerLevel)
            .OrderBy(quest => quest.needLevel)
            .ToList();
    }

    /// <summary>
    /// 新しいクエストを取得
    /// </summary>
    /// <returns>新しいクエストのリスト</returns>
    public List<QuestDisplayData> GetNewQuests()
    {
        if (!IsInitialized) return new List<QuestDisplayData>();

        return availableQuests.Values
            .Where(quest => quest.isNew && quest.isAvailable)
            .OrderBy(quest => quest.sortOrder)
            .ToList();
    }

    /// <summary>
    /// おすすめクエストを取得（プレイヤーの戦闘力に基づく）
    /// </summary>
    /// <param name="playerPower">プレイヤーの戦闘力</param>
    /// <returns>おすすめクエストのリスト</returns>
    public List<QuestDisplayData> GetRecommendedQuests(int playerPower)
    {
        if (!IsInitialized) return new List<QuestDisplayData>();

        // プレイヤー戦闘力の80%〜120%の範囲をおすすめとする
        int minPower = Mathf.RoundToInt(playerPower * 0.8f);
        int maxPower = Mathf.RoundToInt(playerPower * 1.2f);

        return availableQuests.Values
            .Where(quest => quest.isAvailable &&
                           quest.recommendedPower >= minPower &&
                           quest.recommendedPower <= maxPower)
            .OrderBy(quest => Mathf.Abs(quest.recommendedPower - playerPower))
            .ToList();
    }

    #endregion

    #region 公開メソッド - クエスト詳細

    /// <summary>
    /// クエスト詳細データを取得
    /// </summary>
    /// <param name="questId">クエストID</param>
    /// <returns>クエスト詳細データ</returns>
    public QuestDetailData GetQuestDetail(int questId)
    {
        var questMaster = QuestDataManager.Instance.GetQuestData(questId);
        if (questMaster == null) return null;

        var userData = GetUserQuestData(questId);
        var displayData = availableQuests.TryGetValue(questId, out var display) ? display : null;

        return new QuestDetailData
        {
            questMaster = questMaster,
            userQuestData = userData,
            displayData = displayData,
            spawnMonsters = GetQuestMonsters(questId),
            dropTable = GetQuestDropTable(questId),
            isAvailable = CanStartQuest(questId),
            availabilityReason = GetQuestAvailabilityReason(questId)
        };
    }

    /// <summary>
    /// クエストに出現するモンスター情報を取得
    /// </summary>
    /// <param name="questId">クエストID</param>
    /// <returns>出現モンスターのリスト</returns>
    public List<MonsterMasterData> GetQuestMonsters(int questId)
    {
        var questMaster = QuestDataManager.Instance.GetQuestData(questId);
        if (questMaster == null) return new List<MonsterMasterData>();

        var monsters = new List<MonsterMasterData>();
        var monsterIds = questMaster.GetSpawnMonsterIds();

        foreach (var monsterId in monsterIds)
        {
            var monster = QuestDataManager.Instance.GetMonsterData(monsterId);
            if (monster != null)
            {
                monsters.Add(monster);
            }
        }

        return monsters;
    }

    /// <summary>
    /// クエストのドロップテーブルを取得
    /// </summary>
    /// <param name="questId">クエストID</param>
    /// <returns>ドロップテーブル</returns>
    public DropTableMasterData GetQuestDropTable(int questId)
    {
        var questMaster = QuestDataManager.Instance.GetQuestData(questId);
        if (questMaster == null || string.IsNullOrEmpty(questMaster.dropItemTable))
            return null;

        return QuestDataManager.Instance.GetDropTableData(questMaster.dropItemTable);
    }

    #endregion

    #region 公開メソッド - クエスト開始・制御

    /// <summary>
    /// クエストを開始できるかチェック
    /// </summary>
    /// <param name="questId">クエストID</param>
    /// <returns>開始可能な場合true</returns>
    public bool CanStartQuest(int questId)
    {
        var result = ValidateQuestStart(questId);
        return result.isValid;
    }

    /// <summary>
    /// クエスト開始の妥当性チェック（詳細）
    /// </summary>
    /// <param name="questId">クエストID</param>
    /// <returns>検証結果</returns>
    public QuestValidationResult ValidateQuestStart(int questId)
    {
        var result = new QuestValidationResult { questId = questId };

        // マスターデータの存在チェック
        var questMaster = QuestDataManager.Instance.GetQuestData(questId);
        if (questMaster == null)
        {
            result.AddError("クエストデータが見つかりません");
            return result;
        }

        // プレイヤーデータの取得
        var saveData = SaveDataManager.Instance.CurrentSaveData;
        if (saveData == null)
        {
            result.AddError("プレイヤーデータが見つかりません");
            return result;
        }

        // プレイヤーレベルチェック
        if (saveData.playerLevel < questMaster.needLevel)
        {
            result.AddError($"必要レベル不足 (必要: Lv.{questMaster.needLevel}, 現在: Lv.{saveData.playerLevel})");
        }

        // スタミナチェック
        if (saveData.currentStamina < questMaster.requiredStamina)
        {
            result.AddError($"スタミナ不足 (必要: {questMaster.requiredStamina}, 現在: {saveData.currentStamina})");
        }

        // 前提クエストチェック
        if (questMaster.HasRequiredQuest())
        {
            var requiredQuestData = GetUserQuestData(questMaster.requiredClearQuest);
            if (requiredQuestData == null || requiredQuestData.status != QuestStatus.Completed)
            {
                var requiredQuest = QuestDataManager.Instance.GetQuestData(questMaster.requiredClearQuest);
                string questName = requiredQuest?.questName ?? $"ID:{questMaster.requiredClearQuest}";
                result.AddError($"前提クエスト未クリア: {questName}");
            }
        }

        // デイリー制限チェック
        if (!questMaster.IsUnlimitedClear())
        {
            var userQuestData = GetUserQuestData(questId);
            if (userQuestData != null)
            {
                int todayClearCount = GetTodayClearCount(questId);
                if (todayClearCount >= questMaster.dailyClearLimit)
                {
                    result.AddError($"本日の挑戦回数上限 ({questMaster.dailyClearLimit}回)");
                }
            }
        }

        // 期間限定チェック
        if (!questMaster.IsQuestActive())
        {
            result.AddError("クエスト期間外です");
        }

        // 同時実行数チェック
        if (ongoingQuestIds.Count >= maxConcurrentQuests)
        {
            result.AddError($"同時実行可能クエスト数の上限 ({maxConcurrentQuests}個)");
        }

        result.isValid = result.errors.Count == 0;
        return result;
    }

    /// <summary>
    /// クエストを開始
    /// </summary>
    /// <param name="questId">クエストID</param>
    /// <returns>開始結果</returns>
    public QuestStartResult StartQuest(int questId)
    {
        var result = new QuestStartResult
        {
            questId = questId,
            startTime = DateTime.Now,
            isSuccess = false
        };

        try
        {
            // 妥当性チェック
            var validation = ValidateQuestStart(questId);
            if (!validation.isValid)
            {
                result.message = string.Join(", ", validation.errors);
                LogError($"クエスト開始失敗 [{questId}]: {result.message}");
                OnQuestError?.Invoke(result.message);
                return result;
            }

            var questMaster = QuestDataManager.Instance.GetQuestData(questId);
            var saveData = SaveDataManager.Instance.CurrentSaveData;

            // スタミナ消費
            saveData.currentStamina -= questMaster.requiredStamina;

            // 進行中クエストに追加
            ongoingQuestIds.Add(questId);

            // ユーザークエストデータ更新
            var userQuestData = GetOrCreateUserQuestData(questId);
            userQuestData.lastClearDate = DateTime.Now.Date;

            // 保存
            SaveDataManager.Instance.MarkDataDirty();
            SaveDataManager.Instance.SaveSaveData();

            // 結果設定
            result.isSuccess = true;
            result.message = "クエスト開始";
            result.consumedStamina = questMaster.requiredStamina;
            result.expectedRewards = GetExpectedRewards(questId);

            Log($"クエスト開始成功: {questMaster.questName}");
            OnQuestStarted?.Invoke(result);
            OnQuestListUpdated?.Invoke();

            return result;
        }
        catch (Exception e)
        {
            result.message = $"クエスト開始エラー: {e.Message}";
            LogError(result.message);
            OnQuestError?.Invoke(result.message);
            return result;
        }
    }

    /// <summary>
    /// クエストを完了（戦闘勝利時）
    /// </summary>
    /// <param name="questId">クエストID</param>
    /// <param name="battleResult">戦闘結果</param>
    /// <returns>完了結果</returns>
    public QuestCompleteResult CompleteQuest(int questId, BattleResult battleResult)
    {
        var result = new QuestCompleteResult
        {
            questId = questId,
            completedTime = DateTime.Now,
            isSuccess = false
        };

        try
        {
            if (!ongoingQuestIds.Contains(questId))
            {
                result.message = "進行中ではないクエストです";
                return result;
            }

            var questMaster = QuestDataManager.Instance.GetQuestData(questId);
            if (questMaster == null)
            {
                result.message = "クエストデータが見つかりません";
                return result;
            }

            var saveData = SaveDataManager.Instance.CurrentSaveData;

            // 報酬計算・付与
            var rewards = CalculateRewards(questId, battleResult);
            GiveRewards(saveData, rewards);

            // ユーザークエストデータ更新
            var userQuestData = GetOrCreateUserQuestData(questId);
            userQuestData.clearCount++;
            userQuestData.lastClearDate = DateTime.Now.Date;
            userQuestData.status = QuestStatus.Completed;

            // 初回クリア報酬
            bool isFirstClear = userQuestData.clearCount == 1;
            if (isFirstClear && questMaster.HasFirstClearReward())
            {
                var firstClearReward = new QuestReward
                {
                    itemType = questMaster.firstClearItemType,
                    itemId = questMaster.firstClearItemId,
                    quantity = questMaster.firstClearItemQuantity
                };
                GiveReward(saveData, firstClearReward);
                rewards.Add(firstClearReward);
            }

            // 進行中リストから削除
            ongoingQuestIds.Remove(questId);

            // 保存
            SaveDataManager.Instance.MarkDataDirty();
            SaveDataManager.Instance.SaveSaveData();

            // 結果設定
            result.isSuccess = true;
            result.message = "クエストクリア";
            result.rewards = rewards;
            result.isFirstClear = isFirstClear;
            result.totalClearCount = userQuestData.clearCount;

            // クエストリスト更新
            RefreshQuestList();

            Log($"クエストクリア: {questMaster.questName} (クリア回数: {userQuestData.clearCount})");
            OnQuestCompleted?.Invoke(questId, availableQuests.TryGetValue(questId, out var display) ? display : null);
            OnQuestListUpdated?.Invoke();

            return result;
        }
        catch (Exception e)
        {
            result.message = $"クエスト完了エラー: {e.Message}";
            LogError(result.message);
            OnQuestError?.Invoke(result.message);
            return result;
        }
    }

    #endregion

    #region 公開メソッド - データ更新

    /// <summary>
    /// クエストリストを更新
    /// </summary>
    public void RefreshQuestList()
    {
        if (!IsInitialized) return;

        try
        {
            Log("クエストリスト更新開始");

            availableQuests.Clear();
            var allQuests = QuestDataManager.Instance.GetQuestDataList();
            var saveData = SaveDataManager.Instance.CurrentSaveData;

            foreach (var questMaster in allQuests)
            {
                var displayData = CreateQuestDisplayData(questMaster, saveData);
                availableQuests[questMaster.questId] = displayData;
            }

            lastUpdateTime = DateTime.Now;
            OnQuestListUpdated?.Invoke();

            Log($"クエストリスト更新完了: {availableQuests.Count}個のクエスト");
        }
        catch (Exception e)
        {
            LogError($"クエストリスト更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// クエスト状態の定期更新
    /// </summary>
    public void UpdateQuestStates()
    {
        if (!IsInitialized) return;

        try
        {
            bool hasUpdates = false;

            foreach (var questDisplay in availableQuests.Values.ToList())
            {
                var questMaster = QuestDataManager.Instance.GetQuestData(questDisplay.questId);
                if (questMaster == null) continue;

                // 期間限定クエストの状態チェック
                bool wasAvailable = questDisplay.isAvailable;
                bool isCurrentlyAvailable = questMaster.IsQuestActive() && CanStartQuest(questDisplay.questId);

                if (wasAvailable != isCurrentlyAvailable)
                {
                    questDisplay.isAvailable = isCurrentlyAvailable;
                    hasUpdates = true;
                }
            }

            if (hasUpdates)
            {
                OnQuestListUpdated?.Invoke();
                Log("クエスト状態が更新されました");
            }
        }
        catch (Exception e)
        {
            LogError($"クエスト状態更新エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - データ処理

    /// <summary>
    /// QuestDisplayDataを作成
    /// </summary>
    private QuestDisplayData CreateQuestDisplayData(QuestMasterData questMaster, UserSaveData saveData)
    {
        var userQuestData = GetUserQuestData(questMaster.questId);
        bool isNew = userQuestData == null || userQuestData.isNew;
        bool isAvailable = questMaster.IsQuestActive() && CanStartQuest(questMaster.questId);

        return new QuestDisplayData
        {
            questId = questMaster.questId,
            questName = questMaster.questName,
            shortDescription = GetShortDescription(questMaster.description),
            questType = questMaster.questType,
            status = GetQuestStatus(questMaster.questId),
            isAvailable = isAvailable,
            isNew = isNew,
            sortOrder = questMaster.sortOrder,
            needLevel = questMaster.needLevel,
            requiredStamina = questMaster.requiredStamina,
            recommendedPower = questMaster.recommendedPower,
            clearCount = userQuestData?.clearCount ?? 0,
            maxClearCount = questMaster.IsUnlimitedClear() ? -1 : questMaster.dailyClearLimit,
            rewards = GetExpectedRewards(questMaster.questId),
            questIconPath = questMaster.questIconPath
        };
    }

    /// <summary>
    /// 短縮説明文を取得
    /// </summary>
    private string GetShortDescription(string fullDescription)
    {
        if (string.IsNullOrEmpty(fullDescription)) return "";

        const int maxLength = 50;
        if (fullDescription.Length <= maxLength) return fullDescription;

        return fullDescription.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// クエストステータスを取得
    /// </summary>
    private QuestStatus GetQuestStatus(int questId)
    {
        var questMaster = QuestDataManager.Instance.GetQuestData(questId);
        if (questMaster == null) return QuestStatus.Locked;

        if (!questMaster.IsQuestActive()) return QuestStatus.Expired;

        var userQuestData = GetUserQuestData(questId);
        if (userQuestData == null) return QuestStatus.Available;

        return userQuestData.status;
    }

    /// <summary>
    /// ユーザークエストデータを取得
    /// </summary>
    private UserQuestData GetUserQuestData(int questId)
    {
        var saveData = SaveDataManager.Instance.CurrentSaveData;
        if (saveData?.quests == null) return null;
        return saveData.quests.FirstOrDefault(q => q.questId == questId);
    }

    /// <summary>
    /// ユーザークエストデータを取得または作成
    /// </summary>
    private UserQuestData GetOrCreateUserQuestData(int questId)
    {
        var saveData = SaveDataManager.Instance.CurrentSaveData;
        if (saveData.quests == null)
        {
            saveData.quests = new List<UserQuestData>();
        }

        var userQuestData = saveData.quests.FirstOrDefault(q => q.questId == questId);
        if (userQuestData == null)
        {
            userQuestData = new UserQuestData
            {
                questId = questId,
                status = QuestStatus.Available,
                clearCount = 0,
                isNew = true,
                firstClearDate = DateTime.MinValue,
                lastClearDate = DateTime.MinValue
            };
            saveData.quests.Add(userQuestData);
        }

        return userQuestData;
    }

    /// <summary>
    /// 本日のクリア回数を取得
    /// </summary>
    private int GetTodayClearCount(int questId)
    {
        var userQuestData = GetUserQuestData(questId);
        if (userQuestData == null) return 0;

        userQuestData.UpdateTodayClearCount();
        return userQuestData.todayClearCount;
    }

    /// <summary>
    /// クエスト利用可能性の理由を取得
    /// </summary>
    private string GetQuestAvailabilityReason(int questId)
    {
        var validation = ValidateQuestStart(questId);
        if (validation.isValid) return "挑戦可能";

        return validation.errors.FirstOrDefault() ?? "挑戦不可";
    }

    /// <summary>
    /// 期待報酬を取得
    /// </summary>
    private List<QuestReward> GetExpectedRewards(int questId)
    {
        var questMaster = QuestDataManager.Instance.GetQuestData(questId);
        if (questMaster == null) return new List<QuestReward>();

        var rewards = new List<QuestReward>();

        // 基本報酬
        if (questMaster.rewardExp > 0)
        {
            rewards.Add(new QuestReward { itemType = "Experience", itemId = 0, quantity = questMaster.rewardExp });
        }

        if (questMaster.rewardGold > 0)
        {
            rewards.Add(new QuestReward { itemType = "Gold", itemId = 0, quantity = questMaster.rewardGold });
        }

        // ドロップ報酬（期待値）
        var dropTable = GetQuestDropTable(questId);
        if (dropTable != null)
        {
            foreach (var dropItem in dropTable.dropItems.Take(3)) // 主要な3つまで表示
            {
                var expectedQuantity = Mathf.RoundToInt(dropItem.quantity * (dropItem.dropRate / 100f) * questMaster.itemDropQuantity);
                if (expectedQuantity > 0)
                {
                    rewards.Add(new QuestReward
                    {
                        itemType = dropItem.itemType,
                        itemId = dropItem.itemId,
                        quantity = expectedQuantity,
                        isDropReward = true
                    });
                }
            }
        }

        return rewards;
    }

    /// <summary>
    /// 実際の報酬を計算
    /// </summary>
    private List<QuestReward> CalculateRewards(int questId, BattleResult battleResult)
    {
        var questMaster = QuestDataManager.Instance.GetQuestData(questId);
        var rewards = new List<QuestReward>();

        // 基本報酬
        rewards.Add(new QuestReward { itemType = "Experience", itemId = 0, quantity = questMaster.rewardExp });
        rewards.Add(new QuestReward { itemType = "Gold", itemId = 0, quantity = questMaster.rewardGold });

        // ドロップ報酬
        var dropTable = GetQuestDropTable(questId);
        if (dropTable != null)
        {
            var dropResults = dropTable.SimulateDrop(questMaster.itemDropQuantity);
            foreach (var drop in dropResults)
            {
                rewards.Add(new QuestReward
                {
                    itemType = drop.itemType,
                    itemId = drop.itemId,
                    quantity = drop.quantity,
                    isDropReward = true
                });
            }
        }

        return rewards;
    }

    /// <summary>
    /// 報酬を付与
    /// </summary>
    private void GiveRewards(UserSaveData saveData, List<QuestReward> rewards)
    {
        foreach (var reward in rewards)
        {
            GiveReward(saveData, reward);
        }
    }

    /// <summary>
    /// 単一報酬を付与
    /// </summary>
    private void GiveReward(UserSaveData saveData, QuestReward reward)
    {
        switch (reward.itemType.ToLower())
        {
            case "experience":
                saveData.AddExperience(reward.quantity);
                break;
            case "gold":
                saveData.gold += reward.quantity;
                break;
            case "equipment":
                // TODO: 装備追加処理
                Log($"装備報酬付与: ID={reward.itemId}, 数量={reward.quantity}");
                break;
            case "enhanceitem":
                // TODO: 強化アイテム追加処理
                Log($"強化アイテム報酬付与: ID={reward.itemId}, 数量={reward.quantity}");
                break;
            case "supportitem":
                // TODO: 補助アイテム追加処理
                Log($"補助アイテム報酬付与: ID={reward.itemId}, 数量={reward.quantity}");
                break;
            default:
                LogError($"未知の報酬タイプ: {reward.itemType}");
                break;
        }
    }

    #endregion

    #region 内部メソッド - ログ・デバッグ

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[QuestListManager] {message}");
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[QuestListManager] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("クエストリストを手動更新")]
    private void ManualRefreshQuestList()
    {
        RefreshQuestList();
    }

    [ContextMenu("進行中クエストをリセット")]
    private void ResetOngoingQuests()
    {
        ongoingQuestIds.Clear();
        Log("進行中クエストをリセットしました");
    }

    [ContextMenu("クエスト状態をログ出力")]
    private void LogQuestStates()
    {
        if (!IsInitialized)
        {
            Log("QuestListManagerが初期化されていません");
            return;
        }

        Log($"=== クエスト状態 ===");
        Log($"利用可能クエスト数: {availableQuests.Count}");
        Log($"進行中クエスト数: {ongoingQuestIds.Count}");
        Log($"最終更新時刻: {lastUpdateTime}");

        foreach (var quest in availableQuests.Values.Take(5))
        {
            Log($"  {quest.questName} - 利用可能: {quest.isAvailable}, ステータス: {quest.status}");
        }
    }
#endif

    #endregion
}