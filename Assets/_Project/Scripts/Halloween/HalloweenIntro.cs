using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 할로윈 스테이지 시작 연출. <see cref="HalloweenBossShow"/> 가 배경과 함께 띄운다.
///
/// <list type="number">
/// <item><b>평소처럼</b> — 처음 <c>introSeconds</c> 초는 원래 게임 모습이다. 격자 배경, 원래 BGM, 몬스터 없음.</item>
/// <item><b>지지직</b> — 화면이 깨지며 "ERROR CODE: SEIREN". 그 사이 배경이 치이카와로 바뀌고 음악이 끊긴다.</item>
/// <item><b>임무</b> — "세이렌을 처치하고 먼작귀들을 되돌리시오". 할로윈 BGM 이 시작되고 몬스터가 나오기 시작한다.</item>
/// </list>
///
/// 원래 모습은 원래 배경 프리팹을 잠깐 깔았다가 걷는 것으로 낸다. 몬스터는 스포너를 꺼둬서 안 나온다 —
/// 할로윈 스테이지 몬스터는 이미 치이카와라 "원래 모습" 동안 나오면 안 된다.
/// 경과 시간(보스 등장 시각)은 그대로 흐른다.
/// </summary>
public class HalloweenIntro : MonoBehaviour
{
    const float GlitchTime = 1.8f;        // ERROR 화면
    const float SwapAt = 0.9f;            // 그중 배경이 바뀌는 순간
    const float MissionTime = 2.8f;       // 임무 문구
    const float FadeTime = 0.5f;
    const float GlitchTick = 0.05f;
    const int BarCount = 16;

    static readonly Color Red = new(1f, 0.15f, 0.3f);
    static readonly Color Cyan = new(0.2f, 0.95f, 1f);

    GameObject originalBackground;
    AudioClip originalBgm;
    float originalVolume;
    AudioClip halloweenBgm;
    float halloweenVolume;
    float introSeconds;
    string errorText;
    string missionText;

    SpriteRenderer halloweenBackground;
    GameObject originalInstance;
    Spawner spawner;
    BgmSource sceneBgm;

    GameObject overlay;
    CanvasGroup group;
    Image tint;
    RectTransform[] bars;
    TextMeshProUGUI[] layers;   // 빨강·청록·흰색 — 색이 갈라지는 글자

    public void Setup(GameObject originalBg, AudioClip originalClip, float originalVol,
                      AudioClip halloweenClip, float halloweenVol, float seconds, string error, string mission)
    {
        originalBackground = originalBg;
        originalBgm = originalClip;
        originalVolume = originalVol;
        halloweenBgm = halloweenClip;
        halloweenVolume = halloweenVol;
        introSeconds = seconds;
        errorText = error;
        missionText = mission;

        halloweenBackground = GetComponent<SpriteRenderer>();
        if (halloweenBackground != null) halloweenBackground.enabled = false;

        if (originalBackground != null)
            originalInstance = Instantiate(originalBackground, Vector3.zero, Quaternion.identity);

        spawner = FindFirstObjectByType<Spawner>();
        if (spawner != null) spawner.enabled = false;

        // 씬의 배경음 표지판이 아직 안 돌았으면 막는다 — 할로윈 곡이 먼저 새어 나오지 않게.
        // 이미 돌았으면 다음 프레임에 원래 곡으로 덮는다(Start)
        sceneBgm = FindFirstObjectByType<BgmSource>();
        if (sceneBgm != null) sceneBgm.enabled = false;

        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        yield return null;
        if (originalBgm != null) BgmPlayer.Play(originalBgm, originalVolume);

        // 평소처럼. 게임 시간으로 센다(일시정지하면 같이 멈춘다)
        for (float t = 0f; t < introSeconds; t += Time.deltaTime) yield return null;

        BuildOverlay();
        PlayNoise();

        // ── ERROR ──
        SetText(errorText, 120f);
        bool swapped = false;

        for (float t = 0f; t < GlitchTime; t += Time.unscaledDeltaTime)
        {
            Glitch(1f);

            if (!swapped && t >= SwapAt)
            {
                swapped = true;
                SwapToHalloween();
            }

            yield return null;
        }

        if (!swapped) SwapToHalloween();

        // ── 임무 ── 음악과 몬스터가 여기서 시작된다
        SetText(missionText, 76f);
        if (halloweenBgm != null) BgmPlayer.Play(halloweenBgm, halloweenVolume);
        if (spawner != null) spawner.enabled = true;

        for (float t = 0f; t < MissionTime; t += Time.unscaledDeltaTime)
        {
            // 처음엔 세게 깨지다가 금방 또렷해진다
            Glitch(Mathf.Clamp01(1f - t / 0.8f) * 0.8f + 0.06f);
            yield return null;
        }

        for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / FadeTime;
            Glitch(0.05f);
            yield return null;
        }

