using UnityEngine;

/// <summary>
/// 간선 선을 그리고, 뭔가 지나갈 때 꿀렁이게 한다. 간선 프리팹에 붙인다.
///
/// 평소에는 곧은 직선이고, <see cref="Pulse"/> 가 불리면 잠깐 물결치며 굵어진다.
/// 전이가 실제로 일어났는지, 탐색이 어디를 타고 갔는지가 눈에 보인다.
///
/// 붙어 있으면 LinkHolder 가 선 그리기를 통째로 맡긴다. 없으면 두 점짜리 직선으로 그린다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class LinkPulse : MonoBehaviour
{
    [Header("물결")]
    [Tooltip("꿀렁일 때 선을 몇 조각으로 쪼갤지. 2면 물결 없이 직선.")]
    [Range(2, 32)] public int segments = 12;

    [Tooltip("물결의 높이(유닛). 선 길이와 무관한 절대값이다.")]
    public float amplitude = 0.18f;

    [Tooltip("물결이 흐르는 속도. 클수록 빨리 지나간다.")]
    public float speed = 8f;

    [Tooltip("선을 따라 몇 개의 마루가 지나갈지.")]
    public float waves = 1.5f;

    [Header("굵기")]
    [Tooltip("꿀렁일 때 굵어지는 배수. 1이면 안 굵어진다.")]
    public float widthBoost = 2.2f;

    [Header("지속")]
    [Tooltip("한 번 지나갈 때 물결이 유지되는 시간(초).")]
    public float duration = 0.3f;

    LineRenderer line;
    Vector3[] points;

    float baseWidth;
    float remain;
    float strength;

    /// <summary>지금 프레임의 양 끝. LinkHolder 가 매 프레임 채운다.</summary>
    Vector3 from, to;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        baseWidth = line.widthMultiplier;
    }

    // 풀에서 다시 꺼내 쓰므로 되돌린다
    void OnEnable()
    {
        remain = 0f;
        strength = 0f;

        if (line != null) line.widthMultiplier = baseWidth;
    }

    /// <summary>양 끝을 알려준다. 그리는 것은 이쪽이 맡는다.</summary>
    public void SetEnds(Vector3 a, Vector3 b)
    {
        from = a;
        to = b;
    }

    /// <summary>
    /// 뭔가 지나갔다. 세기를 주면 그만큼 크게 꿀렁인다 —
    /// 피해 전이는 세게, 탐색이 훑고 지나가는 것은 약하게 주면 구분된다.
    /// </summary>
    public void Pulse(float amount = 1f)
    {
        // 연달아 오면 더 센 쪽이 이긴다. 약한 신호가 강한 물결을 덮지 않게
        strength = Mathf.Max(strength, Mathf.Clamp01(amount));
        remain = duration;
    }

    void LateUpdate()
    {
        if (line == null) return;

        if (remain > 0f) remain -= Time.deltaTime;
        else strength = 0f;

        Draw();
    }

    void Draw()
    {
        // 잔잔할 때는 두 점이면 충분하다. 정점을 아낀다
        bool wavy = strength > 0f && remain > 0f && segments > 2 && amplitude > 0f;

        if (!wavy)
        {
            line.widthMultiplier = baseWidth;
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            return;
        }

        float fade = Mathf.Clamp01(remain / Mathf.Max(0.01f, duration)) * strength;

        line.widthMultiplier = baseWidth * Mathf.Lerp(1f, widthBoost, fade);

        EnsurePoints();

        Vector3 delta = to - from;

        // 선에 수직인 쪽으로 흔든다. 길이 방향으로는 안 밀린다
        Vector3 side = new Vector3(-delta.y, delta.x, 0f).normalized;

        for (int i = 0; i < points.Length; i++)
        {
            float t = (float)i / (points.Length - 1);

            // 양 끝은 노드에 붙어 있어야 하므로 가운데만 흔든다
            float taper = Mathf.Sin(t * Mathf.PI);

            float phase = (t * waves - Time.time * speed / Mathf.Max(0.01f, waves)) * Mathf.PI * 2f;

            points[i] = from + delta * t + side * (Mathf.Sin(phase) * amplitude * taper * fade);
        }

        line.positionCount = points.Length;
        line.SetPositions(points);
    }

    void EnsurePoints()
    {
        if (points != null && points.Length == segments) return;

        points = new Vector3[segments];
    }
}
