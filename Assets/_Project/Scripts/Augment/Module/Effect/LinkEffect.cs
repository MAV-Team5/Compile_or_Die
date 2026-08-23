using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적중한 대상들을 간선으로 잇는다. 이후 한쪽이 맞으면 이어진 쪽에도 피해가 번진다.
/// neighborsPerNode 하나로 트리부터 그물까지 연속적으로 갈린다.
///
/// <code>
/// neighborsPerNode  1     Tree    가장 가까운 하나에만.  사이클 없음.  간선 = 노드-1
///                   2~3   그물    무리 모양을 따라 둥글게 이어진다
///                   0     완전    거리 안 전부.  간선이 제곱으로 는다
///
/// linkRange               이 거리를 넘는 쌍은 안 잇는다.
///                         없으면 화면 반대편끼리 이어져 플레이어 위를 가로지른다
/// </code>
///
/// 간선은 탐색 표식과 수명이 독립적이다. 표식이 풀려도, 다음 탐색이 시작돼도 끊기지 않는다.
/// </summary>
[System.Serializable]
[ModuleInfo("적중한 대상들을 간선으로 잇는다", "한쪽이 맞으면 이어진 쪽에도 번진다")]
public class LinkEffect : EffectModule
{
    public enum LinkParent
    {
        /// <summary>이번 발동에서 놓인 노드 중 가장 가까운 것. 평면적으로 이어붙인다.</summary>
        Nearest,

        /// <summary>이 파이프라인의 원점 — 하위 파이프라인이면 직전에 맞은 적. 부모가 명확한 트리.</summary>
        Origin
    }

    [Header("부모 고르기")]
    [Tooltip("Nearest — 이번 발동에서 놓인 노드 중 가장 가까운 것에 붙는다. Graph 계열.\n" +
             "Origin  — 이 파이프라인의 원점이 부모가 된다. 하위 파이프라인 안에서만 의미가 있다.\n" +
             "          투사체가 맞힌 적이 부모, 그 너머 부채꼴의 적들이 자식이 된다.")]
    public LinkParent parentMode = LinkParent.Nearest;

    /// <summary>이번 발동에서 이미 놓은 노드. 발동이 바뀌면 비운다.</summary>
    class State
    {
        public int firingId;

        public readonly List<Transform> nodes = new();

        /// <summary>nodes 와 같은 인덱스. 그 노드가 이미 거느린 자식 수.</summary>
        public readonly List<int> children = new();

        public void Clear()
        {
            nodes.Clear();
            children.Clear();
        }

        public void Place(Transform node)
        {
            nodes.Add(node);
            children.Add(0);
        }
    }

    [Header("전이")]
    [Sheet("효과피해")]
    [Tooltip("간선을 타고 넘어가는 피해. 비워두면 시트의 효과 피해(effectDamage)를 쓴다.\n" +
             "배수만 주면 그 값에 비례한다.")]
    public Scalable transfer = Scalable.Ratio(1f);

    [Tooltip("켜면 비율. 0.4 면 원래 피해의 40%가 이웃에게 간다. 끄면 고정값.\n" +
             "비율로 두면 센 공격일수록 크게 번져서 인과가 읽히기 쉽다.")]
    public bool isPercent = true;

    [Detail]
    [Sheet("깊이")]
    [Tooltip("간선을 타고 몇 번 더 번질지. 0이면 시트의 깊이(depth)를 쓰고, 그것도 0이면 이웃까지만.\n" +
             "간선이 쌓이면 지수로 커지므로 2~3 을 넘기지 말 것.")]
    public int hopsOverride = 0;

    [Header("간선")]
    [Sheet("지속시간")]
    [Tooltip("간선 유지 시간(초). 비워두면 시트의 지속시간(duration)을 쓴다.\n" +
             "결과가 0이면 노드가 죽을 때까지 남는다 — 화면이 선으로 뒤덮일 수 있으니 주의.")]
    public Scalable duration = Scalable.Ratio(1f);

    [Tooltip("새 노드가 이을 이웃 수. 가까운 순으로 고른다.\n" +
             "1 — 가장 가까운 하나에만. 사이클 없는 트리\n" +
             "2~3 — 가까운 순 두셋. 무리 모양을 따라 둥글게 이어지는 그물\n" +
             "0 — 거리 안 전부. 간선이 제곱으로 늘어 화면을 덮는다")]
    public int neighborsPerNode = 1;

