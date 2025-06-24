using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

/// <summary>
/// データの読み書き専用Manager（バランス版）
/// - 責任：データの読み書きのみ
/// - ビジネスロジック一切なし
/// - 計算処理一切なし
/// - UI更新処理一切なし
/// </summary>
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // 初期化状態（読み取り専用プロパティ）
    public bool IsInitialized { get; private set; } = false;

    [Header("Data Containers")]
    private SaveDataContainer saveDataContainer;
    private MasterDataContainer masterDataContainer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeContainers();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeContainers()
    {
        saveDataContainer = new SaveDataContainer();
        masterDataContainer = new MasterDataContainer();
    }

    // ===== マスターデータ関連（読み込みのみ） =====

    /// <summary>
    /// マスターデータ初期化
    /// </summary>
    public IEnumerator InitializeMasterDataAsync()
    {
        bool success = false;
        System.Exception error = null;

        yield return StartCoroutine(ExecuteWithErrorHandling(
            masterDataContainer.LoadAllMasterDataAsync(),
            (result, ex) => { success = result; error = ex; }
        ));

        if (success)
        {
            IsInitialized = true;
            Debug.Log("DataManager: マスターデータ初期化完了");
        }
        else
        {
            IsInitialized = false;
            Debug.LogError($"DataManager: マスターデータ初期化エラー - {error?.Message}");
        }
    }

    /// <summary>
    /// マスターデータ再読み込み
    /// </summary>
    public IEnumerator RefreshMasterDataAsync()
    {
        bool success = false;
        System.Exception error = null;

        yield return StartCoroutine(ExecuteWithErrorHandling(
            masterDataContainer.RefreshDataAsync(),
            (result, ex) => { success = result; error = ex; }
        ));

        if (success)
        {
            Debug.Log("DataManager: マスターデータ更新完了");
        }
        else
        {
            Debug.LogError($"DataManager: マスターデータ更新エラー - {error?.Message}");
        }
    }

    /// <summary>
    /// マスターデータの有効性チェック
    /// </summary>
    public bool HasValidMasterData()
    {
        return masterDataContainer.IsValidData();
    }

    // ===== 装備マスターデータ取得 =====

    /// <summary>
    /// 装備マスターデータ取得
    /// </summary>
    public EquipmentMasterData GetEquipmentMasterData(int equipmentId)
    {
        return masterDataContainer.GetEquipmentMaster(equipmentId);
    }

    /// <summary>
    /// 全装備マスターデータ取得
    /// </summary>
    public List<EquipmentMasterData> GetAllEquipmentMasterData()
    {
        return masterDataContainer.GetAllEquipmentMasters();
    }

    // ===== 強化アイテムマスターデータ取得 =====

    /// <summary>
    /// 強化アイテムマスターデータ取得
    /// </summary>
    public EnhanceItemMasterData GetEnhanceItemMasterData(int enhanceItemId)
    {
        return masterDataContainer.GetEnhanceItemMaster(enhanceItemId);
    }

    /// <summary>
    /// 全強化アイテムマスターデータ取得
    /// </summary>
    public List<EnhanceItemMasterData> GetAllEnhanceItemMasterData()
    {
        return masterDataContainer.GetAllEnhanceItemMasters();
    }

    // ===== 補助材料マスターデータ取得 =====

    /// <summary>
    /// 補助材料マスターデータ取得
    /// </summary>
    public SupportItemMasterData GetSupportItemMasterData(int supportItemId)
    {
        return masterDataContainer.GetSupportItemMaster(supportItemId);
    }

    /// <summary>
    /// 全補助材料マスターデータ取得
    /// </summary>
    public List<SupportItemMasterData> GetAllSupportItemMasterData()
    {
        return masterDataContainer.GetAllSupportItemMasters();
    }

    // ===== クエストマスターデータ取得 =====

    /// <summary>
    /// クエストマスターデータ取得
    /// </summary>
    public QuestMasterData GetQuestData(int questId)
    {
        return masterDataContainer.GetQuestMaster(questId);
    }

    /// <summary>
    /// 全クエストマスターデータ取得
    /// </summary>
    public List<QuestMasterData> GetQuestMasterData()
    {
        return masterDataContainer.GetAllQuestMasters();
    }

    // ===== ユーザーデータ関連（読み書き） =====

    /// <summary>
    /// ユーザーデータ読み込み
    /// </summary>
    public IEnumerator LoadUserDataAsync()
    {
        bool success = false;
        System.Exception error = null;

        yield return StartCoroutine(ExecuteWithErrorHandling(
            saveDataContainer.LoadAllUserDataAsync(),
            (result, ex) => { success = result; error = ex; }
        ));

        if (success)
        {
            Debug.Log("DataManager: ユーザーデータ読み込み完了");
            yield return true;
        }
        else
        {
            Debug.LogError($"DataManager: ユーザーデータ読み込みエラー - {error?.Message}");
            yield return false;
        }
    }

    /// <summary>
    /// ユーザー装備データ読み込み
    /// </summary>
    public IEnumerator LoadUserEquipmentDataAsync()
    {
        bool success = false;
        System.Exception error = null;

        yield return StartCoroutine(ExecuteWithErrorHandling(
            saveDataContainer.LoadUserEquipmentsAsync(),
            (result, ex) => { success = result; error = ex; }
        ));

        if (!success)
        {
            Debug.LogError($"DataManager: ユーザー装備データ読み込みエラー - {error?.Message}");
        }
    }

    /// <summary>
    /// ユーザーアイテムデータ読み込み
    /// </summary>
    public IEnumerator LoadUserItemDataAsync()
    {
        bool success = false;
        System.Exception error = null;

        yield return StartCoroutine(ExecuteWithErrorHandling(
            saveDataContainer.LoadUserItemsAsync(),
            (result, ex) => { success = result; error = ex; }
        ));

        if (!success)
        {
            Debug.LogError($"DataManager: ユーザーアイテムデータ読み込みエラー - {error?.Message}");
        }
    }

    /// <summary>
    /// ユーザースキルデータ読み込み
    /// </summary>
    public IEnumerator LoadUserSkillDataAsync()
    {
        bool success = false;
        System.Exception error = null;

        yield return StartCoroutine(ExecuteWithErrorHandling(
            saveDataContainer.LoadUserSkillsAsync(),
            (result, ex) => { success = result; error = ex; }
        ));
    }

    // ===== ユーザープロフィール =====

    /// <summary>
    /// ユーザープロフィール取得
    /// </summary>
    public UserProfile GetUserProfile()
    {
        return saveDataContainer.GetUserProfile();
    }

    /// <summary>
    /// ユーザープロフィール保存
    /// </summary>
    public void SaveUserProfile(UserProfile userProfile)
    {
        saveDataContainer.SaveUserProfile(userProfile);
    }

    // ===== ユーザー装備データ =====

    /// <summary>
    /// ユーザー装備一覧取得
    /// </summary>
    public List<UserEquipment> GetUserEquipments()
    {
        return saveDataContainer.GetUserEquipments();
    }

    /// <summary>
    /// ユーザー装備保存
    /// </summary>
    public void SaveUserEquipment(UserEquipment equipment)
    {
        saveDataContainer.SaveUserEquipment(equipment);
    }

    // ===== ユーザーアイテムデータ =====

    /// <summary>
    /// ユーザーアイテム一覧取得
    /// </summary>
    public List<UserItem> GetUserItems()
    {
        return saveDataContainer.GetUserItems();
    }

    /// <summary>
    /// ユーザーアイテム消費
    /// </summary>
    public void ConsumeUserItem(int itemId, int quantity)
    {
        saveDataContainer.ConsumeUserItem(itemId, quantity);
    }

    // ===== ユーザースキルデータ =====

    /// <summary>
    /// ユーザースキル一覧取得
    /// </summary>
    public List<UserSkill> GetUserSkills()
    {
        return saveDataContainer.GetUserSkills();
    }

    /// <summary>
    /// エラーハンドリング付きコルーチン実行
    /// </summary>
    private IEnumerator ExecuteWithErrorHandling(IEnumerator coroutine, System.Action<bool, System.Exception> callback)
    {
        bool success = true;
        System.Exception caughtException = null;

        // コルーチンを実行し、例外をキャッチ
        bool completed = false;
        StartCoroutine(ExecuteCoroutineWithErrorCapture(coroutine,
            (result, ex) => {
                success = result;
                caughtException = ex;
                completed = true;
            }));

        // 完了まで待機
        yield return new WaitUntil(() => completed);

        callback?.Invoke(success, caughtException);
    }

    private IEnumerator ExecuteCoroutineWithErrorCapture(IEnumerator coroutine, System.Action<bool, System.Exception> callback)
    {
        bool success = true;
        System.Exception caughtException = null;

        while (true)
        {
            object current;
            try
            {
                if (!coroutine.MoveNext())
                    break;

                current = coroutine.Current;
            }
            catch (System.Exception ex)
            {
                success = false;
                caughtException = ex;
                break;
            }

            yield return current;
        }

        callback?.Invoke(success, caughtException);
    }

    // ===== 設定データ =====

    /// <summary>
    /// 音響設定取得
    /// </summary>
    public AudioSettingsData GetAudioSettings()
    {
        return saveDataContainer.GetAudioSettings();
    }

    /// <summary>
    /// 音響設定保存
    /// </summary>
    public void SaveAudioSettings(AudioSettingsData audioSettings)
    {
        saveDataContainer.SaveAudioSettings(audioSettings);
    }

    // ===== デバッグ用メソッド =====

    /// <summary>
    /// データ整合性チェック（デバッグ用）
    /// </summary>
    public void ValidateAllData()
    {
        Debug.Log("=== DataManager データ整合性チェック ===");

        // マスターデータチェック
        Debug.Log($"マスターデータ初期化状態: {IsInitialized}");
        Debug.Log($"マスターデータ有効性: {HasValidMasterData()}");

        // ユーザーデータチェック
        UserProfile profile = GetUserProfile();
        Debug.Log($"ユーザープロフィール: {(profile != null ? "存在" : "なし")}");

        List<UserEquipment> equipments = GetUserEquipments();
        Debug.Log($"ユーザー装備数: {equipments?.Count ?? 0}");

        List<UserItem> items = GetUserItems();
        Debug.Log($"ユーザーアイテム数: {items?.Count ?? 0}");

        Debug.Log("=== データ整合性チェック完了 ===");
    }
}

