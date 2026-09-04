using UnityEngine;

/// <summary>
/// 【좌표 1곳】 스택에서 맨 위 프레임을 꺼내 그 자리를 겨눈다.
/// 그때 기억해둔 피해를 <see cref="AugmentContext.AddBonus"/> 로 얹는다.
///
/// <b>한 번에 하나만 꺼낸다.</b> 편의가 아니라 구조상 그래야 한다 —
/// <c>BonusDamage</c> 는 발동 1회에 하나뿐이라, 여러 프레임을 한꺼번에 꺼내면
/// "프레임마다 다른 피해" 를 표현할 자리가 없다.
/// 되짚기가 연쇄처럼 보이는 것은 <see cref="StackTrigger"/> 가 짧은 간격으로
/// 계속 발동시켜 주기 때문이다.
///
/// 스택이 비었으면 아무것도 안 고른다 — 파이프라인이 알아서 헛발동을 접는다.
/// </summary>
[System.Serializable]
[ModuleInfo("좌표 1곳 — 스택 맨 위", "그때 기억한 피해를 함께 얹는다")]
public class StackPopTargeting : TargetingModule
{
    [Sheet("효과피해")]
    [Tooltip("기억한 피해에 곱할 비율. 0.05 면 5%.\n" +
             "비워두면 시트의 효과피해(effectDamage)를 쓴다.\n\n" +
             "★ 낮게 둘 것 — 모든 피해가 쌓이므로 사실상 '총 피해의 몇 %' 다.")]
    public Scalable ratio = Scalable.Ratio(1f);

    [Sheet("효과범위")]
    [Tooltip("이 단계의 기준 거리. 전달(Area)이 폭발 반경으로 쓴다.")]
    public Scalable rangeOverride = Scalable.Ratio(1f);

    [Fx("다음 자리 경고", "다음 프레임")]
    [Tooltip("★ <b>다음에 터질 자리</b>에 미리 띄우는 표시.\n\n" +
             "폭발이 과거 좌표에서 나기 때문에, 예고가 없으면 어디서 터지는지 모른 채\n" +
             "화면 여기저기가 번쩍이기만 한다. 한 발 앞서 점을 찍어주면\n" +
             "되짚는 길이 눈에 보이고, 파파파팍이 '순서대로' 로 읽힌다.\n\n" +
             "작고 짧게 — 폭발보다 눈에 띄면 안 된다.")]
    public FxGroup warnFx = new();

    public override void Resolve(AugmentContext ctx)
    {
        // 폭발은 "그 자리에 퍼지는 크기" 라 사거리가 아니라 효과 범위를 기준으로 삼는다
        ctx.EffectiveRange = rangeOverride.Of(ctx.Stat.effectRange);

        StackState stack = ctx.Instance.GetShared<StackState>();

        if (!stack.Pop(out StackState.Frame frame)) return;

        ctx.Targets.Add(frame.Position);

        // 다음에 터질 자리를 한 발 앞서 찍어준다. 되짚기 간격이 0.05초라
        // 경고가 폭발보다 딱 한 박자 먼저 흐르는 그림이 된다
        WarnNext(ctx, stack);

        float bonus = frame.Damage * ratio.Of(ctx.Stat.effectDamage);

        if (bonus > 0f) ctx.AddBonus(bonus);

        // 이번 오버플로우에서 얼마나 나갔는지 HUD 가 세어 보여줄 수 있게 모아둔다
        stack.BurstDamage += bonus;
    }

    /// <summary>
    /// 스택에서 <b>그다음</b> 프레임 자리에 경고를 띄운다. 꺼내지는 않는다 —
    /// 다음 발동이 그것을 pop 하면서 같은 자리에서 터진다.
    ///
    /// 평상시 pop(1초 간격)에서도 뜨는데, 그건 그것대로 "다음은 저기" 를 알려줘서
    /// 스택이 어디를 기억하고 있는지 보여주는 역할을 한다.
    /// </summary>
    void WarnNext(AugmentContext ctx, StackState stack)
    {
        if (warnFx == null || warnFx.IsEmpty) return;

        int next = stack.Frames.Count - 1;
        if (next < 0) return;

        warnFx.PlayAt(stack.Frames[next].Position, default, ctx.EffectiveRange);
    }
}
