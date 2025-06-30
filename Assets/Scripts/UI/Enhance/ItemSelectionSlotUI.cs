using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// アイテム選択スロットUI（個別スロット用）
/// ItemSelectionWindowUI内で使用される個別のアイテムスロット
/// </summary>
public class ItemSelectionSlotUI : MonoBehaviour
{
    #region UI References

    [Header("UI要素")]
    [SerializeField] private Button slotButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private GameObject selectionFrame;

    [Header("選択状態の色")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    #endregion

    #region Private Fields

    private int itemId;
    private string equipmentStringId; // 装備のユーザーIDは文字列で保持
    private System.Action onClickCallback;
    private bool isCurrentlySelected;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        SetupButton();
    }

    private void OnDestroy()
    {
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// ボタンの設定
    /// </summary>
    private void SetupButton()
    {
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(() => onClickCallback?.Invoke());
        }
    }

    #endregion

    #region Public Methods - Setup

    /// <summary>
    /// 装備として設定
    /// </summary>
    /// <param name="equipment">ユーザー装備データ</param>
    /// <param name="masterData">装備マスターデータ</param>
    /// <param name="isSelected">選択状態</param>
    /// <param name="onClickCallback">クリック時のコールバック</param>
    public void SetupAsEquipment(UserEquipmentData equipment, EquipmentMasterData masterData, bool isSelected, System.Action onClickCallback)
    {
        if (equipment == null || masterData == null)
        {
            Debug.LogWarning("[ItemSelectionSlotUI] 装備データがnullです");
            return;
        }

        // userEquipmentIdは文字列なので、そのまま保存（装備の場合は特別処理）
        this.equipmentStringId = equipment.userEquipmentId;
        this.itemId = equipment.equipmentMasterId; // マスターIDを使用
        this.onClickCallback = onClickCallback;
        this.isCurrentlySelected = isSelected;

        // UI要素を設定
        SetUIText(nameText, masterData.equipmentName);
        SetUIText(descText, $"強化値: +{equipment.currentEnhancedValue}");
        SetIcon(masterData.equipmentIcon);
        SetSelectionState(isSelected);

        Debug.Log($"[ItemSelectionSlotUI] 装備設定完了: {masterData.equipmentName} (ID: {equipment.userEquipmentId})");
    }

    /// <summary>
    /// 強化アイテムとして設定
    /// </summary>
    /// <param name="enhanceItem">強化アイテムマスターデータ</param>
    /// <param name="isSelected">選択状態</param>
    /// <param name="onClickCallback">クリック時のコールバック</param>
    public void SetupAsEnhanceItem(EnhanceItemMasterData enhanceItem, bool isSelected, System.Action onClickCallback)
    {
        if (enhanceItem == null)
        {
            Debug.LogWarning("[ItemSelectionSlotUI] 強化アイテムデータがnullです");
            return;
        }

        this.itemId = enhanceItem.enhanceItemId;
        this.onClickCallback = onClickCallback;
        this.isCurrentlySelected = isSelected;

        // UI要素を設定
        SetUIText(nameText, enhanceItem.enhanceItemName);
        SetUIText(descText, $"成功率: {enhanceItem.enhanceSuccessRate}%");
        SetIcon(enhanceItem.enhanceItemIcon);
        SetSelectionState(isSelected);

        Debug.Log($"[ItemSelectionSlotUI] 強化アイテム設定完了: {enhanceItem.enhanceItemName}");
    }

    /// <summary>
    /// 補助材料として設定
    /// </summary>
    /// <param name="supportItem">補助材料マスターデータ</param>
    /// <param name="isSelected">選択状態</param>
    /// <param name="onClickCallback">クリック時のコールバック</param>
    public void SetupAsSupportItem(SupportItemMasterData supportItem, bool isSelected, System.Action onClickCallback)
    {
        if (supportItem == null)
        {
            Debug.LogWarning("[ItemSelectionSlotUI] 補助材料データがnullです");
            return;
        }

        this.itemId = supportItem.supportItemId;
        this.onClickCallback = onClickCallback;
        this.isCurrentlySelected = isSelected;

        // UI要素を設定
        SetUIText(nameText, supportItem.supportItemName);
        SetUIText(descText, $"ボーナス: +{supportItem.addEnhanceSuccessRate}%");
        SetIcon(supportItem.supportItemIcon);
        SetSelectionState(isSelected);

        Debug.Log($"[ItemSelectionSlotUI] 補助材料設定完了: {supportItem.supportItemName}");
    }

    #endregion

    #region Public Methods - Update

    /// <summary>
    /// 選択状態を更新
    /// </summary>
    /// <param name="selectedId">現在選択されているアイテムID</param>
    public void UpdateSelectionState(int selectedId)
    {
        bool isSelected = (itemId == selectedId);
        SetSelectionState(isSelected);
    }

    /// <summary>
    /// 選択状態を更新（装備用：文字列ID対応）
    /// </summary>
    /// <param name="selectedEquipmentId">現在選択されている装備のユーザーID</param>
    public void UpdateSelectionStateForEquipment(string selectedEquipmentId)
    {
        bool isSelected = (equipmentStringId == selectedEquipmentId);
        SetSelectionState(isSelected);
    }

    #endregion

    #region Private Methods - UI Update

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

    /// <summary>
    /// アイコンを設定
    /// </summary>
    /// <param name="iconSprite">アイコンのSprite</param>
    private void SetIcon(Sprite iconSprite)
    {
        if (iconImage != null)
        {
            if (iconSprite != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 選択状態を設定
    /// </summary>
    /// <param name="isSelected">選択状態</param>
    private void SetSelectionState(bool isSelected)
    {
        isCurrentlySelected = isSelected;

        // 選択フレームの表示/非表示
        if (selectionFrame != null)
        {
            selectionFrame.SetActive(isSelected);
        }

        // 背景色の変更
        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }

        // ボタンの状態変更
        if (slotButton != null)
        {
            var colorBlock = slotButton.colors;
            colorBlock.normalColor = isSelected ? selectedColor : normalColor;
            slotButton.colors = colorBlock;
        }
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// このスロットのアイテムID
    /// </summary>
    public int ItemId => itemId;

    /// <summary>
    /// 現在の選択状態
    /// </summary>
    public bool IsSelected => isCurrentlySelected;

    #endregion

    #region Inspector Context Menu

    /// <summary>
    /// 選択状態をテスト（Inspector用）
    /// </summary>
    [ContextMenu("Test Selection State")]
    public void TestSelectionState()
    {
        SetSelectionState(!isCurrentlySelected);
        Debug.Log($"[ItemSelectionSlotUI] 選択状態テスト: {isCurrentlySelected}");
    }

    #endregion
}