// ===== データコンテナクラス =====

/// <summary>
/// マスターデータ管理コンテナ
/// </summary>
public class MasterDataContainer
{
    private Dictionary<int, EquipmentMasterData> equipmentMasters = new Dictionary<int, EquipmentMasterData>();
    private Dictionary<int, EnhanceItemMasterData> enhanceItemMasters = new Dictionary<int, EnhanceItemMasterData>();
    private Dictionary<int, SupportItemMasterData> supportItemMasters = new Dictionary<int, SupportItemMasterData>();
    private Dictionary<int, QuestMasterData> questMasters = new Dictionary<int, QuestMasterData>();

    /// <summary>
    /// 全マスターデータ読み込み
    /// </summary>
    public IEnumerator LoadAllMasterDataAsync()
    {
        // CSVファイルからマスターデータを読み込み
        yield return LoadEquipmentMasters();
        yield return LoadEnhanceItemMasters();
        yield return LoadSupportItemMasters();
        yield return LoadQuestMasters();

        Debug.Log("MasterDataContainer: 全マスターデータ読み込み完了");
    }

    /// <summary>
    /// マスターデータ更新
    /// </summary>
    public IEnumerator RefreshDataAsync()
    {
        // データをクリアして再読み込み
        equipmentMasters.Clear();
        enhanceItemMasters.Clear();
        supportItemMasters.Clear();
        questMasters.Clear();

        yield return LoadAllMasterDataAsync();
    }

