using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// マスターデータの読み込み・管理を行うクラス
/// </summary>
public class MasterDataManager : MonoBehaviour
{
    [Header("データ読み込み設定")]
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private bool enableDebugLog = true;

    [Header("データパス設定")]
    [SerializeField] private string equipmentDataPath = "GameData/Equipment";
    [SerializeField] private string enhanceItemDataPath = "GameData/EnhanceItem";
    [SerializeField] private string supportItemDataPath = "GameData/SupportItem";

    // イベント
    public static event System.Action OnMasterDataLoaded;
    public static event System.Action<string> OnMasterDataLoadError;

    // プロパティ
    public static MasterDataManager Instance { get; private set; }
    public bool IsDataLoaded { get; private set; }

    // マスターデータ辞書
    private Dictionary<int, EquipmentMasterData> equipmentDataDict;
    private Dictionary<int, EnhanceItemMasterData> enhanceItemDataDict;
    private Dictionary<int, SupportItemMasterData> supportItemDataDict;

    // リスト形式のデータ（フィルタ・ソート用）
    private List<EquipmentMasterData> equipmentDataList;
    private List<EnhanceItemMasterData> enhanceItemDataList;
    private List<SupportItemMasterData> supportItemDataList;

    // キャッシュ用データ
    private Dictionary<EquipmentType, List<EquipmentMasterData>> equipmentsByType;
    private Dictionary<RarityType, List<EquipmentMasterData>> equipmentsByRarity;
    private Dictionary<AttributeType, List<EnhanceItemMasterData>> enhanceItemsByAttribute;
    private Dictionary<AttributeType, List<SupportItemMasterData>> supportItemsByAttribute;

    #region Unity Lifecycle

