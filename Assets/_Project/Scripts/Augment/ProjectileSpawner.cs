using UnityEngine;

/// <summary>투사체 생성 창구. 풀이 있으면 재사용하고 없는 씬에서는 직접 만든다.</summary>
public static class ProjectileSpawner
{
    public static GameObject Spawn(GameObject prefab, Vector2 position)
    {
        PoolManager pool = GameManager.instance != null ? GameManager.instance.poolManager : null;

        GameObject go = pool != null ? pool.Get(prefab) : Object.Instantiate(prefab);

        go.transform.SetPositionAndRotation(position, Quaternion.identity);
        return go;
    }
}
