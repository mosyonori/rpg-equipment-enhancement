using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// �z�[����ʂ̃��C��UI�Ǘ��N���X
/// �N�G�X�g�{�^������N�G�X�g�I����ʂւ̑J�ڂ��Ǘ�
/// </summary>
public class HomeUIManager : MonoBehaviour
{
    [Header("Main UI Buttons")]
    public Button questButton;
    public Button equipmentButton;
    public Button enhancementButton;
    public Button inventoryButton;

    [Header("Quest UI Panels")]
    public GameObject questSelectionPanel;
    public GameObject questListPanel;
    public GameObject questDeparturePanel;
    public Button questSelectionBackButton;  // �ǉ��F�N�G�X�g�I����ʂ̖߂�{�^��

    [Header("Quest List UI")]
    public Transform questListContent;
    public GameObject questListItemPrefab;

    [Header("Quest Departure UI")]
    public TextMeshProUGUI questTypeText;
    public TextMeshProUGUI questNameText;
    public TextMeshProUGUI questDescriptionText;
    public TextMeshProUGUI clearLimitText;
    public TextMeshProUGUI turnLimitText;
    public TextMeshProUGUI firstClearRewardText;
    public Button departureButton;
    public Button backButton;

    [Header("User Character Display")]
    public GameObject userCharacterDisplay;

    private QuestData currentSelectedQuest;

    private void Start()
    {
        SetupUI();
        InitializeHome();
    }

    private void SetupUI()
    {
        // ���C���{�^���̐ݒ�
        questButton?.onClick.AddListener(OnQuestButtonClicked);
        equipmentButton?.onClick.AddListener(OnEquipmentButtonClicked);
        enhancementButton?.onClick.AddListener(OnEnhancementButtonClicked);
        inventoryButton?.onClick.AddListener(OnInventoryButtonClicked);

        // �N�G�X�g�֘A�{�^���̐ݒ�
        departureButton?.onClick.AddListener(OnDepartureButtonClicked);
        backButton?.onClick.AddListener(OnBackButtonClicked);
        questSelectionBackButton?.onClick.AddListener(OnQuestSelectionBackButtonClicked);

        // ������ԂŃN�G�X�g�֘A�p�l�����\��
        questSelectionPanel?.SetActive(false);
        questListPanel?.SetActive(false);
        questDeparturePanel?.SetActive(false);
    }

    private void InitializeHome()
    {
        // ���[�U�[�L�����N�^�[�\���̏�����
        if (userCharacterDisplay != null)
        {
            userCharacterDisplay.SetActive(true);
        }
    }

    #region Main Button Events

    private void OnQuestButtonClicked()
    {
        Debug.Log("�N�G�X�g�{�^�����N���b�N����܂���");
        ShowQuestSelection();
    }

    private void OnEquipmentButtonClicked()
    {
        Debug.Log("�����ҏW�{�^�����N���b�N����܂���");
        // �����V�[���ւ̑J�ځi��Ŏ����j
        // GameSceneManager.Instance?.LoadEquipmentScene();
    }

    private void OnEnhancementButtonClicked()
    {
        Debug.Log("�����{�^�����N���b�N����܂���");
        GameSceneManager.Instance.LoadEquipmentScene();
        // �����V�[���ւ̑J�ځi��Ŏ����j
        // GameSceneManager.Instance?.LoadEnhancementScene();
    }

    private void OnInventoryButtonClicked()
    {
        Debug.Log("�C���x���g���{�^�����N���b�N����܂���");
        // �C���x���g���V�[���ւ̑J�ځi��Ŏ����j
        // GameSceneManager.Instance?.LoadInventoryScene();
    }

    #endregion

    #region Quest Selection Flow

    private void ShowQuestSelection()
    {
        // �N�G�X�g�I���p�l����\��
        questSelectionPanel?.SetActive(true);
        questListPanel?.SetActive(true);
        questDeparturePanel?.SetActive(false);

        // ���[�U�[�L�����N�^�[�\�����\��
        userCharacterDisplay?.SetActive(false);

        // �N�G�X�g���X�g���X�V
        UpdateQuestList();
    }

    private void UpdateQuestList()
    {
        if (questListContent == null || questListItemPrefab == null)
        {
            Debug.LogError("�N�G�X�g���X�g�̐ݒ肪�s���S�ł�");
            return;
        }

        // �����̃��X�g�A�C�e�����N���A
        foreach (Transform child in questListContent)
        {
            Destroy(child.gameObject);
        }

        // DataManager���璼�ڃN�G�X�g�f�[�^���擾
        var questDataList = DataManager.Instance?.GetAllQuestData();
        if (questDataList == null || questDataList.Count == 0)
        {
            Debug.LogError("�N�G�X�g�f�[�^���擾�ł��܂���");
            return;
        }

        // �e�N�G�X�g�̃��X�g�A�C�e���𐶐�
        foreach (var questData in questDataList)
        {
            CreateQuestListItem(questData);
        }
    }

    private void CreateQuestListItem(QuestData questData)
    {
        GameObject listItem = Instantiate(questListItemPrefab, questListContent);
        QuestListItem questListItem = listItem.GetComponent<QuestListItem>();

        if (questListItem != null)
        {
            questListItem.Initialize(questData);
            questListItem.OnQuestSelected += OnQuestSelected;
        }
    }

