using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 装備強化画面のメインUIクラス
/// スロットボタン方式でのアイテム選択とモーダルウィンドウ表示を管理
/// データアクセス統一ルール: UI層 → EquipmentEnhanceManager → データ層
/// </summary>
public class EquipmentEnhanceUI : MonoBehaviour
{
    #region UI References - スロットボタン関連

    [Header("スロットボタン")]
    [SerializeField] private Button equipmentSlotButton;
    [SerializeField] private Button enhanceItemSlotButton;
    [SerializeField] private Button supportItemSlotButton;

    [Header("スロット表示エリア")]
    [SerializeField] private GameObject equipmentSlotDisplay;
    [SerializeField] private GameObject enhanceItemSlotDisplay;
    [SerializeField] private GameObject supportItemSlotDisplay;

    // 装備スロット表示要素
    [Header("装備表示")]
    [SerializeField] private Image equipmentIcon;
    [SerializeField] private TextMeshProUGUI equipmentNameText;
    [SerializeField] private TextMeshProUGUI equipmentEnhanceText;
    [SerializeField] private TextMeshProUGUI equipmentStaminaText;

    // 強化アイテムスロット表示要素
    [Header("強化アイテム表示")]
    [SerializeField] private Image enhanceItemIcon;
    [SerializeField] private TextMeshProUGUI enhanceItemNameText;
    [SerializeField] private TextMeshProUGUI enhanceItemDescText;

    // 補助材料スロット表示要素
    [Header("補助材料表示")]
    [SerializeField] private Image supportItemIcon;
    [SerializeField] private TextMeshProUGUI supportItemNameText;
    [SerializeField] private TextMeshProUGUI supportItemDescText;

    #endregion

    #region UI References - 詳細パネル

    [Header("詳細パネル")]
    [SerializeField] private GameObject equipmentDetailPanel;
    [SerializeField] private GameObject enhanceEffectPanel;
    [SerializeField] private GameObject supportEffectPanel;

    // 装備詳細パネル
    [Header("装備詳細")]
    [SerializeField] private TextMeshProUGUI equipmentDetailText;
    [SerializeField] private TextMeshProUGUI equipmentAttributeText;
    [SerializeField] private TextMeshProUGUI equipmentStatusText;

    // 強化効果パネル
    [Header("強化効果")]
    [SerializeField] private TextMeshProUGUI enhanceEffectText;
    [SerializeField] private TextMeshProUGUI enhanceSuccessRateText;

    // 補助効果パネル
    [Header("補助効果")]
    [SerializeField] private TextMeshProUGUI supportEffectText;
    [SerializeField] private TextMeshProUGUI supportBonusText;

    #endregion

    #region UI References - 実行関連

    [Header("実行エリア")]
    [SerializeField] private TextMeshProUGUI finalSuccessRateText;
    [SerializeField] private Button executeButton;
    [SerializeField] private TextMeshProUGUI executeButtonText;

    [Header("結果表示")]
    [SerializeField] private GameObject resultWindow;
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI resultDetailsText;
    [SerializeField] private Button resultCloseButton;

    #endregion

    #region Private Fields - 選択状態

    private string selectedEquipmentId;
    private int selectedEnhanceItemId;
    private int selectedSupportItemId; // 0の場合は未選択

    private UserEquipmentData currentEquipment;
    private EnhanceItemMasterData currentEnhanceItem;
    private SupportItemMasterData currentSupportItem;

    #endregion

    #region Private Fields - アイテム選択ウィンドウ

    [Header("アイテム選択ウィンドウ")]
    [SerializeField] private ItemSelectionWindowUI itemSelectionWindow;

    #endregion

    #region Private Fields - 設定

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeUI();
        SetupEventListeners();
        RefreshUI();
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
        LogDebug("EquipmentEnhanceUI初期化開始");

        // 初期状態では何も選択されていない
        ClearAllSelections();

        // 結果ウィンドウを非表示
        if (resultWindow != null)
        {
            resultWindow.SetActive(false);
        }

        // アイテム選択ウィンドウの参照を設定
        if (itemSelectionWindow == null)
        {
            itemSelectionWindow = FindFirstObjectByType<ItemSelectionWindowUI>();
        }

