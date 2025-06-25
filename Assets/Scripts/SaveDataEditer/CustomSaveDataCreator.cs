using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
public class CustomSaveDataCreator : EditorWindow
{
    [MenuItem("Tools/Save Data/Custom Save Data Creator")]
    public static void ShowWindow()
    {
        GetWindow<CustomSaveDataCreator>("Custom Save Data Creator");
    }

    private string playerName = "カスタムプレイヤー";
    private int playerLevel = 1;
    private long gold = 10000;
    private int gems = 100;
    private int stamina = 100;

    private Vector2 scrollPosition;
    private List<EquipmentToAdd> equipmentsToAdd = new List<EquipmentToAdd>();
    private List<ItemToAdd> itemsToAdd = new List<ItemToAdd>();

    [System.Serializable]
    public class EquipmentToAdd
    {
        public int equipmentId;
        public int enhancementLevel;
        public bool isEquipped;
        public bool isLocked;
        public bool isFavorite;
    }

    [System.Serializable]
    public class ItemToAdd
    {
        public ItemType itemType;
        public int itemId;
        public int quantity;
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("カスタムセーブデータ作成", EditorStyles.boldLabel);
        GUILayout.Space(10);

        DrawPlayerSettings();
        GUILayout.Space(10);

        DrawEquipmentSettings();
        GUILayout.Space(10);

        DrawItemSettings();
        GUILayout.Space(10);

        DrawCreateButton();

        EditorGUILayout.EndScrollView();
    }

    private void DrawPlayerSettings()
    {
        GUILayout.Label("プレイヤー設定", EditorStyles.boldLabel);

        playerName = EditorGUILayout.TextField("プレイヤー名", playerName);
        playerLevel = EditorGUILayout.IntField("レベル", playerLevel);
        gold = EditorGUILayout.LongField("ゴールド", gold);
        gems = EditorGUILayout.IntField("ジェム", gems);
        stamina = EditorGUILayout.IntField("スタミナ", stamina);
    }

