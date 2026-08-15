using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타겟팅이 고른 대상을 그대로 때린다. 대상이 늘어나지 않는 것이 Area 와 다르다.
/// 좌표 타겟일 때만 그 자리에 선 적을 찾는다. 비행 없이 즉시 적중.
/// </summary>
[System.Serializable]
[ModuleInfo("타겟을 그대로 즉시 적중", "대상이 안 늘어난다. 주변까지 번지려면 Area")]
public class InstantDelivery : DeliveryModule
{
    [Tooltip("좌표 타겟일 때 그 자리에 선 적을 찾을 반경(유닛). 시트와 무관한 고정값.\n" +
             "'밟은 놈' 판정용이라 작게 두는 것이 보통이다 — 넓게 터뜨리려면 Area.\n" +
             "적 타겟에는 영향 없음.")]
    public float pointSearchRadius = 1f;

    [Fx("적중 연출", "적중 지점")]
    public FxGroup hitFx = new();

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        Vector2 origin = ctx.Owner.position;

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
                Emit(target.Transform, origin, target.Position, ref index, onHit);
                continue;
            }

            // 좌표 타겟은 그 자리에 있는 적을 찾아서 때린다
            TargetQuery.OverlapInto(target.Point, pointSearchRadius, ctx.Owner, hits);

            for (int h = 0; h < hits.Count; h++)
                Emit(hits[h], origin, target.Point, ref index, onHit);
        }
    }

    void Emit(Transform target, Vector2 origin, Vector2 point, ref int index,
              System.Action<HitInfo> onHit)
    {
        // 비행은 없지만 "원점에서 대상 쪽"을 진행 방향으로 본다
        Vector2 toTarget = point - origin;

        hitFx.PlayAt(point, toTarget);

        onHit(new HitInfo
        {
            Target    = target,
            Point     = point,
            Index     = index++,
            Direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.zero
        });
    }
}
