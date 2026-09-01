using UnityEngine;

/// <summary>
/// 【좌표 1곳】 사거리 안 임의 위치를 찍고, 가로(±X) 또는 세로(±Y) 중 하나를 무작위로 골라
/// ctx.Heading 에 실어 넘긴다. Linear Search 전용 — AxisBeamDelivery 와 짝을 이룬다.
/// RandomPoint 와 달리 방향까지 함께 결정한다는 점이 다르다.
/// 적이 없어도 반드시 발동한다 — 좌표 타겟이므로.
/// </summary>
[System.Serializable]
[ModuleInfo("좌표 1곳 — 사거리 안 임의 위치 + 가로/세로 방향",
            "위치는 랜덤, 방향(가로·세로)은 ctx.Heading 에 실어 AxisBeamDelivery 에 넘긴다")]
public class AxisPointTargeting : TargetingModule
{
    [Tooltip("이 단계의 사거리(유닛) — 임의 위치가 나올 반경. 0이면 시트의 사거리(range)를 쓴다.\n" +
             "하위 파이프라인 안에서는 대신 효과 범위(effectRange)를 쓴다.")]
    public float rangeOverride = 0f;

    [Tooltip("반경 대비 안쪽 여백. 0.4 면 반경의 40% 안쪽에는 안 찍힌다. 발밑 발동 방지.")]
    [Range(0f, 1f)] public float minDistanceRatio = 0f;

    public override void Resolve(AugmentContext ctx)
    {
        float range = ResolveRange(ctx);
        Vector2 origin = ctx.Owner.position;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist = Random.Range(range * minDistanceRatio, range);
        Vector2 point = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

        ctx.Targets.Add(point);

        // 빔이 나갈 방향(가로/세로)을 여기서 정해 Delivery 로 실어 넘긴다
        ctx.Heading = Random.value < 0.5f ? Vector2.right : Vector2.up;
    }

    /// <summary>이 단계가 실제로 쓸 사거리. 임의 위치의 반경으로 쓰인다.</summary>
    float ResolveRange(AugmentContext ctx)
        => ctx.EffectiveRange = rangeOverride > 0f ? rangeOverride : ctx.BaseRange;
}