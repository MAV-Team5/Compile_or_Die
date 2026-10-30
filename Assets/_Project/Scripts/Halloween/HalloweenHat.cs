using UnityEngine;

/// <summary>
/// 플레이어 머리 위 마녀 모자. 할로윈 배경으로 바뀌는 순간 <see cref="HalloweenIntro"/> 가 씌운다.
///
/// 플레이어 그림이 애니메이션마다 크기·위치가 달라도 맞도록, 매 프레임 플레이어 스프라이트의
/// 화면상 경계(bounds)를 보고 정수리에 얹는다. 좌우 반전도 따라간다.
/// 그림은 Resources/HalloweenHat.png (Tools/witch_hat.py 로 만든 것).
/// </summary>
public class HalloweenHat : MonoBehaviour
{
    const float WidthRatio = 0.95f;   // 모자 폭 = 플레이어 폭 × 이 값
    const float SinkRatio = 0.30f;    // 모자 높이의 이만큼 머리에 파묻는다
    const float TiltDeg = 8f;         // 살짝 기울여 귀엽게
    const float BobAmp = 0.04f;       // 걸을 때 통통

    SpriteRenderer body;
    SpriteRenderer hat;
    Vector3 lastPos;
    float bob;

    static Sprite sprite;

    /// <summary>플레이어에게 모자를 씌운다. 이미 썼으면 그대로.</summary>
    public static void Put(GameObject player)
    {
        if (player == null || player.GetComponentInChildren<HalloweenHat>() != null) return;

        if (sprite == null)
        {
            var tex = Resources.Load<Texture2D>("HalloweenHat");
            if (tex == null) { Debug.LogWarning("[HalloweenHat] Resources/HalloweenHat.png 없음"); return; }
            tex.filterMode = FilterMode.Bilinear;
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), tex.width);
        }

        var go = new GameObject("Witch Hat");
        go.transform.SetParent(player.transform, false);
        go.AddComponent<HalloweenHat>().Init(player);
    }

    void Init(GameObject player)
    {
        // 플레이어 몸 그림 — 1) 플레이어 오브젝트 자체의 스프라이트 2) Animator 가 붙은 스프라이트
        // 3) 그래도 없으면 가장 작은 것(범위 원·그림자 같은 큰 장식을 몸으로 착각하지 않게)
        body = player.GetComponent<SpriteRenderer>();
        if (body == null)
            foreach (SpriteRenderer sr in player.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.transform != transform && sr.GetComponent<Animator>() != null) { body = sr; break; }
        if (body == null)
        {
            float best = float.MaxValue;
            foreach (SpriteRenderer sr in player.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.transform == transform || sr.sprite == null) continue;
                float area = sr.bounds.size.x * sr.bounds.size.y;
                if (area > 0.01f && area < best) { best = area; body = sr; }
            }
        }

        var names = new System.Text.StringBuilder();
        foreach (SpriteRenderer sr in player.GetComponentsInChildren<SpriteRenderer>(true))
            names.Append($" {sr.name}({sr.bounds.size.x:0.00}x{sr.bounds.size.y:0.00} L={sr.sortingLayerName}/{sr.sortingOrder})");
        Debug.Log($"[HalloweenHat] 씌움 — 몸={(body != null ? body.name : "없음")} | 후보:{names}");

        hat = gameObject.AddComponent<SpriteRenderer>();
        hat.sprite = sprite;
        if (body != null)
        {
            hat.sortingLayerID = body.sortingLayerID;
            hat.sortingOrder = body.sortingOrder + 5;
        }
        lastPos = player.transform.position;
    }

    void LateUpdate()
    {
        if (body == null || hat == null) return;

        hat.enabled = body.enabled && body.gameObject.activeInHierarchy;
        if (!hat.enabled) return;

        Bounds b = body.bounds;
        float w = Mathf.Max(0.1f, b.size.x * WidthRatio);
        float size = w;                       // 그림이 정사각형이라 폭 = 높이

        // 움직이면 통통 튄다
        Vector3 p = transform.parent.position;
        float speed = (p - lastPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPos = p;
        bob += Time.deltaTime * (speed > 0.1f ? 14f : 3f);
        float dy = Mathf.Abs(Mathf.Sin(bob)) * BobAmp * (speed > 0.1f ? 1f : 0.3f) * size;

        bool flip = body.flipX;
        hat.flipX = flip;

        // 부모 스케일이 있어도 월드 크기로 맞춘다
        Vector3 ls = transform.parent.lossyScale;
        float sx = Mathf.Abs(ls.x) > 1e-4f ? Mathf.Abs(ls.x) : 1f;
        float sy = Mathf.Abs(ls.y) > 1e-4f ? Mathf.Abs(ls.y) : 1f;
        transform.localScale = new Vector3(size / sx, size / sy, 1f);

        transform.position = new Vector3(b.center.x, b.max.y - size * SinkRatio + dy, body.transform.position.z);
        transform.rotation = Quaternion.Euler(0f, 0f, flip ? TiltDeg : -TiltDeg);
    }
}
