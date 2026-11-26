using System.Collections.Generic;

/// <summary>
/// 타겟팅 → 전달 → 효과 3축 실행. Runner와 ChainEffect가 공유한다.
/// MonoBehaviour가 아니므로 소환물·연쇄 어디서든 재사용 가능하다.
/// </summary>
public static class AugmentPipeline
{
    /// <summary>대상을 찾아 전달까지 실행한다. 대상이 없으면 false.</summary>
    public static bool Run(AugmentContext ctx,
                           TargetingModule targeting,
                           List<DeliveryModule> deliveries,
                           List<EffectModule> effects,
                           Dictionary<object, AugmentInstance> credits = null)
    {
        if (targeting == null) return false;

        targeting.Resolve(ctx);
        if (ctx.Targets.IsEmpty) return false;

        if (deliveries == null) return true;

        for (int i = 0; i < deliveries.Count; i++)
        {
            DeliveryModule delivery = deliveries[i];
            if (delivery == null) continue;

            AugmentInstance deliveryCredit = CreditOf(credits, delivery);
            using (AugmentTint.Scope(DataOf(ctx)))
                delivery.Execute(ctx, hit => ApplyEffects(ctx, effects, hit, credits, deliveryCredit));
        }

        return true;
    }

    /// <summary>
    /// 효과마다 통계 주인을 정해 둔다. 투사체는 나중에 맞으므로 맞는 이 순간에 켜고 끈다.
    /// 우선순위: 내부 증강이 덧붙인 효과 → 내부 증강이 덧붙인 전달로 맞은 적중 → 바깥에서 물려받은 주인.
    /// </summary>
    static void ApplyEffects(AugmentContext ctx, List<EffectModule> effects, HitInfo hit,
                             Dictionary<object, AugmentInstance> credits, AugmentInstance deliveryCredit)
    {
        if (effects == null || hit.Target == null) return;

        // 같은 대상을 연쇄가 다시 노리지 않도록 즉시 기록
        ctx.ChainVisited.Add(hit.Target);

        // 연쇄·하위 파이프라인의 적중이면 앞 대상에서 이 대상으로 네온선을 긋는다 — 번진 길이 보이게
        if (ctx.Depth > 0 && ctx.Owner != null && ctx.Owner != hit.Target && ctx.Instance != null)
            NeonFlash.Hop(ctx.Instance.Data, ctx.Owner.position, hit.Target.position, ctx.Depth);

        AugmentInstance outer = ctx.Credit;

        // 투사체는 나중 프레임에 맞으므로 여기서 다시 켠다 — 맞을 때 나오는 이펙트도 같은 색
        using var tint = AugmentTint.Scope(DataOf(ctx));

        for (int i = 0; i < effects.Count; i++)
        {
            EffectModule effect = effects[i];
            if (effect == null) continue;

            ctx.Credit = CreditOf(credits, effect) ?? deliveryCredit ?? outer;
            effect.Apply(ctx, hit);
        }

        ctx.Credit = outer;
    }

    static AugmentData DataOf(AugmentContext ctx) => ctx != null && ctx.Instance != null ? ctx.Instance.Data : null;

    static AugmentInstance CreditOf(Dictionary<object, AugmentInstance> credits, object module)
        => credits != null && credits.TryGetValue(module, out AugmentInstance who) ? who : null;
}
