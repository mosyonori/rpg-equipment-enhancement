using UnityEngine;

public class SceneHome_Other_Button : MonoBehaviour
{
    public void GoToTitle()
    {
        SceneManager.Instance.LoadTitleScene();
    }

    public void GoToEquipmentEdit()
    {
        SceneManager.Instance.LoadEquipmentEditScene();
    }

    public void GoToEquipmentEnhance()
    {
        SceneManager.Instance.LoadEquipmentEnhanceScene();
    }

    public void GoToQuestBattle()
    {
        SceneManager.Instance.LoadQuestBattleScene();
    }
}