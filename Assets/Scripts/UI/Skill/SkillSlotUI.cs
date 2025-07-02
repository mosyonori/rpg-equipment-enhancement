using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// スキルスロットUI表示コンポーネント
/// </summary>
public class SkillSlotUI : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Button slotButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image rarityFrame;
    [SerializeField] private Image selectionFrame; // 追加: 選択フレーム
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText; // スキルレベル表示用（将来拡張用）
    [SerializeField] private GameObject lockMark;
    [SerializeField] private GameObject newMark;

    [Header("レアリティ色設定")]
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = Color.magenta;
    [SerializeField] private Color legendaryColor = Color.yellow;

    [Header("選択状態設定")]
    [SerializeField] private Color selectedFrameColor = Color.cyan;
    [SerializeField] private Color normalFrameColor = Color.clear;

    [Header("デフォルトアイコン")]
    [SerializeField] private Sprite defaultSkillIcon;
    [SerializeField] private Sprite emptySlotIcon;
    [SerializeField] private Sprite noSkillIcon; // 「装備無し」用アイコン

    [Header("属性アイコン")]
    [SerializeField] private Sprite fireAttributeIcon;
    [SerializeField] private Sprite waterAttributeIcon;
    [SerializeField] private Sprite windAttributeIcon;
    [SerializeField] private Sprite earthAttributeIcon;
    [SerializeField] private Sprite noneAttributeIcon;

    // イベント
    public System.Action<UserSkillData> OnSlotClicked;
    public System.Action<UserSkillData> OnSlotLongPressed;

    // データ
    private UserSkillData skillData;
    private SkillMasterData masterData;
    private bool isEmpty = true;

    #region Unity Lifecycle

    private void Awake()
    {
        // ボタンイベント設定
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClick);
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// スキルデータを設定して表示更新
    /// </summary>
    public void SetSkillData(UserSkillData skill)
    {
        skillData = skill;
        isEmpty = skill == null;

        if (skill == null)
        {
            SetEmpty();
            return;
        }

        // マスターデータ取得
        masterData = MasterDataManager.Instance?.GetSkillData(skill.skillMasterId);
        if (masterData == null)
        {
            SetEmpty();
            Debug.LogError($"スキルマスターデータが見つかりません: {skill.skillMasterId}");
            return;
        }

        UpdateDisplay();
    }

    /// <summary>
    /// 「装備無し」状態を設定
    /// </summary>
    public void SetNoSkill()
    {
        skillData = null;
        masterData = null;
        isEmpty = false; // 「装備無し」は空ではなく選択可能な状態

        // UI要素を「装備無し」用に設定
        if (iconImage != null) iconImage.sprite = noSkillIcon;
        if (nameText != null) nameText.text = "装備無し";
        if (levelText != null) levelText.text = "";
        if (lockMark != null) lockMark.SetActive(false);
        if (newMark != null) newMark.SetActive(false);
        if (backgroundImage != null) backgroundImage.color = Color.white;
        if (rarityFrame != null) rarityFrame.color = normalFrameColor;

        // 選択フレームを非表示
        SetSelected(false);

        // ボタンを有効化
        if (slotButton != null) slotButton.interactable = true;

        DebugLog("「装備無し」状態に設定");
    }

    /// <summary>
    /// 空のスロット表示
    /// </summary>
    public void SetEmpty()
    {
        skillData = null;
        masterData = null;
        isEmpty = true;

        // UI要素を非表示/初期化
        if (iconImage != null) iconImage.sprite = emptySlotIcon;
        if (nameText != null) nameText.text = "スキル未装備";
        if (levelText != null) levelText.text = "";
        if (lockMark != null) lockMark.SetActive(false);
        if (newMark != null) newMark.SetActive(false);
        if (backgroundImage != null) backgroundImage.color = Color.gray;
        if (rarityFrame != null) rarityFrame.color = commonColor;

        // 選択フレームを非表示
        SetSelected(false);

        // ボタンを有効化（クリックでスキル選択画面を開く）
        if (slotButton != null) slotButton.interactable = true;
    }

    /// <summary>
    /// スキルデータを取得
    /// </summary>
    public UserSkillData GetSkillData()
    {
        return skillData;
    }

    /// <summary>
    /// 空のスロットかどうか
    /// </summary>
    public bool IsEmpty()
    {
        return isEmpty;
    }

    /// <summary>
    /// 「装備無し」状態かどうか
    /// </summary>
    public bool IsNoSkill()
    {
        return !isEmpty && skillData == null;
    }

    /// <summary>
    /// 選択状態を設定
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (selectionFrame != null)
        {
            selectionFrame.gameObject.SetActive(selected);
            if (selected)
            {
                selectionFrame.color = selectedFrameColor;
                DebugLog($"スキル選択フレーム表示: {skillData?.userSkillId ?? "装備無し"}");
            }
        }

        // 背景色も変更（フォールバック）
        if (backgroundImage != null && !isEmpty)
        {
            backgroundImage.color = selected ? selectedFrameColor : Color.white;
        }
    }

    #endregion

    #region 内部メソッド

    private void UpdateDisplay()
    {
        // ボタンを有効化
        if (slotButton != null) slotButton.interactable = true;

        // アイコン設定
        UpdateIcon();

        // 名前設定
        if (nameText != null)
        {
            nameText.text = masterData.skillName;
        }

        // レベル表示（将来拡張用）
        if (levelText != null)
        {
            levelText.text = ""; // 現在はレベル機能なし
        }

        // レアリティフレーム
        if (rarityFrame != null)
        {
            rarityFrame.color = GetRarityColor(masterData.rarity);
        }

        // 状態マーク更新
        UpdateStatusMarks();

        // 背景色
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.white;
        }
    }

    private void UpdateIcon()
    {
        if (iconImage == null) return;

        Sprite iconToUse = null;

        // 1. マスターデータのアイコンを最優先で使用
        if (masterData.skillIcon != null)
        {
            iconToUse = masterData.skillIcon;
            DebugLog($"マスターデータのアイコンを使用: {masterData.skillName}");
        }
        // 2. アイコンがない場合、パスから読み込みを試行
        else if (!string.IsNullOrEmpty(masterData.skillIconPath))
        {
            iconToUse = LoadIconFromPath(masterData.skillIconPath);
            if (iconToUse != null)
            {
                DebugLog($"パスからアイコンを読み込み: {masterData.skillIconPath}");
            }
            else
            {
                DebugLogWarning($"パスからアイコンを読み込めませんでした: {masterData.skillIconPath}");
            }
        }

        // 3. 上記で取得できない場合のみ、フォールバック（属性別アイコン）を使用
        if (iconToUse == null)
        {
            iconToUse = GetFallbackIcon();
            DebugLogWarning($"フォールバックアイコンを使用: {masterData.skillName} (Attribute: {masterData.attributeType})");
        }

        iconImage.sprite = iconToUse;
        iconImage.gameObject.SetActive(iconToUse != null);
    }

    /// <summary>
    /// パスからアイコンを読み込み
    /// </summary>
    private Sprite LoadIconFromPath(string iconPath)
    {
        try
        {
            // パスの正規化（Assets/を除去してResourcesパスに変換）
            string resourcePath = iconPath;
            if (resourcePath.StartsWith("Assets/"))
            {
                resourcePath = resourcePath.Substring(7); // "Assets/"を除去
            }
            if (resourcePath.StartsWith("Resources/"))
            {
                resourcePath = resourcePath.Substring(10); // "Resources/"を除去
            }

            // 拡張子を除去
            if (resourcePath.Contains("."))
            {
                resourcePath = resourcePath.Substring(0, resourcePath.LastIndexOf('.'));
            }

            // Resourcesから読み込み
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                // Texture2Dとして読み込んでからSpriteを探す
                Texture2D texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    // テクスチャからスプライトを取得（Multiple Spriteの場合）
                    Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
                    if (sprites.Length > 0)
                    {
                        sprite = sprites[0]; // 最初のスプライトを使用
                    }
                }
            }

            return sprite;
        }
        catch (System.Exception e)
        {
            DebugLogError($"アイコン読み込みエラー: {iconPath} - {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// フォールバック用の属性別アイコンを取得
    /// </summary>
    private Sprite GetFallbackIcon()
    {
        return masterData.attributeType switch
        {
            AttributeType.Fire => fireAttributeIcon,
            AttributeType.Water => waterAttributeIcon,
            AttributeType.Wind => windAttributeIcon,
            AttributeType.Earth => earthAttributeIcon,
            AttributeType.None => noneAttributeIcon,
            _ => defaultSkillIcon
        };
    }

    private void UpdateStatusMarks()
    {
        // ロックマーク
        if (lockMark != null)
        {
            lockMark.SetActive(skillData != null && skillData.isLocked);
        }

        // 新規マーク（取得から24時間以内）
        if (newMark != null)
        {
            bool isNew = skillData != null && skillData.isNew;
            newMark.SetActive(isNew);
        }
    }

    private Color GetRarityColor(RarityType rarity)
    {
        return rarity switch
        {
            RarityType.Common => commonColor,
            RarityType.Rare => rareColor,
            RarityType.Epic => epicColor,
            RarityType.Legendary => legendaryColor,
            _ => commonColor
        };
    }

    private void OnSlotClick()
    {
        // 空スロット、装備無し、スキル装備済みのいずれでもクリック可能
        // スキル選択画面を開くためのイベントを発火
        OnSlotClicked?.Invoke(skillData);
    }

    #endregion

    #region デバッグ

    private void DebugLog(string message)
    {
        Debug.Log($"[SkillSlotUI] {message}");
    }

    private void DebugLogWarning(string message)
    {
        Debug.LogWarning($"[SkillSlotUI] {message}");
    }

    private void DebugLogError(string message)
    {
        Debug.LogError($"[SkillSlotUI] {message}");
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("テストデータ設定")]
    private void SetTestData()
    {
        // テスト用のダミーデータを設定
        var testSkill = new UserSkillData
        {
            skillMasterId = 1,
            isLocked = false,
            isNew = true
        };

        SetSkillData(testSkill);
    }

    [ContextMenu("装備無し設定")]
    private void SetNoSkillTest()
    {
        SetNoSkill();
    }

    [ContextMenu("空スロット設定")]
    private void SetEmptySlot()
    {
        SetEmpty();
    }
#endif

    #endregion
}