        LogDebug("EquipmentEnhanceUI初期化完了");
    }

    /// <summary>
    /// イベントリスナー設定
    /// </summary>
    private void SetupEventListeners()
    {
        // スロットボタンのイベント
        if (equipmentSlotButton != null)
            equipmentSlotButton.onClick.AddListener(() => OnSlotButtonClicked(SlotType.Equipment));

        if (enhanceItemSlotButton != null)
            enhanceItemSlotButton.onClick.AddListener(() => OnSlotButtonClicked(SlotType.EnhanceItem));

        if (supportItemSlotButton != null)
            supportItemSlotButton.onClick.AddListener(() => OnSlotButtonClicked(SlotType.SupportItem));

        // 実行ボタンのイベント
        if (executeButton != null)
            executeButton.onClick.AddListener(OnExecuteButtonClicked);

        // 結果クローズボタンのイベント
        if (resultCloseButton != null)
            resultCloseButton.onClick.AddListener(OnResultCloseButtonClicked);

        // 強化完了イベント
        if (EquipmentEnhanceManager.Instance != null)
        {
            EquipmentEnhanceManager.Instance.OnEnhanceCompleted += OnEnhanceCompleted;
            EquipmentEnhanceManager.Instance.OnEnhanceError += OnEnhanceError;
        }

        // アイテム選択ウィンドウのイベント - 修正：装備用とその他用に分ける
        if (itemSelectionWindow != null)
        {
            itemSelectionWindow.OnItemSelected += OnItemSelected;
            itemSelectionWindow.OnEquipmentSelected += OnEquipmentSelected; // 装備専用イベント
        }
    }

    /// <summary>
    /// イベントリスナー削除
    /// </summary>
    private void RemoveEventListeners()
    {
        // スロットボタンのイベント
        if (equipmentSlotButton != null)
            equipmentSlotButton.onClick.RemoveAllListeners();

        if (enhanceItemSlotButton != null)
            enhanceItemSlotButton.onClick.RemoveAllListeners();

        if (supportItemSlotButton != null)
            supportItemSlotButton.onClick.RemoveAllListeners();

        // 実行ボタンのイベント
        if (executeButton != null)
            executeButton.onClick.RemoveAllListeners();

        // 結果クローズボタンのイベント
        if (resultCloseButton != null)
            resultCloseButton.onClick.RemoveAllListeners();

        // 強化完了イベント
        if (EquipmentEnhanceManager.Instance != null)
        {
            EquipmentEnhanceManager.Instance.OnEnhanceCompleted -= OnEnhanceCompleted;
            EquipmentEnhanceManager.Instance.OnEnhanceError -= OnEnhanceError;
        }

        // アイテム選択ウィンドウのイベント
        if (itemSelectionWindow != null)
        {
            itemSelectionWindow.OnItemSelected -= OnItemSelected;
            itemSelectionWindow.OnEquipmentSelected -= OnEquipmentSelected;
        }
    }

    #endregion

    #region Slot Button Events

    /// <summary>
    /// スロットタイプ定義
    /// </summary>
    private enum SlotType
    {
        Equipment,
        EnhanceItem,
        SupportItem
    }

    /// <summary>
    /// スロットボタンクリック時の処理
    /// </summary>
    /// <param name="slotType">クリックされたスロットのタイプ</param>
    private void OnSlotButtonClicked(SlotType slotType)
    {
        LogDebug($"スロットボタンクリック: {slotType}");

        if (itemSelectionWindow == null)
        {
            LogError("ItemSelectionWindowUIが見つかりません");
            return;
        }

        switch (slotType)
        {
            case SlotType.Equipment:
                ShowEquipmentSelection();
                break;
            case SlotType.EnhanceItem:
                ShowEnhanceItemSelection();
                break;
            case SlotType.SupportItem:
                ShowSupportItemSelection();
                break;
        }
    }

    /// <summary>
    /// 装備選択ウィンドウを表示
    /// </summary>
    private void ShowEquipmentSelection()
    {
        // 所持している装備一覧を取得
        var equipments = GetUserEquipments();
        itemSelectionWindow.ShowEquipmentSelection(equipments, selectedEquipmentId);
    }

    /// <summary>
    /// 強化アイテム選択ウィンドウを表示
    /// </summary>
    private void ShowEnhanceItemSelection()
    {
        // EquipmentEnhanceManagerから利用可能な強化アイテム一覧を取得
        var enhanceItems = EquipmentEnhanceManager.Instance.GetAvailableEnhanceItems();
        LogDebug($"利用可能な強化アイテム数: {enhanceItems?.Count ?? 0}");

        itemSelectionWindow.ShowEnhanceItemSelection(enhanceItems, selectedEnhanceItemId);
    }

    /// <summary>
    /// 補助材料選択ウィンドウを表示
    /// </summary>
    private void ShowSupportItemSelection()
    {
        // 修正：実際に所持している補助材料のみを表示
        var availableSupportItems = EquipmentEnhanceManager.Instance.GetAvailableSupportItems();
        LogDebug($"所持している補助材料数: {availableSupportItems?.Count ?? 0}");

        itemSelectionWindow.ShowSupportItemSelection(availableSupportItems, selectedSupportItemId);
    }

    #endregion

    #region Item Selection Events

    /// <summary>
    /// アイテム選択完了時の処理（強化アイテム・補助材料用）
    /// </summary>
    /// <param name="itemType">選択されたアイテムのタイプ</param>
    /// <param name="itemId">選択されたアイテムのID</param>
    private void OnItemSelected(ItemSelectionWindowUI.ItemType itemType, int itemId)
    {
        LogDebug($"アイテム選択完了: {itemType}, ID: {itemId}");

        switch (itemType)
        {
            case ItemSelectionWindowUI.ItemType.EnhanceItem:
                SetSelectedEnhanceItem(itemId);
                break;
            case ItemSelectionWindowUI.ItemType.SupportItem:
                SetSelectedSupportItem(itemId);
                break;
                // 装備はOnEquipmentSelectedで処理するため、ここでは処理しない
        }

        RefreshUI();
    }

    /// <summary>
    /// 装備選択完了時の処理（装備専用）
    /// </summary>
    /// <param name="equipmentUserId">選択された装備のユーザーID</param>
    private void OnEquipmentSelected(string equipmentUserId)
    {
        LogDebug($"装備選択完了: {equipmentUserId}");
        SetSelectedEquipment(equipmentUserId);
        RefreshUI();
    }

    /// <summary>
    /// 選択された装備を設定
    /// </summary>
    /// <param name="equipmentId">装備のユーザーID</param>
    private void SetSelectedEquipment(string equipmentId)
    {
        selectedEquipmentId = equipmentId;
        currentEquipment = GetUserEquipmentData(equipmentId);
        LogDebug($"装備選択: {equipmentId}");
    }

    /// <summary>
    /// 選択された強化アイテムを設定
    /// </summary>
    /// <param name="enhanceItemId">強化アイテムのマスターID</param>
    private void SetSelectedEnhanceItem(int enhanceItemId)
    {
        selectedEnhanceItemId = enhanceItemId;
        currentEnhanceItem = MasterDataManager.Instance.GetEnhanceItemData(enhanceItemId);
        LogDebug($"強化アイテム選択: {enhanceItemId}");
    }

    /// <summary>
    /// 選択された補助材料を設定
    /// </summary>
    /// <param name="supportItemId">補助材料のマスターID（0の場合は選択解除）</param>
    private void SetSelectedSupportItem(int supportItemId)
    {
        selectedSupportItemId = supportItemId;
        currentSupportItem = supportItemId > 0 ?
            MasterDataManager.Instance.GetSupportItemData(supportItemId) : null;
        LogDebug($"補助材料選択: {supportItemId}");
    }

    #endregion

    #region Execute Enhancement

    /// <summary>
    /// 強化実行ボタンクリック時の処理
    /// </summary>
    private void OnExecuteButtonClicked()
    {
        LogDebug("強化実行ボタンクリック");

        if (!CanExecuteEnhance())
        {
            LogWarning("強化実行条件が満たされていません");
            return;
        }

        // 強化を実行
        var result = EquipmentEnhanceManager.Instance.ExecuteEnhance(
            selectedEquipmentId,
            selectedEnhanceItemId,
            selectedSupportItemId
        );

        // 結果はOnEnhanceCompletedで処理される
    }

    /// <summary>
    /// 強化実行可能かどうかをチェック
    /// </summary>
    /// <returns>実行可能な場合true</returns>
    private bool CanExecuteEnhance()
    {
        return !string.IsNullOrEmpty(selectedEquipmentId) &&
               selectedEnhanceItemId > 0 &&
               EquipmentEnhanceManager.Instance.CanExecuteEnhance(selectedEquipmentId, selectedEnhanceItemId);
    }

    #endregion

    #region Enhancement Events

    /// <summary>
    /// 強化完了時の処理
    /// </summary>
    /// <param name="result">強化結果</param>
    private void OnEnhanceCompleted(EnhanceResultData result)
    {
        LogDebug($"強化完了: {result?.isSuccess}");

        if (result != null)
        {
            ShowEnhanceResult(result);
            RefreshUI(); // データが更新されたのでUI更新
        }
    }

    /// <summary>
    /// 強化エラー時の処理
    /// </summary>
    /// <param name="errorMessage">エラーメッセージ</param>
    private void OnEnhanceError(string errorMessage)
    {
        LogError($"強化エラー: {errorMessage}");
        ShowErrorResult(errorMessage);
    }

    /// <summary>
    /// 強化結果を表示
    /// </summary>
    /// <param name="result">強化結果</param>
    private void ShowEnhanceResult(EnhanceResultData result)
    {
        if (resultWindow == null) return;

        resultWindow.SetActive(true);

        if (result.isSuccess)
        {
            ShowSuccessResult(result);
        }
        else
        {
            ShowFailureResult(result);
        }
    }

    /// <summary>
    /// 成功結果を表示（修正版：日本語表記対応）
    /// </summary>
    /// <param name="result">強化結果</param>
    private void ShowSuccessResult(EnhanceResultData result)
    {
        if (resultTitleText != null)
        {
            resultTitleText.text = "強化成功！";
            resultTitleText.color = Color.green;
        }

        if (resultDetailsText != null)
        {
            var details = $"強化値: +{result.previousEnhancedValue} → +{result.newEnhancedValue}\n";
            details += $"属性: {ConvertAttributeTypeToJapanese(result.previousAttributeType)} → {ConvertAttributeTypeToJapanese(result.newAttributeType)}\n";
            details += $"耐久値: {result.previousEnhanceStamina} → {result.newEnhanceStamina}\n";
            details += $"成功率: {result.actualSuccessRate:F1}%";

            if (result.statusChanges.Count > 0)
            {
                details += "\n\nステータス変化:";
                foreach (var kvp in result.statusChanges)
                {
                    string japaneseStatusName = ConvertStatusNameToJapanese(kvp.Key);
                    details += $"\n{japaneseStatusName}: +{kvp.Value}";
                }
            }

            resultDetailsText.text = details;
        }
    }

    /// <summary>
    /// 失敗結果を表示（修正版：日本語表記対応）
    /// </summary>
    /// <param name="result">強化結果</param>
    private void ShowFailureResult(EnhanceResultData result)
    {
        if (resultTitleText != null)
        {
            resultTitleText.text = "強化失敗...";
            resultTitleText.color = Color.red;
        }

        if (resultDetailsText != null)
        {
            var details = $"強化値: +{result.previousEnhancedValue} (変化なし)\n";
            details += $"属性: {ConvertAttributeTypeToJapanese(result.previousAttributeType)} (変化なし)\n";
            details += $"耐久値: {result.previousEnhanceStamina} → {result.newEnhanceStamina}\n";
            details += $"成功率: {result.actualSuccessRate:F1}%";

            resultDetailsText.text = details;
        }
    }

    /// <summary>
    /// エラー結果を表示
    /// </summary>
    /// <param name="errorMessage">エラーメッセージ</param>
    private void ShowErrorResult(string errorMessage)
    {
        if (resultWindow == null) return;

        resultWindow.SetActive(true);

        if (resultTitleText != null)
        {
            resultTitleText.text = "エラー発生";
            resultTitleText.color = Color.red;
        }

        if (resultDetailsText != null)
        {
            resultDetailsText.text = errorMessage;
        }
    }

    /// <summary>
    /// 結果ウィンドウクローズボタンクリック時の処理
    /// </summary>
    private void OnResultCloseButtonClicked()
    {
        if (resultWindow != null)
        {
            resultWindow.SetActive(false);
        }
    }

    #endregion

    #region UI Update Methods

    /// <summary>
    /// UIを全体的に更新
    /// </summary>
    public void RefreshUI()
    {
        UpdateSlotDisplays();
        UpdateDetailPanels();
        UpdateExecuteButton();
        UpdateSuccessRate();
    }

    /// <summary>
    /// スロット表示を更新
    /// </summary>
    private void UpdateSlotDisplays()
    {
        UpdateEquipmentSlotDisplay();
        UpdateEnhanceItemSlotDisplay();
        UpdateSupportItemSlotDisplay();
    }

    /// <summary>
    /// 装備スロット表示を更新（修正版：スロットは常に表示）
    /// </summary>
    private void UpdateEquipmentSlotDisplay()
    {
        // 装備スロット表示エリアは常に表示
        if (equipmentSlotDisplay != null)
            equipmentSlotDisplay.SetActive(true);

        if (currentEquipment != null)
        {
            var masterData = MasterDataManager.Instance.GetEquipmentData(currentEquipment.equipmentMasterId);
            if (masterData != null)
            {
                SetUIText(equipmentNameText, masterData.equipmentName);
                SetUIText(equipmentEnhanceText, $"+{currentEquipment.currentEnhancedValue}");
                SetUIText(equipmentStaminaText, $"{currentEquipment.currentEnhanceStamina}/100");

                if (equipmentIcon != null && masterData.equipmentIcon != null)
                {
                    equipmentIcon.sprite = masterData.equipmentIcon;
                    if (equipmentIcon.gameObject != null)
                        equipmentIcon.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            // 装備が選択されていない場合
            SetUIText(equipmentNameText, "装備を選択");
            SetUIText(equipmentEnhanceText, "");
            SetUIText(equipmentStaminaText, "");

            // アイコンを非表示にするか、デフォルト表示
            if (equipmentIcon != null)
            {
                equipmentIcon.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 強化アイテムスロット表示を更新（修正版：スロットは常に表示）
    /// </summary>
    private void UpdateEnhanceItemSlotDisplay()
    {
        // 強化アイテムスロット表示エリアは常に表示
        if (enhanceItemSlotDisplay != null)
            enhanceItemSlotDisplay.SetActive(true);

        if (currentEnhanceItem != null)
        {
            SetUIText(enhanceItemNameText, currentEnhanceItem.enhanceItemName);
            SetUIText(enhanceItemDescText, $"成功率: {currentEnhanceItem.enhanceSuccessRate}%");

            if (enhanceItemIcon != null && currentEnhanceItem.enhanceItemIcon != null)
            {
                enhanceItemIcon.sprite = currentEnhanceItem.enhanceItemIcon;
                if (enhanceItemIcon.gameObject != null)
                    enhanceItemIcon.gameObject.SetActive(true);
            }
        }
        else
        {
            // 強化アイテムが選択されていない場合
            SetUIText(enhanceItemNameText, "強化アイテムを選択");
            SetUIText(enhanceItemDescText, "");

            // アイコンを非表示にする
            if (enhanceItemIcon != null)
            {
                enhanceItemIcon.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 補助材料スロット表示を更新（修正版：選択なしの場合もスロットは表示）
    /// </summary>
    private void UpdateSupportItemSlotDisplay()
    {
        // 補助材料スロット表示エリアは常に表示
        if (supportItemSlotDisplay != null)
            supportItemSlotDisplay.SetActive(true);

        if (currentSupportItem != null)
        {
            // 補助材料が選択されている場合
            SetUIText(supportItemNameText, currentSupportItem.supportItemName);
            SetUIText(supportItemDescText, $"成功率ボーナス: +{currentSupportItem.addEnhanceSuccessRate}%");

            if (supportItemIcon != null && currentSupportItem.supportItemIcon != null)
            {
                supportItemIcon.sprite = currentSupportItem.supportItemIcon;
                if (supportItemIcon.gameObject != null)
                    supportItemIcon.gameObject.SetActive(true);
            }
        }
        else
        {
            // 補助材料が選択されていない場合（選択なし状態）
            SetUIText(supportItemNameText, "補助材料 (任意)");
            SetUIText(supportItemDescText, "");

            // アイコンを非表示にするか、デフォルトアイコンを表示
            if (supportItemIcon != null)
            {
                // アイコン画像を非表示にして、スロット枠は残す
                supportItemIcon.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 詳細パネルを更新
    /// </summary>
    private void UpdateDetailPanels()
    {
        UpdateEquipmentDetailPanel();
        UpdateEnhanceEffectPanel();
        UpdateSupportEffectPanel();
    }

    /// <summary>
    /// 装備詳細パネルを更新（修正版：0の値は表示しない）
    /// </summary>
    private void UpdateEquipmentDetailPanel()
    {
        if (currentEquipment != null)
        {
            var masterData = MasterDataManager.Instance.GetEquipmentData(currentEquipment.equipmentMasterId);
            if (masterData != null)
            {
                var totalStats = currentEquipment.CalculateTotalStats(masterData);

                SetUIText(equipmentDetailText, $"レアリティ: {masterData.rarity}");
                SetUIText(equipmentAttributeText, $"属性: {currentEquipment.currentAttributeType}");

                // 詳細なステータス表示（0の場合は表示しない）
                var statusText = "";

                // 基本ステータス（0でない場合のみ表示）
                if (totalStats.hp > 0)
                    statusText += $"HP: {totalStats.hp}\n";
                if (totalStats.offense > 0)
                    statusText += $"攻撃: {totalStats.offense}\n";
                if (totalStats.defense > 0)
                    statusText += $"防御: {totalStats.defense}\n";
                if (totalStats.speed > 0)
                    statusText += $"速度: {totalStats.speed}\n";

                // クリティカル系（0でない場合のみ表示）
                if (totalStats.criticalRate > 0)
                    statusText += $"クリティカル率: {totalStats.criticalRate}%\n";
                if (totalStats.criticalDamageRate > 0)
                    statusText += $"クリティカルダメージ: {totalStats.criticalDamageRate}%\n";

                // 属性攻撃力（0でない場合のみ表示）
                if (totalStats.fireOffence > 0)
                    statusText += $"火属性攻撃: {totalStats.fireOffence}\n";
                if (totalStats.waterOffence > 0)
                    statusText += $"水属性攻撃: {totalStats.waterOffence}\n";
                if (totalStats.windOffence > 0)
                    statusText += $"風属性攻撃: {totalStats.windOffence}\n";
                if (totalStats.earthOffence > 0)
                    statusText += $"土属性攻撃: {totalStats.earthOffence}\n";

                // 末尾の改行を削除
                statusText = statusText.TrimEnd('\n');

                SetUIText(equipmentStatusText, statusText);
            }

            if (equipmentDetailPanel != null)
                equipmentDetailPanel.SetActive(true);
        }
        else
        {
            if (equipmentDetailPanel != null)
                equipmentDetailPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 強化効果パネルを更新（修正版：日本語表記対応）
    /// </summary>
    private void UpdateEnhanceEffectPanel()
    {
        if (currentEnhanceItem != null && currentEquipment != null)
        {
            var masterData = MasterDataManager.Instance.GetEquipmentData(currentEquipment.equipmentMasterId);
            if (masterData != null)
            {
                var statusIncrease = EnhanceCalculationUtility.CalculateStatusIncrease(
                    masterData.equipmentType, currentEnhanceItem);

                var effectText = "強化効果:\n";
                foreach (var kvp in statusIncrease)
                {
                    if (kvp.Value > 0)
                    {
                        // 英語のステータス名を日本語に変換
                        string japaneseStatusName = ConvertStatusNameToJapanese(kvp.Key);
                        effectText += $"{japaneseStatusName}: +{kvp.Value}\n";
                    }
                }

                SetUIText(enhanceEffectText, effectText);
            }

            // 強化アイテムの基本成功率を表示
            SetUIText(enhanceSuccessRateText, $"成功率: {currentEnhanceItem.enhanceSuccessRate}%");

            if (enhanceEffectPanel != null)
                enhanceEffectPanel.SetActive(true);
        }
        else
        {
            if (enhanceEffectPanel != null)
                enhanceEffectPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 英語のステータス名を日本語に変換（既存メソッドを拡張）
    /// </summary>
    /// <param name="englishStatusName">英語のステータス名</param>
    /// <returns>日本語のステータス名</returns>
    private string ConvertStatusNameToJapanese(string englishStatusName)
    {
        switch (englishStatusName.ToLower())
        {
            case "hp":
                return "HP";
            case "offense":
            case "offence":
                return "攻撃";
            case "defense":
            case "defence":
                return "防御";
            case "speed":
                return "速度";
            case "criticalrate":
                return "クリティカル率";
            case "criticaldamagerate":
                return "クリティカルダメージ";
            case "fireoffence":
            case "fireoffense":
                return "火属性攻撃";
            case "wateroffence":
            case "wateroffense":
                return "水属性攻撃";
            case "windoffence":
            case "windoffense":
                return "風属性攻撃";
            case "earthoffence":
            case "earthoffense":
                return "土属性攻撃";
            default:
                // 未知のステータス名の場合は元の名前を返す
                LogWarning($"未知のステータス名: {englishStatusName}");
                return englishStatusName;
        }
    }

    /// <summary>
    /// 属性タイプを日本語に変換
    /// </summary>
    /// <param name="attributeType">属性タイプ</param>
    /// <returns>日本語の属性名</returns>
    private string ConvertAttributeTypeToJapanese(AttributeType attributeType)
    {
        switch (attributeType)
        {
            case AttributeType.None:
                return "無属性";
            case AttributeType.Fire:
                return "火属性";
            case AttributeType.Water:
                return "水属性";
            case AttributeType.Wind:
                return "風属性";
            case AttributeType.Earth:
                return "土属性";
            default:
                return attributeType.ToString();
        }
    }

    /// <summary>
    /// 補助効果パネルを更新（修正版：descriptionを表示）
    /// </summary>
    private void UpdateSupportEffectPanel()
    {
        if (currentSupportItem != null)
        {
            var effectText = "補助効果:\n";
            if (currentSupportItem.addEnhanceSuccessRate > 0)
                effectText += $"成功率: +{currentSupportItem.addEnhanceSuccessRate}%\n";
            if (currentSupportItem.addEnhanceStamina > 0)
                effectText += $"耐久値: +{currentSupportItem.addEnhanceStamina}\n";
            if (currentSupportItem.multiplStatusUp > 1)
                effectText += $"ステータス倍率: x{currentSupportItem.multiplStatusUp}\n";

            SetUIText(supportEffectText, effectText);

            // 補助材料のdescriptionを表示
            SetUIText(supportBonusText, currentSupportItem.description);

            if (supportEffectPanel != null)
                supportEffectPanel.SetActive(true);
        }
        else
        {
            if (supportEffectPanel != null)
                supportEffectPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 実行ボタンの状態を更新
    /// </summary>
    private void UpdateExecuteButton()
    {
        if (executeButton != null)
        {
            bool canExecute = CanExecuteEnhance();
            executeButton.interactable = canExecute;

            SetUIText(executeButtonText, canExecute ? "強化実行" : "条件不足");
        }
    }

    /// <summary>
    /// 成功率表示を更新
    /// </summary>
    private void UpdateSuccessRate()
    {
        if (CanExecuteEnhance())
        {
            var preview = EquipmentEnhanceManager.Instance.GetEnhancePreview(
                selectedEquipmentId, selectedEnhanceItemId, selectedSupportItemId);

            if (preview != null)
            {
                SetUIText(finalSuccessRateText, $"成功率: {preview.finalSuccessRate:F1}%");
            }
        }
        else
        {
            SetUIText(finalSuccessRateText, "成功率: -");
        }
    }

    /// <summary>
    /// プレビューでのステータス変化詳細を日本語で取得
    /// </summary>
    /// <param name="preview">強化プレビューデータ</param>
    /// <returns>日本語のステータス変化詳細</returns>
    private string GetJapaneseStatusChangeDetails(EnhancePreviewData preview)
    {
        var details = "";

        foreach (var kvp in preview.expectedStatusIncrease)
        {
            if (kvp.Value != 0)
            {
                var currentValue = preview.currentStatuses.ContainsKey(kvp.Key) ? preview.currentStatuses[kvp.Key] : 0;
                var newValue = preview.expectedTotalStatuses.ContainsKey(kvp.Key) ? preview.expectedTotalStatuses[kvp.Key] : currentValue;

                // 英語のステータス名を日本語に変換
                string japaneseStatusName = ConvertStatusNameToJapanese(kvp.Key);
                details += $"{japaneseStatusName}: {currentValue} → {newValue} (+{kvp.Value})\n";
            }
        }

        return details.TrimEnd('\n');
    }

    #endregion

    #region Data Access Methods

    /// <summary>
    /// ユーザー装備データを取得
    /// </summary>
    /// <param name="equipmentId">装備のユーザーID</param>
    /// <returns>ユーザー装備データ</returns>
    private UserEquipmentData GetUserEquipmentData(string equipmentId)
    {
        var saveData = SaveDataManager.Instance.CurrentSaveData;
        return saveData?.equipments?.FirstOrDefault(e => e.userEquipmentId == equipmentId);
    }

    /// <summary>
    /// ユーザーの所持装備一覧を取得
    /// </summary>
    /// <returns>所持装備一覧</returns>
    private List<UserEquipmentData> GetUserEquipments()
    {
        var saveData = SaveDataManager.Instance.CurrentSaveData;
        return saveData?.equipments ?? new List<UserEquipmentData>();
    }

    /// <summary>
    /// ユーザーの所持補助材料一覧を取得
    /// </summary>
    /// <returns>所持補助材料一覧</returns>
    private List<SupportItemMasterData> GetUserSupportItems()
    {
        if (InventoryManager.Instance == null || !InventoryManager.Instance.IsInitialized)
        {
            LogWarning("InventoryManagerが初期化されていません");
            return new List<SupportItemMasterData>();
        }

        // 実際に所持している補助材料のマスターデータを取得
        var saveData = SaveDataManager.Instance.CurrentSaveData;
        if (saveData?.items == null)
        {
            LogWarning("セーブデータまたはアイテムリストがnullです");
            return new List<SupportItemMasterData>();
        }

        var supportItems = new List<SupportItemMasterData>();

        // 所持している補助材料のマスターデータを取得
        foreach (var userItem in saveData.items)
        {
            if (userItem.itemType == ItemType.SupportItem && userItem.quantity > 0)
            {
                var masterData = MasterDataManager.Instance.GetSupportItemData(userItem.itemMasterId);
                if (masterData != null)
                {
                    supportItems.Add(masterData);
                }
            }
        }

        LogDebug($"所持補助材料数: {supportItems.Count}");
        return supportItems;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 全ての選択をクリア
    /// </summary>
    private void ClearAllSelections()
    {
        selectedEquipmentId = string.Empty;
        selectedEnhanceItemId = 0;
        selectedSupportItemId = 0;

        currentEquipment = null;
        currentEnhanceItem = null;
        currentSupportItem = null;
    }

    /// <summary>
    /// UIテキストを安全に設定
    /// </summary>
    /// <param name="textComponent">テキストコンポーネント</param>
    /// <param name="text">設定するテキスト</param>
    private void SetUIText(TextMeshProUGUI textComponent, string text)
    {
        if (textComponent != null)
        {
            textComponent.text = text ?? "";
        }
    }

    #endregion

    #region Debug Methods

    /// <summary>
    /// デバッグログ出力
    /// </summary>
    /// <param name="message">メッセージ</param>
    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[EquipmentEnhanceUI] {message}");
        }
    }

    /// <summary>
    /// 警告ログ出力
    /// </summary>
    /// <param name="message">メッセージ</param>
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[EquipmentEnhanceUI] {message}");
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    /// <param name="message">メッセージ</param>
    private void LogError(string message)
    {
        Debug.LogError($"[EquipmentEnhanceUI] {message}");
    }

    /// <summary>
    /// 現在のアイテム所持状況をデバッグ出力
    /// </summary>
    [ContextMenu("Debug Item Status")]
    public void DebugItemStatus()
    {
        var saveData = SaveDataManager.Instance.CurrentSaveData;
        if (saveData?.items == null)
        {
            LogError("セーブデータまたはアイテムリストがnullです");
            return;
        }

        LogDebug($"=== アイテム所持状況 ===");
        LogDebug($"総アイテム記録数: {saveData.items.Count}");

        foreach (var item in saveData.items)
        {
            LogDebug($"ID:{item.itemMasterId}, Type:{item.itemType}, Qty:{item.quantity}");
        }

        LogDebug($"=== 補助材料 ===");
        var supportItems = GetUserSupportItems();
        LogDebug($"表示可能な補助材料数: {supportItems.Count}");
    }

    #endregion

    #region Inspector Context Menu

    /// <summary>
    /// UIを強制更新（Inspector用）
    /// </summary>
    [ContextMenu("Refresh UI")]
    public void ManualRefreshUI()
    {
        RefreshUI();
    }

    /// <summary>
    /// 選択状態をクリア（Inspector用）
    /// </summary>
    [ContextMenu("Clear Selections")]
    public void ManualClearSelections()
    {
        ClearAllSelections();
        RefreshUI();
    }

    #endregion
}