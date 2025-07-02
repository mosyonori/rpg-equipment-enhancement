using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 装備スロットUI表示コンポーネント
/// 装備スロットとバトルスキルスロット両方に対応
/// </summary>
public class EquipmentSlotUI : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Button slotButton;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image rarityFrame;
    [SerializeField] private Image selectionFrame; // 追加: 選択フレーム
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI enhancementText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private GameObject equippedMark;
    [SerializeField] private GameObject lockMark;
    [SerializeField] private GameObject favoriteMark;
    [SerializeField] private GameObject newMark;

    [Header("レアリティ色設定")]
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = Color.magenta;
    [SerializeField] private Color legendaryColor = Color.yellow;

    [Header("選択状態設定")]
    [SerializeField] private Color selectedFrameColor = Color.cyan;
    [SerializeField] private Color normalFrameColor = Color.clear;

    [Header("装備タイプアイコン")]
    [SerializeField] private Sprite weaponTypeIcon;
    [SerializeField] private Sprite armorTypeIcon;
    [SerializeField] private Sprite accessoryTypeIcon;

    [Header("スロットタイプ設定")]
    [SerializeField] private SlotType slotType = SlotType.Equipment;
    [SerializeField] private int battleSkillSlotNumber = 1; // バトルスキル用スロット番号（1 or 2）

    [Header("バトルスキル用デフォルトアイコン")]
    [SerializeField] private Sprite defaultBattleSkillIcon;
    [SerializeField] private Sprite emptySkillSlotIcon;

    // イベント
    public System.Action<UserEquipmentData> OnSlotClicked;
    public System.Action<UserEquipmentData> OnSlotLongPressed;

    // 装備データ
    private UserEquipmentData equipmentData;
    private EquipmentMasterData masterData;

    // バトルスキル用データ
    private UserSkillData skillData;
    private SkillMasterData skillMasterData;

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
    /// 装備データを設定して表示更新（装備スロット用）
    /// </summary>
    public void SetEquipmentData(UserEquipmentData equipment)
    {
        if (slotType != SlotType.Equipment)
        {
            DebugLogError("装備データの設定は装備スロット専用です");
            return;
        }

        equipmentData = equipment;
        skillData = null;
        skillMasterData = null;

        if (equipment == null)
        {
            SetEmpty();
            return;
        }

        // マスターデータ取得
        masterData = MasterDataManager.Instance?.GetEquipmentData(equipment.equipmentMasterId);
        if (masterData == null)
        {
            SetEmpty();
            Debug.LogError($"装備マスターデータが見つかりません: {equipment.equipmentMasterId}");
            return;
        }

        UpdateDisplay();
    }

    /// <summary>
    /// バトルスキルデータを設定して表示更新（バトルスキルスロット用）
    /// </summary>
    public void SetBattleSkillData(UserSkillData skill)
    {
        if (slotType != SlotType.BattleSkill)
        {
            DebugLogError("スキルデータの設定はバトルスキルスロット専用です");
            return;
        }

        skillData = skill;
        equipmentData = null;
        masterData = null;

        if (skill == null)
        {
            SetEmpty();
            return;
        }

        // スキルマスターデータ取得
        skillMasterData = MasterDataManager.Instance?.GetSkillData(skill.skillMasterId);
        if (skillMasterData == null)
        {
            SetEmpty();
            Debug.LogError($"スキルマスターデータが見つかりません: {skill.skillMasterId}");
            return;
        }

        UpdateDisplay();
    }

    /// <summary>
    /// 空のスロット表示
    /// </summary>
    public void SetEmpty()
    {
        equipmentData = null;
        masterData = null;
        skillData = null;
        skillMasterData = null;

        // UI要素を非表示/初期化
        if (slotType == SlotType.BattleSkill)
        {
            // バトルスキルスロット用の空表示
            if (iconImage != null) iconImage.sprite = emptySkillSlotIcon ?? defaultBattleSkillIcon;
            if (nameText != null) nameText.text = $"スキルスロット{battleSkillSlotNumber}";
            if (enhancementText != null) enhancementText.text = "";
            if (powerText != null) powerText.text = "";
        }
        else
        {
            // 装備スロット用の空表示
            if (iconImage != null) iconImage.sprite = null;
            if (nameText != null) nameText.text = "";
            if (enhancementText != null) enhancementText.text = "";
            if (powerText != null) powerText.text = "";
        }

        if (equippedMark != null) equippedMark.SetActive(false);
        if (lockMark != null) lockMark.SetActive(false);
        if (favoriteMark != null) favoriteMark.SetActive(false);
        if (newMark != null) newMark.SetActive(false);
        if (backgroundImage != null) backgroundImage.color = Color.gray;
        if (rarityFrame != null) rarityFrame.color = commonColor;

        // 選択フレームを非表示
        SetSelected(false);

        // ボタンを有効化（クリックで選択画面を開く）
        if (slotButton != null) slotButton.interactable = true;
    }

    /// <summary>
    /// 装備データを取得
    /// </summary>
    public UserEquipmentData GetEquipmentData()
    {
        return equipmentData;
    }

    /// <summary>
    /// バトルスキルデータを取得
    /// </summary>
    public UserSkillData GetSkillData()
    {
        return skillData;
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
                if (slotType == SlotType.BattleSkill)
                {
                    DebugLog($"スキル選択フレーム表示: スロット{battleSkillSlotNumber}");
                }
                else
                {
                    DebugLog($"装備選択フレーム表示: {equipmentData?.userEquipmentId}");
                }
            }
        }

        // 背景色も変更（フォールバック）
        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? selectedFrameColor : Color.white;
        }
    }

    /// <summary>
    /// バトルスキルスロット番号を設定
    /// </summary>
    public void SetBattleSkillSlotNumber(int slotNumber)
    {
        battleSkillSlotNumber = slotNumber;
    }

    /// <summary>
    /// スロットタイプを設定
    /// </summary>
    public void SetSlotType(SlotType type)
    {
        slotType = type;
    }

    /// <summary>
    /// スロットタイプを取得
    /// </summary>
    public SlotType GetSlotType()
    {
        return slotType;
    }

    /// <summary>
    /// バトルスキルスロット番号を取得
    /// </summary>
    public int GetBattleSkillSlotNumber()
    {
        return battleSkillSlotNumber;
    }

    #endregion

    #region 内部メソッド

    private void UpdateDisplay()
    {
        // ボタンを有効化
        if (slotButton != null) slotButton.interactable = true;

        if (slotType == SlotType.BattleSkill)
        {
            // バトルスキル表示
            UpdateBattleSkillDisplay();
        }
        else
        {
            // 装備表示
            UpdateEquipmentDisplay();
        }
    }

    /// <summary>
    /// バトルスキル表示を更新
    /// </summary>
    private void UpdateBattleSkillDisplay()
    {
        // アイコン設定
        UpdateSkillIcon();

        // 名前設定
        if (nameText != null)
        {
            nameText.text = skillMasterData.skillName;
        }

        // レベル表示（将来拡張用）
        if (enhancementText != null)
        {
            enhancementText.gameObject.SetActive(false); // バトルスキルには強化値なし
        }

        // パワー表示（スキルの場合はダメージ倍率など）
        if (powerText != null)
        {
            powerText.text = $"{skillMasterData.skillDamageMultiplier:F1}倍";
        }

        // レアリティフレーム
        if (rarityFrame != null)
        {
            rarityFrame.color = GetRarityColor(skillMasterData.rarity);
        }

        // ステータスマーク更新
        UpdateSkillStatusMarks();

        // 背景色
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.white;
        }
    }

    /// <summary>
    /// 装備表示を更新
    /// </summary>
    private void UpdateEquipmentDisplay()
    {
        // アイコン設定
        UpdateIcon();

        // 名前設定
        if (nameText != null)
        {
            nameText.text = masterData.equipmentName;
        }

        // 強化値表示
        if (enhancementText != null)
        {
            if (equipmentData.currentEnhancedValue > 0)
            {
                enhancementText.text = $"+{equipmentData.currentEnhancedValue}";
                enhancementText.gameObject.SetActive(true);
            }
            else
            {
                enhancementText.gameObject.SetActive(false);
            }
        }

        // 戦闘力表示
        if (powerText != null)
        {
            var totalStats = equipmentData.CalculateTotalStats(masterData);
            int power = CalculateSimplePower(totalStats);
            powerText.text = power.ToString();
        }

        // レアリティフレーム
        if (rarityFrame != null)
        {
            rarityFrame.color = GetRarityColor(masterData.rarity);
        }

        // ステータスマーク更新
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
        if (masterData.equipmentIcon != null)
        {
            iconToUse = masterData.equipmentIcon;
            DebugLog($"マスターデータのアイコンを使用: {masterData.equipmentName}");
        }
        // 2. アイコンがない場合、パスから読み込みを試行
        else if (!string.IsNullOrEmpty(masterData.equipmentIconPath))
        {
            iconToUse = LoadIconFromPath(masterData.equipmentIconPath);
            if (iconToUse != null)
            {
                DebugLog($"パスからアイコンを読み込み: {masterData.equipmentIconPath}");
            }
            else
            {
                DebugLogWarning($"パスからアイコンを読み込めませんでした: {masterData.equipmentIconPath}");
            }
        }

        // 3. 上記で取得できない場合のフォールバック（装備タイプ別デフォルトアイコン）
        if (iconToUse == null)
        {
            iconToUse = GetFallbackIcon();
            DebugLogWarning($"フォールバックアイコンを使用: {masterData.equipmentName} (Type: {masterData.equipmentType})");
        }

        iconImage.sprite = iconToUse;
        iconImage.gameObject.SetActive(iconToUse != null);
    }

    /// <summary>
    /// スキルアイコンを更新
    /// </summary>
    private void UpdateSkillIcon()
    {
        if (iconImage == null) return;

        Sprite iconToUse = null;

        // 1. マスターデータのアイコンを最優先で使用
        if (skillMasterData.skillIcon != null)
        {
            iconToUse = skillMasterData.skillIcon;
            DebugLog($"マスターデータのアイコンを使用: {skillMasterData.skillName}");
        }
        // 2. アイコンがない場合、パスから読み込みを試行
        else if (!string.IsNullOrEmpty(skillMasterData.skillIconPath))
        {
            iconToUse = LoadIconFromPath(skillMasterData.skillIconPath);
            if (iconToUse != null)
            {
                DebugLog($"パスからアイコンを読み込み: {skillMasterData.skillIconPath}");
            }
            else
            {
                DebugLogWarning($"パスからアイコンを読み込めませんでした: {skillMasterData.skillIconPath}");
            }
        }

        // 3. 上記で取得できない場合のフォールバック
        if (iconToUse == null)
        {
            iconToUse = defaultBattleSkillIcon;
            DebugLogWarning($"フォールバックアイコンを使用: {skillMasterData.skillName}");
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
    /// フォールバック用のタイプ別アイコンを取得
    /// </summary>
    private Sprite GetFallbackIcon()
    {
        return masterData.equipmentType switch
        {
            EquipmentType.Weapon => weaponTypeIcon,
            EquipmentType.Armor => armorTypeIcon,
            EquipmentType.Accessory => accessoryTypeIcon,
            _ => null
        };
    }

    private void UpdateStatusMarks()
    {
        // 装備中マーク
        if (equippedMark != null)
        {
            equippedMark.SetActive(equipmentData.isEquipped);
        }

        // ロックマーク
        if (lockMark != null)
        {
            lockMark.SetActive(equipmentData.isLocked);
        }

        // お気に入りマーク
        if (favoriteMark != null)
        {
            favoriteMark.SetActive(equipmentData.isFavorite);
        }

        // 新規マーク（取得から24時間以内）
        if (newMark != null)
        {
            bool isNew = (System.DateTime.Now - equipmentData.acquiredDate).TotalHours < 24;
            newMark.SetActive(isNew);
        }
    }

    /// <summary>
    /// スキルのステータスマークを更新
    /// </summary>
    private void UpdateSkillStatusMarks()
    {
        // 装備中マーク（バトルスキルの場合は表示しない）
        if (equippedMark != null)
        {
            equippedMark.SetActive(false);
        }

        // ロックマーク
        if (lockMark != null)
        {
            lockMark.SetActive(skillData != null && skillData.isLocked);
        }

        // お気に入りマーク（スキルにはない）
        if (favoriteMark != null)
        {
            favoriteMark.SetActive(false);
        }

        // 新規マーク
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

    private int CalculateSimplePower(EquipmentTotalStats stats)
    {
        // 簡易戦闘力計算
        int power = 0;
        power += stats.hp / 10;
        power += stats.offense * 2;
        power += stats.defense;
        power += stats.speed;
        power += stats.criticalRate / 5;
        power += stats.criticalDamageRate / 10;
        power += stats.fireOffence;
        power += stats.waterOffence;
        power += stats.windOffence;
        power += stats.earthOffence;
        return power;
    }

    private void OnSlotClick()
    {
        if (slotType == SlotType.BattleSkill)
        {
            // バトルスキルスロットの場合：スキル選択イベントを発行
            OnSlotClicked?.Invoke(null); // UserEquipmentDataとしてはnullを渡す
        }
        else
        {
            // 装備スロットの場合：既存処理
            if (equipmentData != null)
            {
                OnSlotClicked?.Invoke(equipmentData);
            }
        }
    }

    #endregion

    #region デバッグ

    /// <summary>
    /// デバッグログ出力
    /// </summary>
    private void DebugLog(string message)
    {
        Debug.Log($"[EquipmentSlotUI] {message}");
    }

    /// <summary>
    /// デバッグ警告ログ出力
    /// </summary>
    private void DebugLogWarning(string message)
    {
        Debug.LogWarning($"[EquipmentSlotUI] {message}");
    }

    /// <summary>
    /// デバッグエラーログ出力
    /// </summary>
    private void DebugLogError(string message)
    {
        Debug.LogError($"[EquipmentSlotUI] {message}");
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("バトルスキルスロットに設定")]
    private void SetToBattleSkillSlot()
    {
        slotType = SlotType.BattleSkill;
        battleSkillSlotNumber = 1;
        SetEmpty();
    }

    [ContextMenu("装備スロットに設定")]
    private void SetToEquipmentSlot()
    {
        slotType = SlotType.Equipment;
        SetEmpty();
    }

    [ContextMenu("テスト装備データ設定")]
    private void SetTestEquipmentData()
    {
        if (slotType != SlotType.Equipment)
        {
            Debug.LogWarning("装備スロットに設定してからテストしてください");
            return;
        }

        // テスト用のダミーデータを設定
        var testEquipment = new UserEquipmentData
        {
            equipmentMasterId = 1,
            currentEnhancedValue = 5,
            currentEnhanceStamina = 95,
            currentAttributeType = AttributeType.Fire,
            isEquipped = true,
            isLocked = false,
            isFavorite = true
        };

        SetEquipmentData(testEquipment);
    }

    [ContextMenu("テストスキルデータ設定")]
    private void SetTestSkillData()
    {
        if (slotType != SlotType.BattleSkill)
        {
            Debug.LogWarning("バトルスキルスロットに設定してからテストしてください");
            return;
        }

        // テスト用のダミーデータを設定
        var testSkill = new UserSkillData
        {
            skillMasterId = 1,
            isLocked = false,
            isNew = true
        };

        SetBattleSkillData(testSkill);
    }

    [ContextMenu("空スロット設定")]
    private void SetEmptySlot()
    {
        SetEmpty();
    }
#endif

    #endregion
}

/// <summary>
/// スロットタイプ定義
/// </summary>
public enum SlotType
{
    Equipment,    // 装備スロット
    BattleSkill   // バトルスキルスロット
}