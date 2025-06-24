using System;
using UnityEngine;

namespace EquipmentEnhance.Data
{
    /// <summary>
    /// 装備強化プレビュー用データクラス
    /// 強化前後のステータス比較と変化量計算を提供
    /// 装備種類別（武器・防具・アクセサリ）の強化内容に対応
    /// </summary>
    [Serializable]
    public class EnhancePreviewData
    {
        #region 現在のステータス
        [Header("現在のステータス")]
        [SerializeField] private int currentEnhanceValue;
        [SerializeField] private int currentHP;
        [SerializeField] private int currentOffense;
        [SerializeField] private int currentDefense;
        [SerializeField] private int currentSpeed;
        [SerializeField] private int currentCriticalRate;
        [SerializeField] private int currentCriticalDamageRate;
        [SerializeField] private int currentEnhanceStamina;

        // 属性攻撃
        [SerializeField] private int currentFireOffence;
        [SerializeField] private int currentWaterOffence;
        [SerializeField] private int currentWindOffence;
        [SerializeField] private int currentEarthOffence;
        #endregion

        #region 予想変化量
        [Header("予想変化量")]
        [SerializeField] private int enhanceValueIncrease;
        [SerializeField] private int hpIncrease;
        [SerializeField] private int offenseIncrease;
        [SerializeField] private int defenseIncrease;
        [SerializeField] private int speedIncrease;
        [SerializeField] private int criticalRateIncrease;
        [SerializeField] private int criticalDamageRateIncrease;
        [SerializeField] private int enhanceStaminaChange; // 正負両方対応

        // 属性攻撃変化量
        [SerializeField] private int fireOffenceChange;
        [SerializeField] private int waterOffenceChange;
        [SerializeField] private int windOffenceChange;
        [SerializeField] private int earthOffenceChange;
        #endregion

        #region コンストラクタ
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public EnhancePreviewData()
        {
            // 全ての値を0で初期化
        }

        /// <summary>
        /// 現在ステータス指定コンストラクタ
        /// </summary>
        public EnhancePreviewData(int enhanceValue, int hp, int offense, int defense,
            int speed, int criticalRate, int criticalDamageRate, int enhanceStamina,
            int fireOffence, int waterOffence, int windOffence, int earthOffence)
        {
            currentEnhanceValue = enhanceValue;
            currentHP = hp;
            currentOffense = offense;
            currentDefense = defense;
            currentSpeed = speed;
            currentCriticalRate = criticalRate;
            currentCriticalDamageRate = criticalDamageRate;
            currentEnhanceStamina = enhanceStamina;
            currentFireOffence = fireOffence;
            currentWaterOffence = waterOffence;
            currentWindOffence = windOffence;
            currentEarthOffence = earthOffence;
        }
        #endregion

        #region 現在値プロパティ（読み取り専用）
        public int CurrentEnhanceValue => currentEnhanceValue;
        public int CurrentHP => currentHP;
        public int CurrentOffense => currentOffense;
        public int CurrentDefense => currentDefense;
        public int CurrentSpeed => currentSpeed;
        public int CurrentCriticalRate => currentCriticalRate;
        public int CurrentCriticalDamageRate => currentCriticalDamageRate;
        public int CurrentEnhanceStamina => currentEnhanceStamina;
        public int CurrentFireOffence => currentFireOffence;
        public int CurrentWaterOffence => currentWaterOffence;
        public int CurrentWindOffence => currentWindOffence;
        public int CurrentEarthOffence => currentEarthOffence;
        #endregion

        #region 変化量プロパティ（読み取り専用）
        public int EnhanceValueIncrease => enhanceValueIncrease;
        public int HPIncrease => hpIncrease;
        public int OffenseIncrease => offenseIncrease;
        public int DefenseIncrease => defenseIncrease;
        public int SpeedIncrease => speedIncrease;
        public int CriticalRateIncrease => criticalRateIncrease;
        public int CriticalDamageRateIncrease => criticalDamageRateIncrease;
        public int EnhanceStaminaChange => enhanceStaminaChange;
        public int FireOffenceChange => fireOffenceChange;
        public int WaterOffenceChange => waterOffenceChange;
        public int WindOffenceChange => windOffenceChange;
        public int EarthOffenceChange => earthOffenceChange;
        #endregion

