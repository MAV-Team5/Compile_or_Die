using UnityEngine;

/// <summary>
/// 근접 휘두르기 본체. 칼날이 부채꼴을 따라 지나가고 그 자리에 잔상이 남는다.
///
/// 칼날은 스프라이트라 자유롭게 그리면 되고, <b>각도 정보는 스프라이트에 없다</b> —
/// 좌우 폭은 칼날이 이동하는 범위가 말해준다. 그래서 halfAngle 을 바꾸면 그림도 자동으로 따라온다.
/// 부채꼴을 스프라이트로 그려두면 판정 각도가 바뀔 때 조용히 어긋나는데, 그 문제가 아예 사라진다.
///
/// 판정은 전달 모듈이 한 프레임에 끝낸다. 이 연출은 그 위를 훑을 뿐이다 —
/// 히트박스는 즉발, 그림은 시간축. 액션 게임의 표준 구성이다.
///
/// 프리팹 규격 — 칼날 스프라이트는 <b>반지름 1유닛</b>, 피벗은 <b>Left</b>.
/// </summary>
public class SwingArc : MonoBehaviour, IDirectionalVisual, ISizedVisual, IArcVisual
{
    [Header("구성")]
    [Tooltip("칼날 스프라이트. 피벗을 Left 로 두면 이 오브젝트를 그대로 돌려도 원점을 축으로 돈다.\n" +
             "＊ 부채꼴이 아니라 칼날 한 장이어야 한다 — 그림에 각도가 있으면 판정과 어긋난다.")]
    public Transform blade;

    [Tooltip("지나간 자리에 남는 잔상. 비우면 칼날만 지나간다.\n" +
             "이쪽이 판정 영역을 정확히 그리는 담당이다.")]
    public SweepFan fan;

    [Tooltip("켜면 잔상 그림이 휘두르는 방향을 따라 돈다 — 베는 자국이 항상 같은 모양으로 나온다.\n" +
             "끄면 그림이 월드 기준으로 고정되고 부채꼴이 그 위를 훑고 지나간다.")]
    public bool artFollowsSwing = true;

    [Header("휘두르기")]
    [Tooltip("한 번 휘두르는 데 걸리는 시간(초).\n" +
             "0.15~0.25 가 자연스럽다. 길면 '칼이 안 닿았는데 죽었다'가 된다.")]
    public float duration = 0.18f;

    [Tooltip("켜면 시계 방향으로 휘두른다. 전달을 두 개 넣어 양손으로 만들 때 하나만 뒤집으면 된다.")]
    public bool clockwise = true;

    [Tooltip("휘두르기 가속. 1이면 등속, 2면 처음이 빠르고 끝이 느려진다 — 칼을 뿌리는 느낌.")]
    [Range(0.5f, 4f)] public float easing = 2f;

    [Header("사라짐")]
    [Tooltip("휘두르기가 끝난 뒤 사라지는 시간(초). 0이면 즉시.")]
    public float fadeOut = 0.1f;

    [Header("미리보기")]
    [Tooltip("SetArc 를 못 받았을 때 쓸 좌우 각도. 씬에서 단독 테스트할 때만 쓰인다.")]
    [Range(0f, 180f)] public float fallbackHalfAngle = 45f;

    [Tooltip("Resize 를 못 받았을 때 쓸 반지름. 씬에서 단독 테스트할 때만 쓰인다.")]
    public float fallbackRadius = 2f;

    SpriteRenderer[] sprites;
    float[] spriteAlpha;

    float elapsed;
    float radius;
    float halfAngle;

    /// <summary>휘두르기 중심 방향(도). Aim 이 채운다.</summary>
    float centerAngle = 90f;

    void Awake()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>();
        spriteAlpha = new float[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
            spriteAlpha[i] = sprites[i].color.a;

        radius = fallbackRadius;
        halfAngle = fallbackHalfAngle;

        if (fan == null) fan = GetComponentInChildren<SweepFan>();
    }

    void Start() => Render(0f);

    // ── 전달 모듈이 알려주는 사실들 ─────────────────────────

    /// <summary>휘두를 중심 방향.</summary>
    public void Aim(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        centerAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    /// <summary>사거리. 칼날 길이와 잔상 반경이 여기서 나온다.</summary>
    public void Resize(float newRadius)
    {
        if (newRadius > 0f) radius = newRadius;
    }

    /// <summary>판정 좌우 각도. 칼날이 훑는 범위가 곧 이 값이 된다.</summary>
    public void SetArc(float halfAngleDegrees)
    {
        if (halfAngleDegrees > 0f) halfAngle = halfAngleDegrees;
    }

    // ── 그리기 ────────────────────────────────────────────

    void Update()
    {
        elapsed += Time.deltaTime;

        float swing = Mathf.Max(0.01f, duration);

        Render(Mathf.Clamp01(elapsed / swing));

        if (elapsed >= swing + fadeOut) Destroy(gameObject);
    }

    void Render(float progress)
    {
        float fade = ResolveFade();
        ApplySpriteAlpha(fade);

        // 처음이 빠르고 끝이 느려지면 칼을 뿌리는 느낌이 난다
        float eased = 1f - Mathf.Pow(1f - progress, easing);

        float direction = clockwise ? -1f : 1f;
        float span = halfAngle * 2f * direction;

        // 한쪽 끝에서 시작해 반대쪽 끝까지 훑는다
        float from = centerAngle - span * 0.5f;
        float swept = span * eased;

        if (blade != null)
        {
            blade.localScale = Vector3.one * radius;
            blade.localRotation = Quaternion.Euler(0f, 0f, from + swept);
        }

        if (fan != null)
        {
            fan.SetAlpha(fade);
            fan.Draw(radius, from, swept, artFollowsSwing ? centerAngle : 0f);
        }
    }

    /// <summary>휘두르는 동안은 1. 끝난 뒤부터 fadeOut 에 걸쳐 0으로.</summary>
    float ResolveFade()
    {
        if (fadeOut <= 0f) return 1f;

        float over = elapsed - duration;
        if (over <= 0f) return 1f;

        return 1f - Mathf.Clamp01(over / fadeOut);
    }

    void ApplySpriteAlpha(float fade)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null) continue;

            Color c = sprites[i].color;
            c.a = spriteAlpha[i] * fade;
            sprites[i].color = c;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        float r = Application.isPlaying ? radius : fallbackRadius;
        float half = Application.isPlaying ? halfAngle : fallbackHalfAngle;

        Gizmos.color = new Color(1f, 0.8f, 0.35f, 0.7f);

        for (int i = -1; i <= 1; i += 2)
        {
            float rad = (centerAngle + half * i) * Mathf.Deg2Rad;
            Gizmos.DrawLine(transform.position,
                            transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * r);
        }
    }
#endif
}
