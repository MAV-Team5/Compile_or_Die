using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 런이 끝난 뒤 결과를 씬의 패널 위에 적는다. StageResult 씬에서 쓴다.
///
/// <b>두 화면으로 나뉜다.</b>
/// <code>
/// Content      결과 · 시간 · 처치 · 레벨 · 보상      플레이어가 볼 것
/// Log Content  증강별 피해 표 · 보상 내역            들여다볼 사람만 볼 것
/// </code>
/// 첫 화면에 표를 다 깔면 정작 "이겼나 졌나" 가 안 보인다.
/// 상세는 로그 버튼 뒤에 숨기고, 첫 화면은 한눈에 읽히게 둔다.
///
/// <b>창과 버튼은 씬이 갖는다.</b> 이 컴포넌트는 글자와 막대만 채운다 —
/// 배경·버튼을 코드가 또 만들면 씬에 이미 있는 것과 겹쳐 두 벌이 된다.
///
/// <see cref="RunResult.Last"/> 만 읽는다 — 씬을 넘어오면서 살아남은 순수 C# 객체다.
/// </summary>
public class RunResultPanel : MonoBehaviour
{
    [Header("어디에 적나")]
    [Tooltip("＊ 필수 — 요약을 채울 자리. 씬의 결과 패널(RectTransform)을 물린다.")]
    [SerializeField] RectTransform content;

    [Tooltip("증강별 피해 표를 채울 자리. 씬의 logPanel 안을 물린다.\n" +
             "비워두면 표를 아예 안 그린다 — 요약만 쓰고 싶을 때.")]
    [SerializeField] RectTransform logContent;

    [Header("글꼴")]
    [Tooltip("비우면 UiTheme 의 고정폭 글꼴을 쓴다. 열 맞춤은 고정폭에서만 제대로 보인다.")]
    [SerializeField] TMP_FontAsset fontOverride;

    [Header("여백")]
    [Tooltip("패널 안쪽 좌우 여백(px).")]
    [SerializeField] float padding = 40f;

    [Tooltip("첫 줄이 시작할 위치. 패널 위에서 이만큼 내려온다(px).")]
    [SerializeField] float topOffset = 32f;

    [Tooltip("한 줄의 높이(px). 글자 크기의 1.3~1.5배는 돼야 안 답답하다.")]
    [SerializeField] float lineHeight = 30f;

    [Tooltip("구역 사이에 띄울 줄 수.")]
    [SerializeField] float blockGap = 1.4f;

    [Header("글자 크기")]
    [SerializeField] float titleSize = 42f;
    [SerializeField] float bodySize = 22f;
    [SerializeField] float tableSize = 20f;

    [Header("표 — 로그 패널")]
    [Tooltip("증강 표에 최대 몇 줄까지. 나머지는 '그 외' 로 묶는다.\n" +
             "로그 패널 높이에 맞춰 조절할 것 — 넘치면 잘려 나간다.")]
    [SerializeField] int maxRows = 12;

    [Tooltip("이름 칸의 너비(글자 수). 한글은 두 칸으로 센다.")]
    [SerializeField] int nameWidth = 18;

    [Tooltip("숫자 칸 너비(글자 수). 오른쪽 정렬이라 자릿수가 늘어도 안 밀린다.")]
    [SerializeField] int totalWidth = 10;
    [SerializeField] int shareWidth = 8;
    [SerializeField] int hitsWidth = 8;
    [SerializeField] int avgWidth = 8;

    [Tooltip("비중 막대의 진하기. 0.3 을 넘으면 글자가 안 읽힌다.")]
    [Range(0f, 1f)][SerializeField] float barAlpha = 0.20f;

    [Header("문구")]
    [Tooltip("맨 위에 흘릴 프롬프트. 컴파일을 다시 시도하는 장면으로 보이게 한다.")]
    [SerializeField] string prompt = "$ gcc -o stage_01 main.c";

    [SerializeField] string clearedTitle = "COMPILE SUCCEEDED";
    [SerializeField] string failedTitle = "COMPILE FAILED";

    [Tooltip("요약 맨 아래에 남길 안내. 상세가 로그에 있다는 걸 모르면 아무도 안 연다.")]
    [SerializeField] string logHint = "  자세한 피해 기록은 LOG";

