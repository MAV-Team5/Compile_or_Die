using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 비행 없이 즉시 적중. 적 타겟은 그 적을, 좌표 타겟은 그 자리의 적을 때린다.
/// Stack pop · Selection Sort 계열.
/// </summary>
[System.Serializable]
public class InstantDelivery : DeliveryModule
{
    [Tooltip("좌표 타겟일 때 그 자리에서 적을 찾을 반경. 적 타겟에는 영향 없음.")]
    public float pointSearchRadius = 1f;

    [Header("적중 연출")]
    [Tooltip("적중 지점에 띄울 이펙트. 비워도 된다.")]
    public GameObject hitVfx;

    public float hitVfxScale = 1f;

    [Tooltip("적중 효과음.")]
    public AudioClip hitSfx;

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        int index = 0;

        for (int i = 0; i < ctx.Targets.Count; i++)
        {
            TargetRef target = ctx.Targets.Items[i];
            if (!target.IsAlive) continue;

            if (target.IsEnemy)
            {
                Emit(target.Transform, target.Position, ref index, onHit);
                continue;
            }

            // 좌표 타겟은 그 자리에 있는 적을 찾아서 때린다
            List<Collider2D> hits = TargetQuery.Overlap(target.Point, pointSearchRadius);

            Transform[] snapshot = new Transform[hits.Count];
            for (int h = 0; h < hits.Count; h++) snapshot[h] = hits[h].transform;

            for (int h = 0; h < snapshot.Length; h++)
            {
                if (ctx.Excluded.Contains(snapshot[h])) continue;
                Emit(snapshot[h], target.Point, ref index, onHit);
            }
        }
    }

    void Emit(Transform target, Vector2 point, ref int index, System.Action<HitInfo> onHit)
    {
        VfxSpawner.SpawnAt(hitVfx, point, hitVfxScale);
        SfxPlayer.Play(hitSfx);

        onHit(new HitInfo { Target = target, Point = point, Index = index++ });
    }
}
