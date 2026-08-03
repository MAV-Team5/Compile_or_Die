/// <summary>
/// 모듈 조립화.
/// </summary>
[System.Serializable] public abstract class AugmentModule {}
/// <summary>
/// 발동 조건 - 언제
/// CooldownTrigger, PassiveTrigger
/// </summary>
[System.Serializable] public abstract class TriggerModule   : AugmentModule { }
[System.Serializable]
public class CooldownTrigger : TriggerModule
{
    public float extraDelay;
}
/// <summary>
/// 목표 대상 - 누구를 / 어디를
/// NearestTargeting, RandomTargeting, PointTargeting
/// </summary>
[System.Serializable] public abstract class TargetingModule : AugmentModule { }
/// <summary>
/// 전달 방식 - 어떻게
/// ProjectileDelivery, AreaDelivery
/// </summary>
[System.Serializable] public abstract class DeliveryModule  : AugmentModule { }
/// <summary>
/// 적중 결과 - 무엇을
/// DamageEffect, SearchEffect
/// </summary>
[System.Serializable] public abstract class EffectModule    : AugmentModule { }
