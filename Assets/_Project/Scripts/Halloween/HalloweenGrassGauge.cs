using UnityEngine;

/// <summary>
/// 블루스크린(좌우반전) 게이지를 캐릭터 몸에 풀이 자라는 것으로 보여준다 — 치이카와 스테이지 전용.
///
/// 화면 위쪽의 가는 바는 싸우는 중에 눈이 안 간다. 대신 캐릭터 발밑에서 초록 풀잎이 솟아
/// <see cref="BlueScreenGauge.Progress"/> 만큼 몸을 타고 오르고, 다 차면 몸을 완전히 덮는다.
/// 눈은 늘 캐릭터를 보고 있으니 게이지가 얼마나 찼는지 자연스럽게 읽힌다.
///
/// <list type="bullet">
/// <item>풀잎 하나하나 높이가 조금씩 달라 들쭉날쭉 자란다. 바람에 흔들리듯 살랑인다.</item>
/// <item>80% 를 넘으면 풀끝이 노랗게 깜빡인다 — 곧 뒤집힌다는 경고.</item>
/// <item>게이지가 0 이 되거나 사거리 안에 블루스크린 몬스터가 없으면 풀이 스르륵 줄어든다.</item>
/// </list>
///
/// 풀잎 그림은 코드로 만든다(가늘어지는 잎 하나). 원래 게이지 바는 그대로 둔다.
/// <see cref="HalloweenBossShow"/> 가 할로윈 스테이지 시작 때 하나 만든다.
/// </summary>
public class HalloweenGrassGauge : MonoBehaviour
{
    const int BladeCount = 18;
    const float Cover = 1.08f;          // 다 찼을 때 몸 높이의 몇 배까지 — 머리끝까지 확실히 덮게
    const float GrowSpeed = 2.5f;       // 화면 값이 실제 진행도를 따라가는 속도(초당)
    const float WarnFrom = 0.8f;        // 이 이상이면 경고 깜빡임
    const float SwayDeg = 7f;

    static readonly Color DarkGreen = new(0.16f, 0.62f, 0.22f, 1f);
    static readonly Color LightGreen = new(0.45f, 0.88f, 0.35f, 1f);
    static readonly Color WarnTip = new(1f, 0.92f, 0.3f, 1f);

    static Sprite bladeSprite;

    Transform player;
    SpriteRenderer body;
    Transform root;
    SpriteRenderer[] blades;
    float[] heightMul, xPos, phase;
    float shown;

    void Update()
    {
        if (player == null && !FindPlayer()) return;

        BlueScreenGauge gauge = BlueScreenGauge.Instance;
        float target = 0f;
        if (gauge != null && gauge.AnyoneInRange && !gauge.IsLocked) target = gauge.Progress;

        shown = Mathf.MoveTowards(shown, target, GrowSpeed * Time.unscaledDeltaTime);

        bool visible = shown > 0.005f;
        if (root.gameObject.activeSelf != visible) root.gameObject.SetActive(visible);
        if (!visible) return;

        Place();
    }

    bool FindPlayer()
    {
        if (GameManager.instance == null || GameManager.instance.player == null) return false;

        player = GameManager.instance.player.transform;
        body = FindBody(player);
        Build();
        return true;
    }

    /// <summary>몸 그림 찾기 — 애니메이터가 붙은 쪽이 몸이다. 없으면 플레이어 자신.</summary>
    static SpriteRenderer FindBody(Transform p)
    {
        foreach (Animator a in p.GetComponentsInChildren<Animator>(true))
        {
            var sr = a.GetComponent<SpriteRenderer>();
            if (sr != null) return sr;
        }
        return p.GetComponent<SpriteRenderer>();
    }

