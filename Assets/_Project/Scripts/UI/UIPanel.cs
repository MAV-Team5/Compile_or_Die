using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 개별 UI 를 열고 닫는다. 더불어 <b>키보드가 이 패널 안에서만 돌게</b> 만든다.
///
/// 방향키 이동과 Enter 는 Unity 가 이미 해준다. 다만 두 가지가 빠져 있다.
///
/// <list type="number">
/// <item><b>시작점</b> — 아무것도 선택돼 있지 않으면 방향키는 어디서 움직일지 몰라 아무 일도 안 한다</item>
/// <item><b>경계</b> — 내비게이션은 패널이라는 개념을 모른다. 캔버스에 켜져 있는 모든 버튼이 후보라
/// 배경 메뉴로 방향키가 새어 나간다</item>
/// </list>
///
/// 그 둘을 여기서 메운다.
/// </summary>
public class UIPanel : MonoBehaviour
{
    [Tooltip("열릴 때 키보드가 시작할 버튼. 비우면 맨 처음 눌릴 수 있는 것을 찾는다.")]
    [SerializeField] Selectable firstSelected;

    [Tooltip("끄면 이 패널은 키보드를 신경 쓰지 않는다.")]
    [SerializeField] bool takeFocus = true;

    [Tooltip("열려 있는 동안 형제 오브젝트들의 상호작용을 끈다.\n\n" +
             "이걸 꺼두면 방향키가 배경 메뉴로 빠져나가고, 뒤에 있는 버튼이 마우스로도 눌린다.\n" +
             "배경처럼 늘 떠 있는 묶음에는 꺼둘 것.")]
    [SerializeField] bool modal = true;

    /// <summary>이 패널이 열리기 직전에 골라져 있던 것. 닫을 때 여기로 되돌린다.</summary>
    GameObject previous;

    /// <summary>내가 잠근 것들. 내가 잠근 것만 푼다 — 원래 잠겨 있던 것은 그대로 둔다.</summary>
    readonly List<CanvasGroup> blocked = new();

    // ── 열고 닫기 ─────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        UiSound.Play(UiCue.Open);
    }

    public void Close()
    {
        UiSound.Play(UiCue.Close);
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (modal) Block();

        if (!takeFocus) return;

        previous = Current != null ? Current.currentSelectedGameObject : null;

        // 여기서 바로 고르지 않는다. 마우스로만 쓰는 사람에게 키보드 커서를 띄우면
        // 뭘 눌러야 하는 건지 헷갈린다 — 방향키를 실제로 누를 때 나타난다
    }

    void OnDisable()
    {
        Unblock();

        if (!takeFocus || Current == null) return;

        // 되돌릴 곳이 이미 사라졌거나 꺼져 있으면 그냥 비운다 —
        // 없는 오브젝트를 고르면 그때부터 방향키가 통째로 먹통이 된다
        bool alive = previous != null && previous.activeInHierarchy;

        Current.SetSelectedGameObject(alive ? previous : null);

        previous = null;
    }

    // ── 키보드가 처음 들어올 때 ───────────────────────────

    /// <summary>
    /// 방향키가 눌렸는데 아무것도 안 골라져 있으면 그때 첫 버튼을 잡는다.
    ///
    /// 여러 패널이 동시에 켜져 있어도 다투지 않는다. 모달이 배경을 잠가두므로
    /// 잠긴 쪽은 눌릴 수 있는 버튼이 없어 스스로 아무것도 고르지 않기 때문.
    /// </summary>
    void Update()
    {
        if (takeFocus) UiFocus.Tick(transform, firstSelected);
    }

    // ── 모달 ──────────────────────────────────────────────

    /// <summary>
    /// 형제 오브젝트들을 잠근다. 패널들이 배경 메뉴와 나란히 놓여 있는 구조를 그대로 쓴다.
    ///
    /// <c>interactable</c> 은 내비게이션과 클릭을, <c>blocksRaycasts</c> 는 마우스 호버까지 막는다.
    /// 둘 다 꺼야 뒤쪽 버튼에 커서가 닿아도 색이 안 변한다.
    /// </summary>
    void Block()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform sibling = parent.GetChild(i);

            if (sibling == transform || !sibling.gameObject.activeSelf) continue;

            if (!sibling.TryGetComponent(out CanvasGroup group))
                group = sibling.gameObject.AddComponent<CanvasGroup>();

            // 이미 잠겨 있으면 남의 것이다. 건드리면 그쪽이 닫힐 때 이상해진다
            if (!group.interactable) continue;

            group.interactable = false;
            group.blocksRaycasts = false;

            blocked.Add(group);
        }
    }

    void Unblock()
    {
        for (int i = 0; i < blocked.Count; i++)
        {
            if (blocked[i] == null) continue;

            blocked[i].interactable = true;
            blocked[i].blocksRaycasts = true;
        }

        blocked.Clear();
    }

    static EventSystem Current => EventSystem.current;
}
