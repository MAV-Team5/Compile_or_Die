using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨업 시 증강 3택 1 화면. 고르는 동안 게임을 멈춘다.
/// 카드는 코드로 조립한다 — 카드 아트가 나오면 프리팹 방식으로 바꾸면 된다.
/// </summary>
public class AugmentSelectUI : MonoBehaviour
{
    [Header("선택지에 나올 증강 풀")]
    [SerializeField] List<AugmentData> augmentPool = new();

    [Header("연결 (비우면 씬에서 찾는다)")]
    [SerializeField] Canvas canvas;
    [SerializeField] AugmentManager augmentManager;
    [SerializeField] TMP_FontAsset font;

    [Header("카드")]
    [SerializeField] int choiceCount = 3;
    [SerializeField] Vector2 cardSize = new(640f, 1100f);
    [SerializeField] float cardGap = 110f;

    RectTransform overlay;
    TMP_Text headerText;
    readonly List<Card> cards = new();
    LevelSystem levelSystem;
    bool open;

    /// <summary>슬롯(카드 위치)별 리롤 사용 여부. 게임 전체에서 위치당 1회만 허용된다.</summary>
    bool[] rerollUsed;

    static readonly Color RerollNormalColor = new(0.16f, 0.18f, 0.26f, 1f);
    static readonly Color RerollLabelActive = new(0.85f, 0.88f, 0.95f, 1f);
    static readonly Color RerollLabelLocked = new(0.45f, 0.45f, 0.5f, 1f);

    class Card
    {
        public RectTransform Root;
        public Button Button;
        public Image Icon;
        public TMP_Text IconFallback;
        public TMP_Text Name;
        public TMP_Text Category;
        public TMP_Text LevelInfo;
        public TMP_Text Description;
        public Button RerollButton;
        public TMP_Text RerollLabel;
        public AugmentData Data;
    }

    void Awake()
    {
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (augmentManager == null) augmentManager = FindAnyObjectByType<AugmentManager>();

        rerollUsed = new bool[choiceCount];
        BuildOverlay();
    }

    void Start()
    {
        levelSystem = GameManager.instance.levelSystem;
        levelSystem.LeveledUp += OnLeveledUp;
    }

    void OnDestroy()
    {
        if (levelSystem != null) levelSystem.LeveledUp -= OnLeveledUp;
    }

    void OnLeveledUp(int _)
    {
        if (!open) TryOpen();
    }

    // ── 열고 닫기 ─────────────────────────────────────────

