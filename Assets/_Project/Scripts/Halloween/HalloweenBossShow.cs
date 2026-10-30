using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 할로윈 보스(세이렌) 연출. 할로윈 배경 프리팹에 붙어 있어서 할로윈 스테이지에서만 돈다.
///
/// <list type="number">
/// <item><b>경고</b> — 판을 끝내는 보스가 나오기 <see cref="warnSeconds"/> 초 전부터 화면 가운데에
///       빨간 네온 글자로 "세이렌 등장 / N초 전" 을 띄운다. 숫자가 바뀔 때마다 튀고, 네온처럼 깜빡인다.</item>
/// <item><b>보스 노래</b> — 보스가 실제로 나온 순간 배경음을 세이렌 노래로 바꾼다(<see cref="HalloweenSkin.PlayBossBgm"/>).</item>
/// <item><b>복싱 링</b> — 보스가 나온 순간 플레이어와 보스 사이에 큰 네온 링을 고정한다.
///       <b>플레이어는 링 밖으로 못 나간다</b> — 보스와 맞붙는 결전장. 조명을 안 받는 머터리얼이라 어두운 시야 밖에서도 빛난다.</item>
/// </list>
///
/// 게임 쪽 코드는 안 건드린다 — <see cref="RunDirector"/> 가 공개한 경과 시간과 EndingBoss 만 읽는다.
/// 경과 시간은 증강 선택 중 멈추므로 카운트다운도 같이 멈춘다.
/// </summary>
public class HalloweenBossShow : MonoBehaviour
{
    [Header("경고")]
    [Tooltip("보스가 나오기 몇 초 전부터 경고를 띄울지.")]
    [Min(1f)] [SerializeField] float warnSeconds = 5f;

    [SerializeField] string warnTitle = "세이렌 등장";

    [Tooltip("{0} 에 남은 초가 들어간다.")]
    [SerializeField] string warnCountdown = "{0}초 전";

    [SerializeField] Color neonCore = new(1f, 0.82f, 0.86f);
    [SerializeField] Color neonGlow = new(1f, 0.05f, 0.2f);

    [Header("시작 연출")]
    [Tooltip("처음 이 시간 동안은 원래 게임 모습(격자 배경·원래 BGM·몬스터 없음)이다.")]
    [Min(0f)] [SerializeField] float introSeconds = 5f;

    [Tooltip("시작 연출 동안 깔 원래 배경.")]
    [SerializeField] GameObject originalBackground;

    [SerializeField] AudioClip originalBgm;
    [Range(0f, 1f)] [SerializeField] float originalBgmVolume = 0.4f;

    [Tooltip("지지직 이후 시작되는 할로윈 BGM.")]
    [SerializeField] AudioClip halloweenBgm;
    [Range(0f, 1f)] [SerializeField] float halloweenBgmVolume = 0.5f;

    [SerializeField] string introError = "ERROR CODE: SEIREN";
    [SerializeField] string introMission = "세이렌을 처치하고\n먼작귀들을 되돌리시오";

    [Header("컴파일 1분 경고")]
    [Tooltip("보스가 나오기 이 초 전에 '1분 남음' 배너를 띄운다. 이때부터 몰림 웨이브가 시작된다.")]
    [Min(0f)] [SerializeField] float compileWarnLead = 70f;

    [SerializeField] string compileWarnTitle = "COMPILE TIME: 1 MINUTE LEFT";
    [SerializeField] string compileWarnSub = "컴파일 종료까지 1분 — 버그가 쏟아진다";

    [Header("보스 체력바 · 페이즈")]
    [SerializeField] string bossName = "SIREN";

    [Tooltip("체력이 이 비율 아래로 떨어지면 2페이즈 — 원래 기믹에 회전 레이저와 물결이 더해진다.")]
    [Range(0.05f, 0.95f)] [SerializeField] float phase2At = 0.5f;

    [Tooltip("2페이즈를 이 정도 시간(초) 안에 잡게 체력을 맞춘다.\n" +
             "1페이즈 동안 보스에게 실제로 넣은 초당 피해(DPS) × 이 시간 = 2페이즈 체력.")]
    [Min(5f)] [SerializeField] float phase2TargetSeconds = 45f;

    [Tooltip("2페이즈 체력 배율의 하한·상한. 원래 남은 체력(50%) 기준.\n" +
             "하한 1 = 약한 빌드도 원래보다 쉬워지지는 않는다. 상한 = 너무 센 빌드에서 끝없이 불어나지 않게.")]
    [Min(1f)] [SerializeField] float phase2MinScale = 1f;
    [Min(1f)] [SerializeField] float phase2MaxScale = 8f;

