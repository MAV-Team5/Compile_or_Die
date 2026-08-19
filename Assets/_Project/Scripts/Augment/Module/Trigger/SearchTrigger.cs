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
    }

    [Tooltip("탐색을 기다리기까지의 쿨타임 배수. 1이면 시트 쿨타임 그대로.\n" +
             "0.5면 절반 주기마다 탐색을 노린다.")]
    public float cooldownScale = 1f;

    public override bool Evaluate(AugmentInstance instance, float deltaTime)
    {
        var s = instance.GetState<State>(this);

        float cd = instance.Stat.cooldown * (cooldownScale > 0f ? cooldownScale : 1f);

        // 쿨타임 미입력(0)은 매 탐색마다 발동이 되므로 그대로 허용한다 —
        // "탐색이 일어날 때마다"가 의도인 증강도 있을 수 있다
        // 탐색을 오래 기다려도 타이머가 무한히 자라지 않게 막는다
        s.timer = Mathf.Min(s.timer + deltaTime, cd);

        if (s.timer < cd)
        {
            // 아직 못 쓴다. 이 사이에 일어난 탐색은 흘려보낸다
            s.seenVersion = SearchRegistry.Version;
            return false;
        }

        return SearchRegistry.Version != s.seenVersion;
    }

    public override void Consume(AugmentContext ctx)
    {
        var s = ctx.Instance.GetState<State>(this);

        s.timer = 0f;
        s.seenVersion = SearchRegistry.Version;

        base.Consume(ctx);
    }

    public override float Progress(AugmentInstance instance)
    {
        float cd = instance.Stat.cooldown * (cooldownScale > 0f ? cooldownScale : 1f);
        if (cd <= 0f) return 1f;

        return Mathf.Clamp01(instance.GetState<State>(this).timer / cd);
    }
}