    void TryOpen()
    {
        if (levelSystem.PendingLevelUps <= 0) return;
        if (GameManager.instance.isGameOver) return;

        if (!Roll())
        {
            // 뽑을 증강이 없으면 선택 없이 흘려보낸다
            while (levelSystem.ConsumePendingLevelUp()) { }
            return;
        }

        open = true;
        overlay.SetAsLastSibling();
        overlay.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    void Close()
    {
        open = false;
        overlay.gameObject.SetActive(false);

        if (!GameManager.instance.isGameOver)
            Time.timeScale = 1f;
    }

    void OnCardClicked(Card card)
    {
        if (card.Data == null) return;

        if (card.Data.instantEffect != InstantItemEffect.None)
            ApplyInstantEffect(card.Data);
        else
            GrantWeapon(card.Data);

        levelSystem.ConsumePendingLevelUp();

        // 레벨업이 쌓여 있으면 닫지 않고 다음 선택지를 바로 깐다
        if (levelSystem.PendingLevelUps > 0 && Roll()) return;

        Close();
    }

    void GrantWeapon(AugmentData data)
    {
        AugmentRunner runner = augmentManager.Grant(data);

        if (LogManager.Instance != null && runner != null)
            LogManager.Instance.Skill(
                $"AUGMENT LOADED: {data.displayName} Lv.{runner.Instance.Level}");
    }

    /// <summary>보유 개념 없이 선택 즉시 한 번 적용되고 사라지는 아이템 효과.</summary>
    void ApplyInstantEffect(AugmentData data)
    {
        Player player = GameManager.instance.player;

        switch (data.instantEffect)
        {
            case InstantItemEffect.Heal:
                if (player != null && player.TryGetComponent(out PlayerHealth health))
                {
                    float amount = health.Max * data.instantValue;
                    health.Heal(amount);

                    if (LogManager.Instance != null)
                        LogManager.Instance.Skill(
                            $"{data.displayName}: HP +{amount:0} ({data.instantValue:P0})");
                }
                break;

            case InstantItemEffect.SpeedBoost:
                if (player != null)
                {
                    player.ApplySpeedBoost(1f + data.instantValue, data.instantDuration);

                    if (LogManager.Instance != null)
                        LogManager.Instance.Skill(
                            $"{data.displayName}: SPEED +{data.instantValue:P0} ({data.instantDuration:0}s)");
                }
                break;
        }
    }

    // ── 선택지 뽑기 ───────────────────────────────────────

    /// <summary>선택지를 새로 뽑아 카드에 싣는다. 뽑을 것이 없으면 false.</summary>
    bool Roll()
    {
        List<AugmentData> candidates = CollectCandidates();
        if (candidates.Count == 0) return false;

        // TODO: 기획 — 보유/시너지 증강 확률 보정. 지금은 균등 랜덤
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int count = Mathf.Min(choiceCount, candidates.Count);

        for (int i = 0; i < cards.Count; i++)
        {
            bool active = i < count;
            cards[i].Root.gameObject.SetActive(active);

            if (active) FillCard(cards[i], candidates[i]);
        }

        // 카드 수에 맞춰 가운데 정렬
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) * 0.5f) * (cardSize.x + cardGap);
            cards[i].Root.anchoredPosition = new Vector2(x, 0f);
        }

        headerText.text = levelSystem.PendingLevelUps > 1
            ? $"AUGMENT SELECT  (+{levelSystem.PendingLevelUps - 1} 대기)"
            : "AUGMENT SELECT";

        RefreshRerollButtons();

        return true;
    }

    // ── 리롤 ──────────────────────────────────────────────

    void OnRerollClicked(int index)
    {
        if (index < 0 || index >= cards.Count) return;
        if (rerollUsed[index]) return;

        Card card = cards[index];
        if (!card.Root.gameObject.activeSelf) return;

        List<AugmentData> candidates = CollectCandidates();
        candidates.RemoveAll(IsCurrentlyShown);
        if (candidates.Count == 0) return; // 버튼이 이미 잠겨 있어야 정상이라 안전장치용

        AugmentData picked = candidates[Random.Range(0, candidates.Count)];
        FillCard(card, picked);

        rerollUsed[index] = true;

        if (LogManager.Instance != null)
            LogManager.Instance.Skill($"REROLL SLOT {index + 1} -> {picked.displayName}");

        RefreshRerollButtons();
    }

    /// <summary>슬롯마다 잠금 여부와 교환 가능 여부에 맞춰 버튼 상태를 갱신한다.</summary>
    void RefreshRerollButtons()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            Card card = cards[i];
            if (!card.Root.gameObject.activeSelf) continue;

            bool locked = rerollUsed[i];
            bool canReroll = !locked && HasRerollAlternative();

            card.RerollButton.interactable = canReroll;
            card.RerollLabel.text = locked ? "USED" : "$ REROLL";
            card.RerollLabel.color = canReroll ? RerollLabelActive : RerollLabelLocked;
        }
    }

    /// <summary>지금 화면에 안 뜬 다른 증강이 하나라도 남아 있는가.</summary>
    bool HasRerollAlternative()
    {
        List<AugmentData> candidates = CollectCandidates();

        for (int i = 0; i < candidates.Count; i++)
            if (!IsCurrentlyShown(candidates[i])) return true;

        return false;
    }

    bool IsCurrentlyShown(AugmentData data)
    {
        for (int i = 0; i < cards.Count; i++)
            if (cards[i].Root.gameObject.activeSelf && cards[i].Data == data) return true;

        return false;
    }

    List<AugmentData> CollectCandidates()
    {
        var result = new List<AugmentData>();

        foreach (AugmentData data in augmentPool)
        {
            if (data == null) continue;

            bool isInstant = data.instantEffect != InstantItemEffect.None;

            // 즉시 효과 아이템은 AugmentManager에 등록되지 않으니 절대 "만렙"이 되지 않는다 —
            // 다른 무기가 전부 만렙이 돼도 이런 아이템만 계속 후보로 남는 이유가 이거다
            if (isInstant)
            {
                if (data.instantEffect == InstantItemEffect.Heal && !RollHealChance())
                    continue;

                result.Add(data);
                continue;
            }

            if (data.levelStats == null || data.levelStats.Length == 0)
                continue;

            // 이미 만렙이면 더 올릴 수 없다
            AugmentRunner owned = augmentManager.Find(data);
            if (owned != null && owned.Instance.Level >= owned.Instance.MaxLevel)
                continue;

            // 내부 증강은 뿌리 증강이 조건 레벨에 도달해야 풀린다
            if (data.rootAugment != null)
            {
                AugmentRunner root = augmentManager.Find(data.rootAugment);
                if (root == null || root.Instance.Level < data.requiredRootLevel)
                    continue;
            }

            result.Add(data);
        }

        return result;
    }

    /// <summary>회복류 즉시 아이템 등장 확률 = 100 - 현재 체력%. 만피면 0%, 빈사면 거의 확정.</summary>
    bool RollHealChance()
    {
        Player player = GameManager.instance.player;

        if (player == null || !player.TryGetComponent(out PlayerHealth health) || health.Max <= 0f)
            return false;

        float hpPercent = health.Current / health.Max * 100f;
        float chance = 100f - hpPercent;

        return Random.Range(0f, 100f) < chance;
    }

    // ── 카드 내용 채우기 ──────────────────────────────────

    void FillCard(Card card, AugmentData data)
    {
        card.Data = data;

        bool hasIcon = data.icon != null;
        card.Icon.enabled = hasIcon;
        card.IconFallback.gameObject.SetActive(!hasIcon);

        if (hasIcon) card.Icon.sprite = data.icon;
        else card.IconFallback.text = string.IsNullOrEmpty(data.displayName)
                                        ? "?" : data.displayName[..1];

        card.Name.text = string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName;

        card.Category.text = $"[ {CategoryLabel(data.category)} ]";
        card.Category.color = CategoryColor(data.category);

        if (data.instantEffect != InstantItemEffect.None)
        {
            card.LevelInfo.text = "즉시 사용";
            card.Description.text = BuildDescription(data, 1);
            return;
        }

        AugmentRunner owned = augmentManager.Find(data);
        int nextLevel = owned != null ? owned.Instance.Level + 1 : 1;

        card.LevelInfo.text = owned != null
            ? $"Lv {owned.Instance.Level} → Lv {nextLevel}"
            : "신규 획득";

        card.Description.text = BuildDescription(data, nextLevel);
    }

    /// <summary>설명 토큰을 다음 레벨 수치로 치환한다. AugmentText와 같은 규칙.</summary>
    static string BuildDescription(AugmentData data, int level)
    {
        string text = string.IsNullOrEmpty(data.descriptionTemplate)
            ? $"{data.displayName} 증강."
            : data.descriptionTemplate;

        text = text.Replace("{name}", data.displayName).Replace("{level}", level.ToString());

        // 즉시 효과 아이템은 levelStats가 없다 — 그 자리는 그냥 안 채워진다
        if (data.levelStats == null || data.levelStats.Length == 0)
            return text;

        AugmentLevelData stat =
            data.levelStats[Mathf.Clamp(level - 1, 0, data.levelStats.Length - 1)];

        return text
            .Replace("{damage}", stat.damage.ToString("0.#"))
            .Replace("{effectDamage}", stat.effectDamage.ToString("0.#"))
            .Replace("{cooldown}", stat.cooldown.ToString("0.#"))
            .Replace("{range}", stat.range.ToString("0.#"))
            .Replace("{count}", stat.count.ToString());
    }

    static string CategoryLabel(AugmentCategory category) => category switch
    {
        AugmentCategory.Search     => "탐색",
        AugmentCategory.Sort       => "정렬",
        AugmentCategory.DataStruct => "자료구조",
        AugmentCategory.Language   => "언어",
        AugmentCategory.Optimize   => "최적화",
        AugmentCategory.Code       => "코드",
        AugmentCategory.Item       => "아이템",
        _                          => category.ToString()
    };

    static Color CategoryColor(AugmentCategory category) => category switch
    {
        AugmentCategory.Search     => new Color(0.35f, 0.9f, 0.95f),
        AugmentCategory.Sort       => new Color(1f, 0.7f, 0.3f),
        AugmentCategory.DataStruct => new Color(0.5f, 0.9f, 0.5f),
        AugmentCategory.Language   => new Color(0.45f, 0.65f, 1f),
        AugmentCategory.Optimize   => new Color(1f, 0.95f, 0.45f),
        AugmentCategory.Code       => new Color(0.8f, 0.6f, 1f),
        AugmentCategory.Item       => new Color(1f, 0.55f, 0.25f),
        _                          => Color.white
    };

    // ── 조립 ──────────────────────────────────────────────

    void BuildOverlay()
    {
        Image dim = UiFactory.CreateImage("AugmentSelect", canvas.transform, new Color(0f, 0f, 0f, 0.72f));
        dim.raycastTarget = true; // 뒤쪽 UI 클릭 차단

        overlay = (RectTransform)dim.transform;
        UiFactory.Stretch(overlay, Vector2.zero, Vector2.one);

        headerText = UiFactory.CreateText("Header", overlay, font,
                                          90f, Color.white, TextAlignmentOptions.Center);
        UiFactory.Place((RectTransform)headerText.transform,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, cardSize.y * 0.5f + 130f), new Vector2(2400f, 120f));

        for (int i = 0; i < choiceCount; i++)
            cards.Add(BuildCard(i));

        overlay.gameObject.SetActive(false);
    }

    Card BuildCard(int index)
    {
        var card = new Card();

        // 테두리가 곧 버튼 판정면이다
        Image border = UiFactory.CreateImage($"Card{index}", overlay, Color.white);
        border.raycastTarget = true;

        card.Root = (RectTransform)border.transform;
        UiFactory.Place(card.Root,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, cardSize);

        card.Button = border.gameObject.AddComponent<Button>();
        card.Button.targetGraphic = border;
        card.Button.transition = Selectable.Transition.ColorTint;
        card.Button.colors = new ColorBlock
        {
            normalColor      = new Color(0.72f, 0.62f, 0.86f),
            highlightedColor = Color.white,
            pressedColor     = new Color(0.5f, 0.42f, 0.62f),
            selectedColor    = Color.white,
            disabledColor    = new Color(0.4f, 0.4f, 0.4f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.08f
        };
        card.Button.onClick.AddListener(() => OnCardClicked(card));

        Image inner = UiFactory.CreateImage("Inner", card.Root, new Color(0.04f, 0.06f, 0.12f, 1f));
        UiFactory.Stretch((RectTransform)inner.transform, Vector2.zero, Vector2.one,
                          new Vector2(8f, 8f), new Vector2(-8f, -8f));

        Image iconSlot = UiFactory.CreateImage("IconSlot", inner.transform, new Color(0.09f, 0.13f, 0.22f, 1f));
        UiFactory.Place((RectTransform)iconSlot.transform,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -70f), new Vector2(300f, 300f));

        card.Icon = UiFactory.CreateImage("Icon", iconSlot.transform, Color.white);
        card.Icon.preserveAspect = true;
        UiFactory.Stretch((RectTransform)card.Icon.transform, Vector2.zero, Vector2.one,
                          new Vector2(12f, 12f), new Vector2(-12f, -12f));

        card.IconFallback = UiFactory.CreateText("IconFallback", iconSlot.transform, font,
                                                 150f, new Color(0.65f, 0.75f, 0.95f), TextAlignmentOptions.Center);
        UiFactory.Stretch((RectTransform)card.IconFallback.transform, Vector2.zero, Vector2.one);

        card.Name = UiFactory.CreateText("Name", inner.transform, font,
                                         64f, Color.white, TextAlignmentOptions.Center);
        card.Name.fontStyle = FontStyles.Bold;
        UiFactory.Place((RectTransform)card.Name.transform,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -420f), new Vector2(cardSize.x - 60f, 90f));

        card.Category = UiFactory.CreateText("Category", inner.transform, font,
                                             42f, Color.white, TextAlignmentOptions.Center);
        UiFactory.Place((RectTransform)card.Category.transform,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -515f), new Vector2(cardSize.x - 60f, 60f));

        card.LevelInfo = UiFactory.CreateText("LevelInfo", inner.transform, font,
                                              40f, new Color(1f, 1f, 1f, 0.55f), TextAlignmentOptions.Center);
        UiFactory.Place((RectTransform)card.LevelInfo.transform,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -580f), new Vector2(cardSize.x - 60f, 55f));

        card.Description = UiFactory.CreateText("Description", inner.transform, font,
                                                44f, new Color(0.85f, 0.88f, 0.95f), TextAlignmentOptions.Top);
        UiFactory.Stretch((RectTransform)card.Description.transform,
                          Vector2.zero, Vector2.one,
                          new Vector2(45f, 40f), new Vector2(-45f, -660f));

        BuildRerollButton(card, index);

        return card;
    }

    /// <summary>카드 바로 아래 붙는 슬롯별 리롤 버튼. 카드와 함께 움직이도록 카드의 자식으로 둔다.</summary>
    void BuildRerollButton(Card card, int index)
    {
        Image bg = UiFactory.CreateImage("RerollButton", card.Root, RerollNormalColor);
        bg.raycastTarget = true;

        var rect = (RectTransform)bg.transform;
        UiFactory.Place(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -30f), new Vector2(cardSize.x * 0.62f, 84f));

        card.RerollButton = bg.gameObject.AddComponent<Button>();
        card.RerollButton.targetGraphic = bg;
        card.RerollButton.transition = Selectable.Transition.ColorTint;
        card.RerollButton.colors = new ColorBlock
        {
            normalColor      = RerollNormalColor,
            highlightedColor = new Color(0.26f, 0.30f, 0.42f, 1f),
            pressedColor     = new Color(0.10f, 0.11f, 0.16f, 1f),
            selectedColor    = RerollNormalColor,
            disabledColor    = new Color(0.12f, 0.12f, 0.12f, 0.6f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.08f
        };
        card.RerollButton.onClick.AddListener(() => OnRerollClicked(index));

        card.RerollLabel = UiFactory.CreateText("Label", bg.transform, font,
                                                34f, RerollLabelActive, TextAlignmentOptions.Center);
        UiFactory.Stretch((RectTransform)card.RerollLabel.transform, Vector2.zero, Vector2.one);
        card.RerollLabel.text = "$ REROLL";
    }
}
