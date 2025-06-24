using System.Collections;
using UnityEngine;

/// <summary>
/// DataManager テスト用初期化スクリプト
/// Service層テスト実行前にDataManagerを初期化
/// </summary>
public class DataManagerTestInit : MonoBehaviour
{
    [Header("自動初期化設定")]
    [SerializeField] private bool autoInitOnStart = true;
    [SerializeField] private float delayBeforeInit = 0.5f; // Service テスト開始前の待機時間

    private void Start()
    {
        if (autoInitOnStart)
        {
            StartCoroutine(InitializeDataManagerAsync());
        }
    }

    /// <summary>
    /// DataManager初期化実行
    /// </summary>
    public IEnumerator InitializeDataManagerAsync()
    {
        Debug.Log("=== DataManager テスト用初期化開始 ===");

        // DataManagerの存在確認
        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager.Instance がnullです。DataManagerがシーンに配置されているか確認してください。");
            yield break;
        }

        // 少し待機（DataManagerのAwake完了待ち）
        yield return new WaitForSeconds(0.1f);

        // マスターデータ初期化
        Debug.Log("マスターデータ初期化中...");
        yield return StartCoroutine(DataManager.Instance.InitializeMasterDataAsync());

        if (DataManager.Instance.IsInitialized)
        {
            Debug.Log("✅ マスターデータ初期化成功");
        }
        else
        {
            Debug.LogWarning("⚠️ マスターデータ初期化失敗 - テストデータで継続");
        }

        // ユーザーデータ初期化
        Debug.Log("ユーザーデータ初期化中...");
        yield return StartCoroutine(DataManager.Instance.LoadUserDataAsync());
        Debug.Log("✅ ユーザーデータ初期化完了");

        // データ検証
        DataManager.Instance.ValidateAllData();

        Debug.Log("=== DataManager 初期化完了 ===");

        // Service層テストを少し遅延して開始（確実な初期化のため）
        yield return new WaitForSeconds(delayBeforeInit);
    }

    /// <summary>
    /// 手動初期化（Inspector用）
    /// </summary>
    [ContextMenu("手動でDataManager初期化")]
    public void ManualInitialize()
    {
        StartCoroutine(InitializeDataManagerAsync());
    }

    /// <summary>
    /// データ状態確認（Inspector用）
    /// </summary>
    [ContextMenu("データ状態確認")]
    public void CheckDataStatus()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("DataManager.Instance がnullです");
            return;
        }

        Debug.Log($"DataManager初期化状態: {DataManager.Instance.IsInitialized}");
        Debug.Log($"マスターデータ有効性: {DataManager.Instance.HasValidMasterData()}");

        // データ詳細確認
        DataManager.Instance.ValidateAllData();
    }
}