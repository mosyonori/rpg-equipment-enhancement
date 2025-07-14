using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 戦闘結果の表示制御UI
/// 責任範囲：
/// - 勝利・敗北画面表示
/// - 戦闘ターン数表示
/// - ホーム画面復帰ボタン
/// - 勝利時の報酬表示（RewardUI統合）
/// データアクセス統一ルール: UI層 → BattleManager → Data層
/// </summary>
public class BattleResultUI : MonoBehaviour
{
    [Header("基本UI要素")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI turnCountText;
    [SerializeField] private TextMeshProUGUI endReasonText;
    [SerializeField] private Button homeButton;

    [Header("勝利・敗北表示設定")]
    [SerializeField] private string victoryText = "勝利！";
    [SerializeField] private string defeatText = "敗北...";
    [SerializeField] private Color victoryColor = Color.yellow;
    [SerializeField] private Color defeatColor = Color.red;

    [Header("背景・演出要素")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite victoryBackgroundSprite;
    [SerializeField] private Sprite defeatBackgroundSprite;

    [Header("報酬表示要素（勝利時のみ）")]
    [SerializeField] private GameObject rewardSection;
    [SerializeField] private TextMeshProUGUI expRewardText;
    [SerializeField] private TextMeshProUGUI goldRewardText;
    [SerializeField] private Transform dropItemListParent;
    [SerializeField] private GameObject dropItemSlotPrefab;

    [Header("ターン表示設定")]
    [SerializeField] private string turnDisplayFormat = "戦闘ターン数: {0}";

    [Header("終了理由メッセージ")]
    [SerializeField] private string victoryMessage = "敵を全て倒しました！";
    [SerializeField] private string defeatMessage = "戦闘に敗北しました...";
    [SerializeField] private string turnLimitMessage = "ターン制限に達しました...";
    [SerializeField] private string disconnectMessage = "戦闘が中断されました";

    [Header("シーン遷移設定")]
    [SerializeField] private string homeSceneName = "HomeScene";

    [Header("デバッグ設定")]
    [SerializeField] private bool enableDebugLog = true;

    // 内部状態
    private bool isInitialized = false;
    private bool isDisplaying = false;
    private BattleResultData currentResult;

    // ドロップアイテムスロット管理
    private System.Collections.Generic.List<GameObject> dropItemSlots = new System.Collections.Generic.List<GameObject>();

    #region Unity Lifecycle

    void Start()
    {
        InitializeUI();
        SetupEventListeners();
        Log("BattleResultUI初期化完了");
    }

    void OnDestroy()
    {
        CleanupEventListeners();
        CleanupDropItemSlots();
    }

    #endregion

    #region 初期化・終了処理

    /// <summary>
    /// UI初期化
    /// </summary>
    private void InitializeUI()
    {
        // コンポーネント存在確認
        if (resultPanel == null)
        {
            LogError("resultPanelが設定されていません。Inspectorで設定してください。");
        }

        if (homeButton == null)
        {
            LogError("homeButtonが設定されていません。Inspectorで設定してください。");
        }
        else
        {
            homeButton.onClick.AddListener(OnHomeButtonClicked);
        }

        // 初期状態設定（非表示）
        SetResultPanelVisible(false);

        isInitialized = true;
        Log("BattleResultUI初期化処理完了");
    }

    /// <summary>
    /// イベントリスナー設定
    /// </summary>
    private void SetupEventListeners()
    {
        // BattleManagerのイベントに登録
        BattleManager.OnBattleCompleted += OnBattleCompleted;
        BattleManager.OnBattleStateChanged += OnBattleStateChanged;
        BattleManager.OnBattleError += OnBattleError;

        Log("BattleManagerイベントリスナー設定完了");
    }

    /// <summary>
    /// イベントリスナー解除
    /// </summary>
    private void CleanupEventListeners()
    {
        // BattleManagerのイベントから解除
        BattleManager.OnBattleCompleted -= OnBattleCompleted;
        BattleManager.OnBattleStateChanged -= OnBattleStateChanged;
        BattleManager.OnBattleError -= OnBattleError;

        Log("BattleManagerイベントリスナー解除完了");
    }

    /// <summary>
    /// ドロップアイテムスロットクリーンアップ
    /// </summary>
    private void CleanupDropItemSlots()
    {
        foreach (var slot in dropItemSlots)
        {
            if (slot != null)
            {
                Destroy(slot);
            }
        }
        dropItemSlots.Clear();
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// 戦闘完了イベントハンドラ
    /// </summary>
    /// <param name="resultData">戦闘結果データ</param>
    private void OnBattleCompleted(BattleResultData resultData)
    {
        try
        {
            if (resultData == null)
            {
                LogError("BattleResultDataがnullです");
                return;
            }

            Log($"戦闘結果受信: {(resultData.isVictory ? "勝利" : "敗北")} (ターン: {resultData.totalTurns})");

            currentResult = resultData;
            DisplayBattleResult();
        }
        catch (Exception e)
        {
            LogError($"戦闘完了処理エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘状態変更イベントハンドラ
    /// </summary>
    /// <param name="newState">新しい戦闘状態</param>
    private void OnBattleStateChanged(BattleState newState)
    {
        Log($"戦闘状態変更: {newState}");

        switch (newState)
        {
            case BattleState.Idle:
            case BattleState.Initializing:
            case BattleState.InProgress:
                // 戦闘中は結果画面を非表示
                if (isDisplaying)
                {
                    HideBattleResult();
                }
                break;

            case BattleState.Completed:
                // 戦闘完了状態では結果画面を表示（OnBattleCompletedで処理）
                break;
        }
    }

    /// <summary>
    /// 戦闘エラーイベントハンドラ
    /// </summary>
    /// <param name="errorMessage">エラーメッセージ</param>
    private void OnBattleError(string errorMessage)
    {
        LogError($"戦闘エラー受信: {errorMessage}");

        // エラー時は結果画面を非表示
        if (isDisplaying)
        {
            HideBattleResult();
        }
    }

    #endregion

    #region 結果画面表示制御

    /// <summary>
    /// 戦闘結果画面表示
    /// </summary>
    private void DisplayBattleResult()
    {
        if (!isInitialized || currentResult == null)
        {
            LogError("初期化未完了または結果データが不正です");
            return;
        }

        try
        {
            // 基本情報表示
            DisplayBasicResult();

            // 勝利時のみ報酬表示
            if (currentResult.isVictory)
            {
                DisplayRewards();
            }
            else
            {
                HideRewards();
            }

            // 結果パネル表示
            SetResultPanelVisible(true);
            isDisplaying = true;

            Log("戦闘結果画面表示完了");
        }
        catch (Exception e)
        {
            LogError($"戦闘結果表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 基本結果情報表示
    /// </summary>
    private void DisplayBasicResult()
    {
        // 勝利・敗北タイトル表示
        if (resultTitleText != null)
        {
            resultTitleText.text = currentResult.isVictory ? victoryText : defeatText;
            resultTitleText.color = currentResult.isVictory ? victoryColor : defeatColor;
        }

        // ターン数表示
        if (turnCountText != null)
        {
            turnCountText.text = string.Format(turnDisplayFormat, currentResult.totalTurns);
        }

        // 終了理由表示
        if (endReasonText != null)
        {
            endReasonText.text = GetEndReasonMessage(currentResult.endReason);
        }

        // 背景画像設定
        if (backgroundImage != null)
        {
            if (currentResult.isVictory && victoryBackgroundSprite != null)
            {
                backgroundImage.sprite = victoryBackgroundSprite;
            }
            else if (!currentResult.isVictory && defeatBackgroundSprite != null)
            {
                backgroundImage.sprite = defeatBackgroundSprite;
            }
        }
    }

    /// <summary>
    /// 報酬表示（勝利時のみ）
    /// </summary>
    private void DisplayRewards()
    {
        if (rewardSection != null)
        {
            rewardSection.SetActive(true);
        }

        // 経験値表示
        if (expRewardText != null)
        {
            expRewardText.text = $"獲得経験値: {currentResult.gainedExp:N0}";
        }

        // ゴールド表示
        if (goldRewardText != null)
        {
            goldRewardText.text = $"獲得ゴールド: {currentResult.gainedGold:N0}";
        }

        // ドロップアイテム表示
        DisplayDropItems();

        Log($"報酬表示: Exp={currentResult.gainedExp}, Gold={currentResult.gainedGold}, DropItems={currentResult.dropItems?.Count ?? 0}");
    }

    /// <summary>
    /// ドロップアイテム表示
    /// </summary>
    private void DisplayDropItems()
    {
        // 既存のスロットをクリア
        CleanupDropItemSlots();

        if (currentResult.dropItems == null || currentResult.dropItems.Count == 0)
        {
            Log("ドロップアイテムはありません");
            return;
        }

        if (dropItemListParent == null || dropItemSlotPrefab == null)
        {
            LogError("ドロップアイテム表示用のUI要素が設定されていません");
            return;
        }

        // ドロップアイテムごとにスロット作成
        foreach (var dropItem in currentResult.dropItems)
        {
            try
            {
                CreateDropItemSlot(dropItem);
            }
            catch (Exception e)
            {
                LogError($"ドロップアイテムスロット作成エラー: {e.Message}");
            }
        }

        Log($"ドロップアイテム表示完了: {dropItemSlots.Count}個");
    }

    /// <summary>
    /// ドロップアイテムスロット作成
    /// </summary>
    /// <param name="dropResult">ドロップ結果</param>
    private void CreateDropItemSlot(DropResult dropResult)
    {
        var slotObject = Instantiate(dropItemSlotPrefab, dropItemListParent);
        dropItemSlots.Add(slotObject);

        // DropItemSlotUIコンポーネントがある場合は初期化
        var dropItemSlot = slotObject.GetComponent<DropItemSlotUI>();
        if (dropItemSlot != null)
        {
            // DropResultをDropItemDataに変換
            var dropItemData = ConvertDropResultToDropItemData(dropResult);
            dropItemSlot.Initialize(dropItemData);
        }
        else
        {
            // フォールバック: テキスト表示
            var textComponent = slotObject.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = $"{dropResult.itemName} x{dropResult.quantity}";
            }
        }

        Log($"ドロップアイテムスロット作成: {dropResult.itemName} x{dropResult.quantity}");
    }

    /// <summary>
    /// DropResultをDropItemDataに変換
    /// </summary>
    /// <param name="dropResult">ドロップ結果</param>
    /// <returns>変換されたDropItemData</returns>
    private DropItemData ConvertDropResultToDropItemData(DropResult dropResult)
    {
        if (dropResult == null)
        {
            LogError("DropResultがnullです");
            return null;
        }

        try
        {
            return new DropItemData
            {
                itemId = dropResult.itemId,
                itemName = dropResult.itemName,
                itemType = dropResult.itemType,
                quantity = dropResult.quantity,
                dropRate = 100 // 実際にドロップしたアイテムなので100%として表示
            };
        }
        catch (Exception e)
        {
            LogError($"DropResult変換エラー: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 報酬セクション非表示
    /// </summary>
    private void HideRewards()
    {
        if (rewardSection != null)
        {
            rewardSection.SetActive(false);
        }
        CleanupDropItemSlots();
    }

    /// <summary>
    /// 戦闘結果画面非表示
    /// </summary>
    private void HideBattleResult()
    {
        SetResultPanelVisible(false);
        isDisplaying = false;
        currentResult = null;
        CleanupDropItemSlots();
        Log("戦闘結果画面非表示");
    }

    /// <summary>
    /// 結果パネル表示制御
    /// </summary>
    /// <param name="visible">表示するかどうか</param>
    private void SetResultPanelVisible(bool visible)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(visible);
        }
    }

    #endregion

    #region ユーザー操作・ナビゲーション

    /// <summary>
    /// ホームボタンクリック処理
    /// </summary>
    private void OnHomeButtonClicked()
    {
        try
        {
            Log("ホーム画面に遷移します");

            // 戦闘データのクリーンアップ
            CleanupBattleData();

            // ホーム画面に遷移
            TransitionToHomeScene();
        }
        catch (Exception e)
        {
            LogError($"ホーム画面遷移エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 戦闘データクリーンアップ
    /// </summary>
    private void CleanupBattleData()
    {
        // 結果画面を非表示
        HideBattleResult();

        // 戦闘状態をリセット（必要に応じて）
        if (BattleManager.Instance != null && BattleManager.Instance.CurrentState != BattleState.Idle)
        {
            Log("戦闘状態をリセット");
            // BattleManagerのリセット処理があれば呼び出し
        }

        Log("戦闘データクリーンアップ完了");
    }

    /// <summary>
    /// ホーム画面への遷移
    /// </summary>
    private void TransitionToHomeScene()
    {
        try
        {
            if (string.IsNullOrEmpty(homeSceneName))
            {
                LogError("ホームシーン名が設定されていません");
                return;
            }

            Log($"シーン遷移: {homeSceneName}");
            SceneManager.LoadScene(homeSceneName);
        }
        catch (Exception e)
        {
            LogError($"シーン遷移エラー: {e.Message}");
        }
    }

    #endregion

    #region ユーティリティメソッド

    /// <summary>
    /// 終了理由メッセージ取得
    /// </summary>
    /// <param name="endReason">戦闘終了理由</param>
    /// <returns>対応するメッセージ</returns>
    private string GetEndReasonMessage(BattleEndReason endReason)
    {
        return endReason switch
        {
            BattleEndReason.Victory => victoryMessage,
            BattleEndReason.Defeat => defeatMessage,
            BattleEndReason.TurnLimit => turnLimitMessage,
            BattleEndReason.Disconnect => disconnectMessage,
            _ => "戦闘が終了しました"
        };
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 手動で戦闘結果を表示（デバッグ用）
    /// </summary>
    /// <param name="resultData">表示する結果データ</param>
    public void DisplayResult(BattleResultData resultData)
    {
        if (resultData == null)
        {
            LogError("結果データがnullです");
            return;
        }

        currentResult = resultData;
        DisplayBattleResult();
        Log("手動で戦闘結果を表示");
    }

    /// <summary>
    /// 結果画面を手動で非表示（デバッグ用）
    /// </summary>
    public void HideResult()
    {
        HideBattleResult();
        Log("手動で戦闘結果を非表示");
    }

    /// <summary>
    /// 表示状態確認
    /// </summary>
    /// <returns>表示中かどうか</returns>
    public bool IsDisplaying()
    {
        return isDisplaying;
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
    /// 現在の結果データ取得
    /// </summary>
    /// <returns>現在の結果データ</returns>
    public BattleResultData GetCurrentResult()
    {
        return currentResult;
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
            Debug.Log($"[BattleResultUI] {message}");
        }
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    private void LogError(string message)
    {
        Debug.LogError($"[BattleResultUI] {message}");
    }

    /// <summary>
    /// デバッグ情報出力
    /// </summary>
    [ContextMenu("デバッグ情報出力")]
    private void DumpDebugInfo()
    {
        Log("=== BattleResultUI デバッグ情報 ===");
        Log($"初期化状態: {isInitialized}");
        Log($"表示状態: {isDisplaying}");
        Log($"結果データ存在: {currentResult != null}");

        if (currentResult != null)
        {
            Log($"結果詳細: {(currentResult.isVictory ? "勝利" : "敗北")}, ターン: {currentResult.totalTurns}");
            Log($"報酬: Exp={currentResult.gainedExp}, Gold={currentResult.gainedGold}");
            Log($"ドロップアイテム数: {currentResult.dropItems?.Count ?? 0}");
        }

        Log($"ドロップスロット数: {dropItemSlots.Count}");
        Log($"UI要素確認:");
        Log($"  resultPanel: {resultPanel != null}");
        Log($"  homeButton: {homeButton != null}");
        Log($"  rewardSection: {rewardSection != null}");
        Log($"  dropItemListParent: {dropItemListParent != null}");
        Log($"  dropItemSlotPrefab: {dropItemSlotPrefab != null}");

        if (BattleManager.Instance != null)
        {
            Log($"BattleManager状態: {BattleManager.Instance.CurrentState}");
        }
    }

    #endregion
}