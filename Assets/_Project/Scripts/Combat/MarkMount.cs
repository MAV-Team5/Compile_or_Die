using UnityEngine;

/// <summary>
/// 탐색 표식 하나가 붙을 자리. 적 프리팹에 빈 오브젝트로 만들어 <b>눈으로 끌어다 놓는다</b>.
///
/// 표식마다 자기 자리를 갖는다 — BFS 는 몸 가운데, 큐는 머리 위 하는 식이다.
/// 어느 자리를 쓸지는 표식 프리팹의 <see cref="MarkSlot"/> 이 정하고,
/// 그 자리가 <b>어디에 얼마나 크게</b> 있는지는 적이 정한다. 역할이 이렇게 갈려야
/// 탐색 증강이 늘어도 적 프리팹을 안 고치고, 적이 늘어도 표식을 안 고친다.
///
/// <b>자리가 겹치지 않으므로 표식 크기가 표식 수에 영향받지 않는다.</b>
/// 예전에는 겹 수로 크기를 정해서, 옆 표식이 사라질 때마다 남은 표식이 커졌다 작아졌다 했다 —
/// 같은 증강인데 적마다 크기가 달라 보여서 "저 적이 더 중요한가" 로 잘못 읽혔다.
///
/// 없어도 된다. 번호가 맞는 자리를 못 찾으면 적 본체에 원본 크기로 붙는다.
/// </summary>
public class MarkMount : MonoBehaviour
{
    [Tooltip("자리 번호. 표식 프리팹의 MarkSlot 과 같은 번호끼리 만난다.\n" +
             "MarkSlot 이 안 붙은 표식은 0번으로 온다.")]
    public int slot = 0;

    [Tooltip("이 자리의 칸 한 변. 표식 프리팹이 몇 픽셀짜리든 이 크기에 맞춰 줄어든다.\n" +
             "머리 위 배지는 작게, 몸을 감싸는 테두리는 크게 잡는다.")]
    public float size = 1f;

    /// <summary>
    /// 실제로 화면에 그려질 한 변(월드 유닛).
    ///
    /// <b>부모 스케일을 곱한다.</b> 안 곱하면 EnemyData 로 적을 두 배로 키워도
    /// 표식만 원래 크기로 남아 붕 떠 보인다.
    /// </summary>
    public float WorldSize => size * Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        float one = WorldSize;

        Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(one, one, 0f));

        UnityEditor.Handles.Label(transform.position + Vector3.up * (one * 0.5f + 0.1f),
                                  $"slot {slot}");
    }
#endif
}
