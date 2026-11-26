using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 증강마다 고유한 네온 색. 아이콘(Tools/neon_icons.py)과 같은 표다 — 이펙트 색이 아이콘 색을 따라야
/// "저 선은 Bash 거구나" 가 한눈에 읽힌다.
///
/// 계열(카테고리)마다 색 범위를 정하고 그 안에서 증강마다 다르다.
/// 탐색=파랑 · 자료구조=청록/초록 · 언어=마젠타/노랑/주황/라임 · 최적화=빨강/주황 · 코드=호박 · 정렬=보라
/// </summary>
public static class AugmentPalette
{
    static readonly Dictionary<string, Color> table = new()
    {
        { "LINEAR_SEARCH", Hex("#4FC3FF") },
        { "BFS", Hex("#3D7BFF") },
        { "DFS", Hex("#8A6BFF") },
        { "GRAPH", Hex("#22F0FF") },
        { "TREE", Hex("#4DFF7A") },
        { "BASH", Hex("#FF3DDC") },
        { "C_LAN", Hex("#FFE14D") },
        { "JAVA", Hex("#FF8A3D") },
        { "PYTHON", Hex("#B6FF3D") },
        { "BASH:KILL", Hex("#FF3D7F") },
        { "BRUTE_FORCE", Hex("#FF4B3D") },
        { "BRUTE_FORCE_OPTIAML", Hex("#FF7A5C") },
        { "FIREWALL", Hex("#FF8F1F") },
        { "ITERATION", Hex("#FFB13D") },
        { "ITERATION_WHILE", Hex("#FFCF6B") },
        { "BUBBLE_SORT", Hex("#C08CFF") },
        { "QUICK_SORT", Hex("#9B5BFF") },
        { "ITEM_0XBEEF", Hex("#FF6B8F") },
        { "ITEM_0XCAFE", Hex("#7DF9FF") },
        { "ITEM_GC", Hex("#7DFFB2") },
    };

    static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.white;

    /// <summary>이 증강의 고유색. 표에 없으면 진화 원본의 색, 그것도 없으면 false.</summary>
    public static bool TryGet(AugmentData data, out Color color)
    {
        color = Color.white;
        if (data == null) return false;

        if (!string.IsNullOrEmpty(data.id) && table.TryGetValue(data.id.ToUpperInvariant(), out color)) return true;
        if (data.rootAugment != null && data.rootAugment != data) return TryGet(data.rootAugment, out color);
        return false;
    }

    /// <summary>아이콘 파일 이름에 쓰는 키. 콜론은 파일 이름에 못 쓰므로 밑줄로.</summary>
    public static string IconKey(AugmentData data)
        => data == null || string.IsNullOrEmpty(data.id) ? null : data.id.ToUpperInvariant().Replace(':', '_');
}
