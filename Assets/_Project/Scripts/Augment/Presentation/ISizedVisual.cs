using UnityEngine;

/// <summary>
/// 판정 크기를 알아야 하는 연출 프리팹이 구현한다.
/// 스포너는 반경만 건네고, 그걸로 스케일을 바꿀지 파티클 설정을 만질지는 프리팹이 정한다.
/// 안 붙이면 크기 정보를 그냥 무시한다.
/// </summary>
public interface ISizedVisual
{
    /// <summary>이 연출이 표현해야 할 판정 반경(월드 유닛). 0이 들어오지 않는다.</summary>
    void Resize(float radius);
}
