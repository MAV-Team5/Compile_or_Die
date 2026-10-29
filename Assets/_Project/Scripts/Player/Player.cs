using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 본체. 입력을 받아 움직이는 일만 한다.
///
/// <b>인스펙터에 조절할 칸이 없다.</b> 능력치는 <see cref="CharacterData"/> 가 갖고 있고
/// <see cref="PlayerSetup"/> 이 Awake 에서 깔아준다. 씬에 칸을 두면 매 런 덮어쓰이는 값을
/// 고치고 왜 안 바뀌냐고 묻게 된다.
/// </summary>
public class Player : MonoBehaviour, IFacingProvider
{
    // 캐릭터 비주얼을 붙이는 일은 PlayerSetup 이 한다 —
    // 어떤 캐릭터인지는 CharacterData 가 정하므로 여기서 알 필요가 없다

    /// <summary>이번 프레임의 입력. PlayerInput 이 채운다.</summary>
    [System.NonSerialized] public Vector2 inputVec;

    /// <summary>이동속도. PlayerSetup 이 캐릭터 값으로 깔고 하드웨어가 그 위에 곱한다.</summary>
    [System.NonSerialized] public float speed;

    /// <summary>경험치를 끌어당기는 반경. 출처는 speed 와 같다.</summary>
    [System.NonSerialized] public float pickupRange;

    /// <summary>주변 적 탐색기. Awake 에서 잡는다.</summary>
    [System.NonSerialized] public Scanner scanner;

    /// <summary>마지막으로 향했던 방향. 손을 떼도 유지된다. 증강이 이 방향으로 발동한다.</summary>
    Vector2 facing = Vector2.right;

    public Vector2 Facing => facing;

    Rigidbody2D rigid;

    /// <summary>블루스크린 재부팅 중일 때 이동 입력을 상하좌우 다 반전시킨다. PlayerRebootController 가 켠다.</summary>
    [System.NonSerialized] public bool invertX;

    // 0xCAFE 같은 한시적 이동속도 버프. 배율 하나만 유지하면 되니 스택 없이 덮어쓴다
    float speedMultiplier = 1f;

    [Header("대시 — Shift")]
    [Tooltip("한 번 대시로 튀어나가는 거리(유닛).")]
    [Min(0.5f)] [SerializeField] float dashDistance = 6f;

    [Tooltip("대시에 걸리는 시간(초). 짧을수록 '팍' 튀어나간다.")]
    [Min(0.02f)] [SerializeField] float dashDuration = 0.14f;

    [Tooltip("모아둘 수 있는 대시 횟수.")]
    [Min(1)] [SerializeField] int dashCharges = 2;

    [Tooltip("빈 대시 한 칸이 다시 차는 데 걸리는 시간(초). 한 칸씩 차례로 찬다.")]
    [Min(0.1f)] [SerializeField] float dashRecharge = 2f;

    [Tooltip("잔상을 남기는 간격(초).")]
    [Min(0.01f)] [SerializeField] float ghostInterval = 0.025f;

    [Tooltip("잔상이 사라지는 데 걸리는 시간(초).")]
    [Min(0.05f)] [SerializeField] float ghostLife = 0.3f;

    [SerializeField] Color ghostTint = new(0.45f, 0.9f, 1f, 0.7f);

    float dashRemain;
    int dashLeft = -1;          // 남은 대시 횟수. -1 이면 아직 안 채움(Awake 에서 가득 채운다)
    float dashRechargeTimer;    // 지금 차오르는 칸의 진행(초)
    float ghostTimer;
    Vector2 dashDir;

    /// <summary>지금 대시 중인가.</summary>
    public bool IsDashing => dashRemain > 0f;

    /// <summary>모아둘 수 있는 대시 횟수.</summary>
    public int DashMaxCharges => Mathf.Max(1, dashCharges);

    /// <summary>지금 쓸 수 있는 대시 횟수.</summary>
    public int DashCharges => Mathf.Max(0, dashLeft);

    /// <summary>지금 차오르는 칸의 진행도(0~1). 다 차 있으면 0.</summary>
    public float DashRechargeProgress => DashCharges >= DashMaxCharges ? 0f : Mathf.Clamp01(dashRechargeTimer / dashRecharge);
    float speedBoostRemain;

