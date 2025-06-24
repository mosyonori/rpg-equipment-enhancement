using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 装備強化画面のメイン制御クラス - 全Controller統合版
/// 各UI Controllerの統合、Service層との連携、選択状態管理を全般
/// 
/// ✅ Phase 2完了版：データアクセス統一・エラーハンドリング強化
/// </summary>
public class EnhanceUIController : MonoBehaviour
{
    [Header("UI Controllers")]
    [SerializeField] private Enhance_EquipmentSelectUIController equipmentSelectUI;
    [SerializeField] private Enhance_EnhanceItemSelectUIController enhanceItemSelectUI;
    [SerializeField] private Enhance_SupportItemSelectUIController supportItemSelectUI;
    [SerializeField] private Enhance_StatusDisplayController statusDisplayUI;
    [SerializeField] private Enhance_ResultUIController resultUI;

    [Header("UI Elements")]
    [SerializeField] private Button enhanceExecuteButton;
    [SerializeField] private TextMeshProUGUI successRateText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Button homeButton;

    [Header("Colors")]
    [SerializeField] private Color enabledButtonColor = Color.white;
    [SerializeField] private Color disabledButtonColor = Color.gray;
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color warningTextColor = Color.red;
    [SerializeField] private Color instructionTextColor = Color.yellow;

    // Service層（完成済み）
    private EquipmentEnhanceService enhanceService = new EquipmentEnhanceService();
    private SuccessRateService successRateService = new SuccessRateService();
    private AttributeManagementService attributeService = new AttributeManagementService();

    // 選択状態
    private UserEquipment selectedEquipment;
    private EnhanceItemMasterData selectedEnhanceItem;
    private SupportItemMasterData selectedSupportItem;

    // 実行状態
    private bool isProcessing = false;

    #region Unity Lifecycle

    private void Start()
    {
        InitializeUI();
        SetupEventListeners();
        UpdateUI();
    }

    private void OnDestroy()
    {
        RemoveEventListeners();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// UI初期化
    /// </summary>
    private void InitializeUI()
    {
        try
        {
            // 初期状態設定
            ResetAllSelections();

            // 強化アイテム選択を無効化（装備選択まで）
            if (enhanceItemSelectUI != null)
            {
                enhanceItemSelectUI.SetInteractable(false);
            }

            // ステータス表示も初期化
            if (statusDisplayUI != null)
            {
                statusDisplayUI.ResetDisplay();
            }

            Debug.Log("[EnhanceUIController] UI初期化完了");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnhanceUIController] UI初期化エラー: {e.Message}");
        }
    }

    /// <summary>
    /// イベントリスナー設定
    /// </summary>
    private void SetupEventListeners()
    {
        try
        {
            // 各UI Controllerのイベント購読
            if (equipmentSelectUI != null)
                equipmentSelectUI.OnEquipmentSelected += OnEquipmentSelected;
            if (enhanceItemSelectUI != null)
                enhanceItemSelectUI.OnEnhanceItemSelected += OnEnhanceItemSelected;
            if (supportItemSelectUI != null)
                supportItemSelectUI.OnSupportItemSelected += OnSupportItemSelected;

            // ボタンイベント設定
            if (enhanceExecuteButton != null)
                enhanceExecuteButton.onClick.AddListener(OnEnhanceExecuteButtonClicked);
            if (homeButton != null)
                homeButton.onClick.AddListener(OnHomeButtonClicked);

            Debug.Log("[EnhanceUIController] イベントリスナー設定完了");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnhanceUIController] イベント設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// イベントリスナー削除
    /// </summary>
    private void RemoveEventListeners()
    {
        try
        {
            if (equipmentSelectUI != null)
                equipmentSelectUI.OnEquipmentSelected -= OnEquipmentSelected;
            if (enhanceItemSelectUI != null)
                enhanceItemSelectUI.OnEnhanceItemSelected -= OnEnhanceItemSelected;
            if (supportItemSelectUI != null)
                supportItemSelectUI.OnSupportItemSelected -= OnSupportItemSelected;

            if (enhanceExecuteButton != null)
                enhanceExecuteButton.onClick.RemoveAllListeners();
            if (homeButton != null)
                homeButton.onClick.RemoveAllListeners();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EnhanceUIController] イベント削除エラー: {e.Message}");
        }
    }

