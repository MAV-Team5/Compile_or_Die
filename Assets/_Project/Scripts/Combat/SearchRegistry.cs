using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 표식이 붙은 적 전체를 모아두는 창구.
/// Stack·Queue·Graph·Flood Fill 처럼 "남이 탐색한 대상"을 쓰는 증강이 여기를 조회한다.
/// </summary>
public static class SearchRegistry
{
    static readonly HashSet<MarkerHolder> tagged = new();

    /// <summary>탐색 목록이 바뀔 때마다 증가. 이벤트 대신 이 값을 폴링해서 변화를 감지한다.</summary>
    public static int Version { get; private set; }

    public static IReadOnlyCollection<MarkerHolder> Tagged => tagged;

    public static void Register(MarkerHolder holder)
    {
        if (holder != null && tagged.Add(holder)) Version++;
    }

    public static void Unregister(MarkerHolder holder)
    {
        if (holder != null && tagged.Remove(holder)) Version++;
    }

    /// <summary>표식이 붙은 적 전부. 결과 리스트는 호출자가 소유한다.</summary>
    public static void CollectAll(List<Transform> results)
    {
        results.Clear();

        foreach (MarkerHolder holder in tagged)
            if (holder != null) results.Add(holder.transform);
    }

    /// <summary>특정 증강이 표식을 남긴 적만.</summary>
    public static void CollectBy(AugmentInstance owner, List<Transform> results)
    {
        results.Clear();

        foreach (MarkerHolder holder in tagged)
            if (holder != null && holder.HasOwner(owner)) results.Add(holder.transform);
    }
}
