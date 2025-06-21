using UnityEngine;

public class EquipmentEnhance_Home_Button : MonoBehaviour
{
    public void GoToHome()
    {
        SceneManager.Instance.LoadHomeScene();
    }
}