using UnityEngine;

/// <summary>
/// <b>직전에 들어간 피해의 몇 %</b> 를 한 번 더 준다. 고정 피해가 아니라 <b>비율</b>이다.
///
/// <code>
///   Bash 연쇄가 4단계까지 번져 마지막에 47 이 들어갔다
///   → Bash:kill (30%) → 14 추가
/// </code>
///
/// <b>왜 DamageEffect 로는 안 되나</b> — DamageEffect 는 시트의 피해량을 쓴다.
/// 연쇄로 계속 불어난 값이 아니라 "1타 기본값" 이라서, 깊이 4까지 번져도 추가 피해가 그대로다.
/// 연쇄의 끝을 보상하려면 <see cref="AugmentContext.LastDamage"/> 를 기준으로 삼아야 한다.
///
/// 간선 전이(<c>LinkHolder.Propagate</c>)와 같은 기준을 쓴다 —
/// 표식 추가피해와 하드웨어 배율이 다 반영된 뒤의 값이다.
/// </summary>
[System.Serializable]
[ModuleInfo("직전 피해의 몇 % 를 한 번 더", "연쇄로 불어난 값을 기준으로 삼는다")]
public class RelayDamageEffect : EffectModule
{
    [Sheet("효과피해")]
    [Tooltip("직전 피해에 곱할 비율. 0.3 이면 30%.\n" +
             "비워두면 시트의 효과피해(effectDamage)를 쓴다 — 레벨업하면 같이 자란다.")]
    public Scalable ratio = Scalable.Ratio(1f);

    [Tooltip("켜면 이 피해의 숫자를 아래 스타일로 띄운다.\n" +
             "＊ 켜두는 것을 권한다 — 같은 적에게 두 숫자가 겹쳐 뜨는데,\n" +
             "  색이 같으면 왜 두 번 뜨는지 알 수 없다.")]
    public bool accentText = true;

    [Fold("숫자 스타일")]
    public DamageTextStyle textStyle = DamageTextStyle.Default;

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null) return;

        // 아직 아무 피해도 안 들어갔다. 이 효과는 뒤에 오는 것이므로 앞이 비면 할 일이 없다
        if (ctx.LastDamage <= 0f) return;

        float amount = ctx.LastDamage * ratio.Of(ctx.Stat.effectDamage);
        if (amount <= 0f) return;

        // 콜라이더가 자식에 있는 프리팹도 있어서 부모까지 훑는다
        if (!hit.Target.TryGetComponent(out IDamageReceiver receiver))
            receiver = hit.Target.GetComponentInParent<IDamageReceiver>();

        if (receiver == null) return;

        var dmg = new DamageContext(ctx.Owner.gameObject, receiver, amount, hit.Target)
        {
            SourceAugment = ctx.Instance,
            StyleOverride = accentText ? textStyle : null,

            // ★ 이미 배율이 곱해진 값에서 떼어낸 것이다. 여기서 또 곱하면 파워가 두 번 먹는다
            HardwareApplied = true
        };

        DamagePipeline.Process(dmg);

        ctx.LastDamage = dmg.Amount;
    }
}
