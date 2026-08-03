using System.Collections.Generic;
using UnityEngine;

/// <summary>타겟팅 결과. 적과 좌표를 함께 담는다.</summary>
public class TargetSet
{
    public readonly List<Transform> Enemies = new();
    public readonly List<Vector2> Points = new();

    public bool IsEmpty => Enemies.Count == 0 && Points.Count == 0;

    public void Clear()
    {
        Enemies.Clear();
        Points.Clear();
    }
}