using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ユーザーの所持アイテム・装備を管理するクラス
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private int maxEquipmentSlots = 1000;
    [SerializeField] private int maxItemTypes = 500;

    // イベント
    public static event System.Action<UserEquipmentData> OnEquipmentAdded;
    public static event System.Action<UserEquipmentData> OnEquipmentRemoved;
    public static event System.Action<UserEquipmentData> OnEquipmentEquipped;
    public static event System.Action<UserEquipmentData> OnEquipmentUnequipped;
    public static event System.Action<UserItemData> OnItemAdded;
    public static event System.Action<UserItemData> OnItemUsed;
    public static event System.Action<UserItemData> OnItemRemoved;
    public static event System.Action OnInventoryChanged;

    // プロパティ
    public static InventoryManager Instance { get; private set; }
    public UserSaveData SaveData => SaveDataManager.Instance?.CurrentSaveData;
    public bool IsInitialized { get; private set; }

    // キャッシュ
    private Dictionary<string, UserEquipmentData> equipmentCache;
    private Dictionary<(ItemType, int), UserItemData> itemCache;
    private List<UserEquipmentData> equippedItemsCache;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCache();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // SaveDataManagerのイベントを購読
        SaveDataManager.OnDataLoaded += OnSaveDataLoaded;
        SaveDataManager.OnDataSaved += OnSaveDataSaved;
    }

    private void OnDestroy()
    {
        // イベント購読解除
        if (SaveDataManager.Instance != null)
        {
            SaveDataManager.OnDataLoaded -= OnSaveDataLoaded;
            SaveDataManager.OnDataSaved -= OnSaveDataSaved;
        }
    }

    #endregion

    #region 初期化

    private void InitializeCache()
    {
        equipmentCache = new Dictionary<string, UserEquipmentData>();
        itemCache = new Dictionary<(ItemType, int), UserItemData>();
        equippedItemsCache = new List<UserEquipmentData>();
    }

    private void OnSaveDataLoaded(UserSaveData saveData)
    {
        RefreshCache();
        IsInitialized = true;
        Debug.Log("[InventoryManager] インベントリを初期化しました");
    }

    private void OnSaveDataSaved(UserSaveData saveData)
    {
        // 保存時に必要な処理があればここに追加
    }

    /// <summary>
    /// キャッシュを更新
    /// </summary>
    public void RefreshCache()
    {
        if (SaveData == null) return;

        // 装備キャッシュ更新
        equipmentCache.Clear();
        equippedItemsCache.Clear();

        foreach (var equipment in SaveData.equipments)
        {
            equipmentCache[equipment.userEquipmentId] = equipment;
            if (equipment.isEquipped)
            {
                equippedItemsCache.Add(equipment);
            }
        }

        // アイテムキャッシュ更新
        itemCache.Clear();
        foreach (var item in SaveData.items)
        {
            itemCache[(item.itemType, item.itemMasterId)] = item;
        }

        OnInventoryChanged?.Invoke();
    }

    #endregion

    #region 装備管理

    /// <summary>
    /// 装備を追加
    /// </summary>
    public bool AddEquipment(int equipmentMasterId)
    {
        if (SaveData == null || !IsInitialized) return false;

        // スロット数チェック
        if (SaveData.equipments.Count >= maxEquipmentSlots)
        {
            Debug.LogWarning("装備スロットが満杯です");
            return false;
        }

        // マスターデータを取得（MasterDataManagerから）
        var masterData = MasterDataManager.Instance?.GetEquipmentData(equipmentMasterId);
        if (masterData == null)
        {
            Debug.LogError($"装備マスターデータが見つかりません: {equipmentMasterId}");
            return false;
        }

        var newEquipment = new UserEquipmentData(masterData);
        SaveData.AddEquipment(newEquipment);

        // キャッシュ更新
        equipmentCache[newEquipment.userEquipmentId] = newEquipment;

        // データ変更通知
        SaveDataManager.Instance.MarkDataDirty();
        OnEquipmentAdded?.Invoke(newEquipment);
        OnInventoryChanged?.Invoke();

        Debug.Log($"装備を追加しました: {masterData.equipmentName}");
        return true;
    }

    /// <summary>
    /// 装備を削除
    /// </summary>
    public bool RemoveEquipment(string userEquipmentId)
    {
        if (SaveData == null || !IsInitialized) return false;

        var equipment = GetEquipment(userEquipmentId);
        if (equipment == null) return false;

        // 装備中の場合は先に外す
        if (equipment.isEquipped)
        {
            UnequipItem(userEquipmentId);
        }

        bool removed = SaveData.RemoveEquipment(userEquipmentId);
        if (removed)
        {
            equipmentCache.Remove(userEquipmentId);
            SaveDataManager.Instance.MarkDataDirty();
            OnEquipmentRemoved?.Invoke(equipment);
            OnInventoryChanged?.Invoke();
        }

        return removed;
    }

    /// <summary>
    /// 装備を取得
    /// </summary>
    public UserEquipmentData GetEquipment(string userEquipmentId)
    {
        return equipmentCache.TryGetValue(userEquipmentId, out var equipment) ? equipment : null;
    }

    /// <summary>
    /// 装備一覧を取得
    /// </summary>
    public List<UserEquipmentData> GetAllEquipments()
    {
        return SaveData?.equipments?.ToList() ?? new List<UserEquipmentData>();
    }

    /// <summary>
    /// 装備タイプ別の装備一覧を取得
    /// </summary>
    public List<UserEquipmentData> GetEquipmentsByType(EquipmentType equipmentType)
    {
        if (!IsInitialized) return new List<UserEquipmentData>();

        return UserDataUtility.FilterEquipmentsByType(
            SaveData.equipments,
            equipmentType,
            MasterDataManager.Instance.GetEquipmentDataDict()
        );
    }

    /// <summary>
    /// 装備中のアイテム一覧を取得
    /// </summary>
    public List<UserEquipmentData> GetEquippedItems(string characterId = "")
    {
        if (string.IsNullOrEmpty(characterId))
        {
            return equippedItemsCache.ToList();
        }

        return equippedItemsCache.Where(eq => eq.equippedCharacterId == characterId).ToList();
    }

    /// <summary>
    /// 装備可能なアイテム一覧を取得
    /// </summary>
    public List<UserEquipmentData> GetEquippableItems(EquipmentType equipmentType)
    {
        if (!IsInitialized) return new List<UserEquipmentData>();

        return UserDataUtility.GetEquippableItems(
            SaveData.equipments,
            equipmentType,
            MasterDataManager.Instance.GetEquipmentDataDict()
        );
    }

    #endregion

    #region 装備・非装備

    /// <summary>
    /// 装備をキャラクターに装着
    /// </summary>
    public bool EquipItem(string userEquipmentId, string characterId = "default")
    {
        if (SaveData == null || !IsInitialized) return false;

        var equipment = GetEquipment(userEquipmentId);
        if (equipment == null || equipment.isEquipped) return false;

        var masterData = MasterDataManager.Instance?.GetEquipmentData(equipment.equipmentMasterId);
        if (masterData == null) return false;

        bool success = SaveData.EquipItem(userEquipmentId, characterId, masterData);
        if (success)
        {
            equippedItemsCache.Add(equipment);
            SaveDataManager.Instance.MarkDataDirty();
            OnEquipmentEquipped?.Invoke(equipment);
            OnInventoryChanged?.Invoke();
        }

        return success;
    }

    /// <summary>
    /// 装備を外す
    /// </summary>
    public bool UnequipItem(string userEquipmentId)
    {
        if (SaveData == null || !IsInitialized) return false;

        var equipment = GetEquipment(userEquipmentId);
        if (equipment == null || !equipment.isEquipped) return false;

        bool success = SaveData.UnEquipItem(userEquipmentId);
        if (success)
        {
            equippedItemsCache.RemoveAll(eq => eq.userEquipmentId == userEquipmentId);
            SaveDataManager.Instance.MarkDataDirty();
            OnEquipmentUnequipped?.Invoke(equipment);
            OnInventoryChanged?.Invoke();
        }

        return success;
    }

    #endregion

    #region アイテム管理

    /// <summary>
    /// アイテムを追加
    /// </summary>
    public bool AddItem(ItemType itemType, int itemMasterId, int quantity = 1)
    {
        if (SaveData == null || !IsInitialized || quantity <= 0) return false;

        UserItemData newItem;

        // マスターデータからアイテムを作成
        if (itemType == ItemType.EnhanceItem)
        {
            var masterData = MasterDataManager.Instance?.GetEnhanceItemData(itemMasterId);
            if (masterData == null)
            {
                Debug.LogError($"強化アイテムマスターデータが見つかりません: {itemMasterId}");
                return false;
            }
            newItem = new UserItemData(masterData, quantity);
        }
        else if (itemType == ItemType.SupportItem)
        {
            var masterData = MasterDataManager.Instance?.GetSupportItemData(itemMasterId);
            if (masterData == null)
            {
                Debug.LogError($"補助アイテムマスターデータが見つかりません: {itemMasterId}");
                return false;
            }
            newItem = new UserItemData(masterData, quantity);
        }
        else
        {
            return false;
        }

        SaveData.AddItem(newItem);

        // キャッシュ更新
        var key = (itemType, itemMasterId);
        if (itemCache.ContainsKey(key))
        {
            itemCache[key] = SaveData.items.Find(i => i.itemType == itemType && i.itemMasterId == itemMasterId);
        }
        else
        {
            itemCache[key] = newItem;
        }

        SaveDataManager.Instance.MarkDataDirty();
        OnItemAdded?.Invoke(newItem);
        OnInventoryChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// アイテムを使用
    /// </summary>
    public bool UseItem(ItemType itemType, int itemMasterId, int quantity = 1)
    {
        if (SaveData == null || !IsInitialized || quantity <= 0) return false;

        var item = GetItem(itemType, itemMasterId);
        if (item == null || !item.CanUse(quantity)) return false;

        bool success = SaveData.UseItem(itemType, itemMasterId, quantity);
        if (success)
        {
            // アイテムが完全に無くなった場合はキャッシュからも削除
            if (item.IsEmpty())
            {
                itemCache.Remove((itemType, itemMasterId));
            }

            SaveDataManager.Instance.MarkDataDirty();
            OnItemUsed?.Invoke(item);
            OnInventoryChanged?.Invoke();
        }

        return success;
    }

    /// <summary>
    /// アイテムを取得
    /// </summary>
    public UserItemData GetItem(ItemType itemType, int itemMasterId)
    {
        var key = (itemType, itemMasterId);
        return itemCache.TryGetValue(key, out var item) ? item : null;
    }

    /// <summary>
    /// アイテム所持数を取得
    /// </summary>
    public int GetItemQuantity(ItemType itemType, int itemMasterId)
    {
        return SaveData?.GetItemQuantity(itemType, itemMasterId) ?? 0;
    }

    /// <summary>
    /// 全アイテム一覧を取得
    /// </summary>
    public List<UserItemData> GetAllItems()
    {
        return SaveData?.items?.ToList() ?? new List<UserItemData>();
    }

    /// <summary>
    /// タイプ別アイテム一覧を取得
    /// </summary>
    public List<UserItemData> GetItemsByType(ItemType itemType)
    {
        if (!IsInitialized) return new List<UserItemData>();

        return UserDataUtility.FilterItemsByType(SaveData.items, itemType);
    }

    /// <summary>
    /// 新規取得アイテムを取得
    /// </summary>
    public List<UserItemData> GetNewItems()
    {
        if (!IsInitialized) return new List<UserItemData>();

        return UserDataUtility.GetNewItems(SaveData.items);
    }

    /// <summary>
    /// アイテムの新規フラグをクリア
    /// </summary>
    public void ClearItemNewFlag(ItemType itemType, int itemMasterId)
    {
        var item = GetItem(itemType, itemMasterId);
        if (item != null && item.isNew)
        {
            item.ClearNewFlag();
            SaveDataManager.Instance.MarkDataDirty();
            OnInventoryChanged?.Invoke();
        }
    }

    /// <summary>
    /// 全アイテムの新規フラグをクリア
    /// </summary>
    public void ClearAllNewFlags()
    {
        if (!IsInitialized) return;

        bool hasChanges = false;
        foreach (var item in SaveData.items)
        {
            if (item.isNew)
            {
                item.ClearNewFlag();
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            SaveDataManager.Instance.MarkDataDirty();
            OnInventoryChanged?.Invoke();
        }
    }

    #endregion

    #region 検索・フィルタ・ソート

    /// <summary>
    /// 装備をレアリティでフィルタ
    /// </summary>
    public List<UserEquipmentData> FilterEquipmentsByRarity(RarityType rarity)
    {
        if (!IsInitialized) return new List<UserEquipmentData>();

        return UserDataUtility.FilterEquipmentsByRarity(
            SaveData.equipments,
            rarity,
            MasterDataManager.Instance.GetEquipmentDataDict()
        );
    }

    /// <summary>
    /// 装備を強化値でソート
    /// </summary>
    public List<UserEquipmentData> SortEquipmentsByEnhancement(bool descending = true)
    {
        if (!IsInitialized) return new List<UserEquipmentData>();

        return UserDataUtility.SortEquipmentsByEnhancement(SaveData.equipments, descending);
    }

    /// <summary>
    /// 装備を取得日でソート
    /// </summary>
    public List<UserEquipmentData> SortEquipmentsByAcquiredDate(bool descending = true)
    {
        if (!IsInitialized) return new List<UserEquipmentData>();

        return UserDataUtility.SortEquipmentsByAcquiredDate(SaveData.equipments, descending);
    }

    /// <summary>
    /// アイテムを数量でソート
    /// </summary>
    public List<UserItemData> SortItemsByQuantity(bool descending = true)
    {
        if (!IsInitialized) return new List<UserItemData>();

        return UserDataUtility.SortItemsByQuantity(SaveData.items, descending);
    }

    /// <summary>
    /// 装備を検索
    /// </summary>
    public List<UserEquipmentData> SearchEquipments(Func<UserEquipmentData, bool> predicate)
    {
        if (!IsInitialized) return new List<UserEquipmentData>();

        return UserDataUtility.SearchItems(SaveData.equipments, predicate);
    }

    /// <summary>
    /// アイテムを検索
    /// </summary>
    public List<UserItemData> SearchItems(Func<UserItemData, bool> predicate)
    {
        if (!IsInitialized) return new List<UserItemData>();

        return UserDataUtility.SearchItems(SaveData.items, predicate);
    }

    #endregion

    #region 統計・計算

    /// <summary>
    /// アイテム所持状況サマリーを取得
    /// </summary>
    public ItemInventorySummary GetInventorySummary()
    {
        return SaveData?.GetItemSummary() ?? new ItemInventorySummary();
    }

    /// <summary>
    /// 装備の総合戦闘力を計算
    /// </summary>
    public int CalculateTotalPower(string characterId = "")
    {
        if (!IsInitialized) return 0;

        var equippedItems = GetEquippedItems(characterId);
        return UserDataUtility.CalculateTotalPower(
            equippedItems,
            MasterDataManager.Instance.GetEquipmentDataDict()
        );
    }

    /// <summary>
    /// インベントリの使用率を計算
    /// </summary>
    public float GetInventoryUsageRate()
    {
        if (!IsInitialized) return 0f;

        float equipmentUsage = (float)SaveData.equipments.Count / maxEquipmentSlots;
        float itemUsage = (float)SaveData.items.Count / maxItemTypes;

        return Mathf.Max(equipmentUsage, itemUsage);
    }

    /// <summary>
    /// 空きスロット数を取得
    /// </summary>
    public int GetAvailableEquipmentSlots()
    {
        if (!IsInitialized) return maxEquipmentSlots;

        return UserDataUtility.CalculateInventorySpace(SaveData.equipments, maxEquipmentSlots);
    }

    /// <summary>
    /// 強化可能装備数を取得
    /// </summary>
    public int GetEnhancableEquipmentCount()
    {
        if (!IsInitialized) return 0;

        var masterDataDict = MasterDataManager.Instance.GetEquipmentDataDict();
        return SaveData.equipments.Count(eq =>
            masterDataDict.ContainsKey(eq.equipmentMasterId) &&
            eq.CanEnhance(masterDataDict[eq.equipmentMasterId])
        );
    }

    #endregion

    #region 装備強化関連

    /// <summary>
    /// 装備強化が可能かチェック
    /// </summary>
    public bool CanEnhanceEquipment(string userEquipmentId, ItemType enhanceItemType, int enhanceItemMasterId)
    {
        if (!IsInitialized) return false;

        var equipment = GetEquipment(userEquipmentId);
        var enhanceItem = GetItem(enhanceItemType, enhanceItemMasterId);

        if (equipment == null || enhanceItem == null) return false;

        return UserDataUtility.CanEnhanceEquipment(
            equipment,
            enhanceItem,
            MasterDataManager.Instance.GetEquipmentDataDict()
        );
    }

    /// <summary>
    /// 装備強化に使用可能なアイテム一覧を取得
    /// </summary>
    public List<UserItemData> GetAvailableEnhanceItems()
    {
        if (!IsInitialized) return new List<UserItemData>();

        return GetItemsByType(ItemType.EnhanceItem).Where(item => item.quantity > 0).ToList();
    }

    /// <summary>
    /// 補助アイテム一覧を取得
    /// </summary>
    public List<UserItemData> GetAvailableSupportItems()
    {
        if (!IsInitialized) return new List<UserItemData>();

        return GetItemsByType(ItemType.SupportItem).Where(item => item.quantity > 0).ToList();
    }

    #endregion

    #region ユーティリティ

    /// <summary>
    /// インベントリデータの整合性をチェック
    /// </summary>
    public List<string> ValidateInventoryData()
    {
        if (!IsInitialized) return new List<string> { "インベントリが初期化されていません" };

        return UserDataUtility.ValidateUserData(SaveData);
    }

    /// <summary>
    /// インベントリの統計情報を取得
    /// </summary>
    public string GetInventoryStatistics()
    {
        if (!IsInitialized) return "データが読み込まれていません";

        var summary = GetInventorySummary();
        var availableSlots = GetAvailableEquipmentSlots();
        var totalPower = CalculateTotalPower();
        var enhancableCount = GetEnhancableEquipmentCount();

        return $@"=== インベントリ統計 ===
装備数: {SaveData.equipments.Count}/{maxEquipmentSlots} (空き: {availableSlots})
強化アイテム: {summary.totalEnhanceItems}種類 {summary.totalEnhanceQuantity}個
補助アイテム: {summary.totalSupportItems}種類 {summary.totalSupportQuantity}個
新規アイテム: {summary.newItemCount}個
総合戦闘力: {totalPower}
強化可能装備: {enhancableCount}個
使用率: {GetInventoryUsageRate():P1}";
    }

    /// <summary>
    /// デバッグ用：詳細なインベントリ状態を取得
    /// </summary>
    public string GetDetailedInventoryStatus()
    {
        if (!IsInitialized)
            return "InventoryManager が初期化されていません";

        if (SaveData == null)
            return "SaveData が null です";

        string status = $@"=== InventoryManager 詳細状態 ===
IsInitialized: {IsInitialized}
SaveData != null: {SaveData != null}
Equipment Count: {SaveData?.equipments?.Count ?? 0}
Items Count: {SaveData?.items?.Count ?? 0}

=== キャッシュ状態 ===
equipmentCache Count: {equipmentCache?.Count ?? 0}
itemCache Count: {itemCache?.Count ?? 0}
equippedItemsCache Count: {equippedItemsCache?.Count ?? 0}

=== アイテム詳細 ===";

        if (SaveData?.items != null)
        {
            foreach (var item in SaveData.items)
            {
                status += $"\n- {item.itemType} ID:{item.itemMasterId} x{item.quantity}";
            }
        }

        status += "\n\n=== GetItemsByType結果 ===";
        var enhanceItems = GetItemsByType(ItemType.EnhanceItem);
        var supportItems = GetItemsByType(ItemType.SupportItem);

        status += $"\nEnhanceItem: {enhanceItems.Count}件";
        status += $"\nSupportItem: {supportItems.Count}件";

        return status;
    }

    /// <summary>
    /// 特定の条件に一致する装備の数を取得
    /// </summary>
    public int CountEquipments(Func<UserEquipmentData, bool> predicate)
    {
        if (!IsInitialized) return 0;

        return SaveData.equipments.Count(predicate);
    }

    /// <summary>
    /// 特定の条件に一致するアイテムの数を取得
    /// </summary>
    public int CountItems(Func<UserItemData, bool> predicate)
    {
        if (!IsInitialized) return 0;

        return SaveData.items.Count(predicate);
    }

    /// <summary>
    /// 装備のロック状態を切り替え
    /// </summary>
    public bool ToggleEquipmentLock(string userEquipmentId)
    {
        var equipment = GetEquipment(userEquipmentId);
        if (equipment == null) return false;

        equipment.isLocked = !equipment.isLocked;
        SaveDataManager.Instance.MarkDataDirty();
        OnInventoryChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 装備のお気に入り状態を切り替え
    /// </summary>
    public bool ToggleEquipmentFavorite(string userEquipmentId)
    {
        var equipment = GetEquipment(userEquipmentId);
        if (equipment == null) return false;

        equipment.isFavorite = !equipment.isFavorite;
        SaveDataManager.Instance.MarkDataDirty();
        OnInventoryChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// アイテムのロック状態を切り替え
    /// </summary>
    public bool ToggleItemLock(ItemType itemType, int itemMasterId)
    {
        var item = GetItem(itemType, itemMasterId);
        if (item == null) return false;

        item.isLocked = !item.isLocked;
        SaveDataManager.Instance.MarkDataDirty();
        OnInventoryChanged?.Invoke();

        return true;
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("インベントリ統計を表示")]
    private void ShowInventoryStatistics()
    {
        Debug.Log(GetInventoryStatistics());
    }

    [ContextMenu("インベントリデータを検証")]
    private void ValidateInventory()
    {
        var errors = ValidateInventoryData();
        if (errors.Count == 0)
        {
            Debug.Log("インベントリデータに問題はありません");
        }
        else
        {
            Debug.LogError($"インベントリデータに{errors.Count}個の問題があります:\n" + string.Join("\n", errors));
        }
    }

    [ContextMenu("キャッシュを更新")]
    private void RefreshCacheManual()
    {
        RefreshCache();
        Debug.Log("キャッシュを手動更新しました");
    }

    [ContextMenu("テスト装備を追加")]
    private void AddTestEquipment()
    {
        AddEquipment(1); // 初心者の剣
        Debug.Log("テスト装備を追加しました");
    }

    [ContextMenu("テストアイテムを追加")]
    private void AddTestItems()
    {
        AddItem(ItemType.EnhanceItem, 1, 5);
        AddItem(ItemType.SupportItem, 1, 3);
        Debug.Log("テストアイテムを追加しました");
    }
#endif

    #endregion
}