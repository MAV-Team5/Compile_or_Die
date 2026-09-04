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

    [Header("while 조건")]
    [Sheet("사거리")]
    [Tooltip("이 반경 안에 적이 있으면 장전을 기다리지 않고 계속 쏜다.\n\n" +
             "＊ while(대상이 있는 동안) 반복 — 반경 안에 적을 두는 것이 곧 조작이 된다.\n" +
             "  붙으면 화력이 폭발하고 떨어지면 원래 장전 주기로 돌아간다.\n\n" +
             "★ 기본값(0 × 0 + 0)은 '안 적음' 이라 조건이 꺼져 있다.\n" +
             "   켜려면 배수를 1로 둘 것 — 0 × 1 이면 시트의 사거리를 그대로 쓴다.\n" +
             "★ 이 트리거를 들고 온 증강의 사거리를 읽는다 —\n" +
             "   내부 증강으로 갈아끼웠으면 뿌리가 아니라 그 증강의 시트다.\n" +
             "   그래서 내부 증강을 레벨업하면 반경이 자란다.\n" +
             "   짧게 잡을수록 붙어야 하는 위험이 커진다.")]
    public Scalable whileRange;

    /// <summary>남은 탄과 마지막 발 이후 흐른 시간. 증강 개체마다 따로 산다.</summary>
    class State
    {
        public float timer;
        public int left;

        /// <summary>첫 판정을 지났는가. 상태 주머니는 증강을 얻을 때 처음 만들어진다.</summary>
        public bool started;
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

        // 얻자마자 첫 장전을 건너뛴다. Consume 이 이어서 탄창을 채우므로
        // 한 발이 아니라 한 탄창이 통째로 나간다
        if (!s.started)
        {
            s.started = true;

            if (fireOnAcquire)
            {
                s.timer = cd;
                return true;
            }
        }

        s.timer = Mathf.Min(s.timer + deltaTime, cd);

        // while 조건 — 반경 안에 적이 있으면 장전을 건너뛴다.
        // 타이머를 채워두는 이유는 Progress 가 꽉 찬 게이지를 보여주게 하려는 것 (연사 중 표시)
        if (WhileHolds(instance))
        {
            s.timer = cd;
            return true;
        }

        return s.timer >= cd;
    }

    /// <summary>
    /// while 반경(월드 유닛). 아무것도 안 적었으면 0 — 조건 자체가 없다.
    ///
    /// <b>뿌리가 아니라 <see cref="AugmentInstance.TriggerStat"/> 를 읽는다.</b>
    /// while 은 내부 증강이 들고 온 조건이라 사거리도 그 증강의 시트에서 나와야 한다 —
    /// 뿌리 사거리를 쓰면 "짧은 반경 안에 붙어야 한다" 는 설계가 통째로 무너진다.
    /// 그리고 그래야 내부 증강을 레벨업할 때 반경이 자란다.
    /// </summary>
    float WhileRadius(AugmentInstance instance)
    {
        // 셋 다 0이면 "안 적었다" 라서 조건 자체가 없다.
        // ＊ 인스펙터에서 새로 만들면 기본이 이 상태다 — 켜려면 배수를 1로 둘 것
        if (whileRange.IsUntouched) return 0f;

        return whileRange.Of(instance.TriggerStat.range);
    }

    /// <summary>while 반경 안에 적이 있는가.</summary>
    bool WhileHolds(AugmentInstance instance)
    {
        if (instance.Owner == null) return false;

        float radius = WhileRadius(instance);
        if (radius <= 0f) return false;

        return TargetQuery.Nearest(instance.Owner.position, radius) != null;
    }

    /// <summary>
    /// 탄창을 새로 채우는 발이 곧 이번 회차의 첫 발이다.
    /// <see cref="Consume"/> 이 <c>left &lt;= 0</c> 일 때 탄창을 채우므로 판정 기준이 같다.
    /// </summary>
    public override bool FirstOfCycle(AugmentInstance instance)
        => instance.GetState<State>(this).left <= 0;

    /// <summary>while 반경은 플레이어가 의식해야 하는 거리다. 있으면 그려준다.</summary>
    public override float DisplayRadius(AugmentInstance instance) => WhileRadius(instance);

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
