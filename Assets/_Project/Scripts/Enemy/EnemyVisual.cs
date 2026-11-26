using UnityEngine;

/// <summary>
/// 적의 겉모습을 한 곳에서 맡는다. <b>머터리얼 교체</b>(피격 흰색 · 탐색 강조)와
/// <b>사망 모션</b>(축소 · 페이드)을 여기서 한다. 상태이상 색조 같은 것도 같은 렌더러를 건드리므로 이 자리로 모일 것이다.
///
/// <b>렌더러 소유자를 하나로 둔 이유</b> — 탐색 강조와 피격 흰색이 각자 sharedMaterial 을 갈아끼우면
/// 흰색이 끝날 때 원본으로 되돌리면서 아직 유효한 탐색 강조까지 지워버린다.
/// 그래서 "지금 어떤 상태인가"만 기억하고 우선순위(피격·사망 &gt; 탐색 &gt; 원본)로 한 번에 정한다.
///
/// 프리팹을 손대지 않도록 <see cref="GetOrAdd"/> 로 필요할 때 붙인다. <see cref="MarkerHolder"/> 와 같은 방식이다.
/// </summary>
public class EnemyVisual : MonoBehaviour
{
    /// <summary>다음 깜빡임까지 최소 간격(초). 이게 없으면 연타당하는 적이 계속 하얗기만 해서 맞는 박자가 안 읽힌다.</summary>
    const float FlashGap = 0.05f;

    /// <summary>죽는 자리 파티클의 동시 상한을 세는 구간(초). 이 구간 안에서 <see cref="FxMax"/> 개까지만 낸다.</summary>
    const float FxWindow = 0.1f;

    const int FxMax = 8;
    const int HitSparkMax = 6;

    const float PunchDuration = 0.09f;
    const float PunchStretchX = 1.22f;
    const float PunchSquashY = 0.82f;

    const float DeathPopDuration = 0.05f;
    const float DeathPopScale = 1.22f;

    const string FlashPath = "HitFlashMaterial";
    const string HitSparkPath = "Fx_Spark_Base";
    const string ImmuneBadgePath = "DeadLock_ImmuneBadge";

    static Material flashMaterial;
    static bool flashSearched;

    static GameObject hitSparkPrefab;
    static bool hitSparkSearched;

    static Sprite immuneBadgeSprite;
    static bool immuneBadgeSearched;
    static float hitSparkWindowStart;
    static int hitSparkCount;

    static float fxWindowStart;
    static int fxCount;

    /// <summary>피격·사망 시 몸통에 씌울 흰색 머터리얼. <c>Resources/HitFlashMaterial</c>. 없으면 흰색 연출만 꺼진다.</summary>
    static Material FlashMaterial
    {
        get
        {
            if (flashSearched) return flashMaterial;

            flashSearched = true;
            flashMaterial = Resources.Load<Material>(FlashPath);

            if (flashMaterial == null)
                Debug.LogWarning($"[EnemyVisual] Resources/{FlashPath}.mat 이 없다. 피격 흰색이 나오지 않는다.");

            return flashMaterial;
        }
    }

    static GameObject HitSparkPrefab
    {
        get
        {
            if (hitSparkSearched) return hitSparkPrefab;

            hitSparkSearched = true;
            hitSparkPrefab = Resources.Load<GameObject>(HitSparkPath);
            return hitSparkPrefab;
        }
    }

    static Sprite ImmuneBadgeSprite
    {
        get
        {
            if (immuneBadgeSearched) return immuneBadgeSprite;

            immuneBadgeSearched = true;
            immuneBadgeSprite = Resources.Load<Sprite>(ImmuneBadgePath);
            return immuneBadgeSprite;
        }
    }

    SpriteRenderer body;
    SpriteRenderer eye;
    SpriteRenderer immuneBadge;
    Material bodyOriginal;
    Material eyeOriginal;
    bool renderersSearched;

    bool tagged;
    bool flashing;
    float flashDuration = 0.07f;
    float flashEndTime;
    float nextFlashTime;

    Vector3 baseScale = Vector3.one;
    bool hasBaseScale;
    bool punching;
    float punchStartTime;

