using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class SaveDataEditorTools : EditorWindow
{
    private string newPlayerName = "テストプレイヤー";
    private int addEquipmentId = 1;
    private int addItemId = 1;
    private int addItemQuantity = 1;
    private ItemType selectedItemType = ItemType.EnhanceItem;

    // スキル関連の新しいフィールド
    private int addSkillId = 1;
    private string selectedSkillIdForRemove = "";
    private string selectedEquipmentIdForSkill = "";
    private string selectedSkillIdForEquip = "";

    private Vector2 scrollPosition;

    [MenuItem("Tools/Save Data/Save Data Editor")]
    public static void ShowWindow()
    {
        GetWindow<SaveDataEditorTools>("Save Data Editor");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Save Data Manager", EditorStyles.boldLabel);
        GUILayout.Space(10);

        DrawSaveDataInfo();
        GUILayout.Space(10);

        DrawSaveDataOperations();
        GUILayout.Space(10);

        DrawInventoryOperations();
        GUILayout.Space(10);

        DrawSkillOperations();
        GUILayout.Space(10);

        DrawTestDataOperations();
        GUILayout.Space(10);

        DrawBackupOperations();

        EditorGUILayout.EndScrollView();
    }

    private void DrawSaveDataInfo()
    {
        GUILayout.Label("セーブデータ情報", EditorStyles.boldLabel);

        if (SaveDataManager.Instance == null)
        {
            EditorGUILayout.HelpBox("SaveDataManagerが見つかりません", MessageType.Warning);
            return;
        }

        var saveData = SaveDataManager.Instance.CurrentSaveData;
        if (saveData == null)
        {
            EditorGUILayout.HelpBox("セーブデータが読み込まれていません", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("プレイヤー名", saveData.playerName);
            EditorGUILayout.LabelField("レベル", saveData.playerLevel.ToString());
            EditorGUILayout.LabelField("ゴールド", saveData.gold.ToString("N0"));
            EditorGUILayout.LabelField("ジェム", saveData.gems.ToString());
            EditorGUILayout.LabelField("装備数", saveData.equipments.Count.ToString());
            EditorGUILayout.LabelField("アイテム種類数", saveData.items.Count.ToString());
            EditorGUILayout.LabelField("スキル数", saveData.skills.Count.ToString());
            EditorGUILayout.LabelField("最終ログイン", saveData.lastLoginDate.ToString("yyyy/MM/dd HH:mm"));

            GUILayout.Space(5);

            var summary = saveData.GetItemSummary();
            EditorGUILayout.LabelField("強化アイテム", $"{summary.totalEnhanceItems}種類 {summary.totalEnhanceQuantity}個");
            EditorGUILayout.LabelField("補助アイテム", $"{summary.totalSupportItems}種類 {summary.totalSupportQuantity}個");
            EditorGUILayout.LabelField("新規アイテム", summary.newItemCount.ToString());

            // スキル情報追加
            int newSkillCount = saveData.skills.Count(s => s.isNew);
            EditorGUILayout.LabelField("新規スキル", newSkillCount.ToString());
        }

        GUILayout.Space(5);
        EditorGUILayout.LabelField("セーブファイルパス", SaveDataManager.Instance.SaveFilePath);
        EditorGUILayout.LabelField("ファイル存在", SaveDataManager.Instance.SaveFileExists().ToString());
    }

    private void DrawSaveDataOperations()
    {
        GUILayout.Label("セーブデータ操作", EditorStyles.boldLabel);

        if (SaveDataManager.Instance == null)
        {
            EditorGUILayout.HelpBox("SaveDataManagerが見つかりません", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("読み込み"))
        {
            SaveDataManager.Instance.LoadSaveData();
        }

        if (GUILayout.Button("保存"))
        {
            SaveDataManager.Instance.SaveSaveData();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        newPlayerName = EditorGUILayout.TextField("プレイヤー名", newPlayerName);
        if (GUILayout.Button("新規作成", GUILayout.Width(80)))
        {
            SaveDataManager.Instance.CreateNewSaveData(newPlayerName);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("リセット"))
        {
            if (EditorUtility.DisplayDialog("確認", "セーブデータをリセットしますか？", "はい", "いいえ"))
            {
                SaveDataManager.Instance.ResetSaveData();
            }
        }

        if (GUILayout.Button("セーブフォルダを開く"))
        {
            string folderPath = Path.GetDirectoryName(SaveDataManager.Instance.SaveFilePath);
            EditorUtility.RevealInFinder(folderPath);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawInventoryOperations()
    {
        GUILayout.Label("インベントリ操作", EditorStyles.boldLabel);

        if (InventoryManager.Instance == null)
        {
            EditorGUILayout.HelpBox("InventoryManagerが見つかりません", MessageType.Warning);
            return;
        }

        if (!InventoryManager.Instance.IsInitialized)
        {
            EditorGUILayout.HelpBox("InventoryManagerが初期化されていません", MessageType.Warning);
            return;
        }

        // 装備追加
        EditorGUILayout.BeginHorizontal();
        addEquipmentId = EditorGUILayout.IntField("装備ID", addEquipmentId);
        if (GUILayout.Button("装備追加", GUILayout.Width(80)))
        {
            InventoryManager.Instance.AddEquipment(addEquipmentId);
        }
        EditorGUILayout.EndHorizontal();

        // アイテム追加
        EditorGUILayout.BeginHorizontal();
        selectedItemType = (ItemType)EditorGUILayout.EnumPopup("アイテムタイプ", selectedItemType);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        addItemId = EditorGUILayout.IntField("アイテムID", addItemId);
        addItemQuantity = EditorGUILayout.IntField("数量", addItemQuantity);
        if (GUILayout.Button("アイテム追加", GUILayout.Width(80)))
        {
            InventoryManager.Instance.AddItem(selectedItemType, addItemId, addItemQuantity);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("インベントリ統計"))
        {
            Debug.Log(InventoryManager.Instance.GetInventoryStatistics());
        }

        if (GUILayout.Button("詳細状況確認"))
        {
            Debug.Log(InventoryManager.Instance.GetDetailedInventoryStatus());
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("データ検証"))
        {
            var errors = InventoryManager.Instance.ValidateInventoryData();
            if (errors.Count == 0)
            {
                Debug.Log("インベントリデータに問題はありません");
            }
            else
            {
                Debug.LogError($"インベントリデータに{errors.Count}個の問題があります:\n" + string.Join("\n", errors));
            }
        }

        if (GUILayout.Button("強制キャッシュ更新"))
        {
            InventoryManager.Instance.RefreshCache();
            Debug.Log("キャッシュを強制更新しました");
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("新規フラグクリア"))
        {
            InventoryManager.Instance.ClearAllNewFlags();
        }

        if (GUILayout.Button("キャッシュ更新"))
        {
            InventoryManager.Instance.RefreshCache();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSkillOperations()
    {
        GUILayout.Label("スキル操作", EditorStyles.boldLabel);

        if (SkillManager.Instance == null)
        {
            EditorGUILayout.HelpBox("SkillManagerが見つかりません", MessageType.Warning);
            return;
        }

        if (!SkillManager.Instance.IsInitialized)
        {
            EditorGUILayout.HelpBox("SkillManagerが初期化されていません", MessageType.Warning);
            return;
        }

        var saveData = SaveDataManager.Instance?.CurrentSaveData;
        if (saveData == null)
        {
            EditorGUILayout.HelpBox("セーブデータが読み込まれていません", MessageType.Warning);
            return;
        }

        // スキル追加
        EditorGUILayout.BeginHorizontal();
        addSkillId = EditorGUILayout.IntField("スキルID", addSkillId);
        if (GUILayout.Button("スキル追加", GUILayout.Width(80)))
        {
            bool success = SkillManager.Instance.AddSkill(addSkillId);
            if (success)
            {
                Debug.Log($"スキルID {addSkillId} を追加しました");
            }
            else
            {
                Debug.LogError($"スキルID {addSkillId} の追加に失敗しました");
            }
        }
        EditorGUILayout.EndHorizontal();

        // スキル削除
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("スキル削除", GUILayout.Width(80));

        // 所持スキルのドロップダウン
        var skillOptions = new string[saveData.skills.Count + 1];
        skillOptions[0] = "選択してください";
        for (int i = 0; i < saveData.skills.Count; i++)
        {
            var skill = saveData.skills[i];
            var masterData = MasterDataManager.Instance?.GetSkillData(skill.skillMasterId);
            string skillName = masterData != null ? masterData.skillName : $"Unknown({skill.skillMasterId})";
            skillOptions[i + 1] = $"{skillName} (ID:{skill.userSkillId.Substring(0, 8)}...)";
        }

        int selectedIndex = 0;
        if (!string.IsNullOrEmpty(selectedSkillIdForRemove))
        {
            for (int i = 0; i < saveData.skills.Count; i++)
            {
                if (saveData.skills[i].userSkillId == selectedSkillIdForRemove)
                {
                    selectedIndex = i + 1;
                    break;
                }
            }
        }

        int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, skillOptions);
        if (newSelectedIndex != selectedIndex)
        {
            selectedSkillIdForRemove = newSelectedIndex > 0 ? saveData.skills[newSelectedIndex - 1].userSkillId : "";
        }

        if (GUILayout.Button("削除", GUILayout.Width(50)) && !string.IsNullOrEmpty(selectedSkillIdForRemove))
        {
            bool success = SkillManager.Instance.RemoveSkill(selectedSkillIdForRemove);
            if (success)
            {
                Debug.Log($"スキルを削除しました: {selectedSkillIdForRemove}");
                selectedSkillIdForRemove = "";
            }
        }
        EditorGUILayout.EndHorizontal();

        // スキル装備機能
        GUILayout.Space(5);
        EditorGUILayout.LabelField("スキル装備", EditorStyles.miniBoldLabel);

        // 装備選択
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("装備選択", GUILayout.Width(80));

        var equipmentOptions = new string[saveData.equipments.Count + 1];
        equipmentOptions[0] = "選択してください";
        for (int i = 0; i < saveData.equipments.Count; i++)
        {
            var equipment = saveData.equipments[i];
            var masterData = MasterDataManager.Instance?.GetEquipmentData(equipment.equipmentMasterId);
            string equipmentName = masterData != null ? masterData.equipmentName : $"Unknown({equipment.equipmentMasterId})";
            string skillInfo = equipment.HasEquippedSkill() ? " [スキル装備済み]" : "";
            equipmentOptions[i + 1] = $"{equipmentName}{skillInfo} (ID:{equipment.userEquipmentId.Substring(0, 8)}...)";
        }

        int selectedEquipmentIndex = 0;
        if (!string.IsNullOrEmpty(selectedEquipmentIdForSkill))
        {
            for (int i = 0; i < saveData.equipments.Count; i++)
            {
                if (saveData.equipments[i].userEquipmentId == selectedEquipmentIdForSkill)
                {
                    selectedEquipmentIndex = i + 1;
                    break;
                }
            }
        }

        int newSelectedEquipmentIndex = EditorGUILayout.Popup(selectedEquipmentIndex, equipmentOptions);
        if (newSelectedEquipmentIndex != selectedEquipmentIndex)
        {
            selectedEquipmentIdForSkill = newSelectedEquipmentIndex > 0 ? saveData.equipments[newSelectedEquipmentIndex - 1].userEquipmentId : "";
        }
        EditorGUILayout.EndHorizontal();

        // スキル選択
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("スキル選択", GUILayout.Width(80));

        // 利用可能なスキルのみ表示
        var availableSkills = SkillManager.Instance.GetAvailableSkills();
        var skillEquipOptions = new string[availableSkills.Count + 1];
        skillEquipOptions[0] = "選択してください";
        for (int i = 0; i < availableSkills.Count; i++)
        {
            var skill = availableSkills[i];
            var masterData = MasterDataManager.Instance?.GetSkillData(skill.skillMasterId);
            string skillName = masterData != null ? masterData.skillName : $"Unknown({skill.skillMasterId})";
            skillEquipOptions[i + 1] = $"{skillName} (ID:{skill.userSkillId.Substring(0, 8)}...)";
        }

        int selectedSkillEquipIndex = 0;
        if (!string.IsNullOrEmpty(selectedSkillIdForEquip))
        {
            for (int i = 0; i < availableSkills.Count; i++)
            {
                if (availableSkills[i].userSkillId == selectedSkillIdForEquip)
                {
                    selectedSkillEquipIndex = i + 1;
                    break;
                }
            }
        }

        int newSelectedSkillEquipIndex = EditorGUILayout.Popup(selectedSkillEquipIndex, skillEquipOptions);
        if (newSelectedSkillEquipIndex != selectedSkillEquipIndex)
        {
            selectedSkillIdForEquip = newSelectedSkillEquipIndex > 0 ? availableSkills[newSelectedSkillEquipIndex - 1].userSkillId : "";
        }
        EditorGUILayout.EndHorizontal();

        // 装備・解除ボタン
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("スキル装備") && !string.IsNullOrEmpty(selectedEquipmentIdForSkill) && !string.IsNullOrEmpty(selectedSkillIdForEquip))
        {
            bool success = SkillManager.Instance.EquipSkillToEquipment(selectedEquipmentIdForSkill, selectedSkillIdForEquip);
            if (success)
            {
                Debug.Log($"スキルを装備しました: 装備{selectedEquipmentIdForSkill} にスキル{selectedSkillIdForEquip}");
                selectedSkillIdForEquip = "";
            }
        }

        if (GUILayout.Button("スキル解除") && !string.IsNullOrEmpty(selectedEquipmentIdForSkill))
        {
            bool success = SkillManager.Instance.UnequipSkillFromEquipment(selectedEquipmentIdForSkill);
            if (success)
            {
                Debug.Log($"スキルを解除しました: 装備{selectedEquipmentIdForSkill}");
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        // スキル統計・管理
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("スキル統計"))
        {
            Debug.Log(SkillManager.Instance.GetSkillStatistics());
        }

        if (GUILayout.Button("スキルデータ検証"))
        {
            var errors = SkillManager.Instance.ValidateSkillData();
            if (errors.Count == 0)
            {
                Debug.Log("スキルデータに問題はありません");
            }
            else
            {
                Debug.LogError($"スキルデータに{errors.Count}個の問題があります:\n" + string.Join("\n", errors));
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("スキル新規フラグクリア"))
        {
            SkillManager.Instance.ClearAllNewFlags();
            Debug.Log("全スキルの新規フラグをクリアしました");
        }

        if (GUILayout.Button("スキルキャッシュ更新"))
        {
            SkillManager.Instance.RefreshCache();
            Debug.Log("スキルキャッシュを更新しました");
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTestDataOperations()
    {
        GUILayout.Label("テストデータ操作", EditorStyles.boldLabel);

        if (InventoryManager.Instance == null || !InventoryManager.Instance.IsInitialized)
        {
            EditorGUILayout.HelpBox("InventoryManagerが利用できません", MessageType.Warning);
            return;
        }

        if (SkillManager.Instance == null || !SkillManager.Instance.IsInitialized)
        {
            EditorGUILayout.HelpBox("SkillManagerが利用できません", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("基本装備セット追加"))
        {
            InventoryManager.Instance.AddEquipment(1); // 初心者の剣
            InventoryManager.Instance.AddEquipment(2); // 初心者の鎧
            InventoryManager.Instance.AddEquipment(3); // 古ぼけた首飾り
            Debug.Log("基本装備セットを追加しました");
        }

        if (GUILayout.Button("レア装備追加"))
        {
            InventoryManager.Instance.AddEquipment(4); // 火のダガー
            InventoryManager.Instance.AddEquipment(5); // 燃える鎧
            Debug.Log("レア装備を追加しました");
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("基本アイテムセット追加"))
        {
            InventoryManager.Instance.AddItem(ItemType.EnhanceItem, 1, 10);
            InventoryManager.Instance.AddItem(ItemType.SupportItem, 1, 5);
            Debug.Log("基本アイテムセットを追加しました");
        }

        if (GUILayout.Button("基本スキルセット追加"))
        {
            SkillManager.Instance.AddSkill(1); // 基本攻撃スキル
            SkillManager.Instance.AddSkill(2); // 基本防御スキル
            SkillManager.Instance.AddSkill(3); // 火属性攻撃スキル（仮）
            Debug.Log("基本スキルセットを追加しました");
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("通貨追加"))
        {
            var saveData = SaveDataManager.Instance.CurrentSaveData;
            if (saveData != null)
            {
                saveData.gold += 10000;
                saveData.gems += 100;
                SaveDataManager.Instance.MarkDataDirty();
                Debug.Log("通貨を追加しました（ゴールド+10000、ジェム+100）");
            }
        }

        if (GUILayout.Button("全種類テストデータ追加"))
        {
            // 装備
            InventoryManager.Instance.AddEquipment(1);
            InventoryManager.Instance.AddEquipment(2);
            InventoryManager.Instance.AddEquipment(3);

            // アイテム
            InventoryManager.Instance.AddItem(ItemType.EnhanceItem, 1, 10);
            InventoryManager.Instance.AddItem(ItemType.SupportItem, 1, 5);

            // スキル
            SkillManager.Instance.AddSkill(1);
            SkillManager.Instance.AddSkill(2);

            // 通貨
            var saveData = SaveDataManager.Instance.CurrentSaveData;
            if (saveData != null)
            {
                saveData.gold += 5000;
                saveData.gems += 50;
                SaveDataManager.Instance.MarkDataDirty();
            }

            Debug.Log("全種類のテストデータを追加しました");
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("レベルアップ"))
        {
            var saveData = SaveDataManager.Instance.CurrentSaveData;
            if (saveData != null)
            {
                saveData.playerLevel += 10;
                saveData.totalExp += 10000;
                SaveDataManager.Instance.MarkDataDirty();
                Debug.Log("レベルを10上げました");
            }
        }

        if (GUILayout.Button("スタミナ回復"))
        {
            var saveData = SaveDataManager.Instance.CurrentSaveData;
            if (saveData != null)
            {
                saveData.stamina = 100;
                SaveDataManager.Instance.MarkDataDirty();
                Debug.Log("スタミナを満タンにしました");
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawBackupOperations()
    {
        GUILayout.Label("バックアップ操作", EditorStyles.boldLabel);

        if (SaveDataManager.Instance == null)
        {
            EditorGUILayout.HelpBox("SaveDataManagerが見つかりません", MessageType.Warning);
            return;
        }

        var backupFiles = SaveDataManager.Instance.GetBackupFiles();

        EditorGUILayout.LabelField("バックアップファイル数", backupFiles.Length.ToString());

        if (backupFiles.Length > 0)
        {
            GUILayout.Label("バックアップファイル:", EditorStyles.miniBoldLabel);

            for (int i = 0; i < Mathf.Min(backupFiles.Length, 5); i++) // 最新5件のみ表示
            {
                EditorGUILayout.BeginHorizontal();

                string fileName = Path.GetFileName(backupFiles[i]);
                EditorGUILayout.LabelField(fileName, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("復旧", GUILayout.Width(50)))
                {
                    if (EditorUtility.DisplayDialog("確認", $"バックアップから復旧しますか？\n{fileName}", "はい", "いいえ"))
                    {
                        SaveDataManager.Instance.RestoreFromBackup(fileName);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            if (backupFiles.Length > 5)
            {
                EditorGUILayout.LabelField($"... 他{backupFiles.Length - 5}件");
            }
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("バックアップフォルダを開く"))
        {
            EditorUtility.RevealInFinder(SaveDataManager.Instance.BackupFolderPath);
        }

        if (GUILayout.Button("手動バックアップ作成"))
        {
            // 手動保存でバックアップが作成される
            SaveDataManager.Instance.SaveSaveData();
            Debug.Log("手動バックアップを作成しました");
        }

        EditorGUILayout.EndHorizontal();
    }

    private void OnInspectorUpdate()
    {
        // エディターウィンドウを定期的に更新
        Repaint();
    }
}
#endif