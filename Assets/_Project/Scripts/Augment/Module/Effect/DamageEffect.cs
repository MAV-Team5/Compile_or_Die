using UnityEngine;

/// <summary>적중 대상에 피해를 준다.</summary>
[System.Serializable]
public class DamageEffect : EffectModule
{
    /// <summary>LevelStat.damage 에 곱해지는 고정 배율. 같은 증강에서 효과별 강약을 줄 때.</summary>
    public float scale = 1f;

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        // 콜라이더가 자식에 있는 프리팹도 있어서 부모까지 훑는다
        if (!hit.Target.TryGetComponent(out IDamageReceiver receiver))
            receiver = hit.Target.GetComponentInParent<IDamageReceiver>();

        if (receiver == null) return;

        float amount = ctx.Stat.damage * scale * ctx.DamageMultiplier;

        DamagePipeline.Process(
            new DamageContext(ctx.Owner.gameObject, receiver, amount));
    }
}
