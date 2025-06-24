using System;
using UnityEngine;
using EquipmentEnhance.Data;

namespace EquipmentEnhance.Data
{
    /// <summary>
    /// 装備強化実行結果データクラス
    /// 強化の成功/失敗結果と関連データを保持
    /// UI表示用メッセージ生成と履歴追跡に使用
    /// </summary>
    [Serializable]
    public class EnhanceResultData
    {
        #region 基本結果データ
        [Header("強化実行結果")]
        [SerializeField] private bool isSuccess;
        [SerializeField] private DateTime executeTime;
        [SerializeField] private string resultMessage;
        [SerializeField] private float successRate; // 実行時の成功率
        #endregion

        #region 装備データ
        [Header("装備関連")]
        [SerializeField] private UserEquipment originalEquipment; // 強化前の装備（コピー）
        [SerializeField] private UserEquipment enhancedEquipment; // 強化後の装備
        [SerializeField] private string equipmentUniqueId; // 装備の一意ID
        #endregion

        #region 消費アイテム
        [Header("消費アイテム")]
        [SerializeField] private int consumedEnhanceItemId;
        [SerializeField] private int consumedSupportItemId;
        [SerializeField] private bool usedSupportItem; // 補助材料を使用したか
        #endregion

        #region プレビューデータ
        [Header("プレビューデータ")]
        [SerializeField] private EnhancePreviewData previewData; // 強化前後比較用
        #endregion

        #region コンストラクタ
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public EnhanceResultData()
        {
            executeTime = DateTime.Now;
            isSuccess = false;
            resultMessage = "";
            consumedEnhanceItemId = -1;
            consumedSupportItemId = -1;
            usedSupportItem = false;
            successRate = 0f;
        }

        /// <summary>
        /// 成功結果用コンストラクタ
        /// </summary>
        public EnhanceResultData(UserEquipment original, UserEquipment enhanced,
            int enhanceItemId, int supportItemId, float rate,
            EnhancePreviewData preview = null)
        {
            executeTime = DateTime.Now;
            isSuccess = true;
            originalEquipment = CloneEquipment(original);
            enhancedEquipment = enhanced;
            equipmentUniqueId = enhanced?.unique_id;
            consumedEnhanceItemId = enhanceItemId;
            consumedSupportItemId = supportItemId;
            usedSupportItem = supportItemId > 0;
            successRate = rate;
            previewData = preview;
            resultMessage = GenerateSuccessMessage();
        }

        /// <summary>
        /// 失敗結果用コンストラクタ
        /// </summary>
        public EnhanceResultData(UserEquipment equipment, int enhanceItemId,
            int supportItemId, float rate, string failureReason = "")
        {
            executeTime = DateTime.Now;
            isSuccess = false;
            originalEquipment = CloneEquipment(equipment);
            enhancedEquipment = CloneEquipment(equipment); // 失敗時は元のまま（耐久のみ減少）
            equipmentUniqueId = equipment?.unique_id;
            consumedEnhanceItemId = enhanceItemId;
            consumedSupportItemId = supportItemId;
            usedSupportItem = supportItemId > 0;
            successRate = rate;
            resultMessage = GenerateFailureMessage(failureReason);
        }
        #endregion

        #region プロパティ（読み取り専用）
        /// <summary>強化成功フラグ</summary>
        public bool IsSuccess => isSuccess;

        /// <summary>強化失敗フラグ</summary>
        public bool IsFailure => !isSuccess;

        /// <summary>実行時刻</summary>
        public DateTime ExecuteTime => executeTime;

        /// <summary>結果メッセージ</summary>
        public string ResultMessage => resultMessage;

        /// <summary>実行時の成功率</summary>
        public float SuccessRate => successRate;

        /// <summary>強化前の装備</summary>
        public UserEquipment OriginalEquipment => originalEquipment;

        /// <summary>強化後の装備</summary>
        public UserEquipment EnhancedEquipment => enhancedEquipment;

        /// <summary>装備の一意ID</summary>
        public string EquipmentUniqueId => equipmentUniqueId;

        /// <summary>消費した強化アイテムID</summary>
        public int ConsumedEnhanceItemId => consumedEnhanceItemId;

