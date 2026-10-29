using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 첫 증강 선택 전에 띄우는 안내. 두 장이다.
///
/// <list type="number">
/// <item><b>증강 레이블</b> — 핵심 셋(공격·탐색·연쇄)을 크게, 나머지 레이블은 작은 칩으로</item>
/// <item><b>대쉬</b> — Shift, 최대 2번, 칸당 2초 충전, HP바 아래 파란 칸</item>
/// </list>
/// 칩 색은 카드에 붙는 레이블 색(<see cref="AugmentTagStyle"/>)과 같다.
/// 진행 버튼은 장마다 잠깐 뒤에 켜진다 — 열리자마자 습관적으로 누른 Space 에 넘어가지 않게.
/// </summary>
public class LabelTutorial : MonoBehaviour
{
    const float ButtonDelay = 0.9f;
    const float Stagger = 0.06f;

    static readonly (AugmentTag tag, string label, string text)[] Core =
    {
        (AugmentTag.Attack, "공격", "적에게 직접 피해를 준다.\n먼저 공격으로 버틴다."),
        (AugmentTag.Mark,   "탐색", "적을 찾아 표식을 남긴다.\n표식 찍힌 적은 더 아프다."),
        (AugmentTag.Chain,  "연쇄", "맞은 적에서 다음 적으로\n번져 나간다."),
    };

    static readonly AugmentTag[] Minor =
    {
        AugmentTag.Single, AugmentTag.Multi, AugmentTag.Projectile, AugmentTag.Pierce,
        AugmentTag.Area, AugmentTag.Explosion, AugmentTag.Melee, AugmentTag.Rapid,
        AugmentTag.Homing, AugmentTag.Transfer, AugmentTag.DamageOverTime, AugmentTag.Summon,
        AugmentTag.Knockback, AugmentTag.Slow, AugmentTag.HighDamage, AugmentTag.Control,
        AugmentTag.Synergy, AugmentTag.Support, AugmentTag.SearchLinked,
    };

    static readonly Color DashBlue = new(0.35f, 0.75f, 1f);

    bool done;
    int page;
    Button proceed;
    TMP_Text proceedText;
    readonly List<CanvasGroup>[] pageItems = { new(), new() };
    readonly GameObject[] pages = new GameObject[2];
    float openedAt;
    TMP_FontAsset font;

    /// <summary>안내를 띄우고 마지막 장의 진행 버튼을 누를 때까지 기다린다. 시간이 멈춰 있어도 돈다.</summary>
    public static IEnumerator Run()
    {
        var go = new GameObject("Label Tutorial");
        var tut = go.AddComponent<LabelTutorial>();
        tut.Build();

        while (!tut.done) yield return null;

        Destroy(go);
    }

    // ── 공용 ──────────────────────────────────────────

