using UnityEngine;

/// <summary>
/// <b>직전에 들어간 피해의 몇 %</b>를 한 번 더 준다. 간선 전이(Link)와 같은 계산이다.
///
/// <code>
/// 피해 = ctx.LastDamage × 비율
/// </code>
///
/// <b>왜 DamageEffect 가 아닌가</b> — DamageEffect 는 자기 시트의 피해량에서 출발한다.
/// Bash:kill 처럼 "연쇄가 끝난 그 피해의 30%" 를 주려면 <b>남이 만든 값</b>을 입력으로 받아야 한다.
///
/// 그래서 이 효과는 <b>혼자서는 아무 일도 못 한다.</b> 같은 적중에서 앞선 효과가
/// 피해를 준 적이 있어야 한다 — 없으면 조용히 넘어간다.
///
/// <code>
/// Bash
///   ChainEffect
///     effects[]      DamageEffect          ← 여기서 LastDamage 가 생긴다
///     finalEffects[] ExtensionEffect(KILL)
///                      └ Bash_Kill
///                          RelayDamageEffect  ← 그 값의 30%
/// </code>
/// </summary>
[System.Serializable]
[ModuleInfo("직전 피해의 몇 %를 더 준다", "앞선 효과가 피해를 줬어야 한다")]
public class RelayDamageEffect : EffectModule
{
    [Sheet("효과피해")]
    [Tooltip("직전 피해에 곱할 비율. 0.3 이면 30%.\n" +
             "비워두면 시트의 효과피해(effectDamage)를 쓴다 — 그래야 레벨을 탄다.")]
    public Scalable ratio = Scalable.Ratio(1f);

    [Tooltip("켜면 이 효과의 피해 숫자를 아래 스타일로 띄운다.")]
    public bool accentText = true;

    [Fold("숫자 스타일")]
    public DamageTextStyle textStyle = DamageTextStyle.Default;

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null) return;

        // 앞선 효과가 아직 아무것도 안 때렸다. 뗄 것이 없으므로 조용히 넘어간다
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

            // ＊ 떼어 온 값에는 하드웨어 배율이 이미 들어 있다.
            //   표시를 안 남기면 파워 배율이 두 번 곱해진다 — 간선 전이가 같은 이유로 켜둔다
            HardwareApplied = true
        };

        DamagePipeline.Process(dmg);
    }
}
