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
    [Tooltip("테두리가 감쌀 한 변(월드 유닛). 적 몸이 이 안에 들어오게 맞춘다.\n" +
             "적 스케일과 무관한 절대 크기다. 씬 뷰의 네모를 보며 조절할 것.")]
    public float size = 1f;

    [Tooltip("표식이 여러 개일 때 한 겹 바깥으로 벌어지는 폭(월드 유닛).\n" +
             "0이면 전부 같은 크기라 위엣것만 보인다.")]
    public float ringGap = 0.12f;

    [Tooltip("겹이 아무리 많아도 이 배율을 넘지 않는다. 테두리가 화면을 덮는 것을 막는다.")]
    [Range(1f, 3f)] public float maxRing = 2f;

    /// <summary>
    /// index 번째 겹의 크기 배율. 0번이 가장 안쪽(1배)이고 바깥으로 갈수록 커진다.
    /// 표식 프리팹은 이미 칸 크기에 맞춰져 있으므로 여기에 곱하기만 하면 된다.
    /// </summary>
    public float RingScale(int index)
    {
        if (index <= 0 || size <= 0f || ringGap <= 0f) return 1f;

        // 지름 기준이라 양쪽으로 벌어진다
        float scale = (size + ringGap * 2f * index) / size;

        return Mathf.Min(scale, maxRing);
    }

#if UNITY_EDITOR
    // 칸이 얼마나 큰지 씬에서 눈으로 보고 맞출 수 있게 한다
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(size, size, 0f));

        // 두 번째 겹이 어디까지 커지는지 미리 보여준다
        Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.3f);
        float second = size * RingScale(1);
        Gizmos.DrawWireCube(transform.position, new Vector3(second, second, 0f));
    }
#endif
}
