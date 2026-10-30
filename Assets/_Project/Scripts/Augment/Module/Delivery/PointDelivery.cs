using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타겟팅이 고른 자리마다 적중 1회를 그대로 넘긴다. 적을 찾지 않는다 — 빈 자리여도 발동한다.
/// 소환 · 설치 · 함정처럼 "그 좌표" 자체가 결과물인 효과 전용.
///
/// 파이프라인은 대상 없는 적중을 버리므로 대상 칸에는 시전자를 넣고, 실제 자리는 <see cref="HitInfo.Point"/> 에 담는다.
/// 그래서 Damage 처럼 대상에게 거는 효과와 함께 쓰면 안 된다.
/// </summary>
[System.Serializable]
[ModuleInfo("찍은 자리에 그대로 내려놓는다 (적 불필요)", "소환·설치 전용. 적을 때리려면 Instant / Area")]
public class PointDelivery : DeliveryModule
{
    [Fx("설치 연출", "찍은 자리")]
    public FxGroup placeFx = new();

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        Vector2 origin = ctx.Owner.position;

        // 효과가 공용 타겟 목록을 다시 채울 수 있으므로 먼저 복사한다
        List<TargetRef> targets = new(ctx.Targets.Items);

        for (int i = 0; i < targets.Count; i++)
        {
            Vector2 point = targets[i].Position;
            Vector2 toPoint = point - origin;
            Vector2 direction = toPoint.sqrMagnitude > 0.0001f ? Aim(toPoint.normalized) : ctx.Heading;

            placeFx.PlayAt(point, direction, 0f, ctx.Owner);

            onHit(new HitInfo
            {
                Target    = ctx.Owner,
                Point     = point,
                Index     = i,
                Direction = direction
            });
        }
    }
}
