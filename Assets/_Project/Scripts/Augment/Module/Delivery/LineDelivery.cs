using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 원점에서 타겟 방향으로 직선 판정. 비행 없이 한 프레임에 선상 전체를 관통한다.
/// Java JIT 레이저 · C 포인터 계열.
/// </summary>
[System.Serializable]
public class LineDelivery : DeliveryModule
{
    [Tooltip("레이저 굵기. 이 폭 안에 걸친 적이 전부 맞는다.")]
    public float width = 0.6f;

    [Tooltip("최대 관통 수. 0이면 선상 전부.")]
    public int maxHits = 0;

    [Tooltip("증강 사거리 대비 레이저 길이 배수.")]
    public float lengthMultiplier = 1f;

    [Tooltip("켜면 타겟마다 따로 쏜다. 끄면 첫 타겟 방향으로 한 줄만.")]
    public bool beamPerTarget = false;

    [Header("발사 연출")]
    [Tooltip("레이저 연출 프리팹. BeamVisual 컴포넌트가 있으면 길이·굵기·페이드를 알아서 처리한다.")]
    public GameObject beamVfx;

    public AudioClip beamSfx;

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        Vector2 origin = ctx.Owner.position;
        float length = ctx.Stat.range * lengthMultiplier;

        if (length <= 0f || width <= 0f) return;

        // 타겟 목록을 먼저 복사한다. 아래 판정이 공용 버퍼를 덮어쓰기 때문
        List<TargetRef> targets = new(ctx.Targets.Items);
        int index = 0;

        for (int t = 0; t < targets.Count; t++)
        {
            Vector2 delta = targets[t].Position - origin;
            if (delta.sqrMagnitude < 0.0001f) continue;

            Fire(ctx, origin, delta.normalized, length, ref index, onHit);

            if (!beamPerTarget) return;
        }
    }

    void Fire(AugmentContext ctx, Vector2 origin, Vector2 dir, float length,
              ref int index, System.Action<HitInfo> onHit)
    {
        SpawnBeamVfx(origin, dir, length);
        SfxPlayer.Play(beamSfx);

        // 원점에서 dir 방향으로 뻗은 직사각형 안의 적을 한 번에 잡는다
        Vector2 center = origin + dir * (length * 0.5f);
        Vector2 size = new(width, length);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        List<Transform> ordered = new();
        TargetQuery.OverlapBoxInto(center, size, angle, ctx.Owner, ordered);

        // 가까운 순으로 관통 순서를 만든다
        ordered.Sort((a, b) =>
            ((Vector2)a.position - origin).sqrMagnitude
            .CompareTo(((Vector2)b.position - origin).sqrMagnitude));

        int limit = maxHits > 0 ? maxHits : ordered.Count;

        for (int i = 0; i < ordered.Count && i < limit; i++)
        {
            onHit(new HitInfo
            {
                Target = ordered[i],
                Point  = ordered[i].position,
                Index  = index++
            });
        }
    }

    void SpawnBeamVfx(Vector2 origin, Vector2 dir, float length)
    {
        if (beamVfx == null) return;

        GameObject go = Object.Instantiate(beamVfx, origin, Quaternion.identity);

        if (go.TryGetComponent(out BeamVisual beam))
        {
            beam.Play(origin, dir, length, width);
            return;
        }

        // BeamVisual 이 없는 프리팹도 일단 보이게는 해준다. 위치와 회전만 맞추고 짧게 정리
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        go.transform.SetPositionAndRotation(
            origin + dir * (length * 0.5f), Quaternion.Euler(0f, 0f, angle));

        Object.Destroy(go, 0.15f);
    }
}
