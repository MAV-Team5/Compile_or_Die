using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 선택. MainB 의 CharacterPanel 에 붙인다.
///
/// <b>왜 RenderTexture 인가</b> — 캐릭터는 SpriteRenderer + Animator 프리팹이고,
/// 로비 캔버스는 Screen Space - Overlay 다. Overlay 캔버스는 씬의 어떤 스프라이트보다
/// 무조건 위에 그려지므로, 프리팹을 패널 위에 그냥 놓을 수 없다.
/// 그래서 캐릭터를 월드 한쪽(<see cref="StageOrigin"/>)에 세우고 전용 카메라가 찍어
/// 그림 한 장으로 만든 뒤 <see cref="RawImage"/> 에 붙인다.
/// 애니메이터·정렬순서가 그대로 살아있어서, <b>고를 때 본 모습이 곧 게임에서 움직이는 모습</b>이다.
///
/// <b>클릭 판정은 만들지 않는다.</b> 가운데 있는 것이 선택이라는 규칙이면 ◀ ▶ 로 충분하고,
/// RawImage 안의 좌표를 월드로 되돌리는 일은 얻는 것에 비해 품이 많이 든다.
///
/// 아트가 아직 없으므로 HardwareUpgradePanel 과 같은 방식으로 코드에서 조립한다.
/// </summary>
public class CharacterSelectPanel : MonoBehaviour
{
    /// <summary>
    /// 캐릭터를 세워둘 월드 좌표. 로비 카메라 화각 밖이면 어디든 된다.
    ///
    /// 레이어로 가르지 않고 거리로 가르는 이유는 레이어가 프로젝트 전역 자원이기 때문 —
    /// 이 화면 하나 때문에 레이어 한 칸을 영구히 쓰는 것보다 좌표를 띄우는 편이 싸다.
    /// </summary>
    static readonly Vector3 StageOrigin = new(0f, 500f, 0f);

    [Header("캐릭터")]
    [Tooltip("왼쪽부터 순서대로 늘어선다. 잠긴 것도 넣어야 물음표 자리가 생긴다.")]
    [SerializeField] CharacterData[] characters;

    [Header("연결")]
    [Tooltip("비우면 UiTheme 의 고정폭 글꼴을 쓴다.")]
    [SerializeField] TMP_FontAsset fontOverride;

    [Header("배치")]
    [Tooltip("캐릭터가 보이는 창의 크기(px).")]
    [SerializeField] Vector2 viewSize = new(1100f, 620f);

    [Tooltip("창을 패널 가운데에서 얼마나 올릴지(px).")]
    [SerializeField] float viewLift = 140f;

    [Header("캐러셀")]
    [Tooltip("칸 사이 월드 거리. 카메라 크기와 같이 조절할 것.")]
    [SerializeField] float slotSpacing = 4.2f;

    [Tooltip("카메라 직교 크기. 키우면 옆 칸이 더 많이 보인다.")]
    [SerializeField] float cameraSize = 2.4f;

    [Tooltip("한 칸 넘어가는 데 걸리는 시간(초). 0이면 즉시.")]
    [SerializeField] float scrollTime = 0.22f;

    [Tooltip("옆 칸 크기 배율.")]
    [Range(0.2f, 1f)] [SerializeField] float sideScale = 0.55f;

    [Tooltip("옆 칸 투명도.")]
    [Range(0f, 1f)] [SerializeField] float sideAlpha = 0.30f;

    // ── 런타임 ────────────────────────────────────────────

    /// <summary>월드에 세운 한 칸. 잠긴 칸은 프리팹 대신 물음표만 선다.</summary>
    class Slot
    {
        public CharacterData Data;
        public Transform Root;
        public SpriteRenderer[] Renderers;
        public Color[] BaseColors;
        public TMP_Text Question;
    }

    readonly List<Slot> slots = new();

    Transform rig;      // 카메라 + 무대. 패널이 닫히면 통째로 꺼진다
    Transform stage;    // 좌우로 밀리는 쪽
    Camera cam;
    RenderTexture texture;

    RawImage view;
    TMP_Text nameText;
    TMP_Text taglineText;
    TMP_Text statText;
    TMP_Text selectLabel;
    Button selectButton;
    Button prevButton;
    Button nextButton;

    UiTheme theme;
    int index;
    float scroll;
    bool built;

