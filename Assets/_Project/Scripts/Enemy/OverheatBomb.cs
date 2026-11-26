using UnityEngine;

/// <summary>
/// Overheat (V2 Thermal Bomb).
/// Chases the player; when within fuseRadius, stops, flashes/pulses faster over fuseDuration
/// with a visible pixel warning ring, then explodes dealing AoE damage within blastRadius and retires.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class OverheatBomb : MonoBehaviour
{
    [Header("Fuse & Blast")]
    [Min(0.2f)] public float fuseRadius = 2.5f;
    [Min(0.2f)] public float fuseDuration = 1.2f;
    [Min(0.5f)] public float blastRadius = 2.75f;
    [Min(1f)] public float blastDamage = 10f;

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
    Color ringBaseColor = new(1f, 0.29f, 0.24f, 0.7f);

    bool fusing;
    float fuseTimer;
    float frameTimer;
    int frameIndex;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        spriter = GetComponent<SpriteRenderer>();
        if (spriter != null) baseColor = spriter.color;

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
        fuseTimer = 0f;
        frameTimer = 0f;
        frameIndex = 0;

        if (enemy != null) enemy.movementLocked = false;

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
                float distSq = (enemy.target.position - (Vector2)transform.position).sqrMagnitude;
                if (distSq <= fuseRadius * fuseRadius)
                    BeginFuse();
            }
            return;
        }

        TickFuse();
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

    void TickFuse()
    {
        fuseTimer += Time.deltaTime;
        float t = Mathf.Clamp01(fuseTimer / Mathf.Max(0.01f, fuseDuration));

        // Flash faster as detonation approaches.
        float freq = Mathf.Lerp(5f, 22f, t);
        float pulse = 0.5f + 0.5f * Mathf.Sin(fuseTimer * freq * Mathf.PI * 2f);

        if (spriter != null)
            spriter.color = Color.Lerp(baseColor, new Color(1f, 0.92f, 0.55f, 1f), pulse);

        if (warningRing != null)
        {
            UpdateRingTransform(t);
            Color rc = ringBaseColor;
            rc.a = Mathf.Lerp(0.35f, 0.95f, pulse);
            warningRing.color = rc;
        }

        if (fuseTimer >= fuseDuration)
            Explode();
    }

    void UpdateRingTransform(float t)
    {
        // Overheat_ring.png is 96x96 at 48 PPU -> 2x2 world units (radius = 1.0 unit at localScale 1).
        float parentScale = Mathf.Max(0.01f, Mathf.Abs(transform.localScale.x));
        float targetLocalScale = (blastRadius / parentScale) * Mathf.Lerp(0.75f, 1f, t);
        warningRing.transform.localScale = new Vector3(targetLocalScale, targetLocalScale, 1f);
    }

    void Explode()
    {
        if (enemy.target != null)
        {
            float distSq = (enemy.target.position - (Vector2)transform.position).sqrMagnitude;
            if (distSq <= blastRadius * blastRadius
                && enemy.target.TryGetComponent(out PlayerHealth ph)
                && ph.AcceptsDamage)
            {
                ph.TakeDamage(blastDamage);
            }
        }

        explodeFx.PlayAt(transform.position, default, blastRadius, transform);

        if (warningRing != null)
            warningRing.gameObject.SetActive(false);

        enemy.Retire();
    }
}
