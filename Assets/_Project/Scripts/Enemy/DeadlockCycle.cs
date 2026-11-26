using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 데드락이 <b>잠김 ↔ 풀림</b>을 오간다. 프리팹에 붙인다.
///
/// <code>
///   잠김   안 움직임 · 무적 · 몸으로 막음      ── 지나갈 수도 없고 죽일 수도 없다
///   풀림   느리게 추적 · 피격됨                ── 이때만 딜을 넣을 수 있다
/// </code>
///
/// <b>왜 잠김에도 콜라이더를 켜두나</b> — 데드락의 정체가 "벽" 이기 때문이다.
/// 무적인 김에 통과까지 되면 그냥 잠깐 안 보이는 적일 뿐이고,
/// 못 지나간다는 것이 곧 이 적이 만드는 압박이다.
///
/// 무적일 때 피해 숫자가 안 뜨는 것은 <see cref="DamagePipeline"/> 이
/// <see cref="IDamageReceiver.AcceptsDamage"/> 를 먼저 보기 때문이다 —
/// 무적인데 숫자가 뜨면 플레이어는 버그로 읽는다.
/// </summary>
[DefaultExecutionOrder(10)]
[RequireComponent(typeof(Enemy))]
public class DeadlockCycle : MonoBehaviour
{
    [Header("주기")]
    [Tooltip("잠겨 있는 시간(초). 이 동안은 못 뚫는다.\n" +
             "＊ 너무 길면 플레이어가 할 게 없어진다. 2~4초 권장.")]
    [Min(0.1f)] public float lockedDuration = 3f;

    [Tooltip("풀려 있는 시간(초). 이 창 안에 딜을 넣어야 한다.\n" +
             "＊ 이 값이 곧 난이도다 — 짧을수록 화력이 필요해진다.")]
    [Min(0.1f)] public float openDuration = 2.5f;

    [Tooltip("스폰 직후 잠긴 상태로 시작할지.\n" +
             "켜두면 다가가는 동안 못 때려서 \"기다렸다 친다\" 는 규칙을 먼저 배운다.")]
    public bool startLocked = true;

    [Tooltip("첫 주기만 이만큼 흔든다(초). 여러 마리가 동시에 열렸다 닫히는 것을 막는다.")]
    [Min(0f)] public float startJitter = 0.6f;

    [Header("질량")]
    [Tooltip("잠겼을 때 질량. 무거울수록 플레이어가 밀어내지 못한다 — 진짜 벽이 된다.")]
    [Min(0.01f)] public float lockedMass = 50f;

    [Tooltip("풀렸을 때 질량. 가벼우면 몸으로 비집고 지나갈 수 있다.\n\n" +
             "＊ 이 차이가 곧 \"열렸다\" 는 신호다 — 애니메이션을 못 봤어도\n" +
             "  밀리는 감촉으로 상태가 바뀐 것을 안다.")]
    [Min(0.01f)] public float openMass = 1f;

    [Header("애니메이터")]
    [Tooltip("잠김 여부를 넘길 bool 파라미터 이름. 컨트롤러의 간선 조건과 같아야 한다.")]
    public string lockParameter = "isLock";

    [Header("연출 (선택)")]
    [Tooltip("잠긴(무적) 상태일 때 켜지는 방벽 링. 비워두면 자식 'BarrierRing' 을 자동으로 찾는다.")]
    public SpriteRenderer barrierRing;

    [Header("무적 오라")]
    [Tooltip("잠겨 있는 동안 주변 적에게 무적을 거는 반경(월드 단위).")]
    [Min(0.1f)] [SerializeField] float auraRadius = 8.4f;

    [Tooltip("오라가 감지할 적 레이어. 비워두면 이 오브젝트의 레이어를 쓴다.")]
    [SerializeField] LayerMask enemyMask;

    [Header("아군 추적 (풀림 상태)")]
    [Tooltip("풀려 있을 때 따라갈 아군 몬스터를 찾는 반경(월드 단위).")]
    [Min(0.5f)] [SerializeField] float allySearchRadius = 25f;

    [Tooltip("아군과 이 거리 이하로 가까워지면 멈춘다.")]
    [Min(0.1f)] [SerializeField] float allyStopDistance = 1.5f;

    [Tooltip("풀려 있을 때 아군 쪽으로 이동하는 속도.")]
    [Min(0.1f)] [SerializeField] float followSpeed = 2.4f;

    [Fx("잠길 때", "본체")] public FxGroup lockFx = new();

    [Fx("풀릴 때", "본체")] public FxGroup unlockFx = new();

    const float AuraScanInterval = 0.12f;
    const float AllyScanInterval = 0.25f;

