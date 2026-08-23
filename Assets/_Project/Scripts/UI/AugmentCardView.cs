using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 증강 카드 한 장. 조립하고, 내용을 채우고, 리롤 버튼 상태를 보인다.
///
/// <b>카드는 아무것도 결정하지 않는다.</b> 무엇을 보여줄지·누를 수 있는지는 전부
/// 밖에서 받는다 — 그래야 카드 아트가 나왔을 때 이 파일만 프리팹 방식으로 갈아끼우면 된다.
///
/// 코드로 조립하지만 MonoBehaviour 라서, 나중에 프리팹을 만들고 슬롯만 물려도 그대로 돈다.
/// </summary>
public class AugmentCardView : MonoBehaviour
{
    /// <summary>카드 한 장을 그리는 데 필요한 치수. 부모가 정한다.</summary>
    public struct Layout
    {
        public Vector2 CardSize;
        public float ButtonSize;
        public float IconSize;
    }

    /// <summary>리롤 버튼이 왜 잠겼는지. 글자를 고르는 데만 쓴다.</summary>
    public enum RerollState
    {
        Ready,

        /// <summary>이번 레벨업에 이 슬롯을 이미 썼다.</summary>
        SlotUsed,

        /// <summary>보유량이 0이다.</summary>
        Empty,

        /// <summary>바꿔 넣을 다른 증강이 없다.</summary>
        NoAlternative
    }

    public AugmentData Data { get; private set; }

    // ── 조각들 ────────────────────────────────────────────
    //
    // [SerializeField] 로 열어둔 이유 — 이 카드를 프리팹으로 만들면 코드가 조립하지 않으므로
    // 인스펙터에서 손으로 물려야 한다. 코드로 만드는 경로에서는 Create 가 그대로 채운다.

    [Header("버튼")]
    [SerializeField] Button choose;
    [SerializeField] Button reroll;

    [Header("본문")]
    [SerializeField] Image icon;
    [SerializeField] TMP_Text iconFallback;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text categoryText;
    [SerializeField] TMP_Text levelText;
    [SerializeField] TMP_Text descriptionText;

    [Header("리롤 버튼 속")]
    [SerializeField] Image rerollIconImage;
    [SerializeField] TMP_Text rerollLabel;

    public Button Choose => choose;
    public Button Reroll => reroll;

    RectTransform root;

    UiTheme theme;
    Layout layout;
    float spinTime;

    /// <summary>이 카드가 실제로 차지하는 크기. 프리팹에서 키웠으면 그 값이 나온다.</summary>
    public Vector2 Size => root != null ? root.rect.size : layout.CardSize;

    // ── 조립 ──────────────────────────────────────────────

