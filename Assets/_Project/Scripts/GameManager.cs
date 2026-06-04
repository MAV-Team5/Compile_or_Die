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
