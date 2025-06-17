using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class ItemSelectionUI : MonoBehaviour
{
    [Header("Selection Panel")]
    public GameObject selectionPanel;
    public Transform contentParent; // ContentPanelを指す
    public Transform dynamicContentParent; // ★新規追加: DynamicContentPanelを指す
    public Button closeButton;

    [Header("Item Button Prefab")]
    public GameObject itemButtonPrefab;

    [Header("None Selection Button")]
    public Button noneSelectionButton; // ★追加: あらかじめ作成した「選択なし」ボタン

    [Header("Selection Type")]
    public SelectionType currentSelectionType;

    // ★重要: イベント定義（EquipmentUpgradeManager.csで使用）
    public System.Action<string, string> OnItemSelected;
    public System.Action OnSelectionCancelled;

    private List<GameObject> instantiatedButtons = new List<GameObject>();

    public enum SelectionType
    {
        Equipment,
        EnhancementItem,
        SupportMaterial
    }

    void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseSelectionPanel);

        // ★「選択なし」ボタンのイベント設定
        if (noneSelectionButton != null)
        {
            noneSelectionButton.onClick.AddListener(() => {
                Debug.Log("「選択なし」ボタンがクリックされました");
                OnItemSelected?.Invoke("", "support_none");
                CloseSelectionPanel();
            });

            // 初期状態では非表示
            noneSelectionButton.gameObject.SetActive(false);
            Debug.Log("「選択なし」ボタンのイベント設定完了");
        }
        else
        {
            Debug.LogWarning("NoneSelectionButton が設定されていません");
        }

        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }

        Debug.Log("ItemSelectionUI初期化完了");
    }

    public void ShowEquipmentSelection()
    {
        Debug.Log("装備選択画面を表示");
        currentSelectionType = SelectionType.Equipment;
        ShowSelectionPanel();

        // ★「選択なし」ボタンを非表示
        if (noneSelectionButton != null)
        {
            noneSelectionButton.gameObject.SetActive(false);
        }

        PopulateEquipmentList();
    }

    public void ShowEnhancementItemSelection()
    {
        Debug.Log("強化アイテム選択画面を表示");
        currentSelectionType = SelectionType.EnhancementItem;
        ShowSelectionPanel();

        // ★「選択なし」ボタンを非表示
        if (noneSelectionButton != null)
        {
            noneSelectionButton.gameObject.SetActive(false);
        }

        PopulateEnhancementItemList();
    }

    public void ShowSupportMaterialSelection()
    {
        Debug.Log("補助材料選択画面を表示");
        currentSelectionType = SelectionType.SupportMaterial;
        ShowSelectionPanel();

        // ★「選択なし」ボタンを表示 - 詳細デバッグ
        if (noneSelectionButton != null)
        {
            Debug.Log($"NoneSelectionButton found: {noneSelectionButton.name}");
            Debug.Log($"NoneSelectionButton current active state: {noneSelectionButton.gameObject.activeInHierarchy}");

            noneSelectionButton.gameObject.SetActive(true);

            Debug.Log($"NoneSelectionButton after SetActive(true): {noneSelectionButton.gameObject.activeInHierarchy}");
            Debug.Log($"NoneSelectionButton parent: {noneSelectionButton.transform.parent?.name}");
            Debug.Log($"NoneSelectionButton position: {noneSelectionButton.transform.position}");
            Debug.Log($"NoneSelectionButton rect: {noneSelectionButton.GetComponent<RectTransform>().rect}");

            // 親オブジェクトのアクティブ状態も確認
            Transform current = noneSelectionButton.transform;
            while (current != null)
            {
                Debug.Log($"Parent check: {current.name} - Active: {current.gameObject.activeSelf}");
                current = current.parent;
            }

            Debug.Log("「選択なし」ボタンを表示しました");
        }
        else
        {
            Debug.LogError("NoneSelectionButton が null です！Inspectorで設定を確認してください");
        }

        PopulateSupportMaterialList();
    }

    private void ShowSelectionPanel()
    {
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(true);
        }
        ClearItemList();
    }

    public void CloseSelectionPanel()
    {
        Debug.Log("選択画面を閉じます");

        if (selectionPanel != null)
        {
            selectionPanel.SetActive(false);
        }

        ClearItemList();
        OnSelectionCancelled?.Invoke(); // ★イベント呼び出し
    }

    private void ClearItemList()
    {
        foreach (GameObject button in instantiatedButtons)
        {
            if (button != null)
                Destroy(button);
        }
        instantiatedButtons.Clear();
    }

    private void PopulateEquipmentList()
    {
        if (DataManager.Instance == null || DataManager.Instance.currentUserData == null)
        {
            Debug.LogError("DataManagerまたはユーザーデータが見つかりません");
            return;
        }

        var equipmentList = DataManager.Instance.GetAllUserEquipments();

        for (int i = 0; i < equipmentList.Count; i++)
        {
            var userEquipment = equipmentList[i];
            var masterData = DataManager.Instance.GetEquipmentData(userEquipment.equipmentId);
            if (masterData != null)
            {
                CreateEquipmentButton(masterData, userEquipment, i);
            }
        }
    }

    private void PopulateEnhancementItemList()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager が見つかりません");
            return;
        }

        // 強化アイテムマスターデータを全て取得して、所持数をチェック
        foreach (var itemData in DataManager.Instance.enhancementItemDatabase)
        {
            int quantity = DataManager.Instance.GetItemQuantity(itemData.itemId);
            if (quantity > 0)
            {
                CreateEnhancementItemButton(itemData, quantity);
            }
        }
    }

    private void PopulateSupportMaterialList()
    {
        Debug.Log("補助材料リスト作成開始");

        // ★動的な「選択なし」ボタン作成は不要（あらかじめUIに配置済み）

        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager が見つかりません");
            return;
        }

        // 補助アイテムマスターデータを全て取得して、所持数をチェック
        int materialCount = 0;
        foreach (var itemData in DataManager.Instance.supportMaterialDatabase)
        {
            int quantity = DataManager.Instance.GetItemQuantity(itemData.materialId);
            Debug.Log($"補助材料チェック: {itemData.materialName} (ID:{itemData.materialId}) 所持数:{quantity}");

            if (quantity > 0)
            {
                CreateSupportMaterialButton(itemData, quantity);
                materialCount++;
            }
        }

        Debug.Log($"補助材料リスト作成完了: あらかじめ配置済み「選択なし」+ {materialCount}個の材料");
    }

    private void CreateNoneSupportMaterialButton()
    {
        Debug.Log("「選択なし」ボタン作成開始");

        if (itemButtonPrefab == null || contentParent == null)
        {
            Debug.LogError("ItemButtonPrefabまたはContentParentが設定されていません");
            return;
        }

        GameObject buttonObj = Instantiate(itemButtonPrefab, contentParent);

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => {
                Debug.Log("「選択なし」ボタンがクリックされました");
                OnItemSelected?.Invoke("", "support_none"); // ★イベント呼び出し
                CloseSelectionPanel();
            });
            Debug.Log("「選択なし」ボタンのクリックイベント設定完了");
        }
        else
        {
            Debug.LogError("「選択なし」ボタンにButtonコンポーネントが見つかりません");
        }

        // 背景色を変更して区別しやすくする
        Image buttonImage = buttonObj.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = new Color(0.8f, 0.8f, 1.0f, 1f); // 薄い青色で区別
            Debug.Log("「選択なし」ボタンの背景色設定完了");
        }

        // UI設定
        SetTextComponent(buttonObj, "NameText", "選択なし");
        SetTextComponent(buttonObj, "DetailText", "補助材料を使用しません");

        // アイコンを非表示
        Transform iconTransform = buttonObj.transform.Find("Icon");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.enabled = false;
                Debug.Log("「選択なし」ボタンのアイコンを非表示に設定");
            }
        }

        instantiatedButtons.Add(buttonObj);
        Debug.Log($"「選択なし」ボタン作成完了。現在のボタン数: {instantiatedButtons.Count}");
    }

    private void CreateEquipmentButton(EquipmentData equipData, UserEquipment userEquip, int equipmentIndex)
    {
        if (itemButtonPrefab == null || contentParent == null)
        {
            Debug.LogError("ItemButtonPrefabまたはContentParentが設定されていません");
            return;
        }

        GameObject buttonObj = Instantiate(itemButtonPrefab, contentParent);

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => {
                Debug.Log($"装備が選択されました: {equipData.equipmentName}");
                OnItemSelected?.Invoke(equipmentIndex.ToString(), "equipment"); // ★イベント呼び出し
                CloseSelectionPanel();
            });
        }

        // UI要素の設定
        SetImageComponent(buttonObj, "Icon", equipData.icon);
        SetTextComponent(buttonObj, "NameText", $"{equipData.equipmentName} +{userEquip.enhancementLevel}");
        SetTextComponent(buttonObj, "DetailText", $"攻撃力: {userEquip.GetTotalAttack():F1}\n耐久: {userEquip.currentDurability}");

        instantiatedButtons.Add(buttonObj);
    }

    private void CreateEnhancementItemButton(EnhancementItemData itemData, int quantity)
    {
        if (itemButtonPrefab == null || contentParent == null)
        {
            Debug.LogError("ItemButtonPrefabまたはContentParentが設定されていません");
            return;
        }

        GameObject buttonObj = Instantiate(itemButtonPrefab, contentParent);

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => {
                Debug.Log($"強化アイテムが選択されました: {itemData.itemName}");
                OnItemSelected?.Invoke(itemData.itemId.ToString(), "enhancement"); // ★イベント呼び出し
                CloseSelectionPanel();
            });
        }

        // UI要素の設定
        SetImageComponent(buttonObj, "Icon", itemData.icon);
        SetTextComponent(buttonObj, "NameText", $"{itemData.itemName} x{quantity}");
        SetTextComponent(buttonObj, "DetailText", $"成功率: {itemData.successRate * 100:F0}%\n{itemData.GetEffectDescription()}");

        instantiatedButtons.Add(buttonObj);
    }

    private void CreateSupportMaterialButton(SupportMaterialData materialData, int quantity)
    {
        if (itemButtonPrefab == null || contentParent == null)
        {
            Debug.LogError("ItemButtonPrefabまたはContentParentが設定されていません");
            return;
        }

        GameObject buttonObj = Instantiate(itemButtonPrefab, contentParent);

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => {
                Debug.Log($"補助材料が選択されました: {materialData.materialName}");
                OnItemSelected?.Invoke(materialData.materialId.ToString(), "support"); // ★イベント呼び出し
                CloseSelectionPanel();
            });
        }

        // UI要素の設定
        SetImageComponent(buttonObj, "Icon", materialData.icon);
        SetTextComponent(buttonObj, "NameText", $"{materialData.materialName} x{quantity}");
        SetTextComponent(buttonObj, "DetailText", materialData.GetEffectDescription());

        instantiatedButtons.Add(buttonObj);
    }

    // ★既存スクリプト互換性維持用メソッド
    public void SelectEquipment(EquipmentData equipData, UserEquipment userEquip)
    {
        // 装備インデックスを検索
        var equipmentList = DataManager.Instance.GetAllUserEquipments();
        for (int i = 0; i < equipmentList.Count; i++)
        {
            if (equipmentList[i] == userEquip)
            {
                OnItemSelected?.Invoke(i.ToString(), "equipment");
                CloseSelectionPanel();
                return;
            }
        }
    }

    public void SelectUpgradeItem(EnhancementItemData itemData)
    {
        OnItemSelected?.Invoke(itemData.itemId.ToString(), "enhancement");
        CloseSelectionPanel();
    }

    public void SelectSupportItem(SupportMaterialData materialData)
    {
        OnItemSelected?.Invoke(materialData.materialId.ToString(), "support");
        CloseSelectionPanel();
    }

    public void DeselectSupportItem()
    {
        OnItemSelected?.Invoke("", "support_none");
        CloseSelectionPanel();
    }

    // ヘルパーメソッド：テキストコンポーネント設定
    private void SetTextComponent(GameObject parent, string childName, string text)
    {
        Transform textTransform = parent.transform.Find(childName);
        if (textTransform != null)
        {
            TextMeshProUGUI textComponent = textTransform.GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }
    }

    // ヘルパーメソッド：画像コンポーネント設定
    private void SetImageComponent(GameObject parent, string childName, Sprite sprite)
    {
        Transform imageTransform = parent.transform.Find(childName);
        if (imageTransform != null)
        {
            Image imageComponent = imageTransform.GetComponent<Image>();
            if (imageComponent != null && sprite != null)
            {
                imageComponent.sprite = sprite;
                imageComponent.enabled = true;
            }
        }
    }
}