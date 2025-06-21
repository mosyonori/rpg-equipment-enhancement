using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class ItemSelectionUI : MonoBehaviour
{
    [Header("Selection Panel")]
    public GameObject selectionPanel;
    public Transform contentParent; // ContentPanel を指定
    public Transform dynamicContentParent; // 動的コンテンツ用（必要に応じて）
    public Button closeButton;

    [Header("Item Button Prefab")]
    public GameObject itemButtonPrefab;

    [Header("None Selection Button")]
    public Button noneSelectionButton; // 「選択なし」ボタン

    [Header("Selection Type")]
    public SelectionType currentSelectionType;

    // イベント定義（EquipmentUpgradeManager.cs で使用）
    public System.Action<string, string> OnItemSelected;
    public System.Action OnSelectionCancelled;

    private List<GameObject> instantiatedButtons = new List<GameObject>();
    [System.Obsolete("", true)] // または
#pragma warning disable CS0414
    private int currentEquipmentIndex = -1;
#pragma warning restore CS0414

    public enum SelectionType
    {
        Equipment,
        EnhancementItem,
        SupportMaterial
    }

    void Start()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        closeButton?.onClick.AddListener(CloseSelectionPanel);

        // 「選択なし」ボタンのイベント設定
        if (noneSelectionButton != null)
        {
            noneSelectionButton.onClick.AddListener(() => {
                OnItemSelected?.Invoke("", "support_none");
                CloseSelectionPanel();
            });
            noneSelectionButton.gameObject.SetActive(false); // 初期状態では非表示
        }

        selectionPanel?.SetActive(false);
    }

    #region 選択UI表示メソッド

    public void ShowEquipmentSelection()
    {
        currentSelectionType = SelectionType.Equipment;
        ShowSelectionPanel();
        SetActive(noneSelectionButton?.gameObject, false);
        PopulateEquipmentList();
    }

    public void ShowEnhancementItemSelection()
    {
        currentSelectionType = SelectionType.EnhancementItem;
        ShowSelectionPanel();
        SetActive(noneSelectionButton?.gameObject, false);
        PopulateEnhancementItemList();
    }

    public void ShowSupportMaterialSelection()
    {
        currentSelectionType = SelectionType.SupportMaterial;
        ShowSelectionPanel();
        SetActive(noneSelectionButton?.gameObject, true);
        PopulateSupportMaterialList();
    }

    private void ShowSelectionPanel()
    {
        selectionPanel?.SetActive(true);
        ClearItemList();
    }

    public void CloseSelectionPanel()
    {
        selectionPanel?.SetActive(false);
        ClearItemList();
        OnSelectionCancelled?.Invoke();
    }

    private void ClearItemList()
    {
        foreach (GameObject button in instantiatedButtons)
        {
            if (button != null)
                Destroy(button);
        }
        instantiatedButtons.Clear();
    }

    #endregion

    #region アイテムリスト生成

    private void PopulateEquipmentList()
    {
        if (DataManager.Instance?.currentUserData == null) return;

        var equipmentList = DataManager.Instance.GetAllUserEquipments();
        for (int i = 0; i < equipmentList.Count; i++)
        {
            var userEquipment = equipmentList[i];
            var masterData = DataManager.Instance.GetEquipmentData(userEquipment.equipmentId);
            if (masterData != null)
            {
                CreateEquipmentButton(masterData, userEquipment, i);
            }
        }
    }

    private void PopulateEnhancementItemList()
    {
        if (DataManager.Instance == null) return;

        // 現在選択中の装備を取得（属性制限チェック用）
        UserEquipment currentEquipment = GetCurrentSelectedEquipment();

        // 強化アイテムマスターデータを全て取得して、所持数と属性制限をチェック
        foreach (var itemData in DataManager.Instance.enhancementItemDatabase)
        {
            int quantity = DataManager.Instance.GetItemQuantity(itemData.itemId);
            if (quantity > 0)
            {
                // 属性制限チェック（ボタンの有効/無効のみ）
                bool canUse = currentEquipment == null || itemData.CanUseOnEquipment(currentEquipment);

                CreateEnhancementItemButton(itemData, quantity, canUse);
            }
        }
    }

    private void PopulateSupportMaterialList()
    {
        if (DataManager.Instance == null) return;

        // 補助アイテムマスターデータを全て取得して、所持数をチェック
        foreach (var itemData in DataManager.Instance.supportMaterialDatabase)
        {
            int quantity = DataManager.Instance.GetItemQuantity(itemData.materialId);
            if (quantity > 0)
            {
                CreateSupportMaterialButton(itemData, quantity);
            }
        }
    }

    /// <summary>
    /// 現在選択中の装備を取得（EquipmentUpgradeManagerから）
    /// </summary>
    private UserEquipment GetCurrentSelectedEquipment()
    {
        var upgradeManager = FindFirstObjectByType<EquipmentUpgradeManager>();
        if (upgradeManager == null) return null;

        // リフレクションを使用してprivateフィールドにアクセス
        var field = typeof(EquipmentUpgradeManager).GetField("currentEquipmentIndex",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            int equipmentIndex = (int)field.GetValue(upgradeManager);
            if (equipmentIndex >= 0)
            {
                return DataManager.Instance?.GetUserEquipment(equipmentIndex);
            }
        }

        return null;
    }

    #endregion

    #region ボタン生成メソッド

    private void CreateEquipmentButton(EquipmentData equipData, UserEquipment userEquip, int equipmentIndex)
    {
        if (itemButtonPrefab == null || contentParent == null) return;

        GameObject buttonObj = Instantiate(itemButtonPrefab, contentParent);
        Button button = buttonObj.GetComponent<Button>();

        if (button != null)
        {
            // 局所変数を使用してキャプチャを明確化
            int capturedIndex = equipmentIndex;
            button.onClick.AddListener(() => {
                OnItemSelected?.Invoke(capturedIndex.ToString(), "equipment");
                CloseSelectionPanel();
            });
        }

        // UI要素の設定
        SetImageComponent(buttonObj, "Icon", equipData.icon);
        SetTextComponent(buttonObj, "NameText", $"{equipData.equipmentName} +{userEquip.enhancementLevel}");
        SetTextComponent(buttonObj, "DetailText", $"攻撃力: {userEquip.GetTotalAttack():F1}\n耐久: {userEquip.currentDurability}");

        // 装備の属性表示
        ElementalType equipmentType = userEquip.GetCurrentElementalType();
        if (equipmentType != ElementalType.None)
        {
            string typeName = UserEquipment.GetElementalTypeName(equipmentType);
            AppendTextComponent(buttonObj, "DetailText", $"\n[{typeName}属性]");
        }

        instantiatedButtons.Add(buttonObj);
    }

    private void CreateEnhancementItemButton(EnhancementItemData itemData, int quantity, bool canUse)
    {
        if (itemButtonPrefab == null || contentParent == null) return;

        GameObject buttonObj = Instantiate(itemButtonPrefab, contentParent);
        Button button = buttonObj.GetComponent<Button>();

        if (button != null)
        {
            button.interactable = canUse;

            if (canUse)
            {
                // 局所変数を使用してキャプチャを明確化
                int capturedItemId = itemData.itemId;
                button.onClick.AddListener(() => {
                    OnItemSelected?.Invoke(capturedItemId.ToString(), "enhancement");
                    CloseSelectionPanel();
                });
            }
        }

        // UI要素の設定
        SetImageComponent(buttonObj, "Icon", itemData.icon);
        SetTextComponent(buttonObj, "NameText", $"{itemData.itemName} x{quantity}");

        string detailText = GetEnhancementItemDetailText(itemData);
        SetTextComponent(buttonObj, "DetailText", detailText);

        instantiatedButtons.Add(buttonObj);
    }

    private void CreateSupportMaterialButton(SupportMaterialData materialData, int quantity)
    {
        if (itemButtonPrefab == null || contentParent == null) return;

        GameObject buttonObj = Instantiate(itemButtonPrefab, contentParent);
        Button button = buttonObj.GetComponent<Button>();

        if (button != null)
        {
            // 局所変数を使用してキャプチャを明確化
            int capturedMaterialId = materialData.materialId;
            button.onClick.AddListener(() => {
                OnItemSelected?.Invoke(capturedMaterialId.ToString(), "support");
                CloseSelectionPanel();
            });
        }

        // UI要素の設定
        SetImageComponent(buttonObj, "Icon", materialData.icon);
        SetTextComponent(buttonObj, "NameText", $"{materialData.materialName} x{quantity}");
        SetTextComponent(buttonObj, "DetailText", materialData.GetEffectDescription());

        instantiatedButtons.Add(buttonObj);
    }

    #endregion

    #region UI操作ヘルパーメソッド

    /// <summary>
    /// 強化アイテムの詳細テキストを装備選択状態に応じて生成
    /// </summary>
    private string GetEnhancementItemDetailText(EnhancementItemData itemData)
    {
        // 現在選択中の装備を取得
        UserEquipment currentEquipment = GetCurrentSelectedEquipment();

        if (currentEquipment == null)
        {
            // 装備が選択されていない場合は基本説明を表示
            string basicInfo = $"{itemData.description}\n";
            basicInfo += $"成功率: {itemData.successRate * 100:F0}%";
            return basicInfo;
        }

        // 装備が選択されている場合は、その装備種類に応じた効果を表示
        var equipmentData = DataManager.Instance?.GetEquipmentData(currentEquipment.equipmentId);
        if (equipmentData == null)
        {
            return itemData.description;
        }

        string effectText;
        string successRateText;
        string durabilityText;

        if (itemData.useEquipmentTypeSpecificBonus)
        {
            // 装備種類別効果を使用
            effectText = itemData.GetEffectDescriptionForEquipmentType(equipmentData.equipmentType);
            float adjustedRate = itemData.GetAdjustedSuccessRate(currentEquipment.enhancementLevel);
            successRateText = $"成功率: {adjustedRate * 100:F1}%";
            durabilityText = $"消費耐久: {itemData.GetDurabilityReduction(equipmentData.equipmentType)}";
        }
        else
        {
            // 従来の効果を使用
            effectText = itemData.GetEffectDescription();
            float adjustedRate = itemData.GetAdjustedSuccessRate(currentEquipment.enhancementLevel);
            successRateText = $"成功率: {adjustedRate * 100:F1}%";
            durabilityText = $"消費耐久: {itemData.GetDurabilityReduction()}";
        }

        // 属性情報を追加
        ElementalType itemType = itemData.GetElementalType(equipmentData.equipmentType);
        if (itemType != ElementalType.None)
        {
            string typeName = UserEquipment.GetElementalTypeName(itemType);
            effectText += $"\n[{typeName}属性強化]";
        }

        return $"{effectText}\n{successRateText}\n{durabilityText}";
    }

    private void SetTextComponent(GameObject parent, string childName, string text)
    {
        Transform textTransform = parent.transform.Find(childName);
        if (textTransform != null)
        {
            TextMeshProUGUI textComponent = textTransform.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }
    }

    private void AppendTextComponent(GameObject parent, string childName, string appendText)
    {
        Transform textTransform = parent.transform.Find(childName);
        if (textTransform != null)
        {
            TextMeshProUGUI textComponent = textTransform.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text += appendText;
            }
        }
    }

    private void SetImageComponent(GameObject parent, string childName, Sprite sprite)
    {
        Transform imageTransform = parent.transform.Find(childName);
        if (imageTransform != null)
        {
            Image imageComponent = imageTransform.GetComponent<Image>();
            if (imageComponent != null && sprite != null)
            {
                imageComponent.sprite = sprite;
                imageComponent.enabled = true;
            }
            else if (imageComponent != null)
            {
                imageComponent.enabled = false;
            }
        }
    }

    private void SetActive(GameObject obj, bool active) => obj?.SetActive(active);

    #endregion

    #region 既存互換性保持用メソッド

    public void SelectEquipment(EquipmentData equipData, UserEquipment userEquip)
    {
        var equipmentList = DataManager.Instance.GetAllUserEquipments();
        for (int i = 0; i < equipmentList.Count; i++)
        {
            if (equipmentList[i] == userEquip)
            {
                OnItemSelected?.Invoke(i.ToString(), "equipment");
                CloseSelectionPanel();
                return;
            }
        }
    }

    public void SelectUpgradeItem(EnhancementItemData itemData)
    {
        OnItemSelected?.Invoke(itemData.itemId.ToString(), "enhancement");
        CloseSelectionPanel();
    }

    public void SelectSupportItem(SupportMaterialData materialData)
    {
        OnItemSelected?.Invoke(materialData.materialId.ToString(), "support");
        CloseSelectionPanel();
    }

    public void DeselectSupportItem()
    {
        OnItemSelected?.Invoke("", "support_none");
        CloseSelectionPanel();
    }

    #endregion
}