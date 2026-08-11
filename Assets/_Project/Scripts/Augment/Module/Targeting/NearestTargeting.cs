using UnityEngine;

/// <summary>사거리 안 가장 가까운 1체. 연쇄 단계에서는 이미 맞은 대상을 건너뛴다.</summary>
[System.Serializable]
public class NearestTargeting : TargetingModule
{
    /// <summary>임시 진단용. 원인 잡으면 지울 것.</summary>
    public bool logTargets = true;

    public override void Resolve(AugmentContext ctx)
    {
        Vector2 from = ctx.Owner.position;

        Transform target = TargetQuery.Nearest(from, ctx.Stat.range, ctx.Excluded);

        if (logTargets)
        {
            int found = TargetQuery.Overlap(from, ctx.Stat.range).Count;

            Debug.Log(target == null
                ? $"d{ctx.Depth} 대상없음 / 원 안 {found}개 / 원점 {from} / 사거리 {ctx.Stat.range}"
                : $"d{ctx.Depth} → {target.name} / 거리 {Vector2.Distance(from, target.position):0.0} / 사거리 {ctx.Stat.range} / 원 안 {found}개");
        }

        if (target != null)
            ctx.Targets.Enemies.Add(target);
    }
}