    /// <summary>빈 오브젝트를 만들고 카드를 조립해 돌려준다.</summary>
    public static AugmentCardView Create(Transform parent, string name, UiTheme theme,
                                         TMP_FontAsset font, Layout layout,
                                         Sprite rerollIcon, float spinTime)
    {
        // 테두리가 곧 버튼 판정면이다 — 마우스를 올리면 테두리만 강조색이 된다
        Image border = UiFactory.CreateImage(name, parent, theme.line);
        border.raycastTarget = true;

        AugmentCardView view = border.gameObject.AddComponent<AugmentCardView>();

        view.theme = theme;
        view.layout = layout;
        view.spinTime = spinTime;
        view.root = (RectTransform)border.transform;

        // 오른 값에 강조색을 입힌다. 문구 해석기는 테마를 모르므로 여기서 알려준다
        AugmentText.ChangeColor = "#" + ColorUtility.ToHtmlStringRGB(theme.accent);

        UiFactory.Place(view.root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, layout.CardSize);

        view.choose = border.gameObject.AddComponent<Button>();
        view.choose.targetGraphic = border;
        view.choose.transition = Selectable.Transition.ColorTint;
        view.choose.colors = new ColorBlock
        {
            normalColor = theme.line,
            highlightedColor = theme.accent,
            pressedColor = theme.surfaceDim,
            selectedColor = theme.accent,
            disabledColor = UiTheme.Fade(theme.dim, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        view.BuildBody(border.transform, font);
        view.BuildRerollButton(font, rerollIcon);

        return view;
    }

    /// <summary>
    /// 프리팹으로 만든 카드를 쓸 때. 조립은 이미 끝나 있으니 배경만 알려준다.
    ///
    /// <b>크기·색·여백을 여기서 덮어쓰지 않는다.</b> 씬에서 다듬은 것을 코드가
    /// 되돌리면 프리팹으로 만든 의미가 없다 — 테마 색이 필요한 글자만 갱신 때 칠한다.
    /// </summary>
    public void Adopt(UiTheme theme, Layout layout, float spinTime)
    {
        this.theme = theme;
        this.layout = layout;
        this.spinTime = spinTime;

        root = (RectTransform)transform;

        // 인스펙터에서 물리는 것을 빠뜨렸을 때를 위한 예비. 없으면 조용히 안 눌린다
        if (choose == null) choose = GetComponent<Button>();

        if (reroll == null)
        {
            Transform t = transform.Find("RerollButton");
            if (t != null) reroll = t.GetComponent<Button>();
        }

        WarnIfIncomplete();
    }

    /// <summary>프리팹에서 빠뜨린 칸을 조용히 넘어가지 않게 한다.</summary>
    void WarnIfIncomplete()
    {
        string missing = "";

        if (choose == null) missing += " Choose";
        if (nameText == null) missing += " Name";
        if (descriptionText == null) missing += " Description";
        if (icon == null) missing += " Icon";

        if (missing.Length > 0)
            Debug.LogWarning($"[AugmentCardView] 프리팹에 안 물린 칸:{missing}", this);
    }

    void BuildBody(Transform border, TMP_FontAsset font)
    {
        float width = layout.CardSize.x;

        Image inner = UiFactory.CreateImage("Inner", border, theme.surface);
        UiFactory.Stretch((RectTransform)inner.transform, Vector2.zero, Vector2.one,
                          new Vector2(8f, 8f), new Vector2(-8f, -8f));

        Image iconSlot = UiFactory.CreateImage("IconSlot", inner.transform, theme.surfaceDim);
        UiFactory.Place((RectTransform)iconSlot.transform,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -70f), new Vector2(300f, 300f));

        icon = UiFactory.CreateImage("Icon", iconSlot.transform, Color.white);   // 스프라이트 원색 유지
        icon.preserveAspect = true;
        UiFactory.Stretch((RectTransform)icon.transform, Vector2.zero, Vector2.one,
                          new Vector2(12f, 12f), new Vector2(-12f, -12f));

        iconFallback = UiFactory.CreateText("IconFallback", iconSlot.transform, font,
                                            150f, theme.dim, TextAlignmentOptions.Center);
        UiFactory.Stretch((RectTransform)iconFallback.transform, Vector2.zero, Vector2.one);

        nameText = UiFactory.CreateText("Name", inner.transform, font,
                                        64f, theme.text, TextAlignmentOptions.Center);
        nameText.fontStyle = FontStyles.Bold;

        // 긴 증강 이름이 카드 밖으로 나가지 않게
        nameText.enableAutoSizing = true;
        nameText.fontSizeMax = 64f;
        nameText.fontSizeMin = 34f;
        UiFactory.Place((RectTransform)nameText.transform,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -420f), new Vector2(width - 60f, 90f));

        categoryText = UiFactory.CreateText("Category", inner.transform, font,
                                            42f, theme.text, TextAlignmentOptions.Center);
        UiFactory.Place((RectTransform)categoryText.transform,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -515f), new Vector2(width - 60f, 60f));

