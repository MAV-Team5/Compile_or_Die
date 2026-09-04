using UnityEngine;

/// <summary>
/// 스택 오버플로우. 준 피해를 좌표째 쌓다가 넘치면 역순으로 되짚으며 터뜨린다.
///
/// <code>
///   [채움]   쿨타임마다 맨 위 하나씩 터뜨린다 — 죽은 시간을 없애는 신호
///   [유예]   경계를 넘어도 잠깐 더 받는다      130 / 80   ← 이 구간이 곧 보상
///   [폭발]   맨 위부터 전부 역순으로 파파파팍
///   [대기]   다 쏟고 쉰다. 이 동안은 기록도 안 한다
/// </code>
///
/// <b>평상시 pop 은 딜이 아니라 신호다.</b> 5% 짜리 한 방은 기여가 거의 없지만
/// 스택은 확실히 갉아먹는다. 주기를 짧게 두면 초반 단일 빌드에서 pop 이 push 를 앞질러
/// <b>평생 오버플로우를 못 보게 된다</b> — 그래서 시트 쿨타임이 아니라 고정값을 쓴다.
///
/// <b>쿨타임 스탯은 '대기' 에 붙는다.</b> 평상시 pop 에 붙이면 쿨감을 올릴수록
/// 스택이 더 빨리 비워져서 오버플로우가 사라진다 — 강해지려고 산 것이 약하게 만든다.
/// </summary>
[System.Serializable]
[ModuleInfo("피해를 쌓다가 넘치면 역순으로 터뜨린다", "쿨타임 스탯은 폭발 뒤 대기시간이다")]
public class StackTrigger : TriggerModule
{
    [Sheet("깊이")]
    [Tooltip("스택 경계. 이만큼 쌓이면 유예에 들어간다. 비워두면 시트의 깊이(depth)를 쓴다.")]
    public Scalable capacity = Scalable.Ratio(1f);

    [Tooltip("평상시 한 개씩 터뜨리는 주기(초). <b>시트를 안 본다.</b>\n\n" +
             "★ 0.5 처럼 짧게 두지 말 것 — 초반에 pop 이 push 를 앞질러\n" +
             "  오버플로우를 한 번도 못 보게 된다. 1.0 이 하한.")]
    [Min(0.2f)] public float idlePopInterval = 1f;

    [Sheet("지속시간")]
    [Tooltip("경계를 넘은 뒤 터지기까지의 유예(초). 이 동안 더 때릴수록 보상이 커진다.\n" +
             "비워두면 시트의 지속시간(duration)을 쓴다.")]
    public Scalable graceTime = Scalable.Fixed(1f);

    [Header("폭발")]
    [Tooltip("되짚기 전체에 쓸 시간(초). ★ 프레임 수와 무관하게 고정이다.\n\n" +
             "많이 쌓였으면 간격이 짧아져 더 촘촘해진다 — 오버필이 길이가 아니라\n" +
             "밀도로 표현되어야 지겨워지지 않는다.")]
    [Min(0.2f)] public float unwindTime = 2f;

    [Tooltip("폭발 사이 최소 간격(초). 너무 짧으면 판정과 연출이 한꺼번에 몰려 진짜로 끊긴다.")]
    [Min(0.01f)] public float minPopInterval = 0.04f;

    [Fx("경계 초과 경고", "시전자")]
    [Tooltip("★ 이 증강에서 제일 중요한 연출이다.\n\n" +
             "유예가 시작될 때 한 번 울린다. \"곧 터진다\" 를 알려야 플레이어가\n" +
             "그 1초에 더 때리러 간다 — 안 알려주면 유예 구간이 그냥 지연일 뿐이다.")]
    public FxGroup graceFx = new();

    [Fx("오버플로우 시작", "시전자")]
    [Tooltip("되짚기가 시작되는 순간 한 번. 굵고 낮은 소리가 어울린다.\n" +
             "이후의 잔폭발은 castFx(시전 연출)가 발마다 울린다.")]
    public FxGroup overflowFx = new();

