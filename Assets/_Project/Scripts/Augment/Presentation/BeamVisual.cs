using UnityEngine;

/// <summary>
/// 레이저 연출. 길이·굵기를 받아 커브대로 재생하고 스스로 사라진다.
/// 스프라이트는 위쪽(+Y)이 진행 방향이어야 한다. 비율과 PPU는 자동 보정된다.
/// </summary>
public class BeamVisual : MonoBehaviour
{
    [SerializeField] SpriteRenderer body;

    [Tooltip("연출이 유지되는 시간(초).")]
    [SerializeField] float duration = 0.12f;

    [Tooltip("시간에 따른 굵기 배수. 가로축 0~1이 재생 구간이다.")]
    [SerializeField] AnimationCurve widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Tooltip("시간에 따른 투명도 배수.")]
    [SerializeField] AnimationCurve alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    float elapsed;
    float baseWidth;
    float beamLength;
    Vector2 spriteUnit;
    Color baseColor;
    bool playing;
    bool captured;

    // 풀에서 재사용되므로 원래 색은 처음 한 번만 기억한다.
    // Play 에서 잡으면 이전 재생이 알파를 0으로 만들어둔 상태를 원본으로 착각한다
    void Awake() => Capture();

    void Capture()
    {
        if (captured) return;
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        if (body == null) return;

        baseColor = body.color;
        captured = true;
    }

    public void Play(Vector2 origin, Vector2 direction, float length, float width)
    {
        Capture();

        if (body == null || body.sprite == null)
        {
            Debug.LogWarning("BeamVisual: SpriteRenderer 나 Sprite 가 없습니다", this);
            Despawn();
            return;
        }

        // 스프라이트 실제 유닛 크기로 나눠야 PPU·비율과 무관하게 맞는다
        spriteUnit = body.sprite.bounds.size;

        if (spriteUnit.x <= 0f || spriteUnit.y <= 0f)
        {
            Despawn();
            return;
        }

        beamLength = length;
        baseWidth  = width;
        elapsed    = 0f;
        playing    = true;

        // 중간 지점에 놓으면 스프라이트 피벗이 어디든 상관없어진다
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        transform.SetPositionAndRotation(
            origin + direction * (length * 0.5f),
            Quaternion.Euler(0f, 0f, angle));

        ApplyFrame(0f);
    }

    void Update()
    {
        if (!playing) return;

        elapsed += Time.deltaTime;

        if (elapsed >= duration)
        {
            Despawn();
            return;
        }

        ApplyFrame(elapsed / duration);
    }

    /// <summary>풀로 돌려보낸다. 파괴하면 풀 목록에 죽은 참조가 남는다.</summary>
    void Despawn()
    {
        playing = false;

        // 다음에 꺼내 쓸 때 투명한 채로 나오지 않게 되돌린다
        if (body != null) body.color = baseColor;

        PooledSpawner.Despawn(gameObject);
    }

    void ApplyFrame(float t)
    {
        float width = baseWidth * widthCurve.Evaluate(t);

        transform.localScale = new Vector3(
            width / spriteUnit.x,
            beamLength / spriteUnit.y,
            1f);

        Color c = baseColor;
        c.a = baseColor.a * alphaCurve.Evaluate(t);
        body.color = c;
    }
}