    static readonly Color BarrierCyan = new(0.302f, 0.816f, 0.882f, 0.65f);    // #4DD0E1
    static readonly Color BarrierMagenta = new(1f, 0.239f, 0.498f, 0.85f);   // #FF3D7F

    Enemy body;
    EnemyVisual visual;
    Animator anim;
    Rigidbody2D rigid;
    SpriteRenderer spriter;

    bool locked;
    float timer;
    float originalMass = 1f;

    readonly HashSet<Enemy> protectedEnemies = new();
    readonly HashSet<Enemy> currentInAura = new();
    readonly List<Enemy> auraScratch = new();
    float auraScanTimer;

    Enemy followTarget;
    float followScanTimer;

    void Awake()
    {
        body = GetComponent<Enemy>();
        visual = EnemyVisual.GetOrAdd(transform);
        anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();

        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (barrierRing == null)
        {
            Transform ringTransform = transform.Find("BarrierRing");
            if (ringTransform != null)
                barrierRing = ringTransform.GetComponent<SpriteRenderer>();
        }

        // 이 컴포넌트가 꺼졌을 때 돌려놓을 값
        if (rigid != null) originalMass = rigid.mass;
    }

    /// <summary>
    /// 풀에서 다시 꺼낼 때마다 처음 상태로 돌린다.
    ///
    /// <b>이게 없으면</b> 지난 개체가 풀린 상태로 죽었을 때 새 개체가 그대로 이어받아,
    /// 잠금 애니메이션과 실제 상태가 어긋난 개체가 섞인다.
    /// </summary>
    void OnEnable()
    {
        ClearAura();
        followTarget = null;
        followScanTimer = 0f;
        locked = startLocked;
        timer = Duration(locked) + Random.Range(0f, startJitter);
        auraScanTimer = 0f;

        ApplyState(silent: true);
    }

    void Update()
    {
        if (body != null && !body.IsAlive)
        {
            ClearAura();
            followTarget = null;
            if (barrierRing != null) barrierRing.enabled = false;
            return;
        }

        if (locked)
        {
            if (barrierRing != null)
            {
                UpdateBarrierRingScale();
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
                barrierRing.color = Color.Lerp(BarrierCyan, BarrierMagenta, pulse);
            }

            auraScanTimer -= Time.deltaTime;
            if (auraScanTimer <= 0f)
            {
                auraScanTimer = AuraScanInterval;
                ScanAura();
            }
        }
        else
        {
            followScanTimer -= Time.deltaTime;
            if (followScanTimer <= 0f)
            {
                followScanTimer = AllyScanInterval;
                ScanFollowTarget();
            }
        }

        timer -= Time.deltaTime;

        if (timer > 0f) return;

        locked = !locked;
        timer = Duration(locked);

        ApplyState(silent: false);
    }

    void FixedUpdate()
    {
        if (rigid == null) return;

        if (locked || body == null || !body.IsAlive)
        {
            rigid.linearVelocity = Vector2.zero;
            return;
        }

        if (followTarget == null || !followTarget.IsAlive || !followTarget.gameObject.activeInHierarchy)
        {
            followTarget = null;
            rigid.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = (Vector2)followTarget.transform.position - rigid.position;
        float distSqr = toTarget.sqrMagnitude;
        if (distSqr <= allyStopDistance * allyStopDistance)
        {
            rigid.linearVelocity = Vector2.zero;
            return;
        }

        float dist = Mathf.Sqrt(distSqr);
        Vector2 dir = toTarget / dist;
        float step = Mathf.Min(followSpeed * Time.fixedDeltaTime, dist - allyStopDistance);
        rigid.MovePosition(rigid.position + dir * step);
        rigid.linearVelocity = Vector2.zero;
    }

    void LateUpdate()
    {
        if (spriter == null || body == null || !body.IsAlive) return;
        if (locked || followTarget == null || !followTarget.IsAlive || !followTarget.gameObject.activeInHierarchy) return;

        float dx = followTarget.transform.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.01f)
            spriter.flipX = dx < 0f;
    }

    /// <summary>지금 잠겨 있는가. 할로윈 스킨이 스폰 직후 자세를 맞출 때 본다.</summary>
    public bool IsLocked => locked;

    float Duration(bool isLocked) => isLocked ? lockedDuration : openDuration;

