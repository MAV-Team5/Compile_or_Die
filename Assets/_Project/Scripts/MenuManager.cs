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
    public UIPanel pausePanel;
    public UIPanel logPanel;

    private bool setPause = false;

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

    public void OnClickStage(int stageId)
    {
        SceneManager.LoadScene("stage " + stageId);
    }

    public void OnClickPause()
    {
        if(setPause == false)
        {
            pausePanel.Open();
            setPause = true;
            Time.timeScale = 0;
        }
        else
        {
            pausePanel.Close();
            setPause = false;
            Time.timeScale = 1;
        }
    }
    

    public void OnClickLog()
    {
        logPanel.Open();
    }



    public void OnClickConnect()
    {
        SceneManager.LoadScene("MainB");
    }
    public void OnClickDisconnect()
    {
        SceneManager.LoadScene("MainA");
    }
    public void OnClickStageExit()
    {
        SceneManager.LoadScene("StageResult");
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
