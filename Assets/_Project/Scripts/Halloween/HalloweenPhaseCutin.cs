using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 1→2페이즈 사이 컷인. 세이렌의 화난 얼굴(빨간 눈)이 지지직거리며 화면에 크게 박혔다가 깨지며 사라진다.
///
/// <list type="number">
/// <item><b>지지직 진입</b> (0.35초) — 화면이 어두워지고 가로 노이즈 줄이 튀며 잡음이 난다.</item>
/// <item><b>쾅</b> (0.2초) — 얼굴이 크게(2.2배) 박혀 들어오며 줄어든다. 하얀 번쩍임 + 화면 흔들림.</item>
/// <item><b>노려봄</b> (1.4초) — 빨강·청록으로 갈라진 잔상이 떨리고, 얼굴이 붉게 맥박친다. 가끔 옆으로 크게 튄다.</item>
/// <item><b>깨지며 퇴장</b> (0.55초) — 노이즈가 다시 거세지고, 얼굴이 가로로 늘어나 찢어지듯 사라진다.</item>
/// </list>
///
/// <b>컷인 동안 게임이 통째로 멈춘다</b>(<see cref="TimeControl.Hold"/>) — 몬스터·투사체·보스 기믹 전부 정지.
/// 혹시 모를 피해를 막으려고 플레이어와 보스 둘 다 무적도 건다.
/// 컷인이 어떻게 끝나든(중간에 오브젝트가 지워져도) OnDestroy 에서 멈춤과 무적을 반드시 푼다.
/// 연출 자체는 unscaled 시간으로 돈다 — 멈춘 화면 위에서 얼굴만 움직인다.
/// </summary>
public class HalloweenPhaseCutin : MonoBehaviour
{
    const float InTime = 0.35f;
    const float SlamTime = 0.2f;
    const float HoldTime = 1.4f;
    const float OutTime = 0.55f;

    const float PortraitSize = 900f;   // 기준 해상도(1920x1080)에서 얼굴 한 변
    const int BarCount = 14;
    const float GlitchTick = 0.05f;

    static readonly Color Red = new(1f, 0.15f, 0.25f);
    static readonly Color Cyan = new(0.2f, 1f, 1f);

    GameObject overlay;
    Image tint, flash;
    RectTransform face;                 // 세 겹(빨강·청록·본체)을 묶는 부모
    Image faceMain, faceRed, faceCyan;
    RectTransform[] bars;
    float nextTick;

    PlayerHealth playerHealth;
    Enemy boss;
    bool holding;
    Action onDone;