    /// <summary>
    /// データ有効性チェック
    /// </summary>
    public bool IsValidData()
    {
        return equipmentMasters.Count > 0 &&
               enhanceItemMasters.Count > 0 &&
               supportItemMasters.Count > 0 &&
               questMasters.Count > 0;
    }

    // ===== 個別データ読み込み（実装予定） =====

    private IEnumerator LoadEquipmentMasters()
    {
        // TODO: m_equipment_data.csv 読み込み
        // 仮のテストデータを作成
        CreateTestEquipmentData();
        yield return null;
    }

    private IEnumerator LoadEnhanceItemMasters()
    {
        // TODO: m_enhance_item_data.csv 読み込み
        // 仮のテストデータを作成
        CreateTestEnhanceItemData();
        yield return null;
    }

    private IEnumerator LoadSupportItemMasters()
    {
        // TODO: m_support_item_data.csv 読み込み
        // 仮のテストデータを作成
        CreateTestSupportItemData();
        yield return null;
    }

    private IEnumerator LoadQuestMasters()
    {
        // TODO: m_quest_data.csv 読み込み
        // 仮のテストデータを作成
        CreateTestQuestData();
        yield return null;
    }

    // ===== テストデータ作成（CSV実装まで） =====

    private void CreateTestEquipmentData()
    {
        var equipment1 = new EquipmentMasterData
        {
            equipment_id = 1,
            equipment_name = "初心者の剣",
            equipment_type = EquipmentType.Weapon,
            rarity = "common",
            base_enhanced_value = 0,
            max_enhanced_value = 999,
            min_enhanced_value = 0,
            base_enhance_stamina = 100,
            equipment_icon_path = "Icons/sword_beginner",
            description = "初心者が使う剣。最低限の攻撃力がある。",
            hp = 0,
            offense = 1,
            defense = 0,
            speed = 0,
            critical_rate = 10,
            critical_damage_rate = 150,
            fire_offence = 0,
            water_offence = 0,
            wind_offence = 0,
            earth_offence = 0
        };

        equipmentMasters[1] = equipment1;
        Debug.Log("MasterDataContainer: テスト装備データ作成完了");
    }

