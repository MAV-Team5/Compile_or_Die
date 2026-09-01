using UnityEngine;

/// <summary>
/// 【좌표 N곳】 적이 아니라 빈 자리를 노린다. 적이 없어도 발동한다는 것이 적 타겟팅과 다르다.
/// Brute Force · Linear Search · Bitwise 전용.
/// </summary>
[System.Serializable]
[ModuleInfo("좌표 N곳 — 빈 자리", "적이 없어도 발동한다. 적을 노리려면 Random")]
public class RandomPointTargeting : TargetingModule
{
    [Sheet("사거리")]
    [Tooltip("이 단계의 사거리(유닛). 비워두면 시트의 사거리(range)를 쓴다.\n" +
             "배수만 주면 그 사거리에 비례한다 — 0 × 0.5 면 절반.\n" +
             "하위 파이프라인 안에서는 사거리 대신 효과 범위(effectRange)를 기준으로 삼는다.")]
    public Scalable rangeOverride = Scalable.Ratio(1f);

    [Tooltip("찍을 좌표 수. 0이면 1곳.\n" +
             "시트의 수량(count)은 발사체 수 전용이라 여기서 참조하지 않는다 — " +
             "둘이 같은 값을 보면 좌표 수 × 발 수로 곱해진다.")]
    public int pointCount = 1;

    [Tooltip("반경 대비 안쪽 여백. 0.4 면 반경의 40% 안쪽에는 안 찍힌다. 발밑 폭격 방지.")]
    [Range(0f, 1f)] public float minDistanceRatio = 0f;

    public override void Resolve(AugmentContext ctx)
    {
        Vector2 from = ctx.Owner.position;
        float resolved = ResolveRange(ctx);

        int count = pointCount > 0 ? pointCount : 1;

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(resolved * minDistanceRatio, resolved);

            ctx.Targets.Add(from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist);
        }
    }

    /// <summary>이 단계가 실제로 쓸 사거리. 전달 단계가 읽도록 기록도 남긴다.</summary>
    float ResolveRange(AugmentContext ctx)
        => ctx.EffectiveRange = rangeOverride.Of(ctx.BaseRange);
}
