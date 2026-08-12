using UnityEngine;

/// <summary>
/// 적중 대상에 탐색 표식을 남긴다. 직접 피해는 주지 않는다.
/// 이후 어떤 공격이든 이 적에 닿으면 표식의 추가 피해가 함께 들어간다.
/// </summary>
[System.Serializable]
public class SearchEffect : EffectModule
{
    [Tooltip("추가 피해량. 0이면 레벨 수치의 effectDamage 를 쓴다.")]
    public float bonusOverride = 0f;

    [Tooltip("켜면 비율. 0.3 이면 원래 피해의 30% 를 더한다. 끄면 고정값.")]
    public bool isPercent = false;

    [Tooltip("표식 지속 시간(초). 0이면 같은 증강이 다시 탐색할 때까지 유지.")]
    public float duration = 0f;

    [Tooltip("적 위에 붙일 표식 오브젝트. 여러 표식은 자동으로 위로 쌓인다.")]
    public GameObject markVfx;

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        MarkerHolder holder = MarkerHolder.GetOrAdd(hit.Target);
        if (holder == null) return;

        float bonus = bonusOverride > 0f ? bonusOverride : ctx.Stat.effectDamage;

        var mark = new SearchMark
        {
            Owner      = ctx.Instance,
            Bonus      = bonus,
            IsPercent  = isPercent,
            ExpireAt   = duration > 0f ? Time.time + duration : 0f,
            Visual     = markVfx != null
                ? Object.Instantiate(markVfx, hit.Target)
                : null
        };

        holder.Apply(mark);
    }
}
