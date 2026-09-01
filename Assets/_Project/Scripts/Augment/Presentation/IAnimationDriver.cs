/// <summary>
/// 애니메이션 파라미터를 받아들이는 오브젝트.
///
/// 증강은 <b>인자 이름과 값만</b> 넘긴다. 어떤 모션이 나가고 언제 돌아올지는
/// 전부 Animator 의 상태 기계가 정한다 — 그래서 규칙이 한 곳에만 있다.
///
/// 상태 이름을 직접 재생하지 않는 이유: 상태는 Animator 의 내부 구현이라
/// 이름이 바뀌면 증강이 깨지고, 전이를 건너뛰어 블렌딩·레이어가 어긋난다.
/// 파라미터는 Animator 가 밖에 내주려고 만든 공개 통로다.
/// </summary>
public interface IAnimationDriver
{
    /// <summary>
    /// 파라미터에 값을 넣는다. 타입 해석은 구현이 맡는다 —
    /// Trigger 는 값을 무시하고, Bool 은 0인지 아닌지로, Int·Float 은 그대로 쓴다.
    /// 모르는 이름이면 조용히 넘어가도 된다.
    /// </summary>
    void SetMotion(string parameter, float value);
}
