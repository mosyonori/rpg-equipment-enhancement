using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 装備強化専用マネージャークラス
/// UI層からの強化リクエストを受け付け、データ層との連携を行う
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

        float timeout = 15f; // タイムアウトを15秒に延長
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
            yield return new WaitForSeconds(0.1f); // 0.1秒間隔でチェック
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
    /// 装備強化を実行
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
                var errorMsg = "無効なパラメータです";
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

            // データを保存
            if (result != null && result.isSuccess)
            {
                ApplyEnhanceResult(equipment, result);
                SaveDataManager.Instance.MarkDataDirty();
                SaveDataManager.Instance.SaveSaveData();
            }

            // イベント通知
            OnEnhanceCompleted?.Invoke(result);

            LogDebug($"強化実行完了: 結果={result?.isSuccess}");
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
    /// 利用可能な強化アイテム一覧を取得
    /// </summary>
    /// <returns>強化アイテムのマスターデータリスト</returns>
    public List<EnhanceItemMasterData> GetAvailableEnhanceItems()
    {
        try
        {
            var allEnhanceItems = MasterDataManager.Instance.GetEnhanceItemDataList();
            if (allEnhanceItems == null)
            {
                LogWarning("強化アイテムデータが取得できませんでした");
                return new List<EnhanceItemMasterData>();
            }

            // 所持しているアイテムのみを返す（将来的に実装）
            // 現在は全てのアイテムを返す
            return allEnhanceItems;
        }
        catch (Exception ex)
        {
            LogError($"強化アイテム一覧取得中にエラー: {ex.Message}");
            return new List<EnhanceItemMasterData>();
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
    /// 実際の強化処理を実行
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

        // 耐久値変化を計算（成功・失敗に関わらず適用）
        int newStamina = EnhanceCalculationUtility.CalculateStaminaChange(previousStamina, enhanceItem);

        EnhanceResultData result;

        if (isSuccess)
        {
            // 成功時の処理
            int newEnhancedValue = previousEnhancedValue + enhanceItem.addEnhancedValue;
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

            // ステータス変化を計算
            var masterData = GetEquipmentMasterData(equipment.equipmentMasterId);
            if (masterData != null)
            {
                var statusIncrease = EnhanceCalculationUtility.CalculateStatusIncrease(masterData.equipmentType, enhanceItem);
                foreach (var kvp in statusIncrease)
                {
                    result.AddStatusChange(kvp.Key, kvp.Value, 0); // 総量は後で計算
                }
            }
        }
        else
        {
            // 失敗時の処理
            result = EnhanceResultData.CreateFailureResult(
                equipment.userEquipmentId,
                enhanceItem.enhanceItemId,
                supportItem?.supportItemId ?? 0,
                previousEnhancedValue,
                previousAttribute,
                previousStamina,
                newStamina,
                successRate
            );
        }

        // 使用アイテム情報を追加
        result.AddUsedItem(ItemUsageData.CreateForEnhanceItem(enhanceItem.enhanceItemId, 1, 1));
        if (supportItem != null)
        {
            result.AddUsedItem(ItemUsageData.CreateForSupportItem(supportItem.supportItemId, 1, 1));
        }

        return result;
    }

    /// <summary>
    /// 強化結果を装備データに適用
    /// </summary>
    /// <param name="equipment">対象装備</param>
    /// <param name="result">強化結果</param>
    private void ApplyEnhanceResult(UserEquipmentData equipment, EnhanceResultData result)
    {
        if (equipment == null || result == null)
        {
            return;
        }

        // 強化値を更新
        equipment.currentEnhancedValue = result.newEnhancedValue;

        // 属性を更新
        equipment.currentAttributeType = result.newAttributeType;

        // 耐久値を更新
        equipment.currentEnhanceStamina = result.newEnhanceStamina;

        // 強化ステータスを更新（成功時のみ）
        if (result.isSuccess)
        {
            foreach (var kvp in result.statusChanges)
            {
                ApplyStatusChange(equipment, kvp.Key, kvp.Value);
            }
        }

        LogDebug($"装備データ更新完了: {equipment.userEquipmentId}");
    }

    /// <summary>
    /// ステータス変化を装備に適用
    /// </summary>
    /// <param name="equipment">対象装備</param>
    /// <param name="statusName">ステータス名</param>
    /// <param name="changeAmount">変化量</param>
    private void ApplyStatusChange(UserEquipmentData equipment, string statusName, int changeAmount)
    {
        switch (statusName)
        {
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
            case "fireOffence":
                equipment.enhancedFireOffence += changeAmount;
                break;
            case "waterOffence":
                equipment.enhancedWaterOffence += changeAmount;
                break;
            case "windOffence":
                equipment.enhancedWindOffence += changeAmount;
                break;
            case "earthOffence":
                equipment.enhancedEarthOffence += changeAmount;
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

        // ステータス変化予測
        var masterData = GetEquipmentMasterData(equipment.equipmentMasterId);
        if (masterData != null)
        {
            var statusIncrease = EnhanceCalculationUtility.CalculateStatusIncrease(masterData.equipmentType, enhanceItem);
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