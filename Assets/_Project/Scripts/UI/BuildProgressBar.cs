using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이머 아래 얇은 컴파일 진행률 바. 타이머가 주 정보고 이건 보조다.
///
/// <code>
///   보스 전   0 → 80%    시간 진행 × (1 − 버그 페널티)   살아 있는 버그가 많을수록 밀린다
///   보스 등장 80 → 30%    한순간에 꺼진다 — 거의 다 됐던 빌드가 무너지는 순간
///   보스 중   30 → 100%   보스 체력이 깎이는 만큼 오른다
///   클리어    100%
/// </code>
/// 보스가 나오는 시각은 여전히 시간이 정한다. 이 바는 보여주기만 한다.
/// </summary>
public class BuildProgressBar : MonoBehaviour
{
    [Tooltip("보스 등장 시점까지 차오르는 비율.")]
    [Range(0.1f, 0.95f)] [SerializeField] float bossShare = 0.8f;

    [Tooltip("보스가 나오는 순간 떨어지는 값. 여기서부터 보스 체력만큼 100%까지 오른다.")]
    [Range(0f, 0.95f)] [SerializeField] float bossDropTo = 0.3f;

    [Tooltip("떨어지는 순간 바가 하얗게 번쩍이는 시간(실제 초).")]
    [Min(0f)] [SerializeField] float dropFlashTime = 0.4f;

    [Tooltip("버그가 기준 마릿수에 닿았을 때 깎이는 최대 비율.")]
    [Range(0f, 0.5f)] [SerializeField] float maxBugPenalty = 0.15f;

    [Tooltip("페널티가 최대가 되는 버그 수. 0이면 스포너의 동시 생존 상한을 쓴다.\n" +
             "상한(150)까지 차는 일이 드물면 출렁임이 작으니, 실제로 자주 보이는 수 근처로 낮출 것.")]
    [Min(0)] [SerializeField] int bugsAtMaxPenalty = 0;

    [Tooltip("표시 값이 목표를 따라가는 속도. 작을수록 부드럽게 출렁인다.")]
    [Min(0.1f)] [SerializeField] float followSpeed = 3f;

    [Tooltip("버그 수 증감(▲▼)을 판정할 비교 간격(초).")]
    [Min(0.5f)] [SerializeField] float trendWindow = 3f;

    [Tooltip("이만큼 넘게 늘거나 줄어야 화살표를 띄운다.")]
    [Min(0)] [SerializeField] int trendThreshold = 3;

    RectTransform fill;
    Image fillImage;
    TMP_Text label;
    UiTheme theme;

    float shown;
    bool wasBossPhase;
    float flashUntil;
    float sampleTimer;
    float windowTimer;
    int lastSample = -1;
    int trend;   // +1 늘어남, -1 줄어듦, 0 그대로
    int bugs;

    /// <summary>타이머 바로 아래에 붙여 조립한다. GameHud 가 부른다.</summary>
    public static BuildProgressBar Create(Transform parent, UiTheme theme, TMP_FontAsset font,
                                          float topOffset, float width)
    {
        RectTransform root = UiFactory.CreateRect("BuildProgress", parent);
        UiFactory.Place(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -topOffset), new Vector2(width, 70f));

        BuildProgressBar bar = root.gameObject.AddComponent<BuildProgressBar>();
        bar.theme = theme;

        Image back = UiFactory.CreateImage("Track", root, theme.line);
        UiFactory.Place(back.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        Vector2.zero, new Vector2(width, 12f));

        bar.fillImage = UiFactory.CreateImage("Fill", back.transform, theme.accent);
        bar.fill = bar.fillImage.rectTransform;
        UiFactory.Stretch(bar.fill, Vector2.zero, new Vector2(0f, 1f));

