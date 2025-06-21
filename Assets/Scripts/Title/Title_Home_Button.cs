using UnityEngine;

public class Title_Home_Button : MonoBehaviour
{
    public void GoToHome()
    {
        SceneManager.Instance.LoadHomeScene();
    }
}