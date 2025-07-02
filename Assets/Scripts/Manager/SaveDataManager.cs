using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// セーブデータの読み込み・保存を管理するクラス
/// </summary>
public class SaveDataManager : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private string saveFileName = "save_data.json";
    [SerializeField] private bool useEncryption = false;
    [SerializeField] private bool autoSaveEnabled = true;
    [SerializeField] private float autoSaveInterval = 60f; // 60秒間隔

    [Header("デバッグ設定")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool createBackup = true;
    [SerializeField] private int maxBackupFiles = 5;

    // イベント
    public static event System.Action<UserSaveData> OnDataLoaded;
    public static event System.Action<UserSaveData> OnDataSaved;
    public static event System.Action<string> OnSaveError;
    public static event System.Action<string> OnLoadError;

    // プロパティ
    public static SaveDataManager Instance { get; private set; }
    public UserSaveData CurrentSaveData { get; private set; }
    public bool IsDataLoaded { get; private set; }
    public string SaveFilePath => Path.Combine(Application.persistentDataPath, "SaveData", saveFileName);
    public string BackupFolderPath => Path.Combine(Application.persistentDataPath, "SaveData", "Backup");

    // 内部変数
    private Coroutine autoSaveCoroutine;
    private bool isDirty = false; // データが変更されたかのフラグ
    private const string ENCRYPTION_KEY = "GameSaveDataKey2024"; // 実際の運用では外部設定推奨

    #region Unity Lifecycle

    private void Awake()
    {
        // シングルトン設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSaveSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 自動ロード
        LoadSaveData();

        // オートセーブ開始
        if (autoSaveEnabled)
        {
            StartAutoSave();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // アプリがフォーカスを失った時に保存
        if (!hasFocus && IsDataLoaded && isDirty)
        {
            SaveSaveData();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // アプリが一時停止した時に保存
        if (pauseStatus && IsDataLoaded && isDirty)
        {
            SaveSaveData();
        }
    }

    #endregion

    #region 初期化

    private void InitializeSaveSystem()
    {
        // セーブディレクトリの作成
        string saveDir = Path.GetDirectoryName(SaveFilePath);
        if (!Directory.Exists(saveDir))
        {
            Directory.CreateDirectory(saveDir);
            DebugLog($"セーブディレクトリを作成しました: {saveDir}");
        }

        // バックアップディレクトリの作成
        if (createBackup && !Directory.Exists(BackupFolderPath))
        {
            Directory.CreateDirectory(BackupFolderPath);
            DebugLog($"バックアップディレクトリを作成しました: {BackupFolderPath}");
        }
    }

    #endregion

    #region 公開メソッド

    // ===== LoadSaveData()メソッドにスキル検証追加 =====
    /// <summary>
    /// セーブデータを読み込み
    /// </summary>
    public bool LoadSaveData()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                string jsonData = File.ReadAllText(SaveFilePath);

                if (useEncryption)
                {
                    jsonData = DecryptData(jsonData);
                }

                CurrentSaveData = JsonUtility.FromJson<UserSaveData>(jsonData);

                if (CurrentSaveData != null)
                {
                    // 最終ログイン日時を更新
                    CurrentSaveData.UpdateLastLoginDate();

                    // スタミナ回復処理
                    CurrentSaveData.RecoverStamina(100, 1); // 最大100、1分間に1回復

                    // スキルデータの検証と修復
                    var skillErrors = ValidateSkillData();
                    if (skillErrors.Count > 0)
                    {
                        DebugLog($"スキルデータに{skillErrors.Count}個の問題を検出し、自動修復を実行しました");
                        foreach (var error in skillErrors.Take(5)) // 最初の5個のエラーのみログ出力
                        {
                            DebugLog($"- {error}");
                        }
                    }

                    // 無効なスキルデータをクリーンアップ
                    int cleanedSkills = CleanupSkillData();
                    if (cleanedSkills > 0)
                    {
                        DebugLog($"スキルデータクリーンアップ: {cleanedSkills}個の無効データを除去");
                    }

                    IsDataLoaded = true;
                    isDirty = true; // ログイン日時更新等により変更フラグを立てる

                    DebugLog($"セーブデータを読み込みました: {CurrentSaveData.playerName}");
                    OnDataLoaded?.Invoke(CurrentSaveData);

                    return true;
                }
            }

            // セーブファイルが存在しない場合は新規作成
            CreateNewSaveData();
            return true;
        }
        catch (Exception e)
        {
            string error = $"セーブデータの読み込みに失敗: {e.Message}";
            DebugLogError(error);
            OnLoadError?.Invoke(error);

            // エラー後は新規データを作成
            CreateNewSaveData();
            return false;
        }
    }


    /// <summary>
    /// セーブデータを保存
    /// </summary>
    public bool SaveSaveData()
    {
        if (CurrentSaveData == null)
        {
            DebugLogError("保存するデータがありません");
            return false;
        }

        try
        {
            // バックアップ作成
            if (createBackup && File.Exists(SaveFilePath))
            {
                CreateBackup();
            }

            string jsonData = JsonUtility.ToJson(CurrentSaveData, true);

            if (useEncryption)
            {
                jsonData = EncryptData(jsonData);
            }

            File.WriteAllText(SaveFilePath, jsonData);

            isDirty = false;
            DebugLog($"セーブデータを保存しました: {CurrentSaveData.playerName}");
            OnDataSaved?.Invoke(CurrentSaveData);

            return true;
        }
        catch (Exception e)
        {
            string error = $"セーブデータの保存に失敗: {e.Message}";
            DebugLogError(error);
            OnSaveError?.Invoke(error);
            return false;
        }
    }

    // ===== CreateNewSaveData()メソッドに追加 =====
    /// <summary>
    /// 新規セーブデータを作成
    /// </summary>
    public void CreateNewSaveData(string playerName = "新規プレイヤー")
    {
        CurrentSaveData = UserDataUtility.CreateNewUserData(playerName);
        IsDataLoaded = true;
        isDirty = true;

        DebugLog($"新規セーブデータを作成しました: {playerName}");
        OnDataLoaded?.Invoke(CurrentSaveData);

        // 即座に保存
        SaveSaveData();
    }

    /// <summary>
    /// データが変更されたことを通知
    /// </summary>
    public void MarkDataDirty()
    {
        isDirty = true;
    }

    /// <summary>
    /// セーブデータをリセット
    /// </summary>
    public void ResetSaveData()
    {
        if (File.Exists(SaveFilePath))
        {
            // 現在のセーブファイルをバックアップに移動
            string backupPath = Path.Combine(BackupFolderPath, $"reset_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.Move(SaveFilePath, backupPath);
        }

        CreateNewSaveData();
        DebugLog("セーブデータをリセットしました");
    }

    /// <summary>
    /// バックアップからデータを復元
    /// </summary>
    public bool RestoreFromBackup(string backupFileName)
    {
        string backupPath = Path.Combine(BackupFolderPath, backupFileName);

        if (!File.Exists(backupPath))
        {
            DebugLogError($"バックアップファイルが見つかりません: {backupPath}");
            return false;
        }

        try
        {
            // 現在のファイルをバックアップ
            if (File.Exists(SaveFilePath))
            {
                string tempBackup = Path.Combine(BackupFolderPath, $"restore_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                File.Copy(SaveFilePath, tempBackup);
            }

            // バックアップファイルを復元
            File.Copy(backupPath, SaveFilePath, true);

            // データを再読み込み
            return LoadSaveData();
        }
        catch (Exception e)
        {
            DebugLogError($"バックアップからの復元に失敗: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// セーブファイルが存在するかチェック
    /// </summary>
    public bool SaveFileExists()
    {
        return File.Exists(SaveFilePath);
    }

    // ===== SetSaveData()メソッドを修正 =====
    /// <summary>
    /// 外部からセーブデータを設定（エディター用）
    /// </summary>
    public void SetSaveData(UserSaveData saveData)
    {
        if (saveData == null)
        {
            DebugLogError("設定しようとしたセーブデータがnullです");
            return;
        }

        CurrentSaveData = saveData;
        IsDataLoaded = true;
        isDirty = true;

        DebugLog($"外部からセーブデータを設定しました: {saveData.playerName}");

        // InventoryManagerのキャッシュを自動更新
        if (InventoryManager.Instance != null)
        {
            // 少し遅延してキャッシュ更新（SetSaveDataの処理完了を待つ）
            StartCoroutine(DelayedCacheRefresh());
        }

        OnDataLoaded?.Invoke(CurrentSaveData);
    }

    // ===== 新規メソッド追加: スキル統計情報 =====
    /// <summary>
    /// スキル関連の統計情報を文字列で取得
    /// </summary>
    public string GetSkillDataSummary()
    {
        if (CurrentSaveData?.skills == null)
            return "スキルデータなし";

        int totalSkillCount = CurrentSaveData.skills.Count;
        int newSkillCount = CurrentSaveData.skills.Count(s => s.isNew);
        int lockedSkillCount = CurrentSaveData.skills.Count(s => s.isLocked);

        // バトルスキル設定状況
        bool hasBattleSkill1 = !string.IsNullOrEmpty(CurrentSaveData.battleSkill1Id);
        bool hasBattleSkill2 = !string.IsNullOrEmpty(CurrentSaveData.battleSkill2Id);

        // 装備中スキル数（装備にスキルが装着されている数）
        int equippedSkillCount = CurrentSaveData.equipments?.Count(eq => !string.IsNullOrEmpty(eq.equippedSkillId)) ?? 0;

        return $@"=== スキル統計 ===
総スキル数: {totalSkillCount}
新規スキル: {newSkillCount}
ロック中スキル: {lockedSkillCount}
装備中スキル: {equippedSkillCount}
バトルスキル1: {(hasBattleSkill1 ? "設定済み" : "未設定")} ({CurrentSaveData.battleSkill1Id})
バトルスキル2: {(hasBattleSkill2 ? "設定済み" : "未設定")} ({CurrentSaveData.battleSkill2Id})";
    }


    // ===== 新規メソッド追加: スキルデータ検証 =====
    /// <summary>
    /// スキルデータの整合性をチェック
    /// </summary>
    public List<string> ValidateSkillData()
    {
        var errors = new List<string>();

        if (CurrentSaveData == null)
        {
            errors.Add("セーブデータが読み込まれていません");
            return errors;
        }

        // スキルデータの基本検証
        if (CurrentSaveData.skills == null)
        {
            errors.Add("スキルリストがnullです");
            CurrentSaveData.skills = new List<UserSkillData>();
            MarkDataDirty();
        }

        // バトルスキルの検証
        ValidateBattleSkills(errors);

        // スキル重複チェック
        var duplicateSkills = CurrentSaveData.skills
            .GroupBy(s => s.userSkillId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateId in duplicateSkills)
        {
            errors.Add($"重複するスキルID: {duplicateId}");
        }

        // 存在しないマスターIDのチェック
        if (MasterDataManager.Instance != null && MasterDataManager.Instance.IsDataLoaded)
        {
            foreach (var skill in CurrentSaveData.skills)
            {
                var masterData = MasterDataManager.Instance.GetSkillData(skill.skillMasterId);
                if (masterData == null)
                {
                    errors.Add($"存在しないスキルマスターID: {skill.skillMasterId} (UserSkillId: {skill.userSkillId})");
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// バトルスキルの整合性をチェック
    /// </summary>
    private void ValidateBattleSkills(List<string> errors)
    {
        // バトルスキル1の検証
        if (!string.IsNullOrEmpty(CurrentSaveData.battleSkill1Id))
        {
            var battleSkill1 = CurrentSaveData.GetSkill(CurrentSaveData.battleSkill1Id);
            if (battleSkill1 == null)
            {
                errors.Add($"バトルスキル1のスキルが見つかりません: {CurrentSaveData.battleSkill1Id}");
                CurrentSaveData.battleSkill1Id = "";
                MarkDataDirty();
            }
        }

        // バトルスキル2の検証
        if (!string.IsNullOrEmpty(CurrentSaveData.battleSkill2Id))
        {
            var battleSkill2 = CurrentSaveData.GetSkill(CurrentSaveData.battleSkill2Id);
            if (battleSkill2 == null)
            {
                errors.Add($"バトルスキル2のスキルが見つかりません: {CurrentSaveData.battleSkill2Id}");
                CurrentSaveData.battleSkill2Id = "";
                MarkDataDirty();
            }
        }

        // 同じスキルが両方に設定されていないかチェック
        if (!string.IsNullOrEmpty(CurrentSaveData.battleSkill1Id) &&
            !string.IsNullOrEmpty(CurrentSaveData.battleSkill2Id) &&
            CurrentSaveData.battleSkill1Id == CurrentSaveData.battleSkill2Id)
        {
            errors.Add("同じスキルがバトルスキル1と2の両方に設定されています");
            CurrentSaveData.battleSkill2Id = "";
            MarkDataDirty();
        }
    }





    // ===== 新規メソッド追加: スキルデータクリーンアップ =====
    /// <summary>
    /// 無効なスキルデータを自動修復
    /// </summary>
    public int CleanupSkillData()
    {
        if (CurrentSaveData?.skills == null)
            return 0;

        int cleanedCount = 0;
        var validSkills = new List<UserSkillData>();

        foreach (var skill in CurrentSaveData.skills)
        {
            bool isValid = true;

            // 基本的な整合性チェック
            if (string.IsNullOrEmpty(skill.userSkillId))
            {
                DebugLog($"無効なスキルを除去: userSkillIdが空");
                isValid = false;
                cleanedCount++;
            }
            else if (skill.skillMasterId <= 0)
            {
                DebugLog($"無効なスキルを除去: 無効なmasterId {skill.skillMasterId}");
                isValid = false;
                cleanedCount++;
            }
            else if (MasterDataManager.Instance?.GetSkillData(skill.skillMasterId) == null)
            {
                DebugLog($"無効なスキルを除去: 存在しないmasterId {skill.skillMasterId}");
                isValid = false;
                cleanedCount++;
            }

            if (isValid)
            {
                validSkills.Add(skill);
            }
        }

        if (cleanedCount > 0)
        {
            CurrentSaveData.skills = validSkills;
            MarkDataDirty();
            DebugLog($"スキルデータクリーンアップ完了: {cleanedCount}個の無効データを除去");
        }

        return cleanedCount;
    }


    // ===== DelayedCacheRefresh()メソッドを修正 =====
    /// <summary>
    /// 遅延キャッシュ更新
    /// </summary>
    private System.Collections.IEnumerator DelayedCacheRefresh()
    {
        yield return new WaitForEndOfFrame();

        if (InventoryManager.Instance != null && InventoryManager.Instance.IsInitialized)
        {
            InventoryManager.Instance.RefreshCache();
            DebugLog("InventoryManagerのキャッシュを自動更新しました");
        }

        // SkillManagerのキャッシュ更新追加
        if (SkillManager.Instance != null && SkillManager.Instance.IsInitialized)
        {
            SkillManager.Instance.RefreshCache();
            DebugLog("SkillManagerのキャッシュを自動更新しました");
        }
    }

    /// <summary>
    /// バックアップファイル一覧を取得
    /// </summary>
    public string[] GetBackupFiles()
    {
        if (!Directory.Exists(BackupFolderPath))
            return new string[0];

        string[] files = Directory.GetFiles(BackupFolderPath, "*.json");
        Array.Sort(files, (x, y) => File.GetCreationTime(y).CompareTo(File.GetCreationTime(x))); // 新しい順
        return files;
    }

    #endregion

    #region オートセーブ

    public void StartAutoSave()
    {
        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
        }

        autoSaveCoroutine = StartCoroutine(AutoSaveCoroutine());
        DebugLog($"オートセーブを開始しました (間隔: {autoSaveInterval}秒)");
    }

    public void StopAutoSave()
    {
        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
            autoSaveCoroutine = null;
            DebugLog("オートセーブを停止しました");
        }
    }

    private IEnumerator AutoSaveCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);

            if (IsDataLoaded && isDirty)
            {
                SaveSaveData();
                DebugLog("オートセーブを実行しました");
            }
        }
    }

    #endregion

    #region バックアップ

    private void CreateBackup()
    {
        try
        {
            // ミリ秒まで含めたタイムスタンプを使用
            string backupFileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json";
            string backupPath = Path.Combine(BackupFolderPath, backupFileName);

            // 念のため重複チェック（通常は発生しないはず）
            if (File.Exists(backupPath))
            {
                // GUIDを追加してユニークにする
                string guid = Guid.NewGuid().ToString("N")[..8]; // 8文字のGUID
                backupFileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{guid}.json";
                backupPath = Path.Combine(BackupFolderPath, backupFileName);
            }

            File.Copy(SaveFilePath, backupPath);
            DebugLog($"バックアップを作成しました: {backupFileName}");

            // 古いバックアップファイルを削除
            CleanupOldBackups();
        }
        catch (Exception e)
        {
            DebugLogError($"バックアップの作成に失敗: {e.Message}");
        }
    }

    private void CleanupOldBackups()
    {
        try
        {
            string[] backupFiles = GetBackupFiles();

            if (backupFiles.Length > maxBackupFiles)
            {
                // 古いファイルを削除
                for (int i = maxBackupFiles; i < backupFiles.Length; i++)
                {
                    File.Delete(backupFiles[i]);
                    DebugLog($"古いバックアップを削除: {Path.GetFileName(backupFiles[i])}");
                }
            }
        }
        catch (Exception e)
        {
            DebugLogError($"バックアップのクリーンアップに失敗: {e.Message}");
        }
    }

    #endregion

    #region 暗号化（簡易版）

    private string EncryptData(string data)
    {
        // 簡易的なXOR暗号化（実際の運用ではより強固な暗号化を推奨）
        byte[] dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
        byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(ENCRYPTION_KEY);

        for (int i = 0; i < dataBytes.Length; i++)
        {
            dataBytes[i] ^= keyBytes[i % keyBytes.Length];
        }

        return Convert.ToBase64String(dataBytes);
    }

    private string DecryptData(string encryptedData)
    {
        // XOR復号化
        byte[] dataBytes = Convert.FromBase64String(encryptedData);
        byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(ENCRYPTION_KEY);

        for (int i = 0; i < dataBytes.Length; i++)
        {
            dataBytes[i] ^= keyBytes[i % keyBytes.Length];
        }

        return System.Text.Encoding.UTF8.GetString(dataBytes);
    }

    #endregion

    #region デバッグ

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SaveDataManager] {message}");
        }
    }

    private void DebugLogError(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogError($"[SaveDataManager] {message}");
        }
    }

    #endregion

    #region エディター用ツール

