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

    [SerializeField]
    private GameObject backgroundPrefab;
    private void Awake()
    {
        instance = this;
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
