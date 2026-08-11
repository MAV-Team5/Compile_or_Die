using UnityEngine;

/// <summary>사거리 안에서 가장 가까운 적 1체. 연쇄 단계에서는 이미 맞은 대상을 건너뛴다.</summary>
[System.Serializable]
public class NearestTargeting : TargetingModule
{
    public override void Resolve(AugmentContext ctx)
    {
        Transform target = TargetQuery.Nearest(
            ctx.Owner.position, ctx.Stat.range, ctx.Excluded);

        if (target != null)
            ctx.Targets.Add(target);
    }
}
