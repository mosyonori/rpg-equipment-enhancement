using System;
using System.Collections.Generic;
using UnityEngine;
using EquipmentEnhance.Data;

namespace EquipmentEnhance.Data
{
    /// <summary>
    /// 補助材料表示用データクラス
    /// 補助材料の表示情報と効果説明を統一管理
    /// 「使用しない」オプションの特別処理にも対応
    /// </summary>
    [Serializable]
    public class SupportItemDisplayData
    {
        #region 基本データ
        [Header("基本情報")]
        [SerializeField] private int supportItemId;
        [SerializeField] private string supportItemName;
        [SerializeField] private int quantity;
        [SerializeField] private bool isNoneOption;
        [SerializeField] private string iconPath;
        [SerializeField] private string rarity;
        #endregion

        #region 効果データ
        [Header("効果情報")]
        [SerializeField] private string effectDescription;
        [SerializeField] private List<string> effectTexts;
        [SerializeField] private bool hasPositiveEffect;
        [SerializeField] private bool hasNegativeEffect;
        #endregion

        #region 詳細効果値
        [Header("詳細効果値")]
        [SerializeField] private int addEnhanceSuccessRate;
        [SerializeField] private int reduceEnhanceSuccessRate;
        [SerializeField] private int addEnhancedValue;
        [SerializeField] private int multiplEnhancedValue;
        [SerializeField] private int addEnhanceStamina;
        [SerializeField] private int multiplStatusUp;
        #endregion

        #region コンストラクタ
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public SupportItemDisplayData()
        {
            supportItemId = -1;
            supportItemName = "";
            quantity = 0;
            isNoneOption = false;
            iconPath = "";
            rarity = "common";
            effectDescription = "";
            effectTexts = new List<string>();
            hasPositiveEffect = false;
            hasNegativeEffect = false;
        }

        /// <summary>
        /// 基本情報指定コンストラクタ
        /// </summary>
        public SupportItemDisplayData(int itemId, string itemName, int itemQuantity, bool noneOption = false)
        {
            supportItemId = itemId;
            supportItemName = itemName;
            quantity = itemQuantity;
            isNoneOption = noneOption;
            iconPath = "";
            rarity = "common";
            effectDescription = "";
            effectTexts = new List<string>();
            hasPositiveEffect = false;
            hasNegativeEffect = false;
        }

        /// <summary>
        /// 完全指定コンストラクタ
        /// </summary>
        public SupportItemDisplayData(int itemId, string itemName, int itemQuantity,
            string description, string icon, string itemRarity,
            int successRateAdd, int successRateReduce, int enhancedValueAdd,
            int enhancedValueMultipl, int staminaAdd, int statusMultipl)
        {
            supportItemId = itemId;
            supportItemName = itemName;
            quantity = itemQuantity;
            isNoneOption = false;
            iconPath = icon;
            rarity = itemRarity;
            effectDescription = description;

            addEnhanceSuccessRate = successRateAdd;
            reduceEnhanceSuccessRate = successRateReduce;
            addEnhancedValue = enhancedValueAdd;
            multiplEnhancedValue = enhancedValueMultipl;
            addEnhanceStamina = staminaAdd;
            multiplStatusUp = statusMultipl;

            GenerateEffectTexts();
            DetermineEffectTypes();
        }
        #endregion

        #region プロパティ（読み取り専用）
        /// <summary>補助材料ID</summary>
        public int SupportItemId => supportItemId;

        /// <summary>補助材料名</summary>
        public string SupportItemName => supportItemName;

        /// <summary>所持数</summary>
        public int Quantity => quantity;

        /// <summary>「使用しない」オプションか</summary>
        public bool IsNoneOption => isNoneOption;

        /// <summary>アイコンパス</summary>
        public string IconPath => iconPath;

        /// <summary>レアリティ</summary>
        public string Rarity => rarity;

        /// <summary>効果説明</summary>
        public string EffectDescription => effectDescription;

        /// <summary>効果テキストリスト</summary>
        public List<string> EffectTexts => new List<string>(effectTexts);

        /// <summary>正の効果があるか</summary>
        public bool HasPositiveEffect => hasPositiveEffect;

        /// <summary>負の効果があるか</summary>
        public bool HasNegativeEffect => hasNegativeEffect;

        /// <summary>成功率増加値</summary>
        public int AddEnhanceSuccessRate => addEnhanceSuccessRate;

        /// <summary>成功率減少値</summary>
        public int ReduceEnhanceSuccessRate => reduceEnhanceSuccessRate;

        /// <summary>強化値加算</summary>
        public int AddEnhancedValue => addEnhancedValue;

        /// <summary>強化値倍率</summary>
        public int MultiplEnhancedValue => multiplEnhancedValue;