    /// <summary>내가 만든 줄만 모아두는 자리. 다시 그릴 때 이것만 버린다.</summary>
    RectTransform lines;
    RectTransform logLines;

    /// <summary>지금 그리는 중인 자리. 요약과 로그를 같은 함수로 그리려고 둔다.</summary>
    RectTransform sheet;

    UiTheme theme;

    TMP_FontAsset font => theme.FontOr(fontOverride);

    void Start()
    {
        // 앞 씬에서 붙잡던 것들은 씬과 함께 파괴돼 스스로 놓을 수 없다
        TimeControl.ReleaseAll();

        Rebuild();
    }

    /// <summary>인스펙터에서 값을 바꾼 뒤 눌러 볼 수 있게 열어둔다.</summary>
    [ContextMenu("다시 그리기")]
    public void Rebuild()
    {
        if (content == null)
        {
            Debug.LogWarning($"[{name}] Content 가 비어 있다. 요약을 적을 자리를 물릴 것 — "
                           + "씬의 결과 패널(RectTransform)이다.", this);
            return;
        }

        theme = UiTheme.Current;

        lines = Reset(lines, content);
        logLines = logContent != null ? Reset(logLines, logContent) : null;

        RunResult result = RunResult.Last;

        DrawSummary(result);
        DrawLog(result);
    }

    /// <summary>지난번에 그린 것을 버리고 새 자리를 만든다. 씬에 원래 있던 자식은 안 건드린다.</summary>
    RectTransform Reset(RectTransform old, RectTransform parent)
    {
        if (old != null) Destroy(old.gameObject);

        RectTransform made = UiFactory.CreateRect("ResultLines", parent);
        UiFactory.Stretch(made, Vector2.zero, Vector2.one);

        return made;
    }

    // ── 첫 화면 ───────────────────────────────────────────

    void DrawSummary(RunResult result)
    {
        sheet = lines;

        float y = -topOffset;

        if (result == null)
        {
            // 결과 없이 이 씬에 바로 들어온 경우. 빈 화면이면 버그로 오해한다
            Text(y, "결과 없음 — 스테이지에서 넘어오지 않았다", theme.warn, bodySize);
            return;
        }

        Text(y, prompt, theme.dim, bodySize);
        y -= lineHeight;

        bool ok = result.Cleared;

        Text(y, "> " + (ok ? clearedTitle : failedTitle),
             ok ? theme.good : theme.warn, titleSize);

        y -= titleSize * 1.3f + lineHeight * (blockGap - 1f);

        y = Field(y, "elapsed", result.ElapsedText);
        y = Field(y, "resolved", $"{result.Kills:N0}");
        y = Field(y, "level", $"Lv {result.Level}");

        if (result.BossesDefeated.Count > 0)
            y = Field(y, "boss", string.Join(", ", result.BossesDefeated));

        y -= lineHeight * (blockGap - 1f);
        y = Rule(y);

        y = Field(y, "reward", $"+{result.Reward:N0} bit", theme.accent);
        y = Field(y, "balance", $"{PlayerProgress.Bits:N0} bit");

        if (logContent != null && !string.IsNullOrEmpty(logHint))
        {
            y -= lineHeight * (blockGap - 1f);
            Text(y, logHint, theme.dim, tableSize);
        }
    }

    // ── 로그 패널 ─────────────────────────────────────────

