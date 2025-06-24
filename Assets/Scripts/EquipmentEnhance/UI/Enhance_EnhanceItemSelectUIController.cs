using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 強化アイテム選択UI制御クラス - プロパティ名修正版
/// 
/// 【責任】
/// - 強化アイテム選択UI制御
/// - 装備種類に応じた強化内容表示
/// - 装備未選択時ボタン無効化
/// 
/// 【重要機能】
/// - 所持強化アイテム一覧表示
/// - 強化アイテム選択処理  
/// - 装備種類別強化内容表示（武器・防具・アクセサリで異なる）
/// - リアルタイムUI更新
/// </summary>
public class Enhance_EnhanceItemSelectUIController : MonoBehaviour
{
    [Header("UI Elements")]
    public Button enhanceItemSelectButton;
    public Image enhanceItemIconImage;
    public GameObject enhanceItemListPanel;
    public Transform enhanceItemListContent;
    public GameObject enhanceItemPrefab;
    public Transform enhanceContentDisplay;
    public GameObject enhanceContentTextPrefab;

    [Header("Button State Colors")]
    public Color enabledButtonColor = Color.white;
    public Color disabledButtonColor = Color.gray;

    // Service層
    private EnhanceDataService dataService = new EnhanceDataService();

    // 選択状態
    private EnhanceItemMasterData selectedEnhanceItem;
    private UserEquipment currentSelectedEquipment;

    // イベント
    public event System.Action<EnhanceItemMasterData> OnEnhanceItemSelected;

    #region Unity Lifecycle

    private void Start()
    {
        SetupButtons();
        ResetSelection();
        SetInteractable(false); // 装備選択まで無効
    }

