using UnityEngine;

/// <summary>
/// 장전했다가 연달아 쏜다. 다 쏘면 다시 장전한다.
///
/// <code>
/// 장전(쿨타임) ──▶ 탕 · 탕 · 탕 · 탕 ──▶ 장전 ──▶ ...
///                  ├─간격─┤
/// </code>
///
/// <b>매 발이 파이프라인 전체를 다시 탄다.</b> 타겟팅도 다시 돌기 때문에
/// "쏠 때마다 그 순간의 최근접을 향해" 가 저절로 나온다 —
/// 한 프레임에 여러 발을 뿌리는 <see cref="MultiShot"/> 과는 감각이 완전히 다르다.
///
/// <b>쿨타임은 마지막 발을 쏜 뒤에 시작된다.</b> 그래서 장탄을 늘려도
/// DPS 가 무한히 오르지 않고 <c>발당 피해 ÷ 간격</c> 에 점근한다 —
/// 장전식 무기가 가져야 할 성질이다.
/// </summary>
[System.Serializable]
[ModuleInfo("장전 후 연속 발동", "다 쏘면 쿨타임이 시작된다")]
public class BurstTrigger : TriggerModule
{
    /// <summary>연사 간격의 하한. 0을 허용하면 프레임 수만큼 쏘게 된다.</summary>
    const float MinInterval = 0.02f;

    [Sheet("수량")]
    [Tooltip("한 번 장전에 몇 발. 비워두면 시트의 수량(count)을 쓴다.\n\n" +
             "1이면 평범한 쿨타임 무기와 같아진다.")]
    public Scalable count = Scalable.Ratio(1f);

    [Tooltip("발 사이 간격(초).\n\n" +
             "고정값을 쓰려면 값에 0.12 처럼 적는다.\n" +
             "★ 값을 0으로 두고 배수만 주면 쿨타임에 비례한다 — 배수 0.04 면 쿨타임의 4%.\n" +
             "   그러면 쿨타임 감소 하나가 재장전과 연사를 같이 당겨준다.")]
    public Scalable interval = Scalable.Ratio(0.04f);

    /// <summary>남은 탄과 마지막 발 이후 흐른 시간. 증강 개체마다 따로 산다.</summary>
    class State
    {
        public float timer;
        public int left;
    }

    public override bool Evaluate(AugmentInstance instance, float deltaTime)
    {
        State s = instance.GetState<State>(this);

        // 발사 중 — 간격만 채우면 된다
        if (s.left > 0)
        {
            s.timer += deltaTime;
            return s.timer >= Interval(instance);
        }

        // 장전 중
        float cd = instance.Stat.cooldown;

        // 쿨타임 미입력(0)은 매 프레임 발동이 되므로 차단
        if (cd <= 0f) return false;

        s.timer = Mathf.Min(s.timer + deltaTime, cd);
        return s.timer >= cd;
    }

    public override void Consume(AugmentContext ctx)
    {
        State s = ctx.Instance.GetState<State>(this);

        // 장전이 끝난 첫 발이면 탄창을 채운다
        if (s.left <= 0) s.left = Mathf.Max(1, count.IntOf(ctx.Instance.Stat.count));

        s.left--;
        s.timer = 0f;

        // 시전 연출은 부모가 처리한다 — 발마다 울린다
        base.Consume(ctx);
    }

    /// <summary>
    /// HUD 게이지. 발사 중에는 남은 탄, 장전 중에는 채워지는 정도를 보여준다.
    /// 줄었다 다시 차는 모양이 곧 탄창이다.
    /// </summary>
    public override float Progress(AugmentInstance instance)
    {
        State s = instance.GetState<State>(this);

        if (s.left > 0)
        {
            int total = Mathf.Max(1, count.IntOf(instance.Stat.count));
            return Mathf.Clamp01((float)s.left / total);
        }

        float cd = instance.Stat.cooldown;

        return cd <= 0f ? 1f : Mathf.Clamp01(s.timer / cd);
    }

    /// <summary>배수만 준 경우 쿨타임에 비례한다. 어느 쪽이든 하한은 지킨다.</summary>
    float Interval(AugmentInstance instance)
        => Mathf.Max(MinInterval, interval.Of(instance.Stat.cooldown));
}
