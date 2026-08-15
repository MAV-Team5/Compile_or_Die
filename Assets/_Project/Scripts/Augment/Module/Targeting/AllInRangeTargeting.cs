using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 【적 전부】 반경 안에 있으면 빠짐없이 담는다. 기본이 무제한인 것이 Random 과 다르다.
/// 부채꼴 각도를 좁히면 "가던 방향으로만" 퍼지는 DFS 전파가 된다.
/// </summary>
[System.Serializable]
[ModuleInfo("적 전부 — 반경 안 전원", "각도를 좁히면 진행 방향 부채꼴만")]
public class AllInRangeTargeting : TargetingModule
{
    [Tooltip("이 단계의 사거리(유닛). 0이면 시트의 사거리(range)를 그대로 쓴다.\n" +
             "하위 파이프라인 안에서는 대신 효과 범위(effectRange)를 쓴다.")]
    public float rangeOverride = 0f;

    [Tooltip("진행 방향 기준 좌우 각도(도). 180이면 전방위, 60이면 앞쪽 부채꼴만.\n" +
             "여기까지 오게 한 방향이 없으면(최초 발동) 각도와 무관하게 전방위로 잡는다.")]
    [Range(0f, 180f)] public float halfAngle = 180f;

    [Tooltip("안전장치. 이 수를 넘으면 잘라낸다. 0이면 시트의 수량(count)을 쓰고, 그것도 0이면 무제한.")]
    public int targetLimit = 0;

    public override void Resolve(AugmentContext ctx)
    {
        Vector2 from = ctx.Owner.position;
        List<Collider2D> hits = TargetQuery.Overlap(from, ResolveRange(ctx));

        int limit = targetLimit > 0 ? targetLimit : ctx.Stat.count;
        if (limit <= 0) limit = int.MaxValue;

        // 방향을 모르면 부채꼴을 만들 수 없으므로 전방위로 물러난다
        bool useCone = ctx.HasDirection && halfAngle < 180f;

        for (int i = 0; i < hits.Count && ctx.Targets.Count < limit; i++)
        {
            Transform t = hits[i].transform;

            if (ctx.ChainVisited.Contains(t)) continue;
            if (useCone && !InsideCone(from, t.position, ctx)) continue;

            ctx.Targets.Add(t);
        }
    }

    /// <summary>진행 방향에서 halfAngle 안쪽에 있는가.</summary>
    bool InsideCone(Vector2 from, Vector2 targetPosition, AugmentContext ctx)
    {
        Vector2 toTarget = targetPosition - from;

        // 원점과 겹친 대상은 각도를 잴 수 없다. 바로 앞으로 치고 통과시킨다
        if (toTarget.sqrMagnitude < 0.0001f) return true;

        return Vector2.Angle(ctx.Heading, toTarget) <= halfAngle;
    }

    /// <summary>이 단계가 실제로 쓸 사거리. 전달 단계가 읽도록 기록도 남긴다.</summary>
    float ResolveRange(AugmentContext ctx)
        => ctx.EffectiveRange = rangeOverride > 0f ? rangeOverride : ctx.BaseRange;
}
