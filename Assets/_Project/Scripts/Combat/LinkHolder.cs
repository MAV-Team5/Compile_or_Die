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
    /// 노드 하나가 가질 수 있는 간선 상한.
    /// 간선은 탐색과 달리 저절로 안 풀리므로 상한이 없으면 무한히 쌓인다.
    /// </summary>
    const int MaxLinks = 8;

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

    // ── 잇고 끊기 ─────────────────────────────────────────

    /// <summary>
    /// 두 노드를 잇는다. 이미 같은 증강이 이어둔 사이면 수명만 갱신한다.
    /// 선 오브젝트는 a 쪽만 들고 있는다.
    /// </summary>
    public static void Connect(Transform a, Transform b, Link template, GameObject linePrefab)
    {
        if (a == null || b == null || a == b) return;

        LinkHolder from = GetOrAdd(a);
        LinkHolder to = GetOrAdd(b);

        if (from == null || to == null) return;

        // 같은 증강이 이미 이은 사이면 새로 긋지 않고 시간만 늘린다
        Link existing = from.Find(to, template.Owner);

        if (existing != null)
        {
            existing.ExpireAt = template.ExpireAt;
            to.Find(from, template.Owner).ExpireAt = template.ExpireAt;
            return;
        }

        GameObject visual = linePrefab != null
            ? Instantiate(linePrefab, a.position, Quaternion.identity)
            : null;

        from.Add(Copy(template, to, visual));
        to.Add(Copy(template, from, null));
    }

    static Link Copy(Link source, LinkHolder other, GameObject visual) => new()
    {
        Other = other,
        Owner = source.Owner,
        Amount = source.Amount,
        IsPercent = source.IsPercent,
        Hops = source.Hops,
        ExpireAt = source.ExpireAt,
        Visual = visual
    };

    void Add(Link link)
    {
        // 오래된 것부터 밀어낸다. 최신 연결이 살아남는 편이 플레이어가 읽기 쉽다
        while (links.Count >= MaxLinks) Remove(0);

        links.Add(link);
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

        if (link.Visual != null) Destroy(link.Visual);
    }

    /// <summary>반대쪽이 끊었을 때 내 목록에서만 지운다. 되돌아오지 않게 Remove 를 안 쓴다.</summary>
    void Forget(LinkHolder other, AugmentInstance owner)
    {
        for (int i = links.Count - 1; i >= 0; i--)
        {
            if (links[i].Other != other || links[i].Owner != owner) continue;

            if (links[i].Visual != null) Destroy(links[i].Visual);

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

    /// <summary>선이 두 노드를 계속 따라다니게 한다. 적은 매 프레임 움직인다.</summary>
    void DrawLine(Link link)
    {
        if (link.Visual == null) return;

        if (!link.Visual.TryGetComponent(out LineRenderer line)) return;

        line.positionCount = 2;
        line.SetPosition(0, transform.position);
        line.SetPosition(1, link.Other.transform.position);
    }

    // 적이 풀로 반납될 때 간선이 남으면 다음 적이 물려받는다
    void OnDisable()
    {
        for (int i = links.Count - 1; i >= 0; i--) Remove(i);
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