    TMP_FontAsset Font => theme.FontOr(fontOverride);

    CharacterData Focused => index >= 0 && index < slots.Count ? slots[index].Data : null;

    // ── 열고 닫기 ─────────────────────────────────────────

    // 패널이 꺼진 채로 시작하므로 Awake 가 아니라 열릴 때 처음 돈다
    void OnEnable()
    {
        theme = UiTheme.Current;

        if (!built)
        {
            if (characters == null || characters.Length == 0)
            {
                Debug.LogError("[CharacterSelectPanel] Characters 가 비어 있다. 화면을 못 만든다.", this);
                return;
            }

            Build();
            built = true;
        }

        // 조립에 실패했으면 아래가 전부 null 을 만진다. 여기서 끊는다
        if (!built) return;

        rig.gameObject.SetActive(true);

        // 지난번에 고른 것으로 돌아간다. 없으면 첫 해금 캐릭터
        index = IndexOf(CharacterContext.Selected);
        if (index < 0) index = FirstUnlocked();

        scroll = index;

        // 아무것도 안 고르고 스테이지로 갈 수 있으므로 열자마자 확정해 둔다
        Commit();
        Layout();
        Refresh();
    }

    void OnDisable()
    {
        // 안 보이는 카메라가 계속 도는 것은 나중에 "왜 프레임이 새지" 를 추적하기 어렵게 만든다
        if (rig != null) rig.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (texture == null) return;

        texture.Release();
        Destroy(texture);
    }

    // ── 매 프레임 ─────────────────────────────────────────

    /// <summary>
    /// 애니메이터가 <c>LateUpdate</c> 보다 먼저 돈다. 색을 여기서 덮어써야
    /// 클립이 스프라이트 색을 건드리는 캐릭터에서도 흐리게 하기가 밀리지 않는다.
    /// </summary>
    void LateUpdate()
    {
        if (!built) return;

        float target = index;

        if (scrollTime <= 0f) scroll = target;
        else scroll = Mathf.MoveTowards(scroll, target,
                                        Time.unscaledDeltaTime / scrollTime);

        Layout();
    }

    /// <summary>스크롤 위치에 맞춰 무대를 밀고, 칸마다 크기·투명도를 매긴다.</summary>
    void Layout()
    {
        stage.localPosition = new Vector3(-scroll * slotSpacing, 0f, 0f);

        for (int i = 0; i < slots.Count; i++)
        {
            float away = Mathf.Clamp01(Mathf.Abs(i - scroll));

            float scale = Mathf.Lerp(1f, sideScale, away);
            float alpha = Mathf.Lerp(1f, sideAlpha, away);

            Slot slot = slots[i];

            slot.Root.localScale = Vector3.one * scale;

            for (int r = 0; r < slot.Renderers.Length; r++)
            {
                if (slot.Renderers[r] == null) continue;

                Color c = slot.BaseColors[r];
                slot.Renderers[r].color = new Color(c.r, c.g, c.b, c.a * alpha);
            }

            if (slot.Question != null)
                slot.Question.color = UiTheme.Fade(theme.dim, alpha);
        }
    }

    // ── 조작 ──────────────────────────────────────────────

    void Step(int delta)
    {
        index = Mathf.Clamp(index + delta, 0, slots.Count - 1);
        Refresh();
    }

    /// <summary>가운데 캐릭터를 이번 런의 캐릭터로 확정한다. 잠긴 것은 받지 않는다.</summary>
    void Commit()
    {
        CharacterData data = Focused;

        if (data == null || !data.unlocked) return;

        CharacterContext.Choose(data);
    }

    void OnSelectClicked()
    {
        Commit();
        Refresh();
    }

    /// <summary>글자와 버튼 상태를 지금 고른 칸에 맞춘다.</summary>
    void Refresh()
    {
        CharacterData data = Focused;

        bool unlocked = data != null && data.unlocked;
        bool chosen = data != null && CharacterContext.Selected == data;

        nameText.text = data == null ? "?" : data.NameOrLocked;
        nameText.color = unlocked ? theme.text : theme.dim;

        taglineText.text = !unlocked ? "LOCKED" : data.tagline;
        taglineText.color = unlocked ? theme.dim : theme.warn;

        statText.text = unlocked ? StatLines(data) : "";

        prevButton.interactable = index > 0;
        nextButton.interactable = index < slots.Count - 1;

        selectButton.interactable = unlocked && !chosen;

        selectLabel.text = !unlocked ? "LOCKED" : chosen ? "> SELECTED" : "SELECT";
        selectLabel.color = !unlocked ? theme.dim : chosen ? theme.good : theme.accent;
    }