    /// <summary>지금 상태를 적과 애니메이터에 반영한다.</summary>
    void ApplyState(bool silent)
    {
        body.invulnerable = locked;
        body.movementLocked = true;
        visual?.SetInvulnerableBadge(locked);

        // 잠기면 무겁고 풀리면 가볍다. 밀리는 감촉만으로도 상태가 읽힌다
        if (rigid != null) rigid.mass = locked ? lockedMass : openMass;

        if (anim != null && !string.IsNullOrEmpty(lockParameter))
            anim.SetBool(lockParameter, locked);

        // 할로윈 스킨: 잠긴 동안 기모으기 자세
        HalloweenSkin.SetCharging(this, locked);

        if (barrierRing != null)
        {
            barrierRing.enabled = locked;
            if (locked)
            {
                UpdateBarrierRingScale();
                barrierRing.color = BarrierCyan;
            }
        }

        if (locked)
        {
            if (!silent)
            {
                auraScanTimer = AuraScanInterval;
                ScanAura();
            }
        }
        else
        {
            ClearAura();
            followScanTimer = 0f;
            if (!silent) ScanFollowTarget();
        }

        // 스폰 순간에는 연출을 안 낸다. 화면 밖에서 소리가 나면 어디서 나는지 알 수 없다
        if (silent) return;

        if (locked) lockFx.PlayAt(transform.position, default, 0f, transform);
        else unlockFx.PlayAt(transform.position, default, 0f, transform);
    }

    void ScanFollowTarget()
    {
        if (body == null || !body.IsAlive)
        {
            followTarget = null;
            return;
        }

        int mask = enemyMask.value != 0 ? enemyMask.value : (1 << gameObject.layer);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, allySearchRadius, mask);

        Enemy nearest = null;
        float bestDistSqr = float.MaxValue;
        Vector2 myPos = transform.position;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || !hit.TryGetComponent(out Enemy other)) continue;
            if (other == body || !other.IsAlive || other.IsStationary) continue;
            if (other.TryGetComponent<DeadlockCycle>(out _)) continue;

            float distSqr = ((Vector2)other.transform.position - myPos).sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                nearest = other;
            }
        }

        followTarget = nearest;
    }

    void ScanAura()
    {
        if (!locked || body == null || !body.IsAlive)
        {
            ClearAura();
            return;
        }

        currentInAura.Clear();

        int mask = enemyMask.value != 0 ? enemyMask.value : (1 << gameObject.layer);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, auraRadius, mask);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || !hit.TryGetComponent(out Enemy other)) continue;
            if (other == body || !other.IsAlive || other.IsStationary) continue;
            if (other.TryGetComponent<DeadlockCycle>(out _)) continue;

            currentInAura.Add(other);

            if (protectedEnemies.Add(other))
                other.PushInvulnerable();
        }

        if (protectedEnemies.Count == 0) return;

        auraScratch.Clear();
        foreach (Enemy e in protectedEnemies)
        {
            if (e == null || !e.IsAlive || !e.gameObject.activeInHierarchy || !currentInAura.Contains(e))
                auraScratch.Add(e);
        }

        for (int i = 0; i < auraScratch.Count; i++)
        {
            Enemy e = auraScratch[i];
            if (e != null) e.PopInvulnerable();
            protectedEnemies.Remove(e);
        }
    }

    void ClearAura()
    {
        if (protectedEnemies.Count == 0)
        {
            currentInAura.Clear();
            auraScratch.Clear();
            return;
        }

        foreach (Enemy e in protectedEnemies)
        {
            if (e != null) e.PopInvulnerable();
        }

        protectedEnemies.Clear();
        currentInAura.Clear();
        auraScratch.Clear();
    }

    void UpdateBarrierRingScale()
    {
        if (barrierRing == null) return;

        Vector2 spriteSize = barrierRing.sprite != null ? (Vector2)barrierRing.sprite.bounds.size : new Vector2(2.56f, 2.56f);
        Vector3 parentLossy = barrierRing.transform.parent != null ? barrierRing.transform.parent.lossyScale : Vector3.one;

        float denomX = Mathf.Max(0.01f, spriteSize.x * Mathf.Abs(parentLossy.x));
        float denomY = Mathf.Max(0.01f, spriteSize.y * Mathf.Abs(parentLossy.y));
        float targetWorldDiameter = auraRadius * 2f;

        barrierRing.transform.localScale = new Vector3(
            targetWorldDiameter / denomX,
            targetWorldDiameter / denomY,
            1f);
    }

    // 이 컴포넌트가 꺼져도 적이 무적·붙박이로 남지 않게
    void OnDisable()
    {
        ClearAura();
        followTarget = null;
        followScanTimer = 0f;
        if (barrierRing != null) barrierRing.enabled = false;
        visual?.SetInvulnerableBadge(false);
        if (body == null) return;

        body.invulnerable = false;
        body.movementLocked = false;

        if (rigid != null) rigid.mass = originalMass;
    }
}
