using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// プレイヤーキャラクターの戦闘中表示制御
/// 責任範囲：
/// - プレイヤー名・画像表示
/// - HPBarUI統合とHP表示
/// - 戦闘不能状態の視覚表現
/// - 状態効果表示制御
/// データアクセス統一ルール: UI層指定用コンポーネント（BattleCharacterDataを受け取り表示のみ）
/// </summary>
public class PlayerBattleUI : MonoBehaviour
{
    #region デバッグ・ログ

    /// <summary>
    /// デバッグログ出力
    /// </summary>
    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[PlayerBattleUI] {message}");
        }
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    private void LogError(string message)
    {
        Debug.LogError($"[PlayerBattleUI] {message}");
    }

    /// <summary>
    /// 警告ログ出力
    /// </summary>
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[PlayerBattleUI] {message}");
    }

    #endregion

    #region フィールド

    [Header("UI設定")]
    [SerializeField] private bool enableDebugLog = true;

    [Header("プレイヤー基本情報")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image playerIconImage;
    [SerializeField] private Image playerPortraitImage;

    [Header("HPバー")]
    [SerializeField] private HPBarUI hpBarUI;

    [Header("状態効果表示")]
    [SerializeField] private StatusEffectUI statusEffectUI;

    [Header("戦闘不能表示")]
    [SerializeField] private GameObject deadOverlay;
    [SerializeField] private Image deadOverlayImage;
    [SerializeField] private TextMeshProUGUI deadStatusText;
    [SerializeField] private CanvasGroup playerUICanvasGroup;

    [Header("戦闘不能設定")]
    [SerializeField] private Color deadOverlayColor = new Color(0, 0, 0, 0.7f);
    [SerializeField] private float deadAlpha = 0.5f;

    // 現在の状態
    private BattleCharacterData currentCharacterData;
    private bool isInitialized = false;
    private bool isDead = false;

    // 元の色・透明度保存
    private Color originalIconColor;
    private Color originalPortraitColor;
    private float originalCanvasGroupAlpha;

    #endregion

    #region プロパティ

    /// <summary>
    /// 初期化完了状態
    /// </summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// 現在のキャラクターデータ
    /// </summary>
    public BattleCharacterData CurrentCharacterData => currentCharacterData;

    /// <summary>
    /// 戦闘不能状態
    /// </summary>
    public bool IsDead => isDead;

    /// <summary>
    /// HPBarUIコンポーネントへの参照（デバッグ・テスト用）
    /// </summary>
    public HPBarUI HPBarUI => hpBarUI;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        Log("PlayerBattleUI Awake開始");
        ValidateComponents();
    }

    private void Start()
    {
        Log("PlayerBattleUI Start開始");
        InitializePlayerUI();
    }

    private void OnDestroy()
    {
        Log("PlayerBattleUI OnDestroy開始");
        CleanupPlayerUI();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネント検証
    /// </summary>
    private void ValidateComponents()
    {
        try
        {
            // 必須コンポーネント確認
            if (hpBarUI == null)
            {
                LogError("hpBarUIが設定されていません");
                return;
            }

            // オプショナルコンポーネント確認
            if (playerNameText == null)
            {
                LogWarning("playerNameTextが設定されていません");
            }

            if (playerIconImage == null && playerPortraitImage == null)
            {
                LogWarning("プレイヤー画像（IconまたはPortrait）が設定されていません");
            }

            if (playerUICanvasGroup == null)
            {
                LogWarning("playerUICanvasGroupが設定されていません");
            }

            if (statusEffectUI == null)
            {
                LogWarning("statusEffectUIが設定されていません（状態効果表示が無効になります）");
            }

            Log("コンポーネント検証完了");
        }
        catch (Exception e)
        {
            LogError($"コンポーネント検証エラー: {e.Message}");
        }
    }

    /// <summary>
    /// プレイヤーUI初期化
    /// </summary>
    private void InitializePlayerUI()
    {
        try
        {
            Log("プレイヤーUI初期化開始");

            // 元の色・透明度を保存
            SaveOriginalAppearance();

            // 戦闘不能オーバーレイ初期化
            InitializeDeadOverlay();

            // 状態効果UI初期化
            InitializeStatusEffectUI();

            // HPBarUIの有効化を確実にする
            EnsureHPBarUIActive();

            // 初期状態設定
            SetDeadState(false);

            isInitialized = true;
            Log("プレイヤーUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"プレイヤーUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 元の外観を保存
    /// </summary>
    private void SaveOriginalAppearance()
    {
        try
        {
            if (playerIconImage != null)
            {
                originalIconColor = playerIconImage.color;
            }

            if (playerPortraitImage != null)
            {
                originalPortraitColor = playerPortraitImage.color;
            }

            if (playerUICanvasGroup != null)
            {
                originalCanvasGroupAlpha = playerUICanvasGroup.alpha;
            }

            Log("元の外観保存完了");
        }
        catch (Exception e)
        {
            LogError($"元の外観保存エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘不能オーバーレイ初期化
    /// </summary>
    private void InitializeDeadOverlay()
    {
        try
        {
            if (deadOverlay != null)
            {
                deadOverlay.SetActive(false);
            }

            if (deadOverlayImage != null)
            {
                deadOverlayImage.color = deadOverlayColor;
            }

            if (deadStatusText != null)
            {
                deadStatusText.text = "戦闘不能";
            }

            Log("戦闘不能オーバーレイ初期化完了");
        }
        catch (Exception e)
        {
            LogError($"戦闘不能オーバーレイ初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 状態効果UI初期化
    /// </summary>
    private void InitializeStatusEffectUI()
    {
        try
        {
            if (statusEffectUI != null)
            {
                // 状態効果UIの初期状態は空で設定
                statusEffectUI.ClearStatusEffects();
                Log("状態効果UI初期化完了");
            }
        }
        catch (Exception e)
        {
            LogError($"状態効果UI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: HPBarUIが確実にアクティブになるようにする
    /// </summary>
    private void EnsureHPBarUIActive()
    {
        try
        {
            if (hpBarUI == null)
            {
                LogError("hpBarUIがnullです");
                return;
            }

            // HPBarUI自体をアクティブ化
            if (!hpBarUI.gameObject.activeInHierarchy)
            {
                Log("HPBarUIが非アクティブのため、有効化します");
                hpBarUI.gameObject.SetActive(true);
            }

            // HPBarUIのコンポーネントも有効化
            if (!hpBarUI.enabled)
            {
                Log("HPBarUIコンポーネントが無効のため、有効化します");
                hpBarUI.enabled = true;
            }

            // 親オブジェクトも確認して有効化
            Transform current = hpBarUI.transform.parent;
            while (current != null && current != this.transform)
            {
                if (!current.gameObject.activeInHierarchy)
                {
                    Log($"HPBarUIの親オブジェクト({current.name})を有効化します");
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }

            // 自分自身も確認
            if (!gameObject.activeInHierarchy)
            {
                Log("PlayerBattleUI自体が非アクティブのため、有効化します");
                gameObject.SetActive(true);
            }

            Log("HPBarUI有効化確認完了");
        }
        catch (Exception e)
        {
            LogError($"HPBarUI有効化確認エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - キャラクターデータ設定

    /// <summary>
    /// キャラクターデータ設定
    /// </summary>
    /// <param name="characterData">プレイヤーキャラクターデータ</param>
    public void SetCharacterData(BattleCharacterData characterData)
    {
        if (characterData == null)
        {
            LogError("BattleCharacterDataがnullです");
            return;
        }

        if (!characterData.isPlayer)
        {
            LogError("プレイヤーキャラクター以外のデータが渡されました");
            return;
        }

        try
        {
            currentCharacterData = characterData;

            // 修正: HPBarUIの状態を確実にチェック
            EnsureHPBarUIActive();

            // UI更新
            UpdatePlayerInfo();
            UpdateHPDisplay();
            UpdateStatusEffects();
            UpdateDeadState();

            Log($"キャラクターデータ設定完了: {characterData.characterName}");
        }
        catch (Exception e)
        {
            LogError($"キャラクターデータ設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// キャラクターデータから更新（BattleUIから呼び出される）
    /// </summary>
    /// <param name="characterData">更新されたプレイヤーキャラクターデータ</param>
    public void UpdateFromCharacterData(BattleCharacterData characterData)
    {
        if (characterData == null)
        {
            LogError("更新用BattleCharacterDataがnullです");
            return;
        }

        try
        {
            // データ更新
            currentCharacterData = characterData;

            // 修正: HP更新前にHPBarUIの状態確認
            EnsureHPBarUIActive();

            // HP表示更新
            UpdateHPDisplay();

            // 状態効果更新
            UpdateStatusEffects();

            // 戦闘不能状態更新
            UpdateDeadState();

            Log($"キャラクターデータ更新完了: {characterData.characterName}");
        }
        catch (Exception e)
        {
            LogError($"キャラクターデータ更新エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - 表示制御

    /// <summary>
    /// 戦闘不能状態設定
    /// </summary>
    /// <param name="dead">戦闘不能状態か</param>
    public void SetDeadState(bool dead)
    {
        try
        {
            isDead = dead;

            // 戦闘不能オーバーレイ表示制御
            if (deadOverlay != null)
            {
                deadOverlay.SetActive(dead);
            }

            // CanvasGroup透明度制御
            if (playerUICanvasGroup != null)
            {
                playerUICanvasGroup.alpha = dead ? deadAlpha : originalCanvasGroupAlpha;
            }

            // プレイヤー画像の色調整
            UpdateImageColors(dead);

            Log($"戦闘不能状態設定: {dead}");
        }
        catch (Exception e)
        {
            LogError($"戦闘不能状態設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// プレイヤーUI表示切替
    /// </summary>
    /// <param name="visible">表示するか</param>
    public void SetVisible(bool visible)
    {
        try
        {
            gameObject.SetActive(visible);
            Log($"プレイヤーUI表示切替: {visible}");
        }
        catch (Exception e)
        {
            LogError($"プレイヤーUI表示切替エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - 演出

    /// <summary>
    /// ダメージ演出
    /// </summary>
    /// <param name="damageAmount">ダメージ量</param>
    public void PlayDamageEffect(int damageAmount)
    {
        try
        {
            // 修正: HPBarUIの有効性確認
            if (hpBarUI != null && hpBarUI.gameObject.activeInHierarchy)
            {
                hpBarUI.PlayDamageFlash(damageAmount);
            }
            else
            {
                LogWarning("HPBarUIが非アクティブのため、ダメージ演出をスキップします");
            }

            Log($"ダメージ演出再生: {damageAmount}");
        }
        catch (Exception e)
        {
            LogError($"ダメージ演出エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 回復演出
    /// </summary>
    /// <param name="healAmount">回復量</param>
    public void PlayHealEffect(int healAmount)
    {
        try
        {
            // 修正: HPBarUIの有効性確認
            if (hpBarUI != null && hpBarUI.gameObject.activeInHierarchy)
            {
                hpBarUI.PlayHealFlash(healAmount);
            }
            else
            {
                LogWarning("HPBarUIが非アクティブのため、回復演出をスキップします");
            }

            Log($"回復演出再生: {healAmount}");
        }
        catch (Exception e)
        {
            LogError($"回復演出エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - 表示更新

    /// <summary>
    /// プレイヤー情報更新
    /// </summary>
    private void UpdatePlayerInfo()
    {
        try
        {
            if (currentCharacterData == null) return;

            // 名前表示更新
            if (playerNameText != null)
            {
                playerNameText.text = currentCharacterData.characterName;
            }

            // アイコン・ポートレート更新は将来実装
            // if (playerIconImage != null)
            // {
            //     playerIconImage.sprite = GetPlayerIcon(currentCharacterData);
            // }

            Log("プレイヤー情報更新完了");
        }
        catch (Exception e)
        {
            LogError($"プレイヤー情報更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 修正: HP表示更新
    /// </summary>
    private void UpdateHPDisplay()
    {
        try
        {
            if (currentCharacterData == null) return;

            if (hpBarUI != null)
            {
                // HPBarUIの状態を再確認
                if (!hpBarUI.gameObject.activeInHierarchy)
                {
                    LogWarning("HP更新時にHPBarUIが非アクティブでした。再度有効化を試行します");
                    EnsureHPBarUIActive();
                }

                // HPBarUIが有効な場合のみ更新
                if (hpBarUI.gameObject.activeInHierarchy)
                {
                    hpBarUI.UpdateFromCharacterData(currentCharacterData);
                    Log($"HP表示更新完了: {currentCharacterData.currentHp}/{currentCharacterData.maxHp}");
                }
                else
                {
                    LogError("HPBarUIの有効化に失敗しました");
                }
            }
            else
            {
                LogError("hpBarUIがnullです");
            }
        }
        catch (Exception e)
        {
            LogError($"HP表示更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 状態効果更新
    /// </summary>
    private void UpdateStatusEffects()
    {
        try
        {
            if (statusEffectUI != null && currentCharacterData != null)
            {
                statusEffectUI.UpdateFromCharacterData(currentCharacterData);
                Log($"状態効果更新: {currentCharacterData.statusEffects?.Count ?? 0}個の効果");
            }
        }
        catch (Exception e)
        {
            LogError($"状態効果更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘不能状態更新
    /// </summary>
    private void UpdateDeadState()
    {
        try
        {
            if (currentCharacterData != null)
            {
                bool shouldBeDead = !currentCharacterData.isAlive || currentCharacterData.currentHp <= 0;

                if (isDead != shouldBeDead)
                {
                    SetDeadState(shouldBeDead);
                }
            }
        }
        catch (Exception e)
        {
            LogError($"戦闘不能状態更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 画像色更新（戦闘不能時のグレーアウト）
    /// </summary>
    private void UpdateImageColors(bool isDead)
    {
        try
        {
            float grayScale = isDead ? 0.5f : 1.0f;

            if (playerIconImage != null)
            {
                Color iconColor = isDead ?
                    new Color(originalIconColor.r * grayScale, originalIconColor.g * grayScale, originalIconColor.b * grayScale, originalIconColor.a) :
                    originalIconColor;
                playerIconImage.color = iconColor;
            }

            if (playerPortraitImage != null)
            {
                Color portraitColor = isDead ?
                    new Color(originalPortraitColor.r * grayScale, originalPortraitColor.g * grayScale, originalPortraitColor.b * grayScale, originalPortraitColor.a) :
                    originalPortraitColor;
                playerPortraitImage.color = portraitColor;
            }
        }
        catch (Exception e)
        {
            LogError($"画像色更新エラー: {e.Message}");
        }
    }

    #endregion

    #region クリーンアップ

    /// <summary>
    /// プレイヤーUIクリーンアップ
    /// </summary>
    private void CleanupPlayerUI()
    {
        try
        {
            // 状態効果UIクリア
            if (statusEffectUI != null)
            {
                statusEffectUI.ClearStatusEffects();
            }

            // 現在の状態をリセット
            currentCharacterData = null;
            isDead = false;

            Log("プレイヤーUIクリーンアップ完了");
        }
        catch (Exception e)
        {
            LogError($"プレイヤーUIクリーンアップエラー: {e.Message}");
        }
    }

    #endregion

    #region デバッグ用公開メソッド

    /// <summary>
    /// デバッグ用：現在の状態情報を出力
    /// </summary>
    [ContextMenu("デバッグ：状態情報出力")]
    public void DebugDumpState()
    {
        Log("=== PlayerBattleUI状態情報 ===");
        Log($"初期化完了: {isInitialized}");
        Log($"戦闘不能状態: {isDead}");
        Log($"GameObjectアクティブ: {gameObject.activeInHierarchy}");
        Log($"HPBarUIアクティブ: {(hpBarUI != null ? hpBarUI.gameObject.activeInHierarchy.ToString() : "null")}");

        if (currentCharacterData != null)
        {
            Log($"キャラクター名: {currentCharacterData.characterName}");
            Log($"HP: {currentCharacterData.currentHp}/{currentCharacterData.maxHp}");
            Log($"生存状態: {currentCharacterData.isAlive}");
            Log($"状態効果数: {currentCharacterData.statusEffects?.Count ?? 0}");

            if (currentCharacterData.statusEffects != null && currentCharacterData.statusEffects.Count > 0)
            {
                Log("状態効果詳細:");
                foreach (var effect in currentCharacterData.statusEffects)
                {
                    Log($"  - {effect.effectName} (残り{effect.remainingTurns}ターン)");
                }
            }
        }
        else
        {
            Log("キャラクターデータ: なし");
        }

        Log("==============================");
    }

    /// <summary>
    /// デバッグ用：HPBarUI有効化テスト
    /// </summary>
    [ContextMenu("デバッグ：HPBarUI有効化テスト")]
    public void DebugEnsureHPBarUIActive()
    {
        Log("デバッグ：HPBarUI有効化テスト実行");
        EnsureHPBarUIActive();
    }

    /// <summary>
    /// デバッグ用：戦闘不能状態テスト
    /// </summary>
    [ContextMenu("デバッグ：戦闘不能状態テスト")]
    public void DebugTestDeadState()
    {
        Log("デバッグ：戦闘不能状態テスト実行");
        SetDeadState(!isDead);
    }

    /// <summary>
    /// デバッグ用：ダメージ演出テスト
    /// </summary>
    [ContextMenu("デバッグ：ダメージ演出テスト")]
    public void DebugTestDamageEffect()
    {
        int testDamage = UnityEngine.Random.Range(10, 50);
        Log($"デバッグ：ダメージ演出テスト実行 ({testDamage}ダメージ)");
        PlayDamageEffect(testDamage);
    }

    /// <summary>
    /// デバッグ用：回復演出テスト
    /// </summary>
    [ContextMenu("デバッグ：回復演出テスト")]
    public void DebugTestHealEffect()
    {
        int testHeal = UnityEngine.Random.Range(5, 25);
        Log($"デバッグ：回復演出テスト実行 ({testHeal}回復)");
        PlayHealEffect(testHeal);
    }

    /// <summary>
    /// デバッグ用：テスト用キャラクターデータ設定
    /// </summary>
    [ContextMenu("デバッグ：テストキャラクター設定")]
    public void DebugSetTestCharacter()
    {
        Log("デバッグ：テストキャラクター設定実行");

        var testCharacter = new BattleCharacterData
        {
            characterId = "test_player",
            characterName = "テストプレイヤー",
            isPlayer = true,
            isAlive = true,
            currentHp = 80,
            maxHp = 100,
            statusEffects = new System.Collections.Generic.List<StatusEffectData>
            {
                new StatusEffectData
                {
                    effectId = 3,
                    effectName = "攻撃力上昇",
                    remainingTurns = 3,
                    displayPriority = 100,
                    colorCode = "#ff6347",
                    offenseMultiplier = 1.5f,
                    isPositive = true
                },
                new StatusEffectData
                {
                    effectId = 8,
                    effectName = "持続回復",
                    remainingTurns = 5,
                    displayPriority = 100,
                    colorCode = "#ff6347",
                    turnStartHealPercent = 10,
                    isPositive = true
                }
            }
        };

        SetCharacterData(testCharacter);
    }

    /// <summary>
    /// デバッグ用：状態効果テスト
    /// </summary>
    [ContextMenu("デバッグ：状態効果表示テスト")]
    public void DebugTestStatusEffects()
    {
        Log("デバッグ：状態効果表示テスト実行");

        if (statusEffectUI != null)
        {
            // StatusEffectUIのデバッグメソッドを呼び出し
            statusEffectUI.DebugAddTestEffects();
        }
        else
        {
            LogWarning("StatusEffectUIが設定されていないため、テストを実行できません");
        }
    }

    /// <summary>
    /// デバッグ用：コンポーネント接続確認
    /// </summary>
    [ContextMenu("デバッグ：コンポーネント接続確認")]
    public void DebugCheckComponents()
    {
        Log("=== コンポーネント接続確認 ===");
        Log($"playerNameText: {(playerNameText != null ? "接続済み" : "未接続")}");
        Log($"playerIconImage: {(playerIconImage != null ? "接続済み" : "未接続")}");
        Log($"playerPortraitImage: {(playerPortraitImage != null ? "接続済み" : "未接続")}");
        Log($"hpBarUI: {(hpBarUI != null ? "接続済み" : "未接続")}");
        Log($"statusEffectUI: {(statusEffectUI != null ? "接続済み" : "未接続")}");
        Log($"playerUICanvasGroup: {(playerUICanvasGroup != null ? "接続済み" : "未接続")}");
        Log($"deadOverlay: {(deadOverlay != null ? "接続済み" : "未接続")}");
        Log("=============================");
    }

    #endregion
}