    private void CreateTestEnhanceItemData()
    {
        var enhanceItem1 = new EnhanceItemMasterData
        {
            enhance_item_id = 1,
            enhance_item_name = "低級強化石",
            attribute_type = "normal",
            rarity = "common",
            max_stack_value = 999,
            add_enhanced_value = 1,
            reduce_enhanced_value = 0,
            add_enhance_stamina = 0,
            reduce_enhance_stamina = 1,
            enhance_success_rate = 90,
            enhance_item_icon_path = "Icons/enhance_stone_low",
            description = "低品質な強化石。能力の伸びは悪い。",
            completion_flag = 1,
            collection_flag = 1,

            // 武器用ステータス増加値
            weapon_hp = 0,
            weapon_offense = 1,
            weapon_defense = 0,
            weapon_speed = 0,
            weapon_critical_rate = 0,
            weapon_critical_damage_rate = 1,
            weapon_fire_offence = 0,
            weapon_water_offence = 0,
            weapon_wind_offence = 0,
            weapon_earth_offence = 0,

            // 防具用ステータス増加値
            armor_hp = 3,
            armor_offense = 0,
            armor_defense = 1,
            armor_speed = 0,
            armor_critical_rate = 0,
            armor_critical_damage_rate = 0,
            armor_fire_offence = 0,
            armor_water_offence = 0,
            armor_wind_offence = 0,
            armor_earth_offence = 0,

            // アクセサリ用ステータス増加値
            accessory_hp = 1,
            accessory_offense = 1,
            accessory_defense = 1,
            accessory_speed = 0,
            accessory_critical_rate = 0,
            accessory_critical_damage_rate = 0,
            accessory_fire_offence = 0,
            accessory_water_offence = 0,
            accessory_wind_offence = 0,
            accessory_earth_offence = 0
        };

        enhanceItemMasters[1] = enhanceItem1;
        Debug.Log("MasterDataContainer: テスト強化アイテムデータ作成完了");
    }

