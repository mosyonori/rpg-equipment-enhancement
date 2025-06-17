using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EquipmentUpgradeManager : MonoBehaviour
{
    [Header("Equipment Selection")]
    public Button equipmentSlotButton; // �����I���{�^��
    public Image equipmentIcon;
    public TextMeshProUGUI equipmentNameText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI criticalRateText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI enhancementLevelText;
    public TextMeshProUGUI durabilityText;
    public Slider durabilitySlider;

    [Header("Elemental Attack Texts (Optional)")]
    public TextMeshProUGUI elementalAttackText;     // �ėp�����U��
    public TextMeshProUGUI fireAttackText;          // �Α����U��
    public TextMeshProUGUI waterAttackText;         // �������U��
    public TextMeshProUGUI windAttackText;          // �������U��
    public TextMeshProUGUI earthAttackText;         // �y�����U��

    [Header("Enhancement Item Selection")]
    public Button enhancementItemButton;
    public Image enhancementItemIcon;
    public TextMeshProUGUI enhancementItemNameText;
    public TextMeshProUGUI enhancementItemQuantityText;
    public TextMeshProUGUI enhancementItemEffectText; // �����A�C�e�����ʕ\���p

    [Header("Support Item Selection")]
    public Button supportItemButton;
    public Image supportItemIcon;
    public TextMeshProUGUI supportItemNameText;
    public TextMeshProUGUI supportItemQuantityText;
    public TextMeshProUGUI supportItemEffectText;

    [Header("Enhancement Button")]
    public Button enhanceButton;
    public TextMeshProUGUI enhanceButtonText;
    public TextMeshProUGUI successRateText;

    [Header("Item Selection UI")]
    public ItemSelectionUI itemSelectionUI;

    [Header("Effect System")]
    public Transform effectSpawnPoint;
    public AudioSource audioSource;

    // ���ݑI�𒆂̃f�[�^
    private int currentEquipmentIndex = -1; // �� �C��: �����l��-1�i���I���j�ɕύX
    private int selectedEnhancementItemId = -1;
    private int selectedSupportItemId = -1;

    private void Start()
    {
        SetupUI();

        // �� �C��: ������Ԃł͉����I������Ă��Ȃ���Ԃɂ���
        currentEquipmentIndex = -1; // -1�Ŗ��I����Ԃ�\��

        RefreshUI();
    }

    private void SetupUI()
    {
        // �{�^���C�x���g�ݒ�
        if (enhancementItemButton != null)
            enhancementItemButton.onClick.AddListener(() => ShowItemSelection("enhancement"));
        else
            Debug.LogWarning("enhancementItemButton ���ݒ肳��Ă��܂���");

        if (supportItemButton != null)
            supportItemButton.onClick.AddListener(() => ShowItemSelection("support"));
        else
            Debug.LogWarning("supportItemButton ���ݒ肳��Ă��܂���");

        if (enhanceButton != null)
            enhanceButton.onClick.AddListener(OnEnhanceButtonClicked);
        else
            Debug.LogWarning("enhanceButton ���ݒ肳��Ă��܂���");

        // �����I���{�^���̃C�x���g�ݒ�
        if (equipmentSlotButton != null)
            equipmentSlotButton.onClick.AddListener(() => ShowItemSelection("equipment"));
        else
            Debug.LogWarning("equipmentSlotButton ���ݒ肳��Ă��܂���");

        // �A�C�e���I��UI�̃C�x���g�ݒ�
        if (itemSelectionUI != null)
        {
            itemSelectionUI.OnItemSelected += OnItemSelected;
            itemSelectionUI.OnSelectionCancelled += OnSelectionCancelled;
            Debug.Log("ItemSelectionUI �C�x���g�ݒ芮��");
        }
        else
        {
            Debug.LogWarning("itemSelectionUI ���ݒ肳��Ă��܂���BInspector �Őݒ肵�Ă��������B");
        }
    }

    // �A�C�e���I��UI�\��
    private void ShowItemSelection(string itemType)
    {
        if (itemSelectionUI == null)
        {
            Debug.LogWarning("itemSelectionUI ���ݒ肳��Ă��܂���B�A�C�e���I����ʂ�\���ł��܂���B");
            return;
        }

        switch (itemType)
        {
            case "equipment":
                itemSelectionUI.ShowEquipmentSelection();
                break;
            case "enhancement":
                itemSelectionUI.ShowEnhancementItemSelection();
                break;
            case "support":
                itemSelectionUI.ShowSupportMaterialSelection();
                break;
            default:
                Debug.LogWarning($"�s���ȃA�C�e���^�C�v: {itemType}");
                break;
        }
    }

    // �����I���i�O������Ăяo�����j
    public void SelectEquipment(int equipmentIndex)
    {
        currentEquipmentIndex = equipmentIndex;
        selectedEnhancementItemId = -1;
        selectedSupportItemId = -1;
        RefreshUI();
    }

    // �����݊����m��: �����X�N���v�g�p��SelectEquipment�I�[�o�[���[�h
    public void SelectEquipment(EquipmentData equipData, UserEquipment userEquip)
    {
        // ���[�U�[�������X�g����C���f�b�N�X������
        var equipmentList = DataManager.Instance.GetAllUserEquipments();
        for (int i = 0; i < equipmentList.Count; i++)
        {
            if (equipmentList[i] == userEquip)
            {
                SelectEquipment(i);
                return;
            }
        }

        Debug.LogWarning("�w�肳�ꂽ���������[�U�[�������X�g�Ɍ�����܂���");
    }

    // �����݊����m��: �����X�N���v�g�p��SelectUpgradeItem
    public void SelectUpgradeItem(EnhancementItemData itemData)
    {
        selectedEnhancementItemId = itemData.itemId;
        RefreshUI();
        Debug.Log($"�����A�C�e����I�����܂���: {itemData.itemName}");
    }

    // �����݊����m��: �����X�N���v�g�p��SelectSupportItem
    public void SelectSupportItem(SupportMaterialData materialData)
    {
        selectedSupportItemId = materialData.materialId;
        RefreshUI();
        Debug.Log($"�⏕�ޗ���I�����܂���: {materialData.materialName}");
    }

    // �⏕�A�C�e���I������
    public void DeselectSupportItem()
    {
        selectedSupportItemId = -1;
        RefreshUI();
    }

    // UI�S�̂��X�V
    private void RefreshUI()
    {
        UpdateEquipmentDisplay();
        UpdateEnhancementItemDisplay();
        UpdateSupportItemDisplay();
        UpdateEnhanceButton();
    }

    // �������\�����X�V
    private void UpdateEquipmentDisplay()
    {
        // �� �C��: �������I������Ă��Ȃ��ꍇ�̏�����ǉ�
        if (currentEquipmentIndex < 0)
        {
            // �������I���̏ꍇ�̕\��
            if (equipmentIcon != null)
            {
                equipmentIcon.sprite = null;
                equipmentIcon.color = new Color(1, 1, 1, 0.3f); // �����O���[
            }

            if (equipmentNameText != null) equipmentNameText.text = "������I�����Ă�������";
            if (enhancementLevelText != null) enhancementLevelText.text = "";

            // �S�ẴX�e�[�^�X�e�L�X�g���\���܂��͋�ɂ���
            SetStatTextEmpty(attackText);
            SetStatTextEmpty(defenseText);
            SetStatTextEmpty(criticalRateText);
            SetStatTextEmpty(hpText);
            SetStatTextEmpty(elementalAttackText);
            SetStatTextEmpty(fireAttackText);
            SetStatTextEmpty(waterAttackText);
            SetStatTextEmpty(windAttackText);
            SetStatTextEmpty(earthAttackText);

            if (durabilityText != null) durabilityText.text = "";
            if (durabilitySlider != null)
            {
                durabilitySlider.value = 0;
                durabilitySlider.gameObject.SetActive(false);
            }

            return;
        }

        var userEquipment = DataManager.Instance?.GetUserEquipment(currentEquipmentIndex);
        if (userEquipment == null)
        {
            Debug.LogWarning($"������������܂���B�C���f�b�N�X: {currentEquipmentIndex}");
            return;
        }

        var masterData = DataManager.Instance.GetEquipmentData(userEquipment.equipmentId);
        if (masterData == null)
        {
            Debug.LogWarning($"�����}�X�^�[�f�[�^��������܂���BID: {userEquipment.equipmentId}");
            return;
        }

        // ��{���
        if (equipmentIcon != null && masterData.icon != null)
        {
            equipmentIcon.sprite = masterData.icon;
            equipmentIcon.color = Color.white; // �A�C�R���𔒐F�ŕ\��
            Debug.Log($"�����A�C�R���ݒ�: {masterData.equipmentName}, �A�C�R��: {masterData.icon?.name ?? "null"}");
        }

        if (equipmentNameText != null) equipmentNameText.text = masterData.equipmentName;

        // �C��: �\�������𒲐�
        // 1. �������x���i��ԏ�j
        if (enhancementLevelText != null) enhancementLevelText.text = $"+{userEquipment.enhancementLevel}";

        // 2. ��{�X�e�[�^�X�\���i�}�X�^�[�f�[�^ + ���[�U�[�{�[�i�X�j
        UpdateStatText(attackText, "�U����", userEquipment.GetTotalAttack());
        UpdateStatText(defenseText, "�h���", userEquipment.GetTotalDefense());
        UpdateStatText(criticalRateText, "�N���e�B�J����", userEquipment.GetTotalCriticalRate(), "%");
        UpdateStatText(hpText, "HP", userEquipment.GetTotalHP());

        // 3. �����U���̕\���i�ݒ肳��Ă���ꍇ�̂݁j
        UpdateStatText(elementalAttackText, "�����U��", userEquipment.GetTotalElementalAttack());
        UpdateStatText(fireAttackText, "�Α����U��", userEquipment.GetTotalFireAttack());
        UpdateStatText(waterAttackText, "�������U��", userEquipment.GetTotalWaterAttack());
        UpdateStatText(windAttackText, "�������U��", userEquipment.GetTotalWindAttack());
        UpdateStatText(earthAttackText, "�y�����U��", userEquipment.GetTotalEarthAttack());

        // 4. �ϋv�x�\���i��ԉ��j
        if (durabilityText != null) durabilityText.text = $"{userEquipment.currentDurability}/{masterData.stats.baseDurability}";

        if (durabilitySlider != null)
        {
            durabilitySlider.gameObject.SetActive(true);
            durabilitySlider.maxValue = masterData.stats.baseDurability;
            durabilitySlider.value = userEquipment.currentDurability;

            // �ϋv�x�ɉ����ĐF��ύX
            float ratio = (float)userEquipment.currentDurability / masterData.stats.baseDurability;
            Color color = ratio > 0.6f ? Color.green : ratio > 0.3f ? Color.yellow : Color.red;

            if (durabilityText != null) durabilityText.color = color;

            var fillImage = durabilitySlider.fillRect?.GetComponent<Image>();
            if (fillImage != null) fillImage.color = color;
        }

        // �f�o�b�O: ���݂̑����U���l�����O�o��
        Debug.Log($"�����X�e�[�^�X�X�V - {masterData.equipmentName}:");
        Debug.Log($"  �Α����U��: {userEquipment.GetTotalFireAttack()} (base: {masterData.stats.baseFireAttack}, bonus: {userEquipment.bonusFireAttack})");
        Debug.Log($"  �������U��: {userEquipment.GetTotalWaterAttack()} (base: {masterData.stats.baseWaterAttack}, bonus: {userEquipment.bonusWaterAttack})");
        Debug.Log($"  �������U��: {userEquipment.GetTotalWindAttack()} (base: {masterData.stats.baseWindAttack}, bonus: {userEquipment.bonusWindAttack})");
        Debug.Log($"  �y�����U��: {userEquipment.GetTotalEarthAttack()} (base: {masterData.stats.baseEarthAttack}, bonus: {userEquipment.bonusEarthAttack})");
    }

    // �� �C��: �����A�C�e���\�����X�V
    private void UpdateEnhancementItemDisplay()
    {
        if (selectedEnhancementItemId < 0)
        {
            // �I������Ă��Ȃ��ꍇ�̕\��
            if (enhancementItemIcon != null)
            {
                enhancementItemIcon.sprite = null;
                enhancementItemIcon.color = new Color(1, 1, 1, 0.3f); // �����O���[
            }

            if (enhancementItemNameText != null)
                enhancementItemNameText.text = "�����A�C�e����I��";

            if (enhancementItemQuantityText != null)
                enhancementItemQuantityText.text = "";

            if (enhancementItemEffectText != null)
            {
                enhancementItemEffectText.text = "�����A�C�e����I�����Ă�������\n�E�����������̌��ʂ��\������܂�\n�E�����������ɉe�����܂�";
            }
        }
        else
        {
            var itemData = DataManager.Instance.GetEnhancementItemData(selectedEnhancementItemId);
            if (itemData != null)
            {
                // �A�C�R���\���̏C��
                if (enhancementItemIcon != null)
                {
                    enhancementItemIcon.sprite = itemData.icon;
                    enhancementItemIcon.color = Color.white; // ���F�ŕ\��
                    Debug.Log($"�����A�C�e���A�C�R���ݒ�: {itemData.itemName}, �A�C�R��: {itemData.icon?.name ?? "null"}");
                }

                if (enhancementItemNameText != null)
                    enhancementItemNameText.text = itemData.itemName; // �A�C�e�����̂ݕ\��

                if (enhancementItemQuantityText != null)
                    enhancementItemQuantityText.text = $"������: {DataManager.Instance.GetItemQuantity(selectedEnhancementItemId)}";

                // �� �d�v�ȏC��: ���ʃe�L�X�g��\���i�}�X�^�[�f�[�^�̊�{���������g�p�j
                if (enhancementItemEffectText != null)
                {
                    // �}�X�^�[�f�[�^�̊�{���������g�p�i���������l��⏕�ޗ��̉e�����󂯂Ȃ��j
                    float baseSuccessRate = itemData.successRate;
                    string effectDescription = itemData.GetEffectDescription();

                    enhancementItemEffectText.text = $"{effectDescription}\n��{������: {baseSuccessRate * 100:F0}%\n���Ցϋv: {itemData.GetDurabilityReduction()}";
                }
            }
            else
            {
                Debug.LogError($"�����A�C�e���f�[�^��������܂���: ID {selectedEnhancementItemId}");
            }
        }
    }

    // �⏕�A�C�e���\�����X�V
    private void UpdateSupportItemDisplay()
    {
        if (selectedSupportItemId < 0)
        {
            // �I������Ă��Ȃ��ꍇ�̕\��
            if (supportItemIcon != null)
            {
                supportItemIcon.sprite = null;
                supportItemIcon.color = new Color(1, 1, 1, 0.3f); // �����O���[
            }

            if (supportItemNameText != null)
                supportItemNameText.text = "�⏕�A�C�e���i�C�Ӂj";

            if (supportItemQuantityText != null)
                supportItemQuantityText.text = "";

            if (supportItemEffectText != null)
                supportItemEffectText.text = "";
        }
        else
        {
            var itemData = DataManager.Instance.GetSupportMaterialData(selectedSupportItemId);
            if (itemData != null)
            {
                // �A�C�R���\���̏C��
                if (supportItemIcon != null)
                {
                    supportItemIcon.sprite = itemData.icon;
                    supportItemIcon.color = Color.white; // ���F�ŕ\��
                    Debug.Log($"�⏕�A�C�e���A�C�R���ݒ�: {itemData.materialName}, �A�C�R��: {itemData.icon?.name ?? "null"}");
                }

                if (supportItemNameText != null)
                    supportItemNameText.text = itemData.materialName; // �A�C�e�����̂ݕ\��

                if (supportItemQuantityText != null)
                    supportItemQuantityText.text = $"������: {DataManager.Instance.GetItemQuantity(selectedSupportItemId)}";

                if (supportItemEffectText != null)
                    supportItemEffectText.text = itemData.GetEffectDescription();
            }
            else
            {
                Debug.LogError($"�⏕�A�C�e���f�[�^��������܂���: ID {selectedSupportItemId}");
            }
        }
    }

    // �A�C�e���X���b�g����ɐݒ�
    private void SetItemSlotEmpty(Image icon, TextMeshProUGUI nameText, TextMeshProUGUI quantityText, string placeholder)
    {
        if (icon != null)
        {
            icon.sprite = null;
            icon.color = new Color(1, 1, 1, 0.3f); // �������ŕ\��
            icon.enabled = true; // �摜�͗L���̂܂܁i�w�i�\���p�j
        }

        if (nameText != null)
        {
            nameText.text = placeholder;
        }

        if (quantityText != null)
        {
            quantityText.text = "";
        }
    }

    // �����{�^���̏�Ԃ��X�V
    private void UpdateEnhanceButton()
    {
        // �� �C��: �������I������Ă��Ȃ��ꍇ�̏�����ǉ�
        if (currentEquipmentIndex < 0)
        {
            enhanceButton.interactable = false;
            enhanceButtonText.text = "�������s";
            successRateText.text = "������I�����Ă�������";
            return;
        }

        var userEquipment = DataManager.Instance.GetUserEquipment(currentEquipmentIndex);
        bool canEnhance = userEquipment != null && selectedEnhancementItemId >= 0 &&
                         userEquipment.CanEnhance() &&
                         DataManager.Instance.GetItemQuantity(selectedEnhancementItemId) > 0;

        enhanceButton.interactable = canEnhance;

        if (canEnhance)
        {
            float successRate = CalculateSuccessRate();
            enhanceButtonText.text = "�������s";
            successRateText.text = $"������: {successRate * 100:F1}%";
        }
        else
        {
            enhanceButtonText.text = "�������s";
            successRateText.text = "�����Ƌ����A�C�e����I�����Ă�������";
        }
    }

    // �����m���v�Z
    private float CalculateSuccessRate()
    {
        // �� �C��: �������I������Ă��Ȃ��ꍇ��0��Ԃ�
        if (currentEquipmentIndex < 0 || selectedEnhancementItemId < 0) return 0f;

        var enhancementItem = DataManager.Instance.GetEnhancementItemData(selectedEnhancementItemId);
        var userEquipment = DataManager.Instance.GetUserEquipment(currentEquipmentIndex);
        if (enhancementItem == null || userEquipment == null) return 0f;

        float baseRate = enhancementItem.GetAdjustedSuccessRate(userEquipment.enhancementLevel);

        // �⏕�A�C�e���̃{�[�i�X
        if (selectedSupportItemId >= 0)
        {
            var supportItem = DataManager.Instance.GetSupportMaterialData(selectedSupportItemId);
            if (supportItem != null && supportItem.materialType == "lucky_stone")
            {
                baseRate += supportItem.successRateModifier;
            }
        }

        return Mathf.Clamp01(baseRate);
    }

    // �����{�^���N���b�N
    private void OnEnhanceButtonClicked()
    {
        // �� �C��: �������I������Ă��Ȃ��ꍇ�̃`�F�b�N��ǉ�
        if (currentEquipmentIndex < 0 || selectedEnhancementItemId < 0)
        {
            Debug.LogWarning("�����ɕK�v�ȃA�C�e�����I������Ă��܂���");
            return;
        }

        // �������s
        bool success = DataManager.Instance.EnhanceEquipment(
            currentEquipmentIndex,
            selectedEnhancementItemId,
            selectedSupportItemId >= 0 ? selectedSupportItemId : -1
        );

        Debug.Log($"��������: {(success ? "����" : "���s")}");

        // �G�t�F�N�g�Đ�
        StartCoroutine(PlayEnhancementEffect(success));

        // �A�C�e���I�������Z�b�g
        selectedEnhancementItemId = -1;
        selectedSupportItemId = -1;

        // �d�v: UI�X�V���������s
        Debug.Log("���������������UI�X�V�J�n");
        RefreshUI();
        Debug.Log("���������������UI�X�V����");

        // �ǉ�: ������̃X�e�[�^�X�m�F
        var userEquipment = DataManager.Instance.GetUserEquipment(currentEquipmentIndex);
        if (userEquipment != null)
        {
            Debug.Log($"=== ����������̍ŏI�m�F ===");
            Debug.Log($"�������x��: {userEquipment.enhancementLevel}");
            Debug.Log($"�Α����U��: {userEquipment.GetTotalFireAttack()} (base: {userEquipment.bonusFireAttack})");
            Debug.Log($"�U����: {userEquipment.GetTotalAttack()}");
            Debug.Log($"==============================");
        }
    }

    // �A�C�e���I�����̃R�[���o�b�N
    private void OnItemSelected(string itemId, string itemType)
    {
        Debug.Log($"�A�C�e���I��: ID={itemId}, Type={itemType}");

        if (itemType == "equipment")
        {
            // �����I���̏ꍇ
            if (int.TryParse(itemId, out int equipIndex))
            {
                SelectEquipment(equipIndex);
                Debug.Log($"�����I������: �C���f�b�N�X {equipIndex}");
            }
            return;
        }

        if (itemType == "support_none")
        {
            // �⏕�ޗ��u�I���Ȃ��v
            Debug.Log("�⏕�ޗ����u�I���Ȃ��v�ɐݒ�");
            selectedSupportItemId = -1; // -1�ɐݒ肵�đI������
            RefreshUI();
            return;
        }

        if (int.TryParse(itemId, out int id))
        {
            switch (itemType)
            {
                case "enhancement":
                    selectedEnhancementItemId = id;
                    Debug.Log($"�����A�C�e���I��: ID={id}");

                    // �����ɃA�C�R���f�o�b�O
                    var enhanceItem = DataManager.Instance.GetEnhancementItemData(id);
                    if (enhanceItem != null)
                    {
                        Debug.Log($"�����A�C�e���ڍ�: {enhanceItem.itemName}, �A�C�R������: {enhanceItem.icon != null}");
                    }
                    break;
                case "support":
                    selectedSupportItemId = id;
                    Debug.Log($"�⏕�ޗ��I��: ID={id}");

                    // �����ɃA�C�R���f�o�b�O
                    var supportItem = DataManager.Instance.GetSupportMaterialData(id);
                    if (supportItem != null)
                    {
                        Debug.Log($"�⏕�ޗ��ڍ�: {supportItem.materialName}, �A�C�R������: {supportItem.icon != null}");
                    }
                    break;
            }
        }

        RefreshUI();
        Debug.Log("UI�X�V����");
    }

    // �I���L�����Z�����̃R�[���o�b�N
    private void OnSelectionCancelled()
    {
        // ���ɏ����Ȃ�
    }

    // �����G�t�F�N�g�Đ�
    private IEnumerator PlayEnhancementEffect(bool success)
    {
        // �G�t�F�N�g�J�n
        if (effectSpawnPoint != null)
        {
            Debug.Log(success ? "���������G�t�F�N�g�Đ�" : "�������s�G�t�F�N�g�Đ�");
        }

        // �����Đ�
        if (audioSource != null)
        {
            // �����Ő���/���s�����Đ�
        }

        yield return new WaitForSeconds(1f); // �G�t�F�N�g����

        Debug.Log(success ? "���������I" : "�������s...");
    }

    // �ǉ��F�X�e�[�^�X�\���p�w���p�[���\�b�h
    private void UpdateStatText(TextMeshProUGUI textComponent, string statName, float value, string suffix = "")
    {
        if (textComponent == null)
        {
            if (value > 0)
            {
                Debug.LogWarning($"{statName} �� TextComponent �� null �ł��i�l: {value}�j");
            }
            return;
        }

        if (value > 0)
        {
            // �l��0���傫���ꍇ�̂ݕ\��
            textComponent.text = $"{statName}: {value:F1}{suffix}";
            textComponent.gameObject.SetActive(true);
            Debug.Log($"�X�e�[�^�X�\���X�V: {statName} = {value:F1}{suffix}");
        }
        else
        {
            // �l��0�ȉ��̏ꍇ�͔�\��
            textComponent.gameObject.SetActive(false);
            Debug.Log($"�X�e�[�^�X��\��: {statName} (�l: {value})");
        }
    }

    // �� �ǉ�: �X�e�[�^�X�e�L�X�g����ɂ���w���p�[���\�b�h
    private void SetStatTextEmpty(TextMeshProUGUI textComponent)
    {
        if (textComponent != null)
        {
            textComponent.text = "";
            textComponent.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // �C�x���g����
        if (itemSelectionUI != null)
        {
            itemSelectionUI.OnItemSelected -= OnItemSelected;
            itemSelectionUI.OnSelectionCancelled -= OnSelectionCancelled;
        }
    }
}