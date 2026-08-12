using UnityEngine;

/// <summary>사거리 안에서 가장 가까운 적 1체. 연쇄 단계에서는 이미 맞은 대상을 건너뛴다.</summary>
[System.Serializable]
public class NearestTargeting : TargetingModule
{
    [Tooltip("탐색 반경. 0이면 증강 사거리(레벨 수치의 range)를 쓴다.")]
    public float rangeOverride = 0f;

    public override void Resolve(AugmentContext ctx)
    {
        Transform target = TargetQuery.Nearest(
            ctx.Owner.position, Range(ctx), ctx.ChainVisited);

        if (target != null)
            ctx.Targets.Add(target);
    }

    /// <summary>오버라이드가 있으면 그걸, 없으면 증강 사거리를 쓴다.</summary>
    float Range(AugmentContext ctx) => rangeOverride > 0f ? rangeOverride : ctx.Stat.range;
}
