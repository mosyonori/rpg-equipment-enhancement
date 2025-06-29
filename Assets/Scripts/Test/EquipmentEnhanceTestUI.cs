using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 装備強化システムのテスト用UIクラス
/// EquipmentEnhanceManagerとUIを連携させる
/// </summary>
public class EquipmentEnhanceTestUI : MonoBehaviour
{
    [Header("装備情報表示")]
    [SerializeField] private TextMeshProUGUI equipmentNameText;
    [SerializeField] private TextMeshProUGUI enhanceValueText;
    [SerializeField] private TextMeshProUGUI attributeText;
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("アイテム選択")]
    [SerializeField] private TMP_Dropdown enhanceItemDropdown;
    [SerializeField] private TMP_Dropdown supportItemDropdown;

    [Header("プレビュー表示")]
    [SerializeField] private TextMeshProUGUI successRateText;
    [SerializeField] private TextMeshProUGUI statusChangeText;
    [SerializeField] private TextMeshProUGUI warningsText;

    [Header("アクションボタン")]
    [SerializeField] private Button previewButton;
    [SerializeField] private Button executeButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button createTestEquipmentButton;

    [Header("結果表示")]
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI resultDetailsText;

    [Header("ログ表示")]
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private ScrollRect logScrollRect;

    [Header("テスト設定")]
    [SerializeField] private bool enableAutoLog = true;

    // 内部変数
    private string currentEquipmentId = "";
    private List<EnhanceItemMasterData> availableEnhanceItems;
    private List<SupportItemMasterData> availableSupportItems;
    private List<string> logEntries = new List<string>();

    private void Start()
    {
        // Manager初期化を待機してからUI初期化
        StartCoroutine(WaitForManagersAndInitialize());
    }

    /// <summary>
    /// Manager初期化完了を待機してからUI初期化
    /// </summary>
    private System.Collections.IEnumerator WaitForManagersAndInitialize()
    {
        AddLog("=== Manager初期化待機中 ===");

        float timeout = 20f; // タイムアウトを20秒に延長
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (CheckManagersReady())
            {
                AddLog("✅ 全Manager初期化完了");
                yield return new WaitForSeconds(0.5f); // 少し待機してから初期化
                InitializeUI();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.2f); // 0.2秒間隔でチェック
        }

        AddLog("❌ Manager初期化がタイムアウトしました");
        AddLog("シーンにMasterDataManager、SaveDataManager、EquipmentEnhanceManagerが配置されているか確認してください");
        AddLog("MasterDataManagerでGameDataがResourcesフォルダに正しく配置されているか確認してください");
    }

    /// <summary>
    /// UIの初期化
    /// </summary>
    private void InitializeUI()
    {
        AddLog("=== 装備強化テストUI初期化開始 ===");

        // UI要素の参照チェック
        if (!CheckUIReferences())
        {
            AddLog("❌ UI要素の参照が設定されていません");
            return;
        }

        // Manager初期化チェック
        if (!CheckManagersReady())
        {
            AddLog("❌ 必要なManagerが初期化されていません");
            return;
        }

        // イベントハンドラーの設定
        SetupEventHandlers();

        // アイテムリストを取得
        LoadAvailableItems();

        // ドロップダウンを初期化
        InitializeDropdowns();

        // テスト用装備を準備
        PrepareTestEquipment();

        // UI表示更新
        RefreshUI();

        AddLog("✅ UI初期化完了");
    }

    /// <summary>
    /// UI要素の参照チェック
    /// </summary>
    private bool CheckUIReferences()
    {
        bool allReferencesSet = true;

        // 必須UI要素をチェック
        if (equipmentNameText == null)
        {
            Debug.LogError("[EquipmentEnhanceTestUI] equipmentNameText が設定されていません");
            allReferencesSet = false;
        }

        if (enhanceItemDropdown == null)
        {
            Debug.LogError("[EquipmentEnhanceTestUI] enhanceItemDropdown が設定されていません");
            allReferencesSet = false;
        }

        if (previewButton == null)
        {
            Debug.LogError("[EquipmentEnhanceTestUI] previewButton が設定されていません");
            allReferencesSet = false;
        }

        if (executeButton == null)
        {
            Debug.LogError("[EquipmentEnhanceTestUI] executeButton が設定されていません");
            allReferencesSet = false;
        }

        if (logText == null)
        {
            Debug.LogWarning("[EquipmentEnhanceTestUI] logText が設定されていません（ログ表示が無効になります）");
        }

        return allReferencesSet;
    }

    /// <summary>
    /// イベントハンドラーの設定
    /// </summary>
    private void SetupEventHandlers()
    {
        // ボタンイベント設定（null チェック付き）
        if (previewButton != null)
            previewButton.onClick.AddListener(OnPreviewButtonClicked);

        if (executeButton != null)
            executeButton.onClick.AddListener(OnExecuteButtonClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetButtonClicked);

        if (createTestEquipmentButton != null)
            createTestEquipmentButton.onClick.AddListener(OnCreateTestEquipmentClicked);

        // ドロップダウンイベント設定（null チェック付き）
        if (enhanceItemDropdown != null)
            enhanceItemDropdown.onValueChanged.AddListener(OnEnhanceItemChanged);

        if (supportItemDropdown != null)
            supportItemDropdown.onValueChanged.AddListener(OnSupportItemChanged);

        // Managerのイベント購読
        if (EquipmentEnhanceManager.Instance != null)
        {
            EquipmentEnhanceManager.Instance.OnEnhanceCompleted += OnEnhanceCompleted;
            EquipmentEnhanceManager.Instance.OnEnhanceError += OnEnhanceError;
        }
    }

    /// <summary>
    /// Managerの準備状況チェック
    /// </summary>
    private bool CheckManagersReady()
    {
        // MasterDataManagerチェック
        if (MasterDataManager.Instance == null)
        {
            AddLog("MasterDataManager.Instanceがnullです");
            return false;
        }

        if (!MasterDataManager.Instance.IsDataLoaded)
        {
            AddLog($"MasterDataManagerのデータ読み込み待機中... (IsDataLoaded: {MasterDataManager.Instance.IsDataLoaded})");
            return false;
        }

        // SaveDataManagerチェック
        if (SaveDataManager.Instance == null)
        {
            AddLog("SaveDataManager.Instanceがnullです");
            return false;
        }

        if (!SaveDataManager.Instance.IsDataLoaded)
        {
            AddLog($"SaveDataManagerのデータ読み込み待機中... (IsDataLoaded: {SaveDataManager.Instance.IsDataLoaded})");
            return false;
        }

        // EquipmentEnhanceManagerチェック
        if (EquipmentEnhanceManager.Instance == null)
        {
            AddLog("EquipmentEnhanceManager.Instanceがnullです");
            return false;
        }

        if (!EquipmentEnhanceManager.Instance.IsInitialized)
        {
            AddLog($"EquipmentEnhanceManagerの初期化待機中... (IsInitialized: {EquipmentEnhanceManager.Instance.IsInitialized})");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 利用可能なアイテムを読み込み
    /// </summary>
    private void LoadAvailableItems()
    {
        availableEnhanceItems = EquipmentEnhanceManager.Instance.GetAvailableEnhanceItems();
        availableSupportItems = MasterDataManager.Instance.GetSupportItemDataList();

        AddLog($"強化アイテム: {availableEnhanceItems.Count}種類");
        AddLog($"補助材料: {availableSupportItems.Count}種類");
    }

    /// <summary>
    /// ドロップダウンの初期化
    /// </summary>
    private void InitializeDropdowns()
    {
        // 強化アイテムドロップダウン
        if (enhanceItemDropdown != null && availableEnhanceItems != null)
        {
            enhanceItemDropdown.ClearOptions();
            var enhanceOptions = availableEnhanceItems.Select(item =>
                $"[{item.rarity}] {item.enhanceItemName} (成功率:{item.enhanceSuccessRate}%)").ToList();
            enhanceItemDropdown.AddOptions(enhanceOptions);
        }

        // 補助材料ドロップダウン
        if (supportItemDropdown != null && availableSupportItems != null)
        {
            supportItemDropdown.ClearOptions();
            var supportOptions = new List<string> { "なし" };
            supportOptions.AddRange(availableSupportItems.Select(item =>
                $"[{item.rarity}] {item.supportItemName}"));
            supportItemDropdown.AddOptions(supportOptions);
        }
    }

    /// <summary>
    /// テスト用装備を準備
    /// </summary>
    private void PrepareTestEquipment()
    {
        var saveData = SaveDataManager.Instance.CurrentSaveData;

        if (saveData.equipments.Count > 0)
        {
            currentEquipmentId = saveData.equipments[0].userEquipmentId;
            AddLog($"既存装備を使用: {currentEquipmentId}");
        }
        else
        {
            CreateTestEquipment();
        }
    }

    /// <summary>
    /// テスト用装備を作成
    /// </summary>
    private void CreateTestEquipment()
    {
        var masterData = MasterDataManager.Instance.GetEquipmentData(1); // 初心者の剣
        if (masterData != null)
        {
            var testEquipment = new UserEquipmentData(masterData);
            var saveData = SaveDataManager.Instance.CurrentSaveData;
            saveData.equipments.Add(testEquipment);

            currentEquipmentId = testEquipment.userEquipmentId;
            SaveDataManager.Instance.MarkDataDirty();
            SaveDataManager.Instance.SaveSaveData();

            AddLog($"✅ テスト用装備作成: {masterData.equipmentName}");
        }
        else
        {
            AddLog("❌ テスト用装備のマスターデータが見つかりません");
        }
    }

    /// <summary>
    /// UI表示を更新
    /// </summary>
    private void RefreshUI()
    {
        UpdateEquipmentInfo();
        UpdatePreview();
        UpdateButtonStates();
    }

    /// <summary>
    /// 装備情報表示を更新
    /// </summary>
    private void UpdateEquipmentInfo()
    {
        if (string.IsNullOrEmpty(currentEquipmentId))
        {
            SetEquipmentInfoText("装備なし", "-", "-", "-", "-");
            return;
        }

        var equipment = GetCurrentEquipment();
        var masterData = GetCurrentEquipmentMaster();

        if (equipment != null && masterData != null)
        {
            var name = masterData.equipmentName;
            var enhanceValue = $"強化値: +{equipment.currentEnhancedValue}";
            var attribute = $"属性: {equipment.currentAttributeType}";
            var stamina = $"耐久値: {equipment.currentEnhanceStamina}/100";

            var totalStats = equipment.CalculateTotalStats(masterData);
            var status = $"HP:{totalStats.hp} 攻撃:{totalStats.offense} 防御:{totalStats.defense}";

            SetEquipmentInfoText(name, enhanceValue, attribute, stamina, status);
        }
        else
        {
            SetEquipmentInfoText("装備データエラー", "-", "-", "-", "-");
        }
    }

    /// <summary>
    /// 装備情報テキストを設定（null チェック付き）
    /// </summary>
    private void SetEquipmentInfoText(string name, string enhance, string attribute, string stamina, string status)
    {
        if (equipmentNameText != null) equipmentNameText.text = name;
        if (enhanceValueText != null) enhanceValueText.text = enhance;
        if (attributeText != null) attributeText.text = attribute;
        if (staminaText != null) staminaText.text = stamina;
        if (statusText != null) statusText.text = status;
    }

    /// <summary>
    /// プレビュー表示を更新
    /// </summary>
    private void UpdatePreview()
    {
        if (string.IsNullOrEmpty(currentEquipmentId) ||
            enhanceItemDropdown == null ||
            availableEnhanceItems == null ||
            enhanceItemDropdown.value < 0 ||
            enhanceItemDropdown.value >= availableEnhanceItems.Count)
        {
            SetPreviewText("成功率: -", "変化: -", "");
            return;
        }

        var enhanceItemId = availableEnhanceItems[enhanceItemDropdown.value].enhanceItemId;
        var supportItemId = (supportItemDropdown != null && supportItemDropdown.value > 0 && availableSupportItems != null) ?
            availableSupportItems[supportItemDropdown.value - 1].supportItemId : 0;

        var preview = EquipmentEnhanceManager.Instance.GetEnhancePreview(
            currentEquipmentId, enhanceItemId, supportItemId);

        if (preview != null)
        {
            var successRate = $"成功率: {preview.finalSuccessRate:F1}%";
            var statusChange = preview.GetStatusChangeDetails();

            var warnings = string.Join("\n", preview.warningMessages);
            var risks = string.Join("\n", preview.riskMessages);
            var warningText = !string.IsNullOrEmpty(warnings + risks) ? warnings + "\n" + risks : "特に問題なし";

            SetPreviewText(successRate, statusChange, warningText);

            // 成功率に応じて色を変更
            if (successRateText != null)
            {
                if (preview.finalSuccessRate >= 80f)
                    successRateText.color = Color.green;
                else if (preview.finalSuccessRate >= 50f)
                    successRateText.color = Color.yellow;
                else
                    successRateText.color = Color.red;
            }
        }
    }

    /// <summary>
    /// プレビューテキストを設定（null チェック付き）
    /// </summary>
    private void SetPreviewText(string successRate, string statusChange, string warnings)
    {
        if (successRateText != null) successRateText.text = successRate;
        if (statusChangeText != null) statusChangeText.text = statusChange;
        if (warningsText != null) warningsText.text = warnings;
    }

    /// <summary>
    /// ボタンの状態を更新
    /// </summary>
    private void UpdateButtonStates()
    {
        bool hasEquipment = !string.IsNullOrEmpty(currentEquipmentId);
        bool hasEnhanceItem = enhanceItemDropdown != null &&
                             availableEnhanceItems != null &&
                             enhanceItemDropdown.value >= 0 &&
                             enhanceItemDropdown.value < availableEnhanceItems.Count;
        bool canExecute = hasEquipment && hasEnhanceItem;

        if (hasEquipment && hasEnhanceItem)
        {
            var enhanceItemId = availableEnhanceItems[enhanceItemDropdown.value].enhanceItemId;
            canExecute = EquipmentEnhanceManager.Instance.CanExecuteEnhance(currentEquipmentId, enhanceItemId);
        }

        // ボタン状態更新（null チェック付き）
        if (previewButton != null) previewButton.interactable = hasEquipment && hasEnhanceItem;
        if (executeButton != null) executeButton.interactable = canExecute;
        if (resetButton != null) resetButton.interactable = hasEquipment;
    }

    /// <summary>
    /// プレビューボタンクリック
    /// </summary>
    private void OnPreviewButtonClicked()
    {
        AddLog("--- プレビュー実行 ---");
        UpdatePreview();

        var enhanceItemId = availableEnhanceItems[enhanceItemDropdown.value].enhanceItemId;
        var supportItemId = supportItemDropdown.value > 0 ?
            availableSupportItems[supportItemDropdown.value - 1].supportItemId : 0;

        var preview = EquipmentEnhanceManager.Instance.GetEnhancePreview(
            currentEquipmentId, enhanceItemId, supportItemId);

        if (preview != null)
        {
            AddLog($"成功率詳細: {preview.GetSuccessRateDetails()}");
            AddLog($"ステータス変化: {preview.GetStatusChangeDetails()}");
        }
    }

    /// <summary>
    /// 強化実行ボタンクリック
    /// </summary>
    private void OnExecuteButtonClicked()
    {
        AddLog("--- 強化実行 ---");

        var enhanceItemId = availableEnhanceItems[enhanceItemDropdown.value].enhanceItemId;
        var supportItemId = supportItemDropdown.value > 0 ?
            availableSupportItems[supportItemDropdown.value - 1].supportItemId : 0;

        AddLog($"使用アイテム: {availableEnhanceItems[enhanceItemDropdown.value].enhanceItemName}");
        if (supportItemId > 0)
        {
            AddLog($"補助材料: {availableSupportItems[supportItemDropdown.value - 1].supportItemName}");
        }

        var result = EquipmentEnhanceManager.Instance.ExecuteEnhance(
            currentEquipmentId, enhanceItemId, supportItemId);

        // 結果は OnEnhanceCompleted で処理される
    }

    /// <summary>
    /// リセットボタンクリック
    /// </summary>
    private void OnResetButtonClicked()
    {
        var equipment = GetCurrentEquipment();
        if (equipment != null)
        {
            // 装備をリセット
            equipment.currentEnhancedValue = 0;
            equipment.currentEnhanceStamina = 100;
            equipment.currentAttributeType = AttributeType.None;
            equipment.ResetEnhancedStats();

            SaveDataManager.Instance.MarkDataDirty();
            SaveDataManager.Instance.SaveSaveData();

            AddLog("✅ 装備をリセットしました");
            RefreshUI();
        }
    }

    /// <summary>
    /// テスト装備作成ボタンクリック
    /// </summary>
    private void OnCreateTestEquipmentClicked()
    {
        CreateTestEquipment();
        RefreshUI();
    }

    /// <summary>
    /// 強化アイテム変更時
    /// </summary>
    private void OnEnhanceItemChanged(int index)
    {
        UpdatePreview();
        UpdateButtonStates();
    }

    /// <summary>
    /// 補助材料変更時
    /// </summary>
    private void OnSupportItemChanged(int index)
    {
        UpdatePreview();
        UpdateButtonStates();
    }

    /// <summary>
    /// 強化完了イベント
    /// </summary>
    private void OnEnhanceCompleted(EnhanceResultData result)
    {
        if (result != null)
        {
            string resultText = result.isSuccess ? "✅ 強化成功！" : "❌ 強化失敗...";
            AddLog($"{resultText} (成功率: {result.actualSuccessRate:F1}%)");

            if (resultTitleText != null) resultTitleText.text = resultText;
            if (resultDetailsText != null) resultDetailsText.text = result.ToString();

            if (result.isSuccess)
            {
                if (resultTitleText != null) resultTitleText.color = Color.green;
                foreach (var statusChange in result.statusChanges)
                {
                    AddLog($"  {statusChange.Key}: +{statusChange.Value}");
                }
            }
            else
            {
                if (resultTitleText != null) resultTitleText.color = Color.red;
            }

            RefreshUI();
        }
    }

    /// <summary>
    /// 強化エラーイベント
    /// </summary>
    private void OnEnhanceError(string errorMessage)
    {
        AddLog($"❌ エラー: {errorMessage}");
        if (resultTitleText != null)
        {
            resultTitleText.text = "エラー発生";
            resultTitleText.color = Color.red;
        }
        if (resultDetailsText != null) resultDetailsText.text = errorMessage;
    }

    /// <summary>
    /// 現在の装備データを取得
    /// </summary>
    private UserEquipmentData GetCurrentEquipment()
    {
        var saveData = SaveDataManager.Instance.CurrentSaveData;
        return saveData?.equipments?.FirstOrDefault(e => e.userEquipmentId == currentEquipmentId);
    }

    /// <summary>
    /// 現在の装備のマスターデータを取得
    /// </summary>
    private EquipmentMasterData GetCurrentEquipmentMaster()
    {
        var equipment = GetCurrentEquipment();
        return equipment != null ? MasterDataManager.Instance.GetEquipmentData(equipment.equipmentMasterId) : null;
    }

    /// <summary>
    /// ログにメッセージを追加
    /// </summary>
    private void AddLog(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string logEntry = $"[{timestamp}] {message}";

        logEntries.Add(logEntry);

        // ログの最大行数制限
        if (logEntries.Count > 100)
        {
            logEntries.RemoveAt(0);
        }

        // UI更新
        if (logText != null)
        {
            logText.text = string.Join("\n", logEntries);

            // スクロールを最下部に移動
            if (logScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                logScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        // コンソールにも出力
        if (enableAutoLog)
        {
            Debug.Log($"[EnhanceTestUI] {message}");
        }
    }

    /// <summary>
    /// 手動でUI更新（Inspector用）
    /// </summary>
    [ContextMenu("Refresh UI")]
    public void ManualRefreshUI()
    {
        RefreshUI();
    }

    /// <summary>
    /// ログクリア（Inspector用）
    /// </summary>
    [ContextMenu("Clear Log")]
    public void ClearLog()
    {
        logEntries.Clear();
        if (logText != null)
        {
            logText.text = "";
        }
        AddLog("ログをクリアしました");
    }

    private void OnDestroy()
    {
        // イベント購読解除
        if (EquipmentEnhanceManager.Instance != null)
        {
            EquipmentEnhanceManager.Instance.OnEnhanceCompleted -= OnEnhanceCompleted;
            EquipmentEnhanceManager.Instance.OnEnhanceError -= OnEnhanceError;
        }
    }
}