using UnityEngine;

/// <summary>
/// 타겟마다 그쪽으로 투사체를 발사한다. 좌표 타겟이면 그 방향으로 쏜다.
/// 타겟을 향해 겨눈다는 점이 각도로 뿌리는 Radial 과 다르다.
/// </summary>
[System.Serializable]
[ModuleInfo("타겟을 겨눠 투사체 발사", "각도로 뿌리려면 Radial")]
public class ProjectileDelivery : ProjectileDeliveryBase
{
    [Fold("다중 발사")]
    public MultiShot multiShot = new();

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        if (!HasPrefab(ctx)) return;

        int shots = multiShot.Resolve(ctx);

        Vector2 origin = ctx.Owner.position;
        PlayLaunch(ctx, origin);

        for (int i = 0; i < ctx.Targets.Count; i++)
        {
            TargetRef target = ctx.Targets.Items[i];
            if (!target.IsAlive) continue;

            Vector2 delta = target.Position - origin;

            // 원점과 목표가 겹치면 방향이 0이 되어 투사체가 제자리에 선다
            if (delta.sqrMagnitude < 0.0001f) continue;

            // 겨냥해서 쏘므로 유도가 쫓을 첫 대상을 그대로 넘긴다
            FireSpread(ctx, origin, Aim(delta.normalized), shots,
                       multiShot.formation, multiShot.spacing, multiShot.spreadPerShot, onHit,
                       target.Transform);
        }
    }
}
