using UnityEngine;

/// <summary>
/// 플레이어 손 오브젝트 제어 스크립트
/// 플레이어 방향 전환(flipX)에 따라 손의 위치·회전·Order 자동 조정
/// HandLeft(isLeft=true), HandRight(isLeft=false) 각각에 부착
/// </summary>
public class Hand : MonoBehaviour
{
    [Header("# 설정")]
    public bool isLeft;             // 왼손(근접) = true / 오른손(원거리) = false
    public SpriteRenderer spriter; // 이 손의 SpriteRenderer (무기 스프라이트 교체용으로 public)

    SpriteRenderer player;          // 플레이어의 SpriteRenderer (flipX 감지용)

    // 정면(우측) 방향일 때 오른손 위치
    Vector3 rightPos  = new Vector3( 0.35f, -0.15f, 0);
    // 반전(좌측) 방향일 때 오른손 위치 (X축 대칭)
    Vector3 rightPosR = new Vector3(-0.35f, -0.15f, 0);

    // 정면일 때 왼손 회전 (-35도: 무기를 앞으로 기울임)
    Quaternion leftRot  = Quaternion.Euler(0, 0, -35f);
    // 반전일 때 왼손 회전 (-145도: 좌우 대칭)
    Quaternion leftRotR = Quaternion.Euler(0, 0, -145f);

    void Awake()
    {
        spriter = GetComponent<SpriteRenderer>();
        // GetComponentsInParent: [0]=자기 자신, [1]=부모(Player)의 SpriteRenderer
        player  = GetComponentsInParent<SpriteRenderer>()[1];
    }

    void LateUpdate()
    {
        // 플레이어의 flipX로 현재 방향 감지 (LateUpdate: Player의 flipX 처리 후 실행)
        bool isReverse = player.flipX;

        if (isLeft)
        {
            // 왼손(근접무기): 회전 변경 + 스프라이트 상하 반전 + Order 조정
            transform.localRotation = isReverse ? leftRotR : leftRot;
            spriter.flipY           = isReverse;    // 반전 시 상하 뒤집기 (칼날 방향 유지)
            // 반전 시: 왼손이 오른쪽에 있으므로 뒤로(4) / 정면: 앞으로(6)
            spriter.sortingOrder    = isReverse ? 4 : 6;
        }
        else
        {
            // 오른손(원거리무기): 위치 변경 + 좌우 반전 + Order 조정
            transform.localPosition = isReverse ? rightPosR : rightPos;
            spriter.flipX           = isReverse;    // 반전 시 총 방향 뒤집기
            // 반전 시: 오른손이 왼쪽에 있으므로 앞으로(6) / 정면: 뒤로(4)
            spriter.sortingOrder    = isReverse ? 6 : 4;
        }
    }
}
