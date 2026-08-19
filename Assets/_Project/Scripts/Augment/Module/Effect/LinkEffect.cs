using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적중한 대상들을 간선으로 잇는다. 이후 한쪽이 맞으면 이어진 쪽에도 피해가 번진다.
///
/// <b>새 노드는 이미 놓인 노드 중 가장 가까운 하나에만 붙는다.</b>
/// 부모가 항상 하나뿐이라 사이클이 안 생긴다 — 정의상 트리다.
/// 반경 안 전부를 잇는 그물(그래프)이 필요하면 connectToAll 을 켠다.
///
/// 간선은 탐색 표식과 수명이 독립적이다. 표식이 풀려도, 다음 탐색이 시작돼도 끊기지 않는다.
/// </summary>
[System.Serializable]
[ModuleInfo("적중한 대상들을 간선으로 잇는다", "한쪽이 맞으면 이어진 쪽에도 번진다")]
public class LinkEffect : EffectModule
{
    /// <summary>이번 발동에서 이미 놓은 노드. 발동이 바뀌면 비운다.</summary>
    class State
    {
        public int firingId;
        public readonly List<Transform> nodes = new();
    }

    [Header("전이")]
    [Tooltip("간선을 타고 넘어가는 피해. 비워두면 시트의 효과 피해(effectDamage)를 쓴다.\n" +
             "배수만 주면 그 값에 비례한다.")]
    public Scalable transfer = Scalable.Ratio(1f);

    [Tooltip("켜면 비율. 0.4 면 원래 피해의 40%가 이웃에게 간다. 끄면 고정값.\n" +
             "비율로 두면 센 공격일수록 크게 번져서 인과가 읽히기 쉽다.")]
    public bool isPercent = true;

    [Tooltip("간선을 타고 몇 번 더 번질지. 0이면 시트의 깊이(depth)를 쓰고, 그것도 0이면 이웃까지만.\n" +
             "간선이 쌓이면 지수로 커지므로 2~3 을 넘기지 말 것.")]
    public int hopsOverride = 0;

    [Header("간선")]
    [Tooltip("간선 유지 시간(초). 비워두면 시트의 지속시간(duration)을 쓴다.\n" +
             "결과가 0이면 노드가 죽을 때까지 남는다 — 화면이 선으로 뒤덮일 수 있으니 주의.")]
    public Scalable duration = Scalable.Ratio(1f);

    [Tooltip("켜면 이번에 놓인 노드 전부와 잇는다(그물). 끄면 가장 가까운 하나에만 붙는다(트리).")]
    public bool connectToAll = false;

    [Tooltip("간선을 그릴 프리팹. LineRenderer 가 붙어 있으면 양 끝을 알아서 따라간다.\n" +
             "비워도 전이는 그대로 일어난다 — 눈에 안 보일 뿐이다.")]
    public GameObject linePrefab;

    [Fx("연결 연출", "새로 이어진 노드")]
    public FxGroup connectFx = new();

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null) return;

        State state = ctx.GetState<State>(this);

        // 발동이 바뀌면 새 트리를 시작한다. 지난 발동의 간선은 그대로 남는다
        if (state.firingId != ctx.FiringId)
        {
            state.firingId = ctx.FiringId;
            state.nodes.Clear();
        }

        var template = new Link
        {
            Owner = ctx.Instance,
            Amount = transfer.Of(ctx.Stat.effectDamage),
            IsPercent = isPercent,
            Hops = ResolveHops(ctx),
            ExpireAt = ResolveExpireAt(ctx)
        };

        ConnectToPlaced(state, hit.Target, template);

        state.nodes.Add(hit.Target);
    }

    void ConnectToPlaced(State state, Transform node, Link template)
    {
        if (state.nodes.Count == 0) return;

        if (connectToAll)
        {
            for (int i = 0; i < state.nodes.Count; i++)
                Join(state.nodes[i], node, template);

            return;
        }

        // 트리 — 이미 놓인 것 중 가장 가까운 하나가 부모가 된다
        Transform parent = Nearest(state.nodes, node.position);

        Join(parent, node, template);
    }

    void Join(Transform from, Transform to, Link template)
    {
        if (from == null || to == null || from == to) return;

        LinkHolder.Connect(from, to, template, linePrefab);

        // 이어진 순간을 새 노드 자리에서 알린다
        connectFx.PlayAt(to.position, default, 0f, to);
    }

    static Transform Nearest(List<Transform> nodes, Vector3 from)
    {
        Transform best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null) continue;

            float sqr = (nodes[i].position - from).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = nodes[i];
            }
        }

        return best;
    }

    int ResolveHops(AugmentContext ctx)
    {
        int hops = hopsOverride > 0 ? hopsOverride : ctx.Stat.depth;

        // 0이면 이웃까지만. 간선이 쌓이는 구조라 상한을 낮게 잡는다
        return Mathf.Clamp(hops <= 0 ? 1 : hops, 1, 4);
    }

    float ResolveExpireAt(AugmentContext ctx)
    {
        float life = duration.Of(ctx.Stat.duration);

        return life > 0f ? Time.time + life : 0f;
    }
}
