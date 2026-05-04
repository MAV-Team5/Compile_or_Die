using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 오브젝트 풀링 시스템
/// Instantiate/Destroy 대신 SetActive(true/false)로 재사용 → GC 부하 감소
/// prefabs 배열 인덱스: [0]=Enemy [1]=Bullet_0(근접) [2]=Bullet_1(원거리)
/// </summary>
public class PoolManager : MonoBehaviour
{
    [Header("# 프리팹 배열")]
    // Inspector에서 연결: [0]=Enemy, [1]=Bullet_0, [2]=Bullet_1
    public GameObject[] prefabs;

    // 각 프리팹별 오브젝트 리스트 (풀)
    // pools[0] = Enemy 풀, pools[1] = Bullet_0 풀 ...
    List<GameObject>[] pools;

    void Awake()
    {
        // 프리팹 개수만큼 풀(리스트) 생성
        pools = new List<GameObject>[prefabs.Length];
        for (int i = 0; i < pools.Length; i++)
            pools[i] = new List<GameObject>();
    }

    /// <summary>
    /// 해당 인덱스의 풀에서 오브젝트 하나를 꺼내 반환
    /// 비활성화된 오브젝트가 있으면 재사용, 없으면 새로 생성
    /// </summary>
    /// <param name="index">prefabs 배열 인덱스</param>
    public GameObject Get(int index)
    {
        GameObject select = null;

        // 풀에서 비활성화 상태(놀고 있는) 오브젝트 탐색
        foreach (GameObject item in pools[index])
        {
            if (!item.activeSelf) // activeSelf=false → 현재 비활성화 상태
            {
                select = item;
                select.SetActive(true); // 활성화해서 재사용
                break;
            }
        }

        // 풀이 비었거나 모두 사용 중이면 새로 생성
        if (select == null)
        {
            // PoolManager의 자식으로 생성 → Hierarchy 정리
            select = Instantiate(prefabs[index], transform);
            pools[index].Add(select); // 풀에 등록 (이후 재사용 대상이 됨)
        }

        return select;
    }
}
