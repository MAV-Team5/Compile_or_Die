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