    [Tooltip("1페이즈가 이보다 짧게 끝나면 DPS 측정이 불안정하다고 보고 이 배율을 쓴다.")]
    [Min(1f)] [SerializeField] float phase2FallbackScale = 3f;
    [Tooltip("1→2페이즈 사이에 크게 띄웠다 사라지는 세이렌 얼굴(지지직 컷인). 그동안 플레이어는 무적.")]
    [SerializeField] Sprite phase2Portrait;
    [SerializeField] string phase2Title = "2 페이즈";
    [SerializeField] string phase2Sub = "레이저 · 물결 주의";

    [Header("경험치 흡수")]
    [Tooltip("세이렌이 나오는 순간 발동할 아이템. GC(흩어진 경험치 전부 흡수)를 물린다.\n" +
             "비우면 같은 효과를 직접 낸다.")]
    [SerializeField] AugmentData collectOnBoss;

    [Header("복싱 링")]
    [Tooltip("링 한 변의 길이(유닛). 카메라는 가로 약 43 · 세로 24유닛을 본다.\n" +
             "보스가 플레이어에게서 40유닛 떨어져 나오므로, 둘을 다 담으려면 60 이상이어야 한다.")]
    [Min(5f)] [SerializeField] float ringSize = 90f;

    [Tooltip("로프 굵기(유닛).")]
    [Min(0.05f)] [SerializeField] float ropeWidth = 0.8f;

    [Tooltip("플레이어가 로프에서 이만큼(유닛) 안쪽까지만 갈 수 있다. 몸이 로프에 파묻히지 않게.")]
    [Min(0f)] [SerializeField] float wallPadding = 0.8f;

    [Tooltip("조명을 안 받는 머터리얼. 비우면 런타임에 셰이더를 찾아 만든다.")]
    [SerializeField] Material ringMaterial;

    [SerializeField] Color[] ropeColors =
    {
        new(1f, 0.1f, 0.25f),
        new(1f, 0.9f, 0.95f),
        new(1f, 0.1f, 0.25f),
    };

    const float RopeGap = 1.6f;      // 로프 사이 간격(유닛)
    const float PostSize = 4f;       // 코너 기둥 한 변
    const float HitFlashTime = 0.25f; // 로프에 부딪혔을 때 하얗게 번쩍이는 시간
    const float PulseHz = 1.2f;      // 링이 숨 쉬는 속도

    float bossAt = -1f;
    bool warned;
    bool announced;
    bool compileWarned;

    GameObject overlay;
    TextMeshProUGUI title;
    TextMeshProUGUI countdown;
    CanvasGroup overlayGroup;
    int lastShown = -1;
    float bumpTime;

    HalloweenBossBar bar;
    HalloweenBossPhase2 phase2;
    HalloweenBugSwarm swarm;
    int phase;
    float phase1Time;          // 보스를 때리고 있던 시간 합
    float bossClock;           // 보스 등장 후 흐른 게임 시간
    float lastBossHit = -999f; // 마지막으로 보스 체력이 깎인 시각(bossClock 기준)
    float lastBossHealth = -1f;
    const float HitWindow = 1f;
    float bannerUntil = -1f;

    Transform ring;
    LineRenderer[] ringLines;
    Color[] ringBaseColors;
    Enemy boss;

    // 링 안쪽 경계(월드). 플레이어 위치를 이 안으로 묶는다
    Vector2 arenaMin, arenaMax;
    Rigidbody2D playerBody;
    float lastBump = -10f;

    void Awake()
    {
        // 시작 연출. 배경과 같은 오브젝트에 붙어 배경 그림을 잠깐 숨긴다
        if (introSeconds > 0f)
            gameObject.AddComponent<HalloweenIntro>().Setup(
                originalBackground, originalBgm, originalBgmVolume,
                halloweenBgm, halloweenBgmVolume, introSeconds,
                introError, introMission.Replace("\\n", "\n"));
    }

    void Start()
    {
        StageData stage = StageContext.Active;
        if (stage == null || stage.bosses == null) return;

        // 판을 끝내는 보스(= 세이렌). 여럿이면 가장 이른 것
        foreach (BossSpawn b in stage.bosses)
        {
            if (b == null || !b.endsRun || !b.IsValid) continue;
            if (bossAt < 0f || b.atSeconds < bossAt) bossAt = b.atSeconds;
        }
    }

