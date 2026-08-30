using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 레벨업 시 증강 3택 1 화면. 고르는 동안 게임을 멈춘다.
///
/// <b>여기서 하는 일은 셋뿐이다</b> — 화면을 열고 닫기, 카드를 배치하기, 클릭을 받기.
/// 무엇이 후보인지는 <see cref="AugmentDraft"/>, 카드를 어떻게 그리는지는
/// <see cref="AugmentCardView"/>, 실제 지급은 <see cref="AugmentManager"/> 몫이다.
/// </summary>
public class AugmentSelectUI : MonoBehaviour
{
    [Header("증강 풀")]
    [Tooltip("스테이지에 풀이 없을 때만 쓰는 예비. 보통은 StageData 가 정한다.")]
    [SerializeField] AugmentPool fallbackPool;

    [Header("연결 (비우면 씬에서 찾는다)")]
    [SerializeField] Canvas canvas;
    [SerializeField] AugmentManager augmentManager;
    [SerializeField] TMP_FontAsset font;

    [Header("리롤 버튼")]
    [Tooltip("리롤 버튼에 띄울 아이콘. 비우면 글자만 나온다.")]
    [SerializeField] Sprite rerollIcon;

    [Tooltip("버튼 한 변(px). 정사각형이다.")]
    [SerializeField] float rerollButtonSize = 130f;

    [Tooltip("아이콘 한 변(px). 버튼보다 작아야 테두리가 보인다.")]
    [SerializeField] float rerollIconSize = 74f;

    [Tooltip("리롤할 때 아이콘이 한 바퀴 도는 시간(초). 0이면 안 돈다.")]
    [SerializeField] float rerollSpinTime = 0.35f;

    [Tooltip("남은 리롤 수 글자를 버튼 아래로 얼마나 띄울지(px).")]
    [SerializeField] float rerollCountGap = 40f;

    [Header("카드")]
    [Tooltip("카드 프리팹. 물리면 그것을 복제해서 쓴다.\n" +
             "비우면 지금까지처럼 코드가 조립한다 — 프리팹이 마음에 안 들면 이 칸만 비우면 된다.")]
    [SerializeField] AugmentCardView cardPrefab;

    [SerializeField] int choiceCount = 3;

    [Tooltip("코드로 조립할 때의 카드 크기. 프리팹을 쓰면 프리팹 크기를 따른다.")]
    [SerializeField] Vector2 cardSize = new(640f, 1100f);

    [SerializeField] float cardGap = 110f;

    [Header("등장 연출")]
    [Tooltip("카드 한 장이 올라오는 데 걸리는 시간(초).")]
    [Min(0f)] [SerializeField] float appearTime = 0.22f;

    [Tooltip("카드 사이의 시작 간격(초). 0이면 세 장이 한꺼번에 뜬다 —\n" +
             "어긋나게 줘야 \"차례로 뽑힌다\"로 읽힌다.")]
    [Min(0f)] [SerializeField] float appearStagger = 0.07f;

    [Tooltip("얼마나 아래에서 올라올지(px).")]
    [Min(0f)] [SerializeField] float appearRise = 90f;

    [Tooltip("커서를 올렸을 때 카드가 커지는 배율. 1이면 안 커진다.")]
    [Range(1f, 1.3f)] [SerializeField] float hoverScale = 1.04f;

    [Tooltip("그 크기로 따라붙는 속도. 크면 딱 붙고 작으면 물렁하게 따라온다.")]
    [Min(1f)] [SerializeField] float hoverSpeed = 14f;

    RectTransform overlay;
    TMP_Text headerText;
    TMP_Text rerollCountText;

    readonly List<AugmentCardView> cards = new();

    /// <summary>지금 깔려 있는 증강들. 리롤이 같은 것을 다시 주지 않게 하는 데 쓴다.</summary>
    readonly List<AugmentData> shown = new();

    /// <summary>
    /// 이번 레벨업에서 리롤로 버린 증강들. 다시 뽑히지 않는다.
    ///
    /// 화면에 떠 있는 것만 걸러내면, 1번 칸에서 버린 것이 2번 칸 리롤에서 되돌아온다.
    /// 리롤을 쓰고도 같은 것을 다시 보는 건 자원을 낭비당한 기분이 든다.
    /// </summary>
    readonly List<AugmentData> rejected = new();

