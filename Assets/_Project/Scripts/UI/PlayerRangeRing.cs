using UnityEngine;

/// <summary>
/// 플레이어 발밑에 그리는 <b>점선 사거리 원</b>. 점이 차오르는 정도가 곧 장전 진행도다.
///
/// <code>
///   ● ● ● ● ○ ○ ○ ○      장전 중 — 밝은 점이 늘어난다
///   ● ● ● ● ● ● ● ●      장전 완료
///   ● ● ● ○ ○ ○ ○ ○      발사 중 — 남은 탄만큼 줄어든다
/// </code>
///
/// <b>왜 점선인가</b> — 꽉 찬 원이나 굵은 테두리는 전투를 가린다.
/// 점선은 "월드의 물체"가 아니라 "보조선"으로 읽혀서, 계속 켜두어도 눈에 안 거슬린다.
/// 점 개수를 고정했으므로 사거리가 늘어도 촘촘함이 그대로다 —
/// 스프라이트를 늘리는 방식은 반경이 커질수록 점선이 늘어져서 다른 물건처럼 보인다.
///
/// <b>반경은 트리거가 정한다</b> (<see cref="TriggerModule.DisplayRadius"/>).
/// 0을 돌려주는 트리거에서는 원이 아예 안 뜬다 — 평범한 사거리까지 다 그리면
/// 화면이 동심원으로 덮이기 때문이다. 지금은 <c>Iteration:while</c> 의 while 반경만 그린다.
///
/// <b>어느 증강을 보여줄지는 인스펙터에서 지정한다.</b> 증강이 열 개면 원도 열 개가 되어
/// 아무것도 안 보이기 때문이다.
///
/// 플레이어의 자식으로 두면 알아서 따라다닌다.
/// </summary>
public class PlayerRangeRing : MonoBehaviour
{
    /// <summary>원을 언제 보여줄지.</summary>
    public enum ShowWhen
    {
        /// <summary>항상. 사거리를 계속 의식해야 하는 증강에.</summary>
        Always,

        /// <summary>장전이 도는 동안만. 쏘는 중에는 사라져 화면이 덜 복잡하다.</summary>
        Reloading,

        /// <summary>장전이 끝나 쏠 수 있을 때만. "지금 쏘면 여기까지" 를 보여준다.</summary>
        Ready
    }

    [Header("무엇을 보여줄지")]
    [Tooltip("＊ 필수 — 이 증강의 사거리와 장전 상태를 그린다.\n" +
             "아직 안 뽑았으면 원이 통째로 숨는다.")]
    [SerializeField] AugmentData watch;

    [Tooltip("원이 뜨는 조건.\n\n" +
             "Always     — 항상\n" +
             "Reloading  — 장전 중에만 (쏘는 동안은 사라짐)\n" +
             "Ready      — 쏠 수 있을 때만")]
    [SerializeField] ShowWhen showWhen = ShowWhen.Reloading;

    [Tooltip("장전이 끝났다고 볼 진행도. 1이면 완전히 찼을 때만 Ready 로 친다.")]
    [Range(0.5f, 1f)] [SerializeField] float readyThreshold = 0.999f;

    [Header("점선")]
    [Tooltip("점 하나에 쓸 그림. 비우면 하얀 사각형이 된다.")]
    [SerializeField] Sprite dotSprite;

    [Tooltip("점 개수. 많을수록 촘촘하지만 그만큼 렌더러가 늘어난다. 24~40 권장.")]
    [Min(4)] [SerializeField] int dotCount = 32;

    [Tooltip("점 하나의 지름(월드 유닛).")]
    [Min(0.01f)] [SerializeField] float dotSize = 0.12f;

    [Tooltip("아직 안 찬 점의 색. ★ 알파를 낮게 둘 것 — 전투를 가리면 안 된다.")]
    [SerializeField] Color idleColor = new(0.30f, 0.82f, 0.88f, 0.18f);

    [Tooltip("찬 점의 색. 장전 진행도를 이 색으로 보여준다.")]
    [SerializeField] Color filledColor = new(0.30f, 0.82f, 0.88f, 0.85f);

    [Header("움직임")]
    [Tooltip("초당 회전 각도. 천천히 돌면 '탐색 중'으로 읽힌다. 0이면 고정.")]
    [SerializeField] float spinSpeed = 10f;

    [Tooltip("정렬 레이어 이름. ★ 비워두면 Default 로 가는데,\n" +
             "배경이 다른 레이어에 있으면 그 뒤로 숨어 안 보인다.\n" +
             "다른 연출과 같은 레이어 이름을 적을 것.")]
    [SerializeField] string sortingLayerName = "";

    [Tooltip("정렬 순서. 플레이어·적보다 뒤지만 배경보다는 앞이어야 한다.\n" +
             "안 보이면 이 값을 올려볼 것 — 이 프로젝트의 연출들은 0~15 를 쓴다.")]
    [SerializeField] int sortingOrder = 1;

    /// <summary>
    /// 반경 배수. 그림이 판정보다 미묘하게 커/작아 보일 때 눈으로 맞추는 칸이다.
    /// <b>1에서 크게 벗어나게 두지 말 것</b> — 원이 실제 판정과 어긋나면 플레이어를 속이게 된다.
    /// </summary>
    [Min(0.05f)] [SerializeField] float rangeScale = 1f;

