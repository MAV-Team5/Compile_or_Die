using UnityEngine;

/// <summary>투사체를 쏘는 전달 모듈들의 공통 설정과 발사 로직.</summary>
[System.Serializable]
public abstract class ProjectileDeliveryBase : DeliveryModule
{
    [Header("투사체")]
    [Required("투사체가 아예 발사되지 않는다")]
    [Tooltip("발사할 투사체 프리팹. AugmentProjectile 컴포넌트가 붙어 있어야 한다.")]
    public GameObject projectilePrefab;

    [Tooltip("초당 이동 거리(유닛). 0이면 시트의 속도(speed)를 쓴다.")]
    public float speed = 12f;

    [Tooltip("몇 명을 뚫고 지나갈지. 0이면 시트의 관통력(pierce)을 쓰고, 그것도 0이면 1명.")]
    public int pierce = 1;

    [Tooltip("타겟팅이 정한 사거리에 곱할 비행 거리 배수. 1.2 면 그 거리의 120% 까지 날아간다.\n" +
             "타겟팅에서 사거리를 좁히면 투사체도 같이 짧아진다.")]
    public float travelRangeMultiplier = 1.2f;

    [Tooltip("최대 생존 시간(초). 시트와 무관한 안전장치. 거리보다 먼저 끝나면 여기서 사라진다.")]
    public float lifetime = 3f;

    [Fx("발사 연출", "발사 원점")]
    public FxGroup launchFx = new();

    /// <summary>프리팹이 물려 있는지 확인한다. 없으면 경고.</summary>
    protected bool HasPrefab(AugmentContext ctx)
    {
        if (projectilePrefab != null) return true;

        Debug.LogWarning($"[{ctx.Instance.Data.name}] {GetType().Name} 에 투사체 프리팹이 없습니다");
        return false;
    }

    protected void PlayLaunch(Vector2 origin) => launchFx.PlayAt(origin);

    /// <summary>한 방향으로 여러 발을 대형에 맞춰 쏜다. shots 가 1이면 그냥 한 발.</summary>
    protected void FireSpread(AugmentContext ctx, Vector2 origin, Vector2 direction,
                              int shots, ShotFormation formation, float spacing,
                              float anglePerShot, System.Action<HitInfo> onHit)
    {
        if (shots <= 1)
        {
            Fire(ctx, origin, direction, onHit);
            return;
        }

        Vector2 perpendicular = new(-direction.y, direction.x);
        float center = (shots - 1) * 0.5f;

        for (int i = 0; i < shots; i++)
        {
            // 2발이면 -0.5·+0.5, 3발이면 -1·0·+1 로 중앙 대칭이 된다
            float lane = i - center;

            // 나란히는 좌우 대칭, 줄줄이는 앞으로 늘어서고 마지막 발이 원점에 선다
            Vector2 spawn = formation == ShotFormation.Parallel
                ? origin + perpendicular * (lane * spacing)
                : origin + direction * ((shots - 1 - i) * spacing);

            Vector2 aim = Mathf.Approximately(anglePerShot, 0f)
                ? direction
                : Rotate(direction, lane * anglePerShot);

            Fire(ctx, spawn, aim, onHit);
        }
    }

    static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    /// <summary>한 발 발사한다.</summary>
    protected void Fire(AugmentContext ctx, Vector2 origin, Vector2 direction,
                        System.Action<HitInfo> onHit)
    {
        float flightSpeed = speed > 0f ? speed : ctx.Stat.speed;

        if (flightSpeed <= 0f)
        {
            Debug.LogWarning($"[{ctx.Instance.Data.name}] 속도가 0이라 투사체가 제자리에 섭니다");
            return;
        }

        int hits = pierce > 0 ? pierce : ctx.Stat.pierce;
        if (hits <= 0) hits = 1;

        GameObject go = ProjectileSpawner.Spawn(projectilePrefab, origin);

        // 비행 거리는 타겟팅이 정한 사거리를 따른다. 좁게 탐색했으면 투사체도 짧게 난다
        float travel = ctx.EffectiveRange * travelRangeMultiplier;

        // 연쇄 단계면 Owner가 방금 맞은 적이다. 그 안에서 태어나므로 한 번은 통과시켜야 한다
        go.GetComponent<AugmentProjectile>()
          .Launch(direction, flightSpeed, lifetime, travel,
                  hits, TargetQuery.Mask, ctx.Owner, onHit);
    }
}
