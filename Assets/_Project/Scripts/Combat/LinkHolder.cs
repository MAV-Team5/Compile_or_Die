using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적이 지닌 간선 목록. 필요할 때 자동으로 붙는다.
/// MarkerHolder · StatusHolder 와 같은 패턴이다.
///
/// 간선은 양방향이라 양쪽에 서로를 등록한다.
/// 한쪽이 죽으면 반대쪽 목록에서도 반드시 지워야 한다 — 안 그러면 죽은 적을 가리키는
/// 간선이 남아 다음 적이 풀에서 재활용될 때 엉뚱한 연결이 살아난다.
/// </summary>
public class LinkHolder : MonoBehaviour
{
    /// <summary>
    /// 간선을 잇는 쪽이 상한을 안 정했을 때 쓰는 값.
    /// 간선은 탐색과 달리 저절로 안 풀리므로 상한이 없으면 무한히 쌓인다.
    /// </summary>
    const int DefaultMaxLinks = 12;

    readonly List<Link> links = new();

    public int Count => links.Count;

    /// <summary>이 노드의 피해 진입점. 전이가 여기로 들어간다.</summary>
    public IDamageReceiver Receiver { get; private set; }

    void Awake() => CacheReceiver();

    void CacheReceiver()
    {
        if (!TryGetComponent(out IDamageReceiver receiver))
            receiver = GetComponentInParent<IDamageReceiver>();

        Receiver = receiver;
    }

    public static LinkHolder GetOrAdd(Transform target)
    {
        if (target == null) return null;

        return target.TryGetComponent(out LinkHolder holder)
            ? holder
            : target.gameObject.AddComponent<LinkHolder>();
    }

    /// <summary>이 증강이 만든 구조에 이미 속해 있는가.</summary>
    public bool BelongsTo(AugmentInstance owner)
    {
        for (int i = 0; i < links.Count; i++)
            if (links[i].Owner == owner && !links[i].IsBroken) return true;

        return false;
    }

    /// <summary>
    /// 이 적이 이미 그 증강의 노드인가.
    /// 트리는 같은 노드를 두 번 매달면 부모가 둘이 되어 사이클이 생기고 깊이가 무너진다.
    /// </summary>
    public static bool IsNodeOf(Transform target, AugmentInstance owner)
        => target != null
        && target.TryGetComponent(out LinkHolder holder)
        && holder.BelongsTo(owner);

    // ── 잇고 끊기 ─────────────────────────────────────────

    /// <summary>
    /// 두 노드를 잇는다. 이미 같은 증강이 이어둔 사이면 수명만 갱신한다.
    /// 선 오브젝트는 a 쪽만 들고 있는다.
    /// </summary>
    /// <summary>새로 이었으면 true, 이미 있어서 수명만 갱신했거나 실패하면 false.</summary>
    public static bool Connect(Transform a, Transform b, Link template, GameObject linePrefab)
    {
        if (a == null || b == null || a == b) return false;

        LinkHolder from = GetOrAdd(a);
        LinkHolder to = GetOrAdd(b);

        if (from == null || to == null) return false;

        // 같은 증강이 이미 이은 사이면 새로 긋지 않고 시간만 늘린다
        Link existing = from.Find(to, template.Owner);

        if (existing != null)
        {
            existing.ExpireAt = template.ExpireAt;

            // 반대쪽이 어떤 이유로 먼저 지워졌을 수도 있다
            Link mirror = to.Find(from, template.Owner);
            if (mirror != null) mirror.ExpireAt = template.ExpireAt;

            return false;
        }

        // a 가 부모, b 가 자식이다. 선 오브젝트도 부모 쪽이 들고 있어서 0번 점이 항상 부모가 된다
        from.Add(Copy(template, to, CreateLine(linePrefab, a.position), toChild: true));
        to.Add(Copy(template, from, null, toChild: false));

        return true;
    }

    /// <summary>
    /// 선 오브젝트를 만든다. LineRenderer 는 자식에 있을 수도 있으므로 아래까지 훑는다.
    /// 못 찾으면 간선은 정상 동작하되 눈에만 안 보이므로 반드시 알린다.
    /// </summary>
    static GameObject CreateLine(GameObject prefab, Vector3 at)
    {
        if (prefab == null) return null;

        // 풀을 거친다. 간선은 계속 나고 죽으므로 씬 루트에 쌓이면 하이어라키가 금방 지저분해진다
        GameObject go = PooledSpawner.Spawn(prefab, at, PoolType.Effect);

        if (!go.TryGetComponent(out LineRenderer line))
            line = go.GetComponentInChildren<LineRenderer>();

        if (line == null)
        {
            WarnOnce(prefab, "간선 프리팹에 LineRenderer 가 없어 선이 안 보입니다", go);
            return go;
        }

        // 로컬 좌표로 두면 선이 부모를 따라 엉뚱한 곳에 그려진다
        line.useWorldSpace = true;
        line.positionCount = 2;

        if (line.sharedMaterial == null)
        {
            WarnOnce(prefab, "LineRenderer 에 머티리얼이 없어 선이 안 보입니다. " +
                             "Sprites/Default 머티리얼을 물려주세요", go);
        }

        return go;
    }

