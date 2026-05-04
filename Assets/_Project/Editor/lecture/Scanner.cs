using UnityEngine;

/// <summary>
/// 범위 내 적 탐지 스크립트
/// Physics2D.CircleCastAll로 원형 범위 탐지 후 가장 가까운 적 반환
/// Player 오브젝트에 부착. Weapon.Fire()에서 nearestTarget 참조
/// </summary>
public class Scanner : MonoBehaviour
{
    [Header("# 탐지 설정")]
    public float scanRange;         // 탐지 반경 (Inspector에서 설정, 권장: 5)
    public LayerMask targetLayer;   // 탐지 대상 레이어 (Enemy 레이어 선택)

    [Header("# 탐지 결과")]
    public RaycastHit2D[] targets;  // 범위 내 모든 적 목록
    public Transform nearestTarget; // 가장 가까운 적 Transform

    void FixedUpdate()
    {
        // CircleCastAll: 원형 범위 내 모든 콜라이더 탐지
        // Vector2.zero + 거리 0: 방향 없이 그 자리에서만 원형 탐지
        targets = Physics2D.CircleCastAll(
            transform.position, // 탐지 중심 (플레이어 위치)
            scanRange,          // 탐지 반경
            Vector2.zero,       // 방향 (없음)
            0,                  // 이동 거리 (0 = 제자리 원형)
            targetLayer         // Enemy 레이어만 탐지
        );

        nearestTarget = GetNearest();
    }

    /// <summary>
    /// targets 배열에서 가장 가까운 적의 Transform 반환
    /// 없으면 null 반환 (Weapon.Fire()에서 null 체크 필수)
    /// </summary>
    Transform GetNearest()
    {
        Transform result = null;
        float minDist    = 100f; // 초기값을 충분히 크게 설정

        foreach (RaycastHit2D target in targets)
        {
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                result  = target.transform;
            }
        }

        return result;
    }
}