    void DrawLog(RunResult result)
    {
        if (logLines == null || result == null) return;

        sheet = logLines;

        float y = -topOffset;

        Text(y, "DAMAGE BY AUGMENT", theme.text, bodySize);
        y -= lineHeight;

        if (result.Damage.Count == 0 || result.TotalDamage <= 0f)
        {
            Text(y, "  기록된 피해가 없다", theme.dim, tableSize);
            return;
        }

        Text(y, Columns("NAME", "TOTAL", "SHARE", "HITS", "AVG"), theme.dim, tableSize);
        y -= lineHeight;

        int shown = Mathf.Min(maxRows, result.Damage.Count);

        for (int i = 0; i < shown; i++)
        {
            Row(y, result.Damage[i], result.TotalDamage);
            y -= lineHeight;
        }

        // 표에 안 들어간 몫을 버리면 비중 합이 100%가 아닌 이유를 알 수 없다
        if (result.Damage.Count > shown)
        {
            float rest = 0f;
            for (int i = shown; i < result.Damage.Count; i++) rest += result.Damage[i].TotalDamage;

            Text(y, Columns($"... {result.Damage.Count - shown}개 더", $"{rest:N0}",
                            $"{rest / result.TotalDamage:P1}", "", ""),
                 theme.dim, tableSize);

            y -= lineHeight;
        }

        y = Rule(y);

        Text(y, Columns("TOTAL", $"{result.TotalDamage:N0}", "100.0%", "", ""),
             theme.text, tableSize);

        y -= lineHeight * blockGap;

        // 보상이 왜 이만큼인지는 기획자용 정보다. 첫 화면이 아니라 여기가 자리다
        Text(y, "REWARD BREAKDOWN", theme.text, bodySize);
        y -= lineHeight;

        Text(y, $"  bits {result.BitsCollected} · kills {result.RewardFromKills} · " +
                $"time {result.RewardFromTime} · clear {result.RewardFromClear}",
             theme.dim, tableSize);
    }

    // ── 조각 ──────────────────────────────────────────────

    /// <summary><c>key   value</c> 형태 한 줄. 열이 맞아야 훑어보기 좋다.</summary>
    float Field(float y, string key, string value, Color? tint = null)
    {
        Text(y, "  " + UiFactory.Pad(key, 10) + value, tint ?? theme.text, bodySize);

        return y - lineHeight;
    }

    string Columns(string name, string total, string share, string hits, string avg)
        => "  " + UiFactory.Pad(name, nameWidth)
                + UiFactory.Pad(total, totalWidth, right: true)
                + UiFactory.Pad(share, shareWidth, right: true)
                + UiFactory.Pad(hits, hitsWidth, right: true)
                + UiFactory.Pad(avg, avgWidth, right: true);

    /// <summary>증강 한 줄. 뒤에 분류색 비중 막대가 깔린다.</summary>
    void Row(float y, RunStats.Entry entry, float total)
    {
        float share = total > 0f ? entry.TotalDamage / total : 0f;

        // 분류색을 쓰면 "어느 계열이 딜을 했나" 가 표를 안 읽어도 보인다
        Color tint = entry.Category.HasValue
            ? theme.ColorOf(entry.Category.Value)
            : theme.dim;

        // 막대를 글자보다 먼저 깔아야 글자가 위에 온다
        Image bar = UiFactory.CreateImage("Bar", sheet, UiTheme.Fade(tint, barAlpha));

        RectTransform box = bar.rectTransform;
        box.anchorMin = new Vector2(0f, 1f);
        box.anchorMax = new Vector2(Mathf.Max(share, 0.003f), 1f);
        box.offsetMin = new Vector2(padding * 0.5f, y - lineHeight + 4f);
        box.offsetMax = new Vector2(0f, y);

        string label = entry.Level > 0 ? $"{entry.Name} Lv{entry.Level}" : entry.Name;

        Text(y, Columns(label, $"{entry.TotalDamage:N0}", $"{share:P1}",
                        $"{entry.Hits:N0}", $"{entry.PerHit:N0}"),
             theme.text, tableSize);
    }

    /// <summary>구역을 나누는 가로선.</summary>
    float Rule(float y)
    {
        Image line = UiFactory.CreateImage("Rule", sheet, theme.line);

        UiFactory.Stretch(line.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                          new Vector2(padding * 0.5f, y - 1f),
                          new Vector2(-padding * 0.5f, y + 1f));

        return y - lineHeight * blockGap * 0.8f;
    }

    /// <summary>왼쪽 정렬 한 줄. 패널 안쪽에 좌우로 꽉 채운다.</summary>
    TMP_Text Text(float y, string body, Color color, float size)
    {
        TMP_Text label = UiFactory.CreateText("Line", sheet, font, size, color,
                                              TextAlignmentOptions.TopLeft);

        RectTransform rect = label.rectTransform;

        // 네 변을 전부 offset 으로 잡는다. sizeDelta 와 섞으면 어느 쪽이 이겼는지 알 수 없다
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(padding, y - size * 1.6f);
        rect.offsetMax = new Vector2(-padding, y);

        label.text = body;

        return label;
    }
}
