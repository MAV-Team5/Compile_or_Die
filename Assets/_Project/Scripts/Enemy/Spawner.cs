using Unity.VisualScripting;
using UnityEngine;

public class Spawner : MonoBehaviour
{

    public Transform[] spawnPoint;
    public int enemyIndex;
    float timer;

    void Awake()
    {
        spawnPoint = GetComponentsInChildren<Transform>();
    }
    void Update()
    {
        SpawnSet(enemyIndex);
    }

    void Spawn()
    {
        GameObject enemy = GameManager.instance.pool.Get(Random.Range(0,1));
        enemy.transform.position = spawnPoint[Random.Range(1, spawnPoint.Length)].position;

    }

    void SpawnByIndex(int index, int points = 0)
    {
        GameObject enemy = GameManager.instance.pool.Get(index);
        if(points == 0)
        {
            enemy.transform.position = spawnPoint[Random.Range(1, spawnPoint.Length)].position;
        }

    }

    void SpawnSet(int enemyIndex = 0, int setPoints = 0, float cycle = 0.3f)
    {
        timer += Time.deltaTime;
        if(timer > cycle)
        {
            timer = 0;
            SpawnByIndex(enemyIndex, setPoints);
        }
    }
}
