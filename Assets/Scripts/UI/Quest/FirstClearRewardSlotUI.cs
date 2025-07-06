using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 初回クリア報酬スロットプレハブ制御クラス
/// 責任範囲：
/// - 初回クリア報酬アイテムの表示
/// - 特別報酬であることの視覚的強調
/// </summary>
public class FirstClearRewardSlotUI : MonoBehaviour
{
    [Header("基本情報表示")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI quantityText;

    [Header("初回報酬専用表示")]
    [SerializeField] private Image firstClearBadge;
    [SerializeField] private TextMeshProUGUI firstClearText;
    [SerializeField] private GameObject specialEffectObject;

    [Header("アイテムタイプ表示")]
    [SerializeField] private Image typeIcon;
    [SerializeField] private TextMeshProUGUI typeText;

    [Header("特別演出")]
    [SerializeField] private Image glowEffect;
    [SerializeField] private ParticleSystem sparkleEffect;
    [SerializeField] private Animator rewardAnimator;

    [Header("カラー設定")]
    [SerializeField] private Color firstClearGlowColor = Color.yellow;
    [SerializeField] private Color specialBorderColor = Color.red;

    [Header("アイテムタイプカラー")]
    [SerializeField] private Color equipmentColor = Color.white;
    [SerializeField] private Color enhanceItemColor = Color.white;
    [SerializeField] private Color supportItemColor = Color.white;
    [SerializeField] private Color goldColor = Color.yellow;
    [SerializeField] private Color gemColor = Color.cyan;
    [SerializeField] private Color defaultColor = Color.white;

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool enableSpecialEffects = true;
    [SerializeField] private bool autoPlayAnimation = true;

    // 内部状態
    private QuestMasterData questMasterData;
    private string rewardItemType;
    private int rewardItemId;
    private int rewardQuantity;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        if (autoPlayAnimation)
        {
            PlayIntroAnimation();
        }
    }

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネントの初期化
    /// </summary>
    private void InitializeComponents()
    {
        // 初期状態設定
        if (specialEffectObject != null)
        {
            specialEffectObject.SetActive(enableSpecialEffects);
        }

        if (sparkleEffect != null)
        {
            if (enableSpecialEffects)
            {
                sparkleEffect.gameObject.SetActive(true);
            }
            else
            {
                sparkleEffect.gameObject.SetActive(false);
            }
        }

        if (firstClearText != null)
        {
            firstClearText.text = "初回クリア報酬";
        }

        // グロー効果の初期設定
        if (glowEffect != null)
        {
            glowEffect.color = firstClearGlowColor;
        }
    }

    /// <summary>
    /// 初回クリア報酬スロットを初期化
    /// </summary>
    /// <param name="questMaster">クエストマスターデータ</param>
    public void Initialize(QuestMasterData questMaster)
    {
        try
        {
            if (questMaster == null)
            {
                LogError("QuestMasterDataがnullです");
                return;
            }

            if (!questMaster.HasFirstClearReward())
            {
                LogError("初回クリア報酬が設定されていません");
                return;
            }

            this.questMasterData = questMaster;
            this.rewardItemType = questMaster.firstClearItemType;
            this.rewardItemId = questMaster.firstClearItemId;
            this.rewardQuantity = questMaster.firstClearItemQuantity;

            // 基本情報表示
            DisplayBasicInfo();

            // アイテムタイプ表示
            DisplayItemType();

            // アイコン読み込み
            LoadRewardIcon();

            // 特別演出
            SetupSpecialEffects();

            Log($"初回クリア報酬スロット初期化完了: {GetRewardDisplayName()}");
        }
        catch (Exception e)
        {
            LogError($"初回クリア報酬スロット初期化エラー: {e.Message}");
        }
    }

    #endregion

    #region 基本情報表示

