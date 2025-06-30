using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// アイテム選択モーダルウィンドウUI（アイテム詳細パネル追加版）
/// 装備、強化アイテム、補助材料の選択ウィンドウを管理
/// データアクセス統一ルール: UI層 → DataManager → データ層
/// </summary>
public class ItemSelectionWindowUI : MonoBehaviour
{
    #region Enums

    /// <summary>
    /// アイテムタイプ定義
    /// </summary>
    public enum ItemType
    {
        Equipment,
        EnhanceItem,
        SupportItem
    }

    #endregion

    #region Events

    /// <summary>
    /// アイテム選択完了イベント（強化アイテム・補助材料用）
    /// </summary>
    public event Action<ItemType, int> OnItemSelected;

    /// <summary>
    /// 装備選択完了イベント（装備専用：文字列ID用）
    /// </summary>
    public event Action<string> OnEquipmentSelected;

    #endregion

    #region UI References

    [Header("ウィンドウ全体")]
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private GameObject backgroundDim;
    [SerializeField] private Button backgroundButton; // 背景クリックで閉じる用

    [Header("ウィンドウヘッダー")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;

    [Header("アイテム一覧")]
    [SerializeField] private ScrollRect itemScrollRect;
    [SerializeField] private Transform itemGridParent;
    [SerializeField] private GridLayoutGroup itemGridLayout;

    [Header("アイテム詳細パネル")]
    [SerializeField] private GameObject itemInfoPanel;
    [SerializeField] private TextMeshProUGUI itemInfoTitleText;
    [SerializeField] private TextMeshProUGUI itemInfoDetailsText;

    [Header("補助材料専用")]
    [SerializeField] private Button noneSelectionButton;
    [SerializeField] private TextMeshProUGUI noneSelectionText;

    [Header("決定・キャンセル")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI confirmButtonText;

    [Header("プレハブ")]
    [SerializeField] private GameObject equipmentSlotPrefab;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private GameObject noneSelectionSlotPrefab; // 「選択なし」専用プレハブ

    #endregion

    #region Private Fields

    private ItemType currentItemType;
    private int selectedItemId;
    private int previousSelectedItemId; // 前回選択されていたアイテム
    private string selectedEquipmentUserId; // 装備選択時の文字列ID

    private List<GameObject> currentSlotObjects = new List<GameObject>();

    // 現在表示中のデータ保持用
    private List<UserEquipmentData> currentEquipments;
    private List<EnhanceItemMasterData> currentEnhanceItems;
    private List<SupportItemMasterData> currentSupportItems;

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
        HideWindow();
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
        LogDebug("ItemSelectionWindowUI初期化開始");

        // 初期状態では非表示
        if (windowRoot != null)
        {
            windowRoot.SetActive(false);
        }

        if (backgroundDim != null)
        {
            backgroundDim.SetActive(false);
        }

        // アイテム詳細パネルを初期化
        if (itemInfoPanel != null)
        {
            itemInfoPanel.SetActive(false);
        }

        LogDebug("ItemSelectionWindowUI初期化完了");
    }

    /// <summary>
    /// イベントリスナー設定
    /// </summary>
    private void SetupEventListeners()
    {
        // 閉じるボタンのイベント
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);

        // 背景ボタンのイベント
        if (backgroundButton != null)
            backgroundButton.onClick.AddListener(OnBackgroundClicked);

        // 補助材料の「選択無し」ボタンのイベント
        if (noneSelectionButton != null)
            noneSelectionButton.onClick.AddListener(OnNoneSelectionClicked);

        // 決定・キャンセルボタンのイベント
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
    }

    /// <summary>
    /// イベントリスナー削除
    /// </summary>
    private void RemoveEventListeners()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();

        if (backgroundButton != null)
            backgroundButton.onClick.RemoveAllListeners();

        if (noneSelectionButton != null)
            noneSelectionButton.onClick.RemoveAllListeners();

        if (confirmButton != null)
            confirmButton.onClick.RemoveAllListeners();

