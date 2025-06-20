using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    [Header("戦闘設定")]
    public int questId = 1;
    public int turnLimit = 30;
    public int currentTurn = 0;

    [Header("参加者")]
    public List<BattlePlayer> players = new List<BattlePlayer>();
    public List<BattleEnemy> enemies = new List<BattleEnemy>();

    [Header("戦闘制御")]
    public Queue<BattleCharacter> actionQueue = new Queue<BattleCharacter>();
    public BattleCharacter currentActor;
    public BattleState currentState = BattleState.Initializing;
    public BattleResult battleResult = BattleResult.None;

    [Header("スポーン位置")]
    public Transform playerSpawnPoint;
    public Transform[] enemySpawnPoints = new Transform[3];

    [Header("UI参照")]
    public GameObject battleUI;
    public GameObject victoryUI;
    public GameObject defeatUI;

    [Header("ヘッダーUI")]
    public TextMeshProUGUI questNameText;
    public TextMeshProUGUI turnInfoText;

    [Header("キャラクターUI")]
    public Transform playerUIParent;
    public Transform enemyUIParent;
    public GameObject characterUIPrefab;

    [Header("バトルログ")]
    public TextMeshProUGUI battleLogText;
    public ScrollRect battleLogScrollRect;

    [Header("ボタン")]
    public Button homeButton;
    public Button autoButton;
    public Button settingsButton;

    [Header("勝敗UI")]
    public TextMeshProUGUI victoryRewardText;
    public Button retryButton;
    public Button victoryHomeButton;
    public Button defeatHomeButton;

    [Header("デバッグ設定")]
    public bool enableDebugLog = true;
    public float actionDelay = 1.0f;

    // BattleManager.cs に以下を追加

    // ===== 1. フィールド追加 =====
    // [Header("デバッグ設定")] の下に以下を追加：

    // UI制御用
    private BattleUIController uiController;

    // ===== 2. メソッド追加 =====
    // InitializeBattle() メソッド内の InitializeUI(); の直後に以下を追加：

    // UI制御初期化

    private void InitializeUIController()
    {
        // UIControllerコンポーネントを取得または追加
        uiController = GetComponent<BattleUIController>();
        if (uiController == null)
        {
            uiController = gameObject.AddComponent<BattleUIController>();
        }

        // UI参照を設定
        uiController.questNameText = questNameText;
        uiController.turnInfoText = turnInfoText;
        uiController.battleLogText = battleLogText;
        uiController.battleLogScrollRect = battleLogScrollRect;
        uiController.playerUIParent = playerUIParent;
        uiController.enemyUIParent = enemyUIParent;
        uiController.characterUIPrefab = characterUIPrefab;

        // UIコントローラー初期化
        if (uiController != null)
        {
            string questName = $"クエスト {questId}"; // TODO: 実際のクエスト名を取得
            uiController.InitializeUI(questName, turnLimit);
        }
    }

    private void InitializeUI()
    {
        // UI要素の初期化
        if (battleUI != null) battleUI.SetActive(true);
        if (victoryUI != null) victoryUI.SetActive(false);
        if (defeatUI != null) defeatUI.SetActive(false);

        // ボタンイベント設定
        SetupButtonEvents();
    }

    private void SetupButtonEvents()
    {
        // ホームボタン
        if (homeButton != null)
            homeButton.onClick.AddListener(OnHomeButtonClicked);
        if (victoryHomeButton != null)
            victoryHomeButton.onClick.AddListener(OnHomeButtonClicked);
        if (defeatHomeButton != null)
            defeatHomeButton.onClick.AddListener(OnHomeButtonClicked);

        // リトライボタン
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryButtonClicked);

        // オートボタン（今後実装）
        if (autoButton != null)
            autoButton.onClick.AddListener(OnAutoButtonClicked);

        // 設定ボタン（今後実装）
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
    }


    public void OnAutoButtonClicked()
    {
        LogDebug("オートボタンが押されました（機能は今後実装）");
        // TODO: オート戦闘のON/OFF切り替え
    }

    public void OnSettingsButtonClicked()
    {
        LogDebug("設定ボタンが押されました（機能は今後実装）");
        // TODO: 設定画面の表示
    }

    // イベント
    public System.Action<BattleResult> OnBattleEnd;

    // 全キャラクターのリスト取得
    public List<BattleCharacter> AllCharacters =>
        players.Cast<BattleCharacter>().Concat(enemies.Cast<BattleCharacter>()).ToList();

    // 生存中のキャラクターのみ取得
    public List<BattleCharacter> AliveCharacters =>
        AllCharacters.Where(c => c.isAlive).ToList();




    private void Start()
    {
        InitializeBattle();
    }

    public void InitializeBattle()
    {
        LogDebug("=== 戦闘初期化開始 ===");

        currentState = BattleState.Initializing;
        currentTurn = 0;
        battleResult = BattleResult.None;


        // クエストデータから戦闘情報を読み込み
        LoadQuestData();

        // プレイヤーデータの初期化
        InitializePlayer();

        // 敵の生成
        SpawnEnemies();

        // 行動順序の決定
        DetermineActionOrder();

        // 戦闘開始
        currentState = BattleState.InProgress;
        LogDebug("=== 戦闘開始 ===");
        StartCoroutine(BattleLoop());
    }

    private void LoadQuestData()
    {
        // TODO: DataManagerからクエストデータを取得
        // 現在は仮設定
        LogDebug($"クエストID {questId} のデータを読み込み");
        // QuestData questData = DataManager.Instance.GetQuestData(questId);
        // if (questData != null)
        // {
        //     turnLimit = questData.turnLimit;
        // }
    }

    private void InitializePlayer()
    {
        // プレイヤーが既に存在するかチェック
        if (players.Count == 0)
        {
            // プレイヤーGameObject作成
            GameObject playerGO = new GameObject("Player");
            if (playerSpawnPoint != null)
            {
                playerGO.transform.SetParent(playerSpawnPoint);
                playerGO.transform.localPosition = Vector3.zero;
            }

            // BattlePlayerコンポーネント追加
            BattlePlayer player = playerGO.AddComponent<BattlePlayer>();
            player.InitializePlayerData();

            players.Add(player);
            LogDebug($"プレイヤーを初期化: {player.characterName} HP:{player.maxHP} ATK:{player.attackPower}");
        }
    }

    private void SpawnEnemies()
    {
        // 既存の敵をクリア
        foreach (var enemy in enemies)
        {
            if (enemy != null) DestroyImmediate(enemy.gameObject);
        }
        enemies.Clear();

        // TODO: クエストデータから敵の出現情報を取得
        // 現在は仮設定でスライムを3体生成
        int[] enemyIds = { 1, 1, 2 }; // スライム、スライム、ファイアスライム

        for (int i = 0; i < enemyIds.Length && i < enemySpawnPoints.Length; i++)
        {
            SpawnEnemy(enemyIds[i], i);
        }

        LogDebug($"敵を {enemies.Count} 体生成しました");
    }

    private void SpawnEnemy(int enemyId, int spawnIndex)
    {
        // 敵GameObject作成
        GameObject enemyGO = new GameObject($"Enemy_{enemyId}_{spawnIndex}");
        if (enemySpawnPoints[spawnIndex] != null)
        {
            enemyGO.transform.SetParent(enemySpawnPoints[spawnIndex]);
            enemyGO.transform.localPosition = Vector3.zero;
        }

        // BattleEnemyコンポーネント追加
        BattleEnemy enemy = enemyGO.AddComponent<BattleEnemy>();
        enemy.position = spawnIndex + 1;
        enemy.spawnOrder = spawnIndex;

        // マスターデータから初期化
        MonsterMasterData monsterData = GetMonsterMasterData(enemyId);
        if (monsterData != null)
        {
            enemy.InitializeFromMasterData(monsterData);
        }
        else
        {
            // フォールバック: デフォルトステータス
            LogDebug($"モンスターID {enemyId} のデータが見つからないため、デフォルト設定を使用");
            enemy.characterName = $"不明な敵{spawnIndex + 1}";
            enemy.enemyId = enemyId;
            enemy.maxHP = enemy.currentHP = 50;
            enemy.attackPower = 15;
            enemy.defensePower = 8;
            enemy.speed = 8;
            enemy.criticalRate = 5.0f;
        }

        enemies.Add(enemy);
    }

    private MonsterMasterData GetMonsterMasterData(int monsterId)
    {
        // Assets/GameData/Monsters/ フォルダからモンスターデータを検索
        var monsterAssets = Resources.LoadAll<MonsterMasterData>("GameData/Monsters");
        foreach (var monsterData in monsterAssets)
        {
            if (monsterData.monsterId == monsterId)
            {
                return monsterData;
            }
        }

        LogDebug($"MonsterID {monsterId} のマスターデータが見つかりません");
        return null;
    }

    private void DetermineActionOrder()
    {
        actionQueue.Clear();

        // 速度順でソート（降順）、同速の場合はプレイヤー優先
        var sortedCharacters = AliveCharacters
            .OrderByDescending(c => c.speed)
            .ThenBy(c => c is BattleEnemy ? 1 : 0) // プレイヤーを先に
            .ThenBy(c => c.position)
            .ToList();

        foreach (var character in sortedCharacters)
        {
            actionQueue.Enqueue(character);
        }

        string orderLog = string.Join(" → ", sortedCharacters.Select(c => $"{c.characterName}(速度{c.speed})"));
        LogDebug($"行動順序決定: {orderLog}");
    }

    private IEnumerator BattleLoop()
    {
        while (currentState == BattleState.InProgress)
        {
            // 限界ターン数チェック
            if (currentTurn >= turnLimit)
            {
                EndBattle(BattleResult.TimeUp);
                yield break;
            }

            // 行動順序が空になったら再生成
            if (actionQueue.Count == 0)
            {
                currentTurn++;
                LogDebug($"=== ターン {currentTurn} 開始 ===");
                DetermineActionOrder();
            }

            // 次のキャラクターの行動
            if (actionQueue.Count > 0)
            {
                currentActor = actionQueue.Dequeue();

                // 生存チェック
                if (!currentActor.isAlive)
                {
                    continue;
                }

                // キャラクターの行動処理
                yield return StartCoroutine(ProcessCharacterTurn(currentActor));

                // 勝敗判定
                if (CheckBattleEnd())
                {
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.2f); // 演出用待機
        }
    }

    private IEnumerator ProcessCharacterTurn(BattleCharacter character)
    {
        LogDebug($"--- {character.characterName} のターン ---");

        // ターン開始処理（状態異常効果など）
        character.OnTurnStart();

        // 死亡チェック（毒ダメージなどで死亡した場合）
        if (!character.isAlive)
        {
            LogDebug($"{character.characterName}はターン開始時に倒れました");
            yield break;
        }

        // 気絶チェック
        bool isStunned = character.activeEffects.Any(e => e.preventAction);
        if (isStunned)
        {
            LogDebug($"{character.characterName}は気絶して行動できません");
            character.OnTurnEnd();
            yield break;
        }

        // 行動選択
        BattleSkill selectedSkill = character.GetNextAction();
        string actionName = selectedSkill?.skillName ?? "通常攻撃";

        // ターゲット選択
        List<BattleCharacter> validTargets = GetValidTargets(character);
        BattleCharacter target = character.SelectTarget(validTargets);

        if (target == null)
        {
            LogDebug($"{character.characterName}の攻撃対象が見つかりません");
            character.OnTurnEnd();
            yield break;
        }

        // 行動実行
        LogDebug($"{character.characterName} が {target.characterName} に {actionName} を使用！");
        yield return StartCoroutine(ExecuteAction(character, selectedSkill, target));

        // ターン終了処理
        character.OnTurnEnd();

        yield return new WaitForSeconds(actionDelay);
    }

    private List<BattleCharacter> GetValidTargets(BattleCharacter attacker)
    {
        if (attacker is BattlePlayer)
        {
            return enemies.Where(e => e.isAlive).Cast<BattleCharacter>().ToList();
        }
        else
        {
            return players.Where(p => p.isAlive).Cast<BattleCharacter>().ToList();
        }
    }

    private IEnumerator ExecuteAction(BattleCharacter attacker, BattleSkill skill, BattleCharacter target)
    {
        // スキルの種類に応じて処理分岐
        if (skill != null)
        {
            yield return StartCoroutine(ExecuteSkill(attacker, skill, target));
        }
        else
        {
            yield return StartCoroutine(ExecuteNormalAttack(attacker, target));
        }
    }

    private IEnumerator ExecuteNormalAttack(BattleCharacter attacker, BattleCharacter target)
    {
        // 通常攻撃の処理
        int damage = DamageCalculator.CalculateDamage(attacker, target);
        target.TakeDamage(damage);

        LogDebug($"{target.characterName} に {damage} のダメージ！ (残りHP: {target.currentHP}/{target.maxHP})");

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator ExecuteSkill(BattleCharacter attacker, BattleSkill skill, BattleCharacter target)
    {
        // スキル使用
        skill.Use();

        // TODO: スキルマスターデータからスキルタイプを取得して処理分岐
        // 現在は簡易実装

        if (skill.skillId.Contains("heal"))
        {
            // 回復スキル
            int healAmount = DamageCalculator.CalculateHealAmount(attacker, skill);
            attacker.Heal(healAmount);
            LogDebug($"{attacker.characterName} は {healAmount} 回復しました！ (HP: {attacker.currentHP}/{attacker.maxHP})");
        }
        else if (skill.skillId.Contains("guard") || skill.skillId.Contains("armor"))
        {
            // バフスキル（防御力上昇など）
            ApplyBuffEffect(attacker, skill);
        }
        else if (skill.skillId.Contains("howl"))
        {
            // デバフスキル（攻撃力低下など）
            ApplyDebuffEffect(target, skill);
        }
        else
        {
            // 攻撃スキル
            int damage = DamageCalculator.CalculateDamage(attacker, target, skill);
            target.TakeDamage(damage);
            LogDebug($"{target.characterName} に {damage} のダメージ！ (残りHP: {target.currentHP}/{target.maxHP})");
        }

        yield return new WaitForSeconds(1.0f);
    }

    private void ApplyBuffEffect(BattleCharacter target, BattleSkill skill)
    {
        // 簡易バフ効果適用
        StatusEffect buffEffect = new StatusEffect
        {
            effectId = "defense_up",
            effectName = "防御力上昇",
            remainingTurns = 3,
            effectType = StatusEffectType.Buff,
            defenseMultiplier = 1.7f
        };

        target.ApplyStatusEffect(buffEffect);
        LogDebug($"{target.characterName} の防御力が上昇しました！");
    }

    private void ApplyDebuffEffect(BattleCharacter target, BattleSkill skill)
    {
        // 簡易デバフ効果適用
        StatusEffect debuffEffect = new StatusEffect
        {
            effectId = "attack_down",
            effectName = "攻撃力低下",
            remainingTurns = 2,
            effectType = StatusEffectType.Debuff,
            attackMultiplier = 0.7f,
            fireAttackMultiplier = 0.7f,
            waterAttackMultiplier = 0.7f,
            windAttackMultiplier = 0.7f,
            earthAttackMultiplier = 0.7f
        };

        target.ApplyStatusEffect(debuffEffect);
        LogDebug($"{target.characterName} の攻撃力が低下しました！");
    }

    private bool CheckBattleEnd()
    {
        bool allEnemiesDead = enemies.All(e => !e.isAlive);
        bool allPlayersDead = players.All(p => !p.isAlive);

        if (allEnemiesDead)
        {
            EndBattle(BattleResult.Victory);
            return true;
        }
        else if (allPlayersDead)
        {
            EndBattle(BattleResult.Defeat);
            return true;
        }

        return false;
    }

    private void EndBattle(BattleResult result)
    {
        battleResult = result;

        switch (result)
        {
            case BattleResult.Victory:
                currentState = BattleState.Victory;
                LogDebug("🎉 戦闘勝利！");
                ShowVictoryUI();
                ProcessVictoryRewards();
                break;

            case BattleResult.Defeat:
                currentState = BattleState.Defeat;
                LogDebug("💀 戦闘敗北...");
                ShowDefeatUI();
                break;

            case BattleResult.TimeUp:
                currentState = BattleState.TimeUp;
                LogDebug("⏰ 時間切れで敗北...");
                ShowDefeatUI();
                break;
        }

        OnBattleEnd?.Invoke(result);
    }

    private void ShowVictoryUI()
    {
        if (battleUI != null) battleUI.SetActive(false);
        if (victoryUI != null) victoryUI.SetActive(true);
    }

    private void ShowDefeatUI()
    {
        if (battleUI != null) battleUI.SetActive(false);
        if (defeatUI != null) defeatUI.SetActive(true);
    }

    private void ProcessVictoryRewards()
    {
        // TODO: クエストマスターデータからドロップ処理
        LogDebug("報酬処理を実行中...");

        // 仮の報酬処理
        int expGain = 50;
        int goldGain = 100;

        LogDebug($"経験値 {expGain} を獲得！");
        LogDebug($"ゴールド {goldGain} を獲得！");
    }

    // UI用のメソッド
    public void OnHomeButtonClicked()
    {
        LogDebug("ホームに戻ります");
        SceneManager.LoadScene("HomeScene");
    }

    public void OnRetryButtonClicked()
    {
        LogDebug("戦闘をリトライします");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }



    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[BattleManager] {message}");
        }
    }
}