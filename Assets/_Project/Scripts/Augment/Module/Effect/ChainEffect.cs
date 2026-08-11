using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적중 지점에서 하위 파이프라인을 다시 실행한다. Bash·C 같은 연쇄 증강용.
/// 하위는 Trigger 없이 타겟팅·전달·효과 3축만 돈다.
/// </summary>
[System.Serializable]
public class ChainEffect : EffectModule
{
    /// <summary>무한 재귀 방지용 절대 상한. 인스펙터 값이 이보다 커도 여기서 잘린다.</summary>
    const int HardLimit = 8;

    [Header("연쇄 단계에서 실행할 파이프라인")]
    [SerializeReference] public TargetingModule targeting;
    [SerializeReference] public List<DeliveryModule> deliveries = new();

    /// <summary>단계마다 적용할 효과. ChainEffect 자신은 자동으로 이어지므로 넣지 말 것.</summary>
    [SerializeReference] public List<EffectModule> effects = new();

    [Header("깊이")]
    [Tooltip("최대 연쇄 단계. 0이면 레벨 수치의 depth 를 쓴다. " +
             "count 는 '몇 개'고 depth 가 '몇 단계'다.")]
    public int maxDepthOverride = 0;

    /// <summary>단계마다 누적되는 피해 증폭. 0.2 면 2단계에서 1.2배, 3단계에서 1.44배.</summary>
    public float amplifyPerDepth = 0.2f;

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null) return;

        int limit = maxDepthOverride > 0 ? maxDepthOverride : ctx.Stat.depth;
        limit = Mathf.Min(limit, HardLimit);

        if (ctx.Depth + 1 >= limit) return;

        // 적중한 적을 원점으로 삼는다. 하위 타겟팅이 여기서부터 검색한다.
        var sub = new AugmentContext();
        sub.BeginChild(hit.Target, ctx, ctx.DamageMultiplier * (1f + amplifyPerDepth));

        // 하위 효과에 자기 자신을 붙여야 다음 단계로 이어진다. 깊이 가드가 종료를 보장한다.
        var chained = new List<EffectModule>(effects) { this };

        AugmentPipeline.Run(sub, targeting, deliveries, chained);
    }
}