    /// <summary>지금 뽑으면 안 되는 것 전부. 매번 새 리스트를 만들지 않으려고 재사용한다.</summary>
    readonly List<AugmentData> excluded = new();

    /// <summary>이번 레벨업에서 이 슬롯을 이미 리롤했는가. 카드가 새로 깔릴 때마다 초기화된다.</summary>
    bool[] rerollUsed;

    /// <summary>
    /// 확정으로 줄 증강들. 하나씩 <b>카드 1장짜리 라운드</b>로 보여준다.
    ///
    /// 캐릭터 고정 증강이 여기 들어온다. 몰래 지급하면 무엇을 들고 시작하는지 모른 채
    /// 게임이 도는데, 카드로 보여주면 반드시 한 번은 읽고 시작하게 된다.
    /// 고를 여지가 없으므로 사실상 확인 버튼이고, 그래서 리롤 칸도 없다.
    /// </summary>
    readonly Queue<AugmentData> forced = new();

    AugmentDraft draft;
    LevelSystem levelSystem;
    UiTheme theme;
    bool open;

    /// <summary>이번 라운드에 실제로 깔린 카드 수. 등장 연출이 이만큼만 돌린다.</summary>
    int activeCount;

    /// <summary>보유 증강 아이콘 줄. 선택 화면 위로 올려 마우스를 받게 한다.</summary>
    AugmentHud hud;

    /// <summary>남은 리롤. RunDirector 가 소유하고 여기서는 읽기만 한다.</summary>
    int RerollsLeft => RunDirector.Current != null ? RunDirector.Current.Rerolls : 0;

    /// <summary>떠 있는 것 + 이번 레벨업에 버린 것.</summary>
    List<AugmentData> Excluded()
    {
        excluded.Clear();
        excluded.AddRange(shown);

        for (int i = 0; i < rejected.Count; i++)
            if (!excluded.Contains(rejected[i])) excluded.Add(rejected[i]);

        return excluded;
    }

    void Awake()
    {
        // BuildOverlay 가 색·글꼴을 쓰므로 반드시 그 전에 잡는다
        theme = UiTheme.Current;

        // 인스펙터에 물려둔 게 있으면 그대로 두고, 비었을 때만 테마 글꼴로 채운다
        if (font == null) font = theme.mono;

        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (augmentManager == null) augmentManager = FindAnyObjectByType<AugmentManager>();

        hud = FindAnyObjectByType<AugmentHud>();

        rerollUsed = new bool[choiceCount];
        BuildOverlay();
    }

    void Start()
    {
        // StageSetup 이 Awake 에서 확정한다. 그래서 풀을 집는 것은 Start 여야 한다
        StageData stage = StageContext.Active;

        AugmentPool pool = stage != null && stage.augmentPool != null
            ? stage.augmentPool
            : fallbackPool;

        if (pool == null)
            Debug.LogError("[AugmentSelectUI] 증강 풀이 없어 선택지가 안 뜬다. " +
                           "StageData 의 Augment Pool 을 채울 것.", this);

        draft = new AugmentDraft(pool, augmentManager);

        levelSystem = GameManager.instance.levelSystem;
        levelSystem.LeveledUp += OnLeveledUp;

        StartCoroutine(OpenStartRounds());
    }

    // ── 런 시작 선택 ──────────────────────────────────────

