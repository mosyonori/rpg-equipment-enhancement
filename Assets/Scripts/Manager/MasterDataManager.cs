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
    [SerializeField] private string characterDataPath = "GameData/Character";
    [SerializeField] private string skillDataPath = "GameData/Skill";
    [SerializeField] private string skillEffectDataPath = "GameData/SkillEffect";

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
    private Dictionary<int, CharacterMasterData> characterDataDict;
    private Dictionary<int, SkillMasterData> skillDataDict;
    private Dictionary<int, SkillEffectMasterData> skillEffectDataDict;

    // リスト形式のデータ（フィルタ・ソート用）
    private List<EquipmentMasterData> equipmentDataList;
    private List<EnhanceItemMasterData> enhanceItemDataList;
    private List<SupportItemMasterData> supportItemDataList;
    private List<CharacterMasterData> characterDataList;
    private List<SkillMasterData> skillDataList;
    private List<SkillEffectMasterData> skillEffectDataList;

    // キャッシュ用データ
    private Dictionary<EquipmentType, List<EquipmentMasterData>> equipmentsByType;
    private Dictionary<RarityType, List<EquipmentMasterData>> equipmentsByRarity;
    private Dictionary<AttributeType, List<EnhanceItemMasterData>> enhanceItemsByAttribute;
    private Dictionary<AttributeType, List<SupportItemMasterData>> supportItemsByAttribute;
    private Dictionary<RarityType, List<CharacterMasterData>> charactersByRarity;
    private Dictionary<TargetType, List<SkillMasterData>> skillsByTargetType;
    private Dictionary<RarityType, List<SkillMasterData>> skillsByRarity;
    private Dictionary<AttributeType, List<SkillMasterData>> skillsByAttribute;

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
        characterDataDict = new Dictionary<int, CharacterMasterData>();
        skillDataDict = new Dictionary<int, SkillMasterData>();
        skillEffectDataDict = new Dictionary<int, SkillEffectMasterData>();

        equipmentDataList = new List<EquipmentMasterData>();
        enhanceItemDataList = new List<EnhanceItemMasterData>();
        supportItemDataList = new List<SupportItemMasterData>();
        characterDataList = new List<CharacterMasterData>();
        skillDataList = new List<SkillMasterData>();
        skillEffectDataList = new List<SkillEffectMasterData>();

        equipmentsByType = new Dictionary<EquipmentType, List<EquipmentMasterData>>();
        equipmentsByRarity = new Dictionary<RarityType, List<EquipmentMasterData>>();
        enhanceItemsByAttribute = new Dictionary<AttributeType, List<EnhanceItemMasterData>>();
        supportItemsByAttribute = new Dictionary<AttributeType, List<SupportItemMasterData>>();
        charactersByRarity = new Dictionary<RarityType, List<CharacterMasterData>>();
        skillsByTargetType = new Dictionary<TargetType, List<SkillMasterData>>();
        skillsByRarity = new Dictionary<RarityType, List<SkillMasterData>>();
        skillsByAttribute = new Dictionary<AttributeType, List<SkillMasterData>>();
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
            success &= LoadCharacterData();
            success &= LoadSkillData();
            success &= LoadSkillEffectData();

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

            DebugLog($"装備データを{equipmentDataList.Count}個読み込みました");
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

            DebugLog($"強化アイテムデータを{enhanceItemDataList.Count}個読み込みました");
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

            DebugLog($"補助アイテムデータを{supportItemDataList.Count}個読み込みました");
            return true;
        }
        catch (Exception e)
        {
            DebugLogError($"補助アイテムデータ読み込みエラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// キャラクターデータを読み込み
    /// </summary>
    public bool LoadCharacterData()
    {
        try
        {
            var characterAssets = Resources.LoadAll<CharacterMasterData>(characterDataPath);

            characterDataDict.Clear();
            characterDataList.Clear();

            foreach (var character in characterAssets)
            {
                if (character == null) continue;

                if (characterDataDict.ContainsKey(character.CharacterId))
                {
                    DebugLogError($"重複するキャラクターID: {character.CharacterId} ({character.CharacterName})");
                    continue;
                }

                characterDataDict[character.CharacterId] = character;
                characterDataList.Add(character);
            }

            DebugLog($"キャラクターデータを{characterDataList.Count}個読み込みました");
            return true;
        }
        catch (Exception e)
        {
            DebugLogError($"キャラクターデータ読み込みエラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// スキルデータを読み込み
    /// </summary>
    public bool LoadSkillData()
    {
        try
        {
            var skillAssets = Resources.LoadAll<SkillMasterData>(skillDataPath);

            skillDataDict.Clear();
            skillDataList.Clear();

            foreach (var skill in skillAssets)
            {
                if (skill == null) continue;

                if (skillDataDict.ContainsKey(skill.skillId))
                {
                    DebugLogError($"重複するスキルID: {skill.skillId} ({skill.skillName})");
                    continue;
                }

                skillDataDict[skill.skillId] = skill;
                skillDataList.Add(skill);
            }

            DebugLog($"スキルデータを{skillDataList.Count}個読み込みました");
            return true;
        }
        catch (Exception e)
        {
            DebugLogError($"スキルデータ読み込みエラー: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// スキル効果データを読み込み
    /// </summary>
    public bool LoadSkillEffectData()
    {
        try
        {
            var skillEffectAssets = Resources.LoadAll<SkillEffectMasterData>(skillEffectDataPath);

            skillEffectDataDict.Clear();
            skillEffectDataList.Clear();

            foreach (var skillEffect in skillEffectAssets)
            {
                if (skillEffect == null) continue;

                if (skillEffectDataDict.ContainsKey(skillEffect.statusEffectId))
                {
                    DebugLogError($"重複するスキル効果ID: {skillEffect.statusEffectId} ({skillEffect.statusEffectName})");
                    continue;
                }

                skillEffectDataDict[skillEffect.statusEffectId] = skillEffect;
                skillEffectDataList.Add(skillEffect);
            }

            DebugLog($"スキル効果データを{skillEffectDataList.Count}個読み込みました");
            return true;
        }
        catch (Exception e)
        {
            DebugLogError($"スキル効果データ読み込みエラー: {e.Message}");
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

    /// <summary>
    /// キャラクターデータを取得
    /// </summary>
    public CharacterMasterData GetCharacterData(int characterId)
    {
        return characterDataDict.TryGetValue(characterId, out var data) ? data : null;
    }

    /// <summary>
    /// スキルデータを取得
    /// </summary>
    public SkillMasterData GetSkillData(int skillId)
    {
        return skillDataDict.TryGetValue(skillId, out var data) ? data : null;
    }

    /// <summary>
    /// スキル効果データを取得
    /// </summary>
    public SkillEffectMasterData GetSkillEffectData(int skillEffectId)
    {
        return skillEffectDataDict.TryGetValue(skillEffectId, out var data) ? data : null;
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
    /// キャラクターデータ辞書を取得
    /// </summary>
    public Dictionary<int, CharacterMasterData> GetCharacterDataDict()
    {
        return new Dictionary<int, CharacterMasterData>(characterDataDict);
    }

    /// <summary>
    /// スキルデータ辞書を取得
    /// </summary>
    public Dictionary<int, SkillMasterData> GetSkillDataDict()
    {
        return new Dictionary<int, SkillMasterData>(skillDataDict);
    }

    /// <summary>
    /// スキル効果データ辞書を取得
    /// </summary>
    public Dictionary<int, SkillEffectMasterData> GetSkillEffectDataDict()
    {
        return new Dictionary<int, SkillEffectMasterData>(skillEffectDataDict);
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

    /// <summary>
    /// キャラクターデータリストを取得
    /// </summary>
    public List<CharacterMasterData> GetCharacterDataList()
    {
        return new List<CharacterMasterData>(characterDataList);
    }

    /// <summary>
    /// スキルデータリストを取得
    /// </summary>
    public List<SkillMasterData> GetSkillDataList()
    {
        return new List<SkillMasterData>(skillDataList);
    }

    /// <summary>
    /// スキル効果データリストを取得
    /// </summary>
    public List<SkillEffectMasterData> GetSkillEffectDataList()
    {
        return new List<SkillEffectMasterData>(skillEffectDataList);
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
    /// レアリティ別のキャラクターデータを取得
    /// </summary>
    public List<CharacterMasterData> GetCharactersByRarity(RarityType rarity)
    {
        if (charactersByRarity.TryGetValue(rarity, out var list))
        {
            return new List<CharacterMasterData>(list);
        }
        return new List<CharacterMasterData>();
    }

    /// <summary>
    /// ターゲットタイプ別のスキルデータを取得
    /// </summary>
    public List<SkillMasterData> GetSkillsByTargetType(TargetType targetType)
    {
        if (skillsByTargetType.TryGetValue(targetType, out var list))
        {
            return new List<SkillMasterData>(list);
        }
        return new List<SkillMasterData>();
    }

    /// <summary>
    /// レアリティ別のスキルデータを取得
    /// </summary>
    public List<SkillMasterData> GetSkillsByRarity(RarityType rarity)
    {
        if (skillsByRarity.TryGetValue(rarity, out var list))
        {
            return new List<SkillMasterData>(list);
        }
        return new List<SkillMasterData>();
    }

    /// <summary>
    /// 属性別のスキルデータを取得
    /// </summary>
    public List<SkillMasterData> GetSkillsByAttribute(AttributeType attributeType)
    {
        if (skillsByAttribute.TryGetValue(attributeType, out var list))
        {
            return new List<SkillMasterData>(list);
        }
        return new List<SkillMasterData>();
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
    /// キャラクターを条件で検索
    /// </summary>
    public List<CharacterMasterData> SearchCharacters(Func<CharacterMasterData, bool> predicate)
    {
        return characterDataList.Where(predicate).ToList();
    }

    /// <summary>
    /// スキルを条件で検索
    /// </summary>
    public List<SkillMasterData> SearchSkills(Func<SkillMasterData, bool> predicate)
    {
        return skillDataList.Where(predicate).ToList();
    }

    /// <summary>
    /// スキル効果を条件で検索
    /// </summary>
    public List<SkillEffectMasterData> SearchSkillEffects(Func<SkillEffectMasterData, bool> predicate)
    {
        return skillEffectDataList.Where(predicate).ToList();
    }

    /// <summary>
    /// 名前で装備を検索
    /// </summary>
    public List<EquipmentMasterData> SearchEquipmentsByName(string name)
    {
        return equipmentDataList.Where(eq => eq.equipmentName.Contains(name)).ToList();
    }

    /// <summary>
    /// 名前でキャラクターを検索
    /// </summary>
    public List<CharacterMasterData> SearchCharactersByName(string name)
    {
        return characterDataList.Where(ch => ch.CharacterName.Contains(name)).ToList();
    }

    /// <summary>
    /// 名前でスキルを検索
    /// </summary>
    public List<SkillMasterData> SearchSkillsByName(string name)
    {
        return skillDataList.Where(skill => skill.skillName.Contains(name)).ToList();
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
            totalCharacters = characterDataList.Count,
            totalSkills = skillDataList.Count,
            totalSkillEffects = skillEffectDataList.Count,
            equipmentsByType = equipmentsByType.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            equipmentsByRarity = equipmentsByRarity.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            enhanceItemsByAttribute = enhanceItemsByAttribute.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            supportItemsByAttribute = supportItemsByAttribute.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            charactersByRarity = charactersByRarity.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            skillsByTargetType = skillsByTargetType.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            skillsByRarity = skillsByRarity.ToDictionary(kv => kv.Key, kv => kv.Value.Count),
            skillsByAttribute = skillsByAttribute.ToDictionary(kv => kv.Key, kv => kv.Value.Count)
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

        // キャラクターデータの検証
        foreach (var character in characterDataList)
        {
            if (character.CharacterId <= 0)
                errors.Add($"無効なキャラクターID: {character.CharacterId}");

            if (string.IsNullOrEmpty(character.CharacterName))
                errors.Add($"キャラクター名が空: ID {character.CharacterId}");

            if (character.BaseLevel <= 0)
                errors.Add($"ベースレベルが無効: {character.CharacterName} ({character.BaseLevel})");

            if (character.MaxLevel < character.BaseLevel)
                errors.Add($"最大レベルがベースレベルより小さい: {character.CharacterName}");

            if (character.Hp < 0)
                errors.Add($"HPが負の値: {character.CharacterName} ({character.Hp})");
        }

        // スキルデータの検証
        foreach (var skill in skillDataList)
        {
            if (skill.skillId <= 0)
                errors.Add($"無効なスキルID: {skill.skillId}");

            if (string.IsNullOrEmpty(skill.skillName))
                errors.Add($"スキル名が空: ID {skill.skillId}");

            if (skill.skillDamageMultiplier < 0)
                errors.Add($"スキルダメージ倍率が負の値: {skill.skillName} ({skill.skillDamageMultiplier})");

            if (skill.skillMaxCoolTime < 0)
                errors.Add($"スキルクールタイムが負の値: {skill.skillName} ({skill.skillMaxCoolTime})");

            if (skill.skillHpCost < 0)
                errors.Add($"スキルHP消費が負の値: {skill.skillName} ({skill.skillHpCost})");

            if (skill.skillMpCost < 0)
                errors.Add($"スキルMP消費が負の値: {skill.skillName} ({skill.skillMpCost})");

            if (skill.skillEffectChance < 0 || skill.skillEffectChance > 100)
                errors.Add($"スキル効果発動率が範囲外: {skill.skillName} ({skill.skillEffectChance}%)");

            if (skill.skillEffectChanceBoss < 0 || skill.skillEffectChanceBoss > 100)
                errors.Add($"スキル効果発動率(ボス)が範囲外: {skill.skillName} ({skill.skillEffectChanceBoss}%)");
        }

        // スキル効果データの検証
        foreach (var skillEffect in skillEffectDataList)
        {
            if (skillEffect.statusEffectId <= 0)
                errors.Add($"無効なスキル効果ID: {skillEffect.statusEffectId}");

            if (string.IsNullOrEmpty(skillEffect.statusEffectName))
                errors.Add($"スキル効果名が空: ID {skillEffect.statusEffectId}");

            if (skillEffect.offenseMultiplier < 0)
                errors.Add($"攻撃力倍率が負の値: {skillEffect.statusEffectName} ({skillEffect.offenseMultiplier})");

            if (skillEffect.defenseMultiplier < 0)
                errors.Add($"防御力倍率が負の値: {skillEffect.statusEffectName} ({skillEffect.defenseMultiplier})");

            if (skillEffect.turnStartDamagePercent < 0 || skillEffect.turnStartDamagePercent > 100)
                errors.Add($"ターン開始ダメージ率が範囲外: {skillEffect.statusEffectName} ({skillEffect.turnStartDamagePercent}%)");

            if (skillEffect.turnStartHealPercent < 0 || skillEffect.turnStartHealPercent > 100)
                errors.Add($"ターン開始回復率が範囲外: {skillEffect.statusEffectName} ({skillEffect.turnStartHealPercent}%)");
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
               supportItemDataList.Count > 0 &&
               characterDataList.Count > 0 &&
               skillDataList.Count > 0 &&
               skillEffectDataList.Count > 0;
    }

    /// <summary>
    /// アイコン設定状況を確認
    /// </summary>
    public string GetIconStatus()
    {
        string status = "=== アイコン設定状況 ===\n\n";

        status += "【装備】\n";
        foreach (var equipment in equipmentDataList)
        {
            bool hasIcon = equipment.equipmentIcon != null;
            bool hasPath = !string.IsNullOrEmpty(equipment.equipmentIconPath);
            status += $"- {equipment.equipmentName} (ID:{equipment.equipmentId}): アイコン={hasIcon}, パス={hasPath}";
            if (hasPath) status += $" [{equipment.equipmentIconPath}]";
            status += "\n";
        }

        status += "\n【強化アイテム】\n";
        foreach (var item in enhanceItemDataList)
        {
            bool hasIcon = item.enhanceItemIcon != null;
            bool hasPath = !string.IsNullOrEmpty(item.enhanceItemIconPath);
            status += $"- {item.enhanceItemName} (ID:{item.enhanceItemId}): アイコン={hasIcon}, パス={hasPath}";
            if (hasPath) status += $" [{item.enhanceItemIconPath}]";
            status += "\n";
        }

        status += "\n【補助アイテム】\n";
        foreach (var item in supportItemDataList)
        {
            bool hasIcon = item.supportItemIcon != null;
            bool hasPath = !string.IsNullOrEmpty(item.supportItemIconPath);
            status += $"- {item.supportItemName} (ID:{item.supportItemId}): アイコン={hasIcon}, パス={hasPath}";
            if (hasPath) status += $" [{item.supportItemIconPath}]";
            status += "\n";
        }

        status += "\n【キャラクター】\n";
        foreach (var character in characterDataList)
        {
            bool hasIcon = character.CharacterIcon != null;
            bool hasPath = !string.IsNullOrEmpty(character.CharacterIconPath);
            status += $"- {character.CharacterName} (ID:{character.CharacterId}): アイコン={hasIcon}, パス={hasPath}";
            if (hasPath) status += $" [{character.CharacterIconPath}]";
            status += "\n";
        }

        status += "\n【スキル】\n";
        foreach (var skill in skillDataList)
        {
            bool hasIcon = skill.skillIcon != null;
            bool hasPath = !string.IsNullOrEmpty(skill.skillIconPath);
            status += $"- {skill.skillName} (ID:{skill.skillId}): アイコン={hasIcon}, パス={hasPath}";
            if (hasPath) status += $" [{skill.skillIconPath}]";
            status += "\n";
        }

        return status;
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

        // キャラクターレアリティ別キャッシュ
        charactersByRarity.Clear();
        foreach (RarityType rarity in Enum.GetValues(typeof(RarityType)))
        {
            charactersByRarity[rarity] = characterDataList.Where(ch => ch.Rarity == rarity).ToList();
        }

        // スキルターゲットタイプ別キャッシュ
        skillsByTargetType.Clear();
        foreach (TargetType targetType in Enum.GetValues(typeof(TargetType)))
        {
            skillsByTargetType[targetType] = skillDataList.Where(skill => skill.skillTargetType == targetType).ToList();
        }

        // スキルレアリティ別キャッシュ
        skillsByRarity.Clear();
        foreach (RarityType rarity in Enum.GetValues(typeof(RarityType)))
        {
            skillsByRarity[rarity] = skillDataList.Where(skill => skill.rarity == rarity).ToList();
        }

        // スキル属性別キャッシュ
        skillsByAttribute.Clear();
        foreach (AttributeType attribute in Enum.GetValues(typeof(AttributeType)))
        {
            skillsByAttribute[attribute] = skillDataList.Where(skill => skill.attributeType == attribute).ToList();
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
    public int totalCharacters;
    public int totalSkills;
    public int totalSkillEffects;
    public Dictionary<EquipmentType, int> equipmentsByType;
    public Dictionary<RarityType, int> equipmentsByRarity;
    public Dictionary<AttributeType, int> enhanceItemsByAttribute;
    public Dictionary<AttributeType, int> supportItemsByAttribute;
    public Dictionary<RarityType, int> charactersByRarity;
    public Dictionary<TargetType, int> skillsByTargetType;
    public Dictionary<RarityType, int> skillsByRarity;
    public Dictionary<AttributeType, int> skillsByAttribute;

    public override string ToString()
    {
        var result = $@"=== マスターデータ統計 ===
装備数: {totalEquipments}
強化アイテム数: {totalEnhanceItems}
補助アイテム数: {totalSupportItems}
キャラクター数: {totalCharacters}
スキル数: {totalSkills}
スキル効果数: {totalSkillEffects}

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

        result += "\n\nキャラクターレアリティ別:";
        foreach (var kv in charactersByRarity)
        {
            result += $"\n  {kv.Key}: {kv.Value}個";
        }

        result += "\n\nスキルターゲットタイプ別:";
        foreach (var kv in skillsByTargetType)
        {
            result += $"\n  {kv.Key}: {kv.Value}個";
        }

        result += "\n\nスキルレアリティ別:";
        foreach (var kv in skillsByRarity)
        {
            result += $"\n  {kv.Key}: {kv.Value}個";
        }

        result += "\n\nスキル属性別:";
        foreach (var kv in skillsByAttribute)
        {
            result += $"\n  {kv.Key}: {kv.Value}個";
        }

        return result;
    }
}