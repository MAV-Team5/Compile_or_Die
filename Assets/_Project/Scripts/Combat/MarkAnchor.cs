using UnityEngine;

/// <summary>
/// 탐색 표식이 감쌀 칸. 적 프리팹에 빈 오브젝트로 만들어 <b>몸 한가운데</b>에 둔다.
/// 표식 프리팹이 몇 픽셀짜리든 이 칸 크기에 맞춰 줄어든다.
///
/// 표식은 적을 둘러싸는 테두리다. 그래서 여러 개가 붙으면 자리를 나눠 쓸 수 없고,
/// <b>한 겹씩 바깥으로</b> 커진다 — 겹 수가 곧 걸린 탐색 수가 된다.
///
/// 없으면 적 본체 자리에 프리팹 크기 그대로 붙는다.
/// </summary>
public class MarkAnchor : MonoBehaviour
{
    [Tooltip("테두리가 감쌀 한 변. 적 몸이 이 안에 들어오게 맞춘다.\n" +
             "적을 키우면 테두리도 같이 커진다 — 프리팹에 그려진 크기 기준이다.")]
    public float size = 1f;

    [Tooltip("표식이 여러 개일 때 한 겹 바깥으로 벌어지는 폭(월드 유닛).\n" +
             "0이면 전부 같은 크기라 위엣것만 보인다.")]
    public float ringGap = 0.12f;

    [Tooltip("겹이 아무리 많아도 이 배율을 넘지 않는다. 테두리가 화면을 덮는 것을 막는다.")]
    [Range(1f, 3f)] public float maxRing = 2f;

    /// <summary>
    /// 실제로 화면에 그려질 한 변(월드 유닛).
    ///
    /// <b>부모 스케일을 곱한다.</b> 안 곱하면 EnemyData 로 적을 두 배로 키워도
    /// 테두리만 원래 크기로 남아 몸을 못 감싼다 — 표식이 붕 떠 보이는 원인이었다.
    /// </summary>
    public float WorldSize => size * OwnerScale;

    /// <summary>겹 사이 간격도 몸집을 따라간다. 안 그러면 큰 적일수록 겹이 붙어 보인다.</summary>
    public float WorldRingGap => ringGap * OwnerScale;

    /// <summary>이 앵커가 실제로 얼마나 확대돼 있나. 적 프리팹의 스케일이 여기 담긴다.</summary>
    float OwnerScale
    {
        get
        {
            Vector3 s = transform.lossyScale;
            return Mathf.Max(0.0001f, Mathf.Abs(s.x));
        }
    }

    /// <summary>
    /// index 번째 겹의 크기 배율. 0번이 가장 안쪽(1배)이고 바깥으로 갈수록 커진다.
    /// 표식은 이미 칸 크기에 맞춰져 있으므로 여기에 곱하기만 하면 된다.
    /// </summary>
    public float RingScale(int index)
    {
        // 배율이라 OwnerScale 이 분자·분모에 다 붙어 약분된다. 여기서는 안 곱한다
        if (index <= 0 || size <= 0f || ringGap <= 0f) return 1f;

        // 지름 기준이라 양쪽으로 벌어진다
        float scale = (size + ringGap * 2f * index) / size;

        return Mathf.Min(scale, maxRing);
    }

#if UNITY_EDITOR
    // 칸이 얼마나 큰지 씬에서 눈으로 보고 맞출 수 있게 한다
    void OnDrawGizmos()
    {
        // 실제로 그려질 크기를 보여줘야 씬 뷰에서 눈으로 맞출 수 있다
        float one = WorldSize;

        Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(one, one, 0f));

        // 두 번째 겹이 어디까지 커지는지 미리 보여준다
        Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.3f);
        float second = one * RingScale(1);
        Gizmos.DrawWireCube(transform.position, new Vector3(second, second, 0f));
    }
#endif
}
