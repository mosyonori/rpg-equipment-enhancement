using System.Collections;
using UnityEngine;

/// <summary>
/// Service層統合テスト管理クラス
/// DataManager初期化 → Service層テスト の順序で実行
/// </summary>
public class ServiceTestManager : MonoBehaviour
{
    [Header("テスト実行設定")]
    [SerializeField] private bool autoRunOnStart = true;
    [SerializeField] private float initWaitTime = 1.0f; // DataManager初期化待ち時間

    [Header("テスト対象")]
    [SerializeField] private EquipmentEnhanceIntegrationTest enhanceIntegrationTest;

    private void Start()
    {
        if (autoRunOnStart)
        {
            StartCoroutine(ExecuteTestSequence());
        }
    }

    /// <summary>
    /// テストシーケンス実行
    /// 1. DataManager初期化
    /// 2. Service層テスト実行
    /// </summary>
    private IEnumerator ExecuteTestSequence()
    {
        Debug.Log("=== Service層統合テスト シーケンス開始 ===");

        // Step 1: DataManager初期化
        yield return StartCoroutine(InitializeDataManager());

        // Step 2: 少し待機（確実な初期化のため）
        yield return new WaitForSeconds(initWaitTime);

        // Step 3: Service層テスト実行
        if (enhanceIntegrationTest != null)
        {
            Debug.Log("=== Service層テスト開始 ===");
            enhanceIntegrationTest.StartIntegrationTest();
        }
        else
        {
            Debug.LogError("EquipmentEnhanceIntegrationTest が設定されていません");
        }

        Debug.Log("=== Service層統合テスト シーケンス完了 ===");
    }

    /// <summary>
    /// DataManager初期化
    /// </summary>
    private IEnumerator InitializeDataManager()
    {
        Debug.Log("--- DataManager初期化開始 ---");

        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager.Instance がnullです。DataManagerをシーンに配置してください。");
            yield break;
        }

        // マスターデータ初期化
        yield return StartCoroutine(DataManager.Instance.InitializeMasterDataAsync());

        if (DataManager.Instance.IsInitialized)
        {
            Debug.Log("✅ マスターデータ初期化成功");
        }
        else
        {
            Debug.LogWarning("⚠️ マスターデータ初期化失敗");
        }

        // ユーザーデータ初期化
        yield return StartCoroutine(DataManager.Instance.LoadUserDataAsync());
        Debug.Log("✅ ユーザーデータ初期化完了");

        // データ検証
        DataManager.Instance.ValidateAllData();

        Debug.Log("--- DataManager初期化完了 ---");
    }

    /// <summary>
    /// 手動テスト実行（Inspector用）
    /// </summary>
    [ContextMenu("手動でテストシーケンス実行")]
    public void ManualTestExecution()
    {
        StartCoroutine(ExecuteTestSequence());
    }

    /// <summary>
    /// DataManagerのみ初期化（Inspector用）
    /// </summary>
    [ContextMenu("DataManagerのみ初期化")]
    public void InitializeDataManagerOnly()
    {
        StartCoroutine(InitializeDataManager());
    }

    /// <summary>
    /// Service層テストのみ実行（Inspector用）
    /// </summary>
    [ContextMenu("Service層テストのみ実行")]
    public void RunServiceTestOnly()
    {
        if (enhanceIntegrationTest != null)
        {
            enhanceIntegrationTest.StartIntegrationTest();
        }
        else
        {
            Debug.LogError("EquipmentEnhanceIntegrationTest が設定されていません");
        }
    }
}