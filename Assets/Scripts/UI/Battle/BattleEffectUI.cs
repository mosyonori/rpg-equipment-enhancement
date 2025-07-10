using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 戦闘エフェクトの表示制御UI
/// スキル発動エフェクト、属性攻撃エフェクト、バフ・デバフエフェクトを管理
/// データアクセス統一ルール: UI層 → Manager層 → Data層
/// </summary>
public class BattleEffectUI : MonoBehaviour
{
    [Header("エフェクト設定")]
    [SerializeField] private bool enableDebugLog = false;
    [SerializeField] private float defaultEffectDuration = 1.0f;
    [SerializeField] private float effectFadeInTime = 0.3f;
    [SerializeField] private float effectFadeOutTime = 0.5f;

    [Header("スキルエフェクト")]
    [SerializeField] private Transform skillEffectParent;
    [SerializeField] private GameObject normalAttackEffectPrefab;
    [SerializeField] private GameObject healSkillEffectPrefab;
    [SerializeField] private GameObject buffSkillEffectPrefab;
    [SerializeField] private GameObject debuffSkillEffectPrefab;

    [Header("属性攻撃エフェクト")]
    [SerializeField] private Transform attributeEffectParent;
    [SerializeField] private GameObject fireAttackEffectPrefab;
    [SerializeField] private GameObject waterAttackEffectPrefab;
    [SerializeField] private GameObject windAttackEffectPrefab;
    [SerializeField] private GameObject earthAttackEffectPrefab;

    [Header("バフ・デバフエフェクト")]
    [SerializeField] private Transform statusEffectParent;
    [SerializeField] private GameObject buffApplyEffectPrefab;
    [SerializeField] private GameObject debuffApplyEffectPrefab;
    [SerializeField] private GameObject statusRemoveEffectPrefab;

    [Header("特殊エフェクト")]
    [SerializeField] private Transform specialEffectParent;
    [SerializeField] private GameObject criticalHitEffectPrefab;
    [SerializeField] private GameObject superEffectiveEffectPrefab;
    [SerializeField] private GameObject notVeryEffectiveEffectPrefab;
    [SerializeField] private GameObject noEffectEffectPrefab;

    [Header("Audio")]
    [SerializeField] private AudioSource effectAudioSource;
    [SerializeField] private AudioClip normalAttackSound;
    [SerializeField] private AudioClip skillUseSound;
    [SerializeField] private AudioClip criticalHitSound;
    [SerializeField] private AudioClip buffApplySound;
    [SerializeField] private AudioClip debuffApplySound;

    // プライベートフィールド
    private Dictionary<string, Transform> characterPositions;
    private Queue<BattleEffectRequest> effectQueue;
    private bool isProcessingEffect;
    private Coroutine effectProcessCoroutine;
    private BattleDataManager battleDataManager;

