using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ステータス表示項目UIコンポーネント
/// 
/// 【責任】
/// - ステータス名と値の表示UI制御
/// - プレハブとして使用されるコンポーネント
/// - 色変更やテキスト更新などのUI操作
/// 
/// 【使用箇所】
/// - Enhance_StatusDisplayController（ステータス項目生成）
/// - statusItemPrefabにアタッチされる
/// 
/// 【設計原則】
/// - UI層：MonoBehaviourを継承したUIコンポーネント
/// - 単一責任：ステータス1項目の表示のみ担当
/// - 再利用性：どのステータス表示でも使用可能
/// </summary>
public class StatusDisplayItem : MonoBehaviour
{
    #region SerializeField

    [Header("UI Elements")]
    [SerializeField] private Text nameText;
    [SerializeField] private Text valueText;

    [Header("Optional UI Elements")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;

    [Header("Colors")]
    [SerializeField] private Color defaultBackgroundColor = Color.white;
    [SerializeField] private Color highlightBackgroundColor = Color.yellow;

    #endregion

    #region Properties

    /// <summary>
    /// ステータス名テキスト（読み取り専用）
    /// </summary>
    public string StatusName => nameText != null ? nameText.text : "";

    /// <summary>
    /// ステータス値テキスト（読み取り専用）
    /// </summary>
    public string StatusValue => valueText != null ? valueText.text : "";

    /// <summary>
    /// セットアップ済みかどうか
    /// </summary>
    public bool IsSetup { get; private set; }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // 必須コンポーネントの自動取得（SerializeFieldが未設定の場合）
        if (nameText == null)
        {
            nameText = transform.Find("NameText")?.GetComponent<Text>();
        }

        if (valueText == null)
        {
            valueText = transform.Find("ValueText")?.GetComponent<Text>();
        }

        // オプショナルコンポーネントの自動取得
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }
    }

    private void OnValidate()
    {
        // エディタでの検証
        ValidateComponents();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// ステータス項目のセットアップ（基本版）
    /// </summary>
    /// <param name="name">ステータス名</param>
    /// <param name="value">ステータス値</param>
    public void Setup(string name, string value)
    {
        Setup(name, value, Color.white);
    }

    /// <summary>
    /// ステータス項目のセットアップ（色指定版）
    /// </summary>
    /// <param name="name">ステータス名</param>
    /// <param name="value">ステータス値</param>
    /// <param name="textColor">値テキストの色</param>
    public void Setup(string name, string value, Color textColor)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning($"[StatusDisplayItem] ステータス名が空です: {gameObject.name}");
            return;
        }

        // ステータス名設定
        if (nameText != null)
        {
            nameText.text = name;
        }
        else
        {
            Debug.LogError($"[StatusDisplayItem] nameTextが見つかりません: {gameObject.name}");
        }

        // ステータス値設定
        if (valueText != null)
        {
            valueText.text = value ?? "0";
            valueText.color = textColor;
        }
        else
        {
            Debug.LogError($"[StatusDisplayItem] valueTextが見つかりません: {gameObject.name}");
        }

        IsSetup = true;
    }

    /// <summary>
    /// StatusDisplayDataからのセットアップ
    /// </summary>
    /// <param name="statusData">ステータスデータ</param>
    public void Setup(StatusDisplayData statusData)
    {
        if (statusData == null)
        {
            Debug.LogWarning($"[StatusDisplayItem] StatusDisplayDataがnullです: {gameObject.name}");
            return;
        }

        Setup(statusData.name, statusData.value.ToString());
    }

    /// <summary>
    /// StatusPreviewDataからのセットアップ
    /// </summary>
    /// <param name="previewData">プレビューデータ</param>
    /// <param name="increaseColor">増加時の色</param>
    /// <param name="decreaseColor">減少時の色</param>
    /// <param name="normalColor">変化なし時の色</param>
    public void Setup(StatusPreviewData previewData, Color increaseColor, Color decreaseColor, Color normalColor)
    {
        if (previewData == null)
        {
            Debug.LogWarning($"[StatusDisplayItem] StatusPreviewDataがnullです: {gameObject.name}");
            return;
        }

        // 変化量に応じた色選択
        Color textColor = normalColor;
        if (previewData.IsIncrease)
            textColor = increaseColor;
        else if (previewData.IsDecrease)
            textColor = decreaseColor;

        // 表示テキスト生成
        string displayText = previewData.GetDisplayText();

        Setup(previewData.name, displayText, textColor);
    }

    /// <summary>
    /// 値のみ更新
    /// </summary>
    /// <param name="value">新しい値</param>
    public void UpdateValue(string value)
    {
        if (valueText != null)
        {
            valueText.text = value ?? "0";
        }
    }

    /// <summary>
    /// 値のみ更新（色指定）
    /// </summary>
    /// <param name="value">新しい値</param>
    /// <param name="textColor">テキスト色</param>
    public void UpdateValue(string value, Color textColor)
    {
        if (valueText != null)
        {
            valueText.text = value ?? "0";
            valueText.color = textColor;
        }
    }

    /// <summary>
    /// ハイライト表示切り替え
    /// </summary>
    /// <param name="highlight">ハイライトするかどうか</param>
    public void SetHighlight(bool highlight)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = highlight ? highlightBackgroundColor : defaultBackgroundColor;
        }
    }

    /// <summary>
    /// アイコン設定
    /// </summary>
    /// <param name="icon">アイコンSprite</param>
    public void SetIcon(Sprite icon)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(icon != null);
        }
    }

    /// <summary>
    /// 表示内容をクリア
    /// </summary>
    public void Clear()
    {
        if (nameText != null) nameText.text = "";
        if (valueText != null) valueText.text = "";
        if (iconImage != null) iconImage.sprite = null;

        SetHighlight(false);
        IsSetup = false;
    }

    #endregion

    #region Validation

    /// <summary>
    /// コンポーネントの妥当性検証
    /// </summary>
    private void ValidateComponents()
    {
        bool hasErrors = false;

        if (nameText == null)
        {
            Debug.LogWarning($"[StatusDisplayItem] nameTextが未設定です: {gameObject.name}");
            hasErrors = true;
        }

        if (valueText == null)
        {
            Debug.LogWarning($"[StatusDisplayItem] valueTextが未設定です: {gameObject.name}");
            hasErrors = true;
        }

        if (hasErrors)
        {
            Debug.LogWarning($"[StatusDisplayItem] 必須コンポーネントが不足しています。Awakeで自動取得を試行します: {gameObject.name}");
        }
    }

    /// <summary>
    /// セットアップ状態の検証
    /// </summary>
    /// <returns>正常にセットアップされている場合true</returns>
    public bool ValidateSetup()
    {
        return IsSetup &&
               nameText != null &&
               valueText != null &&
               !string.IsNullOrEmpty(nameText.text);
    }

    #endregion

    #region Editor Utilities

#if UNITY_EDITOR
    /// <summary>
    /// エディタ用：テスト用データでセットアップ
    /// </summary>
    [ContextMenu("Test Setup")]
    private void TestSetup()
    {
        Setup("テストHP", "100", Color.green);
        Debug.Log($"[StatusDisplayItem] テストセットアップ完了: {gameObject.name}");
    }

    /// <summary>
    /// エディタ用：プレビューテスト
    /// </summary>
    [ContextMenu("Test Preview")]
    private void TestPreview()
    {
        var previewData = new StatusPreviewData("攻撃力", 150, 25);
        Setup(previewData, Color.green, Color.red, Color.white);
        Debug.Log($"[StatusDisplayItem] プレビューテスト完了: {gameObject.name}");
    }
#endif

    #endregion
}