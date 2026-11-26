using UnityEngine;

/// <summary>
/// Overheat (V2 Thermal Bomb).
/// Chases the player; when within fuseRadius, starts a 3s countdown, chases faster toward the player,
/// attaches on contact (within attachRadius), pulses WarningRing to blastRadius, then explodes and retires.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class OverheatBomb : MonoBehaviour
{
    [Header("Fuse & Blast")]
    [Min(0.2f)] public float fuseRadius = 4.5f;
    [Min(0.2f)] public float fuseDuration = 3.0f;
    [Min(0.5f)] public float blastRadius = 4.2f;
    [Min(1f)] public float blastDamage = 10f;

    [Header("Fuse Chase & Attach")]
    [Min(0.5f)] public float fuseChaseSpeedMultiplier = 1.15f;
    [Min(0.5f)] public float fuseChaseSpeed = 9.2f;
    [Min(0.1f)] public float attachRadius = 0.9f;

    [Header("Visuals")]
    public Sprite[] walkFrames;
    public Sprite fuseSprite;
    public SpriteRenderer warningRing;
    [Min(0.05f)] public float frameInterval = 0.25f;

    [Header("FX (Optional)")]
    [Fx("Explode", "Self")] public FxGroup explodeFx = new();

    Enemy enemy;
    SpriteRenderer spriter;
    Color baseColor = Color.white;
    Color ringBaseColor = new(1f, 0.882f, 0.302f, 0.75f);
    Vector3 baseScale = Vector3.one;

    static readonly Color RingGold = new(1f, 0.882f, 0.302f, 1f);    // #FFE14D
    static readonly Color RingOrange = new(1f, 0.561f, 0.122f, 1f);  // #FF8F1F
    static readonly Color RingWhite = Color.white;                   // #FFFFFF
    static readonly Color BlastRed = new(1f, 0.294f, 0.239f, 1f);    // #FF4B3D

    bool fusing;
    bool attached;
    Vector2 attachOffset;
    float fuseTimer;
    float frameTimer;
    int frameIndex;

    Rigidbody2D cachedTarget;
    Player cachedPlayer;

    const float AttachOffsetDistance = 0.45f;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        spriter = GetComponent<SpriteRenderer>();
        if (spriter != null) baseColor = spriter.color;
        baseScale = transform.localScale;

        if (warningRing != null)
        {
            ringBaseColor = warningRing.color;
            warningRing.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        ResetState();
    }

    void OnDisable()
    {
        ResetState();
    }

    void ResetState()
    {
        fusing = false;
        attached = false;
        attachOffset = Vector2.zero;
        fuseTimer = 0f;
        frameTimer = 0f;
        frameIndex = 0;
        cachedTarget = null;
        cachedPlayer = null;

        if (enemy != null) enemy.movementLocked = false;
        transform.localScale = baseScale;

        if (spriter != null)
        {
            spriter.color = baseColor;
            if (walkFrames != null && walkFrames.Length > 0)
                spriter.sprite = walkFrames[0];
        }

        if (warningRing != null)
        {
            warningRing.color = ringBaseColor;
            warningRing.transform.localScale = Vector3.one;
            warningRing.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (enemy == null || !enemy.IsAlive)
        {
            if (warningRing != null && warningRing.gameObject.activeSelf)
                warningRing.gameObject.SetActive(false);
            return;
        }

        if (!fusing)
        {
            TickWalkAnimation();

            if (enemy.target != null)
            {
                float distSq = ((Vector2)enemy.target.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (distSq <= fuseRadius * fuseRadius)
                    BeginFuse();
            }
            return;
        }

        TickFuse();
    }

    void LateUpdate()
    {
        if (!fusing || !attached || enemy == null || !enemy.IsAlive || enemy.target == null)
            return;

        Vector2 playerPos = enemy.target.transform.position;
        transform.position = playerPos + attachOffset;
    }

    void TickWalkAnimation()
    {
        if (spriter == null || walkFrames == null || walkFrames.Length <= 1) return;

        frameTimer += Time.deltaTime;
        if (frameTimer < frameInterval) return;

        frameTimer -= frameInterval;
        frameIndex = (frameIndex + 1) % walkFrames.Length;
        spriter.sprite = walkFrames[frameIndex];
    }

    void BeginFuse()
    {
        fusing = true;
        attached = false;
        attachOffset = Vector2.zero;
        fuseTimer = 0f;
        enemy.movementLocked = true;

        if (fuseSprite != null && spriter != null)
            spriter.sprite = fuseSprite;

        if (warningRing != null)
        {
            warningRing.gameObject.SetActive(true);
            UpdateRingTransform(0f);
        }
    }

    float GetChaseSpeed()
    {
        if (enemy.target != cachedTarget)
        {
            cachedTarget = enemy.target;
            cachedPlayer = cachedTarget != null ? cachedTarget.GetComponent<Player>() : null;
        }

        if (cachedPlayer != null && cachedPlayer.CurrentSpeed > 0f)
            return cachedPlayer.CurrentSpeed * fuseChaseSpeedMultiplier;

        return fuseChaseSpeed;
    }

    void TickFuse()
    {
        fuseTimer += Time.deltaTime;
        float t = Mathf.Clamp01(fuseTimer / Mathf.Max(0.01f, fuseDuration));

        if (!attached && enemy.target != null)
        {
            Vector2 playerPos = enemy.target.transform.position;
            Vector2 pos = transform.position;
            float distSq = (pos - playerPos).sqrMagnitude;

            if (distSq <= attachRadius * attachRadius)
            {
                attached = true;
                Vector2 diff = pos - playerPos;
                attachOffset = diff.sqrMagnitude > 0.0001f
                    ? diff.normalized * AttachOffsetDistance
                    : Vector2.right * AttachOffsetDistance;
                transform.position = playerPos + attachOffset;
            }
            else
            {
                transform.position = Vector2.MoveTowards(pos, playerPos, GetChaseSpeed() * Time.deltaTime);
            }
        }

        // Flash faster as detonation approaches.
        float freq = Mathf.Lerp(5f, 22f, t);
        float pulse = 0.5f + 0.5f * Mathf.Sin(fuseTimer * freq * Mathf.PI * 2f);

        if (spriter != null)
            spriter.color = Color.Lerp(baseColor, RingWhite, pulse * (0.35f + 0.65f * t));

        // Pulse bomb body scale faster as detonation nears.
        float bodyPulse = Mathf.Lerp(1f, 1.18f, pulse * (0.4f + 0.6f * t));
        transform.localScale = baseScale * bodyPulse;

        if (warningRing != null)
        {
            UpdateRingTransform(t, pulse);
            Color rc = t < 0.5f
                ? Color.Lerp(RingGold, RingOrange, t * 2f)
                : Color.Lerp(RingOrange, RingWhite, (t - 0.5f) * 2f);
            rc.a = Mathf.Lerp(0.5f, 0.95f, pulse);
            warningRing.color = rc;
        }

        if (fuseTimer >= fuseDuration)
            Explode();
    }

    void UpdateRingTransform(float t, float pulse = 0f)
    {
        // Overheat_ring.png is 384x384 at 192 PPU -> 2x2 world units (radius = 1.0 unit at localScale 1).
        float parentScale = Mathf.Max(0.01f, Mathf.Abs(transform.localScale.x));
        float expand = Mathf.Lerp(0.5f, 1f, t) * (1f + 0.05f * pulse);
        float targetLocalScale = (blastRadius / parentScale) * expand;
        warningRing.transform.localScale = new Vector3(targetLocalScale, targetLocalScale, 1f);
    }

    void Explode()
    {
        Vector2 center = transform.position;

        if (enemy.target != null)
        {
            float distSq = (enemy.target.position - center).sqrMagnitude;
            if (distSq <= blastRadius * blastRadius
                && enemy.target.TryGetComponent(out PlayerHealth ph)
                && ph.AcceptsDamage)
            {
                ph.TakeDamage(blastDamage);
            }
        }

        explodeFx.PlayAt(center, default, blastRadius, transform);

        // Extra offset spark bursts for an unmistakable detonation.
        float burstOffset = Mathf.Min(0.8f, blastRadius * 0.2f);
        Vector2 burstDir = Random.insideUnitCircle;
        burstDir = burstDir.sqrMagnitude > 0.001f ? burstDir.normalized : Vector2.right;
        explodeFx.PlayAt(center + burstDir * burstOffset, default, blastRadius * 0.85f, transform);
        explodeFx.PlayAt(center - burstDir * burstOffset, default, blastRadius * 0.85f, transform);

        SpawnDetonationShockwave(center);

        if (warningRing != null)
            warningRing.gameObject.SetActive(false);

        enemy.Retire();
    }

    void SpawnDetonationShockwave(Vector2 center)
    {
        if (warningRing == null || warningRing.sprite == null)
            return;

        var go = new GameObject("OverheatShockwave");
        go.transform.position = center;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = warningRing.sprite;
        sr.sortingLayerName = "Effect";
        sr.sortingOrder = 55;
        sr.color = RingWhite;

        var fx = go.AddComponent<OverheatShockwaveFx>();
        fx.Init(sr, blastRadius * 0.6f, blastRadius * 1.35f, 0.25f);
    }

    sealed class OverheatShockwaveFx : MonoBehaviour
    {
        SpriteRenderer sr;
        float startScale;
        float endScale;
        float duration;
        float elapsed;

        public void Init(SpriteRenderer renderer, float fromScale, float toScale, float fxDuration)
        {
            sr = renderer;
            startScale = fromScale;
            endScale = toScale;
            duration = Mathf.Max(0.01f, fxDuration);
            elapsed = 0f;
            transform.localScale = new Vector3(startScale, startScale, 1f);
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float s = Mathf.Lerp(startScale, endScale, t);
            transform.localScale = new Vector3(s, s, 1f);

            if (sr != null)
            {
                Color c = t < 0.5f
                    ? Color.Lerp(RingWhite, RingGold, t * 2f)
                    : Color.Lerp(RingGold, BlastRed, (t - 0.5f) * 2f);
                c.a = 1f - t;
                sr.color = c;
            }

            if (elapsed >= duration)
                Destroy(gameObject);
        }
    }
}
