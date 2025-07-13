using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// モンスターエリア管理クラス
/// 複数のMonsterBattleUIを統合管理
/// データアクセス統一ルール: UI層 → Manager層 → データ層
/// </summary>
public class MonsterAreaManager : MonoBehaviour
{
    [Header("モンスターエリア設定")]
    [SerializeField] private Transform monsterParent;
    [SerializeField] private GameObject monsterBattleUIPrefab;
    [SerializeField] private Vector2 monsterSpacing = new Vector2(200f, 0f);
    [SerializeField] private int maxMonsters = 3;

    [Header("配置設定")]
    [SerializeField] private bool autoArrange = true;
    [SerializeField] private Vector3 startPosition = new Vector3(0f, 0f, 0f);

    [Header("デバッグ設定")]
    [SerializeField] private bool enableDebugLog = true;

    // 内部状態
    private Dictionary<string, MonsterBattleUI> monsterUIs = new Dictionary<string, MonsterBattleUI>();
    private List<BattleCharacterData> currentMonsters = new List<BattleCharacterData>();
    private bool isInitialized = false;

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateComponents();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// MonsterAreaManagerの初期化
    /// </summary>
    public void Initialize()
    {
        if (!Application.isPlaying)
        {
            Log("エディタモードのため初期化をスキップ");
            return;
        }

        try
        {
            Log("MonsterAreaManager初期化開始");

            // 既存のモンスターUIをクリア
            ClearMonsterUIs();

            isInitialized = true;
            Log("MonsterAreaManager初期化完了");
        }
        catch (Exception e)
        {
            LogError($"MonsterAreaManager初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// コンポーネント検証
    /// </summary>
    private void ValidateComponents()
    {
        if (monsterParent == null)
            LogWarning("monsterParentが設定されていません");

        if (monsterBattleUIPrefab == null)
            LogWarning("monsterBattleUIPrefabが設定されていません");
    }

    #endregion

    #region 公開メソッド - イベントハンドラ

    /// <summary>
    /// 修正: 戦闘開始時の処理 - BattleUIからの直接呼び出し用
    /// </summary>
    public void OnBattleStart(BattleSetupData setupData)
    {
        if (!isInitialized) Initialize();

        try
        {
            Log("戦闘開始 - モンスターエリア初期化（基本設定のみ）");

            // 修正: ここではsetupDataの基本情報のみ処理
            // 実際のモンスターデータはBattleUIのDistributeCharacterDataToComponents()から
            // UpdateMonstersData()で受け取る

            Log($"クエストID: {setupData.questId}のモンスターエリア準備完了");
        }
        catch (Exception e)
        {
            LogError($"戦闘開始処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: モンスターデータの直接設定（BattleUIから呼び出される）
    /// </summary>
    public void SetMonsterData(List<BattleCharacterData> monsters)
    {
        if (monsters == null || monsters.Count == 0)
        {
            LogError("SetMonsterData: モンスターデータがnullまたは空です");
            return;
        }

        try
        {
            Log($"モンスターデータ直接設定開始: {monsters.Count}体");

            // モンスターUIを作成
            CreateMonsterUIs(monsters);

            Log($"モンスターデータ直接設定完了: {monsters.Count}体");
        }
        catch (Exception e)
        {
            LogError($"モンスターデータ直接設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ターン開始時の処理
    /// </summary>
    public void OnTurnStart(BattleCharacterData character)
    {
        if (character == null) return;

        try
        {
            // 全てのモンスターUIにターン開始を通知
            foreach (var monsterUI in monsterUIs.Values)
            {
                if (monsterUI != null)
                {
                    monsterUI.OnTurnStart(character);
                }
            }

            // 修正: ターン開始時にデータの同期を確認
            if (!character.isPlayer)
            {
                RefreshMonsterData(character);
            }

            Log($"ターン開始処理完了: {character.characterName}");
        }
        catch (Exception e)
        {
            LogError($"ターン開始処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 行動実行時の処理
    /// </summary>
    public void OnActionExecuted(ActionData action)
    {
        if (action == null) return;

        try
        {
            // 全てのモンスターUIに行動実行を通知
            foreach (var monsterUI in monsterUIs.Values)
            {
                if (monsterUI != null)
                {
                    monsterUI.OnActionExecuted(action);
                }
            }

            // 修正: ダメージを受けたモンスターのデータを更新
            foreach (var damage in action.damageResults)
            {
                if (!string.IsNullOrEmpty(damage.targetId) && monsterUIs.ContainsKey(damage.targetId))
                {
                    RefreshSpecificMonsterData(damage.targetId);
                }
            }

            Log($"行動実行処理完了: {action.GetActionSummary()}");
        }
        catch (Exception e)
        {
            LogError($"行動実行処理エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - データ更新

    /// <summary>
    /// 修正: モンスターデータ更新 - returnによる早期終了を削除
    /// </summary>
    public void UpdateMonstersData(List<BattleCharacterData> monsters)
    {
        if (monsters == null)
        {
            LogWarning("UpdateMonstersData: monstersがnullです");
            return;
        }

        try
        {
            Log($"モンスターデータ更新開始: {monsters.Count}体");

            currentMonsters = new List<BattleCharacterData>(monsters);

            // 修正: 初回のモンスターデータ設定の場合、UIを作成
            bool isFirstTimeSetup = (monsterUIs.Count == 0 && monsters.Count > 0);

            if (isFirstTimeSetup)
            {
                Log("初回モンスターデータ設定のためUI作成");
                CreateMonsterUIs(monsters);
                // 修正: return文を削除して、以下の個別更新処理も実行する
            }

            // 修正: 初回作成時も含めて、各モンスターUIのデータ更新を実行
            foreach (var monster in monsters)
            {
                if (monsterUIs.ContainsKey(monster.characterId))
                {
                    // 修正: SetMonsterDataを使用してデータを確実に更新
                    monsterUIs[monster.characterId].SetMonsterData(monster);
                    Log($"モンスターUI個別データ更新: {monster.characterName}");
                }
                else
                {
                    LogWarning($"モンスターUI未発見: {monster.characterId} - UI再作成を試行");

                    // 修正: UIが見つからない場合は個別に作成を試行
                    if (!isFirstTimeSetup)
                    {
                        CreateSingleMonsterUIForMissingData(monster);
                    }
                }
            }

            // 修正: 作成・更新完了後の検証ログ
            Log($"モンスターデータ更新完了: データ{monsters.Count}体, UI作成済み{monsterUIs.Count}個");

            // 修正: データとUIの整合性チェック
            ValidateDataUIConsistency();
        }
        catch (Exception e)
        {
            LogError($"モンスターデータ更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 指定モンスターのUI取得
    /// </summary>
    public MonsterBattleUI GetMonsterUI(string monsterId)
    {
        if (string.IsNullOrEmpty(monsterId)) return null;

        if (monsterUIs.ContainsKey(monsterId))
        {
            return monsterUIs[monsterId];
        }

        LogWarning($"指定されたモンスターUIが見つかりません: {monsterId}");
        return null;
    }

    /// <summary>
    /// 全てのモンスターUI取得
    /// </summary>
    public List<MonsterBattleUI> GetAllMonsterUIs()
    {
        return new List<MonsterBattleUI>(monsterUIs.Values);
    }

    /// <summary>
    /// 修正: 現在のモンスター数取得
    /// </summary>
    public int GetMonsterCount()
    {
        return currentMonsters?.Count ?? 0;
    }

    /// <summary>
    /// 修正: アクティブなモンスターUI数取得
    /// </summary>
    public int GetActiveMonsterUICount()
    {
        return monsterUIs?.Count ?? 0;
    }

    /// <summary>
    /// 修正: UI作成とデータ設定の完了状態確認
    /// </summary>
    public bool IsUISetupComplete()
    {
        if (currentMonsters == null || currentMonsters.Count == 0)
            return false;

        // データ数とUI数が一致し、全てのUIがデータを持っているかチェック
        if (monsterUIs.Count != currentMonsters.Count)
            return false;

        foreach (var monster in currentMonsters)
        {
            if (!monsterUIs.ContainsKey(monster.characterId))
                return false;
        }

        return true;
    }

    #endregion

    #region 内部メソッド - UI管理

    /// <summary>
    /// モンスターUI作成
    /// </summary>
    private void CreateMonsterUIs(List<BattleCharacterData> monsters)
    {
        if (monsterBattleUIPrefab == null || monsterParent == null)
        {
            LogError("CreateMonsterUIs: 必要なPrefabまたはParentが設定されていません");
            return;
        }

        if (monsters == null || monsters.Count == 0)
        {
            LogWarning("CreateMonsterUIs: モンスターデータが空です");
            return;
        }

        try
        {
            Log($"モンスターUI作成開始: {monsters.Count}体");

            // 既存UIクリア
            ClearMonsterUIs();

            // 新しいUIを作成
            for (int i = 0; i < monsters.Count && i < maxMonsters; i++)
            {
                var monster = monsters[i];
                if (monster != null && !monster.isPlayer)
                {
                    CreateSingleMonsterUI(monster, i);
                }
                else
                {
                    LogWarning($"無効なモンスターデータをスキップ: index={i}");
                }
            }

            currentMonsters = new List<BattleCharacterData>(monsters);
            Log($"モンスターUI作成完了: {monsterUIs.Count}個のUI作成");
        }
        catch (Exception e)
        {
            LogError($"モンスターUI作成エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 単体モンスターUI作成
    /// </summary>
    private void CreateSingleMonsterUI(BattleCharacterData monster, int index)
    {
        if (monster == null)
        {
            LogError($"CreateSingleMonsterUI: モンスターデータがnull (index: {index})");
            return;
        }

        try
        {
            Log($"モンスターUI作成中: {monster.characterName} (index: {index})");

            // プレハブからインスタンス作成
            GameObject monsterObj = Instantiate(monsterBattleUIPrefab, monsterParent);
            if (monsterObj == null)
            {
                LogError($"モンスターオブジェクトの生成に失敗: {monster.characterName}");
                return;
            }

            var monsterUI = monsterObj.GetComponent<MonsterBattleUI>();
            if (monsterUI != null)
            {
                // 修正: Initialize()を先に呼び出してから、データ設定
                monsterUI.Initialize();

                // モンスターデータ設定
                monsterUI.SetMonsterData(monster);

                // 位置設定
                if (autoArrange)
                {
                    SetMonsterPosition(monsterObj.transform, index);
                }

                // 辞書に登録
                monsterUIs[monster.characterId] = monsterUI;

                Log($"モンスターUI作成成功: {monster.characterName} (位置: {index}, ID: {monster.characterId})");
            }
            else
            {
                LogError($"MonsterBattleUIコンポーネントが見つかりません: {monster.characterName}");
                if (Application.isPlaying)
                {
                    Destroy(monsterObj);
                }
            }
        }
        catch (Exception e)
        {
            LogError($"単体モンスターUI作成エラー ({monster?.characterName}): {e.Message}");
        }
    }

    /// <summary>
    /// 修正: 不足しているモンスターデータ用のUI個別作成
    /// </summary>
    private void CreateSingleMonsterUIForMissingData(BattleCharacterData monster)
    {
        if (monster == null || monster.isPlayer) return;

        try
        {
            Log($"不足モンスターUI作成: {monster.characterName}");

            // 現在のUI数をインデックスとして使用
            int index = monsterUIs.Count;
            CreateSingleMonsterUI(monster, index);
        }
        catch (Exception e)
        {
            LogError($"不足モンスターUI作成エラー ({monster?.characterName}): {e.Message}");
        }
    }

    /// <summary>
    /// モンスター位置設定
    /// </summary>
    private void SetMonsterPosition(Transform monsterTransform, int index)
    {
        if (monsterTransform == null) return;

        try
        {
            Vector3 position = startPosition;
            position.x += monsterSpacing.x * index;
            position.y += monsterSpacing.y * index;

            monsterTransform.localPosition = position;
            Log($"モンスター位置設定: index={index}, position={position}");
        }
        catch (Exception e)
        {
            LogError($"モンスター位置設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// モンスターUIクリア
    /// </summary>
    private void ClearMonsterUIs()
    {
        if (monsterParent == null) return;

        try
        {
            Log($"モンスターUIクリア開始: 現在のUI数={monsterUIs.Count}");

            // プレハブモード判定
            bool isPrefabMode = !Application.isPlaying &&
#if UNITY_EDITOR
                UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject);
#else
                false;
#endif

            // 既存のモンスターUIを削除
            foreach (var kvp in monsterUIs)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                {
                    if (Application.isPlaying && !isPrefabMode)
                    {
                        Destroy(kvp.Value.gameObject);
                        Log($"モンスターUI削除: {kvp.Key}");
                    }
                    else
                    {
                        kvp.Value.gameObject.SetActive(false);
                        Log($"モンスターUI非表示: {kvp.Key}");
                    }
                }
            }

            // 直接の子オブジェクトもクリア
            if (Application.isPlaying && !isPrefabMode)
            {
                for (int i = monsterParent.childCount - 1; i >= 0; i--)
                {
                    var child = monsterParent.GetChild(i);
                    if (child != null && child.gameObject != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
            else
            {
                // エディタモード・プレハブモードでは非表示のみ
                for (int i = 0; i < monsterParent.childCount; i++)
                {
                    var child = monsterParent.GetChild(i);
                    if (child != null && child.gameObject != null)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            monsterUIs.Clear();
            currentMonsters.Clear();

            Log($"モンスターUIクリア完了 (プレハブモード: {isPrefabMode}, プレイ中: {Application.isPlaying})");
        }
        catch (Exception e)
        {
            LogError($"モンスターUIクリアエラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - データ同期

    /// <summary>
    /// 修正: 特定モンスターのデータを最新に更新
    /// </summary>
    private void RefreshSpecificMonsterData(string monsterId)
    {
        if (string.IsNullOrEmpty(monsterId)) return;

        try
        {
            // BattleManagerから最新のデータを取得
            if (BattleManager.Instance != null)
            {
                var allCharacters = BattleManager.Instance.GetAllCharacters();
                var updatedMonster = allCharacters?.Find(c => c.characterId == monsterId);

                if (updatedMonster != null && monsterUIs.ContainsKey(monsterId))
                {
                    // currentMonstersリスト内の該当データも更新
                    int index = currentMonsters.FindIndex(m => m.characterId == monsterId);
                    if (index >= 0)
                    {
                        currentMonsters[index] = updatedMonster;
                    }

                    // UIに最新データを設定
                    monsterUIs[monsterId].SetMonsterData(updatedMonster);
                    Log($"モンスターデータ同期完了: {monsterId}");
                }
            }
        }
        catch (Exception e)
        {
            LogError($"特定モンスターデータ同期エラー ({monsterId}): {e.Message}");
        }
    }

    /// <summary>
    /// 修正: 指定キャラクターのデータを最新に更新
    /// </summary>
    private void RefreshMonsterData(BattleCharacterData character)
    {
        if (character == null || character.isPlayer) return;

        try
        {
            RefreshSpecificMonsterData(character.characterId);
        }
        catch (Exception e)
        {
            LogError($"モンスターデータ更新エラー ({character?.characterName}): {e.Message}");
        }
    }

    /// <summary>
    /// 修正: データとUIの整合性チェック
    /// </summary>
    private void ValidateDataUIConsistency()
    {
        try
        {
            if (currentMonsters == null)
            {
                LogWarning("currentMonstersがnullです");
                return;
            }

            Log($"データ・UI整合性チェック: データ{currentMonsters.Count}体 vs UI{monsterUIs.Count}個");

            foreach (var monster in currentMonsters)
            {
                if (!monsterUIs.ContainsKey(monster.characterId))
                {
                    LogWarning($"データ存在・UI不在: {monster.characterName} (ID: {monster.characterId})");
                }
            }

            foreach (var uiPair in monsterUIs)
            {
                var foundData = currentMonsters.Find(m => m.characterId == uiPair.Key);
                if (foundData == null)
                {
                    LogWarning($"UI存在・データ不在: {uiPair.Key}");
                }
            }
        }
        catch (Exception e)
        {
            LogError($"データ・UI整合性チェックエラー: {e.Message}");
        }
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterAreaManager] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogWarning($"[MonsterAreaManager] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[MonsterAreaManager] {message}");
    }

    #endregion

    #region エディタ用ツール

#if UNITY_EDITOR
    [ContextMenu("モンスター配置テスト")]
    private void TestMonsterArrangement()
    {
        Log("モンスター配置テスト");

        if (BattleManager.Instance != null)
        {
            var allCharacters = BattleManager.Instance.GetAllCharacters();
            var enemies = allCharacters.FindAll(c => !c.isPlayer);
            CreateMonsterUIs(enemies);
        }
        else
        {
            LogWarning("BattleManagerが見つかりません");
        }
    }

    [ContextMenu("モンスターUIクリアテスト")]
    private void TestClearMonsterUIs()
    {
        Log("モンスターUIクリアテスト");
        ClearMonsterUIs();
    }

    [ContextMenu("現在の状態を表示")]
    private void ShowCurrentStatus()
    {
        Log($"=== MonsterAreaManager現在の状態 ===");
        Log($"初期化済み: {isInitialized}");
        Log($"モンスターUI数: {monsterUIs.Count}");
        Log($"現在のモンスターデータ数: {currentMonsters.Count}");
        Log($"monsterParent子オブジェクト数: {(monsterParent != null ? monsterParent.childCount : 0)}");
        Log($"UI設定完了: {IsUISetupComplete()}");

        foreach (var kvp in monsterUIs)
        {
            Log($"  UI: {kvp.Key} -> {(kvp.Value != null ? "存在" : "null")}");
        }
    }

    [ContextMenu("データ・UI整合性チェック")]
    private void TestValidateDataUIConsistency()
    {
        ValidateDataUIConsistency();
    }
#endif

    #endregion
}