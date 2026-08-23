using UnityEngine;

/// <summary>
/// 플레이어 발밑을 따라다니는 체력바.
/// 스프라이트를 코드로 만들어 프리팹이 필요 없고, 플레이어 자식이 아니라
/// 피격 플래시에 같이 물들지 않는다.
/// </summary>
public class PlayerHealthBar : MonoBehaviour
{
    static Sprite whiteSprite;

    PlayerHealth health;
    Transform follow;
    Transform fillRoot;

    Vector2 offset;
    Vector2 size;

    public static PlayerHealthBar Create(PlayerHealth target, Vector2 offset, Vector2 size)
    {
        var go = new GameObject("PlayerHealthBar");
        var bar = go.AddComponent<PlayerHealthBar>();
        bar.Build(target, offset, size);
        return bar;
    }

    void Build(PlayerHealth target, Vector2 barOffset, Vector2 barSize)
    {
        health = target;
        follow = target.transform;
        offset = barOffset;
        size = barSize;

        Transform back = MakeQuad("Back", UiTheme.Current.line, 20);
        back.localScale = new Vector3(size.x, size.y, 1f);

        // 왼쪽 끝을 고정한 채 X 스케일만 줄여서 채움을 표현한다
        fillRoot = new GameObject("FillRoot").transform;
        fillRoot.SetParent(transform, false);
        fillRoot.localPosition = new Vector3(-size.x * 0.5f, 0f, 0f);

        Transform fill = MakeQuad("Fill", UiTheme.Current.warn, 21);
        fill.SetParent(fillRoot, false);
        fill.localPosition = new Vector3(0.5f, 0f, 0f);

        health.Changed += OnChanged;
        OnChanged(health.Current, health.Max);
    }

    Transform MakeQuad(string name, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSprite();
        sr.color = color;

        // 정렬 레이어는 순서가 아니라 레이어 자체가 우선이다.
        // "Default"(제일 아래)에 두면 Background/Enemy/Player 레이어에 전부 가려진다.
        // 데미지 텍스트와 같은 "Effect" 레이어를 써서 항상 위에 뜨게 한다.
        sr.sortingLayerName = "Effect";
        sr.sortingOrder = order;
        return go.transform;
    }

    void OnChanged(float current, float max)
    {
        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        fillRoot.localScale = new Vector3(size.x * ratio, size.y * 0.8f, 1f);
    }

    void LateUpdate()
    {
        if (follow == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = follow.position + (Vector3)offset;
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
