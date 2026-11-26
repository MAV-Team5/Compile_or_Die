using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 상단의 보스 체력바. <see cref="HalloweenBossShow"/> 가 보스 등장 순간 만든다.
///
/// <list type="bullet">
/// <item>네온 테두리 + 어두운 바탕 + 빨간 체력</item>
/// <item>깎인 만큼 하얀 띠가 뒤늦게 따라 줄어든다 — 한 방에 얼마나 깎였는지 읽힌다</item>
/// <item><b>두 줄짜리 체력</b> — 1페이즈는 보라색 바 하나가 체력 100%→50% 를 다 쓴다.
///       보라색이 바닥나는 순간 2페이즈가 시작되고, 빨간 바가 새로 차올라 50%→0% 를 쓴다</item>
/// <item>페이즈가 바뀌면 테두리 색이 바뀌고 번쩍인다</item>
/// </list>
/// 체력은 <see cref="Enemy.HealthRatio"/> 만 읽는다.
/// </summary>
public class HalloweenBossBar : MonoBehaviour
{
    const float Width = 1200f;
    const float Height = 40f;
    const float Border = 5f;
    const float ChipDelay = 0.35f;   // 깎인 뒤 하얀 띠가 따라오기 시작하기까지
    const float ChipSpeed = 0.6f;    // 하얀 띠가 줄어드는 속도(비율/초)
    const float RefillTime = 0.7f;   // 2페이즈에 빨간 바가 차오르는 시간

    static readonly Color Phase1Fill = new(0.62f, 0.2f, 1f);
    static readonly Color Phase2Fill = new(0.95f, 0.1f, 0.22f);

    static readonly Color Phase1Border = new(1f, 0.15f, 0.3f);
    static readonly Color Phase2Border = new(0.3f, 0.85f, 1f);

    Enemy boss;
    float threshold;

    Image border;
    RectTransform fill;
    RectTransform chip;
    Image fillImage;
    int phase = 1;
    float refillStart = -1f;
    TextMeshProUGUI phaseLabel;
    CanvasGroup group;

    float shown = 1f;
    float chipShown = 1f;
    float lastDrop;
    float flash;
    Color borderColor = Phase1Border;

    public void Build(Enemy target, string bossName, float phaseThreshold)
    {
        boss = target;
        threshold = phaseThreshold;

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        group = gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        // 테두리 — 바 전체보다 사방으로 Border 만큼 큰 판
        RectTransform frame = Box("Border", transform, new Vector2(Width + Border * 2f, Height + Border * 2f), Phase1Border);
        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 1f);
        frame.pivot = new Vector2(0.5f, 1f);
        frame.anchoredPosition = new Vector2(0f, -70f);
        border = frame.GetComponent<Image>();

        RectTransform back = Box("Back", frame, new Vector2(Width, Height), new Color(0.04f, 0.01f, 0.03f, 0.92f));

        chip = Bar("Chip", back, new Color(1f, 0.92f, 0.95f, 0.9f));
        fill = Bar("Fill", back, Phase1Fill);
        fillImage = fill.GetComponent<Image>();

        Label("Name", frame, bossName, 36f, TextAlignmentOptions.BottomLeft, new Vector2(0f, 1f), new Vector2(Border, 6f));
        phaseLabel = Label("Phase", frame, "PHASE 1", 28f, TextAlignmentOptions.BottomRight, new Vector2(1f, 1f), new Vector2(-Border, 6f));
    }

    public void SetPhase(int phase)
    {
        borderColor = phase >= 2 ? Phase2Border : Phase1Border;
        phaseLabel.text = $"PHASE {phase}";
        phaseLabel.color = borderColor;
        flash = 1f;

        if (phase >= 2 || phase == this.phase) { this.phase = phase; return; }

        this.phase = phase;
    }

    /// <summary>지금 페이즈 안에서의 남은 비율(0~1). 1페이즈는 100→50%, 2페이즈는 50→0% 를 한 바로 쓴다.</summary>
    float SegmentRatio(float ratio)
        => phase >= 2 ? Mathf.Clamp01(ratio / threshold) : Mathf.InverseLerp(threshold, 1f, ratio);

    void Update()
    {
        if (boss == null) return;

        // 2페이즈로 막 넘어왔으면 빨간 바로 갈아입고 바닥부터 차오르게 한다
        if (phase >= 2 && refillStart < 0f)
        {
            refillStart = Time.unscaledTime;
            fillImage.color = Phase2Fill;
            chipShown = 0f;
        }

        float ratio = SegmentRatio(boss.HealthRatio);

        // 차오르는 중엔 연출이 우선이다
        if (refillStart >= 0f)
        {
            float k = Mathf.Clamp01((Time.unscaledTime - refillStart) / RefillTime);
            ratio = Mathf.Min(ratio, 1f - (1f - k) * (1f - k));
            if (k < 1f) chipShown = ratio;
        }

        if (ratio < shown - 0.0001f) lastDrop = Time.unscaledTime;
        shown = ratio;

        // 하얀 띠는 잠깐 기다렸다가 따라 줄어든다
        if (chipShown > shown && Time.unscaledTime - lastDrop > ChipDelay)
            chipShown = Mathf.Max(shown, chipShown - ChipSpeed * Time.unscaledDeltaTime);
        if (chipShown < shown) chipShown = shown;

        fill.anchorMax = new Vector2(shown, 1f);
        chip.anchorMax = new Vector2(chipShown, 1f);

        // 네온 테두리 — 은은하게 숨 쉬고, 페이즈가 바뀌면 하얗게 번쩍인다
        flash = Mathf.Max(0f, flash - Time.unscaledDeltaTime * 1.5f);
        float breath = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 3f);
        border.color = Color.Lerp(borderColor * breath, Color.white, flash);
    }

    // ───────────────────────── 만들기 도우미 ─────────────────────────

    static RectTransform Box(string name, Transform parent, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        return rect;
    }

    /// <summary>왼쪽에 붙어서 anchorMax.x 로 길이가 정해지는 막대.</summary>
    static RectTransform Bar(string name, Transform parent, Color color)
    {
        RectTransform r = Box(name, parent, Vector2.zero, color);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        return r;
    }

    static TextMeshProUGUI Label(string name, Transform parent, string text, float size,
                                 TextAlignmentOptions align, Vector2 anchor, Vector2 offset)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(anchor.x, 0f);
        rect.sizeDelta = new Vector2(600f, size * 1.3f);
        rect.anchoredPosition = offset;

        var t = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = UiTheme.Current != null ? UiTheme.Current.mono : null;
        if (font != null) t.font = font;

        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;

        return t;
    }
}