    #endregion

    #region Selection Event Handlers

    /// <summary>
    /// 装備選択時の処理
    /// </summary>
    public void OnEquipmentSelected(UserEquipment equipment)
    {
        try
        {
            selectedEquipment = equipment;

            // 装備選択後は強化アイテム選択を有効化
            if (enhanceItemSelectUI != null)
            {
                enhanceItemSelectUI.SetInteractable(true);
                enhanceItemSelectUI.SetSelectedEquipment(equipment);
            }

            // 強化アイテムの選択状態をリセット（装備変更時）
            selectedEnhanceItem = null;
            if (enhanceItemSelectUI != null)
            {
                enhanceItemSelectUI.ResetSelection();
            }

            // 補助材料の選択状態もリセット
            selectedSupportItem = null;
            if (supportItemSelectUI != null)
            {
                supportItemSelectUI.ResetSelection();
            }

            UpdateUI();

            Debug.Log($"[EnhanceUIController] 装備選択: {equipment.equipment_id}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnhanceUIController] 装備選択エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 強化アイテム選択時の処理
    /// </summary>
    public void OnEnhanceItemSelected(EnhanceItemMasterData enhanceItem)
    {
        try
        {
            selectedEnhanceItem = enhanceItem;
            UpdateUI();

            Debug.Log($"[EnhanceUIController] 強化アイテム選択: {enhanceItem.enhance_item_id}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnhanceUIController] 強化アイテム選択エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 補助材料選択時の処理
    /// </summary>
    public void OnSupportItemSelected(SupportItemMasterData supportItem)
    {
        try
        {
            selectedSupportItem = supportItem; // nullの場合は「使用しない」
            UpdateUI();

            string itemName = supportItem != null ? supportItem.support_item_name : "使用しない";
            Debug.Log($"[EnhanceUIController] 補助材料選択: {itemName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnhanceUIController] 補助材料選択エラー: {e.Message}");
        }
    }

    #endregion

    #region UI Update

    /// <summary>
    /// UI全体更新
    /// </summary>
    private void UpdateUI()
    {
        try
        {
            UpdateSuccessRateDisplay();
            UpdateInstructionText();
            UpdateEnhanceButtonState();

            // ステータス表示更新
            if (statusDisplayUI != null)
            {
                statusDisplayUI.UpdateDisplay(selectedEquipment, selectedEnhanceItem, selectedSupportItem);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnhanceUIController] UI更新エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 成功率表示更新
    /// </summary>
    private void UpdateSuccessRateDisplay()
    {
        if (successRateText == null) return;

        try
        {
            if (selectedEquipment != null && selectedEnhanceItem != null)
            {
                string rateText = successRateService.GetSuccessRateDisplayText(
                    selectedEquipment, selectedEnhanceItem, selectedSupportItem);
                successRateText.text = $"成功率: {rateText}";
                successRateText.color = normalTextColor;
            }
            else
            {
                successRateText.text = "成功率: --%";
                successRateText.color = disabledButtonColor;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EnhanceUIController] 成功率計算エラー: {e.Message}");
            successRateText.text = "成功率: エラー";
            successRateText.color = warningTextColor;
        }
    }

    /// <summary>
    /// 指示・注意テキスト更新
    /// </summary>
    private void UpdateInstructionText()
    {
        if (instructionText == null) return;

        try
        {
            if (selectedEquipment == null || selectedEnhanceItem == null)
            {
                // 選択未完了の場合
                if (selectedEquipment == null)
                {
                    instructionText.text = "装備と強化アイテムを選択してください";
                    instructionText.color = instructionTextColor;
                }
                else
                {
                    instructionText.text = "強化アイテムを選択してください";
                    instructionText.color = instructionTextColor;
                }
            }
            else
            {
                // 属性警告チェック
                string warning = attributeService.GetAttributeChangeWarning(selectedEquipment, selectedEnhanceItem);

                if (string.IsNullOrEmpty(warning))
                {
                    instructionText.text = ""; // 警告なし
                    instructionText.color = normalTextColor;
                }
                else
                {
                    instructionText.text = warning;
                    instructionText.color = warningTextColor; // 警告は赤色
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EnhanceUIController] 属性警告チェックエラー: {e.Message}");
            instructionText.text = "";
            instructionText.color = normalTextColor;
        }
    }

    /// <summary>
    /// 強化実行ボタン状態更新
    /// </summary>
    private void UpdateEnhanceButtonState()
    {
        if (enhanceExecuteButton == null) return;

        try
        {
            bool canEnhance = selectedEquipment != null && selectedEnhanceItem != null && !isProcessing;

            enhanceExecuteButton.interactable = canEnhance;

            // ボタンテキストの色変更
            Text buttonText = enhanceExecuteButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.color = canEnhance ? enabledButtonColor : disabledButtonColor;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EnhanceUIController] ボタン状態更新エラー: {e.Message}");
        }
    }

    #endregion

    #region Button Event Handlers

    /// <summary>
    /// 強化実行ボタンクリック処理
    /// </summary>
    public void OnEnhanceExecuteButtonClicked()
    {
        if (selectedEquipment != null && selectedEnhanceItem != null && !isProcessing)
        {
            StartCoroutine(ExecuteEnhanceProcess());
        }
    }

    /// <summary>
    /// ホームボタンクリック処理
    /// </summary>
    public void OnHomeButtonClicked()
    {
        if (!isProcessing)
        {
            Debug.Log("[EnhanceUIController] ホーム画面に遷移");

            try
            {
                // カスタムSceneManagerを優先、なければUnity標準を使用
                if (SceneManager.Instance != null)
                {
                    SceneManager.Instance.LoadHomeScene();
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EnhanceUIController] シーン遷移エラー: {e.Message}");
                // フォールバック
                UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene");
            }
        }
    }

    #endregion

    #region Enhance Process

    /// <summary>
    /// 強化処理実行
    /// </summary>
    private IEnumerator ExecuteEnhanceProcess()
    {
        isProcessing = true;

        Debug.Log("[EnhanceUIController] 強化処理開始");

        // UI無効化
        SetUIInteractable(false);

        EnhanceResultData result = null;
        bool hasError = false;

        // 強化実行（try-catchはyield returnの外で実行）
        try
        {
            int supportItemId = selectedSupportItem != null ? selectedSupportItem.support_item_id : -1;

            result = enhanceService.ExecuteEnhance(
                selectedEquipment.unique_id,
                selectedEnhanceItem.enhance_item_id,
                supportItemId
            );

            // 結果メッセージの設定
            if (result != null && string.IsNullOrEmpty(result.ResultMessage))
            {
                result.ResultMessage = result.IsSuccess ?
                    "装備の強化に成功しました！" :
                    "装備の強化に失敗しました...";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnhanceUIController] 強化処理エラー: {e.Message}");
            hasError = true;

            // エラー時の結果データ作成
            result = new EnhanceResultData
            {
                IsSuccess = false,
                ResultMessage = "強化処理中にエラーが発生しました",
                EnhancedEquipment = selectedEquipment,
                ConsumedEnhanceItemId = selectedEnhanceItem.enhance_item_id,
                ConsumedSupportItemId = selectedSupportItem?.support_item_id ?? -1
            };
        }

        // 結果表示（yield returnを含む処理）
        if (result != null)
        {
            yield return ShowEnhanceResult(result);

            // 成功時は選択状態をリセット
            if (result.IsSuccess && !hasError)
            {
                ResetAllSelections();
            }
        }

        // UI再有効化
        SetUIInteractable(true);
        UpdateUI();
        isProcessing = false;

        Debug.Log($"[EnhanceUIController] 強化処理完了: {(result?.IsSuccess == true ? "成功" : "失敗")}");
    }

    /// <summary>
    /// 強化結果表示
    /// </summary>
    private IEnumerator ShowEnhanceResult(EnhanceResultData result)
    {
        if (resultUI != null)
        {
            // 結果表示UIを使用
            yield return resultUI.ShowResult(result);
        }
        else
        {
            // フォールバック：簡易表示
            Debug.Log($"[EnhanceUIController] 強化結果: {result.ResultMessage}");

            if (instructionText != null)
            {
                string originalText = instructionText.text;
                Color originalColor = instructionText.color;

                instructionText.text = result.ResultMessage;
                instructionText.color = result.IsSuccess ? Color.green : Color.red;

                yield return new WaitForSeconds(2f);

                // 元のテキストに戻す
                instructionText.text = originalText;
                instructionText.color = originalColor;
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 全選択状態をリセット
    /// </summary>
    private void ResetAllSelections()
    {
        try
        {
            selectedEquipment = null;
            selectedEnhanceItem = null;
            selectedSupportItem = null;

            // 各UI Controllerのリセット
            if (equipmentSelectUI != null)
                equipmentSelectUI.ResetSelection();
            if (enhanceItemSelectUI != null)
                enhanceItemSelectUI.ResetSelection();
            if (supportItemSelectUI != null)
                supportItemSelectUI.ResetSelection();

            // ステータス表示もリセット
            if (statusDisplayUI != null)
            {
                statusDisplayUI.ResetDisplay();
            }

            // 強化アイテム選択を再度無効化
            if (enhanceItemSelectUI != null)
            {
                enhanceItemSelectUI.SetInteractable(false);
            }

            Debug.Log("[EnhanceUIController] 全選択状態をリセット");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnhanceUIController] 選択リセットエラー: {e.Message}");
        }
    }

    /// <summary>
    /// UI操作可能状態設定
    /// </summary>
    private void SetUIInteractable(bool interactable)
    {
        try
        {
            // 各Controllerの操作可能状態設定
            if (equipmentSelectUI != null)
            {
                equipmentSelectUI.SetInteractable(interactable);
            }

            if (enhanceItemSelectUI != null)
            {
                enhanceItemSelectUI.SetInteractable(interactable && selectedEquipment != null);
            }

            if (supportItemSelectUI != null)
            {
                supportItemSelectUI.SetInteractable(interactable);
            }

            // ホームボタン
            if (homeButton != null)
            {
                homeButton.interactable = interactable;
            }

            // 強化実行ボタンは別途UpdateUI()で制御
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EnhanceUIController] UI状態設定エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 選択状態の検証
    /// </summary>
    public bool ValidateSelections()
    {
        if (selectedEquipment == null)
        {
            Debug.LogWarning("[EnhanceUIController] 装備が選択されていません");
            return false;
        }

        if (selectedEnhanceItem == null)
        {
            Debug.LogWarning("[EnhanceUIController] 強化アイテムが選択されていません");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 現在の選択状態取得
    /// </summary>
    public (UserEquipment equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem) GetCurrentSelections()
    {
        return (selectedEquipment, selectedEnhanceItem, selectedSupportItem);
    }

    /// <summary>
    /// 強化処理中かどうか
    /// </summary>
    public bool IsProcessing()
    {
        return isProcessing;
    }

    #endregion

    #region Debug Methods

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogCurrentSelections()
    {
        Debug.Log($"[EnhanceUIController] 現在の選択状態:");
        Debug.Log($"  装備: {(selectedEquipment != null ? selectedEquipment.equipment_id.ToString() : "未選択")}");
        Debug.Log($"  強化アイテム: {(selectedEnhanceItem != null ? selectedEnhanceItem.enhance_item_id.ToString() : "未選択")}");
        Debug.Log($"  補助材料: {(selectedSupportItem != null ? selectedSupportItem.support_item_name : "使用しない")}");
    }

    /// <summary>
    /// エディタ用：強化成功テスト
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestSuccessEnhance()
    {
        if (resultUI != null)
        {
            resultUI.TestSuccessResult();
        }
    }

    /// <summary>
    /// エディタ用：強化失敗テスト
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestFailureEnhance()
    {
        if (resultUI != null)
        {
            resultUI.TestFailureResult();
        }
    }

    #endregion

    #region Inspector Validation

    private void OnValidate()
    {
        // Inspector設定の検証
        if (enhanceExecuteButton == null)
        {
            Debug.LogWarning("[EnhanceUIController] 強化実行ボタンが設定されていません");
        }

        if (successRateText == null)
        {
            Debug.LogWarning("[EnhanceUIController] 成功率テキストが設定されていません");
        }

        if (instructionText == null)
        {
            Debug.LogWarning("[EnhanceUIController] 指示テキストが設定されていません");
        }

        if (statusDisplayUI == null)
        {
            Debug.LogWarning("[EnhanceUIController] ステータス表示UIが設定されていません");
        }

        if (resultUI == null)
        {
            Debug.LogWarning("[EnhanceUIController] 結果表示UIが設定されていません");
        }
    }

    #endregion
}