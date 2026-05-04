using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전체 상태를 관리하는 싱글톤 매니저
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("# 게임 제어")]
    public bool isLive;
    public float gameTime;
    public float maxGameTime = 300f;

    [Header("# 플레이어 정보")]
    public int playerId;
    public float health;
    public float maxHealth = 100f;
    public int level;
    public int kill;
    public int exp;
    public int[] nextExp = { 10, 20, 40, 80, 100, 150, 200, 250, 300, 400 };

    [Header("# 씬 오브젝트 참조")]
    public Player player;
    public PoolManager pool;
    public LevelUp uiLevelUp;
    public Result uiResult;
    public GameObject uiGameStart;
    public GameObject enemyCleaner;

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (!isLive) return;
        gameTime += Time.deltaTime;
        if (gameTime >= maxGameTime)
        {
            gameTime = maxGameTime;
            GameVictory();
        }
    }

    public void GameStart(int id)
    {
        playerId       = id;
        isLive         = true;
        health         = maxHealth;
        Time.timeScale = 1f;

        player.gameObject.SetActive(true);
        uiLevelUp.Select(playerId % 2);

        // AudioManager null 체크: 오디오 없이도 게임 동작
        PlayBgmSafe(true);
        PlaySfxSafe(AudioManager.Sfx.Select);
    }

    public void GameOver()
    {
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        isLive = false;
        yield return new WaitForSeconds(0.5f);
        uiResult.gameObject.SetActive(true);
        uiResult.Lose();
        Stop();
        PlayBgmSafe(false);
        PlaySfxSafe(AudioManager.Sfx.Lose);
    }

    public void GameVictory()
    {
        StartCoroutine(GameVictoryRoutine());
    }

    IEnumerator GameVictoryRoutine()
    {
        isLive = false;
        enemyCleaner.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        uiResult.gameObject.SetActive(true);
        uiResult.Win();
        Stop();
        PlayBgmSafe(false);
        PlaySfxSafe(AudioManager.Sfx.Win);
    }

    public void GameRetry()
    {
        SceneManager.LoadScene(0);
    }

    public void GetExp()
    {
        if (!isLive) return;
        exp++;
        int levelIndex = Mathf.Min(level, nextExp.Length - 1);
        if (exp == nextExp[levelIndex])
        {
            level++;
            exp = 0;
            uiLevelUp.Show();
        }
    }

    public void Stop()
    {
        isLive         = false;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        isLive         = true;
        Time.timeScale = 1f;
    }

    // ─── 오디오 null 안전 헬퍼 ───────────────────────────────────────────
    // AudioManager 오브젝트가 씬에 없어도 오류 없이 넘어감
    // 오디오 파일 준비 전 테스트 시 유용

    void PlayBgmSafe(bool isPlay)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlayBgm(isPlay);
    }

    void PlaySfxSafe(AudioManager.Sfx sfx)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySfx(sfx);
    }
}
