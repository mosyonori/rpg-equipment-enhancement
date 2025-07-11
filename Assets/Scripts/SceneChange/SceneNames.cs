/// <summary>
/// シーン名の定数管理クラス（修正版）
/// Build Settingsのシーン名と一致させること
/// </summary>
public static class SceneNames
{
    #region Scene Name Constants

    /// <summary>
    /// タイトル画面
    /// </summary>
    public const string TITLE = "TitleScene";

    /// <summary>
    /// ホーム画面（メイン画面）
    /// </summary>
    public const string HOME = "HomeScene";

    /// <summary>
    /// 装備編集画面
    /// </summary>
    public const string EQUIPMENT_EDIT = "InventoryScene";

    /// <summary>
    /// 装備強化画面
    /// </summary>
    public const string EQUIPMENT_ENHANCE = "EquipmentScene";

    /// <summary>
    /// 戦闘画面（修正：実際のシーン名に合わせる）
    /// </summary>
    public const string QUEST_BATTLE = "BattleScene";

    /// <summary>
    /// ガチャ画面（未実装）
    /// </summary>
    public const string GACHA = "GachaScene";

    #endregion

    #region Scene Validation

    /// <summary>
    /// 全シーン名のリスト
    /// </summary>
    public static readonly string[] ALL_SCENES = {
        TITLE,
        HOME,
        EQUIPMENT_EDIT,
        EQUIPMENT_ENHANCE,
        QUEST_BATTLE,
        GACHA
    };

    /// <summary>
    /// 実装済みシーン名のリスト（修正：戦闘シーンを実装済みに変更）
    /// </summary>
    public static readonly string[] IMPLEMENTED_SCENES = {
        TITLE,
        HOME,
        EQUIPMENT_EDIT,
        EQUIPMENT_ENHANCE,
        QUEST_BATTLE  // ← 実装済みに追加
    };

    /// <summary>
    /// 未実装シーン名のリスト（修正：戦闘シーンを除外）
    /// </summary>
    public static readonly string[] NOT_IMPLEMENTED_SCENES = {
        GACHA  // ← 戦闘シーンを除外
    };

    /// <summary>
    /// シーンが実装済みかチェック
    /// </summary>
    /// <param name="sceneName">チェック対象のシーン名</param>
    /// <returns>実装済みの場合true</returns>
    public static bool IsSceneImplemented(string sceneName)
    {
        return System.Array.Exists(IMPLEMENTED_SCENES, scene => scene == sceneName);
    }

    /// <summary>
    /// 有効なシーン名かチェック
    /// </summary>
    /// <param name="sceneName">チェック対象のシーン名</param>
    /// <returns>有効なシーン名の場合true</returns>
    public static bool IsValidSceneName(string sceneName)
    {
        return System.Array.Exists(ALL_SCENES, scene => scene == sceneName);
    }

    #endregion

    #region Scene Display Names

    /// <summary>
    /// シーン名に対応する表示名を取得
    /// </summary>
    /// <param name="sceneName">シーン名</param>
    /// <returns>表示名</returns>
    public static string GetDisplayName(string sceneName)
    {
        return sceneName switch
        {
            TITLE => "タイトル",
            HOME => "ホーム",
            EQUIPMENT_EDIT => "装備編集",
            EQUIPMENT_ENHANCE => "装備強化",
            QUEST_BATTLE => "戦闘", // ← 表示名を簡潔に変更
            GACHA => "ガチャ",
            _ => "不明なシーン"
        };
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// シーン名からファイル名のみを取得（デバッグ用）
    /// </summary>
    /// <param name="sceneName">フルシーン名</param>
    /// <returns>ファイル名のみ</returns>
    public static string GetSceneFileName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return "";

        int lastSlashIndex = sceneName.LastIndexOf('/');
        return lastSlashIndex >= 0 ? sceneName.Substring(lastSlashIndex + 1) : sceneName;
    }

    /// <summary>
    /// Unity SceneManager.GetActiveScene().name で取得される実際のファイル名を取得
    /// </summary>
    /// <param name="sceneConstant">SceneNames定数</param>
    /// <returns>実際のシーンファイル名</returns>
    public static string GetActualSceneName(string sceneConstant)
    {
        // 修正：シーン名をそのまま返す（パス情報を含まない形式に変更したため）
        return sceneConstant;
    }

    /// <summary>
    /// 実際のシーンファイル名からSceneNames定数を取得
    /// </summary>
    /// <param name="actualSceneName">Unity SceneManagerが返すシーン名</param>
    /// <returns>対応するSceneNames定数</returns>
    public static string GetSceneConstant(string actualSceneName)
    {
        return actualSceneName switch
        {
            "TitleScene" => TITLE,
            "HomeScene" => HOME,
            "InventoryScene" => EQUIPMENT_EDIT,
            "EquipmentScene" => EQUIPMENT_ENHANCE,
            "BattleScene" => QUEST_BATTLE,  // ← 修正：BattleSceneに対応
            "GachaScene" => GACHA,
            _ => actualSceneName // 不明な場合はそのまま返す
        };
    }

    /// <summary>
    /// Build Settingsでの参照用シーン名リストを取得
    /// </summary>
    /// <returns>Build Settings用シーン名配列</returns>
    public static string[] GetBuildSettingsSceneNames()
    {
        return new string[]
        {
            TITLE,           // TitleScene
            HOME,            // HomeScene  
            EQUIPMENT_EDIT,  // InventoryScene
            EQUIPMENT_ENHANCE, // EquipmentScene
            QUEST_BATTLE,    // BattleScene（修正：実装済み）
            GACHA           // GachaScene (未実装)
        };
    }

    #endregion
}