    [Sheet("효과범위")]
    [Tooltip("이을 수 있는 최대 거리(유닛). 비워두면 시트의 효과 범위(effectRange)를 쓴다.\n" +
             "＊ 이게 없으면 화면 반대편 노드끼리도 이어져 플레이어 위를 가로지르는 간선이 생긴다.\n" +
             "결과가 0이면 거리 제한 없음.")]
    public Scalable linkRange = Scalable.Ratio(1f);

    [Detail]
    [Tooltip("이은 뒤 이 배수만큼 벌어지면 간선이 끊긴다. 2면 연결 거리의 두 배.\n" +
             "0이면 아무리 멀어져도 안 끊긴다 — 회수된 적의 선이 화면을 가로지를 수 있다.")]
    public float stretchLimit = 2f;

    [Detail]
    [Tooltip("한 노드가 거느릴 수 있는 자식 수. 0이면 제한 없음.\n" +
             "부모 고르기가 Nearest 일 때만 쓰인다 — Origin 은 부모가 이미 정해져 있다.\n" +
             "Origin 에서 자식 수를 줄이려면 하위 파이프라인의 타겟 수를 제한할 것.")]
    public int maxChildren = 0;

    [Detail]
    [Tooltip("노드 하나가 가질 수 있는 간선 상한. 0이면 12.\n" +
             "넘치면 가장 먼 간선부터 밀어낸다 — 가까운 연결이 남아야 덩어리 모양이 유지된다.")]
    public int maxLinksPerNode = 0;

    [Detail]
    [Tooltip("켜면 이미 이 증강의 노드인 적에게도 새 간선을 잇는다 — 사이클이 생긴다.\n" +
             "＊ 트리에서는 꺼둘 것. 켜면 부모가 둘인 노드가 생겨 깊이 제한이 무너진다.\n" +
             "그물처럼 얽혀야 하는 구조에만 켠다.")]
    public bool allowCycles = false;

    [Tooltip("간선을 그릴 프리팹. LineRenderer 가 붙어 있으면 양 끝을 알아서 따라간다.\n" +
             "비워도 전이는 그대로 일어난다 — 눈에 안 보일 뿐이다.")]
    public GameObject linePrefab;

    [Fx("연결 연출", "새로 이어진 노드")]
    public FxGroup connectFx = new();

