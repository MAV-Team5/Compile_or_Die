using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 증강 이펙트(칼날·투사체·장판·빔·파티클)를 그 증강의 아이콘 색(<see cref="AugmentPalette"/>)으로 칠한다.
///
/// <b>어떻게 붙나</b> — 파이프라인이 전달·효과를 돌리는 동안 <see cref="Scope"/> 로 "지금 누구 차례인지" 켜 둔다.
/// 그 사이 <see cref="PooledSpawner.Spawn"/> 으로 나온 오브젝트는 자동으로 칠해진다.
/// 프리팹 하나를 여러 증강이 같이 쓰므로(MeleeSwing 등) 나올 때마다 다시 칠한다.
/// 증강 밖에서 같은 프리팹이 나오면 원래 재질로 되돌린다.
/// </summary>
public static class AugmentTint
{
    static readonly Stack<AugmentData> stack = new();
    static Material recolor;
    static readonly int NeonColor = Shader.PropertyToID("_NeonColor");
    static readonly List<Renderer> renderers = new();

    public static AugmentData Current => stack.Count > 0 ? stack.Peek() : null;

    public readonly struct ScopeHandle : System.IDisposable
    {
        readonly bool pushed;
        public ScopeHandle(bool p) { pushed = p; }
        public void Dispose() { if (pushed && stack.Count > 0) stack.Pop(); }
    }

    /// <summary>using (AugmentTint.Scope(data)) { ... } — 안에서 나온 이펙트가 이 증강 색이 된다.</summary>
    public static ScopeHandle Scope(AugmentData data)
    {
        if (data == null) return new ScopeHandle(false);
        stack.Push(data);
        return new ScopeHandle(true);
    }

    static Material Recolor
    {
        get
        {
            if (recolor == null)
            {
                Shader s = Shader.Find("CoD/NeonRecolor");
                if (s != null) recolor = new Material(s) { name = "Neon Recolor (runtime)" };
            }
            return recolor;
        }
    }

    /// <summary>방금 나온 오브젝트를 지금 차례인 증강 색으로. 차례가 없으면 원래대로.</summary>
    public static void OnSpawn(GameObject go)
    {
        if (go == null) return;
        if (go.TryGetComponent(out KeepOriginalColors _)) return;

        AugmentData data = Current;
        if (data != null && AugmentPalette.TryGet(data, out Color c)) Apply(go, Neon(c));
        else if (go.TryGetComponent(out AugmentTintState state)) state.Restore();
    }

    public static void Apply(GameObject go, Color color)
    {
        Material mat = Recolor;
        if (mat == null) return;

        if (!go.TryGetComponent(out AugmentTintState state)) state = go.AddComponent<AugmentTintState>();
        state.Capture();

        go.GetComponentsInChildren(true, renderers);

        foreach (Renderer r in renderers)
        {
            if (r is SpriteRenderer)
                r.sharedMaterial = ColorMat(null, color);
            else if (r is ParticleSystemRenderer)
                // 파티클은 텍스처를 재질에서 읽는다. 원래 재질의 그림을 옮긴 복사본을 쓴다
                r.sharedMaterial = ColorMat(state.OriginalOf(r), color);
            else if (r is MeshRenderer && r.TryGetComponent(out SweepFan _))
                // 부채꼴 잔상 — 텍스처는 SweepFan 이 블록으로 넣는다
                r.sharedMaterial = ColorMat(null, color);
        }

        renderers.Clear();
    }

    static readonly Dictionary<(Material, Color), Material> mats = new();

    /// <summary>색마다 재질 하나. 원본이 있으면 그 텍스처를 옮겨 담는다(파티클).</summary>
    static Material ColorMat(Material original, Color color)
    {
        var key = (original, color);
        if (mats.TryGetValue(key, out Material m) && m != null) return m;

        m = new Material(Recolor) { name = $"Neon Recolor #{ColorUtility.ToHtmlStringRGB(color)}" };
        m.SetColor(NeonColor, color);

        if (original != null)
        {
            Texture tex = original.HasProperty("_MainTex") ? original.GetTexture("_MainTex")
                        : original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : null;
            if (tex != null) m.SetTexture("_MainTex", tex);
        }

        mats[key] = m;
        return m;
    }

    /// <summary>네온용으로 채도·밝기를 끝까지 올린다. 파스텔 표 색도 선명한 형광색이 된다.</summary>
    public static Color Neon(Color c)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        return Color.HSVToRGB(h, Mathf.Max(s, 0.9f), 1f);
    }

    static Color WithAlpha(Color c, float a) { c.a = a; return c; }
}

/// <summary>칠하기 전 재질을 기억했다가 증강 밖에서 다시 쓰일 때 되돌린다.</summary>
public class AugmentTintState : MonoBehaviour
{
    readonly List<(Renderer r, Material m)> originals = new();
    bool captured;

    public void Capture()
    {
        if (captured) return;
        captured = true;
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            if (r is SpriteRenderer || r is ParticleSystemRenderer || (r is MeshRenderer && r.TryGetComponent(out SweepFan _)))
                originals.Add((r, r.sharedMaterial));
    }

    public Material OriginalOf(Renderer r)
    {
        foreach (var (rr, m) in originals) if (rr == r) return m;
        return r.sharedMaterial;
    }

    public void Restore()
    {
        if (!captured) return;
        foreach (var (r, m) in originals)
        {
            if (r == null) continue;
            r.sharedMaterial = m;
        }
    }
}
