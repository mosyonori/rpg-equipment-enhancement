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
    public Button questSelectionBackButton;

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
        questButton?.onClick.AddListener(OnQuestButtonClicked);
        equipmentButton?.onClick.AddListener(OnEquipmentButtonClicked);
        enhancementButton?.onClick.AddListener(OnEnhancementButtonClicked);
        inventoryButton?.onClick.AddListener(OnInventoryButtonClicked);

        departureButton?.onClick.AddListener(OnDepartureButtonClicked);
        backButton?.onClick.AddListener(OnBackButtonClicked);
        questSelectionBackButton?.onClick.AddListener(OnQuestSelectionBackButtonClicked);

        questSelectionPanel?.SetActive(false);
        questListPanel?.SetActive(false);
        questDeparturePanel?.SetActive(false);
    }

    private void InitializeHome()
    {
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
    }

    private void OnEnhancementButtonClicked()
    {
        Debug.Log("�����{�^�����N���b�N����܂���");
        GameSceneManager.Instance.LoadEquipmentScene();
    }

    private void OnInventoryButtonClicked()
    {
        Debug.Log("�C���x���g���{�^�����N���b�N����܂���");
    }

    #endregion

    #region Quest Selection Flow

    private void ShowQuestSelection()
    {
        questSelectionPanel?.SetActive(true);
        questListPanel?.SetActive(true);
        questDeparturePanel?.SetActive(false);
        userCharacterDisplay?.SetActive(false);
        UpdateQuestList();
    }

    private void UpdateQuestList()
    {
        if (questListContent == null || questListItemPrefab == null)
        {
            Debug.LogError("�N�G�X�g���X�g�̐ݒ肪�s���S�ł�");
            return;
        }

        foreach (Transform child in questListContent)
        {
            Destroy(child.gameObject);
        }

        var questDataList = DataManager.Instance?.GetAllQuestData();
        if (questDataList == null || questDataList.Count == 0)
        {
            Debug.LogError("�N�G�X�g�f�[�^���擾�ł��܂���");
            return;
        }

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
        questListPanel?.SetActive(false);
        questDeparturePanel?.SetActive(true);
        UpdateQuestDepartureInfo(questData);
    }

    private void UpdateQuestDepartureInfo(QuestData questData)
    {
        if (questData == null) return;

        SetText(questTypeText, questData.questType.ToString());
        SetText(questNameText, questData.questName);
        SetText(questDescriptionText, questData.questDescription);

        string clearLimitStr = questData.clearLimit == -1 ? "������" : $"�c��{questData.clearLimit}��";
        SetText(clearLimitText, clearLimitStr);
        SetText(turnLimitText, $"�����^�[��: {questData.turnLimit}");

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
        StartQuest(currentSelectedQuest);
    }

    private void OnQuestSelectionBackButtonClicked()
    {
        CloseQuestSelection();
    }

    private void OnBackButtonClicked()
    {
        if (questDeparturePanel.activeInHierarchy)
        {
            questDeparturePanel?.SetActive(false);
            questListPanel?.SetActive(true);
        }
        else
        {
            CloseQuestSelection();
        }
    }

    private void CloseQuestSelection()
    {
        questSelectionPanel?.SetActive(false);
        questListPanel?.SetActive(false);
        questDeparturePanel?.SetActive(false);
        userCharacterDisplay?.SetActive(true);
        currentSelectedQuest = null;
    }

    #endregion

    #region Quest Execution

    private void StartQuest(QuestData questData)
    {
        Debug.Log("=== �N�G�X�g�J�n�����f�o�b�O ===");

        // 1. ��{�`�F�b�N
        Debug.Log($"�N�G�X�g��: {questData.questName}");
        Debug.Log($"�K�v�X�^�~�i: {questData.requiredStamina}");
        Debug.Log($"�K�v���x��: {questData.requiredLevel}");

        // 2. DataManager�m�F
        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager.Instance �� null �ł�");
            return;
        }

        // 3. �v���C���[���m�F
        int playerLevel = DataManager.Instance.GetPlayerLevel();
        int currentStamina = DataManager.Instance.GetCurrentStamina();
        Debug.Log($"�v���C���[���x��: {playerLevel}");
        Debug.Log($"���݂̃X�^�~�i: {currentStamina}");

        // 4. �O������ڍ׃`�F�b�N
        if (playerLevel < questData.requiredLevel)
        {
            Debug.LogWarning($"���x���s��: �K�v{questData.requiredLevel} / ����{playerLevel}");
            return;
        }

        if (currentStamina < questData.requiredStamina)
        {
            Debug.LogWarning($"�X�^�~�i�s��: �K�v{questData.requiredStamina} / ����{currentStamina}");
            return;
        }

        Debug.Log("�O������`�F�b�N���� - ���ׂ�OK");

        // UI �����
        CloseQuestSelection();

        Debug.Log("�퓬�V�[���ɒ��ڑJ�ڊJ�n");

        // ������ ���ڃV�[���J�ڂ��g�p ������
        try
        {
            Debug.Log("SceneManager.LoadScene(\"BattleScene\") �����s");
            UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
            Debug.Log("SceneManager.LoadScene ���s����");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"�V�[���J�ڃG���[: {e.Message}");
            Debug.LogError($"�X�^�b�N�g���[�X: {e.StackTrace}");
        }

        Debug.Log("=== �N�G�X�g�J�n�����f�o�b�O�I�� ===");
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