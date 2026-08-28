using System.Collections.Generic;
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

    [Header("접촉 판정")]
    [Tooltip("적을 찾을 물리 레이어. 비우면 \"Enemy\" 레이어를 자동으로 쓴다.")]
    [SerializeField] LayerMask enemyLayer;

    SpriteRenderer[] renderers;
    Color[] originalColors;
    float flashRemain;
    bool tinted;

    Collider2D bodyCollider;
    ContactFilter2D enemyFilter;
    readonly List<Collider2D> overlapBuffer = new();

    void Awake()
    {
        Current = maxHealth;

        // 콜라이더는 자기 GameObject 것만 잡는다 — 자식인 무기 판정용 Area는 제외된다.
        // Area는 Rigidbody2D가 없어 플레이어 Rigidbody2D의 복합 콜라이더로 묶이므로,
        // 메시지 기반(OnTriggerStay2D) 판정을 쓰면 무기 사거리에만 들어와도 맞은 걸로 잡힌다.
        // 그래서 몸통 콜라이더 하나만 명시해 직접 겹침 검사를 한다.
        bodyCollider = GetComponent<Collider2D>();

        if (enemyLayer.value == 0) enemyLayer = LayerMask.GetMask("Enemy");

        enemyFilter = new ContactFilter2D();
        enemyFilter.SetLayerMask(enemyLayer);
        enemyFilter.useTriggers = true;
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

    /// <summary>
    /// 최대 체력에 배율을 건다. 하드웨어(SSD)가 런 시작 시 한 번 부른다.
    ///
    /// 늘어난 만큼 지금 체력도 함께 채운다 — 런이 시작되는 시점이라
    /// 최대치만 늘고 현재 체력이 그대로면 시작부터 다친 채로 서 있게 된다.
    /// </summary>
    /// <summary>
    /// 최대 체력을 다시 정하고 가득 채운다. 런 시작에 <see cref="PlayerSetup"/> 이 캐릭터 값으로 부른다.
    ///
    /// 인스펙터 필드를 직접 쓰지 않고 메서드로 두는 이유는 <c>Current</c> 때문이다 —
    /// Awake 끼리는 순서가 없어서, 필드만 바꾸면 이미 지나간 Awake 가 채워둔
    /// 옛 최대치가 현재 체력으로 남는다.
    /// </summary>
    public void SetMaxHealth(float value)
    {
        maxHealth = Mathf.Max(1f, value);
        Current = maxHealth;

        Changed?.Invoke(Current, maxHealth);
    }

    public void ScaleMaxHealth(float multiplier)
    {
        if (multiplier <= 0f || Mathf.Approximately(multiplier, 1f)) return;

        maxHealth *= multiplier;
        Current = maxHealth;

        Changed?.Invoke(Current, maxHealth);
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Min(maxHealth, Current + amount);
        Changed?.Invoke(Current, maxHealth);
    }

    // 몸통 콜라이더 하나만 대상으로 실제로 겹친 적을 직접 찾는다.
    // 플레이어 Rigidbody2D에 묶인 다른 콜라이더(무기 Area 등)는 여기 끼어들지 않는다.
    void FixedUpdate()
    {
        if (IsDead || bodyCollider == null) return;

        int count = Physics2D.OverlapCollider(bodyCollider, enemyFilter, overlapBuffer);

        for (int i = 0; i < count; i++)
        {
            Collider2D other = overlapBuffer[i];

            // 콜라이더가 자식에 있는 적 프리팹도 있어서 부모까지 훑는다
            if (!other.TryGetComponent(out Enemy enemy))
                enemy = other.GetComponentInParent<Enemy>();

            if (enemy == null) continue;

            TakeDamage(enemy.contactDamage * Time.fixedDeltaTime);
        }
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

        // 런을 끝내는 것은 RunDirector 몫이다. 이 이벤트를 듣고 정산·씬 이동까지 처리한다
        Died?.Invoke();
    }
}
