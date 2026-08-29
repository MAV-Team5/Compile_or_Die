using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 소리만 담당하는 조각. 슬라이더 · 토글 · 드롭다운처럼
/// <see cref="NeonTextButton"/> 을 쓸 수 없는 위젯에 붙인다.
///
/// <b>색은 건드리지 않는다.</b> 내장 위젯은 ColorTint 가 이미 색을 맡고 있어서,
/// 여기서까지 칠하면 둘이 서로를 덮어쓴다. 이 컴포넌트는 소리만 낸다.
///
/// 마우스 호버와 키보드 포커스를 같은 사건으로 보는 규칙은 <see cref="NeonTextButton"/> 과 같다 —
/// 제어권이 있는 장치의 신호만 보므로 두 번 울리지 않는다.
/// </summary>
[DisallowMultipleComponent]
public class UiSoundHook : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler,
    IPointerClickHandler, ISubmitHandler
{
    [Tooltip("커서를 올리거나 키보드 포커스가 왔을 때.")]
    [SerializeField] UiCue hoverCue = UiCue.Hover;

    [Tooltip("눌렀을 때. 슬라이더처럼 누르는 개념이 없으면 신경 안 써도 된다.")]
    [SerializeField] UiCue clickCue = UiCue.Click;

    [Tooltip("값이 바뀔 때도 소리를 낸다. 슬라이더를 끌 때 이게 없으면 반응이 없는 것처럼 느껴진다.\n\n" +
             "＊ 효과음 슬라이더에서는 이 소리가 방금 정한 크기로 나므로,\n" +
             "  얼마로 맞춘 것인지 귀로 바로 확인된다.")]
    [SerializeField] bool playOnValueChange = true;

    [SerializeField] UiCue valueCue = UiCue.Tick;

    Selectable selectable;

    bool over;
    bool selected;

    /// <summary>제어권이 있는 장치의 신호만 본다. NeonTextButton 과 같은 규칙.</summary>
    bool Focused => UiFocus.MouseDriving ? over : selected;

    bool Interactable => selectable == null || selectable.IsInteractable();

    void OnEnable()
    {
        selectable = GetComponent<Selectable>();

        over = false;
        selected = false;

        if (playOnValueChange) Listen(true);
    }

    void OnDisable()
    {
        UiFocus.ReportHover(gameObject, false);

        if (playOnValueChange) Listen(false);
    }

    // ── 값 바뀜 ───────────────────────────────────────────

    /// <summary>
    /// 어떤 위젯인지에 따라 이벤트가 다르다. 셋 중 붙어 있는 것 하나만 듣는다.
    ///
    /// 너무 자주 울리는 것은 여기서 막지 않는다 — 뱅크의 최소 간격이 맡는다.
    /// 그래야 소리마다 다른 간격을 줄 수 있다.
    /// </summary>
    void Listen(bool on)
    {
        if (TryGetComponent(out Slider slider))
        {
            if (on) slider.onValueChanged.AddListener(OnFloat);
            else slider.onValueChanged.RemoveListener(OnFloat);
            return;
        }

        if (TryGetComponent(out Toggle toggle))
        {
            if (on) toggle.onValueChanged.AddListener(OnBool);
            else toggle.onValueChanged.RemoveListener(OnBool);
            return;
        }

        if (TryGetComponent(out TMP_Dropdown dropdown))
        {
            if (on) dropdown.onValueChanged.AddListener(OnInt);
            else dropdown.onValueChanged.RemoveListener(OnInt);
        }
    }

    void OnFloat(float _) => Play(valueCue);
    void OnBool(bool _) => Play(valueCue);
    void OnInt(int _) => Play(valueCue);

    // ── 마우스 · 키보드 ───────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        UiFocus.ReportHover(gameObject, true);
        SetOver(true);

        // 커서를 올린 것을 선택해 둔다. 화면에서 밝은 것과 Enter 가 누르는 것을 맞춘다
        if (Interactable && selectable != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UiFocus.ReportHover(gameObject, false);
        SetOver(false);
    }

    public void OnSelect(BaseEventData eventData) => SetSelected(true);
    public void OnDeselect(BaseEventData eventData) => SetSelected(false);

    public void OnPointerClick(PointerEventData eventData) => Click();
    public void OnSubmit(BaseEventData eventData) => Click();

    void SetOver(bool value)
    {
        if (over == value) return;

        bool was = Focused;
        over = value;

        if (Focused && !was) Play(hoverCue);
    }

    void SetSelected(bool value)
    {
        if (selected == value) return;

        bool was = Focused;
        selected = value;

        if (Focused && !was) Play(hoverCue);
    }

    /// <summary>
    /// 슬라이더는 누를 때마다 값도 바뀌므로 클릭음과 틱이 겹친다.
    /// 값 소리를 쓰는 위젯에서는 클릭음을 내지 않는다.
    /// </summary>
    void Click()
    {
        if (playOnValueChange) return;

        Play(Interactable ? clickCue : UiCue.Denied);
    }

    void Play(UiCue cue) => UiSound.Play(cue);
}
