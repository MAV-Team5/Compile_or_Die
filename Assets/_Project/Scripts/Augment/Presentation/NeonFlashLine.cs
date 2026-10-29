using UnityEngine;

/// <summary>
/// <see cref="NeonFlash"/> 의 선 하나. 밝은 심 + 넓게 번지는 빛 두 겹이다.
/// 처음엔 끝에서 끝까지 쭉 뻗어 나가고(0.08초), 그다음 서서히 사라진다 — "이쪽에서 저쪽으로 번졌다" 가 읽힌다.
/// </summary>
public class NeonFlashLine : MonoBehaviour
{
    const float GrowTime = 0.08f;
    const int Points = 8;
    const float Jitter = 0.12f;   // 살짝 지글거리게 — 직선 자만 대면 그냥 UI 선처럼 보인다

    LineRenderer core;
    LineRenderer glow;
    readonly Vector3[] buf = new Vector3[Points];
    readonly float[] offsets = new float[Points];

    Vector3 from, to;
    Color color;
    float coreWidth, glowWidth;
    float life, age;

    public bool Busy => age < life;

    public void Show(Vector3 a, Vector3 b, Color c, float coreW, float glowW, float seconds, Material mat)
    {
        if (core == null)
        {
            glow = Make("Glow", mat, 30);
            core = Make("Core", mat, 31);
        }

        from = a;
        to = b;
        color = c;
        coreWidth = coreW;
        glowWidth = glowW;
        life = seconds;
        age = 0f;

        for (int i = 0; i < Points; i++)
            offsets[i] = (i == 0 || i == Points - 1) ? 0f : Random.Range(-Jitter, Jitter);

        core.enabled = glow.enabled = true;
        Draw();
    }

    LineRenderer Make(string name, Material mat, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = Points;
        lr.numCapVertices = 3;
        lr.sharedMaterial = mat;
        lr.sortingLayerName = "Effect";
        lr.sortingOrder = order;
        lr.textureMode = LineTextureMode.Stretch;
        return lr;
    }

    void Update()
    {
        if (core == null || !Busy)
        {
            if (core != null && core.enabled) core.enabled = glow.enabled = false;
            return;
        }

        age += Time.deltaTime;
        Draw();
    }

    void Draw()
    {
        float grow = Mathf.Clamp01(age / GrowTime);
        float fade = 1f - Mathf.Clamp01((age - GrowTime) / Mathf.Max(0.01f, life - GrowTime));

        Vector3 end = Vector3.Lerp(from, to, grow);
        Vector3 d = end - from;
        Vector3 side = new Vector3(-d.y, d.x, 0f).normalized;

        for (int i = 0; i < Points; i++)
        {
            float t = i / (float)(Points - 1);
            buf[i] = from + d * t + side * offsets[i];
        }

        core.SetPositions(buf);
        glow.SetPositions(buf);

        // 심은 하얗게 뜨게 색을 살짝 밝힌다
        Color hot = Color.Lerp(color, Color.white, 0.45f);
        hot.a = fade;
        Color halo = color;
        halo.a = 0.4f * fade;

        core.startColor = core.endColor = hot;
        glow.startColor = glow.endColor = halo;

        core.startWidth = core.endWidth = coreWidth * (0.6f + 0.4f * fade);
        glow.startWidth = glow.endWidth = glowWidth * (0.7f + 0.3f * fade);
    }
}