        /// <summary>強化耐久加算</summary>
        public int AddEnhanceStamina => addEnhanceStamina;

        /// <summary>ステータス倍率</summary>
        public int MultiplStatusUp => multiplStatusUp;
        #endregion

        #region 判定プロパティ
        /// <summary>使用可能か（所持数が1以上）</summary>
        public bool IsUsable => isNoneOption || quantity > 0;

        /// <summary>実際のアイテムか（「使用しない」ではない）</summary>
        public bool IsRealItem => !isNoneOption && supportItemId > 0;

        /// <summary>効果があるか</summary>
        public bool HasAnyEffect => hasPositiveEffect || hasNegativeEffect;

        /// <summary>成功率に影響するか</summary>
        public bool AffectsSuccessRate => addEnhanceSuccessRate != 0 || reduceEnhanceSuccessRate != 0;

        /// <summary>強化値に影響するか</summary>
        public bool AffectsEnhanceValue => addEnhancedValue != 0 || multiplEnhancedValue > 1;

        /// <summary>ステータスに影響するか</summary>
        public bool AffectsStatus => multiplStatusUp > 1;

        /// <summary>強化耐久に影響するか</summary>
        public bool AffectsStamina => addEnhanceStamina != 0;
        #endregion

        #region ファクトリーメソッド
        /// <summary>
        /// 「使用しない」オプションを生成
        /// </summary>
        public static SupportItemDisplayData CreateNoneOption()
        {
            var noneOption = new SupportItemDisplayData(-1, "使用しない", 1, true);
            noneOption.effectDescription = "補助材料を使用せずに強化を実行します";
            noneOption.effectTexts = new List<string> { "追加効果なし" };
            return noneOption;
        }

        /// <summary>
        /// SupportItemMasterDataから生成
        /// </summary>
        public static SupportItemDisplayData FromMasterData(SupportItemMasterData masterData, int ownedQuantity)
        {
            if (masterData == null)
            {
                Debug.LogWarning("SupportItemDisplayData.FromMasterData: masterData is null");
                return new SupportItemDisplayData();
            }

            return new SupportItemDisplayData(
                masterData.support_item_id,
                masterData.support_item_name,
                ownedQuantity,
                masterData.description,
                masterData.enhance_item_icon_path,
                masterData.rarity,
                masterData.add_enhance_success_rate,
                masterData.reduce_enhance_success_rate,
                masterData.add_enhanced_value,
                masterData.multipl_enhanced_value,
                masterData.add_enhance_stamina,
                masterData.multipl_status_up
            );
        }

        /// <summary>
        /// リストに「使用しない」オプションを含めて生成
        /// </summary>
        public static List<SupportItemDisplayData> CreateListWithNoneOption(List<SupportItemMasterData> masterDataList,
            List<UserItem> ownedItems)
        {
            var result = new List<SupportItemDisplayData>();

            // 「使用しない」オプションを最初に追加
            result.Add(CreateNoneOption());

            // 所持している補助材料を追加
            foreach (var masterData in masterDataList)
            {
                var ownedItem = ownedItems.Find(item => item.item_id == masterData.support_item_id);
                int quantity = ownedItem?.quantity ?? 0;

                if (quantity > 0)
                {
                    result.Add(FromMasterData(masterData, quantity));
                }
            }

            return result;
        }
        #endregion

        #region データ操作メソッド
        /// <summary>
        /// 所持数を更新
        /// </summary>
        public void UpdateQuantity(int newQuantity)
        {
            if (!isNoneOption)
            {
                quantity = Math.Max(0, newQuantity);
            }
        }

        /// <summary>
        /// 効果値を設定
        /// </summary>
        public void SetEffectValues(int successRateAdd, int successRateReduce, int enhancedValueAdd,
            int enhancedValueMultipl, int staminaAdd, int statusMultipl)
        {
            addEnhanceSuccessRate = successRateAdd;
            reduceEnhanceSuccessRate = successRateReduce;
            addEnhancedValue = enhancedValueAdd;
            multiplEnhancedValue = enhancedValueMultipl;
            addEnhanceStamina = staminaAdd;
            multiplStatusUp = statusMultipl;

            GenerateEffectTexts();
            DetermineEffectTypes();
        }

        /// <summary>
        /// カスタム効果説明を設定
        /// </summary>
        public void SetCustomDescription(string description)
        {
            effectDescription = description;
        }
        #endregion

        #region UI表示用メソッド
        /// <summary>
        /// 表示用名前テキストを取得
        /// </summary>
        public string GetDisplayName()
        {
            if (isNoneOption)
            {
                return supportItemName;
            }

            return $"{supportItemName} ({quantity})";
        }