    // イベント
    public static event Action<string> OnEffectStarted;
    public static event Action<string> OnEffectCompleted;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponent();
    }

    private void Start()
    {
        RegisterEvents();
    }

    private void OnDestroy()
    {
        UnregisterEvents();
        StopAllEffects();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネント初期化
    /// </summary>
    private void InitializeComponent()
    {
        characterPositions = new Dictionary<string, Transform>();
        effectQueue = new Queue<BattleEffectRequest>();
        isProcessingEffect = false;

        // BattleDataManagerの参照を取得
        battleDataManager = FindFirstObjectByType<BattleDataManager>();

        // エフェクト親オブジェクトの初期化
        if (skillEffectParent == null)
            skillEffectParent = transform;
        if (attributeEffectParent == null)
            attributeEffectParent = transform;
        if (statusEffectParent == null)
            statusEffectParent = transform;
        if (specialEffectParent == null)
            specialEffectParent = transform;

        DebugLog("BattleEffectUI初期化完了");
    }

    /// <summary>
    /// イベント登録
    /// </summary>
    private void RegisterEvents()
    {
        // Manager層からのイベント受信
        if (BattleManager.Instance != null)
        {
            BattleManager.OnActionExecuted += OnActionExecuted;
        }

        if (BattleCalculationManager.Instance != null)
        {
            BattleCalculationManager.OnDamageCalculated += OnDamageCalculated;
            // OnStatusEffectAppliedイベントは存在しないため、BattleDataManagerから監視
        }

        // BattleDataManagerのイベントを監視
        if (battleDataManager != null)
        {
            BattleDataManager.OnStatusEffectApplied += OnStatusEffectAppliedFromDataManager;
        }
    }

    /// <summary>
    /// イベント登録解除
    /// </summary>
    private void UnregisterEvents()
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.OnActionExecuted -= OnActionExecuted;
        }

        if (BattleCalculationManager.Instance != null)
        {
            BattleCalculationManager.OnDamageCalculated -= OnDamageCalculated;
        }

        // BattleDataManagerのイベント登録解除
        if (battleDataManager != null)
        {
            BattleDataManager.OnStatusEffectApplied -= OnStatusEffectAppliedFromDataManager;
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// キャラクター位置を登録
    /// </summary>
    public void RegisterCharacterPosition(string characterId, Transform characterTransform)
    {
        if (string.IsNullOrEmpty(characterId) || characterTransform == null)
        {
            LogError("無効なキャラクター位置登録");
            return;
        }

        characterPositions[characterId] = characterTransform;
        DebugLog($"キャラクター位置登録: {characterId}");
    }

    /// <summary>
    /// キャラクター位置を削除
    /// </summary>
    public void UnregisterCharacterPosition(string characterId)
    {
        if (characterPositions.ContainsKey(characterId))
        {
            characterPositions.Remove(characterId);
            DebugLog($"キャラクター位置削除: {characterId}");
        }
    }

    /// <summary>
    /// スキルエフェクトを再生
    /// </summary>
    public void PlaySkillEffect(BattleSkillData skill, string casterCharacterId, List<string> targetCharacterIds)
    {
        if (skill == null)
        {
            LogError("スキルデータがnullです");
            return;
        }

        var request = new BattleEffectRequest
        {
            effectType = BattleEffectType.Skill,
            skillData = skill,
            casterCharacterId = casterCharacterId,
            targetCharacterIds = targetCharacterIds,
            duration = defaultEffectDuration
        };

        EnqueueEffect(request);
    }

    /// <summary>
    /// 属性攻撃エフェクトを再生
    /// </summary>
    public void PlayAttributeEffect(AttributeType attributeType, string targetCharacterId, DamageEffectiveness effectiveness)
    {
        var request = new BattleEffectRequest
        {
            effectType = BattleEffectType.Attribute,
            attributeType = attributeType,
            targetCharacterIds = new List<string> { targetCharacterId },
            effectiveness = effectiveness,
            duration = defaultEffectDuration
        };

        EnqueueEffect(request);
    }

    /// <summary>
    /// ステータス効果エフェクトを再生
    /// </summary>
    public void PlayStatusEffect(StatusEffectData statusEffect, string targetCharacterId)
    {
        if (statusEffect == null)
        {
            LogError("ステータス効果データがnullです");
            return;
        }

        var request = new BattleEffectRequest
        {
            effectType = BattleEffectType.StatusEffect,
            statusEffectData = statusEffect,
            targetCharacterIds = new List<string> { targetCharacterId },
            duration = defaultEffectDuration * 0.8f
        };

        EnqueueEffect(request);
    }

    /// <summary>
    /// クリティカルヒットエフェクトを再生
    /// </summary>
    public void PlayCriticalEffect(string targetCharacterId)
    {
        var request = new BattleEffectRequest
        {
            effectType = BattleEffectType.Critical,
            targetCharacterIds = new List<string> { targetCharacterId },
            duration = defaultEffectDuration * 1.2f
        };

        EnqueueEffect(request);
    }

    /// <summary>
    /// 全エフェクトを停止
    /// </summary>
    public void StopAllEffects()
    {
        if (effectProcessCoroutine != null)
        {
            StopCoroutine(effectProcessCoroutine);
            effectProcessCoroutine = null;
        }

        effectQueue.Clear();
        isProcessingEffect = false;

        // 再生中のエフェクトを停止
        StopAllChildEffects();
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// 行動実行イベント処理
    /// </summary>
    private void OnActionExecuted(ActionData action)
    {
        if (action == null) return;

        // スキル使用エフェクト
        if (action.IsSkillUse())
        {
            if (battleDataManager != null)
            {
                var skill = battleDataManager.GetCharacterSkill(action.actorId, action.skillId);
                if (skill != null)
                {
                    PlaySkillEffect(skill, action.actorId, action.targetIds);
                }
            }
        }
        else
        {
            // 通常攻撃エフェクト
            PlayNormalAttackEffect(action.actorId, action.targetIds);
        }
    }

    /// <summary>
    /// ダメージ計算イベント処理
    /// </summary>
    private void OnDamageCalculated(DamageData damageData)
    {
        if (damageData == null) return;

        // 属性攻撃エフェクト
        if (damageData.attackAttribute != AttributeType.None)
        {
            PlayAttributeEffect(damageData.attackAttribute, damageData.targetId, damageData.effectiveness);
        }

        // クリティカルエフェクト
        if (damageData.isCritical)
        {
            PlayCriticalEffect(damageData.targetId);
        }

        // 効果度エフェクト
        if (damageData.effectiveness != DamageEffectiveness.Normal)
        {
            PlayEffectivenessEffect(damageData.effectiveness, damageData.targetId);
        }
    }

    /// <summary>
    /// ステータス効果適用イベント処理（BattleDataManager用）
    /// </summary>
    private void OnStatusEffectAppliedFromDataManager(BattleCharacterData character, StatusEffectData statusEffect)
    {
        if (character == null || statusEffect == null) return;

        PlayStatusEffect(statusEffect, character.characterId);
    }

    #endregion

    #region エフェクト処理

    /// <summary>
    /// エフェクトをキューに追加
    /// </summary>
    private void EnqueueEffect(BattleEffectRequest request)
    {
        effectQueue.Enqueue(request);

        if (!isProcessingEffect)
        {
            effectProcessCoroutine = StartCoroutine(ProcessEffectQueue());
        }
    }

    /// <summary>
    /// エフェクトキュー処理
    /// </summary>
    private IEnumerator ProcessEffectQueue()
    {
        isProcessingEffect = true;

        while (effectQueue.Count > 0)
        {
            var request = effectQueue.Dequeue();
            yield return StartCoroutine(ExecuteEffect(request));

            // エフェクト間の間隔
            yield return new WaitForSeconds(0.1f);
        }

        isProcessingEffect = false;
        effectProcessCoroutine = null;
    }

    /// <summary>
    /// エフェクト実行
    /// </summary>
    private IEnumerator ExecuteEffect(BattleEffectRequest request)
    {
        OnEffectStarted?.Invoke(request.effectType.ToString());

        switch (request.effectType)
        {
            case BattleEffectType.Skill:
                yield return StartCoroutine(ExecuteSkillEffect(request));
                break;
            case BattleEffectType.Attribute:
                yield return StartCoroutine(ExecuteAttributeEffect(request));
                break;
            case BattleEffectType.StatusEffect:
                yield return StartCoroutine(ExecuteStatusEffect(request));
                break;
            case BattleEffectType.Critical:
                yield return StartCoroutine(ExecuteCriticalEffect(request));
                break;
            case BattleEffectType.NormalAttack:
                yield return StartCoroutine(ExecuteNormalAttackEffect(request));
                break;
        }

        OnEffectCompleted?.Invoke(request.effectType.ToString());
    }

    /// <summary>
    /// スキルエフェクト実行
    /// </summary>
    private IEnumerator ExecuteSkillEffect(BattleEffectRequest request)
    {
        var prefab = GetSkillEffectPrefab(request.skillData);
        if (prefab == null) yield break;

        var targets = GetTargetPositions(request.targetCharacterIds);
        if (targets.Count == 0) yield break;

        // 音声再生
        PlaySound(skillUseSound);

        foreach (var targetPos in targets)
        {
            var effect = Instantiate(prefab, targetPos, Quaternion.identity, skillEffectParent);

            // エフェクトアニメーション（標準のコルーチンを使用）
            yield return StartCoroutine(ScaleEffect(effect.transform, Vector3.zero, Vector3.one, effectFadeInTime));
            yield return new WaitForSeconds(request.duration - effectFadeInTime - effectFadeOutTime);
            yield return StartCoroutine(ScaleEffect(effect.transform, Vector3.one, Vector3.zero, effectFadeOutTime));

            if (effect != null)
                Destroy(effect);
        }
    }

    /// <summary>
    /// 属性エフェクト実行
    /// </summary>
    private IEnumerator ExecuteAttributeEffect(BattleEffectRequest request)
    {
        var prefab = GetAttributeEffectPrefab(request.attributeType);
        if (prefab == null) yield break;

        var targets = GetTargetPositions(request.targetCharacterIds);
        if (targets.Count == 0) yield break;

        foreach (var targetPos in targets)
        {
            var effect = Instantiate(prefab, targetPos, Quaternion.identity, attributeEffectParent);

            // 属性に応じた色調整
            ApplyAttributeColor(effect, request.attributeType);

            yield return new WaitForSeconds(request.duration);

            if (effect != null)
                Destroy(effect);
        }
    }

    /// <summary>
    /// ステータス効果エフェクト実行
    /// </summary>
    private IEnumerator ExecuteStatusEffect(BattleEffectRequest request)
    {
        var prefab = GetStatusEffectPrefab(request.statusEffectData);
        if (prefab == null) yield break;

        var targets = GetTargetPositions(request.targetCharacterIds);
        if (targets.Count == 0) yield break;

        // 音声再生
        var sound = request.statusEffectData.isPositive ? buffApplySound : debuffApplySound;
        PlaySound(sound);

        foreach (var targetPos in targets)
        {
            var effect = Instantiate(prefab, targetPos, Quaternion.identity, statusEffectParent);

            // ステータス効果の色適用
            ApplyStatusEffectColor(effect, request.statusEffectData);

            yield return new WaitForSeconds(request.duration);

            if (effect != null)
                Destroy(effect);
        }
    }

    /// <summary>
    /// クリティカルエフェクト実行
    /// </summary>
    private IEnumerator ExecuteCriticalEffect(BattleEffectRequest request)
    {
        if (criticalHitEffectPrefab == null) yield break;

        var targets = GetTargetPositions(request.targetCharacterIds);
        if (targets.Count == 0) yield break;

        // クリティカル音声再生
        PlaySound(criticalHitSound);

        foreach (var targetPos in targets)
        {
            var effect = Instantiate(criticalHitEffectPrefab, targetPos, Quaternion.identity, specialEffectParent);

            // クリティカル専用アニメーション（パンチスケール風）
            yield return StartCoroutine(PunchScaleEffect(effect.transform, request.duration));

            if (effect != null)
                Destroy(effect);
        }
    }

    /// <summary>
    /// 通常攻撃エフェクト実行
    /// </summary>
    private IEnumerator ExecuteNormalAttackEffect(BattleEffectRequest request)
    {
        if (normalAttackEffectPrefab == null) yield break;

        var targets = GetTargetPositions(request.targetCharacterIds);
        if (targets.Count == 0) yield break;

        // 通常攻撃音声再生
        PlaySound(normalAttackSound);

        foreach (var targetPos in targets)
        {
            var effect = Instantiate(normalAttackEffectPrefab, targetPos, Quaternion.identity, skillEffectParent);

            yield return new WaitForSeconds(request.duration);

            if (effect != null)
                Destroy(effect);
        }
    }

    #endregion

    #region アニメーションヘルパー

    /// <summary>
    /// スケールアニメーション
    /// </summary>
    private IEnumerator ScaleEffect(Transform target, Vector3 fromScale, Vector3 toScale, float duration)
    {
        if (target == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            target.localScale = Vector3.Lerp(fromScale, toScale, t);
            yield return null;
        }

        target.localScale = toScale;
    }

    /// <summary>
    /// パンチスケールエフェクト（クリティカル用）
    /// </summary>
    private IEnumerator PunchScaleEffect(Transform target, float duration)
    {
        if (target == null) yield break;

        Vector3 originalScale = target.localScale;
        Vector3 punchScale = originalScale * 1.3f;

        // 拡大
        yield return StartCoroutine(ScaleEffect(target, originalScale, punchScale, duration * 0.3f));
        // 縮小
        yield return StartCoroutine(ScaleEffect(target, punchScale, originalScale * 0.9f, duration * 0.3f));
        // 元に戻す
        yield return StartCoroutine(ScaleEffect(target, originalScale * 0.9f, originalScale, duration * 0.4f));
    }

    #endregion

    #region ヘルパーメソッド

    /// <summary>
    /// 通常攻撃エフェクトを再生
    /// </summary>
    private void PlayNormalAttackEffect(string casterCharacterId, List<string> targetCharacterIds)
    {
        var request = new BattleEffectRequest
        {
            effectType = BattleEffectType.NormalAttack,
            casterCharacterId = casterCharacterId,
            targetCharacterIds = targetCharacterIds,
            duration = defaultEffectDuration * 0.8f
        };

        EnqueueEffect(request);
    }

    /// <summary>
    /// 効果度エフェクトを再生
    /// </summary>
    private void PlayEffectivenessEffect(DamageEffectiveness effectiveness, string targetCharacterId)
    {
        GameObject prefab = effectiveness switch
        {
            DamageEffectiveness.SuperEffective => superEffectiveEffectPrefab,
            DamageEffectiveness.NotVeryEffective => notVeryEffectiveEffectPrefab,
            DamageEffectiveness.NoEffect => noEffectEffectPrefab,
            _ => null
        };

        if (prefab == null) return;

        if (characterPositions.TryGetValue(targetCharacterId, out Transform targetPos))
        {
            var effect = Instantiate(prefab, targetPos.position, Quaternion.identity, specialEffectParent);
            Destroy(effect, defaultEffectDuration);
        }
    }

    /// <summary>
    /// スキルエフェクトプレハブを取得
    /// </summary>
    private GameObject GetSkillEffectPrefab(BattleSkillData skill)
    {
        if (skill.IsHealSkill())
            return healSkillEffectPrefab;
        else if (skill.IsBuffSkill())
            return buffSkillEffectPrefab;
        else if (skill.IsDebuffSkill())
            return debuffSkillEffectPrefab;
        else
            return normalAttackEffectPrefab;
    }

    /// <summary>
    /// 属性エフェクトプレハブを取得
    /// </summary>
    private GameObject GetAttributeEffectPrefab(AttributeType attributeType)
    {
        return attributeType switch
        {
            AttributeType.Fire => fireAttackEffectPrefab,
            AttributeType.Water => waterAttackEffectPrefab,
            AttributeType.Wind => windAttackEffectPrefab,
            AttributeType.Earth => earthAttackEffectPrefab,
            _ => null
        };
    }

    /// <summary>
    /// ステータス効果エフェクトプレハブを取得
    /// </summary>
    private GameObject GetStatusEffectPrefab(StatusEffectData statusEffect)
    {
        if (statusEffect.isPositive)
            return buffApplyEffectPrefab;
        else
            return debuffApplyEffectPrefab;
    }

    /// <summary>
    /// 対象位置リストを取得
    /// </summary>
    private List<Vector3> GetTargetPositions(List<string> targetCharacterIds)
    {
        var positions = new List<Vector3>();

        foreach (var targetId in targetCharacterIds)
        {
            if (characterPositions.TryGetValue(targetId, out Transform targetTransform))
            {
                positions.Add(targetTransform.position);
            }
        }

        return positions;
    }

    /// <summary>
    /// 属性色を適用
    /// </summary>
    private void ApplyAttributeColor(GameObject effect, AttributeType attributeType)
    {
        var renderers = effect.GetComponentsInChildren<Renderer>();
        Color color = attributeType switch
        {
            AttributeType.Fire => Color.red,
            AttributeType.Water => Color.blue,
            AttributeType.Wind => Color.green,
            AttributeType.Earth => new Color(0.8f, 0.6f, 0.2f), // 茶色
            _ => Color.white
        };

        foreach (var renderer in renderers)
        {
            if (renderer.material != null)
            {
                renderer.material.color = color;
            }
        }
    }

    /// <summary>
    /// ステータス効果色を適用
    /// </summary>
    private void ApplyStatusEffectColor(GameObject effect, StatusEffectData statusEffect)
    {
        var renderers = effect.GetComponentsInChildren<Renderer>();
        var color = statusEffect.GetEffectColor();

        foreach (var renderer in renderers)
        {
            if (renderer.material != null)
            {
                renderer.material.color = color;
            }
        }
    }

    /// <summary>
    /// 全子エフェクトを停止
    /// </summary>
    private void StopAllChildEffects()
    {
        // 各エフェクト親の子オブジェクトを削除
        DestroyChildEffects(skillEffectParent);
        DestroyChildEffects(attributeEffectParent);
        DestroyChildEffects(statusEffectParent);
        DestroyChildEffects(specialEffectParent);
    }

    /// <summary>
    /// 子エフェクトを削除
    /// </summary>
    private void DestroyChildEffects(Transform parent)
    {
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            if (parent.GetChild(i) != null)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }

    /// <summary>
    /// 音声再生
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (effectAudioSource != null && clip != null)
        {
            effectAudioSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region ログ・デバッグ

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[BattleEffectUI] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[BattleEffectUI] {message}");
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("全エフェクトをテスト再生")]
    private void TestAllEffects()
    {
        DebugLog("全エフェクトのテスト再生を開始");

        // テスト用のダミー位置
        var dummyPos = transform.position;

        if (normalAttackEffectPrefab != null)
        {
            var effect = Instantiate(normalAttackEffectPrefab, dummyPos, Quaternion.identity, skillEffectParent);
            Destroy(effect, 2f);
        }
    }

    [ContextMenu("エフェクト設定を確認")]
    private void ValidateEffectSetup()
    {
        DebugLog("=== エフェクト設定確認 ===");
        DebugLog($"通常攻撃エフェクト: {(normalAttackEffectPrefab != null ? "設定済み" : "未設定")}");
        DebugLog($"回復スキルエフェクト: {(healSkillEffectPrefab != null ? "設定済み" : "未設定")}");
        DebugLog($"火属性エフェクト: {(fireAttackEffectPrefab != null ? "設定済み" : "未設定")}");
        DebugLog($"クリティカルエフェクト: {(criticalHitEffectPrefab != null ? "設定済み" : "未設定")}");
        DebugLog($"オーディオソース: {(effectAudioSource != null ? "設定済み" : "未設定")}");
    }
#endif

    #endregion
}

#region データ構造

/// <summary>
/// 戦闘エフェクトタイプ（名前空間競合回避）
/// </summary>
public enum BattleEffectType
{
    Skill,
    Attribute,
    StatusEffect,
    Critical,
    NormalAttack
}

/// <summary>
/// 戦闘エフェクトリクエスト
/// </summary>
[System.Serializable]
public class BattleEffectRequest
{
    public BattleEffectType effectType;
    public BattleSkillData skillData;
    public AttributeType attributeType;
    public StatusEffectData statusEffectData;
    public DamageEffectiveness effectiveness;
    public string casterCharacterId;
    public List<string> targetCharacterIds;
    public float duration;

    public BattleEffectRequest()
    {
        targetCharacterIds = new List<string>();
        duration = 1.0f;
    }
}

#endregion