    /// <summary>
    /// 런이 시작될 때 도는 증강 선택. 두 종류가 이어서 뜬다.
    ///
    /// <list type="number">
    /// <item>캐릭터 고정 증강 — 카드 1장. 확인 버튼 성격이라 리롤이 없다</item>
    /// <item>추가 선택 — 레벨업과 완전히 같은 3택. 메인보드 업그레이드분 + 캐릭터분</item>
    /// </list>
    ///
    /// <b>한 프레임 미루는 이유</b> — AugmentManager 도 Start 에서 시험용 증강을 지급하는데,
    /// Start 끼리는 순서가 없다. 여기서 바로 카드를 깔면 곧 받을 증강이 후보로 뜰 수 있다.
    /// <c>yield return null</c> 은 timeScale 이 0이어도 도므로 한 프레임이면 충분하다.
    /// </summary>
    System.Collections.IEnumerator OpenStartRounds()
    {
        yield return null;

        CharacterData character = CharacterContext.Active;

        if (character != null)
            for (int i = 0; i < character.startingAugments.Count; i++)
                if (character.startingAugments[i] != null)
                    forced.Enqueue(character.startingAugments[i]);

        int rounds = forced.Count
                   + HardwareBonus.ExtraStartRounds
                   + (character != null ? character.extraStartRounds : 0);

        if (rounds <= 0) yield break;

        levelSystem.AddPendingSelection(rounds);

        if (!open) TryOpen();
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
        if (!RunDirector.IsPlaying) return;

        if (!Roll())
        {
            // 뽑을 증강이 없으면 선택 없이 흘려보낸다
            while (levelSystem.ConsumePendingLevelUp()) { }
            return;
        }

        open = true;
        overlay.SetAsLastSibling();
        overlay.gameObject.SetActive(true);

        UiSound.Play(UiCue.LevelUp);

        // 반드시 SetActive 뒤에 — 꺼진 오브젝트에서는 코루틴이 시작되지 않는다
        PlayIntro();

        // 어둠 위로 아이콘 줄을 올린다. 무엇을 갖고 있는지 보면서 골라야 하고,
        // 밑에 깔려 있으면 오버레이가 마우스를 가로채 툴팁이 안 뜬다
        if (hud != null) hud.BringToFront();

        // 멈추는 것은 UIManager 가 정한다. 여기서 timeScale 을 만지면 일시정지와 서로를 밟는다
        if (UIManager.Current != null) UIManager.Current.Open(UIManager.Screen.AugmentSelect);
    }

    /// <summary>
    /// 카드를 차례로 띄운다.
    ///
    /// 여기서 포커스를 잡지는 않는다 — 마우스로 고르는 사람에게 카드 하나가 미리
    /// 밝아져 있으면 그게 이미 골라진 것인지 헷갈린다. 방향키를 누르면 그때 나타난다
    /// (<see cref="UiFocus"/>).
    /// </summary>
    void PlayIntro()
    {
        for (int i = 0; i < activeCount && i < cards.Count; i++)
            cards[i].PlayAppear(appearStagger * i, appearTime, appearRise);
    }

    void Update()
    {
        // 카드가 깔려 있을 때만. 안 그러면 화면 밖에서 방향키를 눌러도 카드가 선택된다
        if (open) UiFocus.Tick(overlay);
    }

    void Close()
    {
        open = false;
        overlay.gameObject.SetActive(false);

        // 사라진 카드가 선택된 채로 남으면 그때부터 방향키가 통째로 먹통이 된다
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        if (UIManager.Current != null) UIManager.Current.Close(UIManager.Screen.AugmentSelect);
    }

    void OnCardClicked(AugmentCardView card)
    {
        if (card.Data == null) return;

        UiSound.Play(UiCue.CardPick);

        // 이번 라운드가 확정 라운드였다면 이 클릭으로 소진된다.
        // Roll 이 Peek 으로 깔았고 그 사이에 큐를 건드리는 곳이 없으므로 맨 앞이 곧 이 카드다
        if (forced.Count > 0) forced.Dequeue();

        if (card.Data.instantEffect != InstantItemEffect.None)
            ApplyInstantEffect(card.Data);
        else
            GrantAugment(card.Data);

        levelSystem.ConsumePendingLevelUp();

        // 레벨업이 쌓여 있으면 닫지 않고 다음 선택지를 바로 깐다
        if (levelSystem.PendingLevelUps > 0 && Roll())
        {
            PlayIntro();
            return;
        }

        Close();
    }

    void GrantAugment(AugmentData data)
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
        if (player == null) return;

