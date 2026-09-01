using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;   // Selectable

/// <summary>
/// 글자 자체를 물들이는 네온 버튼 연출. Button 옆에 같이 붙인다 —
/// 누르는 처리는 Button 이 하고, 색만 이쪽이 맡는다.
///
/// <b>왜 Button 의 ColorTint 를 안 쓰나</b> — ColorTint 는 CanvasRenderer 한 장에만 색을 건다.
/// 글꼴이 아틀라스를 여러 장 쓰면(한글처럼 글자 수가 많으면 반드시 그렇게 된다)
/// TMP 가 넘친 글자를 별도 서브메시로 그리는데, 그 서브메시는 색을 못 받는다.
/// 그래서 "다시"는 초록인데 "하기"는 회색인, 한 단어 안에서 색이 갈리는 일이 생긴다.
/// TMP 의 color 에 직접 넣으면 서브메시까지 한꺼번에 바뀐다.
///
/// 색은 씬에 놓인 기존 네온 버튼과 같은 값이다 —
/// 그쪽은 글자색 × 상태색 × 배수 5로 만들지만, 여기서는 그 결과를 그대로 적어둔다.
///
/// <b>마우스 호버와 키보드 포커스를 한 상태로 본다.</b> 둘 다 "지금 이걸 고르려 한다"는
/// 같은 사건이라, 색도 소리도 같아야 한다. 나누어 두면 키보드로 옮겼을 때
/// 아무 표시가 없어서 지금 뭐가 골라져 있는지 알 수 없다.
/// </summary>
public class NeonTextButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler, IPointerClickHandler, ISubmitHandler
{
    [Tooltip("물들일 글자. 비우면 자식에서 찾는다.")]
    [SerializeField] TMP_Text label;

    [Header("상태별 색")]
    [Tooltip("평소. 잠긴 초록.")]
    [SerializeField] Color normal = new(0.120f, 0.469f, 0.120f, 1f);

    [Tooltip("커서를 올렸을 때. 형광으로 튄다.")]
    [SerializeField] Color hover = new(0.238f, 0.980f, 0.354f, 1f);

    [Tooltip("누르고 있을 때.")]
    [SerializeField] Color pressed = new(0.646f, 0.980f, 0.676f, 1f);

    [Tooltip("못 누르는 상태. 아래 Selectable 이 물려 있을 때만 쓰인다.")]
    [SerializeField] Color disabled = new(0.28f, 0.30f, 0.33f, 1f);

    [Tooltip("이 버튼이 지금 눌릴 수 있는지 판단할 대상. 비우면 항상 눌리는 것으로 본다.")]
    [SerializeField] Selectable selectable;

    [Header("포커스 표시")]
    [Tooltip("고르려는 상태일 때 글자 앞에 붙일 것. 비우면 안 붙인다.\n" +
             "터미널 메뉴라면 \"> \" 가 잘 맞는다 — 글자가 오른쪽으로 밀리면서 커서처럼 읽힌다.\n\n" +
             "＊ 가운데 정렬이면 글자가 좌우로 흔들린다. 왼쪽 정렬에 쓸 것.")]
    [SerializeField] string focusPrefix = "";

    [Tooltip("평소에도 접두사 길이만큼 공백을 넣어 자리를 비워둔다.\n\n" +
             "고정폭 글꼴에서는 \"> \" 와 \"  \" 의 폭이 같아서, 켜두면 글자가 전혀 움직이지 않는다.\n" +
             "끄면 포커스일 때 글자가 오른쪽으로 밀린다 — 그 움직임을 연출로 쓰고 싶을 때만 끌 것.")]
    [SerializeField] bool reservePrefixSpace = true;

    [Tooltip("포커스일 때만 켜둘 오브젝트. 화살표 스프라이트 등.\n" +
             "접두사와 달리 글자를 밀지 않아서 가운데 정렬에도 쓸 수 있다.")]
    [SerializeField] GameObject focusMarker;

    /// <summary>접두사가 붙기 전의 원래 글자.</summary>
    string baseText;

    [Header("소리")]
    [Tooltip("끄면 이 버튼만 조용해진다. 소리가 겹치는 자리에 쓴다.")]
    [SerializeField] bool playSounds = true;

    bool over;       // 마우스가 위에 있다
    bool selected;   // 키보드 포커스가 여기 있다
    bool down;

    /// <summary>
    /// 지금 이 버튼을 고르려 하는 상태인가.
    ///
    /// <b>둘을 더하지 않고 제어권이 있는 쪽만 본다.</b> 더하면 커서가 A 위에 있는 채로
    /// 방향키로 B 에 갔을 때 둘 다 밝아져서, Enter 를 누르면 어느 쪽이 눌릴지 알 수 없다.
    /// </summary>
    bool Focused => UiFocus.MouseDriving ? over : selected;

    // interactable 필드가 아니라 IsInteractable() — 부모 CanvasGroup 이 잠갔는지는 이쪽만 안다
    bool Interactable => selectable == null || selectable.IsInteractable();