    void Update()
    {
        RunDirector director = RunDirector.Current;
        if (director == null) return;

        float now = director.Elapsed;

        // 1분 경고 — 보스 경고와 겹치지 않을 때만
        if (!compileWarned && bossAt > 0f && compileWarnLead > warnSeconds
            && now >= bossAt - compileWarnLead && now < bossAt - warnSeconds - 3f)
        {
            compileWarned = true;
            ShowBanner(compileWarnTitle, compileWarnSub, 3f);
        }

        if (!warned && bossAt > 0f && now >= bossAt - warnSeconds && director.EndingBoss == null)
        {
            warned = true;
            ShowWarning();
        }

        if (overlay != null)
        {
            // 경고는 남은 시간을, 페이즈 배너는 정해진 시간만큼 보여준다
            if (bannerUntil > 0f)
            {
                TickWarning(99f);
                if (Time.unscaledTime >= bannerUntil) { HideWarning(); bannerUntil = -1f; }
            }
            else TickWarning(bossAt - now);
        }

        // 1페이즈 동안 "보스를 때리고 있던" 시간만 센다 — DPS 측정용.
        // 체력이 깎인 순간부터 HitWindow 초 안이면 교전 중으로 본다(공격 쿨다운 사이 공백을 메운다).
        // 도망·잡몹 정리로 보스를 안 때린 시간은 빠진다. deltaTime 이라 증강 선택 중엔 안 센다
        if (phase == 1 && boss != null && boss.IsAlive) TrackEngagement(Time.deltaTime);

        // 2페이즈 진입
        if (phase == 1 && boss != null && boss.IsAlive && boss.HealthRatio <= phase2At)
            EnterPhase2();

        if (!announced && director.EndingBoss != null)
        {
            announced = true;
            boss = director.EndingBoss;

            HideWarning();
            HalloweenSkin.PlayBossBgm();
            BuildRing();
            CollectAllExp();

            // 보스전엔 웨이브가 멈추고, 대신 "버그 소환" 기믹으로 무리가 몰려온다
            var swarmGo = new GameObject("Halloween Bug Swarm");
            swarmGo.transform.SetParent(transform, false);
            swarm = swarmGo.AddComponent<HalloweenBugSwarm>();
            swarm.Begin(boss, arenaMin, arenaMax, ringMaterial != null ? ringMaterial : MakeUnlitMaterial());

            phase = 1;
            var barGo = new GameObject("Halloween Boss Bar");
            barGo.transform.SetParent(transform, false);
            bar = barGo.AddComponent<HalloweenBossBar>();
            bar.Build(boss, bossName, phase2At);
        }
    }

    void LateUpdate()
    {
        if (ring == null) return;

        // 판이 끝났으면(보스 처치·사망) 링·체력바·패턴을 걷는다. 결과 화면으로 넘어가는 동안 남아 있을 이유가 없다
        if (RunDirector.Current == null || RunDirector.Current.State != RunState.Playing)
        {
            Destroy(ring.gameObject);
            ring = null;
            if (bar != null) Destroy(bar.gameObject);
            if (phase2 != null) Destroy(phase2.gameObject);
            if (swarm != null) Destroy(swarm.gameObject);
            HideWarning();
            return;
        }

        bool bumped = ConfinePlayer();
        if (bumped) lastBump = Time.time;

        // 평소엔 숨 쉬듯 밝았다 어두워지고, 부딪히면 잠깐 하얗게 번쩍인다
        float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * PulseHz * Mathf.PI * 2f);
        float flash = Mathf.Clamp01(1f - (Time.time - lastBump) / HitFlashTime);

