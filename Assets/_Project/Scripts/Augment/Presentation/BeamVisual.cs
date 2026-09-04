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

    [Tooltip("시간에 따른 길이 배수. 시작점은 고정이고 끝만 자란다.\n\n" +
             "기본값은 1로 평평해서 지금까지처럼 전체 길이로 즉시 나온다.\n" +
             "0에서 1로 오르는 커브를 넣으면 <b>쭉 그어지듯</b> 뻗어나간다 —\n" +
             "선형 탐색처럼 \"훑고 지나간다\" 를 보여줘야 하는 연출에 쓴다.\n\n" +
             "＊ 판정은 발동 순간에 이미 끝나 있다. 이건 눈에 보이는 것만 바꾼다.")]
    [SerializeField] AnimationCurve lengthCurve = AnimationCurve.Constant(0f, 1f, 1f);

    float elapsed;
    float baseWidth;
    float beamLength;
    Vector2 spriteUnit;
    Color baseColor;
    bool playing;
    bool captured;

    /// <summary>빔이 시작되는 자리. 길이가 자라도 여기는 안 움직인다.</summary>
    Vector3 origin;

    Vector2 heading;

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

        // 자리는 매 프레임 ApplyFrame 이 다시 잡는다. 길이가 자라도 시작점이 안 밀리게
        this.origin  = new Vector3(origin.x, origin.y, transform.position.z);
        this.heading = direction;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);

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

        // 길이가 0이 되면 스케일도 0이라 그 프레임에 사라져 보인다. 아주 얇게라도 남긴다
        float length = Mathf.Max(0.0001f, beamLength * lengthCurve.Evaluate(t));

        // 스프라이트 중심이 기준이라, 시작점을 붙박이로 두려면 절반만큼 앞으로 민다
        transform.position = origin + (Vector3)(heading * (length * 0.5f));

        transform.localScale = new Vector3(
            width / spriteUnit.x,
            length / spriteUnit.y,
            1f);

        Color c = baseColor;
        c.a = baseColor.a * alphaCurve.Evaluate(t);
        body.color = c;
    }
}
