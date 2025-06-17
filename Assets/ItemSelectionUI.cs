using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class ItemSelectionUI : MonoBehaviour
{
    [Header("Selection Panel")]
    public GameObject selectionPanel;
    public Transform contentParent; // ContentPanel���w��
    public Transform dynamicContentParent; // ���V�K�ǉ�: DynamicContentPanel���w��
    public Button closeButton;

    [Header("Item Button Prefab")]
    public GameObject itemButtonPrefab;

    [Header("None Selection Button")]
    public Button noneSelectionButton; // ���ǉ�: ���炩���ߍ쐬�����u�I���Ȃ��v�{�^��

    [Header("Selection Type")]
    public SelectionType currentSelectionType;

    // ���d�v: �C�x���g��`�iEquipmentUpgradeManager.cs�Ŏg�p�j
    public System.Action<string, string> OnItemSelected;
    public System.Action OnSelectionCancelled;

    private List<GameObject> instantiatedButtons = new List<GameObject>();

    public enum SelectionType
    {
        Equipment,
        EnhancementItem,
        SupportMaterial
    }

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseSelectionPanel);

        // ���u�I���Ȃ��v�{�^���̃C�x���g�ݒ�
        if (noneSelectionButton != null)
        {
            noneSelectionButton.onClick.AddListener(() => {
                Debug.Log("�u�I���Ȃ��v�{�^�����N���b�N����܂���");
                OnItemSelected?.Invoke("", "support_none");
                CloseSelectionPanel();
            });

            // ������Ԃł͔�\��
            noneSelectionButton.gameObject.SetActive(false);
            Debug.Log("�u�I���Ȃ��v�{�^���̃C�x���g�ݒ芮��");
        }
        else
        {
            Debug.LogWarning("NoneSelectionButton ���ݒ肳��Ă��܂���");
        }

        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }

        Debug.Log("ItemSelectionUI����������");
    }

    public void ShowEquipmentSelection()
    {
        Debug.Log("�����I����ʂ�\��");
        currentSelectionType = SelectionType.Equipment;
        ShowSelectionPanel();

        // ���u�I���Ȃ��v�{�^�����\��
        if (noneSelectionButton != null)
        {
            noneSelectionButton.gameObject.SetActive(false);
        }

        PopulateEquipmentList();
    }

    public void ShowEnhancementItemSelection()
    {
        Debug.Log("�����A�C�e���I����ʂ�\��");
        currentSelectionType = SelectionType.EnhancementItem;
        ShowSelectionPanel();

        // ���u�I���Ȃ��v�{�^�����\��
        if (noneSelectionButton != null)
        {
            noneSelectionButton.gameObject.SetActive(false);
        }

        PopulateEnhancementItemList();
    }

    public void ShowSupportMaterialSelection()
    {
        Debug.Log("�⏕�ޗ��I����ʂ�\��");
        currentSelectionType = SelectionType.SupportMaterial;
        ShowSelectionPanel();

        // ���u�I���Ȃ��v�{�^����\�� - �ڍ׃f�o�b�O
        if (noneSelectionButton != null)
        {
            Debug.Log($"NoneSelectionButton found: {noneSelectionButton.name}");
            Debug.Log($"NoneSelectionButton current active state: {noneSelectionButton.gameObject.activeInHierarchy}");

            noneSelectionButton.gameObject.SetActive(true);

            Debug.Log($"NoneSelectionButton after SetActive(true): {noneSelectionButton.gameObject.activeInHierarchy}");
            Debug.Log($"NoneSelectionButton parent: {noneSelectionButton.transform.parent?.name}");
            Debug.Log($"NoneSelectionButton position: {noneSelectionButton.transform.position}");
            Debug.Log($"NoneSelectionButton rect: {noneSelectionButton.GetComponent<RectTransform>().rect}");

            // �e�I�u�W�F�N�g�̃A�N�e�B�u��Ԃ��m�F
            Transform current = noneSelectionButton.transform;
            while (current != null)
            {
                Debug.Log($"Parent check: {current.name} - Active: {current.gameObject.activeSelf}");
                current = current.parent;
            }

            Debug.Log("�u�I���Ȃ��v�{�^����\�����܂���");
        }
        else
        {
            Debug.LogError("NoneSelectionButton �� null �ł��IInspector�Őݒ���m�F���Ă�������");
        }

        PopulateSupportMaterialList();
    }

    private void ShowSelectionPanel()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(true);
        }
        ClearItemList();
    }

    public void CloseSelectionPanel()
    {
        Debug.Log("�I����ʂ���܂�");

        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }

        ClearItemList();
        OnSelectionCancelled?.Invoke(); // ���C�x���g�Ăяo��
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

    private void PopulateEquipmentList()
    {
        if (DataManager.Instance == null || DataManager.Instance.currentUserData == null)
        {
            Debug.LogError("DataManager�܂��̓��[�U�[�f�[�^��������܂���");
            return;
        }

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
        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager ��������܂���");
            return;
        }

        // �����A�C�e���}�X�^�[�f�[�^��S�Ď擾���āA���������`�F�b�N
        foreach (var itemData in DataManager.Instance.enhancementItemDatabase)
        {
            int quantity = DataManager.Instance.GetItemQuantity(itemData.itemId);
            if (quantity > 0)
            {
                CreateEnhancementItemButton(itemData, quantity);
            }
        }
    }

    private void PopulateSupportMaterialList()
    {
        Debug.Log("�⏕�ޗ����X�g�쐬�J�n");

        // �����I�ȁu�I���Ȃ��v�{�^���쐬�͕s�v�i���炩����UI�ɔz�u�ς݁j

        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager ��������܂���");
            return;
        }

        // �⏕�A�C�e���}�X�^�[�f�[�^��S�Ď擾���āA���������`�F�b�N
        int materialCount = 0;
        foreach (var itemData in DataManager.Instance.supportMaterialDatabase)
        {
            int quantity = DataManager.Instance.GetItemQuantity(itemData.materialId);
            Debug.Log($"�⏕�ޗ��`�F�b�N: {itemData.materialName} (ID:{itemData.materialId}) ������:{quantity}");

            if (quantity > 0)
            {
                CreateSupportMaterialButton(itemData, quantity);
                materialCount++;
            }
        }

        Debug.Log($"�⏕�ޗ����X�g�쐬����: ���炩���ߔz�u�ς݁u�I���Ȃ��v+ {materialCount}�̍ޗ�");
    }

    private void CreateNoneSupportMaterialButton()
    {
        Debug.Log("�u�I���Ȃ��v�{�^���쐬�J�n");

        if (itemButtonPrefab == null || contentParent == null)
        {
            Debug.LogError("ItemButtonPrefab�܂���ContentParent���ݒ肳��Ă��܂���");
            return;
        }

        GameObject buttonObj = Instantiate(itemButtonPrefab, contentParent);

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => {
                Debug.Log("�u�I���Ȃ��v�{�^�����N���b�N����܂���");
                OnItemSelected?.Invoke("", "support_none"); // ���C�x���g�Ăяo��
                CloseSelectionPanel();
            });
            Debug.Log("�u�I���Ȃ��v�{�^���̃N���b�N�C�x���g�ݒ芮��");
        }
        else
        {
            Debug.LogError("�u�I���Ȃ��v�{�^����Button�R���|�[�l���g��������܂���");
        }

        // �w�i�F��ύX���ċ�ʂ��₷������
        Image buttonImage = buttonObj.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = new Color(0.8f, 0.8f, 1.0f, 1f); // �����F�ŋ��
            Debug.Log("�u�I���Ȃ��v�{�^���̔w�i�F�ݒ芮��");
        }

        // UI�ݒ�
        SetTextComponent(buttonObj, "NameText", "�I���Ȃ�");
        SetTextComponent(buttonObj, "DetailText", "�⏕�ޗ����g�p���܂���");

        // �A�C�R�����\��
        Transform iconTransform = buttonObj.transform.Find("Icon");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.enabled = false;
                Debug.Log("�u�I���Ȃ��v�{�^���̃A�C�R�����\���ɐݒ�");
            }
        }

        instantiatedButtons.Add(buttonObj);
        Debug.Log($"�u�I���Ȃ��v�{�^���쐬�����B���݂̃{�^����: {instantiatedButtons.Count}");
    }

    private void CreateEquipmentButton(EquipmentData equipData, UserEquipment userEquip, int equipmentIndex)
    {
        if (itemButtonPrefab == null || contentParent == null)
        {
            Debug.LogError("ItemButtonPrefab�܂���ContentParent���ݒ肳��Ă��܂���");
            return;
        }

        GameObject buttonObj = Instantiate(itemButtonPrefab, contentParent);

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => {
                Debug.Log($"�������I������܂���: {equipData.equipmentName}");
                OnItemSelected?.Invoke(equipmentIndex.ToString(), "equipment"); // ���C�x���g�Ăяo��
                CloseSelectionPanel();
            });
        }

        // UI�v�f�̐ݒ�
        SetImageComponent(buttonObj, "Icon", equipData.icon);
        SetTextComponent(buttonObj, "NameText", $"{equipData.equipmentName} +{userEquip.enhancementLevel}");
        SetTextComponent(buttonObj, "DetailText", $"�U����: {userEquip.GetTotalAttack():F1}\n�ϋv: {userEquip.currentDurability}");

        instantiatedButtons.Add(buttonObj);
    }

    private void CreateEnhancementItemButton(EnhancementItemData itemData, int quantity)
    {
        if (itemButtonPrefab == null || contentParent == null)
        {
            Debug.LogError("ItemButtonPrefab�܂���ContentParent���ݒ肳��Ă��܂���");
            return;
        }

        GameObject buttonObj = Instantiate(itemButtonPrefab, contentParent);

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => {
                Debug.Log($"�����A�C�e�����I������܂���: {itemData.itemName}");
                OnItemSelected?.Invoke(itemData.itemId.ToString(), "enhancement"); // ���C�x���g�Ăяo��
                CloseSelectionPanel();
            });
        }

        // UI�v�f�̐ݒ�
        SetImageComponent(buttonObj, "Icon", itemData.icon);
        SetTextComponent(buttonObj, "NameText", $"{itemData.itemName} x{quantity}");
        SetTextComponent(buttonObj, "DetailText", $"������: {itemData.successRate * 100:F0}%\n{itemData.GetEffectDescription()}");

        instantiatedButtons.Add(buttonObj);
    }

    private void CreateSupportMaterialButton(SupportMaterialData materialData, int quantity)
    {
        if (itemButtonPrefab == null || contentParent == null)
        {
            Debug.LogError("ItemButtonPrefab�܂���ContentParent���ݒ肳��Ă��܂���");
            return;
        }

        GameObject buttonObj = Instantiate(itemButtonPrefab, contentParent);

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => {
                Debug.Log($"�⏕�ޗ����I������܂���: {materialData.materialName}");
                OnItemSelected?.Invoke(materialData.materialId.ToString(), "support"); // ���C�x���g�Ăяo��
                CloseSelectionPanel();
            });
        }

        // UI�v�f�̐ݒ�
        SetImageComponent(buttonObj, "Icon", materialData.icon);
        SetTextComponent(buttonObj, "NameText", $"{materialData.materialName} x{quantity}");
        SetTextComponent(buttonObj, "DetailText", materialData.GetEffectDescription());

        instantiatedButtons.Add(buttonObj);
    }

    // �������X�N���v�g�݊����ێ��p���\�b�h
    public void SelectEquipment(EquipmentData equipData, UserEquipment userEquip)
    {
        // �����C���f�b�N�X������
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

    // �w���p�[���\�b�h�F�e�L�X�g�R���|�[�l���g�ݒ�
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

    // �w���p�[���\�b�h�F�摜�R���|�[�l���g�ݒ�
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
        }
    }
}