    [Detail]
    [Tooltip("적중이 들어올 때마다 무슨 일이 있었는지 콘솔에 찍는다. 확인이 끝나면 끌 것.\n" +
             "로그가 아예 없으면 하위 투사체가 아무것도 못 맞힌 것이다.")]
    public bool logLinks = false;

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null) return;

        float amount = transfer.Of(ctx.Stat.effectDamage);

        // 비율 모드에서 1을 넘으면 홉마다 커져서 몇 단계 만에 화면이 녹는다
        if (isPercent && amount > 1f)
        {
            ModuleWarning.Once(ctx,
                $"전이 비율이 {amount:0.##}(={amount * 100:0}%)라 홉마다 피해가 커집니다. " +
                "감쇠시키려면 1보다 작게 — 시트의 효과피해를 0.5 같은 값으로 둘 것");
        }

        var template = new Link
        {
            Owner = ctx.Instance,
            Amount = amount,
            IsPercent = isPercent,
            Hops = ResolveHops(ctx),
            MaxPerNode = maxLinksPerNode,
            ExpireAt = ResolveExpireAt(ctx),

            // 이을 때보다 넉넉히 준다. 조금 벌어졌다고 바로 끊기면 간선이 깜빡인다
            MaxLength = linkRange.Of(ctx.Stat.effectRange) * stretchLimit
        };

        if (parentMode == LinkParent.Origin)
        {
            JoinToOrigin(ctx, hit.Target, template);
            return;
        }

        State state = ctx.GetState<State>(this);

        // 투사체는 날아가는 동안 다음 발동이 시작될 수 있다.
        // 지난 발동이 늦게 도착한 것이라면 지금 쌓는 트리를 건드리면 안 된다
        if (ctx.FiringId < state.firingId) return;

        // 발동이 바뀌면 새 트리를 시작한다. 지난 발동의 간선은 그대로 남는다
        if (state.firingId != ctx.FiringId)
        {
            state.firingId = ctx.FiringId;
            state.Clear();
        }

        int before = state.nodes.Count;
        float maxDistance = linkRange.Of(ctx.Stat.effectRange);

        ConnectToPlaced(state, hit.Target, template, maxDistance);

        state.Place(hit.Target);

        Log(ctx, hit.Target, before == 0
            ? "첫 노드라 부모 없음 — 등록만"
            : $"놓인 노드 {before}개 중 거리 {maxDistance:0.#} 안의 가까운 순 " +
              $"{(neighborsPerNode > 0 ? neighborsPerNode.ToString() : "전부")}개에 붙임");
    }

    /// <summary>
    /// 원점을 부모로 삼는다. 하위 파이프라인 안에서는 원점이 직전에 맞은 적이라
    /// "투사체가 꿴 적이 부모, 그 너머 적들이 자식"이 그대로 나온다.
    /// </summary>
    void JoinToOrigin(AugmentContext ctx, Transform child, Link template)
    {
        // 최초 발동의 원점은 시전자다. 거기에 이으면 플레이어가 노드가 되어버린다
        if (ctx.Depth == 0)
        {
            ModuleWarning.Once(ctx,
                "Link 의 부모 고르기가 Origin 인데 하위 파이프라인이 아닙니다. " +
                "SubPipeline · Chain 안에 넣거나 Nearest 로 바꿀 것");

            Log(ctx, child, "거부 — 하위 파이프라인이 아님 (원점이 시전자다)");
            return;
        }

        // 이미 이 트리에 속한 적을 또 매달면 부모가 둘이 되어 깊이 계산이 무너진다.
        // 뿌리를 맞히든 중간을 맞히든, 한 단계를 건너뛴 모양이 되는 원인
        if (!allowCycles && LinkHolder.IsNodeOf(child, ctx.Instance))
        {
            Log(ctx, child, "건너뜀 — 이미 이 트리의 노드다");
            return;
        }

        bool made = Join(ctx.Owner, child, template);

        Log(ctx, child, made
            ? $"이음  {ctx.Owner.name} → {child.name}"
            : $"이미 있음 — 수명만 갱신  {ctx.Owner.name} → {child.name}");
    }

    /// <summary>후보를 담아두는 자리. 발동마다 새로 잡지 않게 재사용한다.</summary>
    static readonly List<(int index, float sqr)> candidates = new();

    /// <summary>
    /// 새 노드를 이미 놓인 노드들에 잇는다.
    ///
    /// <b>가까운 순으로 neighborsPerNode 개까지만</b> 잇고, linkRange 를 넘는 쌍은 건너뛴다.
    /// 거리를 안 걸면 화면 반대편끼리 이어져 플레이어 위를 가로지르는 간선이 생긴다.
    /// </summary>
    void ConnectToPlaced(State state, Transform node, Link template, float maxDistance)
    {
        if (state.nodes.Count == 0) return;

        float limitSqr = maxDistance > 0f ? maxDistance * maxDistance : float.MaxValue;

        candidates.Clear();

        for (int i = 0; i < state.nodes.Count; i++)
        {
            if (state.nodes[i] == null) continue;
            if (maxChildren > 0 && state.children[i] >= maxChildren) continue;

            float sqr = (state.nodes[i].position - node.position).sqrMagnitude;
            if (sqr > limitSqr) continue;

            candidates.Add((i, sqr));
        }

        if (candidates.Count == 0) return;

        candidates.Sort((a, b) => a.sqr.CompareTo(b.sqr));

        int want = neighborsPerNode > 0 ? neighborsPerNode : candidates.Count;

        for (int i = 0; i < candidates.Count && i < want; i++)
        {
            int index = candidates[i].index;

            if (Join(state.nodes[index], node, template)) state.children[index]++;
        }
    }

    bool Join(Transform from, Transform to, Link template)
    {
        if (from == null || to == null || from == to) return false;

        bool made = LinkHolder.Connect(from, to, template, linePrefab);

        // 이어진 순간을 새 노드 자리에서 알린다
        if (made) connectFx.PlayAt(to.position, default, 0f, to);

        return made;
    }

    /// <summary>적중 1회가 어떻게 처리됐는지 한 줄로 남긴다.</summary>
    void Log(AugmentContext ctx, Transform child, string what)
    {
        if (!logLinks) return;

        Debug.Log($"[{ctx.Instance.Data.name}] 깊이{ctx.Depth} {parentMode}  {child.name}  →  {what}");
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
