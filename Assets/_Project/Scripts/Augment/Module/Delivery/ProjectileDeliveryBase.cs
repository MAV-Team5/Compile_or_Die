using System.Collections.Generic;
using UnityEngine;

/// <summary>투사체를 쏘는 전달 모듈들의 공통 설정과 발사 로직.</summary>
[System.Serializable]
public abstract class ProjectileDeliveryBase : DeliveryModule
{
    [Header("투사체")]
    [Required("투사체가 아예 발사되지 않는다")]
    [Tooltip("발사할 투사체 프리팹. AugmentProjectile 컴포넌트가 붙어 있어야 한다.")]
    public GameObject projectilePrefab;

    [Sheet("속도")]
    [Tooltip("초당 이동 거리(유닛). 0이면 시트의 속도(speed)를 쓴다 — 레벨업으로 빨라지게 하려면 비워둘 것.")]
    public float speed = 0f;

    [Sheet("관통력")]
    [Tooltip("몇 명을 뚫고 지나갈지.\n" +
             "0 × 1 이면 시트 그대로, 0 × 2 면 시트의 두 배 — 레벨업을 따라간다.\n" +
             "전달을 여러 개 쓸 때 각자 배수만 달리 주면 서로 다른 관통을 갖는다.")]
    public Scalable pierce = Scalable.Ratio(1f);

    [Sheet("사거리")]
    [Tooltip("비행 거리(유닛). 비워두면 타겟팅이 정한 사거리를 쓴다.\n" +
             "배수만 주면 그 사거리에 비례한다 — 0 × 1.2 면 사거리의 120%.")]
    public Scalable travelRange = Scalable.Ratio(1.2f);

    [Detail]
    [Tooltip("최대 생존 시간(초). 시트와 무관한 안전장치. 거리보다 먼저 끝나면 여기서 사라진다.")]
    public float lifetime = 3f;

    [Detail]
    [Tooltip("켜면 이번에 함께 쏜 투사체들이 같은 적을 중복해서 못 맞힌다.\n" +
             "방사·산탄이 한 놈에게 몰리는 것을 막아 여러 대상에 골고루 퍼진다.")]
    public bool oneHitPerTarget = false;

    [Detail]
    [Tooltip("켜면 쏜 자리가 사라질 때 투사체도 같이 접는다.\n" +
             "하위 파이프라인에서 쏜 적이 죽으면, 그 적은 풀에서 되살아나 딴 자리에 선다 —\n" +
             "그대로 두면 원점이 엉뚱한 적으로 바뀌어 잘못 이어진다.\n" +
             "플레이어가 쏘는 투사체에는 영향이 없다.")]
    public bool dieWithOrigin = true;

    [Fold("유도")]
    public Homing homing = new();

    [Fx("발사 연출", "발사 원점")]
    public FxGroup launchFx = new();

    /// <summary>프리팹이 물려 있는지 확인한다. 없으면 경고.</summary>
    protected bool HasPrefab(AugmentContext ctx)
    {
        if (projectilePrefab != null) return true;

        ModuleWarning.Once(ctx, $"{GetType().Name} 에 투사체 프리팹이 없습니다");
        return false;
    }

    /// <summary>발사 연출. 여러 발이면 방향이 갈리므로 이 단계가 향하는 방향을 쓴다.</summary>
    protected void PlayLaunch(AugmentContext ctx, Vector2 origin)
        => launchFx.PlayAt(origin, ctx.Heading, 0f, ctx.Owner);

    /// <summary>
    /// 이번 발사분이 공유할 적중 기록. 끄면 null 이라 투사체마다 따로 센다.
    /// Execute 시작에서 한 번 만들어 그 볼리의 모든 Fire 에 넘길 것.
    /// </summary>
    protected HashSet<Transform> NewVolley()
        => oneHitPerTarget ? new HashSet<Transform>() : null;

    /// <summary>한 방향으로 여러 발을 대형에 맞춰 쏜다. shots 가 1이면 그냥 한 발.</summary>
    protected void FireSpread(AugmentContext ctx, Vector2 origin, Vector2 direction,
                              int shots, ShotFormation formation, float spacing,
                              float anglePerShot, System.Action<HitInfo> onHit,
                              Transform launchTarget = null,
                              HashSet<Transform> volley = null)
    {
        if (shots <= 1)
        {
            Fire(ctx, origin, direction, onHit, launchTarget, volley);
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

            Fire(ctx, spawn, aim, onHit, launchTarget, volley);
        }
    }

    /// <summary>
    /// 한 발 발사한다.
    /// launchTarget 은 유도가 처음 쫓을 대상이다. 겨냥해서 쏘는 전달만 넘길 수 있다.
    /// </summary>
    protected void Fire(AugmentContext ctx, Vector2 origin, Vector2 direction,
                        System.Action<HitInfo> onHit, Transform launchTarget = null,
                        HashSet<Transform> volley = null)
    {
        float flightSpeed = speed > 0f ? speed : ctx.Stat.speed;

        if (flightSpeed <= 0f)
        {
            ModuleWarning.Once(ctx, "속도가 0이라 투사체가 안 나갑니다. " +
                                    "시트의 속도(speed)를 채우거나 모듈에 직접 입력할 것");
            return;
        }

        int hits = pierce.IntOf(ctx.Stat.pierce);

        GameObject go = PooledSpawner.Spawn(projectilePrefab, origin, PoolType.Bullet);

        // 비행 거리는 타겟팅이 정한 사거리를 따른다. 좁게 탐색했으면 투사체도 짧게 난다
        float travel = travelRange.Of(ctx.EffectiveRange);

        // 연쇄 단계면 Owner가 방금 맞은 적이다. 그 안에서 태어나므로 한 번은 통과시켜야 한다
        go.GetComponent<AugmentProjectile>()
          .Launch(direction, flightSpeed, lifetime, travel,
                  hits, TargetQuery.Mask, ctx.Owner, onHit, homing, launchTarget, volley,
                  dieWithOrigin);
    }
}
