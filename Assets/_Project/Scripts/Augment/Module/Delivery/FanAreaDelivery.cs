using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 부채꼴 즉발 판정 + 크기가 판정을 그대로 따라가는 본체. 근접 휘두르기 전용.
/// 본체가 판정 반경과 같은 숫자로 커지는 것이 연출 크기가 고정인 Area 와 다르다.
/// </summary>
[System.Serializable]
[ModuleInfo("부채꼴 휘두르기 — 본체가 판정 크기를 따라간다", "그림이 필요 없으면 Area")]
public class FanAreaDelivery : DeliveryModule
{
    [Tooltip("판정 반경(유닛). 0이면 시트의 효과 범위(effectRange)를 쓴다 — 레벨업으로 낫이 커진다.")]
    public float blastRadius = 0f;

    [Tooltip("향한 방향 기준 좌우 각도(도). 30이면 60도 부채꼴, 60이면 120도 부채꼴.\n" +
             "본체 프리팹의 부채꼴 각도와 같은 값으로 맞출 것.")]
    [Range(0f, 180f)] public float halfAngle = 60f;

    [Header("본체")]
    [Required("판정은 나가지만 부채꼴이 보이지 않는다")]
    [Tooltip("부채꼴 본체 프리팹. 반지름 1유닛 규격으로 만들 것 — 판정 반경이 곧 배율이 된다.\n" +
             "수명은 프리팹이 스스로 관리한다(FxAutoDespawn 등).")]
    public GameObject fanPrefab;

    [Fx("적중 연출", "휘두르기 중심")]
    public FxGroup blastFx = new();

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        // 휘두르기는 "닿은 뒤 퍼지는 크기"라 Area 와 같이 효과 범위를 따른다
        float radius = blastRadius > 0f ? blastRadius : ctx.Stat.effectRange;
        if (radius <= 0f) radius = ctx.EffectiveRange;
        if (radius <= 0f) return;

        // 방향을 모르면 부채꼴을 만들 수 없으므로 원으로 물러난다
        bool useCone = ctx.HasDirection && halfAngle < 180f;

        // 타겟 목록을 먼저 복사한다. 아래 질의가 공용 버퍼를 덮어쓰기 때문 (절대 규칙 6)
        List<TargetRef> centers = new(ctx.Targets.Items);
        List<Transform> hits = new();

        int index = 0;

        for (int c = 0; c < centers.Count; c++)
        {
            Vector2 center = centers[c].Position;

            SpawnFan(center, radius, ctx.Heading);
            blastFx.PlayAt(center, ctx.Heading);

            TargetQuery.OverlapInto(center, radius, ctx.Owner, hits);

            for (int i = 0; i < hits.Count; i++)
            {
                // 부채꼴은 중심에서 바깥으로 퍼진 것으로 본다
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

    /// <summary>본체를 띄운다. 연출이 아니라 이 모듈의 결과물이다 — 판정 크기를 직접 주입한다.</summary>
    void SpawnFan(Vector2 center, float radius, Vector2 heading)
    {
        if (fanPrefab == null) return;

        GameObject go = Object.Instantiate(fanPrefab, center, Quaternion.identity);

        // 반지름 1유닛 규격 프리팹이므로 배율 = 판정 반경. 그림과 판정이 어긋날 수 없다
        go.transform.localScale *= radius;

        // 방향 처리는 프리팹 몫 (RotateToAim · DirectionalSprite 등)
        if (heading.sqrMagnitude > 0.0001f && go.TryGetComponent(out IDirectionalVisual visual))
            visual.Aim(heading.normalized);
    }
}