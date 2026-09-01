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

    void Start()
    {
        // 결과 화면에서 "업그레이드"로 들어온 경우. 로비를 거치지 않고 바로 상점을 연다
        if (LobbyIntent.Consume() == LobbyIntent.Screen.Upgrade) OnClickUpgradeMenu();
    }

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

    [Header("씬 이름")]
    [Tooltip("스테이지가 도는 씬. 스테이지가 늘어도 이 씬 하나를 쓴다 —\n" +
             "무엇이 나오는지는 StageData 가 정하므로 씬을 복제할 이유가 없다.")]
    [SerializeField] string runScene = "Run";

    [Tooltip("고를 수 있는 스테이지. 버튼이 넘긴 번호와 에셋의 Stage Id 로 맞춘다.\n\n" +
             "★ 배열 순서가 아니라 Stage Id 로 찾는다 — 순서를 바꿔도 버튼이 안 어긋난다.")]
    [SerializeField] StageData[] stages;

    /// <summary>
    /// 스테이지를 고르고 들어간다. 버튼의 OnClick 에 번호를 적어 부른다.
    ///
    /// <b>스테이지가 늘어도 씬은 하나다.</b> 무엇이 나오는지는 전부 StageData 가 정하므로
    /// (배경·웨이브·보스·증강 풀·정산), 씬을 복제하면 버그를 고칠 때마다 사본을 다 고쳐야 한다.
    /// </summary>
    public void OnClickRun(int stageId)
    {
        StageData stage = FindStage(stageId);

        // 못 찾았는데 그냥 들어가면 엉뚱한 스테이지가 돌고 원인을 못 찾는다
        if (stage == null)
        {
            Debug.LogWarning($"[MenuManager] Stage Id {stageId} 인 StageData 가 목록에 없다. "
                           + "Stages 배열과 버튼 번호를 맞출 것.", this);
            return;
        }

        StageContext.Choose(stage);

        SceneManager.LoadScene(runScene);
    }

    StageData FindStage(int stageId)
    {
        if (stages == null) return null;

        for (int i = 0; i < stages.Length; i++)
            if (stages[i] != null && stages[i].stageId == stageId) return stages[i];

        return null;
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
    /// <summary>
    /// 런을 그만두고 결과 화면으로. 일시정지 메뉴의 나가기가 부른다.
    ///
    /// 런 중이라면 씬을 직접 넘기지 않고 RunDirector 에 맡긴다 —
    /// 정산을 거쳐야 이번 판에 모은 비트가 지급되고 성적표도 새로 채워진다.
    /// 씬 이동은 정산이 끝난 뒤 RunDirector 가 알아서 한다.
    /// </summary>
    public void OnClickStageExit()
    {
        if (RunDirector.Current != null && RunDirector.IsPlaying)
        {
            RunDirector.Current.Abandon();
            return;
        }

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
