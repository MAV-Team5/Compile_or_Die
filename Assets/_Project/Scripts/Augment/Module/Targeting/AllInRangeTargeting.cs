using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 【적 전부】 반경 안에 있으면 빠짐없이 담는다. 기본이 무제한인 것이 Random 과 다르다.
/// BFS 파동 · 광역 전용.
/// </summary>
[System.Serializable]
[ModuleInfo("적 전부 — 반경 안 전원", "기본이 무제한. 적이 뭉칠수록 강해진다")]
public class AllInRangeTargeting : TargetingModule
{
    [Tooltip("이 단계의 사거리(유닛). 0이면 시트의 사거리(range)를 쓴다.\n" +
             "하위 파이프라인 안에서는 대신 효과 범위(effectRange)를 쓴다.")]
    public float rangeOverride = 0f;

    [Tooltip("안전장치. 이 수를 넘으면 잘라낸다. 0이면 시트의 수량(count)을 쓰고, 그것도 0이면 무제한.")]
    public int targetLimit = 0;

    public override void Resolve(AugmentContext ctx)
    {
        Vector2 from = ctx.Owner.position;
        List<Collider2D> hits = TargetQuery.Overlap(from, ResolveRange(ctx));

        int limit = targetLimit > 0 ? targetLimit : ctx.Stat.count;
        if (limit <= 0) limit = int.MaxValue;

        for (int i = 0; i < hits.Count && ctx.Targets.Count < limit; i++)
        {
            Transform t = hits[i].transform;
            if (!ctx.ChainVisited.Contains(t)) ctx.Targets.Add(t);
        }
    }

    /// <summary>이 단계가 실제로 쓸 사거리. 전달 단계가 읽도록 기록도 남긴다.</summary>
    float ResolveRange(AugmentContext ctx)
        => ctx.EffectiveRange = rangeOverride > 0f ? rangeOverride : ctx.BaseRange;
}