    public void Play(Sprite portrait, Enemy target, Action done)
    {
        onDone = done;

        // 게임 정지 — 컷인 동안 아무것도 안 움직인다
        TimeControl.Hold(this);
        holding = true;

        boss = target;
        if (boss != null) boss.Invulnerable = true;

        // 무적 — 컷인 동안 보스·몬스터에게 맞지 않는다
        if (GameManager.instance != null && GameManager.instance.player != null)
            playerHealth = GameManager.instance.player.GetComponent<PlayerHealth>();
        if (playerHealth != null) playerHealth.PushInvulnerable();

        Build(portrait);
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        HalloweenIntro.PlayNoise();

        // 1. 지지직 진입 — 아직 얼굴은 안 보인다
        for (float t = 0f; t < InTime; t += Time.unscaledDeltaTime)
        {
            float k = t / InTime;
            Glitch(0.6f + 0.4f * k);
            SetTint(0.75f * k);
            SetFace(0f, 1f, 0f, 0f);
            yield return null;
        }

        // 2. 쾅 — 크게 박혀 들어오며 줄어든다
        for (float t = 0f; t < SlamTime; t += Time.unscaledDeltaTime)
        {
            float k = t / SlamTime;
            float ease = 1f - (1f - k) * (1f - k);
            Glitch(0.5f);
            SetTint(0.78f);
            SetFace(Mathf.Clamp01(k * 2.5f), Mathf.Lerp(2.2f, 1f, ease), 30f * (1f - k), 0f);
            SetFlash(0.85f * k);
            yield return null;
        }

        // 3. 노려봄 — 갈라진 잔상 + 붉은 맥박 + 흔들림
        for (float t = 0f; t < HoldTime; t += Time.unscaledDeltaTime)
        {
            float k = t / HoldTime;
            Glitch(0.25f);
            SetTint(0.78f);
            SetFlash(0.85f * Mathf.Clamp01(1f - t / 0.25f));

            float shake = Mathf.Lerp(14f, 3f, Mathf.Clamp01(t / 0.4f));
            float split = 6f + 4f * Mathf.Sin(t * 30f);
            SetFace(1f, 1f + 0.04f * k, split, shake);

            // 붉은 맥박 — 눈이 번쩍이는 느낌
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 14f);
            faceMain.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.6f), pulse * 0.45f);
            yield return null;
        }

        HalloweenIntro.PlayNoise();

        // 4. 깨지며 퇴장 — 가로로 늘어나고 찢어지듯 사라진다
        for (float t = 0f; t < OutTime; t += Time.unscaledDeltaTime)
        {
            float k = t / OutTime;
            Glitch(0.6f + 0.4f * k);
            SetTint(0.78f * (1f - k));
            SetFlash(0f);

            float a = 1f - k * k;
            // 가끔 통째로 꺼졌다 켜진다
            if (UnityEngine.Random.value < 0.3f * k) a *= 0.2f;

            SetFace(a, 1.04f + 0.3f * k, 6f + 60f * k, 10f * k);
            face.localScale = new Vector3(face.localScale.x * (1f + 1.2f * k), face.localScale.y * (1f - 0.7f * k), 1f);
            yield return null;
        }

        Finish();
    }

    void Finish()
    {
        if (overlay != null) Destroy(overlay);
        overlay = null;
        Unfreeze();

        Action done = onDone;
        onDone = null;
        done?.Invoke();

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        Unfreeze();
        if (overlay != null) Destroy(overlay);
    }

    /// <summary>멈춤·무적을 푼다. 두 번 불려도 한 번만 푼다.</summary>
    void Unfreeze()
    {
        if (holding) TimeControl.Release(this);
        holding = false;

        if (playerHealth != null) playerHealth.PopInvulnerable();
        playerHealth = null;

        if (boss != null) boss.Invulnerable = false;
        boss = null;
    }

    // ───────────────────────── 그리기 ─────────────────────────

    /// <param name="alpha">얼굴 투명도</param>
    /// <param name="scale">얼굴 크기 배율</param>
    /// <param name="split">빨강·청록 잔상이 좌우로 벌어지는 폭(px)</param>
    /// <param name="shake">흔들림 폭(px)</param>
    void SetFace(float alpha, float scale, float split, float shake)
    {
        Vector2 jitter = UnityEngine.Random.insideUnitCircle * shake;
        // 가끔 옆으로 크게 튄다
        if (shake > 0f && UnityEngine.Random.value < 0.06f) jitter.x += UnityEngine.Random.Range(-50f, 50f);

        face.anchoredPosition = jitter;
        face.localScale = Vector3.one * scale;

        faceRed.rectTransform.anchoredPosition = new Vector2(-split, UnityEngine.Random.Range(-2f, 2f));
        faceCyan.rectTransform.anchoredPosition = new Vector2(split, UnityEngine.Random.Range(-2f, 2f));

        faceMain.color = new Color(faceMain.color.r, faceMain.color.g, faceMain.color.b, alpha);
        faceRed.color = new Color(Red.r, Red.g, Red.b, alpha * 0.55f);
        faceCyan.color = new Color(Cyan.r, Cyan.g, Cyan.b, alpha * 0.45f);
    }

    void SetTint(float a) => tint.color = new Color(0.03f, 0f, 0.04f, a);
    void SetFlash(float a) => flash.color = new Color(1f, 0.9f, 0.92f, a * 0.6f);

    /// <summary><paramref name="power"/> 0~1. 클수록 노이즈 줄이 많고 크다.</summary>
    void Glitch(float power)
    {
        if (Time.unscaledTime < nextTick) return;
        nextTick = Time.unscaledTime + GlitchTick;

        for (int i = 0; i < bars.Length; i++)
        {
            bool on = UnityEngine.Random.value < 0.55f * power;
            bars[i].gameObject.SetActive(on);
            if (!on) continue;

            bars[i].anchoredPosition = new Vector2(UnityEngine.Random.Range(-200f, 200f) * power, UnityEngine.Random.Range(-540f, 540f));
            bars[i].sizeDelta = new Vector2(UnityEngine.Random.Range(600f, 2400f), UnityEngine.Random.Range(3f, 40f));
            Color c = (i % 3) switch { 0 => Red, 1 => Cyan, _ => Color.white };
            c.a = UnityEngine.Random.Range(0.25f, 0.7f);
            bars[i].GetComponent<Image>().color = c;
        }
    }

    void Build(Sprite portrait)
    {
        overlay = new GameObject("Halloween Phase Cut-in Overlay");

        var canvas = overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 550;   // 보스 경고(500) 위, 시작 연출(600) 아래

        var scaler = overlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var group = overlay.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        tint = FullScreen("Tint");

        // 얼굴 — 빨강·청록 잔상 아래, 본체 위
        var faceGo = new GameObject("Face", typeof(RectTransform));
        faceGo.transform.SetParent(overlay.transform, false);
        face = (RectTransform)faceGo.transform;
        face.sizeDelta = Vector2.one * PortraitSize;

        faceRed = FaceLayer("Red", portrait);
        faceCyan = FaceLayer("Cyan", portrait);
        faceMain = FaceLayer("Main", portrait);

        bars = new RectTransform[BarCount];
        for (int i = 0; i < BarCount; i++)
        {
            var go = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(overlay.transform, false);
            go.GetComponent<Image>().raycastTarget = false;
            bars[i] = (RectTransform)go.transform;
            go.SetActive(false);
        }

        flash = FullScreen("Flash");
    }

    Image FullScreen(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(overlay.transform, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = false;
        return img;
    }

    Image FaceLayer(string name, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(face, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;
        return img;
    }
}
