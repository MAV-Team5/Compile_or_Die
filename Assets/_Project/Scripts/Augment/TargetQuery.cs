using System.Collections.Generic;
using UnityEngine;

/// <summary>증강 타겟팅용 적 검색. AugmentManager가 레이어를 주입한다.</summary>
public static class TargetQuery
{
    static ContactFilter2D filter;
    static readonly List<Collider2D> buffer = new();
    public static LayerMask Mask { get; private set; }
    static bool ready;

    public static void SetLayer(LayerMask mask)
    {
        Mask = mask;
        filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = mask,
            useTriggers = true
        };
        ready = true;
    }

    /// <summary>범위 내 적 목록. 버퍼를 재사용하므로 즉시 소비할 것.</summary>
    public static List<Collider2D> Overlap(Vector2 center, float radius)
    {
        buffer.Clear();

        if (!ready)
        {
            Debug.LogWarning("EnemyQuery: 레이어가 설정되지 않았습니다");
            return buffer;
        }

        Physics2D.OverlapCircle(center, radius, filter, buffer);
        return buffer;
    }

    /// <summary>가장 가까운 1체. exclude에 든 대상은 건너뛴다.</summary>
    public static Transform Nearest(Vector2 center, float radius, HashSet<Transform> exclude = null)
    {
        Transform best = null;
        float bestSqr = float.MaxValue;

        List<Collider2D> hits = Overlap(center, radius);

        for (int i = 0; i < hits.Count; i++)
        {
            Transform t = hits[i].transform;
            if (exclude != null && exclude.Contains(t)) continue;

            float sqr = ((Vector2)t.position - center).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = t;
            }
        }

        return best;
    }
}