using UnityEngine;

/// <summary>타겟마다 투사체를 1발씩 발사한다. 좌표 타겟이면 그 방향으로 쏜다.</summary>
[System.Serializable]
public class ProjectileDelivery : DeliveryModule
{
    [Tooltip("초당 이동 거리(유닛).")]
    public float speed = 12f;

    [Tooltip("최대 생존 시간(초). 사거리보다 먼저 끝나면 여기서 사라진다. 안전장치.")]
    public float lifetime = 3f;

    [Tooltip("몇 명을 뚫고 지나갈지. 1이면 첫 적중에서 소멸.")]
    public int pierce = 1;

    [Tooltip("증강 사거리 대비 비행 거리 배수. 1.2 면 사거리의 120% 까지 날아간다.")]
    public float travelRangeMultiplier = 1.2f;

    [Tooltip("비우면 증강 데이터의 기본 투사체를 쓴다. 연쇄 단계마다 다른 투사체를 쓸 때만 지정.")]
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

        Vector2 origin = ctx.Owner.position;

        VfxSpawner.SpawnAt(launchVfx, origin, launchVfxScale);
        SfxPlayer.Play(launchSfx);

        for (int i = 0; i < ctx.Targets.Count; i++)
        {
            TargetRef target = ctx.Targets.Items[i];
            if (!target.IsAlive) continue;

            Vector2 delta = target.Position - origin;

            // 원점과 목표가 겹치면 방향이 0이 되어 제자리에 선다
            if (delta.sqrMagnitude < 0.0001f) continue;

            GameObject go = ProjectileSpawner.Spawn(prefab, origin);

            go.GetComponent<AugmentProjectile>()
              .Launch(delta.normalized, speed, lifetime,
                      ctx.Stat.range * travelRangeMultiplier,
                      pierce, TargetQuery.Mask, ctx.Excluded, onHit);
        }
    }
}
