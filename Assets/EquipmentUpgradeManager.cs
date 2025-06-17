using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EquipmentUpgradeManager : MonoBehaviour
{
    [Header("Equipment Selection")]
    public Button equipmentSlotButton; // 装備選択ボタン
    public Image equipmentIcon;
    public TextMeshProUGUI equipmentNameText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI criticalRateText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI enhancementLevelText;
    public TextMeshProUGUI durabilityText;
    public Slider durabilitySlider;

    [Header("Elemental Attack Texts (Optional)")]
    public TextMeshProUGUI elementalAttackText;     // 汎用属性攻撃
    public TextMeshProUGUI fireAttackText;          // 火属性攻撃
    public TextMeshProUGUI waterAttackText;         // 水属性攻撃
    public TextMeshProUGUI windAttackText;          // 風属性攻撃
    public TextMeshProUGUI earthAttackText;         // 土属性攻撃

    [Header("Enhancement Item Selection")]
    public Button enhancementItemButton;
    public Image enhancementItemIcon;
    public TextMeshProUGUI enhancementItemNameText;
    public TextMeshProUGUI enhancementItemQuantityText;
    public TextMeshProUGUI enhancementItemEffectText; // 強化アイテム効果表示用

    [Header("Support Item Selection")]
    public Button supportItemButton;
    public Image supportItemIcon;
    public TextMeshProUGUI supportItemNameText;
    public TextMeshProUGUI supportItemQuantityText;
    public TextMeshProUGUI supportItemEffectText;

    [Header("Enhancement Button")]
    public Button enhanceButton;
    public TextMeshProUGUI enhanceButtonText;
    public TextMeshProUGUI successRateText;

    [Header("Item Selection UI")]
    public ItemSelectionUI itemSelectionUI;

    [Header("Effect System")]
    public Transform effectSpawnPoint;
    public AudioSource audioSource;

    // 現在選択中のデータ
    private int currentEquipmentIndex = -1; // ★ 修正: 初期値を-1（未選択）に変更
    private int selectedEnhancementItemId = -1;
    private int selectedSupportItemId = -1;

    private void Start()
    {
        SetupUI();

        // ★ 修正: 初期状態では何も選択されていない状態にする
        currentEquipmentIndex = -1; // -1で未選択状態を表す

        RefreshUI();
    }

    private void SetupUI()
    {
        // ボタンイベント設定
        if (enhancementItemButton != null)
            enhancementItemButton.onClick.AddListener(() => ShowItemSelection("enhancement"));
        else
            Debug.LogWarning("enhancementItemButton が設定されていません");

        if (supportItemButton != null)
            supportItemButton.onClick.AddListener(() => ShowItemSelection("support"));
        else
            Debug.LogWarning("supportItemButton が設定されていません");

        if (enhanceButton != null)
            enhanceButton.onClick.AddListener(OnEnhanceButtonClicked);
        else
            Debug.LogWarning("enhanceButton が設定されていません");

        // 装備選択ボタンのイベント設定
        if (equipmentSlotButton != null)
            equipmentSlotButton.onClick.AddListener(() => ShowItemSelection("equipment"));
        else
            Debug.LogWarning("equipmentSlotButton が設定されていません");

        // アイテム選択UIのイベント設定
        if (itemSelectionUI != null)
        {
            itemSelectionUI.OnItemSelected += OnItemSelected;
            itemSelectionUI.OnSelectionCancelled += OnSelectionCancelled;
            Debug.Log("ItemSelectionUI イベント設定完了");
        }
        else
        {
            Debug.LogWarning("itemSelectionUI が設定されていません。Inspector で設定してください。");
        }
    }

    // アイテム選択UI表示
    private void ShowItemSelection(string itemType)
    {
        if (itemSelectionUI == null)
        {
            Debug.LogWarning("itemSelectionUI が設定されていません。アイテム選択画面を表示できません。");
            return;
        }

        switch (itemType)
        {
            case "equipment":
                itemSelectionUI.ShowEquipmentSelection();
                break;
            case "enhancement":
                itemSelectionUI.ShowEnhancementItemSelection();
                break;
            case "support":
                itemSelectionUI.ShowSupportMaterialSelection();
                break;
            default:
                Debug.LogWarning($"不明なアイテムタイプ: {itemType}");
                break;
        }
    }

    // 装備選択（外部から呼び出される）
    public void SelectEquipment(int equipmentIndex)
    {
        currentEquipmentIndex = equipmentIndex;
        selectedEnhancementItemId = -1;
        selectedSupportItemId = -1;
        RefreshUI();
    }

    // 既存互換性確保: 既存スクリプト用のSelectEquipmentオーバーロード
    public void SelectEquipment(EquipmentData equipData, UserEquipment userEquip)
    {
        // ユーザー装備リストからインデックスを検索
        var equipmentList = DataManager.Instance.GetAllUserEquipments();
        for (int i = 0; i < equipmentList.Count; i++)
        {
            if (equipmentList[i] == userEquip)
            {
                SelectEquipment(i);
                return;
            }
        }

        Debug.LogWarning("指定された装備がユーザー装備リストに見つかりません");
    }

    // 既存互換性確保: 既存スクリプト用のSelectUpgradeItem
    public void SelectUpgradeItem(EnhancementItemData itemData)
    {
        selectedEnhancementItemId = itemData.itemId;
        RefreshUI();
        Debug.Log($"強化アイテムを選択しました: {itemData.itemName}");
    }

    // 既存互換性確保: 既存スクリプト用のSelectSupportItem
    public void SelectSupportItem(SupportMaterialData materialData)
    {
        selectedSupportItemId = materialData.materialId;
        RefreshUI();
        Debug.Log($"補助材料を選択しました: {materialData.materialName}");
    }

    // 補助アイテム選択解除
    public void DeselectSupportItem()
    {
        selectedSupportItemId = -1;
        RefreshUI();
    }

    // UI全体を更新
    private void RefreshUI()
    {
        UpdateEquipmentDisplay();
        UpdateEnhancementItemDisplay();
        UpdateSupportItemDisplay();
        UpdateEnhanceButton();
    }

    // 装備情報表示を更新
    private void UpdateEquipmentDisplay()
    {
        // ★ 修正: 装備が選択されていない場合の処理を追加
        if (currentEquipmentIndex < 0)
        {
            // 装備未選択の場合の表示
            if (equipmentIcon != null)
            {
                equipmentIcon.sprite = null;
                equipmentIcon.color = new Color(1, 1, 1, 0.3f); // 薄いグレー
            }

            if (equipmentNameText != null) equipmentNameText.text = "装備を選択してください";
            if (enhancementLevelText != null) enhancementLevelText.text = "";

            // 全てのステータステキストを非表示または空にする
            SetStatTextEmpty(attackText);
            SetStatTextEmpty(defenseText);
            SetStatTextEmpty(criticalRateText);
            SetStatTextEmpty(hpText);
            SetStatTextEmpty(elementalAttackText);
            SetStatTextEmpty(fireAttackText);
            SetStatTextEmpty(waterAttackText);
            SetStatTextEmpty(windAttackText);
            SetStatTextEmpty(earthAttackText);

            if (durabilityText != null) durabilityText.text = "";
            if (durabilitySlider != null)
            {
                durabilitySlider.value = 0;
                durabilitySlider.gameObject.SetActive(false);
            }

            return;
        }

        var userEquipment = DataManager.Instance?.GetUserEquipment(currentEquipmentIndex);
        if (userEquipment == null)
        {
            Debug.LogWarning($"装備が見つかりません。インデックス: {currentEquipmentIndex}");
            return;
        }

        var masterData = DataManager.Instance.GetEquipmentData(userEquipment.equipmentId);
        if (masterData == null)
        {
            Debug.LogWarning($"装備マスターデータが見つかりません。ID: {userEquipment.equipmentId}");
            return;
        }

        // 基本情報
        if (equipmentIcon != null && masterData.icon != null)
        {
            equipmentIcon.sprite = masterData.icon;
            equipmentIcon.color = Color.white; // アイコンを白色で表示
            Debug.Log($"装備アイコン設定: {masterData.equipmentName}, アイコン: {masterData.icon?.name ?? "null"}");
        }

        if (equipmentNameText != null) equipmentNameText.text = masterData.equipmentName;

        // 修正: 表示順序を調整
        // 1. 強化レベル（一番上）
        if (enhancementLevelText != null) enhancementLevelText.text = $"+{userEquipment.enhancementLevel}";

        // 2. 基本ステータス表示（マスターデータ + ユーザーボーナス）
        UpdateStatText(attackText, "攻撃力", userEquipment.GetTotalAttack());
        UpdateStatText(defenseText, "防御力", userEquipment.GetTotalDefense());
        UpdateStatText(criticalRateText, "クリティカル率", userEquipment.GetTotalCriticalRate(), "%");
        UpdateStatText(hpText, "HP", userEquipment.GetTotalHP());

        // 3. 属性攻撃の表示（設定されている場合のみ）
        UpdateStatText(elementalAttackText, "属性攻撃", userEquipment.GetTotalElementalAttack());
        UpdateStatText(fireAttackText, "火属性攻撃", userEquipment.GetTotalFireAttack());
        UpdateStatText(waterAttackText, "水属性攻撃", userEquipment.GetTotalWaterAttack());
        UpdateStatText(windAttackText, "風属性攻撃", userEquipment.GetTotalWindAttack());
        UpdateStatText(earthAttackText, "土属性攻撃", userEquipment.GetTotalEarthAttack());

        // 4. 耐久度表示（一番下）
        if (durabilityText != null) durabilityText.text = $"{userEquipment.currentDurability}/{masterData.stats.baseDurability}";

        if (durabilitySlider != null)
        {
            durabilitySlider.gameObject.SetActive(true);
            durabilitySlider.maxValue = masterData.stats.baseDurability;
            durabilitySlider.value = userEquipment.currentDurability;

            // 耐久度に応じて色を変更
            float ratio = (float)userEquipment.currentDurability / masterData.stats.baseDurability;
            Color color = ratio > 0.6f ? Color.green : ratio > 0.3f ? Color.yellow : Color.red;

            if (durabilityText != null) durabilityText.color = color;

            var fillImage = durabilitySlider.fillRect?.GetComponent<Image>();
            if (fillImage != null) fillImage.color = color;
        }

        // デバッグ: 現在の属性攻撃値をログ出力
        Debug.Log($"装備ステータス更新 - {masterData.equipmentName}:");
        Debug.Log($"  火属性攻撃: {userEquipment.GetTotalFireAttack()} (base: {masterData.stats.baseFireAttack}, bonus: {userEquipment.bonusFireAttack})");
        Debug.Log($"  水属性攻撃: {userEquipment.GetTotalWaterAttack()} (base: {masterData.stats.baseWaterAttack}, bonus: {userEquipment.bonusWaterAttack})");
        Debug.Log($"  風属性攻撃: {userEquipment.GetTotalWindAttack()} (base: {masterData.stats.baseWindAttack}, bonus: {userEquipment.bonusWindAttack})");
        Debug.Log($"  土属性攻撃: {userEquipment.GetTotalEarthAttack()} (base: {masterData.stats.baseEarthAttack}, bonus: {userEquipment.bonusEarthAttack})");
    }

    // ★ 修正: 強化アイテム表示を更新
    private void UpdateEnhancementItemDisplay()
    {
        if (selectedEnhancementItemId < 0)
        {
            // 選択されていない場合の表示
            if (enhancementItemIcon != null)
            {
                enhancementItemIcon.sprite = null;
                enhancementItemIcon.color = new Color(1, 1, 1, 0.3f); // 薄いグレー
            }

            if (enhancementItemNameText != null)
                enhancementItemNameText.text = "強化アイテムを選択";

            if (enhancementItemQuantityText != null)
                enhancementItemQuantityText.text = "";

            if (enhancementItemEffectText != null)
            {
                enhancementItemEffectText.text = "強化アイテムを選択してください\n・強化成功時の効果が表示されます\n・強化成功率に影響します";
            }
        }
        else
        {
            var itemData = DataManager.Instance.GetEnhancementItemData(selectedEnhancementItemId);
            if (itemData != null)
            {
                // アイコン表示の修正
                if (enhancementItemIcon != null)
                {
                    enhancementItemIcon.sprite = itemData.icon;
                    enhancementItemIcon.color = Color.white; // 白色で表示
                    Debug.Log($"強化アイテムアイコン設定: {itemData.itemName}, アイコン: {itemData.icon?.name ?? "null"}");
                }

                if (enhancementItemNameText != null)
                    enhancementItemNameText.text = itemData.itemName; // アイテム名のみ表示

                if (enhancementItemQuantityText != null)
                    enhancementItemQuantityText.text = $"所持数: {DataManager.Instance.GetItemQuantity(selectedEnhancementItemId)}";

                // ★ 重要な修正: 効果テキストを表示（マスターデータの基本成功率を使用）
                if (enhancementItemEffectText != null)
                {
                    // マスターデータの基本成功率を使用（装備強化値や補助材料の影響を受けない）
                    float baseSuccessRate = itemData.successRate;
                    string effectDescription = itemData.GetEffectDescription();

                    enhancementItemEffectText.text = $"{effectDescription}\n基本成功率: {baseSuccessRate * 100:F0}%\n消耗耐久: {itemData.GetDurabilityReduction()}";
                }
            }
            else
            {
                Debug.LogError($"強化アイテムデータが見つかりません: ID {selectedEnhancementItemId}");
            }
        }
    }

    // 補助アイテム表示を更新
    private void UpdateSupportItemDisplay()
    {
        if (selectedSupportItemId < 0)
        {
            // 選択されていない場合の表示
            if (supportItemIcon != null)
            {
                supportItemIcon.sprite = null;
                supportItemIcon.color = new Color(1, 1, 1, 0.3f); // 薄いグレー
            }

            if (supportItemNameText != null)
                supportItemNameText.text = "補助アイテム（任意）";

            if (supportItemQuantityText != null)
                supportItemQuantityText.text = "";

            if (supportItemEffectText != null)
                supportItemEffectText.text = "";
        }
        else
        {
            var itemData = DataManager.Instance.GetSupportMaterialData(selectedSupportItemId);
            if (itemData != null)
            {
                // アイコン表示の修正
                if (supportItemIcon != null)
                {
                    supportItemIcon.sprite = itemData.icon;
                    supportItemIcon.color = Color.white; // 白色で表示
                    Debug.Log($"補助アイテムアイコン設定: {itemData.materialName}, アイコン: {itemData.icon?.name ?? "null"}");
                }

                if (supportItemNameText != null)
                    supportItemNameText.text = itemData.materialName; // アイテム名のみ表示

                if (supportItemQuantityText != null)
                    supportItemQuantityText.text = $"所持数: {DataManager.Instance.GetItemQuantity(selectedSupportItemId)}";

                if (supportItemEffectText != null)
                    supportItemEffectText.text = itemData.GetEffectDescription();
            }
            else
            {
                Debug.LogError($"補助アイテムデータが見つかりません: ID {selectedSupportItemId}");
            }
        }
    }

    // アイテムスロットを空に設定
    private void SetItemSlotEmpty(Image icon, TextMeshProUGUI nameText, TextMeshProUGUI quantityText, string placeholder)
    {
        if (icon != null)
        {
            icon.sprite = null;
            icon.color = new Color(1, 1, 1, 0.3f); // 半透明で表示
            icon.enabled = true; // 画像は有効のまま（背景表示用）
        }

        if (nameText != null)
        {
            nameText.text = placeholder;
        }

        if (quantityText != null)
        {
            quantityText.text = "";
        }
    }

    // 強化ボタンの状態を更新
    private void UpdateEnhanceButton()
    {
        // ★ 修正: 装備が選択されていない場合の処理を追加
        if (currentEquipmentIndex < 0)
        {
            enhanceButton.interactable = false;
            enhanceButtonText.text = "強化実行";
            successRateText.text = "装備を選択してください";
            return;
        }

        var userEquipment = DataManager.Instance.GetUserEquipment(currentEquipmentIndex);
        bool canEnhance = userEquipment != null && selectedEnhancementItemId >= 0 &&
                         userEquipment.CanEnhance() &&
                         DataManager.Instance.GetItemQuantity(selectedEnhancementItemId) > 0;

        enhanceButton.interactable = canEnhance;

        if (canEnhance)
        {
            float successRate = CalculateSuccessRate();
            enhanceButtonText.text = "強化実行";
            successRateText.text = $"成功率: {successRate * 100:F1}%";
        }
        else
        {
            enhanceButtonText.text = "強化実行";
            successRateText.text = "装備と強化アイテムを選択してください";
        }
    }

    // 成功確率計算
    private float CalculateSuccessRate()
    {
        // ★ 修正: 装備が選択されていない場合は0を返す
        if (currentEquipmentIndex < 0 || selectedEnhancementItemId < 0) return 0f;

        var enhancementItem = DataManager.Instance.GetEnhancementItemData(selectedEnhancementItemId);
        var userEquipment = DataManager.Instance.GetUserEquipment(currentEquipmentIndex);
        if (enhancementItem == null || userEquipment == null) return 0f;

        float baseRate = enhancementItem.GetAdjustedSuccessRate(userEquipment.enhancementLevel);

        // 補助アイテムのボーナス
        if (selectedSupportItemId >= 0)
        {
            var supportItem = DataManager.Instance.GetSupportMaterialData(selectedSupportItemId);
            if (supportItem != null && supportItem.materialType == "lucky_stone")
            {
                baseRate += supportItem.successRateModifier;
            }
        }

        return Mathf.Clamp01(baseRate);
    }

    // 強化ボタンクリック
    private void OnEnhanceButtonClicked()
    {
        // ★ 修正: 装備が選択されていない場合のチェックを追加
        if (currentEquipmentIndex < 0 || selectedEnhancementItemId < 0)
        {
            Debug.LogWarning("強化に必要なアイテムが選択されていません");
            return;
        }

        // 強化実行
        bool success = DataManager.Instance.EnhanceEquipment(
            currentEquipmentIndex,
            selectedEnhancementItemId,
            selectedSupportItemId >= 0 ? selectedSupportItemId : -1
        );

        Debug.Log($"強化結果: {(success ? "成功" : "失敗")}");

        // エフェクト再生
        StartCoroutine(PlayEnhancementEffect(success));

        // アイテム選択をリセット
        selectedEnhancementItemId = -1;
        selectedSupportItemId = -1;

        // 重要: UI更新を強制実行
        Debug.Log("強化処理完了後のUI更新開始");
        RefreshUI();
        Debug.Log("強化処理完了後のUI更新完了");

        // 追加: 強化後のステータス確認
        var userEquipment = DataManager.Instance.GetUserEquipment(currentEquipmentIndex);
        if (userEquipment != null)
        {
            Debug.Log($"=== 強化完了後の最終確認 ===");
            Debug.Log($"強化レベル: {userEquipment.enhancementLevel}");
            Debug.Log($"火属性攻撃: {userEquipment.GetTotalFireAttack()} (base: {userEquipment.bonusFireAttack})");
            Debug.Log($"攻撃力: {userEquipment.GetTotalAttack()}");
            Debug.Log($"==============================");
        }
    }

    // アイテム選択時のコールバック
    private void OnItemSelected(string itemId, string itemType)
    {
        Debug.Log($"アイテム選択: ID={itemId}, Type={itemType}");

        if (itemType == "equipment")
        {
            // 装備選択の場合
            if (int.TryParse(itemId, out int equipIndex))
            {
                SelectEquipment(equipIndex);
                Debug.Log($"装備選択完了: インデックス {equipIndex}");
            }
            return;
        }

        if (itemType == "support_none")
        {
            // 補助材料「選択なし」
            Debug.Log("補助材料を「選択なし」に設定");
            selectedSupportItemId = -1; // -1に設定して選択解除
            RefreshUI();
            return;
        }

        if (int.TryParse(itemId, out int id))
        {
            switch (itemType)
            {
                case "enhancement":
                    selectedEnhancementItemId = id;
                    Debug.Log($"強化アイテム選択: ID={id}");

                    // 即座にアイコンデバッグ
                    var enhanceItem = DataManager.Instance.GetEnhancementItemData(id);
                    if (enhanceItem != null)
                    {
                        Debug.Log($"強化アイテム詳細: {enhanceItem.itemName}, アイコン存在: {enhanceItem.icon != null}");
                    }
                    break;
                case "support":
                    selectedSupportItemId = id;
                    Debug.Log($"補助材料選択: ID={id}");

                    // 即座にアイコンデバッグ
                    var supportItem = DataManager.Instance.GetSupportMaterialData(id);
                    if (supportItem != null)
                    {
                        Debug.Log($"補助材料詳細: {supportItem.materialName}, アイコン存在: {supportItem.icon != null}");
                    }
                    break;
            }
        }

        RefreshUI();
        Debug.Log("UI更新完了");
    }

    // 選択キャンセル時のコールバック
    private void OnSelectionCancelled()
    {
        // 特に処理なし
    }

    // 強化エフェクト再生
    private IEnumerator PlayEnhancementEffect(bool success)
    {
        // エフェクト開始
        if (effectSpawnPoint != null)
        {
            Debug.Log(success ? "強化成功エフェクト再生" : "強化失敗エフェクト再生");
        }

        // 音声再生
        if (audioSource != null)
        {
            // ここで成功/失敗音を再生
        }

        yield return new WaitForSeconds(1f); // エフェクト時間

        Debug.Log(success ? "強化成功！" : "強化失敗...");
    }

    // 追加：ステータス表示用ヘルパーメソッド
    private void UpdateStatText(TextMeshProUGUI textComponent, string statName, float value, string suffix = "")
    {
        if (textComponent == null)
        {
            if (value > 0)
            {
                Debug.LogWarning($"{statName} の TextComponent が null です（値: {value}）");
            }
            return;
        }

        if (value > 0)
        {
            // 値が0より大きい場合のみ表示
            textComponent.text = $"{statName}: {value:F1}{suffix}";
            textComponent.gameObject.SetActive(true);
            Debug.Log($"ステータス表示更新: {statName} = {value:F1}{suffix}");
        }
        else
        {
            // 値が0以下の場合は非表示
            textComponent.gameObject.SetActive(false);
            Debug.Log($"ステータス非表示: {statName} (値: {value})");
        }
    }

    // ★ 追加: ステータステキストを空にするヘルパーメソッド
    private void SetStatTextEmpty(TextMeshProUGUI textComponent)
    {
        if (textComponent != null)
        {
            textComponent.text = "";
            textComponent.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // イベント解除
        if (itemSelectionUI != null)
        {
            itemSelectionUI.OnItemSelected -= OnItemSelected;
            itemSelectionUI.OnSelectionCancelled -= OnSelectionCancelled;
        }
    }
}