        switch (data.instantEffect)
        {
            case InstantItemEffect.Heal:
                if (player.TryGetComponent(out PlayerHealth health))
                {
                    float amount = health.Max * data.instantValue;
                    health.Heal(amount);

                    if (LogManager.Instance != null)
                        LogManager.Instance.Skill(
                            $"{data.displayName}: HP +{amount:0} ({data.instantValue:P0})");
                }
                break;

            case InstantItemEffect.SpeedBoost:
                player.ApplySpeedBoost(1f + data.instantValue, data.instantDuration);

                if (LogManager.Instance != null)
                    LogManager.Instance.Skill(
                        $"{data.displayName}: SPEED +{data.instantValue:P0} ({data.instantDuration:0}s)");
                break;
        }
    }

    // ── 선택지 깔기 ───────────────────────────────────────

    /// <summary>선택지를 새로 뽑아 카드에 싣는다. 뽑을 것이 없으면 false.</summary>
    bool Roll()
    {
        // 확정 증강이 대기 중이면 뽑지 않는다. 그것 한 장만 깔고 리롤도 막는다
        AugmentData fixedPick = forced.Count > 0 ? forced.Peek() : null;

        int count;

        if (fixedPick != null)
        {
            shown.Clear();
            shown.Add(fixedPick);
            count = 1;
        }
        else
        {
            count = draft.Pick(choiceCount, shown);
            if (count == 0) return false;
        }

        activeCount = count;

        for (int i = 0; i < cards.Count; i++)
        {
            bool active = i < count;
            cards[i].Show(active);

            if (active) cards[i].Fill(shown[i], augmentManager);

            cards[i].ShowReroll(fixedPick == null);
        }

        // 카드 수에 맞춰 가운데 정렬. 프리팹에서 폭을 바꿨으면 간격도 따라간다
        float width = cards.Count > 0 ? cards[0].Size.x : cardSize.x;

        for (int i = 0; i < count; i++)
            cards[i].PlaceAt((i - (count - 1) * 0.5f) * (width + cardGap));

        string title = fixedPick != null ? "STARTING AUGMENT" : "AUGMENT SELECT";

        headerText.text = levelSystem.PendingLevelUps > 1
            ? $"{title}  (+{levelSystem.PendingLevelUps - 1} 대기)"
            : title;

        // 슬롯 잠금도 버린 목록도 이번 레벨업 한정이다. 새 카드가 깔렸으니 전부 푼다
        System.Array.Clear(rerollUsed, 0, rerollUsed.Length);
        rejected.Clear();

        // 리롤이 없는 라운드에서는 남은 개수 표시도 감춘다. 쓸 수 없는 자원을 띄워두면
        // 지금 리롤할 수 있는 줄 알고 버튼을 찾게 된다
        rerollCountText.gameObject.SetActive(fixedPick == null);

        if (fixedPick == null) RefreshRerollButtons();

        return true;
    }

    // ── 리롤 ──────────────────────────────────────────────

    void OnRerollClicked(int index)
    {
        if (index < 0 || index >= cards.Count) return;
        if (rerollUsed[index]) return;

        AugmentCardView card = cards[index];
        if (!card.IsShown) return;

        AugmentData picked = draft.PickOne(Excluded());
        if (picked == null) return;   // 버튼이 이미 잠겨 있어야 정상이라 안전장치용

        // 보유량을 먼저 깎는다. 실패하면 아무것도 안 바꾼 채로 끝나야 한다
        if (RunDirector.Current == null || !RunDirector.Current.TrySpendReroll())
        {
            UiSound.Play(UiCue.Denied);
            return;
        }

        UiSound.Play(UiCue.Reroll);

        // 버린 것을 기억해 둔다. 다른 칸을 리롤해도 되돌아오지 않게
        rejected.Add(shown[index]);

        shown[index] = picked;
        card.Fill(picked, augmentManager);
        card.SpinRerollIcon();

        rerollUsed[index] = true;

        if (LogManager.Instance != null)
            LogManager.Instance.Skill(
                $"REROLL SLOT {index + 1} -> {picked.displayName}  (남은 {RerollsLeft})");

        RefreshRerollButtons();
    }

    /// <summary>버튼 잠금과 하단 표시를 지금 상태에 맞춘다.</summary>
    void RefreshRerollButtons()
    {
        int left = RerollsLeft;
        bool hasAlternative = draft.HasAlternative(Excluded());

        // 화면 하단에 남은 개수를 항상 띄운다. 이게 있어야 리롤이 자원으로 읽힌다
        rerollCountText.text = left > 0 ? $"REROLL  x{left}" : "REROLL  EMPTY";
        rerollCountText.color = left > 0 ? theme.accent : theme.dim;

        for (int i = 0; i < cards.Count; i++)
        {
            if (!cards[i].IsShown) continue;

            AugmentCardView.RerollState state =
                  rerollUsed[i]   ? AugmentCardView.RerollState.SlotUsed
                : left <= 0       ? AugmentCardView.RerollState.Empty
                : !hasAlternative ? AugmentCardView.RerollState.NoAlternative
                :                   AugmentCardView.RerollState.Ready;

            cards[i].SetRerollState(state, left);
        }
    }

    // ── 조립 ──────────────────────────────────────────────

    void BuildOverlay()
    {
        Image dim = UiFactory.CreateImage("AugmentSelect", canvas.transform,
                                          UiTheme.Fade(theme.background, 0.88f));
        dim.raycastTarget = true;   // 뒤쪽 UI 클릭 차단

        overlay = (RectTransform)dim.transform;
        UiFactory.Stretch(overlay, Vector2.zero, Vector2.one);

        var layout = new AugmentCardView.Layout
        {
            CardSize = cardSize,
            ButtonSize = rerollButtonSize,
            IconSize = rerollIconSize,
            HoverScale = hoverScale,
            HoverSpeed = hoverSpeed
        };

        // 카드를 먼저 만든다. 머리글·남은 수의 자리가 카드 크기를 따라가기 때문
        for (int i = 0; i < choiceCount; i++)
        {
            AugmentCardView card = MakeCard(i, layout);

            // 람다가 반복 변수를 잡지 않게 값을 복사해 둔다
            int index = i;

            if (card.Choose != null) card.Choose.onClick.AddListener(() => OnCardClicked(card));
            if (card.Reroll != null) card.Reroll.onClick.AddListener(() => OnRerollClicked(index));

            cards.Add(card);
        }

        BuildLabels();

        overlay.gameObject.SetActive(false);
    }

    /// <summary>프리팹이 있으면 복제하고, 없으면 코드로 조립한다.</summary>
    AugmentCardView MakeCard(int index, AugmentCardView.Layout layout)
    {
        if (cardPrefab == null)
            return AugmentCardView.Create(overlay, $"Card{index}", theme, font, layout,
                                          rerollIcon, rerollSpinTime);

        AugmentCardView card = Instantiate(cardPrefab, overlay);
        card.name = $"Card{index}";

        // 조립은 이미 프리팹에 되어 있다. 배경만 알려준다
        card.Adopt(theme, layout, rerollSpinTime);

        return card;
    }

    /// <summary>머리글과 남은 리롤 수. 카드 실제 크기를 알아야 자리가 정해진다.</summary>
    void BuildLabels()
    {
        // 프리팹에서 카드를 키웠으면 그 크기를 따른다. 인스펙터 값을 고집하면 글자가 카드를 파고든다
        Vector2 size = cards.Count > 0 ? cards[0].Size : cardSize;
        float half = size.y * 0.5f;

        headerText = UiFactory.CreateText("Header", overlay, font,
                                          90f, theme.text, TextAlignmentOptions.Center);
        UiFactory.Place((RectTransform)headerText.transform,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, half + 130f), new Vector2(2400f, 120f));

        // 리롤 버튼 바로 아래. 화면 바닥에 붙이면 카드가 화면보다 커서 버튼과 멀리 떨어진다.
        // 카드 반 높이 + 버튼까지의 틈(30) + 버튼 높이 + 여백
        float countY = -(half + 30f + rerollButtonSize + rerollCountGap);

        rerollCountText = UiFactory.CreateText("RerollCount", overlay, font,
                                               44f, theme.accent, TextAlignmentOptions.Center);
        UiFactory.Place((RectTransform)rerollCountText.transform,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f),
                        new Vector2(0f, countY), new Vector2(2400f, 70f));
    }
}
