using UnityEngine;

/// <summary>
/// 트로이 목마(Trojan)의 의태와 돌진 기믹. 프리팹에 붙인다.
///
/// <code>
///   Disguised  DATA_BOX 상자처럼 정지 · 플레이어가 반경 안으로 들어오거나 피격되면 발동
///   Revealing  짧은 변신 예고(텔레그래프) 후 본모습 공개
///   Active     본체 추격 + 초기 돌진 가속
/// </code>
///
/// 풀에서 재사용되므로 <see cref="OnEnable"/> 에서 상태를 반드시 초기화한다.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class TrojanMimic : MonoBehaviour
{
    enum State
    {
        Disguised,
        Revealing,
        Active
    }

    [Header("의태 및 발동")]
    [Tooltip("플레이어가 이 반경(유닛) 안으로 들어오면 정체를 드러낸다.")]
    [Min(0.5f)] public float triggerRadius = 4.5f;

    [Tooltip("상자에서 본체로 변신하는 예고 시간(초). 이 동안은 제자리에서 점멸한다.")]
    [Min(0.05f)] public float revealDuration = 0.45f;

    [Header("돌진 가속")]
    [Tooltip("변신 직후 추가로 빠르게 달려드는 시간(초). 0이면 기본 추격만 한다.")]
    [Min(0f)] public float chargeBurstDuration = 0.7f;

    [Tooltip("돌진 가속 동안 추가로 더하는 이동 속도(유닛/초).")]
    [Min(0f)] public float chargeBonusSpeed = 2.0f;

    [Header("애니메이터")]
    [Tooltip("정체를 드러냈는지 넘길 bool 파라미터 이름. 컨트롤러의 전이 조건과 같아야 한다.")]
    public string revealedParameter = "isRevealed";

    [Header("연출 (선택)")]
    [Fx("변신 발동", "본체")] public FxGroup revealFx = new();

    static readonly Color TelegraphTint = new Color(1f, 0.55f, 0.75f, 1f);

    Enemy body;
    Animator anim;
    Rigidbody2D rigid;
    SpriteRenderer spriter;

    State state;
    float stateTimer;
    float chargeRemain;

    void Awake()
    {
        body = GetComponent<Enemy>();
        anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();

        if (anim == null) anim = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        state = State.Disguised;
        stateTimer = 0f;
        chargeRemain = 0f;

        if (body != null) body.movementLocked = true;
        if (spriter != null) spriter.color = Color.white;
        if (anim != null && !string.IsNullOrEmpty(revealedParameter))
            anim.SetBool(revealedParameter, false);
    }

    void Update()
    {
        if (body == null || !body.IsAlive)
        {
            if (spriter != null) spriter.color = Color.white;
            return;
        }

        switch (state)
        {
            case State.Disguised:
                body.movementLocked = true;
                if (ShouldReveal())
                    BeginReveal();
                break;

            case State.Revealing:
                body.movementLocked = true;
                stateTimer -= Time.deltaTime;

                if (spriter != null)
                {
                    bool flash = Mathf.FloorToInt(Time.time * 18f) % 2 == 0;
                    spriter.color = flash ? TelegraphTint : Color.white;
                }

                if (stateTimer <= 0f)
                    BeginActive();
                break;

            case State.Active:
                body.movementLocked = false;
                if (chargeRemain > 0f)
                    chargeRemain -= Time.deltaTime;
                break;
        }
    }

    void FixedUpdate()
    {
        if (state != State.Active || chargeRemain <= 0f || chargeBonusSpeed <= 0f) return;
        if (body == null || !body.IsAlive || body.target == null || rigid == null) return;

        Vector2 delta = body.target.position - rigid.position;
        if (delta.sqrMagnitude < 0.0001f) return;

        rigid.MovePosition(rigid.position + delta.normalized * (chargeBonusSpeed * Time.fixedDeltaTime));
    }

    bool ShouldReveal()
    {
        // 상자 상태일 때 공격을 받아도 즉시 정체를 드러낸다
        if (body.HealthRatio < 0.999f) return true;
        if (body.target == null) return false;

        Vector2 delta = body.target.position - (Vector2)transform.position;
        return delta.sqrMagnitude <= triggerRadius * triggerRadius;
    }

    void BeginReveal()
    {
        state = State.Revealing;
        stateTimer = revealDuration;
        body.movementLocked = true;

        if (anim != null && !string.IsNullOrEmpty(revealedParameter))
            anim.SetBool(revealedParameter, true);

        revealFx.PlayAt(transform.position, default, 0f, transform);
    }

    void BeginActive()
    {
        state = State.Active;
        chargeRemain = chargeBurstDuration;
        body.movementLocked = false;

        if (spriter != null) spriter.color = Color.white;
    }

    void OnDisable()
    {
        if (body != null) body.movementLocked = false;
        if (spriter != null) spriter.color = Color.white;
    }
}
