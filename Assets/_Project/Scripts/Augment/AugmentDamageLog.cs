using UnityEngine;

/// <summary>
/// 파이프라인을 지난 피해를 <b>기록을 원하는 증강들</b>에게 흘려준다.
///
/// <see cref="DamagePipeline"/> 에서 한 줄 부르면, 스택처럼 "내가 준 피해" 를 모으는
/// 증강이 알아서 받아간다. 연쇄든 트리든 링크 전이든 전부 파이프라인을 다시 통과하므로
/// 이 한 자리만 있으면 새로운 공격 방식이 생겨도 따라온다 —
/// <c>RunStats.Record</c> 가 증강별 집계를 한 줄로 해결하는 것과 같은 이유다.
///
/// <b>자료구조마다 static 레지스트리를 만들지 않으려고</b> 여기서 러너 목록을 훑는다.
/// 러너는 많아야 스무 개고 <see cref="AugmentInstance.TryGetShared{T}"/> 는
/// 없으면 만들지 않으므로, 스택을 안 가진 증강에는 아무 흔적도 안 남는다.
/// </summary>
public static class AugmentDamageLog
{
    /// <summary>
    /// 기록을 원하는 증강이 하나라도 있는가.
    ///
    /// <b>없으면 아무것도 안 한다.</b> 피해는 초당 수백 번 지나가는 자리라,
    /// 아무도 안 듣는데 매번 러너 목록을 훑으면 그 자체가 비용이 된다.
    /// 스택 증강을 뽑는 순간 <see cref="Enable"/> 로 켜진다.
    /// </summary>
    public static bool Listening { get; private set; }

    public static void Enable() => Listening = true;

    /// <summary>런이 다시 시작될 때 끈다. static 이라 씬을 넘어 살아남는다.</summary>
    public static void Reset() => Listening = false;

    public static void Record(DamageContext dmg)
    {
        if (!Listening) return;

        if (dmg == null || dmg.Amount <= 0f) return;

        AugmentManager manager = AugmentManager.Current;
        if (manager == null) return;

        Vector2 at = dmg.TargetTransform != null
            ? (Vector2)dmg.TargetTransform.position
            : Vector2.zero;

        var runners = manager.Runners;

        for (int i = 0; i < runners.Count; i++)
        {
            AugmentInstance inst = runners[i] != null ? runners[i].Instance : null;
            if (inst == null) continue;

            // 스택이 자기 폭발을 다시 쌓으면 오버플로우가 오버플로우를 부른다
            if (dmg.SourceAugment == inst) continue;

            if (inst.TryGetShared(out StackState stack)) stack.Push(at, dmg.Amount);
        }
    }
}
