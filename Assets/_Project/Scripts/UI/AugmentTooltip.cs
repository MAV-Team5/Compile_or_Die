using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 증강 아이콘에 마우스를 올렸을 때 뜨는 설명창. 화면에 하나만 있고 돌려 쓴다.
///
/// <b>게임을 멈추지 않는다.</b> 레벨업 카드와 달리 전투 중에 훑어보는 것이라
/// UIManager 화면 스택에도 들어가지 않는다 — 여기서 시간이 멈추면 툴팁이 곧 일시정지가 된다.
///
/// 조립은 GameHud 가 맡는다. 씬에 따로 만들 것이 없다.
/// </summary>
public class AugmentTooltip : MonoBehaviour
{
    public static AugmentTooltip Current { get; private set; }

    RectTransform canvasRect;
    RectTransform root;

    TMP_Text nameText;
    TMP_Text levelText;
    TMP_Text bodyText;

    UiTheme theme;

    /// <summary>지금 설명 중인 증강. 다른 슬롯으로 넘어가면 갈아끼운다.</summary>
    AugmentRunner shown;

    /// <summary>마지막으로 글을 만든 레벨. 레벨업하면 다시 만든다.</summary>
    int builtLevel = -1;

    // ── 조립 ──────────────────────────────────────────────

    /// <summary>GameHud 가 캔버스를 만들고 나서 한 번 부른다.</summary>
    public static AugmentTooltip Create(Canvas canvas, UiTheme theme, TMP_FontAsset font,
                                        float width = 560f)
    {
        Image border = UiFactory.CreateImage("AugmentTooltip", canvas.transform, theme.line);
        border.raycastTarget = false;   // 툴팁이 마우스를 가로채면 슬롯에서 바로 벗어난다

        AugmentTooltip tip = border.gameObject.AddComponent<AugmentTooltip>();

        tip.theme = theme;
        tip.canvasRect = (RectTransform)canvas.transform;
        tip.root = border.rectTransform;

        // 슬롯 왼쪽 위로 자라게 한다. HUD 가 화면 오른쪽 아래에 있기 때문
        tip.root.anchorMin = tip.root.anchorMax = new Vector2(0f, 0f);
        tip.root.pivot = new Vector2(1f, 0f);
        tip.root.sizeDelta = new Vector2(width, 0f);

        // 세로만 내용에 맞춰 늘어난다. 가로까지 맡기면 한 줄짜리 설명에서 창이 길쭉해진다
        var fit = border.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Frame(border.gameObject, padding: 3);

        Image inner = UiFactory.CreateImage("Inner", border.transform, theme.surface);
        inner.raycastTarget = false;

        var innerFit = inner.gameObject.AddComponent<ContentSizeFitter>();
        innerFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Frame(inner.gameObject, padding: 16, spacing: 4);

        tip.nameText = Line(inner.transform, font, 34f, theme.text, FontStyles.Bold);
        tip.levelText = Line(inner.transform, font, 24f, theme.dim, FontStyles.Normal);
        tip.bodyText = Line(inner.transform, font, 26f, theme.text, FontStyles.Normal);

        Current = tip;
        border.gameObject.SetActive(false);

        return tip;
    }

    /// <summary>세로로 쌓고 내용만큼만 차지하게 하는 껍데기.</summary>
    static void Frame(GameObject go, float padding, float spacing = 0f)
    {
        var group = go.AddComponent<VerticalLayoutGroup>();

        int p = Mathf.RoundToInt(padding);
        group.padding = new RectOffset(p, p, p, p);
        group.spacing = spacing;

        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;
    }

    static TMP_Text Line(Transform parent, TMP_FontAsset font, float size, Color color,
                         FontStyles style)
    {
        TMP_Text text = UiFactory.CreateText("Line", parent, font, size, color,
                                             TextAlignmentOptions.TopLeft);
        text.fontStyle = style;

        return text;
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    // ── 열고 닫기 ─────────────────────────────────────────

    /// <summary>슬롯이 마우스를 받으면 부른다.</summary>
    public static void Show(AugmentRunner runner, RectTransform slot)
    {
        if (Current == null || runner == null || runner.Instance == null) return;

        Current.Open(runner, slot);
    }

    /// <summary>
    /// 슬롯에서 마우스가 벗어나면 부른다.
    /// <b>지금 그 슬롯을 보여주는 중일 때만 닫는다</b> — 슬롯 사이를 빠르게 지나가면
    /// 새 슬롯의 Enter 가 먼저 오고 옛 슬롯의 Exit 가 뒤늦게 와서, 방금 연 것이 닫힌다.
    /// </summary>
    public static void Hide(AugmentRunner runner)
    {
        if (Current == null || Current.shown != runner) return;

        Current.shown = null;
        Current.root.gameObject.SetActive(false);
    }

    void Open(AugmentRunner runner, RectTransform slot)
    {
        shown = runner;
        builtLevel = -1;   // 다른 증강으로 갈아탔으니 글을 새로 만든다

        root.gameObject.SetActive(true);
        root.SetAsLastSibling();

        PlaceNear(slot);
        Rebuild();
    }

    void Update()
    {
        // 훑어보는 도중 레벨이 오르면 숫자가 바뀌어야 한다
        if (shown != null && shown.Instance != null && shown.Instance.Level != builtLevel)
            Rebuild();
    }

    void Rebuild()
    {
        AugmentInstance inst = shown.Instance;
        AugmentData data = inst.Data;

        builtLevel = inst.Level;

        nameText.text = string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName;
        nameText.color = theme.ColorOf(data.category);

        levelText.text = $"[ {AugmentCardView.CategoryLabel(data.category)} ]  Lv {inst.Level}"
                       + (inst.Level >= inst.MaxLevel ? "  (MAX)" : "");

        // 지금 레벨의 값만 보여준다. 다음 레벨과의 비교는 선택 카드가 할 일이다
        bodyText.text = AugmentText.Describe(data, inst.Level);
    }

    /// <summary>
    /// 슬롯의 왼쪽 위에 붙인다. 화면 밖으로 나가면 안쪽으로 밀어 넣는다 —
    /// 아이콘이 오른쪽 끝에 있어서 그냥 두면 창의 절반이 잘린다.
    /// </summary>
    void PlaceNear(RectTransform slot)
    {
        if (slot == null || canvasRect == null) return;

        // 같은 캔버스 안이므로 슬롯의 왼쪽 위 모서리를 캔버스 좌표로 바꾸면 된다
        Vector3[] corners = new Vector3[4];
        slot.GetWorldCorners(corners);

        Vector2 topLeft = canvasRect.InverseTransformPoint(corners[1]);

        // 캔버스 왼쪽 아래가 원점이 되도록 옮긴다 (앵커를 0,0 으로 잡아뒀다)
        Vector2 origin = new(canvasRect.rect.xMin, canvasRect.rect.yMin);
        Vector2 target = topLeft - origin + new Vector2(-12f, 12f);

        // 레이아웃이 아직 안 돌았으면 높이가 0이다. 지금 계산해서 정확한 크기로 밀어 넣는다
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);

        float w = root.rect.width, h = root.rect.height;

        // pivot 이 (1,0) 이라 x 는 오른쪽 끝, y 는 아래쪽 끝을 가리킨다
        target.x = Mathf.Clamp(target.x, w, canvasRect.rect.width);
        target.y = Mathf.Clamp(target.y, 0f, canvasRect.rect.height - h);

        root.anchoredPosition = target;
    }
}