    private void CreateTestSupportItemData()
    {
        var supportItem1 = new SupportItemMasterData
        {
            support_item_id = 1,
            support_item_name = "古びたコイン",
            attribute_type = "normal",
            rarity = "common",
            infinite_use = 0,
            max_stack_value = 999,
            add_enhanced_value = 0,
            multipl_enhanced_value = 1,
            reduce_enhanced_value = 0,
            add_enhance_stamina = 0,
            reduce_enhance_stamina = 0,
            add_enhance_success_rate = 10,
            reduce_enhance_success_rate = 0,
            multipl_status_up = 1,
            enhance_item_icon_path = "Icons/coin_old",
            description = "古いコイン。成功率が増加する。",
            completion_flag = 1,
            collection_flag = 1,

            // 補助材料の直接ステータス効果
            hp = 0,
            offense = 0,
            defense = 0,
            speed = 0,
            critical_rate = 0,
            critical_damage_rate = 0,
            fire_offence = 0,
            water_offence = 0,
            wind_offence = 0,
            earth_offence = 0
        };

        supportItemMasters[1] = supportItem1;
        Debug.Log("MasterDataContainer: テスト補助材料データ作成完了");
    }

    private void CreateTestQuestData()
    {
        var quest1 = new QuestMasterData
        {
            questId = 1,
            questName = "初心者の試練",
            description = "冒険の第一歩を踏み出そう",
            questType = QuestType.Normal,
            needLevel = 1,
            requiredClearQuest = -1,
            clearLimit = -1,
            requiredStamina = 1,
            recommendedPower = 100
        };

        questMasters[1] = quest1;
        Debug.Log("MasterDataContainer: テストクエストデータ作成完了");
    }

    // ===== データ取得メソッド =====

    public EquipmentMasterData GetEquipmentMaster(int id)
    {
        equipmentMasters.TryGetValue(id, out EquipmentMasterData data);
        return data;
    }

    public List<EquipmentMasterData> GetAllEquipmentMasters()
    {
        return new List<EquipmentMasterData>(equipmentMasters.Values);
    }

    public EnhanceItemMasterData GetEnhanceItemMaster(int id)
    {
        enhanceItemMasters.TryGetValue(id, out EnhanceItemMasterData data);
        return data;
    }

    public List<EnhanceItemMasterData> GetAllEnhanceItemMasters()
    {
        return new List<EnhanceItemMasterData>(enhanceItemMasters.Values);
    }

    public SupportItemMasterData GetSupportItemMaster(int id)
    {
        supportItemMasters.TryGetValue(id, out SupportItemMasterData data);
        return data;
    }

    public List<SupportItemMasterData> GetAllSupportItemMasters()
    {
        return new List<SupportItemMasterData>(supportItemMasters.Values);
    }

    public QuestMasterData GetQuestMaster(int id)
    {
        questMasters.TryGetValue(id, out QuestMasterData data);
        return data;
    }

    public List<QuestMasterData> GetAllQuestMasters()
    {
        return new List<QuestMasterData>(questMasters.Values);
    }
}

/// <summary>
/// セーブデータ管理コンテナ
/// </summary>
public class SaveDataContainer
{
    private const string SAVE_DATA_PATH = "Assets/SaveData/";

    private UserProfile userProfile;
    private List<UserEquipment> userEquipments = new List<UserEquipment>();
    private List<UserItem> userItems = new List<UserItem>();
    private List<UserSkill> userSkills = new List<UserSkill>();
    private AudioSettingsData audioSettings;

    /// <summary>
    /// 全ユーザーデータ読み込み
    /// </summary>
    public IEnumerator LoadAllUserDataAsync()
    {
        yield return LoadUserProfileAsync();
        yield return LoadUserEquipmentsAsync();
        yield return LoadUserItemsAsync();
        yield return LoadUserSkillsAsync();
        yield return LoadAudioSettingsAsync();

        Debug.Log("SaveDataContainer: 全ユーザーデータ読み込み完了");
    }

    // ===== 個別データ読み込み =====

    public IEnumerator LoadUserProfileAsync()
    {
        // TODO: user_profile.json 読み込み
        // 仮のテストデータを作成
        CreateTestUserProfile();
        yield return null;
    }

    public IEnumerator LoadUserEquipmentsAsync()
    {
        // TODO: user_equipment.json 読み込み
        // 仮のテストデータを作成
        CreateTestUserEquipments();
        yield return null;
    }

    public IEnumerator LoadUserItemsAsync()
    {
        // TODO: user_items.json 読み込み
        // 仮のテストデータを作成
        CreateTestUserItems();
        yield return null;
    }

