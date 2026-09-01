using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 【적 1체】 탐색 표식이 붙은 적 중 최근접. 표식된 적이 없으면 일반 최근접으로 물러난다.
/// C 포인터 전용. 이 증강 자신은 표식을 남기지 않으므로 다른 탐색 증강(Linear Search 등)과
/// 함께 있어야 표식풀(SearchRegistry)이 채워져 있다.
/// </summary>
[System.Serializable]
[ModuleInfo("적 1체 — 표식된 적 중 최근접, 없으면 아무 적이나 최근접",
            "Nearest 와 달리 탐색풀(SearchRegistry)을 먼저 본다")]
public class NearestMarkedTargeting : TargetingModule
{
    [Tooltip("이 단계의 사거리(유닛). 0이면 시트의 사거리(range)를 쓴다.\n" +
             "하위 파이프라인 안에서는 대신 효과 범위(effectRange)를 쓴다.")]
    public float rangeOverride = 0f;

    // 매 Resolve 마다 새로 만들지 않도록 재사용. static 이라 인스턴스 상태를 두면 안 되는
    // 모듈 규칙과 별개다 — 이건 그때그때 채워 바로 소비하는 임시 버퍼일 뿐이다.
    static readonly List<Transform> markedBuffer = new();

    public override void Resolve(AugmentContext ctx)
    {
        float range = ResolveRange(ctx);
        Vector2 origin = ctx.Owner.position;

        Transform best = NearestMarked(origin, range, ctx.ChainVisited);

        // 표식된 적이 없거나 전부 사거리 밖이면 일반 최근접으로 폴백
        if (best == null)
            best = TargetQuery.Nearest(origin, range, ctx.ChainVisited);

        if (best != null)
            ctx.Targets.Add(best);
    }

    /// <summary>표식풀에서 사거리 안 최근접 1체. 제외 목록(ChainVisited)은 건너뛴다.</summary>
    Transform NearestMarked(Vector2 origin, float range, HashSet<Transform> exclude)
    {
        SearchRegistry.CollectAll(markedBuffer);

        Transform best = null;
        float bestSqr = range * range;

        for (int i = 0; i < markedBuffer.Count; i++)
        {
            Transform t = markedBuffer[i];
            if (t == null) continue;
            if (exclude != null && exclude.Contains(t)) continue;

            float sqr = ((Vector2)t.position - origin).sqrMagnitude;

            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                best = t;
            }
        }

        return best;
    }

    /// <summary>이 단계가 실제로 쓸 사거리. 전달 단계가 읽도록 기록도 남긴다.</summary>
    float ResolveRange(AugmentContext ctx)
        => ctx.EffectiveRange = rangeOverride > 0f ? rangeOverride : ctx.BaseRange;
}
