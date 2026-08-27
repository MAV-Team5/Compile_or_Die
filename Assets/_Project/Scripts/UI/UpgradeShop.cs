using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하드웨어 업그레이드 상점. 재화를 쓰는 유일한 화면이다.
///
/// <b>붙일 곳</b> — MainB(로비) 씬의 업그레이드 패널 오브젝트.
/// 패널 밑에 <c>ShopBody</c> 를 만들어 그 안에만 그리므로, 패널에 이미 있는 것은 안 건드린다.
///
/// 아트가 없어 코드로 조립한다. <see cref="UiTheme"/> 를 쓰므로 색·글꼴은 테마를 고치면 따라온다.
///
/// <b>구매는 <see cref="PlayerProgress.TryUpgrade"/> 가 다 한다</b> —
/// 값 확인·차감·레벨 올리기·저장까지. 여기서는 누르고 다시 그리는 일만 한다.
/// </summary>
public class UpgradeShop : MonoBehaviour
{
    [Header("자료")]
    [Tooltip("부품 표. 비우면 씬의 HardwareLoader 에서 찾아 쓴다.")]
    [SerializeField] HardwareTable table;

    [Header("모양")]
    [Tooltip("비우면 UiTheme 의 고정폭 글꼴을 쓴다.")]
    [SerializeField] TMP_FontAsset fontOverride;

    [Tooltip("부품 한 줄의 높이.")]
    [SerializeField] float rowHeight = 56f;

    [Tooltip("패널 안쪽 여백.")]
    [SerializeField] float margin = 24f;

    UiTheme theme;
    RectTransform body;
    TMP_Text bitsLabel;

    readonly List<Row> rows = new();

    /// <summary>줄 하나가 들고 있는 것. 다시 그릴 때 통째로 만들지 않으려고 붙잡아 둔다.</summary>
    class Row
    {
        public HardwareKind Kind;
        public Button Button;
        public Image Back;
        public TMP_Text Name;
        public TMP_Text Effect;
        public TMP_Text Level;
        public TMP_Text Cost;
    }

    TMP_FontAsset font => theme.FontOr(fontOverride);

    // 패널은 보통 꺼진 채로 시작하므로 Start 가 안 돈다. 열릴 때마다 여기로 들어온다
    void OnEnable()
    {
        if (body == null) Build();

        Refresh();
    }

    // ── 조립 ──────────────────────────────────────────────

    void Build()
    {
        theme = UiTheme.Current;

        if (table == null) FindTable();

        body = UiFactory.CreateRect("ShopBody", transform);
        UiFactory.Stretch(body, Vector2.zero, Vector2.one,
                          new Vector2(margin, margin), new Vector2(-margin, -margin));

        BuildHeader();

        if (table == null)
        {
            Warn("부품 표가 없다.\nHardwareLoader 에 HardwareTable 을 물리거나\n"
               + "이 컴포넌트의 Table 칸을 채울 것.");
            return;
        }

        if (table.Entries.Count == 0)
        {
            Warn("부품 표가 비어 있다.\nCoD → 하드웨어 표 만들기 로 기본값을 채울 수 있다.");
            return;
        }

        BuildRows();
    }

    void BuildHeader()
    {
        TMP_Text title = UiFactory.CreateText("Title", body, font, 30f, theme.text,
                                              TextAlignmentOptions.Left);
        UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        Vector2.zero, new Vector2(400f, 40f));
        title.text = "HARDWARE";

        bitsLabel = UiFactory.CreateText("Bits", body, font, 26f, theme.accent,
                                         TextAlignmentOptions.Right);
        UiFactory.Place(bitsLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        Vector2.zero, new Vector2(400f, 40f));

