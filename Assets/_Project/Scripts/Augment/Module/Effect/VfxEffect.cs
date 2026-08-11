using UnityEngine;

/// <summary>적중 지점에 이펙트를 띄운다. 게임 상태는 바꾸지 않는다.</summary>
[System.Serializable]
public class VfxEffect : EffectModule
{
    /// <summary>비우면 AugmentData.vfxPrefab 을 쓴다.</summary>
    public GameObject prefabOverride;

    public float lifetime = 1f;

    /// <summary>켜면 대상을 따라다닌다. 끄면 적중 지점에 고정.</summary>
    public bool attachToTarget = false;

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        GameObject prefab = prefabOverride != null
            ? prefabOverride
            : ctx.Instance.Data.vfxPrefab;

        if (prefab == null) return;

        GameObject go = Object.Instantiate(prefab, hit.Point, Quaternion.identity);

        if (attachToTarget && hit.Target != null)
            go.transform.SetParent(hit.Target, true);

        Object.Destroy(go, lifetime);
    }
}
