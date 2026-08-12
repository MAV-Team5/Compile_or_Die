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
    /// <summary>손으로 깊게 중첩해도 여기서 멈춘다.</summary>
    const int HardLimit = 8;

    [Header("설정")]
    [Tooltip("하위 파이프라인의 피해 배율. 1이면 그대로, 0.5면 절반.")]
    public float damageMultiplier = 1f;

    // ── 아래는 접어두는 중첩 파이프라인. 길어지므로 설정 밑에 둔다 ──

    [Header("적중한 자리에서 실행할 파이프라인")]
    [SerializeReference] public TargetingModule targeting;

    [SerializeReference] public List<DeliveryModule> deliveries = new();
    [SerializeReference] public List<EffectModule> effects = new();

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null) return;
        if (ctx.Depth + 1 >= HardLimit) return;

        // 적중한 적을 원점으로 삼는다. 하위 타겟팅이 여기서부터 검색한다
        var sub = new AugmentContext();
        sub.BeginChild(hit.Target, ctx, ctx.DamageMultiplier * damageMultiplier);

        AugmentPipeline.Run(sub, targeting, deliveries, effects);
    }
}
