using UnityEngine;

/// <summary>
/// 적중 대상에 피해를 준다.
/// 최종 피해 = 시트의 피해량(damage) × damageScale + 연쇄 누적 보너스
/// </summary>
[System.Serializable]
[ModuleInfo("피해를 준다", "시트의 피해량 × damageScale")]
public class DamageEffect : EffectModule
{
    [Sheet("피해량")]
    [Tooltip("시트의 피해량(damage)에 곱하는 배율. 1이면 그대로, 0.5면 절반.\n" +
             "한 증강 안에서 효과마다 강약을 줄 때 쓴다. 추가 피해가 아니라 배율이다.")]
    public float damageScale = 1f;

    [Tooltip("켜면 이 효과의 피해 숫자를 아래 스타일로 띄운다.\n" +
             "끄면 증강 분류에 정해둔 색을 따른다 — 보통은 꺼두는 것이 통일감이 있다.")]
    public bool accentText = false;

    [Fold("숫자 스타일")]
    public DamageTextStyle textStyle = DamageTextStyle.Default;

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null) return;

        // 콜라이더가 자식에 있는 프리팹도 있어서 부모까지 훑는다
        if (!hit.Target.TryGetComponent(out IDamageReceiver receiver))
            receiver = hit.Target.GetComponentInParent<IDamageReceiver>();

        if (receiver == null) return;

        // 보너스는 배율을 안 탄다. 곱과 합을 섞으면 숫자가 어디서 왔는지 못 따라간다
        float amount = ctx.Stat.damage * damageScale + ctx.BonusDamage;

        // 표식 조회를 위해 맞은 Transform 도 같이 넘긴다
        var dmg = new DamageContext(ctx.Owner.gameObject, receiver, amount, hit.Target)
        {
            // 숫자 색을 증강 분류로 고를 수 있게 출처를 남긴다
            SourceAugment = ctx.Instance,
            StyleOverride = accentText ? textStyle : null
        };

        DamagePipeline.Process(dmg);

        // 파이프라인이 표식·하드웨어 배율까지 얹은 뒤의 값이다.
        // 간선 전이가 쓰는 기준과 같아야 내부 증강의 "그 몇 %" 가 어긋나지 않는다
        ctx.LastDamage = dmg.Amount;
    }
}
