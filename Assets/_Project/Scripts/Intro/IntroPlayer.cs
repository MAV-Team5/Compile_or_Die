using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 인트로 씬. <see cref="IntroScript"/> 를 터미널처럼 한 줄씩 흘리고, 끝나면 다음 씬으로 넘어간다.
///
/// <b>화면은 코드로 조립한다.</b> 씬에는 카메라와 이 컴포넌트가 붙은 오브젝트 하나면 된다.
/// <b>아무 키나 누르면 건너뛴다</b> — 시연장에서는 두 번째 사람부터 대부분 넘긴다.
/// </summary>
public class IntroPlayer : MonoBehaviour
{
    [Header("대본")]
    [Tooltip("＊ 필수 — 흘릴 대본.")]
    [SerializeField] IntroScript script;

    [Tooltip("끝나거나 건너뛰면 갈 씬. Build Settings 에 들어 있어야 한다.")]
    [SerializeField] string nextScene = "MainA";

    [Header("화면")]
    [Tooltip("비우면 UiTheme 의 고정폭 글꼴.")]
    [SerializeField] TMP_FontAsset font;

    [SerializeField] float fontSize = 30f;

    [Tooltip("화면에 남겨둘 최대 줄 수. 넘치면 위에서부터 밀려 올라간다.")]
    [Min(1)] [SerializeField] int maxLines = 18;

    [SerializeField] Color warningColor = new(1f, 0.8f, 0.3f, 1f);

    [Header("글리치")]
    [Tooltip("글리치 줄에서 글자가 흔들리는 최대 거리(픽셀).")]
    [SerializeField] float shakeDistance = 14f;

    [SerializeField] float shakeSeconds = 0.25f;

    [Header("소리 — 비우면 안 난다")]
    [Tooltip("Command 를 타이핑할 때 글자마다. 여러 개면 랜덤.")]
    [SerializeField] AudioClip[] typeClips;

    [Range(0f, 1f)] [SerializeField] float typeVolume = 0.35f;

    [SerializeField] AudioClip[] glitchClips;

    [Range(0f, 1f)] [SerializeField] float glitchVolume = 0.7f;

    [Tooltip("암전 순간.")]
    [SerializeField] AudioClip blackoutClip;

    [Range(0f, 1f)] [SerializeField] float blackoutVolume = 0.8f;

    const string CursorGlyph = "_";
    const int BarWidth = 20;

    readonly List<string> shown = new();
    readonly StringBuilder builder = new();

    TMP_Text body;
    TMP_Text skipHint;
    RectTransform bodyRect;
    Vector2 bodyHome;

    UiTheme theme;
    bool leaving;

    void Awake()
    {
        theme = UiTheme.Current;
        BuildScreen();
    }

    void Start()
    {
        if (script == null)
        {
            Debug.LogError("[IntroPlayer] 대본(IntroScript)이 없다. 바로 다음 씬으로 넘어간다.", this);
            Leave();
            return;
        }

        StartCoroutine(Play());
    }

    void Update()
    {
        if (!leaving && SkipPressed()) Leave();
    }

    // ── 흐름 ──────────────────────────────────────────────

    IEnumerator Play()
    {
        for (int i = 0; i < script.lines.Count; i++)
        {
            IntroScript.Line line = script.lines[i];
            if (line == null) continue;

            if (line.glitch) Glitch();

            switch (line.kind)
            {
                case IntroScript.LineKind.Command:
                    yield return TypeCommand(line);
                    break;

                case IntroScript.LineKind.Progress:
                    yield return FillProgress(line);
                    continue;   // 막대가 차는 시간이 곧 대기라 아래 대기를 건너뛴다

                case IntroScript.LineKind.Blackout:
                    yield return Blackout(line);
                    continue;

                default:
                    Push(Colorize(line.text, line.kind));
                    break;
            }

            yield return Wait(line.holdSeconds);
        }

        Leave();
    }

    IEnumerator TypeCommand(IntroScript.Line line)
    {
        string prompt = string.IsNullOrEmpty(line.prompt) ? script.defaultPrompt : line.prompt;
        string head = Paint(prompt, theme.accent);

        Push(head + CursorGlyph);

        // 프롬프트에 잠깐 커서가 깜빡이는 틈. 사람이 치기 시작하기 전의 망설임이다
        yield return Wait(0.25f);

        float perChar = 1f / Mathf.Max(1f, script.charsPerSecond);

        for (int c = 1; c <= line.text.Length; c++)
        {
            ReplaceLast(head + line.text.Substring(0, c) + CursorGlyph);

            if (!char.IsWhiteSpace(line.text[c - 1])) SfxPlayer.PlayAny(typeClips, typeVolume, 0.02f);

            yield return Wait(perChar);
        }

        ReplaceLast(head + line.text);
    }

