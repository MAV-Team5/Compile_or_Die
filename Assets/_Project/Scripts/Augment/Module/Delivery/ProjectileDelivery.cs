using UnityEngine;

/// <summary>타겟마다 투사체를 발사한다. 좌표 타겟이면 그 방향으로 쏜다.</summary>
[System.Serializable]
public class ProjectileDelivery : ProjectileDeliveryBase
{
    [Header("다중 발사")]
    [Tooltip("타겟 1명당 몇 발. 0이면 레벨 수치의 count 를 쓴다.")]
    public int shotsPerTarget = 1;

    [Tooltip("여러 발을 어떻게 배치할지. 나란히 또는 줄줄이.")]
    public ShotFormation formation = ShotFormation.Parallel;

    [Tooltip("발 사이 간격(유닛). 0이면 한 자리에서 겹쳐 나간다.")]
    public float shotSpacing = 0.4f;

    [Tooltip("발 사이 각도(도). 0이면 완전히 평행, 값을 주면 부채꼴로 퍼진다.")]
    public float spreadPerShot = 0f;

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        if (!HasPrefab(ctx)) return;

        int shots = shotsPerTarget > 0 ? shotsPerTarget : ctx.Stat.count;
        if (shots <= 0) shots = 1;

        Vector2 origin = ctx.Owner.position;
        PlayLaunch(origin);

        for (int i = 0; i < ctx.Targets.Count; i++)
        {
            TargetRef target = ctx.Targets.Items[i];
            if (!target.IsAlive) continue;

            Vector2 delta = target.Position - origin;

            // 원점과 목표가 겹치면 방향이 0이 되어 투사체가 제자리에 선다
            if (delta.sqrMagnitude < 0.0001f) continue;

            FireSpread(ctx, origin, delta.normalized,
                       shots, formation, shotSpacing, spreadPerShot, onHit);
        }
    }
}
