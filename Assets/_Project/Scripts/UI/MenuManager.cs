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

    public void OnClickRun(int stageId)
    {
        SceneManager.LoadScene("Run");
    }

    public void OnClickPause()
    {
        // 런이 이미 끝나 멈춰 있으면 받지 않는다.
        // 안 그러면 여기서 timeScale 을 1로 되돌려, 결과 화면 뒤에서 게임이 다시 돈다
        if (!CanPause()) return;

        if(setPause == false)
        {
            pausePanel.Open();
            setPause = true;

            if (UIManager.Current != null) UIManager.Current.Open(UIManager.Screen.Pause);
        }
        else
        {
            pausePanel.Close();
            setPause = false;

            // 레벨업 카드가 아직 떠 있으면 UIManager 가 안 풀어준다.
            // 예전에는 여기서 timeScale 을 1로 되돌려 카드 뒤에서 게임이 돌았다
            if (UIManager.Current != null) UIManager.Current.Close(UIManager.Screen.Pause);
        }
    }

    /// <summary>런이 이미 끝났으면 일시정지를 받지 않는다.</summary>
    bool CanPause() => RunDirector.IsPlaying;


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
