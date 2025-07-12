using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// クエスト戦闘シーン専用管理クラス
/// 責任範囲：
/// - クエスト選択から戦闘シーンへの遷移データ管理
/// - 戦闘初期化パラメータの設定
/// - 戦闘終了後のホーム画面復帰処理
/// </summary>
public class QuestBattleSceneManager : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;

    // プロパティ
    public static QuestBattleSceneManager Instance { get; private set; }
    public bool IsInitialized { get; private set; }

    // 戦闘データ
    private int selectedQuestId = -1;
    private QuestMasterData currentQuestData;
    private UserSaveData currentUserData;

    // 依存Manager
    private BattleManager battleManager;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(WaitForDependenciesAndStartBattle());
    }

    #endregion

    #region 初期化

    /// <summary>
    /// マネージャー初期化
    /// </summary>
    private void InitializeManager()
    {
        Log("QuestBattleSceneManager初期化開始");

        // GameSceneManagerからクエストIDを取得
        selectedQuestId = QuestSelectionData.GetSelectedQuestId();

        if (selectedQuestId <= 0)
        {
            LogError("有効なクエストIDが設定されていません");
            // デフォルトでテスト用クエストを設定
            selectedQuestId = 1;
            Log($"テスト用クエストID {selectedQuestId} を設定しました");
        }

        Log($"選択されたクエストID: {selectedQuestId}");
        IsInitialized = true;
    }

    /// <summary>
    /// 依存関係確認と戦闘開始
    /// </summary>
    private IEnumerator WaitForDependenciesAndStartBattle()
    {
        Log("依存関係確認開始");

        float timeout = 15f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (CheckDependencies())
            {
                Log("依存関係確認完了 - 戦闘準備開始");
                yield return StartCoroutine(PrepareBattle());
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        // タイムアウト処理
        HandleDependencyTimeout(timeout);
    }

    /// <summary>
    /// 依存関係タイムアウト処理
    /// </summary>
    private void HandleDependencyTimeout(float timeout)
    {
        try
        {
            LogError($"依存関係の初期化がタイムアウトしました（{timeout}秒）");

            // 必要に応じてエラー画面表示やホーム復帰などの処理
            // 例：強制的にホーム画面に戻る
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.GoToHome();
            }
        }
        catch (Exception e)
        {
            LogError($"タイムアウト処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: 依存関係チェック（BattleManager有効化追加）
    /// </summary>
    private bool CheckDependencies()
    {
        // 必須Managerの確認
        if (SaveDataManager.Instance == null || !SaveDataManager.Instance.IsDataLoaded)
        {
            Log("SaveDataManager未初期化");
            return false;
        }

        if (MasterDataManager.Instance == null || !MasterDataManager.Instance.IsDataLoaded)
        {
            Log("MasterDataManager未初期化");
            return false;
        }

        if (QuestDataManager.Instance == null || !QuestDataManager.Instance.IsDataLoaded)
        {
            Log("QuestDataManager未初期化");
            return false;
        }

        // 修正: BattleManager確認と強制有効化
        battleManager = BattleManager.Instance;
        if (battleManager == null)
        {
            Log("BattleManager未初期化");
            return false;
        }

        // 修正: BattleManagerのGameObjectが非アクティブの場合、有効化
        if (!battleManager.gameObject.activeInHierarchy)
        {
            Log("BattleManagerが非アクティブのため、有効化します");
            battleManager.gameObject.SetActive(true);
        }

        if (!battleManager.IsInitialized)
        {
            Log("BattleManager未初期化");
            return false;
        }

        Log("全ての依存関係が満たされています");
        return true;
    }


    #endregion

    #region 戦闘準備・開始

    /// <summary>
    /// 修正: 戦闘準備処理（BattleManager有効化確認追加）
    /// </summary>
    private IEnumerator PrepareBattle()
    {
        Log("戦闘準備処理開始");

        // 修正: BattleManagerの状態を再確認
        if (battleManager == null)
        {
            LogError("battleManagerがnullです");
            yield break;
        }

        if (!battleManager.gameObject.activeInHierarchy)
        {
            LogError("BattleManagerが非アクティブです。有効化します。");
            battleManager.gameObject.SetActive(true);

            // 有効化後、少し待機
            yield return new WaitForSeconds(0.1f);
        }

        // データ検証を事前に実行
        bool dataValid = ValidateBattleData();
        if (!dataValid)
        {
            yield break;
        }

        // 修正: 戦闘データの詳細ログ出力
        LogDetailedBattleData();

        Log($"戦闘準備完了: {currentQuestData.questName}");

        // 少し待機してから戦闘開始
        yield return new WaitForSeconds(0.5f);

        // 戦闘開始
        StartBattleWithErrorHandling();
    }

    /// <summary>
    /// 修正: 戦闘データの詳細ログ出力
    /// </summary>
    private void LogDetailedBattleData()
    {
        Log("=== 詳細戦闘データ確認 ===");

        // クエストデータ詳細
        Log($"クエスト名: {currentQuestData.questName}");
        Log($"必要レベル: Lv.{currentQuestData.needLevel}");
        Log($"必要スタミナ: {currentQuestData.requiredStamina}");
        Log($"推奨戦闘力: {currentQuestData.recommendedPower}");

        // 出現モンスター詳細確認
        var monsterIds = currentQuestData.GetSpawnMonsterIds();
        Log($"出現モンスターID一覧: [{string.Join(", ", monsterIds)}]");

        foreach (var monsterId in monsterIds)
        {
            var monster = QuestDataManager.Instance.GetMonsterData(monsterId);
            if (monster != null)
            {
                Log($"  モンスター: {monster.monsterName} (ID:{monster.monsterId}, HP:{monster.hp}, ATK:{monster.offense})");
            }
            else
            {
                LogError($"  モンスターID {monsterId} のデータが見つかりません！");
            }
        }

        // プレイヤーデータ詳細確認
        Log($"プレイヤーレベル: Lv.{currentUserData.playerLevel}");
        Log($"現在スタミナ: {currentUserData.currentStamina}");
        Log($"所持金: {currentUserData.gold}");

        // 装備データ確認
        if (currentUserData.equipments != null)
        {
            var equippedItems = currentUserData.equipments.FindAll(e => e.isEquipped);
            Log($"装備中アイテム数: {equippedItems.Count}");

            foreach (var equipment in equippedItems)
            {
                var masterData = MasterDataManager.Instance?.GetEquipmentData(equipment.equipmentMasterId);
                if (masterData != null)
                {
                    var totalStats = equipment.CalculateTotalStats(masterData);
                    Log($"  装備: {masterData.equipmentName} - HP+{totalStats.hp}, ATK+{totalStats.offense}, DEF+{totalStats.defense}");
                }
            }
        }

        Log("=== 詳細戦闘データ確認終了 ===");
    }


    /// <summary>
    /// 戦闘データの検証
    /// </summary>
    private bool ValidateBattleData()
    {
        try
        {
            // クエストデータ取得
            currentQuestData = QuestDataManager.Instance.GetQuestData(selectedQuestId);
            if (currentQuestData == null)
            {
                LogError($"クエストID {selectedQuestId} のデータが見つかりません");
                return false;
            }

            // ユーザーデータ取得
            currentUserData = SaveDataManager.Instance.CurrentSaveData;
            if (currentUserData == null)
            {
                LogError("ユーザーセーブデータが見つかりません");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            LogError($"戦闘データ検証エラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// エラーハンドリング付き戦闘開始
    /// </summary>
    private void StartBattleWithErrorHandling()
    {
        try
        {
            StartBattle();
        }
        catch (Exception e)
        {
            LogError($"戦闘開始エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘開始処理
    /// </summary>
    private void StartBattle()
    {
        Log($"戦闘開始: Quest[{currentQuestData.questId}] {currentQuestData.questName}");

        try
        {
            // 戦闘前の詳細ログ
            LogBattleSetupDetails();

            // BattleManagerで戦闘開始
            bool battleStarted = battleManager.StartBattle(currentUserData, currentQuestData);

            if (battleStarted)
            {
                Log("戦闘開始成功");

                // 戦闘完了イベントに登録
                BattleManager.OnBattleCompleted += OnBattleCompleted;
                BattleManager.OnBattleError += OnBattleError;
            }
            else
            {
                LogError("戦闘開始に失敗しました");
            }
        }
        catch (Exception e)
        {
            LogError($"戦闘開始エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘セットアップの詳細ログ
    /// </summary>
    private void LogBattleSetupDetails()
    {
        Log("=== 戦闘セットアップ詳細 ===");
        Log($"クエスト名: {currentQuestData.questName}");
        Log($"必要レベル: Lv.{currentQuestData.needLevel}");
        Log($"必要スタミナ: {currentQuestData.requiredStamina}");
        Log($"推奨戦闘力: {currentQuestData.recommendedPower}");
        Log($"プレイヤーレベル: Lv.{currentUserData.playerLevel}");
        Log($"現在スタミナ: {currentUserData.currentStamina}");

        // 出現モンスター
        var monsterIds = currentQuestData.GetSpawnMonsterIds();
        Log($"出現モンスター数: {monsterIds.Count}");
        foreach (var monsterId in monsterIds)
        {
            var monster = QuestDataManager.Instance.GetMonsterData(monsterId);
            if (monster != null)
            {
                Log($"  - {monster.monsterName} (HP:{monster.hp}, ATK:{monster.offense})");
            }
        }

        // 装備情報
        var equippedItems = currentUserData.equipments?.FindAll(e => e.isEquipped);
        Log($"装備中アイテム数: {equippedItems?.Count ?? 0}");

        Log("=== 戦闘セットアップ詳細終了 ===");
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// 戦闘完了イベントハンドラ（BattleResultUI統合版）
    /// </summary>
    private void OnBattleCompleted(BattleResultData result)
    {
        Log($"戦闘完了: {(result.isVictory ? "勝利" : "敗北")}");

        // イベント登録解除
        BattleManager.OnBattleCompleted -= OnBattleCompleted;
        BattleManager.OnBattleError -= OnBattleError;

        // 戦闘結果処理
        ProcessBattleResult(result);

        // BattleResultUIは自動的にBattleManager.OnBattleCompletedイベントで表示される
        // （BattleResultUIの統合機能により自動処理）
        Log("BattleResultUIによる結果表示開始");
    }

    /// <summary>
    /// 戦闘エラーイベントハンドラ
    /// </summary>
    private void OnBattleError(string errorMessage)
    {
        LogError($"戦闘エラー: {errorMessage}");

        // イベント登録解除
        BattleManager.OnBattleCompleted -= OnBattleCompleted;
        BattleManager.OnBattleError -= OnBattleError;
    }

    #endregion

    #region 戦闘結果処理

    /// <summary>
    /// 戦闘結果処理（BattleResultUI統合版）
    /// </summary>
    private void ProcessBattleResult(BattleResultData result)
    {
        Log("戦闘結果処理開始");

        try
        {
            // 結果の詳細ログ
            LogBattleResult(result);

            // セーブデータ保存
            SaveDataManager.Instance.SaveSaveData();

            Log("戦闘結果処理完了");

            // BattleResultUIが表示されるので、自動復帰は行わない
            // ユーザーがホームボタンを押すまで待機
            Log("BattleResultUI表示中 - ユーザーのホーム復帰操作を待機");
        }
        catch (Exception e)
        {
            LogError($"戦闘結果処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘結果の詳細ログ
    /// </summary>
    private void LogBattleResult(BattleResultData result)
    {
        Log("=== 戦闘結果詳細 ===");
        Log($"勝敗: {(result.isVictory ? "勝利" : "敗北")}");
        Log($"終了理由: {result.endReason}");
        Log($"総ターン数: {result.totalTurns}");
        Log($"戦闘時間: {result.battleDuration:F1}秒");

        if (result.isVictory)
        {
            Log($"獲得経験値: {result.gainedExp}");
            Log($"獲得ゴールド: {result.gainedGold}");
            Log($"ドロップアイテム数: {result.dropItems.Count}");

            foreach (var dropItem in result.dropItems)
            {
                Log($"  - {dropItem.itemType} ID:{dropItem.itemId} x{dropItem.quantity}");
            }
        }

        Log("=== 戦闘結果詳細終了 ===");
    }

    /// <summary>
    /// 遅延後にホーム画面に戻る
    /// </summary>
    private IEnumerator ReturnToHomeAfterDelay(float delay)
    {
        Log($"{delay}秒後にホーム画面に戻ります");
        yield return new WaitForSeconds(delay);

        ReturnToHome();
    }

    /// <summary>
    /// ホーム画面に戻る
    /// </summary>
    public void ReturnToHome()
    {
        Log("ホーム画面に戻ります");

        try
        {
            // クエスト選択データクリア
            QuestSelectionData.ClearSelectedQuest();

            // GameSceneManagerでホーム画面に遷移
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.GoToHome();
            }
            else
            {
                LogError("GameSceneManager.Instanceがnullです");
            }
        }
        catch (Exception e)
        {
            LogError($"ホーム画面復帰エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 現在の戦闘状況を取得
    /// </summary>
    public string GetCurrentBattleStatus()
    {
        if (battleManager == null) return "BattleManager未初期化";

        return $"戦闘状態: {battleManager.CurrentState}, " +
               $"ターン: {battleManager.GetCurrentTurnNumber()}, " +
               $"経過時間: {battleManager.GetBattleElapsedTime():F1}秒";
    }

    /// <summary>
    /// 戦闘を強制終了してホームに戻る
    /// </summary>
    public void ForceReturnToHome()
    {
        Log("戦闘強制終了要求");

        if (battleManager != null)
        {
            battleManager.ForceEndBattle();
        }

        ReturnToHome();
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[QuestBattleSceneManager] {message}");
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[QuestBattleSceneManager] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("現在の戦闘状況を確認")]
    private void CheckCurrentBattleStatus()
    {
        Log(GetCurrentBattleStatus());
    }

    [ContextMenu("ホーム画面に強制復帰")]
    private void EditorForceReturnToHome()
    {
        ForceReturnToHome();
    }

    [ContextMenu("選択されたクエストIDを確認")]
    private void CheckSelectedQuestId()
    {
        int questId = QuestSelectionData.GetSelectedQuestId();
        Log($"選択されたクエストID: {questId}");
    }
#endif

    #endregion
}

/// <summary>
/// クエスト選択データの管理
/// シーン間でのクエスト情報受け渡し用
/// </summary>
public static class QuestSelectionData
{
    private static int selectedQuestId = -1;

    /// <summary>
    /// クエストIDを設定
    /// </summary>
    public static void SetSelectedQuest(int questId)
    {
        selectedQuestId = questId;
        Debug.Log($"[QuestSelectionData] クエストID設定: {questId}");
    }

    /// <summary>
    /// 選択されたクエストIDを取得
    /// </summary>
    public static int GetSelectedQuestId()
    {
        return selectedQuestId;
    }

    /// <summary>
    /// 選択データをクリア
    /// </summary>
    public static void ClearSelectedQuest()
    {
        selectedQuestId = -1;
        Debug.Log("[QuestSelectionData] クエスト選択データクリア");
    }

    /// <summary>
    /// 有効なクエストが選択されているか
    /// </summary>
    public static bool HasValidQuest()
    {
        return selectedQuestId > 0;
    }
}