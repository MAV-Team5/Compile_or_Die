using System;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 증강 문구의 토큰을 현재 값으로 치환한다.
/// <b>설명 카드·로그·툴팁이 전부 이 하나를 쓴다</b> — 예전에는 카드와 로그가 각자
/// 토큰 목록을 들고 있어서, 한쪽에만 있는 토큰이 다른 쪽에서는 글자 그대로 나왔다.
///
/// <code>
/// {damage}          13.5      숫자 그대로
/// {damage:0}        14        C# 표준 서식
/// {effectDamage:%}  30%       비율을 퍼센트로 (×100 + %)
/// {effectDamage*100} 30       직접 곱하기 (÷ 도 된다)
/// </code>
///
/// 모르는 토큰은 <c>{이렇게}</c> 그대로 남는다 — 조용히 사라지면 오타를 못 찾는다.
/// </summary>
public static class AugmentText
{
    /// <summary>{이름}, {이름*2}, {이름:0.0} 을 한 번에 잡는다.</summary>
    static readonly Regex Token = new(
        @"\{\s*([A-Za-z]+)\s*(?:([*/])\s*([0-9.]+))?\s*(?::\s*([^}]+?))?\s*\}",
        RegexOptions.Compiled);

    // ── 바깥에서 부르는 입구 ───────────────────────────────

    /// <summary>
    /// 오른쪽 값을 물들일 색(HTML 16진). 카드가 테마에서 한 번 채워준다.
    /// 비어 있으면 색 없이 화살표만 나온다 — 로그처럼 서식이 안 되는 곳도 있기 때문.
    /// </summary>
    public static string ChangeColor;

    /// <summary>
    /// 카드·툴팁용. 발동 중이 아니어도 레벨만 알면 채울 수 있다.
    /// level 은 "이 증강이 그 레벨일 때"를 뜻한다 — 다음 레벨 미리보기에도 쓴다.
    /// </summary>
    public static string Describe(AugmentData data, int level) => Compare(data, 0, level);

    /// <summary>
    /// 레벨업으로 무엇이 달라지는지 보여준다. <c>피해량 12.6 → 15.0</c>
    ///
    /// <b>값이 실제로 바뀌는 토큰만 화살표가 붙는다.</b> 전부 붙이면 어디가 오르는지
    /// 눈에 안 들어오고, 안 변하는 수치까지 두 번 읽어야 한다.
    ///
    /// from 이 0이면 신규 획득이라 비교할 것이 없다 — 한쪽 값만 나온다.
    /// </summary>
    public static string Compare(AugmentData data, int from, int to)
    {
        if (data == null) return "";

        string template = string.IsNullOrEmpty(data.descriptionTemplate)
            ? $"{data.displayName} 증강."
            : data.descriptionTemplate;

        AugmentLevelData next = StatAt(data, to);

        // 신규거나 레벨이 같으면 비교할 것이 없다
        bool paired = from > 0 && from != to;
        AugmentLevelData prev = paired ? StatAt(data, from) : next;

        return Resolve(template,
                       key => Lookup(key, data.displayName, to, next, 0f),
                       paired ? key => Lookup(key, data.displayName, from, prev, 0f) : null);
    }

    /// <summary>적중 정보가 있는 시점용.</summary>
    public static string Format(string template, AugmentContext ctx, HitInfo hit)
    {
        if (string.IsNullOrEmpty(template) || ctx == null) return template;

        return Resolve(template, key => key switch
        {
            "target" => CleanName(hit.Target),
            "index" => hit.Index,
            _ => Lookup(key, ctx.Instance.Data.displayName, ctx.Instance.Level,
                        ctx.Stat, ctx.BonusDamage)
        });
    }

    /// <summary>적중 없이 발동 시점에만 아는 값들.</summary>
    public static string Format(string template, AugmentContext ctx)
    {
        if (string.IsNullOrEmpty(template) || ctx == null) return template;

        return Resolve(template, key => Lookup(key, ctx.Instance.Data.displayName,
                                               ctx.Instance.Level, ctx.Stat, ctx.BonusDamage));
    }

