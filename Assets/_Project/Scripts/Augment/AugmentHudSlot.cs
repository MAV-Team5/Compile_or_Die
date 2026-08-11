using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>증강 1개의 HUD 칸. 아이콘 · 쿨타임 · 레벨.</summary>
public class AugmentHudSlot : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] Image cooldownFill;
    [SerializeField] TMP_Text levelText;

    [Header("준비 완료 반짝임")]
    [SerializeField] float flashDuration = 0.15f;
    [SerializeField] float flashScale = 0.25f;

    AugmentRunner runner;
    float flashRemain;

    // 매 프레임 ToString() 하면 초당 수십 개 문자열이 GC로 간다
    int shownLevel = -1;

    public void Bind(AugmentRunner target)
    {
        // 다른 Runner로 갈아탈 때 옛 구독을 남기지 않는다
        if (runner != null) runner.BecameReady -= OnBecameReady;

        runner = target;
        runner.BecameReady += OnBecameReady;

        AugmentData data = runner.Instance.Data;
        icon.sprite  = data.icon;
        icon.enabled = data.icon != null;

        name = $"Slot_{data.name}";
    }

    void OnDestroy()
    {
        if (runner != null) runner.BecameReady -= OnBecameReady;
    }

    void OnBecameReady() => flashRemain = flashDuration;

    public void Refresh()
    {
        if (runner == null || runner.Instance == null) return;

        AugmentInstance inst = runner.Instance;
        TriggerModule trigger = inst.Data.trigger;

        float progress = trigger != null ? trigger.Progress(inst) : 1f;

        cooldownFill.fillAmount = 1f - progress;

        if (shownLevel != inst.Level)
        {
            shownLevel = inst.Level;
            levelText.text = shownLevel.ToString();
        }

        UpdateFlash();
    }

    void UpdateFlash()
    {
        if (flashRemain <= 0f)
        {
            transform.localScale = Vector3.one;
            return;
        }

        flashRemain -= Time.deltaTime;

        float t = Mathf.Clamp01(flashRemain / flashDuration);
        transform.localScale = Vector3.one * (1f + flashScale * t);
    }
}