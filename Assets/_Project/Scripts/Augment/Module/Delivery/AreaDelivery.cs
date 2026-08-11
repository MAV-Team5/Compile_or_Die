using System.Collections.Generic;
using UnityEngine;

/// <summary>타겟 지점마다 원형 폭발. 적 타겟이든 좌표 타겟이든 그 위치를 중심으로 삼는다.</summary>
[System.Serializable]
public class AreaDelivery : DeliveryModule
{
    [Tooltip("폭발 반경. 증강 사거리와 별개로 이 폭발 자체의 크기다.")]
    public float blastRadius = 2f;

    [Tooltip("켜면 폭발 반경 대신 증강 사거리를 쓴다. 레벨업으로 폭발이 커지게 할 때.")]
    public bool useAugmentRange = false;

    [Tooltip("중심에 있는 적도 포함할지. 끄면 주변만 맞는다.")]
    public bool includeCenterTarget = true;

    [Tooltip("폭발 지점에 띄울 이펙트. 비워도 된다.")]
    public GameObject blastVfx;

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        float radius = useAugmentRange ? ctx.Stat.range : blastRadius;
        int index = 0;

        // 타겟 목록을 먼저 복사한다. 아래에서 Overlap 이 공용 버퍼를 덮어쓰기 때문
        List<TargetRef> centers = new(ctx.Targets.Items);

        for (int c = 0; c < centers.Count; c++)
        {
            Vector2 center = centers[c].Position;

            if (blastVfx != null)
                Object.Destroy(Object.Instantiate(blastVfx, center, Quaternion.identity), 2f);

            List<Collider2D> hits = TargetQuery.Overlap(center, radius);

            Transform[] snapshot = new Transform[hits.Count];
            for (int i = 0; i < hits.Count; i++) snapshot[i] = hits[i].transform;

            for (int i = 0; i < snapshot.Length; i++)
            {
                if (ctx.Excluded.Contains(snapshot[i])) continue;

                if (!includeCenterTarget && snapshot[i] == centers[c].Transform) continue;

                onHit(new HitInfo { Target = snapshot[i], Point = center, Index = index++ });
            }
        }
    }
}