    public IEnumerator LoadUserSkillsAsync()
    {
        // TODO: user_skills.json 読み込み
        yield return null;
    }

    public IEnumerator LoadAudioSettingsAsync()
    {
        // TODO: audio_settings.json 読み込み
        // デフォルト設定を作成
        audioSettings = new AudioSettingsData();
        yield return null;
    }

    // ===== テストデータ作成 =====

    private void CreateTestUserProfile()
    {
        userProfile = new UserProfile
        {
            userId = 1,
            level = 1,
            experience = 0,
            stamina = 10,
            maxStamina = 10,
            gold = 1000,
            gems = 100,
            lastLoginTime = DateTime.Now,
            lastStaminaRecoveryTime = DateTime.Now,
            mainCharacterId = 1
        };
        Debug.Log("SaveDataContainer: テストユーザープロフィール作成完了");
    }

    private void CreateTestUserEquipments()
    {
        var equipment1 = new UserEquipment
        {
            unique_id = "eq_001",
            equipment_id = 1,
            current_enhanced_value = 0,
            current_enhance_stamina = 100,
            is_equipped = true,
            acquired_time = DateTime.Now,
            hp = 0,
            offense = 1,
            defense = 0,
            speed = 0,
            critical_rate = 10,
            critical_damage_rate = 150,
            fire_offence = 0,
            water_offence = 0,
            wind_offence = 0,
            earth_offence = 0
        };

        userEquipments.Add(equipment1);
        Debug.Log("SaveDataContainer: テストユーザー装備データ作成完了");
    }

    private void CreateTestUserItems()
    {
        var item1 = new UserItem
        {
            item_id = 1,
            item_type = ItemType.EnhanceItem,
            quantity = 10
        };

        var item2 = new UserItem
        {
            item_id = 1,
            item_type = ItemType.SupportItem,
            quantity = 5
        };

        userItems.Add(item1);
        userItems.Add(item2);
        Debug.Log("SaveDataContainer: テストユーザーアイテムデータ作成完了");
    }

    // ===== データ取得メソッド =====

    public UserProfile GetUserProfile()
    {
        return userProfile;
    }

    public List<UserEquipment> GetUserEquipments()
    {
        return userEquipments ?? new List<UserEquipment>();
    }

    public List<UserItem> GetUserItems()
    {
        return userItems ?? new List<UserItem>();
    }

    public List<UserSkill> GetUserSkills()
    {
        return userSkills ?? new List<UserSkill>();
    }

    public AudioSettingsData GetAudioSettings()
    {
        return audioSettings;
    }

    // ===== データ保存メソッド =====

    public void SaveUserProfile(UserProfile profile)
    {
        userProfile = profile;
        // TODO: JSON保存処理
        Debug.Log("SaveDataContainer: ユーザープロフィール保存");
    }

    public void SaveUserEquipment(UserEquipment equipment)
    {
        // 既存装備の更新または新規追加
        int index = userEquipments.FindIndex(e => e.unique_id == equipment.unique_id);
        if (index >= 0)
        {
            userEquipments[index] = equipment;
        }
        else
        {
            userEquipments.Add(equipment);
        }

        // TODO: JSON保存処理
        Debug.Log($"SaveDataContainer: 装備保存 - {equipment.unique_id}");
    }

    public void ConsumeUserItem(int itemId, int quantity)
    {
        UserItem item = userItems.Find(i => i.item_id == itemId);
        if (item != null)
        {
            item.quantity = Mathf.Max(0, item.quantity - quantity);
            // TODO: JSON保存処理
            Debug.Log($"SaveDataContainer: アイテム消費 - ID:{itemId}, 消費量:{quantity}, 残り:{item.quantity}");
        }
    }

    public void SaveAudioSettings(AudioSettingsData settings)
    {
        audioSettings = settings;
        // TODO: JSON保存処理
        Debug.Log("SaveDataContainer: 音響設定保存");
    }
}

// ===== データクラス定義はEnhanceDataService.csに移動 =====
// DataManager.csは軽量化のため、データアクセスメソッドのみを提供