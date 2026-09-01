/// <summary>
/// 런이 지금 어느 단계인가.
///
/// 지금은 셋뿐이지만, 레이어(Source→Runtime→Assembly)가 붙을 자리를 위해
/// 불리언이 아니라 열거로 둔다. 단계가 늘어도 판정하는 쪽 코드가 안 바뀐다.
/// </summary>
public enum RunState
{
    /// <summary>진행 중. 시간이 계속 쌓이고 적이 나온다.</summary>
    Playing,

    /// <summary>최종 보스를 잡았다. 컴파일 성공.</summary>
    Cleared,

    /// <summary>플레이어가 죽었다.</summary>
    Failed
}
