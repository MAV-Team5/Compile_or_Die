using UnityEngine;

/// <summary>적중 1회 정보. 전달 모듈이 채워 효과 모듈에 넘긴다.</summary>
public struct HitInfo
{
    public Transform Target;
    public Vector2 Point;

    /// <summary>이 Delivery 실행 1회 안에서 몇 번째 적중인가. 관통 순번.</summary>
    public int Index;

    /// <summary>
    /// 적중한 순간의 진행 방향(정규화). 투사체 비행 방향 · 레이저 방향 · 폭발이 퍼진 방향.
    /// 하위 파이프라인이 이 방향을 물려받아 "가던 쪽으로 계속" 퍼질 수 있다.
    /// 방향을 알 수 없는 경우 zero.
    /// </summary>
    public Vector2 Direction;
}
