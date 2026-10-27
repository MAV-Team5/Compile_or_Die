using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 런 시작 선택이 끝나면 조작법을 잠깐 띄운다. 게임은 멈추지 않는다.
/// 시간은 게임 시간으로 재므로 레벨업·일시정지 동안에는 같이 멈춘다.
///
/// 씬의 Canvas 에 붙여 두면 스스로 패널을 조립한다.
/// </summary>
public class ControlsHint : MonoBehaviour
{
    [Header("시간")]
    [Tooltip("시작 선택이 끝나고 몇 초 뒤에 띄울지.")]
    [Min(0f)] [SerializeField] float delay = 0.3f;

    [Tooltip("다 보인 채로 머무는 시간(초).")]
    [Min(0f)] [SerializeField] float showTime = 6f;

    [Min(0f)] [SerializeField] float fadeTime = 0.4f;

    [Tooltip("켜면 게임을 켠 뒤 첫 런에만 띄운다. 부스처럼 사람이 계속 바뀌면 끈다.")]
    [SerializeField] bool onlyFirstRun = false;

    [Header("모양")]
    [SerializeField] float fontSize = 46f;

    [Tooltip("화면 아래에서 얼마나 띄울지(px, 기준 해상도).")]
    [SerializeField] float bottomOffset = 260f;

    [Tooltip("동작 이름 칸 폭(글자 수, 한글은 2칸).")]
    [SerializeField] int labelWidth = 8;

    static readonly (string action, string keys)[] Rows =
    {
        ("이동", "WASD / 방향키"),
        ("선택", "Space"),
    };

    static bool shownThisSession;

    CanvasGroup group;
    Coroutine playing;

    void Awake() => Build();

    void OnEnable() => AugmentSelectUI.StartRoundsFinished += Play;
    void OnDisable() => AugmentSelectUI.StartRoundsFinished -= Play;

    void Play()
    {
        if (onlyFirstRun && shownThisSession) return;
        shownThisSession = true;

        if (playing != null) StopCoroutine(playing);
        playing = StartCoroutine(Run());
    }

    System.Collections.IEnumerator Run()
    {
        yield return Wait(delay);

        yield return FadeTo(1f);
        yield return Wait(showTime);
        yield return FadeTo(0f);

        playing = null;
    }

    System.Collections.IEnumerator Wait(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime) yield return null;
    }

    void Update()
    {
        // 런이 끝나면 결과 화면 위에 남지 않게 바로 걷는다
        if (playing == null || RunDirector.IsPlaying) return;

        StopCoroutine(playing);
        playing = null;
        group.alpha = 0f;
    }

    System.Collections.IEnumerator FadeTo(float target)
    {
        float from = group.alpha;

        for (float t = 0f; t < fadeTime; t += Time.deltaTime)
        {
            group.alpha = Mathf.Lerp(from, target, t / fadeTime);
            yield return null;
        }

        group.alpha = target;
    }

    // ── 조립 ──────────────────────────────────────────────

    void Build()
    {
        UiTheme theme = UiTheme.Current;
        TMP_FontAsset font = theme.mono;

        Image panel = UiFactory.CreateImage("ControlsHint", transform, UiTheme.Fade(theme.background, 0.85f));
        RectTransform rect = panel.rectTransform;

        // 하단 가운데에 붙이고 크기는 글자를 따른다
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottomOffset);

        Outline border = panel.gameObject.AddComponent<Outline>();
        border.effectColor = UiTheme.Fade(theme.accent, 0.6f);
        border.effectDistance = new Vector2(3f, -3f);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 32, 32);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fit = panel.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text title = UiFactory.CreateText("Title", panel.transform, font, fontSize, theme.accent, TextAlignmentOptions.Left);
        title.text = "> CONTROLS";
        title.textWrappingMode = TextWrappingModes.NoWrap;

        string keyColor = "#" + ColorUtility.ToHtmlStringRGB(theme.text);
        var body = new System.Text.StringBuilder();

        foreach (var (action, keys) in Rows)
            body.Append($"  {UiFactory.Pad(action, labelWidth)}<color={keyColor}>{keys}</color>\n");

        TMP_Text lines = UiFactory.CreateText("Rows", panel.transform, font, fontSize, theme.dim, TextAlignmentOptions.Left);
        lines.text = body.ToString().TrimEnd('\n');
        lines.textWrappingMode = TextWrappingModes.NoWrap;

        group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }
}
