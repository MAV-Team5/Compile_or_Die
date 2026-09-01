using UnityEngine;

/// <summary>
/// 적중 시 이펙트를 띄운다. 게임 상태는 바꾸지 않는 순수 연출 전용.
/// 전달 모듈에 딸린 연출과 달리, 적중한 대상마다 따로 나온다.
/// </summary>
[System.Serializable]
[ModuleInfo("적중 지점에 이펙트", "게임 상태는 바꾸지 않는다")]
public class VfxEffect : EffectModule
{
    [Required("아무 연출도 나오지 않는다")]
    [Tooltip("띄울 이펙트 프리팹. 수명은 프리팹이 스스로 관리한다.")]
    public GameObject vfxPrefab;

    [Tooltip("이펙트가 나타날 위치.")]
    public VfxAnchor anchor = VfxAnchor.HitPoint;

    [Tooltip("이펙트 크기 배수. 1이면 프리팹 그대로.")]
    public float scale = 1f;

    public override void Apply(AugmentContext ctx, HitInfo hit)
        => VfxSpawner.Spawn(vfxPrefab, anchor, scale, ctx, hit);
}
