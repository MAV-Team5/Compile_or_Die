using UnityEngine;

/// <summary>
/// 총알(투사체) 스크립트
/// 근접무기(per=-100): 무한 관통, Weapon 자식으로 회전
/// 원거리무기(per>=0): velocity로 이동, 관통력 소진 시 비활성화
/// </summary>
public class Bullet : MonoBehaviour
{
    public float damage;    // 총알 데미지
    public int per;         // 관통력: -100=무한(근접), 0=관통없음, n=n회 관통

    Rigidbody2D rigid;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Weapon에서 풀에서 꺼낸 후 호출
    /// </summary>
    /// <param name="damage">적용 데미지</param>
    /// <param name="per">관통력 (-100: 근접무한, 0이상: 원거리)</param>
    /// <param name="dir">발사 방향 (근접은 Vector3.zero)</param>
    public void Init(float damage, int per, Vector3 dir)
    {
        this.damage = damage;
        this.per    = per;

        // per >= 0 이면 원거리 총알 → velocity로 직선 이동
        // per == -100 이면 근접무기 → Weapon 오브젝트 회전에 딸려 움직임
        if (per >= 0)
            rigid.linearVelocity = dir * 15f; // 총알 속도: 15
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Enemy 태그가 아니면 무시
        if (!collision.CompareTag("Enemy")) return;
        // 근접무기(per=-100)는 이 함수에서 비활성화 처리 안 함 (무한 관통)
        if (per == -100) return;

        per--; // 관통력 1 감소

        // 0 미만이 되면 관통력 소진 → 총알 비활성화
        // per < 0 조건: 정확히 -1이 아닌 경우도 안전하게 처리
        if (per < 0)
        {
            rigid.linearVelocity = Vector2.zero; // 속도 초기화 (재사용 시 남은 속도 방지)
            gameObject.SetActive(false);          // 풀로 반납
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        // 플레이어의 Area(20×20 감지 영역) 밖으로 나가면 비활성화
        // 빗나간 원거리 총알이 화면 밖으로 무한 비행하는 버그 방지
        if (!collision.CompareTag("Area")) return;
        if (per == -100) return; // 근접무기는 해당 없음

        rigid.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);
    }
}
