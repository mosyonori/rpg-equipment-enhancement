using UnityEngine;
using UnityEditor;
using System.IO;

public static class MasterDataIconUpdater
{
    [MenuItem("Tools/Master Data/Update All Icons")]
    public static void UpdateAllIcons()
    {
        UpdateEquipmentIcons();
        UpdateEnhanceItemIcons();
        UpdateSupportItemIcons();
        AssetDatabase.SaveAssets();
        Debug.Log("全てのアイコンの更新が完了しました。");
    }

    [MenuItem("Tools/Master Data/Update Equipment Icons")]
    public static void UpdateEquipmentIcons()
    {
        string[] equipmentGuids = AssetDatabase.FindAssets("t:EquipmentMasterData", new[] { "Assets/GameData/Equipment" });
        int updatedCount = 0;

        foreach (string guid in equipmentGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            EquipmentMasterData equipment = AssetDatabase.LoadAssetAtPath<EquipmentMasterData>(assetPath);

            if (equipment != null && !string.IsNullOrEmpty(equipment.equipmentIconPath))
            {
                Sprite icon = LoadSpriteFromPath(equipment.equipmentIconPath);
                if (icon != null && equipment.equipmentIcon != icon)
                {
                    equipment.equipmentIcon = icon;
                    EditorUtility.SetDirty(equipment);
                    updatedCount++;
                }
            }
        }

        Debug.Log($"装備アイコンを{updatedCount}件更新しました。");
    }

    [MenuItem("Tools/Master Data/Update Enhance Item Icons")]
    public static void UpdateEnhanceItemIcons()
    {
        string[] enhanceItemGuids = AssetDatabase.FindAssets("t:EnhanceItemMasterData", new[] { "Assets/GameData/EnhanceItem" });
        int updatedCount = 0;

        foreach (string guid in enhanceItemGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            EnhanceItemMasterData enhanceItem = AssetDatabase.LoadAssetAtPath<EnhanceItemMasterData>(assetPath);

            if (enhanceItem != null && !string.IsNullOrEmpty(enhanceItem.enhanceItemIconPath))
            {
                Sprite icon = LoadSpriteFromPath(enhanceItem.enhanceItemIconPath);
                if (icon != null && enhanceItem.enhanceItemIcon != icon)
                {
                    enhanceItem.enhanceItemIcon = icon;
                    EditorUtility.SetDirty(enhanceItem);
                    updatedCount++;
                }
            }
        }

        Debug.Log($"強化アイテムアイコンを{updatedCount}件更新しました。");
    }

    [MenuItem("Tools/Master Data/Update Support Item Icons")]
    public static void UpdateSupportItemIcons()
    {
        string[] supportItemGuids = AssetDatabase.FindAssets("t:SupportItemMasterData", new[] { "Assets/GameData/SupportItem" });
        int updatedCount = 0;

        foreach (string guid in supportItemGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            SupportItemMasterData supportItem = AssetDatabase.LoadAssetAtPath<SupportItemMasterData>(assetPath);

            if (supportItem != null && !string.IsNullOrEmpty(supportItem.supportItemIconPath))
            {
                Sprite icon = LoadSpriteFromPath(supportItem.supportItemIconPath);
                if (icon != null && supportItem.supportItemIcon != icon)
                {
                    supportItem.supportItemIcon = icon;
                    EditorUtility.SetDirty(supportItem);
                    updatedCount++;
                }
            }
        }

        Debug.Log($"補助アイテムアイコンを{updatedCount}件更新しました。");
    }

    private static Sprite LoadSpriteFromPath(string iconPath)
    {
        // アイコンパスが相対パスの場合、Assetsフォルダからの相対パスとして処理
        string fullPath = iconPath;
        if (!fullPath.StartsWith("Assets/"))
        {
            fullPath = "Assets/" + iconPath;
        }

        // 拡張子がない場合は一般的な画像拡張子で検索
        string[] extensions = { "", ".png", ".jpg", ".jpeg", ".tga", ".psd" };

        foreach (string ext in extensions)
        {
            string testPath = fullPath + ext;

            // まずはSprite として直接読み込みを試す
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(testPath);
            if (sprite != null)
            {
                return sprite;
            }

            // Texture2Dとして読み込んでからSpriteを探す
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(testPath);
            if (texture != null)
            {
                // テクスチャからスプライトを取得
                string texturePath = AssetDatabase.GetAssetPath(texture);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);

                foreach (Object asset in assets)
                {
                    if (asset is Sprite spriteAsset)
                    {
                        return spriteAsset;
                    }
                }
            }
        }