    private void DrawEquipmentSettings()
    {
        GUILayout.Label("装備設定", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("装備追加"))
        {
            equipmentsToAdd.Add(new EquipmentToAdd());
        }
        if (GUILayout.Button("プリセット装備追加"))
        {
            AddPresetEquipments();
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < equipmentsToAdd.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"装備 {i + 1}", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("削除", GUILayout.Width(50)))
            {
                equipmentsToAdd.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();

            equipmentsToAdd[i].equipmentId = EditorGUILayout.IntField("装備ID", equipmentsToAdd[i].equipmentId);
            equipmentsToAdd[i].enhancementLevel = EditorGUILayout.IntField("強化値", equipmentsToAdd[i].enhancementLevel);
            equipmentsToAdd[i].isEquipped = EditorGUILayout.Toggle("装備中", equipmentsToAdd[i].isEquipped);
            equipmentsToAdd[i].isLocked = EditorGUILayout.Toggle("ロック", equipmentsToAdd[i].isLocked);
            equipmentsToAdd[i].isFavorite = EditorGUILayout.Toggle("お気に入り", equipmentsToAdd[i].isFavorite);

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawItemSettings()
    {
        GUILayout.Label("アイテム設定", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("アイテム追加"))
        {
            itemsToAdd.Add(new ItemToAdd());
        }
        if (GUILayout.Button("プリセットアイテム追加"))
        {
            AddPresetItems();
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < itemsToAdd.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"アイテム {i + 1}", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("削除", GUILayout.Width(50)))
            {
                itemsToAdd.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();

            itemsToAdd[i].itemType = (ItemType)EditorGUILayout.EnumPopup("アイテムタイプ", itemsToAdd[i].itemType);
            itemsToAdd[i].itemId = EditorGUILayout.IntField("アイテムID", itemsToAdd[i].itemId);
            itemsToAdd[i].quantity = EditorGUILayout.IntField("数量", itemsToAdd[i].quantity);

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawCreateButton()
    {
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("セーブデータ作成", GUILayout.Height(30)))
        {
            CreateCustomSaveData();
        }

        if (GUILayout.Button("現在のデータをベースに作成", GUILayout.Height(30)))
        {
            CreateFromCurrentData();
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        if (GUILayout.Button("JSONファイルをエクスポート", GUILayout.Height(25)))
        {
            ExportToJson();
        }
    }

    private void AddPresetEquipments()
    {
        // プリセット装備の追加
        equipmentsToAdd.Add(new EquipmentToAdd { equipmentId = 1, enhancementLevel = 5, isEquipped = true });
        equipmentsToAdd.Add(new EquipmentToAdd { equipmentId = 2, enhancementLevel = 3, isEquipped = true });
        equipmentsToAdd.Add(new EquipmentToAdd { equipmentId = 3, enhancementLevel = 0, isEquipped = true });
        equipmentsToAdd.Add(new EquipmentToAdd { equipmentId = 4, enhancementLevel = 10, isLocked = true, isFavorite = true });
        equipmentsToAdd.Add(new EquipmentToAdd { equipmentId = 5, enhancementLevel = 7, isFavorite = true });
    }

    private void AddPresetItems()
    {
        // プリセットアイテムの追加
        itemsToAdd.Add(new ItemToAdd { itemType = ItemType.EnhanceItem, itemId = 1, quantity = 20 });
        itemsToAdd.Add(new ItemToAdd { itemType = ItemType.EnhanceItem, itemId = 2, quantity = 10 });
        itemsToAdd.Add(new ItemToAdd { itemType = ItemType.EnhanceItem, itemId = 3, quantity = 5 });
        itemsToAdd.Add(new ItemToAdd { itemType = ItemType.EnhanceItem, itemId = 4, quantity = 3 });
        itemsToAdd.Add(new ItemToAdd { itemType = ItemType.SupportItem, itemId = 1, quantity = 15 });
        itemsToAdd.Add(new ItemToAdd { itemType = ItemType.SupportItem, itemId = 2, quantity = 8 });
    }

    private void CreateCustomSaveData()
    {
        if (SaveDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("エラー", "SaveDataManagerが見つかりません", "OK");
            return;
        }

        // 新規セーブデータ作成
        var saveData = new UserSaveData
        {
            playerName = playerName,
            playerLevel = playerLevel,
            totalExp = playerLevel * 1000,
            gold = gold,
            gems = gems,
            stamina = stamina,
            createDate = System.DateTime.Now,
            lastLoginDate = System.DateTime.Now
        };

        // 装備追加
        foreach (var equipToAdd in equipmentsToAdd)
        {
            var masterData = MasterDataManager.Instance?.GetEquipmentData(equipToAdd.equipmentId);
            if (masterData != null)
            {
                var equipment = new UserEquipmentData(masterData)
                {
                    currentEnhancedValue = equipToAdd.enhancementLevel,
                    isEquipped = equipToAdd.isEquipped,
                    isLocked = equipToAdd.isLocked,
                    isFavorite = equipToAdd.isFavorite
                };

                // 強化による追加ステータスを簡易計算（実際の強化システムとは異なる）
                if (equipToAdd.enhancementLevel > 0)
                {
                    equipment.enhancedOffense = equipToAdd.enhancementLevel * 2;
                    equipment.enhancedHp = equipToAdd.enhancementLevel * 5;
                    equipment.enhancedDefense = equipToAdd.enhancementLevel;
                }

                saveData.AddEquipment(equipment);
            }
        }

        // アイテム追加
        foreach (var itemToAdd in itemsToAdd)
        {
            if (itemToAdd.itemType == ItemType.EnhanceItem)
            {
                var masterData = MasterDataManager.Instance?.GetEnhanceItemData(itemToAdd.itemId);
                if (masterData != null)
                {
                    var item = new UserItemData(masterData, itemToAdd.quantity);
                    saveData.AddItem(item);
                }
            }
            else if (itemToAdd.itemType == ItemType.SupportItem)
            {
                var masterData = MasterDataManager.Instance?.GetSupportItemData(itemToAdd.itemId);
                if (masterData != null)
                {
                    var item = new UserItemData(masterData, itemToAdd.quantity);
                    saveData.AddItem(item);
                }
            }
        }

        // セーブデータを設定して保存
        SaveDataManager.Instance.SetSaveData(saveData);
        SaveDataManager.Instance.SaveSaveData();

        // インベントリマネージャーのキャッシュを更新
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RefreshCache();
        }

        EditorUtility.DisplayDialog("完了", "カスタムセーブデータを作成しました", "OK");
        Debug.Log("カスタムセーブデータを作成しました");
    }

    private void CreateFromCurrentData()
    {
        if (SaveDataManager.Instance?.CurrentSaveData == null)
        {
            EditorUtility.DisplayDialog("エラー", "現在のセーブデータが見つかりません", "OK");
            return;
        }

        var currentData = SaveDataManager.Instance.CurrentSaveData;

        // 現在のデータから値を取得
        playerName = currentData.playerName;
        playerLevel = currentData.playerLevel;
        gold = currentData.gold;
        gems = currentData.gems;
        stamina = currentData.stamina;

        // 装備リストをクリアして現在の装備を追加
        equipmentsToAdd.Clear();
        foreach (var equipment in currentData.equipments)
        {
            equipmentsToAdd.Add(new EquipmentToAdd
            {
                equipmentId = equipment.equipmentMasterId,
                enhancementLevel = equipment.currentEnhancedValue,
                isEquipped = equipment.isEquipped,
                isLocked = equipment.isLocked,
                isFavorite = equipment.isFavorite
            });
        }

        // アイテムリストをクリアして現在のアイテムを追加
        itemsToAdd.Clear();
        foreach (var item in currentData.items)
        {
            itemsToAdd.Add(new ItemToAdd
            {
                itemType = item.itemType,
                itemId = item.itemMasterId,
                quantity = item.quantity
            });
        }

        EditorUtility.DisplayDialog("完了", "現在のデータをエディターに読み込みました", "OK");
    }

    private void ExportToJson()
    {
        if (SaveDataManager.Instance?.CurrentSaveData == null)
        {
            EditorUtility.DisplayDialog("エラー", "セーブデータが見つかりません", "OK");
            return;
        }

        string json = JsonUtility.ToJson(SaveDataManager.Instance.CurrentSaveData, true);
        string path = EditorUtility.SaveFilePanel("JSONエクスポート", "", "custom_save_data.json", "json");

        if (!string.IsNullOrEmpty(path))
        {
            System.IO.File.WriteAllText(path, json);
            EditorUtility.DisplayDialog("完了", $"JSONファイルをエクスポートしました:\n{path}", "OK");
        }
    }
}
#endif