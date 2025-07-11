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
    /// 戦闘開始時の処理
    /// </summary>
    public void OnBattleStart(BattleSetupData setupData)
    {
        if (!isInitialized) Initialize();

        try
        {
            Log("戦闘開始 - モンスターエリア初期化");

            // BattleManagerからモンスターデータを取得
            if (BattleManager.Instance != null)
            {
                var allCharacters = BattleManager.Instance.GetAllCharacters();
                var enemies = allCharacters.FindAll(c => !c.isPlayer);

                CreateMonsterUIs(enemies);
                Log($"モンスターUI作成完了: {enemies.Count}体");
            }
        }
        catch (Exception e)
        {
            LogError($"戦闘開始処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ターン開始時の処理
    /// </summary>
    public void OnTurnStart(BattleCharacterData character)
    {
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
        }
        catch (Exception e)
        {
            LogError($"行動実行処理エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - データ更新

    /// <summary>
    /// モンスターデータ更新
    /// </summary>
    public void UpdateMonstersData(List<BattleCharacterData> monsters)
    {
        try
        {
            currentMonsters = new List<BattleCharacterData>(monsters);

            // 各モンスターUIのデータ更新
            foreach (var monster in monsters)
            {
                if (monsterUIs.ContainsKey(monster.characterId))
                {
                    monsterUIs[monster.characterId].UpdateMonsterData();
                }
            }

            Log($"モンスターデータ更新: {monsters.Count}体");
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
        if (monsterUIs.ContainsKey(monsterId))
        {
            return monsterUIs[monsterId];
        }
        return null;
    }

    /// <summary>
    /// 全てのモンスターUI取得
    /// </summary>
    public List<MonsterBattleUI> GetAllMonsterUIs()
    {
        return new List<MonsterBattleUI>(monsterUIs.Values);
    }

    #endregion

    #region 内部メソッド - UI管理

    /// <summary>
    /// モンスターUI作成
    /// </summary>
    private void CreateMonsterUIs(List<BattleCharacterData> monsters)
    {
        if (monsterBattleUIPrefab == null || monsterParent == null) return;

        try
        {
            // 既存UIクリア
            ClearMonsterUIs();

            // 新しいUIを作成
            for (int i = 0; i < monsters.Count && i < maxMonsters; i++)
            {
                var monster = monsters[i];
                CreateSingleMonsterUI(monster, i);
            }

            currentMonsters = new List<BattleCharacterData>(monsters);
            Log($"モンスターUI作成完了: {monsters.Count}体");
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
        try
        {
            // プレハブからインスタンス作成
            GameObject monsterObj = Instantiate(monsterBattleUIPrefab, monsterParent);
            var monsterUI = monsterObj.GetComponent<MonsterBattleUI>();

            if (monsterUI != null)
            {
                // モンスターデータ設定
                monsterUI.SetMonsterData(monster);

                // 位置設定
                if (autoArrange)
                {
                    SetMonsterPosition(monsterObj.transform, index);
                }

                // 辞書に登録
                monsterUIs[monster.characterId] = monsterUI;

                Log($"モンスターUI作成: {monster.characterName} (位置: {index})");
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
            LogError($"単体モンスターUI作成エラー: {e.Message}");
        }
    }

    /// <summary>
    /// モンスター位置設定
    /// </summary>
    private void SetMonsterPosition(Transform monsterTransform, int index)
    {
        try
        {
            Vector3 position = startPosition;
            position.x += monsterSpacing.x * index;
            position.y += monsterSpacing.y * index;

            monsterTransform.localPosition = position;
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
            // プレハブモード判定
            bool isPrefabMode = !Application.isPlaying &&
#if UNITY_EDITOR
                UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject);
#else
                false;
#endif

            // 既存のモンスターUIを削除
            foreach (var monsterUI in monsterUIs.Values)
            {
                if (monsterUI != null && monsterUI.gameObject != null)
                {
                    if (Application.isPlaying && !isPrefabMode)
                    {
                        Destroy(monsterUI.gameObject);
                    }
                    else
                    {
                        monsterUI.gameObject.SetActive(false);
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

    #region ログ・デバッグ

    private void Log(string message)
    {
        Debug.Log($"[MonsterAreaManager] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[MonsterAreaManager] {message}");
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
#endif

    #endregion
}