    Transform spinner;
    SpriteRenderer[] dots;
    AugmentRunner runner;
    float searchTimer;
    bool warned;

    static Sprite fallbackDot;

    /// <summary>점 그림을 안 넣었을 때 쓰는 흰 사각형. 한 번만 만든다.</summary>
    static Sprite FallbackDot
    {
        get
        {
            if (fallbackDot != null) return fallbackDot;

            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];

            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;

            tex.SetPixels(pixels);
            tex.Apply();

            fallbackDot = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);

            return fallbackDot;
        }
    }

    void Awake() => Build();

    void Build()
    {
        // 회전은 이 자식만 돈다. 이 컴포넌트가 붙은 오브젝트를 돌리면 형제까지 같이 돈다
        spinner = new GameObject("Ring").transform;
        spinner.SetParent(transform, false);

        dots = new SpriteRenderer[dotCount];

        for (int i = 0; i < dotCount; i++)
        {
            var go = new GameObject($"Dot{i}", typeof(SpriteRenderer));
            go.transform.SetParent(spinner, false);

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();

            // 스프라이트가 없으면 SpriteRenderer 는 아무것도 안 그린다 (UI Image 와 다르다).
            // 비워둔 채로 "왜 안 보이지" 하는 일이 없게 흰 점을 만들어 쓴다
            sr.sprite = dotSprite != null ? dotSprite : FallbackDot;
            sr.color = idleColor;
            sr.sortingOrder = sortingOrder;

            if (!string.IsNullOrEmpty(sortingLayerName))
                sr.sortingLayerName = sortingLayerName;

            dots[i] = sr;
        }

        SetVisible(false);
    }

    void LateUpdate()
    {
        AugmentRunner found = Resolve();

        if (found == null || found.Instance == null)
        {
            SetVisible(false);
            return;
        }

        // ★ 에셋이 아니라 Build 를 본다. 내부 증강(Iteration:while)이 트리거를 갈아끼웠으면 그쪽이다
        TriggerModule trigger = found.Instance.Build.Trigger;

        if (trigger == null)
        {
            SetVisible(false);
            return;
        }

        // 그릴 반경은 트리거가 정한다. while 조건처럼 "의식해야 하는 거리" 만 0이 아니다 —
        // 평범한 사거리까지 다 그리면 화면이 동심원으로 덮인다
        float radius = trigger.DisplayRadius(found.Instance) * rangeScale;

        if (radius <= 0.01f)
        {
            SetVisible(false);
            return;
        }

        float progress = trigger.Progress(found.Instance);

        bool ready = progress >= readyThreshold;

        // 조건에 안 맞으면 통째로 감춘다. 계속 켜두면 전투 중에 시야를 먹는다
        bool show = showWhen switch
        {
            ShowWhen.Reloading => !ready,
            ShowWhen.Ready     => ready,
            _                  => true
        };

        if (!show)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        if (spinSpeed != 0f)
            spinner.localRotation = Quaternion.Euler(0f, 0f, Time.time * spinSpeed);

        // 차오른 점의 수. 진행도가 0.5면 절반이 밝다
        int lit = Mathf.RoundToInt(Mathf.Clamp01(progress) * dotCount);

        for (int i = 0; i < dots.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / dots.Length;

            dots[i].transform.localPosition =
                new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);

            dots[i].transform.localScale = Vector3.one * dotSize / SpriteUnit(dots[i]);

            dots[i].color = i < lit ? filledColor : idleColor;
        }
    }

    /// <summary>스프라이트 원본 크기. 어떤 그림을 넣어도 dotSize 로 나오게 나눠준다.</summary>
    static float SpriteUnit(SpriteRenderer sr)
    {
        if (sr.sprite == null) return 1f;

        Vector2 size = sr.sprite.bounds.size;
        float longest = Mathf.Max(size.x, size.y);

        return longest > 0.0001f ? longest : 1f;
    }

    /// <summary>
    /// 볼 증강의 러너를 찾는다.
    ///
    /// 매 프레임 찾지 않는다 — 아직 안 뽑은 증강은 계속 못 찾으므로 헛도는 조회가 된다.
    /// 한 번 찾으면 그대로 쓰고, 못 찾은 동안만 이따금 다시 본다.
    /// </summary>
    AugmentRunner Resolve()
    {
        if (runner != null && runner.Instance != null) return runner;

        if (watch == null)
        {
            if (!warned)
            {
                warned = true;
                Debug.LogWarning("[PlayerRangeRing] Watch 가 비어 있다. 볼 증강을 지정해야 원이 뜬다.", this);
            }

            return null;
        }

        if (AugmentManager.Current == null) return null;

        searchTimer -= Time.deltaTime;
        if (searchTimer > 0f) return null;

        searchTimer = 0.5f;
        runner = AugmentManager.Current.Find(watch);

        return runner;
    }

    void SetVisible(bool on)
    {
        if (spinner != null && spinner.gameObject.activeSelf != on)
            spinner.gameObject.SetActive(on);
    }
}
