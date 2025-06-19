using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// クエストリストの各アイテムを管理するスクリプト
/// プレハブにアタッチして使用
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

    // イベント
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
    /// クエストデータでアイテムを初期化
    /// </summary>
    /// <param name="data">表示するクエストデータ</param>
    public void Initialize(QuestData data)
    {
        questData = data;
        UpdateDisplay();
        UpdateSelectability();
    }

    private void UpdateDisplay()
    {
        if (questData == null) return;

        // クエストタイプの表示
        SetText(questTypeText, GetQuestTypeDisplayName(questData.questType));

        // クエスト名の表示
        SetText(questNameText, questData.questName);

        // クリア制限の表示
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
        // プレイヤーレベルチェック
        int playerLevel = DataManager.Instance?.GetPlayerLevel() ?? 1;
        if (playerLevel < questData.requiredLevel)
        {
            return false;
        }

        // 前提クエストチェック（既存のQuestDataの構造に合わせる）
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

        // クリア制限チェック
        if (questData.clearLimit > 0)
        {
            int remainingClears = DataManager.Instance.GetQuestRemainingClears(questData.questId);
            if (remainingClears <= 0)
            {
                return false;
            }
        }

        // スタミナチェック
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

        // 背景色の更新
        Color targetColor = selectable ? normalColor : disabledColor;
        if (backgroundImage != null)
        {
            backgroundImage.color = targetColor;
        }

        // テキストの透明度調整
        float alpha = selectable ? 1.0f : 0.5f;
        SetTextAlpha(questTypeText, alpha);
        SetTextAlpha(questNameText, alpha);
        SetTextAlpha(clearLimitText, alpha);
    }

    private void OnSelectButtonClicked()
    {
        if (!isSelectable || questData == null) return;

        Debug.Log($"クエストリストアイテムが選択されました: {questData.questName}");
        OnQuestSelected?.Invoke(questData);
    }

    #region Display Helpers

    private string GetQuestTypeDisplayName(QuestType questType)
    {
        switch (questType)
        {
            case QuestType.Normal:
                return "通常";
            case QuestType.Daily:
                return "デイリー";
            case QuestType.Event:
                return "イベント";
            default:
                return questType.ToString();
        }
    }

    private string GetClearLimitDisplay(int clearLimit)
    {
        if (clearLimit == -1)
        {
            return "無制限";
        }
        else
        {
            int remaining = DataManager.Instance?.GetQuestRemainingClears(questData.questId) ?? clearLimit;
            return $"残り{remaining}回";
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
    /// 選択時のアニメーション効果
    /// </summary>
    public void PlaySelectAnimation()
    {
        if (backgroundImage != null)
        {
            // 簡単な色変更アニメーション
            StartCoroutine(AnimateColorChange());
        }
    }

    private System.Collections.IEnumerator AnimateColorChange()
    {
        Color originalColor = backgroundImage.color;
        Color highlightColor = selectedColor;

        // ハイライト
        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            backgroundImage.color = Color.Lerp(originalColor, highlightColor, t);
            yield return null;
        }

        // 元に戻す
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