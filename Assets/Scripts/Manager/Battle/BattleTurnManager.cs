using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

/// <summary>
/// ターン・行動順制御管理クラス
/// ターン制バトルの進行制御とオートバトルAIを全担当
/// </summary>
public class BattleTurnManager : MonoBehaviour
{
    [Header("ターン制御設定")]
    [SerializeField] private float turnActionDelay = 1.0f;
    [SerializeField] private float skillAnimationDelay = 0.5f;
    [SerializeField] private bool enableAIDebugLog = true;

    [Header("オートバトル設定")]
    [SerializeField] private float autoActionInterval = 2.0f;
    [SerializeField] private bool prioritizeWeakTargets = true;
    [SerializeField] private bool avoidOverkill = true;

    // シングルトンパターン
    public static BattleTurnManager Instance { get; private set; }

    // 内部状況
    public bool IsTurnInProgress { get; private set; }
    public int CurrentTurnNumber { get; private set; }
    public string CurrentActorId { get; private set; }

    // 行動順序管理
    private List<string> turnOrder;
    private int currentTurnIndex;

    // イベント
    public static event System.Action<string, int> OnTurnStart;
    public static event System.Action<ActionData> OnActionExecuted;
    public static event System.Action<int> OnTurnComplete;
    public static event System.Action<int> OnTurnCompleted; // BattleManager互換用
    public static event System.Action OnAllEnemiesDefeated; // BattleManager互換用
    public static event System.Action OnPlayerDefeated; // BattleManager互換用
    public static event System.Action OnTurnLimitReached; // BattleManager互換用

    // 参照Manager
    private BattleDataManager dataManager;
    private BattleCalculationManager calculationManager;

    // ターン制限値
    private int turnLimit = -1; // -1は無制限

    // コルーチン制御
    private Coroutine turnCoroutine;