    string StatLines(CharacterData data)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"{UiFactory.Pad("체력", 10)}{data.maxHealth:0}");
        sb.AppendLine($"{UiFactory.Pad("이동속도", 10)}{data.moveSpeed:0.##}");
        sb.AppendLine($"{UiFactory.Pad("획득범위", 10)}{data.pickupRange:0.##}");

        // 무엇을 들고 시작하는지가 캐릭터를 고르는 가장 큰 이유다. 이름을 그대로 보여준다
        string start = "-";

        if (data.startingAugments != null && data.startingAugments.Count > 0)
        {
            var names = new List<string>();

            for (int i = 0; i < data.startingAugments.Count; i++)
                if (data.startingAugments[i] != null)
                    names.Add(data.startingAugments[i].displayName);

            if (names.Count > 0) start = string.Join(", ", names);
        }

        sb.AppendLine($"{UiFactory.Pad("시작 증강", 10)}{start}");

        if (data.extraStartRounds > 0)
            sb.AppendLine($"{UiFactory.Pad("추가 선택", 10)}+{data.extraStartRounds}");

        return sb.ToString().TrimEnd();
    }

    int IndexOf(CharacterData data)
    {
        if (data == null) return -1;

        for (int i = 0; i < slots.Count; i++)
            if (slots[i].Data == data) return i;

        return -1;
    }

    int FirstUnlocked()
    {
        for (int i = 0; i < slots.Count; i++)
            if (slots[i].Data != null && slots[i].Data.unlocked) return i;

        return 0;
    }

    // ── 조립 ──────────────────────────────────────────────

    void Build()
    {
        BuildRig();
        BuildSlots();
        BuildUi();
    }

    /// <summary>월드에 무대와 전용 카메라를 세운다. 패널이 닫히면 통째로 꺼진다.</summary>
    void BuildRig()
    {
        var rigGo = new GameObject("CharacterStageRig");
        rig = rigGo.transform;
        rig.position = StageOrigin;

        var stageGo = new GameObject("Stage");
        stage = stageGo.transform;
        stage.SetParent(rig, false);

        int width = Mathf.Max(64, Mathf.RoundToInt(viewSize.x));
        int height = Mathf.Max(64, Mathf.RoundToInt(viewSize.y));

        texture = new RenderTexture(width, height, 16) { name = "CharacterSelectRT" };

        var camGo = new GameObject("CharacterCam");
        camGo.transform.SetParent(rig, false);
        camGo.transform.localPosition = new Vector3(0f, 0f, -10f);

        cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = cameraSize;
        cam.clearFlags = CameraClearFlags.SolidColor;

        // 알파를 0으로 두면 파이프라인에 따라 뒷배경이 비치거나 안 비친다.
        // 어느 쪽이든 같아 보이게 불투명한 색으로 채운다
        cam.backgroundColor = theme.surfaceDim;

        cam.targetTexture = texture;   // 이걸 물리면 화면에는 안 그린다
        cam.depth = -50;
    }

    void BuildSlots()
    {
        for (int i = 0; i < characters.Length; i++)
        {
            CharacterData data = characters[i];

            var root = new GameObject($"Slot{i}_{(data != null ? data.name : "empty")}").transform;
            root.SetParent(stage, false);
            root.localPosition = new Vector3(i * slotSpacing, 0f, 0f);

            var slot = new Slot { Data = data, Root = root };

            bool showVisual = data != null && data.unlocked && data.visualPrefab != null;

            if (showVisual) Instantiate(data.visualPrefab, root, false);
            else slot.Question = BuildQuestion(root);

            slot.Renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            slot.BaseColors = new Color[slot.Renderers.Length];

            for (int r = 0; r < slot.Renderers.Length; r++)
                slot.BaseColors[r] = slot.Renderers[r].color;

            slots.Add(slot);
        }
    }

    /// <summary>잠긴 칸. 아직 아트가 없으므로 월드 글자 하나로 대신한다.</summary>
    TMP_Text BuildQuestion(Transform parent)
    {
        var go = new GameObject("Locked");
        go.transform.SetParent(parent, false);

        TextMeshPro text = go.AddComponent<TextMeshPro>();

        if (Font != null) text.font = Font;

        text.text = "?";
        text.fontSize = 14f;
        text.color = theme.dim;
        text.alignment = TextAlignmentOptions.Center;

        text.rectTransform.sizeDelta = new Vector2(4f, 4f);

        return text;
    }

    void BuildUi()
    {
        var root = (RectTransform)transform;

        TMP_Text title = UiFactory.CreateText("Title", root, Font, 64f, theme.text,
                                              TextAlignmentOptions.Left);
        UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                        new Vector2(60f, -60f), new Vector2(1400f, 90f));
        title.text = "> CHARACTER";

        BuildView(root);
        BuildArrows(root);
        BuildInfo(root);
        BuildSelect(root);
    }

    void BuildView(RectTransform root)
    {
        RectTransform frame = UiFactory.CreateRect("View", root);
        UiFactory.Place(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, viewLift), viewSize);

        Image edge = frame.gameObject.AddComponent<Image>();
        edge.color = theme.line;

        view = UiFactory.CreateRect("Screen", frame).gameObject.AddComponent<RawImage>();
        UiFactory.Stretch((RectTransform)view.transform, Vector2.zero, Vector2.one,
                          new Vector2(3f, 3f), new Vector2(-3f, -3f));

        view.texture = texture;
        view.raycastTarget = false;
    }

    void BuildArrows(RectTransform root)
    {
        prevButton = BuildArrow(root, "Prev", "<", -1);
        nextButton = BuildArrow(root, "Next", ">", 1);
    }

    Button BuildArrow(RectTransform root, string name, string glyph, int delta)
    {
        float x = (viewSize.x * 0.5f + 90f) * delta;

        RectTransform rect = UiFactory.CreateRect(name, root);
        UiFactory.Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(x, viewLift), new Vector2(120f, 120f));

        Image face = rect.gameObject.AddComponent<Image>();
        face.color = theme.surface;
        face.raycastTarget = true;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.colors = theme.ButtonColors(theme.surface);
        button.onClick.AddListener(() => Step(delta));

        TMP_Text label = UiFactory.CreateText("Text", rect, Font, 64f, theme.accent,
                                              TextAlignmentOptions.Center);
        UiFactory.Stretch(label.rectTransform, Vector2.zero, Vector2.one);
        label.text = glyph;

        return button;
    }

    void BuildInfo(RectTransform root)
    {
        float top = viewLift - viewSize.y * 0.5f - 40f;

        nameText = UiFactory.CreateText("Name", root, Font, 72f, theme.text,
                                        TextAlignmentOptions.Center);
        UiFactory.Place(nameText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f),
                        new Vector2(0f, top), new Vector2(1200f, 90f));

        taglineText = UiFactory.CreateText("Tagline", root, Font, 40f, theme.dim,
                                           TextAlignmentOptions.Center);
        UiFactory.Place(taglineText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f),
                        new Vector2(0f, top - 100f), new Vector2(1200f, 70f));

        statText = UiFactory.CreateText("Stats", root, Font, 38f, theme.dim,
                                        TextAlignmentOptions.TopLeft);
        UiFactory.Place(statText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f),
                        new Vector2(0f, top - 190f), new Vector2(760f, 260f));
    }

    void BuildSelect(RectTransform root)
    {
        RectTransform rect = UiFactory.CreateRect("Select", root);
        UiFactory.Place(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 150f), new Vector2(560f, 120f));

        Image face = rect.gameObject.AddComponent<Image>();
        face.color = theme.surface;
        face.raycastTarget = true;

        selectButton = rect.gameObject.AddComponent<Button>();
        selectButton.targetGraphic = face;
        selectButton.colors = theme.ButtonColors(theme.surface);
        selectButton.onClick.AddListener(OnSelectClicked);

        selectLabel = UiFactory.CreateText("Text", rect, Font, 52f, theme.accent,
                                           TextAlignmentOptions.Center);
        UiFactory.Stretch(selectLabel.rectTransform, Vector2.zero, Vector2.one);
        selectLabel.text = "SELECT";
    }
}
