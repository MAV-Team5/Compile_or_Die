using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 【적 N체】 탐색 표식이 붙은 적만 고른다. 새로 찾지 않고 <b>남이 찾아둔 것을 쓴다</b>.
/// 자료구조 계열(Tree · Graph · Stack · Queue · Flood Fill) 전용.
///
/// 표식이 하나도 없으면 대상이 없어 발동 자체가 안 된다 — 그것이 이 계열의 성격이다.
/// </summary>
[System.Serializable]
[ModuleInfo("적 N체 — 표식이 붙은 적만", "새로 찾지 않고 남이 탐색해둔 것을 쓴다")]
public class SearchPoolTargeting : TargetingModule
{
    [Tooltip("이 단계의 사거리(유닛). 비워두면 시트의 사거리(range)를 쓴다.\n" +
             "배수만 주면 그 사거리에 비례한다 — 0 × 0.5 면 절반.\n" +
             "화면 밖 표식까지 잇지 않으려면 반드시 걸어둘 것.")]
    public Scalable rangeOverride = Scalable.Ratio(1f);

    [Tooltip("고를 최대 수. 0이면 시트의 수량(count)을 쓰고, 그것도 0이면 반경 안 전부.")]
    public int targetLimit = 0;

    [Tooltip("켜면 가까운 순으로 고른다. 끄면 탐색풀에 등록된 순서 그대로.\n" +
             "트리를 자연스럽게 키우려면 켜두는 것이 좋다.")]
    public bool nearestFirst = true;

    static readonly List<Transform> pool = new();

    public override void Resolve(AugmentContext ctx)
    {
        Vector2 from = ctx.Owner.position;
        float range = ResolveRange(ctx);

        SearchRegistry.CollectAll(pool);

        // 반경 밖과 이미 맞은 대상을 먼저 걸러야 개수 제한이 엉뚱한 것에 쓰이지 않는다
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            Transform t = pool[i];

            bool drop = t == null
                     || ctx.ChainVisited.Contains(t)
                     || (range > 0f && ((Vector2)t.position - from).sqrMagnitude > range * range);

            if (drop) pool.RemoveAt(i);
        }

        if (nearestFirst)
        {
            pool.Sort((a, b) =>
                ((Vector2)a.position - from).sqrMagnitude
                .CompareTo(((Vector2)b.position - from).sqrMagnitude));
        }

        int limit = targetLimit > 0 ? targetLimit : ctx.Stat.count;
        if (limit <= 0) limit = pool.Count;

        for (int i = 0; i < pool.Count && i < limit; i++)
            ctx.Targets.Add(pool[i]);
    }

    /// <summary>이 단계가 실제로 쓸 사거리. 전달 단계가 읽도록 기록도 남긴다.</summary>
    float ResolveRange(AugmentContext ctx)
        => ctx.EffectiveRange = rangeOverride.Of(ctx.BaseRange);
}
