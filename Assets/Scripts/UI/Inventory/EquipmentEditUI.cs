using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 装備編集画面のメインUI
/// 修正版：オブジェクトライフサイクル管理とイベント処理の安全化
/// </summary>
public class EquipmentEditUI : MonoBehaviour
{
    [Header("装備スロットボタン")]
    [SerializeField] private Button weaponSlotButton;
    [SerializeField] private Button armorSlotButton;
    [SerializeField] private Button accessorySlotButton;
    [SerializeField] private Button futureSlot1Button;
    [SerializeField] private Button futureSlot2Button;

    [Header("装備スロット表示")]
    [SerializeField] private Image weaponSlotIcon;
    [SerializeField] private Image armorSlotIcon;
    [SerializeField] private Image accessorySlotIcon;
    [SerializeField] private TextMeshProUGUI weaponSlotText;
    [SerializeField] private TextMeshProUGUI armorSlotText;
    [SerializeField] private TextMeshProUGUI accessorySlotText;

    [Header("ナビゲーションボタン")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button enhanceButton;

    [Header("基本ステータス表示")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private TextMeshProUGUI totalPowerText;
    [SerializeField] private TextMeshProUGUI equipmentCountText;

    [Header("詳細ステータス表示")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI offenseText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI criticalRateText;
    [SerializeField] private TextMeshProUGUI criticalDamageText;
    [SerializeField] private TextMeshProUGUI fireOffenceText;
    [SerializeField] private TextMeshProUGUI waterOffenceText;
    [SerializeField] private TextMeshProUGUI windOffenceText;
    [SerializeField] private TextMeshProUGUI earthOffenceText;

    [Header("装備選択ポップアップ")]
    [SerializeField] private EquipmentSelectionPopup equipmentSelectionPopup;

    [Header("インベントリパネル")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button inventoryCloseButton;

    [Header("デフォルトアイコン")]
    [SerializeField] private Sprite defaultWeaponIcon;
    [SerializeField] private Sprite defaultArmorIcon;
    [SerializeField] private Sprite defaultAccessoryIcon;

    [Header("スキルスロット")]
    [SerializeField] private Button skillSlot1Button;
    [SerializeField] private Button skillSlot2Button;
    [SerializeField] private Image skillSlot1Icon;
    [SerializeField] private Image skillSlot2Icon;
    [SerializeField] private TextMeshProUGUI skillSlot1Text;
    [SerializeField] private TextMeshProUGUI skillSlot2Text;

    [Header("スキル選択ポップアップ")]
    [SerializeField] private SkillSelectionPopup skillSelectionPopup;

    [Header("デフォルトスキルアイコン")]
    [SerializeField] private Sprite defaultSkillIcon;

    [Header("デバッグ")]
    [SerializeField] private bool enableDebugLog = true;

    // イベント
    public System.Action OnBackButtonClicked;
    public System.Action OnInventoryButtonClicked;
    public System.Action OnEnhanceButtonClicked;

    // 前回に開いた装備タイプを記録
    private EquipmentType lastOpenedEquipmentType;

    // === 安全性チェック用フラグ追加 ===
    private bool isDestroying = false;
    private bool isEquipmentProcessing = false;

    #region Unity Lifecycle

    private void Awake()
    {
        SetupButtons();
        SetupPopupEvents();
        SetupInventoryPanel();
    }

    private void Start()
    {
        SubscribeToEvents();
        RefreshDisplay();
    }

    private void OnDestroy()
    {
        // === 破棄フラグを設定 ===
        isDestroying = true;

        // イベント購読解除
        UnsubscribeFromEvents();

        DebugLog("EquipmentEditUI が破棄されました");
    }

    #endregion

    #region 初期化

    private void SetupButtons()
    {
        // 装備スロットボタン
        if (weaponSlotButton != null)
            weaponSlotButton.onClick.AddListener(() => OpenEquipmentSelection(EquipmentType.Weapon));
        if (armorSlotButton != null)
            armorSlotButton.onClick.AddListener(() => OpenEquipmentSelection(EquipmentType.Armor));
        if (accessorySlotButton != null)
            accessorySlotButton.onClick.AddListener(() => OpenEquipmentSelection(EquipmentType.Accessory));

        // スキルスロットボタン
        if (skillSlot1Button != null)
        {
            skillSlot1Button.onClick.AddListener(() => OpenSkillSelection(1));
            skillSlot1Button.interactable = true;
        }
        if (skillSlot2Button != null)
        {
            skillSlot2Button.onClick.AddListener(() => OpenSkillSelection(2));
            skillSlot2Button.interactable = true;
        }

        // ナビゲーションボタン
        if (backButton != null)
            backButton.onClick.AddListener(() => OnBackButtonClicked?.Invoke());
        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(ShowInventoryPanel);
        if (enhanceButton != null)
            enhanceButton.onClick.AddListener(() => OnEnhanceButtonClicked?.Invoke());
    }

    private void SetupPopupEvents()
    {
        // === 装備選択ポップアップイベント設定（安全化） ===
        if (equipmentSelectionPopup != null)
        {
            equipmentSelectionPopup.OnEquipmentSelected += OnEquipmentSelectedSafe;
            equipmentSelectionPopup.OnEquipmentRemoved += OnEquipmentRemovedSafe;
            equipmentSelectionPopup.OnPopupClosed += OnPopupClosedSafe;
            DebugLog("装備選択ポップアップイベントを安全に設定しました");
        }

        // === スキル選択ポップアップイベント設定（安全化） ===
        if (skillSelectionPopup != null)
        {
            skillSelectionPopup.OnSkillSelected += OnSkillSelectedSafe;
            skillSelectionPopup.OnSkillRemoved += OnSkillRemovedSafe;
            skillSelectionPopup.OnPopupClosed += OnPopupClosedSafe;
            DebugLog("スキル選択ポップアップイベントを安全に設定しました");
        }
    }

    private void SetupInventoryPanel()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }

        if (inventoryCloseButton != null)
        {
            inventoryCloseButton.onClick.AddListener(HideInventoryPanel);
        }
    }

    private void SubscribeToEvents()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnEquipmentEquipped += OnEquipmentEquippedSafe;
            InventoryManager.OnEquipmentUnequipped += OnEquipmentUnequippedSafe;
            InventoryManager.OnInventoryChanged += OnInventoryChangedSafe;
        }

        if (SkillManager.Instance != null)
        {
            SkillManager.OnSkillInventoryChanged += OnSkillInventoryChangedSafe;
        }
    }

