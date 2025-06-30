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
    [SerializeField] private bool enableDebugLog = true;

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
            // 修正: 依存関係の初期化完了を待機
            StartCoroutine(WaitForDependenciesAndInitialize());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 修正: Start()からイベント購読を削除（初期化完了後に行う）
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

    #region 修正: 初期化処理

    /// <summary>
    /// 依存関係の初期化完了を待機してから初期化
    /// </summary>
    private System.Collections.IEnumerator WaitForDependenciesAndInitialize()
    {
        DebugLog("InventoryManager初期化開始 - 依存関係チェック中...");

        float timeout = 15f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (CheckDependencies())
            {
                DebugLog("依存関係確認完了 - 初期化実行");
                Initialize();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        DebugLogError($"依存関係の初期化がタイムアウトしました（{timeout}秒）");
        DebugLogError("MasterDataManagerとSaveDataManagerがシーンに配置され、正常に初期化されているか確認してください");
    }

    /// <summary>
    /// 依存するマネージャーの初期化チェック
    /// </summary>
    /// <returns>全ての依存関係が満たされている場合true</returns>
    private bool CheckDependencies()
    {
        // MasterDataManagerチェック
        if (MasterDataManager.Instance == null)
        {
            DebugLog("MasterDataManager.Instanceがnullです");
            return false;
        }

        if (!MasterDataManager.Instance.IsDataLoaded)
        {
            DebugLog($"MasterDataManagerのデータ読み込みが未完了です (IsDataLoaded: {MasterDataManager.Instance.IsDataLoaded})");
            return false;
        }

        // SaveDataManagerチェック
        if (SaveDataManager.Instance == null)
        {
            DebugLog("SaveDataManager.Instanceがnullです");
            return false;
        }

        if (!SaveDataManager.Instance.IsDataLoaded)
        {
            DebugLog($"SaveDataManagerのデータ読み込みが未完了です (IsDataLoaded: {SaveDataManager.Instance.IsDataLoaded})");
            return false;
        }

        DebugLog("全ての依存関係が満たされています");
        return true;
    }

    /// <summary>
    /// マネージャーの初期化
    /// </summary>
    private void Initialize()
    {
        if (IsInitialized)
        {
            DebugLog("既に初期化済みです");
            return;
        }

        DebugLog("InventoryManager初期化実行");

        // 依存するマネージャーの最終チェック
        if (!CheckDependencies())
        {
            DebugLogError("初期化時に依存関係チェックに失敗しました");
            return;
        }

        // イベント購読をここで行う
        SaveDataManager.OnDataLoaded += OnSaveDataLoaded;
        SaveDataManager.OnDataSaved += OnSaveDataSaved;

        // 既にデータが読み込まれている場合は即座に初期化
        if (SaveData != null)
        {
            RefreshCache();
        }

        IsInitialized = true;
        DebugLog("InventoryManager初期化完了");
    }

    #endregion

    #region デバッグ

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[InventoryManager] {message}");
        }
    }

    private void DebugLogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[InventoryManager] {message}");
        }
    }

    #endregion

    #region 既存の初期化（RefreshCache等）

    private void InitializeCache()
    {
        equipmentCache = new Dictionary<string, UserEquipmentData>();
        itemCache = new Dictionary<(ItemType, int), UserItemData>();
        equippedItemsCache = new List<UserEquipmentData>();
    }

    private void OnSaveDataLoaded(UserSaveData saveData)
    {
        RefreshCache();
        DebugLog("インベントリを初期化しました");
    }

    private void OnSaveDataSaved(UserSaveData saveData)
    {
        // 保存後に必要な処理があればここに追加
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

        // 装備リストの整合性をチェック・修正
        ValidateAndFixEquipmentLists();

        // アイテムキャッシュ更新
        itemCache.Clear();
        foreach (var item in SaveData.items)
        {
            itemCache[(item.itemType, item.itemMasterId)] = item;
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 装備リストの整合性をチェック・修正
    /// </summary>
    private void ValidateAndFixEquipmentLists()
    {
        var masterDataDict = MasterDataManager.Instance?.GetEquipmentDataDict();
        if (masterDataDict == null) return;

        // 装備中アイテムから正しい装備リストを再構築
        SaveData.equippedWeaponIds.Clear();
        SaveData.equippedArmorIds.Clear();
        SaveData.equippedAccessoryIds.Clear();

        foreach (var equipment in SaveData.equipments)
        {
            if (!equipment.isEquipped) continue;

            if (masterDataDict.ContainsKey(equipment.equipmentMasterId))
            {
                var masterData = masterDataDict[equipment.equipmentMasterId];
                switch (masterData.equipmentType)
                {
                    case EquipmentType.Weapon:
                        SaveData.equippedWeaponIds.Add(equipment.userEquipmentId);
                        break;
                    case EquipmentType.Armor:
                        SaveData.equippedArmorIds.Add(equipment.userEquipmentId);
                        break;
                    case EquipmentType.Accessory:
                        SaveData.equippedAccessoryIds.Add(equipment.userEquipmentId);
                        break;
                }
            }
        }

        DebugLog($"装備リスト修正: 武器{SaveData.equippedWeaponIds.Count}, 防具{SaveData.equippedArmorIds.Count}, アクセサリー{SaveData.equippedAccessoryIds.Count}");
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
    /// 装備削除前の条件チェック（UI用）
    /// </summary>
    public (bool canDelete, string errorMessage) CanDeleteEquipment(string userEquipmentId)
    {
        var equipment = GetEquipment(userEquipmentId);
        if (equipment == null)
            return (false, "装備が見つかりません");

        if (equipment.isEquipped)
            return (false, "装備中は削除できません");

        if (equipment.isLocked)
            return (false, "装備はロック中です");

        return (true, "");
    }

    /// <summary>
    /// 装備削除（UI用ラッパー）
    /// </summary>
    public bool DeleteEquipment(string userEquipmentId)
    {
        return RemoveEquipment(userEquipmentId);
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
        if (equipment == null || equipment.isEquipped)
        {
            DebugLog($"装備不可: 装備が見つからないか既に装備中です ({userEquipmentId})");
            return false;
        }

        var masterData = MasterDataManager.Instance?.GetEquipmentData(equipment.equipmentMasterId);
        if (masterData == null)
        {
            DebugLogError($"マスターデータが見つかりません: {equipment.equipmentMasterId}");
            return false;
        }

        DebugLog($"装備処理開始: {masterData.equipmentName} (Type: {masterData.equipmentType})");

        // 1. 同じタイプの既存装備を先に外す
        UnequipSameTypeItem(characterId, masterData.equipmentType);

        // 2. 新しい装備を装着
        bool success = SaveData.EquipItem(userEquipmentId, characterId, masterData);
        if (success)
        {
            // 3. キャッシュを更新
            RefreshEquippedCache();

            SaveDataManager.Instance.MarkDataDirty();
            OnEquipmentEquipped?.Invoke(equipment);
            OnInventoryChanged?.Invoke();

            DebugLog($"装備完了: {masterData.equipmentName}");
        }
        else
        {
            DebugLogError($"装備処理に失敗: {userEquipmentId}");
        }

        return success;
    }

    /// <summary>
    /// 同じタイプの装備を外す（内部処理用）
    /// </summary>
    private void UnequipSameTypeItem(string characterId, EquipmentType equipmentType)
    {
        var currentEquipped = GetEquippedItemByType(equipmentType);
        if (currentEquipped != null)
        {
            DebugLog($"既存装備を外します: {currentEquipped.userEquipmentId} (Type: {equipmentType})");
            UnequipItem(currentEquipped.userEquipmentId);
        }
    }

    /// <summary>
    /// 装備キャッシュを強制更新
    /// </summary>
    private void RefreshEquippedCache()
    {
        equippedItemsCache.Clear();

        foreach (var equipment in SaveData.equipments)
        {
            if (equipment.isEquipped)
            {
                equippedItemsCache.Add(equipment);
            }
        }

        DebugLog($"装備キャッシュ更新: {equippedItemsCache.Count}個の装備");
    }

    /// <summary>
    /// 装備を外す
    /// </summary>
    public bool UnequipItem(string userEquipmentId)
    {
        if (SaveData == null || !IsInitialized)
        {
            DebugLog("SaveDataまたはInventoryManagerが初期化されていません");
            return false;
        }

        var equipment = GetEquipment(userEquipmentId);
        if (equipment == null)
        {
            DebugLog($"装備が見つかりません: {userEquipmentId}");
            return false;
        }

        if (!equipment.isEquipped)
        {
            DebugLog($"装備は既に外されています: {userEquipmentId}");
            return false; // 既に外されている場合はfalseを返す
        }

        bool success = SaveData.UnEquipItem(userEquipmentId);
        if (success)
        {
            equippedItemsCache.RemoveAll(eq => eq.userEquipmentId == userEquipmentId);
            SaveDataManager.Instance.MarkDataDirty();
            OnEquipmentUnequipped?.Invoke(equipment);
            OnInventoryChanged?.Invoke();
            DebugLog($"装備を外しました: {userEquipmentId}");
        }
        else
        {
            DebugLogError($"装備解除処理に失敗しました: {userEquipmentId}");
        }

        return success;
    }

    /// <summary>
    /// 指定された装備タイプの装備中アイテムを取得
    /// </summary>
    public UserEquipmentData GetEquippedItemByType(EquipmentType equipmentType)
    {
        if (!IsInitialized) return null;

        var masterDataDict = MasterDataManager.Instance?.GetEquipmentDataDict();
        if (masterDataDict == null) return null;

        var equippedItems = GetEquippedItems();
        return equippedItems.Find(eq =>
        {
            if (masterDataDict.ContainsKey(eq.equipmentMasterId))
            {
                return masterDataDict[eq.equipmentMasterId].equipmentType == equipmentType;
            }
            return false;
        });
    }

    /// <summary>
    /// 指定された装備タイプの装備を外す
    /// </summary>
    public bool UnequipItemByType(EquipmentType equipmentType)
    {
        var equippedItem = GetEquippedItemByType(equipmentType);
        if (equippedItem == null)
        {
            DebugLog($"外す装備が見つかりません: {equipmentType}");
            return false;
        }

        return UnequipItem(equippedItem.userEquipmentId);
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

        // マスターデータからアイテムを構成
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
    /// 装備の合計戦闘力を計算
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

        var errors = UserDataUtility.ValidateUserData(SaveData);

        // 装備の重複チェックを追加
        var equipmentDuplicateErrors = CheckEquipmentDuplicates();
        errors.AddRange(equipmentDuplicateErrors);

        return errors;
    }

    /// <summary>
    /// 装備の重複をチェック
    /// </summary>
    private List<string> CheckEquipmentDuplicates()
    {
        var errors = new List<string>();
        var masterDataDict = MasterDataManager.Instance?.GetEquipmentDataDict();

        if (masterDataDict == null) return errors;

        // タイプ別に装備中アイテムを分類
        var weaponCount = 0;
        var armorCount = 0;
        var accessoryCount = 0;

        foreach (var equipment in SaveData.equipments)
        {
            if (!equipment.isEquipped) continue;

            if (masterDataDict.ContainsKey(equipment.equipmentMasterId))
            {
                var masterData = masterDataDict[equipment.equipmentMasterId];
                switch (masterData.equipmentType)
                {
                    case EquipmentType.Weapon:
                        weaponCount++;
                        break;
                    case EquipmentType.Armor:
                        armorCount++;
                        break;
                    case EquipmentType.Accessory:
                        accessoryCount++;
                        break;
                }
            }
        }

        // 重複チェック
        if (weaponCount > 1) errors.Add($"武器が{weaponCount}個装備されています（1個まで）");
        if (armorCount > 1) errors.Add($"防具が{armorCount}個装備されています（1個まで）");
        if (accessoryCount > 1) errors.Add($"アクセサリーが{accessoryCount}個装備されています（1個まで）");

        return errors;
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
合計戦闘力: {totalPower}
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

        status += $"\nEnhanceItem: {enhanceItems.Count}個";
        status += $"\nSupportItem: {supportItems.Count}個";

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

    [ContextMenu("詳細状態を表示")]
    private void ShowDetailedStatus()
    {
        Debug.Log(GetDetailedInventoryStatus());
    }
#endif

    #endregion
}