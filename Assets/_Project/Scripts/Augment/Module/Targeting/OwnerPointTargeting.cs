using UnityEngine;

/// <summary>
/// 원점 그 자리를 좌표로 삼는다. 대상을 찾지 않는다.
/// 연쇄 단계에서 원점은 직전에 맞은 적이므로, 그 자리에서 다시 터지는 폭발에 쓴다.
/// </summary>
[System.Serializable]
public class OwnerPointTargeting : TargetingModule
{
    [Tooltip("원점에서 이만큼 떨어진 무작위 위치에 찍는다. 0이면 정확히 원점.")]
    public float scatterRadius = 0f;

    public override void Resolve(AugmentContext ctx)
    {
        Vector2 point = ctx.Owner.position;

        if (scatterRadius > 0f)
            point += Random.insideUnitCircle * scatterRadius;

        ctx.Targets.Add(point);
    }
}
