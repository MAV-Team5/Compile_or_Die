using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("# Game Control")]
    public bool isLive;
    public float gameTime;
    public float maxGameTime = 300f;

    [Header("# Player Info")]
    public int playerId;
    public float health;
    public float maxHealth = 100f;
    public int level;
    public int kill;
    public int exp;
    public int[] nextExp = { 10, 20, 40, 80, 100, 150, 200, 250, 300, 400 };

    [Header("# Game Object")]
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
        playerId  = id;
        isLive    = true;
        health    = maxHealth;
        Time.timeScale = 1f;

        player.gameObject.SetActive(true);
        uiLevelUp.Select(playerId % 2);

        AudioManager.instance.PlayBgm(true);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Select);
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
        AudioManager.instance.PlayBgm(false);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Lose);
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
        AudioManager.instance.PlayBgm(false);
        AudioManager.instance.PlaySfx(AudioManager.Sfx.Win);
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
        isLive = false;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        isLive = true;
        Time.timeScale = 1f;
    }
}
