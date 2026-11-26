using UnityEngine;

/// <summary>
/// Ranged enemy AI (used by XSS &lt;script&gt; Caster).
/// Chases normally until within <see cref="fireRange"/> of the player, then locks movement,
/// telegraphs with <see cref="chargeSprite"/> and a subtle scale/color pulse, and fires
/// <see cref="projectilePrefab"/> toward the player before cooling down.
/// </summary>
[RequireComponent(typeof(Enemy), typeof(SpriteRenderer))]
public class RangedCaster : MonoBehaviour
{
    [Header("Targeting & Timing")]
    [Tooltip("Distance (world units) at which the enemy stops moving and begins aiming.")]
    [Min(0.5f)] public float fireRange = 9.0f;

    [Tooltip("Telegraph duration (seconds) before firing.")]
    [Min(0.05f)] public float chargeDuration = 0.8f;

    [Tooltip("Cooldown (seconds) after firing before the next charge begins.")]
    [Min(0.1f)] public float fireInterval = 2.8f;

    [Tooltip("Number of projectiles fired per burst after charging.")]
    [Min(1)] [SerializeField] int burstCount = 3;

    [Tooltip("Delay (seconds) between consecutive shots in a burst.")]
    [Min(0.02f)] [SerializeField] float burstInterval = 0.14f;

    [Header("Projectile")]
    [Tooltip("Projectile prefab with SemicolonProjectile component.")]
    public GameObject projectilePrefab;

    [Tooltip("Local offset from enemy center where the projectile spawns.")]
    public Vector2 muzzleOffset = Vector2.zero;

    [Header("Visuals")]
    [Tooltip("Idle/move animation frames (e.g., XSS_0, XSS_1).")]
    public Sprite[] idleFrames;

    [Tooltip("Sprite shown while telegraphing/charging a shot.")]
    public Sprite chargeSprite;

    [Tooltip("Optional sprite shown while despawning after death.")]
    public Sprite deathSprite;

    [Min(0.05f)] public float frameInterval = 0.25f;

    [Tooltip("Additive scale pulse amplitude during charge.")]
    [Range(0f, 0.4f)] public float chargePulseScale = 0.12f;

    [Tooltip("Peak tint color blended in during charge pulse.")]
    public Color chargePulseColor = new(1f, 0.72f, 0.55f, 1f);

    Enemy enemy;
    SpriteRenderer spriter;

    bool isCharging;
    float chargeTimer;
    bool isBursting;
    int burstRemaining;
    float burstTimer;
    float cooldownTimer;
    float frameTimer;
    int frameIndex;

    bool hasChargeScale;
    Vector3 chargeBaseScale;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        spriter = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        ResetState();

