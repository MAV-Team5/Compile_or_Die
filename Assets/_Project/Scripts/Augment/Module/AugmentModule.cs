/// <summary>
/// 모듈 조립화.
/// </summary>
[System.Serializable] public abstract class AugmentModule { }


/// <summary>
/// 발동 조건 - 언제
/// CooldownTrigger, PassiveTrigger
/// </summary>
[System.Serializable] public abstract class TriggerModule : AugmentModule
{
    /// <summary>발동 준비 여부. 상태를 소비하지 않는다.</summary>
    public abstract bool Evaluate(AugmentInstance instance, float deltaTime);

    /// <summary>발동 성사 시 호출. 쿨타임을 소비한다.</summary>
    public virtual void Consume(AugmentInstance instance) { }

    /// <summary>0~1 진행률. HUD 표시용.</summary>
    public virtual float Progress(AugmentInstance instance) => 1f;
}

/// <summary>
/// 목표 대상 - 누구를 / 어디를
/// NearestTargeting, RandomTargeting, PointTargeting
/// </summary>
[System.Serializable] public abstract class TargetingModule : AugmentModule
{
    public abstract void Resolve(AugmentContext ctx);
}

/// <summary>
/// 전달 방식 - 어떻게
/// ProjectileDelivery, AreaDelivery
/// </summary>
[System.Serializable] public abstract class DeliveryModule : AugmentModule
{
    /// <summary>전달. 적중할 때마다 onHit 호출.</summary>
    public abstract void Execute(AugmentContext ctx, System.Action<HitInfo> onHit);
}

/// <summary>
/// 적중 결과 - 무엇을
/// DamageEffect, SearchEffect
/// </summary>
[System.Serializable] public abstract class EffectModule : AugmentModule
{
    /// <summary>적중 1회 결과 적용.</summary>
    public abstract void Apply(AugmentContext ctx, HitInfo hit);
}
