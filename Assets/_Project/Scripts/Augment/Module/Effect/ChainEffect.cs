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
    [Tooltip("최대 연쇄 단계. 0이면 시트의 깊이(depth)를 쓴다. 8을 넘을 수 없다.\n" +
             "수량(count)은 '몇 개'고 깊이(depth)가 '몇 단계'다. 섞지 말 것.")]
    public int maxDepthOverride = 0;

    [Tooltip("단계마다 누적되는 피해 배율. 0.2 면 2단계에서 1.2배, 3단계에서 1.44배.\n" +
             "지수로 불어나므로 균일한 피해를 원하면 0.")]
    public float amplifyPerDepth = 0.2f;

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

        // 깊이를 다 썼다 = 여기가 체인의 끝
        if (ctx.Depth + 1 >= limit)
        {
            ApplyFinal(ctx, hit);
            return;
        }

        // 적중한 적을 원점으로 삼는다. 하위 타겟팅이 여기서부터 검색한다
        var sub = new AugmentContext();
        sub.BeginChild(hit.Target, ctx, ctx.DamageMultiplier * (1f + amplifyPerDepth),
                       hit.Direction);

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
