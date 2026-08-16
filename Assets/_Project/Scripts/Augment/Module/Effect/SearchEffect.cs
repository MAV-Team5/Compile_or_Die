using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적중 대상에 탐색 표식을 남긴다. 직접 피해는 주지 않는 딜 증폭 전용.
/// 이후 어떤 공격이든 이 적에 닿으면 표식의 추가 피해가 함께 들어간다.
/// </summary>
[System.Serializable]
[ModuleInfo("탐색 표식을 남긴다", "직접 피해는 없다. 이후 모든 공격에 추가 피해가 붙는다")]
public class SearchEffect : EffectModule
{
    [Tooltip("추가 피해량. 0이면 시트의 효과 피해(effectDamage)를 쓴다.")]
    public float bonusOverride = 0f;

    [Tooltip("켜면 비율. 0.3 이면 원래 피해의 30% 를 더한다. 끄면 고정값.")]
    public bool isPercent = false;

    [Tooltip("표식 지속 시간(초). 0이면 시트의 지속시간(duration)을 쓰고, 그것도 0이면 무기한.")]
    public float durationOverride = 0f;

    [Tooltip("켜면 다시 발동할 때 이 증강의 지난 표식을 전부 해제한다. 끄면 표식이 계속 쌓인다.\n" +
             "판정은 증강 단위다 — 한 증강 안에 Search 가 여럿이어도 발동당 한 번만 해제한다.")]
    public bool releaseOnRefire = true;

    [Tooltip("적 위에 붙일 표식 오브젝트. 여러 표식은 자동으로 위로 쌓인다.\n" +
             "위치와 크기는 적 프리팹의 MarkAnchor 가 정한다.\n" +
             "비워도 추가 피해는 그대로 들어간다 — 눈에 안 보일 뿐이다.")]
    public GameObject markVfx;

    [Fx("표식 발동 연출", "표식 위치")]
    public FxGroup burstFx = new();

    [Tooltip("표식 발동 연출의 최소 간격(초). 빠른 공격에 도배되는 것을 막는다.\n" +
             "표식 자체는 사라지지 않는다 — 연출만 걸러진다.")]
    public float burstInterval = 0.1f;

    static readonly List<Transform> releaseBuffer = new();

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        MarkerHolder holder = MarkerHolder.GetOrAdd(hit.Target);
        if (holder == null) return;

        // 이번 발동의 첫 표식이면 지난 발동의 표식부터 걷어낸다.
        // 모듈이 아니라 증강 단위로 재야 같은 발동의 Search 끼리 서로 지우지 않는다
        if (ctx.Instance.LastSearchFiringId != ctx.FiringId)
        {
            ctx.Instance.LastSearchFiringId = ctx.FiringId;

            if (releaseOnRefire) ReleasePrevious(ctx.Instance);
        }

        float bonus = bonusOverride > 0f ? bonusOverride : ctx.Stat.effectDamage;
        float life = durationOverride > 0f ? durationOverride : ctx.Stat.duration;

        // 표식 오브젝트 생성은 MarkerHolder 가 맡는다. 적 프리팹의 MarkAnchor 를 알아야 하므로
        holder.Apply(new SearchMark
        {
            Owner         = ctx.Instance,
            Bonus         = bonus,
            IsPercent     = isPercent,
            ExpireAt      = life > 0f ? Time.time + life : 0f,
            VisualPrefab  = markVfx,
            BurstFx       = burstFx,
            BurstInterval = burstInterval
        });
    }

    /// <summary>이 증강이 남긴 표식을 전부 해제한다.</summary>
    static void ReleasePrevious(AugmentInstance owner)
    {
        // 해제하면 레지스트리가 바뀌므로 먼저 복사해두고 순회한다
        SearchRegistry.CollectBy(owner, releaseBuffer);

        for (int i = 0; i < releaseBuffer.Count; i++)
            if (releaseBuffer[i] != null &&
                releaseBuffer[i].TryGetComponent(out MarkerHolder holder))
                holder.RemoveByOwner(owner);
    }
}
