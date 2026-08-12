using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타겟 지점마다 원형 폭발. 적 타겟이든 좌표 타겟이든 그 위치를 중심으로 삼는다.
/// 비행이 없으므로 같은 프레임에 즉시 판정된다.
/// </summary>
[System.Serializable]
[ModuleInfo("타겟 지점마다 원형 폭발", "비행이 없어 같은 프레임에 판정된다")]
public class AreaDelivery : DeliveryModule
{
    [Tooltip("폭발 반경(유닛). 0이면 시트의 효과 범위(effectRange)를 쓴다 — 레벨업으로 폭발이 커진다.")]
    public float blastRadius = 2f;

    [Tooltip("중심에 있는 적도 포함할지. 끄면 주변만 맞는다.")]
    public bool includeCenterTarget = true;

    [Fx("폭발 연출", "폭발 중심")]
    public FxGroup blastFx = new();

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        // 폭발은 "닿은 뒤 퍼지는 크기"라 사거리가 아니라 효과 범위를 따른다
        float radius = blastRadius > 0f ? blastRadius : ctx.Stat.effectRange;
        if (radius <= 0f) return;

        // 타겟 목록을 먼저 복사한다. 아래 질의가 공용 버퍼를 덮어쓰기 때문
        List<TargetRef> centers = new(ctx.Targets.Items);
        List<Transform> hits = new();

        int index = 0;

        for (int c = 0; c < centers.Count; c++)
        {
            Vector2 center = centers[c].Position;

            blastFx.PlayAt(center);

            TargetQuery.OverlapInto(center, radius, ctx.Owner, hits);

            for (int i = 0; i < hits.Count; i++)
            {
                if (!includeCenterTarget && hits[i] == centers[c].Transform) continue;

                onHit(new HitInfo { Target = hits[i], Point = center, Index = index++ });
            }
        }
    }
}
