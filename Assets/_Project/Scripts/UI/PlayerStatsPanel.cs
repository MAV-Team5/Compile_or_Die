using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// 일시정지 화면의 상태 표. 지금 캐릭터가 어떤 상태인지 한눈에 본다.
///
/// <b>배치는 씬에서 한다.</b> 이 컴포넌트는 글자만 채운다 —
/// 칸을 코드로 만들면 여백 하나 고치는 데도 컴파일을 기다려야 한다.
///
/// PausePanel 에 붙이고, 아래 세 칸에 TMP 오브젝트를 물린다.
/// <b>물리지 않은 칸은 그냥 건너뛴다</b> — 세 구역 중 원하는 것만 둬도 된다.
///
/// 갱신은 열릴 때 한 번이면 된다. 일시정지 중에는 값이 안 변하기 때문.
/// </summary>
public class PlayerStatsPanel : MonoBehaviour
{
    [Header("글자 칸 — 씬에 배치한 TMP 를 물린다")]
    [Tooltip("체력 · 이동속도 · 레벨.")]
    [SerializeField] TMP_Text characterText;

    [Tooltip("증강 전역 보정. 걸린 것이 없으면 '보정 없음'.")]
    [SerializeField] TMP_Text modifierText;

    [Tooltip("시간 · 처치 수 · 리롤 · 보유 증강.")]
    [SerializeField] TMP_Text runText;

    [Header("모양")]
    [Tooltip("이름 칸의 너비(글자 수). 고정폭 글꼴이면 값이 세로로 줄맞춤된다.\n" +
             "한글은 두 칸으로 세므로 실제 글자 수보다 크게 잡을 것.")]
    [SerializeField] int labelWidth = 14;

    readonly StringBuilder sb = new();

    void OnEnable() => Refresh();

    /// <summary>바깥에서 강제로 다시 그리고 싶을 때.</summary>
    public void Refresh()
    {
        if (characterText != null) characterText.text = BuildCharacter();
        if (modifierText != null) modifierText.text = BuildModifiers();
        if (runText != null) runText.text = BuildRun();
    }

    // ── 구역 ──────────────────────────────────────────────

    string BuildCharacter()
    {
        sb.Clear();

        Player player = GameManager.instance != null ? GameManager.instance.player : null;

        if (player != null && player.TryGetComponent(out PlayerHealth health))
            Row("체력", $"{Mathf.CeilToInt(health.Current)} / {health.Max:0}");

        if (player != null)
        {
            // 배율이 1이면 버프가 없다는 뜻이라 굳이 적지 않는다
            string boost = Mathf.Approximately(player.SpeedMultiplier, 1f)
                ? ""
                : $"   ({Signed(player.SpeedMultiplier - 1f, percent: true)})";

            Row("이동속도", $"{player.CurrentSpeed:0.#}{boost}");
        }

        LevelSystem level = GameManager.instance != null ? GameManager.instance.levelSystem : null;

        if (level != null)
            Row("레벨", $"Lv {level.Level}  ({level.CurrentExp} / {level.RequiredExp})");

        return Done("캐릭터 정보 없음");
    }

    string BuildModifiers()
    {
        sb.Clear();

        PlayerStats stats = PlayerStats.Current;

        if (stats == null) return "보정 없음";

        foreach (StatKind kind in System.Enum.GetValues(typeof(StatKind)))
        {
            if (!stats.HasBonus(kind)) continue;

            Row(LabelOf(kind), Describe(stats, kind));
        }

        // 하드웨어 업그레이드를 배선하기 전까지는 보통 여기가 빈다
        return Done("보정 없음");
    }

    string BuildRun()
    {
        sb.Clear();

        Row("시간", RunResult.Format(RunDirector.RunTime));
        Row("처치", RunDirector.KillCount.ToString());

        RunDirector run = RunDirector.Current;

        if (run != null)
        {
            Row("남은 리롤", run.Rerolls.ToString());
            Row("모은 비트", run.Bits.ToString());
        }

        AugmentManager augments = FindAnyObjectByType<AugmentManager>();

        if (augments != null) Row("보유 증강", $"{augments.Runners.Count}개");

        return Done("");
    }

    // ── 줄 만들기 ─────────────────────────────────────────

    void Row(string label, string value)
        => sb.Append(Pad(label, labelWidth)).Append(value).Append('\n');

    string Done(string empty)
    {
        if (sb.Length == 0) return empty;

        sb.Length--;   // 마지막 줄바꿈은 뺀다. 아래 여백이 한 줄 더 생긴다
        return sb.ToString();
    }

    /// <summary>
    /// 이름 칸을 공백으로 채워 값이 세로로 맞게 한다.
    ///
    /// <b>한글은 두 칸을 차지한다.</b> 글자 수로만 세면 고정폭 글꼴에서도
    /// "체력"과 "이동속도"의 값 시작 위치가 어긋난다.
    /// </summary>
    static string Pad(string label, int width)
    {
        int used = 0;

        for (int i = 0; i < label.Length; i++) used += IsWide(label[i]) ? 2 : 1;

        return label + new string(' ', Mathf.Max(1, width - used));
    }

    /// <summary>한글·한자·가나처럼 두 칸을 쓰는 글자인가.</summary>
    static bool IsWide(char c)
        => (c >= 0x1100 && c <= 0x115F)     // 한글 자모
        || (c >= 0x2E80 && c <= 0xA4CF)     // 한자·부수·가나
        || (c >= 0xAC00 && c <= 0xD7A3)     // 한글 음절
        || (c >= 0xF900 && c <= 0xFAFF)     // 호환 한자
        || (c >= 0xFF00 && c <= 0xFF60);    // 전각 기호

    // ── 보정 읽기 ─────────────────────────────────────────

    static string Describe(PlayerStats stats, StatKind kind)
    {
        float add = stats.AddOf(kind);
        float percent = stats.PercentOf(kind);

        // 쿨타임만 배율이 오를수록 짧아진다. +20% 라고 적으면 거꾸로 읽힌다
        bool inverted = kind == StatKind.Cooldown;

        string a = Mathf.Approximately(add, 0f)
            ? "" : Signed(inverted ? -add : add, percent: false);

        string p = Mathf.Approximately(percent, 0f)
            ? "" : Signed(inverted ? -percent : percent, percent: true);

        if (a.Length == 0) return p;
        if (p.Length == 0) return a;

        return $"{a}   {p}";
    }

    /// <summary>부호를 붙인다. 0보다 크면 +, 작으면 −.</summary>
    static string Signed(float value, bool percent)
    {
        string body = percent ? $"{Mathf.Abs(value) * 100f:0.#}%" : $"{Mathf.Abs(value):0.#}";

        return (value >= 0f ? "+" : "−") + body;
    }

    static string LabelOf(StatKind kind) => kind switch
    {
        StatKind.Damage => "피해량",
        StatKind.EffectDamage => "효과 피해",
        StatKind.Cooldown => "쿨타임",
        StatKind.Range => "사거리",
        StatKind.EffectRange => "효과 범위",
        StatKind.Duration => "지속시간",
        StatKind.Speed => "투사체 속도",
        StatKind.Count => "수량",
        StatKind.Pierce => "관통력",
        StatKind.Depth => "깊이",
        _ => kind.ToString()
    };
}
