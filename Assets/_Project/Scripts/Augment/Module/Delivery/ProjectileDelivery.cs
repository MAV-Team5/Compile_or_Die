using UnityEngine;

/// <summary>대상마다 투사체를 1발씩 발사한다. 연쇄는 ChainEffect가 담당한다.</summary>
[System.Serializable]
public class ProjectileDelivery : DeliveryModule
{
    public float speed = 12f;
    public float lifetime = 3f;
    public int pierce = 1;

    /// <summary>사거리 대비 투사체 도달 거리 배수. 조준 후 적이 움직이는 여유분.</summary>
    public float rangeMultiplier = 1.2f;

    /// <summary>비우면 AugmentData.projectilePrefab 을 쓴다. 연쇄 단계별로 다른 투사체를 쓸 때 지정.</summary>
    public GameObject prefabOverride;

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        GameObject prefab = prefabOverride != null
            ? prefabOverride
            : ctx.Instance.Data.projectilePrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"[{ctx.Instance.Data.name}] projectilePrefab 미지정");
            return;
        }

        Vector2 origin = ctx.Owner.position;

        for (int i = 0; i < ctx.Targets.Enemies.Count; i++)
        {
            Transform target = ctx.Targets.Enemies[i];
            if (target == null || !target.gameObject.activeInHierarchy) continue;

            Vector2 delta = (Vector2)target.position - origin;

            // 원점과 대상이 겹치면 방향이 0이 되어 투사체가 제자리에 선다
            if (delta.sqrMagnitude < 0.0001f) continue;

            Vector2 dir = delta.normalized;

            GameObject go = Object.Instantiate(prefab, origin, Quaternion.identity);

            go.GetComponent<AugmentProjectile>()
              .Launch(dir, speed, lifetime, ctx.Stat.range * rangeMultiplier,
                      pierce, TargetQuery.Mask, ctx.Excluded, onHit);
        }
    }
}
