using UnityEngine;

/// <summary>
/// 【좌표 1곳】 원점 그 자리. 아무것도 찾지 않으므로 주변에 적이 없어도 반드시 발동한다.
/// 연쇄 단계에서 원점은 직전에 맞은 적이므로, 그 자리에서 다시 터지는 폭발 전용.
/// </summary>
[System.Serializable]
[ModuleInfo("좌표 1곳 — 원점 그 자리", "연쇄 단계에서는 직전에 맞은 적 위치가 된다")]
public class OwnerPointTargeting : TargetingModule
{
    [Tooltip("이 단계의 사거리(유닛). 0이면 시트의 사거리(range)를 쓴다.\n" +
             "대상을 찾는 데는 안 쓰이고, 뒤따르는 투사체·폭발의 크기 기준이 된다.\n" +
             "하위 파이프라인 안에서는 대신 효과 범위(effectRange)를 쓴다.")]
    public float rangeOverride = 0f;

    [Tooltip("원점에서 이만큼 떨어진 무작위 위치에 찍는다. 0이면 정확히 원점.")]
    public float scatterRadius = 0f;

    public override void Resolve(AugmentContext ctx)
    {
        // 대상을 찾지 않아도 기준 거리는 남겨야 전달 단계가 반경을 안다
        ctx.EffectiveRange = rangeOverride > 0f ? rangeOverride : ctx.BaseRange;

        Vector2 point = ctx.Owner.position;

        if (scatterRadius > 0f)
            point += Random.insideUnitCircle * scatterRadius;

        ctx.Targets.Add(point);
    }
}
