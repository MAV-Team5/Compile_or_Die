using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 큐 증강의 대기열. <b>먼저 들어온 적이 먼저 처리된다.</b>
///
/// 들어온 시각을 같이 들고 있는 것이 핵심이다 — 그래야 "오래 기다린 적일수록 아프다" 가
/// 성립하고, 그때 비로소 FIFO 가 수치를 만든다. 앞에서 빼든 뒤에서 빼든 결과가 같으면
/// 순서는 장식일 뿐이다.
/// </summary>
public class QueueState
{
    public struct Entry
    {
        public Transform Target;
        public float EnqueuedAt;
    }

    readonly List<Entry> items = new();

    public int Count => items.Count;

    public IReadOnlyList<Entry> Items => items;

    public bool Contains(Transform t)
    {
        for (int i = 0; i < items.Count; i++)
            if (items[i].Target == t) return true;

        return false;
    }

    /// <summary>뒤에 붙인다. 이미 있거나 자리가 없으면 false.</summary>
    public bool Enqueue(Transform target, int capacity)
    {
        if (target == null || items.Count >= capacity || Contains(target)) return false;

        items.Add(new Entry { Target = target, EnqueuedAt = Time.time });

        return true;
    }

    /// <summary>맨 앞을 꺼낸다 — 가장 오래 기다린 적이다.</summary>
    public bool Dequeue(out Entry entry)
    {
        if (items.Count == 0)
        {
            entry = default;
            return false;
        }

        entry = items[0];
        items.RemoveAt(0);

        return true;
    }

    /// <summary>
    /// 죽었거나 풀로 돌아간 적을 걷어낸다. <b>순서는 그대로 유지된다.</b>
    ///
    /// 적은 언제든 죽으므로 큐에는 늘 빈 자리가 생긴다. 이걸 안 치우면
    /// 자리만 차지해서 새 적이 못 들어오고, 디큐가 죽은 것을 때리려다 헛돈다.
    /// </summary>
    public void Prune()
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            Transform t = items[i].Target;

            if (t == null || !t.gameObject.activeInHierarchy) items.RemoveAt(i);
        }
    }

    /// <summary>맨 앞이 기다린 시간(초). 비었으면 0.</summary>
    public float FrontWait => items.Count > 0 ? Time.time - items[0].EnqueuedAt : 0f;

    public void Clear() => items.Clear();
}
