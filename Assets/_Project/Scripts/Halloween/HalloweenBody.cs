using UnityEngine;

/// <summary>
/// 적 몸통 스프라이트를 스킨으로 덮고, 상태(평소·기모으기·공격·피격·사망)에 맞는 그림·소리를 낸다.
/// <see cref="HalloweenSkin.ApplyEnemy"/> 가 붙인다.
///
/// <b>LateUpdate 에서 매 프레임 덮는 이유</b> — 적 애니메이터가 스프라이트를 키프레임으로 바꾼다.
/// 애니메이터는 그대로 돌게 두고(다른 키프레임·파라미터는 살아 있어야 한다) 그림만 마지막에 덮는다.
///
/// 머터리얼·색·뒤집기는 원래 렌더러 그대로라 피격 흰색·탐색 강조·사망 페이드가 그대로 먹는다.
///
/// <b>우선순위</b>: 사망 &gt; 피격 &gt; 공격 &gt; 기모으기 &gt; 평소.
/// 그림이 없는 상태는 건너뛰고 다음 순위로 내려간다 — 소리만 넣어도 동작한다.
/// </summary>
[DisallowMultipleComponent]
public class HalloweenBody : MonoBehaviour
{
    /// <summary>같은 목소리를 다시 내기까지의 간격. 잡몹이 떼로 죽을 때 목소리가 뭉개지지 않게.</summary>
    public const float VoiceInterval = 0.12f;

    /// <summary>사망 그림의 마지막 장을 잠깐 붙잡아 둔다. 바로 사라지면 마지막 표정이 안 읽힌다.</summary>
    public const float DeathHold = 0.25f;

    /// <summary>한 번 재생 상태(피격·공격)의 최소 길이. 그림이 한 장이어도 눈에 보여야 한다.</summary>
    const float MinOneShot = 0.18f;

    // 맞는 소리·공격 소리는 훨씬 자주 나므로 더 띄우고 작게 낸다
    const float HitInterval = 0.15f;
    const float HitVolume = 0.6f;
    const float AttackInterval = 0.2f;
    const float AttackVolume = 0.7f;

    // 글리치 — 3초에 한 번 0.3초 동안 지지직. 몬스터마다 시점이 달라 화면이 통째로 깜빡이지 않는다
    const float GlitchEvery = 3f;
    const float GlitchLength = 0.3f;
    const float GlitchStep = 0.05f;
    const float GlitchShift = 0.08f;    // 좌우로 튀는 폭(그림 너비 비율)
    const float GlitchStretch = 0.18f;  // 가로로 늘었다 줄었다 하는 폭
    static readonly Color GlitchRed = new(1f, 0.35f, 0.4f);
    static readonly Color GlitchCyan = new(0.35f, 1f, 1f);

    const float SquashAmount = 0.07f;
    const float SquashHz = 2.2f;

    /// <summary>원래 그림이 없을 때 쓰는 크기. 기존 적은 256px · PPU 128 = 2유닛이다.</summary>
    const float FallbackUnits = 2f;

    enum Slot { Idle, Hit, Attack, Charge, Death }

    SpriteRenderer body;
    SpriteRenderer eye;
    Enemy enemy;
    bool cached;
    bool eyeWasEnabled;

    bool measured;
    float originalUnits = FallbackUnits;
    Sprite originalSprite;

    HalloweenSkinDB.EnemySkin skin;
    Sprite[] idle, hit, attack, charge, death;

    // 페이즈별 원본. SetPhase 가 위의 idle·hit·attack·charge 를 이쪽으로 갈아끼운다
    Sprite[] idle1, hit1, attack1, charge1;
    Sprite[] idle2, hit2, attack2, charge2;
    Sprite[] glitchLeft2, glitchRight2;
    Sprite[] glitchLeft1, glitchRight1;

    // 글리치용 — 평소 그림을 피벗만 좌우로 민 것. 트랜스폼을 움직이면 물리와 부딪히므로 그림만 옮긴다
    Sprite[] glitchLeft, glitchRight;
    bool glitchOn;
    bool wasGlitching;
    float fps;
    bool bounce;
    float phase;
    Vector3 baseScale;

