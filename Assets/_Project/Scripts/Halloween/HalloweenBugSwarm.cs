using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스전 기믹 "버그 소환". 보스가 나오면 웨이브 스폰을 멈추고, 대신 주기적으로
/// 빨간 글씨로 예고한 뒤 플레이어 근처 한쪽에서 몬스터가 <b>무리지어</b> 몰려온다.
///
/// <list type="bullet">
/// <item>예고: 화면 위쪽 "버그 소환" 빨간 글씨 + 무리가 나올 자리에 빨간 원(바닥)</item>
/// <item>무리: 화면 바로 바깥 한 점 주변에 뭉쳐서 나온다 — 흩뿌리지 않는다. 적은 원래대로 플레이어를 쫓는다</item>
/// <item>2페이즈는 간격이 짧아지고 양쪽에서 두 무리가 온다</item>
/// </list>
/// <see cref="HalloweenBossShow"/> 가 보스 등장 순간 붙이고, 판이 끝나면 지운다.
/// </summary>
public class HalloweenBugSwarm : MonoBehaviour
{
    const string Label = "버그 소환";

    const float FirstDelay = 6f;        // 보스 등장 뒤 첫 소환까지
    const float Interval1 = 11f;        // 1페이즈 소환 간격
    const float Interval2 = 8f;         // 2페이즈
    const float WarnTime = 1.6f;        // 예고 → 실제 소환
    const int Count1 = 12;              // 1페이즈 한 무리 마릿수
    const int Count2 = 9;               // 2페이즈 무리당 마릿수(두 무리)
    const float SpawnDistance = 15f;    // 플레이어에서 무리 중심까지
    const float ClusterRadius = 1.8f;   // 무리가 뭉치는 반경

    static readonly Color Red = new(1f, 0.12f, 0.18f);

    Enemy boss;
    Vector2 arenaMin, arenaMax;
    int phase = 1;

    float nextAt;
    float spawnAt = -1f;
    Vector2[] pending;

    GameObject overlay;
    TextMeshProUGUI text;
    LineRenderer[] markers;
    Material markerMat;

    public void Begin(Enemy target, Vector2 min, Vector2 max, Material lineMaterial)
    {
        boss = target;
        arenaMin = min;
        arenaMax = max;
        markerMat = lineMaterial;
        nextAt = Time.time + FirstDelay;

        if (Spawner.Current != null)
        {
            Spawner.Current.WavesPaused = true;
            Spawner.Current.ClearMinions();
        }

        BuildOverlay();
    }

    public void SetPhase(int p) => phase = p;

    void OnDestroy()
    {
        if (Spawner.Current != null) Spawner.Current.WavesPaused = false;
        if (overlay != null) Destroy(overlay);
        ClearMarkers();
    }

    void Update()
    {
        if (boss == null || !boss.IsAlive || !RunDirector.IsPlaying)
        {
            Hide();
            return;
        }

        // 예고 중
        if (spawnAt > 0f)
        {
            float remain = spawnAt - Time.time;
            Blink(remain);

            if (remain <= 0f)
            {
                Release();
                spawnAt = -1f;
                Hide();
                nextAt = Time.time + (phase >= 2 ? Interval2 : Interval1);
            }
            return;
        }

        if (Time.time >= nextAt) Warn();
    }

    // ── 예고 ──────────────────────────────────────────

    void Warn()
    {
        Vector2 player = PlayerPos();
        int swarms = phase >= 2 ? 2 : 1;
        pending = new Vector2[swarms];

        float angle = Random.Range(0f, Mathf.PI * 2f);
        for (int i = 0; i < swarms; i++)
        {
            // 두 무리면 서로 반대편에서
            float a = angle + i * Mathf.PI;
            Vector2 p = player + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * SpawnDistance;
            p.x = Mathf.Clamp(p.x, arenaMin.x + ClusterRadius, arenaMax.x - ClusterRadius);
            p.y = Mathf.Clamp(p.y, arenaMin.y + ClusterRadius, arenaMax.y - ClusterRadius);
            pending[i] = p;
        }

        spawnAt = Time.time + WarnTime;

        overlay.SetActive(true);
        text.text = swarms > 1 ? Label + "  ×2" : Label;

        ClearMarkers();
        markers = new LineRenderer[swarms];
        for (int i = 0; i < swarms; i++) markers[i] = MakeMarker(pending[i]);

        UiSound.Play(UiCue.CardPick);
    }

    void Blink(float remain)
    {
        // 빠르게 깜빡이다 소환 직전엔 계속 켜진다
        bool on = remain < 0.4f || Mathf.Repeat(Time.time * 6f, 1f) < 0.65f;
        text.alpha = on ? 1f : 0.25f;

        float k = 1f - Mathf.Clamp01(remain / WarnTime);
        text.rectTransform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.time * 18f));

        if (markers == null) return;
        foreach (LineRenderer lr in markers)
        {
            if (lr == null) continue;
            // 원이 점점 좁혀지며 진해진다
            lr.transform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, k);
            Color c = Red;
            c.a = on ? 0.9f : 0.35f;
            lr.startColor = lr.endColor = c;
        }
    }

    void Release()
    {
        if (Spawner.Current == null || pending == null) return;

        int count = phase >= 2 ? Count2 : Count1;
        int total = 0;
        foreach (Vector2 p in pending) total += Spawner.Current.SpawnSwarm(p, ClusterRadius, count);

        if (LogManager.Instance != null) LogManager.Instance.System($"bug summon x{total}");
    }

    void Hide()
    {
        if (overlay != null && overlay.activeSelf && spawnAt < 0f) overlay.SetActive(false);
        if (spawnAt < 0f) ClearMarkers();
    }

    Vector2 PlayerPos()
    {
        if (GameManager.instance != null && GameManager.instance.player != null)
            return GameManager.instance.player.transform.position;
        return (arenaMin + arenaMax) * 0.5f;
    }

    // ── 그림 ──────────────────────────────────────────

    void BuildOverlay()
    {
        overlay = new GameObject("Bug Summon Warning");
        overlay.transform.SetParent(transform, false);

        var canvas = overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 460;

        var scaler = overlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(overlay.transform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(1200f, 130f);
        rect.anchoredPosition = new Vector2(0f, -150f);   // 보스 체력바 아래

        text = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = UiTheme.Current != null ? UiTheme.Current.mono : null;
        if (font != null) text.font = font;
        text.fontSize = 96f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Red;
        text.raycastTarget = false;

        Material m = text.fontMaterial;
        m.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
        m.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.25f, 0f, 0.02f));
        m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(1f, 0f, 0.1f, 0.7f));
        m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.9f);
        m.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.5f);
        text.UpdateMeshPadding();

        overlay.SetActive(false);
    }

    LineRenderer MakeMarker(Vector2 at)
    {
        var go = new GameObject("Bug Summon Marker");
        go.transform.position = new Vector3(at.x, at.y, 0f);

        const int N = 40;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = N;
        for (int i = 0; i < N; i++)
        {
            float a = i * Mathf.PI * 2f / N;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * (ClusterRadius + 0.6f));
        }
        lr.startWidth = lr.endWidth = 0.25f;
        lr.startColor = lr.endColor = Red;
        if (markerMat != null) lr.sharedMaterial = markerMat;
        lr.sortingLayerName = "Background";
        lr.sortingOrder = 20;
        return lr;
    }

    void ClearMarkers()
    {
        if (markers == null) return;
        foreach (LineRenderer lr in markers) if (lr != null) Destroy(lr.gameObject);
        markers = null;
    }
}
