using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 블루스크린 발동 시 화면 전체에 깜빡이는 "REBOOTING..." 연출.
///
/// PlayerRebootController.Triggered 구독 없이 BlueScreenGauge.Triggered 를 직접 듣는다 —
/// 재부팅 로직(이동 반전)과 화면 연출을 분리해서, 나중에 연출만 갈아끼우기 쉽게 한다.
/// </summary>
public class BlueScreenOverlay : MonoBehaviour
{
    [Header("연결 (비우면 씬에서 찾는다)")]
    [SerializeField] Canvas canvas;
    [SerializeField] TMP_FontAsset font;
    [SerializeField] PlayerRebootController reboot;

    [Header("연출")]
    [SerializeField] Color tintColor = new(0f, 0.1f, 0.6f, 0.35f);
    [Tooltip("깜빡이는 주기(초). 값이 작을수록 빠르게 깜빡인다.")]
    [SerializeField] float flickerSpeed = 6f;
    [SerializeField] string message = "REBOOTING...";

    Image tint;
    TMP_Text label;
    Coroutine playRoutine;

    void Start()
    {
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (reboot == null) reboot = FindAnyObjectByType<PlayerRebootController>();

        if (canvas == null || BlueScreenGauge.Instance == null)
        {
            Debug.LogWarning("[BlueScreenOverlay] Canvas 나 BlueScreenGauge 를 못 찾았다.", this);
            enabled = false;
            return;
        }

        Build();
        BlueScreenGauge.Instance.Triggered += HandleTriggered;
    }

    void OnDestroy()
    {
        if (BlueScreenGauge.Instance != null)
            BlueScreenGauge.Instance.Triggered -= HandleTriggered;
    }

    void Build()
    {
        tint = UiFactory.CreateImage("BlueScreenTint", canvas.transform, tintColor);
        var tintRect = (RectTransform)tint.transform;
        UiFactory.Stretch(tintRect, Vector2.zero, Vector2.one);
        tintRect.SetAsLastSibling();

        label = UiFactory.CreateText("BlueScreenLabel", tint.transform, font,
                                     140f, Color.white, TextAlignmentOptions.Center);
        UiFactory.Stretch((RectTransform)label.transform, Vector2.zero, Vector2.one);
        label.text = message;

        tint.gameObject.SetActive(false);
    }

    void HandleTriggered()
    {
        if (playRoutine != null) StopCoroutine(playRoutine);

        // 재부팅 시간과 연출 시간을 동일하게 맞춘다. 컨트롤러가 없으면 기본 3초
        float duration = reboot != null ? reboot.RebootDuration : 3f;
        playRoutine = StartCoroutine(Play(duration));
    }

    IEnumerator Play(float duration)
    {
        tint.gameObject.SetActive(true);
        tint.transform.SetAsLastSibling();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // 사인파로 깜빡이는 알파 — 시스템 오류 특유의 지지직거리는 느낌
            float flicker = 0.5f + 0.5f * Mathf.Sin(elapsed * flickerSpeed);
            Color c = tintColor;
            c.a = tintColor.a * Mathf.Lerp(0.4f, 1f, flicker);
            tint.color = c;

            yield return null;
        }

        tint.gameObject.SetActive(false);
        playRoutine = null;
    }
}