    // 사망 모션. 죽는 동안은 항상 흰색이다 — 흰색이 곧 "죽는 중" 신호라서
    // 시체와 살아있는 적이 겹쳐도 한눈에 구분된다
    bool dying;
    EnemyData.DeathStyle deathStyle;
    float deathStartTime;
    float deathLength;
    Vector3 deathStartScale;
    Color deathBodyColor;
    Color deathEyeColor;

    /// <summary>없으면 붙여서 돌려준다.</summary>
    public static EnemyVisual GetOrAdd(Transform target)
    {
        if (target == null) return null;

        return target.TryGetComponent(out EnemyVisual visual)
            ? visual
            : target.gameObject.AddComponent<EnemyVisual>();
    }

    void Awake()
    {
        // 깜빡이거나 죽는 동안에만 Update 가 돈다. 적이 수백 마리여도 평소엔 비용이 없다
        baseScale = transform.localScale;
        hasBaseScale = true;
        enabled = false;
    }

    /// <summary>
    /// 몸통은 적 루트의 SpriteRenderer, 눈은 TagEye 가 붙은 자식이다.
    /// 처음 한 번만 찾는다 — 이때는 아직 아무도 머터리얼을 바꾸지 않았으므로 지금 값이 원본이다.
    /// </summary>
    void EnsureRenderers()
    {
        if (renderersSearched) return;

        renderersSearched = true;

        body = GetComponent<SpriteRenderer>();
        if (body != null) bodyOriginal = body.sharedMaterial;

        TagEye tagEye = GetComponentInChildren<TagEye>(true);
        if (tagEye != null)
        {
            eye = tagEye.GetComponent<SpriteRenderer>();
            eyeOriginal = eye.sharedMaterial;
        }
    }

    /// <summary>깜빡임 길이(초). 0 이하면 이 적은 깜빡이지 않는다. 적마다 EnemyData 가 정한다.</summary>
    public void SetFlashDuration(float seconds)
    {
        flashDuration = seconds;
    }

    /// <summary>피격 펀치 스케일의 기준 크기를 갱신한다. <see cref="Enemy.Init"/> 이 스케일을 정한 뒤 호출한다.</summary>
    public void SetBaseScale(Vector3 scale)
    {
        baseScale = scale;
        hasBaseScale = true;
        if (!punching && !dying) transform.localScale = baseScale;
        if (immuneBadge != null && immuneBadge.enabled) UpdateImmuneBadgeTransform();
    }

    /// <summary>외부 무적 오라 등으로 보호받는 동안 머리 위에 IMMUNE 배지를 띄운다.</summary>
    public void SetInvulnerableBadge(bool on)
    {
        if (!on)
        {
            if (immuneBadge != null) immuneBadge.enabled = false;
            return;
        }

        if (!gameObject.activeInHierarchy) return;

        Sprite badgeSprite = ImmuneBadgeSprite;
        if (badgeSprite == null) return;

        EnsureRenderers();

        if (immuneBadge == null)
        {
            var go = new GameObject("ImmuneBadge");
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);

            immuneBadge = go.AddComponent<SpriteRenderer>();
            immuneBadge.sprite = badgeSprite;
            immuneBadge.sortingLayerName = "Effect";
            immuneBadge.sortingOrder = 60;
        }

