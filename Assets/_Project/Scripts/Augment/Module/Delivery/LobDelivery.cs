using UnityEngine;

/// <summary>
/// 타겟에게 포물선으로 던진다. 날아가는 동안은 아무도 안 맞고, <b>떨어지는 순간 그 타겟을 한 번 적중</b>한다.
/// 떨어진 자리를 넓게 터뜨리려면 효과에 SubPipeline(OwnerPoint → Area) 를 넣는다.
///
/// 타겟이 움직이면 따라가서 그 위에 떨어진다. 날아가는 도중 죽으면 마지막 자리 근처의 적을 대신 맞힌다.
/// </summary>
[System.Serializable]
[ModuleInfo("포물선으로 던져 떨어질 때 적중", "착지점을 넓게 터뜨리려면 효과에 SubPipeline + Area")]
public class LobDelivery : DeliveryModule
{
    [Tooltip("비행 시간(초). 거리와 무관하게 이 시간에 떨어진다.")]
    [Min(0.05f)] public float flightTime = 0.45f;

    [Tooltip("포물선 높이(유닛).")]
    [Min(0f)] public float arcHeight = 1.5f;

    [Tooltip("타겟이 도중에 죽었을 때 착지점에서 대신 맞힐 적을 찾는 반경. 0이면 헛떨어진다.")]
    [Min(0f)] public float fallbackRadius = 1.5f;

    [Tooltip("날아가는 그림. 비우면 판정만 난다.")]
    public GameObject bodyPrefab;

    [Fx("착지 연출", "착지 지점")]
    public FxGroup landFx = new();

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        Vector2 origin = ctx.Owner.position;

        for (int i = 0; i < ctx.Targets.Count; i++)
        {
            TargetRef target = ctx.Targets.Items[i];
            if (!target.IsAlive) continue;

            Throw(ctx, origin, target, onHit);
        }
    }

    void Throw(AugmentContext ctx, Vector2 origin, TargetRef target, System.Action<HitInfo> onHit)
    {
        Transform enemy = target.Transform;
        Vector2 last = target.Position;

        Vector2 Destination()
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy) last = enemy.position;
            return last;
        }

        GameObject body = PooledSpawner.Spawn(bodyPrefab, origin);
        Transform owner = ctx.Owner;

        // 투사체 프리팹을 그림으로 물려도 보이게 한다. 발사(Launch) 없이 꺼내진 투사체는
        // 수명 0으로 첫 프레임에 스스로 사라지고, 콜라이더는 날아가는 도중 적을 건드린다
        SetFlightOnly(body, true);

        ArcMover.Move(body != null ? body.transform : null, origin, Destination, flightTime, arcHeight,
            landed =>
            {
                // 풀로 돌려보내기 전에 되살린다. 같은 프리팹이 진짜 투사체로 다시 쓰일 수 있다
                SetFlightOnly(body, false);
                PooledSpawner.Despawn(body);

                Vector2 direction = landed - origin;
                direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : ctx.Heading;

                landFx.PlayAt(landed, direction, 0f, owner);

                Transform hit = enemy != null && enemy.gameObject.activeInHierarchy
                    ? enemy
                    : (fallbackRadius > 0f ? TargetQuery.Nearest(landed, fallbackRadius) : null);

                if (hit == null) return;

                onHit(new HitInfo { Target = hit, Point = landed, Index = 0, Direction = direction });
            });
    }

    /// <summary>날아가는 동안 그림만 남긴다 — 투사체 동작과 충돌을 끈다. false 면 원래대로 켠다.</summary>
    static void SetFlightOnly(GameObject body, bool flightOnly)
    {
        if (body == null) return;

        if (body.TryGetComponent(out AugmentProjectile projectile)) projectile.enabled = !flightOnly;

        foreach (Collider2D collider in body.GetComponentsInChildren<Collider2D>(true))
            collider.enabled = !flightOnly;
    }
}
