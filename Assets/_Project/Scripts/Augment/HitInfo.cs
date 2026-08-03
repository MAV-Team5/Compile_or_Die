using UnityEngine;

/// <summary>적중 1회 정보.</summary>
public struct HitInfo
{
    public Transform Target;
    public Vector2 Point;
    public int Index;      // 체인 몇 번째 / 투사체 몇 번째
}