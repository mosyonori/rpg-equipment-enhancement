using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ユーザーの所持スキルを管理するクラス
/// </summary>
public class SkillManager : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private int maxSkillSlots = 1000;
    [SerializeField] private bool enableDebugLog = true;

    // イベント
    public static event System.Action<UserSkillData> OnSkillAdded;
    public static event System.Action<UserSkillData> OnSkillRemoved;
    public static event System.Action<string, string> OnSkillEquipped;    // equipmentId, skillId
    public static event System.Action<string> OnSkillUnequipped;          // equipmentId
    public static event System.Action OnSkillInventoryChanged;

    // プロパティ
    public static SkillManager Instance { get; private set; }
    public UserSaveData SaveData => SaveDataManager.Instance?.CurrentSaveData;
    public bool IsInitialized { get; private set; }

    // キャッシュ
    private Dictionary<string, UserSkillData> skillCache;
    private Dictionary<string, string> equipmentSkillCache; // equipmentId -> skillId

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
        DebugLog("SkillManager初期化開始 - 依存関係チェック中...");

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

        DebugLog("SkillManager初期化実行");

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
        DebugLog("SkillManager初期化完了");
    }

    #endregion

    #region デバッグ

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SkillManager] {message}");
        }
    }

    private void DebugLogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[SkillManager] {message}");
        }
    }

    #endregion

    #region 既存の初期化（RefreshCache等）

    private void InitializeCache()
    {
        skillCache = new Dictionary<string, UserSkillData>();
        equipmentSkillCache = new Dictionary<string, string>();
    }

    private void OnSaveDataLoaded(UserSaveData saveData)
    {
        RefreshCache();
        DebugLog("スキルインベントリを初期化しました");
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

        // スキルキャッシュ更新
        skillCache.Clear();
        foreach (var skill in SaveData.skills)
        {
            skillCache[skill.userSkillId] = skill;
        }

        // 装備スキルキャッシュ更新
        equipmentSkillCache.Clear();
        foreach (var equipment in SaveData.equipments)
        {
            if (!string.IsNullOrEmpty(equipment.equippedSkillId))
            {
                equipmentSkillCache[equipment.userEquipmentId] = equipment.equippedSkillId;
            }
        }

        OnSkillInventoryChanged?.Invoke();
    }

    #endregion

    #region スキル管理

    /// <summary>
    /// スキルを追加
    /// </summary>
    public bool AddSkill(int skillMasterId)
    {
        if (SaveData == null || !IsInitialized) return false;

        // スロット数チェック
        if (SaveData.skills.Count >= maxSkillSlots)
        {
            Debug.LogWarning("スキルスロットが満杯です");
            return false;
        }

        // マスターデータを取得（MasterDataManagerから）
        var masterData = MasterDataManager.Instance?.GetSkillData(skillMasterId);
        if (masterData == null)
        {
            Debug.LogError($"スキルマスターデータが見つかりません: {skillMasterId}");
            return false;
        }

        var newSkill = new UserSkillData(masterData);
        SaveData.AddSkill(newSkill);

        // キャッシュ更新
        skillCache[newSkill.userSkillId] = newSkill;

        // データ変更通知
        SaveDataManager.Instance.MarkDataDirty();
        OnSkillAdded?.Invoke(newSkill);
        OnSkillInventoryChanged?.Invoke();

        Debug.Log($"スキルを追加しました: {masterData.skillName}");
        return true;
    }

    /// <summary>
    /// スキルを削除
    /// </summary>
    public bool RemoveSkill(string userSkillId)
    {
        if (SaveData == null || !IsInitialized) return false;

        var skill = GetSkill(userSkillId);
        if (skill == null) return false;

        // 装備中の場合は先に解除
        UnequipSkillFromAllEquipments(userSkillId);

        bool removed = SaveData.RemoveSkill(userSkillId);
        if (removed)
        {
            skillCache.Remove(userSkillId);
            SaveDataManager.Instance.MarkDataDirty();
            OnSkillRemoved?.Invoke(skill);
            OnSkillInventoryChanged?.Invoke();
        }

        return removed;
    }

    /// <summary>
    /// スキルを取得
    /// </summary>
    public UserSkillData GetSkill(string userSkillId)
    {
        return skillCache.TryGetValue(userSkillId, out var skill) ? skill : null;
    }

    /// <summary>
    /// 全スキル一覧を取得
    /// </summary>
    public List<UserSkillData> GetAllSkills()
    {
        return SaveData?.skills?.ToList() ?? new List<UserSkillData>();
    }

    /// <summary>
    /// レアリティ別スキル一覧を取得
    /// </summary>
    public List<UserSkillData> GetSkillsByRarity(RarityType rarity)
    {
        if (!IsInitialized) return new List<UserSkillData>();

        return UserDataUtility.FilterSkillsByRarity(
            SaveData.skills,
            rarity,
            MasterDataManager.Instance.GetSkillDataDict()
        );
    }

    /// <summary>
    /// 属性別スキル一覧を取得
    /// </summary>
    public List<UserSkillData> GetSkillsByAttribute(AttributeType attributeType)
    {
        if (!IsInitialized) return new List<UserSkillData>();

        return UserDataUtility.FilterSkillsByAttribute(
            SaveData.skills,
            attributeType,
            MasterDataManager.Instance.GetSkillDataDict()
        );
    }

    /// <summary>
    /// ターゲットタイプ別スキル一覧を取得
    /// </summary>
    public List<UserSkillData> GetSkillsByTargetType(TargetType targetType)
    {
        if (!IsInitialized) return new List<UserSkillData>();

        return UserDataUtility.FilterSkillsByTargetType(
            SaveData.skills,
            targetType,
            MasterDataManager.Instance.GetSkillDataDict()
        );
    }

    /// <summary>
    /// 新規取得スキルを取得
    /// </summary>
    public List<UserSkillData> GetNewSkills()
    {
        if (!IsInitialized) return new List<UserSkillData>();

        return UserDataUtility.GetNewSkills(SaveData.skills);
    }

    #endregion

    #region 装備・スキル関連管理

    /// <summary>
    /// 装備にスキルを装着
    /// </summary>
    public bool EquipSkillToEquipment(string equipmentId, string skillId)
    {
        if (SaveData == null || !IsInitialized) return false;

        var equipment = SaveData.GetEquipment(equipmentId);
        var skill = GetSkill(skillId);

        if (equipment == null || skill == null)
        {
            DebugLog($"装備またはスキルが見つかりません: Equipment={equipmentId}, Skill={skillId}");
            return false;
        }

        // 既に別のスキルが装備されている場合は解除
        if (!string.IsNullOrEmpty(equipment.equippedSkillId))
        {
            UnequipSkillFromEquipment(equipmentId);
        }

        // 同じスキルが他の装備に装着されている場合は解除
        UnequipSkillFromAllEquipments(skillId);

        // スキルを装備に装着
        equipment.EquipSkill(skillId);
        equipmentSkillCache[equipmentId] = skillId;

        SaveDataManager.Instance.MarkDataDirty();
        OnSkillEquipped?.Invoke(equipmentId, skillId);
        OnSkillInventoryChanged?.Invoke();

        var masterData = MasterDataManager.Instance?.GetSkillData(skill.skillMasterId);
        DebugLog($"スキル装備完了: {masterData?.skillName} → 装備 {equipmentId}");

        return true;
    }

    /// <summary>
    /// 装備からスキルを解除
    /// </summary>
    public bool UnequipSkillFromEquipment(string equipmentId)
    {
        if (SaveData == null || !IsInitialized) return false;

        var equipment = SaveData.GetEquipment(equipmentId);
        if (equipment == null || !equipment.HasEquippedSkill())
        {
            DebugLog($"装備にスキルが装着されていません: {equipmentId}");
            return false;
        }

        equipment.UnequipSkill();
        equipmentSkillCache.Remove(equipmentId);

        SaveDataManager.Instance.MarkDataDirty();
        OnSkillUnequipped?.Invoke(equipmentId);
        OnSkillInventoryChanged?.Invoke();

        DebugLog($"スキル解除完了: 装備 {equipmentId}");
        return true;
    }

    /// <summary>
    /// 指定スキルを全ての装備から解除
    /// </summary>
    private void UnequipSkillFromAllEquipments(string skillId)
    {
        if (SaveData == null) return;

        var equippedEquipments = SaveData.equipments.Where(eq => eq.equippedSkillId == skillId).ToList();

        foreach (var equipment in equippedEquipments)
        {
            equipment.UnequipSkill();
            equipmentSkillCache.Remove(equipment.userEquipmentId);
            OnSkillUnequipped?.Invoke(equipment.userEquipmentId);
        }

        if (equippedEquipments.Count > 0)
        {
            SaveDataManager.Instance.MarkDataDirty();
            OnSkillInventoryChanged?.Invoke();
        }
    }

    /// <summary>
    /// 装備可能なスキル一覧を取得
    /// </summary>
    public List<UserSkillData> GetAvailableSkills()
    {
        if (!IsInitialized) return new List<UserSkillData>();

        // 現在他の装備に装着されていないスキルを取得
        var equippedSkillIds = equipmentSkillCache.Values.ToHashSet();

        return SaveData.skills.Where(skill =>
            !equippedSkillIds.Contains(skill.userSkillId)
        ).ToList();
    }

    /// <summary>
    /// 装備中のスキル一覧を取得
    /// </summary>
    public List<UserSkillData> GetEquippedSkills()
    {
        if (!IsInitialized) return new List<UserSkillData>();

        var equippedSkillIds = equipmentSkillCache.Values.ToHashSet();

        return SaveData.skills.Where(skill =>
            equippedSkillIds.Contains(skill.userSkillId)
        ).ToList();
    }

    /// <summary>
    /// 装備に装着されているスキルを取得
    /// </summary>
    public UserSkillData GetEquippedSkill(string equipmentId)
    {
        if (!equipmentSkillCache.TryGetValue(equipmentId, out var skillId))
            return null;

        return GetSkill(skillId);
    }

    #endregion

    #region 検索・フィルタ・ソート

    /// <summary>
    /// スキルを取得日でソート
    /// </summary>
    public List<UserSkillData> SortSkillsByAcquiredDate(bool descending = true)
    {
        if (!IsInitialized) return new List<UserSkillData>();

        return UserDataUtility.SortSkillsByAcquiredDate(SaveData.skills, descending);
    }

    /// <summary>
    /// スキルを検索
    /// </summary>
    public List<UserSkillData> SearchSkills(Func<UserSkillData, bool> predicate)
    {
        if (!IsInitialized) return new List<UserSkillData>();

        return UserDataUtility.SearchItems(SaveData.skills, predicate);
    }

    #endregion

    #region 統計・計算

    /// <summary>
    /// スキルインベントリサマリーを取得
    /// </summary>
    public SkillInventorySummary GetSkillInventorySummary()
    {
        if (!IsInitialized) return new SkillInventorySummary();

        var summary = new SkillInventorySummary();
        var masterDataDict = MasterDataManager.Instance?.GetSkillDataDict();

        if (masterDataDict == null) return summary;

        summary.totalSkills = SaveData.skills.Count;
        summary.newSkillCount = SaveData.skills.Count(s => s.isNew);
        summary.equippedSkillCount = equipmentSkillCache.Count;
        summary.availableSkillCount = summary.totalSkills - summary.equippedSkillCount;

        // レアリティ別統計
        foreach (var skill in SaveData.skills)
        {
            if (masterDataDict.ContainsKey(skill.skillMasterId))
            {
                var masterData = masterDataDict[skill.skillMasterId];
                switch (masterData.rarity)
                {
                    case RarityType.Common:
                        summary.commonSkillCount++;
                        break;
                    case RarityType.Rare:
                        summary.rareSkillCount++;
                        break;
                    case RarityType.Epic:
                        summary.epicSkillCount++;
                        break;
                    case RarityType.Legendary:
                        summary.legendarySkillCount++;
                        break;
                }
            }
        }

        return summary;
    }

    /// <summary>
    /// 空きスキルスロット数を取得
    /// </summary>
    public int GetAvailableSkillSlots()
    {
        if (!IsInitialized) return maxSkillSlots;

        return maxSkillSlots - SaveData.skills.Count;
    }

    #endregion

    #region ユーティリティ

    /// <summary>
    /// スキルの新規フラグをクリア
    /// </summary>
    public void ClearSkillNewFlag(string userSkillId)
    {
        var skill = GetSkill(userSkillId);
        if (skill != null && skill.isNew)
        {
            skill.ClearNewFlag();
            SaveDataManager.Instance.MarkDataDirty();
            OnSkillInventoryChanged?.Invoke();
        }
    }

    /// <summary>
    /// 全スキルの新規フラグをクリア
    /// </summary>
    public void ClearAllNewFlags()
    {
        if (!IsInitialized) return;

        bool hasChanges = false;
        foreach (var skill in SaveData.skills)
        {
            if (skill.isNew)
            {
                skill.ClearNewFlag();
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            SaveDataManager.Instance.MarkDataDirty();
            OnSkillInventoryChanged?.Invoke();
        }
    }

    /// <summary>
    /// スキルデータの整合性をチェック
    /// </summary>
    public List<string> ValidateSkillData()
    {
        if (!IsInitialized) return new List<string> { "SkillManagerが初期化されていません" };

        var errors = new List<string>();

        // 重複チェック
        var duplicateSkillIds = SaveData.skills
            .GroupBy(skill => skill.userSkillId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateId in duplicateSkillIds)
            errors.Add($"重複するスキルID: {duplicateId}");

        // 装備関連性チェック
        foreach (var equipmentSkill in equipmentSkillCache)
        {
            var equipment = SaveData.GetEquipment(equipmentSkill.Key);
            var skill = GetSkill(equipmentSkill.Value);

            if (equipment == null)
                errors.Add($"存在しない装備にスキルが装着されています: {equipmentSkill.Key}");

            if (skill == null)
                errors.Add($"存在しないスキルが装備に装着されています: {equipmentSkill.Value}");

            if (equipment != null && equipment.equippedSkillId != equipmentSkill.Value)
                errors.Add($"装備とキャッシュのスキル情報が不一致: 装備{equipmentSkill.Key}");
        }

        return errors;
    }

    /// <summary>
    /// スキル統計情報を取得
    /// </summary>
    public string GetSkillStatistics()
    {
        if (!IsInitialized) return "データが読み込まれていません";

        var summary = GetSkillInventorySummary();
        var availableSlots = GetAvailableSkillSlots();

        return $@"=== スキル統計 ===
スキル数: {SaveData.skills.Count}/{maxSkillSlots} (空き: {availableSlots})
新規スキル: {summary.newSkillCount}個
装備中スキル: {summary.equippedSkillCount}個
利用可能スキル: {summary.availableSkillCount}個

レアリティ別:
- Common: {summary.commonSkillCount}個
- Rare: {summary.rareSkillCount}個
- Epic: {summary.epicSkillCount}個
- Legendary: {summary.legendarySkillCount}個";
    }

    /// <summary>
    /// 指定の条件に一致するスキルの数を取得
    /// </summary>
    public int CountSkills(Func<UserSkillData, bool> predicate)
    {
        if (!IsInitialized) return 0;

        return SaveData.skills.Count(predicate);
    }

    /// <summary>
    /// スキルのロック状態を切り替え
    /// </summary>
    public bool ToggleSkillLock(string userSkillId)
    {
        var skill = GetSkill(userSkillId);
        if (skill == null) return false;

        skill.ToggleLock();
        SaveDataManager.Instance.MarkDataDirty();
        OnSkillInventoryChanged?.Invoke();

        return true;
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("スキル統計を表示")]
    private void ShowSkillStatistics()
    {
        Debug.Log(GetSkillStatistics());
    }

    [ContextMenu("スキルデータを検証")]
    private void ValidateSkills()
    {
        var errors = ValidateSkillData();
        if (errors.Count == 0)
        {
            Debug.Log("スキルデータに問題はありません");
        }
        else
        {
            Debug.LogError($"スキルデータに{errors.Count}個の問題があります:\n" + string.Join("\n", errors));
        }
    }

    [ContextMenu("キャッシュを更新")]
    private void RefreshCacheManual()
    {
        RefreshCache();
        Debug.Log("キャッシュを手動更新しました");
    }

    [ContextMenu("テストスキルを追加")]
    private void AddTestSkill()
    {
        AddSkill(1); // 基本攻撃スキル
        Debug.Log("テストスキルを追加しました");
    }
#endif

    #endregion
}

/// <summary>
/// スキルインベントリサマリー
/// </summary>
[System.Serializable]
public class SkillInventorySummary
{
    public int totalSkills;         // 総スキル数
    public int newSkillCount;       // 新規スキル数
    public int equippedSkillCount;  // 装備中スキル数
    public int availableSkillCount; // 利用可能スキル数
    public int commonSkillCount;    // コモンスキル数
    public int rareSkillCount;      // レアスキル数
    public int epicSkillCount;      // エピックスキル数
    public int legendarySkillCount; // レジェンダリースキル数
}