    /// <summary>코드로 붙일 때 쓴다. 인스펙터로 물렸으면 부를 필요 없다.</summary>
    public void Bind(TMP_Text target, Selectable owner = null)
    {
        label = target;
        selectable = owner;

        CaptureBaseText();
        Apply();
    }

    /// <summary>글자를 코드로 바꿨으면 불러준다. 접두사 기준이 되는 원문을 다시 잡는다.</summary>
    public void RefreshText()
    {
        CaptureBaseText();
        Apply();
    }

    void CaptureBaseText()
    {
        if (label == null) return;

        baseText = label.text;

        if (string.IsNullOrEmpty(focusPrefix)) return;

        // 이미 우리가 만든 글자를 다시 원문으로 잡으면 "> > connect" 처럼 겹쳐 쌓인다.
        // 접두사든 그 자리를 비워둔 공백이든 걷어내고 원문만 남긴다
        if (baseText.StartsWith(focusPrefix))
            baseText = baseText.Substring(focusPrefix.Length);
        else if (reservePrefixSpace && baseText.StartsWith(Blank))
            baseText = baseText.Substring(focusPrefix.Length);
    }

    /// <summary>접두사가 들어갈 자리를 채우는 공백. 고정폭 글꼴이라 폭이 같다.</summary>
    string Blank => new(' ', focusPrefix.Length);

    /// <summary>
    /// interactable 을 바꾼 뒤 부른다.
    /// 색은 포인터가 드나들 때만 다시 계산되므로, 커서가 밖에 있으면 회색으로 안 바뀐다.
    /// </summary>
    public void Refresh() => Apply();

    void OnEnable()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>();

        // 화면이 다시 열릴 때 눌린 색이 남아 있지 않게 상태부터 비운다
        over = false;
        selected = false;
        down = false;

        CaptureBaseText();
        Apply();

        // 제어권이 바뀌면 보고 있어야 할 신호가 달라진다. 그때 색을 다시 칠한다
        UiFocus.DeviceChanged += Apply;
    }

    void OnDisable()
    {
        UiFocus.DeviceChanged -= Apply;

        UiFocus.ReportHover(gameObject, false);
    }

    // ── 마우스 ────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        UiFocus.ReportHover(gameObject, true);

        SetOver(true);

        // 커서를 올린 것을 곧바로 선택해 둔다. 그래야 화면에서 밝은 것과
        // Enter 가 누르는 것이 같아진다
        if (Interactable && selectable != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UiFocus.ReportHover(gameObject, false);

        // 누른 채로 빠져나가면 눌림도 함께 푼다. 안 그러면 밝은 색으로 굳는다
        down = false;
        SetOver(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        down = true;
        Apply();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        down = false;
        Apply();
    }

    public void OnPointerClick(PointerEventData eventData) => PlayClick();

    // ── 키보드 ────────────────────────────────────────────

    public void OnSelect(BaseEventData eventData) => SetSelected(true);

    public void OnDeselect(BaseEventData eventData)
    {
        down = false;
        SetSelected(false);
    }

    /// <summary>Enter·Space 로 눌렀을 때. 마우스 클릭과 달리 이쪽으로 온다.</summary>
    public void OnSubmit(BaseEventData eventData) => PlayClick();

    // ── 상태 ──────────────────────────────────────────────

    void SetOver(bool value)
    {
        if (over == value) return;

        bool was = Focused;
        over = value;

        Announce(was);
    }

    void SetSelected(bool value)
    {
        if (selected == value) return;

        bool was = Focused;
        selected = value;

        Announce(was);
    }

    /// <summary>
    /// 색을 다시 칠하고, 방금 "고르려는 상태"가 되었으면 소리를 낸다.
    ///
    /// 마우스와 키보드가 같은 버튼을 동시에 가리키는 경우가 흔하다 —
    /// 눌러서 포커스가 잡힌 채로 커서가 그 위에 있는 상태. 그때 두 번 울리지 않게
    /// <b>둘 중 하나라도 있었는가</b>를 기준으로 바뀐 순간만 잡는다.
    /// </summary>
    void Announce(bool wasFocused)
    {
        if (Focused && !wasFocused) Play(UiCue.Hover);

        Apply();
    }

    void PlayClick() => Play(Interactable ? UiCue.Click : UiCue.Denied);

    void Play(UiCue cue)
    {
        if (playSounds) UiSound.Play(cue);
    }

    void Apply()
    {
        if (label == null) return;

        bool marked = Focused && Interactable;

        if (focusMarker != null && focusMarker.activeSelf != marked)
            focusMarker.SetActive(marked);

        if (!string.IsNullOrEmpty(focusPrefix))
        {
            // 평소에도 같은 폭의 공백을 넣어두면 접두사가 붙어도 글자가 안 움직인다
            string want = marked ? focusPrefix + baseText
                        : reservePrefixSpace ? Blank + baseText
                        : baseText;

            // TMP 는 text 를 넣을 때마다 메시를 다시 만든다. 달라졌을 때만 넣는다
            if (label.text != want) label.text = want;
        }

        label.color = !Interactable ? disabled
                    : down          ? pressed
                    : Focused       ? hover
                    :                 normal;
    }
}
