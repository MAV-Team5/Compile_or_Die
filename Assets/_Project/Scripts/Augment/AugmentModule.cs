/// <summary>
/// 모듈 조립화.
/// </summary>
[System.Serializable] public abstract class AugmentModule {}
/// <summary>
/// 발동조건 모듈, 
/// CooldownTrigger, PassiveTrigger
/// </summary>
[System.Serializable] public abstract class TriggerModule   : AugmentModule { }
/// <summary>
/// 타겟대상 모듈,
/// NearestTargeting, RandomTargeting, PointTargeting
/// </summary>
[System.Serializable] public abstract class TargetingModule : AugmentModule { }
/// <summary>
/// 전달방식 모듈,
/// ProjectileDelivery, AreaDelivery
/// </summary>
[System.Serializable] public abstract class DeliveryModule  : AugmentModule { }
/// <summary>
/// 적중효과 모듈,
/// DamageEffect, SearchEffect
/// </summary>
[System.Serializable] public abstract class EffectModule    : AugmentModule { }
