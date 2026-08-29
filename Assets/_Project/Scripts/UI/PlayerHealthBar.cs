using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 발밑을 따라다니는 체력바. 스프라이트를 코드로 만들어 프리팹이 필요 없다.
///
/// <b>칸으로 나눈다.</b> 매끄러운 막대는 얼마나 남았는지는 알려주지만
/// "몇 대 더 맞을 수 있나"는 알려주지 않는다. 칸이 하나씩 꺼지면 그게 셀 수 있는 수가 된다.
///
/// <b>칸 수가 아니라 칸당 체력을 고정한다.</b> 그래서 최대 체력이 큰 캐릭터는
/// 바 자체가 길어진다 — 비율 막대로 두면 100 짜리와 200 짜리가 똑같이 보인다.
/// 바 길이가 곧 그 캐릭터가 얼마나 튼튼한지다.
///
/// <b>평소에는 숨어 있다.</b> 체력이 바뀔 때만 잠깐 떠오르고, 위험할 때는 계속 보인다.
/// </summary>
public class PlayerHealthBar : MonoBehaviour
{
    /// <summary>사라지기 직전에 흐려지는 시간. 툭 꺼지면 눈에 거슬린다.</summary>
    const float FadeTime = 0.4f;

    /// <summary>
    /// 바의 생김새와 규칙. 값은 <see cref="PlayerHealth"/> 인스펙터에 있다 —
    /// 여기는 코드로 만들어지는 오브젝트라 인스펙터가 없다.
    /// </summary>
    public struct Layout
    {
        public Vector2 Offset;
        public Vector2 SegmentSize;
        public float SegmentGap;
        public float HealthPerSegment;
        public int MaxSegments;
        public float ShowTime;
        public float AlwaysShowBelow;
    }

    static Sprite whiteSprite;

    PlayerHealth health;
    Transform follow;
    Layout layout;

    readonly List<SpriteRenderer> segments = new();

    /// <summary>지금 조립된 칸이 어느 최대 체력을 기준으로 만들어졌는가.</summary>
    float builtForMax = -1f;

    /// <summary>칸 하나가 맡는 체력. 칸 수 상한에 걸리면 이 값이 커진다.</summary>
    float healthPerSegment;

    float ratio = 1f;
    float remain;
    float appliedAlpha = -1f;

    public static PlayerHealthBar Create(PlayerHealth target, Layout layout)
    {
        var go = new GameObject("PlayerHealthBar");
        var bar = go.AddComponent<PlayerHealthBar>();
        bar.Build(target, layout);
        return bar;
    }

    void Build(PlayerHealth target, Layout barLayout)
    {
        health = target;
        follow = target.transform;
        layout = barLayout;

        health.Changed += OnChanged;

        // 런 시작에 한 번 띄운다 — 체력바가 어디 있는지는 알려주고 시작해야 한다
        OnChanged(health.Current, health.Max);
    }

    // ── 조립 ──────────────────────────────────────────────

    /// <summary>
    /// 최대 체력에 맞춰 칸을 다시 만든다.
    ///
    /// 하드웨어(SSD)가 <c>Start</c> 에서 최대 체력을 늘리는데, 이 바도 <c>Start</c> 에서
    /// 만들어져 둘 사이 순서가 없다. 그래서 조립 시점에 한 번 재는 것으로는 부족하고,
    /// 최대 체력이 바뀌면 그때 다시 만든다.
    /// </summary>
    void EnsureSegments(float max)
    {
        if (Mathf.Approximately(builtForMax, max)) return;

        builtForMax = max;

        for (int i = 0; i < segments.Count; i++)
            if (segments[i] != null) Destroy(segments[i].gameObject);

        segments.Clear();

        float per = Mathf.Max(1f, layout.HealthPerSegment);
        int count = Mathf.Max(1, Mathf.CeilToInt(max / per));

        // 너무 길어지면 바가 화면을 가로지른다. 상한에 걸리면 한 칸이 더 많이 맡는다
        if (layout.MaxSegments > 0 && count > layout.MaxSegments)
        {
            count = layout.MaxSegments;
            per = max / count;
        }

        healthPerSegment = per;

        float step = layout.SegmentSize.x + layout.SegmentGap;
        float start = -(count - 1) * 0.5f * step;

        for (int i = 0; i < count; i++)
        {
            SpriteRenderer segment = MakeQuad($"Seg{i}", 21);

            segment.transform.localPosition = new Vector3(start + step * i, 0f, 0f);
            segment.transform.localScale =
                new Vector3(layout.SegmentSize.x, layout.SegmentSize.y, 1f);

            segments.Add(segment);
        }

        appliedAlpha = -1f;   // 새로 만들었으니 색을 다시 칠해야 한다
    }

    SpriteRenderer MakeQuad(string name, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSprite();

        // 정렬 레이어는 순서가 아니라 레이어 자체가 우선이다.
        // "Default"(제일 아래)에 두면 Background/Enemy/Player 레이어에 전부 가려진다.
        // 데미지 텍스트와 같은 "Effect" 레이어를 써서 항상 위에 뜨게 한다.
        sr.sortingLayerName = "Effect";
        sr.sortingOrder = order;

        return sr;
    }

    // ── 갱신 ──────────────────────────────────────────────

    void OnChanged(float current, float max)
    {
        EnsureSegments(max);

        ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        // 피해든 회복이든 체력이 움직였으면 다시 띄운다
        remain = layout.ShowTime;

        // 올림이라 1이라도 남아 있으면 그 칸은 켜져 있다.
        // 켜진 칸 수가 곧 "앞으로 몇 번 더 버티나"로 읽힌다
        int lit = Mathf.CeilToInt(current / healthPerSegment);

        for (int i = 0; i < segments.Count; i++)
            segments[i].color = i < lit ? UiTheme.Current.warn : UiTheme.Current.line;

        appliedAlpha = -1f;
    }

    void LateUpdate()
    {
        if (follow == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = follow.position + (Vector3)layout.Offset;

        Fade();
    }

    /// <summary>남은 시간에 따라 흐려지고 사라진다. 위험한 체력에서는 그대로 떠 있는다.</summary>
    void Fade()
    {
        // 위험할 때 숨으면 5%인 줄도 모르고 계속 뛰어다니게 된다.
        // 0으로 두면 이 예외가 꺼진다
        bool pinned = layout.AlwaysShowBelow > 0f && ratio <= layout.AlwaysShowBelow;

        if (!pinned && remain > 0f) remain -= Time.deltaTime;

        float alpha = pinned       ? 1f
                    : remain <= 0f ? 0f
                    : Mathf.Clamp01(remain / FadeTime);

        Apply(alpha);
    }

    void Apply(float alpha)
    {
        if (Mathf.Approximately(appliedAlpha, alpha)) return;

        appliedAlpha = alpha;

        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] == null) continue;

            // 완전히 투명해지면 그리기 자체를 끈다 — 알파 0짜리도 드로우콜은 나간다
            segments[i].enabled = alpha > 0f;

            Color c = segments[i].color;
            segments[i].color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    void OnDestroy()
    {
        if (health != null) health.Changed -= OnChanged;
    }

    static Sprite WhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        // pixelsPerUnit 1 → 1×1 유닛 사각형. 스케일이 곧 월드 크기가 된다
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }
}
