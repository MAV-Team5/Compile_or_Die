using UnityEngine;

/// <summary>
/// 증강 문구의 토큰을 현재 값으로 치환한다.
/// 로그와 설명문이 같은 규칙을 쓴다.
/// </summary>
public static class AugmentText
{
    /// <summary>적중 정보가 있는 시점용.</summary>
    public static string Format(string template, AugmentContext ctx, HitInfo hit)
    {
        if (string.IsNullOrEmpty(template)) return template;

        string text = Format(template, ctx);

        if (template.Contains("{target}"))
            text = text.Replace("{target}", CleanName(hit.Target));

        if (template.Contains("{index}"))
            text = text.Replace("{index}", hit.Index.ToString());

        return text;
    }

    /// <summary>적중 없이 발동 시점에만 아는 값들.</summary>
    public static string Format(string template, AugmentContext ctx)
    {
        if (string.IsNullOrEmpty(template)) return template;

        AugmentLevelData stat = ctx.Stat;
        string text = template;

        if (text.Contains("{name}"))
            text = text.Replace("{name}", ctx.Instance.Data.displayName);

        if (text.Contains("{level}"))
            text = text.Replace("{level}", ctx.Instance.Level.ToString());

        if (text.Contains("{depth}"))
            text = text.Replace("{depth}", ctx.Depth.ToString());

        if (text.Contains("{damage}"))
            text = text.Replace("{damage}", (stat.damage * ctx.DamageMultiplier).ToString("0.#"));

        if (text.Contains("{count}"))
            text = text.Replace("{count}", stat.count.ToString());

        if (text.Contains("{range}"))
            text = text.Replace("{range}", stat.range.ToString("0.#"));

        return text;
    }

    /// <summary>풀링된 오브젝트의 "(Clone)" 을 떼어 로그를 깔끔하게 만든다.</summary>
    static string CleanName(Transform t)
    {
        if (t == null) return "?";

        string n = t.name;
        int paren = n.IndexOf("(Clone)");

        return paren > 0 ? n[..paren] : n;
    }
}
