using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 세이렌 2페이즈 패턴. 원래 기믹(블루스크린 시전)은 보스 쪽에서 그대로 돌고, 여기서 두 가지를 얹는다.
///
/// <b>1. 십자 레이저</b> — 보스 중심에서 네 방향으로 뻗는 전기 레이저가 시계방향으로 돈다.
///    처음 <see cref="LaserWarmup"/> 초는 가는 경고선만 보이고 피해가 없다. 그다음부터 닿으면 계속 아프다.
///
/// <b>2. 물결(파도타기)</b> — 보스 자리에서 동심원 띠가 한 칸씩 퍼져 나간다. 띠는 피해(파란 네온)와 안전(빈칸)이
///    번갈아 있고, 한 걸음마다 켜진 띠와 꺼진 띠가 뒤바뀐다. 가만히 서 있으면 두 걸음에 한 번 맞고,
///    한 칸씩 옮겨 다니면 안 맞는다. <b>다음 걸음에 켜질 띠는 미리 깜빡이는 테두리로 보여준다.</b>
///
/// 피해는 <see cref="PlayerHealth.TakeDamage"/> 로 준다 — 피격 연출·사망 처리가 원래 경로를 그대로 탄다.
/// 시간은 Time.deltaTime 이라 증강 선택 중엔 패턴도 멈춘다.
/// </summary>
public class HalloweenBossPhase2 : MonoBehaviour
{
    // ── 레이저 ──
    const float LaserArm = 70f;          // 중심에서 한쪽 끝까지(유닛)
    const float LaserSpin = 18f;         // 시계방향 회전 속도(도/초)
    const float LaserWarmup = 2f;        // 경고선만 보이는 시간
    const float LaserWidth = 1.1f;       // 판정·그림 굵기
    const float LaserDps = 15f;          // 닿아 있는 동안 초당 피해
    const float LaserTick = 0.2f;
    const int LaserPoints = 48;          // 한 줄의 꺾임 수 — 많을수록 지글거림이 촘촘하다
    const float LaserJitter = 0.55f;     // 지글거림 폭
    const float LaserJitterEvery = 0.04f;

    // ── 물결 ──
    const float BandWidth = 9f;          // 띠 하나의 폭(유닛)
    // 한 걸음(띠가 한 칸 퍼지는) 시간. 걸음 속도 3 이면 띠 하나(9)를 걸어서 건너는 데 3초 —
    // 대시(6)를 섞어야 다음 걸음 전에 옮길 수 있게 2초로 둔다
    const float StepTime = 2f;
    const float StepGrace = 0.25f;       // 걸음이 바뀐 직후 판정을 미루는 시간 — 바뀐 걸 보고 피할 틈
    const float WaveRadius = 60f;        // 이 반경까지 퍼지면 그 물결은 끝
    const float WaveRest = 3.5f;         // 물결과 물결 사이 쉬는 시간
    const float FirstWaveDelay = 2.5f;   // 2페이즈 시작 후 첫 물결까지
    const float WaveDamage = 8f;         // 켜진 띠에 있으면 걸음마다 한 번
    const int CircleSegments = 72;

    static readonly Color LaserCore = new(0.85f, 1f, 1f, 1f);
    static readonly Color LaserGlow = new(0.2f, 0.75f, 1f, 0.55f);
    static readonly Color LaserWarn = new(1f, 0.25f, 0.4f, 0.7f);
    static readonly Color BandFill = new(0.15f, 0.65f, 1f, 0.35f);
    static readonly Color BandEdge = new(0.45f, 0.95f, 1f, 0.95f);
    static readonly Color TelegraphEdge = new(0.6f, 0.9f, 1f, 0.6f);

    Enemy boss;
    Material mat;
    PlayerHealth playerHealth;
    Transform player;

    float age;

    // 레이저
    float angle;                     // 도. 줄어들수록 시계방향
    LineRenderer[] laserCore = new LineRenderer[2];
    LineRenderer[] laserGlow = new LineRenderer[2];
    LineRenderer centerOrb;
    float jitterTimer;
    float laserTickTimer;
    readonly Vector3[] laserBuf = new Vector3[LaserPoints + 1];
    readonly float[][] jitter = { new float[LaserPoints + 1], new float[LaserPoints + 1] };

    // 물결
    bool waveRunning;
    float waveTimer;
    Vector2 waveCenter;
    int step;
    float stepStart;
    int lastHitStep = -1;
    int Steps => Mathf.CeilToInt(WaveRadius / BandWidth);
    readonly List<LineRenderer> pool = new();
    int poolUsed;
    readonly Vector3[] circleBuf = new Vector3[CircleSegments];

