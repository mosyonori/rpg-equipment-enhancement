using System;
using UnityEngine;
using TMPro;

/// <summary>
/// 戦闘進行情報の表示UI
/// 責任範囲：
/// - クエスト名をタイトルとして表示
/// - 現在ターン数と最大ターン数表示
/// - BattleManagerからのイベント受信とUI更新
/// データアクセス統一ルール: UI層 → BattleManager → Data層
/// </summary>
public class BattleInfoUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI turnInfoText;

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private string defaultQuestTitle = "戦闘中";
    [SerializeField] private string defaultTurnInfo = "ターン: --/--";

    // 内部状態
    private bool isInitialized = false;
    private string currentQuestName = "";
    private int currentTurn = 0;
    private int maxTurn = 0;

    #region Unity Lifecycle

    void Start()
    {
        InitializeUI();
        SetupEventListeners();
        Log("BattleInfoUI初期化完了");
    }

    void OnDestroy()
    {
        CleanupEventListeners();
    }

    #endregion

    #region 初期化・終了処理

    /// <summary>
    /// UI初期化
    /// </summary>
    private void InitializeUI()
    {
        // UI要素の初期状態設定
        UpdateQuestTitle(defaultQuestTitle);
        UpdateTurnInfo(defaultTurnInfo);

        // コンポーネント存在確認
        if (questTitleText == null)
        {
            LogError("questTitleTextが設定されていません。Inspectorで設定してください。");
        }

        if (turnInfoText == null)
        {
            LogError("turnInfoTextが設定されていません。Inspectorで設定してください。");
        }

        isInitialized = true;
        Log("BattleInfoUI初期化処理完了");
    }

    /// <summary>
    /// イベントリスナー設定
    /// </summary>
    private void SetupEventListeners()
    {
        // BattleManagerのイベントに登録
        BattleManager.OnBattleInitialized += OnBattleInitialized;
        BattleManager.OnCharacterTurnStart += OnCharacterTurnStart;
        BattleManager.OnBattleStateChanged += OnBattleStateChanged;
        BattleManager.OnBattleCompleted += OnBattleCompleted;
        BattleManager.OnBattleError += OnBattleError;

        Log("BattleManagerイベントリスナー設定完了");
    }

    /// <summary>
    /// イベントリスナー解除
    /// </summary>
    private void CleanupEventListeners()
    {
        // BattleManagerのイベントから解除
        BattleManager.OnBattleInitialized -= OnBattleInitialized;
        BattleManager.OnCharacterTurnStart -= OnCharacterTurnStart;
        BattleManager.OnBattleStateChanged -= OnBattleStateChanged;
        BattleManager.OnBattleCompleted -= OnBattleCompleted;
        BattleManager.OnBattleError -= OnBattleError;

        Log("BattleManagerイベントリスナー解除完了");
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
            if (setupData == null)
            {
                LogError("BattleSetupDataがnullです");
                return;
            }

            // クエスト名の存在確認
            if (string.IsNullOrEmpty(setupData.questName))
            {
                LogError("questNameが空または無効です");
                currentQuestName = defaultQuestTitle;
            }
            else
            {
                currentQuestName = setupData.questName;
            }
            maxTurn = setupData.turnLimit;
            currentTurn = 0; // 初期化時はターン0

            // UI更新
            UpdateQuestTitle(currentQuestName);
            UpdateTurnDisplay();

            Log($"戦闘初期化: クエスト='{currentQuestName}', 最大ターン={maxTurn}");
        }
        catch (Exception e)
        {
            LogError($"戦闘初期化処理エラー: {e.Message}");
            // エラー時はデフォルト表示
            UpdateQuestTitle(defaultQuestTitle);
            UpdateTurnInfo(defaultTurnInfo);
        }
    }

    /// <summary>
    /// キャラクターターン開始イベントハンドラ
    /// </summary>
    /// <param name="character">行動開始キャラクター</param>
    private void OnCharacterTurnStart(BattleCharacterData character)
    {
        try
        {
            // BattleManagerから最新のターン数を取得
            if (BattleManager.Instance != null)
            {
                int latestTurn = BattleManager.Instance.GetCurrentTurnNumber();

                // ターン数が更新された場合のみUI更新
                if (latestTurn != currentTurn)
                {
                    currentTurn = latestTurn;
                    UpdateTurnDisplay();
                    Log($"ターン更新: {currentTurn}/{maxTurn} - {character?.characterName ?? "不明"}のターン");
                }
            }
        }
        catch (Exception e)
        {
            LogError($"ターン開始処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘状態変更イベントハンドラ（ログ出力のみ）
    /// </summary>
    /// <param name="newState">新しい戦闘状態</param>
    private void OnBattleStateChanged(BattleState newState)
    {
        Log($"戦闘状態変更: {newState}");

        // 状態に応じた内部処理（必要に応じて追加）
        switch (newState)
        {
            case BattleState.Idle:
                Log("戦闘待機状態");
                break;
            case BattleState.Initializing:
                Log("戦闘初期化状態");
                break;
            case BattleState.InProgress:
                Log("戦闘進行状態");
                break;
            case BattleState.Completed:
                Log("戦闘完了状態");
                break;
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
            if (resultData != null)
            {
                string resultText = resultData.isVictory ? "勝利" : "敗北";
                Log($"戦闘完了: {resultText} (最終ターン: {resultData.totalTurns})");

                // 最終ターン数を表示に反映
                currentTurn = resultData.totalTurns;
                UpdateTurnDisplay();
            }
        }
        catch (Exception e)
        {
            LogError($"戦闘完了処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘エラーイベントハンドラ
    /// </summary>
    /// <param name="errorMessage">エラーメッセージ</param>
    private void OnBattleError(string errorMessage)
    {
        LogError($"戦闘エラー受信: {errorMessage}");

        // エラー時はデフォルト表示に戻す
        UpdateQuestTitle(defaultQuestTitle);
        UpdateTurnInfo(defaultTurnInfo);
    }

    #endregion

    #region UI更新メソッド

    /// <summary>
    /// クエストタイトル更新
    /// </summary>
    /// <param name="title">表示するタイトル</param>
    private void UpdateQuestTitle(string title)
    {
        if (questTitleText != null)
        {
            questTitleText.text = title ?? defaultQuestTitle;
        }
    }

    /// <summary>
    /// ターン情報更新
    /// </summary>
    /// <param name="turnText">表示するターン情報</param>
    private void UpdateTurnInfo(string turnText)
    {
        if (turnInfoText != null)
        {
            turnInfoText.text = turnText ?? defaultTurnInfo;
        }
    }

    /// <summary>
    /// ターン表示更新（現在ターン/最大ターン形式）
    /// </summary>
    private void UpdateTurnDisplay()
    {
        string turnText = $"ターン: {currentTurn}/{maxTurn}";
        UpdateTurnInfo(turnText);
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 手動でUI更新（デバッグ用）
    /// </summary>
    public void RefreshDisplay()
    {
        if (!isInitialized)
        {
            LogError("BattleInfoUIが初期化されていません");
            return;
        }

        try
        {
            // BattleManagerから最新データを取得して更新
            if (BattleManager.Instance != null)
            {
                currentTurn = BattleManager.Instance.GetCurrentTurnNumber();
                UpdateTurnDisplay();
                Log("手動でUI更新完了");
            }
        }
        catch (Exception e)
        {
            LogError($"手動UI更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 初期化状態確認
    /// </summary>
    /// <returns>初期化済みかどうか</returns>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    /// <summary>
    /// 現在の表示情報取得（デバッグ用）
    /// </summary>
    /// <returns>現在の表示情報</returns>
    public string GetCurrentDisplayInfo()
    {
        return $"Quest: '{currentQuestName}', Turn: {currentTurn}/{maxTurn}";
    }

    #endregion

    #region エラーハンドリング・ログ

    /// <summary>
    /// データ取得失敗時のデフォルト表示設定
    /// </summary>
    private void SetDefaultDisplay()
    {
        UpdateQuestTitle(defaultQuestTitle);
        UpdateTurnInfo(defaultTurnInfo);
        LogError("データ取得に失敗したため、デフォルト表示に設定しました");
    }

    /// <summary>
    /// ログ出力
    /// </summary>
    /// <param name="message">ログメッセージ</param>
    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[BattleInfoUI] {message}");
        }
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    private void LogError(string message)
    {
        Debug.LogError($"[BattleInfoUI] {message}");
    }

    #endregion

    #region デバッグ機能

    /// <summary>
    /// デバッグ情報出力
    /// </summary>
    [ContextMenu("デバッグ情報出力")]
    private void DumpDebugInfo()
    {
        Log("=== BattleInfoUI デバッグ情報 ===");
        Log($"初期化状態: {isInitialized}");
        Log($"現在の表示: {GetCurrentDisplayInfo()}");
        Log($"questTitleText存在: {questTitleText != null}");
        Log($"turnInfoText存在: {turnInfoText != null}");
        Log($"BattleManager存在: {BattleManager.Instance != null}");

        if (BattleManager.Instance != null)
        {
            Log($"BattleManager状態: {BattleManager.Instance.CurrentState}");
            Log($"BattleManager初期化: {BattleManager.Instance.IsInitialized}");
        }
    }

    #endregion
}