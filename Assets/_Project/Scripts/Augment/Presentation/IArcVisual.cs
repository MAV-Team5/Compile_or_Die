/// <summary>
/// 부채꼴 각도를 받아들이는 연출.
///
/// 전달 모듈이 "좌우 몇 도까지 때린다"만 알려주고, 그것을 어떻게 그릴지는 프리팹이 정한다 —
/// 호를 훑든, 부채꼴을 통째로 띄우든, 파티클을 뿌리든 상관없다.
///
/// 이 통로가 있어야 판정 각도와 그림이 어긋나지 않는다.
/// 스프라이트에 45도짜리 부채꼴을 그려두면 halfAngle 을 바꿔도 그림은 그대로라 거짓말이 된다.
/// </summary>
public interface IArcVisual
{
    /// <summary>중심 방향 기준 좌우 각도(도). 180이면 완전한 원.</summary>
    void SetArc(float halfAngleDegrees);
}