        #region 強化後予想値プロパティ（計算）
        public int AfterEnhanceValue => currentEnhanceValue + enhanceValueIncrease;
        public int AfterHP => Math.Max(0, currentHP + hpIncrease);
        public int AfterOffense => Math.Max(0, currentOffense + offenseIncrease);
        public int AfterDefense => Math.Max(0, currentDefense + defenseIncrease);
        public int AfterSpeed => Math.Max(0, currentSpeed + speedIncrease);
        public int AfterCriticalRate => Math.Max(0, currentCriticalRate + criticalRateIncrease);
        public int AfterCriticalDamageRate => Math.Max(0, currentCriticalDamageRate + criticalDamageRateIncrease);
        public int AfterEnhanceStamina => Math.Max(0, currentEnhanceStamina + enhanceStaminaChange);
        public int AfterFireOffence => Math.Max(0, currentFireOffence + fireOffenceChange);
        public int AfterWaterOffence => Math.Max(0, currentWaterOffence + waterOffenceChange);
        public int AfterWindOffence => Math.Max(0, currentWindOffence + windOffenceChange);
        public int AfterEarthOffence => Math.Max(0, currentEarthOffence + earthOffenceChange);
        #endregion

        #region 変化判定プロパティ
        /// <summary>強化値が増加するか</summary>
        public bool HasEnhanceValueIncrease => enhanceValueIncrease > 0;

        /// <summary>ステータスに変化があるか</summary>
        public bool HasStatusChange =>
            hpIncrease != 0 || offenseIncrease != 0 || defenseIncrease != 0 ||
            speedIncrease != 0 || criticalRateIncrease != 0 || criticalDamageRateIncrease != 0;

        /// <summary>属性攻撃に変化があるか</summary>
        public bool HasAttributeChange =>
            fireOffenceChange != 0 || waterOffenceChange != 0 ||
            windOffenceChange != 0 || earthOffenceChange != 0;

        /// <summary>強化耐久に変化があるか</summary>
        public bool HasStaminaChange => enhanceStaminaChange != 0;

        /// <summary>何らかの変化があるか</summary>
        public bool HasAnyChange =>
            HasEnhanceValueIncrease || HasStatusChange || HasAttributeChange || HasStaminaChange;
        #endregion

        #region セッターメソッド（immutable設計のため制限的）
        /// <summary>
        /// 現在のステータスを設定
        /// </summary>
        public void SetCurrentStatus(int enhanceValue, int hp, int offense, int defense,
            int speed, int criticalRate, int criticalDamageRate, int enhanceStamina,
            int fireOffence, int waterOffence, int windOffence, int earthOffence)
        {
            currentEnhanceValue = enhanceValue;
            currentHP = hp;
            currentOffense = offense;
            currentDefense = defense;
            currentSpeed = speed;
            currentCriticalRate = criticalRate;
            currentCriticalDamageRate = criticalDamageRate;
            currentEnhanceStamina = enhanceStamina;
            currentFireOffence = fireOffence;
            currentWaterOffence = waterOffence;
            currentWindOffence = windOffence;
            currentEarthOffence = earthOffence;
        }

        /// <summary>
        /// 変化量を設定
        /// </summary>
        public void SetChanges(int enhanceValueInc, int hpInc, int offenseInc, int defenseInc,
            int speedInc, int criticalRateInc, int criticalDamageRateInc, int staminaChange,
            int fireOffenceChg, int waterOffenceChg, int windOffenceChg, int earthOffenceChg)
        {
            enhanceValueIncrease = enhanceValueInc;
            hpIncrease = hpInc;
            offenseIncrease = offenseInc;
            defenseIncrease = defenseInc;
            speedIncrease = speedInc;
            criticalRateIncrease = criticalRateInc;
            criticalDamageRateIncrease = criticalDamageRateInc;
            enhanceStaminaChange = staminaChange;
            fireOffenceChange = fireOffenceChg;
            waterOffenceChange = waterOffenceChg;
            windOffenceChange = windOffenceChg;
            earthOffenceChange = earthOffenceChg;
        }
        #endregion

