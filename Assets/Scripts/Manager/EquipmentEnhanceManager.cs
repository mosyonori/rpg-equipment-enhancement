using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 装備強化専用マネージャークラス
/// UI層からの強化リクエストを受け付け、データ層との橋渡しを行う
/// データアクセス統一ルール: UI層 → Manager層 → Data層
/// </summary>
public class EquipmentEnhanceManager : MonoBehaviour
{
    #region Singleton Pattern

    private static EquipmentEnhanceManager _instance;
    public static EquipmentEnhanceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<EquipmentEnhanceManager>();
                if (_instance == null)
                {
                    var go = new GameObject("EquipmentEnhanceManager");
                    _instance = go.AddComponent<EquipmentEnhanceManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// 強化実行完了時のイベント
    /// </summary>
    public event Action<EnhanceResultData> OnEnhanceCompleted;

    /// <summary>
    /// 強化エラー発生時のイベント
    /// </summary>
    public event Action<string> OnEnhanceError;

    #endregion

    #region Properties

    [Header("設定")]
    [SerializeField] private bool enableDebugLog = true;

    /// <summary>
    /// マネージャーが初期化済みかどうか
    /// </summary>
    public bool IsInitialized { get; private set; }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            // 初期化は他のManagerの準備完了後に実行
            StartCoroutine(WaitForDependenciesAndInitialize());
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// 依存関係の準備完了を待機してから初期化
    /// </summary>
    private System.Collections.IEnumerator WaitForDependenciesAndInitialize()
    {
        LogDebug("EquipmentEnhanceManager初期化開始 - 依存関係チェック中...");

        float timeout = 15f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (CheckDependencies())
            {
                LogDebug("依存関係確認完了 - 初期化実行");
                Initialize();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        LogError($"依存関係の初期化がタイムアウトしました（{timeout}秒）");
        LogError("MasterDataManagerとSaveDataManagerがシーンに配置され、正常に初期化されているか確認してください");
    }

    /// <summary>
    /// マネージャーの初期化
    /// </summary>
    private void Initialize()
    {
        if (IsInitialized)
        {
            LogDebug("既に初期化済みです");
            return;
        }

        LogDebug("EquipmentEnhanceManager初期化実行");

        // 依存するマネージャーの最終チェック
        if (!CheckDependencies())
        {
            LogError("初期化時に依存関係チェックに失敗しました");
            return;
        }

        IsInitialized = true;
        LogDebug("EquipmentEnhanceManager初期化完了");
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
            LogDebug("MasterDataManager.Instanceがnullです");
            return false;
        }

        if (!MasterDataManager.Instance.IsDataLoaded)
        {
            LogDebug($"MasterDataManagerのデータ読み込みが未完了です (IsDataLoaded: {MasterDataManager.Instance.IsDataLoaded})");
            return false;
        }

        // SaveDataManagerチェック
        if (SaveDataManager.Instance == null)
        {
            LogDebug("SaveDataManager.Instanceがnullです");
            return false;
        }

        if (!SaveDataManager.Instance.IsDataLoaded)
        {
            LogDebug($"SaveDataManagerのデータ読み込みが未完了です (IsDataLoaded: {SaveDataManager.Instance.IsDataLoaded})");
            return false;
        }

        LogDebug("全ての依存関係が満たされています");
        return true;
    }

    #endregion

    #region Main API Methods

    /// <summary>
    /// 装備強化を実行（修正版：失敗時も耐久値変更を適用）
    /// </summary>
    /// <param name="equipmentId">対象装備のユーザーID</param>
    /// <param name="enhanceItemId">強化アイテムのマスターID</param>
    /// <param name="supportItemId">補助材料のマスターID（0の場合は未使用）</param>
    /// <returns>強化結果データ</returns>
    public EnhanceResultData ExecuteEnhance(string equipmentId, int enhanceItemId, int supportItemId = 0)
    {
        LogDebug($"強化実行開始: Equipment={equipmentId}, EnhanceItem={enhanceItemId}, SupportItem={supportItemId}");

        try
        {
            // 基本的な入力チェック
            if (string.IsNullOrEmpty(equipmentId) || enhanceItemId <= 0)
            {
                var errorMsg = "無効なパラメーター";
                OnEnhanceError?.Invoke(errorMsg);
                return null;
            }

            // 装備データを取得
            var equipment = GetUserEquipmentData(equipmentId);
            if (equipment == null)
            {
                var errorMsg = $"装備が見つかりません: {equipmentId}";
                OnEnhanceError?.Invoke(errorMsg);
                return null;
            }

            // 強化アイテムデータを取得
            var enhanceItem = GetEnhanceItemData(enhanceItemId);
            if (enhanceItem == null)
            {
                var errorMsg = $"強化アイテムが見つかりません: {enhanceItemId}";
                OnEnhanceError?.Invoke(errorMsg);
                return null;
            }

            // 補助材料データを取得（使用する場合）
            SupportItemMasterData supportItem = null;
            if (supportItemId > 0)
            {
                supportItem = GetSupportItemData(supportItemId);
                if (supportItem == null)
                {
                    var errorMsg = $"補助材料が見つかりません: {supportItemId}";
                    OnEnhanceError?.Invoke(errorMsg);
                    return null;
                }
            }

            // 強化可能性チェック
            if (!CanExecuteEnhance(equipmentId, enhanceItemId))
            {
                var errorMsg = "強化を実行できません";
                OnEnhanceError?.Invoke(errorMsg);
                return null;
            }

            // 強化計算と実行
            var result = PerformEnhance(equipment, enhanceItem, supportItem);

            // 修正：成功・失敗に関わらずデータを保存
            if (result != null)
            {
                ApplyEnhanceResult(equipment, result);
                SaveDataManager.Instance.MarkDataDirty();
                SaveDataManager.Instance.SaveSaveData();
            }

            // イベント通知
            OnEnhanceCompleted?.Invoke(result);

            LogDebug($"強化実行完了: 結果={result?.isSuccess}, 耐久値={equipment.currentEnhanceStamina}");
            return result;
        }
        catch (Exception ex)
        {
            var errorMsg = $"強化実行中にエラーが発生しました: {ex.Message}";
            LogError(errorMsg);
            OnEnhanceError?.Invoke(errorMsg);
            return null;
        }
    }

    /// <summary>
    /// 強化プレビューを取得
    /// </summary>
    /// <param name="equipmentId">対象装備のユーザーID</param>
    /// <param name="enhanceItemId">強化アイテムのマスターID</param>
    /// <param name="supportItemId">補助材料のマスターID（0の場合は未使用）</param>
    /// <returns>強化プレビューデータ</returns>
    public EnhancePreviewData GetEnhancePreview(string equipmentId, int enhanceItemId, int supportItemId = 0)
    {
        try
        {
            var preview = new EnhancePreviewData();

            // 装備データを取得
            var equipment = GetUserEquipmentData(equipmentId);
            if (equipment == null)
            {
                preview.canEnhance = false;
                preview.AddWarningMessage("装備が見つかりません");
                return preview;
            }

            // 強化アイテムデータを取得
            var enhanceItem = GetEnhanceItemData(enhanceItemId);
            if (enhanceItem == null)
            {
                preview.canEnhance = false;
                preview.AddWarningMessage("強化アイテムが見つかりません");
                return preview;
            }

            // 補助材料データを取得（使用する場合）
            SupportItemMasterData supportItem = null;
            if (supportItemId > 0)
            {
                supportItem = GetSupportItemData(supportItemId);
            }

            // プレビューデータを構築
            BuildEnhancePreview(preview, equipment, enhanceItem, supportItem);

            return preview;
        }
        catch (Exception ex)
        {
            LogError($"プレビュー取得中にエラー: {ex.Message}");
            var errorPreview = new EnhancePreviewData();
            errorPreview.canEnhance = false;
            errorPreview.AddWarningMessage("プレビューの取得に失敗しました");
            return errorPreview;
        }
    }

    /// <summary>
    /// 利用可能な強化アイテム一覧を取得（実際に所持しているアイテムのみ）
    /// </summary>
    /// <returns>強化アイテムのマスターデータリスト</returns>
    public List<EnhanceItemMasterData> GetAvailableEnhanceItems()
    {
        try
        {
            var availableItems = new List<EnhanceItemMasterData>();
            var saveData = SaveDataManager.Instance.CurrentSaveData;

            if (saveData?.items == null)
            {
                LogWarning("セーブデータまたはアイテムリストがnullです");
                return availableItems;
            }

            // 実際に所持している強化アイテムのマスターデータを取得
            foreach (var userItem in saveData.items)
            {
                // 強化アイテムタイプで、数量が1以上のもののみ
                if (userItem.itemType == ItemType.EnhanceItem && userItem.quantity > 0)
                {
                    var masterData = MasterDataManager.Instance.GetEnhanceItemData(userItem.itemMasterId);
                    if (masterData != null)
                    {
                        availableItems.Add(masterData);
                    }
                }
            }

            LogDebug($"所持している強化アイテム数: {availableItems.Count}");
            return availableItems;
        }
        catch (Exception ex)
        {
            LogError($"強化アイテム一覧取得中にエラー: {ex.Message}");
            return new List<EnhanceItemMasterData>();
        }
    }

    /// <summary>
    /// 利用可能な補助材料一覧を取得（実際に所持しているアイテムのみ）
    /// </summary>
    /// <returns>補助材料のマスターデータリスト</returns>
    public List<SupportItemMasterData> GetAvailableSupportItems()
    {
        try
        {
            var availableItems = new List<SupportItemMasterData>();
            var saveData = SaveDataManager.Instance.CurrentSaveData;

            if (saveData?.items == null)
            {
                LogWarning("セーブデータまたはアイテムリストがnullです");
                return availableItems;
            }

            // 実際に所持している補助材料のマスターデータを取得
            foreach (var userItem in saveData.items)
            {
                // 補助材料タイプで、数量が1以上のもののみ
                if (userItem.itemType == ItemType.SupportItem && userItem.quantity > 0)
                {
                    var masterData = MasterDataManager.Instance.GetSupportItemData(userItem.itemMasterId);
                    if (masterData != null)
                    {
                        availableItems.Add(masterData);
                    }
                }
            }

            LogDebug($"所持している補助材料数: {availableItems.Count}");
            return availableItems;
        }
        catch (Exception ex)
        {
            LogError($"補助材料一覧取得中にエラー: {ex.Message}");
            return new List<SupportItemMasterData>();
        }
    }

    /// <summary>
    /// 強化実行可能性チェック
    /// </summary>
    /// <param name="equipmentId">対象装備のユーザーID</param>
    /// <param name="enhanceItemId">強化アイテムのマスターID</param>
    /// <returns>強化実行可能な場合true</returns>
    public bool CanExecuteEnhance(string equipmentId, int enhanceItemId)
    {
        try
        {
            var equipment = GetUserEquipmentData(equipmentId);
            var enhanceItem = GetEnhanceItemData(enhanceItemId);

            if (equipment == null || enhanceItem == null)
            {
                return false;
            }

            // 基本的な強化可能性チェック
            return EnhanceCalculationUtility.CanEnhance(equipment, enhanceItem);
        }
        catch (Exception ex)
        {
            LogError($"強化可能性チェック中にエラー: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 実際の強化処理を実行（修正版：失敗時の耐久値処理を追加）
    /// </summary>
    /// <param name="equipment">対象装備</param>
    /// <param name="enhanceItem">強化アイテム</param>
    /// <param name="supportItem">補助材料</param>
    /// <returns>強化結果</returns>
    private EnhanceResultData PerformEnhance(UserEquipmentData equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        // 成功率を計算
        float successRate = EnhanceCalculationUtility.CalculateSuccessRate(equipment, enhanceItem, supportItem);

        // 強化実行前の状態を保存
        int previousEnhancedValue = equipment.currentEnhancedValue;
        AttributeType previousAttribute = equipment.currentAttributeType;
        int previousStamina = equipment.currentEnhanceStamina;

        // 強化成功/失敗を判定
        bool isSuccess = EnhanceCalculationUtility.RollEnhanceSuccess(successRate);

        // 耐久値変化を計算（修正版：補助材料の効果を適用、成功失敗に関わらず）
        int baseStaminaChange = EnhanceCalculationUtility.CalculateStaminaChange(previousStamina, enhanceItem) - previousStamina;
        int finalStaminaChange = EnhanceCalculationUtility.CalculateSupportItemStaminaEffect(supportItem, baseStaminaChange);
        int newStamina = Math.Max(0, Math.Min(100, previousStamina + finalStaminaChange));

        EnhanceResultData result;

        if (isSuccess)
        {
            // 成功時の処理：補助材料の倍率効果を適用

            // 強化値の計算（補助材料の倍率効果を適用）
            int baseEnhanceValueIncrease = enhanceItem.addEnhancedValue;
            int modifiedEnhanceValueIncrease = EnhanceCalculationUtility.CalculateSupportItemEnhanceValueEffect(supportItem, baseEnhanceValueIncrease);
            int newEnhancedValue = previousEnhancedValue + modifiedEnhanceValueIncrease;

            AttributeType newAttribute = EnhanceCalculationUtility.CalculateAttributeChange(previousAttribute, enhanceItem.attributeType);

            result = EnhanceResultData.CreateSuccessResult(
                equipment.userEquipmentId,
                enhanceItem.enhanceItemId,
                supportItem?.supportItemId ?? 0,
                previousEnhancedValue,
                newEnhancedValue,
                previousAttribute,
                newAttribute,
                previousStamina,
                newStamina,
                successRate
            );

            // ステータス変化を計算（補助材料の効果を適用）
            var masterData = GetEquipmentMasterData(equipment.equipmentMasterId);
            if (masterData != null)
            {
                // 基本のステータス増加量を取得
                var baseStatusIncrease = EnhanceCalculationUtility.CalculateStatusIncrease(masterData.equipmentType, enhanceItem);

                // 補助材料の効果を適用（素晴らしい薬の倍率効果など）
                var modifiedStatusIncrease = EnhanceCalculationUtility.CalculateSupportItemStatusEffect(supportItem, baseStatusIncrease);

                // 通常のステータス増加（属性攻撃力を除く）
                foreach (var kvp in modifiedStatusIncrease)
                {
                    // 属性攻撃力は別途処理するのでここでは除外
                    if (!kvp.Key.Contains("Offence"))
                    {
                        result.AddStatusChange(kvp.Key, kvp.Value, 0);
                    }
                }

                // 属性攻撃力の変更を計算（属性変更時のリセット処理込み、補助材料効果適用）
                var baseAttributeChanges = EnhanceCalculationUtility.CalculateAttributeAttackChange(equipment, enhanceItem, newAttribute);

                // 属性攻撃力にも補助材料の効果を適用
                int multiplier = EnhanceCalculationUtility.GetStatusMultiplier(supportItem);
                foreach (var kvp in baseAttributeChanges)
                {
                    int modifiedValue = kvp.Value;

                    // 新しい属性攻撃力の場合（0でない場合）は補助材料効果を適用
                    if (kvp.Value > 0 && multiplier > 1)
                    {
                        modifiedValue = kvp.Value * multiplier;
                    }

                    result.AddStatusChange(kvp.Key, modifiedValue, 0);
                }
            }
        }
        else
        {
            // 失敗時の処理（耐久値変化のみ適用、倍率効果なし）
            result = EnhanceResultData.CreateFailureResult(
                equipment.userEquipmentId,
                enhanceItem.enhanceItemId,
                supportItem?.supportItemId ?? 0,
                previousEnhancedValue,
                previousAttribute,
                previousStamina,
                newStamina,  // 失敗時も耐久値は変化する
                successRate
            );
        }

        // 使用アイテム情報を追加
        result.AddUsedItem(ItemUsageData.CreateForEnhanceItem(enhanceItem.enhanceItemId, 1, 1));
        if (supportItem != null)
        {
            result.AddUsedItem(ItemUsageData.CreateForSupportItem(supportItem.supportItemId, 1, 1));
        }

        LogDebug($"強化処理完了: 成功={isSuccess}, 耐久値変化={previousStamina}→{newStamina}");
        return result;
    }

    /// <summary>
    /// 強化結果を装備データに適用（修正版：失敗時も耐久値変化を適用）
    /// </summary>
    /// <param name="equipment">対象装備</param>
    /// <param name="result">強化結果</param>
    private void ApplyEnhanceResult(UserEquipmentData equipment, EnhanceResultData result)
    {
        if (equipment == null || result == null)
        {
            return;
        }

        // 耐久値は成功・失敗に関わらず常に更新
        equipment.currentEnhanceStamina = result.newEnhanceStamina;

        if (result.isSuccess)
        {
            // 成功時のみ強化値と属性を更新
            equipment.currentEnhancedValue = result.newEnhancedValue;
            equipment.currentAttributeType = result.newAttributeType;

            // 強化ステータスを更新
            foreach (var kvp in result.statusChanges)
            {
                ApplyStatusChange(equipment, kvp.Key, kvp.Value);
            }
        }
        // 失敗時は強化値、属性、ステータスは変更しない（耐久値のみ変化）

        LogDebug($"装備データ更新完了: {equipment.userEquipmentId}, 成功: {result.isSuccess}, 耐久値: {equipment.currentEnhanceStamina}");
    }

    /// <summary>
    /// ステータス変化を装備に適用（修正版：属性攻撃力と通常ステータスを区別、属性変更時は完全リセット）
    /// </summary>
    /// <param name="equipment">対象装備</param>
    /// <param name="statusName">ステータス名</param>
    /// <param name="changeAmount">変化量または絶対値</param>
    private void ApplyStatusChange(UserEquipmentData equipment, string statusName, int changeAmount)
    {
        switch (statusName)
        {
            // 通常ステータスは加算
            case "hp":
                equipment.enhancedHp += changeAmount;
                break;
            case "offense":
                equipment.enhancedOffense += changeAmount;
                break;
            case "defense":
                equipment.enhancedDefense += changeAmount;
                break;
            case "speed":
                equipment.enhancedSpeed += changeAmount;
                break;
            case "criticalRate":
                equipment.enhancedCriticalRate += changeAmount;
                break;
            case "criticalDamageRate":
                equipment.enhancedCriticalDamageRate += changeAmount;
                break;

            // 属性攻撃力は絶対値で設定または計算結果を適用
            case "fireOffence":
                var masterData = GetEquipmentMasterData(equipment.equipmentMasterId);
                if (changeAmount == 0)
                {
                    // 0の場合は装備の基本値を相殺して完全に0にする
                    equipment.enhancedFireOffence = masterData != null ? -masterData.fireOffence : 0;
                }
                else
                {
                    // changeAmountが総合値の場合（属性変更時）
                    if (equipment.currentAttributeType != AttributeType.Fire && changeAmount > 0)
                    {
                        // 新しい属性に変更される場合：基本値を相殺して強化分のみを設定
                        equipment.enhancedFireOffence = changeAmount - (masterData?.fireOffence ?? 0);
                    }
                    else
                    {
                        // 同じ属性で強化される場合：changeAmountは既に総合値なので、基本値を引いて強化分を設定
                        equipment.enhancedFireOffence = changeAmount - (masterData?.fireOffence ?? 0);
                    }
                }
                break;
            case "waterOffence":
                masterData = GetEquipmentMasterData(equipment.equipmentMasterId);
                if (changeAmount == 0)
                {
                    equipment.enhancedWaterOffence = masterData != null ? -masterData.waterOffence : 0;
                }
                else
                {
                    if (equipment.currentAttributeType != AttributeType.Water && changeAmount > 0)
                    {
                        equipment.enhancedWaterOffence = changeAmount - (masterData?.waterOffence ?? 0);
                    }
                    else
                    {
                        equipment.enhancedWaterOffence = changeAmount - (masterData?.waterOffence ?? 0);
                    }
                }
                break;
            case "windOffence":
                masterData = GetEquipmentMasterData(equipment.equipmentMasterId);
                if (changeAmount == 0)
                {
                    equipment.enhancedWindOffence = masterData != null ? -masterData.windOffence : 0;
                }
                else
                {
                    if (equipment.currentAttributeType != AttributeType.Wind && changeAmount > 0)
                    {
                        equipment.enhancedWindOffence = changeAmount - (masterData?.windOffence ?? 0);
                    }
                    else
                    {
                        equipment.enhancedWindOffence = changeAmount - (masterData?.windOffence ?? 0);
                    }
                }
                break;
            case "earthOffence":
                masterData = GetEquipmentMasterData(equipment.equipmentMasterId);
                if (changeAmount == 0)
                {
                    equipment.enhancedEarthOffence = masterData != null ? -masterData.earthOffence : 0;
                }
                else
                {
                    if (equipment.currentAttributeType != AttributeType.Earth && changeAmount > 0)
                    {
                        equipment.enhancedEarthOffence = changeAmount - (masterData?.earthOffence ?? 0);
                    }
                    else
                    {
                        equipment.enhancedEarthOffence = changeAmount - (masterData?.earthOffence ?? 0);
                    }
                }
                break;
            default:
                LogWarning($"未知のステータス名: {statusName}");
                break;
        }
    }

    /// <summary>
    /// 強化プレビューデータを構築
    /// </summary>
    /// <param name="preview">構築対象のプレビューデータ</param>
    /// <param name="equipment">対象装備</param>
    /// <param name="enhanceItem">強化アイテム</param>
    /// <param name="supportItem">補助材料</param>
    private void BuildEnhancePreview(EnhancePreviewData preview, UserEquipmentData equipment, EnhanceItemMasterData enhanceItem, SupportItemMasterData supportItem)
    {
        // 現在の状態を設定
        preview.currentEnhancedValue = equipment.currentEnhancedValue;
        preview.currentAttributeType = equipment.currentAttributeType;
        preview.currentEnhanceStamina = equipment.currentEnhanceStamina;

        // 成功率を計算
        preview.baseSuccessRate = enhanceItem.enhanceSuccessRate;
        preview.enhanceValuePenalty = EnhanceCalculationUtility.CalculateEnhanceValuePenalty(equipment.currentEnhancedValue);
        preview.supportItemBonus = EnhanceCalculationUtility.CalculateSupportItemBonus(supportItem);
        preview.finalSuccessRate = EnhanceCalculationUtility.CalculateSuccessRate(equipment, enhanceItem, supportItem);

        // 予想される変化を計算
        preview.expectedEnhancedValue = equipment.currentEnhancedValue + enhanceItem.addEnhancedValue;
        preview.expectedAttributeType = EnhanceCalculationUtility.CalculateAttributeChange(equipment.currentAttributeType, enhanceItem.attributeType);
        preview.expectedEnhanceStamina = EnhanceCalculationUtility.CalculateStaminaChange(equipment.currentEnhanceStamina, enhanceItem);

        // ステータス変化予想
        var masterData = GetEquipmentMasterData(equipment.equipmentMasterId);
        if (masterData != null)
        {
            var statusIncrease = EnhanceCalculationUtility.CalculateStatusIncrease(
                masterData.equipmentType, enhanceItem);
            foreach (var kvp in statusIncrease)
            {
                preview.SetExpectedStatusIncrease(kvp.Key, kvp.Value);
            }
        }

        // 強化可能性とリスクを判定
        preview.canEnhance = EnhanceCalculationUtility.CanEnhance(equipment, enhanceItem);
        preview.hasEnoughItems = true; // 将来的にアイテム所持チェックを実装

        // 特殊効果フラグを設定
        preview.willAttributeChange = preview.HasAttributeChange();
        preview.willStaminaDecrease = preview.expectedEnhanceStamina < preview.currentEnhanceStamina;
        preview.isStaminaRestoration = enhanceItem.addEnhanceStamina > 0;
        preview.isEnhanceReset = enhanceItem.reduceEnhancedValue > 0;

        // 警告メッセージを追加
        if (preview.finalSuccessRate < 50f)
        {
            preview.AddWarningMessage("成功率が50%を下回っています");
        }

        if (preview.willStaminaDecrease)
        {
            preview.AddRiskMessage("耐久値が減少します");
        }
    }

    #endregion

    #region Data Access Methods

    /// <summary>
    /// ユーザー装備データを取得
    /// </summary>
    /// <param name="equipmentId">装備のユーザーID</param>
    /// <returns>ユーザー装備データ</returns>
    private UserEquipmentData GetUserEquipmentData(string equipmentId)
    {
        var saveData = SaveDataManager.Instance.CurrentSaveData;
        return saveData?.equipments?.FirstOrDefault(e => e.userEquipmentId == equipmentId);
    }

    /// <summary>
    /// 強化アイテムマスターデータを取得
    /// </summary>
    /// <param name="enhanceItemId">強化アイテムのマスターID</param>
    /// <returns>強化アイテムマスターデータ</returns>
    private EnhanceItemMasterData GetEnhanceItemData(int enhanceItemId)
    {
        return MasterDataManager.Instance.GetEnhanceItemData(enhanceItemId);
    }

    /// <summary>
    /// 補助材料マスターデータを取得
    /// </summary>
    /// <param name="supportItemId">補助材料のマスターID</param>
    /// <returns>補助材料マスターデータ</returns>
    private SupportItemMasterData GetSupportItemData(int supportItemId)
    {
        return MasterDataManager.Instance.GetSupportItemData(supportItemId);
    }

    /// <summary>
    /// 装備マスターデータを取得
    /// </summary>
    /// <param name="equipmentMasterId">装備のマスターID</param>
    /// <returns>装備マスターデータ</returns>
    private EquipmentMasterData GetEquipmentMasterData(int equipmentMasterId)
    {
        return MasterDataManager.Instance.GetEquipmentData(equipmentMasterId);
    }

    #endregion

    #region Debug Methods

    /// <summary>
    /// デバッグログ出力
    /// </summary>
    /// <param name="message">メッセージ</param>
    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[EquipmentEnhanceManager] {message}");
        }
    }

    /// <summary>
    /// 警告ログ出力
    /// </summary>
    /// <param name="message">メッセージ</param>
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[EquipmentEnhanceManager] {message}");
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    /// <param name="message">メッセージ</param>
    private void LogError(string message)
    {
        Debug.LogError($"[EquipmentEnhanceManager] {message}");
    }

    #endregion
}