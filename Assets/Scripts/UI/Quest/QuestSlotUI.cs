using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// クエストスロットプレハブ制御クラス
/// 責任範囲：
/// - 個別クエストの基本情報表示
/// - 選択状態の視覚的表現
/// - クリック時の選択要求送信
/// </summary>
public class QuestSlotUI : MonoBehaviour
{
    [Header("基本情報表示")]
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private TextMeshProUGUI clearCountText;

    [Header("アイコン・画像")]
    [SerializeField] private Image questIcon;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image typeIcon;

    [Header("状態表示")]
    [SerializeField] private GameObject newBadge;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject expiredOverlay;

    [Header("ボタン")]
    [SerializeField] private Button selectButton;

    [Header("選択状態")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color disabledColor = Color.gray;

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;

    // イベント
    public event Action<int> OnSlotClicked;

    // 内部状態
    private QuestDisplayData questData;
    private bool isSelected = false;
    private bool isInteractable = true;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネントの初期化
    /// </summary>
    private void InitializeComponents()
    {
        // 必須コンポーネントの確認
        if (selectButton == null)
        {
            selectButton = GetComponent<Button>();
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnButtonClicked);
        }

        // 初期状態設定
        SetSelected(false);

        if (newBadge != null)
        {
            newBadge.SetActive(false);
        }

        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(false);
        }

