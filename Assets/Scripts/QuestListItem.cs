using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// �N�G�X�g���X�g�̊e�A�C�e�����Ǘ�����X�N���v�g
/// �v���n�u�ɃA�^�b�`���Ďg�p
/// </summary>
public class QuestListItem : MonoBehaviour
{
    [Header("UI Components")]
    public Button selectButton;
    public TextMeshProUGUI questTypeText;
    public TextMeshProUGUI questNameText;
    public TextMeshProUGUI clearLimitText;
    public Image backgroundImage;

    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color disabledColor = Color.gray;

    // �C�x���g
    public event Action<QuestData> OnQuestSelected;

    private QuestData questData;
    private bool isSelectable = true;

    private void Start()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }
    }

    /// <summary>
    /// �N�G�X�g�f�[�^�ŃA�C�e����������
    /// </summary>
    /// <param name="data">�\������N�G�X�g�f�[�^</param>
    public void Initialize(QuestData data)
    {
        questData = data;
        UpdateDisplay();
        UpdateSelectability();
    }

    private void UpdateDisplay()
    {
        if (questData == null) return;

        // �N�G�X�g�^�C�v�̕\��
        SetText(questTypeText, GetQuestTypeDisplayName(questData.questType));

        // �N�G�X�g���̕\��
        SetText(questNameText, questData.questName);

        // �N���A�����̕\��
        string clearLimitDisplay = GetClearLimitDisplay(questData.clearLimit);
        SetText(clearLimitText, clearLimitDisplay);
    }

    private void UpdateSelectability()
    {
        if (questData == null)
        {
            SetSelectable(false);
            return;
        }

        bool canSelect = CanSelectQuest();
        SetSelectable(canSelect);
    }

    private bool CanSelectQuest()
    {
        // �v���C���[���x���`�F�b�N
        int playerLevel = DataManager.Instance?.GetPlayerLevel() ?? 1;
        if (playerLevel < questData.requiredLevel)
        {
            return false;
        }

        // �O��N�G�X�g�`�F�b�N�i������QuestData�̍\���ɍ��킹��j
        if (questData.prerequisiteQuestIds != null && questData.prerequisiteQuestIds.Length > 0)
        {
            foreach (int prerequisiteId in questData.prerequisiteQuestIds)
            {
                if (!DataManager.Instance.IsQuestCleared(prerequisiteId))
                {
                    return false;
                }
            }
        }

        // �N���A�����`�F�b�N
        if (questData.clearLimit > 0)
        {
            int remainingClears = DataManager.Instance.GetQuestRemainingClears(questData.questId);
            if (remainingClears <= 0)
            {
                return false;
            }
        }

        // �X�^�~�i�`�F�b�N
        int currentStamina = DataManager.Instance?.GetCurrentStamina() ?? 0;
        if (currentStamina < questData.requiredStamina)
        {
            return false;
        }

        return true;
    }

    private void SetSelectable(bool selectable)
    {
        isSelectable = selectable;

        if (selectButton != null)
        {
            selectButton.interactable = selectable;
        }

        // �w�i�F�̍X�V
        Color targetColor = selectable ? normalColor : disabledColor;
        if (backgroundImage != null)
        {
            backgroundImage.color = targetColor;
        }

        // �e�L�X�g�̓����x����
        float alpha = selectable ? 1.0f : 0.5f;
        SetTextAlpha(questTypeText, alpha);
        SetTextAlpha(questNameText, alpha);
        SetTextAlpha(clearLimitText, alpha);
    }

    private void OnSelectButtonClicked()
    {
        if (!isSelectable || questData == null) return;

        Debug.Log($"�N�G�X�g���X�g�A�C�e�����I������܂���: {questData.questName}");
        OnQuestSelected?.Invoke(questData);
    }

    #region Display Helpers

    private string GetQuestTypeDisplayName(QuestType questType)
    {
        switch (questType)
        {
            case QuestType.Normal:
                return "�ʏ�";
            case QuestType.Daily:
                return "�f�C���[";
            case QuestType.Event:
                return "�C�x���g";
            default:
                return questType.ToString();
        }
    }

    private string GetClearLimitDisplay(int clearLimit)
    {
        if (clearLimit == -1)
        {
            return "������";
        }
        else
        {
            int remaining = DataManager.Instance?.GetQuestRemainingClears(questData.questId) ?? clearLimit;
            return $"�c��{remaining}��";
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

    private void SetTextAlpha(TextMeshProUGUI textComponent, float alpha)
    {
        if (textComponent != null)
        {
            Color color = textComponent.color;
            color.a = alpha;
            textComponent.color = color;
        }
    }

    #endregion

    #region Animation Effects (Optional)

    /// <summary>
    /// �I�����̃A�j���[�V��������
    /// </summary>
    public void PlaySelectAnimation()
    {
        if (backgroundImage != null)
        {
            // �ȒP�ȐF�ύX�A�j���[�V����
            StartCoroutine(AnimateColorChange());
        }
    }

    private System.Collections.IEnumerator AnimateColorChange()
    {
        Color originalColor = backgroundImage.color;
        Color highlightColor = selectedColor;

        // �n�C���C�g
        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            backgroundImage.color = Color.Lerp(originalColor, highlightColor, t);
            yield return null;
        }

        // ���ɖ߂�
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            backgroundImage.color = Color.Lerp(highlightColor, originalColor, t);
            yield return null;
        }

        backgroundImage.color = originalColor;
    }

    #endregion
}