    /// <summary>지금 걸린 이동속도 배율. 1이면 버프 없음.</summary>
    public float SpeedMultiplier => speedMultiplier;

    /// <summary>
    /// 버프까지 반영한 실제 이동속도.
    ///
    /// 하드웨어(키보드) 보정은 런이 시작될 때 <see cref="HardwareBonus"/> 가
    /// <see cref="speed"/> 자체에 곱해둔다 — 여기서 또 곱하지 않는다.
    /// </summary>
    public float CurrentSpeed => speed * speedMultiplier;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();

        scanner = GetComponent<Scanner>();
    }

    void Update()
    {
        TickDash();

        if (speedBoostRemain <= 0f) return;

        speedBoostRemain -= Time.deltaTime;
        if (speedBoostRemain <= 0f) speedMultiplier = 1f;
    }

    /// <summary>
    /// Shift 를 누른 순간 지금 가는 방향(손을 뗐으면 마지막으로 본 방향)으로 팍 튀어나간다.
    /// 이동은 FixedUpdate 가 하고, 여기서는 횟수·충전·잔상만 맡는다.
    ///
    /// <b>횟수제</b> — <see cref="dashCharges"/> 번까지 모아둘 수 있고, 쓰면 한 칸 빠진다.
    /// 빈 칸은 <see cref="dashRecharge"/> 초에 한 칸씩 차례로 찬다. 연달아 두 번 쓰고 나면 4초를 기다려야 다 찬다.
    /// 시간이 멈춘 동안(증강 선택)은 deltaTime 이 0 이라 충전도 같이 멈춘다.
    /// </summary>
    void TickDash()
    {
        if (dashLeft < 0) dashLeft = DashMaxCharges;

        if (dashLeft < DashMaxCharges)
        {
            dashRechargeTimer += Time.deltaTime;
            if (dashRechargeTimer >= dashRecharge)
            {
                dashRechargeTimer = 0f;
                dashLeft++;
            }
        }
        else dashRechargeTimer = 0f;

        Keyboard kb = Keyboard.current;
        bool pressed = kb != null && kb.shiftKey.wasPressedThisFrame && Time.timeScale > 0f;

        if (pressed && dashLeft > 0 && dashRemain <= 0f)
        {
            dashLeft--;
            dashDir = inputVec.sqrMagnitude > 0.0001f ? inputVec.normalized : facing;

            // 재부팅(입력 반전) 중이면 대시도 반전된다 — 걷기와 같은 규칙
            if (invertX) dashDir = -dashDir;

            dashRemain = dashDuration;
            ghostTimer = 0f;
        }

        if (dashRemain <= 0f) return;

        ghostTimer -= Time.deltaTime;
        if (ghostTimer <= 0f)
        {
            ghostTimer = ghostInterval;
            DashGhost.Spawn(transform, ghostTint, ghostLife);
        }
    }

    private void FixedUpdate()
    {
        // 대시 중엔 입력 대신 대시 방향으로 정해진 속도로 날아간다
        if (dashRemain > 0f)
        {
            float step = Mathf.Min(dashRemain, Time.fixedDeltaTime);
            rigid.MovePosition(rigid.position + dashDir * (dashDistance / dashDuration) * step);
            dashRemain -= Time.fixedDeltaTime;
            return;
        }

        Vector2 vec = inputVec;
        if (invertX) vec = -vec;

        Vector2 nextvec = vec * CurrentSpeed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextvec);
    }

    /// <summary>지속시간 동안 이동속도에 배율을 곱한다. 다시 걸리면 남은 시간을 갱신한다.</summary>
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        speedMultiplier = multiplier;
        speedBoostRemain = duration;
    }

    void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>();

        // 입력이 0이 되는 순간(손 뗌)에는 갱신하지 않아야 마지막 방향이 남는다
        if (inputVec.sqrMagnitude > 0.0001f)
            facing = inputVec.normalized;
    }

    /// <summary>경험치는 LevelSystem 이 소유한다. 여기서는 전달만 한다.</summary>
    public void GetExp(int amount)
    {
        GameManager.instance.levelSystem.AddExp(amount);

        // 로그는 따로 — 짧은 시간에 여러 번 먹으면 로그 창이 한 줄로 묶어준다
        if (LogManager.Instance != null) LogManager.Instance.ExpGained(amount);
    }
}