#if UNITY_EDITOR
    [ContextMenu("セーブデータ情報を表示")]
    private void ShowSaveDataInfo()
    {
        if (CurrentSaveData != null)
        {
            Debug.Log(UserDataUtility.GetUserDataSummary(CurrentSaveData));
        }
        else
        {
            Debug.Log("セーブデータが読み込まれていません");
        }
    }

    [ContextMenu("セーブデータを手動保存")]
    private void ManualSave()
    {
        SaveSaveData();
    }

    [ContextMenu("セーブデータを手動読み込み")]
    private void ManualLoad()
    {
        LoadSaveData();
    }

    [ContextMenu("新規セーブデータ作成")]
    private void CreateNewSave()
    {
        CreateNewSaveData("テストプレイヤー");
    }
#endif

    // ===== エディター用ツール追加 =====
#if UNITY_EDITOR
    [ContextMenu("スキルデータを検証")]
    private void ValidateSkillDataEditor()
    {
        var errors = ValidateSkillData();
        if (errors.Count == 0)
        {
            Debug.Log("スキルデータに問題はありません");
        }
        else
        {
            Debug.LogWarning($"スキルデータに{errors.Count}個の問題があります:\n" + string.Join("\n", errors));
        }
    }

    [ContextMenu("スキルデータをクリーンアップ")]
    private void CleanupSkillDataEditor()
    {
        int cleaned = CleanupSkillData();
        Debug.Log($"スキルデータクリーンアップ完了: {cleaned}個のデータを修復");
    }

    [ContextMenu("スキル統計を表示")]
    private void ShowSkillStatistics()
    {
        string summary = GetSkillDataSummary();
        Debug.Log(summary);
    }

    [ContextMenu("テストスキルを追加")]
    private void AddTestSkillData()
    {
        if (CurrentSaveData?.skills == null)
        {
            Debug.LogWarning("セーブデータが読み込まれていません");
            return;
        }

        // テスト用スキルデータを追加
        if (MasterDataManager.Instance?.GetSkillData(1) != null)
        {
            var testSkill = new UserSkillData(MasterDataManager.Instance.GetSkillData(1));
            CurrentSaveData.AddSkill(testSkill);
            MarkDataDirty();
            Debug.Log($"テストスキルを追加: {testSkill.userSkillId}");
        }
        else
        {
            Debug.LogWarning("スキルマスターデータ(ID:1)が見つかりません");
        }
    }
#endif

    #endregion
}