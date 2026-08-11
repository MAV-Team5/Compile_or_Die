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
                           List<EffectModule> effects)
    {
        if (targeting == null) return false;

        targeting.Resolve(ctx);
        if (ctx.Targets.IsEmpty) return false;

        if (deliveries == null) return true;

        for (int i = 0; i < deliveries.Count; i++)
            deliveries[i]?.Execute(ctx, hit => ApplyEffects(ctx, effects, hit));

        return true;
    }

    /// <summary>적중 1회에 모든 효과를 순서대로 적용한다.</summary>
    static void ApplyEffects(AugmentContext ctx, List<EffectModule> effects, HitInfo hit)
    {
        if (effects == null || hit.Target == null) return;

        // 같은 대상을 연쇄가 다시 노리지 않도록 즉시 기록
        ctx.Excluded.Add(hit.Target);

        for (int i = 0; i < effects.Count; i++)
            effects[i]?.Apply(ctx, hit);
    }
}
