using UnityEngine;

public class Scanner : MonoBehaviour
{
    public float scanRange;
    public LayerMask targetLayer;

    // 매 물리 프레임 새로 채워지는 결과다. 인스펙터에 두면 조절하는 값처럼 보이고,
    // 씬 파일에도 지난 판의 찌꺼기가 저장된다
    [System.NonSerialized] public RaycastHit2D[] targets;
    [System.NonSerialized] public Transform nearestTarget;

    // 하드웨어(모니터)는 카메라 렌즈만 넓힌다 — 여기 반경은 안 건드린다.
    // 화면이 넓어져도 증강이 적을 찾는 거리는 그대로다

    void FixedUpdate()
    {
        targets = Physics2D.CircleCastAll(transform.position, scanRange, Vector2.zero, 0, targetLayer);
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
