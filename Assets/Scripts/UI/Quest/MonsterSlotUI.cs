using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// モンスタースロットプレハブ制御クラス
/// 責任範囲：
/// - 出現モンスターの基本情報表示
/// - モンスター名・アイコン表示
/// - レアリティ・属性の視覚的表現
/// </summary>
public class MonsterSlotUI : MonoBehaviour
{
    [Header("基本情報表示")]
    [SerializeField] private Image monsterIcon;
    [SerializeField] private TextMeshProUGUI monsterNameText;
    [SerializeField] private TextMeshProUGUI monsterTypeText;

    [Header("ステータス表示")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI offenseText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI speedText;

    [Header("レアリティ表現")]
    [SerializeField] private Image rarityBorder;
    [SerializeField] private Image rarityBackground;
    [SerializeField] private GameObject[] rarityStars;

    [Header("属性表現")]
    [SerializeField] private Image attributeIcon;
    [SerializeField] private Image attributeBackground;

    [Header("特殊表示")]
    [SerializeField] private GameObject bossIcon;
    [SerializeField] private GameObject criticalIcon;
    [SerializeField] private TextMeshProUGUI criticalRateText;

    [Header("カラー設定")]
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = Color.yellow;
    [SerializeField] private Color legendaryColor = Color.red;

    [Header("属性カラー設定")]
    [SerializeField] private Color fireColor = Color.red;
    [SerializeField] private Color waterColor = Color.blue;
    [SerializeField] private Color windColor = Color.green;
    [SerializeField] private Color earthColor = Color.yellow;
    [SerializeField] private Color noneColor = Color.gray;

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showDetailedStats = true;

    // 内部状態
    private MonsterMasterData monsterData;

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeComponents();
    }

    #endregion

    #region 初期化

