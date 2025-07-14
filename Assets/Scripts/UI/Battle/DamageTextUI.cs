using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// ダメージ数値の表示演出UI
/// 責任範囲：
/// - ダメージ数値ポップアップ
/// - 表示箇所の調整(ダメージを受けたキャラの場所に表示)
/// - 数値色分け（ダメージ(有利・通常・不利)・回復）
/// データアクセス統一ルール: UI層 → BattleManager → Data層
/// </summary>
public class DamageTextUI : MonoBehaviour
{
    [Header("プレハブ設定")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private Transform damageTextParent;

    [Header("表示設定")]
    [SerializeField] private float displayDuration = 2.0f;
    [SerializeField] private float moveDistance = 100f;
    [SerializeField] private Vector3 positionOffset = new Vector3(0, 50f, 0);
    [SerializeField] private int maxSimultaneousTexts = 10;

    [Header("色設定")]
    [SerializeField] private Color damageAdvantageColor = new Color(1f, 0.27f, 0.27f, 1f); // #FF4444 (有利)
    [SerializeField] private Color damageNormalColor = new Color(1f, 1f, 1f, 1f); // #FFFFFF (通常)
    [SerializeField] private Color damageDisadvantageColor = new Color(0.27f, 0.53f, 1f, 1f); // #4488FF (不利・青系)
    [SerializeField] private Color healColor = new Color(0.27f, 1f, 0.27f, 1f); // #44FF44 (回復)
    [SerializeField] private Color criticalColor = new Color(1f, 0.84f, 0f, 1f); // #FFD700 (クリティカル)

    [Header("アニメーション設定")]
    [SerializeField] private float popUpScale = 1.5f;
    [SerializeField] private float animationSpeed = 1.0f;
    [SerializeField] private Ease moveEase = Ease.OutQuart;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    [Header("デバッグ設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showDebugPositions = false;

    // 内部状態
    private bool isInitialized = false;
    private List<GameObject> activeDamageTexts = new List<GameObject>();
    private Queue<GameObject> textPool = new Queue<GameObject>();
    private Camera battleCamera;

    // キャラクター位置追跡
    private Dictionary<string, Transform> characterTransforms = new Dictionary<string, Transform>();

    #region Unity Lifecycle

    void Start()
    {
        InitializeUI();
        SetupEventListeners();
        Log("DamageTextUI初期化完了");
    }

    void OnDestroy()
    {
        CleanupEventListeners();
        CleanupTextPool();
    }

    #endregion

    #region 初期化・終了処理

    /// <summary>
    /// UI初期化
    /// </summary>
    private void InitializeUI()
    {
        // カメラ取得
        battleCamera = Camera.main ?? FindFirstObjectByType<Camera>();

        if (battleCamera == null)
        {
            LogError("戦闘用カメラが見つかりません");
        }

        // 親オブジェクト確認
        if (damageTextParent == null)
        {
            damageTextParent = this.transform;
            Log("damageTextParentが未設定のため、自身のTransformを使用します");
        }

        // プレハブ確認
        if (damageTextPrefab == null)
        {
            LogError("damageTextPrefabが設定されていません。Inspectorで設定してください。");
        }

        // テキストプール初期化
        InitializeTextPool();

        isInitialized = true;
        Log("DamageTextUI初期化処理完了");
    }

    /// <summary>
    /// テキストプール初期化
    /// </summary>
    private void InitializeTextPool()
    {
        if (damageTextPrefab == null) return;

        // 初期プール作成
        for (int i = 0; i < maxSimultaneousTexts; i++)
        {
            var textObj = Instantiate(damageTextPrefab, damageTextParent);
            textObj.SetActive(false);
            textPool.Enqueue(textObj);
        }

        Log($"ダメージテキストプールを初期化: {maxSimultaneousTexts}個");
    }

    /// <summary>
    /// イベントリスナー設定
    /// </summary>
    private void SetupEventListeners()
    {
        // BattleManagerのイベントに登録
        BattleManager.OnActionExecuted += OnActionExecuted;
        BattleManager.OnBattleInitialized += OnBattleInitialized;
        BattleManager.OnBattleCompleted += OnBattleCompleted;

        Log("BattleManagerイベントリスナー設定完了");
    }

    /// <summary>
    /// イベントリスナー解除
    /// </summary>
    private void CleanupEventListeners()
    {
        // BattleManagerのイベントから解除
        BattleManager.OnActionExecuted -= OnActionExecuted;
        BattleManager.OnBattleInitialized -= OnBattleInitialized;
        BattleManager.OnBattleCompleted -= OnBattleCompleted;

        Log("BattleManagerイベントリスナー解除完了");
    }

    /// <summary>
    /// テキストプールクリーンアップ
    /// </summary>
    private void CleanupTextPool()
    {
        // アクティブなテキストを停止
        foreach (var textObj in activeDamageTexts)
        {
            if (textObj != null)
            {
                DOTween.Kill(textObj);
                Destroy(textObj);
            }
        }
        activeDamageTexts.Clear();

        // プールのテキストを削除
        while (textPool.Count > 0)
        {
            var textObj = textPool.Dequeue();
            if (textObj != null)
            {
                Destroy(textObj);
            }
        }

        Log("テキストプールクリーンアップ完了");
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// 戦闘初期化イベントハンドラ
    /// </summary>
    /// <param name="setupData">戦闘セットアップデータ</param>
    private void OnBattleInitialized(BattleSetupData setupData)
    {
        try
        {
            // キャラクター位置情報をクリア
            characterTransforms.Clear();
            Log("戦闘初期化: キャラクター位置情報をリセット");
        }
        catch (Exception e)
        {
            LogError($"戦闘初期化処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 行動実行イベントハンドラ
    /// </summary>
    /// <param name="action">行動データ</param>
    private void OnActionExecuted(ActionData action)
    {
        try
        {
            if (action?.damageResults == null || action.damageResults.Count == 0)
            {
                return; // ダメージが発生していない行動はスキップ
            }

            Log($"ダメージ表示処理開始: {action.actorName}の行動");

            // 各ダメージ結果について表示
            foreach (var damageData in action.damageResults)
            {
                ShowDamageText(damageData, action);
            }
        }
        catch (Exception e)
        {
            LogError($"行動実行処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘完了イベントハンドラ
    /// </summary>
    /// <param name="resultData">戦闘結果データ</param>
    private void OnBattleCompleted(BattleResultData resultData)
    {
        try
        {
            Log("戦闘完了: ダメージテキスト表示を停止");

            // 全てのアクティブなダメージテキストを即座に非表示
            foreach (var textObj in activeDamageTexts)
            {
                if (textObj != null)
                {
                    DOTween.Kill(textObj);
                    ReturnTextToPool(textObj);
                }
            }
            activeDamageTexts.Clear();
        }
        catch (Exception e)
        {
            LogError($"戦闘完了処理エラー: {e.Message}");
        }
    }

    #endregion

    #region ダメージテキスト表示機能

    /// <summary>
    /// ダメージテキスト表示
    /// </summary>
    /// <param name="damageData">ダメージデータ</param>
    /// <param name="actionData">行動データ</param>
    private void ShowDamageText(DamageData damageData, ActionData actionData)
    {
        if (!isInitialized || damageData == null)
        {
            LogError("DamageTextUIが初期化されていないか、ダメージデータがnullです");
            return;
        }

        try
        {
            // ダメージテキストオブジェクト取得
            var textObj = GetTextFromPool();
            if (textObj == null)
            {
                LogError("ダメージテキストオブジェクトの取得に失敗しました");
                return;
            }

            // テキスト設定
            SetupDamageText(textObj, damageData, actionData);

            // 表示位置設定
            Vector3 displayPosition = GetDisplayPosition(damageData.targetName);
            textObj.transform.position = displayPosition;

            // アニメーション開始
            StartDamageTextAnimation(textObj, damageData);

            Log($"ダメージテキスト表示: {damageData.targetName} に {damageData.finalDamage}");
        }
        catch (Exception e)
        {
            LogError($"ダメージテキスト表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ダメージテキストの内容設定
    /// </summary>
    /// <param name="textObj">テキストオブジェクト</param>
    /// <param name="damageData">ダメージデータ</param>
    /// <param name="actionData">行動データ</param>
    private void SetupDamageText(GameObject textObj, DamageData damageData, ActionData actionData)
    {
        var textComponent = textObj.GetComponent<TextMeshProUGUI>();
        if (textComponent == null)
        {
            LogError("TextMeshProUGUIコンポーネントが見つかりません");
            return;
        }

        // ダメージ値のテキスト設定
        string damageText = FormatDamageText(damageData);
        textComponent.text = damageText;

        // 色設定
        Color textColor = GetDamageColor(damageData, actionData);
        textComponent.color = textColor;

        // フォントサイズ調整（クリティカルの場合は大きく）
        float fontSize = damageData.isCritical ? 48f : 36f;
        textComponent.fontSize = fontSize;

        Log($"テキスト設定完了: {damageText}, 色: {textColor}");
    }

    /// <summary>
    /// ダメージテキストのフォーマット
    /// </summary>
    /// <param name="damageData">ダメージデータ</param>
    /// <returns>フォーマット済みテキスト</returns>
    private string FormatDamageText(DamageData damageData)
    {
        int damage = damageData.finalDamage;

        if (damage > 0)
        {
            // ダメージの場合
            string baseText = damage.ToString();
            return damageData.isCritical ? $"CRITICAL!\n{baseText}" : baseText;
        }
        else if (damage < 0)
        {
            // 回復の場合
            return $"+{Math.Abs(damage)}";
        }
        else
        {
            // ダメージ0の場合
            return "MISS";
        }
    }

    /// <summary>
    /// ダメージタイプに応じた色取得
    /// </summary>
    /// <param name="damageData">ダメージデータ</param>
    /// <param name="actionData">行動データ</param>
    /// <returns>表示色</returns>
    private Color GetDamageColor(DamageData damageData, ActionData actionData)
    {
        // クリティカルの場合は専用色
        if (damageData.isCritical)
        {
            return criticalColor;
        }

        // 回復の場合
        if (damageData.finalDamage < 0)
        {
            return healColor;
        }

        // ダメージの属性相性による色分け
        return GetAttributeAdvantageColor(damageData, actionData);
    }

    /// <summary>
    /// 属性相性による色取得
    /// </summary>
    /// <param name="damageData">ダメージデータ</param>
    /// <param name="actionData">行動データ</param>
    /// <returns>属性相性に応じた色</returns>
    private Color GetAttributeAdvantageColor(DamageData damageData, ActionData actionData)
    {
        // DamageDataのeffectivenessプロパティを使用して判定
        switch (damageData.effectiveness)
        {
            case DamageEffectiveness.SuperEffective:
                return damageAdvantageColor;
            case DamageEffectiveness.NotVeryEffective:
                return damageDisadvantageColor;
            case DamageEffectiveness.Normal:
            default:
                return damageNormalColor;
        }
    }

    #endregion

    #region 表示位置制御

    /// <summary>
    /// ダメージ表示位置取得
    /// </summary>
    /// <param name="targetName">対象キャラクター名</param>
    /// <returns>表示位置（ワールド座標）</returns>
    private Vector3 GetDisplayPosition(string targetName)
    {
        try
        {
            // キャラクター位置を取得
            Vector3 characterPosition = GetCharacterPosition(targetName);

            // オフセットを適用
            Vector3 displayPosition = characterPosition + positionOffset;

            // 複数同時表示の重複回避
            displayPosition = AdjustForOverlap(displayPosition);

            // 画面端補正
            displayPosition = ClampToScreen(displayPosition);

            if (showDebugPositions)
            {
                Log($"表示位置設定: {targetName} at {displayPosition}");
            }

            return displayPosition;
        }
        catch (Exception e)
        {
            LogError($"表示位置取得エラー: {e.Message}");
            return transform.position;
        }
    }

    /// <summary>
    /// キャラクター位置取得
    /// </summary>
    /// <param name="characterName">キャラクター名</param>
    /// <returns>キャラクター位置</returns>
    private Vector3 GetCharacterPosition(string characterName)
    {
        // キャッシュされた位置情報から取得
        if (characterTransforms.TryGetValue(characterName, out Transform cachedTransform))
        {
            if (cachedTransform != null)
            {
                return cachedTransform.position;
            }
        }

        // BattleManagerから位置情報を取得
        Vector3 position = GetCharacterPositionFromBattleManager(characterName);

        // デフォルト位置（画面中央）
        if (position == Vector3.zero)
        {
            position = GetDefaultDisplayPosition(characterName);
        }

        return position;
    }

    /// <summary>
    /// BattleManagerからキャラクター位置を取得
    /// </summary>
    /// <param name="characterName">キャラクター名</param>
    /// <returns>キャラクター位置</returns>
    private Vector3 GetCharacterPositionFromBattleManager(string characterName)
    {
        if (BattleManager.Instance == null)
        {
            return Vector3.zero;
        }

        try
        {
            // プレイヤーキャラクターの場合
            var playerChar = BattleManager.Instance.GetPlayerCharacter();
            if (playerChar != null &&
                (playerChar.characterName == characterName || playerChar.displayName == characterName))
            {
                return playerChar.battlePosition;
            }

            // 敵キャラクターの場合
            var enemies = BattleManager.Instance.GetEnemyCharacters();
            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    if (enemy.characterName == characterName || enemy.displayName == characterName)
                    {
                        return enemy.battlePosition;
                    }
                }
            }
        }
        catch (Exception e)
        {
            LogError($"BattleManagerからの位置取得エラー: {e.Message}");
        }

        return Vector3.zero;
    }

    /// <summary>
    /// デフォルト表示位置取得
    /// </summary>
    /// <param name="characterName">キャラクター名</param>
    /// <returns>デフォルト位置</returns>
    private Vector3 GetDefaultDisplayPosition(string characterName)
    {
        if (battleCamera == null)
        {
            return transform.position;
        }

        // 画面中央をベースにプレイヤー/敵で左右に分ける
        Vector3 screenCenter = battleCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 10f));

        // プレイヤーは左側、敵は右側（簡易判定）
        bool isPlayer = characterName.ToLower().Contains("プレイヤー") || characterName.ToLower().Contains("player");
        float horizontalOffset = isPlayer ? -200f : 200f;

        return screenCenter + new Vector3(horizontalOffset, 0, 0);
    }

    /// <summary>
    /// 重複表示回避調整
    /// </summary>
    /// <param name="basePosition">基本位置</param>
    /// <returns>調整後位置</returns>
    private Vector3 AdjustForOverlap(Vector3 basePosition)
    {
        const float overlapThreshold = 50f;
        const float adjustmentStep = 30f;

        Vector3 adjustedPosition = basePosition;
        int attempts = 0;
        const int maxAttempts = 5;

        while (attempts < maxAttempts)
        {
            bool hasOverlap = false;

            foreach (var activeText in activeDamageTexts)
            {
                if (activeText != null && Vector3.Distance(activeText.transform.position, adjustedPosition) < overlapThreshold)
                {
                    hasOverlap = true;
                    break;
                }
            }

            if (!hasOverlap)
            {
                break;
            }

            // 上方向にずらす
            adjustedPosition.y += adjustmentStep;
            attempts++;
        }

        return adjustedPosition;
    }

    /// <summary>
    /// 画面端での表示位置補正
    /// </summary>
    /// <param name="worldPosition">ワールド座標</param>
    /// <returns>補正後座標</returns>
    private Vector3 ClampToScreen(Vector3 worldPosition)
    {
        if (battleCamera == null) return worldPosition;

        Vector3 screenPos = battleCamera.WorldToScreenPoint(worldPosition);

        // 画面内にクランプ
        screenPos.x = Mathf.Clamp(screenPos.x, 50f, Screen.width - 50f);
        screenPos.y = Mathf.Clamp(screenPos.y, 50f, Screen.height - 50f);

        return battleCamera.ScreenToWorldPoint(screenPos);
    }

    #endregion

    #region アニメーション制御

    /// <summary>
    /// ダメージテキストアニメーション開始
    /// </summary>
    /// <param name="textObj">テキストオブジェクト</param>
    /// <param name="damageData">ダメージデータ</param>
    private void StartDamageTextAnimation(GameObject textObj, DamageData damageData)
    {
        textObj.SetActive(true);
        activeDamageTexts.Add(textObj);

        // 初期スケール設定
        textObj.transform.localScale = Vector3.zero;

        // 戦闘速度を考慮した時間調整
        float speedMultiplier = BattleManager.Instance?.BattleSpeedMultiplier ?? 1.0f;
        float adjustedDuration = displayDuration / speedMultiplier;
        float adjustedAnimSpeed = animationSpeed * speedMultiplier;

        // アニメーションシーケンス作成
        var sequence = DOTween.Sequence();

        // ポップアップアニメーション
        sequence.Append(textObj.transform.DOScale(popUpScale, 0.2f / adjustedAnimSpeed).SetEase(scaleEase));
        sequence.Append(textObj.transform.DOScale(1f, 0.1f / adjustedAnimSpeed).SetEase(scaleEase));

        // 移動アニメーション
        Vector3 endPosition = textObj.transform.position + Vector3.up * moveDistance;
        sequence.Join(textObj.transform.DOMove(endPosition, adjustedDuration).SetEase(moveEase));

        // フェードアウト
        var textComponent = textObj.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            sequence.Join(textComponent.DOFade(0f, adjustedDuration * 0.5f).SetDelay(adjustedDuration * 0.5f));
        }

        // アニメーション完了時の処理
        sequence.OnComplete(() => {
            ReturnTextToPool(textObj);
            activeDamageTexts.Remove(textObj);
        });

        // シーケンス開始
        sequence.Play();

        Log($"ダメージアニメーション開始: 時間{adjustedDuration}秒");
    }

    #endregion

    #region オブジェクトプール管理

    /// <summary>
    /// プールからテキストオブジェクト取得
    /// </summary>
    /// <returns>テキストオブジェクト</returns>
    private GameObject GetTextFromPool()
    {
        if (textPool.Count > 0)
        {
            return textPool.Dequeue();
        }

        // プールが空の場合は新規作成
        if (damageTextPrefab != null)
        {
            var newTextObj = Instantiate(damageTextPrefab, damageTextParent);
            Log("プールが空のため新規テキストオブジェクトを作成");
            return newTextObj;
        }

        LogError("damageTextPrefabがnullのためテキストオブジェクトを作成できません");
        return null;
    }

    /// <summary>
    /// テキストオブジェクトをプールに返却
    /// </summary>
    /// <param name="textObj">返却するテキストオブジェクト</param>
    private void ReturnTextToPool(GameObject textObj)
    {
        if (textObj == null) return;

        // アニメーション停止
        DOTween.Kill(textObj);

        // 初期状態にリセット
        textObj.transform.localScale = Vector3.one;
        textObj.SetActive(false);

        var textComponent = textObj.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.alpha = 1f;
        }

        // プールに返却
        textPool.Enqueue(textObj);
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// キャラクター位置を手動登録（他のUIから呼び出し用）
    /// </summary>
    /// <param name="characterName">キャラクター名</param>
    /// <param name="characterTransform">キャラクターのTransform</param>
    public void RegisterCharacterTransform(string characterName, Transform characterTransform)
    {
        if (string.IsNullOrEmpty(characterName) || characterTransform == null)
        {
            LogError("無効なキャラクター情報の登録試行");
            return;
        }

        characterTransforms[characterName] = characterTransform;
        Log($"キャラクター位置登録: {characterName}");
    }

    /// <summary>
    /// キャラクター位置登録解除
    /// </summary>
    /// <param name="characterName">キャラクター名</param>
    public void UnregisterCharacterTransform(string characterName)
    {
        if (characterTransforms.ContainsKey(characterName))
        {
            characterTransforms.Remove(characterName);
            Log($"キャラクター位置登録解除: {characterName}");
        }
    }

    /// <summary>
    /// 全ダメージテキストのクリア（強制終了用）
    /// </summary>
    public void ClearAllDamageTexts()
    {
        foreach (var textObj in activeDamageTexts)
        {
            if (textObj != null)
            {
                DOTween.Kill(textObj);
                ReturnTextToPool(textObj);
            }
        }
        activeDamageTexts.Clear();
        Log("全ダメージテキストをクリアしました");
    }

    /// <summary>
    /// 初期化状態確認
    /// </summary>
    /// <returns>初期化済みかどうか</returns>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    #endregion

    #region ログ・デバッグ機能

    /// <summary>
    /// ログ出力
    /// </summary>
    /// <param name="message">ログメッセージ</param>
    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[DamageTextUI] {message}");
        }
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    private void LogError(string message)
    {
        Debug.LogError($"[DamageTextUI] {message}");
    }

    /// <summary>
    /// デバッグ情報出力
    /// </summary>
    [ContextMenu("デバッグ情報出力")]
    private void DumpDebugInfo()
    {
        Log("=== DamageTextUI デバッグ情報 ===");
        Log($"初期化状態: {isInitialized}");
        Log($"アクティブテキスト数: {activeDamageTexts.Count}");
        Log($"プール内テキスト数: {textPool.Count}");
        Log($"登録キャラクター数: {characterTransforms.Count}");
        Log($"戦闘カメラ存在: {battleCamera != null}");
        Log($"プレハブ存在: {damageTextPrefab != null}");

        if (BattleManager.Instance != null)
        {
            Log($"戦闘速度倍率: {BattleManager.Instance.BattleSpeedMultiplier}");
        }
    }

    #endregion
}