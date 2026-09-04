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
    [Fx("잠길 때", "본체")] public FxGroup lockFx = new();

    [Fx("풀릴 때", "본체")] public FxGroup unlockFx = new();

    Enemy body;
    Animator anim;
    Rigidbody2D rigid;

    bool locked;
    float timer;
    float originalMass = 1f;

    void Awake()
    {
        body = GetComponent<Enemy>();
        anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();

        if (anim == null) anim = GetComponentInChildren<Animator>();

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
        locked = startLocked;
        timer = Duration(locked) + Random.Range(0f, startJitter);

        ApplyState(silent: true);
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer > 0f) return;

        locked = !locked;
        timer = Duration(locked);

        ApplyState(silent: false);
    }

    float Duration(bool isLocked) => isLocked ? lockedDuration : openDuration;

    /// <summary>지금 상태를 적과 애니메이터에 반영한다.</summary>
    void ApplyState(bool silent)
    {
        body.invulnerable = locked;
        body.movementLocked = locked;

        // 잠기면 무겁고 풀리면 가볍다. 밀리는 감촉만으로도 상태가 읽힌다
        if (rigid != null) rigid.mass = locked ? lockedMass : openMass;

        if (anim != null && !string.IsNullOrEmpty(lockParameter))
            anim.SetBool(lockParameter, locked);

        // 스폰 순간에는 연출을 안 낸다. 화면 밖에서 소리가 나면 어디서 나는지 알 수 없다
        if (silent) return;

        if (locked) lockFx.PlayAt(transform.position, default, 0f, transform);
        else unlockFx.PlayAt(transform.position, default, 0f, transform);
    }

    // 이 컴포넌트가 꺼져도 적이 무적·붙박이로 남지 않게
    void OnDisable()
    {
        if (body == null) return;

        body.invulnerable = false;
        body.movementLocked = false;

        if (rigid != null) rigid.mass = originalMass;
    }
}
