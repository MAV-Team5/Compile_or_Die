using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적을 포물선으로 날려 보내는 공용 규칙. 날리는 효과(Toss · Partition …)가 모두 여기를 거친다.
///
/// <list type="bullet">
/// <item>날아가는 동안 스스로 못 움직이게 묶고, <b>내가 건 잠금만</b> 푼다 — 데드락처럼 원래 묶인 적은 그대로</item>
/// <item>이미 날고 있는 적은 다시 잡지 않는다 — 두 효과가 위치를 서로 덮어써 덜덜 떠는 것을 막는다</item>
/// <item>날아가다 죽거나 풀로 돌아가면 그 자리에서 멈추고 onLost 를 부른다</item>
/// </list>
/// </summary>
public static class EnemyLaunch
{
    static readonly HashSet<Enemy> airborne = new();

    public static bool IsAirborne(Enemy enemy) => enemy != null && airborne.Contains(enemy);

    /// <summary>날릴 수 있는 적인가. 살아 있고, 설치물이 아니고, 이미 날고 있지 않아야 한다.</summary>
    public static bool CanLaunch(Enemy enemy)
        => enemy != null && enemy.IsAlive && !enemy.IsStationary && !airborne.Contains(enemy);

    /// <summary>
    /// enemy 를 to 까지 날린다. 못 날리면 false. 착지하면 onLanded(착지점), 도중에 잃으면 onLost.
    /// </summary>
    public static bool Launch(Enemy enemy, Vector2 to, float duration, float height,
                              System.Action<Vector2> onLanded, System.Action onLost = null)
    {
        if (!CanLaunch(enemy)) return false;

        bool lockedByMe = !enemy.movementLocked;
        enemy.movementLocked = true;
        airborne.Add(enemy);

        ArcMover.Move(enemy.transform, enemy.transform.position, () => to, duration, height,
            landed =>
            {
                Release(enemy, lockedByMe);
                onLanded?.Invoke(landed);
            },
            () =>
            {
                if (enemy != null && enemy.IsAlive) return true;

                Release(enemy, lockedByMe);
                onLost?.Invoke();
                return false;
            });

        return true;
    }

    static void Release(Enemy enemy, bool lockedByMe)
    {
        airborne.Remove(enemy);
        if (lockedByMe && enemy != null) enemy.movementLocked = false;
    }
}
