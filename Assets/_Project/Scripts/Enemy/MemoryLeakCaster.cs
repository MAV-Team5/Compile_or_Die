using UnityEngine;

/// <summary>
/// Memory Leak (V1 Cracked Purple Pipe).
/// Keeps distance from the player (kiting), stops to telegraph, and lobs arcing toxic puddles
/// onto a random point near the player.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class MemoryLeakCaster : MonoBehaviour
{
    [Header("Kiting & Range")]
    [Tooltip("Minimum distance to maintain from the player; retreats when closer.")]
    [Min(0.5f)] public float minKeepDistance = 5.5f;

    [Tooltip("Distance at which the enemy stops approaching and holds position.")]
    [Min(1f)] public float maxEngageDistance = 9.0f;

    [Tooltip("Movement speed while retreating from the player.")]
    [Min(0.1f)] public float retreatSpeed = 2.6f;

    [Header("Leak Puddle")]
    [Tooltip("Prefab with MemoryLeakPuddle component to spawn on the ground.")]
    public GameObject puddlePrefab;

    [Tooltip("Optional sprite for the arcing blob (falls back to puddlePrefab's SpriteRenderer sprite).")]
    public Sprite blobSprite;

    [Tooltip("Seconds between puddle lobs.")]
    [Min(0.2f)] public float dropInterval = 3.8f;

    [Tooltip("Initial delay after spawn before lobbing the first puddle.")]
    [Min(0f)] public float firstDropDelay = 1.5f;

    [Tooltip("Telegraph stop duration before launching the puddle blob.")]
    [Min(0.05f)] public float telegraphDuration = 0.35f;

    [Tooltip("Random landing radius around the player's position.")]
    [Min(0f)] public float lobRadius = 3.2f;

    [Tooltip("Flight duration (seconds) of the arcing puddle blob.")]
    [Min(0.1f)] public float lobFlightDuration = 0.45f;

    [Tooltip("Peak vertical arc height (world units) of the lobbed blob.")]
    [Min(0f)] public float lobArcHeight = 1.5f;

    [Tooltip("Subtle purple tint blended in during telegraph.")]
    public Color telegraphColor = new(0.85f, 0.45f, 1f, 1f);

    [Header("Sprite Animation")]
    [Tooltip("Walk/idle frames (e.g., MemoryLeak_0, MemoryLeak_1).")]
    public Sprite[] walkFrames;

    [Tooltip("Optional death sprite shown while despawning.")]
    public Sprite deathSprite;

    [Min(0.05f)] public float frameInterval = 0.25f;

    Enemy enemy;
    SpriteRenderer spriter;
    float dropTimer;
    float frameTimer;
    int frameIndex;

    bool isTelegraphing;
    float telegraphTimer;
    bool hasTelegraphScale;
    Vector3 telegraphBaseScale;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        spriter = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        ResetState();

        if (spriter != null && walkFrames != null && walkFrames.Length > 0)
            spriter.sprite = walkFrames[0];
    }

    void OnDisable()
    {
        ResetState();
    }

    void ResetState()
    {
        dropTimer = firstDropDelay;
        frameTimer = 0f;
        frameIndex = 0;

        CancelTelegraph();

        if (enemy != null)
            enemy.movementLocked = false;
    }

    void Update()
    {
        if (enemy == null) return;

        if (!enemy.IsAlive)
        {
            CancelTelegraph();
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
            CancelTelegraph();
            enemy.movementLocked = false;
            TickAnimation();
            return;
        }

        if (isTelegraphing)
        {
            enemy.movementLocked = true;
            UpdateTelegraph(player.position);
            return;
        }

        TickAnimation();

        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        float dist = toPlayer.magnitude;

        if (dist < minKeepDistance)
        {
            enemy.movementLocked = true;
            Vector2 awayDir = dist > 0.001f ? -toPlayer / dist : Vector2.right;
            transform.position += (Vector3)(awayDir * retreatSpeed * Time.deltaTime);
        }
        else if (dist <= maxEngageDistance)
        {
            enemy.movementLocked = true;
        }
        else
        {
            enemy.movementLocked = false;
        }

        dropTimer -= Time.deltaTime;
        if (dropTimer > 0f) return;

        if (dist <= maxEngageDistance + 2f)
            BeginTelegraph();
        else
            dropTimer = 0f;
    }

    void BeginTelegraph()
    {
        isTelegraphing = true;
        telegraphTimer = 0f;
        telegraphBaseScale = transform.localScale;
        hasTelegraphScale = true;
        enemy.movementLocked = true;
    }

    void UpdateTelegraph(Vector2 playerPos)
    {
        telegraphTimer += Time.deltaTime;
        float progress = telegraphDuration > 0f ? Mathf.Clamp01(telegraphTimer / telegraphDuration) : 1f;
        float pulse = Mathf.Sin(progress * Mathf.PI);

        if (hasTelegraphScale)
        {
            transform.localScale = new Vector3(
                telegraphBaseScale.x * (1f + 0.14f * pulse),
                telegraphBaseScale.y * (1f - 0.12f * pulse),
                telegraphBaseScale.z);
        }

        if (spriter != null)
            spriter.color = Color.Lerp(Color.white, telegraphColor, pulse);

        if (telegraphTimer < telegraphDuration) return;

        CancelTelegraph();
        LobPuddle(playerPos);
        dropTimer = dropInterval;
    }

    void CancelTelegraph()
    {
        isTelegraphing = false;
        telegraphTimer = 0f;

        if (hasTelegraphScale)
        {
            transform.localScale = telegraphBaseScale;
            hasTelegraphScale = false;
        }

        if (spriter != null)
            spriter.color = Color.white;
    }

    void TickAnimation()
    {
        if (spriter == null || walkFrames == null || walkFrames.Length <= 1) return;

        frameTimer += Time.deltaTime;
        if (frameTimer < frameInterval) return;

        frameTimer -= frameInterval;
        frameIndex = (frameIndex + 1) % walkFrames.Length;
        spriter.sprite = walkFrames[frameIndex];
    }

    void LobPuddle(Vector2 playerPos)
    {
        if (puddlePrefab == null) return;

        Vector2 targetPos = playerPos + Random.insideUnitCircle * lobRadius;
        Sprite[] boilFrames = puddlePrefab.TryGetComponent(out MemoryLeakPuddle puddle) ? puddle.BoilFrames : null;
        float boilInterval = puddle != null ? puddle.BoilFrameInterval : 0.16f;
        Sprite sprite = blobSprite != null
            ? blobSprite
            : (boilFrames != null && boilFrames.Length > 0 && boilFrames[0] != null
                ? boilFrames[0]
                : (puddlePrefab.TryGetComponent(out SpriteRenderer puddleSr) ? puddleSr.sprite : null));

        var go = new GameObject("MemoryLeakLobBlob");
        go.transform.position = transform.position;
        Vector3 puddleScale = puddlePrefab.transform.localScale;
        go.transform.localScale = new Vector3(puddleScale.x * 0.65f, puddleScale.y * 0.65f, puddleScale.z);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Item";
        sr.sortingOrder = 5;
        sr.color = new Color(0.85f, 1f, 0.85f, 0.95f);

        var runner = go.AddComponent<MemoryLeakBlobArc>();
        runner.Init(puddlePrefab, transform.position, targetPos, lobFlightDuration, lobArcHeight, sr, boilFrames, boilInterval);
    }
}

