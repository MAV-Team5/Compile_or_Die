using UnityEngine;

/// <summary>
/// 레이더 스윕 연출. 범위만큼 원을 그리고 스캔 라인이 한 바퀴 돌면서 지나간 자리를 밝힌다.
/// 탐색(BFS·DFS) 증강의 시전 연출 전용.
///
/// 판정과는 무관하다 — 표식은 이 연출이 돌기 전에 이미 다 붙어 있다.
/// 레이더는 "방금 이 범위를 훑었다"를 보여줄 뿐이다.
///
/// 프리팹 규격 — 링과 스캔 라인 스프라이트는 <b>반지름 1유닛</b>으로 그린다.
/// 그래야 Resize 가 스케일만 바꿔서 어떤 사거리에도 맞는다.
/// 잔광 부채꼴은 메시라 규격이 필요 없다.
/// </summary>
public class RadarSweep : MonoBehaviour, ISizedVisual
{
    [Header("구성")]
    [Tooltip("테두리 원. 반지름 1유닛 규격 — 지름이 정확히 2유닛인 스프라이트.\n" +
             "비워도 된다. 잔광만으로도 범위가 보인다.")]
    public Transform ring;

    [Tooltip("회전하는 축. 자식으로 스캔 라인 스프라이트를 둔다.\n" +
             "스캔 라인 스프라이트의 피벗은 반드시 Left — 중심이면 프로펠러가 된다.")]
    public Transform pivot;

    [Tooltip("지나간 자리를 채우는 잔광. 비우면 스캔 라인만 돈다.")]
    public SweepFan fan;

    [Header("회전")]
    [Tooltip("한 바퀴 도는 데 걸리는 시간(초). 쿨타임보다 짧게 둘 것 — 겹치면 레이더가 두 개 뜬다.")]
    public float duration = 0.5f;

    [Tooltip("몇 바퀴 돌지. 1이면 한 바퀴.")]
    public float turns = 1f;

    [Tooltip("시작 각도(도). 0이 오른쪽, 90이 위.")]
    public float startAngle = 90f;

    [Tooltip("켜면 시계 방향으로 돈다.")]
    public bool clockwise = true;

    [Header("잔광")]
    [Tooltip("스캔 라인 뒤로 남는 꼬리 길이(도). 0이면 지나간 자리가 전부 남아 원이 다 채워진다.\n" +
             "90 정도면 진짜 레이더처럼 꼬리만 따라다닌다.")]
    [Range(0f, 360f)] public float trailAngle = 0f;

    [Header("투명도")]
    [Tooltip("전체 투명도. 링·스캔 라인·잔광에 한꺼번에 곱해진다.")]
    [Range(0f, 1f)] public float opacity = 1f;

    [Tooltip("한 바퀴를 다 돈 뒤 사라지는 데 걸리는 시간(초). 0이면 즉시 사라진다.")]
    public float fadeOut = 0.15f;

    [Tooltip("Resize 를 못 받았을 때 쓸 반지름. 씬에서 단독으로 테스트할 때만 쓰인다.")]
    public float fallbackRadius = 3f;

    SpriteRenderer[] sprites;
    float[] spriteAlpha;

    float elapsed;
    float radius;

    void Awake()
    {
        // 페이드는 자식 전부에 걸어야 링과 라인이 같이 사라진다.
        // 프리팹에 그려둔 알파를 기준으로 삼으므로 여기서 미리 기억해둔다
        sprites = GetComponentsInChildren<SpriteRenderer>();
        spriteAlpha = new float[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
            spriteAlpha[i] = sprites[i].color.a;

        radius = fallbackRadius;

        AutoWire();
    }

    /// <summary>
    /// 슬롯을 깜빡해도 자식에서 찾아 쓴다.
    /// 비어 있으면 아무 일도 안 일어나면서 경고도 없어 원인을 못 찾는다.
    /// </summary>
    void AutoWire()
    {
        if (fan == null) fan = GetComponentInChildren<SweepFan>();

        if (fan == null && ring == null && pivot == null)
        {
            Debug.LogWarning($"[{name}] RadarSweep 에 잔광·링·스캔 라인이 하나도 연결되지 않았습니다. " +
                             "아무것도 보이지 않습니다", this);
        }
    }

#if UNITY_EDITOR
    // 컴포넌트를 붙이는 순간 자식을 훑어 슬롯을 채워둔다
    void Reset()
    {
        fan = GetComponentInChildren<SweepFan>();

        foreach (Transform child in transform)
        {
            if (ring == null && child.name.Contains("Ring")) ring = child;
            if (pivot == null && child.name.Contains("Pivot")) pivot = child;
        }
    }
#endif

    void Start()
    {
        // Resize 는 생성 직후 외부에서 불린다. 안 불렸으면 fallback 으로라도 보이게 한다
        ApplyRadius();
        Render(0f);
    }

    /// <summary>사거리를 받아 크기를 맞춘다. VfxSpawner 가 생성 직후 부른다.</summary>
    public void Resize(float newRadius)
    {
        if (newRadius <= 0f) return;

        radius = newRadius;
        ApplyRadius();
    }

    /// <summary>반지름 1유닛 규격이라 스케일이 곧 반지름이다. 잔광은 그릴 때 직접 받는다.</summary>
    void ApplyRadius()
    {
        if (ring != null) ring.localScale = Vector3.one * radius;
        if (pivot != null) pivot.localScale = Vector3.one * radius;
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        float spin = Mathf.Max(0.01f, duration);
        float progress = Mathf.Clamp01(elapsed / spin);

        Render(progress);

        if (elapsed >= spin + fadeOut) Destroy(gameObject);
    }

    void Render(float progress)
    {
        float fade = ResolveFade();

        ApplySpriteAlpha(fade);

        float swept = 360f * turns * progress * (clockwise ? -1f : 1f);

        if (pivot != null)
            pivot.localRotation = Quaternion.Euler(0f, 0f, startAngle + swept);

        DrawTrail(swept, fade);
    }

    /// <summary>한 바퀴를 다 돌기 전엔 1. 그 뒤부터 fadeOut 에 걸쳐 0으로.</summary>
    float ResolveFade()
    {
        if (fadeOut <= 0f) return opacity;

        float over = elapsed - duration;
        if (over <= 0f) return opacity;

        return opacity * (1f - Mathf.Clamp01(over / fadeOut));
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

    void DrawTrail(float swept, float fade)
    {
        if (fan == null) return;

        fan.SetAlpha(fade);

        // 꼬리를 안 쓰면 지나간 자리가 전부 남는다 — 시작점부터 현재 각도까지
        if (trailAngle <= 0f)
        {
            fan.Draw(radius, startAngle, swept);
            return;
        }

        // 꼬리를 쓰면 스캔 라인 뒤 trailAngle 만큼만 남는다.
        // 도는 방향이 반대면 꼬리도 반대편에 달려야 한다
        float direction = clockwise ? -1f : 1f;
        float tail = Mathf.Min(Mathf.Abs(swept), trailAngle) * direction;

        fan.Draw(radius, startAngle + swept - tail, tail);
    }

#if UNITY_EDITOR
    // 씬에서 반경을 눈으로 확인할 수 있게
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.35f, 1f, 0.6f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? radius : fallbackRadius);
    }
#endif
}