        /// <summary>
        /// 所持数表示テキストを取得
        /// </summary>
        public string GetQuantityText()
        {
            if (isNoneOption)
            {
                return "";
            }

            return $"所持数: {quantity}";
        }

        /// <summary>
        /// レアリティ色を取得
        /// </summary>
        public Color GetRarityColor()
        {
            switch (rarity.ToLower())
            {
                case "common": return Color.gray;
                case "rare": return Color.blue;
                case "epic": return Color.magenta;
                case "legendary": return Color.yellow;
                default: return Color.white;
            }
        }

        /// <summary>
        /// 効果サマリーテキストを取得
        /// </summary>
        public string GetEffectSummary()
        {
            if (isNoneOption)
            {
                return "効果なし";
            }

            if (effectTexts.Count == 0)
            {
                return "効果なし";
            }

            return string.Join(", ", effectTexts);
        }

        /// <summary>
        /// 詳細効果テキストを取得
        /// </summary>
        public string GetDetailedEffectText()
        {
            if (isNoneOption)
            {
                return effectDescription;
            }

            if (effectTexts.Count == 0)
            {
                return effectDescription;
            }

            return $"{effectDescription}\n\n効果:\n• " + string.Join("\n• ", effectTexts);
        }
        #endregion

        #region 内部ヘルパーメソッド
        /// <summary>
        /// 効果テキストを生成
        /// </summary>
        private void GenerateEffectTexts()
        {
            effectTexts.Clear();

            if (isNoneOption)
            {
                effectTexts.Add("追加効果なし");
                return;
            }

            // 成功率効果
            if (addEnhanceSuccessRate > 0)
            {
                effectTexts.Add($"成功率 +{addEnhanceSuccessRate}%");
            }

            if (reduceEnhanceSuccessRate > 0)
            {
                effectTexts.Add($"成功率 -{reduceEnhanceSuccessRate}%");
            }

            // 強化値効果
            if (addEnhancedValue > 0)
            {
                effectTexts.Add($"強化値 +{addEnhancedValue}");
            }

            if (multiplEnhancedValue > 1)
            {
                effectTexts.Add($"強化値 x{multiplEnhancedValue}");
            }

            // ステータス効果
            if (multiplStatusUp > 1)
            {
                effectTexts.Add($"ステータス x{multiplStatusUp}");
            }

            // 強化耐久効果
            if (addEnhanceStamina > 0)
            {
                effectTexts.Add($"強化耐久 +{addEnhanceStamina}");
            }

            // 効果がない場合
            if (effectTexts.Count == 0)
            {
                effectTexts.Add("効果なし");
            }
        }

        /// <summary>
        /// 効果タイプを判定
        /// </summary>
        private void DetermineEffectTypes()
        {
            hasPositiveEffect = addEnhanceSuccessRate > 0 || addEnhancedValue > 0 ||
                               multiplEnhancedValue > 1 || multiplStatusUp > 1 || addEnhanceStamina > 0;

            hasNegativeEffect = reduceEnhanceSuccessRate > 0;
        }
        #endregion

        #region デバッグ用
        /// <summary>
        /// デバッグ用文字列表現
        /// </summary>
        public override string ToString()
        {
            if (isNoneOption)
            {
                return "SupportItemDisplayData: 使用しない";
            }

            return $"SupportItemDisplayData: {supportItemName} (ID:{supportItemId}, 所持数:{quantity})";
        }

        /// <summary>
        /// 妥当性チェック
        /// </summary>
        public bool IsValid()
        {
            // 基本的な妥当性チェック
            bool hasValidName = !string.IsNullOrEmpty(supportItemName);
            bool hasValidQuantity = quantity >= 0;
            bool hasValidId = isNoneOption ? supportItemId == -1 : supportItemId > 0;

            return hasValidName && hasValidQuantity && hasValidId;
        }

        /// <summary>
        /// 効果データの整合性チェック
        /// </summary>
        public bool IsEffectDataConsistent()
        {
            if (isNoneOption)
            {
                // 「使用しない」オプションは全て0であるべき
                return addEnhanceSuccessRate == 0 && reduceEnhanceSuccessRate == 0 &&
                       addEnhancedValue == 0 && multiplEnhancedValue <= 1 &&
                       addEnhanceStamina == 0 && multiplStatusUp <= 1;
            }

            // 実際のアイテムは負の値を持たないべき
            return addEnhanceSuccessRate >= 0 && reduceEnhanceSuccessRate >= 0 &&
                   addEnhancedValue >= 0 && multiplEnhancedValue >= 0 &&
                   addEnhanceStamina >= 0 && multiplStatusUp >= 0;
        }
        #endregion
    }
}