    public void Begin(Enemy target, Material material)
    {
        boss = target;
        mat = material;

        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            player = GameManager.instance.player.transform;
            playerHealth = player.GetComponent<PlayerHealth>();
        }

        for (int i = 0; i < 2; i++)
        {
            laserGlow[i] = Line($"Laser Glow {i}", LaserWidth * 2.6f, LaserGlow, "Effect", 40);
            laserCore[i] = Line($"Laser Core {i}", LaserWidth * 0.55f, LaserCore, "Effect", 41);
            laserGlow[i].positionCount = laserCore[i].positionCount = LaserPoints + 1;
        }

        centerOrb = Line("Laser Orb", 1.2f, LaserCore, "Effect", 42);
        centerOrb.loop = true;

        waveTimer = FirstWaveDelay;
    }

    void Update()
    {
        if (boss == null || !boss.IsAlive) return;

        float dt = Time.deltaTime;
        age += dt;

        TickLaser(dt);
        TickWave(dt);
    }

    // ───────────────────────── 레이저 ─────────────────────────

    void TickLaser(float dt)
    {
        Vector2 c = boss.transform.position;
        angle -= LaserSpin * dt;

        bool live = age >= LaserWarmup;

        jitterTimer -= dt;
        if (jitterTimer <= 0f)
        {
            jitterTimer = LaserJitterEvery;
            for (int k = 0; k < 2; k++)
                for (int i = 0; i <= LaserPoints; i++)
                {
                    // 가운데와 양 끝은 덜 흔들린다 — 중심이 떠 보이지 않게
                    float t = Mathf.Abs(i / (float)LaserPoints * 2f - 1f);
                    jitter[k][i] = (Random.value - 0.5f) * 2f * LaserJitter * Mathf.Clamp01(t * 3f);
                }
        }

        for (int k = 0; k < 2; k++)
        {
            float rad = (angle + k * 90f) * Mathf.Deg2Rad;
            Vector2 dir = new(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 perp = new(-dir.y, dir.x);

            for (int i = 0; i <= LaserPoints; i++)
            {
                float along = -LaserArm + 2f * LaserArm * i / LaserPoints;
                Vector2 p = c + dir * along + perp * (live ? jitter[k][i] : 0f);
                laserBuf[i] = new Vector3(p.x, p.y, 0f);
            }

            laserCore[k].SetPositions(laserBuf);
            laserGlow[k].SetPositions(laserBuf);

            if (live)
            {
                laserCore[k].startWidth = laserCore[k].endWidth = LaserWidth * 0.55f;
                laserGlow[k].startWidth = laserGlow[k].endWidth = LaserWidth * (2.3f + 0.5f * Mathf.Sin(age * 25f + k));
                laserCore[k].startColor = laserCore[k].endColor = LaserCore;
                laserGlow[k].startColor = laserGlow[k].endColor = LaserGlow;
            }
            else
            {
                // 경고선 — 가늘고 붉게 깜빡인다
                float blink = 0.5f + 0.5f * Mathf.Sin(age * 18f);
                Color w = LaserWarn;
                w.a *= blink;
                laserCore[k].startWidth = laserCore[k].endWidth = 0.18f;
                laserGlow[k].startWidth = laserGlow[k].endWidth = 0f;
                laserCore[k].startColor = laserCore[k].endColor = w;
            }
        }

        // 가운데 빛 덩어리
        float orb = live ? 1.6f + 0.3f * Mathf.Sin(age * 20f) : 0.6f;
        Circle(centerOrb, c, orb * 0.5f);
        centerOrb.startWidth = centerOrb.endWidth = orb;

        if (!live || player == null) return;

        laserTickTimer -= dt;
        if (laserTickTimer > 0f) return;

        if (LaserHits(c))
        {
            laserTickTimer = LaserTick;
            Hurt(LaserDps * LaserTick);
        }
    }

    bool LaserHits(Vector2 c)
    {
        Vector2 p = (Vector2)player.position - c;
        const float reach = LaserWidth * 0.5f + 0.4f;   // 0.4 = 플레이어 몸 반지름쯤

        for (int k = 0; k < 2; k++)
        {
            float rad = (angle + k * 90f) * Mathf.Deg2Rad;
            Vector2 dir = new(Mathf.Cos(rad), Mathf.Sin(rad));

            // 직선까지의 거리 = 방향과 수직인 성분
            float off = Mathf.Abs(p.x * dir.y - p.y * dir.x);
            if (off <= reach && Mathf.Abs(Vector2.Dot(p, dir)) <= LaserArm) return true;
        }

        return false;
    }

    // ───────────────────────── 물결 ─────────────────────────

    void TickWave(float dt)
    {
        poolUsed = 0;

        if (!waveRunning)
        {
            waveTimer -= dt;
            if (waveTimer <= 0f)
            {
                waveRunning = true;
                waveCenter = boss.transform.position;
                step = 0;
                stepStart = age;
                lastHitStep = -1;
            }
        }

        if (waveRunning)
        {
            // 다음 걸음으로
            while (age - stepStart >= StepTime)
            {
                stepStart += StepTime;
                step++;
            }

            if (step >= Steps)
            {
                waveRunning = false;
                waveTimer = WaveRest;
            }
            else
            {
                DrawWave();
                CheckWaveHit();
            }
        }

        // 이번 프레임에 안 쓴 선은 숨긴다
        for (int i = poolUsed; i < pool.Count; i++)
            if (pool[i].enabled) pool[i].enabled = false;
    }

    /// <summary>
    /// 걸음 s 에서 띠 i 가 켜져 있는가. 퍼져 나간 앞머리(i ≤ s) 안에서 한 칸 걸러 켜지고,
    /// 걸음마다 켜진 칸과 꺼진 칸이 뒤바뀐다(파도타기).
    /// </summary>
    static bool Active(int s, int i) => i >= 0 && i <= s && ((s - i) & 1) == 0;

    void DrawWave()
    {
        float phase = (age - stepStart) / StepTime;     // 이번 걸음 진행도 0~1
        float blink = 0.55f + 0.45f * Mathf.Sin(age * 22f);

        int maxBand = Mathf.Min(step + 1, Steps - 1);

        for (int i = 0; i <= maxBand; i++)
        {
            float inner = i * BandWidth;
            float outer = inner + BandWidth;

            if (Active(step, i))
            {
                // 켜진 띠 — 채움 + 양쪽 네온 테두리. 막 켜졌을 땐 더 밝게
                Color fill = BandFill;
                fill.a *= 1f + 0.8f * (1f - phase);
                Band(inner, outer, fill);
                Edge(outer, 0.28f, BandEdge);
                if (inner > 0f) Edge(inner, 0.28f, BandEdge);
            }

            // 다음 걸음에 켜질 띠 — 깜빡이는 가는 테두리로 미리 알려준다
            if (step + 1 < Steps && Active(step + 1, i))
            {
                Color t = TelegraphEdge;
                t.a *= blink * Mathf.Clamp01(phase * 1.6f);   // 걸음 후반으로 갈수록 또렷해진다
                Edge(outer - 0.15f, 0.14f, t);
                if (inner > 0f) Edge(inner + 0.15f, 0.14f, t);
            }
        }
    }

    void CheckWaveHit()
    {
        if (player == null || lastHitStep == step) return;
        if (age - stepStart < StepGrace) return;

        float d = Vector2.Distance(player.position, waveCenter);
        int band = Mathf.FloorToInt(d / BandWidth);

        if (!Active(step, band)) return;

        lastHitStep = step;
        Hurt(WaveDamage);
    }

    void Band(float inner, float outer, Color color)
    {
        LineRenderer lr = Take(0);
        float r = (inner + outer) * 0.5f;
        lr.startWidth = lr.endWidth = outer - inner;
        lr.startColor = lr.endColor = color;
        Circle(lr, waveCenter, r);
    }

    void Edge(float radius, float width, Color color)
    {
        LineRenderer lr = Take(1);
        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = color;
        Circle(lr, waveCenter, radius);
    }

    /// <summary>풀에서 선 하나를 꺼낸다. 채움은 테두리보다 아래에 그린다.</summary>
    LineRenderer Take(int layer)
    {
        LineRenderer lr;

        if (poolUsed < pool.Count)
        {
            lr = pool[poolUsed];
        }
        else
        {
            lr = Line("Wave", 1f, Color.white, "Background", 20);
            lr.loop = true;
            pool.Add(lr);
        }

        poolUsed++;
        lr.enabled = true;
        lr.sortingOrder = 20 + layer;
        return lr;
    }

    // ───────────────────────── 공용 ─────────────────────────

    void Hurt(float amount)
    {
        if (playerHealth != null && playerHealth.AcceptsDamage) playerHealth.TakeDamage(amount);
    }

    void Circle(LineRenderer lr, Vector2 center, float radius)
    {
        lr.positionCount = CircleSegments;
        for (int i = 0; i < CircleSegments; i++)
        {
            float a = i * Mathf.PI * 2f / CircleSegments;
            circleBuf[i] = new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, 0f);
        }
        lr.SetPositions(circleBuf);
    }

    LineRenderer Line(string name, float width, Color color, string sortingLayer, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = color;
        lr.numCapVertices = 2;
        lr.sharedMaterial = mat;
        lr.sortingLayerName = sortingLayer;
        lr.sortingOrder = order;

        return lr;
    }
}
