using UnityEngine;

/// <summary>
/// 탐색 표식이 들어갈 칸. 적 프리팹에 빈 오브젝트로 만들어 머리 위에 두면 된다.
/// 표식 프리팹이 몇 픽셀짜리든 이 칸 크기에 맞춰 줄어든다.
///
/// 없으면 적 본체 위에 프리팹 크기 그대로 붙는다.
/// </summary>
public class MarkAnchor : MonoBehaviour
{
    [Tooltip("표식이 들어갈 칸의 한 변(월드 유닛). 표식은 여기 안에 꽉 차게 맞춰진다.\n" +
             "적 스케일과 무관한 절대 크기다.")]
    public float size = 0.5f;

    [Tooltip("표식이 여러 개일 때 위로 쌓이는 간격(월드 유닛). 0이면 칸 크기를 그대로 쓴다.")]
    public float spacing = 0f;

    public float Spacing => spacing > 0f ? spacing : size;

#if UNITY_EDITOR
    // 칸이 얼마나 큰지 씬에서 눈으로 보고 맞출 수 있게 한다
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(size, size, 0f));
    }
#endif
}
