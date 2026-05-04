using UnityEngine;

public class Spawner : MonoBehaviour
{
    public SpawnData[] spawnData;
    public Transform[] spawnPoints;

    public float levelTime;
    int level;
    float timer;

    void Awake()
    {
        spawnPoints = GetComponentsInChildren<Transform>();
        levelTime   = GameManager.instance.maxGameTime / spawnData.Length;
    }

    void Update()
    {
        if (!GameManager.instance.isLive) return;

        timer += Time.deltaTime;
        level  = Mathf.Min(
            Mathf.FloorToInt(GameManager.instance.gameTime / levelTime),
            spawnData.Length - 1
        );

        if (timer > spawnData[level].spawnTime)
        {
            timer = 0;
            Spawn();
        }
    }

    void Spawn()
    {
        GameObject enemy = GameManager.instance.pool.Get(0);
        enemy.transform.position = spawnPoints[Random.Range(1, spawnPoints.Length)].position;
        enemy.GetComponent<Enemy>().Init(spawnData[level]);
    }
}

[System.Serializable]
public class SpawnData
{
    public int   spriteType;
    public float spawnTime;
    public float health;
    public float speed;
}