    static Link Copy(Link source, LinkHolder other, GameObject visual, bool toChild) => new()
    {
        Other = other,
        ToChild = toChild,
        Owner = source.Owner,
        Amount = source.Amount,
        IsPercent = source.IsPercent,
        Hops = source.Hops,
        MaxPerNode = source.MaxPerNode,
        ExpireAt = source.ExpireAt,
        Visual = visual,
        Line = visual != null ? visual.GetComponentInChildren<LineRenderer>() : null,
        Pulse = visual != null ? visual.GetComponentInChildren<LinkPulse>() : null
    };

    void Add(Link link)
    {
        int cap = link.MaxPerNode > 0 ? link.MaxPerNode : DefaultMaxLinks;

        // 가장 먼 간선부터 밀어낸다. 가까운 연결이 남아야 덩어리 모양이 유지된다 —
        // 오래된 것부터 버리면 방금 만든 국소 구조가 먼저 깨진다
        while (links.Count >= cap) Remove(Farthest());

        links.Add(link);
    }

    /// <summary>가장 멀리 이어진 간선의 자리. 끊어도 모양이 덜 상한다.</summary>
    int Farthest()
    {
        int worst = 0;
        float worstSqr = -1f;

        for (int i = 0; i < links.Count; i++)
        {
            // 이미 끊어진 것이 있으면 그것부터 버린다
            if (links[i].IsBroken) return i;

            float sqr = (links[i].Other.transform.position - transform.position).sqrMagnitude;

            if (sqr <= worstSqr) continue;

            worstSqr = sqr;
            worst = i;
        }

        return worst;
    }

    Link Find(LinkHolder other, AugmentInstance owner)
    {
        for (int i = 0; i < links.Count; i++)
            if (links[i].Other == other && links[i].Owner == owner) return links[i];

        return null;
    }

    /// <summary>이 증강이 이은 간선을 전부 끊는다.</summary>
    public void RemoveByOwner(AugmentInstance owner)
    {
        for (int i = links.Count - 1; i >= 0; i--)
            if (links[i].Owner == owner) Remove(i);
    }

    void Remove(int index)
    {
        Link link = links[index];
        links.RemoveAt(index);

        // 반대쪽에도 같은 간선이 있으므로 같이 지운다
        if (link.Other != null) link.Other.Forget(this, link.Owner);

        Recycle(link.Visual);
    }

    static readonly HashSet<GameObject> warned = new();

    /// <summary>간선은 풀에서 계속 재사용되므로 같은 경고가 매번 뜨면 콘솔이 마비된다.</summary>
    static void WarnOnce(GameObject prefab, string reason, Object context)
    {
        if (!warned.Add(prefab)) return;

        Debug.LogWarning($"[{prefab.name}] {reason}", context);
    }

    /// <summary>선을 풀로 돌려보낸다. 파괴하지 않아야 다음 간선이 재사용한다.</summary>
    static void Recycle(GameObject visual)
    {
        PooledSpawner.Despawn(visual);
    }

    /// <summary>반대쪽이 끊었을 때 내 목록에서만 지운다. 되돌아오지 않게 Remove 를 안 쓴다.</summary>
    void Forget(LinkHolder other, AugmentInstance owner)
    {
        for (int i = links.Count - 1; i >= 0; i--)
        {
            if (links[i].Other != other || links[i].Owner != owner) continue;

            Recycle(links[i].Visual);

            links.RemoveAt(i);
        }
    }

    // ── 유지 ──────────────────────────────────────────────

    void Update()
    {
        if (links.Count == 0) return;

        for (int i = links.Count - 1; i >= 0; i--)
        {
            if (links[i].IsExpired || links[i].IsBroken)
            {
                Remove(i);
                continue;
            }

            DrawLine(links[i]);
        }
    }

    /// <summary>
    /// 선이 두 노드를 계속 따라다니게 한다. 적은 매 프레임 움직인다.
    ///
    /// <b>0번 점은 항상 부모다.</b> 선 오브젝트는 Connect 의 첫 인자 쪽만 들고 있고,
    /// 트리에서 그쪽이 부모이기 때문이다.
    /// 그래서 LineRenderer 의 Width Curve 와 Color Gradient 를 쓰면
    /// 코드 없이 "뿌리는 굵고 끝은 가늘게" 를 표현할 수 있다.
    /// </summary>
    void DrawLine(Link link)
    {
        Vector3 a = transform.position;
        Vector3 b = link.Other.transform.position;

        // 꿀렁이는 연출이 붙어 있으면 그리기를 통째로 맡긴다
        if (link.Pulse != null)
        {
            link.Pulse.SetEnds(a, b);
            return;
        }

        if (link.Line == null) return;

        link.Line.SetPosition(0, a);
        link.Line.SetPosition(1, b);
    }

    // 적이 풀로 반납될 때 간선이 남으면 다음 적이 물려받는다
    void OnDisable()
    {
        for (int i = links.Count - 1; i >= 0; i--) Remove(i);
    }

    // ── 트리 탐색 ─────────────────────────────────────────

