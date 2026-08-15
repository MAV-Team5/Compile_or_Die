using UnityEngine;

/// <summary>
/// 【좌표 1곳】 향한 방향 앞. 아무것도 찾지 않으므로 주변에 적이 없어도 반드시 발동한다.
/// 최초 발동은 시전자가 바라보는 쪽, 하위 파이프라인은 날아온 쪽으로 찍는다.
/// 제자리에 찍으려면 OwnerPoint.
/// </summary>
[System.Serializable]
[ModuleInfo("좌표 1곳 — 향한 방향 앞", "적이 없어도 반드시 발동한다. 제자리는 OwnerPoint")]
public class DirectionPointTargeting : TargetingModule
{
    [Tooltip("이 단계의 사거리(유닛). 0이면 시트의 사거리(range)를 그대로 쓴다.\n" +
             "하위 파이프라인 안에서는 대신 효과 범위(effectRange)를 쓴다.")]
    public float rangeOverride = 0f;

    [Tooltip("사거리 대비 얼마나 앞에 찍을지. 0.6이면 사거리의 60% 지점.\n" +
             "근접 휘두르기는 작게, 원거리 착탄은 크게.")]
    [Range(0f, 2f)] public float distanceRatio = 0.6f;

    [Tooltip("방향을 모를 때 쓸 각도(도). 0이 오른쪽, 90이 위.\n" +
             "시전자가 방향을 안 알려주는 경우에만 쓰인다.")]
    public float fallbackAngle = 90f;

    public override void Resolve(AugmentContext ctx)
    {
        Vector2 from = ctx.Owner.position;
        float range = ResolveRange(ctx);

        Vector2 direction = ctx.HasDirection ? ctx.Heading.normalized : FallbackDirection();

        ctx.Targets.Add(from + direction * (range * distanceRatio));
    }

    Vector2 FallbackDirection()
    {
        float rad = fallbackAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    /// <summary>이 단계가 실제로 쓸 사거리. 전달 단계가 읽도록 기록도 남긴다.</summary>
    float ResolveRange(AugmentContext ctx)
        => ctx.EffectiveRange = rangeOverride > 0f ? rangeOverride : ctx.BaseRange;
}
