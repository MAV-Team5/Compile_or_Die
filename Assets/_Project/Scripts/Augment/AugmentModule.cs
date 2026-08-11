using UnityEngine;

/// <summary>
/// 모듈 조립화. 4축의 공통 부모.
/// </summary>
[System.Serializable] public abstract class AugmentModule { }


/// <summary>
/// 발동 조건 - 언제
/// CooldownTrigger
/// </summary>
[System.Serializable] public abstract class TriggerModule : AugmentModule
{
    [Header("시전 연출")]
    [Tooltip("발동이 성사된 순간 시전자 위치에 재생. 연쇄 단계에서는 직전에 맞은 적 위치.")]
    public GameObject castVfx;

    [Tooltip("이펙트 크기 배수. 1이면 프리팹 그대로.")]
    public float castVfxScale = 1f;

    [Tooltip("발동 순간 재생할 효과음.")]
    public AudioClip castSfx;

    /// <summary>발동 준비 여부. 상태를 소비하지 않는다.</summary>
    public abstract bool Evaluate(AugmentInstance instance, float deltaTime);

    /// <summary>발동 성사 시 호출. 쿨타임을 소비하고 시전 연출을 낸다.</summary>
    public virtual void Consume(AugmentContext ctx)
    {
        VfxSpawner.SpawnAt(castVfx, ctx.Owner.position, castVfxScale);
        SfxPlayer.Play(castSfx);
    }

    /// <summary>0~1 진행률. HUD 표시용.</summary>
    public virtual float Progress(AugmentInstance instance) => 1f;
}

/// <summary>
/// 목표 대상 - 누구를 / 어디를
/// Nearest · Random · AllInRange · RandomPoint · OwnerPoint
/// </summary>
[System.Serializable] public abstract class TargetingModule : AugmentModule
{
    /// <summary>대상을 찾아 ctx.Targets 에 채운다.</summary>
    public abstract void Resolve(AugmentContext ctx);
}

/// <summary>
/// 전달 방식 - 어떻게
/// Projectile · Instant · Area · Radial
/// </summary>
[System.Serializable] public abstract class DeliveryModule : AugmentModule
{
    /// <summary>전달. 적중할 때마다 onHit 호출.</summary>
    public abstract void Execute(AugmentContext ctx, System.Action<HitInfo> onHit);
}

/// <summary>
/// 적중 결과 - 무엇을
/// Damage · Knockback · Vfx · Sfx · Chain · SubPipeline
/// </summary>
[System.Serializable] public abstract class EffectModule : AugmentModule
{
    /// <summary>적중 1회 결과 적용.</summary>
    public abstract void Apply(AugmentContext ctx, HitInfo hit);
}
