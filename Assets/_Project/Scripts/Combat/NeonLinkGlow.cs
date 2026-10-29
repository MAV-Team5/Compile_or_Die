using UnityEngine;

/// <summary>
/// 간선(Graph·Tree)에 덧씌우는 네온 빛. 원래 선 바깥으로 넓고 흐린 빛을 한 겹 더 그린다.
/// <see cref="LinkHolder"/> 가 간선을 만들 때 붙인다 — 간선 프리팹을 고치지 않아도 모든 간선이 빛난다.
///
/// 원래 선의 모양(LinkPulse 가 꿀렁이게 만든 것)을 매 프레임 그대로 베낀다. 그래서 LinkPulse 뒤에 돈다.
/// 피해가 전이되며 원래 선이 굵어지면 빛도 같이 밝아져 "지금 이 간선으로 흘렀다" 가 보인다.
/// </summary>
[DefaultExecutionOrder(200)]
public class NeonLinkGlow : MonoBehaviour
{
    const float GlowScale = 4.5f;   // 원래 선 굵기 대비 빛 굵기
    const float RestAlpha = 0.3f;
    const float PulseAlpha = 0.8f;

    LineRenderer main;
    LineRenderer glow;
    float baseWidth;
    Vector3[] buf = new Vector3[2];

    void Awake()
    {
        main = GetComponent<LineRenderer>();
        if (main == null) main = GetComponentInChildren<LineRenderer>();
        if (main == null) { enabled = false; return; }

        baseWidth = Mathf.Max(0.0001f, main.widthMultiplier);

        var go = new GameObject("Neon Glow");
        go.transform.SetParent(transform, false);
        glow = go.AddComponent<LineRenderer>();
        glow.useWorldSpace = true;
        glow.numCapVertices = 2;
        glow.numCornerVertices = 2;

        Material mat = Resources.Load<Material>("NeonLine");
        glow.sharedMaterial = mat != null ? mat : main.sharedMaterial;
        glow.sortingLayerID = main.sortingLayerID;
        glow.sortingOrder = main.sortingOrder - 1;
    }

    void LateUpdate()
    {
        if (main == null || glow == null) return;

        glow.enabled = main.enabled && main.gameObject.activeInHierarchy;
        if (!glow.enabled) return;

        int n = main.positionCount;
        if (buf.Length != n) buf = new Vector3[n];
        main.GetPositions(buf);
        glow.positionCount = n;
        glow.SetPositions(buf);

        // 원래 선이 굵어진 만큼 = 지금 피해가 흐르는 중
        float pulse = Mathf.Clamp01((main.widthMultiplier / baseWidth - 1f) / 1.2f);

        Color c = main.startColor;
        c.a = Mathf.Lerp(RestAlpha, PulseAlpha, pulse);
        glow.startColor = glow.endColor = c;
        glow.widthMultiplier = main.widthMultiplier * GlowScale * (1f + 0.5f * pulse);
    }
}
