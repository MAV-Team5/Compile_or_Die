using UnityEngine;

/// <summary>
/// 적중 대상에 피해를 준다.
/// 최종 피해 = 레벨 수치의 damage × damageScale × 연쇄 증폭
/// </summary>
[System.Serializable]
[ModuleInfo("피해를 준다", "레벨 수치의 damage × damageScale")]
public class DamageEffect : EffectModule
{
    [Tooltip("레벨 수치의 damage 에 곱하는 배율. 1이면 그대로, 0.5면 절반. " +
             "한 증강 안에서 효과마다 강약을 줄 때 쓴다. 추가 피해가 아니라 배율이다.")]
    public float damageScale = 1f;

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null) return;

        // 콜라이더가 자식에 있는 프리팹도 있어서 부모까지 훑는다
        if (!hit.Target.TryGetComponent(out IDamageReceiver receiver))
            receiver = hit.Target.GetComponentInParent<IDamageReceiver>();

        if (receiver == null) return;

        float amount = ctx.Stat.damage * damageScale * ctx.DamageMultiplier;

        // 표식 조회를 위해 맞은 Transform 도 같이 넘긴다
        DamagePipeline.Process(
            new DamageContext(ctx.Owner.gameObject, receiver, amount, hit.Target));
    }
}
