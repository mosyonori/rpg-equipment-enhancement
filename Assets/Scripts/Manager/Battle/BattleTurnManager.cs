using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

/// <summary>
/// ターン・行動順制御管理クラス
/// ターン制バトルの進行制御とオートバトルAIを担当
/// </summary>
public class BattleTurnManager : MonoBehaviour
{
    [Header("ターン制御設定")]
    [SerializeField] private float turnActionDelay = 1.0f;
    [SerializeField] private float skillAnimationDelay = 0.5f;
    [SerializeField] private bool enableAIDebugLog = false;

    [Header("オートバトル設定")]
    [SerializeField] private float autoActionInterval = 2.0f;
    [SerializeField] private bool prioritizeWeakTargets = true;
    [SerializeField] private bool avoidOverkill = true;

    // シングルトンパターン
    public static BattleTurnManager Instance { get; private set; }

    // 戦闘状態
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

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 依存関係は実行時に取得（BattleDataManagerはシングルトンではない）
        calculationManager = BattleCalculationManager.Instance;

        if (calculationManager == null)
        {
            DebugLogError("BattleCalculationManagerが見つかりません");
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
        DebugLog($"戦闘速度設定: {speedMultiplier}倍速");
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
        else if (!IsTurnInProgress)
        {
            StartTurnProcessing();
        }
        DebugLog($"一時停止設定: {paused}");
    }

    /// <summary>
    /// 戦闘初期化（BattleManager互換用）
    /// </summary>
    public void InitializeBattle(List<BattleCharacterData> characters, int battleTurnLimit)
    {
        SetTurnLimit(battleTurnLimit);
        InitializeTurnOrder();
        DebugLog($"戦闘初期化完了: キャラクター{characters.Count}体, ターン制限{battleTurnLimit}");
    }

    #endregion

    #region 公開メソッド - ターン管理

    /// <summary>
    /// 戦闘開始時の行動順序を決定
    /// 戦闘画面フロー ステップ4の実装
    /// </summary>
    public void InitializeTurnOrder()
    {
        if (dataManager == null)
        {
            DebugLogError("BattleDataManagerが初期化されていません");
            return;
        }

        var allCharacters = dataManager.GetAllCharacters();
        if (allCharacters == null || allCharacters.Count == 0)
        {
            DebugLogError("戦闘キャラクターが存在しません");
            return;
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

        DebugLog($"行動順序を決定: {string.Join(" → ", turnOrder)}");
    }

    /// <summary>
    /// ターン処理を開始
    /// </summary>
    public void StartTurnProcessing()
    {
        if (IsTurnInProgress)
        {
            DebugLogWarning("既にターン処理中です");
            return;
        }

        if (turnCoroutine != null)
        {
            StopCoroutine(turnCoroutine);
        }

        turnCoroutine = StartCoroutine(TurnProcessingCoroutine());
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
        }

        IsTurnInProgress = false;
        CurrentActorId = "";
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
        // 優先順位に基づくスキル選択
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
    /// メインターン処理コルーチン
    /// 戦闘画面フローの ステップ5-17を実装
    /// </summary>
    private IEnumerator TurnProcessingCoroutine()
    {
        IsTurnInProgress = true;

        while (!dataManager.AreAllPlayersDefeated() && !dataManager.AreAllEnemiesDefeated())
        {
            // ステップ5: 限界ターン数チェック
            if (CheckTurnLimit())
            {
                OnTurnLimitReached?.Invoke();
                yield break;
            }

            // 現在の行動者を取得
            CurrentActorId = GetCurrentActorId();
            var currentActor = dataManager.GetCharacter(CurrentActorId);

            if (currentActor == null || !currentActor.isAlive)
            {
                // 死亡キャラクターをスキップして次へ
                AdvanceToNextActor();
                continue;
            }

            DebugLog($"ターン{CurrentTurnNumber}: {currentActor.characterName}の行動開始");
            OnTurnStart?.Invoke(CurrentActorId, CurrentTurnNumber);

            // ステップ6: 行動前処理
            yield return StartCoroutine(PreActionProcessing(currentActor));

            // 行動不能チェック（スタン等）
            if (!currentActor.CanAct() || dataManager.IsCharacterActionBlocked(currentActor.characterId))
            {
                if (dataManager.IsCharacterActionBlocked(currentActor.characterId))
                {
                    DebugLog($"{currentActor.characterName}は状態効果により行動不能です");
                }
                else
                {
                    DebugLog($"{currentActor.characterName}は行動不能です");
                }

                yield return StartCoroutine(PostActionProcessing(currentActor));
                AdvanceToNextActor();
                continue;
            }

            // ステップ7-8: 自動行動実行
            yield return StartCoroutine(ExecuteAutoAction(currentActor));

            // ステップ16: ターン終了時の状態異常処理
            yield return StartCoroutine(PostActionProcessing(currentActor));

            // ステップ13: 勝敗判定
            if (dataManager.AreAllPlayersDefeated())
            {
                OnPlayerDefeated?.Invoke();
                break;
            }
            else if (dataManager.AreAllEnemiesDefeated())
            {
                OnAllEnemiesDefeated?.Invoke();
                break;
            }

            // 次の行動者へ移行
            AdvanceToNextActor();

            // 行動間の待機時間
            yield return new WaitForSeconds(turnActionDelay);
        }

        IsTurnInProgress = false;
        DebugLog("ターン処理を終了します");
    }

    /// <summary>
    /// 行動前処理（CT減算、ターン開始時効果）
    /// 戦闘画面フロー ステップ6の実装
    /// </summary>
    private IEnumerator PreActionProcessing(BattleCharacterData actor)
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

        yield return new WaitForSeconds(0.1f);
    }

    /// <summary>
    /// オート行動実行
    /// 戦闘画面フロー ステップ7-11の実装
    /// </summary>
    private IEnumerator ExecuteAutoAction(BattleCharacterData actor)
    {
        // ステップ7: 行動優先順位に従い自動行動
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
    /// 行動後処理（状態効果のターン減算）
    /// 戦闘画面フロー ステップ16の実装
    /// </summary>
    private IEnumerator PostActionProcessing(BattleCharacterData actor)
    {
        // 状態効果のターン数減算
        dataManager.ProcessTurnStartStatusEffects(actor.characterId);

        yield return new WaitForSeconds(0.1f);
    }

    #endregion

    #region スキル選択AI

    /// <summary>
    /// 優先順位に基づくスキル選択
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

            // 状態効果の付与判定（修正版：TODOを解決）
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
    /// 限界ターン数チェック
    /// 戦闘画面フロー ステップ5の実装
    /// </summary>
    private bool CheckTurnLimit()
    {
        if (turnLimit > 0 && CurrentTurnNumber > turnLimit)
        {
            DebugLog($"限界ターン数{turnLimit}に到達しました");
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

        // 1巡した場合はターン数を増加
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

    private void DebugLog(string message)
    {
        if (enableAIDebugLog)
        {
            Debug.Log($"[BattleTurnManager] {message}");
        }
    }

    private void DebugLogWarning(string message)
    {
        if (enableAIDebugLog)
        {
            Debug.LogWarning($"[BattleTurnManager] {message}");
        }
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
    }
#endif

    #endregion
}