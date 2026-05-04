using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public Vector2 inputVec;
    public float speed;
    public Scanner scanner;
    public Hand[] hands;
    public RuntimeAnimatorController[] animCon;

    Rigidbody2D rigid;
    SpriteRenderer spriter;
    Animator anim;
    PlayerInput playerInput;
    Vector2 nextVec;

    void Awake()
    {
        rigid       = GetComponent<Rigidbody2D>();
        spriter     = GetComponent<SpriteRenderer>();
        anim        = GetComponent<Animator>();
        scanner     = GetComponent<Scanner>();
        playerInput = GetComponent<PlayerInput>();
        hands       = GetComponentsInChildren<Hand>(true);
    }

    void OnEnable()
    {
        // 활성화될 때마다 입력 초기화
        inputVec = Vector2.zero;
        speed    = 3f;
        speed   *= Character.Speed;

        // playerId 범위 체크 후 애니메이터 교체
        if (animCon != null && animCon.Length > GameManager.instance.playerId)
            anim.runtimeAnimatorController = animCon[GameManager.instance.playerId];
    }

    // ─── New Input System 콜백 ────────────────────────────────────────────
    // PlayerInput 컴포넌트 → Behavior: Send Messages 필수
    // Input Action Asset의 Action 이름이 "Move" 여야 OnMove 자동 매핑
    void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>();
    }

    // ─── 생명주기 ─────────────────────────────────────────────────────────
    void Update()
    {
        if (!GameManager.instance.isLive) return;
        nextVec = inputVec * speed * Time.fixedDeltaTime;
    }

    void FixedUpdate()
    {
        if (!GameManager.instance.isLive) return;
        rigid.MovePosition(rigid.position + nextVec);
    }

    void LateUpdate()
    {
        if (!GameManager.instance.isLive) return;

        anim.SetFloat("Speed", inputVec.magnitude);

        if (inputVec.x != 0)
            spriter.flipX = inputVec.x < 0;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!GameManager.instance.isLive) return;
        // Tag 필터: Enemy만 데미지
        if (!collision.gameObject.CompareTag("Enemy")) return;

        GameManager.instance.health -= Time.deltaTime * 10f;

        if (GameManager.instance.health < 0)
        {
            // 인덱스 2 이상 자식 비활성화 (Shadow, Area 유지)
            for (int i = 2; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(false);

            anim.SetTrigger("Dead");
            GameManager.instance.GameOver();
        }
    }
}
