using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적중 시 다른 파이프라인을 딱 한 번 실행한다. 투사체 → 폭발 같은 전환용.
/// 여러 단계가 필요하면 하위 effects 에 이걸 또 넣으면 된다.
/// 같은 동작을 반복하려면 ChainEffect 를 쓸 것.
/// </summary>
[System.Serializable]
public class SubPipelineEffect : EffectModule
{
    /// <summary>손으로 깊게 중첩해도 여기서 멈춘다.</summary>
    const int HardLimit = 8;

    [Header("적중 시 실행할 파이프라인")]
    [SerializeReference] public TargetingModule targeting;
    [SerializeReference] public List<DeliveryModule> deliveries = new();
    [SerializeReference] public List<EffectModule> effects = new();

    [Tooltip("하위 파이프라인의 피해 배율. 1이면 그대로.")]
    public float damageMultiplier = 1f;

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
