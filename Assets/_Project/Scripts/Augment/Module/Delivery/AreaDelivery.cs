using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타겟 지점을 중심으로 다시 훑어서 주변까지 때린다. 대상이 늘어나는 것이 Instant 와 다르다.
/// 각도를 좁히면 부채꼴(휘두르기)이 된다. 비행이 없어 같은 프레임에 판정된다.
/// </summary>
[System.Serializable]
[ModuleInfo("타겟 지점마다 원형·부채꼴 폭발", "주변까지 번진다. 타겟만 때리려면 Instant")]
public class AreaDelivery : DeliveryModule
{
    [Tooltip("폭발 반경(유닛). 0이면 시트의 효과 범위(effectRange)를 쓴다 — 레벨업으로 폭발이 커진다.")]
    public float blastRadius = 2f;

    [Tooltip("향한 방향 기준 좌우 각도(도). 180이면 완전한 원, 90이면 앞쪽 부채꼴(휘두르기).\n" +
             "방향을 모르면 각도와 무관하게 원으로 터진다.")]
    [Range(0f, 180f)] public float halfAngle = 180f;

    [Tooltip("중심에 있는 적도 포함할지. 끄면 주변만 맞는다.")]
    public bool includeCenterTarget = true;

    [Fx("폭발 연출", "폭발 중심")]
    public FxGroup blastFx = new();

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        // 폭발은 "닿은 뒤 퍼지는 크기"라 사거리가 아니라 효과 범위를 따른다
        float radius = blastRadius > 0f ? blastRadius : ctx.Stat.effectRange;
        if (radius <= 0f) return;

        // 방향을 모르면 부채꼴을 만들 수 없으므로 원으로 물러난다
        bool useCone = ctx.HasDirection && halfAngle < 180f;

        // 타겟 목록을 먼저 복사한다. 아래 질의가 공용 버퍼를 덮어쓰기 때문
        List<TargetRef> centers = new(ctx.Targets.Items);
        List<Transform> hits = new();

        int index = 0;

        for (int c = 0; c < centers.Count; c++)
        {
            Vector2 center = centers[c].Position;

            blastFx.PlayAt(center, ctx.Heading);

            TargetQuery.OverlapInto(center, radius, ctx.Owner, hits);

            for (int i = 0; i < hits.Count; i++)
            {
                if (!includeCenterTarget && hits[i] == centers[c].Transform) continue;

                // 폭발은 중심에서 바깥으로 퍼진 것으로 본다
                Vector2 outward = (Vector2)hits[i].position - center;
                bool hasOutward = outward.sqrMagnitude > 0.0001f;

                // 중심에 겹친 대상은 각도를 잴 수 없으니 통과시킨다
                if (useCone && hasOutward && Vector2.Angle(ctx.Heading, outward) > halfAngle)
                    continue;

                onHit(new HitInfo
                {
                    Target    = hits[i],
                    Point     = center,
                    Index     = index++,
                    Direction = hasOutward ? outward.normalized : Vector2.zero
                });
            }
        }
    }
}