        if (spriter != null && idleFrames != null && idleFrames.Length > 0 && idleFrames[0] != null)
            spriter.sprite = idleFrames[0];
    }

    void OnDisable()
    {
        ResetState();
    }

    void ResetState()
    {
        isCharging = false;
        chargeTimer = 0f;
        isBursting = false;
        burstRemaining = 0;
        burstTimer = 0f;
        cooldownTimer = 0f;
        frameTimer = 0f;
        frameIndex = 0;

        if (hasChargeScale)
        {
            transform.localScale = chargeBaseScale;
            hasChargeScale = false;
        }

        if (spriter != null)
            spriter.color = Color.white;

        if (enemy != null)
            enemy.movementLocked = false;
    }

    void Update()
    {
        if (enemy == null) return;

        if (!enemy.IsAlive)
        {
            CancelCharge();
            CancelBurst();
            enemy.movementLocked = false;

            if (deathSprite != null && spriter != null && spriter.sprite != deathSprite)
                spriter.sprite = deathSprite;
            return;
        }

        Transform player = GameManager.instance != null && GameManager.instance.player != null
            ? GameManager.instance.player.transform
            : null;

        if (player == null)
        {
            CancelCharge();
            CancelBurst();
            enemy.movementLocked = false;
            TickIdleAnimation();
            return;
        }

        if (isBursting)
        {
            UpdateBurst(player.position);
            return;
        }

        float sqrDist = ((Vector2)transform.position - (Vector2)player.position).sqrMagnitude;
        bool inRange = sqrDist <= fireRange * fireRange;

        if (!inRange)
        {
            CancelCharge();
            enemy.movementLocked = false;
            TickIdleAnimation();
            if (cooldownTimer > 0f)
                cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
            return;
        }

        enemy.movementLocked = true;

        if (isCharging)
        {
            UpdateCharge(player.position);
            return;
        }

        TickIdleAnimation();

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        BeginCharge();
    }

    void BeginCharge()
    {
        isCharging = true;
        chargeTimer = 0f;
        chargeBaseScale = transform.localScale;
        hasChargeScale = true;

        if (spriter != null && chargeSprite != null)
            spriter.sprite = chargeSprite;
    }

    void UpdateCharge(Vector3 targetPosition)
    {
        chargeTimer += Time.deltaTime;
        float progress = chargeDuration > 0f ? Mathf.Clamp01(chargeTimer / chargeDuration) : 1f;
        float pulse = Mathf.Sin(progress * Mathf.PI * 3f) * 0.5f + 0.5f;

        if (hasChargeScale)
            transform.localScale = chargeBaseScale * (1f + pulse * chargePulseScale);

        if (spriter != null)
        {
            if (chargeSprite != null && spriter.sprite != chargeSprite)
                spriter.sprite = chargeSprite;
            spriter.color = Color.Lerp(Color.white, chargePulseColor, pulse);
        }

        if (chargeTimer < chargeDuration) return;

        CancelCharge();
        BeginBurst(targetPosition);
    }

    void BeginBurst(Vector3 targetPosition)
    {
        Fire(targetPosition);

        int remaining = Mathf.Max(1, burstCount) - 1;
        if (remaining > 0)
        {
            isBursting = true;
            burstRemaining = remaining;
            burstTimer = burstInterval;
        }
        else
        {
            CancelBurst();
            cooldownTimer = fireInterval;
        }
    }

    void UpdateBurst(Vector3 targetPosition)
    {
        enemy.movementLocked = true;
        TickIdleAnimation();

        burstTimer -= Time.deltaTime;
        if (burstTimer > 0f) return;

        Fire(targetPosition);
        burstRemaining--;

        if (burstRemaining > 0)
        {
            burstTimer = burstInterval;
        }
        else
        {
            CancelBurst();
            cooldownTimer = fireInterval;
        }
    }

    void CancelBurst()
    {
        isBursting = false;
        burstRemaining = 0;
        burstTimer = 0f;
    }

    void CancelCharge()
    {
        if (!isCharging && !hasChargeScale) return;

        isCharging = false;
        chargeTimer = 0f;

        if (hasChargeScale)
        {
            transform.localScale = chargeBaseScale;
            hasChargeScale = false;
        }

        if (spriter != null)
        {
            spriter.color = Color.white;
            if (idleFrames != null && idleFrames.Length > 0 && idleFrames[frameIndex % idleFrames.Length] != null)
                spriter.sprite = idleFrames[frameIndex % idleFrames.Length];
        }
    }

    void TickIdleAnimation()
    {
        if (spriter == null || idleFrames == null || idleFrames.Length == 0) return;

        if (idleFrames.Length == 1)
        {
            if (idleFrames[0] != null && spriter.sprite != idleFrames[0])
                spriter.sprite = idleFrames[0];
            return;
        }

        frameTimer += Time.deltaTime;
        if (frameTimer < frameInterval) return;

        frameTimer -= frameInterval;
        frameIndex = (frameIndex + 1) % idleFrames.Length;
        if (idleFrames[frameIndex] != null)
            spriter.sprite = idleFrames[frameIndex];
    }

    void Fire(Vector2 targetPosition)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[{name}] projectilePrefab is not assigned.", this);
            return;
        }

        Vector2 spawnAt = (Vector2)transform.position + muzzleOffset;
        GameObject go = PooledSpawner.Spawn(projectilePrefab, spawnAt, PoolType.Bullet);
        if (go == null)
            go = Instantiate(projectilePrefab, spawnAt, Quaternion.identity);

        if (go != null && go.TryGetComponent(out SemicolonProjectile projectile))
            projectile.Launch(targetPosition);
    }
}
