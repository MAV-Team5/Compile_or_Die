using UnityEngine;

[System.Serializable]
public class ProjectileDelivery : DeliveryModule
{
    public float speed = 12f;
    public float lifetime = 3f;
    public int pierce = 1;

    // 사거리 대비 투사체 도달 거리 배수 (조준 후 적이 움직이는 여유분)
    public float rangeMultiplier = 1.2f;

    public override void Execute(AugmentContext ctx, System.Action<HitInfo> onHit)
    {
        GameObject prefab = ctx.Instance.Data.projectilePrefab;

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

            Vector2 dir = ((Vector2)target.position - origin).normalized;

            GameObject go = Object.Instantiate(prefab, origin, Quaternion.identity);
            go.GetComponent<AugmentProjectile>()
              .Launch(dir, speed, lifetime, ctx.Stat.range * rangeMultiplier,
                      pierce, TargetQuery.Mask, onHit);
        }
    }
}