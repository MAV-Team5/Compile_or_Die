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
        if (speedBoostRemain <= 0f) return;

        speedBoostRemain -= Time.deltaTime;
        if (speedBoostRemain <= 0f) speedMultiplier = 1f;
    }

    private void FixedUpdate()
    {
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
