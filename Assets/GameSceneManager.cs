using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// �Q�[���S�̂̃V�[���J�ڂ��Ǘ�����}�l�[�W���[
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    [Header("�V�[�����ݒ�")]
    public string titleSceneName = "TitleScene";
    public string homeSceneName = "HomeScene";
    public string equipmentSceneName = "EquipmentScene";
    public string questSceneName = "QuestScene";

    [Header("���[�h�ݒ�")]
    public bool useLoadingScreen = true;
    public float minimumLoadTime = 1.0f;

    private static GameSceneManager instance;
    public static GameSceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                // DontDestroyOnLoad�I�u�W�F�N�g���猟��
                instance = FindFirstObjectByType<GameSceneManager>();
                if (instance == null)
                {
                    // �V�����쐬
                    GameObject go = new GameObject("GameSceneManager");
                    instance = go.AddComponent<GameSceneManager>();
                    DontDestroyOnLoad(go);
                    Debug.Log("GameSceneManager �������쐬���܂���");
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        // �V���O���g������
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("GameSceneManager �����������܂���");
        }
        else if (instance != this)
        {
            Debug.Log("�d������GameSceneManager���폜���܂�");
            Destroy(gameObject);
        }
    }

    #region �V�[���J�ڃ��\�b�h

    /// <summary>
    /// �^�C�g����ʂɑJ��
    /// </summary>
    public void LoadTitleScene()
    {
        Debug.Log("�^�C�g����ʂɑJ�ڂ��܂�");
        LoadScene(titleSceneName);
    }

    /// <summary>
    /// �z�[����ʂɑJ��
    /// </summary>
    public void LoadHomeScene()
    {
        Debug.Log("�z�[����ʂɑJ�ڂ��܂�");
        LoadScene(homeSceneName);
    }

    /// <summary>
    /// ����������ʂɑJ��
    /// </summary>
    public void LoadEquipmentScene()
    {
        Debug.Log("����������ʂɑJ�ڂ��܂�");
        LoadScene(equipmentSceneName);
    }

    /// <summary>
    /// �N�G�X�g��ʂɑJ��
    /// </summary>
    public void LoadQuestScene()
    {
        Debug.Log("�N�G�X�g��ʂɑJ�ڂ��܂�");
        LoadScene(questSceneName);
    }

    /// <summary>
    /// �w�肳�ꂽ�V�[���ɑJ��
    /// </summary>
    public void LoadScene(string sceneName)
    {
        // �V�[���̑��݊m�F
        if (!DoesSceneExist(sceneName))
        {
            Debug.LogError($"�V�[�� '{sceneName}' �� Build Settings �ɑ��݂��܂���BBuild Settings �Œǉ����Ă��������B");
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
    /// ���[�h��ʕt���ŃV�[���J��
    /// </summary>
    private IEnumerator LoadSceneWithLoading(string sceneName)
    {
        // ���[�h�J�n���̏���
        OnLoadStart();

        float startTime = Time.time;

        // �񓯊��ŃV�[�������[�h
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // ���[�h�i�s�󋵂��Ď�
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            OnLoadProgress(progress);

            // ���[�h���������A�ŏ����Ԃ��o�߂����ꍇ
            if (asyncLoad.progress >= 0.9f && Time.time - startTime >= minimumLoadTime)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        // ���[�h�������̏���
        OnLoadComplete();
    }

    #endregion

    #region ���[�h�C�x���g

    /// <summary>
    /// ���[�h�J�n���̏���
    /// </summary>
    private void OnLoadStart()
    {
        Debug.Log("�V�[�����[�h�J�n");
        // �����Ń��[�h��ʂ�\��
        // ��: LoadingUI.Instance.Show();
    }

    /// <summary>
    /// ���[�h�i�s�󋵍X�V
    /// </summary>
    private void OnLoadProgress(float progress)
    {
        Debug.Log($"���[�h�i�s��: {progress * 100:F1}%");
        // �����Ń��[�h��ʂ̃v���O���X�o�[���X�V
        // ��: LoadingUI.Instance.SetProgress(progress);
    }

    /// <summary>
    /// ���[�h�������̏���
    /// </summary>
    private void OnLoadComplete()
    {
        Debug.Log("�V�[�����[�h����");
        // �����Ń��[�h��ʂ��\��
        // ��: LoadingUI.Instance.Hide();
    }

    #endregion

    #region ���[�e�B���e�B

    /// <summary>
    /// ���݂̃V�[�������擾
    /// </summary>
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// �w�肳�ꂽ�V�[�������݂̃V�[�����`�F�b�N
    /// </summary>
    public bool IsCurrentScene(string sceneName)
    {
        return GetCurrentSceneName() == sceneName;
    }

    /// <summary>
    /// �V�[�������݂��邩�`�F�b�N
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

    #region �f�o�b�O�p

    [ContextMenu("Load Title Scene")]
    public void DebugLoadTitle() => LoadTitleScene();

    [ContextMenu("Load Home Scene")]
    public void DebugLoadHome() => LoadHomeScene();

    [ContextMenu("Load Equipment Scene")]
    public void DebugLoadEquipment() => LoadEquipmentScene();

    [ContextMenu("Print Current Scene")]
    public void DebugPrintCurrentScene()
    {
        Debug.Log($"���݂̃V�[��: {GetCurrentSceneName()}");
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