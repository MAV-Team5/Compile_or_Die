using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대기열. 탐색된 적을 줄 세워두고 간격마다 맨 앞을 처리한다.
///
/// <code>
///   [인큐]     탐색 표식이 붙은 적을 뒤에 붙인다 (자리가 있으면)
///   [대기]     줄에 선 동안 시간이 쌓인다 — 이것이 곧 피해가 된다
///   [디큐]     간격마다 맨 앞을 꺼낸다
///   [백프레셔] 줄이 밀리면 처리가 빨라진다
/// </code>
///
/// <b>백프레셔가 이 증강의 성격을 정한다.</b> 적이 몰릴수록 빨리 처리하니 대기가 짧아지고,
/// 그래서 한 방이 약해진다 (리틀의 법칙: 대기열 = 도착률 × 대기시간).
/// 적이 적을 때 오래 기다린 놈이 크게 아프다 —
/// <b>스택과 정확히 반대다.</b> 스택은 몰릴수록 강해진다.
///
/// <b>탐색 증강이 없으면 아무것도 못 한다.</b> 큐는 남이 찾아둔 것을 줄 세우는 일만 한다.
/// </summary>
[System.Serializable]
[ModuleInfo("탐색된 적을 줄 세워 순서대로 처리", "오래 기다린 적일수록 아프다")]
public class QueueTrigger : TriggerModule
{
    [Sheet("수량")]
    [Tooltip("큐 최대 크기. 비워두면 시트의 수량(count)을 쓴다.\n" +
             "＊ 작을수록 대기가 짧아 한 방이 약해지고, 클수록 오래 묵혀 세진다.")]
    public Scalable capacity = Scalable.Ratio(1f);

    [Header("백프레셔")]
    [Tooltip("큐가 가득 찼을 때 디큐 간격에 곱할 값. 0.4 면 2.5배 빨라진다.\n" +
             "1이면 백프레셔 없음 — 밀려도 처리 속도가 그대로다.")]
    [Range(0.05f, 1f)] public float fullSpeedUp = 0.4f;

    class State
    {
        public float timer;
        public bool started;
    }

    static readonly List<Transform> buffer = new();

    int Capacity(AugmentInstance instance) => Mathf.Max(1, capacity.IntOf(instance.Stat.count, 8));

    /// <summary>지금 디큐 간격. 줄이 밀릴수록 짧아진다.</summary>
    float Interval(AugmentInstance instance, QueueState q)
    {
        float baseInterval = instance.Stat.cooldown;

        if (baseInterval <= 0f) return 0f;

        float fill = Mathf.Clamp01((float)q.Count / Capacity(instance));

        return baseInterval * Mathf.Lerp(1f, fullSpeedUp, fill);
    }

    public override bool Evaluate(AugmentInstance instance, float deltaTime)
    {
        QueueState q = instance.GetShared<QueueState>();
        State s = instance.GetState<State>(this);

        // 죽은 적을 먼저 치운다. 안 치우면 자리만 먹고 디큐가 헛돈다
        q.Prune();

        Enqueue(instance, q);

        float interval = Interval(instance, q);

        if (interval <= 0f) return false;

        if (!s.started)
        {
            s.started = true;
            if (fireOnAcquire) s.timer = interval;
        }

        s.timer = Mathf.Min(s.timer + deltaTime, interval);

        if (s.timer < interval) return false;

        // 줄이 비었으면 아직 때가 아니다. 타이머는 채워둔 채로 기다린다
        return q.Count > 0;
    }

    /// <summary>
    /// 탐색 표식이 붙은 적 중 아직 줄에 없는 것을 뒤에 붙인다.
    ///
    /// <b>순서는 큐가 만든다.</b> SearchRegistry 는 HashSet 이라 순서가 없어서,
    /// "발견한 순서" 가 곧 대기 순서가 된다. 같은 프레임에 우르르 들어온 것끼리는
    /// 순서가 임의지만 대기 시간이 같으므로 결과가 달라지지 않는다.
    /// </summary>
    void Enqueue(AugmentInstance instance, QueueState q)
    {
        int cap = Capacity(instance);

        if (q.Count >= cap) return;

        SearchRegistry.CollectAll(buffer);

        for (int i = 0; i < buffer.Count && q.Count < cap; i++)
            q.Enqueue(buffer[i], cap);
    }

    public override void Consume(AugmentContext ctx)
    {
        ctx.Instance.GetState<State>(this).timer = 0f;

        base.Consume(ctx);
    }

    /// <summary>HUD 게이지 — 줄이 얼마나 찼는가. 밀린 정도가 곧 위협이자 화력이다.</summary>
    public override float Progress(AugmentInstance instance)
    {
        QueueState q = instance.GetShared<QueueState>();

        return Mathf.Clamp01((float)q.Count / Capacity(instance));
    }
}
