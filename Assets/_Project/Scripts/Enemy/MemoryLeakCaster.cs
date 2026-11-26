using UnityEngine;

/// <summary>
/// Memory Leak (V1 Cracked Purple Pipe).
/// Moves steadily while periodically dropping a glowing green toxic puddle behind it.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class MemoryLeakCaster : MonoBehaviour
{
    [Header("Leak Puddle")]
    [Tooltip("Prefab with MemoryLeakPuddle component to spawn on the ground.")]
    public GameObject puddlePrefab;

    [Tooltip("Seconds between puddle drops.")]
    [Min(0.2f)] public float dropInterval = 2.0f;

    [Tooltip("Initial delay after spawn before dropping the first puddle.")]
    [Min(0f)] public float firstDropDelay = 0.8f;

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

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        spriter = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        dropTimer = firstDropDelay;
        frameTimer = 0f;
        frameIndex = 0;

        if (spriter != null && walkFrames != null && walkFrames.Length > 0)
            spriter.sprite = walkFrames[0];
    }

    void OnDisable()
    {
        dropTimer = 0f;
        frameTimer = 0f;
        frameIndex = 0;
    }

    void Update()
    {
        if (enemy == null) return;

        if (!enemy.IsAlive)
        {
            if (deathSprite != null && spriter != null && spriter.sprite != deathSprite)
                spriter.sprite = deathSprite;
            return;
        }

        TickAnimation();

        dropTimer -= Time.deltaTime;
        if (dropTimer > 0f) return;

        dropTimer = dropInterval;
        DropPuddle();
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

    void DropPuddle()
    {
        if (puddlePrefab == null) return;

        PooledSpawner.Spawn(puddlePrefab, transform.position, PoolType.Effect);
    }
}
