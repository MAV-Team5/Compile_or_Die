using UnityEngine;

/// <summary>
/// 방향을 알아야 하는 연출 프리팹이 구현한다.
/// 스포너는 방향만 건네고, 그걸로 회전할지 8방향 클립을 고를지는 프리팹이 정한다.
/// 안 붙이면 방향 정보를 그냥 무시한다.
/// </summary>
public interface IDirectionalVisual
{
    /// <summary>이 연출이 향할 방향(정규화). zero 가 들어오지 않는다.</summary>
    void Aim(Vector2 direction);
}
