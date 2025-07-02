using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// スキル選択ポップアップUI
/// EquipmentSelectionPopupと同様の構造でスキル選択機能を提供
/// 戦闘用スキルと装備用スキル両方に対応
/// </summary>
public class SkillSelectionPopup : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI confirmButtonText;
    [SerializeField] private Transform skillGridParent;
    [SerializeField] private GameObject skillSlotPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("スキル解除ボタン設定")]
    [SerializeField] private GameObject removeSkillSlotPrefab;

    [Header("詳細ステータス表示")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private TextMeshProUGUI selectedSkillNameText;
    [SerializeField] private TextMeshProUGUI selectedSkillTypeText;
    [SerializeField] private TextMeshProUGUI selectedSkillAttributeText;
    [SerializeField] private TextMeshProUGUI selectedSkillRarityText;
    [SerializeField] private TextMeshProUGUI selectedSkillDamageText;
    [SerializeField] private TextMeshProUGUI selectedSkillTargetText;
    [SerializeField] private TextMeshProUGUI selectedSkillCoolTimeText;
    [SerializeField] private TextMeshProUGUI selectedSkillHpCostText;
    [SerializeField] private TextMeshProUGUI selectedSkillMpCostText;
    [SerializeField] private TextMeshProUGUI selectedSkillDescriptionText;

    [Header("ボタンテキスト色設定")]
    [SerializeField] private Color enabledTextColor = Color.white;
    [SerializeField] private Color disabledTextColor = Color.gray;

    [Header("デバッグ")]
    [SerializeField] private bool enableDebugLog = true;

    // イベント
    public System.Action<UserSkillData> OnSkillSelected;
    public System.Action OnSkillRemoved;
    public System.Action OnPopupClosed;

    // 内部状態
    private string currentEquipmentId;
    private UserSkillData selectedSkill;
    private List<SkillSlotUI> skillSlots = new List<SkillSlotUI>();
    private GameObject removeSkillSlot; // Grid内のスキル解除ボタン

    #region Unity Lifecycle

    private void Awake()
    {
        SetupButtons();
        HidePopup();
    }

    #endregion

    #region 初期化

    private void SetupButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePopup);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmSelection);
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// スキル選択ポップアップを表示
    /// </summary>
    public void ShowSkillSelection(string equipmentIdOrSlotId)
    {
        currentEquipmentId = equipmentIdOrSlotId;
        selectedSkill = null;

        // 他のポップアップを確実に非表示にする
        EnsureOtherPopupsHidden();

        // タイトル設定
        UpdateTitle();

        // スキルリスト表示
        DisplaySkillList();

        // 詳細パネル初期化
        HideDetailsPanel();

        // ポップアップ表示
        ShowPopup();

        DebugLog($"スキル選択ポップアップを表示: ID={equipmentIdOrSlotId}");
    }

    /// <summary>
    /// 他のポップアップを確実に非表示にする
    /// </summary>
    private void EnsureOtherPopupsHidden()
    {
        // EquipmentSelectionPopupを非表示にする
        var equipmentPopup = FindFirstObjectByType<EquipmentSelectionPopup>();
        if (equipmentPopup != null)
        {
            equipmentPopup.HidePopup();
        }

        // 他の選択UI要素もクリアする
        ClearPreviousSelections();
    }

    /// <summary>
    /// 前回の選択状態をクリア
    /// </summary>
    private void ClearPreviousSelections()
    {
        // Grid内の全ての子オブジェクトを一旦削除
        if (skillGridParent != null)
        {
            for (int i = skillGridParent.childCount - 1; i >= 0; i--)
            {
                var child = skillGridParent.GetChild(i);
                if (child != null)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        // スロットリストもクリア
        skillSlots.Clear();

        // 削除用スロットもクリア
        if (removeSkillSlot != null)
        {
            DestroyImmediate(removeSkillSlot);
            removeSkillSlot = null;
        }

        DebugLog("前回の選択状態をクリアしました");
    }

    /// <summary>
    /// ポップアップを非表示
    /// </summary>
    public void HidePopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        selectedSkill = null;
        HideDetailsPanel();
        OnPopupClosed?.Invoke();

        DebugLog("スキル選択ポップアップを非表示");
    }

    /// <summary>
    /// 現在表示中の装備IDまたはスロットIDを取得
    /// </summary>
    public string GetCurrentEquipmentId()
    {
        return currentEquipmentId;
    }

    #endregion

    #region 内部メソッド

    private void ShowPopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }
    }

    private void UpdateTitle()
    {
        if (titleText == null) return;

        titleText.text = "スキルを選択";
    }

    private void DisplaySkillList()
    {
        if (!IsManagersReady()) return;

        // 既存のアイテムをクリア
        ClearAllSlots();

        // スキル解除ボタンを最初に作成
        CreateRemoveSkillSlot();

        // 利用可能なスキル一覧を取得（戦闘用スキル選択の場合は、現在装備されていないスキル）
        var availableSkills = GetAvailableSkillsForSelection();

        DebugLog($"表示可能スキル数: {availableSkills.Count}");

        // スキルアイテムスロットを作成
        CreateSkillSlots(availableSkills);

        // ボタン状態更新
        UpdateButtonStates();
    }

    /// <summary>
    /// 選択用の利用可能スキル一覧を取得
    /// </summary>
    private List<UserSkillData> GetAvailableSkillsForSelection()
    {
        var allSkills = SkillManager.Instance.GetAllSkills();

        if (IsBattleSkillSlot())
        {
            // 戦闘用スキルスロットの場合：現在戦闘用に装備されていないスキルを取得
            var saveData = SaveDataManager.Instance?.CurrentSaveData;
            if (saveData == null) return new List<UserSkillData>();

            return allSkills.Where(skill =>
                !saveData.IsBattleSkillEquipped(skill.userSkillId)
            ).ToList();
        }
        else
        {
            // 装備用スキルの場合：装備に付属しないスキルを取得（従来の実装）
            return SkillManager.Instance.GetAvailableSkills();
        }
    }

    /// <summary>
    /// 戦闘用スキルスロットかどうかを判定
    /// </summary>
    private bool IsBattleSkillSlot()
    {
        return currentEquipmentId != null && currentEquipmentId.StartsWith("battle_skill_slot_");
    }

    /// <summary>
    /// 全スロットをクリア（安全版）
    /// </summary>
    private void ClearAllSlots()
    {
        // スキルスロットを破壊
        foreach (var slot in skillSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                DestroyImmediate(slot.gameObject);
            }
        }
        skillSlots.Clear();

        // スキル解除スロットを破壊
        if (removeSkillSlot != null)
        {
            DestroyImmediate(removeSkillSlot);
            removeSkillSlot = null;
        }

        // グリッド内の全ての子オブジェクトを安全に削除
        ClearAllGridChildrenSafe();

        DebugLog("全スロットをクリアしました");
    }

    /// <summary>
    /// グリッド内の全ての子オブジェクトを安全に削除
    /// </summary>
    private void ClearAllGridChildrenSafe()
    {
        if (skillGridParent == null) return;

        // 子オブジェクトのリストを事前に取得
        List<Transform> childrenToDestroy = new List<Transform>();

        for (int i = 0; i < skillGridParent.childCount; i++)
        {
            var child = skillGridParent.GetChild(i);
            if (child != null)
            {
                childrenToDestroy.Add(child);
            }
        }

        // 事前に取得したリストから安全に削除
        foreach (var child in childrenToDestroy)
        {
            if (child != null && child.gameObject != null)
            {
                // デバッグログは削除前に出力
                string childName = child.name;
                string componentType = "Unknown";

                var skillSlot = child.GetComponent<SkillSlotUI>();
                var equipmentSlot = child.GetComponent<EquipmentSlotUI>();

                if (skillSlot != null)
                    componentType = "SkillSlotUI";
                else if (equipmentSlot != null)
                    componentType = "EquipmentSlotUI";
                else
                    componentType = "Other";

                DebugLog($"グリッド子オブジェクトを削除: {childName} - {componentType}");

                // オブジェクトを削除
                DestroyImmediate(child.gameObject);
            }
        }

        DebugLog($"グリッド内の{childrenToDestroy.Count}個のオブジェクトを削除しました");
    }

    /// <summary>
    /// スキル解除ボタンスロットを作成（Grid内の最初の位置）
    /// </summary>
    private void CreateRemoveSkillSlot()
    {
        if (removeSkillSlotPrefab == null)
        {
            DebugLogError("スキル解除スロットプレハブが設定されていません");
            return;
        }

        // スキル解除スロットを生成
        removeSkillSlot = Instantiate(removeSkillSlotPrefab, skillGridParent);

        // 最初の位置に配置
        removeSkillSlot.transform.SetSiblingIndex(0);

        // Layout Elementを追加してGrid Layoutに参加させる
        LayoutElement layoutElement = removeSkillSlot.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = removeSkillSlot.AddComponent<LayoutElement>();
        }
        // ignoreLayout = false にして Grid Layout に参加させる
        layoutElement.ignoreLayout = false;

        // Grid Layout Groupのセルサイズに合わせる（適正サイズとして設定）
        GridLayoutGroup gridLayout = skillGridParent.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            layoutElement.preferredWidth = gridLayout.cellSize.x;
            layoutElement.preferredHeight = gridLayout.cellSize.y;
        }

        // ボタンイベントを設定
        Button removeButton = removeSkillSlot.GetComponent<Button>();
        if (removeButton != null)
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(RemoveSkill);
        }

        DebugLog("スキル解除スロットを作成しました（Grid内最初の位置、Grid Layoutに参加）");
    }

    /// <summary>
    /// スキルアイテムスロットを作成
    /// </summary>
    private void CreateSkillSlots(List<UserSkillData> availableSkills)
    {
        foreach (var skill in availableSkills)
        {
            GameObject newSlot = Instantiate(skillSlotPrefab, skillGridParent);
            SkillSlotUI slotUI = newSlot.GetComponent<SkillSlotUI>();

            if (slotUI != null)
            {
                slotUI.SetSkillData(skill);
                slotUI.OnSlotClicked = OnSkillSlotClicked;
                slotUI.SetSelected(false);
                skillSlots.Add(slotUI);
            }
        }

        DebugLog($"スキルスロットを{availableSkills.Count}個作成しました");
    }

    private void OnSkillSlotClicked(UserSkillData skill)
    {
        selectedSkill = skill;

        // 選択状態の見た目更新
        UpdateSelectionVisual();

        // 詳細ステータス表示更新（新規追加）
        UpdateDetailsPanel();

        // ボタン状態更新
        UpdateButtonStates();

        DebugLog($"スキルが選択されました: {skill?.userSkillId ?? "null"}");
    }

    /// <summary>
    /// 詳細ステータスパネルを更新（新規追加）
    /// </summary>
    private void UpdateDetailsPanel()
    {
        if (selectedSkill == null)
        {
            HideDetailsPanel();
            return;
        }

        var masterData = MasterDataManager.Instance?.GetSkillData(selectedSkill.skillMasterId);
        if (masterData == null)
        {
            DebugLogError($"スキルマスターデータが見つかりません: {selectedSkill.skillMasterId}");
            HideDetailsPanel();
            return;
        }

        ShowDetailsPanel();

        // 基本情報表示
        UpdateBasicInfo(masterData);

        // 詳細ステータス表示
        UpdateDetailedStats(masterData);

        DebugLog($"詳細ステータス表示を更新: {masterData.skillName}");
    }

    /// <summary>
    /// 基本情報を更新
    /// </summary>
    private void UpdateBasicInfo(SkillMasterData masterData)
    {
        // スキル名
        if (selectedSkillNameText != null)
        {
            selectedSkillNameText.text = masterData.skillName;
        }

        // スキルタイプ
        if (selectedSkillTypeText != null)
        {
            selectedSkillTypeText.text = GetSkillTypeDisplayName(masterData.skillType);
        }

        // 属性
        if (selectedSkillAttributeText != null)
        {
            selectedSkillAttributeText.text = GetAttributeDisplayName(masterData.attributeType);
        }

        // レアリティ
        if (selectedSkillRarityText != null)
        {
            selectedSkillRarityText.text = GetRarityDisplayName(masterData.rarity);
        }
    }

    /// <summary>
    /// 詳細ステータスを更新
    /// </summary>
    private void UpdateDetailedStats(SkillMasterData masterData)
    {
        // ダメージ倍率
        if (selectedSkillDamageText != null)
        {
            selectedSkillDamageText.text = $"{masterData.skillDamageMultiplier:F1}倍";
        }

        // ターゲット
        if (selectedSkillTargetText != null)
        {
            selectedSkillTargetText.text = GetTargetTypeDisplayName(masterData.skillTargetType);
        }

        // クールタイム
        if (selectedSkillCoolTimeText != null)
        {
            selectedSkillCoolTimeText.text = $"{masterData.skillMaxCoolTime}ターン";
        }

        // HPコスト
        if (selectedSkillHpCostText != null)
        {
            selectedSkillHpCostText.text = masterData.skillHpCost.ToString();
        }

        // MPコスト
        if (selectedSkillMpCostText != null)
        {
            selectedSkillMpCostText.text = masterData.skillMpCost.ToString();
        }

        // 説明文
        if (selectedSkillDescriptionText != null)
        {
            selectedSkillDescriptionText.text = masterData.description;
        }
    }

    /// <summary>
    /// 詳細パネルを表示
    /// </summary>
    private void ShowDetailsPanel()
    {
        if (detailsPanel != null)
        {
            detailsPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 詳細パネルを非表示
    /// </summary>
    private void HideDetailsPanel()
    {
        if (detailsPanel != null)
        {
            detailsPanel.SetActive(false);
        }
    }

    private void UpdateSelectionVisual()
    {
        foreach (var slot in skillSlots)
        {
            if (slot.gameObject.activeInHierarchy)
            {
                bool isSelected = selectedSkill != null &&
                    slot.GetSkillData()?.userSkillId == selectedSkill.userSkillId;
                slot.SetSelected(isSelected);
            }
        }
    }

    private void UpdateButtonStates()
    {
        bool hasSelection = selectedSkill != null;

        // Confirmボタンのテキスト色を変更（ボタン自体は常に有効）
        if (confirmButton != null)
        {
            confirmButton.interactable = true; // 常に有効

            if (confirmButtonText != null)
            {
                confirmButtonText.color = hasSelection ? enabledTextColor : disabledTextColor;
            }
        }

        // Grid内のスキル解除ボタンの状態を更新
        UpdateRemoveButtonState();
    }

    /// <summary>
    /// スキル解除ボタンの状態を更新
    /// </summary>
    private void UpdateRemoveButtonState()
    {
        bool hasEquippedSkill = HasEquippedSkill();

        // Grid内のボタン
        if (removeSkillSlot != null)
        {
            Button gridRemoveButton = removeSkillSlot.GetComponent<Button>();
            if (gridRemoveButton != null)
            {
                gridRemoveButton.interactable = hasEquippedSkill;

                // ボタンの見た目を更新
                Image buttonImage = gridRemoveButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = hasEquippedSkill ? Color.white : Color.gray;
                }
            }
        }
    }

    private bool HasEquippedSkill()
    {
        if (string.IsNullOrEmpty(currentEquipmentId)) return false;

        if (IsBattleSkillSlot())
        {
            // 戦闘用スキルスロットの場合
            var saveData = SaveDataManager.Instance?.CurrentSaveData;
            if (saveData == null) return false;

            if (currentEquipmentId == "battle_skill_slot_1")
            {
                return !string.IsNullOrEmpty(saveData.battleSkill1Id);
            }
            else if (currentEquipmentId == "battle_skill_slot_2")
            {
                return !string.IsNullOrEmpty(saveData.battleSkill2Id);
            }
            return false;
        }
        else
        {
            // 装備に付属するスキルの場合
            var equippedSkill = SkillManager.Instance.GetEquippedSkill(currentEquipmentId);
            return equippedSkill != null;
        }
    }

    private void ConfirmSelection()
    {
        // 選択状態のチェック
        if (selectedSkill == null)
        {
            DebugLog("スキルが選択されていません");
            return;
        }

        // イベントが設定されているかチェック
        if (OnSkillSelected == null)
        {
            DebugLogError("OnSkillSelectedイベントが設定されていません");
            return;
        }

        DebugLog($"スキル選択を確定: {selectedSkill.userSkillId}");

        try
        {
            OnSkillSelected.Invoke(selectedSkill);
            HidePopup();
        }
        catch (System.Exception e)
        {
            DebugLogError($"スキル選択確定時にエラーが発生: {e.Message}");
        }
    }

    private void RemoveSkill()
    {
        // スキル外し可能かチェック
        if (!HasEquippedSkill())
        {
            DebugLog("外すスキルがありません");
            return;
        }

        // イベントが設定されているかチェック
        if (OnSkillRemoved == null)
        {
            DebugLogError("OnSkillRemovedイベントが設定されていません");
            return;
        }

        DebugLog($"スキルを外します: ID={currentEquipmentId}");

        try
        {
            OnSkillRemoved.Invoke();
            HidePopup();
        }
        catch (System.Exception e)
        {
            DebugLogError($"スキル外し時にエラーが発生: {e.Message}");
        }
    }

    private bool IsManagersReady()
    {
        return SkillManager.Instance != null &&
               SkillManager.Instance.IsInitialized &&
               MasterDataManager.Instance != null &&
               MasterDataManager.Instance.IsDataLoaded &&
               SaveDataManager.Instance != null &&
               SaveDataManager.Instance.IsDataLoaded;
    }

    #endregion

    #region 表示名変換ユーティリティ

    private string GetSkillTypeDisplayName(SkillType skillType)
    {
        return skillType switch
        {
            SkillType.Attack => "攻撃系",
            SkillType.Heal => "回復系",
            SkillType.Buff => "バフ系",
            SkillType.Debuff => "デバフ系",
            SkillType.Support => "サポート系",
            SkillType.Special => "特殊系",
            _ => "不明"
        };
    }

    private string GetAttributeDisplayName(AttributeType attributeType)
    {
        return attributeType switch
        {
            AttributeType.Fire => "火",
            AttributeType.Water => "水",
            AttributeType.Wind => "風",
            AttributeType.Earth => "土",
            AttributeType.None => "無",
            _ => "不明"
        };
    }

    private string GetRarityDisplayName(RarityType rarity)
    {
        return rarity switch
        {
            RarityType.Common => "コモン",
            RarityType.Rare => "レア",
            RarityType.Epic => "エピック",
            RarityType.Legendary => "レジェンダリー",
            _ => "不明"
        };
    }

    private string GetTargetTypeDisplayName(TargetType targetType)
    {
        return targetType switch
        {
            TargetType.Self => "自分",
            TargetType.EnemySingle => "敵単体",
            TargetType.EnemyAll => "敵全体",
            TargetType.AllySingle => "味方単体",
            TargetType.AllyAll => "味方全体",
            TargetType.Random => "ランダム",
            _ => "不明"
        };
    }

    #endregion

    #region デバッグ

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SkillSelectionPopup] {message}");
        }
    }

    private void DebugLogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[SkillSelectionPopup] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("戦闘用スキル選択テスト")]
    private void TestShowBattleSkills()
    {
        ShowSkillSelection("battle_skill_slot_1");
    }

    [ContextMenu("装備用スキル選択テスト")]
    private void TestShowEquipmentSkills()
    {
        ShowSkillSelection("test-equipment-id");
    }

    [ContextMenu("詳細ステータステスト")]
    private void TestDetailedStats()
    {
        if (selectedSkill != null)
        {
            UpdateDetailsPanel();
            Debug.Log("詳細ステータス表示をテスト更新しました");
        }
        else
        {
            Debug.LogWarning("スキルが選択されていません");
        }
    }

    [ContextMenu("Grid内スキル解除ボタンテスト")]
    private void TestGridRemoveButton()
    {
        CreateRemoveSkillSlot();
        Debug.Log("Grid内スキル解除ボタンをテスト作成しました");
    }

    [ContextMenu("戦闘用スキル判定テスト")]
    private void TestBattleSkillDetection()
    {
        Debug.Log($"現在のID: {currentEquipmentId}");
        Debug.Log($"戦闘用スキルスロット判定: {IsBattleSkillSlot()}");
        Debug.Log($"スキル装備状況: {HasEquippedSkill()}");
    }
#endif

    #endregion
}