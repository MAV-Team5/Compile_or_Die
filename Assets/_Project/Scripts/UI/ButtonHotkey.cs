using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 버튼에 단축키를 단다. 키를 누르면 Enter 로 누른 것과 똑같이 눌린다 — 눌림 색 변화와 OnClick 모두.
/// 버튼이 꺼져 있거나 잠겨 있으면(interactable 해제, 부모 CanvasGroup 잠금 포함) 반응하지 않는다.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonHotkey : MonoBehaviour
{
    [Tooltip("이 중 하나라도 누르면 버튼이 눌린다.")]
    [SerializeField] Key[] keys = { Key.R };

    [Tooltip("화면이 켜진 직후 이 시간(실제 초) 동안은 키를 무시한다.\n" +
             "결과 화면이 뜨는 순간 전투 중에 연타하던 키가 그대로 들어가는 것을 막는다.")]
    [Min(0f)] [SerializeField] float ignoreAfterEnable = 0.5f;

    Button button;
    float enabledAt;

    void Awake() => button = GetComponent<Button>();

    void OnEnable() => enabledAt = Time.unscaledTime;

    void Update()
    {
        if (Time.unscaledTime - enabledAt < ignoreAfterEnable) return;
        if (!button.IsInteractable() || !Pressed(out bool submitKey)) return;

        EventSystem system = EventSystem.current;

        // Space·Enter 는 골라져 있는 버튼도 Submit 으로 받는다. 다른 버튼이 골라져 있으면
        // 그쪽이 눌리게 두고 여기서는 물러난다 — 안 그러면 버튼 두 개가 동시에 눌린다
        if (submitKey && system != null && system.currentSelectedGameObject != null &&
            system.currentSelectedGameObject != gameObject) return;

        if (system != null)
            ExecuteEvents.Execute(gameObject, new BaseEventData(system), ExecuteEvents.submitHandler);
        else
            button.onClick.Invoke();
    }

    bool Pressed(out bool submitKey)
    {
        submitKey = false;

        Keyboard kb = Keyboard.current;
        if (kb == null) return false;

        foreach (Key key in keys)
        {
            if (key == Key.None || !kb[key].wasPressedThisFrame) continue;

            submitKey = key == Key.Space || key == Key.Enter || key == Key.NumpadEnter;
            return true;
        }

        return false;
    }
}
