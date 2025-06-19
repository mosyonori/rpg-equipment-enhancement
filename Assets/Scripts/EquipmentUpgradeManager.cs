using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EquipmentUpgradeManager : MonoBehaviour
{
    [Header("Equipment Selection")]
    public Button equipmentSlotButton;
    public Image equipmentIcon;
    public TextMeshProUGUI equipmentNameText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI criticalRateText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI enhancementLevelText;
    public TextMeshProUGUI durabilityText;
    public Slider durabilitySlider;

    [Header("Elemental Attack Texts")]
    public TextMeshProUGUI elementalAttackText;
    public TextMeshProUGUI fireAttackText;
    public TextMeshProUGUI waterAttackText;
    public TextMeshProUGUI windAttackText;
    public TextMeshProUGUI earthAttackText;

    [Header("Enhancement Item Selection")]
    public Button enhancementItemButton;
    public Image enhancementItemIcon;
    public TextMeshProUGUI enhancementItemNameText;
    public TextMeshProUGUI enhancementItemQuantityText;
    public TextMeshProUGUI enhancementItemEffectText;

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
    public TextMeshProUGUI actualSuccessRateText;
    public GameObject warningPanel;

    [Header("Navigation Buttons")]
    public Button homeButton;  // ★追加: ホーム画面に戻るボタン

    [Header("Button Visual Settings")]
    public Color enabledTextColor = Color.red;
    public Color disabledTextColor = Color.gray;
    public float pressedScale = 0.95f;
    public float animationDuration = 0.1f;

    [Header("Elemental Restriction UI")]
    public TextMeshProUGUI elementalRestrictionText;
    public GameObject restrictionWarningPanel;
    public TextMeshProUGUI equipmentElementalTypeText;
    public TextMeshProUGUI itemElementalTypeText;

    [Header("Item Selection UI")]
    public ItemSelectionUI itemSelectionUI;

    [Header("Effect System")]
    public Transform effectSpawnPoint;
    public AudioSource audioSource;

    // 現在選択中のデータ
    private int currentEquipmentIndex = -1;
    private int selectedEnhancementItemId = -1;
    private int selectedSupportItemId = -1;

    private void Start()
    {
        SetupUI();
        RefreshUI();
    }

    private void SetupUI()
    {
        enhancementItemButton?.onClick.AddListener(() => ShowItemSelection("enhancement"));
        supportItemButton?.onClick.AddListener(() => ShowItemSelection("support"));
        enhanceButton?.onClick.AddListener(OnEnhanceButtonClicked);
        equipmentSlotButton?.onClick.AddListener(() => ShowItemSelection("equipment"));

        // ★追加: ホームボタンの設定
        homeButton?.onClick.AddListener(OnHomeButtonClicked);

        if (itemSelectionUI != null)
        {
            itemSelectionUI.OnItemSelected += OnItemSelected;
            itemSelectionUI.OnSelectionCancelled += OnSelectionCancelled;
        }

        // 強化ボタンにプレス効果を追加
        SetupEnhanceButtonEffect();
    }

    /// <summary>
    /// ホームボタンクリック時の処理
    /// </summary>
    private void OnHomeButtonClicked()
    {
        // GameSceneManagerを探してホーム画面に遷移
        var sceneManager = Object.FindFirstObjectByType<GameSceneManager>();
        if (sceneManager != null)
        {
            sceneManager.LoadHomeScene();
        }
        else
        {
            Debug.LogWarning("GameSceneManagerが見つかりません。ホーム画面への遷移ができませんでした。");
        }
    }

    /// <summary>
    /// 強化ボタンにプレス効果を設定
    /// </summary>
    private void SetupEnhanceButtonEffect()
    {
        if (enhanceButton == null) return;

        // ボタンのEventTriggerを取得または追加
        var eventTrigger = enhanceButton.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = enhanceButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        // PointerDown イベント（ボタンを押した時）
        var pointerDownEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerDownEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((eventData) => { OnButtonPressed(); });
        eventTrigger.triggers.Add(pointerDownEntry);

        // PointerUp イベント（ボタンを離した時）
        var pointerUpEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerUpEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((eventData) => { OnButtonReleased(); });
        eventTrigger.triggers.Add(pointerUpEntry);

        // PointerExit イベント（ボタンから離れた時）
        var pointerExitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerExitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        pointerExitEntry.callback.AddListener((eventData) => { OnButtonReleased(); });
        eventTrigger.triggers.Add(pointerExitEntry);
    }

    /// <summary>
    /// ボタンが押された時のアニメーション
    /// </summary>
    private void OnButtonPressed()
    {
        if (enhanceButton != null && enhanceButton.interactable)
        {
            StartCoroutine(ScaleButton(pressedScale));
        }
    }

    /// <summary>
    /// ボタンが離された時のアニメーション
    /// </summary>
    private void OnButtonReleased()
    {
        if (enhanceButton != null)
        {
            StartCoroutine(ScaleButton(1f));
        }
    }

    /// <summary>
    /// ボタンのスケールアニメーション
    /// </summary>
    private IEnumerator ScaleButton(float targetScale)
    {
        if (enhanceButton == null) yield break;

        Vector3 startScale = enhanceButton.transform.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // イージング効果を追加（滑らかなアニメーション）
            t = Mathf.SmoothStep(0f, 1f, t);

            enhanceButton.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        enhanceButton.transform.localScale = endScale;
    }

    private void ShowItemSelection(string itemType)
    {
        if (itemSelectionUI == null) return;

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
        }
    }

    public void SelectEquipment(int equipmentIndex)
    {
        currentEquipmentIndex = equipmentIndex;
        selectedEnhancementItemId = -1;
        selectedSupportItemId = -1;
        RefreshUI();

        // ★追加: 装備選択時に強化アイテム表示も更新
        UpdateEnhancementItemDisplayAfterEquipmentChange();
    }

    /// <summary>
    /// 装備変更後に強化アイテムの表示を更新
    /// </summary>
    private void UpdateEnhancementItemDisplayAfterEquipmentChange()
    {
        // 強化アイテムが選択されている場合は表示を更新
        if (selectedEnhancementItemId >= 0)
        {
            UpdateEnhancementItemDisplay();
        }
    }

    // 既存互換性保持用
    public void SelectEquipment(EquipmentData equipData, UserEquipment userEquip)
    {
        var equipmentList = DataManager.Instance.GetAllUserEquipments();
        for (int i = 0; i < equipmentList.Count; i++)
        {
            if (equipmentList[i] == userEquip)
            {
                SelectEquipment(i);
                return;
            }
        }
    }

    public void SelectUpgradeItem(EnhancementItemData itemData)
    {
        selectedEnhancementItemId = itemData.itemId;
        RefreshUI();
    }

    public void SelectSupportItem(SupportMaterialData materialData)
    {
        selectedSupportItemId = materialData.materialId;
        RefreshUI();
    }

    public void DeselectSupportItem()
    {
        selectedSupportItemId = -1;
        RefreshUI();
    }

    private void RefreshUI()
    {
        UpdateEquipmentDisplay();
        UpdateEnhancementItemDisplay();
        UpdateSupportItemDisplay();
        UpdateElementalRestrictionDisplay();
        UpdateEnhanceButton();

        // ★追加: レイアウト更新の強制実行
        Canvas.ForceUpdateCanvases();

        // さらに確実にするため1フレーム後にも更新
        StartCoroutine(ForceLayoutUpdateNextFrame());
    }

    /// <summary>
    /// 次フレームでレイアウト強制更新
    /// </summary>
    private System.Collections.IEnumerator ForceLayoutUpdateNextFrame()
    {
        yield return null; // 1フレーム待機
        Canvas.ForceUpdateCanvases();

        // より強力な更新処理
        ForceRebuildLayout();
    }

    /// <summary>
    /// Layout Groupの強制リビルド
    /// </summary>
    private void ForceRebuildLayout()
    {
        // 装備エリア全体のLayout Groupを取得して強制更新
        var layoutGroups = GetComponentsInChildren<UnityEngine.UI.LayoutGroup>();
        foreach (var layoutGroup in layoutGroups)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
        }

        // Content Size Fitterも強制更新
        var contentSizeFitters = GetComponentsInChildren<UnityEngine.UI.ContentSizeFitter>();
        foreach (var fitter in contentSizeFitters)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(fitter.GetComponent<RectTransform>());
        }
    }

    #region 装備表示

    private void UpdateEquipmentDisplay()
    {
        if (currentEquipmentIndex < 0)
        {
            SetEquipmentEmptyDisplay();
            return;
        }

        var userEquipment = DataManager.Instance?.GetUserEquipment(currentEquipmentIndex);
        if (userEquipment == null) return;

        var masterData = DataManager.Instance.GetEquipmentData(userEquipment.equipmentId);
        if (masterData == null) return;

        // 基本情報
        SetIcon(equipmentIcon, masterData.icon);
        SetText(equipmentNameText, masterData.equipmentName);
        SetText(enhancementLevelText, $"強化値：+{userEquipment.enhancementLevel}");

        // ステータス
        UpdateStatText(attackText, "攻撃力", userEquipment.GetTotalAttack());
        UpdateStatText(defenseText, "防御力", userEquipment.GetTotalDefense());
        UpdateStatText(criticalRateText, "クリティカル率", userEquipment.GetTotalCriticalRate(), "%");
        UpdateStatText(hpText, "HP", userEquipment.GetTotalHP());

        // 属性攻撃
        UpdateStatText(elementalAttackText, "属性攻撃", userEquipment.GetTotalElementalAttack());
        UpdateStatText(fireAttackText, "火属性攻撃", userEquipment.GetTotalFireAttack());
        UpdateStatText(waterAttackText, "水属性攻撃", userEquipment.GetTotalWaterAttack());
        UpdateStatText(windAttackText, "風属性攻撃", userEquipment.GetTotalWindAttack());
        UpdateStatText(earthAttackText, "土属性攻撃", userEquipment.GetTotalEarthAttack());

        UpdateDurabilityDisplay(userEquipment, masterData);
        UpdateEquipmentElementalDisplay(userEquipment);
    }

    private void SetEquipmentEmptyDisplay()
    {
        SetIcon(equipmentIcon, null);
        SetText(equipmentNameText, "装備を選択");
        SetText(enhancementLevelText, "");

        SetStatTextEmpty(attackText);
        SetStatTextEmpty(defenseText);
        SetStatTextEmpty(criticalRateText);
        SetStatTextEmpty(hpText);
        SetStatTextEmpty(elementalAttackText);
        SetStatTextEmpty(fireAttackText);
        SetStatTextEmpty(waterAttackText);
        SetStatTextEmpty(windAttackText);
        SetStatTextEmpty(earthAttackText);

        SetText(durabilityText, "");
        SetSliderActive(durabilitySlider, false);
        SetTextActive(equipmentElementalTypeText, false);
    }

    private void UpdateDurabilityDisplay(UserEquipment userEquipment, EquipmentData masterData)
    {
        SetText(durabilityText, $"強化耐久：{userEquipment.currentDurability}/{masterData.stats.baseDurability}");
        SetSliderActive(durabilitySlider, true);

        if (durabilitySlider != null)
        {
            durabilitySlider.maxValue = masterData.stats.baseDurability;
            durabilitySlider.value = userEquipment.currentDurability;

            float ratio = (float)userEquipment.currentDurability / masterData.stats.baseDurability;
            Color color = ratio > 0.6f ? Color.green : ratio > 0.3f ? Color.yellow : Color.red;

            SetTextColor(durabilityText, color);
            var fillImage = durabilitySlider.fillRect?.GetComponent<Image>();
            if (fillImage != null) fillImage.color = color;
        }
    }

    private void UpdateEquipmentElementalDisplay(UserEquipment userEquipment)
    {
        if (equipmentElementalTypeText == null) return;

        ElementalType currentType = userEquipment.GetCurrentElementalType();
        string typeName = UserEquipment.GetElementalTypeName(currentType);

        if (currentType == ElementalType.None)
        {
            SetText(equipmentElementalTypeText, "無属性");
            SetTextColor(equipmentElementalTypeText, Color.gray);
        }
        else
        {
            SetText(equipmentElementalTypeText, $"{typeName}属性");
            SetTextColor(equipmentElementalTypeText, GetElementalTypeColor(currentType));
        }

        SetTextActive(equipmentElementalTypeText, true);
    }

    #endregion

    #region 強化アイテム表示

    private void UpdateEnhancementItemDisplay()
    {
        if (selectedEnhancementItemId < 0)
        {
            SetIcon(enhancementItemIcon, null);
            SetText(enhancementItemNameText, "強化アイテムを選択");
            SetText(enhancementItemQuantityText, "");
            SetText(enhancementItemEffectText, null);
            UpdateItemElementalDisplay(null);
        }
        else
        {
            var itemData = DataManager.Instance.GetEnhancementItemData(selectedEnhancementItemId);
            if (itemData != null)
            {
                SetIcon(enhancementItemIcon, itemData.icon);
                SetText(enhancementItemNameText, itemData.itemName);
                SetText(enhancementItemQuantityText, $"所持数: {DataManager.Instance.GetItemQuantity(selectedEnhancementItemId)}");

                // ★修正: 装備選択状態に応じて効果表示を変更
                string effectText = GetEnhancementItemEffectText(itemData);
                SetText(enhancementItemEffectText, effectText);

                UpdateItemElementalDisplay(itemData);
            }
        }
    }

    /// <summary>
    /// 強化アイテムの効果テキストを装備選択状態に応じて生成
    /// </summary>
    private string GetEnhancementItemEffectText(EnhancementItemData itemData)
    {
        var userEquipment = DataManager.Instance.GetUserEquipment(currentEquipmentIndex);

        if (userEquipment == null)
        {
            // 装備が選択されていない場合は基本説明を表示
            return itemData.description;
        }

        // 装備が選択されている場合は、その装備種類に応じた効果を表示
        var equipmentData = DataManager.Instance.GetEquipmentData(userEquipment.equipmentId);
        if (equipmentData == null)
        {
            return itemData.description;
        }

        // 装備種類に応じた効果を取得
        string equipmentTypeEffect;
        int durabilityReduction;

        if (itemData.useEquipmentTypeSpecificBonus)
        {
            equipmentTypeEffect = itemData.GetEffectDescriptionForEquipmentType(equipmentData.equipmentType);
            durabilityReduction = itemData.GetDurabilityReduction(equipmentData.equipmentType);
        }
        else
        {
            equipmentTypeEffect = itemData.GetEffectDescription();
            durabilityReduction = itemData.GetDurabilityReduction();
        }

        // 成功率と耐久消費情報を追加
        float successRate = itemData.GetAdjustedSuccessRate(userEquipment.enhancementLevel);

        return $"{equipmentTypeEffect}\n" +
               $"成功率: {successRate * 100:F1}%\n" +
               $"消費耐久: {durabilityReduction}";
    }

    private void UpdateItemElementalDisplay(EnhancementItemData itemData)
    {
        if (itemElementalTypeText == null) return;

        if (itemData == null)
        {
            SetTextActive(itemElementalTypeText, false);
            return;
        }

        // 装備種類を考慮した属性判定
        var userEquipment = DataManager.Instance.GetUserEquipment(currentEquipmentIndex);
        EquipmentType equipmentType = EquipmentType.Weapon;

        if (userEquipment != null)
        {
            var equipmentData = DataManager.Instance.GetEquipmentData(userEquipment.equipmentId);
            equipmentType = equipmentData?.equipmentType ?? EquipmentType.Weapon;
        }

        ElementalType itemType = itemData.GetElementalType(equipmentType);
        string typeName = UserEquipment.GetElementalTypeName(itemType);

        if (itemType == ElementalType.None)
        {
            SetText(itemElementalTypeText, "無属性強化");
            SetTextColor(itemElementalTypeText, Color.gray);
        }
        else
        {
            SetText(itemElementalTypeText, $"{typeName}属性強化");
            SetTextColor(itemElementalTypeText, GetElementalTypeColor(itemType));
        }

        SetTextActive(itemElementalTypeText, true);
    }

    #endregion

    #region 補助アイテム表示

    private void UpdateSupportItemDisplay()
    {
        if (selectedSupportItemId < 0)
        {
            SetIcon(supportItemIcon, null);
            SetText(supportItemNameText, "補助材料を選択（任意）");
            SetText(supportItemQuantityText, "");
            SetText(supportItemEffectText, "");
        }
        else
        {
            var itemData = DataManager.Instance.GetSupportMaterialData(selectedSupportItemId);
            if (itemData != null)
            {
                SetIcon(supportItemIcon, itemData.icon);
                SetText(supportItemNameText, itemData.materialName);
                SetText(supportItemQuantityText, $"所持数: {DataManager.Instance.GetItemQuantity(selectedSupportItemId)}");
                SetText(supportItemEffectText, itemData.GetEffectDescription());
            }
        }
    }

    #endregion

    #region 属性制限表示

    private void UpdateElementalRestrictionDisplay()
    {
        // 属性制限表示は装備とアイテムの属性表示のみで十分
        // 制限違反時の特別な表示は行わない
    }

    #endregion

    #region 強化ボタン

    private void UpdateEnhanceButton()
    {
        var userEquipment = DataManager.Instance.GetUserEquipment(currentEquipmentIndex);
        var enhancementItem = DataManager.Instance.GetEnhancementItemData(selectedEnhancementItemId);

        bool canEnhance = currentEquipmentIndex >= 0 && userEquipment != null &&
                         selectedEnhancementItemId >= 0 && enhancementItem != null &&
                         userEquipment.CanEnhance() &&
                         DataManager.Instance.GetItemQuantity(selectedEnhancementItemId) > 0 &&
                         enhancementItem.CanUseOnEquipment(userEquipment);

        SetButtonInteractable(enhanceButton, canEnhance);

        string warningMessage = GetWarningMessage(userEquipment, enhancementItem);
        bool shouldShowWarning = !string.IsNullOrEmpty(warningMessage);

        SetText(successRateText, warningMessage);
        SetActive(warningPanel, shouldShowWarning);

        if (actualSuccessRateText != null)
        {
            float successRate = canEnhance ? CalculateSuccessRate() : 0f;
            SetText(actualSuccessRateText, $"成功率: {successRate * 100:F1}%");
        }

        // ボタンテキストの色を変更
        UpdateButtonTextColor(canEnhance);

        SetText(enhanceButtonText, "強化実行");
    }

    /// <summary>
    /// ボタンテキストの色を更新
    /// </summary>
    private void UpdateButtonTextColor(bool canEnhance)
    {
        if (enhanceButtonText != null)
        {
            Color targetColor = canEnhance ? enabledTextColor : disabledTextColor;
            enhanceButtonText.color = targetColor;
        }
    }

    private string GetWarningMessage(UserEquipment userEquipment, EnhancementItemData enhancementItem)
    {
        if (currentEquipmentIndex < 0 && selectedEnhancementItemId < 0)
            return "装備と強化アイテムを選択してください";

        if (currentEquipmentIndex < 0)
            return "装備を選択してください";

        if (selectedEnhancementItemId < 0)
            return "強化アイテムを選択してください";

        if (userEquipment != null && enhancementItem != null && !enhancementItem.CanUseOnEquipment(userEquipment))
            return "属性制限により使用できません";

        if (userEquipment != null && !userEquipment.CanEnhance())
            return "耐久度が不足しています";

        return "";
    }

    private float CalculateSuccessRate()
    {
        if (currentEquipmentIndex < 0 || selectedEnhancementItemId < 0) return 0f;

        var enhancementItem = DataManager.Instance.GetEnhancementItemData(selectedEnhancementItemId);
        var userEquipment = DataManager.Instance.GetUserEquipment(currentEquipmentIndex);
        if (enhancementItem == null || userEquipment == null) return 0f;

        float baseRate = enhancementItem.GetAdjustedSuccessRate(userEquipment.enhancementLevel);

        if (selectedSupportItemId >= 0)
        {
            var supportItem = DataManager.Instance.GetSupportMaterialData(selectedSupportItemId);
            if (supportItem != null)
            {
                baseRate += supportItem.successRateModifier;
                if (supportItem.guaranteeSuccess)
                    baseRate = 1.0f;
            }
        }

        return Mathf.Clamp01(baseRate);
    }

    #endregion

    #region イベント処理

    private void OnEnhanceButtonClicked()
    {
        if (currentEquipmentIndex < 0 || selectedEnhancementItemId < 0) return;

        // 強化前の状態をログ出力
        Debug.Log($"強化実行: 装備インデックス={currentEquipmentIndex}, 強化アイテムID={selectedEnhancementItemId}, 補助アイテムID={selectedSupportItemId}");

        // 属性制限チェックを含む強化実行
        bool success = DataManager.Instance.EnhanceEquipmentWithElementalCheck(
            currentEquipmentIndex,
            selectedEnhancementItemId,
            selectedSupportItemId >= 0 ? selectedSupportItemId : -1
        );

        // ★重要: 強化実行後は成功/失敗に関わらず、必ずアイテム選択をリセット
        Debug.Log($"強化結果: {(success ? "成功" : "失敗")} - アイテム選択をリセット");
        selectedEnhancementItemId = -1;
        selectedSupportItemId = -1;

        // エフェクト再生
        if (success)
        {
            StartCoroutine(PlayEnhancementEffect(success));
        }

        // UI更新
        RefreshUI();

        // 強化後の状態確認
        Debug.Log($"UI更新後: 強化アイテム選択={selectedEnhancementItemId}, 補助アイテム選択={selectedSupportItemId}");
    }

    private void OnItemSelected(string itemId, string itemType)
    {
        if (itemType == "equipment")
        {
            if (int.TryParse(itemId, out int equipIndex))
                SelectEquipment(equipIndex);
            return;
        }

        if (itemType == "support_none")
        {
            selectedSupportItemId = -1;
            RefreshUI();
            return;
        }

        if (int.TryParse(itemId, out int id))
        {
            switch (itemType)
            {
                case "enhancement":
                    selectedEnhancementItemId = id;
                    break;
                case "support":
                    selectedSupportItemId = id;
                    break;
            }
        }

        RefreshUI();
    }

    private void OnSelectionCancelled()
    {
        // 特に処理なし
    }

    private IEnumerator PlayEnhancementEffect(bool success)
    {
        // エフェクト・音声再生処理
        yield return new WaitForSeconds(1f);
    }

    #endregion

    #region ユーティリティメソッド

    private void UpdateStatText(TextMeshProUGUI textComponent, string statName, float value, string suffix = "")
    {
        if (textComponent == null) return;

        if (value > 0)
        {
            SetText(textComponent, $"{statName}: {value:F1}{suffix}");
            SetTextActive(textComponent, true);
        }
        else
        {
            SetTextActive(textComponent, false);
        }
    }

    private void SetStatTextEmpty(TextMeshProUGUI textComponent)
    {
        if (textComponent != null)
        {
            SetText(textComponent, "");
            SetTextActive(textComponent, false);
        }
    }

    private Color GetElementalTypeColor(ElementalType type)
    {
        switch (type)
        {
            case ElementalType.Fire: return new Color(1f, 0.3f, 0.3f);
            case ElementalType.Water: return new Color(0.3f, 0.7f, 1f);
            case ElementalType.Wind: return new Color(0.7f, 1f, 0.7f);
            case ElementalType.Earth: return new Color(0.8f, 0.6f, 0.3f);
            default: return Color.gray;
        }
    }

    // UI操作ヘルパーメソッド
    private void SetText(TextMeshProUGUI text, string value) => text?.SetText(value);
    private void SetTextColor(TextMeshProUGUI text, Color color) { if (text != null) text.color = color; }
    private void SetTextActive(TextMeshProUGUI text, bool active) => text?.gameObject.SetActive(active);
    private void SetActive(GameObject obj, bool active) => obj?.SetActive(active);
    private void SetButtonInteractable(Button button, bool interactable) { if (button != null) button.interactable = interactable; }
    private void SetSliderActive(Slider slider, bool active) => slider?.gameObject.SetActive(active);

    private void SetIcon(Image icon, Sprite sprite)
    {
        if (icon == null) return;

        if (sprite != null)
        {
            icon.sprite = sprite;
            icon.enabled = true;
            icon.color = Color.white;
        }
        else
        {
            icon.sprite = null;
            icon.enabled = false;
        }
    }

    #endregion

    private void OnDestroy()
    {
        if (itemSelectionUI != null)
        {
            itemSelectionUI.OnItemSelected -= OnItemSelected;
            itemSelectionUI.OnSelectionCancelled -= OnSelectionCancelled;
        }
    }
}