    void Build()
    {
        root = new GameObject("Grass Gauge").transform;
        root.SetParent(player, false);

        // 플레이어 그림들보다 앞에 그린다
        int order = 0;
        string layer = "Player";
        foreach (SpriteRenderer sr in player.GetComponentsInChildren<SpriteRenderer>(true))
            if (sr.sortingLayerName == layer) order = Mathf.Max(order, sr.sortingOrder);

        Sprite sprite = BladeSprite();
        blades = new SpriteRenderer[BladeCount];
        heightMul = new float[BladeCount];
        xPos = new float[BladeCount];
        phase = new float[BladeCount];

        var rng = new System.Random(7);
        for (int i = 0; i < BladeCount; i++)
        {
            var go = new GameObject("Blade");
            go.transform.SetParent(root, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = layer;
            sr.sortingOrder = order + 1 + (i % 2);   // 앞뒤로 섞여 겹친다
            sr.color = Color.Lerp(DarkGreen, LightGreen, (float)rng.NextDouble());
            blades[i] = sr;

            // 가운데 잎이 조금 더 높고 가장자리는 낮다 — 덤불 모양
            float u = (i + 0.5f) / BladeCount;
            float bell = 1f - Mathf.Abs(u - 0.5f) * 0.6f;
            heightMul[i] = bell * (0.8f + 0.35f * (float)rng.NextDouble());
            xPos[i] = (u - 0.5f) + ((float)rng.NextDouble() - 0.5f) * 0.06f;
            phase[i] = (float)rng.NextDouble() * 6.28f;
        }

        root.gameObject.SetActive(false);
    }

    /// <summary>몸 크기에 맞춰 풀잎을 세우고 흔든다.</summary>
    void Place()
    {
        Bounds b = body != null ? body.bounds : new Bounds(player.position, Vector3.one);
        Vector3 lossy = player.lossyScale;
        float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : Mathf.Abs(lossy.x);
        float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : Mathf.Abs(lossy.y);

        // 루트는 몸 아래 가운데. 월드 크기를 부모 배율로 나눠 로컬로 바꾼다
        root.position = new Vector3(b.center.x, b.min.y, player.position.z);
        root.rotation = Quaternion.identity;

        float width = b.size.x * 1.15f;
        float height = b.size.y * Cover;
        float bladeW = width / BladeCount * 2.2f;   // 잎끼리 겹치게 넉넉히

        float warn = shown >= WarnFrom ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 14f) : 0f;
        float t = Time.time;

        for (int i = 0; i < BladeCount; i++)
        {
            Transform tr = blades[i].transform;

            // 잎마다 자라는 시점을 살짝 엇갈려 들쭉날쭉 올라오게
            float grow = Mathf.Clamp01(shown * 1.15f - (1f - heightMul[i]) * 0.15f);
            float h = height * heightMul[i] * grow;

            tr.localPosition = new Vector3(xPos[i] * width / sx, 0f, 0f);
            tr.localScale = new Vector3(bladeW / 0.25f / sx, h / sy, 1f);   // 잎 그림은 0.25 x 1 유닛

            float sway = Mathf.Sin(t * 2.2f + phase[i]) * SwayDeg * (0.4f + shown);
            tr.localRotation = Quaternion.Euler(0f, 0f, sway);

            Color baseCol = Color.Lerp(DarkGreen, LightGreen, (phase[i] / 6.28f));
            blades[i].color = Color.Lerp(baseCol, WarnTip, warn * 0.6f);
        }
    }

    /// <summary>풀잎 하나 — 아래가 넓고 위로 갈수록 뾰족해지는 흰 잎(색은 SpriteRenderer 가 입힌다).</summary>
    static Sprite BladeSprite()
    {
        if (bladeSprite != null) return bladeSprite;

        const int w = 16, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = y / (float)(h - 1);
            // 살짝 휘는 잎 — 위로 갈수록 오른쪽으로 기운다
            float center = (w - 1) * 0.5f + v * v * 2.5f;
            float half = (w * 0.5f - 0.5f) * Mathf.Pow(1f - v, 0.8f);

            for (int x = 0; x < w; x++)
            {
                float d = Mathf.Abs(x - center);
                float a = Mathf.Clamp01(half - d + 0.5f);
                // 가운데 잎맥이 조금 밝다
                float shade = 0.82f + 0.18f * Mathf.Clamp01(1f - d / Mathf.Max(half, 0.01f));
                px[y * w + x] = new Color(shade, shade, shade, a);
            }
        }

        tex.SetPixels(px);
        tex.Apply();

        bladeSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 64f);
        return bladeSprite;
    }
}
