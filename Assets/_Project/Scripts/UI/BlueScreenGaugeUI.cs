using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 상단 블루스크린 게이지. 칸으로 나뉘지 않은 <b>순수 연속 바 하나</b>로만 그린다 —
/// 구분선조차 없다. BlueScreenGauge.Progress(0~1)를 그대로 반영하기만 하면 되고,
/// 진행도 자체가 이미 시간에 따라 매끄럽게 늘어나므로 여기서 따로 보간할 필요도 없다.
///
/// <b>표시 조건</b> — BlueScreenGauge.AnyoneInRange 가 false 면 숨긴다.
///
/// 모듈식: 크기·색·번쩍임 속도는 인스펙터에서 조절. GameHud 와 별개 컴포넌트.
/// </summary>
public class BlueScreenGaugeUI : MonoBehaviour
{
    [Header("연결 (비우면 씬에서 찾는다)")]
    [SerializeField] Canvas canvas;

    [Header("배치")]
    [Tooltip("화면 위쪽 중앙 기준 오프셋. GameHud 의 타이머와 안 겹치게 y를 조정할 것.")]
    [SerializeField] Vector2 anchoredPosition = new(0f, -190f);
    [SerializeField] Vector2 barSize = new(460f, 30f);

    [Header("색")]
    [SerializeField] Color emptyColor = new(0.85f, 0.85f, 0.85f, 0.35f);
    [SerializeField] Color fillColor = new(0.9f, 0.15f, 0.15f, 1f);
    [SerializeField] Color flashColor = new(1f, 0.9f, 0.3f, 1f);

    [Header("번쩍임 — 다 차기 직전 경고")]
    [Range(0f, 1f)][SerializeField] float warnThreshold = 0.8f;
    [SerializeField] float warnPulseSpeed = 6f;

    [Header("번쩍임 — 몬스터가 캐스트 완주할 때마다")]
    [SerializeField] float flashDuration = 0.25f;

    RectTransform root;
    Image fillImage;
    BlueScreenGauge gauge;
    float flashTimer;

    void Start()
    {
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        gauge = BlueScreenGauge.Instance;

        if (canvas == null || gauge == null)
        {
            Debug.LogWarning("[BlueScreenGaugeUI] Canvas 나 BlueScreenGauge 를 못 찾았다.", this);
            enabled = false;
            return;
        }

        Build();
        gauge.CastRegistered += OnCastRegistered;
    }

    void OnDestroy()
    {
        if (gauge != null) gauge.CastRegistered -= OnCastRegistered;
    }

    void Build()
    {
        root = UiFactory.CreateRect("BlueScreenGauge", canvas.transform);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = anchoredPosition;
        root.sizeDelta = barSize;

        Image back = UiFactory.CreateImage("Back", root, emptyColor);
        UiFactory.Stretch((RectTransform)back.transform, Vector2.zero, Vector2.one);

        fillImage = UiFactory.CreateImage("Fill", root, fillColor);
        UiFactory.Stretch((RectTransform)fillImage.transform, Vector2.zero, new Vector2(0f, 1f));

        root.gameObject.SetActive(false);
    }

    void Update()
    {
        if (root == null) return;

        bool show = gauge.AnyoneInRange;
        if (root.gameObject.activeSelf != show)
            root.gameObject.SetActive(show);

        if (!show) return;

        // Progress 자체가 이미 시간에 따라 매끄럽게 늘어나는 값이라 여기서 또 보간할 필요가 없다.
        // 그대로 반영만 하면 물 차오르듯 자연스럽게 움직인다
        fillImage.rectTransform.anchorMax = new Vector2(gauge.Progress, 1f);
        fillImage.color = ComputeColor();

        if (flashTimer > 0f) flashTimer -= Time.deltaTime;
    }

    Color ComputeColor()
    {
        if (flashTimer > 0f)
        {
            float t = flashTimer / flashDuration;
            return Color.Lerp(fillColor, flashColor, t);
        }

        if (gauge.Progress >= warnThreshold)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * warnPulseSpeed);
            return Color.Lerp(fillColor, flashColor, pulse * 0.5f);
        }

        return fillColor;
    }

    void OnCastRegistered() => flashTimer = flashDuration;
}
