using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 증강 1개의 HUD 칸. 아이콘 · 쿨타임 · 레벨.
/// 마우스를 올리면 <see cref="AugmentTooltip"/> 이 설명을 띄운다.
/// </summary>
public class AugmentHudSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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

    void Awake()
    {
        // 마우스를 받으려면 칸 전체를 덮는 판정면이 있어야 한다.
        // 아이콘만 판정면이면 빈 구석에서는 툴팁이 안 뜬다
        if (GetComponent<Graphic>() == null)
        {
            var hit = gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;

            // 아이콘·쿨타임 위로 올라오면 그림을 가린다. 맨 뒤로 보낸다
            hit.rectTransform.SetAsFirstSibling();
        }
    }

    public void OnPointerEnter(PointerEventData _) => AugmentTooltip.Show(runner, (RectTransform)transform);

    public void OnPointerExit(PointerEventData _) => AugmentTooltip.Hide(runner);

    void OnDisable() => AugmentTooltip.Hide(runner);

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

        AugmentTooltip.Hide(runner);
    }

    void OnBecameReady() => flashRemain = flashDuration;

    public void Refresh()
    {
        if (runner == null || runner.Instance == null) return;

        AugmentInstance inst = runner.Instance;
        // 에셋이 아니라 Build 를 본다. 내부 증강이 트리거를 갈아끼웠으면 게이지도 그쪽을 따라야 한다
        TriggerModule trigger = inst.Build.Trigger;

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