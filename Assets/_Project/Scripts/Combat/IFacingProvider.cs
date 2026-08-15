using UnityEngine;

/// <summary>
/// 바라보는 방향을 알려주는 대상. 증강이 시전자 방향을 알아야 할 때 이걸 통해 묻는다.
/// 증강 시스템이 Player 를 직접 알지 않도록 끊어두는 계약이다.
/// </summary>
public interface IFacingProvider
{
    /// <summary>멈춰 있어도 마지막으로 향했던 방향을 유지한다. 정규화된 값.</summary>
    Vector2 Facing { get; }
}
