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
/// 방향은 IDirectionalVisual 을 통해 넘기며, 처리 방식은 프리팹이 정한다.
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

        // 적중 방향을 먼저 쓰고, 없으면 이 단계가 향하는 방향으로 물러난다
        Vector2 direction = hit.Direction.sqrMagnitude > 0.0001f ? hit.Direction : ctx.Heading;

        GameObject go = Create(prefab, position, scale, direction);

        // Target 앵커만 대상을 따라간다
        if (anchor == VfxAnchor.Target && hit.Target != null)
            go.transform.SetParent(hit.Target, true);
    }

    /// <summary>적중 정보가 없는 시점(시전·발사)용.</summary>
    public static void SpawnAt(GameObject prefab, Vector2 position, float scale,
                               Vector2 direction = default)
    {
        if (prefab == null) return;

        Create(prefab, position, scale, direction);
    }

    static GameObject Create(GameObject prefab, Vector2 position, float scale, Vector2 direction)
    {
        GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);

        if (!Mathf.Approximately(scale, 1f))
            go.transform.localScale *= scale;

        // 방향을 쓸 줄 아는 프리팹에만 건넨다. 회전이든 클립 선택이든 프리팹 몫
        if (direction.sqrMagnitude > 0.0001f && go.TryGetComponent(out IDirectionalVisual visual))
            visual.Aim(direction.normalized);

        return go;
    }
}
