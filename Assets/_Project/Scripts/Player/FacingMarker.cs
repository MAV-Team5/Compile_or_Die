using UnityEngine;

/// <summary>
/// 플레이어가 어디를 보는지 알려주는 표시. 캐릭터 둘레를 도는 화살표다.
///
/// <b>각도를 보간한다.</b> 위치를 직접 옮기면 방향이 바뀔 때 지름길로 가로질러
/// 캐릭터를 뚫고 지나간다. 각도를 돌리면 저절로 호를 그린다 —
/// 오른쪽에서 왼쪽으로 바꾸면 반 바퀴를 크게 돌아간다.
///
/// <b>Player 의 자식으로 둘 것.</b> 플레이어는 회전하지 않으므로
/// localPosition 이 곧 월드 방향이 되어 계산이 단순해진다.
/// </summary>
public class FacingMarker : MonoBehaviour
{
    [Tooltip("방향을 알려줄 대상. 비우면 부모에서 찾는다.")]
    [SerializeField] Player source;

    [Header("궤도")]
    [Tooltip("캐릭터 중심에서 떨어진 거리(유닛).")]
    [SerializeField] float radius = 0.8f;

    [Tooltip("방향 전환에 걸리는 시간(초). 0이면 즉시 꺾인다.\n\n" +
             "★ 각도와 무관하게 늘 같은 시간이 걸린다 —\n" +
             "   45도든 180도든 0.12초. 반대 방향으로 꺾을 때만 굼떠 보이지 않는다.")]
    [SerializeField] float turnTime = 0.12f;

    [Tooltip("궤도를 도는 동안 화살표도 바깥을 향해 돌지.\n" +
             "끄면 모양이 고정된다 — 글자로 읽혀야 하는 표시에 쓴다.")]
    [SerializeField] bool faceOutward = true;

    [Header("연출")]
    [Tooltip("가만히 있을 때 궤도를 살짝 좁힌다. 0이면 항상 같은 거리.\n" +
             "움직일 때 앞으로 뻗는 느낌을 준다.")]
    [SerializeField] float idleShrink = 0.15f;

    [Tooltip("궤도 거리가 바뀌는 속도(유닛/초).")]
    [SerializeField] float shrinkSpeed = 3f;

    /// <summary>지금 화살표가 있는 각도(도).</summary>
    float angle;

    /// <summary>이번 전환의 시작 각도와 목표 각도. 방향이 바뀔 때마다 다시 잡는다.</summary>
    float from;
    float to;

    /// <summary>이번 전환이 시작된 뒤 흐른 시간.</summary>
    float elapsed;

    /// <summary>지금 궤도 거리. 멈추면 줄고 움직이면 늘어난다.</summary>
    float distance;

    void Awake()
    {
        if (source == null) source = GetComponentInParent<Player>();

        distance = radius;

        if (source == null) return;

        angle = from = to = Angle(source.Facing);
        elapsed = turnTime;
    }

    // 이동이 끝난 뒤 자리를 잡아야 한 프레임 늦게 따라오지 않는다
    void LateUpdate()
    {
        if (source == null) return;

        angle = Turn(Angle(source.Facing), Time.deltaTime);

        bool moving = source.inputVec.sqrMagnitude > 0.0001f;
        float want = moving ? radius : radius - idleShrink;

        distance = Mathf.MoveTowards(distance, want, shrinkSpeed * Time.deltaTime);

        float rad = angle * Mathf.Deg2Rad;

        transform.localPosition = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * distance;

        if (faceOutward) transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// 목표 각도로 <see cref="turnTime"/> 에 걸쳐 돈다.
    ///
    /// <b>각속도가 아니라 시간을 고정한다.</b> 각속도를 고정하면 반대 방향으로 꺾을 때
    /// 네 배 오래 걸려서, 정작 제일 급한 순간에 표시가 제일 늦게 따라온다.
    ///
    /// 목표가 도중에 또 바뀌면 <b>지금 자리에서</b> 새로 시작한다 — 튀지 않는다.
    /// </summary>
    float Turn(float target, float deltaTime)
    {
        if (turnTime <= 0f) { from = to = target; return target; }

        // 목표가 바뀌었으면 지금 각도에서 다시 출발한다
        if (Mathf.Abs(Mathf.DeltaAngle(to, target)) > 0.01f)
        {
            from = angle;
            to = target;
            elapsed = 0f;
        }

        elapsed += deltaTime;

        float t = Mathf.Clamp01(elapsed / turnTime);

        // 짧은 쪽으로 돈다. 끝에서 살짝 감속해야 딱 멈추는 느낌이 안 난다
        return Mathf.LerpAngle(from, to, Mathf.SmoothStep(0f, 1f, t));
    }

    /// <summary>방향 벡터를 각도로. 0도가 오른쪽이며 반시계로 커진다.</summary>
    static float Angle(Vector2 direction)
        => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 center = transform.parent != null ? transform.parent.position : transform.position;

        Gizmos.color = new Color(0.3f, 0.8f, 0.9f, 0.5f);
        Gizmos.DrawWireSphere(center, radius);
    }
#endif
}
