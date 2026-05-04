using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 오브젝트 제어 스크립트
/// 이동 / 애니메이션 / 피격 / 사망 처리 담당
/// </summary>
public class Player : MonoBehaviour
{
    [Header("# 이동")]
    public Vector2 inputVec;        // 현재 프레임 입력 방향 벡터 (-1 ~ 1)
    public float speed;             // 이동 속도 (OnEnable에서 캐릭터 특성 반영)

    [Header("# 참조")]
    public Scanner scanner;         // 가장 가까운 적 탐지 컴포넌트
    public Hand[] hands;            // 손 오브젝트 배열 [0]=왼손(근접) [1]=오른손(원거리)
    public RuntimeAnimatorController[] animCon; // 캐릭터별 애니메이터 컨트롤러 배열

    Rigidbody2D rigid;              // 물리 이동에 사용
    SpriteRenderer spriter;         // 좌우 플립(방향 전환)에 사용
    Animator anim;                  // 애니메이션 상태 제어
    PlayerInput playerInput;        // New Input System 컴포넌트 참조
    Vector2 nextVec;                // 다음 물리 프레임 이동량 (Update → FixedUpdate 전달용)

    void Awake()
    {
        // 컴포넌트 초기화 (씬 로드 시 1회)
        rigid       = GetComponent<Rigidbody2D>();
        spriter     = GetComponent<SpriteRenderer>();
        anim        = GetComponent<Animator>();
        scanner     = GetComponent<Scanner>();
        playerInput = GetComponent<PlayerInput>();
        // includeInactive=true: 비활성화된 Hand 오브젝트도 포함해서 가져옴
        hands       = GetComponentsInChildren<Hand>(true);
    }

    void OnEnable()
    {
        // 오브젝트 활성화될 때마다 호출 (캐릭터 선택 시 GameStart에서 SetActive(true))
        inputVec = Vector2.zero;    // 이전 입력값 초기화 (방향키 유지 버그 방지)
        speed    = 3f;
        speed   *= Character.Speed; // 캐릭터 0번: 이동속도 10% 보너스 적용

        // 선택한 캐릭터 ID에 맞는 애니메이터 컨트롤러로 교체
        if (animCon != null && animCon.Length > GameManager.instance.playerId)
            anim.runtimeAnimatorController = animCon[GameManager.instance.playerId];
    }

    // ─── New Input System 콜백 ────────────────────────────────────────────
    // PlayerInput 컴포넌트 Behavior: "Send Messages" 설정 시 자동 호출
    // Input Action Asset의 Action 이름이 정확히 "Move" 여야 매핑됨
    void OnMove(InputValue value)
    {
        inputVec = value.Get<Vector2>(); // WASD / 게임패드 스틱 → Vector2로 수신
    }

    // ─── 생명주기 함수 ────────────────────────────────────────────────────

    void Update()
    {
        // isLive가 false면 (레벨업 창, 게임오버 등) 입력 처리 중단
        if (!GameManager.instance.isLive) return;

        // 이동량 계산: 방향 × 속도 × 물리 프레임 시간
        // fixedDeltaTime: FixedUpdate 한 프레임 소요 시간 (프레임 독립 이동)
        nextVec = inputVec * speed * Time.fixedDeltaTime;
    }

    void FixedUpdate()
    {
        if (!GameManager.instance.isLive) return;
        // MovePosition: Rigidbody2D 물리 기반 위치 이동 (Collider 충돌 유지)
        rigid.MovePosition(rigid.position + nextVec);
    }

    void LateUpdate()
    {
        // Update 완료 후 실행 → 이동 결과를 반영한 시각 처리
        if (!GameManager.instance.isLive) return;

        // Animator Speed 파라미터: magnitude = 벡터 크기 (이동 중 > 0, 정지 = 0)
        anim.SetFloat("Speed", inputVec.magnitude);

        // 이동 방향에 따라 스프라이트 좌우 반전 (키를 누를 때만 갱신 → 마지막 방향 유지)
        if (inputVec.x != 0)
            spriter.flipX = inputVec.x < 0; // 왼쪽 이동 시 반전
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // 게임 종료 상태면 피격 처리 안 함
        if (!GameManager.instance.isLive) return;
        // Enemy 태그 오브젝트에 닿았을 때만 데미지 (타일맵 등 오접촉 방지)
        if (!collision.gameObject.CompareTag("Enemy")) return;

        // 닿아있는 동안 매 프레임 체력 감소 (초당 10 데미지)
        // Time.deltaTime 곱해서 프레임 수와 무관하게 일정 속도로 감소
        GameManager.instance.health -= Time.deltaTime * 10f;

        if (GameManager.instance.health < 0)
        {
            // 자식 오브젝트 중 인덱스 2 이상 비활성화
            // [0]=Shadow, [1]=Area 는 유지 / 나머지(Weapon, Hand 등) 제거
            for (int i = 2; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(false);

            anim.SetTrigger("Dead"); // 사망 애니메이션 트리거
            GameManager.instance.GameOver();
        }
    }
}
