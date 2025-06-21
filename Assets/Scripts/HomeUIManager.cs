using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// ホーム画面のメインUI管理クラス
/// クエストボタンからクエスト選択画面への遷移を管理
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
        Debug.Log("クエストボタンがクリックされました");
        ShowQuestSelection();
    }

    private void OnEquipmentButtonClicked()
    {
        Debug.Log("装備編集ボタンがクリックされました");
    }

    private void OnEnhancementButtonClicked()
    {
        Debug.Log("強化ボタンがクリックされました");
        GameSceneManager.Instance.LoadEquipmentScene();
    }

    private void OnInventoryButtonClicked()
    {
        Debug.Log("インベントリボタンがクリックされました");
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
            Debug.LogError("クエストリストの設定が不完全です");
            return;
        }

        foreach (Transform child in questListContent)
        {
            Destroy(child.gameObject);
        }

        var questDataList = DataManager.Instance?.GetAllQuestData();
        if (questDataList == null || questDataList.Count == 0)
        {
            Debug.LogError("クエストデータが取得できません");
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
        Debug.Log($"クエストが選択されました: {questData.questName}");
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

        string clearLimitStr = questData.clearLimit == -1 ? "無制限" : $"残り{questData.clearLimit}回";
        SetText(clearLimitText, clearLimitStr);
        SetText(turnLimitText, $"制限ターン: {questData.turnLimit}");

        if (questData.hasFirstClearReward && questData.firstClearItemId > 0)
        {
            string rewardText = $"初回報酬: {questData.firstClearItemType} x{questData.firstClearItemQuantity}";
            SetText(firstClearRewardText, rewardText);
        }
        else
        {
            SetText(firstClearRewardText, "初回報酬: なし");
        }
    }

    private void OnDepartureButtonClicked()
    {
        if (currentSelectedQuest == null)
        {
            Debug.LogError("選択されたクエストがありません");
            return;
        }

        Debug.Log($"クエストに出撃: {currentSelectedQuest.questName}");
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
        Debug.Log("=== クエスト開始処理デバッグ ===");

        // 1. 基本チェック
        Debug.Log($"クエスト名: {questData.questName}");
        Debug.Log($"必要スタミナ: {questData.requiredStamina}");
        Debug.Log($"必要レベル: {questData.requiredLevel}");

        // 2. DataManager確認
        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager.Instance が null です");
            return;
        }

        // 3. プレイヤー情報確認
        int playerLevel = DataManager.Instance.GetPlayerLevel();
        int currentStamina = DataManager.Instance.GetCurrentStamina();
        Debug.Log($"プレイヤーレベル: {playerLevel}");
        Debug.Log($"現在のスタミナ: {currentStamina}");

        // 4. 前提条件詳細チェック
        if (playerLevel < questData.requiredLevel)
        {
            Debug.LogWarning($"レベル不足: 必要{questData.requiredLevel} / 現在{playerLevel}");
            return;
        }

        if (currentStamina < questData.requiredStamina)
        {
            Debug.LogWarning($"スタミナ不足: 必要{questData.requiredStamina} / 現在{currentStamina}");
            return;
        }

        Debug.Log("前提条件チェック完了 - すべてOK");

        // UI を閉じる
        CloseQuestSelection();

        Debug.Log("戦闘シーンに直接遷移開始");

        // ★★★ 直接シーン遷移を使用 ★★★
        try
        {
            Debug.Log("SceneManager.LoadScene(\"BattleScene\") を実行");
            UnityEngine.SceneManagement.SceneManager.LoadScene("BattleScene");
            Debug.Log("SceneManager.LoadScene 実行完了");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"シーン遷移エラー: {e.Message}");
            Debug.LogError($"スタックトレース: {e.StackTrace}");
        }

        Debug.Log("=== クエスト開始処理デバッグ終了 ===");
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