        if (cancelButton != null)
            cancelButton.onClick.RemoveAllListeners();
    }

    #endregion

    #region Public Methods - Show Window

    /// <summary>
    /// 装備選択ウィンドウを表示
    /// </summary>
    /// <param name="equipments">選択可能な装備一覧</param>
    /// <param name="currentSelectedId">現在選択されている装備ID</param>
    public void ShowEquipmentSelection(List<UserEquipmentData> equipments, string currentSelectedId)
    {
        LogDebug($"装備選択ウィンドウ表示: {equipments?.Count}個");

        currentItemType = ItemType.Equipment;
        currentEquipments = equipments;
        // 装備の場合は文字列IDなので、現在選択中の装備のマスターIDを取得
        previousSelectedItemId = 0;
        selectedEquipmentUserId = currentSelectedId;

        if (!string.IsNullOrEmpty(currentSelectedId))
        {
            var currentEquipment = equipments?.FirstOrDefault(e => e.userEquipmentId == currentSelectedId);
            if (currentEquipment != null)
            {
                previousSelectedItemId = currentEquipment.equipmentMasterId;
            }
        }

        SetWindowTitle("装備を選択");
        ShowNoneSelectionButton(false);
        CreateEquipmentSlots(equipments, currentSelectedId);
        ShowWindow();
    }

    /// <summary>
    /// 強化アイテム選択ウィンドウを表示
    /// </summary>
    /// <param name="enhanceItems">選択可能な強化アイテム一覧</param>
    /// <param name="currentSelectedId">現在選択されている強化アイテムID</param>
    public void ShowEnhanceItemSelection(List<EnhanceItemMasterData> enhanceItems, int currentSelectedId)
    {
        LogDebug($"強化アイテム選択ウィンドウ表示: {enhanceItems?.Count}個");

        currentItemType = ItemType.EnhanceItem;
        currentEnhanceItems = enhanceItems;
        previousSelectedItemId = currentSelectedId;

        SetWindowTitle("強化アイテムを選択");
        ShowNoneSelectionButton(false);
        CreateEnhanceItemSlots(enhanceItems, currentSelectedId);
        ShowWindow();
    }

    /// <summary>
    /// 補助材料選択ウィンドウを表示
    /// </summary>
    /// <param name="supportItems">選択可能な補助材料一覧</param>
    /// <param name="currentSelectedId">現在選択されている補助材料ID</param>
    public void ShowSupportItemSelection(List<SupportItemMasterData> supportItems, int currentSelectedId)
    {
        LogDebug($"補助材料選択ウィンドウ表示: {supportItems?.Count}個");

        currentItemType = ItemType.SupportItem;
        currentSupportItems = supportItems;
        previousSelectedItemId = currentSelectedId;

        SetWindowTitle("補助材料を選択");
        ShowNoneSelectionButton(false); // 従来の「選択無し」ボタンは非表示
        CreateSupportItemSlots(supportItems, currentSelectedId);
        ShowWindow();
    }

    #endregion

    #region Private Methods - Window Control

    /// <summary>
    /// ウィンドウを表示
    /// </summary>
    private void ShowWindow()
    {
        if (backgroundDim != null)
            backgroundDim.SetActive(true);

        if (windowRoot != null)
            windowRoot.SetActive(true);

        // 選択状態をリセット
        selectedItemId = previousSelectedItemId;
        UpdateConfirmButton();

        LogDebug("アイテム選択ウィンドウ表示");
    }

    /// <summary>
    /// ウィンドウを非表示
    /// </summary>
    private void HideWindow()
    {
        if (windowRoot != null)
            windowRoot.SetActive(false);

        if (backgroundDim != null)
            backgroundDim.SetActive(false);

        // アイテム詳細パネルを非表示
        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(false);

        // スロットをクリア
        ClearCurrentSlots();

        LogDebug("アイテム選択ウィンドウ非表示");
    }

    /// <summary>
    /// ウィンドウタイトルを設定
    /// </summary>
    /// <param name="title">タイトル</param>
    private void SetWindowTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
    }

    /// <summary>
    /// 「選択無し」ボタンの表示/非表示を設定
    /// </summary>
    /// <param name="show">表示する場合true</param>
    private void ShowNoneSelectionButton(bool show)
    {
        if (noneSelectionButton != null)
        {
            noneSelectionButton.gameObject.SetActive(show);
        }
    }

    #endregion

    #region Private Methods - Slot Creation

    /// <summary>
    /// 修正: 装備スロットを作成（お気に入り・ロック状態を表示順序に反映）
    /// </summary>
    /// <param name="equipments">装備一覧</param>
    /// <param name="currentSelectedId">現在選択されている装備ID</param>
    private void CreateEquipmentSlots(List<UserEquipmentData> equipments, string currentSelectedId)
    {
        ClearCurrentSlots();

        if (equipments == null || equipmentSlotPrefab == null || itemGridParent == null)
        {
            LogWarning("装備スロット作成に必要な要素が不足しています");
            return;
        }

        // 修正: 装備をお気に入り・ロック状態で並び替え（お気に入り優先、その後ロック優先）
        var sortedEquipments = SortEquipmentsByPriority(equipments);

        foreach (var equipment in sortedEquipments)
        {
            var slotObj = Instantiate(equipmentSlotPrefab, itemGridParent);
            currentSlotObjects.Add(slotObj);

            // スロットにデータを設定
            var slotComponent = slotObj.GetComponent<ItemSelectionSlotUI>();
            if (slotComponent != null)
            {
                var masterData = MasterDataManager.Instance.GetEquipmentData(equipment.equipmentMasterId);
                if (masterData != null)
                {
                    slotComponent.SetupAsEquipment(
                        equipment,
                        masterData,
                        equipment.userEquipmentId == currentSelectedId,
                        () => OnEquipmentSlotClicked(equipment.userEquipmentId) // 修正：ユーザーIDを直接渡す
                    );
                }
            }
        }

        LogDebug($"装備スロット作成完了: {sortedEquipments.Count}個（お気に入り優先ソート適用）");
    }

    /// <summary>
    /// 修正: 装備をお気に入り・ロック状態で優先順位付けしてソート
    /// </summary>
    /// <param name="equipments">ソート対象の装備一覧</param>
    /// <returns>ソート済み装備一覧</returns>
    private List<UserEquipmentData> SortEquipmentsByPriority(List<UserEquipmentData> equipments)
    {
        return equipments.OrderByDescending(eq => eq.isFavorite ? 1 : 0)  // お気に入り優先
                        .ThenByDescending(eq => eq.isLocked ? 1 : 0)      // 次にロック優先
                        .ThenByDescending(eq => eq.currentEnhancedValue)  // 次に強化値順
                        .ThenBy(eq => eq.equipmentMasterId)              // 最後にマスターID順
                        .ToList();
    }

    /// <summary>
    /// 強化アイテムスロットを作成
    /// </summary>
    /// <param name="enhanceItems">強化アイテム一覧</param>
    /// <param name="currentSelectedId">現在選択されている強化アイテムID</param>
    private void CreateEnhanceItemSlots(List<EnhanceItemMasterData> enhanceItems, int currentSelectedId)
    {
        ClearCurrentSlots();

        if (enhanceItems == null || itemSlotPrefab == null || itemGridParent == null)
        {
            LogWarning("強化アイテムスロット作成に必要な要素が不足しています");
            return;
        }

        foreach (var enhanceItem in enhanceItems)
        {
            var slotObj = Instantiate(itemSlotPrefab, itemGridParent);
            currentSlotObjects.Add(slotObj);

            // スロットにデータを設定
            var slotComponent = slotObj.GetComponent<ItemSelectionSlotUI>();
            if (slotComponent != null)
            {
                slotComponent.SetupAsEnhanceItem(
                    enhanceItem,
                    enhanceItem.enhanceItemId == currentSelectedId,
                    () => OnSlotClicked(enhanceItem.enhanceItemId)
                );
            }
        }

        LogDebug($"強化アイテムスロット作成完了: {enhanceItems.Count}個");
    }

    /// <summary>
    /// 補助材料スロットを作成（修正版：「選択なし」をGrid内に組み込み）
    /// </summary>
    /// <param name="supportItems">補助材料一覧</param>
    /// <param name="currentSelectedId">現在選択されている補助材料ID</param>
    private void CreateSupportItemSlots(List<SupportItemMasterData> supportItems, int currentSelectedId)
    {
        ClearCurrentSlots();

        if (itemSlotPrefab == null || itemGridParent == null)
        {
            LogWarning("補助材料スロット作成に必要な要素が不足しています");
            return;
        }

        // 1. 最初に「選択なし」スロットを作成
        if (noneSelectionSlotPrefab != null)
        {
            var noneSlotObj = Instantiate(noneSelectionSlotPrefab, itemGridParent);
            currentSlotObjects.Add(noneSlotObj);

            var noneSlotComponent = noneSlotObj.GetComponent<NoneSelectionSlotUI>();
            if (noneSlotComponent != null)
            {
                // 既存のNoneSelectionSlotUIに合わせてSetClickCallbackを使用
                noneSlotComponent.SetClickCallback(() => OnNoneSelectionClicked());
                noneSlotComponent.UpdateSelectionState(currentSelectedId == 0);
            }
        }
        else
        {
            LogWarning("noneSelectionSlotPrefabが設定されていません");
        }

        // 2. 補助材料スロットを作成
        if (supportItems != null)
        {
            foreach (var supportItem in supportItems)
            {
                var slotObj = Instantiate(itemSlotPrefab, itemGridParent);
                currentSlotObjects.Add(slotObj);

                // スロットにデータを設定
                var slotComponent = slotObj.GetComponent<ItemSelectionSlotUI>();
                if (slotComponent != null)
                {
                    slotComponent.SetupAsSupportItem(
                        supportItem,
                        supportItem.supportItemId == currentSelectedId,
                        () => OnSlotClicked(supportItem.supportItemId)
                    );
                }
            }
        }

        LogDebug($"補助材料スロット作成完了: 選択なし1個 + {supportItems?.Count ?? 0}個");
    }

    /// <summary>
    /// 現在のスロットをクリア
    /// </summary>
    private void ClearCurrentSlots()
    {
        foreach (var slotObj in currentSlotObjects)
        {
            if (slotObj != null)
            {
                Destroy(slotObj);
            }
        }
        currentSlotObjects.Clear();
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// スロットクリック時の処理（強化アイテム・補助材料用）
    /// </summary>
    /// <param name="itemId">クリックされたアイテムID</param>
    private void OnSlotClicked(int itemId)
    {
        LogDebug($"スロットクリック: {itemId}");

        selectedItemId = itemId;
        UpdateSlotSelection();
        UpdateConfirmButton();

        // アイテム詳細を表示
        ShowItemInfo(itemId);
    }

    /// <summary>
    /// 装備スロットクリック時の処理（文字列ID対応）
    /// </summary>
    /// <param name="equipmentUserId">クリックされた装備のユーザーID</param>
    private void OnEquipmentSlotClicked(string equipmentUserId)
    {
        LogDebug($"装備スロットクリック: {equipmentUserId}");

        // 装備の場合はマスターIDを取得してUI用に使用
        var equipment = GetEquipmentByUserId(equipmentUserId);
        if (equipment != null)
        {
            selectedItemId = equipment.equipmentMasterId;
            selectedEquipmentUserId = equipmentUserId;
            UpdateSlotSelection();
            UpdateConfirmButton();

            // 装備詳細を表示
            ShowEquipmentInfo(equipment);
        }
    }

    /// <summary>
    /// 「選択無し」ボタンクリック時の処理
    /// </summary>
    private void OnNoneSelectionClicked()
    {
        LogDebug("「選択無し」ボタンクリック");

        selectedItemId = 0; // 0は選択無しを表す
        UpdateSlotSelection();
        UpdateConfirmButton();

        // アイテム詳細パネルを非表示
        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(false);
    }

    /// <summary>
    /// 決定ボタンクリック時の処理
    /// </summary>
    private void OnConfirmButtonClicked()
    {
        LogDebug($"決定ボタンクリック: {currentItemType}, ID: {selectedItemId}");

        if (currentItemType == ItemType.Equipment && !string.IsNullOrEmpty(selectedEquipmentUserId))
        {
            // 装備の場合は文字列IDでイベントを発火
            OnEquipmentSelected?.Invoke(selectedEquipmentUserId);
        }
        else
        {
            // その他のアイテムは従来通り
            OnItemSelected?.Invoke(currentItemType, selectedItemId);
        }

        // ウィンドウを閉じる
        HideWindow();
    }

    /// <summary>
    /// キャンセルボタンクリック時の処理
    /// </summary>
    private void OnCancelButtonClicked()
    {
        LogDebug("キャンセルボタンクリック");
        HideWindow();
    }

    /// <summary>
    /// 閉じるボタンクリック時の処理
    /// </summary>
    private void OnCloseButtonClicked()
    {
        LogDebug("閉じるボタンクリック");
        HideWindow();
    }

    /// <summary>
    /// 背景クリック時の処理
    /// </summary>
    private void OnBackgroundClicked()
    {
        LogDebug("背景クリック");
        HideWindow();
    }

    #endregion

    #region Item Info Display

    /// <summary>
    /// 修正: 装備詳細を表示（お気に入り・ロック状態を含む）
    /// </summary>
    /// <param name="equipment">装備データ</param>
    private void ShowEquipmentInfo(UserEquipmentData equipment)
    {
        if (equipment == null || itemInfoPanel == null) return;

        var masterData = MasterDataManager.Instance.GetEquipmentData(equipment.equipmentMasterId);
        if (masterData == null) return;

        // 合計ステータスを計算
        var totalStats = equipment.CalculateTotalStats(masterData);

        // タイトル設定
        if (itemInfoTitleText != null)
        {
            itemInfoTitleText.text = masterData.equipmentName;
        }

        // 詳細情報設定
        if (itemInfoDetailsText != null)
        {
            var details = $"強化値: +{equipment.currentEnhancedValue}\n";
            details += $"強化耐久値: {equipment.currentEnhanceStamina}/100\n";
            details += $"HP: {totalStats.hp}\n";
            details += $"攻撃: {totalStats.offense}\n";
            details += $"防御: {totalStats.defense}\n";
            details += $"速度: {totalStats.speed}\n";
            details += $"クリティカル率: {totalStats.criticalRate}%\n";
            details += $"クリティカルダメージ: {totalStats.criticalDamageRate}%\n";
            details += $"火属性攻撃: {totalStats.fireOffence}\n";
            details += $"水属性攻撃: {totalStats.waterOffence}\n";
            details += $"風属性攻撃: {totalStats.windOffence}\n";
            details += $"土属性攻撃: {totalStats.earthOffence}";

            itemInfoDetailsText.text = details;
        }

        itemInfoPanel.SetActive(true);
    }

    /// <summary>
    /// アイテム詳細を表示（強化アイテム・補助材料用）
    /// </summary>
    /// <param name="itemId">アイテムID</param>
    private void ShowItemInfo(int itemId)
    {
        if (itemInfoPanel == null) return;

        switch (currentItemType)
        {
            case ItemType.EnhanceItem:
                ShowEnhanceItemInfo(itemId);
                break;
            case ItemType.SupportItem:
                ShowSupportItemInfo(itemId);
                break;
        }
    }

    /// <summary>
    /// 強化アイテム詳細を表示
    /// </summary>
    /// <param name="enhanceItemId">強化アイテムID</param>
    private void ShowEnhanceItemInfo(int enhanceItemId)
    {
        var enhanceItem = MasterDataManager.Instance.GetEnhanceItemData(enhanceItemId);
        if (enhanceItem == null) return;

        // 所持数を取得
        int quantity = GetUserItemQuantity(ItemType.EnhanceItem, enhanceItemId);

        // タイトル設定
        if (itemInfoTitleText != null)
        {
            itemInfoTitleText.text = enhanceItem.enhanceItemName;
        }

        // 詳細情報設定
        if (itemInfoDetailsText != null)
        {
            var details = $"所持数: {quantity}個\n\n";
            details += enhanceItem.description;

            itemInfoDetailsText.text = details;
        }

        itemInfoPanel.SetActive(true);
    }

    /// <summary>
    /// 補助材料詳細を表示
    /// </summary>
    /// <param name="supportItemId">補助材料ID</param>
    private void ShowSupportItemInfo(int supportItemId)
    {
        var supportItem = MasterDataManager.Instance.GetSupportItemData(supportItemId);
        if (supportItem == null) return;

        // 所持数を取得
        int quantity = GetUserItemQuantity(ItemType.SupportItem, supportItemId);

        // タイトル設定
        if (itemInfoTitleText != null)
        {
            itemInfoTitleText.text = supportItem.supportItemName;
        }

        // 詳細情報設定
        if (itemInfoDetailsText != null)
        {
            var details = $"所持数: {quantity}個\n\n";
            details += supportItem.description;

            itemInfoDetailsText.text = details;
        }

        itemInfoPanel.SetActive(true);
    }

    #endregion

    #region Private Methods - UI Update

    /// <summary>
    /// スロットの選択状態を更新（修正版：「選択なし」スロットにも対応）
    /// </summary>
    private void UpdateSlotSelection()
    {
        foreach (var slotObj in currentSlotObjects)
        {
            // 通常のアイテムスロット
            var slotComponent = slotObj.GetComponent<ItemSelectionSlotUI>();
            if (slotComponent != null)
            {
                if (currentItemType == ItemType.Equipment)
                {
                    // 装備の場合は文字列IDで比較
                    slotComponent.UpdateSelectionStateForEquipment(selectedEquipmentUserId);
                }
                else
                {
                    // その他のアイテムは数値IDで比較
                    slotComponent.UpdateSelectionState(selectedItemId);
                }
                continue;
            }

            // 「選択なし」スロット
            var noneSlotComponent = slotObj.GetComponent<NoneSelectionSlotUI>();
            if (noneSlotComponent != null)
            {
                noneSlotComponent.UpdateSelectionState(selectedItemId == 0);
            }
        }
    }

    /// <summary>
    /// 決定ボタンの状態を更新
    /// </summary>
    private void UpdateConfirmButton()
    {
        if (confirmButton != null)
        {
            bool canConfirm = CanConfirmSelection();
            confirmButton.interactable = canConfirm;

            if (confirmButtonText != null)
            {
                confirmButtonText.text = canConfirm ? "決定" : "選択";
            }
        }
    }

    /// <summary>
    /// 選択確定可能かどうかをチェック
    /// </summary>
    /// <returns>確定可能な場合true</returns>
    private bool CanConfirmSelection()
    {
        switch (currentItemType)
        {
            case ItemType.Equipment:
                return selectedItemId > 0; // 装備は必須選択
            case ItemType.EnhanceItem:
                return selectedItemId > 0; // 強化アイテムは必須選択
            case ItemType.SupportItem:
                return true; // 補助材料は任意（0でも可）
            default:
                return false;
        }
    }

    /// <summary>
    /// ユーザーIDから装備データを取得
    /// </summary>
    /// <param name="userId">装備のユーザーID</param>
    /// <returns>装備データ</returns>
    private UserEquipmentData GetEquipmentByUserId(string userId)
    {
        return currentEquipments?.FirstOrDefault(e => e.userEquipmentId == userId);
    }

    /// <summary>
    /// ユーザーのアイテム所持数を取得
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    /// <param name="itemId">アイテムID</param>
    /// <returns>所持数</returns>
    private int GetUserItemQuantity(ItemType itemType, int itemId)
    {
        var saveData = SaveDataManager.Instance.CurrentSaveData;
        if (saveData?.items == null) return 0;

        // グローバルなItemTypeを使用して比較
        global::ItemType targetItemType = itemType == ItemType.EnhanceItem ?
            global::ItemType.EnhanceItem : global::ItemType.SupportItem;

        var userItem = saveData.items.FirstOrDefault(item =>
            item.itemMasterId == itemId && item.itemType == targetItemType);

        return userItem?.quantity ?? 0;
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
            Debug.Log($"[ItemSelectionWindowUI] {message}");
        }
    }

    /// <summary>
    /// 警告ログ出力
    /// </summary>
    /// <param name="message">メッセージ</param>
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[ItemSelectionWindowUI] {message}");
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    /// <param name="message">メッセージ</param>
    private void LogError(string message)
    {
        Debug.LogError($"[ItemSelectionWindowUI] {message}");
    }

    #endregion

    #region Inspector Context Menu

    /// <summary>
    /// ウィンドウを強制的に非表示（Inspector用）
    /// </summary>
    [ContextMenu("Force Hide Window")]
    public void ForceHideWindow()
    {
        HideWindow();
    }

    /// <summary>
    /// 修正: 装備ソート機能をテスト（Inspector用）
    /// </summary>
    [ContextMenu("Test Equipment Sort")]
    public void TestEquipmentSort()
    {
        if (currentEquipments != null && currentEquipments.Count > 0)
        {
            var sorted = SortEquipmentsByPriority(currentEquipments);
            LogDebug($"装備ソートテスト: {sorted.Count}個の装備をお気に入り・ロック優先でソート");

            for (int i = 0; i < Mathf.Min(5, sorted.Count); i++)
            {
                var eq = sorted[i];
                LogDebug($"  {i + 1}. {eq.userEquipmentId} (お気に入り:{eq.isFavorite}, ロック:{eq.isLocked})");
            }
        }
        else
        {
            LogDebug("テスト対象の装備データがありません");
        }
    }

    #endregion
}