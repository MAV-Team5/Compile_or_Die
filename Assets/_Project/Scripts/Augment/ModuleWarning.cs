using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조립이 잘못된 증강을 알리는 창구.
///
/// 이런 경고는 발동할 때마다 나므로 그냥 찍으면 콘솔이 마비된다 —
/// 방사 8발이면 프레임당 8줄이고, Debug.LogWarning 은 스택트레이스를 모아서 비싸다.
/// 같은 증강의 같은 문제는 한 번만 알린다. 고칠 정보는 첫 줄에 다 들어있다.
/// </summary>
public static class ModuleWarning
{
    static readonly HashSet<string> shown = new();

    /// <summary>이 증강의 이 문제를 처음 만났을 때만 경고한다.</summary>
    public static void Once(AugmentContext ctx, string reason)
    {
        string augment = ctx?.Instance?.Data != null ? ctx.Instance.Data.name : "이름 없는 증강";

        if (!shown.Add($"{augment}/{reason}")) return;

        Debug.LogWarning($"[{augment}] {reason}");
    }

    /// <summary>런을 다시 시작할 때 부른다. 안 부르면 에디터 세션 내내 침묵한다.</summary>
    public static void Reset() => shown.Clear();
}
