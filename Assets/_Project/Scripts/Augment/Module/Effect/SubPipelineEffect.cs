using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적중한 자리에서 다른 파이프라인을 딱 한 번 실행한다. 투사체 → 폭발 같은 전환 전용.
/// 같은 동작을 여러 단계 반복하려면 Chain 을 쓸 것.
/// </summary>
[System.Serializable]
[ModuleInfo("다른 파이프라인을 한 번만 실행", "반복하려면 Chain")]
public class SubPipelineEffect : EffectModule
{
    /// <summary>
    /// 손으로 깊게 중첩해도 여기서 멈춘다.
    /// 깊이 해석은 Chain 과 같다 — Depth 는 "지금까지 몇 번 번졌나"다.
    /// </summary>
    const int HardLimit = 8;

    [Header("설정")]
    [Tooltip("연쇄가 여기까지 쌓아온 추가 피해를 물려받을지.\n" +
             "끄면 시트 피해량만으로 시작한다 — 폭발처럼 별개의 타격으로 보이게 할 때.\n\n" +
             "하위 파이프라인이 얼마나 셀지는 그쪽 DamageEffect 의 damageScale 로 정한다.")]
    public bool inheritBonus = true;

    // ── 아래는 접어두는 중첩 파이프라인. 길어지므로 설정 밑에 둔다 ──

    [Header("적중한 자리에서 실행할 파이프라인")]
    [SerializeReference] public TargetingModule targeting;

    [SerializeReference] public List<DeliveryModule> deliveries = new();
    [SerializeReference] public List<EffectModule> effects = new();

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null) return;
        if (ctx.Depth >= HardLimit) return;

        // 적중한 적을 원점으로 삼는다. 하위 타겟팅이 여기서부터 검색한다
        var sub = new AugmentContext();
        sub.BeginChild(hit.Target, ctx, inheritBonus ? ctx.BonusDamage : 0f, hit.Direction);

        AugmentPipeline.Run(sub, targeting, deliveries, effects);
    }
}
