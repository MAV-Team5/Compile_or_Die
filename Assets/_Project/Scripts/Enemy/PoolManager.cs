using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public GameObject[] enemyPrefabs;

    List<GameObject>[] pools;

    void Awake()
    {
        pools = new List<GameObject>[enemyPrefabs.Length];

        for (int index = 0; index < pools.Length; index++)
        {
            pools[index] = new List<GameObject>();
        }

        Debug.Log(pools.Length);
    }


    public GameObject Get(int index)
    {
        GameObject select = null;

        foreach(GameObject item in pools[index])
        {
            if (item.activeSelf)
            {
                select = item;
                select.SetActive(true);
                break;
            }
        }
        if(select == null)
        {
            select = Instantiate(enemyPrefabs[index], transform);
            pools[index].Add(select);
        }

        return select;
    }
}
