using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 코드로 UI를 조립할 때 쓰는 공용 생성기.
/// 아직 아트·프리팹이 없는 HUD를 프리팹 없이 띄우기 위한 것.
/// </summary>
public static class UiFactory
{
    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");

        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    /// <summary>단색 사각형. 스프라이트 없이 색만 칠한다.</summary>
    public static Image CreateImage(string name, Transform parent, Color color)
    {
        Image image = CreateRect(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font,
                                      float size, Color color, TextAlignmentOptions align)
    {
        TextMeshProUGUI text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();

        if (font != null) text.font = font;

        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>부모의 anchorMin~Max 영역으로 펼친다. offset 은 안쪽 여백.</summary>
    public static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
                               Vector2 offsetMin = default, Vector2 offsetMax = default)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    /// <summary>
    /// 고정폭 글꼴에서 열을 맞추려고 공백으로 채운다.
    ///
    /// <b>한글은 두 칸을 차지한다.</b> 글자 수로만 세면 "체력" 과 "이동속도" 의
    /// 값 시작 위치가 어긋난다 — 고정폭 글꼴이어도 그렇다.
    /// </summary>
    public static string Pad(string text, int width, bool right = false)
    {
        text ??= "";

        int used = 0;
        for (int i = 0; i < text.Length; i++) used += IsWide(text[i]) ? 2 : 1;

        int fill = Mathf.Max(0, width - used);
        string space = new(' ', fill);

        return right ? space + text : text + space;
    }

    /// <summary>한글·한자·가나처럼 두 칸을 쓰는 글자인가.</summary>
    static bool IsWide(char c)
        => (c >= 0x1100 && c <= 0x115F)     // 한글 자모
        || (c >= 0x2E80 && c <= 0xA4CF)     // 한자·부수·가나
        || (c >= 0xAC00 && c <= 0xD7A3)     // 한글 음절
        || (c >= 0xF900 && c <= 0xFAFF)     // 호환 한자
        || (c >= 0xFF00 && c <= 0xFF60);    // 전각 기호

    /// <summary>한 점에 고정하고 크기를 직접 준다.</summary>
    public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot,
                             Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
