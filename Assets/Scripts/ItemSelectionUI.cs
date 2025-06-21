using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class ItemSelectionUI : MonoBehaviour
{
    [Header("Selection Panel")]
    public GameObject selectionPanel;
    public Transform contentParent; // ContentPanel ���w��
    public Transform dynamicContentParent; // ���I�R���e���c�p�i�K�v�ɉ����āj
    public Button closeButton;

    [Header("Item Button Prefab")]
    public GameObject itemButtonPrefab;

    [Header("None Selection Button")]
    public Button noneSelectionButton; // �u�I���Ȃ��v�{�^��

    [Header("Selection Type")]
    public SelectionType currentSelectionType;

    // �C�x���g��`�iEquipmentUpgradeManager.cs �Ŏg�p�j
    public System.Action<string, string> OnItemSelected;
    public System.Action OnSelectionCancelled;

    private List<GameObject> instantiatedButtons = new List<GameObject>();
    [System.Obsolete("", true)] // �܂���
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

        // �u�I���Ȃ��v�{�^���̃C�x���g�ݒ�
        if (noneSelectionButton != null)
        {
            noneSelectionButton.onClick.AddListener(() => {
                OnItemSelected?.Invoke("", "support_none");
                CloseSelectionPanel();
            });
            noneSelectionButton.gameObject.SetActive(false); // ������Ԃł͔�\��
        }

        selectionPanel?.SetActive(false);
    }

    #region �I��UI�\�����\�b�h

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

    #region �A�C�e�����X�g����

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

        // ���ݑI�𒆂̑������擾�i���������`�F�b�N�p�j
        UserEquipment currentEquipment = GetCurrentSelectedEquipment();

        // �����A�C�e���}�X�^�[�f�[�^��S�Ď擾���āA�������Ƒ����������`�F�b�N
        foreach (var itemData in DataManager.Instance.enhancementItemDatabase)
        {
            int quantity = DataManager.Instance.GetItemQuantity(itemData.itemId);
            if (quantity > 0)
            {
                // ���������`�F�b�N�i�{�^���̗L��/�����̂݁j
                bool canUse = currentEquipment == null || itemData.CanUseOnEquipment(currentEquipment);

                CreateEnhancementItemButton(itemData, quantity, canUse);
            }
        }
    }

    private void PopulateSupportMaterialList()
    {
        if (DataManager.Instance == null) return;

        // �⏕�A�C�e���}�X�^�[�f�[�^��S�Ď擾���āA���������`�F�b�N
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
    /// ���ݑI�𒆂̑������擾�iEquipmentUpgradeManager����j
    /// </summary>
    private UserEquipment GetCurrentSelectedEquipment()
    {
        var upgradeManager = FindFirstObjectByType<EquipmentUpgradeManager>();
        if (upgradeManager == null) return null;

        // ���t���N�V�������g�p����private�t�B�[���h�ɃA�N�Z�X
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

    #region �{�^���������\�b�h

    private void CreateEquipmentButton(EquipmentData equipData, UserEquipment userEquip, int equipmentIndex)
    {
        if (itemButtonPrefab == null || contentParent == null) return;

        GameObject buttonObj = Instantiate(itemButtonPrefab, contentParent);
        Button button = buttonObj.GetComponent<Button>();

        if (button != null)
        {
            // �Ǐ��ϐ����g�p���ăL���v�`���𖾊m��
            int capturedIndex = equipmentIndex;
            button.onClick.AddListener(() => {
                OnItemSelected?.Invoke(capturedIndex.ToString(), "equipment");
                CloseSelectionPanel();
            });
        }

        // UI�v�f�̐ݒ�
        SetImageComponent(buttonObj, "Icon", equipData.icon);
        SetTextComponent(buttonObj, "NameText", $"{equipData.equipmentName} +{userEquip.enhancementLevel}");
        SetTextComponent(buttonObj, "DetailText", $"�U����: {userEquip.GetTotalAttack():F1}\n�ϋv: {userEquip.currentDurability}");

        // �����̑����\��
        ElementalType equipmentType = userEquip.GetCurrentElementalType();
        if (equipmentType != ElementalType.None)
        {
            string typeName = UserEquipment.GetElementalTypeName(equipmentType);
            AppendTextComponent(buttonObj, "DetailText", $"\n[{typeName}����]");
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
                // �Ǐ��ϐ����g�p���ăL���v�`���𖾊m��
                int capturedItemId = itemData.itemId;
                button.onClick.AddListener(() => {
                    OnItemSelected?.Invoke(capturedItemId.ToString(), "enhancement");
                    CloseSelectionPanel();
                });
            }
        }

        // UI�v�f�̐ݒ�
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
            // �Ǐ��ϐ����g�p���ăL���v�`���𖾊m��
            int capturedMaterialId = materialData.materialId;
            button.onClick.AddListener(() => {
                OnItemSelected?.Invoke(capturedMaterialId.ToString(), "support");
                CloseSelectionPanel();
            });
        }

        // UI�v�f�̐ݒ�
        SetImageComponent(buttonObj, "Icon", materialData.icon);
        SetTextComponent(buttonObj, "NameText", $"{materialData.materialName} x{quantity}");
        SetTextComponent(buttonObj, "DetailText", materialData.GetEffectDescription());

        instantiatedButtons.Add(buttonObj);
    }

    #endregion

    #region UI����w���p�[���\�b�h

    /// <summary>
    /// �����A�C�e���̏ڍ׃e�L�X�g�𑕔��I����Ԃɉ����Đ���
    /// </summary>
    private string GetEnhancementItemDetailText(EnhancementItemData itemData)
    {
        // ���ݑI�𒆂̑������擾
        UserEquipment currentEquipment = GetCurrentSelectedEquipment();

        if (currentEquipment == null)
        {
            // �������I������Ă��Ȃ��ꍇ�͊�{������\��
            string basicInfo = $"{itemData.description}\n";
            basicInfo += $"������: {itemData.successRate * 100:F0}%";
            return basicInfo;
        }

        // �������I������Ă���ꍇ�́A���̑�����ނɉ��������ʂ�\��
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
            // ������ޕʌ��ʂ��g�p
            effectText = itemData.GetEffectDescriptionForEquipmentType(equipmentData.equipmentType);
            float adjustedRate = itemData.GetAdjustedSuccessRate(currentEquipment.enhancementLevel);
            successRateText = $"������: {adjustedRate * 100:F1}%";
            durabilityText = $"����ϋv: {itemData.GetDurabilityReduction(equipmentData.equipmentType)}";
        }
        else
        {
            // �]���̌��ʂ��g�p
            effectText = itemData.GetEffectDescription();
            float adjustedRate = itemData.GetAdjustedSuccessRate(currentEquipment.enhancementLevel);
            successRateText = $"������: {adjustedRate * 100:F1}%";
            durabilityText = $"����ϋv: {itemData.GetDurabilityReduction()}";
        }

        // ��������ǉ�
        ElementalType itemType = itemData.GetElementalType(equipmentData.equipmentType);
        if (itemType != ElementalType.None)
        {
            string typeName = UserEquipment.GetElementalTypeName(itemType);
            effectText += $"\n[{typeName}��������]";
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

    #region �����݊����ێ��p���\�b�h

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