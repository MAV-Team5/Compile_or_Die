using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
    using UnityEditor;
    #endif

public class MenuManager : MonoBehaviour
{
    public GameObject settingPanel;

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
        settingPanel.SetActive(true);
    }

    public void CloseSettingMenu()
    {
        settingPanel.SetActive(false);
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
        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
}
