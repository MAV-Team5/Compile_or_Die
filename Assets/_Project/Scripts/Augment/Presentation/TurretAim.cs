using UnityEngine;

/// <summary>
/// 소환물의 포신을 가장 가까운 적 쪽으로 돌린다. 공격은 하지 않는다 — 공격은 러너의 파이프라인 몫.
/// 대상이 없으면 좌우로 천천히 훑어 "찾는 중"임을 보여준다.
/// 스프라이트는 위쪽(+Y)이 포신 방향이어야 한다.
/// </summary>
public class TurretAim : MonoBehaviour
{
    [Tooltip("초당 회전 각도. 빠를수록 실제 탄 방향과 어긋나지 않는다.")]
    [SerializeField] float turnSpeed = 720f;

    [Tooltip("대상을 다시 찾는 간격(초). 매 프레임 찾을 필요는 없다.")]
    [SerializeField] float retargetInterval = 0.1f;

    [Tooltip("러너의 사거리를 못 읽을 때 쓸 탐색 반경(유닛).")]
    [SerializeField] float fallbackRange = 5f;

    [Header("대상이 없을 때")]
    [SerializeField] float idleSweepAngle = 40f;
    [SerializeField] float idleSweepSpeed = 1.2f;

    AugmentRunner runner;
    Transform target;
    float nextRetargetAt;
    float idleCenterAngle;

    /// <summary>지금 포신이 보는 방향.</summary>
    public Vector2 Facing => transform.up;

    void OnEnable()
    {
        target = null;
        nextRetargetAt = 0f;
        idleCenterAngle = transform.eulerAngles.z;
    }

    void Update()
    {
        // 러너는 Summon 이 생성 직후에 붙이므로 OnEnable 시점엔 아직 없다
        if (runner == null) runner = GetComponentInParent<AugmentRunner>();

        if (Time.time >= nextRetargetAt)
        {
            target = TargetQuery.Nearest(transform.position, Range);
            nextRetargetAt = Time.time + retargetInterval;
        }

        bool hasTarget = target != null && target.gameObject.activeInHierarchy;
        float goal = hasTarget ? AngleTo(target.position) : IdleAngle();
        if (hasTarget) idleCenterAngle = goal;

        float next = Mathf.MoveTowardsAngle(transform.eulerAngles.z, goal, turnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, next);
    }

    float Range => runner != null && runner.Instance != null ? runner.Instance.Stat.range : fallbackRange;

    float AngleTo(Vector2 point)
    {
        Vector2 dir = point - (Vector2)transform.position;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // +Y 기준 스프라이트라 -90
    }

    float IdleAngle() => idleCenterAngle + Mathf.Sin(Time.time * idleSweepSpeed) * idleSweepAngle;
}
