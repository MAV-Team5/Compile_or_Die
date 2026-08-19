using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour, IFacingProvider
{
    public GameObject characterPrefab;
    Transform visualRoot;

    public Vector2 inputVec;

    /// <summary>마지막으로 향했던 방향. 손을 떼도 유지된다. 증강이 이 방향으로 발동한다.</summary>
    Vector2 facing = Vector2.right;

    public Vector2 Facing => facing;
    public float speed;
    public Scanner scanner;


    public float pickupRange;
    Rigidbody2D rigid;

    // 0xCAFE 같은 한시적 이동속도 버프. 배율 하나만 유지하면 되니 스택 없이 덮어쓴다
    float speedMultiplier = 1f;
    float speedBoostRemain;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        visualRoot = GetComponent<Transform>();

        scanner = GetComponent<Scanner>();

        SelectCharacter();
    }

    void Update()
    {
        if (speedBoostRemain <= 0f) return;

        speedBoostRemain -= Time.deltaTime;
        if (speedBoostRemain <= 0f) speedMultiplier = 1f;
    }

    private void FixedUpdate()
    {
        Vector2 nextvec = inputVec * speed * speedMultiplier * Time.fixedDeltaTime;
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

    public void SelectCharacter()
    {
        GameObject visual = Instantiate(characterPrefab);
        visual.transform.SetParent(visualRoot,false);

    }

    /// <summary>경험치는 LevelSystem 이 소유한다. 여기서는 전달만 한다.</summary>
    public void GetExp(int amount)
    {
        GameManager.instance.levelSystem.AddExp(amount);

        // 로그는 따로 — 짧은 시간에 여러 번 먹으면 한 줄로 묶어준다
        GameManager.instance.expManager.AddExpLog(amount);
    }
}
