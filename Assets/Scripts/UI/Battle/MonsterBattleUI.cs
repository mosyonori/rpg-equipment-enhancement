using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// モンスター（敵キャラクター）の戦闘中表示制御
/// 責任範囲：
/// - モンスター名・displayName表示
/// - モンスター画像表示
/// - HPBarUI統合とHP表示
/// - 戦闘不能状態の視覚表現
/// - 状態効果表示制御
/// - 1体のモンスター表示に特化（複数体管理はBattleUIが全担）
/// データアクセス統一ルール: UI層指定用コンポーネント（BattleCharacterDataを受け取り表示のみ）
/// </summary>
public class MonsterBattleUI : MonoBehaviour
{
    #region デバッグ・ログ

    /// <summary>
    /// デバッグログ出力
    /// </summary>
    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterBattleUI] {message}");
        }
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    private void LogError(string message)
    {
        Debug.LogError($"[MonsterBattleUI] {message}");
    }

    /// <summary>
    /// 警告ログ出力
    /// </summary>
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[MonsterBattleUI] {message}");
    }

    #endregion

    #region フィールド

    [Header("UI設定")]
    [SerializeField] private bool enableDebugLog = true;

    [Header("モンスター基本情報")]
    [SerializeField] private TextMeshProUGUI monsterNameText;
    [SerializeField] private Image monsterIconImage;
    [SerializeField] private Image monsterPortraitImage;

    [Header("HPバー")]
    [SerializeField] private HPBarUI hpBarUI;

    [Header("状態効果表示")]
    [SerializeField] private StatusEffectUI statusEffectUI;

    [Header("戦闘不能表示")]
    [SerializeField] private GameObject deadOverlay;
    [SerializeField] private Image deadOverlayImage;
    [SerializeField] private TextMeshProUGUI deadStatusText;
    [SerializeField] private CanvasGroup monsterUICanvasGroup;

    [Header("戦闘不能設定")]
    [SerializeField] private Color deadOverlayColor = new Color(0, 0, 0, 0.7f);
    [SerializeField] private float deadAlpha = 0.3f;

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
    /// モンスターのキャラクターID
    /// </summary>
    public string CharacterId => currentCharacterData?.characterId;

    /// <summary>
    /// モンスターのインスタンスID
    /// </summary>
    public string InstanceId => currentCharacterData?.instanceId;

    /// <summary>
    /// 表示名（displayName優先、なければcharacterName）
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (currentCharacterData == null) return "";
            return !string.IsNullOrEmpty(currentCharacterData.displayName)
                ? currentCharacterData.displayName
                : currentCharacterData.characterName;
        }
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        Log("MonsterBattleUI Awake開始");
        ValidateComponents();
    }

    private void Start()
    {
        Log("MonsterBattleUI Start開始");
        InitializeMonsterUI();
    }

    private void OnDestroy()
    {
        Log("MonsterBattleUI OnDestroy開始");
        CleanupMonsterUI();
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
            if (monsterNameText == null)
            {
                LogWarning("monsterNameTextが設定されていません");
            }

            if (monsterIconImage == null && monsterPortraitImage == null)
            {
                LogWarning("モンスター画像（IconまたはPortrait）が設定されていません");
            }

            if (monsterUICanvasGroup == null)
            {
                LogWarning("monsterUICanvasGroupが設定されていません");
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
    /// モンスターUI初期化
    /// </summary>
    private void InitializeMonsterUI()
    {
        try
        {
            Log("モンスターUI初期化開始");

            // 元の色・透明度を保存
            SaveOriginalAppearance();

            // 戦闘不能オーバーレイ初期化
            InitializeDeadOverlay();

            // 状態効果UI初期化
            InitializeStatusEffectUI();

            // 初期状態設定
            SetDeadState(false);

            isInitialized = true;
            Log("モンスターUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"モンスターUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 元の外観を保存
    /// </summary>
    private void SaveOriginalAppearance()
    {
        try
        {
            if (monsterIconImage != null)
            {
                originalIconColor = monsterIconImage.color;
            }

            if (monsterPortraitImage != null)
            {
                originalPortraitColor = monsterPortraitImage.color;
            }

            if (monsterUICanvasGroup != null)
            {
                originalCanvasGroupAlpha = monsterUICanvasGroup.alpha;
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
                deadStatusText.text = "撃破";
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

    #endregion

    #region 公開メソッド - キャラクターデータ設定

    /// <summary>
    /// キャラクターデータ設定
    /// </summary>
    /// <param name="characterData">モンスターキャラクターデータ</param>
    public void SetCharacterData(BattleCharacterData characterData)
    {
        if (characterData == null)
        {
            LogError("BattleCharacterDataがnullです");
            return;
        }

        if (characterData.isPlayer)
        {
            LogError("プレイヤーキャラクターのデータが渡されました。MonsterBattleUIは敵専用です");
            return;
        }

        try
        {
            currentCharacterData = characterData;

            // UI更新
            UpdateMonsterInfo();
            UpdateHPDisplay();
            UpdateStatusEffects();
            UpdateDeadState();

            Log($"キャラクターデータ設定完了: {DisplayName} (ID: {CharacterId}, InstanceID: {InstanceId})");
        }
        catch (Exception e)
        {
            LogError($"キャラクターデータ設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// キャラクターデータから更新（BattleUIから呼び出される）
    /// </summary>
    /// <param name="characterData">更新されたモンスターキャラクターデータ</param>
    public void UpdateFromCharacterData(BattleCharacterData characterData)
    {
        if (characterData == null)
        {
            LogError("更新用BattleCharacterDataがnullです");
            return;
        }

        // 同じモンスターかチェック
        if (currentCharacterData != null &&
            currentCharacterData.characterId != characterData.characterId)
        {
            LogWarning($"異なるモンスターのデータが渡されました。現在: {currentCharacterData.characterId}, 新規: {characterData.characterId}");
        }

        try
        {
            // データ更新
            currentCharacterData = characterData;

            // HP表示更新
            UpdateHPDisplay();

            // 状態効果更新
            UpdateStatusEffects();

            // 戦闘不能状態更新
            UpdateDeadState();

            Log($"キャラクターデータ更新完了: {DisplayName}");
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
            if (monsterUICanvasGroup != null)
            {
                monsterUICanvasGroup.alpha = dead ? deadAlpha : originalCanvasGroupAlpha;
            }

            // モンスター画像の色調整
            UpdateImageColors(dead);

            Log($"戦闘不能状態設定: {dead}");
        }
        catch (Exception e)
        {
            LogError($"戦闘不能状態設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// モンスターUI表示切替
    /// </summary>
    /// <param name="visible">表示するか</param>
    public void SetVisible(bool visible)
    {
        try
        {
            gameObject.SetActive(visible);
            Log($"モンスターUI表示切替: {visible}");
        }
        catch (Exception e)
        {
            LogError($"モンスターUI表示切替エラー: {e.Message}");
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
            if (hpBarUI != null)
            {
                hpBarUI.PlayDamageFlash(damageAmount);
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
            if (hpBarUI != null)
            {
                hpBarUI.PlayHealFlash(healAmount);
            }

            Log($"回復演出再生: {healAmount}");
        }
        catch (Exception e)
        {
            LogError($"回復演出エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - 照合・検索

    /// <summary>
    /// 指定されたキャラクターIDと一致するかチェック
    /// </summary>
    /// <param name="characterId">確認するキャラクターID</param>
    /// <returns>一致するかどうか</returns>
    public bool MatchesCharacterId(string characterId)
    {
        return currentCharacterData?.characterId == characterId;
    }

    /// <summary>
    /// 指定されたインスタンスIDと一致するかチェック
    /// </summary>
    /// <param name="instanceId">確認するインスタンスID</param>
    /// <returns>一致するかどうか</returns>
    public bool MatchesInstanceId(string instanceId)
    {
        return currentCharacterData?.instanceId == instanceId;
    }

    /// <summary>
    /// 指定されたBattleCharacterDataと同じモンスターかチェック
    /// </summary>
    /// <param name="characterData">確認するキャラクターデータ</param>
    /// <returns>同じモンスターかどうか</returns>
    public bool MatchesCharacterData(BattleCharacterData characterData)
    {
        if (characterData == null || currentCharacterData == null) return false;

        // characterIdで比較（instanceIdがある場合はそちらも確認）
        bool idMatch = currentCharacterData.characterId == characterData.characterId;

        if (!string.IsNullOrEmpty(currentCharacterData.instanceId) &&
            !string.IsNullOrEmpty(characterData.instanceId))
        {
            return idMatch && currentCharacterData.instanceId == characterData.instanceId;
        }

        return idMatch;
    }

    #endregion

    #region 内部メソッド - 表示更新

    /// <summary>
    /// モンスター情報更新
    /// </summary>
    private void UpdateMonsterInfo()
    {
        try
        {
            if (currentCharacterData == null) return;

            // 名前表示更新（displayName優先）
            if (monsterNameText != null)
            {
                monsterNameText.text = DisplayName;
            }

            // アイコン・ポートレート更新は将来実装
            // if (monsterIconImage != null)
            // {
            //     monsterIconImage.sprite = GetMonsterIcon(currentCharacterData);
            // }

            Log("モンスター情報更新完了");
        }
        catch (Exception e)
        {
            LogError($"モンスター情報更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// HP表示更新
    /// </summary>
    private void UpdateHPDisplay()
    {
        try
        {
            if (hpBarUI != null && currentCharacterData != null)
            {
                hpBarUI.UpdateFromCharacterData(currentCharacterData);
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
            float grayScale = isDead ? 0.3f : 1.0f; // モンスターはより暗く

            if (monsterIconImage != null)
            {
                Color iconColor = isDead ?
                    new Color(originalIconColor.r * grayScale, originalIconColor.g * grayScale, originalIconColor.b * grayScale, originalIconColor.a) :
                    originalIconColor;
                monsterIconImage.color = iconColor;
            }

            if (monsterPortraitImage != null)
            {
                Color portraitColor = isDead ?
                    new Color(originalPortraitColor.r * grayScale, originalPortraitColor.g * grayScale, originalPortraitColor.b * grayScale, originalPortraitColor.a) :
                    originalPortraitColor;
                monsterPortraitImage.color = portraitColor;
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
    /// モンスターUIクリーンアップ
    /// </summary>
    private void CleanupMonsterUI()
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

            Log("モンスターUIクリーンアップ完了");
        }
        catch (Exception e)
        {
            LogError($"モンスターUIクリーンアップエラー: {e.Message}");
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
        Log("=== MonsterBattleUI状態情報 ===");
        Log($"初期化完了: {isInitialized}");
        Log($"戦闘不能状態: {isDead}");

        if (currentCharacterData != null)
        {
            Log($"キャラクターID: {currentCharacterData.characterId}");
            Log($"インスタンスID: {currentCharacterData.instanceId}");
            Log($"キャラクター名: {currentCharacterData.characterName}");
            Log($"表示名: {currentCharacterData.displayName}");
            Log($"表示名（自動選択）: {DisplayName}");
            Log($"HP: {currentCharacterData.currentHp}/{currentCharacterData.maxHp}");
            Log($"生存状態: {currentCharacterData.isAlive}");
            Log($"プレイヤーフラグ: {currentCharacterData.isPlayer}");
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

        Log("=================================");
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
        int testDamage = UnityEngine.Random.Range(15, 60);
        Log($"デバッグ：ダメージ演出テスト実行 ({testDamage}ダメージ)");
        PlayDamageEffect(testDamage);
    }

    /// <summary>
    /// デバッグ用：回復演出テスト
    /// </summary>
    [ContextMenu("デバッグ：回復演出テスト")]
    public void DebugTestHealEffect()
    {
        int testHeal = UnityEngine.Random.Range(10, 30);
        Log($"デバッグ：回復演出テスト実行 ({testHeal}回復)");
        PlayHealEffect(testHeal);
    }

    /// <summary>
    /// デバッグ用：テスト用モンスターデータ設定
    /// </summary>
    [ContextMenu("デバッグ：テストモンスター設定")]
    public void DebugSetTestMonster()
    {
        Log("デバッグ：テストモンスター設定実行");

        var testMonster = new BattleCharacterData
        {
            characterId = "test_monster_001",
            instanceId = "test_instance_001",
            characterName = "テストスライム",
            displayName = "テストスライム(強)",
            isPlayer = false,
            isAlive = true,
            currentHp = 60,
            maxHp = 80,
            statusEffects = new System.Collections.Generic.List<StatusEffectData>
            {
                new StatusEffectData
                {
                    effectId = 1,
                    effectName = "攻撃力低下",
                    remainingTurns = 2,
                    displayPriority = 100,
                    colorCode = "#4169e1",
                    offenseMultiplier = 0.7f,
                    isPositive = false
                },
                new StatusEffectData
                {
                    effectId = 7,
                    effectName = "毒",
                    remainingTurns = 3,
                    displayPriority = 100,
                    colorCode = "#4169e1",
                    turnStartDamagePercent = 5,
                    isPositive = false
                }
            }
        };

        SetCharacterData(testMonster);
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
        Log($"monsterNameText: {(monsterNameText != null ? "接続済み" : "未接続")}");
        Log($"monsterIconImage: {(monsterIconImage != null ? "接続済み" : "未接続")}");
        Log($"monsterPortraitImage: {(monsterPortraitImage != null ? "接続済み" : "未接続")}");
        Log($"hpBarUI: {(hpBarUI != null ? "接続済み" : "未接続")}");
        Log($"statusEffectUI: {(statusEffectUI != null ? "接続済み" : "未接続")}");
        Log($"monsterUICanvasGroup: {(monsterUICanvasGroup != null ? "接続済み" : "未接続")}");
        Log($"deadOverlay: {(deadOverlay != null ? "接続済み" : "未接続")}");
        Log("=============================");
    }

    /// <summary>
    /// デバッグ用：ID照合テスト
    /// </summary>
    [ContextMenu("デバッグ：ID照合テスト")]
    public void DebugTestIdMatching()
    {
        if (currentCharacterData == null)
        {
            Log("デバッグ：キャラクターデータが設定されていません");
            return;
        }

        Log("=== ID照合テスト ===");
        Log($"CharacterIDマッチ: {MatchesCharacterId(currentCharacterData.characterId)}");
        Log($"InstanceIDマッチ: {MatchesInstanceId(currentCharacterData.instanceId)}");
        Log($"CharacterDataマッチ: {MatchesCharacterData(currentCharacterData)}");
        Log($"間違いIDテスト: {MatchesCharacterId("wrong_id")}");
        Log("==================");
    }

    #endregion
}