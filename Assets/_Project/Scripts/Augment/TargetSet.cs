using System.Collections.Generic;
using UnityEngine;

/// <summary>타겟 1개. 적이거나 좌표거나 둘 중 하나지만 위치는 항상 있다.</summary>
public struct TargetRef
{
    /// <summary>좌표만 노리는 타겟팅이면 null.</summary>
    public Transform Transform;

    /// <summary>Transform 이 없을 때 쓰는 고정 좌표.</summary>
    public Vector2 Point;

    /// <summary>적이면 현재 위치를 따라간다. 좌표면 고정값.</summary>
    public Vector2 Position => Transform != null ? (Vector2)Transform.position : Point;

    public bool IsEnemy => Transform != null;

    /// <summary>적이 사라졌거나 풀에 반납됐는지.</summary>
    public bool IsAlive => Transform == null || Transform.gameObject.activeInHierarchy;
}

/// <summary>
/// 타겟팅 결과. 적과 좌표를 한 목록에 담아서
/// 어떤 Delivery 든 종류를 가리지 않고 처리할 수 있게 한다.
/// </summary>
public class TargetSet
{
    public readonly List<TargetRef> Items = new();

    public int Count => Items.Count;
    public bool IsEmpty => Items.Count == 0;

    public void Clear() => Items.Clear();

    public void Add(Transform enemy)
        => Items.Add(new TargetRef { Transform = enemy });

    public void Add(Vector2 point)
        => Items.Add(new TargetRef { Point = point });
}