    /// <summary>
    /// コンポーネントの初期化
    /// </summary>
    private void InitializeComponents()
    {
        // 初期状態設定
        if (bossIcon != null)
        {
            bossIcon.SetActive(false);
        }

        if (criticalIcon != null)
        {
            criticalIcon.SetActive(false);
        }

        // レアリティスター初期化
        if (rarityStars != null)
        {
            foreach (var star in rarityStars)
            {
                if (star != null)
                {
                    star.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// モンスタースロットを初期化
    /// </summary>
    /// <param name="monsterMasterData">モンスターマスターデータ</param>
    public void Initialize(MonsterMasterData monsterMasterData)
    {
        try
        {
            if (monsterMasterData == null)
            {
                LogError("MonsterMasterDataがnullです");
                return;
            }

            this.monsterData = monsterMasterData;

            // 基本情報表示
            DisplayBasicInfo();

            // ステータス表示
            DisplayStats();

            // レアリティ表現
            DisplayRarity();

            // 属性表現
            DisplayAttribute();

            // 特殊表示
            DisplaySpecialFeatures();

            // アイコン読み込み
            LoadMonsterIcon();

            Log($"モンスタースロット初期化完了: {monsterData.monsterName}");
        }
        catch (Exception e)
        {
            LogError($"モンスタースロット初期化エラー: {e.Message}");
        }
    }

    #endregion

    #region 基本情報表示

    /// <summary>
    /// 基本情報を表示
    /// </summary>
    private void DisplayBasicInfo()
    {
        if (monsterData == null) return;

        // モンスター名
        if (monsterNameText != null)
        {
            monsterNameText.text = monsterData.monsterName;
        }

        // モンスタータイプ
        if (monsterTypeText != null)
        {
            string typeDisplay = monsterData.IsBoss() ? "ボス" : "通常";
            monsterTypeText.text = typeDisplay;
        }
    }

    /// <summary>
    /// ステータスを表示
    /// </summary>
    private void DisplayStats()
    {
        if (monsterData == null || !showDetailedStats) return;

        // HP
        if (hpText != null)
        {
            hpText.text = $"HP: {monsterData.hp:N0}";
        }

        // 攻撃力
        if (offenseText != null)
        {
            offenseText.text = $"ATK: {monsterData.offense:N0}";
        }

        // 防御力
        if (defenseText != null)
        {
            defenseText.text = $"DEF: {monsterData.defense:N0}";
        }

        // 速度
        if (speedText != null)
        {
            speedText.text = $"SPD: {monsterData.speed:N0}";
        }
    }

    #endregion

    #region レアリティ表現

    /// <summary>
    /// レアリティを表現
    /// </summary>
    private void DisplayRarity()
    {
        if (monsterData == null) return;

        // レアリティカラー設定
        Color rarityColor = GetRarityColor(monsterData.rarity);

        if (rarityBorder != null)
        {
            rarityBorder.color = rarityColor;
        }

        if (rarityBackground != null)
        {
            Color bgColor = rarityColor;
            bgColor.a = 0.3f; // 透明度調整
            rarityBackground.color = bgColor;
        }

        // レアリティスター表示
        DisplayRarityStars(monsterData.rarity);
    }

    /// <summary>
    /// レアリティに対応する色を取得
    /// </summary>
    /// <param name="rarity">レアリティ</param>
    /// <returns>レアリティカラー</returns>
    private Color GetRarityColor(RarityType rarity)
    {
        return rarity switch
        {
            RarityType.Common => commonColor,
            RarityType.Rare => rareColor,
            RarityType.Epic => epicColor,
            RarityType.Legendary => legendaryColor,
            _ => commonColor
        };
    }

    /// <summary>
    /// レアリティスターを表示
    /// </summary>
    /// <param name="rarity">レアリティ</param>
    private void DisplayRarityStars(RarityType rarity)
    {
        if (rarityStars == null) return;

        int starCount = GetRarityStarCount(rarity);

        for (int i = 0; i < rarityStars.Length; i++)
        {
            if (rarityStars[i] != null)
            {
                rarityStars[i].SetActive(i < starCount);
            }
        }
    }

    /// <summary>
    /// レアリティに対応するスター数を取得
    /// </summary>
    /// <param name="rarity">レアリティ</param>
    /// <returns>スター数</returns>
    private int GetRarityStarCount(RarityType rarity)
    {
        return rarity switch
        {
            RarityType.Common => 1,
            RarityType.Rare => 2,
            RarityType.Epic => 3,
            RarityType.Legendary => 4,
            _ => 1
        };
    }

    #endregion

    #region 属性表現

    /// <summary>
    /// 属性を表現
    /// </summary>
    private void DisplayAttribute()
    {
        if (monsterData == null) return;

        // 属性アイコン
        if (attributeIcon != null)
        {
            LoadAttributeIcon(monsterData.attributeType);
        }

        // 属性背景色
        if (attributeBackground != null)
        {
            Color attributeColor = GetAttributeColor(monsterData.attributeType);
            attributeColor.a = 0.5f; // 透明度調整
            attributeBackground.color = attributeColor;
        }
    }

    /// <summary>
    /// 属性アイコンを読み込み
    /// </summary>
    /// <param name="attributeType">属性タイプ</param>
    private void LoadAttributeIcon(AttributeType attributeType)
    {
        try
        {
            if (attributeType == AttributeType.None)
            {
                attributeIcon.gameObject.SetActive(false);
                return;
            }

            string iconPath = GetAttributeIconPath(attributeType);
            var sprite = Resources.Load<Sprite>(iconPath);

            if (sprite != null)
            {
                attributeIcon.sprite = sprite;
                attributeIcon.gameObject.SetActive(true);
            }
            else
            {
                Log($"属性アイコンが見つかりません: {iconPath}");
                attributeIcon.gameObject.SetActive(false);
            }
        }
        catch (Exception e)
        {
            LogError($"属性アイコン読み込みエラー: {e.Message}");
            attributeIcon.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 属性アイコンパスを取得
    /// </summary>
    /// <param name="attributeType">属性タイプ</param>
    /// <returns>アイコンパス</returns>
    private string GetAttributeIconPath(AttributeType attributeType)
    {
        return attributeType switch
        {
            AttributeType.Fire => "Icons/Attribute/fire_icon",
            AttributeType.Water => "Icons/Attribute/water_icon",
            AttributeType.Wind => "Icons/Attribute/wind_icon",
            AttributeType.Earth => "Icons/Attribute/earth_icon",
            _ => ""
        };
    }

    /// <summary>
    /// 属性に対応する色を取得
    /// </summary>
    /// <param name="attributeType">属性タイプ</param>
    /// <returns>属性カラー</returns>
    private Color GetAttributeColor(AttributeType attributeType)
    {
        return attributeType switch
        {
            AttributeType.Fire => fireColor,
            AttributeType.Water => waterColor,
            AttributeType.Wind => windColor,
            AttributeType.Earth => earthColor,
            AttributeType.None => noneColor,
            _ => noneColor
        };
    }

    #endregion

    #region 特殊表示

    /// <summary>
    /// 特殊機能を表示
    /// </summary>
    private void DisplaySpecialFeatures()
    {
        if (monsterData == null) return;

        // ボスアイコン
        if (bossIcon != null)
        {
            bossIcon.SetActive(monsterData.IsBoss());
        }

        // クリティカル情報
        if (monsterData.criticalRate > 0)
        {
            if (criticalIcon != null)
            {
                criticalIcon.SetActive(true);
            }

            if (criticalRateText != null)
            {
                criticalRateText.text = $"{monsterData.criticalRate}%";
                criticalRateText.gameObject.SetActive(true);
            }
        }
        else
        {
            if (criticalIcon != null)
            {
                criticalIcon.SetActive(false);
            }

            if (criticalRateText != null)
            {
                criticalRateText.gameObject.SetActive(false);
            }
        }
    }

    #endregion

    #region アイコン読み込み（修正版）

    /// <summary>
    /// モンスターアイコンを読み込み（修正：正しいフォルダパス対応）
    /// </summary>
    private void LoadMonsterIcon()
    {
        try
        {
            if (monsterIcon == null || monsterData == null) return;

            string iconPath = GetMonsterIconPathFromMasterData();

            Log($"モンスターアイコンパス取得: ID={monsterData.monsterId}, Path={iconPath}");

            var sprite = Resources.Load<Sprite>(iconPath);

            if (sprite != null)
            {
                monsterIcon.sprite = sprite;
                monsterIcon.gameObject.SetActive(true);
                Log($"モンスターアイコン読み込み成功: {iconPath}");
            }
            else
            {
                Log($"モンスターアイコンが見つかりません: {iconPath}");
                SetDefaultMonsterIcon();
            }
        }
        catch (Exception e)
        {
            LogError($"モンスターアイコン読み込みエラー: {e.Message}");
            SetDefaultMonsterIcon();
        }
    }

    /// <summary>
    /// MasterDataManagerからモンスターアイコンパスを取得（修正：マスターデータ優先方式）
    /// </summary>
    /// <returns>アイコンパス</returns>
    private string GetMonsterIconPathFromMasterData()
    {
        // マスターデータにアイコンパスが設定されている場合は、それを優先して使用
        if (!string.IsNullOrEmpty(monsterData.monsterIconPath))
        {
            Log($"モンスターマスターデータからアイコンパス取得成功: ID={monsterData.monsterId}, Path={monsterData.monsterIconPath}");
            return monsterData.monsterIconPath;
        }
        else
        {
            LogError($"モンスターマスターデータにアイコンパスが設定されていません: ID={monsterData.monsterId}");
            LogError($"マスターデータの monsterIconPath プロパティを確認してください");

            // 緊急フォールバック（本来は使用されるべきではない）
            string emergencyFallback = $"Icons/Monster/default_monster";
            LogError($"緊急フォールバック使用: {emergencyFallback}");
            return emergencyFallback;
        }
    }

    /// <summary>
    /// デフォルトモンスターアイコンを設定
    /// </summary>
    private void SetDefaultMonsterIcon()
    {
        try
        {
            var defaultSprite = Resources.Load<Sprite>("Icons/Monster/default_monster");
            if (defaultSprite != null)
            {
                monsterIcon.sprite = defaultSprite;
                monsterIcon.gameObject.SetActive(true);
            }
            else
            {
                monsterIcon.gameObject.SetActive(false);
            }
        }
        catch (Exception e)
        {
            LogError($"デフォルトアイコン設定エラー: {e.Message}");
            monsterIcon.gameObject.SetActive(false);
        }
    }

    #endregion

    #region ユーティリティ

    /// <summary>
    /// モンスターデータを取得
    /// </summary>
    /// <returns>モンスターマスターデータ</returns>
    public MonsterMasterData GetMonsterData()
    {
        return monsterData;
    }

    /// <summary>
    /// スロットの有効性をチェック
    /// </summary>
    /// <returns>有効な場合true</returns>
    public bool IsValidSlot()
    {
        return monsterData != null && monsterData.monsterId > 0;
    }

    /// <summary>
    /// モンスター情報の更新
    /// </summary>
    /// <param name="updatedMonsterData">更新されたモンスターデータ</param>
    public void UpdateMonsterData(MonsterMasterData updatedMonsterData)
    {
        if (updatedMonsterData == null || updatedMonsterData.monsterId != monsterData?.monsterId)
        {
            LogError("無効なモンスターデータ更新要求");
            return;
        }

        Initialize(updatedMonsterData);
    }

    /// <summary>
    /// デバッグ情報を取得
    /// </summary>
    /// <returns>デバッグ用文字列</returns>
    public string GetDebugInfo()
    {
        if (monsterData == null) return "MonsterData: null";

        return $"Monster[{monsterData.monsterId}] {monsterData.monsterName} - " +
               $"Type: {monsterData.monsterType}, Attribute: {monsterData.attributeType}, " +
               $"Rarity: {monsterData.rarity}, HP: {monsterData.hp}, ATK: {monsterData.offense}";
    }

    #endregion

    #region ログ・デバッグ

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MonsterSlotUI] {message}");
        }
    }

    private void LogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[MonsterSlotUI] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("モンスター情報をログ出力")]
    private void LogMonsterInfo()
    {
        Log(GetDebugInfo());
    }

    [ContextMenu("レアリティ表示をテスト")]
    private void TestRarityDisplay()
    {
        if (monsterData != null)
        {
            DisplayRarity();
            Log($"レアリティ表示テスト: {monsterData.rarity}");
        }
    }

    [ContextMenu("属性表示をテスト")]
    private void TestAttributeDisplay()
    {
        if (monsterData != null)
        {
            DisplayAttribute();
            Log($"属性表示テスト: {monsterData.attributeType}");
        }
    }

    private void OnValidate()
    {
        // エディター上でのカラー変更を即座に反映
        if (Application.isPlaying && monsterData != null)
        {
            DisplayRarity();
            DisplayAttribute();
        }
    }
#endif

    #endregion
}