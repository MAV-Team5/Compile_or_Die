using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
public enum PoolType
{
    Enemy,
    Bullet,
    Effect
}
//오브젝트 풀링을 위한 풀 매니저, 각각의 풀 리스트를 사용.
public class PoolManager : MonoBehaviour
{
    [Header("Enemy")]
    public GameObject[] enemyPrefabs;
    [Header("Bullet")]
    public GameObject[] bulletPrefabs;
    [Header("Effect")]
    public GameObject[] effectPrefabs;


    List<GameObject>[] enemyPools;
    List<GameObject>[] bulletPools;
    List<GameObject>[] effectPools;

    void Awake()
    {
        enemyPools = CreatePools(enemyPrefabs.Length);
        bulletPools = CreatePools(bulletPrefabs.Length);
        effectPools = CreatePools(effectPrefabs.Length);
    }

    /// <summary>
    /// 각 풀 초기화 함수.
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    List<GameObject>[] CreatePools(int size)
    {
        List<GameObject>[] pools = new List<GameObject>[size];

        for (int i = 0; i < size; i++)
        {
            pools[i] = new List<GameObject>();
        }

        return pools;
    }
    /// <summary>
    /// 각 풀링 타입에 맞춰 오브젝트 찾아오기. 호출용.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public GameObject Get(PoolType type, int index)
    {
        switch (type)
        {
            case PoolType.Enemy:
                return GetObject(enemyPools, enemyPrefabs, index);
            case PoolType.Bullet:
                return GetObject(bulletPools, bulletPrefabs, index);
            case PoolType.Effect:
                return GetObject(effectPools, effectPrefabs, index);
            default:
                return null;
        }
    }

    /// <summary>
    /// 해당하는 풀에서 인덱스로 프리팹 목록에서 찾아 활성화.
    /// </summary>
    /// <param name="pools"></param>
    /// <param name="prefabs"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public GameObject GetObject(List<GameObject>[] pools, GameObject[] prefabs, int index)
    {
        GameObject select = null;

        foreach(GameObject item in pools[index])
        {
            if (!item.activeSelf)
            {
                select = item;
                select.SetActive(true);
                break;
            }
        }
        if(select == null)
        {
            select = Instantiate(prefabs[index], transform);
            pools[index].Add(select);
        }

        return select;
    }
}