    private void UnsubscribeFromEvents()
    {
        // InventoryManagerイベント解除
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnEquipmentEquipped -= OnEquipmentEquippedSafe;
            InventoryManager.OnEquipmentUnequipped -= OnEquipmentUnequippedSafe;
            InventoryManager.OnInventoryChanged -= OnInventoryChangedSafe;
        }

        // SkillManagerイベント解除
        if (SkillManager.Instance != null)
        {
            SkillManager.OnSkillInventoryChanged -= OnSkillInventoryChangedSafe;
        }

        // ポップアップイベント解除
        if (equipmentSelectionPopup != null)
        {
            equipmentSelectionPopup.OnEquipmentSelected -= OnEquipmentSelectedSafe;
            equipmentSelectionPopup.OnEquipmentRemoved -= OnEquipmentRemovedSafe;
            equipmentSelectionPopup.OnPopupClosed -= OnPopupClosedSafe;
        }

        if (skillSelectionPopup != null)
        {
            skillSelectionPopup.OnSkillSelected -= OnSkillSelectedSafe;
            skillSelectionPopup.OnSkillRemoved -= OnSkillRemovedSafe;
            skillSelectionPopup.OnPopupClosed -= OnPopupClosedSafe;
        }
    }

    #endregion

    #region 装備選択処理

    private void OpenEquipmentSelection(EquipmentType equipmentType)
    {
        if (isDestroying) return;

        DebugLog($"装備選択を開始: {equipmentType}");

        if (equipmentSelectionPopup == null)
        {
            DebugLogError("装備選択ポップアップが設定されていません");
            return;
        }

        // 他のポップアップを確実に閉じる
        CloseAllOtherPopups();

        lastOpenedEquipmentType = equipmentType;
        equipmentSelectionPopup.ShowEquipmentSelection(equipmentType);

        DebugLog($"装備選択を開始: {equipmentType}");
    }

    private void CloseAllOtherPopups()
    {
        if (skillSelectionPopup != null)
        {
            skillSelectionPopup.HidePopup();
        }
    }

    #endregion

    #region スキル選択処理

    private void OpenSkillSelection(int slotNumber)
    {
        if (isDestroying) return;

        if (skillSelectionPopup == null)
        {
            DebugLogError("スキル選択ポップアップが設定されていません");
            return;
        }

        // 装備選択ポップアップを確実に閉じる
        if (equipmentSelectionPopup != null)
        {
            equipmentSelectionPopup.HidePopup();
        }

        string slotId = $"battle_skill_slot_{slotNumber}";
        skillSelectionPopup.ShowSkillSelection(slotId);
        DebugLog($"スキル選択を開始: スロット{slotNumber}");
    }

    #endregion

    #region 安全なイベントハンドラー

    // === 装備選択イベント（安全版） ===
    private void OnEquipmentSelectedSafe(UserEquipmentData equipment)
    {
        if (isDestroying || equipment == null) return;

        DebugLog($"装備選択イベント（安全版）: {equipment.userEquipmentId}");

        // 装備処理中フラグを設定
        if (isEquipmentProcessing)
        {
            DebugLogError("既に装備処理中です。重複実行を回避します。");
            return;
        }

        StartCoroutine(ProcessEquipmentSelectionSafe(equipment));
    }

    private System.Collections.IEnumerator ProcessEquipmentSelectionSafe(UserEquipmentData equipment)
    {
        isEquipmentProcessing = true;

        try
        {
            DebugLog($"装備処理開始: {equipment.userEquipmentId}");

            // オブジェクトが有効かチェック
            if (isDestroying || this == null)
            {
                DebugLogError("オブジェクトが破棄されているため装備処理を中止");
                yield break;
            }

            // InventoryManagerの状態チェック
            if (!IsManagersReady())
            {
                DebugLogError("マネージャーが準備できていません");
                yield break;
            }

            // 装備実行
            bool success = false;
            try
            {
                success = InventoryManager.Instance.EquipItem(equipment.userEquipmentId);
                DebugLog($"装備実行結果: {success}");
            }
            catch (System.Exception e)
            {
                DebugLogError($"装備実行中にエラー: {e.Message}");
                yield break;
            }

            // 1フレーム待機してから表示更新
            yield return new WaitForEndOfFrame();

            // 再度オブジェクトが有効かチェック
            if (isDestroying || this == null)
            {
                DebugLogError("装備処理中にオブジェクトが破棄されました");
                yield break;
            }

            if (success)
            {
                DebugLog("装備が成功しました");
                RefreshDisplay();
            }
            else
            {
                DebugLogError("装備に失敗しました");
            }
        }
        finally
        {
            // 処理完了フラグをリセット
            isEquipmentProcessing = false;
        }
    }

    private void OnEquipmentRemovedSafe()
    {
        if (isDestroying) return;

        DebugLog("装備解除イベント（安全版）");

        EquipmentType targetType = GetCurrentPopupEquipmentType();
        bool success = InventoryManager.Instance?.UnequipItemByType(targetType) ?? false;

        if (success)
        {
            DebugLog($"装備を外しました: {targetType}");
        }
    }

    // === スキル選択イベント（安全版） ===
    private void OnSkillSelectedSafe(UserSkillData skill)
    {
        if (isDestroying || skill == null) return;

        string currentSlotId = skillSelectionPopup?.GetCurrentEquipmentId();
        int slotNumber = GetSlotNumberFromId(currentSlotId);

        if (slotNumber == 0)
        {
            DebugLogError($"無効なスロットID: {currentSlotId}");
            return;
        }

        var saveData = SaveDataManager.Instance?.CurrentSaveData;
        if (saveData != null)
        {
            bool success = saveData.SetBattleSkill(slotNumber, skill.userSkillId);
            if (success)
            {
                SaveDataManager.Instance.MarkDataDirty();
                DebugLog($"スキルを装備しました: スロット{slotNumber} -> {skill.userSkillId}");
                RefreshDisplay();
            }
        }
    }

    private void OnSkillRemovedSafe()
    {
        if (isDestroying) return;

        string currentSlotId = skillSelectionPopup?.GetCurrentEquipmentId();
        int slotNumber = GetSlotNumberFromId(currentSlotId);

        if (slotNumber == 0) return;

        var saveData = SaveDataManager.Instance?.CurrentSaveData;
        if (saveData != null)
        {
            saveData.ClearBattleSkill(slotNumber);
            SaveDataManager.Instance.MarkDataDirty();
            DebugLog($"スキルを解除しました: スロット{slotNumber}");
            RefreshDisplay();
        }
    }

    // === 共通イベント（安全版） ===
    private void OnPopupClosedSafe()
    {
        if (isDestroying) return;
        DebugLog("ポップアップが閉じられました（安全版）");
    }

    private void OnEquipmentEquippedSafe(UserEquipmentData equipment)
    {
        if (isDestroying) return;
        DebugLog($"装備装着イベント（安全版）: {equipment.userEquipmentId}");
        StartCoroutine(DelayedRefreshSafe());
    }

    private void OnEquipmentUnequippedSafe(UserEquipmentData equipment)
    {
        if (isDestroying) return;
        DebugLog($"装備解除イベント（安全版）: {equipment.userEquipmentId}");
        StartCoroutine(DelayedRefreshSafe());
    }

    private void OnInventoryChangedSafe()
    {
        if (isDestroying) return;
        StartCoroutine(DelayedRefreshSafe());
    }

    private void OnSkillInventoryChangedSafe()
    {
        if (isDestroying) return;
        UpdateSkillSlots();
    }

    private System.Collections.IEnumerator DelayedRefreshSafe()
    {
        yield return new WaitForEndOfFrame();

        if (!isDestroying && this != null)
        {
            RefreshDisplay();
        }
    }

    #endregion

    #region 表示更新

    public void RefreshDisplay()
    {
        if (isDestroying || !IsManagersReady()) return;

        UpdatePlayerInfo();
        UpdateEquipmentSlots();
        UpdateSkillSlots();
        UpdateDetailedStatus();

        DebugLog("装備編集画面の表示を更新しました");
    }

    private void UpdatePlayerInfo()
    {
        if (isDestroying) return;

        var saveData = SaveDataManager.Instance?.CurrentSaveData;
        if (saveData == null) return;

        if (playerNameText != null) playerNameText.text = saveData.playerName;
        if (playerLevelText != null) playerLevelText.text = $"Lv.{saveData.playerLevel}";

        if (totalPowerText != null)
        {
            int totalPower = InventoryManager.Instance?.CalculateTotalPower() ?? 0;
            totalPowerText.text = totalPower.ToString();
        }

        if (equipmentCountText != null)
        {
            int currentCount = saveData.equipments.Count;
            int maxCount = 1000;
            equipmentCountText.text = $"{currentCount}/{maxCount}";
        }
    }

    private void UpdateDetailedStatus()
    {
        if (isDestroying) return;

        // キャラクターの基本ステータスを取得
        var characterStats = GetCharacterBaseStatus();

        // 装備中アイテムのステータス合計を取得
        var equipmentStats = GetEquippedItemsStatus();

        // 合計ステータスを計算
        var totalStats = CalculateTotalStatus(characterStats, equipmentStats);

        // UI表示を更新
        UpdateStatusDisplay(totalStats);

        DebugLog($"詳細ステータス更新完了 - HP:{totalStats.hp}, 攻撃:{totalStats.offense}, 防御:{totalStats.defense}");
    }

    private CharacterStatus GetCharacterBaseStatus()
    {
        var characterData = MasterDataManager.Instance?.GetCharacterData(1);

        if (characterData == null)
        {
            DebugLogError("キャラクターデータが見つかりません（ID:1）");
            return new CharacterStatus();
        }

        return new CharacterStatus
        {
            hp = characterData.Hp,
            offense = characterData.Offense,
            defense = characterData.Defense,
            speed = characterData.Speed,
            criticalRate = characterData.CriticalRate,
            criticalDamageRate = characterData.CriticalDamageRate,
            fireOffence = characterData.FireOffence,
            waterOffence = characterData.WaterOffence,
            windOffence = characterData.WindOffence,
            earthOffence = characterData.EarthOffence
        };
    }

    private CharacterStatus GetEquippedItemsStatus()
    {
        var totalStats = new CharacterStatus();
        var equippedItems = InventoryManager.Instance?.GetEquippedItems();

        if (equippedItems == null || equippedItems.Count == 0)
        {
            return totalStats;
        }

        foreach (var equippedItem in equippedItems)
        {
            var masterData = MasterDataManager.Instance?.GetEquipmentData(equippedItem.equipmentMasterId);
            if (masterData == null) continue;

            // 基本ステータス
            totalStats.hp += masterData.hp;
            totalStats.offense += masterData.offense;
            totalStats.defense += masterData.defense;
            totalStats.speed += masterData.speed;
            totalStats.criticalRate += masterData.criticalRate;
            totalStats.criticalDamageRate += masterData.criticalDamageRate;
            totalStats.fireOffence += masterData.fireOffence;
            totalStats.waterOffence += masterData.waterOffence;
            totalStats.windOffence += masterData.windOffence;
            totalStats.earthOffence += masterData.earthOffence;

            // 強化による追加ステータス
            totalStats.hp += equippedItem.enhancedHp;
            totalStats.offense += equippedItem.enhancedOffense;
            totalStats.defense += equippedItem.enhancedDefense;
            totalStats.speed += equippedItem.enhancedSpeed;
            totalStats.criticalRate += equippedItem.enhancedCriticalRate;
            totalStats.criticalDamageRate += equippedItem.enhancedCriticalDamageRate;
            totalStats.fireOffence += equippedItem.enhancedFireOffence;
            totalStats.waterOffence += equippedItem.enhancedWaterOffence;
            totalStats.windOffence += equippedItem.enhancedWindOffence;
            totalStats.earthOffence += equippedItem.enhancedEarthOffence;
        }

        return totalStats;
    }

    private CharacterStatus CalculateTotalStatus(CharacterStatus characterStats, CharacterStatus equipmentStats)
    {
        return new CharacterStatus
        {
            hp = characterStats.hp + equipmentStats.hp,
            offense = characterStats.offense + equipmentStats.offense,
            defense = characterStats.defense + equipmentStats.defense,
            speed = characterStats.speed + equipmentStats.speed,
            criticalRate = characterStats.criticalRate + equipmentStats.criticalRate,
            criticalDamageRate = characterStats.criticalDamageRate + equipmentStats.criticalDamageRate,
            fireOffence = characterStats.fireOffence + equipmentStats.fireOffence,
            waterOffence = characterStats.waterOffence + equipmentStats.waterOffence,
            windOffence = characterStats.windOffence + equipmentStats.windOffence,
            earthOffence = characterStats.earthOffence + equipmentStats.earthOffence
        };
    }

    private void UpdateStatusDisplay(CharacterStatus stats)
    {
        if (hpText != null) hpText.text = stats.hp.ToString();
        if (offenseText != null) offenseText.text = stats.offense.ToString();
        if (defenseText != null) defenseText.text = stats.defense.ToString();
        if (speedText != null) speedText.text = stats.speed.ToString();
        if (criticalRateText != null) criticalRateText.text = $"{stats.criticalRate}%";
        if (criticalDamageText != null) criticalDamageText.text = $"{stats.criticalDamageRate}%";
        if (fireOffenceText != null) fireOffenceText.text = stats.fireOffence.ToString();
        if (waterOffenceText != null) waterOffenceText.text = stats.waterOffence.ToString();
        if (windOffenceText != null) windOffenceText.text = stats.windOffence.ToString();
        if (earthOffenceText != null) earthOffenceText.text = stats.earthOffence.ToString();
    }

    private void UpdateEquipmentSlots()
    {
        if (isDestroying) return;

        UpdateEquipmentSlot(EquipmentType.Weapon, weaponSlotIcon, weaponSlotText, defaultWeaponIcon);
        UpdateEquipmentSlot(EquipmentType.Armor, armorSlotIcon, armorSlotText, defaultArmorIcon);
        UpdateEquipmentSlot(EquipmentType.Accessory, accessorySlotIcon, accessorySlotText, defaultAccessoryIcon);
    }

    private void UpdateEquipmentSlot(EquipmentType equipmentType, Image iconImage, TextMeshProUGUI nameText, Sprite defaultIcon)
    {
        if (isDestroying || iconImage == null) return;

        var equippedItems = InventoryManager.Instance.GetEquippedItems();
        var equippedItem = equippedItems.Find(eq =>
        {
            var masterData = MasterDataManager.Instance?.GetEquipmentData(eq.equipmentMasterId);
            return masterData?.equipmentType == equipmentType;
        });

        DebugLog($"装備スロット更新: {equipmentType}, 装備中アイテム: {(equippedItem != null ? equippedItem.userEquipmentId : "なし")}");

        if (equippedItem != null)
        {
            // 装備中のアイテム表示
            var masterData = MasterDataManager.Instance.GetEquipmentData(equippedItem.equipmentMasterId);
            if (masterData != null)
            {
                // アイコン設定
                Sprite iconToUse = masterData.equipmentIcon ?? defaultIcon;
                iconImage.sprite = iconToUse;

                DebugLog($"アイコン設定: {masterData.equipmentName}, Icon: {(masterData.equipmentIcon != null ? "あり" : "デフォルト")}");

                // 名前設定
                if (nameText != null)
                {
                    string displayName = masterData.equipmentName;
                    if (equippedItem.currentEnhancedValue > 0)
                    {
                        displayName += $" +{equippedItem.currentEnhancedValue}";
                    }
                    nameText.text = displayName;
                }
            }
        }
        else
        {
            // 装備なしの表示
            iconImage.sprite = defaultIcon;
            if (nameText != null)
            {
                nameText.text = GetEmptySlotText(equipmentType);
            }

            DebugLog($"装備なし表示: {equipmentType}");
        }
    }

    private string GetEmptySlotText(EquipmentType equipmentType)
    {
        return equipmentType switch
        {
            EquipmentType.Weapon => "武器",
            EquipmentType.Armor => "防具",
            EquipmentType.Accessory => "アクセサリー",
            _ => "装備なし"
        };
    }

    private void UpdateSkillSlots()
    {
        if (isDestroying) return;

        UpdateSkillSlot(1, skillSlot1Icon, skillSlot1Text);
        UpdateSkillSlot(2, skillSlot2Icon, skillSlot2Text);
    }

    private void UpdateSkillSlot(int slotNumber, Image iconImage, TextMeshProUGUI nameText)
    {
        if (isDestroying || iconImage == null) return;

        var saveData = SaveDataManager.Instance?.CurrentSaveData;
        if (saveData == null) return;

        string skillId = saveData.GetBattleSkill(slotNumber);

        if (!string.IsNullOrEmpty(skillId))
        {
            var skill = SkillManager.Instance?.GetSkill(skillId);
            if (skill != null)
            {
                var masterData = MasterDataManager.Instance?.GetSkillData(skill.skillMasterId);
                if (masterData != null)
                {
                    iconImage.sprite = masterData.skillIcon ?? defaultSkillIcon;
                    if (nameText != null)
                        nameText.text = masterData.skillName;
                    return;
                }
            }
        }

        // スキル未装備の表示
        iconImage.sprite = defaultSkillIcon;
        if (nameText != null)
            nameText.text = $"スキル{slotNumber}";
    }

    #endregion

    #region ユーティリティ

    private bool IsManagersReady()
    {
        return !isDestroying &&
               InventoryManager.Instance != null &&
               InventoryManager.Instance.IsInitialized &&
               SaveDataManager.Instance != null &&
               SaveDataManager.Instance.IsDataLoaded &&
               MasterDataManager.Instance != null &&
               MasterDataManager.Instance.IsDataLoaded &&
               SkillManager.Instance != null &&
               SkillManager.Instance.IsInitialized;
    }

    private EquipmentType GetCurrentPopupEquipmentType()
    {
        if (equipmentSelectionPopup != null)
        {
            return equipmentSelectionPopup.GetCurrentEquipmentType();
        }
        return lastOpenedEquipmentType;
    }

    private int GetSlotNumberFromId(string slotId)
    {
        if (string.IsNullOrEmpty(slotId)) return 0;
        if (slotId == "battle_skill_slot_1") return 1;
        if (slotId == "battle_skill_slot_2") return 2;
        return 0;
    }

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[EquipmentEditUI] {message}");
        }
    }

    private void DebugLogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[EquipmentEditUI] {message}");
        }
    }

    #endregion

    #region インベントリパネル制御・その他のメソッド

    private void ShowInventoryPanel()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            OnInventoryButtonClicked?.Invoke();
        }
    }

    private void HideInventoryPanel()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    // UpdatePlayerInfo, UpdateDetailedStatus, UpdateEquipmentSlots 等の
    // 既存メソッドはそのまま使用可能（isDestroying チェックを追加推奨）

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("表示を強制更新")]
    private void ForceRefresh()
    {
        RefreshDisplay();
    }

    [ContextMenu("武器選択をテスト")]
    private void TestWeaponSelection()
    {
        OpenEquipmentSelection(EquipmentType.Weapon);
    }

    [ContextMenu("ステータステスト")]
    private void TestStatusCalculation()
    {
        var characterStats = GetCharacterBaseStatus();
        var equipmentStats = GetEquippedItemsStatus();
        var totalStats = CalculateTotalStatus(characterStats, equipmentStats);

        Debug.Log($"キャラクター基本ステータス: HP={characterStats.hp}, 攻撃={characterStats.offense}");
        Debug.Log($"装備ステータス: HP={equipmentStats.hp}, 攻撃={equipmentStats.offense}");
        Debug.Log($"合計ステータス: HP={totalStats.hp}, 攻撃={totalStats.offense}");
    }

    [ContextMenu("スキルスロット1選択テスト")]
    private void TestSkillSlot1Selection()
    {
        OpenSkillSelection(1);
    }

    [ContextMenu("スキルスロット2選択テスト")]
    private void TestSkillSlot2Selection()
    {
        OpenSkillSelection(2);
    }
#endif

    #endregion
}

/// <summary>
/// キャラクターステータス用の構造体
/// </summary>
[System.Serializable]
public struct CharacterStatus
{
    public int hp;
    public int offense;
    public int defense;
    public int speed;
    public int criticalRate;
    public int criticalDamageRate;
    public int fireOffence;
    public int waterOffence;
    public int windOffence;
    public int earthOffence;
}