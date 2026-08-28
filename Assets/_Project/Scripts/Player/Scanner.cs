using UnityEngine;

public class Scanner : MonoBehaviour
{
    public float scanRange;
    public LayerMask targetLayer;
    public RaycastHit2D[] targets;
    public Transform nearestTarget;

    /// <summary>
    /// 하드웨어(모니터)가 올리는 시야 배율. 1이면 보정 없음. HardwareLoader 가 채운다.
    ///
    /// <b>scanRange 를 직접 곱하지 않는다.</b> 인스펙터 값이 곧 원본이어야
    /// 두 번 주입하거나 씬을 다시 열었을 때 값이 누적되지 않는다.
    /// </summary>
    public float RangeMultiplier { get; set; } = 1f;

    /// <summary>배율까지 반영한 실제 탐지 반경.</summary>
    public float CurrentRange => scanRange * RangeMultiplier;

    void FixedUpdate()
    {
        targets = Physics2D.CircleCastAll(transform.position, CurrentRange, Vector2.zero, 0, targetLayer);
        nearestTarget = GetNearest();
    }

    Transform GetNearest()
    {
        Transform result = null;
        float diff = 100;

        foreach (RaycastHit2D target in targets)
        {
            Vector3 myPos = transform.position;
            Vector3 targetPos = target.transform.position;
            float curDiff = Vector3.Distance(myPos, targetPos);

            if (curDiff < diff)
            {
                diff = curDiff;
                result = target.transform;
            }
        }

        return result;
    }
}
