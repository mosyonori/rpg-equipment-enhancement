using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームのシーン遷移を管理するクラス
/// </summary>
public class SceneManager : MonoBehaviour
{
    #region Singleton
    public static SceneManager Instance { get; private set; }

    private void Awake()
    {
        // シングルトンの設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Scene Names
    // シーン名の定数定義
    public const string TITLE_SCENE = "Scenes/TitleScene";
    public const string HOME_SCENE = "Scenes/HomeScene";
    public const string EQUIPMENT_EDIT_SCENE = "Scenes/EquipmentEditScene";
    public const string EQUIPMENT_ENHANCE_SCENE = "Scenes/EquipmentEnhanceScene";
    public const string QUEST_BATTLE_SCENE = "Scenes/QuestBattleScene";
    #endregion

    #region Properties
    /// <summary>
    /// 現在ロード中かどうか
    /// </summary>
    public bool IsLoading { get; private set; } = false;

    /// <summary>
    /// 現在のシーン名
    /// </summary>
    public string CurrentScene { get; private set; }
    #endregion

    #region Unity Events
    private void Start()
    {
        // 現在のシーン名を取得
        CurrentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
    #endregion

    #region Public Methods - Generic
    /// <summary>
    /// 指定したシーンを読み込む
    /// </summary>
    /// <param name="sceneName">シーン名</param>
    public void LoadScene(string sceneName)
    {
        if (IsLoading) return;

        IsLoading = true;
        CurrentScene = sceneName;
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        IsLoading = false;
    }
    #endregion

    #region Public Methods - Specific Scenes
    /// <summary>
    /// タイトル画面に遷移
    /// </summary>
    public void LoadTitleScene()
    {
        LoadScene(TITLE_SCENE);
    }

    /// <summary>
    /// ホーム画面に遷移
    /// </summary>
    public void LoadHomeScene()
    {
        LoadScene(HOME_SCENE);
    }

    /// <summary>
    /// 装備編集画面に遷移
    /// </summary>
    public void LoadEquipmentEditScene()
    {
        LoadScene(EQUIPMENT_EDIT_SCENE);
    }

    /// <summary>
    /// 装備強化画面に遷移
    /// </summary>
    public void LoadEquipmentEnhanceScene()
    {
        LoadScene(EQUIPMENT_ENHANCE_SCENE);
    }

    /// <summary>
    /// クエスト戦闘画面に遷移
    /// </summary>
    public void LoadQuestBattleScene()
    {
        LoadScene(QUEST_BATTLE_SCENE);
    }
    #endregion
}