using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 연쇄가 한 단계 번질 때 앞 대상에서 다음 대상으로 번쩍이는 네온선.
/// <see cref="AugmentPipeline"/> 이 하위 파이프라인(깊이 1 이상)의 적중마다 부른다.
///
/// <b>왜 필요한가</b> — 연쇄는 계산상으로는 잘 일어나지만, 투사체가 휙 날아가고 끝이라
/// "누가 누구에게서 번졌는지" 가 안 보였다. 선이 남으면 연쇄의 모양(일직선·가지·그물)이 그대로 읽힌다.
///
/// 증강마다 색이 다르다 — Bash 파이프는 마젠타, C 포인터는 노랑, Tree 는 초록, Graph 는 청록, 탐색은 파랑.
/// 범위 공격(정렬 등)은 한 점에서 사방으로 선이 뻗어 지저분해지므로 그리지 않는다.
/// </summary>
public static class NeonFlash
{
    const float Life = 0.45f;
    const float CoreWidth = 0.14f;
    const float GlowWidth = 0.7f;
    const int MaxAlive = 64;          // 한꺼번에 떠 있을 수 있는 선 수. 넘치면 가장 오래된 것을 재활용

    static readonly Color Bash = new(1f, 0.25f, 0.9f);
    static readonly Color C = new(1f, 0.9f, 0.25f);
    static readonly Color Tree = new(0.45f, 1f, 0.4f);
    static readonly Color Graph = new(0.25f, 1f, 1f);
    static readonly Color Search = new(0.35f, 0.6f, 1f);

    static Material material;
    static readonly List<NeonFlashLine> pool = new();
    static int cursor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        pool.Clear();
        cursor = 0;
        material = null;
    }

    /// <summary>이 증강의 연쇄를 그릴지와 그 색. 그리지 않으면 false.</summary>
    public static bool ColorOf(AugmentData data, out Color color)
    {
        color = default;
        if (data == null) return false;

        string id = data.id != null ? data.id.ToUpperInvariant() : string.Empty;

        // 그릴 대상 — 연쇄가 보여야 하는 것만. 정렬·범위 공격은 선이 사방으로 뻗어 지저분하다
        bool draws = id.Contains("BASH") || id == "C" || id.StartsWith("C_") || id.Contains("TREE") || id.Contains("GRAPH")
                     || data.category == AugmentCategory.Search || data.category == AugmentCategory.DataStruct;
        if (!draws) return false;

        // 색은 아이콘과 같은 표에서 — 선만 보고도 어느 증강인지 안다
        if (AugmentPalette.TryGet(data, out color)) return true;

        if (id.Contains("BASH")) color = Bash;
        else if (id.StartsWith("C_")) color = C;
        else if (id.Contains("TREE")) color = Tree;
        else if (data.category == AugmentCategory.Search) color = Search;
        else color = Graph;
        return true;
    }

    /// <summary>앞 대상 → 다음 대상으로 한 번 번쩍인다. <paramref name="depth"/> 가 깊을수록 조금 더 밝다.</summary>
    public static void Hop(AugmentData data, Vector3 from, Vector3 to, int depth)
    {
        if (!ColorOf(data, out Color color)) return;
        if ((to - from).sqrMagnitude < 0.04f) return;

        Material mat = Material();
        if (mat == null) return;

        NeonFlashLine line = Take();
        line.Show(from, to, color, CoreWidth * (1f + 0.15f * Mathf.Min(depth, 4)), GlowWidth, Life, mat);
    }

    static Material Material()
    {
        if (material != null) return material;

        material = Resources.Load<Material>("NeonLine");
        if (material == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (s == null) s = Shader.Find("Sprites/Default");
            if (s != null) material = new Material(s);
        }

        return material;
    }

    static NeonFlashLine Take()
    {
        // 살아 있는(재생이 끝난) 것부터 재활용
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && !pool[i].Busy) return pool[i];

        if (pool.Count < MaxAlive)
        {
            var go = new GameObject("NeonFlash");
            Object.DontDestroyOnLoad(go);
            var line = go.AddComponent<NeonFlashLine>();
            pool.Add(line);
            return line;
        }

        // 다 쓰는 중이면 가장 오래된 것부터 돌려 쓴다
        cursor = (cursor + 1) % pool.Count;
        return pool[cursor];
    }
}
