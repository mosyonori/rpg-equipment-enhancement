using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 強化アイテム選択UI制御クラス
/// 
/// 【責任】
/// - 強化アイテム選択UI制御
/// - 装備種類に応じた強化内容表示
/// - 装備未選択時はボタン無効化
/// 
/// 【主要機能】
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

    private void Start()
    {
        SetupButtons();
        ResetSelection();
        SetInteractable(false); // 初期状態は無効
    }

    private void SetupButtons()
    {
        if (enhanceItemSelectButton != null)
        {
            enhanceItemSelectButton.onClick.AddListener(OnEnhanceItemSelectButtonClicked);
        }
    }

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

    public void OnEnhanceItemSelectButtonClicked()
    {
        if (currentSelectedEquipment == null)
        {
            Debug.LogWarning("装備が選択されていません");
            return;
        }

        ShowEnhanceItemList();
    }

    private void ShowEnhanceItemList()
    {
        List<UserItem> ownedEnhanceItems = dataService.GetOwnedEnhanceItems();

        // 既存のアイテムを削除
        foreach (Transform child in enhanceItemListContent)
        {
            Destroy(child.gameObject);
        }

        // 強化アイテム生成
        foreach (var userItem in ownedEnhanceItems)
        {
            if (userItem.quantity <= 0) continue; // 所持数0のアイテムはスキップ

            EnhanceItemMasterData masterData = dataService.GetEnhanceItemMaster(userItem.item_id);
            if (masterData == null) continue;

            GameObject itemObj = Instantiate(enhanceItemPrefab, enhanceItemListContent);
            EnhanceItemListItemUI itemUI = itemObj.GetComponent<EnhanceItemListItemUI>();

            if (itemUI != null)
            {
                itemUI.Setup(masterData, userItem.quantity, OnEnhanceItemClicked);
            }
        }

        enhanceItemListPanel.SetActive(true);
    }

    private void OnEnhanceItemClicked(EnhanceItemMasterData enhanceItem)
    {
        selectedEnhanceItem = enhanceItem;

        // UI更新
        UpdateEnhanceItemDisplay();
        UpdateEnhanceContent();

        // パネルを閉じる
        enhanceItemListPanel.SetActive(false);

        // イベント通知
        OnEnhanceItemSelected?.Invoke(enhanceItem);
    }

    private void UpdateEnhanceItemDisplay()
    {
        if (selectedEnhanceItem != null)
        {
            // アイコン表示
            enhanceItemIconImage.sprite = LoadEnhanceItemIcon(selectedEnhanceItem.enhance_item_icon_path);
            enhanceItemIconImage.color = Color.white;
        }
    }

    private void UpdateEnhanceContent()
    {
        // 強化内容表示をクリア
        foreach (Transform child in enhanceContentDisplay)
        {
            Destroy(child.gameObject);
        }

        if (selectedEnhanceItem != null && currentSelectedEquipment != null)
        {
            // 装備種類に応じた強化内容を表示
            DisplayEnhanceContentByEquipmentType();
        }
    }

    private void DisplayEnhanceContentByEquipmentType()
    {
        EquipmentMasterData equipmentMaster = dataService.GetEquipmentMaster(currentSelectedEquipment.equipment_id);

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
        }
    }

    private void DisplayWeaponEnhanceContent()
    {
        // 武器強化内容表示
        // 仕様：武器：強化値+1、攻撃+1、クリティカルダメージ+1%
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

    private void DisplayArmorEnhanceContent()
    {
        // 防具強化内容表示
        // 仕様：防具：強化値+1、HP+3、防御+1
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

    private void DisplayAccessoryEnhanceContent()
    {
        // アクセサリ強化内容表示
        // 仕様：アクセ：強化値+1、HP+1、攻撃+1、防御+1
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

    private void CreateContentText(string content)
    {
        if (enhanceContentTextPrefab == null || enhanceContentDisplay == null) return;

        GameObject textObj = Instantiate(enhanceContentTextPrefab, enhanceContentDisplay);
        Text textComponent = textObj.GetComponent<Text>();

        if (textComponent != null)
        {
            textComponent.text = content;
        }
    }

    public void ResetSelection()
    {
        selectedEnhanceItem = null;
        currentSelectedEquipment = null;

        if (enhanceItemIconImage != null)
        {
            enhanceItemIconImage.sprite = null;
            enhanceItemIconImage.color = Color.gray;
        }

        // 強化内容表示をクリア
        foreach (Transform child in enhanceContentDisplay)
        {
            Destroy(child.gameObject);
        }

        // ボタンを無効化
        SetInteractable(false);
    }

    private Sprite LoadEnhanceItemIcon(string iconPath)
    {
        // TODO: アイコン読み込み実装
        // Resources.Loadやアドレサブルアセットシステムを使用
        return null;
    }

    /// <summary>
    /// 現在選択されている強化アイテムを取得
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
}

/// <summary>
/// 強化アイテムリストの個別アイテムUI
/// EnhanceItemListItemUIクラスの参考実装
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

    public void Setup(EnhanceItemMasterData masterData, int quantity, System.Action<EnhanceItemMasterData> onClick)
    {
        itemData = masterData;
        onClickCallback = onClick;

        // UI更新
        if (itemNameText != null)
            itemNameText.text = masterData.enhance_item_name;

        if (quantityText != null)
            quantityText.text = $"所持数: {quantity}";

        if (rarityText != null)
            rarityText.text = GetRarityText(masterData.rarity);

        // アイコン設定
        if (itemIcon != null)
        {
            // TODO: アイコン読み込み実装
            // itemIcon.sprite = LoadIcon(masterData.enhance_item_icon_path);
        }

        // ボタンイベント設定
        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(OnItemClicked);
        }
    }

    private void OnItemClicked()
    {
        onClickCallback?.Invoke(itemData);
    }

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
}