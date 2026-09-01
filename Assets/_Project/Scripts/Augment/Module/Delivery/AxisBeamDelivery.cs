using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타겟 좌표를 중심으로 한 직선(가로/세로) 판정. 원점(시전자)이 아니라 타겟팅이 찍은
/// 좌표 자체에서 빔이 발생한다. LineDelivery 는 "원점→타겟 방향"으로 쏘지만
/// 이건 "타겟 위치가 곧 빔의 중심"이라는 점이 다르다. Linear Search 전용.
/// </summary>
[System.Serializable]
[ModuleInfo("타겟 좌표를 중심으로 직선 관통", "원점이 아니라 타겟 위치에서 빔이 생긴다. 원점 기준이면 Line")]
public class AxisBeamDelivery : DeliveryModule
{
    [Header("레이저")]
    [Required("판정은 나가지만 레이저가 보이지 않는다")]
    [Tooltip("레이저 본체 프리팹. BeamVisual 컴포넌트가 붙어 있으면 길이·굵기·페이드를 알아서 맞춘다.")]
    public GameObject beamPrefab;

    [Tooltip("레이저 굵기(유닛). 시트와 무관한 고정값. 이 폭 안에 걸친 적이 전부 맞는다.")]
    public float width = 0.6f;

    [Tooltip("레이저 길이(유닛). 0이면 시트의 효과 범위(effectRange)를 쓴다.\n" +
             "\"닿은 뒤 퍼지는 크기\"라 사거리(range)가 아니라 효과 범위를 기준으로 삼는다.")]
    public float length = 0f;

    [Tooltip("최대 관통 수. 0이면 시트의 관통력(pierce)을 쓰고, 그것도 0이면 선상 전부.")]
    public int maxHits = 0;

    [Fx("발사 연출", "빔 중심")]
    public FxGroup fireFx = new();

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        float len = length > 0f ? length : ctx.Stat.effectRange;
        if (len <= 0f || width <= 0f) return;

        // 방향을 모르면(전방위 발동) 가로로 물러난다 — 정상 흐름에선 타겟팅이 채워준다
        Vector2 dir = ctx.HasDirection ? ctx.Heading.normalized : Vector2.right;

        // 타겟 목록을 먼저 복사한다. 아래 판정이 공용 버퍼를 덮어쓰기 때문
        List<TargetRef> targets = new(ctx.Targets.Items);
        int index = 0;

        for (int t = 0; t < targets.Count; t++)
            Fire(ctx, targets[t].Position, dir, len, ref index, onHit);
    }

    /// <summary>center 를 빔의 "중심"으로 삼아 dir 방향 직사각형 판정을 한 프레임에 낸다.</summary>
    void Fire(AugmentContext ctx, Vector2 center, Vector2 dir, float len,
              ref int index, System.Action<HitInfo> onHit)
    {
        SpawnBeam(center, dir, len);
        fireFx.PlayAt(center, dir);

        Vector2 size = new(width, len);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        List<Transform> ordered = new();
        TargetQuery.OverlapBoxInto(center, size, angle, ctx.Owner, ordered);

        // 빔 시작점(center 에서 dir 반대쪽 끝) 기준 가까운 순으로 관통 순서를 만든다
        Vector2 beamStart = center - dir * (len * 0.5f);
        ordered.Sort((a, b) =>
            ((Vector2)a.position - beamStart).sqrMagnitude
            .CompareTo(((Vector2)b.position - beamStart).sqrMagnitude));

        int limit = maxHits > 0 ? maxHits : ctx.Stat.pierce;
        if (limit <= 0) limit = ordered.Count;

        for (int i = 0; i < ordered.Count && i < limit; i++)
        {
            onHit(new HitInfo
            {
                Target    = ordered[i],
                Point     = ordered[i].position,
                Index     = index++,
                Direction = dir
            });
        }
    }

    /// <summary>레이저 본체를 띄운다. 연출이 아니라 이 모듈의 결과물이다.</summary>
    void SpawnBeam(Vector2 center, Vector2 dir, float len)
    {
        if (beamPrefab == null) return;

        Vector2 beamStart = center - dir * (len * 0.5f);
        GameObject go = Object.Instantiate(beamPrefab, center, Quaternion.identity);

        if (go.TryGetComponent(out BeamVisual beam))
        {
            beam.Play(beamStart, dir, len, width);
            return;
        }

        // BeamVisual 이 없는 프리팹도 일단 보이게는 해준다. 위치와 회전만 맞추고 짧게 정리
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        go.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, 0f, angle));

        Object.Destroy(go, 0.15f);
    }
}
