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
        if (anchor == VfxAnchor.Target) Attach(go, hit.Target);
    }

    /// <summary>
    /// 적중 정보가 없는 시점(시전·발사)용.
    /// attachTo 를 주면 그 오브젝트에 붙어 따라다닌다 — 몸에 붙는 연출용.
    /// </summary>
    public static void SpawnAt(GameObject prefab, Vector2 position, float scale,
                               Vector2 direction = default, float radius = 0f,
                               Transform attachTo = null)
    {
        if (prefab == null) return;

        GameObject go = Create(prefab, position, scale, direction, radius);

        Attach(go, attachTo);
    }

    /// <summary>
    /// 연출을 대상에 붙인다. 월드 크기를 보존하는 것이 핵심 —
    /// Instantiate 의 parent 인자를 쓰면 부모 스케일이 곱해져 크기가 통째로 어긋난다.
    /// </summary>
    public static void Attach(GameObject go, Transform parent)
    {
        if (go == null || parent == null) return;

        go.transform.SetParent(parent, true);
    }

    static GameObject Create(GameObject prefab, Vector2 position, float scale,
                             Vector2 direction, float radius = 0f)
    {
        GameObject go = PooledSpawner.Spawn(prefab, position, PoolType.Effect);

        if (!Mathf.Approximately(scale, 1f))
            go.transform.localScale *= scale;

        // 방향을 쓸 줄 아는 프리팹에만 건넨다. 회전이든 클립 선택이든 프리팹 몫
        if (direction.sqrMagnitude > 0.0001f && go.TryGetComponent(out IDirectionalVisual aimed))
            aimed.Aim(direction.normalized);

        // 판정 크기도 마찬가지. 스케일을 바꿀지 파티클을 만질지는 프리팹이 정한다
        if (radius > 0.0001f && go.TryGetComponent(out ISizedVisual sized))
            sized.Resize(radius);

        return go;
    }
}