    // 한 번 재생 상태
    Slot oneShot;
    float oneShotStart;
    float oneShotEnd;

    bool charging;
    float chargeStart;

    bool deathSeen;
    float deathStart;

    /// <summary>프레임 수와 fps 로 한 번 재생의 길이를 잰다.</summary>
    public static float OneShotLength(int frames, float fps)
        => Mathf.Max(MinOneShot, frames / Mathf.Max(0.1f, fps));

    void Cache()
    {
        if (cached) return;

        cached = true;
        body = GetComponent<SpriteRenderer>();
        enemy = GetComponent<Enemy>();

        TagEye tagEye = GetComponentInChildren<TagEye>(true);
        if (tagEye != null) eye = tagEye.GetComponent<SpriteRenderer>();
        if (eye != null) eyeWasEnabled = eye.enabled;
    }

    /// <summary><paramref name="newSkin"/> 이 null 이면 원래대로 되돌린다.</summary>
    public void Setup(HalloweenSkinDB.EnemySkin newSkin, EnemyData data)
    {
        Cache();

        // 풀에서 재사용되므로 지난 개체의 상태를 지운다
        oneShotEnd = 0f;
        deathSeen = false;

        if (newSkin == null || body == null || data == null)
        {
            Restore();
            return;
        }

        skin = newSkin;

        // 원래 그림의 크기는 처음 한 번만 잰다. 이때는 아직 아무것도 덮지 않았다
        if (!measured)
        {
            measured = true;
            originalSprite = body.sprite;

            if (skin.referenceUnits > 0f)
            {
                originalUnits = skin.referenceUnits;
            }
            else if (originalSprite != null)
            {
                Vector3 size = originalSprite.bounds.size;
                originalUnits = Mathf.Max(size.x, size.y);
            }
        }

        // 모든 상태가 같은 PPU 를 쓴다 — 같은 캔버스(512)로 뽑은 그림이면 상태가 바뀌어도 크기가 안 튄다.
        // 기준은 평소 그림 첫 장, 없으면 있는 것 중 아무거나
        Sprite reference = First(skin.idleFrames) ?? First(skin.chargeFrames) ?? First(skin.attackFrames)
                           ?? First(skin.hitFrames) ?? First(skin.deathFrames);
        float ppu = HalloweenSkin.PpuFor(reference, originalUnits * skin.sizeMultiplier);

        idle = HalloweenSkin.Resized(data, (int)Slot.Idle, skin.idleFrames, ppu);
        hit = HalloweenSkin.Resized(data, (int)Slot.Hit, skin.hitFrames, ppu);
        attack = HalloweenSkin.Resized(data, (int)Slot.Attack, skin.attackFrames, ppu);
        charge = HalloweenSkin.Resized(data, (int)Slot.Charge, skin.chargeFrames, ppu);
        death = HalloweenSkin.Resized(data, (int)Slot.Death, skin.deathFrames, ppu);

        Sprite[] glitchSource = HalloweenSkin.Has(skin.idleFrames) ? skin.idleFrames
                              : HalloweenSkin.Has(skin.chargeFrames) ? skin.chargeFrames : skin.attackFrames;
        glitchLeft = HalloweenSkin.Resized(data, 20, glitchSource, ppu, GlitchShift);
        glitchRight = HalloweenSkin.Resized(data, 21, glitchSource, ppu, -GlitchShift);
        glitchOn = skin.glitch && glitchLeft.Length > 0;

        // 2페이즈 그림. 없는 상태는 1페이즈 그림을 그대로 쓴다
        idle1 = idle; hit1 = hit; attack1 = attack; charge1 = charge;
        glitchLeft1 = glitchLeft; glitchRight1 = glitchRight;

        idle2 = Or(HalloweenSkin.Resized(data, 30, skin.phase2Idle, ppu), idle1);
        hit2 = Or(HalloweenSkin.Resized(data, 31, skin.phase2Hit, ppu), hit1);
        attack2 = Or(HalloweenSkin.Resized(data, 32, skin.phase2Attack, ppu), attack1);
        charge2 = Or(HalloweenSkin.Resized(data, 33, skin.phase2Charge, ppu), charge1);

        if (HalloweenSkin.Has(skin.phase2Idle))
        {
            glitchLeft2 = HalloweenSkin.Resized(data, 34, skin.phase2Idle, ppu, GlitchShift);
            glitchRight2 = HalloweenSkin.Resized(data, 35, skin.phase2Idle, ppu, -GlitchShift);
        }
        else
        {
            glitchLeft2 = glitchLeft1;
            glitchRight2 = glitchRight1;
        }
        wasGlitching = false;

        // 지난 개체가 글리치 색인 채 반납됐을 수 있다
        body.color = Color.white;

        fps = Mathf.Max(0.1f, skin.fps);
        bounce = skin.bounce;
        phase = Random.value * 10f;

        // Enemy.Init 이 크기를 정한 직후라 지금 값이 이번 개체의 기준 크기다
        baseScale = transform.localScale;

        bool anySprite = idle.Length + hit.Length + attack.Length + charge.Length + death.Length > 0;

        // 눈 장식은 새 그림 위에 엉뚱하게 떠 있으므로 숨긴다. 소리만 바꾼 적은 그대로 둔다
        if (eye != null) eye.enabled = anySprite ? false : eyeWasEnabled;

        if (idle.Length > 0) body.sprite = idle[0];

        // 데드락은 Init 보다 먼저(OnEnable) 잠김 상태를 알리는데, 첫 스폰 땐 이 컴포넌트가 아직 없었다
        if (TryGetComponent(out DeadlockCycle cycle)) charging = cycle.IsLocked;

        enabled = true;
    }