    int Capacity(AugmentInstance instance)
        => Mathf.Max(1, capacity.IntOf(instance.Stat.depth, 20));

    /// <summary>시전자 자리에서 연출을 낸다. 상태가 바뀌는 순간에만 부른다.</summary>
    static void PlayAtOwner(AugmentInstance instance, FxGroup fx)
    {
        if (fx == null || instance.Owner == null) return;

        fx.PlayAt(instance.Owner.position, default, 0f, instance.Owner);
    }

    float Grace(AugmentInstance instance) => Mathf.Max(0f, graceTime.Of(instance.Stat.duration));

    public override bool Evaluate(AugmentInstance instance, float deltaTime)
    {
        StackState s = instance.GetShared<StackState>();

        // 스택을 가진 증강이 생겼으니 이제 피해를 흘려받아야 한다.
        // 아무도 안 들을 때는 파이프라인이 이 일을 건너뛴다
        AugmentDamageLog.Enable();

        switch (s.Now)
        {
            case StackState.Phase.Filling:
                return TickFilling(instance, s, deltaTime);

            case StackState.Phase.Grace:
                return TickGrace(instance, s, deltaTime);

            case StackState.Phase.Unwinding:
                return TickUnwinding(s, deltaTime);

            default:
                return TickCooldown(instance, s, deltaTime);
        }
    }

    bool TickFilling(AugmentInstance instance, StackState s, float dt)
    {
        // 경계를 넘었으면 유예로. 여기서 바로 안 터뜨리는 것이 이 증강의 전부다
        if (s.Frames.Count >= Capacity(instance))
        {
            s.Now = StackState.Phase.Grace;
            s.Timer = Grace(instance);

            PlayAtOwner(instance, graceFx);

            return false;
        }

        s.Timer -= dt;

        if (s.Timer > 0f) return false;

        s.Timer = idlePopInterval;

        // 쌓인 게 없으면 터뜨릴 것도 없다
        return s.Frames.Count > 0;
    }

    bool TickGrace(AugmentInstance instance, StackState s, float dt)
    {
        s.Timer -= dt;

        if (s.Timer > 0f) return false;

        s.Now = StackState.Phase.Unwinding;
        s.BurstTotal = Mathf.Max(1, s.Frames.Count);
        s.BurstDamage = 0f;

        // 프레임이 많을수록 촘촘해진다. 길이는 그대로 두고 밀도만 올린다
        s.PopTimer = 0f;

        PlayAtOwner(instance, overflowFx);

        return false;
    }

    bool TickUnwinding(StackState s, float dt)
    {
        if (s.Frames.Count == 0)
        {
            s.Now = StackState.Phase.Cooldown;
            s.Timer = 0f;
            s.Clear();

            return false;
        }

        s.PopTimer -= dt;

        if (s.PopTimer > 0f) return false;

        s.PopTimer = Mathf.Max(minPopInterval, unwindTime / s.BurstTotal);

        return true;
    }

    bool TickCooldown(AugmentInstance instance, StackState s, float dt)
    {
        float cd = instance.Stat.cooldown;

        s.Timer += dt;

        if (s.Timer < cd) return false;

        s.Now = StackState.Phase.Filling;
        s.Timer = idlePopInterval;

        return false;
    }

    /// <summary>
    /// HUD 게이지 — 얼마나 찼는가. 유예 구간에서는 1을 넘겨 보내지 않는다.
    /// 대기 중에는 남은 시간을 보여준다.
    /// </summary>
    public override float Progress(AugmentInstance instance)
    {
        StackState s = instance.GetShared<StackState>();

        if (s.Now == StackState.Phase.Cooldown)
        {
            float cd = instance.Stat.cooldown;
            return cd <= 0f ? 1f : Mathf.Clamp01(s.Timer / cd);
        }

        return Mathf.Clamp01((float)s.Frames.Count / Capacity(instance));
    }
}
