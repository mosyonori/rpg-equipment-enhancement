using UnityEngine;

public class EquipmentEdit_Home_Button : MonoBehaviour
{
    public void GoToHome()
    {
        SceneManager.Instance.LoadHomeScene();
    }
}