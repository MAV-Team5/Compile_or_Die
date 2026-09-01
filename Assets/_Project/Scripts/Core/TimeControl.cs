using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시간이 흐르는지를 정하는 유일한 곳. <c>Time.timeScale</c> 을 대입하는 코드는 여기뿐이다.
///
/// <b>왜 따로 두나</b> — 멈추는 이유가 여럿이고 서로 겹친다.
/// 레벨업 카드가 떠 있는데 일시정지를 눌렀다 풀면, 각자 timeScale 을 만지던 시절에는
/// <b>카드가 떠 있는 채로 게임이 다시 돌아갔다.</b>
///
/// 그래서 "멈춰라"가 아니라 <b>"내 이유로 붙잡는다"</b>로 다룬다.
/// 붙잡은 이유가 하나라도 남아 있으면 시간은 안 흐른다.
///
/// <code>
/// TimeControl.Hold(this);      // 내가 붙잡는다
/// TimeControl.Release(this);   // 내 볼일은 끝났다 (남이 붙잡고 있으면 여전히 멈춤)
/// </code>
/// </summary>
public static class TimeControl
{
    /// <summary>지금 시간을 붙잡고 있는 것들. 비면 시간이 흐른다.</summary>
    static readonly HashSet<object> holders = new();

    public static bool IsFrozen => holders.Count > 0;

    /// <summary>지금 붙잡고 있는 수. 디버그용.</summary>
    public static int HoldCount => holders.Count;

    /// <summary>이 이유로 시간을 멈춘다. 같은 이유로 여러 번 불러도 한 번으로 친다.</summary>
    public static void Hold(object reason)
    {
        if (reason == null) return;

        holders.Add(reason);
        Apply();
    }

    /// <summary>이 이유를 놓는다. 다른 이유가 남아 있으면 여전히 멈춰 있다.</summary>
    public static void Release(object reason)
    {
        if (reason == null) return;

        holders.Remove(reason);
        Apply();
    }

    /// <summary>
    /// 전부 놓고 시간을 흐르게 한다. 씬이 바뀔 때 부른다 —
    /// 붙잡고 있던 오브젝트가 씬과 함께 파괴되면 스스로 놓을 수 없기 때문.
    /// </summary>
    public static void ReleaseAll()
    {
        holders.Clear();
        Apply();
    }

    static void Apply() => Time.timeScale = holders.Count > 0 ? 0f : 1f;
}
