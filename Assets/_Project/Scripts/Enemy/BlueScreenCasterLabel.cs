using TMPro;
using UnityEngine;

/// <summary>
/// 블루스크린 몬스터 머리 위에 뜨는 캐스트 단계 텍스트. 월드 스페이스라 카메라를 따로 안 만든다.
///
/// BlueScreenCaster 와 같은 오브젝트에 붙인다. 이벤트로만 연결돼 있어서
/// 텍스트 디자인(폰트·크기·위치)만 바꾸고 싶으면 이 파일만 건드리면 된다.
/// </summary>
[RequireComponent(typeof(BlueScreenCaster))]
public class BlueScreenCasterLabel : MonoBehaviour
{
    [SerializeField] Vector3 offset = new(0f, 1.2f, 0f);
    [SerializeField] float fontSize = 3.5f;
    [SerializeField] Color color = new(1f, 0.3f, 0.3f, 1f);

    BlueScreenCaster caster;
    TextMeshPro label;

    void Awake()
    {
        caster = GetComponent<BlueScreenCaster>();

        var go = new GameObject("CastLabel");
        go.layer = gameObject.layer; // 기본값(Default) 을 가지면 카메라 Culling Mask 에 따라 Game 뷰에서만 안 보일 수 있다
        go.transform.SetParent(transform, false);
        go.transform.localPosition = offset;

        label = go.AddComponent<TextMeshPro>();
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.text = string.Empty;

        // 정렬 레이어를 명시하지 않으면 기본값("Default", order 0)으로 렌더링되는데,
        // Sorting Layer 목록에서 "Default"가 몬스터 스프라이트보다 아래면 order 값과 무관하게 가려진다.
        // PlayerHealthBar 와 같은 이유로 "Effect" 레이어를 명시해 항상 위에 띄게 한다
        Renderer labelRenderer = label.GetComponent<Renderer>();
        labelRenderer.sortingLayerName = "Effect";
        labelRenderer.sortingOrder = 100;

        // 크기와 방향 보정은 LateUpdate 가 매 프레임 맡는다 —
        // Enemy.Init() 이 Awake 이후에 sizeScale 로 부모 스케일을 바꾸므로 여기서 고정하면 시점이 안 맞는다
    }

    void OnEnable() => caster.StageChanged += OnStageChanged;
    void OnDisable()
    {
        caster.StageChanged -= OnStageChanged;
        if (label != null) label.text = string.Empty;
    }

    void OnStageChanged(int stage, string text) => label.text = text;

    // 부모가 좌우 반전되거나(Enemy.flipToFace) 크기가 바뀌어도(sizeScale)
    // 텍스트는 항상 일정한 월드 크기와 정자세로 보이게, 부모 스케일을 매 프레임 역으로 상쇄한다
    void LateUpdate()
    {
        if (label == null || label.transform.parent == null) return;

        Vector3 parentScale = label.transform.parent.lossyScale;

        // 0으로 나누지 않게 방어. 극히 드물게나 발생하는 부모 스케일 0은 무시한다
        float px = Mathf.Abs(parentScale.x) > 0.0001f ? parentScale.x : 1f;
        float py = Mathf.Abs(parentScale.y) > 0.0001f ? parentScale.y : 1f;

        // 부모 스케일의 역수를 곱해서 월드 기준 크기를 항상 1로 고정한다.
        // X는 부호까지 반영해야 부모가 뒤집혔을 때 글자도 같이 뒤집히는 것을 막는다
        label.transform.localScale = new Vector3(1f / px, 1f / Mathf.Abs(py), 1f);
    }
}