    private void OnQuestSelected(QuestData questData)
    {
        Debug.Log($"�N�G�X�g���I������܂���: {questData.questName}");
        currentSelectedQuest = questData;
        ShowQuestDeparture(questData);
    }

    private void ShowQuestDeparture(QuestData questData)
    {
        // �N�G�X�g���X�g���\���ɂ��āA�o����ʂ�\��
        questListPanel?.SetActive(false);
        questDeparturePanel?.SetActive(true);

        // �N�G�X�g�ڍ׏���\��
        UpdateQuestDepartureInfo(questData);
    }

    private void UpdateQuestDepartureInfo(QuestData questData)
    {
        if (questData == null) return;

        // ��{���̕\��
        SetText(questTypeText, questData.questType.ToString());
        SetText(questNameText, questData.questName);
        SetText(questDescriptionText, questData.questDescription);

        // �N���A�����̕\��
        string clearLimitStr = questData.clearLimit == -1 ? "������" : $"�c��{questData.clearLimit}��";
        SetText(clearLimitText, clearLimitStr);

        // �^�[�������̕\��
        SetText(turnLimitText, $"�����^�[��: {questData.turnLimit}");

        // ����N���A��V�̕\���i������QuestData�̍\���ɍ��킹��j
        if (questData.hasFirstClearReward && questData.firstClearItemId > 0)
        {
            string rewardText = $"�����V: {questData.firstClearItemType} x{questData.firstClearItemQuantity}";
            SetText(firstClearRewardText, rewardText);
        }
        else
        {
            SetText(firstClearRewardText, "�����V: �Ȃ�");
        }
    }

    private void OnDepartureButtonClicked()
    {
        if (currentSelectedQuest == null)
        {
            Debug.LogError("�I�����ꂽ�N�G�X�g������܂���");
            return;
        }

        Debug.Log($"�N�G�X�g�ɏo��: {currentSelectedQuest.questName}");

        // �N�G�X�g�V�[���ւ̑J��
        // GameSceneManager.Instance?.LoadQuestScene(currentSelectedQuest.questId);

        // ���̏����i���ۂ̃N�G�X�g���s���W�b�N�͕ʓr�����j
        StartQuest(currentSelectedQuest);
    }

    private void OnQuestSelectionBackButtonClicked()
    {
        // �N�G�X�g�I��S�̂���ăz�[����ʂɖ߂�
        CloseQuestSelection();
    }

    private void OnBackButtonClicked()
    {
        if (questDeparturePanel.activeInHierarchy)
        {
            // �o����ʂ���N�G�X�g���X�g�ɖ߂�
            questDeparturePanel?.SetActive(false);
            questListPanel?.SetActive(true);
        }
        else
        {
            // �N�G�X�g�I��S�̂���ăz�[����ʂɖ߂�
            CloseQuestSelection();
        }
    }

    private void CloseQuestSelection()
    {
        questSelectionPanel?.SetActive(false);
        questListPanel?.SetActive(false);
        questDeparturePanel?.SetActive(false);

        // ���[�U�[�L�����N�^�[�\�����ĕ\��
        userCharacterDisplay?.SetActive(true);

        currentSelectedQuest = null;
    }

    #endregion

    #region Quest Execution

    private void StartQuest(QuestData questData)
    {
        // �X�^�~�i�`�F�b�N
        int currentStamina = DataManager.Instance?.GetCurrentStamina() ?? 0;
        if (currentStamina < questData.requiredStamina)
        {
            Debug.LogWarning("�X�^�~�i���s�����Ă��܂�");
            // �X�^�~�i�s���̃_�C�A���O�\��
            return;
        }

        // �O��N�G�X�g�`�F�b�N
        if (questData.prerequisiteQuestIds != null && questData.prerequisiteQuestIds.Length > 0)
        {
            foreach (int prerequisiteId in questData.prerequisiteQuestIds)
            {
                if (!DataManager.Instance.IsQuestCleared(prerequisiteId))
                {
                    Debug.LogWarning($"�O��N�G�X�g�iID: {prerequisiteId}�j���N���A����Ă��܂���");
                    return;
                }
            }
        }

        // �N�G�X�g�J�n����
        Debug.Log($"�N�G�X�g�J�n: {questData.questName}");

        // �X�^�~�i����
        DataManager.Instance?.ConsumeStamina(questData.requiredStamina);

        // �N�G�X�g��ʂւ̑J��
        CloseQuestSelection();

        // ���ۂ̃N�G�X�g���s�i�o�g���V�[���Ȃǁj�͕ʓr����
        // QuestManager.Instance?.StartQuest(questData);

        // DataManager�̃N�G�X�g�J�n�@�\���g�p
        bool questStarted = DataManager.Instance.StartQuest(currentSelectedQuest.questId);
        if (questStarted)
        {
            Debug.Log($"�N�G�X�g {currentSelectedQuest.questName} ���J�n���܂���");
            // �����Ŏ��ۂ̃N�G�X�g�V�[���ɑJ��
            // GameSceneManager.Instance?.LoadQuestScene();
        }
    }

    #endregion

    #region Utility Methods

    private void SetText(TextMeshProUGUI textComponent, string text)
    {
        if (textComponent != null)
        {
            textComponent.text = text;
        }
    }

    #endregion
}