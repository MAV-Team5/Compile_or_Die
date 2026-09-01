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
    [Sheet("사거리")]
    [Tooltip("이 단계의 사거리(유닛). 비워두면 시트의 사거리(range)를 쓴다.\n" +
             "배수만 주면 그 사거리에 비례한다 — 0 × 0.5 면 절반.\n" +
             "화면 밖 표식까지 잇지 않으려면 반드시 걸어둘 것.")]
    public Scalable rangeOverride = Scalable.Ratio(1f);

    [Sheet("수량")]
    [Tooltip("고를 최대 수. 0 × 1 이면 시트 그대로, 0 × 2 면 두 배.\n" +
             "시트도 0이면 반경 안 전부.")]
    public Scalable targetLimit = Scalable.Ratio(1f);

    [Detail]
    [Tooltip("켜면 가까운 순으로 고른다. 끄면 탐색풀에 등록된 순서 그대로.\n" +
             "트리를 자연스럽게 키우려면 켜두는 것이 좋다.")]
    public bool nearestFirst = true;

    [Tooltip("이만큼 못 모으면 아무것도 고르지 않는다 — 발동이 미뤄지고 쿨타임은 유지된다.\n" +
             "간선은 둘 이상이어야 이을 수 있어서 기본이 2다. 1로 두면 헛발동이 는다.")]
    public int minTargets = 2;

    [Detail]
    [Tooltip("발동할 때마다 몇 개가 걸러졌는지 콘솔에 찍는다. 튜닝이 끝나면 끌 것.\n" +
             "표식은 많은데 최종이 적으면 사거리나 개수 제한 중 하나가 자르고 있다는 뜻이다.")]
    public bool logFunnel = false;

    static readonly List<Transform> pool = new();

    public override void Resolve(AugmentContext ctx)
    {
        Vector2 from = ctx.Owner.position;
        float range = ResolveRange(ctx);

        SearchRegistry.CollectAll(pool);

        int tagged = pool.Count;

        // 반경 밖과 이미 맞은 대상을 먼저 걸러야 개수 제한이 엉뚱한 것에 쓰이지 않는다
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            Transform t = pool[i];

            bool drop = t == null
                     || ctx.ChainVisited.Contains(t)
                     || (range > 0f && ((Vector2)t.position - from).sqrMagnitude > range * range);

            if (drop) pool.RemoveAt(i);
        }

        int inRange = pool.Count;

        // 모자라면 아무것도 안 집는다. 대상이 없으면 발동이 성사되지 않아 쿨타임이 보존된다
        if (pool.Count < minTargets)
        {
            Log(ctx, tagged, inRange, 0, range, $"최소 {minTargets}개에 못 미쳐 발동을 미룸");
            return;
        }

        if (nearestFirst)
        {
            pool.Sort((a, b) =>
                ((Vector2)a.position - from).sqrMagnitude
                .CompareTo(((Vector2)b.position - from).sqrMagnitude));
        }

        int limit = targetLimit.IntOf(ctx.Stat.count, pool.Count);

        for (int i = 0; i < pool.Count && i < limit; i++)
            ctx.Targets.Add(pool[i]);

        Log(ctx, tagged, inRange, ctx.Targets.Count, range, $"상한 {limit}");
    }

    /// <summary>어디서 잘렸는지 한 줄로 보여준다. 표식이 많은데 최종이 적으면 범인이 여기 있다.</summary>
    void Log(AugmentContext ctx, int tagged, int inRange, int picked, float range, string note)
    {
        if (!logFunnel) return;

        Debug.Log($"[{ctx.Instance.Data.name}] 탐색풀 {tagged} → 사거리{range:0.#} 안 {inRange} → 선택 {picked}  ({note})");
    }

    /// <summary>이 단계가 실제로 쓸 사거리. 전달 단계가 읽도록 기록도 남긴다.</summary>
    float ResolveRange(AugmentContext ctx)
        => ctx.EffectiveRange = rangeOverride.Of(ctx.BaseRange);
}
