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
    /// 装備強化を実行（修正版：失敗時も耐久値変更を適用、アイテム消費処理を追加）
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

            // アイテム所持数チェック
            if (!CheckItemAvailability(enhanceItemId, supportItemId))
            {
                var errorMsg = "必要なアイテムが不足しています";
                OnEnhanceError?.Invoke(errorMsg);
                return null;
            }

            // 強化計算と実行
            var result = PerformEnhance(equipment, enhanceItem, supportItem);

            // 修正：成功・失敗に関わらずデータを保存
            if (result != null)
            {
                // 1. 装備データに強化結果を適用
                ApplyEnhanceResult(equipment, result);

                // 2. 使用したアイテムを消費（重要：この処理が抜けていた）
                ConsumeUsedItems(result);

                // 3. データ保存
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
    /// アイテム所持数チェック（新規追加）
    /// </summary>
    /// <param name="enhanceItemId">強化アイテムID</param>
    /// <param name="supportItemId">補助材料ID（0の場合は未使用）</param>
    /// <returns>必要なアイテムを所持している場合true</returns>
    private bool CheckItemAvailability(int enhanceItemId, int supportItemId)
    {
        var saveData = SaveDataManager.Instance.CurrentSaveData;
        if (saveData?.items == null)
        {
            LogError("セーブデータまたはアイテムリストがnullです");
            return false;
        }

        // 強化アイテムの所持数チェック
        var enhanceItemData = saveData.items.FirstOrDefault(item =>
            item.itemType == ItemType.EnhanceItem && item.itemMasterId == enhanceItemId);

        if (enhanceItemData == null || enhanceItemData.quantity < 1)
        {
            LogWarning($"強化アイテムが不足しています: ID={enhanceItemId}");
            return false;
        }

        // 補助材料の所持数チェック（使用する場合のみ）
        if (supportItemId > 0)
        {
            var supportItemData = saveData.items.FirstOrDefault(item =>
                item.itemType == ItemType.SupportItem && item.itemMasterId == supportItemId);

            if (supportItemData == null || supportItemData.quantity < 1)
            {
                LogWarning($"補助材料が不足しています: ID={supportItemId}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 使用したアイテムを消費（修正版：ItemUsageDataのファクトリーメソッド使用）
    /// </summary>
    /// <param name="result">強化結果データ</param>
    private void ConsumeUsedItems(EnhanceResultData result)
    {
        if (result?.usedItems == null || result.usedItems.Count == 0)
        {
            LogWarning("使用アイテム情報がありません");
            return;
        }

        var saveData = SaveDataManager.Instance.CurrentSaveData;
        if (saveData?.items == null)
        {
            LogError("セーブデータまたはアイテムリストがnullです");
            return;
        }

        foreach (var usedItem in result.usedItems)
        {
            // アイテムタイプに応じて対象を検索
            var targetItem = saveData.items.FirstOrDefault(item =>
                item.itemType == usedItem.itemType && item.itemMasterId == usedItem.itemId);

            if (targetItem != null)
            {
                // アイテム数量を減算
                int previousQuantity = targetItem.quantity;
                bool success = targetItem.UseItem(usedItem.usedQuantity);

                if (success)
                {
                    LogDebug($"アイテム消費成功: {GetItemTypeName(usedItem.itemType)} ID={usedItem.itemId}, " +
                            $"数量: {previousQuantity} → {targetItem.quantity} (消費: {usedItem.usedQuantity})");

                    // 所持数が0になった場合、リストから削除
                    if (targetItem.IsEmpty())
                    {
                        saveData.items.Remove(targetItem);
                        LogDebug($"{GetItemTypeName(usedItem.itemType)} ID={usedItem.itemId} の所持数が0になったため、リストから削除しました");
                    }
                }
                else
                {
                    LogError($"アイテム消費に失敗: {GetItemTypeName(usedItem.itemType)} ID={usedItem.itemId} " +
                            $"(要求: {usedItem.usedQuantity}, 所持: {targetItem.quantity})");
                }
            }
            else
            {
                LogError($"消費対象のアイテムが見つかりません: {GetItemTypeName(usedItem.itemType)} ID={usedItem.itemId}");
            }
        }
    }

    /// <summary>
    /// アイテムタイプ名を取得（デバッグ用）
    /// </summary>
    /// <param name="itemType">アイテムタイプ</param>
    /// <returns>アイテムタイプ名</returns>
    private string GetItemTypeName(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.EnhanceItem => "強化アイテム",
            ItemType.SupportItem => "補助材料",
            _ => "不明なアイテム"
        };
    }

    /// <summary>
    /// 実際の強化処理を実行（修正版v2：リザルト表示と装備更新を分離）
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

        // 耐久値変化を計算
        int baseStaminaChange = EnhanceCalculationUtility.CalculateStaminaChange(previousStamina, enhanceItem) - previousStamina;
        int finalStaminaChange = EnhanceCalculationUtility.CalculateSupportItemStaminaEffect(supportItem, baseStaminaChange);
        int newStamina = Math.Max(0, Math.Min(100, previousStamina + finalStaminaChange));

        EnhanceResultData result;

        if (isSuccess)
        {
            // 成功時の処理
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

            // リザルト表示用のステータス変化を計算（表示用：増加分のみ）
            CalculateStatusChangesForDisplay(result, equipment, enhanceItem, newAttribute, supportItem);
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
        result.AddUsedItem(ItemUsageData.CreateForEnhanceItem(enhanceItem.enhanceItemId, 1));
        if (supportItem != null)
        {
            result.AddUsedItem(ItemUsageData.CreateForSupportItem(supportItem.supportItemId, 1));
        }

        LogDebug($"強化処理完了: 成功={isSuccess}, 耐久値変化={previousStamina}→{newStamina}");
        return result;
    }

    /// <summary>
    /// 【新規追加】リザルト表示用のステータス変化を計算（表示専用：増加分のみ）
    /// </summary>
    /// <param name="result">結果データ</param>
    /// <param name="equipment">対象装備</param>
    /// <param name="enhanceItem">強化アイテム</param>
    /// <param name="newAttribute">変更後の属性</param>
    /// <param name="supportItem">補助材料</param>
    private void CalculateStatusChangesForDisplay(EnhanceResultData result, UserEquipmentData equipment,
        EnhanceItemMasterData enhanceItem, AttributeType newAttribute, SupportItemMasterData supportItem)
    {
        var masterData = GetEquipmentMasterData(equipment.equipmentMasterId);
        if (masterData == null) return;

        // 基本のステータス増加量を取得
        var baseStatusIncrease = EnhanceCalculationUtility.CalculateStatusIncrease(masterData.equipmentType, enhanceItem);

        // 補助材料の効果を適用
        var modifiedStatusIncrease = EnhanceCalculationUtility.CalculateSupportItemStatusEffect(supportItem, baseStatusIncrease);

        // 通常のステータス増加（属性攻撃力を除く）- 表示用なので増加分のみ
        foreach (var kvp in modifiedStatusIncrease)
        {
            if (!kvp.Key.Contains("Offence"))
            {
                result.AddStatusChange(kvp.Key, kvp.Value, 0);
            }
        }

        // 属性攻撃力の表示用変化を計算
        CalculateAttributeAttackDisplayChanges(result, equipment, enhanceItem, newAttribute, supportItem);
    }

    /// <summary>
    /// 【新規追加】属性攻撃力の表示用変化を計算（リザルト表示専用）
    /// </summary>
    /// <param name="result">結果データ</param>
    /// <param name="equipment">対象装備</param>
    /// <param name="enhanceItem">強化アイテム</param>
    /// <param name="newAttribute">変更後の属性</param>
    /// <param name="supportItem">補助材料</param>
    private void CalculateAttributeAttackDisplayChanges(EnhanceResultData result, UserEquipmentData equipment,
        EnhanceItemMasterData enhanceItem, AttributeType newAttribute, SupportItemMasterData supportItem)
    {
        var masterData = GetEquipmentMasterData(equipment.equipmentMasterId);
        if (masterData == null) return;

        // 強化アイテムに属性が設定されている場合
        if (newAttribute != AttributeType.None && enhanceItem.attributeType != AttributeType.None)
        {
            bool isAttributeChanging = equipment.currentAttributeType != newAttribute;
            var statusIncrease = EnhanceCalculationUtility.CalculateStatusIncrease(masterData.equipmentType, enhanceItem);
            int multiplier = EnhanceCalculationUtility.GetStatusMultiplier(supportItem);

            if (isAttributeChanging)
            {
                // 属性変更時：リザルト表示では新しい属性攻撃力の増加分のみを表示
                switch (newAttribute)
                {
                    case AttributeType.Fire:
                        if (statusIncrease.ContainsKey("fireOffence"))
                        {
                            int increase = statusIncrease["fireOffence"] * multiplier;
                            result.AddStatusChange("fireOffence", increase, 0);
                        }
                        break;
                    case AttributeType.Water:
                        if (statusIncrease.ContainsKey("waterOffence"))
                        {
                            int increase = statusIncrease["waterOffence"] * multiplier;
                            result.AddStatusChange("waterOffence", increase, 0);
                        }
                        break;
                    case AttributeType.Wind:
                        if (statusIncrease.ContainsKey("windOffence"))
                        {
                            int increase = statusIncrease["windOffence"] * multiplier;
                            result.AddStatusChange("windOffence", increase, 0);
                        }
                        break;
                    case AttributeType.Earth:
                        if (statusIncrease.ContainsKey("earthOffence"))
                        {
                            int increase = statusIncrease["earthOffence"] * multiplier;
                            result.AddStatusChange("earthOffence", increase, 0);
                        }
                        break;
                }
            }
            else
            {
                // 同じ属性での強化：該当属性攻撃力の増加分を表示
                switch (newAttribute)
                {
                    case AttributeType.Fire:
                        if (statusIncrease.ContainsKey("fireOffence"))
                        {
                            int increase = statusIncrease["fireOffence"] * multiplier;
                            result.AddStatusChange("fireOffence", increase, 0);
                        }
                        break;
                    case AttributeType.Water:
                        if (statusIncrease.ContainsKey("waterOffence"))
                        {
                            int increase = statusIncrease["waterOffence"] * multiplier;
                            result.AddStatusChange("waterOffence", increase, 0);
                        }
                        break;
                    case AttributeType.Wind:
                        if (statusIncrease.ContainsKey("windOffence"))
                        {
                            int increase = statusIncrease["windOffence"] * multiplier;
                            result.AddStatusChange("windOffence", increase, 0);
                        }
                        break;
                    case AttributeType.Earth:
                        if (statusIncrease.ContainsKey("earthOffence"))
                        {
                            int increase = statusIncrease["earthOffence"] * multiplier;
                            result.AddStatusChange("earthOffence", increase, 0);
                        }
                        break;
                }
            }
        }
    }

    /// 強化結果を装備データに適用（修正版：装備の実際の更新は従来通り）
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

            // 装備の実際のステータス更新（従来通りの処理で絶対値ベース）
            ApplyActualStatusChangesToEquipment(equipment, result);
        }
        // 失敗時は強化値、属性、ステータスは変更しない（耐久値のみ変化）

        LogDebug($"装備データ更新完了: {equipment.userEquipmentId}, 成功: {result.isSuccess}, 耐久値: {equipment.currentEnhanceStamina}");
    }

    /// <summary>
    /// 【新規追加】装備の実際のステータス更新（従来通りの絶対値ベース処理）
    /// </summary>
    /// <param name="equipment">対象装備</param>
    /// <param name="result">強化結果</param>
    private void ApplyActualStatusChangesToEquipment(UserEquipmentData equipment, EnhanceResultData result)
    {
        var masterData = GetEquipmentMasterData(equipment.equipmentMasterId);
        if (masterData == null) return;

        // 通常ステータスの更新（増加分を加算）
        foreach (var kvp in result.statusChanges)
        {
            if (!kvp.Key.Contains("Offence"))
            {
                ApplyStatusChange(equipment, kvp.Key, kvp.Value);
            }
        }

        // 属性攻撃力の更新（従来通りの絶対値ベース処理）
        ApplyAttributeAttackChanges(equipment, result, masterData);
    }

    /// <summary>
    /// 【新規追加】属性攻撃力の実際の更新処理（絶対値ベース）
    /// </summary>
    /// <param name="equipment">対象装備</param>
    /// <param name="result">強化結果</param>
    /// <param name="masterData">装備マスターデータ</param>
    private void ApplyAttributeAttackChanges(UserEquipmentData equipment, EnhanceResultData result, EquipmentMasterData masterData)
    {
        // 属性が変更された場合の処理
        if (result.previousAttributeType != result.newAttributeType)
        {
            // 全ての属性攻撃力をリセット
            equipment.enhancedFireOffence = -masterData.fireOffence;
            equipment.enhancedWaterOffence = -masterData.waterOffence;
            equipment.enhancedWindOffence = -masterData.windOffence;
            equipment.enhancedEarthOffence = -masterData.earthOffence;

            // 新しい属性の攻撃力を設定
            switch (result.newAttributeType)
            {
                case AttributeType.Fire:
                    if (result.statusChanges.ContainsKey("fireOffence"))
                    {
                        int totalNewValue = result.statusChanges["fireOffence"];
                        equipment.enhancedFireOffence = totalNewValue - masterData.fireOffence;
                    }
                    break;
                case AttributeType.Water:
                    if (result.statusChanges.ContainsKey("waterOffence"))
                    {
                        int totalNewValue = result.statusChanges["waterOffence"];
                        equipment.enhancedWaterOffence = totalNewValue - masterData.waterOffence;
                    }
                    break;
                case AttributeType.Wind:
                    if (result.statusChanges.ContainsKey("windOffence"))
                    {
                        int totalNewValue = result.statusChanges["windOffence"];
                        equipment.enhancedWindOffence = totalNewValue - masterData.windOffence;
                    }
                    break;
                case AttributeType.Earth:
                    if (result.statusChanges.ContainsKey("earthOffence"))
                    {
                        int totalNewValue = result.statusChanges["earthOffence"];
                        equipment.enhancedEarthOffence = totalNewValue - masterData.earthOffence;
                    }
                    break;
            }
        }
        else
        {
            // 同じ属性での強化：該当属性攻撃力に増加分を加算
            switch (result.newAttributeType)
            {
                case AttributeType.Fire:
                    if (result.statusChanges.ContainsKey("fireOffence"))
                    {
                        equipment.enhancedFireOffence += result.statusChanges["fireOffence"];
                    }
                    break;
                case AttributeType.Water:
                    if (result.statusChanges.ContainsKey("waterOffence"))
                    {
                        equipment.enhancedWaterOffence += result.statusChanges["waterOffence"];
                    }
                    break;
                case AttributeType.Wind:
                    if (result.statusChanges.ContainsKey("windOffence"))
                    {
                        equipment.enhancedWindOffence += result.statusChanges["windOffence"];
                    }
                    break;
                case AttributeType.Earth:
                    if (result.statusChanges.ContainsKey("earthOffence"))
                    {
                        equipment.enhancedEarthOffence += result.statusChanges["earthOffence"];
                    }
                    break;
            }
        }
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
        preview.hasEnoughItems = CheckItemAvailability(enhanceItem.enhanceItemId, supportItem?.supportItemId ?? 0);

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

        if (!preview.hasEnoughItems)
        {
            preview.AddWarningMessage("必要なアイテムが不足しています");
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