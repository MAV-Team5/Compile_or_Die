using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적이 지닌 탐색 표식 목록. 필요할 때 자동으로 붙으므로 프리팹을 손댈 필요가 없다.
/// 표식 오브젝트의 생성·배치·정리와 전역 탐색풀 등록을 맡는다.
///
/// 위치와 크기는 적 프리팹의 MarkAnchor 가 정한다. 코드는 몸집을 재지 않는다.
/// </summary>
public class MarkerHolder : MonoBehaviour
{
    /// <summary>MarkAnchor 가 없는 적에게 쓰는 기본 간격.</summary>
    const float FallbackSpacing = 0.25f;

    /// <summary>MarkAnchor 가 없을 때 본체 위로 얼마나 띄울지.</summary>
    const float FallbackHeight = 0.6f;

    readonly List<SearchMark> marks = new();

    MarkAnchor anchor;
    bool anchorSearched;

    public int Count => marks.Count;

    /// <summary>없으면 붙여서 돌려준다.</summary>
    public static MarkerHolder GetOrAdd(Transform target)
    {
        if (target == null) return null;

        return target.TryGetComponent(out MarkerHolder holder)
            ? holder
            : target.gameObject.AddComponent<MarkerHolder>();
    }

    /// <summary>이 증강의 표식을 지니고 있는지. 탐색풀 조회에 쓴다.</summary>
    public bool HasOwner(AugmentInstance owner)
    {
        for (int i = 0; i < marks.Count; i++)
            if (marks[i].Owner == owner) return true;

        return false;
    }

    /// <summary>같은 증강의 표식이 이미 있으면 덮어쓴다.</summary>
    public void Apply(SearchMark mark)
    {
        RemoveByOwner(mark.Owner);

        marks.Add(mark);

        CreateVisual(mark);
        Reposition();
        SyncRegistry();
    }

    /// <summary>특정 증강이 남긴 표식만 걷어낸다. 쿨타임 갱신 시 지난 탐색 해제용.</summary>
    public void RemoveByOwner(AugmentInstance owner)
    {
        bool changed = false;

        for (int i = marks.Count - 1; i >= 0; i--)
        {
            if (marks[i].Owner != owner) continue;

            Remove(i);
            changed = true;
        }

        if (!changed) return;

        Reposition();
        SyncRegistry();
    }

    /// <summary>
    /// 표식들이 더해주는 추가 피해 합계. 실제로 들어간 표식은 연출도 낸다.
    /// 표식은 사라지지 않는다 — 지속되며 매 공격마다 다시 얹힌다.
    /// </summary>
    public float Consume(float baseAmount)
    {
        float sum = 0f;

        for (int i = 0; i < marks.Count; i++)
        {
            float bonus = marks[i].Evaluate(baseAmount);
            if (bonus <= 0f) continue;

            sum += bonus;

            // 표식이 붙어 있는 자리에서 터뜨린다. 오브젝트가 없으면 적 위치
            Vector2 at = marks[i].Visual != null
                ? (Vector2)marks[i].Visual.transform.position
                : (Vector2)transform.position;

            marks[i].PlayBurst(at, transform);
        }

        return sum;
    }

    void Update()
    {
        // 이 컴포넌트는 한 번 붙으면 안 떨어진다. 표식이 없는 동안 헛돌지 않게 막는다
        if (marks.Count == 0) return;

        bool changed = false;

        for (int i = marks.Count - 1; i >= 0; i--)
        {
            if (!marks[i].IsExpired) continue;

            Remove(i);
            changed = true;
        }

        if (!changed) return;

        Reposition();
        SyncRegistry();
    }

    // 적이 풀로 반납될 때 표식이 남아있으면 다음 적이 물려받는다
    void OnDisable()
    {
        for (int i = marks.Count - 1; i >= 0; i--) Remove(i);

        SearchRegistry.Unregister(this);
    }

    void Remove(int index)
    {
        if (marks[index].Visual != null) Destroy(marks[index].Visual);

        marks.RemoveAt(index);
    }

    /// <summary>표식이 하나라도 있으면 탐색풀에, 없으면 빠진다.</summary>
    void SyncRegistry()
    {
        if (marks.Count > 0) SearchRegistry.Register(this);
        else SearchRegistry.Unregister(this);
    }

    /// <summary>표식이 붙을 자리. 프리팹에 MarkAnchor 가 없으면 본체를 쓴다.</summary>
    Transform Mount
    {
        get
        {
            if (!anchorSearched)
            {
                anchor = GetComponentInChildren<MarkAnchor>();
                anchorSearched = true;
            }

            return anchor != null ? anchor.transform : transform;
        }
    }

    float Spacing => anchor != null ? anchor.Spacing : FallbackSpacing;
    float BaseHeight => anchor != null ? 0f : FallbackHeight;

    void CreateVisual(SearchMark mark)
    {
        if (mark.VisualPrefab == null) return;

        // worldPositionStays 를 켜둬야(기본값) 프리팹에 그린 크기로 먼저 나온다.
        // 부모 스케일이 상쇄되므로 여기서 잰 크기가 곧 원본 크기다
        mark.Visual = Instantiate(mark.VisualPrefab, Mount);
        mark.Visual.name = $"Mark_{mark.Owner.Data.name}";

        FitToAnchor(mark.Visual);
    }

    /// <summary>
    /// 표식을 앵커 칸 크기에 맞춰 줄인다. 원본이 512픽셀이든 64픽셀이든 결과가 같다.
    /// 긴 변을 기준으로 맞춰야 칸 밖으로 삐져나오지 않는다.
    /// </summary>
    void FitToAnchor(GameObject visual)
    {
        if (anchor == null || anchor.size <= 0.0001f) return;

        Renderer renderer = visual.GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        Vector3 natural = renderer.bounds.size;
        float longest = Mathf.Max(natural.x, natural.y);

        if (longest <= 0.0001f) return;

        visual.transform.localScale *= anchor.size / longest;
    }

    /// <summary>
    /// 표식이 겹치지 않게 위로 쌓는다. 크기는 손대지 않는다 — 프리팹이 그린 그대로.
    /// 간격도 월드 단위라 적 스케일에 휘둘리지 않는다.
    /// </summary>
    void Reposition()
    {
        Vector3 origin = Mount.position + Vector3.up * BaseHeight;
        float spacing = Spacing;

        for (int i = 0; i < marks.Count; i++)
        {
            if (marks[i].Visual == null) continue;

            marks[i].Visual.transform.position = origin + Vector3.up * (i * spacing);
        }
    }

#if UNITY_EDITOR
    // 표식 오브젝트가 없어도 씬에서 탐색 대상을 눈으로 찾을 수 있게 한다
    void OnDrawGizmos()
    {
        if (marks.Count == 0) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.35f);

        string where = anchor != null ? $"칸 {anchor.size:F2}" : "앵커 없음 — 원본 크기";

        UnityEditor.Handles.Label(
            Mount.position + Vector3.up * 0.4f, $"탐색 {marks.Count} · {where}");
    }
#endif
}