        #region ファクトリーメソッド
        /// <summary>
        /// UserEquipmentから現在ステータスを設定したインスタンスを生成
        /// </summary>
        public static EnhancePreviewData FromUserEquipment(UserEquipment equipment)
        {
            if (equipment == null)
            {
                Debug.LogWarning("EnhancePreviewData.FromUserEquipment: equipment is null");
                return new EnhancePreviewData();
            }

            return new EnhancePreviewData(
                equipment.current_enhanced_value,
                equipment.hp,
                equipment.offense,
                equipment.defense,
                equipment.speed,
                equipment.critical_rate,
                equipment.critical_damage_rate,
                equipment.current_enhance_stamina,
                equipment.fire_offence,
                equipment.water_offence,
                equipment.wind_offence,
                equipment.earth_offence
            );
        }

        /// <summary>
        /// 変化なしのプレビューデータを生成
        /// </summary>
        public static EnhancePreviewData CreateNoChange(UserEquipment equipment)
        {
            var preview = FromUserEquipment(equipment);
            // 変化量は全て0のまま
            return preview;
        }
        #endregion

        #region UI表示用メソッド
        /// <summary>
        /// 変化量のテキスト表示を取得
        /// </summary>
        public string GetChangeText(int changeValue)
        {
            if (changeValue == 0) return "";
            return changeValue > 0 ? $" (+{changeValue})" : $" ({changeValue})";
        }

        /// <summary>
        /// ステータス表示用テキストを取得
        /// </summary>
        public string GetStatusDisplayText(int currentValue, int changeValue)
        {
            int afterValue = Math.Max(0, currentValue + changeValue);
            string changeText = GetChangeText(changeValue);
            return $"{afterValue}{changeText}";
        }

        /// <summary>
        /// 強化値表示テキスト
        /// </summary>
        public string GetEnhanceValueDisplayText()
        {
            return GetStatusDisplayText(currentEnhanceValue, enhanceValueIncrease);
        }

        /// <summary>
        /// HP表示テキスト
        /// </summary>
        public string GetHPDisplayText()
        {
            return GetStatusDisplayText(currentHP, hpIncrease);
        }

        /// <summary>
        /// 攻撃力表示テキスト
        /// </summary>
        public string GetOffenseDisplayText()
        {
            return GetStatusDisplayText(currentOffense, offenseIncrease);
        }

        /// <summary>
        /// 防御力表示テキスト
        /// </summary>
        public string GetDefenseDisplayText()
        {
            return GetStatusDisplayText(currentDefense, defenseIncrease);
        }

        /// <summary>
        /// 強化耐久表示テキスト
        /// </summary>
        public string GetEnhanceStaminaDisplayText()
        {
            return GetStatusDisplayText(currentEnhanceStamina, enhanceStaminaChange);
        }
        #endregion

        #region デバッグ用
        /// <summary>
        /// デバッグ用文字列表現
        /// </summary>
        public override string ToString()
        {
            return $"EnhancePreviewData: " +
                   $"強化値{GetEnhanceValueDisplayText()}, " +
                   $"HP{GetHPDisplayText()}, " +
                   $"攻撃{GetOffenseDisplayText()}, " +
                   $"防御{GetDefenseDisplayText()}";
        }

        /// <summary>
        /// 妥当性チェック
        /// </summary>
        public bool IsValid()
        {
            // 基本的な妥当性チェック
            return currentEnhanceValue >= 0 &&
                   currentHP >= 0 &&
                   currentOffense >= 0 &&
                   currentDefense >= 0 &&
                   currentEnhanceStamina >= 0;
        }
        #endregion
    }
}