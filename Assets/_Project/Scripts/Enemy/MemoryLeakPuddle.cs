using UnityEngine;

/// <summary>
/// Toxic green puddle dropped by Memory Leak.
/// Deals light DoT and slows the player while they stand on it, then despawns via PooledSpawner.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class MemoryLeakPuddle : MonoBehaviour
{
    [Header("Lifetime")]
    [Min(0.5f)] [SerializeField] float duration = 4.5f;
    [Min(0.05f)] [SerializeField] float fadeDuration = 0.5f;

    [Header("Hazard")]
    [Min(0f)] [SerializeField] float damagePerTick = 1.5f;
    [Min(0.1f)] [SerializeField] float tickInterval = 0.5f;

    [Tooltip("Speed multiplier applied to player while inside (e.g., 0.65 = 35% slow).")]
    [Range(0.2f, 1f)] [SerializeField] float slowMultiplier = 0.65f;
    [Min(0.05f)] [SerializeField] float slowRefreshDuration = 0.25f;

    SpriteRenderer sr;
    CircleCollider2D col;
    Color baseColor = Color.white;
    Vector3 baseScale = Vector3.one;

    float elapsed;
    float tickTimer;
    Player playerInside;
    PlayerHealth playerHealthInside;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();

        // Keep out of the Enemy layer so player targeting/augments don't treat puddles as enemies.
        gameObject.layer = LayerMask.NameToLayer("Default");

        if (col != null) col.isTrigger = true;
        if (sr != null)
        {
            sr.sortingLayerName = "Item";
            sr.sortingOrder = 1;
            baseColor = sr.color;
        }

        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        elapsed = 0f;
        tickTimer = 0f;
        playerInside = null;
        playerHealthInside = null;

        if (sr != null) sr.color = baseColor;
        transform.localScale = baseScale;
    }

    void OnDisable()
    {
        playerInside = null;
        playerHealthInside = null;

        if (sr != null) sr.color = baseColor;
        transform.localScale = baseScale;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= duration)
        {
            PooledSpawner.Despawn(gameObject);
            return;
        }

        UpdateVisual();
        TickHazard();
    }

    void UpdateVisual()
    {
        if (sr == null) return;

        float remain = duration - elapsed;
        float alpha = remain < fadeDuration ? Mathf.Clamp01(remain / fadeDuration) : 1f;
        float pulse = 0.88f + 0.12f * Mathf.Sin(elapsed * 5f);

        Color c = baseColor;
        c.a = baseColor.a * alpha * pulse;
        sr.color = c;
    }

    void TickHazard()
    {
        if (playerInside == null && playerHealthInside == null) return;

        if (playerInside != null && slowMultiplier < 1f && playerInside.SpeedMultiplier <= 1f)
            playerInside.ApplySpeedBoost(slowMultiplier, slowRefreshDuration);

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f) return;

        tickTimer = tickInterval;
        if (damagePerTick > 0f && playerHealthInside != null && playerHealthInside.AcceptsDamage)
            playerHealthInside.TakeDamage(damagePerTick);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Only match the player's body collider directly (avoid child weapon Area colliders).
        if (!other.TryGetComponent(out PlayerHealth ph)) return;

        playerHealthInside = ph;
        other.TryGetComponent(out playerInside);
        tickTimer = 0f;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerHealth ph)) return;
        if (ph != playerHealthInside) return;

        playerHealthInside = null;
        playerInside = null;
    }
}
