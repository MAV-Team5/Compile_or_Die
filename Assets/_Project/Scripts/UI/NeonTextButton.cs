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
/// </summary>
public class NeonTextButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
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

    bool over;
    bool down;

    /// <summary>코드로 붙일 때 쓴다. 인스펙터로 물렸으면 부를 필요 없다.</summary>
    public void Bind(TMP_Text target, Selectable owner = null)
    {
        label = target;
        selectable = owner;
        Apply();
    }

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
        down = false;

        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        over = true;
        Apply();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 누른 채로 빠져나가면 눌림도 함께 푼다. 안 그러면 밝은 색으로 굳는다
        over = false;
        down = false;
        Apply();
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

    void Apply()
    {
        if (label == null) return;

        if (selectable != null && !selectable.interactable)
        {
            label.color = disabled;
            return;
        }

        label.color = down ? pressed : over ? hover : normal;
    }
}
