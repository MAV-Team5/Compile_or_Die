using UnityEngine;

/// <summary>연출이 어디에 나타날지.</summary>
public enum VfxAnchor
{
    /// <summary>시전자 위치. 연쇄 단계에서는 직전에 맞은 적이 된다.</summary>
    Owner,

    /// <summary>적중한 좌표에 고정.</summary>
    HitPoint,

    /// <summary>적중한 대상에 붙어서 따라다닌다.</summary>
    Target
}

/// <summary>
/// 연출 프리팹을 지정 위치에 띄운다.
/// 수명은 프리팹이 스스로 관리한다 (파티클의 Stop Action = Destroy).
/// </summary>
public static class VfxSpawner
{
    public static void Spawn(GameObject prefab, VfxAnchor anchor, float scale,
                             AugmentContext ctx, HitInfo hit)
    {
        if (prefab == null) return;

        Vector2 position = anchor switch
        {
            VfxAnchor.HitPoint => hit.Point,
            VfxAnchor.Target   => hit.Target != null ? (Vector2)hit.Target.position : hit.Point,
            _                  => ctx.Owner.position
        };

        GameObject go = Create(prefab, position, scale);

        // Target 앵커만 대상을 따라간다
        if (anchor == VfxAnchor.Target && hit.Target != null)
            go.transform.SetParent(hit.Target, true);
    }

    /// <summary>적중 정보가 없는 시점(시전·발사)용.</summary>
    public static void SpawnAt(GameObject prefab, Vector2 position, float scale)
    {
        if (prefab == null) return;

        Create(prefab, position, scale);
    }

    static GameObject Create(GameObject prefab, Vector2 position, float scale)
    {
        GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);

        if (!Mathf.Approximately(scale, 1f))
            go.transform.localScale *= scale;

        return go;
    }
}
