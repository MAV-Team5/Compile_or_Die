using UnityEngine;

/// <summary>
/// 투사체 뒤로 실이 늘어나는 연출. 거미줄처럼 쏘아 붙이는 느낌에 쓴다.
/// 투사체 프리팹에 <see cref="LineRenderer"/> 와 함께 붙인다.
///
/// 출발점은 <see cref="AugmentProjectile"/> 가 발사할 때 알려준다 —
/// 풀에서 꺼낼 때 OnEnable 이 위치보다 먼저 불려서 스스로 기억하면 지난 발사 자리를 잡는다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ProjectileThread : MonoBehaviour
{
    [Tooltip("실이 늘어나는 최대 길이(유닛). 0이면 제한 없이 계속 늘어난다.\n" +
             "값을 주면 그 길이를 넘는 순간 뒤쪽 끝이 따라온다 — 꼬리처럼 보인다.")]
    public float maxLength = 0f;

    [Tooltip("실이 처지는 정도(유닛). 0이면 곧은 직선.\n" +
             "길이에 비례해 늘어져서 거미줄처럼 보인다.")]
    public float sag = 0f;

    [Tooltip("처짐을 그리는 데 쓸 조각 수. 처짐이 0이면 무시된다.")]
    [Range(2, 24)] public int segments = 8;

    LineRenderer line;
    Vector3[] points;

    Vector3 origin;

    /// <summary>원점이 움직이는 대상이면 따라간다. 없으면 처음 찍은 좌표에 고정.</summary>
    Transform anchor;

    bool flying;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
    }

    void OnEnable()
    {
        flying = false;
        anchor = null;
    }

    /// <summary>
    /// 발사 순간 출발점을 받는다. AugmentProjectile 이 부른다.
    ///
    /// from 은 좌표, source 는 그 자리의 주인이다.
    /// 주인이 살아 있으면 실 끝이 따라간다 — 안 그러면 쏜 적이 걸어갈 때 실만 허공에 남는다.
    /// </summary>
    public void Begin(Vector3 from, Transform source = null)
    {
        origin = from;
        anchor = source;
        flying = true;

        Draw();
    }

    void LateUpdate()
    {
        if (flying) Draw();
    }

    void Draw()
    {
        Vector3 tip = transform.position;

        // 주인이 살아 있을 때만 따라간다.
        // 적은 죽으면 풀로 반납됐다가 딴 자리에서 되살아난다 — Transform 은 살아 있으므로
        // null 검사만으로는 못 걸러내고, 실 꼬리가 스폰 지점으로 순간이동한다.
        if (anchor != null)
        {
            if (anchor.gameObject.activeInHierarchy) origin = anchor.position;
            else anchor = null;   // 한 번 놓치면 다시 붙지 않는다. 마지막 자리에 남는다
        }

        Vector3 tail = origin;

        // 길이를 제한하면 뒤쪽 끝이 따라와 꼬리가 된다
        if (maxLength > 0f)
        {
            Vector3 back = tail - tip;

            if (back.magnitude > maxLength) tail = tip + back.normalized * maxLength;
        }

        if (sag <= 0f || segments <= 2)
        {
            line.positionCount = 2;
            line.SetPosition(0, tail);
            line.SetPosition(1, tip);
            return;
        }

        EnsurePoints();

        Vector3 delta = tip - tail;

        // 길수록 많이 처진다. 양 끝은 붙어 있어야 하므로 가운데만 내려간다
        float drop = sag * Mathf.Min(1f, delta.magnitude / Mathf.Max(0.01f, maxLength > 0f ? maxLength : 5f));

        for (int i = 0; i < points.Length; i++)
        {
            float t = (float)i / (points.Length - 1);

            points[i] = tail + delta * t + Vector3.down * (Mathf.Sin(t * Mathf.PI) * drop);
        }

        line.positionCount = points.Length;
        line.SetPositions(points);
    }

    void EnsurePoints()
    {
        if (points == null || points.Length != segments) points = new Vector3[segments];
    }
}
