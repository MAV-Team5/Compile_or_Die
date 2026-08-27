using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 런이 끝난 뒤 뜨는 결과 패널. StageResult 씬의 빈 오브젝트에 붙인다.
///
/// <see cref="RunResult.Last"/> 만 읽는다 — 씬을 넘어오면서 살아남은 순수 C# 객체다.
/// 아트가 아직 없으므로 GameHud 와 같은 방식으로 코드에서 조립한다.
///
/// <b>이 패널의 핵심은 증강별 피해 표다.</b> 어느 증강이 실제로 일했는지가
/// 시트 밸런싱을 고칠 유일한 근거이기 때문에, 요약보다 위쪽 자리를 준다.
/// </summary>
public class RunResultPanel : MonoBehaviour
{
    [Header("글꼴")]
    [Tooltip("비우면 UiTheme 의 고정폭 글꼴을 쓴다.")]
    [SerializeField] TMP_FontAsset fontOverride;

    [Header("씬 이동")]
    [Tooltip("다시 하기로 돌아갈 스테이지 씬.")]
    [SerializeField] string retryScene = "stage1";

    [Tooltip("하드웨어 업그레이드·스테이지 선택이 있는 로비.")]
    [SerializeField] string lobbyScene = "MainB";

    [Tooltip("나가기로 돌아갈 첫 화면.")]
    [SerializeField] string titleScene = "MainA";

    [Header("표")]
    [Tooltip("증강 표에 최대 몇 줄까지 보여줄지. 나머지는 '그 외'로 묶는다.")]
    [SerializeField] int maxRows = 10;

    const float RowHeight = 30f;
    const float Margin = 40f;

    const float ButtonWidth = 300f;
    const float ButtonGap = 40f;

    RectTransform root;

    UiTheme theme;

    /// <summary>인스펙터에 물린 게 있으면 그것, 없으면 테마 글꼴.</summary>
    TMP_FontAsset font => theme.FontOr(fontOverride);

    void Start()
    {
        // 앞 씬에서 붙잡던 것들은 씬과 함께 파괴돼 스스로 놓을 수 없다
        TimeControl.ReleaseAll();

        theme = UiTheme.Current;

        Build();
    }

    void Build()
    {
        root = CreateCanvas();

        Image panel = UiFactory.CreateImage("Panel", root, theme.background);
        UiFactory.Stretch(panel.rectTransform, Vector2.zero, Vector2.one,
                          new Vector2(Margin, Margin), new Vector2(-Margin, -Margin));

        RunResult result = RunResult.Last;

        if (result == null)
        {
            // 결과 없이 이 씬에 바로 들어온 경우. 조용히 빈 화면을 보여주면 버그로 오해한다
            Line(panel.rectTransform, -Margin, "결과 없음 — 스테이지에서 넘어오지 않았다", theme.dim, 28f);
            Buttons(panel.rectTransform);
            return;
        }

        float y = -30f;

        y = Header(panel.rectTransform, y, result);
        y = Summary(panel.rectTransform, y, result);
        y = DamageTable(panel.rectTransform, y, result);

        Buttons(panel.rectTransform);
    }

    // ── 구역 ──────────────────────────────────────────────

    float Header(RectTransform parent, float y, RunResult result)
    {
        string title = result.Cleared ? "COMPILE SUCCEEDED" : "COMPILE FAILED";
        Color color = result.Cleared ? theme.good : theme.warn;

        Line(parent, y, title, color, 44f);

        return y - 62f;
    }

    float Summary(RectTransform parent, float y, RunResult result)
    {
        Line(parent, y,
             $"버틴 시간 {result.ElapsedText}    처치 {result.Kills}    도달 Lv {result.Level}",
             theme.text, 24f);

        y -= 32f;

        if (result.BossesDefeated.Count > 0)
        {
            Line(parent, y, $"처치한 보스: {string.Join(", ", result.BossesDefeated)}", theme.dim, 20f);
            y -= 28f;
        }

        // 보상은 왜 이만큼인지가 보여야 기획자가 계수를 고칠 수 있다
        Line(parent, y,
             $"보상 {result.Reward} bit   " +
             $"( 비트 {result.BitsCollected} + 처치 {result.RewardFromKills} " +
             $"+ 시간 {result.RewardFromTime} + 클리어 {result.RewardFromClear} )",
             theme.text, 22f);

        y -= 30f;

        Line(parent, y, $"보유 {PlayerProgress.Bits} bit", theme.dim, 20f);

        return y - 44f;
    }

    float DamageTable(RectTransform parent, float y, RunResult result)
    {
        Line(parent, y, "증강별 피해", theme.text, 26f);
        y -= 34f;

        if (result.Damage.Count == 0 || result.TotalDamage <= 0f)
        {
            Line(parent, y, "기록된 피해가 없다", theme.dim, 20f);
            return y - RowHeight;
        }

        int shown = Mathf.Min(maxRows, result.Damage.Count);

        for (int i = 0; i < shown; i++)
        {
            Row(parent, y, result.Damage[i], result.TotalDamage);
            y -= RowHeight;
        }

        // 표에 안 들어간 몫을 버리면 비중 합이 100%가 아닌 이유를 알 수 없다
        if (result.Damage.Count > shown)
        {
            float rest = 0f;
            for (int i = shown; i < result.Damage.Count; i++) rest += result.Damage[i].TotalDamage;

            Line(parent, y,
                 $"그 외 {result.Damage.Count - shown}개   {rest:N0}   " +
                 $"{rest / result.TotalDamage:P1}",
                 theme.dim, 18f);

            y -= RowHeight;
        }

        return y;
    }

