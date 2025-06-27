using UnityEngine;

/// <summary>
/// キャラクター経験値テーブルのマスターデータ
/// ScriptableObject形式で管理される
/// </summary>
[CreateAssetMenu(fileName = "CharacterExperienceTable_", menuName = "Master Data/Character Experience Table")]
public class CharacterExperienceTableMasterData : ScriptableObject
{
    [Header("経験値データ")]
    [SerializeField] private int characterLevel;
    [SerializeField] private int needExperience;
    [SerializeField] private int totalExperience;

    // プロパティ（読み取り専用）
    public int CharacterLevel => characterLevel;
    public int NeedExperience => needExperience;
    public int TotalExperience => totalExperience;

    /// <summary>
    /// CSVデータから経験値テーブルデータを設定
    /// </summary>
    public void SetDataFromCSV(int level, int needExp, int totalExp)
    {
        characterLevel = level;
        needExperience = needExp;
        totalExperience = totalExp;
    }

    /// <summary>
    /// データの妥当性をチェック
    /// </summary>
    public bool ValidateData()
    {
        if (characterLevel <= 0)
        {
            Debug.LogError($"Invalid character level: {characterLevel}");
            return false;
        }

        if (needExperience < 0)
        {
            Debug.LogError($"Invalid need experience: {needExperience} for level {characterLevel}");
            return false;
        }

        if (totalExperience < 0)
        {
            Debug.LogError($"Invalid total experience: {totalExperience} for level {characterLevel}");
            return false;
        }

        return true;
    }
}

/// <summary>
/// 経験値テーブル管理用のユーティリティクラス
/// </summary>
public static class CharacterExperienceUtility
{
    private static CharacterExperienceTableMasterData[] experienceTable;

    /// <summary>
    /// 経験値テーブルを読み込み
    /// </summary>
    public static void LoadExperienceTable()
    {
        experienceTable = Resources.LoadAll<CharacterExperienceTableMasterData>("GameData/ExperienceTable");
        System.Array.Sort(experienceTable, (a, b) => a.CharacterLevel.CompareTo(b.CharacterLevel));
    }

    /// <summary>
    /// 指定レベルの経験値データを取得
    /// </summary>
    public static CharacterExperienceTableMasterData GetExperienceData(int level)
    {
        if (experienceTable == null || experienceTable.Length == 0)
        {
            LoadExperienceTable();
        }

        if (experienceTable == null || level <= 0 || level > experienceTable.Length)
        {
            return null;
        }

        return experienceTable[level - 1];
    }

    /// <summary>
    /// 経験値から該当レベルを計算
    /// </summary>
    public static int CalculateLevelFromExperience(int currentExp)
    {
        if (experienceTable == null || experienceTable.Length == 0)
        {
            LoadExperienceTable();
        }

        if (experienceTable == null)
        {
            return 1;
        }

        for (int i = experienceTable.Length - 1; i >= 0; i--)
        {
            if (currentExp >= experienceTable[i].TotalExperience)
            {
                return experienceTable[i].CharacterLevel;
            }
        }

        return 1;
    }

    /// <summary>
    /// 指定レベルに必要な累計経験値を取得
    /// </summary>
    public static int GetTotalExperienceForLevel(int level)
    {
        var expData = GetExperienceData(level);
        return expData?.TotalExperience ?? 0;
    }

    /// <summary>
    /// 次のレベルまでに必要な経験値を計算
    /// </summary>
    public static int CalculateExpToNextLevel(int currentExp, int currentLevel)
    {
        var nextLevelData = GetExperienceData(currentLevel + 1);
        if (nextLevelData == null)
        {
            return 0; // 最大レベル
        }

        return nextLevelData.TotalExperience - currentExp;
    }

    /// <summary>
    /// レベルアップ可能かチェック
    /// </summary>
    public static bool CanLevelUp(int currentExp, int currentLevel)
    {
        return CalculateExpToNextLevel(currentExp, currentLevel) <= 0 && currentLevel < GetMaxLevel();
    }

    /// <summary>
    /// 最大レベルを取得
    /// </summary>
    public static int GetMaxLevel()
    {
        if (experienceTable == null || experienceTable.Length == 0)
        {
            LoadExperienceTable();
        }

        return experienceTable?.Length ?? 1;
    }
}