using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 체력. 적과 겹쳐 있는 동안 계속 피해를 받는다 — 뱀서라이크라 무적 시간은 없다.
///
/// <b>여기는 수치만 센다.</b> 피격을 눈에 보이게 하는 일은 <see cref="PlayerHitFeedback"/> 몫이다 —
/// 매 물리 프레임 피해가 들어오는 구조라 연출에 별도의 장치가 필요한데,
/// 그것까지 여기 두면 체력 계산과 뒤섞인다.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageReceiver
{
    /// <summary>
    /// 이번 런의 최대 체력. <b>인스펙터에 두지 않는다.</b>
    ///
    /// 원본은 <see cref="CharacterData"/> 이고, <see cref="PlayerSetup"/> 이 Awake 에서
    /// <see cref="SetMaxHealth"/> 로 깔고 하드웨어(SSD)가 Start 에서 그 위에 곱한다.
    /// 씬에 칸을 두면 매 런 덮어쓰이는 값을 고치고 왜 안 바뀌냐고 묻게 된다.
    ///
    /// 여기 100은 아무도 설정해주지 않았을 때의 최후 기본값일 뿐이다.
    /// </summary>
    float maxHealth = 100f;

    [Tooltip("피격 연출. 비우면 같은 오브젝트에서 찾는다. 없어도 체력은 정상 동작한다.")]
    [SerializeField] PlayerHitFeedback feedback;

    [Header("체력바")]
    [SerializeField] Vector2 barOffset = new(0f, -1.4f);

    [Tooltip("칸 하나의 크기.")]
    [SerializeField] Vector2 barSegmentSize = new(0.18f, 0.22f);

    [Tooltip("칸 사이 간격.")]
    [Min(0f)] [SerializeField] float barSegmentGap = 0.05f;

    [Tooltip("칸 하나가 맡는 체력.\n\n" +
             "＊ 칸 수가 아니라 칸당 체력을 고정한다 — 최대 체력이 큰 캐릭터는\n" +
             "바가 길어져서, 고르는 순간부터 누가 튼튼한지 보인다.")]
    [Min(1f)] [SerializeField] float barHealthPerSegment = 10f;

    [Tooltip("칸 수 상한. 넘으면 한 칸이 더 많은 체력을 맡아 바 길이가 여기서 멈춘다.")]
    [Min(1)] [SerializeField] int barMaxSegments = 20;

    [Tooltip("체력이 바뀐 뒤 체력바가 떠 있는 시간(초). 지나면 흐려지며 사라진다.")]
    [Min(0f)] [SerializeField] float barShowTime = 5f;

    [Tooltip("체력이 이 비율 이하면 계속 띄워둔다. 0이면 항상 시간이 지나면 사라진다.\n" +
             "0.3 이면 30% 아래에서 고정.")]
    [Range(0f, 1f)] [SerializeField] float barAlwaysShowBelow = 0.3f;

    public float Max => maxHealth;
    public float Current { get; private set; }
    public bool IsDead { get; private set; }

    /// <summary>(현재, 최대). 피해·회복 모두 알린다.</summary>
    public event System.Action<float, float> Changed;
    public event System.Action Died;

    [Header("접촉 판정")]
    [Tooltip("적을 찾을 물리 레이어. 비우면 \"Enemy\" 레이어를 자동으로 쓴다.")]
    [SerializeField] LayerMask enemyLayer;

    [Tooltip("적과 겹쳐 있을 때 피해가 들어오는 간격(초).\n\n" +
             "적의 접촉 피해는 \"초당\" 값이라, 한 틱에 (초당 피해 × 이 간격) 만큼 들어온다 —\n" +
             "간격을 바꿔도 초당 피해량은 그대로다. 밸런싱을 다시 안 해도 된다.")]
    [Min(0.05f)] [SerializeField] float contactTickInterval = 0.5f;

    Collider2D bodyCollider;
    ContactFilter2D enemyFilter;
    readonly List<Collider2D> overlapBuffer = new();

    /// <summary>다음 접촉 피해까지 남은 시간. 실제로 맞을 때만 다시 채운다.</summary>
    float contactTimer;

    void Awake()
    {
        Current = maxHealth;

        // 콜라이더는 자기 GameObject 것만 잡는다 — 자식인 무기 판정용 Area는 제외된다.
        // Area는 Rigidbody2D가 없어 플레이어 Rigidbody2D의 복합 콜라이더로 묶이므로,
        // 메시지 기반(OnTriggerStay2D) 판정을 쓰면 무기 사거리에만 들어와도 맞은 걸로 잡힌다.
        // 그래서 몸통 콜라이더 하나만 명시해 직접 겹침 검사를 한다.
        bodyCollider = GetComponent<Collider2D>();

        if (feedback == null) feedback = GetComponent<PlayerHitFeedback>();

        if (enemyLayer.value == 0) enemyLayer = LayerMask.GetMask("Enemy");

        enemyFilter = new ContactFilter2D();
        enemyFilter.SetLayerMask(enemyLayer);
        enemyFilter.useTriggers = true;
    }

    void Start()
    {
        PlayerHealthBar.Create(this, new PlayerHealthBar.Layout
        {
            Offset = barOffset,
            SegmentSize = barSegmentSize,
            SegmentGap = barSegmentGap,
            HealthPerSegment = barHealthPerSegment,
            MaxSegments = barMaxSegments,
            ShowTime = barShowTime,
            AlwaysShowBelow = barAlwaysShowBelow
        });
    }

    /// <summary>
    /// 피해 진입점. 접촉이든 투사체든 전부 여기로 모은다.
    ///
    /// <b>체력은 정수로만 움직인다.</b> 소수점으로 깎이면 표시 숫자는 한참 그대로인데
    /// 막대만 미끄러져서, 맞고 있다는 것도 몇 대 남았다는 것도 읽히지 않는다.
    /// 여기 한 곳에서 자르면 어느 경로로 들어온 피해든 정수가 된다.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        // 최소 1 — 아주 약한 적이 0을 주면 닿아도 아무 일이 안 일어난 것처럼 보인다
        int damage = Mathf.Max(1, Mathf.RoundToInt(amount));

        Current = Mathf.Max(0f, Current - damage);

        if (feedback != null) feedback.Hit();

        Changed?.Invoke(Current, maxHealth);

        if (Current <= 0f) Die();
    }

    /// <summary>
    /// 최대 체력을 다시 정하고 가득 채운다. 런 시작에 <see cref="PlayerSetup"/> 이 캐릭터 값으로 부른다.
    ///
    /// 인스펙터 필드를 직접 쓰지 않고 메서드로 두는 이유는 <c>Current</c> 때문이다 —
    /// Awake 끼리는 순서가 없어서, 필드만 바꾸면 이미 지나간 Awake 가 채워둔
    /// 옛 최대치가 현재 체력으로 남는다.
    /// </summary>
    public void SetMaxHealth(float value)
    {
        // 최대치도 정수여야 칸 나누기와 표시가 어긋나지 않는다
        maxHealth = Mathf.Max(1f, Mathf.Round(value));
        Current = maxHealth;

        Changed?.Invoke(Current, maxHealth);
    }

    /// <summary>
    /// 최대 체력에 배율을 건다. 하드웨어(SSD)가 런 시작 시 한 번 부른다.
    ///
    /// 늘어난 만큼 지금 체력도 함께 채운다 — 런이 시작되는 시점이라
    /// 최대치만 늘고 현재 체력이 그대로면 시작부터 다친 채로 서 있게 된다.
    /// </summary>
    public void ScaleMaxHealth(float multiplier)
    {
        if (multiplier <= 0f || Mathf.Approximately(multiplier, 1f)) return;

        maxHealth = Mathf.Max(1f, Mathf.Round(maxHealth * multiplier));
        Current = maxHealth;

        Changed?.Invoke(Current, maxHealth);
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Min(maxHealth, Current + Mathf.Max(1, Mathf.RoundToInt(amount)));
        Changed?.Invoke(Current, maxHealth);
    }

    /// <summary>
    /// 몸통 콜라이더 하나만 대상으로 실제로 겹친 적을 직접 찾는다.
    /// 플레이어 Rigidbody2D에 묶인 다른 콜라이더(무기 Area 등)는 여기 끼어들지 않는다.
    ///
    /// <b>매 프레임이 아니라 틱으로 준다.</b> 프레임마다 조금씩 깎으면
    /// 체력이 소수점으로 미끄러져서 "몇 대 맞으면 죽는지"를 셀 수가 없다.
    /// 겹친 적들의 초당 피해를 합쳐 한 번에 정수로 준다 — 많이 둘러싸일수록 한 대가 아프다.
    /// </summary>
    void FixedUpdate()
    {
        if (IsDead || bodyCollider == null) return;

        contactTimer -= Time.fixedDeltaTime;
        if (contactTimer > 0f) return;

        int count = Physics2D.OverlapCollider(bodyCollider, enemyFilter, overlapBuffer);

        float perSecond = 0f;

        for (int i = 0; i < count; i++)
        {
            Collider2D other = overlapBuffer[i];

            // 콜라이더가 자식에 있는 적 프리팹도 있어서 부모까지 훑는다
            if (!other.TryGetComponent(out Enemy enemy))
                enemy = other.GetComponentInParent<Enemy>();

            if (enemy == null) continue;

            perSecond += enemy.contactDamage;
        }

        // 안 닿았으면 타이머를 소모하지 않는다 — 그래야 처음 닿는 순간 바로 아프다
        if (perSecond <= 0f) return;

        contactTimer = contactTickInterval;

        TakeDamage(perSecond * contactTickInterval);
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
