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
    public Button questSelectionBackButton;  // 追加：クエスト選択画面の戻るボタン

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
        // メインボタンの設定
        questButton?.onClick.AddListener(OnQuestButtonClicked);
        equipmentButton?.onClick.AddListener(OnEquipmentButtonClicked);
        enhancementButton?.onClick.AddListener(OnEnhancementButtonClicked);
        inventoryButton?.onClick.AddListener(OnInventoryButtonClicked);

        // クエスト関連ボタンの設定
        departureButton?.onClick.AddListener(OnDepartureButtonClicked);
        backButton?.onClick.AddListener(OnBackButtonClicked);
        questSelectionBackButton?.onClick.AddListener(OnQuestSelectionBackButtonClicked);

        // 初期状態でクエスト関連パネルを非表示
        questSelectionPanel?.SetActive(false);
        questListPanel?.SetActive(false);
        questDeparturePanel?.SetActive(false);
    }

    private void InitializeHome()
    {
        // ユーザーキャラクター表示の初期化
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
        // 装備シーンへの遷移（後で実装）
        // GameSceneManager.Instance?.LoadEquipmentScene();
    }

    private void OnEnhancementButtonClicked()
    {
        Debug.Log("強化ボタンがクリックされました");
        GameSceneManager.Instance.LoadEquipmentScene();
        // 強化シーンへの遷移（後で実装）
        // GameSceneManager.Instance?.LoadEnhancementScene();
    }

    private void OnInventoryButtonClicked()
    {
        Debug.Log("インベントリボタンがクリックされました");
        // インベントリシーンへの遷移（後で実装）
        // GameSceneManager.Instance?.LoadInventoryScene();
    }

    #endregion

    #region Quest Selection Flow

    private void ShowQuestSelection()
    {
        // クエスト選択パネルを表示
        questSelectionPanel?.SetActive(true);
        questListPanel?.SetActive(true);
        questDeparturePanel?.SetActive(false);

        // ユーザーキャラクター表示を非表示
        userCharacterDisplay?.SetActive(false);

        // クエストリストを更新
        UpdateQuestList();
    }

    private void UpdateQuestList()
    {
        if (questListContent == null || questListItemPrefab == null)
        {
            Debug.LogError("クエストリストの設定が不完全です");
            return;
        }

        // 既存のリストアイテムをクリア
        foreach (Transform child in questListContent)
        {
            Destroy(child.gameObject);
        }

        // DataManagerから直接クエストデータを取得
        var questDataList = DataManager.Instance?.GetAllQuestData();
        if (questDataList == null || questDataList.Count == 0)
        {
            Debug.LogError("クエストデータが取得できません");
            return;
        }

        // 各クエストのリストアイテムを生成
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
        // クエストリストを非表示にして、出撃画面を表示
        questListPanel?.SetActive(false);
        questDeparturePanel?.SetActive(true);

        // クエスト詳細情報を表示
        UpdateQuestDepartureInfo(questData);
    }

    private void UpdateQuestDepartureInfo(QuestData questData)
    {
        if (questData == null) return;

        // 基本情報の表示
        SetText(questTypeText, questData.questType.ToString());
        SetText(questNameText, questData.questName);
        SetText(questDescriptionText, questData.questDescription);

        // クリア制限の表示
        string clearLimitStr = questData.clearLimit == -1 ? "無制限" : $"残り{questData.clearLimit}回";
        SetText(clearLimitText, clearLimitStr);

        // ターン制限の表示
        SetText(turnLimitText, $"制限ターン: {questData.turnLimit}");

        // 初回クリア報酬の表示（既存のQuestDataの構造に合わせる）
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

        // クエストシーンへの遷移
        // GameSceneManager.Instance?.LoadQuestScene(currentSelectedQuest.questId);

        // 仮の処理（実際のクエスト実行ロジックは別途実装）
        StartQuest(currentSelectedQuest);
    }

    private void OnQuestSelectionBackButtonClicked()
    {
        // クエスト選択全体を閉じてホーム画面に戻る
        CloseQuestSelection();
    }

    private void OnBackButtonClicked()
    {
        if (questDeparturePanel.activeInHierarchy)
        {
            // 出撃画面からクエストリストに戻る
            questDeparturePanel?.SetActive(false);
            questListPanel?.SetActive(true);
        }
        else
        {
            // クエスト選択全体を閉じてホーム画面に戻る
            CloseQuestSelection();
        }
    }

    private void CloseQuestSelection()
    {
        questSelectionPanel?.SetActive(false);
        questListPanel?.SetActive(false);
        questDeparturePanel?.SetActive(false);

        // ユーザーキャラクター表示を再表示
        userCharacterDisplay?.SetActive(true);

        currentSelectedQuest = null;
    }

    #endregion

    #region Quest Execution

    private void StartQuest(QuestData questData)
    {
        // スタミナチェック
        int currentStamina = DataManager.Instance?.GetCurrentStamina() ?? 0;
        if (currentStamina < questData.requiredStamina)
        {
            Debug.LogWarning("スタミナが不足しています");
            // スタミナ不足のダイアログ表示
            return;
        }

        // 前提クエストチェック
        if (questData.prerequisiteQuestIds != null && questData.prerequisiteQuestIds.Length > 0)
        {
            foreach (int prerequisiteId in questData.prerequisiteQuestIds)
            {
                if (!DataManager.Instance.IsQuestCleared(prerequisiteId))
                {
                    Debug.LogWarning($"前提クエスト（ID: {prerequisiteId}）がクリアされていません");
                    return;
                }
            }
        }

        // クエスト開始処理
        Debug.Log($"クエスト開始: {questData.questName}");

        // スタミナ消費
        DataManager.Instance?.ConsumeStamina(questData.requiredStamina);

        // クエスト画面への遷移
        CloseQuestSelection();

        // 実際のクエスト実行（バトルシーンなど）は別途実装
        // QuestManager.Instance?.StartQuest(questData);

        // DataManagerのクエスト開始機能を使用
        bool questStarted = DataManager.Instance.StartQuest(currentSelectedQuest.questId);
        if (questStarted)
        {
            Debug.Log($"クエスト {currentSelectedQuest.questName} を開始しました");
            // ここで実際のクエストシーンに遷移
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