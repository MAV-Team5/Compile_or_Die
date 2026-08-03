using UnityEngine;

[System.Serializable]
public class NearestTargeting : TargetingModule
{
    public override void Resolve(AugmentContext ctx)
    {
        Transform target = TargetQuery.Nearest(ctx.Owner.position, ctx.Stat.range);

        if (target != null)
            ctx.Targets.Enemies.Add(target);
    }

}