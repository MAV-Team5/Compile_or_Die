using UnityEngine;

/// <summary>증강을 이루는 부품 4축의 공통 부모.</summary>
[System.Serializable] public abstract class AugmentModule { }


/// <summary>
/// ① 발동 조건 — 언제 터지나
/// Cooldown
/// </summary>
[System.Serializable] public abstract class TriggerModule : AugmentModule
{
    [Fx("시전 연출", "시전자")]
    public FxGroup castFx = new();

    /// <summary>발동 준비 여부. 상태를 소비하지 않는다.</summary>
    public abstract bool Evaluate(AugmentInstance instance, float deltaTime);

    /// <summary>발동 성사 시 호출. 쿨타임을 소비하고 시전 연출을 낸다.</summary>
    public virtual void Consume(AugmentContext ctx)
        => castFx.PlayAt(ctx.Owner.position, ctx.Heading);

    /// <summary>0~1 진행률. HUD 표시용.</summary>
    public virtual float Progress(AugmentInstance instance) => 1f;
}

/// <summary>
/// ② 목표 대상 — 누구를 / 어디를
/// 적을 고르는 것 : Nearest(1체) · Random(N체) · AllInRange(전부)
/// 좌표를 찍는 것 : RandomPoint(N곳) · OwnerPoint(원점)
/// </summary>
[System.Serializable] public abstract class TargetingModule : AugmentModule
{
    /// <summary>
    /// 대상을 찾아 ctx.Targets 에 채운다.
    /// 이때 ctx.EffectiveRange 에 이 단계의 기준 거리를 반드시 기록할 것 —
    /// 전달 모듈이 그 값으로 비행 거리와 폭발 크기를 정한다.
    /// </summary>
    public abstract void Resolve(AugmentContext ctx);
}

/// <summary>
/// ③ 전달 방식 — 어떻게 닿나
/// 날아가는 것 : Projectile(겨눔) · Radial(각도)
/// 즉시 닿는 것 : Instant(대상) · Area(원형) · Line(직선)
/// </summary>
[System.Serializable] public abstract class DeliveryModule : AugmentModule
{
    /// <summary>전달. 적중할 때마다 onHit 호출.</summary>
    public abstract void Execute(AugmentContext ctx, System.Action<HitInfo> onHit);
}

/// <summary>
/// ④ 적중 결과 — 무슨 일이 일어나나
/// 상태를 바꾸는 것 : Damage · Knockback · Search
/// 이어가는 것     : Chain(반복) · SubPipeline(1회)
/// 보여주는 것     : Vfx · Sfx · Log
/// </summary>
[System.Serializable] public abstract class EffectModule : AugmentModule
{
    /// <summary>적중 1회 결과 적용.</summary>
    public abstract void Apply(AugmentContext ctx, HitInfo hit);
}
