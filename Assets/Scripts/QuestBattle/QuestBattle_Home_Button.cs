using UnityEngine;

public class QuestBattle_Home_Button : MonoBehaviour
{
    public void GoToHome()
    {
        SceneManager.Instance.LoadHomeScene();
    }
}