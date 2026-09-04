using UnityEngine;

/// <summary>
/// 【적 1체】 큐의 맨 앞 — <b>가장 오래 기다린 적</b>을 꺼내 겨눈다.
/// 기다린 시간만큼 추가 피해를 <see cref="AugmentContext.AddBonus"/> 로 얹는다.
///
/// <b>이 한 줄이 FIFO 를 수치로 만든다.</b> 대기 시간이 피해가 되지 않으면
/// 앞에서 빼든 뒤에서 빼든 결과가 같아서, 큐와 스택이 화면에서 구분되지 않는다.
///
/// 꺼낸 적은 큐에서 빠진다 — 처리가 곧 퇴장이다.
/// </summary>
[System.Serializable]
[ModuleInfo("적 1체 — 큐 맨 앞", "기다린 시간만큼 더 아프다")]
public class QueueFrontTargeting : TargetingModule
{
    [Sheet("효과피해")]
    [Tooltip("1초 기다릴 때마다 더할 피해. 비워두면 시트의 효과피해(effectDamage)를 쓴다.")]
    public Scalable perSecond = Scalable.Ratio(1f);

    [Sheet("지속시간")]
    [Tooltip("대기 보너스를 세는 상한(초). 비워두면 시트의 지속시간(duration)을 쓴다.\n\n" +
             "★ 반드시 걸어둘 것 — 적이 한 마리만 남아 30초를 기다리면 피해가 폭주한다.\n" +
             "  백프레셔가 대기를 줄여주긴 하지만 그건 줄이 밀릴 때 얘기다.")]
    public Scalable maxWait = Scalable.Ratio(1f);

    [Sheet("사거리")]
    [Tooltip("이 거리 밖으로 벗어난 적은 처리하지 않고 큐에서만 뺀다.\n" +
             "0이면 거리 제한 없음 — 화면 밖까지 때리게 되니 보통은 걸어둔다.")]
    public Scalable rangeLimit = Scalable.Ratio(1f);

    public override void Resolve(AugmentContext ctx)
    {
        QueueState q = ctx.Instance.GetShared<QueueState>();

        ctx.EffectiveRange = rangeLimit.Of(ctx.Stat.range);

        if (!q.Dequeue(out QueueState.Entry entry)) return;
        if (entry.Target == null || !entry.Target.gameObject.activeInHierarchy) return;

        // 너무 멀어졌으면 처리를 포기한다. 큐에서는 이미 빠졌으므로 자리는 비워진다
        if (ctx.EffectiveRange > 0f)
        {
            float distance = Vector2.Distance(ctx.Owner.position, entry.Target.position);

            if (distance > ctx.EffectiveRange) return;
        }

        ctx.Targets.Add(entry.Target);

        float cap = maxWait.Of(ctx.Stat.duration);
        float waited = Time.time - entry.EnqueuedAt;

        if (cap > 0f) waited = Mathf.Min(waited, cap);

        float bonus = waited * perSecond.Of(ctx.Stat.effectDamage);

        if (bonus > 0f) ctx.AddBonus(bonus);
    }
}