    /// <summary>
    /// 基本情報を表示
    /// </summary>
    private void DisplayBasicInfo()
    {
        if (questMasterData == null) return;

        // アイテム名
        if (itemNameText != null)
        {
            string displayName = GetRewardDisplayName();
            itemNameText.text = displayName;
        }

        // 数量
        if (quantityText != null)
        {
            if (rewardQuantity > 1)
            {
                quantityText.text = $"x{rewardQuantity:N0}";
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 報酬の表示名を取得
    /// </summary>
    /// <returns>報酬表示名</returns>
    private string GetRewardDisplayName()
    {
        if (string.IsNullOrEmpty(rewardItemType)) return "不明なアイテム";

        return rewardItemType.ToLower() switch
        {
            "gold" => "ゴールド",
            "gem" => "ジェム",
            "equipment" => GetEquipmentName(rewardItemId),
            "enhanceitem" => GetEnhanceItemName(rewardItemId),
            "enhance" => GetEnhanceItemName(rewardItemId),
            "supportitem" => GetSupportItemName(rewardItemId),
            "support" => GetSupportItemName(rewardItemId),
            _ => $"{rewardItemType} ID:{rewardItemId}"
        };
    }

    /// <summary>
    /// 装備名をMasterDataManagerから取得
    /// </summary>
    /// <param name="equipmentId">装備ID</param>
    /// <returns>装備名</returns>
    private string GetEquipmentName(int equipmentId)
    {
        try
        {
            if (MasterDataManager.Instance == null)
            {
                LogError("MasterDataManagerが利用できません");
                return $"装備 ID:{equipmentId}";
            }

            var equipmentData = MasterDataManager.Instance.GetEquipmentData(equipmentId);
            if (equipmentData != null)
            {
                return equipmentData.equipmentName;
            }
            else
            {
                LogError($"装備マスターデータが見つかりません: ID {equipmentId}");
                return $"装備 ID:{equipmentId}";
            }
        }
        catch (Exception e)
        {
            LogError($"装備名取得エラー: {e.Message}");
            return $"装備 ID:{equipmentId}";
        }
    }

    /// <summary>
    /// 強化アイテム名をMasterDataManagerから取得
    /// </summary>
    /// <param name="itemId">アイテムID</param>
    /// <returns>アイテム名</returns>
    private string GetEnhanceItemName(int itemId)
    {
        try
        {
            if (MasterDataManager.Instance == null)
            {
                LogError("MasterDataManagerが利用できません");
                return $"強化素材 ID:{itemId}";
            }

            var enhanceItemData = MasterDataManager.Instance.GetEnhanceItemData(itemId);
            if (enhanceItemData != null)
            {
                return enhanceItemData.enhanceItemName;
            }
            else
            {
                LogError($"強化アイテムマスターデータが見つかりません: ID {itemId}");
                return $"強化素材 ID:{itemId}";
            }
        }
        catch (Exception e)
        {
            LogError($"強化アイテム名取得エラー: {e.Message}");
            return $"強化素材 ID:{itemId}";
        }
    }

    /// <summary>
    /// 補助アイテム名をMasterDataManagerから取得
    /// </summary>
    /// <param name="itemId">アイテムID</param>
    /// <returns>アイテム名</returns>
    private string GetSupportItemName(int itemId)
    {
        try
        {
            if (MasterDataManager.Instance == null)
            {
                LogError("MasterDataManagerが利用できません");
                return $"補助アイテム ID:{itemId}";
            }

            var supportItemData = MasterDataManager.Instance.GetSupportItemData(itemId);
            if (supportItemData != null)
            {
                return supportItemData.supportItemName;
            }
            else
            {
                LogError($"補助アイテムマスターデータが見つかりません: ID {itemId}");
                return $"補助アイテム ID:{itemId}";
            }
        }
        catch (Exception e)
        {
            LogError($"補助アイテム名取得エラー: {e.Message}");
            return $"補助アイテム ID:{itemId}";
        }
    }

    /// <summary>
    /// アイテムタイプを表示
    /// </summary>
    private void DisplayItemType()
    {
        if (string.IsNullOrEmpty(rewardItemType)) return;

        // タイプテキスト
        if (typeText != null)
        {
            string typeDisplayName = GetItemTypeDisplayName(rewardItemType);
            typeText.text = typeDisplayName;
            typeText.color = GetItemTypeColor(rewardItemType);
        }

        // タイプアイコン
        if (typeIcon != null)
        {
            LoadItemTypeIcon(rewardItemType);
        }
    }

    /// <summary>
    /// アイテムタイプの表示名を取得
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    /// <returns>表示名</returns>
    private string GetItemTypeDisplayName(string itemType)
    {
        return itemType?.ToLower() switch
        {
            "gold" => "通貨",
            "gem" => "プレミアム通貨",
            "equipment" => "装備",
            "enhanceitem" => "強化素材",
            "enhance" => "強化素材",
            "supportitem" => "補助アイテム",
            "support" => "補助アイテム",
            _ => "特別報酬"
        };
    }

    /// <summary>
    /// アイテムタイプに対応する色を取得
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    /// <returns>対応する色</returns>
    private Color GetItemTypeColor(string itemType)
    {
        return itemType?.ToLower() switch
        {
            "gold" => goldColor,
            "gem" => gemColor,
            "equipment" => equipmentColor,
            "enhanceitem" => enhanceItemColor,
            "enhance" => enhanceItemColor,
            "supportitem" => supportItemColor,
            "support" => supportItemColor,
            _ => defaultColor
        };
    }

    #endregion

    #region アイコン読み込み

    /// <summary>
    /// 報酬アイコンを読み込み
    /// </summary>
    private void LoadRewardIcon()
    {
        try
        {
            if (itemIcon == null) return;

            string iconPath = GetRewardIconPathFromMasterData(rewardItemType, rewardItemId);
            var sprite = Resources.Load<Sprite>(iconPath);

            if (sprite != null)
            {
                itemIcon.sprite = sprite;
                itemIcon.gameObject.SetActive(true);
                Log($"報酬アイコン読み込み成功: {iconPath}");
            }
            else
            {
                Log($"報酬アイコンが見つかりません: {iconPath}");
                SetDefaultRewardIcon();
            }
        }
        catch (Exception e)
        {
            LogError($"報酬アイコン読み込みエラー: {e.Message}");
            SetDefaultRewardIcon();
        }
    }

    /// <summary>
    /// MasterDataManagerから報酬アイコンパスを取得
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    /// <param name="itemId">アイテムID</param>
    /// <returns>アイコンパス</returns>
    private string GetRewardIconPathFromMasterData(string itemType, int itemId)
    {
        // 通貨系は従来通り
        if (itemType?.ToLower() == "gold" || itemType?.ToLower() == "gem")
        {
            return GetFallbackRewardIconPath(itemType, itemId);
        }

        // MasterDataManagerが利用可能かチェック
        if (MasterDataManager.Instance == null)
        {
            LogError("MasterDataManagerが利用できません - フォールバック処理");
            return GetFallbackRewardIconPath(itemType, itemId);
        }

        try
        {
            return itemType?.ToLower() switch
            {
                "equipment" => GetEquipmentIconPath(itemId),
                "enhanceitem" => GetEnhanceItemIconPath(itemId),
                "enhance" => GetEnhanceItemIconPath(itemId),
                "supportitem" => GetSupportItemIconPath(itemId),
                "support" => GetSupportItemIconPath(itemId),
                _ => GetFallbackRewardIconPath(itemType, itemId)
            };
        }
        catch (Exception e)
        {
            LogError($"マスターデータからのアイコンパス取得エラー: {e.Message}");
            return GetFallbackRewardIconPath(itemType, itemId);
        }
    }

    /// <summary>
    /// 装備アイコンパスを取得
    /// </summary>
    /// <param name="equipmentId">装備ID</param>
    /// <returns>アイコンパス</returns>
    private string GetEquipmentIconPath(int equipmentId)
    {
        var equipmentData = MasterDataManager.Instance.GetEquipmentData(equipmentId);
        if (equipmentData != null)
        {
            // 装備タイプに応じたフォルダを決定
            string equipmentTypeFolder = GetEquipmentTypeFolder(equipmentData.equipmentType);

            if (!string.IsNullOrEmpty(equipmentData.equipmentIconPath))
            {
                return equipmentData.equipmentIconPath;
            }
            else if (equipmentData.equipmentIcon != null)
            {
                return $"Icons/{equipmentTypeFolder}/{equipmentData.equipmentIcon.name}";
            }
            else
            {
                return $"Icons/{equipmentTypeFolder}/{equipmentTypeFolder}_{equipmentId}";
            }
        }
        else
        {
            LogError($"装備マスターデータが見つかりません: ID={equipmentId}");
            return $"Icons/Weapon/Weapon_{equipmentId}";
        }
    }

    /// <summary>
    /// 装備タイプに対応するフォルダ名を取得
    /// </summary>
    /// <param name="equipmentType">装備タイプ</param>
    /// <returns>フォルダ名</returns>
    private string GetEquipmentTypeFolder(EquipmentType equipmentType)
    {
        return equipmentType switch
        {
            EquipmentType.Weapon => "Weapon",
            EquipmentType.Armor => "Armor",
            EquipmentType.Accessory => "Accessory",
            _ => "Weapon"
        };
    }

    /// <summary>
    /// 強化アイテムアイコンパスを取得
    /// </summary>
    /// <param name="itemId">強化アイテムID</param>
    /// <returns>アイコンパス</returns>
    private string GetEnhanceItemIconPath(int itemId)
    {
        var enhanceItemData = MasterDataManager.Instance.GetEnhanceItemData(itemId);
        if (enhanceItemData != null)
        {
            if (!string.IsNullOrEmpty(enhanceItemData.enhanceItemIconPath))
            {
                return enhanceItemData.enhanceItemIconPath;
            }
            else if (enhanceItemData.enhanceItemIcon != null)
            {
                return $"Icons/EnhanceItem/{enhanceItemData.enhanceItemIcon.name}";
            }
            else
            {
                return $"Icons/EnhanceItem/Enhance_{itemId}";
            }
        }
        else
        {
            LogError($"強化アイテムマスターデータが見つかりません: ID={itemId}");
            return $"Icons/EnhanceItem/Enhance_{itemId}";
        }
    }

    /// <summary>
    /// 補助アイテムアイコンパスを取得
    /// </summary>
    /// <param name="itemId">補助アイテムID</param>
    /// <returns>アイコンパス</returns>
    private string GetSupportItemIconPath(int itemId)
    {
        var supportItemData = MasterDataManager.Instance.GetSupportItemData(itemId);
        if (supportItemData != null)
        {
            if (!string.IsNullOrEmpty(supportItemData.supportItemIconPath))
            {
                return supportItemData.supportItemIconPath;
            }
            else if (supportItemData.supportItemIcon != null)
            {
                return $"Icons/SupportItem/{supportItemData.supportItemIcon.name}";
            }
            else
            {
                return $"Icons/SupportItem/Support_{itemId}";
            }
        }
        else
        {
            LogError($"補助アイテムマスターデータが見つかりません: ID={itemId}");
            return $"Icons/SupportItem/Support_{itemId}";
        }
    }

    /// <summary>
    /// フォールバック報酬アイコンパスを取得
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    /// <param name="itemId">アイテムID</param>
    /// <returns>アイコンパス</returns>
    private string GetFallbackRewardIconPath(string itemType, int itemId)
    {
        return itemType?.ToLower() switch
        {
            "gold" => "Icons/Currency/gold_icon",
            "gem" => "Icons/Currency/gem_icon",
            "equipment" => $"Icons/Weapon/Weapon_{itemId}",
            "enhanceitem" => $"Icons/EnhanceItem/Enhance_{itemId}",
            "enhance" => $"Icons/EnhanceItem/Enhance_{itemId}",
            "supportitem" => $"Icons/SupportItem/Support_{itemId}",
            "support" => $"Icons/SupportItem/Support_{itemId}",
            "skill" => $"Icons/Skill/skill_{itemId}",
            _ => "Icons/Reward/special_reward"
        };
    }

    /// <summary>
    /// タイプアイコンを読み込み
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    private void LoadItemTypeIcon(string itemType)
    {
        try
        {
            string iconPath = GetItemTypeIconPath(itemType);
            var sprite = Resources.Load<Sprite>(iconPath);

            if (sprite != null)
            {
                typeIcon.sprite = sprite;
                typeIcon.color = GetItemTypeColor(itemType);
                typeIcon.gameObject.SetActive(true);
            }
            else
            {
                typeIcon.gameObject.SetActive(false);
            }
        }
        catch (Exception e)
        {
            LogError($"タイプアイコン読み込みエラー: {e.Message}");
            typeIcon.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// アイテムタイプアイコンパスを取得
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    /// <returns>アイコンパス</returns>
    private string GetItemTypeIconPath(string itemType)
    {
        return itemType?.ToLower() switch
        {
            "gold" => "Icons/Currency/gold_type_icon",
            "gem" => "Icons/Currency/gem_type_icon",
            "equipment" => "Icons/ItemType/equipment_icon",
            "enhanceitem" => "Icons/ItemType/enhance_icon",
            "enhance" => "Icons/ItemType/enhance_icon",
            "supportitem" => "Icons/ItemType/support_icon",
            "support" => "Icons/ItemType/support_icon",
            _ => "Icons/ItemType/special_icon"
        };
    }

    /// <summary>
    /// デフォルト報酬アイコンを設定
    /// </summary>
    private void SetDefaultRewardIcon()
    {
        try
        {
            var defaultSprite = Resources.Load<Sprite>("Icons/Reward/first_clear_default");
            if (defaultSprite != null)
            {
                itemIcon.sprite = defaultSprite;
                itemIcon.gameObject.SetActive(true);
            }
            else
            {
                itemIcon.gameObject.SetActive(false);
            }
        }
        catch (Exception e)
        {
            LogError($"デフォルト報酬アイコン設定エラー: {e.Message}");
            itemIcon.gameObject.SetActive(false);
        }
    }

    #endregion

    #region 特別演出

    /// <summary>
    /// 特別演出を設定
    /// </summary>
    private void SetupSpecialEffects()
    {
        if (!enableSpecialEffects) return;

        // グロー効果
        SetupGlowEffect();

        // パーティクル効果
        SetupParticleEffect();

        // バッジ表示
        SetupFirstClearBadge();
    }

    /// <summary>
    /// グロー効果を設定
    /// </summary>
    private void SetupGlowEffect()
    {
        if (glowEffect == null) return;

        // グロー色設定
        Color glowColor = GetItemTypeColor(rewardItemType);
        glowEffect.color = glowColor;
        glowEffect.gameObject.SetActive(true);
    }

    /// <summary>
    /// パーティクル効果を設定
    /// </summary>
    private void SetupParticleEffect()
    {
        if (sparkleEffect == null) return;

        var main = sparkleEffect.main;
        main.startColor = firstClearGlowColor;

        // パーティクル再生
        if (sparkleEffect.isPlaying)
        {
            sparkleEffect.Stop();
        }
        sparkleEffect.Play();
    }

    /// <summary>
    /// 初回クリアバッジを設定
    /// </summary>
    private void SetupFirstClearBadge()
    {
        if (firstClearBadge == null) return;

        firstClearBadge.color = specialBorderColor;
        firstClearBadge.gameObject.SetActive(true);
    }

    /// <summary>
    /// イントロアニメーションを再生
    /// </summary>
    private void PlayIntroAnimation()
    {
        if (rewardAnimator == null) return;

        try
        {
            rewardAnimator.SetTrigger("PlayIntro");
            Log("初回クリア報酬イントロアニメーション再生");
        }
        catch (Exception e)
        {
            LogError($"イントロアニメーション再生エラー: {e.Message}");
        }
    }

    /// <summary>
    /// 取得アニメーションを再生
    /// </summary>
    public void PlayObtainAnimation()
    {
        if (rewardAnimator == null) return;

        try
        {
            rewardAnimator.SetTrigger("PlayObtain");

            // パーティクル効果も再生
            if (sparkleEffect != null)
            {
                sparkleEffect.Stop();
                sparkleEffect.Play();
            }

            Log("報酬取得アニメーション再生");
        }
        catch (Exception e)
        {
            LogError($"取得アニメーション再生エラー: {e.Message}");
        }
    }

    #endregion

    #region ユーティリティ

    /// <summary>
    /// 報酬データを取得
    /// </summary>
    /// <returns>報酬データ（タプル）</returns>
    public (string itemType, int itemId, int quantity) GetRewardData()
    {
        return (rewardItemType, rewardItemId, rewardQuantity);
    }

    /// <summary>
    /// スロットの有効性をチェック
    /// </summary>
    /// <returns>有効な場合true</returns>
    public bool IsValidSlot()
    {
        return questMasterData != null &&
               !string.IsNullOrEmpty(rewardItemType) &&
               rewardQuantity > 0;
    }

    /// <summary>
    /// 特別演出の有効/無効を切り替え
    /// </summary>
    /// <param name="enabled">有効フラグ</param>
    public void SetSpecialEffectsEnabled(bool enabled)
    {
        enableSpecialEffects = enabled;

        if (specialEffectObject != null)
        {
            specialEffectObject.SetActive(enabled);
        }

        if (sparkleEffect != null)
        {
            if (enabled)
            {
                sparkleEffect.gameObject.SetActive(true);
                sparkleEffect.Play();
            }
            else
            {
                sparkleEffect.Stop();
                sparkleEffect.gameObject.SetActive(false);
            }
        }

        if (glowEffect != null)
        {
            glowEffect.gameObject.SetActive(enabled);
        }
    }

    /// <summary>
    /// 報酬価値を計算
    /// </summary>
    /// <returns>報酬価値</returns>
    public int CalculateRewardValue()
    {
        return rewardItemType?.ToLower() switch
        {
            "gold" => rewardQuantity,
            "gem" => rewardQuantity * 100,
            "equipment" => rewardQuantity * 1000,
            "enhanceitem" => rewardQuantity * 50,
            "enhance" => rewardQuantity * 50,
            "supportitem" => rewardQuantity * 25,
            "support" => rewardQuantity * 25,
            _ => rewardQuantity
        };
    }

    /// <summary>
    /// デバッグ情報を取得
    /// </summary>
    /// <returns>デバッグ用文字列</returns>
    public string GetDebugInfo()
    {
        if (questMasterData == null) return "QuestMasterData: null";

        return $"FirstClearReward[Quest:{questMasterData.questId}] {GetRewardDisplayName()} - " +
               $"Type: {rewardItemType}, ID: {rewardItemId}, Quantity: {rewardQuantity}, " +
               $"Value: {CalculateRewardValue()}, Effects: {enableSpecialEffects}";
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[FirstClearRewardSlotUI] {message}");
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[FirstClearRewardSlotUI] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("報酬情報をログ出力")]
    private void LogRewardInfo()
    {
        Log(GetDebugInfo());
    }

    [ContextMenu("取得アニメーションをテスト")]
    private void TestObtainAnimation()
    {
        PlayObtainAnimation();
    }

    [ContextMenu("特別演出をトグル")]
    private void ToggleSpecialEffects()
    {
        SetSpecialEffectsEnabled(!enableSpecialEffects);
        Log($"特別演出: {enableSpecialEffects}");
    }

    [ContextMenu("報酬価値を計算")]
    private void CalculateAndLogRewardValue()
    {
        int value = CalculateRewardValue();
        Log($"報酬価値: {value}");
    }

    private void OnValidate()
    {
        // エディター上での設定変更を即座に反映
        if (Application.isPlaying && questMasterData != null)
        {
            SetupSpecialEffects();
            DisplayItemType();
        }
    }
#endif

    #endregion
}