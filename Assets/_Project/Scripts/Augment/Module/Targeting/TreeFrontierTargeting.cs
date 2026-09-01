using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 【적 N체】 원점에서 시작해 <b>내 트리의 잎</b>을 찾아낸다. 자라날 자리를 고르는 타겟팅.
///
/// 간선을 타고 끝까지 내려가되 도중의 노드는 건드리지 않는다.
/// 그래서 발동할 때마다 트리가 한 층씩 바깥으로 자란다 — 매번 뿌리 주변에서 다시 펴지 않는다.
///
/// 아직 노드가 아닌 적을 맞혔으면 그 자리가 곧 첫 잎이 된다.
/// 하위 파이프라인 안에서만 의미가 있다 — 원점이 적이어야 하기 때문이다.
/// </summary>
[System.Serializable]
[ModuleInfo("적 N체 — 내 트리의 잎", "간선을 타고 끝까지 가서 자랄 자리를 고른다")]
public class TreeFrontierTargeting : TargetingModule
{
    [Sheet("사거리")]
    [Tooltip("이 단계의 사거리(유닛). 비워두면 시트의 사거리(range)를 쓴다.\n" +
             "잎을 찾는 데는 안 쓰이고, 뒤따르는 전달 모듈이 참고한다.")]
    public Scalable rangeOverride = Scalable.Ratio(1f);

    [Sheet("깊이")]
    [Tooltip("트리가 자랄 수 있는 최대 층수. 0 × 1 이면 시트 그대로.\n" +
             "이 층에 닿은 잎은 목록에서 빠져서 더 자라지 않는다.")]
    public Scalable maxDepth = Scalable.Ratio(1f);

    [Tooltip("한 번에 자랄 잎의 수. 0이면 찾은 잎 전부.\n" +
             "제한을 두면 가지가 고르게 자라지 않고 일부만 뻗는다.")]
    public int targetLimit = 0;

    static readonly List<Transform> frontier = new();

    public override void Resolve(AugmentContext ctx)
    {
        ctx.EffectiveRange = rangeOverride.Of(ctx.BaseRange);

        // 최초 발동의 원점은 시전자다. 거기서 트리를 찾으면 플레이어가 뿌리가 되어버린다
        if (ctx.Depth == 0)
        {
            ModuleWarning.Once(ctx,
                "TreeFrontier 가 하위 파이프라인이 아닌 곳에 있습니다. " +
                "SubPipeline · Chain 안에 넣을 것");

            return;
        }

        // 아직 아무 간선도 없는 적이면 그 자리가 첫 잎이다
        if (!ctx.Owner.TryGetComponent(out LinkHolder root))
        {
            ctx.Targets.Add(ctx.Owner);
            return;
        }

        root.CollectFrontier(ctx.Instance, maxDepth.IntOf(ctx.Stat.depth), frontier);

        int limit = targetLimit > 0 ? targetLimit : frontier.Count;

        for (int i = 0; i < frontier.Count && i < limit; i++)
            ctx.Targets.Add(frontier[i]);
    }
}
