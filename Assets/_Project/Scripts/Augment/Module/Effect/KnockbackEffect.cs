using UnityEngine;

/// <summary>
/// 적중 대상을 공격이 온 방향으로 밀거나 당긴다.
/// 정렬 계열처럼 적을 모으거나 흩는 군중 제어 전용.
/// </summary>
[System.Serializable]
[ModuleInfo("밀거나 당긴다", "정렬 계열의 군중 제어")]
public class KnockbackEffect : EffectModule
{
    [Tooltip("밀려나는 거리(유닛). 시트와 무관한 고정값. 플레이어 크기를 1로 보고 감을 잡으면 된다.")]
    public float distance = 1.5f;

    [Tooltip("켜면 반대로 끌어당긴다. 폭발이면 중심으로 모이고, 투사체면 시전자 쪽으로 온다.\n" +
             "Selection Sort 같은 견인 전용.")]
    public bool pull = false;

    [Tooltip("당길 때 켜면 폭발 중심까지만 끌어온다 — 중심을 넘어가지 않고, 중심에 선 적은 그대로 둔다.\n" +
             "Area 로 한 점에 모을 때 쓴다. 투사체 견인처럼 중심이 따로 없는 전달에는 끌 것.")]
    public bool gatherToCenter = false;

    [Tooltip("밀린 뒤 대상이 스스로 못 움직이는 시간(초). 0이면 즉시 다시 다가온다.")]
    public float stunDuration = 0.2f;

    [Tooltip("켜면 엘리트·보스도 밀린다. 끄면 그들은 밀리지도 멈추지도 않는다 — 큰 적이 휘둘리면 위협감이 사라진다.")]
    public bool affectsHeavy = false;

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null) return;
        if (!affectsHeavy && IsHeavy(hit.Target)) return;

        Vector2 direction = ResolveDirection(ctx, hit);
        if (direction == Vector2.zero) return;

        Vector2 push = pull && gatherToCenter
            ? GatherPush(hit)
            : direction * distance * (pull ? -1f : 1f);

        // 적이 매 프레임 스스로 이동하므로, 잠시 멈추게 하지 않으면 넉백이 즉시 지워진다
        if (hit.Target.TryGetComponent(out IDisplaceable displaceable))
            displaceable.Displace(push, stunDuration);
        else
            hit.Target.position += (Vector3)push;
    }

    /// <summary>적중 지점(폭발 중심) 쪽으로, 남은 거리만큼만 끌어온다.</summary>
    Vector2 GatherPush(HitInfo hit)
    {
        Vector2 toCenter = hit.Point - (Vector2)hit.Target.position;
        return Vector2.ClampMagnitude(toCenter, distance);
    }

    /// <summary>엘리트·보스인가. 보스는 데이터 없이 나올 수도 있어 표식도 함께 본다.</summary>
    static bool IsHeavy(Transform target)
    {
        Enemy enemy = target.GetComponentInParent<Enemy>();
        if (enemy == null) return false;

        return !enemy.IsMinion || enemy.TryGetComponent(out BossMarker _);
    }

    /// <summary>
    /// 밀어낼 방향. 공격이 실제로 온 쪽을 그대로 쓴다 —
    /// 투사체는 비행 방향, 레이저는 발사 방향, 폭발은 중심에서 바깥.
    /// </summary>
    static Vector2 ResolveDirection(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Direction.sqrMagnitude > 0.0001f) return hit.Direction.normalized;

        // 방향을 알 수 없는 전달이면 원점에서 대상 쪽으로 물러난다
        Vector2 delta = (Vector2)hit.Target.position - (Vector2)ctx.Owner.position;

        return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.zero;
    }
}
