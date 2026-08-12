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

    [Header("폭발 연출")]
    [Tooltip("폭발 지점에 띄울 이펙트. 비워도 된다.")]
    public GameObject blastVfx;

    public float blastVfxScale = 1f;

    [Tooltip("폭발 효과음.")]
    public AudioClip blastSfx;

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        float radius = useAugmentRange ? ctx.Stat.range : blastRadius;
        if (radius <= 0f) return;

        // 타겟 목록을 먼저 복사한다. 아래 질의가 공용 버퍼를 덮어쓰기 때문
        List<TargetRef> centers = new(ctx.Targets.Items);
        List<Transform> hits = new();

        int index = 0;

        for (int c = 0; c < centers.Count; c++)
        {
            Vector2 center = centers[c].Position;

            VfxSpawner.SpawnAt(blastVfx, center, blastVfxScale);
            SfxPlayer.Play(blastSfx);

            TargetQuery.OverlapInto(center, radius, ctx.Owner, hits);

            for (int i = 0; i < hits.Count; i++)
            {
                if (!includeCenterTarget && hits[i] == centers[c].Transform) continue;

                onHit(new HitInfo { Target = hits[i], Point = center, Index = index++ });
            }
        }
    }
}
