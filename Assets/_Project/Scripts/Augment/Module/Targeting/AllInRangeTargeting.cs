using System.Collections.Generic;
using UnityEngine;

/// <summary>사거리 안 적을 전부. BFS 파동 · 광역 계열.</summary>
[System.Serializable]
public class AllInRangeTargeting : TargetingModule
{
    [Tooltip("최대 몇 체까지 담을지. 0이면 레벨 수치의 count 를 쓰고, 그것도 0이면 제한 없음.")]
    public int maxTargets = 0;

    public override void Resolve(AugmentContext ctx)
    {
        Vector2 from = ctx.Owner.position;
        List<Collider2D> hits = TargetQuery.Overlap(from, ctx.Stat.range);

        int limit = maxTargets > 0 ? maxTargets : ctx.Stat.count;
        if (limit <= 0) limit = int.MaxValue;

        for (int i = 0; i < hits.Count && ctx.Targets.Count < limit; i++)
        {
            Transform t = hits[i].transform;
            if (!ctx.Excluded.Contains(t)) ctx.Targets.Add(t);
        }
    }
}