        bar.label = UiFactory.CreateText("Label", root, font, 34f, theme.dim, TextAlignmentOptions.Center);
        UiFactory.Stretch(bar.label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -18f));
        bar.label.textWrappingMode = TextWrappingModes.NoWrap;

        return bar;
    }

    void Update()
    {
        RunDirector director = RunDirector.Current;
        if (director == null || director.EndingBossAt <= 0f || fill == null)
        {
            gameObject.SetActive(false);   // 끝을 정해주는 보스가 없으면 진행률이라는 말이 성립하지 않는다
            return;
        }

        SampleBugs();

        float target = Target(director, out bool bossPhase);

        // 보스가 나오는 순간은 미끄러지지 않고 툭 꺼진다. 천천히 내려가면 "무너졌다"로 안 읽힌다
        if (bossPhase && !wasBossPhase)
        {
            shown = bossDropTo;
            flashUntil = Time.unscaledTime + dropFlashTime;
        }
        wasBossPhase = bossPhase;

        // 클리어 순간은 바로 채운다. 따라가다 결과 화면으로 넘어가면 덜 찬 채로 끝난다
        shown = director.State == RunState.Cleared
            ? 1f
            : Mathf.Lerp(shown, target, 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime));

        fill.anchorMax = new Vector2(shown, 1f);
        fillImage.color = Time.unscaledTime < flashUntil ? Color.white
                        : bossPhase ? theme.warn : theme.accent;

        RefreshLabel(director, bossPhase);
    }

    float Target(RunDirector director, out bool bossPhase)
    {
        Enemy boss = director.EndingBoss;
        bossPhase = boss != null;

        if (director.State == RunState.Cleared) return 1f;

        if (bossPhase) return bossDropTo + (1f - bossDropTo) * (1f - boss.HealthRatio);

        float time = Mathf.Clamp01(RunDirector.RunTime / director.EndingBossAt);

        int full = bugsAtMaxPenalty > 0 ? bugsAtMaxPenalty
                 : Spawner.Current != null ? Spawner.Current.MaxAlive : 0;
        float crowd = full > 0 ? Mathf.Clamp01((float)bugs / full) : 0f;

        return bossShare * time * (1f - maxBugPenalty * crowd);
    }

    /// <summary>버그 수는 0.5초마다 센다. 화살표는 trendWindow 전 값과 비교한다.</summary>
    void SampleBugs()
    {
        sampleTimer -= Time.unscaledDeltaTime;
        if (sampleTimer > 0f) return;

        sampleTimer = 0.5f;
        bugs = Spawner.Current != null ? Spawner.Current.AliveCount : 0;

        windowTimer += 0.5f;
        if (windowTimer < trendWindow && lastSample >= 0) return;

        windowTimer = 0f;
        trend = lastSample < 0 ? 0
              : bugs > lastSample + trendThreshold ? 1
              : bugs < lastSample - trendThreshold ? -1
              : 0;
        lastSample = bugs;
    }

    // 지난번에 쓴 라벨 내용. 같으면 다시 쓰지 않는다 — 매 프레임 문자열을 만들면 쓰레기가 쌓인다
    int lastPercent = -1, lastBugs = -1, lastTrend, lastKills = -1;
    RunState lastState;
    bool lastBoss;

    void RefreshLabel(RunDirector director, bool bossPhase)
    {
        int percent = Mathf.FloorToInt(shown * 100f);
        int kills = RunDirector.KillCount;

        if (percent == lastPercent && bugs == lastBugs && trend == lastTrend && kills == lastKills &&
            director.State == lastState && bossPhase == lastBoss) return;

        lastPercent = percent;
        lastBugs = bugs;
        lastTrend = trend;
        lastKills = kills;
        lastState = director.State;
        lastBoss = bossPhase;

        label.text = Label(director, bossPhase, percent, kills);
    }

    string Label(RunDirector director, bool bossPhase, int percent, int kills)
    {
        string head = director.State == RunState.Cleared ? "BUILD SUCCESS"
                    : bossPhase ? "ERROR: unresolved symbol"
                    : "compiling...";

        string arrow = trend > 0 ? $" <color={Hex(theme.warn)}>▲</color>"
                     : trend < 0 ? $" <color={Hex(theme.good)}>▼</color>"
                     : "";

        return $"{head}  {percent}%     bugs {bugs}{arrow}     fixed {kills}";
    }

    static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
}
