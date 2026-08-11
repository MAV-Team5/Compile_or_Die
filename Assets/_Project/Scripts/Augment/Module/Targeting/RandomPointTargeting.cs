using UnityEngine;

/// <summary>적이 아니라 빈 좌표를 노린다. Brute Force · Linear Search · Bitwise 계열.</summary>
[System.Serializable]
public class RandomPointTargeting : TargetingModule
{
    [Tooltip("탐색 반경. 0이면 증강 사거리(레벨 수치의 range)를 쓴다.")]
    public float rangeOverride = 0f;

    [Tooltip("찍을 좌표 수. 0이면 레벨 수치의 count 를 쓴다. 그것도 0이면 1곳.")]
    public int pointCount = 1;

    [Tooltip("사거리 대비 최소 거리. 0.4 면 사거리의 40% 안쪽에는 안 찍힌다. 발밑 폭격 방지용.")]
    [Range(0f, 1f)] public float minDistanceRatio = 0f;

    [Tooltip("켜면 적이 있는 자리 근처를 우선해서 찍는다. 끄면 완전 무작위.")]
    public bool preferOccupied = false;

    public override void Resolve(AugmentContext ctx)
    {
        Vector2 from = ctx.Owner.position;
        float range = Range(ctx);

        int count = pointCount > 0 ? pointCount : ctx.Stat.count;
        if (count <= 0) count = 1;

        // 적 근처를 노리는 모드면 실제 적 위치를 좌표로 삼는다
        if (preferOccupied)
        {
            var hits = TargetQuery.Overlap(from, range);

            for (int i = 0; i < hits.Count && ctx.Targets.Count < count; i++)
                ctx.Targets.Add((Vector2)hits[i].transform.position);

            if (ctx.Targets.Count >= count) return;
        }

        while (ctx.Targets.Count < count)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(range * minDistanceRatio, range);

            ctx.Targets.Add(from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist);
        }
    }

    /// <summary>오버라이드가 있으면 그걸, 없으면 증강 사거리를 쓴다.</summary>
    float Range(AugmentContext ctx) => rangeOverride > 0f ? rangeOverride : ctx.Stat.range;
}