        /// <summary>消費した補助材料ID</summary>
        public int ConsumedSupportItemId => consumedSupportItemId;

        /// <summary>補助材料を使用したか</summary>
        public bool UsedSupportItem => usedSupportItem;

        /// <summary>プレビューデータ</summary>
        public EnhancePreviewData PreviewData => previewData;
        #endregion

        #region ファクトリーメソッド
        /// <summary>
        /// 成功結果データを生成
        /// </summary>
        public static EnhanceResultData CreateSuccess(UserEquipment original, UserEquipment enhanced,
            int enhanceItemId, int supportItemId, float successRate, EnhancePreviewData preview = null)
        {
            return new EnhanceResultData(original, enhanced, enhanceItemId, supportItemId, successRate, preview);
        }

        /// <summary>
        /// 失敗結果データを生成
        /// </summary>
        public static EnhanceResultData CreateFailure(UserEquipment equipment, int enhanceItemId,
            int supportItemId, float successRate, string reason = "")
        {
            return new EnhanceResultData(equipment, enhanceItemId, supportItemId, successRate, reason);
        }

        /// <summary>
        /// エラー結果データを生成
        /// </summary>
        public static EnhanceResultData CreateError(string errorMessage)
        {
            var result = new EnhanceResultData();
            result.isSuccess = false;
            result.resultMessage = $"エラー: {errorMessage}";
            return result;
        }
        #endregion

        #region データ操作メソッド
        /// <summary>
        /// プレビューデータを設定
        /// </summary>
        public void SetPreviewData(EnhancePreviewData preview)
        {
            previewData = preview;
        }

        /// <summary>
        /// カスタムメッセージを設定
        /// </summary>
        public void SetCustomMessage(string message)
        {
            resultMessage = message;
        }

        /// <summary>
        /// 強化後装備を更新（失敗後の耐久減少など）
        /// </summary>
        public void UpdateEnhancedEquipment(UserEquipment updated)
        {
            enhancedEquipment = updated;
        }
        #endregion

        #region 判定メソッド
        /// <summary>
        /// 強化値が変化したか
        /// </summary>
        public bool HasEnhanceValueChanged()
        {
            if (originalEquipment == null || enhancedEquipment == null) return false;
            return originalEquipment.current_enhanced_value != enhancedEquipment.current_enhanced_value;
        }

        /// <summary>
        /// ステータスが変化したか
        /// </summary>
        public bool HasStatusChanged()
        {
            if (originalEquipment == null || enhancedEquipment == null) return false;

            return originalEquipment.hp != enhancedEquipment.hp ||
                   originalEquipment.offense != enhancedEquipment.offense ||
                   originalEquipment.defense != enhancedEquipment.defense ||
                   originalEquipment.speed != enhancedEquipment.speed ||
                   originalEquipment.critical_rate != enhancedEquipment.critical_rate ||
                   originalEquipment.critical_damage_rate != enhancedEquipment.critical_damage_rate;
        }

        /// <summary>
        /// 属性攻撃が変化したか
        /// </summary>
        public bool HasAttributeChanged()
        {
            if (originalEquipment == null || enhancedEquipment == null) return false;

            return originalEquipment.fire_offence != enhancedEquipment.fire_offence ||
                   originalEquipment.water_offence != enhancedEquipment.water_offence ||
                   originalEquipment.wind_offence != enhancedEquipment.wind_offence ||
                   originalEquipment.earth_offence != enhancedEquipment.earth_offence;
        }

        /// <summary>
        /// 強化耐久が変化したか
        /// </summary>
        public bool HasStaminaChanged()
        {
            if (originalEquipment == null || enhancedEquipment == null) return false;
            return originalEquipment.current_enhance_stamina != enhancedEquipment.current_enhance_stamina;
        }
        #endregion

        #region UI表示用メソッド
        /// <summary>
        /// 成功率表示テキストを取得
        /// </summary>
        public string GetSuccessRateText()
        {
            return $"{successRate:F1}%";
        }

        /// <summary>
        /// 実行時刻表示テキストを取得
        /// </summary>
        public string GetExecuteTimeText()
        {
            return executeTime.ToString("yyyy/MM/dd HH:mm:ss");
        }

