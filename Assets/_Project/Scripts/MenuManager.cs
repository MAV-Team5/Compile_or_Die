using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
    using UnityEditor;
    #endif

// 메뉴매니저는 씬 이동 및 선택메뉴 활성화(열기)만 담당.
public class MenuManager : MonoBehaviour
{
    public UIPanel settingPanel;
    public UIPanel stagePanel;
    public UIPanel characterPanel;
    public UIPanel upgardePanel;

    public void OnClickCharacterMenu()
    {
        characterPanel.Open();
    }

    public void OnClickUpgradeMenu()
    {
        upgardePanel.Open();
    }

    public void OnClickStageMenu()
    {
        stagePanel.Open();
    }

    public void OnClickSettingMenu()
    {
        settingPanel.Open();
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
