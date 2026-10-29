using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 범위 안에서 체력이 가장 많은 적. 정렬(버블·힙)처럼 "가장 큰 값" 을 고를 때 쓴다.
/// 상자 같은 설치물과 죽는 중인 적은 뺀다.
/// </summary>
[System.Serializable]
[ModuleInfo("적 1체 — 범위 안 체력 최대", "정렬 계열의 '가장 큰 값'")]
public class HighestHealthTargeting : TargetingModule
{
    [Sheet("사거리")]
    [Tooltip("찾을 반경(유닛). 비워두면 시트의 사거리(range).\n" +
             "하위 파이프라인 안에서는 사거리 대신 효과 범위(effectRange)를 기준으로 삼는다.")]
    public Scalable rangeOverride = Scalable.Ratio(1f);

    [Tooltip("원점(직전에 맞은 적) 자신도 후보에 넣을지.\n" +
             "켜면 가장 무거운 적이 계속 앞으로 밀려가고(버블 정렬), 끄면 매번 다른 적이 뽑힌다.")]
    public bool includeOrigin = true;

    [Tooltip("보스도 후보에 넣을지.")]
    public bool includeBosses = false;

    readonly List<Transform> found = new();

    public override void Resolve(AugmentContext ctx)
    {
        float range = ctx.EffectiveRange = rangeOverride.Of(ctx.BaseRange);

        TargetQuery.OverlapInto(ctx.Owner.position, range, includeOrigin ? null : ctx.Owner, found);

        Enemy best = null;

        for (int i = 0; i < found.Count; i++)
        {
            if (found[i] == null) continue;

            Enemy enemy = found[i].GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.IsAlive || enemy.IsStationary) continue;
            if (!includeBosses && enemy.TryGetComponent(out BossMarker _)) continue;

            if (best == null || enemy.Health > best.Health) best = enemy;
        }

        if (best != null) ctx.Targets.Add(best.transform);
    }
}
