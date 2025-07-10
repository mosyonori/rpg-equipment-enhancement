using System;
using UnityEngine;
using TMPro;

/// <summary>
/// 戦闘情報表示UI（スリム化版）
/// 役割：クエストタイトル表示、現在ターン数表示のみ
/// データアクセス統一ルール: UI層 → Manager層 → データ層
/// </summary>
public class BattleInfoUI : MonoBehaviour
{
    [Header("クエスト情報表示")]
    [SerializeField] private TextMeshProUGUI questTitleText;

    [Header("ターン情報表示")]
    [SerializeField] private TextMeshProUGUI currentTurnText;
    [SerializeField] private TextMeshProUGUI maxTurnText;

    [Header("テキストフォーマット")]
    [SerializeField] private string turnFormat = "ターン: {0}";
    [SerializeField] private string maxTurnFormat = "/ {0}";

    // イベント
    public static event Action<int> OnTurnChanged;

    // 内部状態
    private bool isInitialized = false;
    private int currentTurn = 1;
    private int maxTurn = -1; // -1は無制限
    private string questTitle = "";

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateComponents();
    }

    private void Start()
    {
        Initialize();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// 戦闘情報UI初期化
    /// </summary>
    public void Initialize()
    {
        try
        {
            Log("BattleInfoUI初期化開始");

            // 初期表示更新
            UpdateQuestTitleDisplay();
            UpdateTurnDisplay();

            isInitialized = true;
            Log("BattleInfoUI初期化完了");
        }
        catch (Exception e)
        {
            LogError($"BattleInfoUI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// コンポーネント検証
    /// </summary>
    private void ValidateComponents()
    {
        if (questTitleText == null)
            LogWarning("questTitleTextが設定されていません");

        if (currentTurnText == null)
            LogWarning("currentTurnTextが設定されていません");
    }

    #endregion

    #region 公開メソッド - イベントハンドラ

    /// <summary>
    /// 戦闘開始時の処理
    /// </summary>
    public void OnBattleStart(BattleSetupData setupData)
    {
        if (!isInitialized) Initialize();

        try
        {
            Log("戦闘開始 - 戦闘情報UI初期化");

            // 戦闘設定からデータ設定
            // BattleManagerから現在の戦闘セットアップデータを取得してクエスト名を設定
            if (BattleManager.Instance != null)
            {
                var currentSetup = BattleManager.Instance.GetCurrentBattleSetup();
                if (currentSetup != null)
                {
                    // questIdからQuestDataManagerを経由してクエスト名を取得
                    var questMasterData = QuestDataManager.Instance?.GetQuestData(currentSetup.questId);
                    questTitle = questMasterData?.questName ?? "クエスト";
                }
                else
                {
                    questTitle = "クエスト";
                }
            }
            else
            {
                questTitle = "クエスト";
            }

            maxTurn = setupData.turnLimit;
            currentTurn = 1;

            // 表示更新
            UpdateQuestTitleDisplay();
            UpdateTurnDisplay();

            Log($"戦闘情報初期化完了: クエスト「{questTitle}」最大ターン{(maxTurn > 0 ? maxTurn.ToString() : "無制限")}");
        }
        catch (Exception e)
        {
            LogError($"戦闘開始処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ターン開始時の処理
    /// </summary>
    public void OnTurnStart(BattleCharacterData character)
    {
        try
        {
            // プレイヤーのターン時のみターン数を更新
            if (character.isPlayer)
            {
                UpdateTurnNumber();
            }

            Log($"ターン開始処理: {character.characterName} (ターン{currentTurn})");
        }
        catch (Exception e)
        {
            LogError($"ターン開始処理エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド - 状態更新

    /// <summary>
    /// クエストタイトル設定
    /// </summary>
    public void SetQuestTitle(string title)
    {
        try
        {
            questTitle = title ?? "クエスト";
            UpdateQuestTitleDisplay();
            Log($"クエストタイトル設定: {questTitle}");
        }
        catch (Exception e)
        {
            LogError($"クエストタイトル設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// ターン数強制更新
    /// </summary>
    public void SetTurnNumber(int turn)
    {
        try
        {
            currentTurn = Math.Max(1, turn);
            UpdateTurnDisplay();
            OnTurnChanged?.Invoke(currentTurn);

            Log($"ターン数設定: {currentTurn}");
        }
        catch (Exception e)
        {
            LogError($"ターン数設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 最大ターン数設定
    /// </summary>
    public void SetMaxTurn(int maxTurnCount)
    {
        try
        {
            maxTurn = maxTurnCount;
            UpdateTurnDisplay();

            Log($"最大ターン数設定: {(maxTurn > 0 ? maxTurn.ToString() : "無制限")}");
        }
        catch (Exception e)
        {
            LogError($"最大ターン数設定エラー: {e.Message}");
        }
    }

    #endregion

    #region 内部メソッド - ターン管理

    /// <summary>
    /// ターン数更新
    /// </summary>
    private void UpdateTurnNumber()
    {
        currentTurn++;
        UpdateTurnDisplay();
        OnTurnChanged?.Invoke(currentTurn);
    }

    /// <summary>
    /// ターン表示更新
    /// </summary>
    private void UpdateTurnDisplay()
    {
        try
        {
            if (currentTurnText != null)
                currentTurnText.text = string.Format(turnFormat, currentTurn);

            if (maxTurnText != null && maxTurn > 0)
                maxTurnText.text = string.Format(maxTurnFormat, maxTurn);
            else if (maxTurnText != null)
                maxTurnText.text = ""; // 無制限の場合は空文字
        }
        catch (Exception e)
        {
            LogError($"ターン表示更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// クエストタイトル表示更新
    /// </summary>
    private void UpdateQuestTitleDisplay()
    {
        try
        {
            if (questTitleText != null)
                questTitleText.text = questTitle;
        }
        catch (Exception e)
        {
            LogError($"クエストタイトル表示更新エラー: {e.Message}");
        }
    }

    #endregion

    #region ゲッター

    /// <summary>
    /// 現在のターン数取得
    /// </summary>
    public int GetCurrentTurn()
    {
        return currentTurn;
    }

    /// <summary>
    /// 最大ターン数取得
    /// </summary>
    public int GetMaxTurn()
    {
        return maxTurn;
    }

    /// <summary>
    /// クエストタイトル取得
    /// </summary>
    public string GetQuestTitle()
    {
        return questTitle;
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        Debug.Log($"[BattleInfoUI] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[BattleInfoUI] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[BattleInfoUI] {message}");
    }

    #endregion

    #region エディタ用ツール

#if UNITY_EDITOR
    [ContextMenu("ターン数テスト（5ターン目）")]
    private void TestSetTurn5()
    {
        SetTurnNumber(5);
        Log("ターン数を5に設定");
    }

    [ContextMenu("最大ターン設定テスト")]
    private void TestSetMaxTurn()
    {
        SetMaxTurn(10);
        Log("最大ターンを10に設定");
    }

    [ContextMenu("クエストタイトル設定テスト")]
    private void TestSetQuestTitle()
    {
        SetQuestTitle("テストクエスト");
        Log("クエストタイトルをテスト用に設定");
    }
#endif

    #endregion
}