        if (expiredOverlay != null)
        {
            expiredOverlay.SetActive(false);
        }
    }

    /// <summary>
    /// クエストスロットを初期化
    /// </summary>
    /// <param name="questDisplayData">表示するクエストデータ</param>
    public void Initialize(QuestDisplayData questDisplayData)
    {
        try
        {
            if (questDisplayData == null)
            {
                LogError("QuestDisplayDataがnullです");
                return;
            }

            this.questData = questDisplayData;

            // 基本情報の表示
            DisplayBasicInfo();

            // 状態の表示
            DisplayQuestStatus();

            // アイコンの表示
            DisplayQuestIcon();

            // 相互作用可能性の設定
            UpdateInteractability();

            Log($"クエストスロット初期化完了: {questData.questName}");
        }
        catch (Exception e)
        {
            LogError($"クエストスロット初期化エラー: {e.Message}");
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 選択状態を設定
    /// </summary>
    /// <param name="selected">選択状態</param>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisualState();
    }

    /// <summary>
    /// 相互作用可能性を設定
    /// </summary>
    /// <param name="interactable">相互作用可能かどうか</param>
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        UpdateInteractability();
    }

    /// <summary>
    /// クエストデータを取得
    /// </summary>
    /// <returns>クエストデータ</returns>
    public QuestDisplayData GetQuestData()
    {
        return questData;
    }

    /// <summary>
    /// クエストIDを取得
    /// </summary>
    /// <returns>クエストID</returns>
    public int GetQuestId()
    {
        return questData?.questId ?? -1;
    }

    #endregion

    #region 表示処理

    /// <summary>
    /// 基本情報を表示
    /// </summary>
    private void DisplayBasicInfo()
    {
        if (questData == null) return;

        // クエスト名
        if (questNameText != null)
        {
            questNameText.text = questData.questName;
        }

        // 必要レベル
        if (levelText != null)
        {
            levelText.text = $"Lv.{questData.needLevel}";
        }

        // 必要スタミナ
        if (staminaText != null)
        {
            staminaText.text = $"スタミナ: {questData.requiredStamina}";
        }

        // クリア回数
        if (clearCountText != null)
        {
            if (questData.maxClearCount == -1)
            {
                clearCountText.text = $"クリア: {questData.clearCount}回";
            }
            else
            {
                clearCountText.text = $"クリア: {questData.clearCount}/{questData.maxClearCount}回";
            }
        }
    }

    /// <summary>
    /// クエスト状態を表示
    /// </summary>
    private void DisplayQuestStatus()
    {
        if (questData == null) return;

        // NEW バッジ
        if (newBadge != null)
        {
            newBadge.SetActive(questData.isNew);
        }

        // ロック状態
        if (lockedOverlay != null)
        {
            bool isLocked = questData.status == QuestStatus.Locked;
            lockedOverlay.SetActive(isLocked);
        }

        // 期限切れ状態
        if (expiredOverlay != null)
        {
            bool isExpired = questData.status == QuestStatus.Expired;
            expiredOverlay.SetActive(isExpired);
        }
    }

    /// <summary>
    /// クエストアイコンを表示
    /// </summary>
    private void DisplayQuestIcon()
    {
        if (questData == null) return;

        // クエストアイコン
        if (questIcon != null && !string.IsNullOrEmpty(questData.questIconPath))
        {
            LoadQuestIcon(questData.questIconPath);
        }

        // タイプアイコン
        if (typeIcon != null)
        {
            LoadTypeIcon(questData.questType);
        }
    }

    /// <summary>
    /// クエストアイコンを読み込み
    /// </summary>
    /// <param name="iconPath">アイコンパス</param>
    private void LoadQuestIcon(string iconPath)
    {
        try
        {
            // リソースからアイコンを読み込み
            var sprite = Resources.Load<Sprite>(iconPath);
            if (sprite != null)
            {
                questIcon.sprite = sprite;
                questIcon.gameObject.SetActive(true);
            }
            else
            {
                Log($"クエストアイコンが見つかりません: {iconPath}");
                questIcon.gameObject.SetActive(false);
            }
        }
        catch (Exception e)
        {
            LogError($"クエストアイコン読み込みエラー: {e.Message}");
            questIcon.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// タイプアイコンを読み込み
    /// </summary>
    /// <param name="questType">クエストタイプ</param>
    private void LoadTypeIcon(QuestType questType)
    {
        try
        {
            string typeIconPath = GetTypeIconPath(questType);
            if (string.IsNullOrEmpty(typeIconPath)) return;

            var sprite = Resources.Load<Sprite>(typeIconPath);
            if (sprite != null)
            {
                typeIcon.sprite = sprite;
                typeIcon.gameObject.SetActive(true);
            }
            else
            {
                typeIcon.gameObject.SetActive(false);
            }
        }
        catch (Exception e)
        {
            LogError($"タイプアイコン読み込みエラー: {e.Message}");
            typeIcon.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// クエストタイプに対応するアイコンパスを取得
    /// </summary>
    /// <param name="questType">クエストタイプ</param>
    /// <returns>アイコンパス</returns>
    private string GetTypeIconPath(QuestType questType)
    {
        return questType switch
        {
            QuestType.Story => "Icons/Quest/story_icon",
            QuestType.Daily => "Icons/Quest/daily_icon",
            QuestType.Weekly => "Icons/Quest/weekly_icon",
            QuestType.Event => "Icons/Quest/event_icon",
            _ => ""
        };
    }

    #endregion

    #region 視覚状態制御

    /// <summary>
    /// 視覚状態を更新
    /// </summary>
    private void UpdateVisualState()
    {
        if (backgroundImage == null) return;

        Color targetColor;

        if (!isInteractable)
        {
            targetColor = disabledColor;
        }
        else if (isSelected)
        {
            targetColor = selectedColor;
        }
        else
        {
            targetColor = normalColor;
        }

        backgroundImage.color = targetColor;
    }

    /// <summary>
    /// 相互作用可能性を更新
    /// </summary>
    private void UpdateInteractability()
    {
        if (questData == null) return;

        // クエストの利用可能性チェック
        bool canInteract = questData.isAvailable &&
                          questData.status != QuestStatus.Locked &&
                          questData.status != QuestStatus.Expired &&
                          isInteractable;

        if (selectButton != null)
        {
            selectButton.interactable = canInteract;
        }

        UpdateVisualState();
    }

    #endregion

    #region イベントハンドラ

    /// <summary>
    /// ボタンクリック処理
    /// </summary>
    private void OnButtonClicked()
    {
        try
        {
            if (questData == null)
            {
                LogError("クエストデータがnullです");
                return;
            }

            if (!questData.isAvailable)
            {
                Log($"利用できないクエストです: {questData.questName}");
                return;
            }

            Log($"クエストスロットクリック: {questData.questName}");

            // クリックイベント通知
            OnSlotClicked?.Invoke(questData.questId);
        }
        catch (Exception e)
        {
            LogError($"ボタンクリック処理エラー: {e.Message}");
        }
    }

    #endregion

    #region ユーティリティ

    /// <summary>
    /// クエスト情報の更新
    /// </summary>
    /// <param name="updatedQuestData">更新されたクエストデータ</param>
    public void UpdateQuestData(QuestDisplayData updatedQuestData)
    {
        if (updatedQuestData == null || updatedQuestData.questId != questData?.questId)
        {
            LogError("無効なクエストデータ更新要求");
            return;
        }

        Initialize(updatedQuestData);
    }

    /// <summary>
    /// スロットの有効性をチェック
    /// </summary>
    /// <returns>有効な場合true</returns>
    public bool IsValidSlot()
    {
        return questData != null && questData.questId > 0;
    }

    /// <summary>
    /// デバッグ情報を取得
    /// </summary>
    /// <returns>デバッグ用文字列</returns>
    public string GetDebugInfo()
    {
        if (questData == null) return "QuestData: null";

        return $"Quest[{questData.questId}] {questData.questName} - " +
               $"Status: {questData.status}, Available: {questData.isAvailable}, " +
               $"Selected: {isSelected}, Interactable: {isInteractable}";
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[QuestSlotUI] {message}");
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[QuestSlotUI] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("スロット情報をログ出力")]
    private void LogSlotInfo()
    {
        Log(GetDebugInfo());
    }

    [ContextMenu("選択状態をトグル")]
    private void ToggleSelection()
    {
        SetSelected(!isSelected);
    }

    [ContextMenu("相互作用可能性をトグル")]
    private void ToggleInteractability()
    {
        SetInteractable(!isInteractable);
    }

    private void OnValidate()
    {
        // エディター上でのカラー変更を即座に反映
        if (Application.isPlaying)
        {
            UpdateVisualState();
        }
    }
#endif

    #endregion
}