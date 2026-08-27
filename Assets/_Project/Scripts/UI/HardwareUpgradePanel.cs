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

    static readonly Color ArrowOn     = new(0.243f, 1f,     0.361f, 1f);
    static readonly Color ArrowOff    = new(0.150f, 0.175f, 0.205f, 1f);

    static readonly Color CardFace    = new(0.043f, 0.059f, 0.086f, 1f);
    static readonly Color CardEdge    = new(0.110f, 0.400f, 0.180f, 1f);
    static readonly Color CardEdgeMax = new(0.243f, 1f,     0.361f, 1f);
    static readonly Color CardEdgeOff = new(0.100f, 0.120f, 0.145f, 1f);

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
            Image pip = UiFactory.CreateImage($"Pip{i}", parent, PipLocked);
            pip.sprite = TriangleSprite();

            UiFactory.Place(pip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(start + step * i, -486f), new Vector2(pipSize, pipSize));

            pips[i] = pip;
        }

        return pips;
    }

    /// <summary>◀ 값 ▶ 한 줄. 화살표는 적용 레벨만 움직이고, 가운데가 실제 구매다.</summary>
    void BuildControls(Card card, RectTransform parent, HardwareTable.Entry entry)
    {
        const float arrowSize = 70f;
        const float buyWidth = 340f;
        const float rowY = -600f;

        card.DownArrow = UiFactory.CreateImage("DownArrow", parent, ArrowOff);
        card.DownArrow.sprite = TriangleSprite();
        card.DownArrow.raycastTarget = true;

        // 같은 삼각형을 뒤집어 왼쪽 화살표로 쓴다. 스프라이트를 하나 더 만들 이유가 없다
        card.DownArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);

        UiFactory.Place(card.DownArrow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(-(buyWidth * 0.5f + 50f), rowY), new Vector2(arrowSize, arrowSize));

        card.Down = card.DownArrow.gameObject.AddComponent<Button>();
        card.Down.targetGraphic = card.DownArrow;
        card.Down.colors = ArrowColors();
        card.Down.onClick.AddListener(() => Step(card, -1));

        card.UpArrow = UiFactory.CreateImage("UpArrow", parent, ArrowOff);
        card.UpArrow.sprite = TriangleSprite();
        card.UpArrow.raycastTarget = true;

        UiFactory.Place(card.UpArrow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(buyWidth * 0.5f + 50f, rowY), new Vector2(arrowSize, arrowSize));

        card.Up = card.UpArrow.gameObject.AddComponent<Button>();
        card.Up.targetGraphic = card.UpArrow;
        card.Up.colors = ArrowColors();
        card.Up.onClick.AddListener(() => Step(card, +1));

        // 가운데 구매 버튼. 글자만 있고 상자는 없다 — 결과 화면 버튼과 같은 양식
        Image hit = UiFactory.CreateImage("Buy", parent, Color.clear);
        hit.raycastTarget = true;

        UiFactory.Place(hit.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, rowY), new Vector2(buyWidth, 80f));

        card.BuyLabel = UiFactory.CreateText("Label", hit.rectTransform, Font, 40f,
                                             Color.white, TextAlignmentOptions.Center);
        UiFactory.Stretch(card.BuyLabel.rectTransform, Vector2.zero, Vector2.one);

        card.Buy = hit.gameObject.AddComponent<Button>();
        card.Buy.targetGraphic = hit;
        card.Buy.transition = Selectable.Transition.None;
        card.Buy.onClick.AddListener(() => Purchase(card));

        card.BuyNeon = hit.gameObject.AddComponent<NeonTextButton>();
        card.BuyNeon.Bind(card.BuyLabel, card.Buy);
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

        card.Edge.color = entry.Locked ? CardEdgeOff
                        : purchased >= max ? CardEdgeMax
                        : CardEdge;
    }

    // ── 삼각형 ────────────────────────────────────────────

    static Sprite triangle;

    /// <summary>
    /// 오른쪽을 향한 삼각형. 글꼴에 ▶ 가 없을 수 있어 직접 그린다 —
    /// 없는 글자를 쓰면 네모(두부)가 뜨는데, 그게 언제 터질지는 글꼴을 바꿔봐야 안다.
    /// </summary>
    static Sprite TriangleSprite()
    {
        if (triangle != null) return triangle;

        const int size = 64;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            // 왼쪽 변에서 오른쪽 꼭짓점으로 갈수록 세로 폭이 좁아진다
            float t = y / (float)(size - 1);
            float half = Mathf.Abs(t - 0.5f) * 2f;          // 0(가운데) ~ 1(위아래 끝)
            float edge = (1f - half) * size;                // 그 높이에서 칠할 가로 길이

            for (int x = 0; x < size; x++)
                pixels[y * size + x] = x <= edge
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        triangle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));

        return triangle;
    }
}
