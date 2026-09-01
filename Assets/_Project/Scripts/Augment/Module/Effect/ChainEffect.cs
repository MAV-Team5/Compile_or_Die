using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적중한 적에서 같은 파이프라인을 다시 실행해 계속 번져나간다. Bash · C 같은 연쇄 증강 전용.
/// 하위는 Trigger 없이 타겟팅 · 전달 · 효과 3축만 돈다.
/// </summary>
[System.Serializable]
[ModuleInfo("같은 파이프라인을 반복해 번져나간다", "한 번만이면 SubPipeline")]
public class ChainEffect : EffectModule
{
    /// <summary>무한 재귀 방지용 절대 상한. 인스펙터 값이 이보다 커도 여기서 잘린다.</summary>
    const int HardLimit = 8;

    [Header("설정")]
    [Sheet("깊이")]
    [Tooltip("몇 번 더 번질지. 0이면 시트의 깊이(depth)를 쓴다. 8을 넘을 수 없다.\n" +
             "3이면 최초 적중 뒤 3번 더 번진다 — 선형 연쇄 기준 대상 4개.\n" +
             "수량(count)은 '몇 개'고 깊이(depth)가 '몇 번'이다. 섞지 말 것.")]
    public int maxDepthOverride = 0;

    [Sheet("효과피해")]
    [Tooltip("한 단계 번질 때마다 더해지는 추가 피해. 시트 피해량 대비 비율이다.\n" +
             "0.15 면 한 단계마다 +15% — 깊이 3이면 마지막 대상이 +45% 를 받는다.\n" +
             "비워두면 시트의 효과피해(effectDamage)를 쓴다.\n\n" +
             "곱이 아니라 합이라서 깊어져도 계산이 터지지 않는다.")]
    public Scalable damagePerDepth = Scalable.Ratio(1f);

    // ── 아래는 접어두는 중첩 파이프라인. 길어지므로 설정 밑에 둔다 ──

    [Header("단계마다 실행할 파이프라인")]
    [SerializeReference] public TargetingModule targeting;

    [SerializeReference] public List<DeliveryModule> deliveries = new();

    [Tooltip("단계마다 적용할 효과. Chain 자신은 자동으로 이어지므로 여기 넣지 말 것.")]
    [SerializeReference] public List<EffectModule> effects = new();

    [Tooltip("연쇄가 끝난 마지막 대상에게만 추가로 적용. Bash:kill 전용.\n" +
             "깊이를 다 썼든 대상이 없어 멈췄든 양쪽 다 여기로 온다.")]
    [SerializeReference] public List<EffectModule> finalEffects = new();

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null) return;

        int limit = maxDepthOverride > 0 ? maxDepthOverride : ctx.Stat.depth;
        limit = Mathf.Min(limit, HardLimit);

        // 깊이 = 번지는 횟수. 최초 적중(Depth 0)에서 이미 다 썼으면 여기가 끝
        if (ctx.Depth >= limit)
        {
            ApplyFinal(ctx, hit);
            return;
        }

        // 한 단계 더 갈 때마다 보너스가 한 번 더 쌓인다. 시트 피해량에 비례하므로
        // 레벨이 올라 피해량이 커지면 보너스도 같이 커진다
        float step = ctx.Stat.damage * damagePerDepth.Of(ctx.Stat.effectDamage);

        // 적중한 적을 원점으로 삼는다. 하위 타겟팅이 여기서부터 검색한다
        var sub = new AugmentContext();
        sub.BeginChild(hit.Target, ctx, ctx.BonusDamage + step, hit.Direction);

        // 하위 효과에 자기 자신을 붙여야 다음 단계로 이어진다. 깊이 가드가 종료를 보장한다
        var chained = new List<EffectModule>(effects) { this };

        // 못 번졌다 = 대상이 없어 여기서 멈춘 것이므로 이 대상이 마지막
        if (!AugmentPipeline.Run(sub, targeting, deliveries, chained))
            ApplyFinal(ctx, hit);
    }

    void ApplyFinal(AugmentContext ctx, HitInfo hit)
    {
        for (int i = 0; i < finalEffects.Count; i++)
            finalEffects[i]?.Apply(ctx, hit);
    }
}
