using UnityEngine;

/// <summary>적중 대상을 발사 원점 기준으로 밀거나 당긴다.</summary>
[System.Serializable]
public class KnockbackEffect : EffectModule
{
    [Tooltip("밀려나는 거리(유닛). 플레이어 크기를 1로 보고 감을 잡으면 된다.")]
    public float distance = 1.5f;

    [Tooltip("켜면 원점 쪽으로 끌어당긴다. Selection Sort 같은 견인 계열용.")]
    public bool pull = false;

    [Tooltip("밀린 뒤 대상이 스스로 못 움직이는 시간(초). 0이면 즉시 다시 다가온다.")]
    public float stunDuration = 0.2f;

    public override void Apply(AugmentContext ctx, HitInfo hit)
    {
        if (hit.Target == null) return;

        Vector2 origin = ctx.Owner.position;
        Vector2 delta = (Vector2)hit.Target.position - origin;

        if (delta.sqrMagnitude < 0.0001f) return;

        Vector2 push = delta.normalized * distance * (pull ? -1f : 1f);

        // 적이 매 프레임 스스로 이동하므로, 잠시 멈추게 하지 않으면 넉백이 즉시 지워진다
        if (hit.Target.TryGetComponent(out IDisplaceable displaceable))
            displaceable.Displace(push, stunDuration);
        else
            hit.Target.position += (Vector3)push;
    }
}