    void Restore()
    {
        skin = null;
        idle = hit = attack = charge = death = null;
        glitchLeft = glitchRight = null;
        glitchOn = false;
        enabled = false;

        if (eye != null) eye.enabled = eyeWasEnabled;
        if (body != null && originalSprite != null) body.sprite = originalSprite;
    }

    // 풀로 돌아갈 때 기모으기 상태를 지운다. Setup 에서 안 지우는 이유는 위 데드락 주석 참고
    void OnDisable()
    {
        charging = false;
        oneShotEnd = 0f;
    }

    static Sprite[] Or(Sprite[] a, Sprite[] fallback) => a != null && a.Length > 0 ? a : fallback;

    /// <summary>페이즈를 바꾼다. 2면 p2_ 그림으로, 그 밖이면 원래 그림으로.</summary>
    public void SetPhase(int phase)
    {
        if (skin == null || idle1 == null) return;

        bool two = phase >= 2;
        idle = two ? idle2 : idle1;
        hit = two ? hit2 : hit1;
        attack = two ? attack2 : attack1;
        charge = two ? charge2 : charge1;
        glitchLeft = two ? glitchLeft2 : glitchLeft1;
        glitchRight = two ? glitchRight2 : glitchRight1;
    }

    static Sprite First(Sprite[] frames) => frames != null && frames.Length > 0 ? frames[0] : null;

    // ───────────────────────── 상태 신호 ─────────────────────────

    public void OnHit()
    {
        if (skin == null || !enemy.IsAlive) return;

        if (HalloweenSkin.Has(skin.hitClips))
            SfxPlayer.PlayAny(skin.hitClips, HalloweenSkin.VoiceVolume * HitVolume, HitInterval);

        // 연타당할 때 매번 처음부터 다시 틀면 첫 장에 붙박인다. 재생 중이면 그대로 둔다
        if (hit.Length > 0 && !Playing(Slot.Hit))
            StartOneShot(Slot.Hit, hit.Length);
    }

    public void OnAttack()
    {
        if (skin == null || !enemy.IsAlive) return;

        // 근접은 닿아 있는 동안 계속 불린다. 동작이 끝나기 전엔 다시 시작하지 않는다
        if (Playing(Slot.Attack)) return;

        if (HalloweenSkin.Has(skin.attackClips))
            SfxPlayer.PlayAny(skin.attackClips, HalloweenSkin.VoiceVolume * AttackVolume, AttackInterval);

        // 맞는 중이면 그림은 피격이 이긴다. 소리는 낸다
        if (attack.Length > 0 && !Playing(Slot.Hit))
            StartOneShot(Slot.Attack, attack.Length);
    }