        levelText = UiFactory.CreateText("LevelInfo", inner.transform, font,
                                         40f, theme.dim, TextAlignmentOptions.Center);
        UiFactory.Place((RectTransform)levelText.transform,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -580f), new Vector2(width - 60f, 55f));

        descriptionText = UiFactory.CreateText("Description", inner.transform, font,
                                               44f, theme.text, TextAlignmentOptions.Top);
        UiFactory.Stretch((RectTransform)descriptionText.transform,
                          Vector2.zero, Vector2.one,
                          new Vector2(45f, 40f), new Vector2(-45f, -660f));

        // 설명 길이는 증강마다 제각각이다. 칸을 넘기느니 글자를 줄인다 —
        // 잘라내면 어느 수치가 사라졌는지 플레이어가 알 수 없다
        descriptionText.enableAutoSizing = true;
        descriptionText.fontSizeMax = 44f;
        descriptionText.fontSizeMin = 24f;
    }

    /// <summary>카드 바로 아래 붙는 정사각형 리롤 버튼. 카드와 함께 움직이도록 자식으로 둔다.</summary>
    void BuildRerollButton(TMP_FontAsset font, Sprite rerollIcon)
    {
        Image border = UiFactory.CreateImage("RerollButton", root, theme.line);
        border.raycastTarget = true;

        UiFactory.Place((RectTransform)border.transform,
                        new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -30f),
                        new Vector2(layout.ButtonSize, layout.ButtonSize));

        reroll = border.gameObject.AddComponent<Button>();
        reroll.targetGraphic = border;
        reroll.transition = Selectable.Transition.ColorTint;
        reroll.colors = new ColorBlock
        {
            normalColor = theme.line,
            highlightedColor = theme.accent,   // 마우스를 올리면 테두리가 살아난다
            pressedColor = theme.surfaceDim,
            selectedColor = theme.accent,
            disabledColor = UiTheme.Fade(theme.dim, 0.35f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        // 안쪽을 덮어 테두리만 띠로 남긴다. 이 이미지는 색이 안 변하므로 대비가 유지된다
        Image inner = UiFactory.CreateImage("Inner", border.transform, theme.surfaceDim);
        inner.raycastTarget = false;
        UiFactory.Stretch((RectTransform)inner.transform, Vector2.zero, Vector2.one,
                          new Vector2(4f, 4f), new Vector2(-4f, -4f));

        if (rerollIcon != null)
        {
            rerollIconImage = UiFactory.CreateImage("Icon", inner.transform, theme.text);
            rerollIconImage.sprite = rerollIcon;
            rerollIconImage.preserveAspect = true;

            // 한가운데 고정. 회전축이 곧 버튼 한가운데가 된다
            UiFactory.Place((RectTransform)rerollIconImage.transform,
                            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                            Vector2.zero, new Vector2(layout.IconSize, layout.IconSize));

            // 아이콘이 있으면 글자는 만들지 않는다. 남은 수는 하단 표시가 이미 보여준다
            return;
        }

        rerollLabel = UiFactory.CreateText("Label", inner.transform, font,
                                           26f, theme.text, TextAlignmentOptions.Center);
        UiFactory.Stretch((RectTransform)rerollLabel.transform, Vector2.zero, Vector2.one,
                          new Vector2(6f, 6f), new Vector2(-6f, -6f));
        rerollLabel.text = "REROLL";
    }

    // ── 보이기 ────────────────────────────────────────────

    public bool IsShown => gameObject.activeSelf;

    public void Show(bool on) => gameObject.SetActive(on);

    public void PlaceAt(float x) => root.anchoredPosition = new Vector2(x, 0f);

    /// <summary>카드에 증강 하나를 싣는다.</summary>
    public void Fill(AugmentData data, AugmentManager owned)
    {
        Data = data;

        bool hasIcon = data.icon != null;
        icon.enabled = hasIcon;
        iconFallback.gameObject.SetActive(!hasIcon);

        if (hasIcon) icon.sprite = data.icon;
        else iconFallback.text = string.IsNullOrEmpty(data.displayName)
                                    ? "?" : data.displayName[..1];

        nameText.text = string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName;

        categoryText.text = $"[ {CategoryLabel(data.category)} ]";
        categoryText.color = theme.ColorOf(data.category);

        if (data.instantEffect != InstantItemEffect.None)
        {
            levelText.text = "즉시 사용";
            descriptionText.text = AugmentText.Describe(data, 1);
            return;
        }

        AugmentRunner mine = owned != null ? owned.Find(data) : null;
        int current = mine != null ? mine.Instance.Level : 0;
        int nextLevel = current + 1;

        levelText.text = mine != null ? $"Lv {current} → Lv {nextLevel}" : "신규 획득";

        // 이미 가진 증강이면 무엇이 오르는지 나란히 보여준다. 신규면 한쪽 값만
        descriptionText.text = AugmentText.Compare(data, current, nextLevel);
    }

    /// <summary>리롤 버튼의 잠금과 글자를 상태에 맞춘다.</summary>
    public void SetRerollState(RerollState state, int left)
    {
        bool ready = state == RerollState.Ready;

        reroll.interactable = ready;

        Color tint = ready ? theme.text : theme.dim;

        if (rerollIconImage != null) rerollIconImage.color = tint;

        // 글자는 아이콘을 안 물렸을 때만 있다. 버튼이 정사각형이라 문구를 짧게 쓴다
        if (rerollLabel == null) return;

        rerollLabel.text = state switch
        {
            RerollState.SlotUsed => "USED",
            RerollState.Empty => "EMPTY",
            RerollState.NoAlternative => "NO ALT",
            _ => $"REROLL\n x{left}"
        };

        rerollLabel.color = tint;
    }

    /// <summary>
    /// 리롤 아이콘을 한 바퀴 돌린다. 선택 중에는 timeScale 이 0이라
    /// 반드시 실제 시간으로 재야 한다 — deltaTime 을 쓰면 영영 안 돈다.
    /// </summary>
    public void SpinRerollIcon()
    {
        if (rerollIconImage == null || spinTime <= 0f) return;

        StartCoroutine(Spin());
    }

    System.Collections.IEnumerator Spin()
    {
        float t = 0f;

        while (t < spinTime)
        {
            t += Time.unscaledDeltaTime;

            rerollIconImage.transform.localRotation =
                Quaternion.Euler(0f, 0f, -360f * Mathf.Clamp01(t / spinTime));

            yield return null;
        }

        rerollIconImage.transform.localRotation = Quaternion.identity;
    }

    // ── 글 만들기 ─────────────────────────────────────────

    public static string CategoryLabel(AugmentCategory category) => category switch
    {
        AugmentCategory.Search => "탐색",
        AugmentCategory.Sort => "정렬",
        AugmentCategory.DataStruct => "자료구조",
        AugmentCategory.Language => "언어",
        AugmentCategory.Optimize => "최적화",
        AugmentCategory.Code => "코드",
        AugmentCategory.Item => "아이템",
        _ => category.ToString()
    };
}
