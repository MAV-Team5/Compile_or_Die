using UnityEngine;

/// <summary>
/// 제자리에 나고 스스로 사라지는 오브젝트의 생성 창구.
/// 풀이 있으면 재사용하고, 풀이 없는 씬에서는 직접 만든다.
///
/// <b>여기로 나온 것은 반드시 Despawn 으로 돌려보내야 한다.</b>
/// Destroy 하면 풀 목록에 죽은 참조가 남는다.
///
/// 부모에 매달려야 하는 것(표식·상태이상 시각)과 오래 사는 것(소환물)은
/// 풀에 안 맞으므로 이 창구를 쓰지 않는다.
/// </summary>
public static class PooledSpawner
{
    public static GameObject Spawn(GameObject prefab, Vector2 position,
                                   PoolType category = PoolType.Effect)
    {
        if (prefab == null) return null;

        PoolManager pool = GameManager.instance != null ? GameManager.instance.poolManager : null;

        GameObject go = pool != null
            ? pool.Get(prefab, category)
            : Object.Instantiate(prefab);

        go.transform.SetPositionAndRotation(position, Quaternion.identity);

        EnsureParticleReplay(go);

        return go;
    }

    /// <summary>
    /// 파티클이 있으면 되감아 트는 컴포넌트를 붙여둔다.
    /// 프리팹마다 챙기게 하면 언젠가 빠뜨리므로 여기서 한 번에 처리한다.
    /// 이미 붙어 있으면 아무 일도 안 하고, 다음부터는 OnEnable 이 알아서 돈다.
    /// </summary>
    static void EnsureParticleReplay(GameObject go)
    {
        if (go.TryGetComponent(out PooledParticles _)) return;
        if (go.GetComponentInChildren<ParticleSystem>(true) == null) return;

        go.AddComponent<PooledParticles>();
    }

    /// <summary>풀로 돌려보낸다. 파괴하지 않아야 다음에 재사용된다.</summary>
    public static void Despawn(GameObject go)
    {
        if (go != null) go.SetActive(false);
    }
}