        // 가로로 펼치는 선이라 offset 만 쓴다. Place 와 섞으면 어느 쪽이 이기는지 모호해진다
        Image rule = UiFactory.CreateImage("Rule", body, theme.line);
        UiFactory.Stretch(rule.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                          new Vector2(0f, -50f), new Vector2(0f, -48f));
    }

    void BuildRows()
    {
        float gap = theme.Space(1);
        float top = -theme.Space(9);   // 제목 + 구분선 아래

        for (int i = 0; i < table.Entries.Count; i++)
        {
            HardwareTable.Entry entry = table.Entries[i];

            rows.Add(MakeRow(entry, top - (rowHeight + gap) * i));
        }
    }

    Row MakeRow(HardwareTable.Entry entry, float y)
    {
        var row = new Row { Kind = entry.kind };

        row.Back = UiFactory.CreateImage($"Row_{entry.kind}", body, theme.surfaceDim);
        row.Back.raycastTarget = true;   // 버튼이 클릭을 받으려면 켜야 한다

        UiFactory.Stretch(row.Back.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                          new Vector2(0f, y - rowHeight), new Vector2(0f, y));

        row.Button = row.Back.gameObject.AddComponent<Button>();
        row.Button.targetGraphic = row.Back;
        row.Button.colors = theme.ButtonColors(theme.surfaceDim);

        HardwareKind kind = entry.kind;
        row.Button.onClick.AddListener(() => Buy(kind));

        float pad = theme.Space(1.5f);

        row.Name = Label(row.Back.transform, 24f, theme.text, TextAlignmentOptions.Left,
                         new Vector2(0f, 1f), new Vector2(pad, -pad), new Vector2(320f, 26f));

        row.Effect = Label(row.Back.transform, 18f, theme.dim, TextAlignmentOptions.Left,
                           new Vector2(0f, 0f), new Vector2(pad, pad), new Vector2(420f, 22f));

        row.Level = Label(row.Back.transform, 20f, theme.dim, TextAlignmentOptions.Right,
                          new Vector2(1f, 1f), new Vector2(-pad, -pad), new Vector2(200f, 26f));

        row.Cost = Label(row.Back.transform, 22f, theme.accent, TextAlignmentOptions.Right,
                         new Vector2(1f, 0f), new Vector2(-pad, pad), new Vector2(200f, 24f));

        row.Name.text = string.IsNullOrEmpty(entry.displayName)
            ? entry.kind.ToString() : entry.displayName;

        return row;
    }

    TMP_Text Label(Transform parent, float size, Color color, TextAlignmentOptions align,
                   Vector2 corner, Vector2 offset, Vector2 box)
    {
        TMP_Text text = UiFactory.CreateText("Label", parent, font, size, color, align);

        UiFactory.Place(text.rectTransform, corner, corner, offset, box);

        return text;
    }

    // ── 다시 그리기 ───────────────────────────────────────

    void Refresh()
    {
        if (bitsLabel != null) bitsLabel.text = $"{PlayerProgress.Bits:N0} bit";

        if (table == null) return;

        for (int i = 0; i < rows.Count; i++) RefreshRow(rows[i]);
    }

    void RefreshRow(Row row)
    {
        HardwareTable.Entry entry = table.Find(row.Kind);
        if (entry == null) return;

        int level = PlayerProgress.LevelOf(row.Kind);
        int cost = table.CostToUpgrade(row.Kind, level);

        row.Level.text = $"Lv {level} / {entry.maxLevel}";

        // 다음 레벨에서 무엇이 되는지를 보여준다. 지금 값이 아니라 사고 난 뒤의 값이다
        row.Effect.text = entry.DescribeAt(level + 1);

        bool locked = entry.maxLevel <= 0;
        bool maxed = cost < 0 && !locked;
        bool poor = cost >= 0 && PlayerProgress.Bits < cost;

        if (locked)
        {
            row.Cost.text = "LOCKED";
            row.Cost.color = theme.dim;
            row.Effect.text = entry.description;
        }
        else if (maxed)
        {
            row.Cost.text = "MAX";
            row.Cost.color = theme.good;
            row.Effect.text = entry.DescribeAt(level);
        }
        else
        {
            row.Cost.text = $"{cost:N0} bit";
            row.Cost.color = poor ? theme.warn : theme.accent;
        }

        row.Button.interactable = !locked && !maxed && !poor;
    }

    void Buy(HardwareKind kind)
    {
        // 실패해도 다시 그린다 — 값이 바뀌었는데 화면만 옛 상태로 남는 편이 더 나쁘다
        PlayerProgress.TryUpgrade(table, kind);

        Refresh();
    }

    // ── 잔가지 ────────────────────────────────────────────

    void FindTable()
    {
        HardwareLoader loader = FindAnyObjectByType<HardwareLoader>();

        if (loader != null) table = loader.Table;
    }

    void Warn(string message)
    {
        TMP_Text text = UiFactory.CreateText("Warn", body, font, 22f, theme.warn,
                                             TextAlignmentOptions.Center);
        UiFactory.Stretch(text.rectTransform, Vector2.zero, Vector2.one);

        text.text = message;

        Debug.LogWarning($"[UpgradeShop] {message.Replace('\n', ' ')}", this);
    }
}
