using UnityEngine;

/// <summary>
/// 체력바 바로 밑의 대시 칸. 체력바와 같은 길이를 대시 횟수만큼 나눈 파란 칸이다.
/// <see cref="PlayerHealthBar"/> 가 만들 때 자식으로 붙인다.
///
/// <list type="bullet">
/// <item>쓸 수 있는 대시는 밝은 파랑으로 꽉 차 있다</item>
/// <item>다시 차오르는 칸은 왼쪽부터 흐린 파랑으로 채워진다 — 언제 다시 쓸 수 있는지 보인다</item>
/// <item>체력바가 보일 때, 그리고 대시를 써서 칸이 비어 있는 동안 보인다. 다 차면 잠깐 뒤 사라진다</item>
/// </list>
/// </summary>
public class PlayerDashBar : MonoBehaviour
{
    const float HeightRatio = 0.75f;   // 체력 칸 높이 대비
    const float Linger = 0.8f;         // 다 찬 뒤 더 보이는 시간
    const float FadeTime = 0.4f;

    static readonly Color Full = new(0.25f, 0.8f, 1f);
    static readonly Color Charging = new(0.25f, 0.65f, 1f, 0.55f);

    static Sprite whiteSprite;

    PlayerHealthBar bar;
    Player player;

    SpriteRenderer[] backs = new SpriteRenderer[0];
    SpriteRenderer[] fills = new SpriteRenderer[0];
    float linger;

    public static void Attach(PlayerHealthBar healthBar, Player owner)
    {
        if (healthBar == null || owner == null) return;

        var go = new GameObject("PlayerDashBar");
        go.transform.SetParent(healthBar.transform, false);

        var dash = go.AddComponent<PlayerDashBar>();
        dash.bar = healthBar;
        dash.player = owner;
    }

    void LateUpdate()
    {
        if (bar == null || player == null)
        {
            Destroy(gameObject);
            return;
        }

        int max = player.DashMaxCharges;
        int left = player.DashCharges;
        Ensure(max);

        PlayerHealthBar.Layout l = bar.BarLayout;
        float width = bar.Width > 0f ? bar.Width : l.SegmentSize.x * 10f;
        float gap = l.SegmentGap;
        float h = l.SegmentSize.y * HeightRatio;
        float y = -(l.SegmentSize.y * 0.5f + gap * 1.5f + h * 0.5f);
        float cell = (width - gap * (max - 1)) / max;

        // 보이는 정도 — 체력바가 보이거나, 칸이 비어 있거나, 막 다 찼을 때
        bool notFull = left < max;
        if (notFull) linger = Linger + FadeTime;
        else linger -= Time.deltaTime;

        float own = notFull ? 1f : Mathf.Clamp01(linger / FadeTime);
        float alpha = Mathf.Max(bar.Alpha, own);
        bool show = alpha > 0f;

        Color empty = UiTheme.Current != null ? UiTheme.Current.line : new Color(1f, 1f, 1f, 0.2f);

        for (int i = 0; i < max; i++)
        {
            float x = -width * 0.5f + cell * 0.5f + i * (cell + gap);

            backs[i].enabled = show;
            backs[i].transform.localPosition = new Vector3(x, y, 0f);
            backs[i].transform.localScale = new Vector3(cell, h, 1f);
            backs[i].color = WithAlpha(empty, empty.a * alpha);

            // 이 칸이 얼마나 찼나 — 남은 횟수 안이면 가득, 바로 다음 칸이면 충전 중, 그 뒤는 빈칸
            float fill = i < left ? 1f : i == left ? player.DashRechargeProgress : 0f;
            Color c = i < left ? Full : Charging;

            fills[i].enabled = show && fill > 0f;
            float fw = cell * fill;
            fills[i].transform.localPosition = new Vector3(x - cell * 0.5f + fw * 0.5f, y, 0f);
            fills[i].transform.localScale = new Vector3(fw, h, 1f);
            fills[i].color = WithAlpha(c, c.a * alpha);
        }
    }

    static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);

    void Ensure(int count)
    {
        if (backs.Length == count) return;

        foreach (SpriteRenderer s in backs) if (s != null) Destroy(s.gameObject);
        foreach (SpriteRenderer s in fills) if (s != null) Destroy(s.gameObject);

        backs = new SpriteRenderer[count];
        fills = new SpriteRenderer[count];

        for (int i = 0; i < count; i++)
        {
            backs[i] = Quad($"DashBack{i}", 21);
            fills[i] = Quad($"DashFill{i}", 22);
        }
    }

    SpriteRenderer Quad(string name, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSprite();

        // 체력바와 같은 레이어 — 몬스터·플레이어 위에 뜬다
        sr.sortingLayerName = "Effect";
        sr.sortingOrder = order;
        return sr;
    }

    static Sprite WhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        // 1×1 유닛 사각형. 스케일이 곧 월드 크기
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }
}
