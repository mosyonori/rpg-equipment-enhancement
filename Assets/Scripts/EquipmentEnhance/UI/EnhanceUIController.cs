using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 装備強化画面のメイン制御クラス
/// 各UI Controllerの統括、Service層との連携、選択状態管理を担当
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
    [SerializeField] private Text successRateText;
    [SerializeField] private Text instructionText;
    [SerializeField] private Button homeButton;

    [Header("Colors")]
    [SerializeField] private Color enabledButtonColor = Color.white;
    [SerializeField] private Color disabledButtonColor = Color.gray;

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
        // 初期状態設定
        ResetAllSelections();

        // 強化アイテム選択を無効化（装備選択まで）
        enhanceItemSelectUI.SetInteractable(false);

        Debug.Log("[EnhanceUIController] UI初期化完了");
    }

    /// <summary>
    /// イベントリスナー設定
    /// </summary>
    private void SetupEventListeners()
    {
        // 各UI Controllerのイベント購読
        equipmentSelectUI.OnEquipmentSelected += OnEquipmentSelected;
        enhanceItemSelectUI.OnEnhanceItemSelected += OnEnhanceItemSelected;
        supportItemSelectUI.OnSupportItemSelected += OnSupportItemSelected;

        // ボタンイベント設定
        enhanceExecuteButton.onClick.AddListener(OnEnhanceExecuteButtonClicked);
        homeButton.onClick.AddListener(OnHomeButtonClicked);
    }

    /// <summary>
    /// イベントリスナー削除
    /// </summary>
    private void RemoveEventListeners()
    {
        if (equipmentSelectUI != null)
            equipmentSelectUI.OnEquipmentSelected -= OnEquipmentSelected;
        if (enhanceItemSelectUI != null)
            enhanceItemSelectUI.OnEnhanceItemSelected -= OnEnhanceItemSelected;
        if (supportItemSelectUI != null)
            supportItemSelectUI.OnSupportItemSelected -= OnSupportItemSelected;

        enhanceExecuteButton.onClick.RemoveAllListeners();
        homeButton.onClick.RemoveAllListeners();
    }

    #endregion

    #region Selection Event Handlers

    /// <summary>
    /// 装備選択時の処理
    /// </summary>
    public void OnEquipmentSelected(UserEquipment equipment)
    {
        selectedEquipment = equipment;

        // 装備選択後は強化アイテム選択を有効化
        enhanceItemSelectUI.SetInteractable(true);
        enhanceItemSelectUI.SetCurrentEquipment(equipment);

        // 強化アイテムの選択状態をリセット（装備変更時）
        selectedEnhanceItem = null;
        enhanceItemSelectUI.ResetSelection();

        // 補助材料の選択状態もリセット
        selectedSupportItem = null;
        supportItemSelectUI.ResetSelection();

        UpdateUI();

        Debug.Log($"[EnhanceUIController] 装備選択: {equipment.equipment_id}");
    }

    /// <summary>
    /// 強化アイテム選択時の処理
    /// </summary>
    public void OnEnhanceItemSelected(EnhanceItemMasterData enhanceItem)
    {
        selectedEnhanceItem = enhanceItem;
        UpdateUI();

        Debug.Log($"[EnhanceUIController] 強化アイテム選択: {enhanceItem.enhance_item_id}");
    }

    /// <summary>
    /// 補助材料選択時の処理
    /// </summary>
    public void OnSupportItemSelected(SupportItemMasterData supportItem)
    {
        selectedSupportItem = supportItem; // nullの場合は「使用しない」
        UpdateUI();

        string itemName = supportItem != null ? supportItem.support_item_name : "使用しない";
        Debug.Log($"[EnhanceUIController] 補助材料選択: {itemName}");
    }

    #endregion

    #region UI Update

    /// <summary>
    /// UI全体更新
    /// </summary>
    private void UpdateUI()
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

    /// <summary>
    /// 成功率表示更新
    /// </summary>
    private void UpdateSuccessRateDisplay()
    {
        if (selectedEquipment != null && selectedEnhanceItem != null)
        {
            try
            {
                string rateText = successRateService.GetSuccessRateDisplayText(
                    selectedEquipment, selectedEnhanceItem, selectedSupportItem);
                successRateText.text = $"成功率: {rateText}";
                successRateText.color = Color.white;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EnhanceUIController] 成功率計算エラー: {e.Message}");
                successRateText.text = "成功率: --%";
                successRateText.color = Color.red;
            }
        }
        else
        {
            successRateText.text = "成功率: --%";
            successRateText.color = disabledButtonColor;
        }
    }

    /// <summary>
    /// 指示・注意テキスト更新
    /// </summary>
    private void UpdateInstructionText()
    {
        if (selectedEquipment == null || selectedEnhanceItem == null)
        {
            // 選択未完了の場合
            if (selectedEquipment == null)
            {
                instructionText.text = "装備と強化アイテムを選択してください";
                instructionText.color = Color.yellow;
            }
            else
            {
                instructionText.text = "強化アイテムを選択してください";
                instructionText.color = Color.yellow;
            }
        }
        else
        {
            // 属性警告チェック
            try
            {
                string warning = attributeService.GetAttributeChangeWarning(selectedEquipment, selectedEnhanceItem);

                if (string.IsNullOrEmpty(warning))
                {
                    instructionText.text = ""; // 警告なし
                }
                else
                {
                    instructionText.text = warning;
                    instructionText.color = Color.red; // 警告は赤色
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EnhanceUIController] 属性警告チェックエラー: {e.Message}");
                instructionText.text = "";
            }
        }
    }

    /// <summary>
    /// 強化実行ボタン状態更新
    /// </summary>
    private void UpdateEnhanceButtonState()
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
            SceneManager.Instance.LoadHomeScene();
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
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnhanceUIController] 強化処理エラー: {e.Message}");
            hasError = true;

            // エラー時の結果データ作成
            result = new EnhanceResultData
            {
                IsSuccess = false,
                ResultMessage = "強化処理中にエラーが発生しました"
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
            // 結果表示UIがない場合は簡易表示
            Debug.Log($"[EnhanceUIController] 強化結果: {result.ResultMessage}");
            yield return new WaitForSeconds(2f);
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 全選択状態をリセット
    /// </summary>
    private void ResetAllSelections()
    {
        selectedEquipment = null;
        selectedEnhanceItem = null;
        selectedSupportItem = null;

        // 各UI Controllerのリセット
        equipmentSelectUI?.ResetSelection();
        enhanceItemSelectUI?.ResetSelection();
        supportItemSelectUI?.ResetSelection();

        // 強化アイテム選択を再度無効化
        enhanceItemSelectUI?.SetInteractable(false);

        Debug.Log("[EnhanceUIController] 全選択状態をリセット");
    }

    /// <summary>
    /// UI操作可能状態設定
    /// </summary>
    private void SetUIInteractable(bool interactable)
    {
        equipmentSelectUI?.SetInteractable(interactable);
        enhanceItemSelectUI?.SetInteractable(interactable && selectedEquipment != null);
        supportItemSelectUI?.SetInteractable(interactable);
        homeButton.interactable = interactable;

        // 強化実行ボタンは別途UpdateUI()で制御
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

    #endregion
}