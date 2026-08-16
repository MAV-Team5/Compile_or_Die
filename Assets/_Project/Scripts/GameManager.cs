using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public float gameTime;
    public float maxGameTime = 2 * 10f;

    public Player player;
    public PoolManager poolManager;
    public ExpManager expManager;

    /// <summary>이번 런의 처치 수. Enemy.Dead 가 올린다.</summary>
    public int kills { get; private set; }

    public bool isGameOver { get; private set; }

    public LevelSystem levelSystem { get; private set; }

    /// <summary>남은 시간(초). HUD 타이머가 읽는다.</summary>
    public float RemainingTime => Mathf.Max(0f, maxGameTime - gameTime);

    [SerializeField]
    private GameObject backgroundPrefab;
    private void Awake()
    {
        instance = this;

        // 일시정지 중 씬이 넘어와도 멈춘 채 시작하지 않게
        Time.timeScale = 1f;

        levelSystem = GetComponent<LevelSystem>();
        if (levelSystem == null) levelSystem = gameObject.AddComponent<LevelSystem>();

        // 씬에 PlayerHealth 를 붙이지 않았어도 체력 시스템이 돌게 한다
        if (player != null && player.GetComponent<PlayerHealth>() == null)
            player.gameObject.AddComponent<PlayerHealth>();
    }

    public void AddKill()
    {
        if (!isGameOver) kills++;
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Time.timeScale = 0f;
    }

    void Start()
    {
        GameObject background = Instantiate(backgroundPrefab, Vector3.zero, quaternion.identity);

        LogManager.Instance.NoneLog("SYSTEM BOOT COMPLETE");
        LogManager.Instance.NoneLog("> boot --safe-mode");
        LogManager.Instance.NoneLog("> kernel loaded");
        LogManager.Instance.NoneLog("> process attached : PLAYER_01");
        LogManager.Instance.NoneLog("> loading modules...");
        LogManager.Instance.NoneLog("> firewall.dll initialized");
        LogManager.Instance.NoneLog("> threat scanner online");
        LogManager.Instance.NoneLog("> stage=01");
        LogManager.Instance.NoneLog("> objective=\"stabilize memory sector\"");

    }

    void Update()
    {
        gameTime += Time.deltaTime;

        if (gameTime > maxGameTime)
        {
            gameTime = maxGameTime;
        }
    }
}
