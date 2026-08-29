using UnityEngine;

/// <summary>
/// 피격을 눈에 보이게 만드는 곳. 플레이어 오브젝트에 붙인다.
///
/// <b>왜 캐릭터를 물들이지 않는가</b> — 뱀서라이크는 적과 겹친 채로 있는 것이 기본이라
/// 피해가 매 물리 프레임 들어온다. "맞으면 0.12초 붉게" 같은 타이머 방식은
/// 겹쳐 있는 동안 타이머가 계속 갱신되어 <b>영원히 붉은 상태</b>가 된다.
/// 그래서 밝기를 타이머가 아니라 사인파로 만든다 — 계속 맞아도 오르내리므로
/// "지금 맞고 있다"가 계속 읽히고, 켜진 채로 굳지 않는다.
///
/// 대상도 캐릭터 스프라이트가 아니라 뒤에 깔린 광원이다. 캐릭터 색을 안 건드리므로
/// 애니메이션이나 다른 연출과 서로를 덮어쓰지 않는다.
/// </summary>
public class PlayerHitFeedback : MonoBehaviour
{
    [Header("광원")]
    [Tooltip("캐릭터 뒤에 깔 스프라이트. 비워두면 광원 없이 애니메이션만 나간다.\n" +
             "정렬 순서를 캐릭터(본체 10 · 배경 2)보다 낮게 둘 것.")]
    [SerializeField] SpriteRenderer glow;

    [SerializeField] Color color = new(1f, 0.25f, 0.25f);

    [Tooltip("가장 어두울 때 / 가장 밝을 때의 투명도.")]
    [Range(0f, 1f)] [SerializeField] float minAlpha = 0.15f;
    [Range(0f, 1f)] [SerializeField] float maxAlpha = 0.85f;

    [Tooltip("초당 깜빡이는 횟수.")]
    [Min(0.1f)] [SerializeField] float pulsesPerSecond = 6f;

    [Header("지속")]
    [Tooltip("마지막 피격 뒤에도 이만큼은 계속 깜빡인다. 짧으면 한 대 맞은 게 안 보인다.")]
    [Min(0f)] [SerializeField] float holdTime = 0.25f;

    [Tooltip("그 뒤 서서히 꺼지는 시간. 0이면 즉시 꺼진다.")]
    [Min(0f)] [SerializeField] float fadeTime = 0.35f;

    [Header("애니메이션")]
    [Tooltip("비우면 애니메이션은 건드리지 않는다.")]
    [SerializeField] string hitParameter = "Stop";

    [Tooltip("피해는 매 물리 프레임 들어오므로 그대로 쏘면 초당 50번 걸린다.\n" +
             "이 간격보다 자주는 다시 발동하지 않는다 — 광원은 계속 깜빡이되\n" +
             "애니메이션만 일정 간격으로 튄다.")]
    [Min(0f)] [SerializeField] float retriggerInterval = 0.6f;

    [Tooltip("비우면 같은 오브젝트에서 찾는다.")]
    [SerializeField] AnimatorDriver animatorDriver;

    /// <summary>남은 유지 시간. 이게 있는 동안은 사인파로 깜빡인다.</summary>
    float hold;

    /// <summary>전체 밝기 배율. 유지 중에는 1, 끝나면 0으로 내려간다.</summary>
    float level;

    float triggerCooldown;

    void Awake()
    {
        if (animatorDriver == null) animatorDriver = GetComponent<AnimatorDriver>();

        Hide();
    }

    /// <summary>피격 때마다 부른다. 매 물리 프레임 불려도 괜찮게 만들어져 있다.</summary>
    public void Hit()
    {
        hold = holdTime;
        level = 1f;

        if (triggerCooldown > 0f) return;

        triggerCooldown = retriggerInterval;

        if (animatorDriver != null && !string.IsNullOrEmpty(hitParameter))
            animatorDriver.SetMotion(hitParameter, 1f);
    }

    void Update()
    {
        if (triggerCooldown > 0f) triggerCooldown -= Time.deltaTime;

        if (hold > 0f) hold -= Time.deltaTime;
        else if (level > 0f) level = fadeTime <= 0f ? 0f : Mathf.Max(0f, level - Time.deltaTime / fadeTime);

        Draw();
    }

    void Draw()
    {
        if (glow == null) return;

        if (level <= 0f)
        {
            Hide();
            return;
        }

        // 사인파를 0~1로 옮겨 담는다. 유지가 끝나면 level 이 내려가면서 통째로 어두워진다
        float wave = (Mathf.Sin(Time.time * pulsesPerSecond * Mathf.PI * 2f) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, wave) * level;

        glow.color = new Color(color.r, color.g, color.b, alpha);

        if (!glow.enabled) glow.enabled = true;
    }

    void Hide()
    {
        if (glow != null) glow.enabled = false;
    }
}