    private void Awake()
    {
        // シングルトン設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCollections();

            if (loadOnAwake)
            {
                LoadAllMasterData();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region 初期化

    private void InitializeCollections()
    {
        equipmentDataDict = new Dictionary<int, EquipmentMasterData>();
        enhanceItemDataDict = new Dictionary<int, EnhanceItemMasterData>();
        supportItemDataDict = new Dictionary<int, SupportItemMasterData>();

        equipmentDataList = new List<EquipmentMasterData>();
        enhanceItemDataList = new List<EnhanceItemMasterData>();
        supportItemDataList = new List<SupportItemMasterData>();

        equipmentsByType = new Dictionary<EquipmentType, List<EquipmentMasterData>>();
        equipmentsByRarity = new Dictionary<RarityType, List<EquipmentMasterData>>();
        enhanceItemsByAttribute = new Dictionary<AttributeType, List<EnhanceItemMasterData>>();
        supportItemsByAttribute = new Dictionary<AttributeType, List<SupportItemMasterData>>();
    }

    #endregion

    #region 公開メソッド - データ読み込み

    /// <summary>
    /// 全マスターデータを読み込み
    /// </summary>
    public bool LoadAllMasterData()
    {
        try
        {
            DebugLog("マスターデータの読み込みを開始します");

            bool success = true;
            success &= LoadEquipmentData();
            success &= LoadEnhanceItemData();
            success &= LoadSupportItemData();

            if (success)
            {
                BuildCacheData();
                IsDataLoaded = true;
                DebugLog("全マスターデータの読み込みが完了しました");
                OnMasterDataLoaded?.Invoke();
            }
            else
            {
                string error = "一部のマスターデータの読み込みに失敗しました";
                DebugLogError(error);
                OnMasterDataLoadError?.Invoke(error);
            }

            return success;
        }
        catch (Exception e)
        {
            string error = $"マスターデータ読み込み中にエラーが発生: {e.Message}";
            DebugLogError(error);
            OnMasterDataLoadError?.Invoke(error);
            return false;
        }
    }

    /// <summary>
    /// 装備データを読み込み
    /// </summary>
    public bool LoadEquipmentData()
    {
        try
        {
            var equipmentAssets = Resources.LoadAll<EquipmentMasterData>(equipmentDataPath);

            equipmentDataDict.Clear();
            equipmentDataList.Clear();

            foreach (var equipment in equipmentAssets)
            {
                if (equipment == null) continue;

                if (equipmentDataDict.ContainsKey(equipment.equipmentId))
                {
                    DebugLogError($"重複する装備ID: {equipment.equipmentId} ({equipment.equipmentName})");
                    continue;
                }

                equipmentDataDict[equipment.equipmentId] = equipment;
                equipmentDataList.Add(equipment);
            }

            DebugLog($"装備データを{equipmentDataList.Count}件読み込みました");
            return true;
        }
        catch (Exception e)
        {
            DebugLogError($"装備データ読み込みエラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 強化アイテムデータを読み込み
    /// </summary>
    public bool LoadEnhanceItemData()
    {
        try
        {
            var enhanceItemAssets = Resources.LoadAll<EnhanceItemMasterData>(enhanceItemDataPath);

            enhanceItemDataDict.Clear();
            enhanceItemDataList.Clear();

            foreach (var enhanceItem in enhanceItemAssets)
            {
                if (enhanceItem == null) continue;

                if (enhanceItemDataDict.ContainsKey(enhanceItem.enhanceItemId))
                {
                    DebugLogError($"重複する強化アイテムID: {enhanceItem.enhanceItemId} ({enhanceItem.enhanceItemName})");
                    continue;
                }

                enhanceItemDataDict[enhanceItem.enhanceItemId] = enhanceItem;
                enhanceItemDataList.Add(enhanceItem);
            }

            DebugLog($"強化アイテムデータを{enhanceItemDataList.Count}件読み込みました");
            return true;
        }
        catch (Exception e)
        {
            DebugLogError($"強化アイテムデータ読み込みエラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 補助アイテムデータを読み込み
    /// </summary>
    public bool LoadSupportItemData()
    {
        try
        {
            var supportItemAssets = Resources.LoadAll<SupportItemMasterData>(supportItemDataPath);

            supportItemDataDict.Clear();
            supportItemDataList.Clear();

            foreach (var supportItem in supportItemAssets)
            {
                if (supportItem == null) continue;

                if (supportItemDataDict.ContainsKey(supportItem.supportItemId))
                {
                    DebugLogError($"重複する補助アイテムID: {supportItem.supportItemId} ({supportItem.supportItemName})");
                    continue;
                }

                supportItemDataDict[supportItem.supportItemId] = supportItem;
                supportItemDataList.Add(supportItem);
            }

            DebugLog($"補助アイテムデータを{supportItemDataList.Count}件読み込みました");
            return true;
        }
        catch (Exception e)
        {
            DebugLogError($"補助アイテムデータ読み込みエラー: {e.Message}");
            return false;
        }
    }

    #endregion

    #region 公開メソッド - データ取得（単体）

    /// <summary>
    /// 装備データを取得
    /// </summary>
    public EquipmentMasterData GetEquipmentData(int equipmentId)
    {
        return equipmentDataDict.TryGetValue(equipmentId, out var data) ? data : null;
    }

    /// <summary>
    /// 強化アイテムデータを取得
    /// </summary>
    public EnhanceItemMasterData GetEnhanceItemData(int enhanceItemId)
    {
        return enhanceItemDataDict.TryGetValue(enhanceItemId, out var data) ? data : null;
    }

    /// <summary>
    /// 補助アイテムデータを取得
    /// </summary>
    public SupportItemMasterData GetSupportItemData(int supportItemId)
    {
        return supportItemDataDict.TryGetValue(supportItemId, out var data) ? data : null;
    }

    #endregion

    #region 公開メソッド - データ取得（辞書・リスト）

    /// <summary>
    /// 装備データ辞書を取得
    /// </summary>
    public Dictionary<int, EquipmentMasterData> GetEquipmentDataDict()
    {
        return new Dictionary<int, EquipmentMasterData>(equipmentDataDict);
    }

    /// <summary>
    /// 強化アイテムデータ辞書を取得
    /// </summary>
    public Dictionary<int, EnhanceItemMasterData> GetEnhanceItemDataDict()
    {
        return new Dictionary<int, EnhanceItemMasterData>(enhanceItemDataDict);
    }

    /// <summary>
    /// 補助アイテムデータ辞書を取得
    /// </summary>
    public Dictionary<int, SupportItemMasterData> GetSupportItemDataDict()
    {
        return new Dictionary<int, SupportItemMasterData>(supportItemDataDict);
    }

    /// <summary>
    /// 装備データリストを取得
    /// </summary>
    public List<EquipmentMasterData> GetEquipmentDataList()
    {
        return new List<EquipmentMasterData>(equipmentDataList);
    }

    /// <summary>
    /// 強化アイテムデータリストを取得
    /// </summary>
    public List<EnhanceItemMasterData> GetEnhanceItemDataList()
    {
        return new List<EnhanceItemMasterData>(enhanceItemDataList);
    }

    /// <summary>
    /// 補助アイテムデータリストを取得
    /// </summary>
    public List<SupportItemMasterData> GetSupportItemDataList()
    {
        return new List<SupportItemMasterData>(supportItemDataList);
    }

    #endregion

    #region 公開メソッド - フィルタ取得

    /// <summary>
    /// 装備タイプ別の装備データを取得
    /// </summary>
    public List<EquipmentMasterData> GetEquipmentsByType(EquipmentType equipmentType)
    {
        if (equipmentsByType.TryGetValue(equipmentType, out var list))
        {
            return new List<EquipmentMasterData>(list);
        }
        return new List<EquipmentMasterData>();
    }

    /// <summary>
    /// レアリティ別の装備データを取得
    /// </summary>
    public List<EquipmentMasterData> GetEquipmentsByRarity(RarityType rarity)
    {
        if (equipmentsByRarity.TryGetValue(rarity, out var list))
        {
            return new List<EquipmentMasterData>(list);
        }
        return new List<EquipmentMasterData>();
    }

    /// <summary>
    /// 属性別の強化アイテムデータを取得
    /// </summary>
    public List<EnhanceItemMasterData> GetEnhanceItemsByAttribute(AttributeType attributeType)
    {
        if (enhanceItemsByAttribute.TryGetValue(attributeType, out var list))
        {
            return new List<EnhanceItemMasterData>(list);
        }
        return new List<EnhanceItemMasterData>();
    }

    /// <summary>
    /// 属性別の補助アイテムデータを取得
    /// </summary>
    public List<SupportItemMasterData> GetSupportItemsByAttribute(AttributeType attributeType)
    {
        if (supportItemsByAttribute.TryGetValue(attributeType, out var list))
        {
            return new List<SupportItemMasterData>(list);
        }
        return new List<SupportItemMasterData>();
    }

    /// <summary>
    /// レアリティ別の強化アイテムデータを取得
    /// </summary>
    public List<EnhanceItemMasterData> GetEnhanceItemsByRarity(RarityType rarity)
    {
        return enhanceItemDataList.Where(item => item.rarity == rarity).ToList();
    }

    /// <summary>
    /// レアリティ別の補助アイテムデータを取得
    /// </summary>
    public List<SupportItemMasterData> GetSupportItemsByRarity(RarityType rarity)
    {
        return supportItemDataList.Where(item => item.rarity == rarity).ToList();
    }

    #endregion

    #region 公開メソッド - 検索・条件指定

    /// <summary>
    /// 装備データを条件で検索
    /// </summary>
    public List<EquipmentMasterData> SearchEquipments(Func<EquipmentMasterData, bool> predicate)
    {
        return equipmentDataList.Where(predicate).ToList();
    }

    /// <summary>
    /// 強化アイテムデータを条件で検索
    /// </summary>
    public List<EnhanceItemMasterData> SearchEnhanceItems(Func<EnhanceItemMasterData, bool> predicate)
    {
        return enhanceItemDataList.Where(predicate).ToList();
    }

    /// <summary>
    /// 補助アイテムデータを条件で検索
    /// </summary>
    public List<SupportItemMasterData> SearchSupportItems(Func<SupportItemMasterData, bool> predicate)
    {
        return supportItemDataList.Where(predicate).ToList();
    }

    /// <summary>
    /// 名前で装備を検索
    /// </summary>
    public List<EquipmentMasterData> SearchEquipmentsByName(string name)
    {
        return equipmentDataList.Where(eq => eq.equipmentName.Contains(name)).ToList();
    }

    /// <summary>
    /// 特定の強化値に達するとスキルが解放される装備を取得
    /// </summary>
    public List<EquipmentMasterData> GetEquipmentsWithSkillUnlock()
    {
        return equipmentDataList.Where(eq => eq.equipmentUnlockSkillId > 0).ToList();
    }

    /// <summary>
    /// 特定の強化値に達するとキャラクターが解放される装備を取得
    /// </summary>
    public List<EquipmentMasterData> GetEquipmentsWithCharacterUnlock()
    {
        return equipmentDataList.Where(eq => !string.IsNullOrEmpty(eq.equipmentUnlockCharacterId)).ToList();
    }

    #endregion

    #region 公開メソッド - 統計・検証

    /// <summary>
    /// マスターデータの統計情報を取得
    /// </summary>
    public MasterDataStatistics GetStatistics()
    {
        return new MasterDataStatistics
        {
            totalEquipments = equipmentDataList.Count,
            totalEnhanceItems = enhanceItemDataList.Count,
            totalSupportItems = supportItemDataList.Count,
            equipmentsByType = equipmentsByType.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            equipmentsByRarity = equipmentsByRarity.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            enhanceItemsByAttribute = enhanceItemsByAttribute.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            supportItemsByAttribute = supportItemsByAttribute.ToDictionary(kv => kv.Key, kv => kv.Value.Count)
        };
    }

    /// <summary>
    /// マスターデータの整合性を検証
    /// </summary>
    public List<string> ValidateMasterData()
    {
        List<string> errors = new List<string>();

        // 装備データの検証
        foreach (var equipment in equipmentDataList)
        {
            if (equipment.equipmentId <= 0)
                errors.Add($"無効な装備ID: {equipment.equipmentId}");

            if (string.IsNullOrEmpty(equipment.equipmentName))
                errors.Add($"装備名が空: ID {equipment.equipmentId}");

            if (equipment.maxEnhancedValue < equipment.baseEnhancedValue)
                errors.Add($"最大強化値が基本値より小さい: {equipment.equipmentName}");

            if (equipment.maxEnhanceStamina < equipment.baseEnhanceStamina)
                errors.Add($"最大強化耐久値が基本値より小さい: {equipment.equipmentName}");
        }

        // 強化アイテムデータの検証
        foreach (var enhanceItem in enhanceItemDataList)
        {
            if (enhanceItem.enhanceItemId <= 0)
                errors.Add($"無効な強化アイテムID: {enhanceItem.enhanceItemId}");

            if (string.IsNullOrEmpty(enhanceItem.enhanceItemName))
                errors.Add($"強化アイテム名が空: ID {enhanceItem.enhanceItemId}");

            if (enhanceItem.enhanceSuccessRate < 0 || enhanceItem.enhanceSuccessRate > 100)
                errors.Add($"強化成功率が範囲外: {enhanceItem.enhanceItemName} ({enhanceItem.enhanceSuccessRate}%)");
        }

        // 補助アイテムデータの検証
        foreach (var supportItem in supportItemDataList)
        {
            if (supportItem.supportItemId <= 0)
                errors.Add($"無効な補助アイテムID: {supportItem.supportItemId}");

            if (string.IsNullOrEmpty(supportItem.supportItemName))
                errors.Add($"補助アイテム名が空: ID {supportItem.supportItemId}");
        }

        return errors;
    }

    /// <summary>
    /// データが存在するかチェック
    /// </summary>
    public bool HasData()
    {
        return IsDataLoaded &&
               equipmentDataList.Count > 0 &&
               enhanceItemDataList.Count > 0 &&
               supportItemDataList.Count > 0;
    }

    #endregion

    #region 内部メソッド

    /// <summary>
    /// キャッシュデータを構築
    /// </summary>
    private void BuildCacheData()
    {
        // 装備タイプ別キャッシュ
        equipmentsByType.Clear();
        foreach (EquipmentType type in Enum.GetValues(typeof(EquipmentType)))
        {
            equipmentsByType[type] = equipmentDataList.Where(eq => eq.equipmentType == type).ToList();
        }

        // 装備レアリティ別キャッシュ
        equipmentsByRarity.Clear();
        foreach (RarityType rarity in Enum.GetValues(typeof(RarityType)))
        {
            equipmentsByRarity[rarity] = equipmentDataList.Where(eq => eq.rarity == rarity).ToList();
        }

        // 強化アイテム属性別キャッシュ
        enhanceItemsByAttribute.Clear();
        foreach (AttributeType attribute in Enum.GetValues(typeof(AttributeType)))
        {
            enhanceItemsByAttribute[attribute] = enhanceItemDataList.Where(item => item.attributeType == attribute).ToList();
        }

        // 補助アイテム属性別キャッシュ
        supportItemsByAttribute.Clear();
        foreach (AttributeType attribute in Enum.GetValues(typeof(AttributeType)))
        {
            supportItemsByAttribute[attribute] = supportItemDataList.Where(item => item.attributeType == attribute).ToList();
        }

        DebugLog("キャッシュデータの構築が完了しました");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MasterDataManager] {message}");
        }
    }

    private void DebugLogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[MasterDataManager] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("マスターデータを再読み込み")]
    private void ReloadMasterData()
    {
        LoadAllMasterData();
    }

    [ContextMenu("マスターデータ統計を表示")]
    private void ShowStatistics()
    {
        var stats = GetStatistics();
        Debug.Log(stats.ToString());
    }

    [ContextMenu("マスターデータを検証")]
    private void ValidateData()
    {
        var errors = ValidateMasterData();
        if (errors.Count == 0)
        {
            Debug.Log("マスターデータに問題はありません");
        }
        else
        {
            Debug.LogError($"マスターデータに{errors.Count}個の問題があります:\n" + string.Join("\n", errors));
        }
    }
#endif

    #endregion
}

/// <summary>
/// マスターデータの統計情報
/// </summary>
[System.Serializable]
public class MasterDataStatistics
{
    public int totalEquipments;
    public int totalEnhanceItems;
    public int totalSupportItems;
    public Dictionary<EquipmentType, int> equipmentsByType;
    public Dictionary<RarityType, int> equipmentsByRarity;
    public Dictionary<AttributeType, int> enhanceItemsByAttribute;
    public Dictionary<AttributeType, int> supportItemsByAttribute;

    public override string ToString()
    {
        var result = $@"=== マスターデータ統計 ===
装備数: {totalEquipments}
強化アイテム数: {totalEnhanceItems}
補助アイテム数: {totalSupportItems}

装備タイプ別:";

        foreach (var kv in equipmentsByType)
        {
            result += $"\n  {kv.Key}: {kv.Value}個";
        }

        result += "\n\n装備レアリティ別:";
        foreach (var kv in equipmentsByRarity)
        {
            result += $"\n  {kv.Key}: {kv.Value}個";
        }

        return result;
    }
}