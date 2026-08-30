using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 지금 UI 를 <b>무엇으로 조작하고 있는지</b>를 한 곳에서 정한다.
///
/// <b>왜 필요한가</b> — 마우스 호버와 키보드 포커스는 서로를 모른다.
/// 각자 켜지게 두면 커서가 A 위에 있는 채로 방향키로 B 에 가면 둘 다 밝아지고,
/// 그때 Enter 를 누르면 어느 쪽이 눌릴지 화면만 봐서는 알 수 없다.
///
/// 그래서 <b>마지막에 움직인 쪽이 제어권을 갖는다.</b>
/// 마우스를 움직이면 키보드 커서가 꺼지고, 방향키를 누르면 마우스 호버가 꺼진다.
/// 버튼은 <see cref="MouseDriving"/> 을 보고 자기가 어느 신호를 봐야 할지 정한다.
///
/// 씬에 아무것도 놓을 필요가 없다 — 처음 쓰일 때 숨은 오브젝트 하나가 알아서 생긴다.
/// </summary>
public static class UiFocus
{
    public enum Device { Mouse, Keyboard }

    /// <summary>마지막으로 조작한 장치.</summary>
    public static Device Active { get; private set; } = Device.Mouse;

    public static bool MouseDriving => Active == Device.Mouse;

    /// <summary>제어권이 바뀌었다. 버튼들이 색을 다시 칠하려고 듣는다.</summary>
    public static event System.Action DeviceChanged;

    /// <summary>지금 커서가 올라가 있는 것. 제어권이 마우스로 넘어올 때 이쪽을 선택한다.</summary>
    static GameObject hovered;

    // ── 버튼이 알려주는 것 ────────────────────────────────

    /// <summary>커서가 들어오거나 나갔다. NeonTextButton · AugmentCardView 가 부른다.</summary>
    public static void ReportHover(GameObject target, bool entered)
    {
        if (entered) hovered = target;
        else if (hovered == target) hovered = null;
    }

    // ── 장치 판정 ─────────────────────────────────────────

    /// <summary>숨은 감시자가 매 프레임 부른다.</summary>
    internal static void Poll()
    {
        // 방향키가 우선이다. 같은 프레임에 둘 다 움직였다면 의도가 분명한 쪽은 키 입력이다
        if (NavigatePressed()) Switch(Device.Keyboard);
        else if (MouseMoved()) Switch(Device.Mouse);
    }

    static void Switch(Device device)
    {
        if (Active == device) return;

        Active = device;

        if (device == Device.Mouse) TakeOverWithMouse();

        DeviceChanged?.Invoke();
    }

    /// <summary>
    /// 제어권이 마우스로 넘어왔다. 커서가 올라가 있는 것을 선택해 Enter 와 화면을 맞춘다.
    ///
    /// 빈 곳을 가리키고 있으면 선택을 비운다 — 아무것도 안 밝은데 Enter 는 먹는 상태가
    /// 제일 헷갈리기 때문.
    /// </summary>
    static void TakeOverWithMouse()
    {
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(hovered);
    }

    static bool MouseMoved()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return false;

        if (mouse.leftButton.wasPressedThisFrame) return true;

        // 아주 미세한 떨림으로 제어권이 넘어가면 키보드로 조작하다 신경질이 난다
        return mouse.delta.ReadValue().sqrMagnitude > 1f;
    }

    /// <summary>이번 프레임에 방향키(또는 스틱)가 움직였는가.</summary>
    public static bool NavigatePressed(EventSystem system = null)
    {
        system ??= EventSystem.current;
        if (system == null) return false;

        // 이 프로젝트의 씬은 전부 InputSystemUIInputModule 을 쓴다.
        // 다른 모듈이면 조용히 false — 키보드가 안 될 뿐 게임은 돈다
        if (system.currentInputModule is not InputSystemUIInputModule module) return false;
        if (module.move == null || module.move.action == null) return false;

        return module.move.action.triggered;
    }

    // ── 첫 진입 ───────────────────────────────────────────

    /// <summary>
    /// 방향키가 눌렸는데 아무것도 안 골라져 있으면 <paramref name="root"/> 안의 첫 버튼을 잡는다.
    ///
    /// 화면이 열리자마자 잡지 않는 이유는 마우스로만 쓰는 사람 때문이다 —
    /// 안 쓸 커서를 띄워두면 그게 이미 골라진 것인지 헷갈린다.
    /// 매 프레임 불러도 되게 만들어져 있다.
    /// </summary>
    public static void Tick(Transform root, Selectable preferred = null)
    {
        EventSystem system = EventSystem.current;

        if (system == null || root == null) return;
        if (system.currentSelectedGameObject != null) return;
        if (!NavigatePressed(system)) return;

        Selectable target = FindFirst(root, preferred);
        if (target == null) return;

        system.SetSelectedGameObject(target.gameObject);
    }

    /// <summary>지정한 것이 쓸 만하면 그것, 아니면 자식 중 눌릴 수 있는 첫 번째.</summary>
    public static Selectable FindFirst(Transform root, Selectable preferred = null)
    {
        if (Usable(preferred)) return preferred;
        if (root == null) return null;

        // 꺼진 것은 빼고 걷는다. 화면에 없는 버튼을 고르면 아무 표시도 안 보인다
        Selectable[] all = root.GetComponentsInChildren<Selectable>(false);

        for (int i = 0; i < all.Length; i++)
            if (Usable(all[i])) return all[i];

        return null;
    }

    /// <summary>
    /// <c>interactable</c> 필드가 아니라 <c>IsInteractable()</c> 을 봐야 한다 —
    /// 필드는 자기 값만 알고, 부모 CanvasGroup 이 잠갔는지는 이 메서드만 안다.
    /// </summary>
    public static bool Usable(Selectable s)
        => s != null && s.isActiveAndEnabled && s.IsInteractable();

    // ── 숨은 감시자 ───────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        // 씬마다 컴포넌트를 놓게 하면 하나 빠뜨렸을 때 그 씬만 조용히 안 된다.
        // 게임이 시작될 때 스스로 하나 만들고 씬을 넘어가도 살아남는다
        Active = Device.Mouse;
        hovered = null;

        var go = new GameObject("UiFocusDriver") { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(go);
        go.AddComponent<UiFocusDriver>();
    }
}

/// <summary><see cref="UiFocus"/> 를 매 프레임 깨우기만 하는 껍데기.</summary>
public class UiFocusDriver : MonoBehaviour
{
    void Update() => UiFocus.Poll();
}
