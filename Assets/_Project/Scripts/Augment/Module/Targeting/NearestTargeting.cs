using UnityEngine;

/// <summary>
/// 【적 1체】 가장 가까운 하나만 고른다.
/// 여럿이 필요하면 Random, 전부가 필요하면 AllInRange.
/// 연쇄 단계에서는 이미 맞은 대상을 건너뛴다.
/// </summary>
[System.Serializable]
[ModuleInfo("적 1체 — 가장 가까운", "여럿이면 Random, 전부면 AllInRange")]
public class NearestTargeting : TargetingModule
{
    [Tooltip("이 단계의 사거리(유닛). 0이면 레벨 수치의 range 를 그대로 쓴다.\n" +
             "적을 찾는 범위이자, 뒤따르는 투사체 비행 거리·폭발 크기의 기준이 된다.")]
    public float rangeOverride = 0f;

    public override void Resolve(AugmentContext ctx)
    {
        Transform target = TargetQuery.Nearest(
            ctx.Owner.position, ResolveRange(ctx), ctx.ChainVisited);

        if (target != null)
            ctx.Targets.Add(target);
    }

    /// <summary>이 단계가 실제로 쓸 사거리. 전달 단계가 읽도록 기록도 남긴다.</summary>
    float ResolveRange(AugmentContext ctx)
        => ctx.EffectiveRange = rangeOverride > 0f ? rangeOverride : ctx.Stat.range;
}
