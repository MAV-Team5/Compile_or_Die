using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타겟 지점을 중심으로 다시 훑어서 주변까지 때린다. 대상이 늘어나는 것이 Instant 와 다르다.
/// 각도를 좁히면 부채꼴(휘두르기)이 된다. 비행이 없어 같은 프레임에 판정된다.
///
/// 본체 프리팹을 넣으면 판정 반경에 딱 맞춰 띄운다 — 근접 휘두르기·장판이 이 경우다.
/// 본체는 판정 그 자체의 그림이라 크기를 고를 수 없다. 다르게 그리면 플레이어를 속이는 것.
/// </summary>
[System.Serializable]
[ModuleInfo("타겟 지점마다 원형·부채꼴 판정", "주변까지 번진다. 타겟만 때리려면 Instant")]
public class AreaDelivery : DeliveryModule
{
    [Sheet("효과범위")]
    [Tooltip("판정 반경(유닛). 비워두면 시트의 효과 범위(effectRange)를 쓴다.\n" +
             "배수만 주면 효과 범위에 비례해 자란다 — 0 × 0.5 면 효과 범위의 절반.")]
    public Scalable blastRadius = Scalable.Ratio(1f);

    [Tooltip("향한 방향 기준 좌우 각도(도). 180이면 완전한 원, 45면 앞쪽 부채꼴(휘두르기).\n" +
             "방향을 모르면 각도와 무관하게 원으로 터진다.")]
    [Range(0f, 180f)] public float halfAngle = 180f;

    [Detail]
    [Tooltip("중심에 있는 적도 포함할지. 끄면 주변만 맞는다.")]
    public bool includeCenterTarget = true;

    [Header("본체 (선택)")]
    [Tooltip("판정을 눈에 보이게 하는 프리팹. 비우면 판정만 나가고 아무것도 안 보인다.\n" +
             "반지름 1유닛 규격으로 만들 것 — 판정 반경이 곧 배율이 된다.\n" +
             "수명은 프리팹이 스스로 관리한다(FxAutoDespawn 등).")]
    public GameObject bodyPrefab;

    [Tooltip("켜면 본체가 시전자에 붙어 따라다닌다. 걸으면서 휘두를 때 칼이 뒤로 처지지 않는다.\n" +
             "제자리에 남겨야 하는 장판이면 꺼둘 것.")]
    public bool attachBodyToOwner = false;

    [Fx("타격 연출", "판정 중심")]
    public FxGroup blastFx = new();

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        // 폭발은 "닿은 뒤 퍼지는 크기"라 사거리가 아니라 효과 범위를 따른다
        float radius = blastRadius.Of(ctx.Stat.effectRange);

        if (radius <= 0f)
        {
            ModuleWarning.Once(ctx, "Area 반경이 0이라 폭발이 안 납니다. " +
                                    "시트의 효과범위(effectRange)를 채우거나 폭발 반경을 직접 입력할 것");
            return;
        }

        Vector2 heading = Aim(ctx.Heading);

        // 방향을 모르면 부채꼴을 만들 수 없으므로 원으로 물러난다
        bool useCone = ctx.HasDirection && halfAngle < 180f;

        // 타겟 목록을 먼저 복사한다. 아래 질의가 공용 버퍼를 덮어쓰기 때문
        List<TargetRef> centers = new(ctx.Targets.Items);
        List<Transform> hits = new();

        int index = 0;

        for (int c = 0; c < centers.Count; c++)
        {
            Vector2 center = centers[c].Position;

            SpawnBody(center, radius, heading, ctx.Owner);

            // 판정 반경을 연출에도 넘긴다. 따라 커질지는 연출 쪽 체크가 정한다
            blastFx.PlayAt(center, heading, radius, ctx.Owner);

            TargetQuery.OverlapInto(center, radius, ctx.Owner, hits);

            for (int i = 0; i < hits.Count; i++)
            {
                if (!includeCenterTarget && hits[i] == centers[c].Transform) continue;

                // 폭발은 중심에서 바깥으로 퍼진 것으로 본다
                Vector2 outward = (Vector2)hits[i].position - center;
                bool hasOutward = outward.sqrMagnitude > 0.0001f;

                // 중심에 겹친 대상은 각도를 잴 수 없으니 통과시킨다
                if (useCone && hasOutward && Vector2.Angle(heading, outward) > halfAngle)
                    continue;

                onHit(new HitInfo
                {
                    Target    = hits[i],
                    Point     = center,
                    Index     = index++,
                    // 중심에 겹친 적은 퍼진 방향이 없다. 하위 파이프라인이 방향을 잃지 않게
                    // 이 폭발이 향하던 쪽을 물려준다
                    Direction = hasOutward ? outward.normalized : heading
                });
            }
        }
    }

    /// <summary>
    /// 본체를 띄운다. 연출이 아니라 판정의 그림이라 크기를 무조건 반경에 맞춘다.
    /// 방향·크기를 어떻게 반영할지는 프리팹 몫 (RotateToAim · DirectionalSprite · ISizedVisual).
    /// </summary>
    void SpawnBody(Vector2 center, float radius, Vector2 heading, Transform owner)
    {
        if (bodyPrefab == null) return;

        GameObject go = PooledSpawner.Spawn(bodyPrefab, center, PoolType.Effect);

        if (heading.sqrMagnitude > 0.0001f && go.TryGetComponent(out IDirectionalVisual aimed))
            aimed.Aim(heading.normalized);

        // 판정 각도를 그대로 넘긴다. 스프라이트에 부채꼴을 그려두면 여기가 바뀔 때 조용히 어긋난다
        if (go.TryGetComponent(out IArcVisual arc)) arc.SetArc(halfAngle);

        if (go.TryGetComponent(out ISizedVisual sized)) sized.Resize(radius);
        else go.transform.localScale *= radius;   // 반지름 1유닛 규격 프리팹의 기본 처리

        // 크기를 정한 뒤에 붙인다. Attach 가 월드 크기를 보존한다
        if (attachBodyToOwner) VfxSpawner.Attach(go, owner);
    }
}