        Destroy(overlay);
        Destroy(this);
    }

    void SwapToHalloween()
    {
        if (originalInstance != null) Destroy(originalInstance);
        if (halloweenBackground != null) halloweenBackground.enabled = true;
        BgmPlayer.Stop();
    }

    void OnDestroy()
    {
        // 연출 도중 판이 끝나도 스포너가 꺼진 채 남지 않게
        if (spawner != null && !spawner.enabled) spawner.enabled = true;
        if (overlay != null) Destroy(overlay);
    }

    // ───────────────────────── 지지직 화면 ─────────────────────────

    float nextTick;

    /// <summary><paramref name="power"/> 0~1. 클수록 많이·크게 깨진다.</summary>
    void Glitch(float power)
    {
        if (Time.unscaledTime < nextTick) return;
        nextTick = Time.unscaledTime + GlitchTick;

        // 화면 전체가 어둡게 번쩍인다
        tint.color = new Color(0.02f, 0f, 0.05f, Random.Range(0.35f, 0.75f) * Mathf.Max(0.3f, power));

        // 가로 줄이 아무 데나 생겼다 사라진다
        for (int i = 0; i < bars.Length; i++)
        {
            bool on = Random.value < 0.55f * power;
            bars[i].gameObject.SetActive(on);
            if (!on) continue;

            bars[i].anchoredPosition = new Vector2(Random.Range(-200f, 200f) * power, Random.Range(-540f, 540f));
            bars[i].sizeDelta = new Vector2(Random.Range(600f, 2400f), Random.Range(3f, 40f));
            var img = bars[i].GetComponent<Image>();
            Color c = (i % 3) switch { 0 => Red, 1 => Cyan, _ => Color.white };
            c.a = Random.Range(0.25f, 0.7f);
            img.color = c;
        }

        // 글자 색이 갈라지고 옆으로 튄다. 가끔 크게 어긋난다
        float split = Random.Range(4f, 18f) * power;
        float jump = Random.value < 0.25f * power ? Random.Range(-60f, 60f) : 0f;

        layers[0].rectTransform.anchoredPosition = new Vector2(-split + jump, Random.Range(-3f, 3f) * power);
        layers[1].rectTransform.anchoredPosition = new Vector2(split + jump, Random.Range(-3f, 3f) * power);
        layers[2].rectTransform.anchoredPosition = new Vector2(jump * 0.5f, 0f);

        // 아주 가끔 통째로 꺼진다
        layers[2].alpha = Random.value < 0.12f * power ? 0.15f : 1f;
    }

    void BuildOverlay()
    {
        overlay = new GameObject("Halloween Intro Glitch");

        var canvas = overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;

        var scaler = overlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        group = overlay.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        tint = Rect("Tint", Vector2.zero, Color.clear);
        var tr = tint.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;

        bars = new RectTransform[BarCount];
        for (int i = 0; i < BarCount; i++) bars[i] = Rect("Bar", new Vector2(100f, 10f), Color.white).rectTransform;

        layers = new[] { Label("R", Red), Label("C", Cyan), Label("W", Color.white) };
    }

    Image Rect(string name, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(overlay.transform, false);
        var r = (RectTransform)go.transform;
        r.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    TextMeshProUGUI Label(string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(overlay.transform, false);
        var r = (RectTransform)go.transform;
        r.sizeDelta = new Vector2(1800f, 400f);

        var t = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = UiTheme.Current != null ? UiTheme.Current.mono : null;
        if (font != null) t.font = font;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    void SetText(string text, float size)
    {
        foreach (TextMeshProUGUI t in layers)
        {
            t.text = text;
            t.fontSize = size;
        }
    }

    /// <summary>지지직 소리. 따로 파일 없이 잡음을 만들어 튼다 — 끊겼다 이어지는 라디오 잡음처럼.</summary>
    static void PlayNoise()
    {
        const int rate = 22050;
        const float seconds = 0.9f;
        int n = (int)(rate * seconds);
        var data = new float[n];

        float hold = 0f;
        for (int i = 0; i < n; i++)
        {
            // 짧게 끊기는 구간을 섞어 "치직—치직" 하게
            bool gap = Mathf.Sin(i * 0.0021f) > 0.6f && Random.value < 0.9f;
            if (i % 3 == 0) hold = Random.Range(-1f, 1f);
            float fade = 1f - i / (float)n;
            data[i] = gap ? 0f : hold * 0.35f * fade;
        }

        AudioClip clip = AudioClip.Create("HalloweenGlitchNoise", n, 1, rate, false);
        clip.SetData(data, 0);
        SfxPlayer.Play(clip, 0.8f, 0f);
    }
}