        for (int i = 0; i < ringLines.Length; i++)
        {
            Color c = Color.Lerp(ringBaseColors[i], Color.white, flash);
            c.a = Mathf.Max(pulse, flash);
            ringLines[i].startColor = ringLines[i].endColor = c;
        }
    }

    /// <summary>
    /// 플레이어를 링 안으로 되돌린다. 밖으로 나가려 했으면 true.
    ///
    /// <b>벽 콜라이더 대신 위치를 직접 묶는 이유</b> — 콜라이더는 물리 레이어 설정에 따라 몬스터·투사체까지
    /// 막거나, 플레이어 몸이 트리거라 아예 안 막힐 수 있다. 위치를 묶으면 이동·대시·넉백 어떤 경로로 밀려도 확실히 막힌다.
    /// 물리 계산이 끝난 뒤(LateUpdate) 되돌리므로 화면엔 밖으로 나간 모습이 한 프레임도 안 그려진다.
    /// </summary>
    bool ConfinePlayer()
    {
        if (playerBody == null)
        {
            if (GameManager.instance == null || GameManager.instance.player == null) return false;
            playerBody = GameManager.instance.player.GetComponent<Rigidbody2D>();
            if (playerBody == null) return false;
        }

        Vector2 p = playerBody.position;
        Vector2 clamped = new(Mathf.Clamp(p.x, arenaMin.x, arenaMax.x), Mathf.Clamp(p.y, arenaMin.y, arenaMax.y));

        if (clamped == p) return false;

        playerBody.position = clamped;
        playerBody.linearVelocity = Vector2.zero;

        Transform t = playerBody.transform;
        t.position = new Vector3(clamped.x, clamped.y, t.position.z);

        return true;
    }

    /// <summary>
    /// 2페이즈. 원래 기믹은 보스가 계속 돌리고, 회전 레이저와 물결을 더한다.
    /// 체력바 테두리가 파랗게 바뀌고, 화면에 잠깐 배너를 띄운다.
    /// </summary>
    void EnterPhase2()
    {
        phase = 2;

        // 2페이즈 체력을 플레이어 DPS 에 맞춘다 — 최대·현재 체력을 같이 늘려 비율(50%)은 그대로,
        // 남은 체력만 커진다. 체력바는 비율만 읽으므로 2페이즈 바(50→0%)가 그만큼 느리게 줄어든다
        boss.ScaleHealth(Phase2Scale());

        if (bar != null) bar.SetPhase(2);

        // 1→2페이즈 사이 컷인 — 세이렌 얼굴이 지지직거리며 크게 떴다 사라진다.
        // 컷인 동안은 게임이 멈추고 플레이어·보스 둘 다 무적이며, 2페이즈 패턴(레이저·물결)은 컷인이 끝난 뒤에 시작한다
        if (phase2Portrait != null)
        {
            var cut = new GameObject("Halloween Phase Cut-in");
            cut.transform.SetParent(transform, false);
            cut.AddComponent<HalloweenPhaseCutin>().Play(phase2Portrait, boss, StartPhase2);
        }
        else StartPhase2();
    }

    /// <summary>컷인이 끝난 뒤 실제 2페이즈를 켠다.</summary>
    void StartPhase2()
    {
        if (boss == null || !boss.IsAlive || ring == null) return;

        if (swarm != null) swarm.SetPhase(2);

        // 2페이즈 그림(p2_)이 있으면 보스 모습이 바뀐다
        HalloweenSkin.SetPhase(boss, 2);

        var go = new GameObject("Halloween Boss Phase 2");
        phase2 = go.AddComponent<HalloweenBossPhase2>();
        phase2.Begin(boss, ringMaterial != null ? ringMaterial : MakeUnlitMaterial());

        ShowBanner(phase2Title, phase2Sub, 2.2f);
    }

    /// <summary>보스 체력이 깎였는지 보고, 교전 중이면 1페이즈 시간을 늘린다.</summary>
    void TrackEngagement(float dt)
    {
        bossClock += dt;

        float hp = boss.Health;
        if (lastBossHealth >= 0f && hp < lastBossHealth) lastBossHit = bossClock;
        lastBossHealth = hp;

        if (bossClock - lastBossHit <= HitWindow) phase1Time += dt;
    }

    /// <summary>
    /// 2페이즈 체력 배율. 1페이즈에 보스에게 실제로 들어간 피해 ÷ 보스를 때리고 있던 시간 = 보스 대상 DPS.
    /// 그 DPS 로 <see cref="phase2TargetSeconds"/> 초 걸리는 체력이 되도록 남은 체력에 곱할 값을 낸다.
    ///
    /// 보스 대상 DPS 를 쓰는 이유 — 전체 피해량은 잡몹에 흩어진 광역 피해까지 섞여 보스전 체감과 안 맞는다.
    /// </summary>
    float Phase2Scale()
    {
        float ratio = boss.HealthRatio;
        float remaining = boss.Health;
        if (ratio <= 0f || remaining <= 0f) return phase2FallbackScale;

        float max = remaining / ratio;
        float dealt = max - remaining;     // 1페이즈 동안 깎은 체력

        if (phase1Time < 3f || dealt <= 0f)
        {
            Debug.Log($"[HalloweenBoss] 2페이즈 — 1페이즈가 너무 짧아(교전 {phase1Time:0.0}초) 기본 배율 ×{phase2FallbackScale}");
            return phase2FallbackScale;
        }

        float dps = dealt / phase1Time;
        float wanted = dps * phase2TargetSeconds;
        float scale = Mathf.Clamp(wanted / remaining, phase2MinScale, phase2MaxScale);

        Debug.Log($"[HalloweenBoss] 2페이즈 — 보스 대상 DPS {dps:0.#} (교전 {phase1Time:0.0}초 / 전체 {bossClock:0.0}초, {dealt:0}피해) → " +
                  $"목표 {phase2TargetSeconds:0}초 · 체력 ×{scale:0.00} ({remaining:0} → {remaining * scale:0})");
        return scale;
    }

    /// <summary>경고와 같은 네온 글자로 잠깐 띄우는 배너.</summary>
    void ShowBanner(string head, string sub, float seconds)
    {
        HideWarning();
        ShowWarning();
        title.text = head;
        countdown.text = sub;
        lastShown = 99;
        bumpTime = Time.unscaledTime;
        bannerUntil = Time.unscaledTime + seconds;
    }

    /// <summary>
    /// 맵에 흩어진 경험치를 전부 빨아들인다. 결전 전에 정리해주는 보상 —
    /// 링에 갇히면 바깥 오브를 주우러 갈 수 없으니 여기서 몰아준다.
    /// GC 아이템과 똑같은 효과라 그 아이템을 그대로 발동한다(오브가 사방에서 끌려온다).
    /// </summary>
    void CollectAllExp()
    {
        if (collectOnBoss != null && InstantItem.Apply(collectOnBoss)) return;

        // 아이템이 안 물렸거나 효과가 없을 때의 대비. 같은 동작을 직접 한다
        foreach (ExpMove orb in FindObjectsByType<ExpMove>(FindObjectsSortMode.None))
            if (orb != null) orb.ForcePull();
    }

    // ───────────────────────── 경고 ─────────────────────────

    void ShowWarning()
    {
        overlay = new GameObject("Halloween Boss Warning");
        overlay.transform.SetParent(transform, false);

        var canvas = overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = overlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        overlayGroup = overlay.AddComponent<CanvasGroup>();
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;

        // 글자 뒤 어두운 띠. 밝은 배경 위에서도 네온이 묻히지 않게
        var band = new GameObject("Band", typeof(RectTransform), typeof(Image));
        band.transform.SetParent(overlay.transform, false);
        var bandRect = (RectTransform)band.transform;
        bandRect.anchorMin = new Vector2(0f, 0.5f);
        bandRect.anchorMax = new Vector2(1f, 0.5f);
        bandRect.sizeDelta = new Vector2(0f, 330f);
        bandRect.anchoredPosition = new Vector2(0f, 120f);
        band.GetComponent<Image>().color = new Color(0.05f, 0f, 0.02f, 0.55f);

        title = MakeText("Title", warnTitle, 150f, new Vector2(0f, 175f));
        countdown = MakeText("Countdown", string.Empty, 90f, new Vector2(0f, 40f));
    }

    TextMeshProUGUI MakeText(string name, string text, float size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(overlay.transform, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1800f, size * 1.4f);
        rect.anchoredPosition = pos;

        var t = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = UiTheme.Current != null ? UiTheme.Current.mono : null;
        if (font != null) t.font = font;

        t.text = text;
        t.fontSize = size;
        // 긴 문구(COMPILE TIME …)가 칸을 넘지 않게 글자를 줄인다
        t.enableAutoSizing = true;
        t.fontSizeMax = size;
        t.fontSizeMin = size * 0.4f;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = neonCore;
        t.raycastTarget = false;

        // 네온: 밝은 속(흰빛 도는 분홍) + 빨간 테두리 + 번지는 빨간 빛.
        // fontMaterial 은 이 글자 전용 복사본이라 다른 UI 글자에 번지지 않는다
        Material m = t.fontMaterial;
        m.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
        m.SetColor(ShaderUtilities.ID_OutlineColor, neonGlow);
        m.EnableKeyword(ShaderUtilities.Keyword_Glow);
        m.SetColor(ShaderUtilities.ID_GlowColor, neonGlow);
        m.SetFloat(ShaderUtilities.ID_GlowOuter, 0.8f);
        m.SetFloat(ShaderUtilities.ID_GlowInner, 0.1f);
        m.SetFloat(ShaderUtilities.ID_GlowPower, 0.9f);
        m.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        m.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(neonGlow.r, neonGlow.g, neonGlow.b, 0.8f));
        m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.9f);
        m.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.6f);
        t.UpdateMeshPadding();

        return t;
    }

    void TickWarning(float remain)
    {
        int shown = Mathf.Clamp(Mathf.CeilToInt(remain), 1, Mathf.CeilToInt(warnSeconds));

        // 페이즈 배너일 땐 카운트다운 글자를 덮어쓰지 않는다
        if (bannerUntil < 0f && shown != lastShown)
        {
            lastShown = shown;
            countdown.text = string.Format(warnCountdown, shown);
            bumpTime = Time.unscaledTime;
        }

        // 숫자가 바뀔 때 튀었다가 돌아온다
        float k = Mathf.Clamp01((Time.unscaledTime - bumpTime) / 0.25f);
        float bump = 1f + 0.35f * (1f - k) * (1f - k);
        countdown.rectTransform.localScale = Vector3.one * bump;

        // 네온 깜빡임 — 대부분 켜져 있다가 가끔 툭 꺼진다. 고정 패턴이라 매번 같은 리듬
        float f = Time.unscaledTime * 9f;
        bool flick = Mathf.Sin(f) > 0.93f || Mathf.Sin(f * 0.37f + 1.3f) > 0.97f;
        title.alpha = flick ? 0.35f : 1f;
        overlayGroup.alpha = 0.9f + 0.1f * Mathf.Sin(Time.unscaledTime * 6f);
    }

    void HideWarning()
    {
        if (overlay != null) Destroy(overlay);
        overlay = null;
    }

    // ───────────────────────── 복싱 링 ─────────────────────────

    void BuildRing()
    {
        if (boss == null) return;

        Material mat = ringMaterial != null ? ringMaterial : MakeUnlitMaterial();

        // 플레이어와 보스의 한가운데에 고정한다 — 둘 다 링 안에서 시작한다
        Vector2 center = boss.transform.position;
        if (GameManager.instance != null && GameManager.instance.player != null)
            center = ((Vector2)GameManager.instance.player.transform.position + center) * 0.5f;

        ring = new GameObject("Halloween Boss Ring").transform;
        ring.position = new Vector3(center.x, center.y, 0f);

        // 가장 안쪽 로프 안쪽이 플레이어가 다닐 수 있는 곳
        float inner = ringSize * 0.5f - 2f * RopeGap - ropeWidth - wallPadding;
        arenaMin = center - Vector2.one * inner;
        arenaMax = center + Vector2.one * inner;

        var lines = new System.Collections.Generic.List<LineRenderer>();
        float half = ringSize * 0.5f;

        // 로프 세 줄 — 바깥에서 안쪽으로
        for (int i = 0; i < 3; i++)
        {
            float h = half - i * RopeGap;
            Color c = ropeColors[i % ropeColors.Length];
            lines.Add(MakeSquare($"Rope {i + 1}", h, ropeWidth, c, mat, 10 + i));
        }

        // 코너 기둥 — 꽉 찬 작은 사각형. 굵은 선 하나로 칠한다
        Vector2[] corners = { new(-half, -half), new(half, -half), new(half, half), new(-half, half) };
        foreach (Vector2 c in corners)
        {
            LineRenderer post = MakeSquare("Post", PostSize * 0.25f, PostSize * 0.5f, new Color(1f, 0.95f, 0.98f), mat, 14);
            post.transform.localPosition = c;
            lines.Add(post);
        }

        ringLines = lines.ToArray();

        ringBaseColors = new Color[ringLines.Length];
        for (int i = 0; i < ringLines.Length; i++) ringBaseColors[i] = ringLines[i].startColor;
    }

    LineRenderer MakeSquare(string name, float half, float width, Color color, Material mat, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(ring, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 4;
        lr.SetPositions(new[]
        {
            new Vector3(-half, -half), new Vector3(half, -half),
            new Vector3(half, half), new Vector3(-half, half),
        });
        lr.startWidth = lr.endWidth = width;
        lr.numCornerVertices = 2;
        lr.startColor = lr.endColor = color;
        lr.sharedMaterial = mat;

        // 바닥에 깔린다 — 배경 바로 위, 몬스터·플레이어 아래
        lr.sortingLayerName = "Background";
        lr.sortingOrder = order;

        return lr;
    }

    static Material MakeUnlitMaterial()
    {
        Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (s == null) s = Shader.Find("Sprites/Default");
        return new Material(s);
    }
}
