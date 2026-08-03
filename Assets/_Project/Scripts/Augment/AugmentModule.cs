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
    public abstract bool Evaluate(AugmentContext ctx, float deltaTime);
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
