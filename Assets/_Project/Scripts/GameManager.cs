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
    public PoolManager pool;

    [SerializeField]
    private GameObject backgroundPrefab;
    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        GameObject background = Instantiate(backgroundPrefab, Vector3.zero, quaternion.identity);

        LogManager.Instance.System("SYSTEM BOOT COMPLETE");
        LogManager.Instance.DebugLog("POOL INITIALIZED");
        LogManager.Instance.Combat("ENEMY DETECTED");
        LogManager.Instance.Exp("EXP GAINED (+5)");
        LogManager.Instance.Skill("FIREWALL READY");
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
