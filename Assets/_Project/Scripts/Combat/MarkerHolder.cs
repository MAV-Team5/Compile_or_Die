using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적이 지닌 탐색 표식 목록. 필요할 때 자동으로 붙으므로 프리팹을 손댈 필요가 없다.
/// 표식 오브젝트의 배치와 정리, 그리고 전역 탐색풀 등록도 여기서 맡는다.
/// </summary>
public class MarkerHolder : MonoBehaviour
{
    const float BaseHeight = 0.6f;
    const float Spacing = 0.25f;

    readonly List<SearchMark> marks = new();

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

    /// <summary>표식들이 더해주는 추가 피해 합계.</summary>
    public float TotalBonus(float baseAmount)
    {
        float sum = 0f;

        for (int i = 0; i < marks.Count; i++)
            sum += marks[i].Evaluate(baseAmount);

        return sum;
    }

    void Update()
    {
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

    /// <summary>표식이 겹치지 않게 위로 쌓는다.</summary>
    void Reposition()
    {
        for (int i = 0; i < marks.Count; i++)
        {
            if (marks[i].Visual == null) continue;

            marks[i].Visual.transform.localPosition =
                new Vector3(0f, BaseHeight + i * Spacing, 0f);
        }
    }

#if UNITY_EDITOR
    // 표식 오브젝트가 없어도 씬에서 탐색 대상을 눈으로 찾을 수 있게 한다
    void OnDrawGizmos()
    {
        if (marks.Count == 0) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.35f);

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.9f, $"탐색 {marks.Count}");
    }
#endif
}
