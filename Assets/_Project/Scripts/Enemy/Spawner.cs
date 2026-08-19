using UnityEngine;

public class Spawner : MonoBehaviour
{

    public Transform[] spawnPoint;
    public SpawnData[] spawnData;
    // public int enemyIndex;
    int level;
    float timer;
    public int prefabId;

    void Awake()
    {
        spawnPoint = GetComponentsInChildren<Transform>();
    }
    void Update()
    {
        timer += Time.deltaTime;
        level = Mathf.Min(Mathf.FloorToInt(GameManager.instance.gameTime / 10f), spawnData.Length - 1);

        if (timer > spawnData[level].spawnTime)
        {
            timer = 0;
            Spawn();
        }
    }

    void Spawn()
    {
        GameObject enemy = GameManager.instance.poolManager.Get(PoolType.Enemy,prefabId);
        enemy.transform.position = spawnPoint[Random.Range(1, spawnPoint.Length)].position;

        // 물리 좌표는 다음 FixedUpdate 까지 transform 을 따라오지 않는다.
        // 그 사이에 증강이 사거리 검색을 하면 갓 스폰한 적이 원점에 있는 것으로 잡힌다
        Physics2D.SyncTransforms();

        enemy.GetComponent<Enemy>().Init(spawnData[level]);
    }

    // void SpawnByIndex(int index, int points = 0)
    // {
    //     GameObject enemy = GameManager.instance.pool.Get(index);
    //     if(points == 0)
    //     {
    //         enemy.transform.position = spawnPoint[Random.Range(1, spawnPoint.Length)].position;
    //     }

    // }

    // void SpawnSet(int enemyIndex = 0, int setPoints = 0, float cycle = 0.3f)
    // {
    //     timer += Time.deltaTime;
    //     if(timer > cycle)
    //     {
    //         timer = 0;
    //         SpawnByIndex(enemyIndex, setPoints);
    //     }
    // }
}

[System.Serializable]
public class SpawnData
{
    public int spriteType;
    public float spawnTime;
    public int health;
    public float speed;

    /// <summary>플레이어와 닿아 있는 동안 초당 주는 피해.</summary>
    public float contactDamage = 10f;
}