    IEnumerator FillProgress(IntroScript.Line line)
    {
        Push("");

        float duration = Mathf.Max(0.01f, line.holdSeconds);

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            ReplaceLast(ProgressText(line.text, Mathf.Lerp(0f, line.progressTo, t / duration)));
            yield return null;
        }

        ReplaceLast(ProgressText(line.text, line.progressTo));
    }

    /// <summary>실제 패키지 매니저들이 쓰는 모양 그대로 — [#####.....] 61%</summary>
    static string ProgressText(string label, float percent)
    {
        int filled = Mathf.RoundToInt(BarWidth * percent / 100f);

        return $"> {label} [{new string('#', filled)}{new string('.', BarWidth - filled)}] {percent:0}%";
    }

    IEnumerator Blackout(IntroScript.Line line)
    {
        shown.Clear();
        Refresh();

        SfxPlayer.Play(blackoutClip, blackoutVolume);

        yield return Wait(line.holdSeconds);
    }

    // ── 화면 ──────────────────────────────────────────────

    void Push(string text)
    {
        shown.Add(text);

        while (shown.Count > maxLines) shown.RemoveAt(0);

        Refresh();
    }

    void ReplaceLast(string text)
    {
        if (shown.Count == 0) shown.Add(text);
        else shown[shown.Count - 1] = text;

        Refresh();
    }

    void Refresh()
    {
        builder.Clear();

        for (int i = 0; i < shown.Count; i++)
        {
            if (i > 0) builder.Append('\n');
            builder.Append(shown[i]);
        }

        body.text = builder.ToString();
    }

    string Colorize(string text, IntroScript.LineKind kind) => kind switch
    {
        IntroScript.LineKind.Warning => Paint(text, warningColor),
        IntroScript.LineKind.Error   => Paint(text, theme.warn),
        _                            => Paint(text, theme.dim)
    };

    static string Paint(string text, Color color)
        => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";

    void Glitch()
    {
        SfxPlayer.PlayAny(glitchClips, glitchVolume, 0.05f);
        StartCoroutine(Shake());
    }

    IEnumerator Shake()
    {
        for (float t = 0f; t < shakeSeconds; t += Time.unscaledDeltaTime)
        {
            // 끝으로 갈수록 잦아든다
            float strength = shakeDistance * (1f - t / shakeSeconds);
            bodyRect.anchoredPosition = bodyHome + Random.insideUnitCircle * strength;
            yield return null;
        }

        bodyRect.anchoredPosition = bodyHome;
    }

    void BuildScreen()
    {
        var canvasGo = new GameObject("IntroCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image background = UiFactory.CreateImage("Background", canvasGo.transform, Color.black);
        UiFactory.Stretch(background.rectTransform, Vector2.zero, Vector2.one);

        TMP_FontAsset face = theme.FontOr(font);

        body = UiFactory.CreateText("Body", canvasGo.transform, face, fontSize, theme.text,
                                    TextAlignmentOptions.TopLeft);
        bodyRect = body.rectTransform;
        UiFactory.Stretch(bodyRect, Vector2.zero, Vector2.one, new Vector2(80f, 80f), new Vector2(-80f, -80f));
        bodyHome = bodyRect.anchoredPosition;
        body.richText = true;

        skipHint = UiFactory.CreateText("SkipHint", canvasGo.transform, face, fontSize * 0.6f,
                                        theme.dim, TextAlignmentOptions.BottomRight);
        UiFactory.Stretch(skipHint.rectTransform, Vector2.zero, Vector2.one, new Vector2(40f, 30f), new Vector2(-40f, -30f));
        skipHint.text = "[ANY KEY] skip";
    }

    // ── 나가기 ────────────────────────────────────────────

    static bool SkipPressed()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;

        return Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
    }

    void Leave()
    {
        if (leaving) return;

        leaving = true;
        StopAllCoroutines();

        SceneManager.LoadScene(nextScene);
    }

    /// <summary>인트로는 시간 정지와 상관없이 흘러야 한다. 실제 시간으로 잰다.</summary>
    static WaitForSecondsRealtime Wait(float seconds) => new(seconds);
}
