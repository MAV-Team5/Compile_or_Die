using UnityEngine;

/// <summary>적중 시 이펙트를 띄운다. 게임 상태는 바꾸지 않는다.</summary>
[System.Serializable]
public class VfxEffect : EffectModule
{
    [Tooltip("띄울 이펙트 프리팹. 수명은 프리팹이 스스로 관리한다.")]
    public GameObject vfxPrefab;

    [Tooltip("이펙트가 나타날 위치.")]
    public VfxAnchor anchor = VfxAnchor.HitPoint;

    [Tooltip("이펙트 크기 배수. 1이면 프리팹 그대로.")]
    public float scale = 1f;

    public override void Apply(AugmentContext ctx, HitInfo hit)
        => VfxSpawner.Spawn(vfxPrefab, anchor, scale, ctx, hit);
}