    /// <summary>
    /// 이 노드에서 자식 간선을 타고 내려가 <b>더 자랄 수 있는 잎</b>을 모은다.
    /// 트리가 발동마다 한 층씩 자라게 하는 핵심 — 도중의 노드는 건드리지 않고 끝에서만 새 자식이 난다.
    ///
    /// maxDepth 에 닿은 잎은 제외한다. 그래서 시트의 깊이가 곧 트리의 최대 층수가 된다.
    /// </summary>
    public void CollectFrontier(AugmentInstance owner, int maxDepth, List<Transform> results)
    {
        results.Clear();

        // 맞은 자리가 트리 한가운데일 수 있다. 거기서 0부터 세면 깊이가 그만큼 밀려
        // 상한을 넘겨 자란다. 반드시 뿌리까지 거슬러 올라간 뒤 세야 한다
        Walk(RootOf(owner), owner, 0, maxDepth, results, new HashSet<LinkHolder>());
    }

    /// <summary>부모 간선을 타고 끝까지 올라간다. 이 증강이 만든 트리의 뿌리.</summary>
    LinkHolder RootOf(AugmentInstance owner)
    {
        LinkHolder node = this;
        var seen = new HashSet<LinkHolder> { this };

        while (true)
        {
            LinkHolder parent = node.ParentOf(owner);

            // 부모가 없으면 뿌리다. 간선이 꼬여 돌고 있어도 여기서 멈춘다
            if (parent == null || !seen.Add(parent)) return node;

            node = parent;
        }
    }

    /// <summary>나를 자식으로 삼은 노드. 트리라면 하나뿐이다.</summary>
    LinkHolder ParentOf(AugmentInstance owner)
    {
        for (int i = 0; i < links.Count; i++)
            if (!links[i].ToChild && links[i].Owner == owner && !links[i].IsBroken)
                return links[i].Other;

        return null;
    }

    static void Walk(LinkHolder node, AugmentInstance owner, int depth, int maxDepth,
                     List<Transform> results, HashSet<LinkHolder> visited)
    {
        if (node == null || !node.isActiveAndEnabled) return;

        // 간선이 꼬여도 같은 노드를 두 번 밟지 않는다
        if (!visited.Add(node)) return;

        bool hasChild = false;

        for (int i = 0; i < node.links.Count; i++)
        {
            Link link = node.links[i];

            if (!link.ToChild || link.Owner != owner || link.IsBroken) continue;

            hasChild = true;

            // 탐색이 이 간선을 훑고 지나갔다. 전이보다 약하게 — 둘이 구분돼야 한다
            link.Ripple(0.4f);

            Walk(link.Other, owner, depth + 1, maxDepth, results, visited);
        }

        if (hasChild) return;

        // 자식이 없으면 잎이다. 층을 다 쓴 잎은 더 못 자라므로 뺀다
        if (maxDepth <= 0 || depth < maxDepth) results.Add(node.transform);
    }

    // ── 전이 ──────────────────────────────────────────────

    /// <summary>
    /// 이 노드가 맞은 피해를 이웃으로 흘린다. DamagePipeline 7단계가 부른다.
    ///
    /// 전이된 피해도 파이프라인을 그대로 통과하므로 표식 보정과 분류 색이 함께 붙는다.
    /// 되짚기는 visited 로, 무한 확산은 홉 예산으로 막는다.
    /// </summary>
    public void Propagate(DamageContext source)
    {
        if (links.Count == 0 || source.Amount <= 0f) return;

        // 최초 전이면 자기 자신부터 방문 처리한다
        HashSet<Transform> visited = source.LinkVisited ?? new HashSet<Transform> { transform };

        // 전이가 이웃을 죽이면 그쪽 OnDisable 이 이 목록에서도 간선을 지운다.
        // 원본을 그대로 순회하면 사라진 칸을 읽게 되므로 먼저 복사해둔다
        List<Link> snapshot = new(links);

        for (int i = 0; i < snapshot.Count; i++)
        {
            Link link = snapshot[i];
            if (link.IsBroken) continue;

            int hops = source.LinkHops > 0 ? source.LinkHops : link.Hops;
            if (hops <= 0) continue;

            if (!visited.Add(link.Other.transform)) continue;

            float amount = link.Transfer(source.Amount);
            if (amount <= 0f || link.Other.Receiver == null) continue;

            // 앞선 전이가 이 이웃을 이미 죽였을 수 있다
            if (link.IsBroken) continue;

            // 피해가 실제로 이 간선을 탔다. 세게 꿀렁인다
            link.Ripple(1f);

            DamagePipeline.Process(
                new DamageContext(source.Source, link.Other.Receiver, amount,
                                  link.Other.transform)
                {
                    SourceAugment = link.Owner,
                    LinkHops = hops - 1,
                    LinkVisited = visited
                });
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (links.Count == 0) return;

        Gizmos.color = new Color(0.6f, 1f, 0.4f, 0.5f);

        for (int i = 0; i < links.Count; i++)
        {
            if (links[i].IsBroken) continue;

            Gizmos.DrawLine(transform.position, links[i].Other.transform.position);
        }
    }
#endif
}
