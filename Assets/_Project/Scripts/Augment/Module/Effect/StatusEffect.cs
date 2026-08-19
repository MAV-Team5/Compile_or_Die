using UnityEngine;

/// <summary>
/// 적중 대상에 지속 효과를 건다. 지속 피해 · 둔화 같은 버프/디버프 전용.
/// 종류를 바꿔 끼우는 구조라 새 상태이상은 클래스 하나만 추가하면 된다.
///
/// 탐색 표식은 여기가 아니라 Search 모듈이다 — 전역 탐색풀에 등록되고
/// 다른 증강이 그 목록을 조회하는, 성격이 다른 물건이라 따로 산다.
/// </summary>
[System.Serializable]
[ModuleInfo("지속 효과를 건다 — 지속피해 · 둔화", "탐색 표식은 Search 로")]
public class StatusEffect : EffectModule
{
    [Tooltip("지속 시간(초). 비워두면 시트의 지속시간(duration)을 쓴다.\n" +
             "결과가 0이면 걷어낼 때까지 무기한 유지된다.")]
    public Scalable duration = Scalable.Ratio(1f);

    [Tooltip("상태의 세기. 비워두면 시트의 효과 피해(effectDamage)를 쓴다.\n" +
             "지속 피해는 틱당 피해로, 둔화는 감속 비율로 읽힌다.")]
    public Scalable magnitude = Scalable.Ratio(1f);

    [Tooltip("걸 상태의 종류. 비우면 아무 일도 일어나지 않는다.")]
    [SerializeReference] public Status status;

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (status == null || hit.Target == null) return;

        StatusHolder holder = StatusHolder.GetOrAdd(hit.Target);
        if (holder == null) return;

        holder.Apply(
            status,
            ctx.Instance,
            duration.Of(ctx.Stat.duration),
            magnitude.Of(ctx.Stat.effectDamage));
    }
}
