using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 戦闘アニメーションの制御UI
/// 攻撃モーション再生、スキル使用アニメーション、撃破・勝利アニメーションを管理
/// データアクセス統一ルール: UI層 → Manager層 → Data層
/// </summary>
public class BattleAnimationUI : MonoBehaviour
{
    [Header("アニメーション設定")]
    [SerializeField] private bool enableDebugLog = false;
    [SerializeField] private float defaultAnimationSpeed = 1.0f;
    [SerializeField] private bool enableBattleAnimations = true;

    [Header("キャラクターアニメーション")]
    [SerializeField] private Transform playerAnimationParent;
    [SerializeField] private Transform enemyAnimationParent;
    [SerializeField] private float characterMoveDistance = 50f;
    [SerializeField] private float characterMoveDuration = 0.5f;

    [Header("攻撃アニメーション")]
    [SerializeField] private float normalAttackDuration = 1.0f;
    [SerializeField] private float skillAttackDuration = 1.5f;
    [SerializeField] private AnimationCurve attackMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("スキルアニメーション")]
    [SerializeField] private GameObject skillCastEffectPrefab;
    [SerializeField] private Transform skillEffectParent;
    [SerializeField] private float skillCastDuration = 1.0f;
    [SerializeField] private float skillActivationDelay = 0.3f;

    [Header("撃破アニメーション")]
    [SerializeField] private float defeatAnimationDuration = 2.0f;
    [SerializeField] private AnimationCurve defeatFadeCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
    [SerializeField] private GameObject defeatEffectPrefab;

    [Header("勝利アニメーション")]
    [SerializeField] private GameObject victoryEffectPrefab;
    [SerializeField] private Transform victoryEffectParent;
    [SerializeField] private float victoryAnimationDuration = 3.0f;
    [SerializeField] private AnimationCurve victoryScaleCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1.2f), new Keyframe(1, 1));

    [Header("UI要素アニメーション")]
    [SerializeField] private CanvasGroup battleUICanvasGroup;
    [SerializeField] private float uiFadeInDuration = 0.5f;
    [SerializeField] private float uiFadeOutDuration = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioSource animationAudioSource;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip skillCastSound;
    [SerializeField] private AudioClip defeatSound;
    [SerializeField] private AudioClip victorySound;

    // プライベートフィールド
    private Dictionary<string, CharacterAnimationData> characterAnimators;
    private Queue<BattleAnimationRequest> animationQueue;
    private bool isAnimating;
    private Coroutine animationProcessCoroutine;
    private float currentBattleSpeed = 1.0f;
    private BattleDataManager battleDataManager;

    // イベント
    public static event Action<string> OnAnimationStarted;
    public static event Action<string> OnAnimationCompleted;
    public static event Action OnBattleAnimationsCompleted;

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
        StopAllAnimations();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネント初期化
    /// </summary>
    private void InitializeComponent()
    {
        characterAnimators = new Dictionary<string, CharacterAnimationData>();
        animationQueue = new Queue<BattleAnimationRequest>();
        isAnimating = false;

        // BattleDataManagerの参照を取得
        battleDataManager = FindFirstObjectByType<BattleDataManager>();

        // 親オブジェクトの初期化
        if (playerAnimationParent == null)
            playerAnimationParent = transform;
        if (enemyAnimationParent == null)
            enemyAnimationParent = transform;
        if (skillEffectParent == null)
            skillEffectParent = transform;
        if (victoryEffectParent == null)
            victoryEffectParent = transform;

        // UI初期化
        if (battleUICanvasGroup != null)
        {
            battleUICanvasGroup.alpha = 0f;
            StartCoroutine(FadeInBattleUI());
        }

        DebugLog("BattleAnimationUI初期化完了");
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
            BattleManager.OnBattleCompleted += OnBattleCompleted;
        }

        // BattleDataManagerのイベント登録
        if (battleDataManager != null)
        {
            BattleDataManager.OnCharacterDefeated += OnCharacterDefeated;
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
            BattleManager.OnBattleCompleted -= OnBattleCompleted;
        }

        // BattleDataManagerのイベント登録解除
        if (battleDataManager != null)
        {
            BattleDataManager.OnCharacterDefeated -= OnCharacterDefeated;
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// キャラクターアニメーターを登録
    /// </summary>
    public void RegisterCharacterAnimator(string characterId, Animator animator, Transform transform, Image characterImage)
    {
        if (string.IsNullOrEmpty(characterId))
        {
            LogError("無効なキャラクターID");
            return;
        }

        var animationData = new CharacterAnimationData
        {
            characterId = characterId,
            animator = animator,
            characterTransform = transform,
            characterImage = characterImage,
            originalPosition = transform != null ? transform.position : Vector3.zero,
            originalScale = transform != null ? transform.localScale : Vector3.one
        };

        characterAnimators[characterId] = animationData;
        DebugLog($"キャラクターアニメーター登録: {characterId}");
    }

    /// <summary>
    /// キャラクターアニメーターを削除
    /// </summary>
    public void UnregisterCharacterAnimator(string characterId)
    {
        if (characterAnimators.ContainsKey(characterId))
        {
            characterAnimators.Remove(characterId);
            DebugLog($"キャラクターアニメーター削除: {characterId}");
        }
    }

    /// <summary>
    /// 攻撃アニメーションを再生
    /// </summary>
    public void PlayAttackAnimation(string attackerId, List<string> targetIds, bool isSkillAttack = false)
    {
        var request = new BattleAnimationRequest
        {
            animationType = BattleAnimationType.Attack,
            attackerId = attackerId,
            targetIds = targetIds,
            isSkillAttack = isSkillAttack,
            duration = isSkillAttack ? skillAttackDuration : normalAttackDuration
        };

        EnqueueAnimation(request);
    }

    /// <summary>
    /// スキル使用アニメーションを再生
    /// </summary>
    public void PlaySkillAnimation(string casterId, BattleSkillData skill, List<string> targetIds)
    {
        if (skill == null)
        {
            LogError("スキルデータがnullです");
            return;
        }

        var request = new BattleAnimationRequest
        {
            animationType = BattleAnimationType.SkillCast,
            attackerId = casterId,
            targetIds = targetIds,
            skillData = skill,
            duration = skillCastDuration
        };

        EnqueueAnimation(request);
    }

    /// <summary>
    /// 撃破アニメーションを再生
    /// </summary>
    public void PlayDefeatAnimation(string characterId)
    {
        var request = new BattleAnimationRequest
        {
            animationType = BattleAnimationType.Defeat,
            targetIds = new List<string> { characterId },
            duration = defeatAnimationDuration
        };

        EnqueueAnimation(request);
    }

    /// <summary>
    /// 勝利アニメーションを再生
    /// </summary>
    public void PlayVictoryAnimation(List<string> victoriousCharacterIds)
    {
        var request = new BattleAnimationRequest
        {
            animationType = BattleAnimationType.Victory,
            targetIds = victoriousCharacterIds,
            duration = victoryAnimationDuration
        };

        EnqueueAnimation(request);
    }

    /// <summary>
    /// 戦闘速度を設定
    /// </summary>
    public void SetBattleSpeed(float speedMultiplier)
    {
        currentBattleSpeed = Mathf.Clamp(speedMultiplier, 0.5f, 4.0f);
        DebugLog($"戦闘アニメーション速度設定: {currentBattleSpeed}倍速");
    }

    /// <summary>
    /// 全アニメーションを停止
    /// </summary>
    public void StopAllAnimations()
    {
        if (animationProcessCoroutine != null)
        {
            StopCoroutine(animationProcessCoroutine);
            animationProcessCoroutine = null;
        }

        animationQueue.Clear();
        isAnimating = false;

        // キャラクターを元の位置に戻す
        ResetAllCharacterPositions();
    }

    /// <summary>
    /// アニメーション有効/無効設定
    /// </summary>
    public void SetAnimationsEnabled(bool enabled)
    {
        enableBattleAnimations = enabled;
        DebugLog($"戦闘アニメーション: {(enabled ? "有効" : "無効")}");
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// 行動実行イベント処理
    /// </summary>
    private void OnActionExecuted(ActionData action)
    {
        if (action == null || !enableBattleAnimations) return;

        if (action.IsSkillUse())
        {
            // スキル使用アニメーション
            if (battleDataManager != null)
            {
                var skill = battleDataManager.GetCharacterSkill(action.actorId, action.skillId);
                if (skill != null)
                {
                    PlaySkillAnimation(action.actorId, skill, action.targetIds);
                }
            }
        }
        else
        {
            // 通常攻撃アニメーション
            PlayAttackAnimation(action.actorId, action.targetIds, false);
        }
    }

    /// <summary>
    /// キャラクター撃破イベント処理
    /// </summary>
    private void OnCharacterDefeated(BattleCharacterData character)
    {
        if (character == null || !enableBattleAnimations) return;

        PlayDefeatAnimation(character.characterId);
    }

    /// <summary>
    /// 戦闘完了イベント処理
    /// </summary>
    private void OnBattleCompleted(BattleResultData battleResult)
    {
        if (battleResult == null || !enableBattleAnimations) return;

        if (battleResult.isVictory)
        {
            // 勝利アニメーション（生存しているプレイヤーキャラクター）
            var playerIds = new List<string>();
            foreach (var kvp in characterAnimators)
            {
                if (kvp.Value.characterId.Contains("player"))
                {
                    playerIds.Add(kvp.Key);
                }
            }
            PlayVictoryAnimation(playerIds);
        }

        // UI フェードアウト
        StartCoroutine(FadeOutBattleUI());
    }

    #endregion

    #region アニメーション処理

    /// <summary>
    /// アニメーションをキューに追加
    /// </summary>
    private void EnqueueAnimation(BattleAnimationRequest request)
    {
        // 戦闘速度を反映
        request.duration /= currentBattleSpeed;

        animationQueue.Enqueue(request);

        if (!isAnimating)
        {
            animationProcessCoroutine = StartCoroutine(ProcessAnimationQueue());
        }
    }

    /// <summary>
    /// アニメーションキュー処理
    /// </summary>
    private IEnumerator ProcessAnimationQueue()
    {
        isAnimating = true;

        while (animationQueue.Count > 0)
        {
            var request = animationQueue.Dequeue();
            yield return StartCoroutine(ExecuteAnimation(request));

            // アニメーション間の間隔
            yield return new WaitForSeconds(0.1f / currentBattleSpeed);
        }

        isAnimating = false;
        animationProcessCoroutine = null;
        OnBattleAnimationsCompleted?.Invoke();
    }

    /// <summary>
    /// アニメーション実行
    /// </summary>
    private IEnumerator ExecuteAnimation(BattleAnimationRequest request)
    {
        OnAnimationStarted?.Invoke(request.animationType.ToString());

        switch (request.animationType)
        {
            case BattleAnimationType.Attack:
                yield return StartCoroutine(ExecuteAttackAnimation(request));
                break;
            case BattleAnimationType.SkillCast:
                yield return StartCoroutine(ExecuteSkillCastAnimation(request));
                break;
            case BattleAnimationType.Defeat:
                yield return StartCoroutine(ExecuteDefeatAnimation(request));
                break;
            case BattleAnimationType.Victory:
                yield return StartCoroutine(ExecuteVictoryAnimation(request));
                break;
        }

        OnAnimationCompleted?.Invoke(request.animationType.ToString());
    }

    /// <summary>
    /// 攻撃アニメーション実行
    /// </summary>
    private IEnumerator ExecuteAttackAnimation(BattleAnimationRequest request)
    {
        var attackerData = GetCharacterAnimationData(request.attackerId);
        if (attackerData == null) yield break;

        // 音声再生
        PlaySound(attackSound);

        // 攻撃者のアニメーション
        if (attackerData.animator != null)
        {
            attackerData.animator.SetTrigger("Attack");
        }

        // 前進アニメーション
        yield return StartCoroutine(MoveCharacterToTarget(attackerData, request.duration * 0.3f));

        // 攻撃演出（中央で少し待機）
        yield return new WaitForSeconds(request.duration * 0.4f);

        // 元の位置に戻る
        yield return StartCoroutine(MoveCharacterToOriginalPosition(attackerData, request.duration * 0.3f));

        // 対象キャラクターのダメージアニメーション
        foreach (var targetId in request.targetIds)
        {
            StartCoroutine(PlayDamageReaction(targetId));
        }
    }

    /// <summary>
    /// スキルキャストアニメーション実行
    /// </summary>
    private IEnumerator ExecuteSkillCastAnimation(BattleAnimationRequest request)
    {
        var casterData = GetCharacterAnimationData(request.attackerId);
        if (casterData == null) yield break;

        // スキルキャスト音声
        PlaySound(skillCastSound);

        // キャストエフェクト生成
        GameObject castEffect = null;
        if (skillCastEffectPrefab != null && casterData.characterTransform != null)
        {
            castEffect = Instantiate(skillCastEffectPrefab, casterData.characterTransform.position, Quaternion.identity, skillEffectParent);
        }

        // キャスターのアニメーション
        if (casterData.animator != null)
        {
            casterData.animator.SetTrigger("SkillCast");
        }

        // スキル発動までの遅延
        yield return new WaitForSeconds(skillActivationDelay / currentBattleSpeed);

        // スキル発動アニメーション
        yield return StartCoroutine(PlaySkillActivationEffect(casterData, request.skillData));

        // エフェクトクリーンアップ
        if (castEffect != null)
        {
            Destroy(castEffect);
        }

        // 対象キャラクターへの効果
        foreach (var targetId in request.targetIds)
        {
            StartCoroutine(PlaySkillEffectOnTarget(targetId, request.skillData));
        }
    }

    /// <summary>
    /// 撃破アニメーション実行
    /// </summary>
    private IEnumerator ExecuteDefeatAnimation(BattleAnimationRequest request)
    {
        if (request.targetIds.Count == 0) yield break;

        var targetId = request.targetIds[0];
        var targetData = GetCharacterAnimationData(targetId);
        if (targetData == null) yield break;

        // 撃破音声
        PlaySound(defeatSound);

        // 撃破エフェクト生成
        if (defeatEffectPrefab != null && targetData.characterTransform != null)
        {
            var effect = Instantiate(defeatEffectPrefab, targetData.characterTransform.position, Quaternion.identity);
            Destroy(effect, request.duration);
        }

        // フェードアウトアニメーション
        yield return StartCoroutine(FadeOutCharacter(targetData, request.duration));

        DebugLog($"撃破アニメーション完了: {targetId}");
    }

    /// <summary>
    /// 勝利アニメーション実行
    /// </summary>
    private IEnumerator ExecuteVictoryAnimation(BattleAnimationRequest request)
    {
        // 勝利音声
        PlaySound(victorySound);

        // 勝利エフェクト生成
        if (victoryEffectPrefab != null)
        {
            var effect = Instantiate(victoryEffectPrefab, victoryEffectParent);
            Destroy(effect, request.duration);
        }

        // 勝利キャラクターのアニメーション
        var victoryAnimations = new List<Coroutine>();
        foreach (var characterId in request.targetIds)
        {
            var characterData = GetCharacterAnimationData(characterId);
            if (characterData != null)
            {
                if (characterData.animator != null)
                {
                    characterData.animator.SetTrigger("Victory");
                }
                victoryAnimations.Add(StartCoroutine(PlayVictoryBounce(characterData, request.duration)));
            }
        }

        // 全ての勝利アニメーションの完了を待つ
        foreach (var animation in victoryAnimations)
        {
            yield return animation;
        }

        DebugLog("勝利アニメーション完了");
    }

    #endregion

    #region アニメーションヘルパー

    /// <summary>
    /// キャラクターを対象方向に移動
    /// </summary>
    private IEnumerator MoveCharacterToTarget(CharacterAnimationData characterData, float duration)
    {
        if (characterData.characterTransform == null) yield break;

        Vector3 startPos = characterData.originalPosition;
        Vector3 targetPos = startPos + Vector3.right * characterMoveDistance;

        yield return StartCoroutine(MoveCharacter(characterData.characterTransform, startPos, targetPos, duration, attackMoveCurve));
    }

    /// <summary>
    /// キャラクターを元の位置に戻す
    /// </summary>
    private IEnumerator MoveCharacterToOriginalPosition(CharacterAnimationData characterData, float duration)
    {
        if (characterData.characterTransform == null) yield break;

        Vector3 currentPos = characterData.characterTransform.position;
        Vector3 originalPos = characterData.originalPosition;

        yield return StartCoroutine(MoveCharacter(characterData.characterTransform, currentPos, originalPos, duration, attackMoveCurve));
    }

    /// <summary>
    /// キャラクター移動アニメーション
    /// </summary>
    private IEnumerator MoveCharacter(Transform character, Vector3 from, Vector3 to, float duration, AnimationCurve curve)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = curve.Evaluate(t);

            character.position = Vector3.Lerp(from, to, curveValue);
            yield return null;
        }

        character.position = to;
    }

    /// <summary>
    /// ダメージリアクション
    /// </summary>
    private IEnumerator PlayDamageReaction(string characterId)
    {
        var characterData = GetCharacterAnimationData(characterId);
        if (characterData == null) yield break;

        if (characterData.animator != null)
        {
            characterData.animator.SetTrigger("Damage");
        }

        // 軽く揺らすアニメーション
        if (characterData.characterTransform != null)
        {
            yield return StartCoroutine(ShakeCharacter(characterData.characterTransform, 0.3f, 10f));
        }
    }

    /// <summary>
    /// スキル発動エフェクト
    /// </summary>
    private IEnumerator PlaySkillActivationEffect(CharacterAnimationData casterData, BattleSkillData skill)
    {
        if (casterData.characterTransform == null) yield break;

        // スキルタイプに応じたエフェクト
        yield return StartCoroutine(ScaleCharacter(casterData.characterTransform, Vector3.one * 1.1f, 0.2f));
        yield return StartCoroutine(ScaleCharacter(casterData.characterTransform, casterData.originalScale, 0.2f));
    }

    /// <summary>
    /// 対象へのスキル効果
    /// </summary>
    private IEnumerator PlaySkillEffectOnTarget(string targetId, BattleSkillData skill)
    {
        var targetData = GetCharacterAnimationData(targetId);
        if (targetData == null) yield break;

        if (skill.IsHealSkill())
        {
            // 回復スキルの場合は光るエフェクト
            yield return StartCoroutine(FlashCharacter(targetData, Color.green, 0.5f));
        }
        else
        {
            // 攻撃スキルの場合はダメージリアクション
            yield return StartCoroutine(PlayDamageReaction(targetId));
        }
    }

    /// <summary>
    /// キャラクターフェードアウト
    /// </summary>
    private IEnumerator FadeOutCharacter(CharacterAnimationData characterData, float duration)
    {
        if (characterData.characterImage == null) yield break;

        Color startColor = characterData.characterImage.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = defeatFadeCurve.Evaluate(elapsed / duration);

            characterData.characterImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        characterData.characterImage.color = endColor;
    }

    /// <summary>
    /// 勝利バウンス
    /// </summary>
    private IEnumerator PlayVictoryBounce(CharacterAnimationData characterData, float duration)
    {
        if (characterData.characterTransform == null) yield break;

        Vector3 originalScale = characterData.originalScale;
        Vector3 bounceScale = originalScale * 1.2f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveValue = victoryScaleCurve.Evaluate(t);

            Vector3 currentScale = Vector3.Lerp(originalScale, bounceScale, curveValue);
            characterData.characterTransform.localScale = currentScale;
            yield return null;
        }

        characterData.characterTransform.localScale = originalScale;
    }

    /// <summary>
    /// キャラクター振動
    /// </summary>
    private IEnumerator ShakeCharacter(Transform character, float duration, float intensity)
    {
        Vector3 originalPos = character.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = UnityEngine.Random.Range(-1f, 1f) * intensity * (1f - elapsed / duration);
            float y = UnityEngine.Random.Range(-1f, 1f) * intensity * (1f - elapsed / duration);

            character.position = originalPos + new Vector3(x, y, 0);
            yield return null;
        }

        character.position = originalPos;
    }

    /// <summary>
    /// キャラクタースケール
    /// </summary>
    private IEnumerator ScaleCharacter(Transform character, Vector3 targetScale, float duration)
    {
        Vector3 startScale = character.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            character.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        character.localScale = targetScale;
    }

    /// <summary>
    /// キャラクターフラッシュ
    /// </summary>
    private IEnumerator FlashCharacter(CharacterAnimationData characterData, Color flashColor, float duration)
    {
        if (characterData.characterImage == null) yield break;

        Color originalColor = characterData.characterImage.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 4f, 1f);

            characterData.characterImage.color = Color.Lerp(originalColor, flashColor, t);
            yield return null;
        }

        characterData.characterImage.color = originalColor;
    }

    /// <summary>
    /// UI フェードイン
    /// </summary>
    private IEnumerator FadeInBattleUI()
    {
        if (battleUICanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < uiFadeInDuration)
        {
            elapsed += Time.deltaTime;
            battleUICanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / uiFadeInDuration);
            yield return null;
        }

        battleUICanvasGroup.alpha = 1f;
        battleUICanvasGroup.interactable = true;
        battleUICanvasGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// UI フェードアウト
    /// </summary>
    private IEnumerator FadeOutBattleUI()
    {
        if (battleUICanvasGroup == null) yield break;

        battleUICanvasGroup.interactable = false;
        battleUICanvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < uiFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            battleUICanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / uiFadeOutDuration);
            yield return null;
        }

        battleUICanvasGroup.alpha = 0f;
    }

    #endregion

    #region ヘルパーメソッド

    /// <summary>
    /// キャラクターアニメーションデータを取得
    /// </summary>
    private CharacterAnimationData GetCharacterAnimationData(string characterId)
    {
        characterAnimators.TryGetValue(characterId, out CharacterAnimationData data);
        return data;
    }

    /// <summary>
    /// 全キャラクターを元の位置に戻す
    /// </summary>
    private void ResetAllCharacterPositions()
    {
        foreach (var kvp in characterAnimators)
        {
            var data = kvp.Value;
            if (data.characterTransform != null)
            {
                data.characterTransform.position = data.originalPosition;
                data.characterTransform.localScale = data.originalScale;
            }

            if (data.characterImage != null)
            {
                var color = data.characterImage.color;
                color.a = 1f;
                data.characterImage.color = color;
            }
        }
    }

    /// <summary>
    /// 音声再生
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (animationAudioSource != null && clip != null)
        {
            animationAudioSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region ログ・デバッグ

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[BattleAnimationUI] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[BattleAnimationUI] {message}");
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("アニメーションテスト実行")]
    private void TestAnimations()
    {
        DebugLog("アニメーションテストを開始");

        // テスト用のキャラクターIDリスト
        var testPlayerIds = new List<string> { "player" };
        var testEnemyIds = new List<string> { "monster_1" };

        // 攻撃アニメーションテスト
        PlayAttackAnimation("player", testEnemyIds, false);
    }

    [ContextMenu("アニメーション設定確認")]
    private void ValidateAnimationSetup()
    {
        DebugLog("=== アニメーション設定確認 ===");
        DebugLog($"プレイヤーアニメーション親: {(playerAnimationParent != null ? "設定済み" : "未設定")}");
        DebugLog($"敵アニメーション親: {(enemyAnimationParent != null ? "設定済み" : "未設定")}");
        DebugLog($"スキルエフェクト親: {(skillEffectParent != null ? "設定済み" : "未設定")}");
        DebugLog($"勝利エフェクト親: {(victoryEffectParent != null ? "設定済み" : "未設定")}");
        DebugLog($"スキルキャストエフェクト: {(skillCastEffectPrefab != null ? "設定済み" : "未設定")}");
        DebugLog($"撃破エフェクト: {(defeatEffectPrefab != null ? "設定済み" : "未設定")}");
        DebugLog($"勝利エフェクト: {(victoryEffectPrefab != null ? "設定済み" : "未設定")}");
        DebugLog($"バトルUIキャンバスグループ: {(battleUICanvasGroup != null ? "設定済み" : "未設定")}");
        DebugLog($"オーディオソース: {(animationAudioSource != null ? "設定済み" : "未設定")}");
        DebugLog($"登録済みキャラクター数: {characterAnimators.Count}");
        DebugLog($"アニメーション有効: {enableBattleAnimations}");
        DebugLog($"現在の戦闘速度: {currentBattleSpeed}倍速");
    }

    [ContextMenu("キャラクター位置リセット")]
    private void EditorResetCharacterPositions()
    {
        ResetAllCharacterPositions();
        DebugLog("全キャラクターの位置をリセットしました");
    }

    [ContextMenu("勝利アニメーションテスト")]
    private void TestVictoryAnimation()
    {
        var playerIds = new List<string>();
        foreach (var kvp in characterAnimators)
        {
            if (kvp.Value.characterId.Contains("player"))
            {
                playerIds.Add(kvp.Key);
            }
        }

        if (playerIds.Count > 0)
        {
            PlayVictoryAnimation(playerIds);
            DebugLog("勝利アニメーションテストを実行");
        }
        else
        {
            DebugLog("プレイヤーキャラクターが登録されていません");
        }
    }
#endif

    #endregion
}

#region データ構造

/// <summary>
/// 戦闘アニメーションタイプ
/// </summary>
public enum BattleAnimationType
{
    Attack,
    SkillCast,
    Defeat,
    Victory
}

/// <summary>
/// 戦闘アニメーションリクエスト
/// </summary>
[System.Serializable]
public class BattleAnimationRequest
{
    public BattleAnimationType animationType;
    public string attackerId;
    public List<string> targetIds;
    public BattleSkillData skillData;
    public bool isSkillAttack;
    public float duration;

    public BattleAnimationRequest()
    {
        targetIds = new List<string>();
        duration = 1.0f;
    }
}

/// <summary>
/// キャラクターアニメーションデータ
/// </summary>
[System.Serializable]
public class CharacterAnimationData
{
    public string characterId;
    public Animator animator;
    public Transform characterTransform;
    public Image characterImage;
    public Vector3 originalPosition;
    public Vector3 originalScale;
    public bool isAnimating;

    public CharacterAnimationData()
    {
        originalPosition = Vector3.zero;
        originalScale = Vector3.one;
        isAnimating = false;
    }
}

#endregion