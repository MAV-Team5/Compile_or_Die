using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지금 어느 화면이 떠 있는지를 아는 유일한 곳.
///
/// <b>화면을 그리지 않는다.</b> 그리는 것은 각 화면(GameHud · AugmentSelectUI · MenuManager)이
/// 계속 맡는다. 여기가 정하는 것은 <b>순서와 멈춤</b>뿐이다 —
/// 나중에 연 것이 위로 오고, 멈춰야 하는 화면이 하나라도 열려 있으면 시간이 안 흐른다.
///
/// 화면이 각자 timeScale 을 만지면 서로를 밟는다. 실제로 카드가 떠 있는데
/// 일시정지를 눌렀다 풀면 게임이 뒤에서 다시 돌던 버그가 있었다.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Current { get; private set; }

    /// <summary>어떤 화면인가. 겹칠 때 누가 위인지 정하는 데도 쓴다.</summary>
    public enum Screen
    {
        AugmentSelect,
        Pause,
        Result
    }

    /// <summary>연 순서대로. 마지막 것이 맨 위다.</summary>
    readonly List<Screen> stack = new();

    /// <summary>이 화면들이 떠 있는 동안에는 게임이 멈춘다.</summary>
    static readonly HashSet<Screen> Freezing = new() { Screen.AugmentSelect, Screen.Pause };

    /// <summary>맨 위 화면. 아무것도 없으면 null.</summary>
    public Screen? Top => stack.Count > 0 ? stack[^1] : null;

    public bool IsOpen(Screen screen) => stack.Contains(screen);

    /// <summary>화면이 하나라도 떠 있는가. 게임 입력을 막을지 판단하는 데 쓴다.</summary>
    public bool AnyOpen => stack.Count > 0;

    void Awake()
    {
        Current = this;

        // 씬을 넘어오며 멈춘 채 시작하지 않게. 붙잡고 있던 오브젝트는 이미 파괴됐다
        stack.Clear();
        TimeControl.ReleaseAll();
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    /// <summary>화면을 연다. 이미 떠 있으면 맨 위로 올린다.</summary>
    public void Open(Screen screen)
    {
        stack.Remove(screen);
        stack.Add(screen);

        Sync();
    }

    /// <summary>화면을 닫는다. 안 떠 있으면 아무 일도 없다.</summary>
    public void Close(Screen screen)
    {
        if (!stack.Remove(screen)) return;

        Sync();
    }

    /// <summary>맨 위 화면을 닫는다. ESC 처리에 쓴다.</summary>
    public Screen? CloseTop()
    {
        if (stack.Count == 0) return null;

        Screen top = stack[^1];
        Close(top);

        return top;
    }

    /// <summary>
    /// 스택 상태를 시간에 반영한다.
    /// 멈춰야 하는 화면마다 따로 붙잡으므로, 둘이 겹쳤다가 하나만 닫혀도 안 풀린다.
    /// </summary>
    void Sync()
    {
        foreach (Screen screen in Freezing)
        {
            if (stack.Contains(screen)) TimeControl.Hold(Key(screen));
            else TimeControl.Release(Key(screen));
        }
    }

    /// <summary>
    /// TimeControl 에 넘길 이유표. 열거값은 박싱될 때마다 다른 객체가 되어
    /// HashSet 이 같은 것으로 못 알아보므로, 화면마다 고정된 문자열을 쓴다.
    /// </summary>
    static string Key(Screen screen) => screen switch
    {
        Screen.AugmentSelect => "UI.AugmentSelect",
        Screen.Pause => "UI.Pause",
        Screen.Result => "UI.Result",
        _ => "UI." + screen
    };
}
