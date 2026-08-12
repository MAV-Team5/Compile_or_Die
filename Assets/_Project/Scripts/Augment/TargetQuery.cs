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

        // 스폰 직후처럼 물리 좌표가 transform 을 아직 따라오지 못한 대상이 섞여 들어온다
        float sqrLimit = radius * radius;

        for (int i = buffer.Count - 1; i >= 0; i--)
        {
            if (((Vector2)buffer[i].transform.position - center).sqrMagnitude > sqrLimit)
                buffer.RemoveAt(i);
        }

        return buffer;
    }

    /// <summary>
    /// 범위 내 적을 results 로 복사한다.
    /// 공용 버퍼는 다음 질의에 덮어써지므로, 적중 콜백을 부를 거라면 반드시 이쪽을 쓸 것.
    /// </summary>
    public static void OverlapInto(Vector2 center, float radius,
                                   Transform skip, List<Transform> results)
    {
        results.Clear();
        Copy(Overlap(center, radius), skip, results);
    }

    /// <summary>회전 사각 영역 안의 적을 results 로 복사한다.</summary>
    public static void OverlapBoxInto(Vector2 center, Vector2 size, float angle,
                                      Transform skip, List<Transform> results)
    {
        results.Clear();
        Copy(OverlapBox(center, size, angle), skip, results);
    }

    /// <summary>skip 은 연쇄 원점(직전에 맞은 적)을 걸러내는 용도다.</summary>
    static void Copy(List<Collider2D> hits, Transform skip, List<Transform> results)
    {
        for (int i = 0; i < hits.Count; i++)
        {
            Transform t = hits[i].transform;
            if (t == skip) continue;

            results.Add(t);
        }
    }

    /// <summary>
    /// 회전된 사각 영역 안의 적. 레이저·직선 판정용.
    /// 반환 리스트는 Overlap 과 같은 공용 버퍼라 즉시 소비할 것.
    /// </summary>
    public static List<Collider2D> OverlapBox(Vector2 center, Vector2 size, float angle)
    {
        buffer.Clear();

        if (!ready)
        {
            Debug.LogWarning("TargetQuery: 레이어가 설정되지 않았습니다");
            return buffer;
        }

        Physics2D.OverlapBox(center, size, angle, filter, buffer);
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