sealed class MemoryLeakBlobArc : MonoBehaviour
{
    GameObject puddlePrefab;
    Vector2 startPos;
    Vector2 targetPos;
    float duration;
    float arcHeight;
    float elapsed;
    Vector3 baseScale;
    SpriteRenderer sr;
    Sprite[] boilFrames;
    float boilFrameInterval;
    float boilTimer;
    int boilIndex;

    public void Init(
        GameObject prefab,
        Vector2 start,
        Vector2 target,
        float flightDuration,
        float height,
        SpriteRenderer renderer = null,
        Sprite[] frames = null,
        float frameInterval = 0.16f)
    {
        puddlePrefab = prefab;
        startPos = start;
        targetPos = target;
        duration = Mathf.Max(0.05f, flightDuration);
        arcHeight = height;
        elapsed = 0f;
        baseScale = transform.localScale;
        sr = renderer != null ? renderer : GetComponent<SpriteRenderer>();
        boilFrames = frames;
        boilFrameInterval = Mathf.Max(0.04f, frameInterval);
        boilTimer = 0f;
        boilIndex = 0;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        Vector2 pos = Vector2.Lerp(startPos, targetPos, t);
        pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
        transform.position = pos;

        float pulse = 0.5f - 0.5f * Mathf.Cos(elapsed * 18f);
        float scaleBoost = 1f + 0.25f * pulse;
        transform.localScale = new Vector3(baseScale.x * scaleBoost, baseScale.y * scaleBoost, baseScale.z);
        transform.Rotate(0f, 0f, 540f * Time.deltaTime);

        if (sr != null && boilFrames != null && boilFrames.Length > 1)
        {
            boilTimer += Time.deltaTime;
            if (boilTimer >= boilFrameInterval)
            {
                boilTimer -= boilFrameInterval;
                boilIndex = (boilIndex + 1) % boilFrames.Length;
                if (boilFrames[boilIndex] != null)
                    sr.sprite = boilFrames[boilIndex];
            }
        }

        if (t < 1f) return;

        if (puddlePrefab != null)
            PooledSpawner.Spawn(puddlePrefab, targetPos, PoolType.Effect);

        Destroy(gameObject);
    }
}
