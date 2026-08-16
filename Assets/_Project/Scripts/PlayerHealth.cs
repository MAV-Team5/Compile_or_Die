using UnityEngine;

/// <summary>
/// 플레이어 체력. 적과 겹쳐 있는 동안 계속 피해를 받는다 — 뱀서라이크라 무적 시간은 없다.
/// 피격 시 캐릭터를 붉게 물들이고, 죽으면 GameManager에 알린다.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageReceiver
{
    [SerializeField] float maxHealth = 100f;

    [Header("피격 연출")]
    [SerializeField] Color hitColor = new(1f, 0.35f, 0.35f);
    [SerializeField] float flashDuration = 0.12f;

    [Header("체력바")]
    [SerializeField] Vector2 barOffset = new(0f, -1.4f);
    [SerializeField] Vector2 barSize = new(2f, 0.22f);

    public float Max => maxHealth;
    public float Current { get; private set; }
    public bool IsDead { get; private set; }

    /// <summary>(현재, 최대). 피해·회복 모두 알린다.</summary>
    public event System.Action<float, float> Changed;
    public event System.Action Died;

    SpriteRenderer[] renderers;
    Color[] originalColors;
    float flashRemain;
    bool tinted;

    void Awake()
    {
        Current = maxHealth;
    }

    void Start()
    {
        // 캐릭터 비주얼은 Player.Awake 에서 자식으로 붙으므로 그 뒤에 걷는다
        renderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].color;

        PlayerHealthBar.Create(this, barOffset, barSize);
    }

    /// <summary>피해 진입점. 접촉이든 투사체든 전부 여기로 모은다.</summary>
    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);
        flashRemain = flashDuration;

        Changed?.Invoke(Current, maxHealth);

        if (Current <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Min(maxHealth, Current + amount);
        Changed?.Invoke(Current, maxHealth);
    }

    // 적과 겹쳐 있는 동안 매 물리 스텝마다 들어온다. 닿아 있는 시간만큼 피해가 쌓인다
    void OnCollisionStay2D(Collision2D collision)
    {
        ApplyContact(collision.collider);
    }

    // 적 콜라이더가 트리거로 바뀌어도 접촉 피해는 유지되게 양쪽 다 받는다
    void OnTriggerStay2D(Collider2D other)
    {
        ApplyContact(other);
    }

    void ApplyContact(Collider2D other)
    {
        // 콜라이더가 자식에 있는 적 프리팹도 있어서 부모까지 훑는다
        if (!other.TryGetComponent(out Enemy enemy))
            enemy = other.GetComponentInParent<Enemy>();

        if (enemy == null) return;

        TakeDamage(enemy.contactDamage * Time.fixedDeltaTime);
    }

    void Update()
    {
        if (flashRemain > 0f)
        {
            flashRemain -= Time.deltaTime;
            SetTint(true);
        }
        else
        {
            SetTint(false);
        }
    }

    void SetTint(bool on)
    {
        if (renderers == null || tinted == on) return;

        tinted = on;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            renderers[i].color = on ? hitColor : originalColors[i];
        }
    }

    void Die()
    {
        IsDead = true;

        if (LogManager.Instance != null)
            LogManager.Instance.Error("FATAL: PROCESS TERMINATED");

        Died?.Invoke();

        GameManager.instance.GameOver();
    }
}
