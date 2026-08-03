using UnityEngine;

[System.Serializable]
public class NearestTargeting : TargetingModule
{
    public float extraDelay;

    class State { public float timer; }

    public override void Resolve(AugmentContext ctx)
    {
        Transform target = TargetQuery.Nearest(ctx.Owner.position, ctx.Stat.range);

        if (target != null)
            ctx.Targets.Enemies.Add(target);
    }

}