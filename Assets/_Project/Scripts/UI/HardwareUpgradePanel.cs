using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하드웨어 업그레이드 상점. MainB 의 UpgradePanel 에 붙인다.
///
/// 아트가 아직 없으므로 GameHud · RunResultPanel 과 같은 방식으로 코드에서 조립한다.
/// 카드 프리팹이 생기면 <see cref="BuildCard"/> 만 갈아끼우면 된다.
///
/// <b>레벨이 두 가지라는 것이 이 화면의 전부다.</b>
/// 산 레벨(구매)은 비트를 치러야 오르고, 켠 레벨(적용)은 그 안에서 공짜로 오르내린다.
/// 삼각형 칸이 그 둘을 한눈에 보여준다 — 켠 칸 · 샀지만 안 켠 칸 · 아직 못 산 칸.
/// </summary>
public class HardwareUpgradePanel : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("＊ 필수 — 부품별 상승폭과 값이 적힌 표. 비우면 화면이 비어 있다.")]
    [SerializeField] HardwareTable table;

    [Tooltip("비우면 UiTheme 의 고정폭 글꼴을 쓴다.")]
    [SerializeField] TMP_FontAsset fontOverride;

    [Header("배치")]
    [Tooltip("한 줄에 몇 칸.")]
    [SerializeField] int columns = 5;

    [SerializeField] Vector2 cardSize = new(660f, 740f);
    [SerializeField] Vector2 cardGap = new(40f, 40f);

    // ── 색 ────────────────────────────────────────────────
    //
    // 네온은 밝기 차로 낸다. 켠 칸만 형광이고 나머지는 단계적으로 어두워져서,
    // "지금 몇 칸 켜져 있고 몇 칸 더 살 수 있는가"가 글자를 읽기 전에 보인다.

    static readonly Color PipOn       = new(0.243f, 1f,     0.361f, 1f);     // 켠 칸
    static readonly Color PipOwned    = new(0.122f, 0.478f, 0.180f, 1f);     // 샀지만 안 켠 칸
    static readonly Color PipLocked   = new(0.110f, 0.140f, 0.170f, 1f);     // 아직 못 산 칸

    // 화살표는 흰색이다. 초록으로 두면 위의 레벨 칸과 같은 색이라 무엇이 조작부인지 헷갈린다
    static readonly Color ArrowOn     = new(0.930f, 0.950f, 0.980f, 1f);
    static readonly Color ArrowOff    = new(0.200f, 0.225f, 0.255f, 1f);

    static readonly Color CardFace    = new(0.043f, 0.059f, 0.086f, 1f);
    static readonly Color CardEdge    = new(0.110f, 0.400f, 0.180f, 1f);
    static readonly Color CardEdgeMax = new(0.243f, 1f,     0.361f, 1f);
    static readonly Color CardEdgeOff = new(0.100f, 0.120f, 0.145f, 1f);

    // 구매 버튼. 글자만 있으면 눌리는 것인지 알 수 없어 테두리와 바닥을 준다
    static readonly Color BuyFace     = new(0.075f, 0.110f, 0.090f, 1f);
    static readonly Color BuyEdge     = new(0.243f, 0.700f, 0.361f, 1f);
    static readonly Color BuyFaceOff  = new(0.070f, 0.080f, 0.095f, 1f);
    static readonly Color BuyEdgeOff  = new(0.160f, 0.185f, 0.215f, 1f);

    static readonly Color TextName    = new(0.784f, 0.827f, 0.878f, 1f);
    static readonly Color TextEffect  = new(0.353f, 0.780f, 0.459f, 1f);
    static readonly Color TextDim     = new(0.353f, 0.400f, 0.459f, 1f);

    /// <summary>삼각형 칸의 최대 개수. 표에서 가장 긴 부품을 따른다.</summary>
    int pipCount;

    UiTheme theme;
    TMP_Text bitsText;
    bool built;

    readonly List<Card> cards = new();

    TMP_FontAsset Font => theme.FontOr(fontOverride);

    class Card
    {
        public HardwareKind Kind;
        public Image Edge;
        public TMP_Text Effect;
        public Image[] Pips;

        public Button Down;
        public Button Up;
        public Image DownArrow;
        public Image UpArrow;

        public Button Buy;
        public TMP_Text BuyLabel;
        public NeonTextButton BuyNeon;
        public Image BuyEdge;
        public Image BuyFace;
    }

    // 패널이 꺼진 채로 시작하므로 Awake 가 아니라 열릴 때 처음 돈다
    void OnEnable()
    {
        theme = UiTheme.Current;

        if (!built)
        {
            if (table == null)
            {
                Debug.LogError("[HardwareUpgradePanel] HardwareTable 이 없다. 화면을 못 만든다.", this);
                return;
            }

            Build();
            built = true;
        }

        RefreshAll();
    }

    // ── 조립 ──────────────────────────────────────────────

    void Build()
    {
        var root = (RectTransform)transform;

        pipCount = 0;
        for (int i = 0; i < table.Entries.Count; i++)
            pipCount = Mathf.Max(pipCount, table.Entries[i].MaxLevel);

        Header(root);
        Grid(root);
    }

    void Header(RectTransform root)
    {
        TMP_Text title = UiFactory.CreateText("Title", root, Font, 64f, TextName,
                                              TextAlignmentOptions.Left);

        UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(60f, -60f), new Vector2(1400f, 90f));

        title.text = "> HARDWARE";

        bitsText = UiFactory.CreateText("Bits", root, Font, 64f, PipOn,
                                        TextAlignmentOptions.Right);

        UiFactory.Place(bitsText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-60f, -60f), new Vector2(900f, 90f));

        ResetButton(root);
    }

    /// <summary>
    /// 산 것을 전부 물리고 비트를 돌려받는다. 보유 비트 왼쪽에 둔다.
    /// 실수로 눌러도 잃는 것이 없으므로 되묻지 않는다 — 값이 그대로 돌아오기 때문.
    /// </summary>
    void ResetButton(RectTransform root)
    {
        Image edge = UiFactory.CreateImage("ResetButton", root, BuyEdgeOff);
        edge.raycastTarget = true;

        UiFactory.Place(edge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                        new Vector2(-1000f, -105f), new Vector2(260f, 78f));

        Image face = UiFactory.CreateImage("Face", edge.rectTransform, BuyFaceOff);
        UiFactory.Stretch(face.rectTransform, Vector2.zero, Vector2.one,
                          new Vector2(2f, 2f), new Vector2(-2f, -2f));

        TMP_Text label = UiFactory.CreateText("Label", face.rectTransform, Font, 36f,
                                              Color.white, TextAlignmentOptions.Center);
        UiFactory.Stretch(label.rectTransform, Vector2.zero, Vector2.one);
        label.text = "초기화";

        var button = edge.gameObject.AddComponent<Button>();
        button.targetGraphic = edge;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(ResetAll);

        edge.gameObject.AddComponent<NeonTextButton>().Bind(label, button);
    }

    void ResetAll()
    {
        PlayerProgress.RefundAll(table);

        RefreshAll();
    }

    void Grid(RectTransform root)
    {
        RectTransform holder = UiFactory.CreateRect("Cards", root);

        // 위아래로 표제와 닫기 버튼 자리를 비워둔다
        UiFactory.Stretch(holder, Vector2.zero, Vector2.one,
                          new Vector2(0f, 260f), new Vector2(0f, -190f));

        var grid = holder.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = cardSize;
        grid.spacing = cardGap;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, columns);
        grid.childAlignment = TextAnchor.MiddleCenter;

        // 표에 적힌 순서 그대로 놓는다. 순서를 바꾸고 싶으면 표에서 줄을 옮기면 된다
        for (int i = 0; i < table.Entries.Count; i++)
            cards.Add(BuildCard(holder, table.Entries[i]));
    }

    Card BuildCard(RectTransform parent, HardwareTable.Entry entry)
    {
        var card = new Card { Kind = entry.kind };

        // 테두리 한 겹 + 안쪽 면. 테두리 색이 곧 그 부품의 상태다
        card.Edge = UiFactory.CreateImage($"Card_{entry.kind}", parent, CardEdge);

        Image face = UiFactory.CreateImage("Face", card.Edge.rectTransform, CardFace);
        UiFactory.Stretch(face.rectTransform, Vector2.zero, Vector2.one,
                          new Vector2(3f, 3f), new Vector2(-3f, -3f));

        RectTransform inner = face.rectTransform;

        // 아이콘 자리. 이미지가 없으면 부품 이름 첫 글자가 대신 들어간다
        Image slot = UiFactory.CreateImage("IconSlot", inner, new Color(0.078f, 0.102f, 0.141f, 1f));
        UiFactory.Place(slot.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -40f), new Vector2(260f, 260f));

        if (entry.icon != null)
        {
            Image icon = UiFactory.CreateImage("Icon", slot.rectTransform, Color.white);
            icon.sprite = entry.icon;
            icon.preserveAspect = true;
            UiFactory.Stretch(icon.rectTransform, Vector2.zero, Vector2.one,
                              new Vector2(20f, 20f), new Vector2(-20f, -20f));
        }
        else
        {
            TMP_Text mark = UiFactory.CreateText("IconFallback", slot.rectTransform, Font,
                                                 110f, TextDim, TextAlignmentOptions.Center);
            UiFactory.Stretch(mark.rectTransform, Vector2.zero, Vector2.one);
            mark.text = string.IsNullOrEmpty(entry.displayName) ? "?" : entry.displayName[..1];
        }

        TMP_Text name = UiFactory.CreateText("Name", inner, Font, 52f, TextName,
                                             TextAlignmentOptions.Center);
        UiFactory.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -320f), new Vector2(cardSize.x - 40f, 70f));
        name.text = entry.displayName;

        card.Effect = UiFactory.CreateText("Effect", inner, Font, 36f, TextEffect,
                                           TextAlignmentOptions.Center);
        UiFactory.Place(card.Effect.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -396f), new Vector2(cardSize.x - 40f, 60f));

        card.Pips = BuildPips(inner, entry);

        BuildControls(card, inner, entry);

        return card;
    }

    /// <summary>레벨 칸. 표에서 최대 레벨이 짧은 부품은 남는 칸을 아예 안 만든다.</summary>
    Image[] BuildPips(RectTransform parent, HardwareTable.Entry entry)
    {
        int count = Mathf.Max(entry.MaxLevel, 1);

        var pips = new Image[count];

        const float pipSize = 62f;
        const float pipGap = 14f;

        float step = pipSize + pipGap;
        float start = -(count - 1) * 0.5f * step;

        for (int i = 0; i < count; i++)
        {
            // 다이아몬드다. 화살표와 같은 삼각형이면 어느 쪽이 조작부인지 헷갈린다
            Image pip = UiFactory.CreateImage($"Pip{i}", parent, PipLocked);
            pip.sprite = DiamondSprite();

            UiFactory.Place(pip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(start + step * i, -486f), new Vector2(pipSize, pipSize));

            pips[i] = pip;
        }

        return pips;
    }

    /// <summary>◀ 값 ▶ 한 줄. 화살표는 적용 레벨만 움직이고, 가운데가 실제 구매다.</summary>
    void BuildControls(Card card, RectTransform parent, HardwareTable.Entry entry)
    {
        const float arrowSize = 64f;
        const float buyWidth = 320f;
        const float buyHeight = 88f;
        const float rowY = -600f;

        // 이 줄에 놓이는 것은 전부 가운데를 기준으로 맞춘다.
        // 카드의 다른 요소처럼 위쪽 기준으로 두면, 화살표를 180도 돌릴 때
        // 회전축이 위 모서리라 왼쪽 화살표만 그만큼 떠오른다
        float centerY = rowY - buyHeight * 0.5f;
        float arrowX = buyWidth * 0.5f + 56f;

        card.BuyEdge = UiFactory.CreateImage("Buy", parent, BuyEdge);
        card.BuyEdge.raycastTarget = true;

        UiFactory.Place(card.BuyEdge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, centerY), new Vector2(buyWidth, buyHeight));

        card.BuyFace = UiFactory.CreateImage("Face", card.BuyEdge.rectTransform, BuyFace);
        UiFactory.Stretch(card.BuyFace.rectTransform, Vector2.zero, Vector2.one,
                          new Vector2(2f, 2f), new Vector2(-2f, -2f));

        card.BuyLabel = UiFactory.CreateText("Label", card.BuyFace.rectTransform, Font, 38f,
                                             Color.white, TextAlignmentOptions.Center);
        UiFactory.Stretch(card.BuyLabel.rectTransform, Vector2.zero, Vector2.one);

        card.Buy = card.BuyEdge.gameObject.AddComponent<Button>();
        card.Buy.targetGraphic = card.BuyEdge;
        card.Buy.transition = Selectable.Transition.None;
        card.Buy.onClick.AddListener(() => Purchase(card));

        card.BuyNeon = card.BuyEdge.gameObject.AddComponent<NeonTextButton>();
        card.BuyNeon.Bind(card.BuyLabel, card.Buy);

        card.DownArrow = Arrow(parent, "DownArrow", new Vector2(-arrowX, centerY), arrowSize, flip: true);
        card.Down = card.DownArrow.gameObject.AddComponent<Button>();
        card.Down.targetGraphic = card.DownArrow;
        card.Down.colors = ArrowColors();
        card.Down.onClick.AddListener(() => Step(card, -1));

        card.UpArrow = Arrow(parent, "UpArrow", new Vector2(arrowX, centerY), arrowSize, flip: false);
        card.Up = card.UpArrow.gameObject.AddComponent<Button>();
        card.Up.targetGraphic = card.UpArrow;
        card.Up.colors = ArrowColors();
        card.Up.onClick.AddListener(() => Step(card, +1));
    }

    /// <summary>삼각형 화살표 하나. 왼쪽 것은 같은 스프라이트를 180도 돌려 쓴다.</summary>
    Image Arrow(RectTransform parent, string name, Vector2 center, float size, bool flip)
    {
        Image arrow = UiFactory.CreateImage(name, parent, ArrowOff);
        arrow.sprite = TriangleSprite();
        arrow.raycastTarget = true;

        // 축이 한가운데여야 돌려도 자리가 그대로다
        UiFactory.Place(arrow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                        center, new Vector2(size, size));

        if (flip) arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);

        return arrow;
    }

    static ColorBlock ArrowColors() => new()
    {
        normalColor      = Color.white,
        highlightedColor = new Color(1.6f, 1.6f, 1.6f, 1f),
        pressedColor     = new Color(0.7f, 0.7f, 0.7f, 1f),
        selectedColor    = Color.white,
        disabledColor    = Color.white,
        colorMultiplier  = 1f,
        fadeDuration     = 0.08f
    };

    // ── 조작 ──────────────────────────────────────────────

    /// <summary>적용 레벨만 움직인다. 값은 오가지 않는다.</summary>
    void Step(Card card, int delta)
    {
        PlayerProgress.SetActiveLevel(card.Kind,
                                      PlayerProgress.ActiveLevel(card.Kind) + delta);

        RefreshAll();
    }

    void Purchase(Card card)
    {
        // 산 단계는 곧바로 켜진다. 그래서 사고 나면 ▶ 는 다시 잠긴다 — 이미 최대라서
        if (!PlayerProgress.TryUpgrade(table, card.Kind)) return;

        RefreshAll();
    }

    // ── 갱신 ──────────────────────────────────────────────

    void RefreshAll()
    {
        if (!built) return;

        // 비트가 줄면 다른 카드도 못 사게 되므로 한 장만 고쳐서는 안 된다
        bitsText.text = $"{PlayerProgress.Bits:N0} bit";

        for (int i = 0; i < cards.Count; i++) Refresh(cards[i]);
    }

    void Refresh(Card card)
    {
        HardwareTable.Entry entry = table.Find(card.Kind);
        if (entry == null) return;

        int purchased = PlayerProgress.PurchasedLevel(card.Kind);
        int active = PlayerProgress.ActiveLevel(card.Kind);
        int max = entry.MaxLevel;

        card.Effect.text = entry.Locked ? "준비 중" : table.DescribeAt(card.Kind, active);
        card.Effect.color = entry.Locked ? TextDim : TextEffect;

        for (int i = 0; i < card.Pips.Length; i++)
        {
            // 켠 칸 → 샀지만 안 켠 칸 → 아직 못 산 칸 순으로 어두워진다
            card.Pips[i].color = i < active ? PipOn
                               : i < purchased ? PipOwned
                               : PipLocked;
        }

        card.Down.interactable = active > 0;
        card.Up.interactable = active < purchased;

        card.DownArrow.color = card.Down.interactable ? ArrowOn : ArrowOff;
        card.UpArrow.color = card.Up.interactable ? ArrowOn : ArrowOff;

        int cost = table.CostToUpgrade(card.Kind, purchased);

        if (entry.Locked)
        {
            card.BuyLabel.text = "잠김";
            card.Buy.interactable = false;
        }
        else if (cost < 0)
        {
            card.BuyLabel.text = "MAX";
            card.Buy.interactable = false;
        }
        else
        {
            card.BuyLabel.text = $"{cost:N0} bit";
            card.Buy.interactable = PlayerProgress.Bits >= cost;
        }

        // interactable 을 바꾼 뒤에 불러야 커서가 밖에 있어도 회색으로 바뀐다
        card.BuyNeon.Refresh();

        card.BuyEdge.color = card.Buy.interactable ? BuyEdge : BuyEdgeOff;
        card.BuyFace.color = card.Buy.interactable ? BuyFace : BuyFaceOff;

        card.Edge.color = entry.Locked ? CardEdgeOff
                        : purchased >= max ? CardEdgeMax
                        : CardEdge;
    }

    // ── 도형 ──────────────────────────────────────────────
    //
    // ▶ ◆ 같은 글자는 글꼴에 없으면 네모(두부)로 뜨는데, 그게 언제 터질지는
    // 글꼴을 바꿔봐야 안다. 그래서 직접 그려 쓴다.

    static Sprite triangle;
    static Sprite diamond;

    /// <summary>레벨 칸으로 쓰는 마름모.</summary>
    static Sprite DiamondSprite()
    {
        if (diamond != null) return diamond;

        diamond = Draw((x, y) => Mathf.Abs(x - 0.5f) + Mathf.Abs(y - 0.5f) <= 0.5f);

        return diamond;
    }

    /// <summary>주어진 판정으로 흰 도형을 그린다. 좌표는 0~1.</summary>
    static Sprite Draw(System.Func<float, float, bool> inside)
    {
        const int size = 64;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];

        var on = new Color32(255, 255, 255, 255);
        var off = new Color32(255, 255, 255, 0);

        for (int y = 0; y < size; y++)
        {
            float v = y / (float)(size - 1);

            for (int x = 0; x < size; x++)
                pixels[y * size + x] = inside(x / (float)(size - 1), v) ? on : off;
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>오른쪽을 향한 삼각형. 화살표로 쓴다.</summary>
    static Sprite TriangleSprite()
    {
        if (triangle != null) return triangle;

        // 위아래 끝으로 갈수록 오른쪽 꼭짓점 쪽이 좁아진다
        triangle = Draw((x, y) => x <= 1f - Mathf.Abs(y - 0.5f) * 2f);

        return triangle;
    }
}
