using UnityEngine;

/// <summary>투사체를 쏘는 Delivery 들의 공통 파라미터와 발사 로직.</summary>
[System.Serializable]
public abstract class ProjectileDeliveryBase : DeliveryModule
{
    [Header("투사체")]
    [Tooltip("초당 이동 거리(유닛).")]
    public float speed = 12f;

    [Tooltip("최대 생존 시간(초). 사거리보다 먼저 끝나면 여기서 사라진다. 안전장치.")]
    public float lifetime = 3f;

    [Tooltip("몇 명을 뚫고 지나갈지. 1이면 첫 적중에서 소멸.")]
    public int pierce = 1;

    [Tooltip("증강 사거리 대비 비행 거리 배수. 1.2 면 사거리의 120% 까지 날아간다.")]
    public float travelRangeMultiplier = 1.2f;

    [Tooltip("발사할 투사체 프리팹. AugmentProjectile 컴포넌트가 있어야 한다.")]
    public GameObject projectilePrefab;

    [Header("발사 연출")]
    [Tooltip("발사 원점에 띄울 이펙트.")]
    public GameObject launchVfx;

    public float launchVfxScale = 1f;

    [Tooltip("발사 순간 효과음.")]
    public AudioClip launchSfx;

    /// <summary>프리팹이 물려 있는지 확인한다. 없으면 경고.</summary>
    protected bool HasPrefab(AugmentContext ctx)
    {
        if (projectilePrefab != null) return true;

        Debug.LogWarning($"[{ctx.Instance.Data.name}] {GetType().Name} 에 투사체 프리팹이 없습니다");
        return false;
    }

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

    protected void PlayLaunch(Vector2 origin)
    {
        VfxSpawner.SpawnAt(launchVfx, origin, launchVfxScale);
        SfxPlayer.Play(launchSfx);
    }

    /// <summary>한 발 발사한다.</summary>
    protected void Fire(AugmentContext ctx, Vector2 origin, Vector2 direction,
                        System.Action<HitInfo> onHit)
    {
        if (speed <= 0f)
        {
            Debug.LogWarning($"[{ctx.Instance.Data.name}] speed 가 0이라 투사체가 제자리에 섭니다");
            return;
        }

        GameObject go = ProjectileSpawner.Spawn(projectilePrefab, origin);

        // 연쇄 단계면 Owner가 방금 맞은 적이다. 그 안에서 태어나므로 한 번은 통과시켜야 한다
        go.GetComponent<AugmentProjectile>()
          .Launch(direction, speed, lifetime, ctx.Stat.range * travelRangeMultiplier,
                  pierce, TargetQuery.Mask, ctx.Owner, onHit);
    }
}
