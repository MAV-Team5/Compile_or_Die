using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 비행 없이 즉시 적중. 적 타겟은 그 적을, 좌표 타겟은 그 자리의 적을 때린다.
/// Stack pop · Selection Sort 전용.
/// </summary>
[System.Serializable]
[ModuleInfo("비행 없이 즉시 적중", "좌표 타겟이면 그 자리의 적을 때린다")]
public class InstantDelivery : DeliveryModule
{
    [Tooltip("좌표 타겟일 때 그 자리에서 적을 찾을 반경(유닛). 적 타겟에는 영향 없음.")]
    public float pointSearchRadius = 1f;

    [Fx("적중 연출", "적중 지점")]
    public FxGroup hitFx = new();

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        // 타겟 목록을 먼저 복사한다. 아래 질의가 공용 버퍼를 덮어쓰기 때문
        List<TargetRef> targets = new(ctx.Targets.Items);
        List<Transform> hits = new();

        int index = 0;

        for (int i = 0; i < targets.Count; i++)
        {
            TargetRef target = targets[i];
            if (!target.IsAlive) continue;

            if (target.IsEnemy)
            {
                Emit(target.Transform, target.Position, ref index, onHit);
                continue;
            }

            // 좌표 타겟은 그 자리에 있는 적을 찾아서 때린다
            TargetQuery.OverlapInto(target.Point, pointSearchRadius, ctx.Owner, hits);

            for (int h = 0; h < hits.Count; h++)
                Emit(hits[h], target.Point, ref index, onHit);
        }
    }

    void Emit(Transform target, Vector2 point, ref int index, System.Action<HitInfo> onHit)
    {
        hitFx.PlayAt(point);

        onHit(new HitInfo { Target = target, Point = point, Index = index++ });
    }
}
