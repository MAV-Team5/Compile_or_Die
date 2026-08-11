using System.Collections.Generic;
using UnityEngine;

/// <summary>사거리 안 무작위 적. DFS · Bubble Sort · Quick Sort 계열.</summary>
[System.Serializable]
public class RandomTargeting : TargetingModule
{
    [Tooltip("탐색 반경. 0이면 증강 사거리(레벨 수치의 range)를 쓴다.")]
    public float rangeOverride = 0f;

    [Tooltip("뽑을 적의 수. 0이면 레벨 수치의 count 를 쓴다. 그것도 0이면 1체.")]
    public int pickCount = 1;

    public override void Resolve(AugmentContext ctx)
    {
        Vector2 from = ctx.Owner.position;
        List<Collider2D> hits = TargetQuery.Overlap(from, Range(ctx));

        // 제외 대상을 먼저 걸러야 무작위 추첨이 한쪽으로 쏠리지 않는다
        List<Transform> pool = new();

        for (int i = 0; i < hits.Count; i++)
        {
            Transform t = hits[i].transform;
            if (!ctx.Excluded.Contains(t)) pool.Add(t);
        }

        int want = pickCount > 0 ? pickCount : ctx.Stat.count;
        if (want <= 0) want = 1;

        for (int i = 0; i < want && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            ctx.Targets.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }

    /// <summary>오버라이드가 있으면 그걸, 없으면 증강 사거리를 쓴다.</summary>
    float Range(AugmentContext ctx) => rangeOverride > 0f ? rangeOverride : ctx.Stat.range;
}
