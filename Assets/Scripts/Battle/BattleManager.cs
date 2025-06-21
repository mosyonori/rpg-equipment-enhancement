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

    private void Start()
    {
        InitializeBattle();
    }

    public void InitializeBattle()
    {
        currentState = BattleState.Initializing;
        currentTurn = 0;
        battleResult = BattleResult.None;

        InitializeUI();
        LoadQuestData();
        InitializePlayer();
        SpawnEnemies();

        // ★デバッグ: 初期化後の状況確認
        AddBattleLog($"戦闘初期化完了 - プレイヤー数: {players.Count}, 敵数: {enemies.Count}");
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                AddBattleLog($"初期化敵: {enemy.characterName} HP:{enemy.currentHP}/{enemy.maxHP} 生存:{enemy.isAlive}");
            }
        }

        DetermineActionOrder();

        currentState = BattleState.InProgress;
        StartCoroutine(BattleLoop());
    }

    private void LoadQuestData()
    {
        // TODO: DataManagerからクエストデータを取得
        // 現在は仮設定
    }

    private void InitializePlayer()
    {
        if (players.Count == 0)
        {
            GameObject playerGO = new GameObject("Player");

            if (playerSpawnPoint != null)
            {
                playerGO.transform.SetParent(playerSpawnPoint, false);
                playerGO.transform.localPosition = Vector3.zero;
            }

            BattlePlayer player = playerGO.AddComponent<BattlePlayer>();

            if (player != null)
            {
                // 基本ステータス設定
                player.characterName = "プレイヤー";
                player.maxHP = player.currentHP = 100;
                player.attackPower = 25;
                player.defensePower = 12;
                player.speed = 15;
                player.criticalRate = 10.0f;
                player.position = 0;
                player.isAlive = true;

                // スキル初期化
                player.skill1 = new BattleSkill
                {
                    skillId = "skill_player_attack",
                    skillName = "強攻撃",
                    maxCoolTime = 2,
                    currentCoolTime = 0
                };

                player.skill2 = new BattleSkill
                {
                    skillId = "skill_player_heal",
                    skillName = "回復",
                    maxCoolTime = 3,
                    currentCoolTime = 0
                };

                player.activeEffects = new List<StatusEffect>();
                players.Add(player);

                // 視覚表示作成
                if (playerSpawnPoint != null)
                {
                    CreateCharacterVisual(player, playerSpawnPoint, Color.blue);
                }
            }
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

        int[] enemyIds = { 1, 1, 2 };

        for (int i = 0; i < enemyIds.Length && i < enemySpawnPoints.Length; i++)
        {
            SpawnEnemy(enemyIds[i], i);
        }
    }

    private void SpawnEnemy(int enemyId, int spawnIndex)
    {
        GameObject enemyGO = new GameObject($"Enemy_{enemyId}_{spawnIndex}");

        if (enemySpawnPoints != null && spawnIndex < enemySpawnPoints.Length && enemySpawnPoints[spawnIndex] != null)
        {
            enemyGO.transform.SetParent(enemySpawnPoints[spawnIndex], false);
            enemyGO.transform.localPosition = Vector3.zero;
        }

        BattleEnemy enemy = enemyGO.AddComponent<BattleEnemy>();

        if (enemy != null)
        {
            enemy.position = spawnIndex + 1;

            // マスターデータから初期化を試行
            MonsterMasterData monsterData = GetMonsterMasterData(enemyId);
            if (monsterData != null)
            {
                try
                {
                    var initMethod = enemy.GetType().GetMethod("InitializeFromMasterData");
                    if (initMethod != null)
                    {
                        initMethod.Invoke(enemy, new object[] { monsterData });
                        AddBattleLog($"マスターデータから敵を初期化: {enemy.characterName}");
                    }
                    else
                    {
                        SetEnemyDefaultStats(enemy, enemyId, spawnIndex);
                    }
                }
                catch
                {
                    SetEnemyDefaultStats(enemy, enemyId, spawnIndex);
                }
            }
            else
            {
                SetEnemyDefaultStats(enemy, enemyId, spawnIndex);
            }

            // ★重要: 敵をリストに追加
            enemies.Add(enemy);
            AddBattleLog($"敵を生成: {enemy.characterName} HP:{enemy.currentHP}/{enemy.maxHP}, 生存: {enemy.isAlive}");

            // 視覚表示作成
            if (enemySpawnPoints != null && spawnIndex < enemySpawnPoints.Length && enemySpawnPoints[spawnIndex] != null)
            {
                CreateCharacterVisual(enemy, enemySpawnPoints[spawnIndex], Color.red);
            }
        }
    }

    private void SetEnemyDefaultStats(BattleEnemy enemy, int enemyId, int spawnIndex)
    {
        enemy.characterName = $"不明な敵{spawnIndex + 1}";
        enemy.maxHP = enemy.currentHP = 50;
        enemy.attackPower = 15;
        enemy.defensePower = 8;
        enemy.speed = 8;
        enemy.criticalRate = 5.0f;
        enemy.isAlive = true;

        enemy.skill1 = new BattleSkill
        {
            skillId = "skill_enemy_attack",
            skillName = "敵攻撃",
            maxCoolTime = 3,
            currentCoolTime = 0
        };

        enemy.activeEffects = new List<StatusEffect>();
    }

    private MonsterMasterData GetMonsterMasterData(int monsterId)
    {
        var monsterAssets = Resources.LoadAll<MonsterMasterData>("GameData/Monsters");
        return monsterAssets.FirstOrDefault(data => data.monsterId == monsterId);
    }

    // キャラクター視覚表示作成（3D対応）
    private void CreateCharacterVisual(BattleCharacter character, Transform spawnPoint, Color color)
    {
        if (character == null || spawnPoint == null) return;

        GameObject visualGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visualGO.name = $"{character.characterName}_Visual";
        visualGO.transform.SetParent(spawnPoint, false);
        visualGO.transform.localPosition = Vector3.zero;
        visualGO.transform.localScale = Vector3.one * 2.0f;

        // マテリアル設定
        var renderer = visualGO.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            renderer.material = mat;
        }

        // 3Dテキスト表示
        CreateWorldSpaceText(character, visualGO.transform);
    }

    private void CreateWorldSpaceText(BattleCharacter character, Transform parent)
    {
        GameObject textGO = new GameObject($"{character.characterName}_Text");
        textGO.transform.SetParent(parent, false);
        textGO.transform.localPosition = new Vector3(0, 2.5f, 0);

        TextMesh textMesh = textGO.AddComponent<TextMesh>();
        textMesh.text = character.characterName;
        textMesh.fontSize = 40;
        textMesh.characterSize = 0.1f;
        textMesh.color = Color.white;
        textMesh.anchor = TextAnchor.MiddleCenter;

        // カメラ方向に向ける
        if (Camera.main != null)
        {
            Vector3 directionToCamera = Camera.main.transform.position - textGO.transform.position;
            textGO.transform.rotation = Quaternion.LookRotation(-directionToCamera);
        }
    }

    private void DetermineActionOrder()
    {
        actionQueue.Clear();

        var sortedCharacters = AllCharacters
            .Where(c => c != null && c.isAlive)
            .OrderByDescending(c => c.speed)
            .ThenBy(c => c is BattleEnemy ? 1 : 0)
            .ThenBy(c => c.position)
            .ToList();

        foreach (var character in sortedCharacters)
        {
            actionQueue.Enqueue(character);
        }
    }

    private IEnumerator BattleLoop()
    {
        while (currentState == BattleState.InProgress)
        {
            if (currentTurn >= turnLimit)
            {
                EndBattle(BattleResult.TimeUp);
                yield break;
            }

            if (actionQueue.Count == 0)
            {
                currentTurn++;
                UpdateUI();
                DetermineActionOrder();
            }

            if (actionQueue.Count > 0)
            {
                currentActor = actionQueue.Dequeue();

                if (currentActor != null && currentActor.isAlive)
                {
                    yield return StartCoroutine(ProcessCharacterTurn(currentActor));
                    UpdateUI();

                    if (CheckBattleEnd())
                    {
                        yield break;
                    }
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator ProcessCharacterTurn(BattleCharacter character)
    {
        if (character == null) yield break;

        // ターン開始処理
        character.OnTurnStart();
        if (!character.isAlive) yield break;

        // 気絶チェック
        bool isStunned = character.activeEffects?.Any(e => e.preventAction) == true;
        if (isStunned)
        {
            character.OnTurnEnd();
            yield break;
        }

        // 行動選択
        BattleSkill selectedSkill = character.GetNextAction();
        string actionName = selectedSkill?.skillName ?? "通常攻撃";

        // ターゲット選択
        var validTargets = GetValidTargets(character);
        if (validTargets.Count == 0)
        {
            character.OnTurnEnd();
            yield break;
        }

        BattleCharacter target = character.SelectTarget(validTargets);
        if (target == null)
        {
            character.OnTurnEnd();
            yield break;
        }

        // 行動実行
        AddBattleLog($"{character.characterName} が {target.characterName} に {actionName} を使用！");
        yield return StartCoroutine(ExecuteAction(character, selectedSkill, target));

        // ターン終了処理
        character.OnTurnEnd();
        yield return new WaitForSeconds(actionDelay);
    }

    private List<BattleCharacter> GetValidTargets(BattleCharacter attacker)
    {
        if (attacker is BattlePlayer)
        {
            return enemies.Where(e => e != null && e.isAlive).Cast<BattleCharacter>().ToList();
        }
        else
        {
            return players.Where(p => p != null && p.isAlive).Cast<BattleCharacter>().ToList();
        }
    }

    private IEnumerator ExecuteAction(BattleCharacter attacker, BattleSkill skill, BattleCharacter target)
    {
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
        if (attacker == null || target == null) yield break;

        int damage = DamageCalculator.CalculateDamage(attacker, target);
        target.TakeDamage(damage);

        AddBattleLog($"{target.characterName} に {damage} のダメージ！ (残りHP: {target.currentHP}/{target.maxHP})");
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator ExecuteSkill(BattleCharacter attacker, BattleSkill skill, BattleCharacter target)
    {
        if (skill == null || attacker == null) yield break;

        skill.Use();

        if (skill.skillId.Contains("heal"))
        {
            int healAmount = DamageCalculator.CalculateHealAmount(attacker, skill);
            attacker.Heal(healAmount);
            AddBattleLog($"{attacker.characterName} は {healAmount} 回復しました！ (HP: {attacker.currentHP}/{attacker.maxHP})");
        }
        else if (skill.skillId.Contains("guard") || skill.skillId.Contains("armor"))
        {
            ApplyBuffEffect(attacker, skill);
        }
        else if (skill.skillId.Contains("howl"))
        {
            ApplyDebuffEffect(target, skill);
        }
        else
        {
            if (target != null)
            {
                int damage = DamageCalculator.CalculateDamage(attacker, target, skill);
                target.TakeDamage(damage);
                AddBattleLog($"{target.characterName} に {damage} のダメージ！ (残りHP: {target.currentHP}/{target.maxHP})");
            }
        }

        yield return new WaitForSeconds(1.0f);
    }

    private void ApplyBuffEffect(BattleCharacter target, BattleSkill skill)
    {
        if (target == null) return;

        StatusEffect buffEffect = new StatusEffect
        {
            effectId = "defense_up",
            effectName = "防御力上昇",
            remainingTurns = 3,
            effectType = StatusEffectType.Buff,
            defenseMultiplier = 1.7f
        };

        target.ApplyStatusEffect(buffEffect);
        AddBattleLog($"{target.characterName} の防御力が上昇しました！");
    }

    private void ApplyDebuffEffect(BattleCharacter target, BattleSkill skill)
    {
        if (target == null) return;

        StatusEffect debuffEffect = new StatusEffect
        {
            effectId = "attack_down",
            effectName = "攻撃力低下",
            remainingTurns = 2,
            effectType = StatusEffectType.Debuff,
            attackMultiplier = 0.7f
        };

        target.ApplyStatusEffect(debuffEffect);
        AddBattleLog($"{target.characterName} の攻撃力が低下しました！");
    }

    private bool CheckBattleEnd()
    {
        bool allEnemiesDead = enemies.All(e => e == null || !e.isAlive);
        bool allPlayersDead = players.All(p => p == null || !p.isAlive);

        // デバッグ用ログ
        AddBattleLog($"戦闘終了チェック - 敵全滅: {allEnemiesDead}, プレイヤー全滅: {allPlayersDead}");

        // 生存状況を詳細ログ
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                AddBattleLog($"敵 {enemy.characterName}: HP {enemy.currentHP}/{enemy.maxHP}, 生存: {enemy.isAlive}");
            }
        }

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
                ShowVictoryUI();
                ProcessVictoryRewards();
                break;

            case BattleResult.Defeat:
                currentState = BattleState.Defeat;
                ShowDefeatUI();
                break;

            case BattleResult.TimeUp:
                currentState = BattleState.TimeUp;
                ShowDefeatUI();
                break;
        }

        OnBattleEnd?.Invoke(result);
    }

    private void ShowVictoryUI()
    {
        if (battleUI != null)
        {
            // 戦闘UI要素を非表示
            foreach (Transform child in battleUI.transform)
            {
                if (child.gameObject != victoryUI && child.gameObject != defeatUI)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        if (victoryUI != null)
        {
            victoryUI.SetActive(true);
        }
        else
        {
            AddBattleLog("=== 🎉 戦闘勝利！ ===");
        }
    }

    private void ShowDefeatUI()
    {
        if (battleUI != null)
        {
            // 戦闘UI要素を非表示
            foreach (Transform child in battleUI.transform)
            {
                if (child.gameObject != victoryUI && child.gameObject != defeatUI)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        if (defeatUI != null)
        {
            defeatUI.SetActive(true);
        }
        else
        {
            AddBattleLog("=== 💀 戦闘敗北... ===");
        }
    }

    private void ProcessVictoryRewards()
    {
        int expGain = 50;
        int goldGain = 100;
        AddBattleLog($"経験値 {expGain} を獲得！");
        AddBattleLog($"ゴールド {goldGain} を獲得！");
    }

    private void InitializeUI()
    {
        if (battleUI != null) battleUI.SetActive(true);
        if (victoryUI != null) victoryUI.SetActive(false);
        if (defeatUI != null) defeatUI.SetActive(false);

        SetupButtonEvents();

        if (battleLogText != null)
        {
            battleLogText.text = "=== 戦闘開始 ===\n";
        }
    }

    private void UpdateUI()
    {
        if (questNameText != null)
            questNameText.text = $"クエスト {questId}";

        if (turnInfoText != null)
            turnInfoText.text = $"ターン {currentTurn}/{turnLimit}";

        UpdateCharacterUI();
    }

    private void UpdateCharacterUI()
    {
        // プレイヤーUI更新
        if (playerUIParent != null)
        {
            // 既存のUIを削除
            for (int i = playerUIParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(playerUIParent.GetChild(i).gameObject);
            }

            // プレイヤーUIを作成
            foreach (var player in players)
            {
                if (player != null)
                {
                    CreateCharacterUI(player, playerUIParent);
                    // UI専用の視覚表示も作成
                    CreateUICharacterVisual(player, playerUIParent, Color.blue);
                }
            }
        }

        // 敵UI更新
        if (enemyUIParent != null)
        {
            // 既存のUIを削除
            for (int i = enemyUIParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(enemyUIParent.GetChild(i).gameObject);
            }

            // 敵UIを作成
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    CreateCharacterUI(enemy, enemyUIParent);
                    // UI専用の視覚表示も作成
                    CreateUICharacterVisual(enemy, enemyUIParent, Color.red);
                }
            }
        }
    }

    // UI専用のキャラクター視覚表示を復元
    private void CreateUICharacterVisual(BattleCharacter character, Transform parent, Color color)
    {
        if (character == null || parent == null) return;

        // UI専用の円形画像を作成
        GameObject visualGO = new GameObject($"{character.characterName}_UIVisual");
        visualGO.transform.SetParent(parent, false);

        // RectTransform設定
        var rectTransform = visualGO.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(60, 60);
        rectTransform.anchorMin = new Vector2(0.5f, 0.9f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.9f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        // Image コンポーネント追加
        var image = visualGO.AddComponent<UnityEngine.UI.Image>();

        // 円形のスプライトを作成
        Texture2D texture = new Texture2D(64, 64);
        Color32[] pixels = new Color32[64 * 64];

        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                int index = y * 64 + x;

                if (distance <= 28)
                {
                    pixels[index] = color;
                }
                else
                {
                    pixels[index] = Color.clear;
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        Sprite circleSprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        image.sprite = circleSprite;

        AddBattleLog($"UI視覚表示作成: {character.characterName} ({color})");
    }

    private void CreateCharacterUI(BattleCharacter character, Transform parent)
    {
        if (character == null || parent == null) return;

        if (characterUIPrefab != null)
        {
            GameObject uiGO = Instantiate(characterUIPrefab, parent);
            var nameText = uiGO.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = $"{character.characterName}\nHP: {character.currentHP}/{character.maxHP}";
            }
        }
        else
        {
            // プレハブがない場合は簡易テキストUIを作成
            GameObject uiGO = new GameObject($"{character.characterName}_UI");
            uiGO.transform.SetParent(parent, false);

            var rectTransform = uiGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);

            var text = uiGO.AddComponent<TextMeshProUGUI>();
            text.text = $"{character.characterName}\nHP: {character.currentHP}/{character.maxHP}";
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
        }
    }

    private void SetupButtonEvents()
    {
        homeButton?.onClick.AddListener(OnHomeButtonClicked);
        victoryHomeButton?.onClick.AddListener(OnHomeButtonClicked);
        defeatHomeButton?.onClick.AddListener(OnHomeButtonClicked);
        retryButton?.onClick.AddListener(OnRetryButtonClicked);
        autoButton?.onClick.AddListener(OnAutoButtonClicked);
        settingsButton?.onClick.AddListener(OnSettingsButtonClicked);
    }

    public void OnHomeButtonClicked()
    {
        SceneManager.LoadScene("HomeScene");
    }

    public void OnRetryButtonClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnAutoButtonClicked()
    {
        // TODO: オート戦闘機能実装
    }

    public void OnSettingsButtonClicked()
    {
        // TODO: 設定画面実装
    }

    private void AddBattleLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[BattleManager] {message}");
        }

        if (battleLogText != null)
        {
            if (string.IsNullOrEmpty(battleLogText.text))
            {
                battleLogText.text = message;
            }
            else
            {
                battleLogText.text += "\n" + message;
            }

            if (battleLogScrollRect != null)
            {
                StartCoroutine(ScrollToBottom());
            }
        }
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); // 2フレーム待機で確実に更新

        if (battleLogScrollRect != null)
        {
            Canvas.ForceUpdateCanvases(); // キャンバス強制更新
            battleLogScrollRect.verticalNormalizedPosition = 0f;

            // スクロールビューの設定確認・修正
            var scrollRect = battleLogScrollRect;
            if (scrollRect.content != null)
            {
                // コンテンツサイズを更新
                var contentSizeFitter = scrollRect.content.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter == null)
                {
                    contentSizeFitter = scrollRect.content.gameObject.AddComponent<ContentSizeFitter>();
                    contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }

                // レイアウトグループの追加
                var layoutGroup = scrollRect.content.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
                    layoutGroup.childForceExpandHeight = false;
                    layoutGroup.childControlHeight = true;
                }
            }
        }
    }

    // イベント
    public System.Action<BattleResult> OnBattleEnd;

    // プロパティ
    public List<BattleCharacter> AllCharacters =>
        players.Cast<BattleCharacter>().Concat(enemies.Cast<BattleCharacter>()).ToList();

    public List<BattleCharacter> AliveCharacters =>
        AllCharacters.Where(c => c != null && c.isAlive).ToList();
}