    CanvasGroup Item(int p, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(pages[p].transform, false);
        UiFactory.Place((RectTransform)go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        var g = go.GetComponent<CanvasGroup>();
        g.alpha = 0f;
        pageItems[p].Add(g);
        return g;
    }

    TMP_Text Text(Transform parent, string name, float size, Color color, string text,
                  Vector2 pos, Vector2 box, TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
    {
        TMP_Text t = UiFactory.CreateText(name, parent, font, size, color, align);
        t.text = text;
        if (bold) t.fontStyle = FontStyles.Bold;
        UiFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, box);
        return t;
    }

    static void Glow(Graphic g, Color c, float alpha, float dist)
    {
        var o = g.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(c.r, c.g, c.b, alpha);
        o.effectDistance = new Vector2(dist, -dist);
    }

    // ── 만들기 ────────────────────────────────────────

    void Build()
    {
        openedAt = Time.unscaledTime;
        font = UiTheme.Current != null ? UiTheme.Current.mono : null;

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        Image back = UiFactory.CreateImage("Back", transform, new Color(0.02f, 0.02f, 0.05f, 0.93f));
        UiFactory.Stretch(back.rectTransform, Vector2.zero, Vector2.one);

        for (int p = 0; p < 2; p++)
        {
            pages[p] = UiFactory.CreateRect(p == 0 ? "Page Labels" : "Page Dash", transform).gameObject;
            UiFactory.Stretch((RectTransform)pages[p].transform, Vector2.zero, Vector2.one);
        }

        BuildLabelsPage();
        BuildDashPage();
        BuildButton();

        ShowPage(0);
    }

    void BuildLabelsPage()
    {
        Transform root = pages[0].transform;

        Text(root, "Page", 24f, new Color(0.5f, 0.53f, 0.6f), "1 / 2", new Vector2(0f, 490f), new Vector2(400f, 34f));
        Text(root, "Title", 64f, Color.white, "증강 레이블", new Vector2(0f, 430f), new Vector2(1600f, 80f), bold: true);
        Text(root, "Sub", 30f, new Color(0.75f, 0.78f, 0.85f),
             "카드 이름 아래 색 칩이 그 증강이 하는 일이다 — 핵심은 셋", new Vector2(0f, 368f), new Vector2(1600f, 44f));

        // 핵심 셋: 큰 카드
        const float W = 500f, H = 300f, Gap = 70f;
        float x0 = -(Core.Length * W + (Core.Length - 1) * Gap) * 0.5f + W * 0.5f;
        for (int i = 0; i < Core.Length; i++)
        {
            var (tag, label, text) = Core[i];
            Color c = AugmentTagStyle.ColorOf(tag);
            float x = x0 + i * (W + Gap);
            CanvasGroup g = Item(0, $"Core {label}", new Vector2(x, 150f), new Vector2(W, H));

            Image frame = UiFactory.CreateImage("Frame", g.transform, new Color(c.r * 0.16f, c.g * 0.16f, c.b * 0.16f, 0.95f));
            UiFactory.Stretch(frame.rectTransform, Vector2.zero, Vector2.one);
            Glow(frame, c, 0.9f, 4f);

            Image chip = UiFactory.CreateImage("Chip", g.transform, c);
            UiFactory.Place(chip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(0f, -30f), new Vector2(300f, 104f));
            Glow(chip, c, 0.45f, 7f);

            TMP_Text name = UiFactory.CreateText("Label", chip.transform, font, 66f, new Color(0.05f, 0.05f, 0.08f), TextAlignmentOptions.Center);
            name.fontStyle = FontStyles.Bold;
            name.text = label;
            UiFactory.Stretch(name.rectTransform, Vector2.zero, Vector2.one);

            TMP_Text desc = UiFactory.CreateText("Desc", g.transform, font, 32f, Color.Lerp(c, Color.white, 0.6f), TextAlignmentOptions.Top);
            desc.text = text;
            UiFactory.Place(desc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(0f, -164f), new Vector2(W - 30f, 120f));

            if (i < Core.Length - 1)
                Text(root, "Arrow", 54f, new Color(1f, 0.84f, 0.3f), "▶",
                     new Vector2(x + (W + Gap) * 0.5f, 150f), new Vector2(70f, 70f));
        }

        Text(root, "Flow", 36f, new Color(1f, 0.84f, 0.3f),
             "공격으로 버티고  ▶  탐색으로 표식을 남기고  ▶  연쇄로 번지게",
             new Vector2(0f, -60f), new Vector2(1700f, 56f), bold: true);

        // 나머지 레이블: 작은 칩 두 줄
        Text(root, "MinorHead", 26f, new Color(0.55f, 0.58f, 0.66f),
             "그 밖의 레이블 — 공격 방식과 부가 효과", new Vector2(0f, -170f), new Vector2(1600f, 36f));

        const float cw = 150f, ch = 46f, cg = 14f;
        const int perRow = 10;
        for (int i = 0; i < Minor.Length; i++)
        {
            int row = i / perRow, col = i % perRow;
            int inRow = Mathf.Min(perRow, Minor.Length - row * perRow);
            float rx0 = -(inRow * cw + (inRow - 1) * cg) * 0.5f + cw * 0.5f;
            Color c = AugmentTagStyle.ColorOf(Minor[i]);

            CanvasGroup g = Item(0, $"Minor {Minor[i]}", new Vector2(rx0 + col * (cw + cg), -228f - row * (ch + cg)), new Vector2(cw, ch));
            Image chip = UiFactory.CreateImage("Chip", g.transform, c);
            UiFactory.Stretch(chip.rectTransform, Vector2.zero, Vector2.one);

            TMP_Text n = UiFactory.CreateText("Label", chip.transform, font, 26f, new Color(0.05f, 0.05f, 0.08f), TextAlignmentOptions.Center);
            n.fontStyle = FontStyles.Bold;
            n.text = AugmentTagStyle.Label(Minor[i]);
            n.enableAutoSizing = true;
            n.fontSizeMax = 26f;
            n.fontSizeMin = 16f;
            UiFactory.Stretch(n.rectTransform, Vector2.zero, Vector2.one);
        }
    }

    void BuildDashPage()
    {
        Transform root = pages[1].transform;

        Text(root, "Page", 24f, new Color(0.5f, 0.53f, 0.6f), "2 / 2", new Vector2(0f, 490f), new Vector2(400f, 34f));
        Text(root, "Title", 64f, DashBlue, "대쉬", new Vector2(0f, 430f), new Vector2(1600f, 80f), bold: true);
        Text(root, "Sub", 30f, new Color(0.75f, 0.78f, 0.85f),
             "몰리면 빠져나가라 — 이동 방향으로 순간 돌진한다", new Vector2(0f, 368f), new Vector2(1600f, 44f));

        // 키
        {
            CanvasGroup g = Item(1, "Key", new Vector2(0f, 200f), new Vector2(420f, 150f));
            Image key = UiFactory.CreateImage("Key", g.transform, DashBlue);
            UiFactory.Stretch(key.rectTransform, Vector2.zero, Vector2.one);
            Glow(key, DashBlue, 0.45f, 8f);
            TMP_Text k = UiFactory.CreateText("Text", key.transform, font, 84f, new Color(0.03f, 0.05f, 0.1f), TextAlignmentOptions.Center);
            k.fontStyle = FontStyles.Bold;
            k.text = "SHIFT";
            UiFactory.Stretch(k.rectTransform, Vector2.zero, Vector2.one);
        }

        // 세 가지 규칙 — 칸마다 따로
        (string head, string body)[] rules =
        {
            ("최대 2번", "연달아 두 번까지\n쓸 수 있다"),
            ("칸당 2초", "쓴 칸은 2초마다\n하나씩 다시 찬다"),
            ("파란 칸", "HP바 아래 파란 칸이\n남은 횟수다"),
        };

        const float W = 440f, H = 230f, Gap = 60f;
        float x0 = -(rules.Length * W + (rules.Length - 1) * Gap) * 0.5f + W * 0.5f;
        for (int i = 0; i < rules.Length; i++)
        {
            CanvasGroup g = Item(1, $"Rule {i}", new Vector2(x0 + i * (W + Gap), -70f), new Vector2(W, H));

            Image box = UiFactory.CreateImage("Box", g.transform, new Color(0.04f, 0.1f, 0.18f, 0.95f));
            UiFactory.Stretch(box.rectTransform, Vector2.zero, Vector2.one);
            Glow(box, DashBlue, 0.85f, 3f);

            TMP_Text h = UiFactory.CreateText("Head", g.transform, font, 46f, DashBlue, TextAlignmentOptions.Center);
            h.fontStyle = FontStyles.Bold;
            h.text = rules[i].head;
            UiFactory.Place(h.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(W - 20f, 64f));

            TMP_Text b = UiFactory.CreateText("Body", g.transform, font, 30f, Color.white, TextAlignmentOptions.Top);
            b.text = rules[i].body;
            UiFactory.Place(b.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(W - 30f, 110f));

            // 마지막 칸엔 HP바 + 파란 칸 두 개 그림
            if (i == 2)
            {
                Image hp = UiFactory.CreateImage("Hp", g.transform, new Color(0.9f, 0.2f, 0.25f));
                UiFactory.Place(hp.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(200f, 14f));
                for (int c = 0; c < 2; c++)
                {
                    Image cell = UiFactory.CreateImage("Cell", g.transform, DashBlue);
                    UiFactory.Place(cell.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                                    new Vector2(-51f + c * 102f, 24f), new Vector2(96f, 12f));
                }
            }
        }
    }

    void BuildButton()
    {
        Image btnImg = UiFactory.CreateImage("Proceed", transform, new Color(1f, 0.82f, 0.25f));
        UiFactory.Place(btnImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -420f), new Vector2(380f, 88f));
        btnImg.raycastTarget = true;

        proceed = btnImg.gameObject.AddComponent<Button>();
        proceed.targetGraphic = btnImg;
        ColorBlock cb = proceed.colors;
        cb.disabledColor = new Color(0.35f, 0.35f, 0.38f, 1f);
        cb.highlightedColor = Color.white;
        proceed.colors = cb;
        proceed.interactable = false;
        proceed.onClick.AddListener(Next);

        proceedText = UiFactory.CreateText("Text", btnImg.transform, font, 44f, new Color(0.05f, 0.05f, 0.08f), TextAlignmentOptions.Center);
        proceedText.fontStyle = FontStyles.Bold;
        UiFactory.Stretch(proceedText.rectTransform, Vector2.zero, Vector2.one);
    }

    // ── 진행 ──────────────────────────────────────────

    void ShowPage(int p)
    {
        page = p;
        openedAt = Time.unscaledTime;
        for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == p);
        foreach (CanvasGroup g in pageItems[p]) g.alpha = 0f;

        proceed.interactable = false;
        proceedText.text = p < pages.Length - 1 ? "다음  ▶" : "시작  ▶";
    }

    void Update()
    {
        float t = Time.unscaledTime - openedAt;

        List<CanvasGroup> items = pageItems[page];
        for (int i = 0; i < items.Count; i++)
            items[i].alpha = Mathf.Clamp01((t - i * Stagger) / 0.25f);

        if (!proceed.interactable && t >= ButtonDelay)
        {
            proceed.interactable = true;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(proceed.gameObject);
        }

        if (!proceed.interactable) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            Next();
    }

    void Next()
    {
        if (done || !proceed.interactable) return;
        UiSound.Play(UiCue.CardPick);

        if (page < pages.Length - 1) ShowPage(page + 1);
        else done = true;
    }
}
