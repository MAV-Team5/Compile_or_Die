using UnityEngine;

/// <summary>적중 1회 정보.</summary>
public struct HitInfo
{
    public Transform Target;
    public Vector2 Point;

    /// <summary>이 Delivery 실행 1회 안에서 몇 번째 적중인가. 관통 순번.</summary>
    public int Index;
}
