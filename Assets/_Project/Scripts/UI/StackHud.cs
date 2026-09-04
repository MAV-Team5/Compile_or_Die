using TMPro;
using UnityEngine;

/// <summary>
/// 스택 증강의 상태를 숫자로 보여준다. <c>STACK  47 / 80</c>
///
/// <b>왜 숫자인가</b> — 이 증강의 재미는 "경계를 얼마나 넘겼나" 다.
/// 막대 게이지는 꽉 차면 거기서 멈춰서 <c>130 / 80</c> 의 130 을 보여줄 수 없다.
/// 오버필한 만큼이 곧 보상인데 그게 안 보이면 유예 구간에 더 때릴 이유가 사라진다.
///
/// 단계마다 색이 바뀐다 — 유예는 붉게 깜빡이고, 되짚는 동안은 누적 피해를 센다.
/// 스택 증강을 안 뽑았으면 통째로 숨는다.
/// </summary>
public class StackHud : MonoBehaviour
{
    [Header("연결 (비우면 씬에서 찾는다)")]
    [SerializeField] Canvas canvas;
    [SerializeField] TMP_FontAsset font;

    [Tooltip("＊ 필수 — 어느 증강의 스택을 보여줄지.")]
    [SerializeField] AugmentData watch;

    [Header("배치")]
    [Tooltip("화면 아래쪽 중앙 기준 오프셋. 다른 HUD 와 안 겹치게 조정할 것.")]
    [SerializeField] Vector2 anchoredPosition = new(0f, 140f);

    [SerializeField] float fontSize = 34f;

    [Header("색")]
    [SerializeField] Color fillingColor = new(0.35f, 0.40f, 0.46f, 1f);
    [SerializeField] Color nearFullColor = new(1f, 0.78f, 0.30f, 1f);
    [SerializeField] Color graceColor = new(1f, 0.35f, 0.35f, 1f);
    [SerializeField] Color burstColor = new(0.30f, 0.82f, 0.88f, 1f);

    [Tooltip("경계의 이 비율을 넘으면 색이 바뀐다. 곧 넘친다는 예고.")]
    [Range(0.5f, 1f)] [SerializeField] float nearFullAt = 0.8f;

    [Tooltip("유예 중 깜빡이는 속도.")]
    [SerializeField] float blinkSpeed = 8f;

    TMP_Text label;
    AugmentRunner runner;
    float searchTimer;

    void Start()
    {
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            enabled = false;
            return;
        }

        if (font == null && UiTheme.Current != null) font = UiTheme.Current.mono;

        label = UiFactory.CreateText("StackHud", canvas.transform, font, fontSize,
                                     fillingColor, TextAlignmentOptions.Center);

        UiFactory.Place((RectTransform)label.transform,
                        new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        anchoredPosition, new Vector2(420f, 48f));

        label.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (label == null) return;

        AugmentRunner found = Resolve();

        if (found?.Instance == null || !found.Instance.TryGetShared(out StackState s))
        {
            label.gameObject.SetActive(false);
            return;
        }

        label.gameObject.SetActive(true);

        int capacity = Capacity(found.Instance);

        switch (s.Now)
        {
            case StackState.Phase.Grace:
                // 깜빡임으로 "지금 더 때려라" 를 알린다. 숫자는 경계를 넘겨서 보여준다
                label.text = $"STACK  {s.Frames.Count} / {capacity}";
                label.color = Color.Lerp(graceColor, Color.white,
                                         Mathf.PingPong(Time.time * blinkSpeed, 1f));
                break;

            case StackState.Phase.Unwinding:
                label.text = $"UNWINDING  {s.Frames.Count}   +{s.BurstDamage:N0}";
                label.color = burstColor;
                break;

            case StackState.Phase.Cooldown:
                label.text = "STACK  reallocating...";
                label.color = fillingColor;
                break;

            default:
                label.text = $"STACK  {s.Frames.Count} / {capacity}";
                label.color = s.Frames.Count >= capacity * nearFullAt
                    ? nearFullColor
                    : fillingColor;
                break;
        }
    }

    /// <summary>경계는 트리거가 안다. 여기서 다시 계산하면 두 곳이 어긋난다.</summary>
    static int Capacity(AugmentInstance instance)
    {
        // 트리거가 StackTrigger 가 아니면 경계를 알 길이 없다 — 쌓인 수만 보여준다
        return instance.Build.Trigger is StackTrigger ? StackCapacityOf(instance) : 0;
    }

    static int StackCapacityOf(AugmentInstance instance)
        => Mathf.Max(1, instance.Stat.depth);

    AugmentRunner Resolve()
    {
        if (runner != null && runner.Instance != null) return runner;

        if (watch == null || AugmentManager.Current == null) return null;

        searchTimer -= Time.deltaTime;
        if (searchTimer > 0f) return null;

        searchTimer = 0.5f;
        runner = AugmentManager.Current.Find(watch);

        return runner;
    }
}
