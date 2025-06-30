using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 「選択なし」専用のアイテムスロットUI
/// 補助材料選択時にGrid Layout Groupの先頭に表示される
/// </summary>
public class NoneSelectionSlotUI : MonoBehaviour
{
    #region UI References

    [Header("UI要素")]
    [SerializeField] private Button slotButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private GameObject selectionFrame;

    [Header("選択状態の色")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    [Header("「選択なし」設定")]
    [SerializeField] private Sprite noneSelectionIcon; // 「選択なし」用のアイコン
    [SerializeField] private string noneSelectionText = "選択なし";
    [SerializeField] private string noneSelectionDesc = "補助材料を使用しません";

    #endregion

    #region Private Fields

    private System.Action onClickCallback;
    private bool isCurrentlySelected;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        SetupButton();
        SetupAsNoneSelection();
    }

    private void OnDestroy()
    {
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// ボタンの設定
    /// </summary>
    private void SetupButton()
    {
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(() => onClickCallback?.Invoke());
        }
    }

    /// <summary>
    /// 「選択なし」として設定
    /// </summary>
    private void SetupAsNoneSelection()
    {
        // UI要素を設定
        SetUIText(nameText, noneSelectionText);
        SetUIText(descText, noneSelectionDesc);
        SetIcon(noneSelectionIcon);
        SetSelectionState(false);

        Debug.Log("[NoneSelectionSlotUI] 「選択なし」スロット設定完了");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// クリックコールバックを設定
    /// </summary>
    /// <param name="onClickCallback">クリック時のコールバック</param>
    public void SetClickCallback(System.Action onClickCallback)
    {
        this.onClickCallback = onClickCallback;
    }

    /// <summary>
    /// 選択状態を更新
    /// </summary>
    /// <param name="isSelected">選択状態</param>
    public void UpdateSelectionState(bool isSelected)
    {
        SetSelectionState(isSelected);
    }

    #endregion

    #region Private Methods - UI Update

    /// <summary>
    /// UIテキストを安全に設定
    /// </summary>
    /// <param name="textComponent">テキストコンポーネント</param>
    /// <param name="text">設定するテキスト</param>
    private void SetUIText(TextMeshProUGUI textComponent, string text)
    {
        if (textComponent != null)
        {
            textComponent.text = text ?? "";
        }
    }

    /// <summary>
    /// アイコンを設定
    /// </summary>
    /// <param name="iconSprite">アイコンのSprite</param>
    private void SetIcon(Sprite iconSprite)
    {
        if (iconImage != null)
        {
            if (iconSprite != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                // デフォルトアイコンまたは非表示
                iconImage.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 選択状態を設定
    /// </summary>
    /// <param name="isSelected">選択状態</param>
    private void SetSelectionState(bool isSelected)
    {
        isCurrentlySelected = isSelected;

        // 選択フレームの表示/非表示
        if (selectionFrame != null)
        {
            selectionFrame.SetActive(isSelected);
        }

        // 背景色の変更
        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }

        // ボタンの状態変更
        if (slotButton != null)
        {
            var colorBlock = slotButton.colors;
            colorBlock.normalColor = isSelected ? selectedColor : normalColor;
            slotButton.colors = colorBlock;
        }
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// 現在の選択状態
    /// </summary>
    public bool IsSelected => isCurrentlySelected;

    #endregion

    #region Inspector Context Menu

    /// <summary>
    /// 選択状態をテスト（Inspector用）
    /// </summary>
    [ContextMenu("Test Selection State")]
    public void TestSelectionState()
    {
        SetSelectionState(!isCurrentlySelected);
        Debug.Log($"[NoneSelectionSlotUI] 選択状態テスト: {isCurrentlySelected}");
    }

    #endregion
}