    private void OnDestroy()
    {
        RemoveEventListeners();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// ボタンイベント設定
    /// </summary>
    private void SetupButtons()
    {
        if (enhanceItemSelectButton != null)
        {
            enhanceItemSelectButton.onClick.AddListener(OnEnhanceItemSelectButtonClicked);
        }
    }

    /// <summary>
    /// イベントリスナー削除
    /// </summary>
    private void RemoveEventListeners()
    {
        if (enhanceItemSelectButton != null)
        {
            enhanceItemSelectButton.onClick.RemoveAllListeners();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 装備選択状態の更新
    /// 装備が選択されたときに外部から呼び出される
    /// </summary>
    public void SetSelectedEquipment(UserEquipment equipment)
    {
        currentSelectedEquipment = equipment;

        // 装備が選択されていれば強化アイテム選択を有効化
        SetInteractable(equipment != null);

        // 強化アイテムが既に選択されていれば強化内容を更新
        if (selectedEnhanceItem != null)
        {
            UpdateEnhanceContent();
        }
    }

    /// <summary>
    /// ボタンの有効/無効状態設定
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (enhanceItemSelectButton != null)
        {
            enhanceItemSelectButton.interactable = interactable;

            // ボタンの色も変更
            ColorBlock colors = enhanceItemSelectButton.colors;
            colors.normalColor = interactable ? enabledButtonColor : disabledButtonColor;
            enhanceItemSelectButton.colors = colors;
        }
    }

    /// <summary>
    /// 強化アイテム選択ボタンクリック処理
    /// </summary>
    public void OnEnhanceItemSelectButtonClicked()
    {
        if (currentSelectedEquipment == null)
        {
            Debug.LogWarning("[Enhance_EnhanceItemSelectUIController] 装備が選択されていません");
            return;
        }

        ShowEnhanceItemList();
    }

    /// <summary>
    /// 選択状態リセット
    /// </summary>
    public void ResetSelection()
    {
        selectedEnhanceItem = null;
        currentSelectedEquipment = null;

        UpdateEnhanceItemDisplay();
        ClearEnhanceContentDisplay();

        // ボタンを無効化
        SetInteractable(false);
    }

    /// <summary>
    /// 現在選択中の強化アイテム取得
    /// </summary>
    public EnhanceItemMasterData GetSelectedEnhanceItem()
    {
        return selectedEnhanceItem;
    }

    /// <summary>
    /// 強化アイテムが選択されているかどうか
    /// </summary>
    public bool HasSelectedItem()
    {
        return selectedEnhanceItem != null;
    }

    #endregion

    #region Enhance Item List Management

    /// <summary>
    /// 強化アイテム一覧表示
    /// </summary>
    private void ShowEnhanceItemList()
    {
        try
        {
            List<UserItem> ownedEnhanceItems = dataService.GetOwnedEnhanceItems();

            if (ownedEnhanceItems == null || ownedEnhanceItems.Count == 0)
            {
                Debug.LogWarning("[Enhance_EnhanceItemSelectUIController] 所持強化アイテムが見つかりません");
                return;
            }

            // 既存のアイテムを削除
            ClearEnhanceItemList();

            // 強化アイテム生成
            CreateEnhanceItemListItems(ownedEnhanceItems);

            // パネル表示
            if (enhanceItemListPanel != null)
            {
                enhanceItemListPanel.SetActive(true);
            }

            Debug.Log($"[Enhance_EnhanceItemSelectUIController] 強化アイテム一覧表示: {ownedEnhanceItems.Count}個");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Enhance_EnhanceItemSelectUIController] 強化アイテム一覧表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 強化アイテムリストアイテム生成
    /// </summary>
    private void CreateEnhanceItemListItems(List<UserItem> ownedEnhanceItems)
    {
        foreach (var userItem in ownedEnhanceItems)
        {
            if (userItem.quantity <= 0) continue; // 所持数0のアイテムはスキップ

            try
            {
                EnhanceItemMasterData masterData = dataService.GetEnhanceItemMaster(userItem.item_id);
                if (masterData == null)
                {
                    Debug.LogWarning($"[Enhance_EnhanceItemSelectUIController] 強化アイテムマスターデータが見つかりません: {userItem.item_id}");
                    continue;
                }

                GameObject itemObj = Instantiate(enhanceItemPrefab, enhanceItemListContent);
                EnhanceItemListItemUI itemUI = itemObj.GetComponent<EnhanceItemListItemUI>();

                if (itemUI != null)
                {
                    itemUI.Setup(masterData, userItem.quantity, OnEnhanceItemClicked);
                }
                else
                {
                    Debug.LogWarning($"[Enhance_EnhanceItemSelectUIController] EnhanceItemListItemUIコンポーネントが見つかりません");

                    // フォールバック: 基本的なボタン設定
                    Button itemButton = itemObj.GetComponent<Button>();
                    if (itemButton != null)
                    {
                        itemButton.onClick.AddListener(() => OnEnhanceItemClicked(masterData));
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Enhance_EnhanceItemSelectUIController] 強化アイテム生成エラー: {userItem.item_id}, {e.Message}");
            }
        }
    }

    /// <summary>
    /// 強化アイテムリストクリア
    /// </summary>
    private void ClearEnhanceItemList()
    {
        if (enhanceItemListContent == null) return;

        foreach (Transform child in enhanceItemListContent)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    #endregion

    #region Enhance Item Selection

    /// <summary>
    /// 強化アイテムクリック処理
    /// </summary>
    private void OnEnhanceItemClicked(EnhanceItemMasterData enhanceItem)
    {
        if (enhanceItem == null)
        {
            Debug.LogWarning("[Enhance_EnhanceItemSelectUIController] 無効な強化アイテムが選択されました");
            return;
        }

        selectedEnhanceItem = enhanceItem;

        // UI更新
        UpdateEnhanceItemDisplay();
        UpdateEnhanceContent();

        // パネルを閉じる
        if (enhanceItemListPanel != null)
        {
            enhanceItemListPanel.SetActive(false);
        }

        // イベント通知
        OnEnhanceItemSelected?.Invoke(enhanceItem);

        Debug.Log($"[Enhance_EnhanceItemSelectUIController] 強化アイテム選択: {enhanceItem.enhance_item_name}");
    }

    #endregion

    #region Display Update

    /// <summary>
    /// 強化アイテム表示更新
    /// </summary>
    private void UpdateEnhanceItemDisplay()
    {
        if (enhanceItemIconImage == null) return;

        if (selectedEnhanceItem != null)
        {
            // Unity上のマスターデータからアイコンを取得
            enhanceItemIconImage.sprite = LoadEnhanceItemIcon(selectedEnhanceItem.enhance_item_id);
            enhanceItemIconImage.color = Color.white;
        }
        else
        {
            // 未選択の場合
            enhanceItemIconImage.sprite = null;
            enhanceItemIconImage.color = Color.gray;
        }
    }

    /// <summary>
    /// 強化内容表示更新
    /// </summary>
    private void UpdateEnhanceContent()
    {
        // 強化内容表示をクリア
        ClearEnhanceContentDisplay();

        if (selectedEnhanceItem != null && currentSelectedEquipment != null)
        {
            // 装備種類に応じた強化内容を表示
            DisplayEnhanceContentByEquipmentType();
        }
    }

    /// <summary>
    /// 装備種類別強化内容表示
    /// </summary>
    private void DisplayEnhanceContentByEquipmentType()
    {
        try
        {
            EquipmentMasterData equipmentMaster = dataService.GetEquipmentMaster(currentSelectedEquipment.equipment_id);

            if (equipmentMaster == null)
            {
                Debug.LogWarning($"[Enhance_EnhanceItemSelectUIController] 装備マスターデータが見つかりません: {currentSelectedEquipment.equipment_id}");
                return;
            }

            switch (equipmentMaster.equipment_type)
            {
                case EquipmentType.Weapon:
                    DisplayWeaponEnhanceContent();
                    break;
                case EquipmentType.Armor:
                    DisplayArmorEnhanceContent();
                    break;
                case EquipmentType.Accessory:
                    DisplayAccessoryEnhanceContent();
                    break;
                default:
                    Debug.LogWarning($"[Enhance_EnhanceItemSelectUIController] 未対応の装備種類: {equipmentMaster.equipment_type}");
                    break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Enhance_EnhanceItemSelectUIController] 強化内容表示エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 武器強化内容表示
    /// 仕様：武器：強化値+1、攻撃+1、クリティカルダメージ+1%
    /// </summary>
    private void DisplayWeaponEnhanceContent()
    {
        CreateContentText($"強化値: +{selectedEnhanceItem.add_enhanced_value}");

        if (selectedEnhanceItem.weapon_hp > 0)
            CreateContentText($"HP: +{selectedEnhanceItem.weapon_hp}");

        if (selectedEnhanceItem.weapon_offense > 0)
            CreateContentText($"攻撃力: +{selectedEnhanceItem.weapon_offense}");

        if (selectedEnhanceItem.weapon_defense > 0)
            CreateContentText($"防御力: +{selectedEnhanceItem.weapon_defense}");

        if (selectedEnhanceItem.weapon_speed > 0)
            CreateContentText($"速度: +{selectedEnhanceItem.weapon_speed}");

        if (selectedEnhanceItem.weapon_critical_rate > 0)
            CreateContentText($"クリティカル率: +{selectedEnhanceItem.weapon_critical_rate}%");

        if (selectedEnhanceItem.weapon_critical_damage_rate > 0)
            CreateContentText($"クリティカルダメージ: +{selectedEnhanceItem.weapon_critical_damage_rate}%");

        // 武器用属性攻撃表示
        DisplayWeaponAttributeContent();
    }

    /// <summary>
    /// 防具強化内容表示
    /// 仕様：防具：強化値+1、HP+3、防御+1
    /// </summary>
    private void DisplayArmorEnhanceContent()
    {
        CreateContentText($"強化値: +{selectedEnhanceItem.add_enhanced_value}");

        if (selectedEnhanceItem.armor_hp > 0)
            CreateContentText($"HP: +{selectedEnhanceItem.armor_hp}");

        if (selectedEnhanceItem.armor_offense > 0)
            CreateContentText($"攻撃力: +{selectedEnhanceItem.armor_offense}");

        if (selectedEnhanceItem.armor_defense > 0)
            CreateContentText($"防御力: +{selectedEnhanceItem.armor_defense}");

        if (selectedEnhanceItem.armor_speed > 0)
            CreateContentText($"速度: +{selectedEnhanceItem.armor_speed}");

        if (selectedEnhanceItem.armor_critical_rate > 0)
            CreateContentText($"クリティカル率: +{selectedEnhanceItem.armor_critical_rate}%");

        if (selectedEnhanceItem.armor_critical_damage_rate > 0)
            CreateContentText($"クリティカルダメージ: +{selectedEnhanceItem.armor_critical_damage_rate}%");

        // 防具用属性攻撃表示
        DisplayArmorAttributeContent();
    }

    /// <summary>
    /// アクセサリ強化内容表示
    /// 仕様：アクセ：強化値+1、HP+1、攻撃+1、防御+1
    /// </summary>
    private void DisplayAccessoryEnhanceContent()
    {
        CreateContentText($"強化値: +{selectedEnhanceItem.add_enhanced_value}");

        if (selectedEnhanceItem.accessory_hp > 0)
            CreateContentText($"HP: +{selectedEnhanceItem.accessory_hp}");

        if (selectedEnhanceItem.accessory_offense > 0)
            CreateContentText($"攻撃力: +{selectedEnhanceItem.accessory_offense}");

        if (selectedEnhanceItem.accessory_defense > 0)
            CreateContentText($"防御力: +{selectedEnhanceItem.accessory_defense}");

        if (selectedEnhanceItem.accessory_speed > 0)
            CreateContentText($"速度: +{selectedEnhanceItem.accessory_speed}");

        if (selectedEnhanceItem.accessory_critical_rate > 0)
            CreateContentText($"クリティカル率: +{selectedEnhanceItem.accessory_critical_rate}%");

        if (selectedEnhanceItem.accessory_critical_damage_rate > 0)
            CreateContentText($"クリティカルダメージ: +{selectedEnhanceItem.accessory_critical_damage_rate}%");

        // アクセサリ用属性攻撃表示
        DisplayAccessoryAttributeContent();
    }

    /// <summary>
    /// 武器用属性攻撃内容表示
    /// </summary>
    private void DisplayWeaponAttributeContent()
    {
        if (selectedEnhanceItem.weapon_fire_offence > 0)
            CreateContentText($"火属性攻撃: +{selectedEnhanceItem.weapon_fire_offence}");
        if (selectedEnhanceItem.weapon_water_offence > 0)
            CreateContentText($"水属性攻撃: +{selectedEnhanceItem.weapon_water_offence}");
        if (selectedEnhanceItem.weapon_wind_offence > 0)
            CreateContentText($"風属性攻撃: +{selectedEnhanceItem.weapon_wind_offence}");
        if (selectedEnhanceItem.weapon_earth_offence > 0)
            CreateContentText($"土属性攻撃: +{selectedEnhanceItem.weapon_earth_offence}");
    }

    /// <summary>
    /// 防具用属性攻撃内容表示
    /// </summary>
    private void DisplayArmorAttributeContent()
    {
        if (selectedEnhanceItem.armor_fire_offence > 0)
            CreateContentText($"火属性攻撃: +{selectedEnhanceItem.armor_fire_offence}");
        if (selectedEnhanceItem.armor_water_offence > 0)
            CreateContentText($"水属性攻撃: +{selectedEnhanceItem.armor_water_offence}");
        if (selectedEnhanceItem.armor_wind_offence > 0)
            CreateContentText($"風属性攻撃: +{selectedEnhanceItem.armor_wind_offence}");
        if (selectedEnhanceItem.armor_earth_offence > 0)
            CreateContentText($"土属性攻撃: +{selectedEnhanceItem.armor_earth_offence}");
    }

    /// <summary>
    /// アクセサリ用属性攻撃内容表示
    /// </summary>
    private void DisplayAccessoryAttributeContent()
    {
        if (selectedEnhanceItem.accessory_fire_offence > 0)
            CreateContentText($"火属性攻撃: +{selectedEnhanceItem.accessory_fire_offence}");
        if (selectedEnhanceItem.accessory_water_offence > 0)
            CreateContentText($"水属性攻撃: +{selectedEnhanceItem.accessory_water_offence}");
        if (selectedEnhanceItem.accessory_wind_offence > 0)
            CreateContentText($"風属性攻撃: +{selectedEnhanceItem.accessory_wind_offence}");
        if (selectedEnhanceItem.accessory_earth_offence > 0)
            CreateContentText($"土属性攻撃: +{selectedEnhanceItem.accessory_earth_offence}");
    }

    /// <summary>
    /// 強化内容テキスト生成
    /// </summary>
    private void CreateContentText(string content)
    {
        if (enhanceContentTextPrefab == null || enhanceContentDisplay == null) return;

        try
        {
            GameObject textObj = Instantiate(enhanceContentTextPrefab, enhanceContentDisplay);
            Text textComponent = textObj.GetComponent<Text>();

            if (textComponent != null)
            {
                textComponent.text = content;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Enhance_EnhanceItemSelectUIController] 強化内容テキスト生成エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 強化内容表示クリア
    /// </summary>
    private void ClearEnhanceContentDisplay()
    {
        if (enhanceContentDisplay == null) return;

        foreach (Transform child in enhanceContentDisplay)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 強化アイテムアイコン読み込み
    /// </summary>
    private Sprite LoadEnhanceItemIcon(int enhanceItemId)
    {
        try
        {
            // Unity上のマスターデータからアイコンを読み込み（IDベース）
            return Resources.Load<Sprite>($"Icons/EnhanceItems/enhance_item_{enhanceItemId:D3}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Enhance_EnhanceItemSelectUIController] アイコン読み込み失敗: enhance_item_{enhanceItemId:D3}, {e.Message}");
            return null;
        }
    }

    #endregion
}

/// <summary>
/// 強化アイテムリストの個別アイテムUI - プロパティ名修正版
/// EnhanceItemListItemUIクラスの参照実装
/// </summary>
public class EnhanceItemListItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Button itemButton;
    public Image itemIcon;
    public Text itemNameText;
    public Text quantityText;
    public Text rarityText;

    private EnhanceItemMasterData itemData;
    private System.Action<EnhanceItemMasterData> onClickCallback;

    #region Public Methods

    /// <summary>
    /// 強化アイテムセットアップ
    /// </summary>
    public void Setup(EnhanceItemMasterData masterData, int quantity, System.Action<EnhanceItemMasterData> onClick)
    {
        itemData = masterData;
        onClickCallback = onClick;

        // UI更新
        UpdateDisplay(quantity);

        // ボタンイベント設定
        SetupButton();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 表示更新
    /// </summary>
    private void UpdateDisplay(int quantity)
    {
        if (itemData == null) return;

        // アイテム名表示
        if (itemNameText != null)
            itemNameText.text = itemData.enhance_item_name;

        // 所持数表示
        if (quantityText != null)
            quantityText.text = $"所持数: {quantity}";

        // レアリティ表示
        if (rarityText != null)
            rarityText.text = GetRarityText(itemData.rarity);

        // アイコン設定
        if (itemIcon != null)
        {
            // Unity上のマスターデータからアイコンを読み込み（IDベース）
            itemIcon.sprite = LoadIcon(itemData.enhance_item_id);
        }
    }

    /// <summary>
    /// ボタン設定
    /// </summary>
    private void SetupButton()
    {
        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(OnItemClicked);
        }
    }

    /// <summary>
    /// アイテムクリック処理
    /// </summary>
    private void OnItemClicked()
    {
        onClickCallback?.Invoke(itemData);
    }

    /// <summary>
    /// レアリティテキスト取得
    /// </summary>
    private string GetRarityText(string rarity)
    {
        switch (rarity.ToLower())
        {
            case "common": return "コモン";
            case "rare": return "レア";
            case "epic": return "エピック";
            case "legendary": return "レジェンダリー";
            default: return rarity;
        }
    }

    /// <summary>
    /// アイコン読み込み
    /// </summary>
    private Sprite LoadIcon(int enhanceItemId)
    {
        try
        {
            // Unity上のマスターデータからアイコンを読み込み（IDベース）
            return Resources.Load<Sprite>($"Icons/EnhanceItems/enhance_item_{enhanceItemId:D3}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EnhanceItemListItemUI] アイコン読み込み失敗: enhance_item_{enhanceItemId:D3}, {e.Message}");
            return null;
        }
    }

    #endregion
}