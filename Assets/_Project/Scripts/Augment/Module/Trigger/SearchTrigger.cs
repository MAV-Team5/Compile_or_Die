using UnityEngine;

/// <summary>
/// 쿨타임이 찬 뒤 <b>다음 탐색이 일어나면</b> 발동한다. 남의 탐색에 얹혀 가는 반응형 증강 전용.
///
/// 쿨다운 중에 일어난 탐색은 기억만 하고 넘긴다 —
/// 그래야 밀린 탐색이 쌓였다가 쿨타임이 차는 순간 즉발하지 않는다.
///
/// 탐색 증강을 하나도 안 들고 있으면 영영 발동하지 않는다. 그것이 이 증강의 성격이다.
/// </summary>
[System.Serializable]
[ModuleInfo("쿨타임 후 다음 탐색에 얹혀 발동", "혼자서는 발동하지 않는다. 탐색 증강이 있어야 한다")]
public class SearchTrigger : TriggerModule
{
    class State
    {
        public float timer;
        public int seenVersion;

        /// <summary>탐색을 감지했고 멎기를 기다리는 중.</summary>
        public bool waiting;

        /// <summary>탐색이 멎은 뒤 남은 대기 시간.</summary>
        public float settle;

        /// <summary>첫 판정을 지났는가. 상태 주머니는 증강을 얻을 때 처음 만들어진다.</summary>
        public bool started;
    }

    [Tooltip("탐색을 기다리기까지의 쿨타임 배수. 1이면 시트 쿨타임 그대로.\n" +
             "0.5면 절반 주기마다 탐색을 노린다.")]
    public float cooldownScale = 1f;

    [Tooltip("탐색이 멎고 이만큼 지나야 발동한다(초).\n" +
             "＊ 투사체로 퍼지는 탐색은 표식이 다 붙는 데 시간이 걸린다. " +
             "0으로 두면 첫 표식만 보고 발동해서 대상이 한둘밖에 안 잡힌다.\n" +
             "BFS 투사체 비행 시간보다 조금 길게 잡을 것.")]
    public float settleDelay = 0.3f;

    public override bool Evaluate(AugmentInstance instance, float deltaTime)
    {
        var s = instance.GetState<State>(this);

        float cd = instance.Stat.cooldown * (cooldownScale > 0f ? cooldownScale : 1f);

        // 얻자마자 쿨타임을 채워둔다. 이 트리거는 스스로 나가지 않고 남의 탐색에 반응하므로
        // "즉시 발동" 이 아니라 "다음 탐색을 바로 받을 수 있는 상태" 가 된다
        if (!s.started)
        {
            s.started = true;

            if (fireOnAcquire)
            {
                s.timer = cd;
                s.seenVersion = SearchRegistry.Version;
            }
        }

        // 탐색을 오래 기다려도 타이머가 무한히 자라지 않게 막는다
        s.timer = Mathf.Min(s.timer + deltaTime, cd);

        if (s.timer < cd)
        {
            // 아직 못 쓴다. 이 사이에 일어난 탐색은 흘려보낸다
            s.seenVersion = SearchRegistry.Version;
            s.waiting = false;
            return false;
        }

        int version = SearchRegistry.Version;

        // 탐색이 아직 퍼지는 중이다. 표식이 계속 늘어나므로 지금 스냅샷을 뜨면 한둘만 잡힌다
        if (version != s.seenVersion)
        {
            s.seenVersion = version;
            s.settle = settleDelay;
            s.waiting = true;

            return false;
        }

        if (!s.waiting) return false;

        // 탐색이 멎었다. 잠깐 더 기다렸다가 다 붙은 상태로 집는다
        s.settle -= deltaTime;

        return s.settle <= 0f;
    }

    public override void Consume(AugmentContext ctx)
    {
        var s = ctx.Instance.GetState<State>(this);

        s.timer = 0f;
        s.seenVersion = SearchRegistry.Version;
        s.waiting = false;

        base.Consume(ctx);
    }

    public override float Progress(AugmentInstance instance)
    {
        float cd = instance.Stat.cooldown * (cooldownScale > 0f ? cooldownScale : 1f);
        if (cd <= 0f) return 1f;

        return Mathf.Clamp01(instance.GetState<State>(this).timer / cd);
    }
}
