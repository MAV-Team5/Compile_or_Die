using UnityEngine;

/// <summary>원점에서 균등한 각도로 여러 발 방사. 타겟과 무관하게 사방으로 나간다.</summary>
[System.Serializable]
public class RadialDelivery : DeliveryModule
{
    [Tooltip("발사 수. 0이면 레벨 수치의 count 를 쓴다.")]
    public int projectileCount = 0;

    [Tooltip("초당 이동 거리(유닛).")]
    public float speed = 10f;

    [Tooltip("최대 생존 시간(초). 안전장치.")]
    public float lifetime = 3f;

    [Tooltip("몇 명을 뚫고 지나갈지.")]
    public int pierce = 1;

    [Tooltip("증강 사거리 대비 비행 거리 배수.")]
    public float travelRangeMultiplier = 1.2f;

    [Tooltip("켜면 매번 시작 각도가 무작위. 끄면 항상 같은 방향에서 시작해 패턴이 고정된다.")]
    public bool randomizeStartAngle = true;

    [Tooltip("비우면 증강 데이터의 기본 투사체를 쓴다.")]
    public GameObject projectileOverride;

    [Header("발사 연출")]
    [Tooltip("발사 원점에 띄울 이펙트.")]
    public GameObject launchVfx;

    public float launchVfxScale = 1f;

    [Tooltip("발사 순간 효과음.")]
    public AudioClip launchSfx;

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        GameObject prefab = projectileOverride != null
            ? projectileOverride
            : ctx.Instance.Data.projectilePrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"[{ctx.Instance.Data.name}] 투사체 프리팹이 없습니다");
            return;
        }

        int count = projectileCount > 0 ? projectileCount : ctx.Stat.count;
        if (count <= 0) count = 1;

        Vector2 origin = ctx.Owner.position;

        VfxSpawner.SpawnAt(launchVfx, origin, launchVfxScale);
        SfxPlayer.Play(launchSfx);

        float step = 360f / count;
        float start = randomizeStartAngle ? Random.Range(0f, 360f) : 0f;

        for (int i = 0; i < count; i++)
        {
            float rad = (start + step * i) * Mathf.Deg2Rad;
            Vector2 dir = new(Mathf.Cos(rad), Mathf.Sin(rad));

            GameObject go = ProjectileSpawner.Spawn(prefab, origin);

            go.GetComponent<AugmentProjectile>()
              .Launch(dir, speed, lifetime, ctx.Stat.range * travelRangeMultiplier,
                      pierce, TargetQuery.Mask, ctx.Excluded, onHit);
        }
    }
}