    /// <summary>증강 한 줄 — 이름·수치·비중 막대.</summary>
    void Row(RectTransform parent, float y, RunStats.Entry entry, float total)
    {
        float share = total > 0f ? entry.TotalDamage / total : 0f;

        // 막대를 글자 뒤에 먼저 깔아야 글자가 위에 온다
        Image bar = UiFactory.CreateImage("Bar", parent, UiTheme.Fade(theme.accent, 0.22f));

        RectTransform box = bar.rectTransform;

        // 부모 폭을 모르는 시점이라 오른쪽 앵커로 비율을 잡는다.
        // 0이면 아예 안 보여서 줄이 비어 보이므로 최소 폭을 남긴다
        box.anchorMin = new Vector2(0f, 1f);
        box.anchorMax = new Vector2(Mathf.Max(share, 0.004f), 1f);
        box.offsetMin = new Vector2(20f, y - (RowHeight - 6f));
        box.offsetMax = new Vector2(0f, y);

        string level = entry.Level > 0 ? $" Lv{entry.Level}" : "";

        TMP_Text left = Line(parent, y, $"{entry.Name}{level}", theme.text, 20f);
        left.alignment = TextAlignmentOptions.Left;
        left.rectTransform.offsetMin = new Vector2(32f, left.rectTransform.offsetMin.y);

        TMP_Text right = Line(parent, y,
            $"{entry.TotalDamage:N0}    {share:P1}    {entry.Hits}회    평균 {entry.PerHit:N0}",
            theme.dim, 18f);

        right.alignment = TextAlignmentOptions.Right;
        right.rectTransform.offsetMax = new Vector2(-32f, right.rectTransform.offsetMax.y);
    }

    // ── 조각 ──────────────────────────────────────────────

    RectTransform CreateCanvas()
    {
        var go = new GameObject("RunResultCanvas", typeof(Canvas), typeof(CanvasScaler),
                                typeof(GraphicRaycaster));

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        return (RectTransform)go.transform;
    }

    /// <summary>위에서부터 y 만큼 내려온 자리에 한 줄. 좌우로는 부모에 꽉 채운다.</summary>
    TMP_Text Line(RectTransform parent, float y, string text, Color color, float size)
    {
        TMP_Text label = UiFactory.CreateText("Line", parent, font, size, color,
                                              TextAlignmentOptions.Center);

        RectTransform rect = label.rectTransform;

        // 네 변을 전부 offset 으로 잡는다. sizeDelta·anchoredPosition 과 섞으면
        // 어느 쪽이 이겼는지 알 수 없어 줄이 어긋난다
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(20f, y - size * 1.4f);
        rect.offsetMax = new Vector2(-20f, y);

        label.text = text;

        return label;
    }

    void Buttons(RectTransform parent)
    {
        (string label, string scene, LobbyIntent.Screen open)[] entries =
        {
            ("다시 하기",   retryScene, LobbyIntent.Screen.None),
            ("업그레이드", lobbyScene, LobbyIntent.Screen.Upgrade),
            ("나가기",     titleScene, LobbyIntent.Screen.None)
        };

        // 가운데를 기준으로 좌우 대칭. 개수가 바뀌어도 자리가 알아서 맞는다
        float step = ButtonWidth + ButtonGap;
        float start = -(entries.Length - 1) * 0.5f * step;

        for (int i = 0; i < entries.Length; i++)
            Button(parent, new Vector2(start + step * i, 40f),
                   entries[i].label, entries[i].scene, entries[i].open);
    }

    /// <summary>
    /// 배경 상자 없이 글자만 있는 버튼. 씬의 기존 버튼과 같은 양식이다.
    ///
    /// <b>네온은 글자를 물들여서 낸다.</b> 최종 색 = 글자색 × 상태색 × 배수 라서,
    /// 글자색을 어둡게 깔아두면 평소에는 잠긴 초록이고 커서를 올리면 형광으로 튄다.
    /// 상자를 물들이는 방식으로는 이 느낌이 안 나온다.
    /// </summary>
    void Button(RectTransform parent, Vector2 position, string text, string scene,
                LobbyIntent.Screen open = LobbyIntent.Screen.None)
    {
        // 글자에는 raycast 가 없다. 글자 사이 빈틈에서도 눌리도록 판정면을 따로 깐다.
        // 완전히 투명하므로 상자로 보이지는 않는다
        Image hitArea = UiFactory.CreateImage($"Btn_{text}", parent, Color.clear);
        hitArea.raycastTarget = true;

        UiFactory.Place(hitArea.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        position, new Vector2(ButtonWidth, 56f));

        TMP_Text label = UiFactory.CreateText("Label", hitArea.rectTransform, font, 24f,
                                              Color.white, TextAlignmentOptions.Center);

        UiFactory.Stretch(label.rectTransform, Vector2.zero, Vector2.one);

        // 터미널 프롬프트. 씬의 기존 버튼도 같은 모양이다
        label.text = $"> {text}";

        Button button = hitArea.gameObject.AddComponent<Button>();
        button.targetGraphic = hitArea;

        // 색은 NeonTextButton 이 글자에 직접 넣는다.
        // Button 의 ColorTint 로는 아틀라스가 갈린 한글에서 색이 따로 논다
        button.transition = Selectable.Transition.None;

        hitArea.gameObject.AddComponent<NeonTextButton>().Bind(label);

        string target = scene;
        LobbyIntent.Screen intent = open;

        button.onClick.AddListener(() =>
        {
            if (string.IsNullOrEmpty(target)) return;

            // 로비에 도착해서 어느 화면을 열지 미리 적어둔다. 씬을 넘기기 전이어야 한다
            LobbyIntent.Request(intent);

            SceneManager.LoadScene(target);
        });
    }
}