        Debug.LogWarning($"アイコンが見つかりません: {iconPath}");
        return null;
    }

    [MenuItem("Tools/Master Data/Validate All Data")]
    public static void ValidateAllData()
    {
        ValidateEquipmentData();
        ValidateEnhanceItemData();
        ValidateSupportItemData();
        Debug.Log("全てのデータ検証が完了しました。");
    }

    private static void ValidateEquipmentData()
    {
        string[] equipmentGuids = AssetDatabase.FindAssets("t:EquipmentMasterData", new[] { "Assets/GameData/Equipment" });

        foreach (string guid in equipmentGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            EquipmentMasterData equipment = AssetDatabase.LoadAssetAtPath<EquipmentMasterData>(assetPath);

            if (equipment != null)
            {
                // 基本的な検証
                if (equipment.equipmentId <= 0)
                    Debug.LogError($"装備ID が無効です: {equipment.name}");

                if (string.IsNullOrEmpty(equipment.equipmentName))
                    Debug.LogError($"装備名が空です: {equipment.name}");

                if (equipment.maxEnhancedValue < equipment.baseEnhancedValue)
                    Debug.LogError($"最大強化値が基本強化値より小さいです: {equipment.name}");

                if (equipment.equipmentIcon == null && !string.IsNullOrEmpty(equipment.equipmentIconPath))
                    Debug.LogWarning($"アイコンが設定されていません: {equipment.name} (Path: {equipment.equipmentIconPath})");
            }
        }
    }

    private static void ValidateEnhanceItemData()
    {
        string[] enhanceItemGuids = AssetDatabase.FindAssets("t:EnhanceItemMasterData", new[] { "Assets/GameData/EnhanceItem" });

        foreach (string guid in enhanceItemGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            EnhanceItemMasterData enhanceItem = AssetDatabase.LoadAssetAtPath<EnhanceItemMasterData>(assetPath);

            if (enhanceItem != null)
            {
                if (enhanceItem.enhanceItemId <= 0)
                    Debug.LogError($"強化アイテムID が無効です: {enhanceItem.name}");

                if (string.IsNullOrEmpty(enhanceItem.enhanceItemName))
                    Debug.LogError($"強化アイテム名が空です: {enhanceItem.name}");

                if (enhanceItem.enhanceSuccessRate < 0 || enhanceItem.enhanceSuccessRate > 100)
                    Debug.LogWarning($"強化成功率が範囲外です (0-100): {enhanceItem.name} ({enhanceItem.enhanceSuccessRate}%)");

                if (enhanceItem.enhanceItemIcon == null && !string.IsNullOrEmpty(enhanceItem.enhanceItemIconPath))
                    Debug.LogWarning($"アイコンが設定されていません: {enhanceItem.name} (Path: {enhanceItem.enhanceItemIconPath})");
            }
        }
    }

    private static void ValidateSupportItemData()
    {
        string[] supportItemGuids = AssetDatabase.FindAssets("t:SupportItemMasterData", new[] { "Assets/GameData/SupportItem" });

        foreach (string guid in supportItemGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            SupportItemMasterData supportItem = AssetDatabase.LoadAssetAtPath<SupportItemMasterData>(assetPath);

            if (supportItem != null)
            {
                if (supportItem.supportItemId <= 0)
                    Debug.LogError($"補助アイテムID が無効です: {supportItem.name}");

                if (string.IsNullOrEmpty(supportItem.supportItemName))
                    Debug.LogError($"補助アイテム名が空です: {supportItem.name}");

                if (supportItem.supportItemIcon == null && !string.IsNullOrEmpty(supportItem.supportItemIconPath))
                    Debug.LogWarning($"アイコンが設定されていません: {supportItem.name} (Path: {supportItem.supportItemIconPath})");
            }
        }
    }
}