    // ── 토큰 표 ───────────────────────────────────────────

    /// <summary>
    /// 토큰 하나의 값. 없으면 null 을 돌려 원문을 그대로 남긴다.
    /// 시트 열 이름을 그대로 토큰으로 쓴다 — 기획자가 시트를 보며 문구를 쓸 수 있게.
    /// </summary>
    static object Lookup(string key, string name, int level, AugmentLevelData stat, float bonus)
        => key switch
        {
            "name" => name,
            "level" => level,

            "damage" => stat.damage + bonus,
            "effectDamage" => stat.effectDamage,
            "cooldown" => stat.cooldown,
            "range" => stat.range,
            "effectRange" => stat.effectRange,
            "duration" => stat.duration,
            "speed" => stat.speed,
            "count" => stat.count,
            "pierce" => stat.pierce,
            "depth" => stat.depth,

            _ => null
        };

    /// <summary>레벨은 1부터. 표 밖으로 나가면 가장 가까운 칸을 쓴다.</summary>
    static AugmentLevelData StatAt(AugmentData data, int level)
    {
        if (data.levelStats == null || data.levelStats.Length == 0) return default;

        return data.levelStats[Mathf.Clamp(level - 1, 0, data.levelStats.Length - 1)];
    }

    // ── 치환 ──────────────────────────────────────────────

    static string Resolve(string template, Func<string, object> lookup,
                          Func<string, object> before = null)
    {
        // 인스펙터에서 \n 을 글자로 친 경우를 살려준다.
        // TextArea 에서 Enter 를 친 진짜 줄바꿈은 그대로 통과한다
        string text = template.Replace("\\n", "\n");

        return Token.Replace(text, m =>
        {
            string key = m.Groups[1].Value;
            object value = lookup(key);

            // 모르는 토큰은 손대지 않는다. 사라지면 오타를 영영 못 찾는다
            if (value == null) return m.Value;

            if (value is string s) return s;

            string op = m.Groups[2].Value, operand = m.Groups[3].Value, fmt = m.Groups[4].Value;

            string now = Render(ToNumber(value), op, operand, fmt, value is int);

            if (before == null) return now;

            object was = before(key);
            if (was == null || was is string) return now;

            string old = Render(ToNumber(was), op, operand, fmt, was is int);

            // 글자로 같으면 화살표를 안 붙인다. 소수점 아래가 달라도 표시가 같으면
            // 플레이어에게는 안 바뀐 것이나 마찬가지다
            return old == now ? now : $"{old} → {Highlight(now)}";
        });
    }

    /// <summary>바뀐 값을 눈에 띄게. 색이 안 정해져 있으면 그대로 둔다.</summary>
    static string Highlight(string value)
        => string.IsNullOrEmpty(ChangeColor) ? value : $"<color={ChangeColor}>{value}</color>";

    static float ToNumber(object value) => value is int i ? i : (float)value;

    /// <summary>곱하고 나눈 뒤 서식을 입힌다.</summary>
    static string Render(float value, string op, string operand, string format, bool whole)
    {
        if (op.Length > 0 && float.TryParse(operand, out float n) && n != 0f)
        {
            value = op == "*" ? value * n : value / n;
            whole = false;   // 계산하면 소수가 나올 수 있다
        }

        if (format == "%") return (value * 100f).ToString("0.#") + "%";

        if (format.Length > 0) return value.ToString(format);

        // 서식을 안 줬으면 정수는 정수답게, 소수는 한 자리까지
        return whole ? value.ToString("0") : value.ToString("0.#");
    }

    /// <summary>풀링된 오브젝트의 "(Clone)" 을 떼어 로그를 깔끔하게 만든다.</summary>
    static string CleanName(Transform t)
    {
        if (t == null) return "?";

        string n = t.name;
        int paren = n.IndexOf("(Clone)", StringComparison.Ordinal);

        return paren > 0 ? n[..paren] : n;
    }
}
