using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대상을 포물선으로 날려 보낸다. 날아가는 동안은 스스로 못 움직인다.
/// 떨어지면 <see cref="landEffects"/> 를 그 자리에서 실행하고, <see cref="nextTargeting"/> 이 있으면
/// 착지 자리에서 다음 대상을 골라 <b>자신을 다시 실행</b>한다 — 거리는 매번 falloff 배씩 줄어든다.
///
/// <code>
/// 버블 정렬 예)
///   TossEffect
///     landEffects    : SubPipeline(OwnerPoint → Area → 끌어당김 · 피해)   ← 착지마다 모으기
///     nextTargeting  : HighestHealthTargeting                           ← 가장 무거운 적을
///     nextDeliveries : InstantDelivery                                   ← 다시 날린다
/// </code>
///
/// 반복 횟수는 깊이(시트 depth)를 따른다. ChainEffect 처럼 자기 자신을 다음 단계에 붙이고, 횟수가 종료를 보장한다.
/// </summary>
[System.Serializable]
[ModuleInfo("대상을 포물선으로 날려 보낸다", "착지마다 효과 실행 · 다음 대상을 골라 반복")]
public class TossEffect : EffectModule
{
    // SubPipelineEffect 의 깊이 가드(8)보다 작아야 마지막 착지에서도 착지 효과가 돈다
    const int HardLimit = 6;

    [Sheet("깊이")]
    [Tooltip("이 발동에서 최대 몇 번 날릴지. 0이면 시트의 깊이(depth), 그것도 0이면 1번. 6을 넘을 수 없다.")]
    public int maxTossOverride = 0;

    [Tooltip("첫 번째로 날리는 거리(유닛).")]
    [Min(0f)] public float distance = 3f;

    [Tooltip("날릴 때마다 직전 거리에 곱하는 값. 0.5 면 절반씩.")]
    [Range(0.1f, 1f)] public float falloff = 0.5f;

    [Tooltip("첫 번째 비행 시간(초). 이후는 거리에 맞춰 짧아진다(최소 40%).")]
    [Min(0.05f)] public float flightTime = 0.35f;

    [Tooltip("포물선 높이(유닛).")]
    [Min(0f)] public float arcHeight = 1.2f;

    [Tooltip("켜면 처음 들어온 방향으로만 계속 날린다. 끄면 이번 적중의 방향(원점 → 대상)을 따른다.")]
    public bool keepHeading = true;

    [Header("착지할 때마다 — 날아간 적이 원점이 된다")]
    [SerializeReference] public List<EffectModule> landEffects = new();

    [Header("다음에 날릴 대상 — 착지 자리에서 실행. 비우면 한 번만 날린다")]
    [SerializeReference] public TargetingModule nextTargeting;

    [Tooltip("다음 대상에게 닿는 방법. 보통 InstantDelivery. 여기 효과는 자동으로 이 TossEffect 다.")]
    [SerializeReference] public List<DeliveryModule> nextDeliveries = new();

    /// <summary>발동(FiringId)마다 몇 번 날렸는지. 반복이 몇 번째인지 알아야 거리를 줄이고 멈춘다.</summary>
    class State
    {
        public readonly Dictionary<int, int> Tosses = new();
    }

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null || ctx.Instance == null) return;

        Enemy enemy = hit.Target.GetComponentInParent<Enemy>();
        if (!EnemyLaunch.CanLaunch(enemy)) return;

        State state = ctx.Instance.GetState<State>(this);
        int firing = ctx.FiringId;
        int limit = Mathf.Min(maxTossOverride > 0 ? maxTossOverride : Mathf.Max(1, ctx.Stat.depth), HardLimit);

        state.Tosses.TryGetValue(firing, out int done);
        if (done >= limit)
        {
            state.Tosses.Remove(firing);
            return;
        }

        Vector2 direction = Direction(ctx, hit);
        if (direction == Vector2.zero)
        {
            state.Tosses.Remove(firing);
            return;
        }

        state.Tosses[firing] = done + 1;

        float shrink = Mathf.Pow(falloff, done);
        Vector2 to = (Vector2)enemy.transform.position + direction * (distance * shrink);

        // 효과는 착지할 때 돌므로, 지금의 통계 주인을 붙잡아 둔다. 그때쯤 ctx.Credit 은 이미 되돌려져 있다
        AugmentInstance credit = ctx.Credit;

        EnemyLaunch.Launch(enemy, to, flightTime * Mathf.Max(0.4f, shrink), arcHeight,
            landed => Land(ctx, enemy, landed, direction, credit, state, limit),
            () => state.Tosses.Remove(firing));   // 날아가다 죽었다. 이 발동의 반복은 여기서 끝
    }

    Vector2 Direction(AugmentContext ctx, HitInfo hit)
    {
        if (keepHeading && ctx.HasDirection) return ctx.Heading.normalized;
        if (hit.Direction.sqrMagnitude > 0.0001f) return hit.Direction.normalized;
        return ctx.HasDirection ? ctx.Heading.normalized : Vector2.zero;
    }

    void Land(AugmentContext ctx, Enemy enemy, Vector2 landed, Vector2 direction,
              AugmentInstance credit, State state, int limit)
    {
        int firing = ctx.FiringId;
        var hit = new HitInfo { Target = enemy.transform, Point = landed, Direction = direction };

        // ① 착지 효과. 날아간 적을 원점으로 삼는다 — SubPipeline(OwnerPoint) 가 이 자리를 터뜨린다
        var land = new AugmentContext();
        land.BeginChild(enemy.transform, ctx, ctx.BonusDamage, direction);
        land.Credit = credit;

        for (int i = 0; i < landEffects.Count; i++) landEffects[i]?.Apply(land, hit);

        // ② 다음 대상. 횟수가 남았고 고를 방법이 있을 때만
        bool canContinue = nextTargeting != null && nextDeliveries.Count > 0 && enemy != null &&
                           state.Tosses.TryGetValue(firing, out int done) && done < limit;

        if (!canContinue)
        {
            state.Tosses.Remove(firing);
            return;
        }

        var next = new AugmentContext();
        next.BeginChild(enemy.transform, ctx, ctx.BonusDamage, direction);
        next.Credit = credit;

        // 자기 자신을 효과로 붙여야 다음 대상도 날아간다. 횟수 가드가 종료를 보장한다
        var chained = new List<EffectModule> { this };

        if (!AugmentPipeline.Run(next, nextTargeting, nextDeliveries, chained))
            state.Tosses.Remove(firing);
    }
}
