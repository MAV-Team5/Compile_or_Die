using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 커서를 올리거나 키보드로 고르면 커진다. 눌릴 수 없는 상태면 커지지 않는다.
/// 선택 화면은 timeScale 이 0이라 실제 시간으로 따라간다.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class SelectableHoverScale : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Range(1f, 1.5f)] public float scale = 1.2f;
    [Min(1f)] public float speed = 16f;

    Selectable selectable;
    bool over;
    bool selected;

    bool Focused => (over || selected) && selectable.IsInteractable();

    void Awake() => selectable = GetComponent<Selectable>();

    void OnDisable()
    {
        over = false;
        selected = false;
        transform.localScale = Vector3.one;
    }

    void Update()
    {
        float target = Focused ? scale : 1f;

        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target,
                                            1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData eventData) => over = true;
    public void OnPointerExit(PointerEventData eventData) => over = false;
    public void OnSelect(BaseEventData eventData) => selected = true;
    public void OnDeselect(BaseEventData eventData) => selected = false;
}
