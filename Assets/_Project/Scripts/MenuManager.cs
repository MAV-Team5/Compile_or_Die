using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{

    public void OnClickCharacterMenu()
    {
        Debug.Log("character");
    }

    public void OnClickUpgradeMenu()
    {
        Debug.Log("upgrade");
    }

    public void OnClickStageMenu()
    {
        SceneManager.LoadScene("stage 1");
    }

    public void OnClickSettingMenu()
    {
        //settingsPanel.SetActive(true);
    }

    public void CloseSetting()
    {
        //settingsPanel.SetActive(false);
    }
    public void OnClickConnect()
    {
        SceneManager.LoadScene("MainB");
    }
    public void OnClickDisconnect()
    {
        SceneManager.LoadScene("MainA");
    }
    public void OnClickExit()
    {
        Application.Quit();
    }
}