    // 修正: 初期化状態管理
    private bool isManagerInitialized = false;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DebugLog("BattleTurnManager Awake - シングルトン設定完了");
        }
        else
        {
            DebugLog("BattleTurnManager重複インスタンス検出 - 削除");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 依存関係は後から設定されるため、BattleDataManagerはシングルトンではない）
        calculationManager = BattleCalculationManager.Instance;

        if (calculationManager == null)
        {
            DebugLogError("BattleCalculationManagerが見つかりません");
        }
        else
        {
            DebugLog("BattleCalculationManager参照取得完了");
        }
    }

    #endregion

    #region 公開メソッド - 設定・制御

    /// <summary>
    /// BattleDataManagerの参照を設定
    /// </summary>
    public void SetDataManager(BattleDataManager battleDataManager)
    {
        dataManager = battleDataManager;
        DebugLog("BattleDataManagerの参照を設定しました");

        // 修正: データマネージャー設定完了フラグ
        CheckManagerInitialization();
    }

    /// <summary>
    /// ターン制限を設定
    /// </summary>
    public void SetTurnLimit(int limit)
    {
        turnLimit = limit;
        DebugLog($"ターン制限を設定: {(limit > 0 ? limit.ToString() : "無制限")}");
    }

    /// <summary>
    /// 戦闘速度を設定（BattleManager互換用）
    /// </summary>
    public void SetBattleSpeed(float speedMultiplier)
    {
        // 実装は現在のところ何もしない（将来拡張可能）
        DebugLog($"戦闘速度設定: {speedMultiplier}倍");
    }

    /// <summary>
    /// 一時停止設定（BattleManager互換用）
    /// </summary>
    public void SetPaused(bool paused)
    {
        if (paused)
        {
            StopTurnProcessing();
        }
        else if (!IsTurnInProgress && isManagerInitialized)
        {
            StartTurnProcessing();
        }
        DebugLog($"一時停止設定: {paused}");
    }

    /// <summary>
    /// 修正: 戦闘初期化（BattleManager経由用）
    /// </summary>
    public void InitializeBattle(List<BattleCharacterData> characters, int battleTurnLimit)
    {
        if (characters == null || characters.Count == 0)
        {
            DebugLogError("キャラクターリストが空です");
            return;
        }

        SetTurnLimit(battleTurnLimit);

        // 修正: データマネージャーが設定されていることを確認
        if (dataManager == null)
        {
            DebugLogError("BattleDataManagerが設定されていません。InitializeBattleを実行できません。");
            return;
        }

        InitializeTurnOrder();

        DebugLog($"戦闘初期化完了: キャラクター{characters.Count}体, ターン制限{battleTurnLimit}");
        DebugLog($"行動順序: {string.Join(" → ", turnOrder)}");
    }

    /// <summary>
    /// 修正: Manager初期化状態チェック
    /// </summary>
    private void CheckManagerInitialization()
    {
        if (dataManager != null && calculationManager != null)
        {
            isManagerInitialized = true;
            DebugLog("全Managerの初期化完了");
        }
    }

    #endregion

    #region 公開メソッド - ターン管理

    /// <summary>
    /// 修正: 行動順序初期化（デバッグ強化版）
    /// </summary>
    public void InitializeTurnOrder()
    {
        DebugLog("** 行動順序初期化開始 **");

        if (dataManager == null)
        {
            DebugLogError("BattleDataManager が初期化されていません");
            return;
        }

        var allCharacters = dataManager.GetAllCharacters();
        DebugLog($"取得キャラクター数: {allCharacters?.Count ?? 0}");

        if (allCharacters == null || allCharacters.Count == 0)
        {
            DebugLogError("戦闘キャラクターが存在しません");
            return;
        }

        // 生存キャラクターをログ出力
        var aliveCharacters = allCharacters.Where(c => c.isAlive && c.CanAct()).ToList();
        DebugLog($"生存行動可能キャラクター数: {aliveCharacters.Count}");

        foreach (var character in aliveCharacters)
        {
            DebugLog($"行動可能: {character.characterName} (速度:{character.speed}, プレイヤー:{character.isPlayer})");
        }

        // 速度順でソート（同速の場合はプレイヤー優先、敵は配置順）
        var sortedCharacters = allCharacters
            .Where(c => c.isAlive && c.CanAct())
            .OrderByDescending(c => c.speed)
            .ThenByDescending(c => c.isPlayer ? 1 : 0) // プレイヤー優先
            .ThenBy(c => c.characterId) // 敵は配置順（IDの昇順）
            .ToList();

        turnOrder = sortedCharacters.Select(c => c.characterId).ToList();
        currentTurnIndex = 0;
        CurrentTurnNumber = 1;

        DebugLog($"** 行動順序を決定: {string.Join(" → ", turnOrder)} **");
        DebugLog($"最初の行動者: {GetCurrentActorId()}");
    }

    /// <summary>
    /// 修正: StartTurnProcessing（エラー修正版）
    /// </summary>
    public void StartTurnProcessing()
    {
        // 最重要ログを必ず出力
        DebugLog("**** StartTurnProcessing が呼び出されました ****");

        try
        {
            DebugLog($"現在の状態チェック開始");
            DebugLog($"IsTurnInProgress: {IsTurnInProgress}");
            DebugLog($"isManagerInitialized: {isManagerInitialized}");
            DebugLog($"gameObject.activeInHierarchy: {gameObject.activeInHierarchy}");
            DebugLog($"this.enabled: {this.enabled}");

            if (IsTurnInProgress)
            {
                DebugLogWarning("既にターン処理中です");
                return;
            }

            // 修正: 初期化状態チェック
            if (!isManagerInitialized)
            {
                DebugLogError("Managerが完全に初期化されていません");
                return;
            }

            DebugLog($"dataManager チェック: {(dataManager == null ? "null" : "存在")}");

            if (dataManager == null)
            {
                DebugLogError("BattleDataManagerが設定されていません");
                return;
            }

            // 修正: GetAllCharacters()が実際のデータを確認
            var allCharacters = dataManager.GetAllCharacters();
            DebugLog($"dataManager.GetAllCharacters(): {allCharacters?.Count ?? 0}体");

            if (allCharacters != null)
            {
                foreach (var character in allCharacters)
                {
                    DebugLog($"キャラクター: {character.characterName} (プレイヤー:{character.isPlayer}, 生存:{character.isAlive})");
                }
            }

            DebugLog($"turnOrder チェック: {(turnOrder == null ? "null" : $"要素数{turnOrder.Count}")}");

            // 修正: ターン順序が未初期化または空の場合、強制的に再初期化
            if (turnOrder == null || turnOrder.Count == 0)
            {
                DebugLogWarning("行動順序が初期化されていません - 再初期化を実行");

                InitializeTurnOrder();

                DebugLog($"再初期化後のturnOrder: {(turnOrder == null ? "null" : $"要素数{turnOrder.Count}")}");

                if (turnOrder == null || turnOrder.Count == 0)
                {
                    DebugLogError("行動順序の再初期化に失敗しました");
                    return;
                }
            }

            // 修正: 既存コルーチンの安全な停止
            if (turnCoroutine != null)
            {
                DebugLog("既存のコルーチンを停止");
                StopCoroutine(turnCoroutine);
                turnCoroutine = null;
            }

            DebugLog("===== ターン処理開始 =====");
            DebugLog($"行動順序: {string.Join(" → ", turnOrder)}");
            DebugLog($"最初の行動者: {GetCurrentActorId()}");

            DebugLog("StartCoroutine(TurnProcessingCoroutine)呼び出し");
            turnCoroutine = StartCoroutine(TurnProcessingCoroutine());
            DebugLog("StartCoroutine(TurnProcessingCoroutine)完了");

        }
        catch (System.Exception e)
        {
            DebugLogError($"StartTurnProcessing中に例外: {e.Message}");
            DebugLogError($"スタックトレース: {e.StackTrace}");
        }

        DebugLog("**** StartTurnProcessing 終了 ****");
    }

    /// <summary>
    /// ターン処理を停止
    /// </summary>
    public void StopTurnProcessing()
    {
        if (turnCoroutine != null)
        {
            StopCoroutine(turnCoroutine);
            turnCoroutine = null;
            DebugLog("ターン処理コルーチン停止");
        }

        IsTurnInProgress = false;
        CurrentActorId = "";
        DebugLog("ターン処理停止完了");
    }

    /// <summary>
    /// 現在の行動者を取得
    /// </summary>
    public BattleCharacterData GetCurrentActor()
    {
        if (string.IsNullOrEmpty(CurrentActorId)) return null;
        return dataManager?.GetCharacter(CurrentActorId);
    }

    /// <summary>
    /// 次の行動者を取得（プレビュー用）
    /// </summary>
    public BattleCharacterData GetNextActor()
    {
        if (turnOrder == null || turnOrder.Count == 0) return null;

        int nextIndex = (currentTurnIndex + 1) % turnOrder.Count;
        return dataManager?.GetCharacter(turnOrder[nextIndex]);
    }

    /// <summary>
    /// 行動順序リストを取得
    /// </summary>
    public List<BattleCharacterData> GetTurnOrder()
    {
        if (turnOrder == null || dataManager == null) return new List<BattleCharacterData>();

        return turnOrder
            .Select(id => dataManager.GetCharacter(id))
            .Where(c => c != null && c.isAlive)
            .ToList();
    }

    /// <summary>
    /// 現在のターンキャラクターを取得（BattleManager互換用）
    /// </summary>
    public BattleCharacterData GetCurrentTurnCharacter()
    {
        return GetCurrentActor();
    }

    /// <summary>
    /// 次のターンに進む（BattleManager互換用）
    /// </summary>
    public void AdvanceToNextTurn()
    {
        AdvanceToNextActor();
    }

    /// <summary>
    /// キャラクターの行動を決定（BattleManager互換用）
    /// </summary>
    public ActionData DecideAction(BattleCharacterData character, List<BattleCharacterData> allCharacters)
    {
        // 優先順位に従うスキル選択
        var selectedSkill = SelectSkillByPriority(character);

        if (selectedSkill != null)
        {
            // スキル使用のActionDataを作成
            var targets = GetValidTargets(character, selectedSkill);
            if (targets.Count > 0)
            {
                var targetIds = targets.Select(t => t.characterId).ToList();
                var targetNames = targets.Select(t => t.characterName).ToList();

                return ActionData.CreateSkillUse(
                    character.characterId,
                    character.characterName,
                    character.isPlayer,
                    selectedSkill,
                    targetIds,
                    targetNames,
                    CurrentTurnNumber
                );
            }
        }

        // 通常攻撃のActionDataを作成
        var enemies = allCharacters.Where(c => c.isPlayer != character.isPlayer && c.isAlive).ToList();
        if (enemies.Count > 0)
        {
            var target = enemies[UnityEngine.Random.Range(0, enemies.Count)];
            return ActionData.CreateNormalAttack(
                character.characterId,
                character.characterName,
                character.isPlayer,
                target.characterId,
                target.characterName,
                CurrentTurnNumber
            );
        }

        return null;
    }

    #endregion

    #region ターン処理コルーチン

    /// <summary>
    /// 修正: TurnProcessingCoroutine（エラー修正版）
    /// </summary>
    private IEnumerator TurnProcessingCoroutine()
    {
        DebugLog("★★★ TurnProcessingCoroutine 開始 ★★★");

        IsTurnInProgress = true;
        DebugLog($"IsTurnInProgress を true に設定");

        int loopCount = 0;
        const int MAX_LOOPS = 1000;

        DebugLog("while ループ開始前の状態チェック");
        DebugLog($"AreAllPlayersDefeated: {dataManager.AreAllPlayersDefeated()}");
        DebugLog($"AreAllEnemiesDefeated: {dataManager.AreAllEnemiesDefeated()}");

        while (!dataManager.AreAllPlayersDefeated() && !dataManager.AreAllEnemiesDefeated())
        {
            loopCount++;
            DebugLog($"===== ループ {loopCount} 開始 =====");

            if (loopCount > MAX_LOOPS)
            {
                DebugLogError($"ターン処理が{MAX_LOOPS}回を超えました。強制終了します。");
                break;
            }

            // ステップ5: 制限ターン数チェック
            if (CheckTurnLimit())
            {
                DebugLog("ターン制限に達しました");
                OnTurnLimitReached?.Invoke();
                yield break;
            }

            // 現在の行動者を取得
            CurrentActorId = GetCurrentActorId();
            var currentActor = dataManager.GetCharacter(CurrentActorId);

            DebugLog($"ターン{CurrentTurnNumber} - 行動者ID: {CurrentActorId}");
            DebugLog($"行動者データ: {(currentActor != null ? $"{currentActor.characterName}(生存:{currentActor.isAlive})" : "null")}");

            if (currentActor == null || !currentActor.isAlive)
            {
                DebugLog($"行動者が無効または死亡しているためスキップ: {CurrentActorId}");
                AdvanceToNextActor();
                continue;
            }

            DebugLog($"ターン{CurrentTurnNumber}: {currentActor.characterName}の行動開始");
            OnTurnStart?.Invoke(CurrentActorId, CurrentTurnNumber);

            // 修正: 実際のターン処理を実行
            yield return StartCoroutine(ExecuteActorTurn(currentActor));

            // 次の行動者へ移行
            AdvanceToNextActor();

            DebugLog($"===== ループ {loopCount} 終了 =====");
            yield return new WaitForSeconds(turnActionDelay);
        }

        // 戦闘終了判定
        if (dataManager.AreAllPlayersDefeated())
        {
            DebugLog("プレイヤー全滅");
            OnPlayerDefeated?.Invoke();
        }
        else if (dataManager.AreAllEnemiesDefeated())
        {
            DebugLog("敵全滅");
            OnAllEnemiesDefeated?.Invoke();
        }

        IsTurnInProgress = false;
        DebugLog($"★★★ TurnProcessingCoroutine 終了 (ループ数: {loopCount}) ★★★");
    }

    /// <summary>
    /// 修正: 個別ターン実行処理
    /// </summary>
    private IEnumerator ExecuteActorTurn(BattleCharacterData actor)
    {
        DebugLog($"{actor.characterName}のターン実行開始");

        // エラーハンドリングは各メソッド内で個別に行う

        // 行動前処理（CT減算、ターン開始時効果）
        yield return StartCoroutine(PreActionProcessing(actor));

        // 行動決定・実行
        if (actor.CanAct())
        {
            yield return StartCoroutine(ExecuteAutoAction(actor));
        }
        else
        {
            DebugLog($"{actor.characterName}は行動不能");
        }

        // 行動後処理（状態効果のターン数減算）
        yield return StartCoroutine(PostActionProcessing(actor));

        DebugLog($"{actor.characterName}のターン実行完了");
    }

    /// <summary>
    /// 行動前処理（CT減算、ターン開始時効果）
    /// 戦闘画面フロー ステップ6の実装
    /// </summary>
    private IEnumerator PreActionProcessing(BattleCharacterData actor)
    {
        try
        {
            // 全スキルのCT減算
            dataManager.ReduceSkillCooldowns(actor.characterId);

            // ターン開始時の状態効果処理
            if (calculationManager != null)
            {
                var turnStartEffects = calculationManager.CalculateTurnStartEffects(actor);
                foreach (var effectDamage in turnStartEffects)
                {
                    effectDamage.ApplyDamageToTarget(actor);
                    DebugLog($"{actor.characterName}にターン開始効果: {effectDamage.finalDamage}");
                }
            }
        }
        catch (System.Exception e)
        {
            DebugLogError($"行動前処理エラー ({actor.characterName}): {e.Message}");
        }

        yield return new WaitForSeconds(0.1f);
    }

    /// <summary>
    /// オート行動実行
    /// 戦闘画面フロー ステップ7-11の実装
    /// </summary>
    private IEnumerator ExecuteAutoAction(BattleCharacterData actor)
    {
        // ステップ7: 行動優先順位に従う自動行動
        var selectedSkill = SelectSkillByPriority(actor);

        if (selectedSkill != null)
        {
            // スキル使用
            yield return StartCoroutine(ExecuteSkillActionCoroutine(actor, selectedSkill));
        }
        else
        {
            // 通常攻撃
            yield return StartCoroutine(ExecuteNormalAttackCoroutine(actor));
        }
    }

    /// <summary>
    /// 行動後処理（状態効果のターン数減算）
    /// 戦闘画面フロー ステップ16の実装
    /// </summary>
    private IEnumerator PostActionProcessing(BattleCharacterData actor)
    {
        try
        {
            // 状態効果のターン数減算
            dataManager.ProcessTurnStartStatusEffects(actor.characterId);
        }
        catch (System.Exception e)
        {
            DebugLogError($"行動後処理エラー ({actor.characterName}): {e.Message}");
        }

        yield return new WaitForSeconds(0.1f);
    }

    #endregion

    #region スキル選択AI

    /// <summary>
    /// 優先順位に従うスキル選択
    /// 優先順位：設定スキル1（使用可能） > 設定スキル2（使用可能） > 通常攻撃
    /// </summary>
    private BattleSkillData SelectSkillByPriority(BattleCharacterData actor)
    {
        var skills = dataManager.GetCharacterSkills(actor.characterId);
        if (skills == null || skills.Count == 0) return null;

        // 使用可能なスキルを優先順位順でチェック
        foreach (var skill in skills.OrderBy(s => s.skillId)) // スキルIDの昇順で優先順位を判定
        {
            if (skill.CanUse(actor.currentHp, actor.currentMp))
            {
                // スキルの使用可能性をAIで判定
                if (IsSkillWorthUsing(actor, skill))
                {
                    return skill;
                }
            }
        }

        return null; // 使用すべきスキルがない場合は通常攻撃
    }

    /// <summary>
    /// スキル使用価値判定AI
    /// </summary>
    private bool IsSkillWorthUsing(BattleCharacterData actor, BattleSkillData skill)
    {
        // 攻撃スキルの場合
        if (skill.IsAttackSkill())
        {
            var targets = GetValidTargets(actor, skill);
            if (targets.Count == 0) return false;

            // 威力が通常攻撃より高い場合は使用
            return skill.damageMultiplier > 1.0f;
        }

        // 回復スキルの場合
        if (skill.IsHealSkill())
        {
            // 味方のHPが75%以下の場合に使用
            if (actor.isPlayer)
            {
                return actor.GetHpRatio() < 0.75f;
            }
            else
            {
                var allies = dataManager.GetAllCharacters()
                    .Where(c => c.isPlayer == actor.isPlayer && c.isAlive)
                    .ToList();
                return allies.Any(c => c.GetHpRatio() < 0.75f);
            }
        }

        // バフ・デバフスキルの場合
        if (skill.IsBuffSkill() || skill.IsDebuffSkill())
        {
            // 状態効果が付いていない場合に使用
            return !actor.statusEffects.Any(e => e.effectId == skill.statusEffectId && e.IsActive());
        }

        return true; // その他のスキルは基本的に使用
    }

    /// <summary>
    /// 有効なターゲットを取得
    /// </summary>
    private List<BattleCharacterData> GetValidTargets(BattleCharacterData actor, BattleSkillData skill)
    {
        var allCharacters = dataManager.GetAllCharacters();

        return skill.targetType switch
        {
            TargetType.EnemySingle or TargetType.EnemyAll =>
                allCharacters.Where(c => c.isPlayer != actor.isPlayer && c.isAlive).ToList(),
            TargetType.AllySingle or TargetType.AllyAll =>
                allCharacters.Where(c => c.isPlayer == actor.isPlayer && c.isAlive).ToList(),
            TargetType.Self =>
                new List<BattleCharacterData> { actor },
            _ => new List<BattleCharacterData>()
        };
    }

    /// <summary>
    /// 最適なターゲット選択AI
    /// </summary>
    private BattleCharacterData SelectBestTarget(BattleCharacterData actor, BattleSkillData skill)
    {
        var validTargets = GetValidTargets(actor, skill);
        if (validTargets.Count == 0) return null;

        if (skill.IsEnemyTargetSkill())
        {
            if (prioritizeWeakTargets)
            {
                // HP割合が最も低い敵を優先
                return validTargets.OrderBy(t => t.GetHpRatio()).First();
            }
            else
            {
                // ランダム選択
                return validTargets[UnityEngine.Random.Range(0, validTargets.Count)];
            }
        }
        else if (skill.IsAllyTargetSkill())
        {
            if (skill.IsHealSkill())
            {
                // HP割合が最も低い味方を優先
                return validTargets.OrderBy(t => t.GetHpRatio()).First();
            }
            else
            {
                // バフの場合は自分を優先
                return validTargets.Contains(actor) ? actor : validTargets.First();
            }
        }

        return validTargets.First();
    }

    #endregion

    #region 行動実行

    /// <summary>
    /// スキル行動実行
    /// </summary>
    private IEnumerator ExecuteSkillActionCoroutine(BattleCharacterData actor, BattleSkillData skill)
    {
        var targets = new List<BattleCharacterData>();

        // ターゲット選択
        if (skill.targetType == TargetType.EnemyAll || skill.targetType == TargetType.AllyAll)
        {
            // 全体対象
            targets = GetValidTargets(actor, skill);
        }
        else
        {
            // 単体対象
            var target = SelectBestTarget(actor, skill);
            if (target != null)
            {
                targets.Add(target);
            }
        }

        if (targets.Count == 0)
        {
            DebugLogWarning($"{actor.characterName}のスキル{skill.skillName}に有効なターゲットがありません");
            yield break;
        }

        var targetNames = targets.Select(t => t.characterName).ToList();

        // ActionData作成
        var action = ActionData.CreateSkillUse(
            actor.characterId,
            actor.characterName,
            actor.isPlayer,
            skill,
            targets.Select(t => t.characterId).ToList(),
            targetNames,
            CurrentTurnNumber
        );

        // ステップ9-11: ダメージ判定・計算・適用
        foreach (var target in targets)
        {
            DamageData damageData;

            if (skill.IsHealSkill())
            {
                damageData = calculationManager.CalculateHealAmount(actor, target, skill);
            }
            else
            {
                damageData = calculationManager.CalculateAttackDamage(actor, target, skill);
            }

            damageData.ApplyDamageToTarget(target);
            action.AddDamageResult(damageData);

            // 状態効果の付与判定（修正版：TODO解決）
            if (skill.HasStatusEffect())
            {
                bool statusEffectApplied;

                if (skill.targetType == TargetType.Self)
                {
                    // 自分対象は100%発動
                    statusEffectApplied = dataManager.ProcessSelfStatusEffect(actor, skill);
                }
                else
                {
                    // その他は確率判定を含めて処理
                    statusEffectApplied = dataManager.ProcessSkillStatusEffect(actor, target, skill);
                }

                if (statusEffectApplied)
                {
                    DebugLog($"{actor.characterName}が{target.characterName}に状態効果を適用成功: {skill.skillName}");
                }
                else
                {
                    DebugLog($"{actor.characterName}の{target.characterName}への状態効果適用失敗: {skill.skillName}");
                }
            }
        }

        action.actionSucceeded = true;
        action.resultMessage = $"{skill.skillName}を使用";

        // 統計更新
        dataManager.UpdateCharacterSkillUsage(actor.characterId);

        // ステップ12: 使用スキルのCT設定
        dataManager.UseSkill(actor.characterId, skill.skillId);
        DebugLog($"{skill.skillName}のCTをリセット: {skill.maxCoolTime}ターン");

        // イベント通知
        OnActionExecuted?.Invoke(action);
        dataManager.AddBattleLog(action);

        DebugLog($"{actor.characterName}が{skill.skillName}を使用: {action.GetActionSummary()}");

        yield return new WaitForSeconds(skillAnimationDelay);
    }

    /// <summary>
    /// 通常攻撃実行
    /// </summary>
    private IEnumerator ExecuteNormalAttackCoroutine(BattleCharacterData actor)
    {
        // 攻撃対象を選択（敵キャラクター）
        var enemies = dataManager.GetAllCharacters()
            .Where(c => c.isPlayer != actor.isPlayer && c.isAlive)
            .ToList();

        if (enemies.Count == 0)
        {
            DebugLogWarning($"{actor.characterName}の攻撃対象が存在しません");
            yield break;
        }

        var target = enemies[UnityEngine.Random.Range(0, enemies.Count)];

        // ActionData作成
        var action = ActionData.CreateNormalAttack(
            actor.characterId,
            actor.characterName,
            actor.isPlayer,
            target.characterId,
            target.characterName,
            CurrentTurnNumber
        );

        // ステップ9-11: ダメージ判定・計算・適用
        var damageData = calculationManager.CalculateAttackDamage(actor, target);
        damageData.ApplyDamageToTarget(target);
        action.AddDamageResult(damageData);

        action.actionSucceeded = true;
        action.resultMessage = "通常攻撃";

        // 統計更新
        dataManager.UpdateCharacterDamageStats(actor.characterId, damageData.finalDamage, 0);

        // イベント通知
        OnActionExecuted?.Invoke(action);
        dataManager.AddBattleLog(action);

        DebugLog($"{actor.characterName}が通常攻撃: {action.GetActionSummary()}");

        yield return new WaitForSeconds(turnActionDelay);
    }

    #endregion

    #region ヘルパーメソッド

    /// <summary>
    /// 制限ターン数チェック
    /// 戦闘画面フロー ステップ5の実装
    /// </summary>
    private bool CheckTurnLimit()
    {
        if (turnLimit > 0 && CurrentTurnNumber > turnLimit)
        {
            DebugLog($"制限ターン数{turnLimit}に到達しました");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 現在の行動者IDを取得
    /// </summary>
    private string GetCurrentActorId()
    {
        if (turnOrder == null || turnOrder.Count == 0) return "";
        return turnOrder[currentTurnIndex];
    }

    /// <summary>
    /// 次の行動者に進む
    /// 戦闘画面フロー ステップ17の実装
    /// </summary>
    private void AdvanceToNextActor()
    {
        if (turnOrder == null || turnOrder.Count == 0) return;

        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;

        // 1周した場合はターン数を増加
        if (currentTurnIndex == 0)
        {
            CurrentTurnNumber++;
            OnTurnComplete?.Invoke(CurrentTurnNumber);
            OnTurnCompleted?.Invoke(CurrentTurnNumber); // BattleManager互換用
            DebugLog($"ターン{CurrentTurnNumber}に移行");
        }
    }

    #endregion

    #region デバッグ

    /// <summary>
    /// 修正: デバッグログメソッド（強制出力版）
    /// </summary>
    private void DebugLog(string message)
    {
        // 設定に関係なく常に出力
        Debug.Log($"[BattleTurnManager] {message}");
    }

    private void DebugLogWarning(string message)
    {
        Debug.LogWarning($"[BattleTurnManager] {message}");
    }

    private void DebugLogError(string message)
    {
        Debug.LogError($"[BattleTurnManager] {message}");
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("現在のターン情報を表示")]
    private void ShowCurrentTurnInfo()
    {
        if (turnOrder == null || dataManager == null)
        {
            Debug.Log("ターン情報が初期化されていません");
            return;
        }

        Debug.Log($"=== ターン情報 ===");
        Debug.Log($"現在ターン: {CurrentTurnNumber}");
        Debug.Log($"現在の行動者: {CurrentActorId}");
        Debug.Log($"行動順序: {string.Join(" → ", turnOrder)}");
        Debug.Log($"ターン処理中: {IsTurnInProgress}");
        Debug.Log($"Manager初期化完了: {isManagerInitialized}");
    }

    [ContextMenu("ターン処理を強制開始")]
    private void ForceStartTurnProcessing()
    {
        Debug.Log("=== ターン処理強制開始 ===");
        if (!isManagerInitialized)
        {
            Debug.LogWarning("Manager初期化が未完了です");
        }
        StartTurnProcessing();
    }

    [ContextMenu("ターン処理を強制停止")]
    private void ForceStopTurnProcessing()
    {
        Debug.Log("=== ターン処理強制停止 ===");
        StopTurnProcessing();
    }

    [ContextMenu("現在の状態をデバッグ出力")]
    private void DebugCurrentState()
    {
        Debug.Log("=== BattleTurnManager現在の状態 ===");
        Debug.Log($"Instance存在: {(Instance != null)}");
        Debug.Log($"GameObject.activeInHierarchy: {gameObject.activeInHierarchy}");
        Debug.Log($"this.enabled: {this.enabled}");
        Debug.Log($"isManagerInitialized: {isManagerInitialized}");
        Debug.Log($"IsTurnInProgress: {IsTurnInProgress}");
        Debug.Log($"CurrentTurnNumber: {CurrentTurnNumber}");
        Debug.Log($"CurrentActorId: {CurrentActorId}");
        Debug.Log($"dataManager存在: {(dataManager != null)}");
        Debug.Log($"calculationManager存在: {(calculationManager != null)}");
        Debug.Log($"turnOrder存在: {(turnOrder != null)}");
        if (turnOrder != null)
        {
            Debug.Log($"turnOrder要素数: {turnOrder.Count}");
            Debug.Log($"turnOrder内容: [{string.Join(", ", turnOrder)}]");
        }
        Debug.Log($"turnCoroutine実行中: {(turnCoroutine != null)}");
    }
#endif

    #endregion
}