        /// <summary>
        /// 短縮結果表示テキストを取得
        /// </summary>
        public string GetShortResultText()
        {
            if (isSuccess)
            {
                return $"強化成功！ +{enhancedEquipment?.current_enhanced_value - originalEquipment?.current_enhanced_value}";
            }
            else
            {
                return "強化失敗...";
            }
        }

        /// <summary>
        /// 詳細結果表示テキストを取得
        /// </summary>
        public string GetDetailedResultText()
        {
            string baseText = GetShortResultText();
            string timeText = GetExecuteTimeText();
            string rateText = GetSuccessRateText();

            return $"{baseText}\n実行時刻: {timeText}\n成功率: {rateText}";
        }
        #endregion

        #region 内部ヘルパーメソッド
        /// <summary>
        /// 装備データのディープコピーを作成
        /// </summary>
        private UserEquipment CloneEquipment(UserEquipment original)
        {
            if (original == null) return null;

            // TODO: 実際のUserEquipmentクラスにCloneメソッドがあればそれを使用
            // ここでは簡易的な実装例
            return new UserEquipment
            {
                unique_id = original.unique_id,
                equipment_id = original.equipment_id,
                current_enhanced_value = original.current_enhanced_value,
                hp = original.hp,
                offense = original.offense,
                defense = original.defense,
                speed = original.speed,
                critical_rate = original.critical_rate,
                critical_damage_rate = original.critical_damage_rate,
                current_enhance_stamina = original.current_enhance_stamina,
                fire_offence = original.fire_offence,
                water_offence = original.water_offence,
                wind_offence = original.wind_offence,
                earth_offence = original.earth_offence,
                is_equipped = original.is_equipped
            };
        }

        /// <summary>
        /// 成功メッセージを生成
        /// </summary>
        private string GenerateSuccessMessage()
        {
            if (enhancedEquipment == null) return "強化成功！";

            int enhanceIncrease = enhancedEquipment.current_enhanced_value -
                                  (originalEquipment?.current_enhanced_value ?? 0);

            return $"強化成功！強化値が +{enhanceIncrease} 上昇しました！";
        }

        /// <summary>
        /// 失敗メッセージを生成
        /// </summary>
        private string GenerateFailureMessage(string reason = "")
        {
            string baseMessage = "強化に失敗しました...";

            if (!string.IsNullOrEmpty(reason))
            {
                baseMessage += $"\n原因: {reason}";
            }

            baseMessage += "\n強化アイテムと補助材料は消費されました。";

            return baseMessage;
        }
        #endregion

        #region デバッグ用
        /// <summary>
        /// デバッグ用文字列表現
        /// </summary>
        public override string ToString()
        {
            string status = isSuccess ? "成功" : "失敗";
            string equipmentInfo = enhancedEquipment != null ?
                $"装備ID:{enhancedEquipment.equipment_id}, 強化値:{enhancedEquipment.current_enhanced_value}" :
                "装備なし";

            return $"EnhanceResultData: {status}, {equipmentInfo}, 成功率:{successRate}%";
        }

        /// <summary>
        /// データの妥当性をチェック
        /// </summary>
        public bool IsValid()
        {
            // 基本的な妥当性チェック
            bool hasEquipmentData = enhancedEquipment != null;
            bool hasValidItemId = consumedEnhanceItemId > 0;
            bool hasValidMessage = !string.IsNullOrEmpty(resultMessage);
            bool hasValidRate = successRate >= 0f && successRate <= 100f;

            return hasEquipmentData && hasValidItemId && hasValidMessage && hasValidRate;
        }

        /// <summary>
        /// データの整合性をチェック
        /// </summary>
        public bool IsConsistent()
        {
            if (!IsValid()) return false;

            // 成功時は何らかの変化があるべき
            if (isSuccess)
            {
                return HasEnhanceValueChanged() || HasStatusChanged() ||
                       HasAttributeChanged() || HasStaminaChanged();
            }

            // 失敗時は強化値以外の主要ステータスは変化しないべき
            // （耐久は減少する可能性がある）
            return true;
        }
        #endregion
    }
}