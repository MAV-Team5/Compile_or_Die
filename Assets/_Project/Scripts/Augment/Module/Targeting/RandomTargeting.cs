using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 【적 N체】 반경 안에서 무작위로 뽑는다. 뽑을 수를 정해두는 것이 AllInRange 와 다르다.
/// DFS · Bubble Sort · Quick Sort 전용.
/// </summary>
[System.Serializable]
[ModuleInfo("적 N체 — 무작위", "뽑을 수를 정해두는 것이 AllInRange 와 다르다")]
public class RandomTargeting : TargetingModule
{
    [Tooltip("이 단계의 사거리(유닛). 0이면 시트의 사거리(range)를 쓴다.\n" +
             "하위 파이프라인 안에서는 대신 효과 범위(effectRange)를 쓴다.")]
    public float rangeOverride = 0f;

    [Tooltip("뽑을 적의 수. 0이면 시트의 수량(count)을 쓰고, 그것도 0이면 1체.")]
    public int targetCount = 1;

    public override void Resolve(AugmentContext ctx)
    {
        Vector2 from = ctx.Owner.position;
        List<Collider2D> hits = TargetQuery.Overlap(from, ResolveRange(ctx));

        // 제외 대상을 먼저 걸러야 무작위 추첨이 한쪽으로 쏠리지 않는다
        List<Transform> pool = new();

        for (int i = 0; i < hits.Count; i++)
        {
            Transform t = hits[i].transform;
            if (!ctx.ChainVisited.Contains(t)) pool.Add(t);
        }

        int want = targetCount > 0 ? targetCount : ctx.Stat.count;
        if (want <= 0) want = 1;

        for (int i = 0; i < want && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            ctx.Targets.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }

    /// <summary>이 단계가 실제로 쓸 사거리. 전달 단계가 읽도록 기록도 남긴다.</summary>
    float ResolveRange(AugmentContext ctx)
        => ctx.EffectiveRange = rangeOverride > 0f ? rangeOverride : ctx.BaseRange;
}
