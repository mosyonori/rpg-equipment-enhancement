using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// シーン遷移ボタン用コンポーネント
/// 各シーンのボタンにアタッチして使用
/// Inspector で遷移先を設定可能
/// </summary>
public class SceneTransitionButton : MonoBehaviour
{
    #region Enums

    /// <summary>
    /// 遷移先タイプ
    /// </summary>
    public enum TransitionType
    {
        ToTitle,           // タイトル画面へ
        ToHome,            // ホーム画面へ
        ToEquipmentEdit,   // 装備編集画面へ
        ToEquipmentEnhance,// 装備強化画面へ
        ToQuestBattle,     // クエスト戦闘画面へ（未実装）
        ToGacha,           // ガチャ画面へ（未実装）
        GoBack,            // 前の画面に戻る
        CustomScene        // カスタムシーン名指定
    }

    #endregion

    #region Inspector Fields

    [Header("遷移設定")]
    [SerializeField] private TransitionType transitionType = TransitionType.ToHome;
    [SerializeField] private string customSceneName = ""; // CustomScene選択時のシーン名
    [SerializeField] private bool requireConfirmation = false; // 確認ダイアログの有無

    [Header("UI参照")]
    [SerializeField] private Button transitionButton;
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool disableWhenTransitioning = true; // 遷移中はボタン無効化

    #endregion

    #region Private Fields

    private bool isInitialized = false;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeButton();
    }

    private void OnEnable()
    {
        // 遷移イベントを監視
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.OnTransitionStarted += OnTransitionStarted;
            SceneTransitionManager.OnTransitionCompleted += OnTransitionCompleted;
        }
    }

    private void OnDisable()
    {
        // イベント監視解除
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.OnTransitionStarted -= OnTransitionStarted;
            SceneTransitionManager.OnTransitionCompleted -= OnTransitionCompleted;
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// ボタン初期化
    /// </summary>
    private void InitializeButton()
    {
        // ボタン参照を自動取得
        if (transitionButton == null)
        {
            transitionButton = GetComponent<Button>();
        }

        if (transitionButton == null)
        {
            LogError("Buttonコンポーネントが見つかりません");
            return;
        }

        // ボタンテキスト参照を自動取得
        if (buttonText == null)
        {
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
        }

        // ボタンイベント設定
        transitionButton.onClick.RemoveAllListeners();
        transitionButton.onClick.AddListener(OnButtonClicked);

        // ボタンテキスト設定
        UpdateButtonText();

        // 初期状態の設定
        UpdateButtonState();

        isInitialized = true;
        LogDebug($"遷移ボタン初期化完了: {transitionType}");
    }

    /// <summary>
    /// ボタンテキストを自動設定
    /// </summary>
    private void UpdateButtonText()
    {
        if (buttonText == null) return;

        string displayText = transitionType switch
        {
            TransitionType.ToTitle => "タイトルへ",
            TransitionType.ToHome => "ホームへ",
            TransitionType.ToEquipmentEdit => "装備編集",
            TransitionType.ToEquipmentEnhance => "装備強化",
            TransitionType.ToQuestBattle => "クエスト",
            TransitionType.ToGacha => "ガチャ",
            TransitionType.GoBack => "戻る",
            TransitionType.CustomScene => !string.IsNullOrEmpty(customSceneName) ? customSceneName : "カスタム",
            _ => "移動"
        };

        buttonText.text = displayText;
    }

    #endregion

    #region Button Event Handlers

    /// <summary>
    /// ボタンクリック時の処理
    /// </summary>
    private void OnButtonClicked()
    {
        if (!isInitialized)
        {
            LogWarning("ボタンが初期化されていません");
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            LogError("SceneTransitionManagerが見つかりません");
            return;
        }

        if (SceneTransitionManager.Instance.IsTransitioning)
        {
            LogWarning("既に遷移中です");
            return;
        }

        LogDebug($"遷移ボタンクリック: {transitionType}");

        // 確認ダイアログが必要な場合
        if (requireConfirmation)
        {
            ShowConfirmationDialog();
            return;
        }

        // 遷移実行
        ExecuteTransition();
    }

    /// <summary>
    /// 遷移実行
    /// </summary>
    private void ExecuteTransition()
    {
        try
        {
            switch (transitionType)
            {
                case TransitionType.ToTitle:
                    SceneTransitionManager.Instance.TransitionToTitle();
                    break;

                case TransitionType.ToHome:
                    SceneTransitionManager.Instance.TransitionToHome();
                    break;

                case TransitionType.ToEquipmentEdit:
                    SceneTransitionManager.Instance.TransitionToEquipmentEdit();
                    break;

                case TransitionType.ToEquipmentEnhance:
                    SceneTransitionManager.Instance.TransitionToEquipmentEnhance();
                    break;

                case TransitionType.ToQuestBattle:
                    SceneTransitionManager.Instance.TransitionToQuestBattle();
                    break;

                case TransitionType.ToGacha:
                    SceneTransitionManager.Instance.TransitionToGacha();
                    break;

                case TransitionType.GoBack:
                    SceneTransitionManager.Instance.GoBack();
                    break;

                case TransitionType.CustomScene:
                    if (!string.IsNullOrEmpty(customSceneName))
                    {
                        SceneTransitionManager.Instance.TransitionToScene(customSceneName);
                    }
                    else
                    {
                        LogError("カスタムシーン名が設定されていません");
                    }
                    break;

                default:
                    LogError($"未対応の遷移タイプ: {transitionType}");
                    break;
            }
        }
        catch (System.Exception e)
        {
            LogError($"遷移実行エラー: {e.Message}");
        }
    }

    #endregion

    #region Confirmation Dialog

    /// <summary>
    /// 確認ダイアログ表示
    /// </summary>
    private void ShowConfirmationDialog()
    {
        string targetName = GetTransitionTargetName();
        string message = $"{targetName}に移動しますか？";

        // TODO: 実際の確認ダイアログUI実装
        /*
        // 実装例（コメントアウト）:
        ConfirmationDialog.Show(
            message,
            onConfirm: ExecuteTransition,
            onCancel: () => LogDebug("遷移をキャンセルしました")
        );
        */

        // 暫定的にデバッグログで確認
        LogDebug($"確認ダイアログ表示: {message}");

        // 暫定的に常に実行（実際のUIができるまで）
        ExecuteTransition();
    }

    /// <summary>
    /// 遷移先名を取得
    /// </summary>
    private string GetTransitionTargetName()
    {
        return transitionType switch
        {
            TransitionType.ToTitle => "タイトル画面",
            TransitionType.ToHome => "ホーム画面",
            TransitionType.ToEquipmentEdit => "装備編集画面",
            TransitionType.ToEquipmentEnhance => "装備強化画面",
            TransitionType.ToQuestBattle => "クエスト画面",
            TransitionType.ToGacha => "ガチャ画面",
            TransitionType.GoBack => "前の画面",
            TransitionType.CustomScene => customSceneName,
            _ => "不明な画面"
        };
    }

    #endregion

    #region State Management

    /// <summary>
    /// ボタン状態更新
    /// </summary>
    private void UpdateButtonState()
    {
        if (transitionButton == null) return;

        bool shouldEnable = true;

        // 遷移中は無効化（設定により）
        if (disableWhenTransitioning && SceneTransitionManager.Instance != null)
        {
            shouldEnable = !SceneTransitionManager.Instance.IsTransitioning;
        }

        // 未実装シーンの場合は警告色に変更
        if (IsUnimplementedScene())
        {
            SetButtonAsUnimplemented();
        }

        transitionButton.interactable = shouldEnable;
    }

    /// <summary>
    /// 未実装シーンかチェック
    /// </summary>
    private bool IsUnimplementedScene()
    {
        return transitionType switch
        {
            TransitionType.ToQuestBattle => !SceneNames.IsSceneImplemented(SceneNames.QUEST_BATTLE),
            TransitionType.ToGacha => !SceneNames.IsSceneImplemented(SceneNames.GACHA),
            TransitionType.CustomScene => !SceneNames.IsSceneImplemented(customSceneName),
            _ => false
        };
    }

    /// <summary>
    /// 未実装ボタンとして設定
    /// </summary>
    private void SetButtonAsUnimplemented()
    {
        if (buttonText != null)
        {
            buttonText.color = Color.gray;
        }

        // TODO: 未実装アイコンの表示など
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// 遷移開始時のハンドラー
    /// </summary>
    private void OnTransitionStarted(string fromScene, string toScene)
    {
        if (disableWhenTransitioning)
        {
            UpdateButtonState();
        }
    }

    /// <summary>
    /// 遷移完了時のハンドラー
    /// </summary>
    private void OnTransitionCompleted(string sceneName)
    {
        UpdateButtonState();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 遷移タイプを動的に変更
    /// </summary>
    /// <param name="newType">新しい遷移タイプ</param>
    /// <param name="customScene">カスタムシーン名（CustomScene選択時）</param>
    public void SetTransitionType(TransitionType newType, string customScene = "")
    {
        transitionType = newType;
        if (newType == TransitionType.CustomScene)
        {
            customSceneName = customScene;
        }

        UpdateButtonText();
        UpdateButtonState();

        LogDebug($"遷移タイプ変更: {newType}");
    }

    /// <summary>
    /// ボタンの有効/無効を設定
    /// </summary>
    /// <param name="enabled">有効にする場合true</param>
    public void SetButtonEnabled(bool enabled)
    {
        if (transitionButton != null)
        {
            transitionButton.interactable = enabled;
        }
    }

    #endregion

    #region Debug Methods

    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SceneTransitionButton] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[SceneTransitionButton] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SceneTransitionButton] {message}");
    }

    #endregion

    #region Inspector Context Menu