    public void SetCharging(bool on)
    {
        if (skin == null || on == charging) return;

        charging = on;
        if (!on) return;

        chargeStart = Time.time;

        if (enemy.IsAlive && HalloweenSkin.Has(skin.chargeClips))
            SfxPlayer.PlayAny(skin.chargeClips, HalloweenSkin.VoiceVolume * AttackVolume, VoiceInterval);
    }

    bool Playing(Slot slot) => oneShot == slot && Time.time < oneShotEnd;

    void StartOneShot(Slot slot, int frames)
    {
        oneShot = slot;
        oneShotStart = Time.time;
        oneShotEnd = oneShotStart + OneShotLength(frames, fps);
    }

    // ───────────────────────── 그리기 ─────────────────────────

    void LateUpdate()
    {
        if (skin == null || body == null) return;

        float now = Time.time;
        bool alive = enemy == null || enemy.IsAlive;
        Sprite pick = null;

        if (!alive)
        {
            if (!deathSeen)
            {
                deathSeen = true;
                deathStart = now;
            }

            // 사망 그림이 없으면 마지막 모습 그대로 EnemyVisual 이 하얗게 줄인다
            if (death.Length > 0) pick = Once(death, now - deathStart);
        }
        else if (now < oneShotEnd && oneShot == Slot.Hit && hit.Length > 0)
        {
            pick = Once(hit, now - oneShotStart);
        }
        else if (now < oneShotEnd && oneShot == Slot.Attack && attack.Length > 0)
        {
            pick = Once(attack, now - oneShotStart);
        }
        else if (charging && charge.Length > 0)
        {
            pick = Loop(charge, now - chargeStart);
        }
        else if (idle.Length > 0)
        {
            pick = Loop(idle, now + phase);
        }

        // 글리치. 사망만 이긴다 — 짧아서 피격·공격 그림을 잠깐 덮어도 읽는 데 지장이 없다
        bool glitching = false;
        float stretch = 1f;

        if (alive && glitchOn)
        {
            float c = (now + phase) % GlitchEvery;

            if (c < GlitchLength)
            {
                glitching = true;
                int step = (int)(c / GlitchStep);

                pick = (step % 2 == 0 ? glitchLeft : glitchRight)[0];
                body.color = (step % 3) switch { 0 => GlitchRed, 1 => GlitchCyan, _ => Color.white };

                // -1 ~ +1 을 오가는 고정 패턴. 난수를 쓰면 프레임마다 값이 튀어 너무 산만하다
                stretch = 1f + ((step * 7 % 5) - 2) * 0.5f * GlitchStretch;
            }
        }

        // 글리치가 막 끝났으면 색을 돌려놓는다. 평소엔 색을 안 건드린다 — 죽음 페이드가 색을 쓴다
        bool glitchEnded = wasGlitching && !glitching;
        if (glitchEnded && alive) body.color = Color.white;
        wasGlitching = glitching;

        if (pick != null) body.sprite = pick;

        // 살아 있는 동안만 찌그러진다. 죽는 동안은 EnemyVisual 이 크기를 쥐고 있다
        if (alive && (bounce || glitching || glitchEnded))
        {
            float s = bounce ? Mathf.Sin((now + phase) * SquashHz * Mathf.PI * 2f) * SquashAmount : 0f;
            transform.localScale = new Vector3(baseScale.x * (1f + s) * stretch, baseScale.y * (1f - s), baseScale.z);
        }
    }

    Sprite Loop(Sprite[] frames, float t)
        => frames.Length > 1 ? frames[(int)(Mathf.Max(0f, t) * fps) % frames.Length] : frames[0];

    /// <summary>한 번 재생. 끝나면 마지막 장에서 멈춘다.</summary>
    Sprite Once(Sprite[] frames, float t)
        => frames[Mathf.Min(frames.Length - 1, (int)(Mathf.Max(0f, t) * fps))];
}
