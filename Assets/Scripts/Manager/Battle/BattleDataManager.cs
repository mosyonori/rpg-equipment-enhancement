using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 戦闘中のデータ状態管理
/// 責任範囲：
/// - 戦闘キャラクター状態管理
/// - バフ・デバフ状態管理
/// - 戦闘ログ管理
/// - 結果・報酬データ処理
/// データアクセス統一ルール: BattleManager → BattleDataManager → Data層
/// </summary>
public class BattleDataManager : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private int maxBattleLogEntries = 100;
    [SerializeField] private bool autoCleanupDefeatedCharacters = true;

    // イベント
    public static event Action<BattleCharacterData> OnCharacterDataUpdated;
    public static event Action<BattleCharacterData, StatusEffectData> OnStatusEffectApplied;
    public static event Action<BattleCharacterData, StatusEffectData> OnStatusEffectRemoved;
    public static event Action<BattleCharacterData> OnCharacterDefeated;
    public static event Action<ActionData> OnBattleLogAdded;

    // プロパティ
    public bool IsInitialized { get; private set; }
    public int AliveCharacterCount => battleCharacters?.Count(c => c.isAlive) ?? 0;
    public int AliveEnemyCount => battleCharacters?.Count(c => !c.isPlayer && c.isAlive) ?? 0;
    public int AlivePlayerCount => battleCharacters?.Count(c => c.isPlayer && c.isAlive) ?? 0;

    // 内部データ
    private List<BattleCharacterData> battleCharacters;
    private List<ActionData> battleLog;
    private BattleSetupData currentBattleSetup;
    private Dictionary<string, List<StatusEffectData>> characterStatusEffects;
    private Dictionary<string, Dictionary<int, BattleSkillData>> characterSkills;

    #region 初期化

    /// <summary>
    /// 戦闘データ初期化
    /// </summary>
    /// <param name="characters">戦闘参加キャラクター</param>
    /// <param name="battleSetup">戦闘設定データ</param>
    public void InitializeBattle(List<BattleCharacterData> characters, BattleSetupData battleSetup)
    {
        try
        {
            Log("BattleDataManager初期化開始");

            // データ初期化
            battleCharacters = new List<BattleCharacterData>(characters);
            battleLog = new List<ActionData>();
            currentBattleSetup = battleSetup;
            characterStatusEffects = new Dictionary<string, List<StatusEffectData>>();
            characterSkills = new Dictionary<string, Dictionary<int, BattleSkillData>>();

            // キャラクター別データ初期化
            foreach (var character in battleCharacters)
            {
                InitializeCharacterData(character);
            }

            IsInitialized = true;
            Log($"BattleDataManager初期化完了: キャラクター{battleCharacters.Count}体");
        }
        catch (Exception e)
        {
            LogError($"BattleDataManager初期化エラー: {e.Message}");
            IsInitialized = false;
        }
    }

    /// <summary>
    /// キャラクター個別データ初期化
    /// </summary>
    private void InitializeCharacterData(BattleCharacterData character)
    {
        // 状態効果リスト初期化
        characterStatusEffects[character.characterId] = new List<StatusEffectData>();

        // スキルデータ初期化
        characterSkills[character.characterId] = new Dictionary<int, BattleSkillData>();

        // 既存スキルを戦闘用スキルデータに変換
        foreach (var skillData in character.availableSkills)
        {
            characterSkills[character.characterId][skillData.skillId] = skillData;
        }

        // プレイヤーの場合、装備スキルも追加
        if (character.isPlayer)
        {
            AddPlayerEquippedSkills(character);
        }

        Log($"キャラクターデータ初期化: {character.characterName} (スキル{character.availableSkills.Count}個)");
    }

    /// <summary>
    /// プレイヤーの装備スキル追加
    /// </summary>
    private void AddPlayerEquippedSkills(BattleCharacterData character)
    {
        if (currentBattleSetup?.playerSkillIds == null) return;

        foreach (var skillId in currentBattleSetup.playerSkillIds)
        {
            var userSkill = SaveDataManager.Instance.CurrentSaveData?.GetSkill(skillId);
            if (userSkill != null)
            {
                var skillMaster = MasterDataManager.Instance.GetSkillData(userSkill.skillMasterId);
                if (skillMaster != null)
                {
                    var battleSkill = BattleSkillData.CreateFromSkillMaster(skillMaster);
                    character.availableSkills.Add(battleSkill);
                    characterSkills[character.characterId][battleSkill.skillId] = battleSkill;
                    Log($"プレイヤースキル追加: {battleSkill.skillName}");
                }
            }
        }
    }

    #endregion

    #region 公開メソッド - キャラクター管理

    /// <summary>
    /// 全キャラクターデータを取得
    /// </summary>
    public List<BattleCharacterData> GetAllCharacters()
    {
        return new List<BattleCharacterData>(battleCharacters ?? new List<BattleCharacterData>());
    }

    /// <summary>
    /// 指定IDのキャラクターを取得
    /// </summary>
    public BattleCharacterData GetCharacter(string characterId)
    {
        return battleCharacters?.Find(c => c.characterId == characterId);
    }

    /// <summary>
    /// 生存キャラクター一覧を取得
    /// </summary>
    public List<BattleCharacterData> GetAliveCharacters()
    {
        return battleCharacters?.FindAll(c => c.isAlive) ?? new List<BattleCharacterData>();
    }

    /// <summary>
    /// 生存している敵キャラクター一覧を取得
    /// </summary>
    public List<BattleCharacterData> GetAliveEnemies()
    {
        return battleCharacters?.FindAll(c => !c.isPlayer && c.isAlive) ?? new List<BattleCharacterData>();
    }

    /// <summary>
    /// 生存しているプレイヤーキャラクター一覧を取得
    /// </summary>
    public List<BattleCharacterData> GetAlivePlayers()
    {
        return battleCharacters?.FindAll(c => c.isPlayer && c.isAlive) ?? new List<BattleCharacterData>();
    }

    /// <summary>
    /// キャラクターのHPを更新
    /// </summary>
    public void UpdateCharacterHP(string characterId, int newHp)
    {
        var character = GetCharacter(characterId);
        if (character != null)
        {
            int oldHp = character.currentHp;
            character.currentHp = Mathf.Clamp(newHp, 0, character.maxHp);

            // 死亡判定
            if (character.currentHp <= 0 && character.isAlive)
            {
                character.isAlive = false;
                OnCharacterDefeated?.Invoke(character);
                Log($"{character.characterName}が撃破されました");
            }

            OnCharacterDataUpdated?.Invoke(character);
            Log($"{character.characterName}のHP更新: {oldHp} → {character.currentHp}");
        }
    }

    /// <summary>
    /// キャラクターのダメージ統計を更新
    /// </summary>
    public void UpdateCharacterDamageStats(string characterId, int damageDealt, int damageReceived)
    {
        var character = GetCharacter(characterId);
        if (character != null)
        {
            character.damageDealt += damageDealt;
            character.damageReceived += damageReceived;
            OnCharacterDataUpdated?.Invoke(character);
        }
    }

    /// <summary>
    /// キャラクターのスキル使用回数を更新
    /// </summary>
    public void UpdateCharacterSkillUsage(string characterId)
    {
        var character = GetCharacter(characterId);
        if (character != null)
        {
            character.skillsUsed++;
            OnCharacterDataUpdated?.Invoke(character);
        }
    }

    #endregion

    #region 公開メソッド - スキル管理

    /// <summary>
    /// キャラクターの使用可能スキル一覧を取得
    /// </summary>
    public List<BattleSkillData> GetCharacterSkills(string characterId)
    {
        if (characterSkills.ContainsKey(characterId))
        {
            return characterSkills[characterId].Values.ToList();
        }
        return new List<BattleSkillData>();
    }

    /// <summary>
    /// 指定スキルを取得
    /// </summary>
    public BattleSkillData GetCharacterSkill(string characterId, int skillId)
    {
        if (characterSkills.ContainsKey(characterId) &&
            characterSkills[characterId].ContainsKey(skillId))
        {
            return characterSkills[characterId][skillId];
        }
        return null;
    }

    /// <summary>
    /// 使用可能なスキル一覧を取得
    /// </summary>
    public List<BattleSkillData> GetUsableSkills(string characterId)
    {
        var character = GetCharacter(characterId);
        if (character == null) return new List<BattleSkillData>();

        var skills = GetCharacterSkills(characterId);
        return skills.FindAll(s => s.CanUse(character.currentHp, character.currentMp));
    }

    /// <summary>
    /// スキルのCTを減算
    /// </summary>
    public void ReduceSkillCooldowns(string characterId)
    {
        if (!characterSkills.ContainsKey(characterId)) return;

        foreach (var skill in characterSkills[characterId].Values)
        {
            skill.ReduceCoolTime();
        }

        Log($"{GetCharacter(characterId)?.characterName}のスキルCT減算");
    }

    /// <summary>
    /// スキル使用後のCTリセット
    /// </summary>
    public void UseSkill(string characterId, int skillId)
    {
        var skill = GetCharacterSkill(characterId, skillId);
        if (skill != null)
        {
            skill.ResetCoolTime();
            UpdateCharacterSkillUsage(characterId);
            Log($"スキル使用: {skill.skillName} (CT={skill.maxCoolTime}にリセット)");
        }
    }

    #endregion

    #region 公開メソッド - 状態効果管理

    /// <summary>
    /// キャラクターの状態効果一覧を取得
    /// </summary>
    public List<StatusEffectData> GetCharacterStatusEffects(string characterId)
    {
        if (characterStatusEffects.ContainsKey(characterId))
        {
            return new List<StatusEffectData>(characterStatusEffects[characterId]);
        }
        return new List<StatusEffectData>();
    }

    /// <summary>
    /// 状態効果を適用
    /// </summary>
    public void ApplyStatusEffect(string characterId, StatusEffectData statusEffect)
    {
        if (!characterStatusEffects.ContainsKey(characterId))
        {
            characterStatusEffects[characterId] = new List<StatusEffectData>();
        }

        var effects = characterStatusEffects[characterId];

        // 既存の同種効果をチェック
        var existingEffect = effects.Find(e => e.effectId == statusEffect.effectId);
        if (existingEffect != null)
        {
            if (statusEffect.CanStackWith(existingEffect))
            {
                // 重複可能な場合はスタック
                existingEffect.StackWith(statusEffect);
                Log($"状態効果スタック: {statusEffect.effectName} (残り{existingEffect.remainingTurns}ターン)");
            }
            else
            {
                // 重複不可能な場合は置き換え
                effects.Remove(existingEffect);
                effects.Add(statusEffect);
                Log($"状態効果置き換え: {statusEffect.effectName}");
            }
        }
        else
        {
            // 新規追加
            effects.Add(statusEffect);
            Log($"状態効果追加: {statusEffect.effectName} (残り{statusEffect.remainingTurns}ターン)");
        }

        OnStatusEffectApplied?.Invoke(GetCharacter(characterId), statusEffect);
        OnCharacterDataUpdated?.Invoke(GetCharacter(characterId));
    }

    /// <summary>
    /// 状態効果を除去
    /// </summary>
    public void RemoveStatusEffect(string characterId, int effectId)
    {
        if (!characterStatusEffects.ContainsKey(characterId)) return;

        var effects = characterStatusEffects[characterId];
        var effect = effects.Find(e => e.effectId == effectId);
        if (effect != null)
        {
            effects.Remove(effect);
            OnStatusEffectRemoved?.Invoke(GetCharacter(characterId), effect);
            OnCharacterDataUpdated?.Invoke(GetCharacter(characterId));
            Log($"状態効果除去: {effect.effectName}");
        }
    }

    /// <summary>
    /// ターン開始時の状態効果処理
    /// </summary>
    public void ProcessTurnStartStatusEffects(string characterId)
    {
        if (!characterStatusEffects.ContainsKey(characterId)) return;

        var character = GetCharacter(characterId);
        if (character == null) return;

        var effects = characterStatusEffects[characterId];
        var effectsToRemove = new List<StatusEffectData>();

        foreach (var effect in effects)
        {
            // ターン開始時ダメージ・回復処理
            if (effect.HasTurnStartEffect())
            {
                int hpChange = effect.CalculateTurnStartHpChange(character.maxHp);
                if (hpChange != 0)
                {
                    UpdateCharacterHP(characterId, character.currentHp + hpChange);
                    string effectType = hpChange > 0 ? "回復" : "ダメージ";
                    Log($"{character.characterName}: {effect.effectName}により{Mathf.Abs(hpChange)}{effectType}");
                }
            }

            // ターン数減算
            effect.ProcessTurn();

            // 効果終了チェック
            if (!effect.IsActive())
            {
                effectsToRemove.Add(effect);
            }
        }

        // 終了した効果を除去
        foreach (var effect in effectsToRemove)
        {
            effects.Remove(effect);
            OnStatusEffectRemoved?.Invoke(character, effect);
            Log($"状態効果終了: {effect.effectName}");
        }

        if (effectsToRemove.Count > 0)
        {
            OnCharacterDataUpdated?.Invoke(character);
        }
    }

    /// <summary>
    /// キャラクターの行動阻害状態をチェック
    /// </summary>
    public bool IsCharacterActionBlocked(string characterId)
    {
        var effects = GetCharacterStatusEffects(characterId);
        return effects.Any(e => e.preventAction && e.IsActive());
    }

    /// <summary>
    /// 状態効果を適用したステータスを取得
    /// </summary>
    public void ApplyStatusEffectsToStats(string characterId, ref int offense, ref int defense,
        ref int fireOffense, ref int waterOffense, ref int windOffense, ref int earthOffense)
    {
        var effects = GetCharacterStatusEffects(characterId);
        foreach (var effect in effects.Where(e => e.IsActive()))
        {
            effect.ApplyToStats(ref offense, ref defense, ref fireOffense, ref waterOffense, ref windOffense, ref earthOffense);
        }
    }

    #endregion

    /// <summary>
    /// スキル使用時の状態効果発動・適用の統合処理
    /// BattleTurnManagerから呼び出される
    /// </summary>
    public bool ProcessSkillStatusEffect(BattleCharacterData caster, BattleCharacterData target, BattleSkillData skill)
    {
        if (caster == null || target == null || skill == null || !skill.HasStatusEffect())
        {
            return false;
        }

        try
        {
            // 1. BattleCalculationManagerで発動判定
            if (BattleCalculationManager.Instance == null)
            {
                LogError("BattleCalculationManagerが見つかりません");
                return false;
            }

            bool effectTriggered = BattleCalculationManager.Instance.CalculateStatusEffectChance(skill, target);
            if (!effectTriggered)
            {
                Log($"{skill.skillName}の状態効果発動判定: 失敗");
                return false;
            }

            // 2. 状態効果データの作成
            var statusEffect = CreateStatusEffectFromSkill(skill, caster.characterId);
            if (statusEffect == null)
            {
                LogError($"状態効果の作成に失敗: {skill.skillName}");
                return false;
            }

            // 3. 既存のApplyStatusEffect()メソッドを使用して適用
            ApplyStatusEffect(target.characterId, statusEffect);

            Log($"{caster.characterName}が{target.characterName}に{statusEffect.effectName}を付与");
            return true;
        }
        catch (System.Exception e)
        {
            LogError($"状態効果処理中にエラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 自分自身への状態効果適用（100%発動）
    /// </summary>
    public bool ProcessSelfStatusEffect(BattleCharacterData caster, BattleSkillData skill)
    {
        if (caster == null || skill == null || !skill.HasStatusEffect())
        {
            return false;
        }

        try
        {
            var statusEffect = CreateStatusEffectFromSkill(skill, caster.characterId);
            if (statusEffect == null)
            {
                LogError($"自分自身への状態効果作成に失敗: {skill.skillName}");
                return false;
            }

            ApplyStatusEffect(caster.characterId, statusEffect);
            Log($"{caster.characterName}が自分自身に{statusEffect.effectName}を付与");
            return true;
        }
        catch (System.Exception e)
        {
            LogError($"自分自身への状態効果処理中にエラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// スキル情報から状態効果データを作成
    /// </summary>
    private StatusEffectData CreateStatusEffectFromSkill(BattleSkillData skill, string casterId)
    {
        if (skill == null || !skill.HasStatusEffect())
        {
            return null;
        }

        try
        {
            var effectMaster = MasterDataManager.Instance?.GetSkillEffectData(skill.statusEffectId);
            if (effectMaster == null)
            {
                LogError($"スキル効果マスターデータが見つかりません: ID={skill.statusEffectId}");
                return null;
            }

            // StatusEffectData.CreateFromSkillEffectMasterの正しいシグネチャに合わせて呼び出し
            var statusEffect = StatusEffectData.CreateFromSkillEffectMaster(
                effectMaster,
                casterId,
                casterId,  // targetIdとしてもcasterIdを使用（自分に付与する場合）
                skill.skillId
            );

            if (statusEffect == null)
            {
                LogError($"StatusEffectDataの作成に失敗: SkillEffectID={skill.statusEffectId}");
                return null;
            }

            return statusEffect;
        }
        catch (System.Exception e)
        {
            LogError($"状態効果作成中にエラー: {e.Message}");
            return null;
        }
    }




    #region 公開メソッド - 戦闘ログ管理

    /// <summary>
    /// 戦闘ログを追加
    /// </summary>
    public void AddBattleLog(ActionData actionData)
    {
        if (battleLog.Count >= maxBattleLogEntries)
        {
            // 古いログを削除
            battleLog.RemoveAt(0);
        }

        battleLog.Add(actionData);
        OnBattleLogAdded?.Invoke(actionData);
        Log($"戦闘ログ追加: {actionData.GetActionSummary()}");
    }

    /// <summary>
    /// 戦闘ログ一覧を取得
    /// </summary>
    public List<ActionData> GetBattleLog()
    {
        return new List<ActionData>(battleLog);
    }

    /// <summary>
    /// 最新の戦闘ログを取得
    /// </summary>
    public ActionData GetLatestBattleLog()
    {
        return battleLog.LastOrDefault();
    }

    /// <summary>
    /// 指定ターンの戦闘ログを取得
    /// </summary>
    public List<ActionData> GetBattleLogByTurn(int turnNumber)
    {
        return battleLog.FindAll(log => log.turnNumber == turnNumber);
    }

    #endregion

    #region 公開メソッド - 戦闘統計

    /// <summary>
    /// 全滅チェック（敵）
    /// </summary>
    public bool AreAllEnemiesDefeated()
    {
        return GetAliveEnemies().Count == 0;
    }

    /// <summary>
    /// 全滅チェック（プレイヤー）
    /// </summary>
    public bool AreAllPlayersDefeated()
    {
        return GetAlivePlayers().Count == 0;
    }

    /// <summary>
    /// 戦闘統計データを取得
    /// </summary>
    public BattleStatistics GetBattleStatistics()
    {
        var stats = new BattleStatistics();

        foreach (var character in battleCharacters)
        {
            if (character.isPlayer)
            {
                stats.totalPlayerDamageDealt += character.damageDealt;
                stats.totalPlayerDamageReceived += character.damageReceived;
                stats.totalPlayerSkillsUsed += character.skillsUsed;
            }
            else
            {
                stats.totalEnemyDamageDealt += character.damageDealt;
                stats.totalEnemyDamageReceived += character.damageReceived;
                stats.totalEnemySkillsUsed += character.skillsUsed;
            }
        }

        stats.totalActions = battleLog.Count;
        stats.totalCriticalHits = battleLog.Sum(log => log.GetCriticalCount());
        stats.totalDefeats = battleCharacters.Count(c => !c.isAlive);

        return stats;
    }

    #endregion

    #region 内部メソッド

    /// <summary>
    /// 撃破されたキャラクターのクリーンアップ
    /// </summary>
    private void CleanupDefeatedCharacters()
    {
        if (!autoCleanupDefeatedCharacters) return;

        var defeatedCharacters = battleCharacters.FindAll(c => !c.isAlive);
        foreach (var character in defeatedCharacters)
        {
            // 状態効果をクリア
            if (characterStatusEffects.ContainsKey(character.characterId))
            {
                characterStatusEffects[character.characterId].Clear();
            }
        }
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[BattleDataManager] {message}");
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[BattleDataManager] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("戦闘データ状況をログ出力")]
    private void LogBattleDataState()
    {
        Log($"=== 戦闘データ状況 ===");
        Log($"キャラクター総数: {battleCharacters?.Count ?? 0}");
        Log($"生存キャラクター数: {AliveCharacterCount}");
        Log($"生存敵数: {AliveEnemyCount}");
        Log($"生存プレイヤー数: {AlivePlayerCount}");
        Log($"戦闘ログ数: {battleLog?.Count ?? 0}");

        if (battleCharacters != null)
        {
            foreach (var character in battleCharacters)
            {
                var statusCount = GetCharacterStatusEffects(character.characterId).Count;
                var skillCount = GetCharacterSkills(character.characterId).Count;
                Log($"  {character.characterName}: HP{character.currentHp}/{character.maxHp}, " +
                    $"状態効果{statusCount}個, スキル{skillCount}個, " +
                    $"与ダメージ{character.damageDealt}, 被ダメージ{character.damageReceived}");
            }
        }
    }
#endif

    #endregion
}

/// <summary>
/// 戦闘統計データ
/// </summary>
[System.Serializable]
public class BattleStatistics
{
    public int totalActions;                    // 総行動数
    public int totalCriticalHits;               // 総クリティカル数
    public int totalDefeats;                    // 総撃破数
    public int totalPlayerDamageDealt;          // プレイヤー総与ダメージ
    public int totalPlayerDamageReceived;       // プレイヤー総被ダメージ
    public int totalPlayerSkillsUsed;           // プレイヤー総スキル使用数
    public int totalEnemyDamageDealt;           // 敵総与ダメージ
    public int totalEnemyDamageReceived;        // 敵総被ダメージ
    public int totalEnemySkillsUsed;            // 敵総スキル使用数

    public override string ToString()
    {
        return $"戦闘統計: 行動{totalActions}回, クリティカル{totalCriticalHits}回, " +
               $"P与ダメージ{totalPlayerDamageDealt}, P被ダメージ{totalPlayerDamageReceived}";
    }
}