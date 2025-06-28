using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 装備編集画面のメインUI
/// </summary>
public class EquipmentEditUI : MonoBehaviour
{
    [Header("装備スロットボタン")]
    [SerializeField] private Button weaponSlotButton;
    [SerializeField] private Button armorSlotButton;
    [SerializeField] private Button accessorySlotButton;
    [SerializeField] private Button futureSlot1Button; // 将来用スロット
    [SerializeField] private Button futureSlot2Button; // 将来用スロット

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

    [Header("デバッグ")]
    [SerializeField] private bool enableDebugLog = true;

    // イベント
    public System.Action OnBackButtonClicked;
    public System.Action OnInventoryButtonClicked;
    public System.Action OnEnhanceButtonClicked;

    // 前回に開いた装備タイプを記録
    private EquipmentType lastOpenedEquipmentType;

    #region Unity Lifecycle

    private void Awake()
    {
        SetupButtons();
        SetupPopupEvents();
        SetupInventoryPanel();
    }

    private void Start()
    {
        // イベント購読
        SubscribeToEvents();

        // 初期表示更新
        RefreshDisplay();
    }

    private void OnDestroy()
    {
        // イベント購読解除
        UnsubscribeFromEvents();
    }

    #endregion

    #region 初期化

    private void SetupButtons()
    {
        // 装備スロットボタン
        if (weaponSlotButton != null)
        {
            weaponSlotButton.onClick.AddListener(() => OpenEquipmentSelection(EquipmentType.Weapon));
        }

        if (armorSlotButton != null)
        {
            armorSlotButton.onClick.AddListener(() => OpenEquipmentSelection(EquipmentType.Armor));
        }

        if (accessorySlotButton != null)
        {
            accessorySlotButton.onClick.AddListener(() => OpenEquipmentSelection(EquipmentType.Accessory));
        }

        // 将来用スロットは無効化
        if (futureSlot1Button != null)
        {
            futureSlot1Button.interactable = false;
        }

        if (futureSlot2Button != null)
        {
            futureSlot2Button.interactable = false;
        }

        // ナビゲーションボタン
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => OnBackButtonClicked?.Invoke());
        }

        if (inventoryButton != null)
        {
            inventoryButton.onClick.AddListener(ShowInventoryPanel);
            DebugLog("インベントリボタンにShowInventoryPanelを設定しました");
        }

        if (enhanceButton != null)
        {
            enhanceButton.onClick.AddListener(() => OnEnhanceButtonClicked?.Invoke());
        }
    }

    private void SetupPopupEvents()
    {
        if (equipmentSelectionPopup != null)
        {
            equipmentSelectionPopup.OnEquipmentSelected += OnEquipmentSelected;
            equipmentSelectionPopup.OnEquipmentRemoved += OnEquipmentRemoved;
            equipmentSelectionPopup.OnPopupClosed += OnPopupClosed;
        }
    }

    /// <summary>
    /// インベントリパネルの初期化
    /// </summary>
    private void SetupInventoryPanel()
    {
        // インベントリパネルを非表示状態に設定
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            DebugLog("インベントリパネルを非表示状態に初期化しました");
        }

        // インベントリ閉じるボタンの設定
        if (inventoryCloseButton != null)
        {
            inventoryCloseButton.onClick.AddListener(HideInventoryPanel);
            DebugLog("インベントリ閉じるボタンを設定しました");
        }
    }

    private void SubscribeToEvents()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnEquipmentEquipped += OnEquipmentEquipped;
            InventoryManager.OnEquipmentUnequipped += OnEquipmentUnequipped;
            InventoryManager.OnInventoryChanged += OnInventoryChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnEquipmentEquipped -= OnEquipmentEquipped;
            InventoryManager.OnEquipmentUnequipped -= OnEquipmentUnequipped;
            InventoryManager.OnInventoryChanged -= OnInventoryChanged;
        }

        if (equipmentSelectionPopup != null)
        {
            equipmentSelectionPopup.OnEquipmentSelected -= OnEquipmentSelected;
            equipmentSelectionPopup.OnEquipmentRemoved -= OnEquipmentRemoved;
            equipmentSelectionPopup.OnPopupClosed -= OnPopupClosed;
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 表示を更新
    /// </summary>
    public void RefreshDisplay()
    {
        if (!IsManagersReady()) return;

        UpdatePlayerInfo();
        UpdateEquipmentSlots();
        UpdateDetailedStatus(); // 新規追加

        DebugLog("装備編集画面の表示を更新しました");
    }

    #endregion

    #region インベントリパネル制御

    /// <summary>
    /// インベントリパネルを表示
    /// </summary>
    private void ShowInventoryPanel()
    {
        if (inventoryPanel == null)
        {
            DebugLogError("インベントリパネルが設定されていません");
            return;
        }

        DebugLog($"インベントリパネル表示前の状態: {inventoryPanel.activeSelf}");

        inventoryPanel.SetActive(true);

        DebugLog($"インベントリパネル表示後の状態: {inventoryPanel.activeSelf}");
        DebugLog("インベントリパネルを表示しました");

        // 外部イベントも呼び出し（従来の機能との互換性）
        OnInventoryButtonClicked?.Invoke();
    }

    /// <summary>
    /// インベントリパネルを非表示
    /// </summary>
    private void HideInventoryPanel()
    {
        if (inventoryPanel == null)
        {
            DebugLogError("インベントリパネルが設定されていません");
            return;
        }

        DebugLog($"インベントリパネル非表示前の状態: {inventoryPanel.activeSelf}");

        inventoryPanel.SetActive(false);

        DebugLog($"インベントリパネル非表示後の状態: {inventoryPanel.activeSelf}");
        DebugLog("インベントリパネルを非表示にしました");
    }

    #endregion

    #region 内部メソッド - 表示更新

    private void UpdatePlayerInfo()
    {
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
            int maxCount = 1000; // 設定可能にする
            equipmentCountText.text = $"{currentCount}/{maxCount}";
        }
    }

    /// <summary>
    /// 詳細ステータス表示を更新（新規追加）
    /// </summary>
    private void UpdateDetailedStatus()
    {
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

    /// <summary>
    /// キャラクターの基本ステータスを取得
    /// </summary>
    private CharacterStatus GetCharacterBaseStatus()
    {
        // キャラクターID=1の基礎値データを取得（固定）
        var characterData = MasterDataManager.Instance?.GetCharacterData(1);

        if (characterData == null)
        {
            DebugLogError("キャラクターデータが見つかりません（ID:1）");
            return new CharacterStatus(); // 空のステータスを返す
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

    /// <summary>
    /// 装備中アイテムのステータス合計を取得
    /// </summary>
    private CharacterStatus GetEquippedItemsStatus()
    {
        var totalStats = new CharacterStatus();
        var equippedItems = InventoryManager.Instance?.GetEquippedItems();

        if (equippedItems == null || equippedItems.Count == 0)
        {
            return totalStats; // 装備なしの場合は0を返す
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

    /// <summary>
    /// 合計ステータスを計算
    /// </summary>
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

    /// <summary>
    /// ステータス表示を更新
    /// </summary>
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
        UpdateEquipmentSlot(EquipmentType.Weapon, weaponSlotIcon, weaponSlotText, defaultWeaponIcon);
        UpdateEquipmentSlot(EquipmentType.Armor, armorSlotIcon, armorSlotText, defaultArmorIcon);
        UpdateEquipmentSlot(EquipmentType.Accessory, accessorySlotIcon, accessorySlotText, defaultAccessoryIcon);
    }

    private void UpdateEquipmentSlot(EquipmentType equipmentType, Image iconImage, TextMeshProUGUI nameText, Sprite defaultIcon)
    {
        if (iconImage == null) return;

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
            EquipmentType.Weapon => "武器なし",
            EquipmentType.Armor => "防具なし",
            EquipmentType.Accessory => "アクセサリーなし",
            _ => "装備なし"
        };
    }

    #endregion

    #region 内部メソッド - 装備選択

    private void OpenEquipmentSelection(EquipmentType equipmentType)
    {
        if (equipmentSelectionPopup == null)
        {
            DebugLogError("装備選択ポップアップが設定されていません");
            return;
        }

        // 装備タイプを記録
        lastOpenedEquipmentType = equipmentType;

        equipmentSelectionPopup.ShowEquipmentSelection(equipmentType);
        DebugLog($"装備選択を開始: {equipmentType}");
    }

    #endregion

    #region イベントハンドラー

    private void OnEquipmentSelected(UserEquipmentData equipment)
    {
        if (equipment == null) return;

        // 装備を装着
        bool success = InventoryManager.Instance.EquipItem(equipment.userEquipmentId);

        if (success)
        {
            DebugLog($"装備を装着しました: {equipment.userEquipmentId}");
            // 装備画面の表示を即座に更新
            RefreshDisplay();
        }
        else
        {
            DebugLogError($"装備の装着に失敗しました: {equipment.userEquipmentId}");
        }
    }

    private void OnEquipmentRemoved()
    {
        // 現在のポップアップで表示している装備タイプの装備を外す
        EquipmentType targetType = GetCurrentPopupEquipmentType();

        DebugLog($"装備解除を開始: {targetType}");

        // InventoryManagerの新しいメソッドを使用
        bool success = InventoryManager.Instance.UnequipItemByType(targetType);

        if (success)
        {
            DebugLog($"装備を外しました: {targetType}");
            // 表示更新はイベントで自動実行されるため、ここでは呼ばない
        }
        else
        {
            DebugLog($"外す装備がないか、既に外されています: {targetType}");
        }
    }

    /// <summary>
    /// 現在のポップアップで表示している装備タイプを取得
    /// </summary>
    private EquipmentType GetCurrentPopupEquipmentType()
    {
        // ポップアップから直接取得
        if (equipmentSelectionPopup != null)
        {
            return equipmentSelectionPopup.GetCurrentEquipmentType();
        }

        // フォールバック: 記録された値を使用
        return lastOpenedEquipmentType;
    }

    private void OnPopupClosed()
    {
        DebugLog("装備選択ポップアップが閉じられました");
    }

    private void OnEquipmentEquipped(UserEquipmentData equipment)
    {
        DebugLog($"装備装着イベント: {equipment.userEquipmentId}");
        // 少し遅延して表示更新（装備処理の完了を待つ）
        StartCoroutine(DelayedRefresh());
    }

    private void OnEquipmentUnequipped(UserEquipmentData equipment)
    {
        DebugLog($"装備解除イベント: {equipment.userEquipmentId}");
        // 少し遅延して表示更新（装備処理の完了を待つ）
        StartCoroutine(DelayedRefresh());
    }

    private void OnInventoryChanged()
    {
        // 少し遅延して表示更新
        StartCoroutine(DelayedRefresh());
    }

    /// <summary>
    /// 遅延して表示更新（装備処理の完了を待つ）
    /// </summary>
    private System.Collections.IEnumerator DelayedRefresh()
    {
        yield return new WaitForEndOfFrame(); // 1フレーム待機
        RefreshDisplay();
    }

    #endregion

    #region ユーティリティ

    private bool IsManagersReady()
    {
        return InventoryManager.Instance != null &&
               InventoryManager.Instance.IsInitialized &&
               SaveDataManager.Instance != null &&
               SaveDataManager.Instance.IsDataLoaded &&
               MasterDataManager.Instance != null &&
               MasterDataManager.Instance.IsDataLoaded;
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

    [ContextMenu("インベントリパネル表示テスト")]
    private void TestShowInventoryPanel()
    {
        ShowInventoryPanel();
    }

    [ContextMenu("インベントリパネル非表示テスト")]
    private void TestHideInventoryPanel()
    {
        HideInventoryPanel();
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