#if UNITY_EDITOR
    [ContextMenu("ボタン設定を表示")]
    private void ShowButtonSettings()
    {
        LogDebug($"=== ボタン設定 ===");
        LogDebug($"遷移タイプ: {transitionType}");
        LogDebug($"カスタムシーン名: {customSceneName}");
        LogDebug($"確認ダイアログ: {requireConfirmation}");
        LogDebug($"遷移中無効化: {disableWhenTransitioning}");
        LogDebug($"未実装シーン: {IsUnimplementedScene()}");
    }

    [ContextMenu("遷移テスト")]
    private void TestTransition()
    {
        if (Application.isPlaying)
        {
            LogDebug("遷移テスト実行");
            OnButtonClicked();
        }
        else
        {
            LogDebug("実行時のみテスト可能です");
        }
    }

    [ContextMenu("ボタンテキスト更新")]
    private void UpdateButtonTextEditor()
    {
        UpdateButtonText();
        LogDebug("ボタンテキストを更新しました");
    }
#endif

    #endregion

    #region Validation

    /// <summary>
    /// 設定の妥当性チェック
    /// </summary>
    private void OnValidate()
    {
        if (transitionType == TransitionType.CustomScene && string.IsNullOrEmpty(customSceneName))
        {
            Debug.LogWarning("[SceneTransitionButton] CustomScene選択時はcustomSceneNameを設定してください");
        }

        // エディタ時にボタンテキストを更新
        if (Application.isPlaying && isInitialized)
        {
            UpdateButtonText();
            UpdateButtonState();
        }
    }

    #endregion
}