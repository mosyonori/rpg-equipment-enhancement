using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// ゲーム全体のシーン遷移を管理するマネージャー
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    [Header("シーン名設定")]
    public string titleSceneName = "TitleScene";
    public string homeSceneName = "HomeScene";
    public string equipmentSceneName = "EquipmentScene";
    public string questSceneName = "QuestScene";

    [Header("ロード設定")]
    public bool useLoadingScreen = true;
    public float minimumLoadTime = 1.0f;

    private static GameSceneManager instance;
    public static GameSceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                // DontDestroyOnLoadオブジェクトから検索
                instance = FindFirstObjectByType<GameSceneManager>();
                if (instance == null)
                {
                    // 新しく作成
                    GameObject go = new GameObject("GameSceneManager");
                    instance = go.AddComponent<GameSceneManager>();
                    DontDestroyOnLoad(go);
                    Debug.Log("GameSceneManager を自動作成しました");
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        // シングルトン実装
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("GameSceneManager を初期化しました");
        }
        else if (instance != this)
        {
            Debug.Log("重複するGameSceneManagerを削除します");
            Destroy(gameObject);
        }
    }

    #region シーン遷移メソッド

    /// <summary>
    /// タイトル画面に遷移
    /// </summary>
    public void LoadTitleScene()
    {
        Debug.Log("タイトル画面に遷移します");
        LoadScene(titleSceneName);
    }

    /// <summary>
    /// ホーム画面に遷移
    /// </summary>
    public void LoadHomeScene()
    {
        Debug.Log("ホーム画面に遷移します");
        LoadScene(homeSceneName);
    }

    /// <summary>
    /// 装備強化画面に遷移
    /// </summary>
    public void LoadEquipmentScene()
    {
        Debug.Log("装備強化画面に遷移します");
        LoadScene(equipmentSceneName);
    }

    /// <summary>
    /// クエスト画面に遷移
    /// </summary>
    public void LoadQuestScene()
    {
        Debug.Log("クエスト画面に遷移します");
        LoadScene(questSceneName);
    }

    /// <summary>
    /// 指定されたシーンに遷移
    /// </summary>
    public void LoadScene(string sceneName)
    {
        // シーンの存在確認
        if (!DoesSceneExist(sceneName))
        {
            Debug.LogError($"シーン '{sceneName}' が Build Settings に存在しません。Build Settings で追加してください。");
            return;
        }

        if (useLoadingScreen)
        {
            StartCoroutine(LoadSceneWithLoading(sceneName));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// ロード画面付きでシーン遷移
    /// </summary>
    private IEnumerator LoadSceneWithLoading(string sceneName)
    {
        // ロード開始時の処理
        OnLoadStart();

        float startTime = Time.time;

        // 非同期でシーンをロード
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // ロード進行状況を監視
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            OnLoadProgress(progress);

            // ロードが完了し、最小時間も経過した場合
            if (asyncLoad.progress >= 0.9f && Time.time - startTime >= minimumLoadTime)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        // ロード完了時の処理
        OnLoadComplete();
    }

    #endregion

    #region ロードイベント

    /// <summary>
    /// ロード開始時の処理
    /// </summary>
    private void OnLoadStart()
    {
        Debug.Log("シーンロード開始");
        // ここでロード画面を表示
        // 例: LoadingUI.Instance.Show();
    }

    /// <summary>
    /// ロード進行状況更新
    /// </summary>
    private void OnLoadProgress(float progress)
    {
        Debug.Log($"ロード進行状況: {progress * 100:F1}%");
        // ここでロード画面のプログレスバーを更新
        // 例: LoadingUI.Instance.SetProgress(progress);
    }

    /// <summary>
    /// ロード完了時の処理
    /// </summary>
    private void OnLoadComplete()
    {
        Debug.Log("シーンロード完了");
        // ここでロード画面を非表示
        // 例: LoadingUI.Instance.Hide();
    }

    #endregion

    #region ユーティリティ

    /// <summary>
    /// 現在のシーン名を取得
    /// </summary>
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// 指定されたシーンが現在のシーンかチェック
    /// </summary>
    public bool IsCurrentScene(string sceneName)
    {
        return GetCurrentSceneName() == sceneName;
    }

    /// <summary>
    /// シーンが存在するかチェック
    /// </summary>
    public bool DoesSceneExist(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName)
                return true;
        }
        return false;
    }

    #endregion

    #region デバッグ用

    [ContextMenu("Load Title Scene")]
    public void DebugLoadTitle() => LoadTitleScene();

    [ContextMenu("Load Home Scene")]
    public void DebugLoadHome() => LoadHomeScene();

    [ContextMenu("Load Equipment Scene")]
    public void DebugLoadEquipment() => LoadEquipmentScene();

    [ContextMenu("Print Current Scene")]
    public void DebugPrintCurrentScene()
    {
        Debug.Log($"現在のシーン: {GetCurrentSceneName()}");
    }

    [ContextMenu("Check Scene Existence")]
    public void DebugCheckSceneExistence()
    {
        Debug.Log($"Title Scene exists: {DoesSceneExist(titleSceneName)}");
        Debug.Log($"Home Scene exists: {DoesSceneExist(homeSceneName)}");
        Debug.Log($"Equipment Scene exists: {DoesSceneExist(equipmentSceneName)}");
    }

    #endregion
}