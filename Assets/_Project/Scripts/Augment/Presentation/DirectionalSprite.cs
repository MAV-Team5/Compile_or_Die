using UnityEngine;

/// <summary>
/// 방향에 맞는 애니메이션 클립을 골라 재생한다. 회전을 안 쓰므로 도트가 뭉개지지 않는다.
/// 클립 이름을 "Swing_E" "Swing_NE" … 처럼 맞춰두기만 하면 전이(Transition)를 그릴 필요가 없다.
/// </summary>
[RequireComponent(typeof(Animator))]
public class DirectionalSprite : MonoBehaviour, IDirectionalVisual
{
    /// <summary>0시 방향이 아니라 오른쪽(E)부터 반시계로 센다. Atan2 결과와 순서가 같다.</summary>
    static readonly string[] Eight = { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };
    static readonly string[] Four = { "E", "N", "W", "S" };

    [SerializeField] Animator animator;

    [Tooltip("클립 이름 앞부분. 'Swing' 이면 Swing_E · Swing_NE … 를 찾는다.")]
    [SerializeField] string clipPrefix = "Swing";

    [Tooltip("켜면 4방향(E·N·W·S)만 쓴다. 그릴 클립이 줄어든다.")]
    [SerializeField] bool fourWayOnly = false;

    void Reset() => animator = GetComponent<Animator>();

    public void Aim(Vector2 direction)
    {
        if (animator == null) return;

        string[] names = fourWayOnly ? Four : Eight;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float step = 360f / names.Length;

        int index = Mathf.RoundToInt(Mathf.Repeat(angle, 360f) / step) % names.Length;

        animator.Play($"{clipPrefix}_{names[index]}", 0, 0f);
    }
}