        UpdateImmuneBadgeTransform();
        immuneBadge.enabled = true;
    }

    void UpdateImmuneBadgeTransform()
    {
        if (immuneBadge == null) return;

        float localY = body != null && body.sprite != null ? body.sprite.bounds.max.y + 0.25f : 0.85f;
        immuneBadge.transform.localPosition = new Vector3(0f, localY, 0f);

        Vector3 parentScale = transform.localScale;
        float px = Mathf.Abs(parentScale.x) > 0.0001f ? parentScale.x : 1f;
        float py = Mathf.Abs(parentScale.y) > 0.0001f ? Mathf.Abs(parentScale.y) : 1f;
        immuneBadge.transform.localScale = new Vector3(1f / px, 1f / py, 1f);
    }

    /// <summary>탐색 표식이 붙어 있는 동안 켠다. 같은 값이면 아무것도 안 한다.</summary>
    public void SetTagged(bool on)
    {
        if (on == tagged) return;

        tagged = on;
        Apply();
    }

    /// <summary>
    /// 몸통을 잠깐 하얗게 한다. 이미 깜빡이는 중이거나 직전에 끝났으면 무시한다.
    /// 코루틴 대신 종료 시각을 적어두고 Update 에서 확인한다 — 코루틴은 호출마다 할당이 생긴다.
    /// </summary>
    public void Flash()
    {
        if (dying || flashDuration <= 0f || flashing) return;
        if (!gameObject.activeInHierarchy) return;

        float now = Time.time;
        if (now < nextFlashTime) return;
        if (FlashMaterial == null) return;

        EnsureRenderers();
        if (body == null) return;

        if (!hasBaseScale)
        {
            baseScale = transform.localScale;
            hasBaseScale = true;
        }

        flashing = true;
        flashEndTime = now + flashDuration;
        Apply();

        punching = true;
        punchStartTime = now;
        transform.localScale = new Vector3(
            baseScale.x * PunchStretchX,
            baseScale.y * PunchSquashY,
            baseScale.z);

        PlayHitSpark(transform.position, now);

        enabled = true;
    }

    /// <summary>
    /// 사망 모션을 시작한다. 죽는 동안 몸통은 흰색을 유지하며 줄어들고/투명해진다.
    ///
    /// <b>None 이면 아무것도 안 한다</b> — 사망 스프라이트·애니메이션이 있는 적은
    /// 흰색으로 덮으면 그림이 가려지기 때문이다. 길이는 호출자가 정한다(<see cref="EnemyData.DeathWait"/>).
    /// </summary>
    public void PlayDeath(EnemyData.DeathStyle style, float seconds)
    {
        if (immuneBadge != null) immuneBadge.enabled = false;

        if (style == EnemyData.DeathStyle.None || seconds <= 0f) return;
        if (!gameObject.activeInHierarchy) return;

        EnsureRenderers();
        if (body == null) return;

        ClearPunch();

        dying = true;
        deathStyle = style;
        deathStartTime = Time.time;
        deathLength = seconds;

        // 죽는 순간의 값을 기억해둔다. 풀로 돌아갈 때 이 값으로 되돌려야 다음 개체가 투명한 채로 안 나온다
        deathStartScale = transform.localScale;
        deathBodyColor = body.color;
        if (eye != null) deathEyeColor = eye.color;

        Apply();

        enabled = true;
    }

    /// <summary>
    /// 죽는 자리에 파티클을 낸다. <b>짧은 시간에 몇 개까지만</b> 낸다 —
    /// 잡몹이 한꺼번에 죽을 때 파티클이 전부 터지면 화면이 가려지고 프레임이 떨어진다.
    /// 상한을 넘으면 파티클만 건너뛰고 나머지 연출(축소·페이드)은 그대로 나간다.
    /// </summary>
    public static void PlayDeathFx(FxGroup fx, Vector2 at)
    {
        if (fx == null || fx.IsEmpty) return;

        float now = Time.time;

        if (now - fxWindowStart > FxWindow)
        {
            fxWindowStart = now;
            fxCount = 0;
        }

        if (fxCount >= FxMax) return;

        fxCount++;
        fx.PlayAt(at);
    }

    static void PlayHitSpark(Vector2 at, float now)
    {
        if (HitSparkPrefab == null) return;

        if (now - hitSparkWindowStart > FxWindow)
        {
            hitSparkWindowStart = now;
            hitSparkCount = 0;
        }

        if (hitSparkCount >= HitSparkMax) return;

        hitSparkCount++;
        PooledSpawner.Spawn(HitSparkPrefab, at, PoolType.Effect);
    }

    /// <summary>
    /// 풀에서 꺼낼 때 호출한다. 지난 개체가 깜빡이거나 죽는 도중 반납됐어도
    /// 흰 채로, 투명한 채로 나오지 않게 한다.
    /// </summary>
    public void ResetState()
    {
        ClearDeath();
        ClearPunch();

        flashing = false;
        nextFlashTime = 0f;
        if (immuneBadge != null) immuneBadge.enabled = false;
        enabled = false;

        if (renderersSearched) Apply();
    }

    void Update()
    {
        float now = Time.time;

        if (flashing && now >= flashEndTime)
        {
            flashing = false;
            nextFlashTime = now + FlashGap;
            Apply();
        }

        if (dying) TickDeath(now);
        else if (punching) TickPunch(now);

        if (!flashing && !punching && !dying) enabled = false;
    }

    void TickPunch(float now)
    {
        float t = Mathf.Clamp01((now - punchStartTime) / PunchDuration);

        if (t >= 1f)
        {
            ClearPunch();
            return;
        }

        float ease = (1f - t) * (1f - t);
        float sx = 1f + (PunchStretchX - 1f) * ease;
        float sy = 1f + (PunchSquashY - 1f) * ease;
        transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
    }

    void ClearPunch()
    {
        if (!punching) return;

        punching = false;
        if (hasBaseScale) transform.localScale = baseScale;
    }

    /// <summary>
    /// 죽음 진행도에 맞춰 크기와 투명도를 정한다. 끝난 뒤에는 건드리지 않는다 —
    /// 비활성화는 Enemy 가 같은 길이로 기다렸다가 한다.
    ///
    /// 처음 0.05초 동안 살짝 커졌다가(팝) 끝에서 확 줄어든다(1-t²).
    /// </summary>
    void TickDeath(float now)
    {
        float elapsed = now - deathStartTime;
        float popTime = Mathf.Min(DeathPopDuration, deathLength * 0.5f);

        if (elapsed < popTime)
        {
            float p = popTime > 0f ? Mathf.Clamp01(elapsed / popTime) : 1f;
            float popScale = 1f + (DeathPopScale - 1f) * Mathf.Sin(p * Mathf.PI * 0.5f);
            transform.localScale = deathStartScale * popScale;
            return;
        }

        float t = Mathf.Clamp01((elapsed - popTime) / Mathf.Max(0.0001f, deathLength - popTime));

        if (deathStyle == EnemyData.DeathStyle.Shrink || deathStyle == EnemyData.DeathStyle.ShrinkFade)
            transform.localScale = deathStartScale * (DeathPopScale * Mathf.Max(0f, 1f - t * t));

        if (deathStyle == EnemyData.DeathStyle.Fade || deathStyle == EnemyData.DeathStyle.ShrinkFade)
        {
            float a = 1f - t;

            Color c = deathBodyColor;
            c.a *= a;
            body.color = c;

            if (eye != null)
            {
                Color e = deathEyeColor;
                e.a *= a;
                eye.color = e;
            }
        }
    }

    /// <summary>죽는 동안 바꾼 값을 원래대로 돌려놓는다. 죽는 중이 아니면 아무것도 안 한다.</summary>
    void ClearDeath()
    {
        if (!dying) return;

        dying = false;

        // 스케일은 Enemy.Init 이 다시 정하지만, Init 이 안 불리는 경로에서도 0 으로 남지 않게 한다
        transform.localScale = deathStartScale;

        if (body != null) body.color = deathBodyColor;
        if (eye != null) eye.color = deathEyeColor;
    }

    // 깜빡이거나 죽는 도중 비활성화돼도(풀 반납) 원본으로 돌려놓는다. 안 그러면 다음 개체가 이전 상태로 나온다
    void OnDisable()
    {
        if (immuneBadge != null) immuneBadge.enabled = false;

        if (!flashing && !punching && !dying) return;

        flashing = false;
        ClearDeath();
        ClearPunch();
        enabled = false;
        Apply();
    }

    /// <summary>
    /// 지금 상태에 맞는 머터리얼을 한 번에 정한다. 우선순위는 피격·사망 &gt; 탐색 &gt; 원본.
    ///
    /// <b>sharedMaterial 을 쓴다.</b> material 로 접근하면 적마다 머터리얼이 복제돼서 배칭이 깨진다.
    /// 눈은 피격·사망으로 안 바뀌고 탐색 강조만 따른다.
    /// </summary>
    void Apply()
    {
        EnsureRenderers();

        if (body != null)
        {
            Material m = bodyOriginal;

            if ((flashing || dying) && FlashMaterial != null) m = FlashMaterial;
            else if (tagged && TagHighlight.Body != null) m = TagHighlight.Body;

            if (body.sharedMaterial != m) body.sharedMaterial = m;
        }

        if (eye != null)
        {
            Material m = tagged && TagHighlight.Eye != null ? TagHighlight.Eye : eyeOriginal;

            if (eye.sharedMaterial != m) eye.sharedMaterial = m;
        }
    }
}
