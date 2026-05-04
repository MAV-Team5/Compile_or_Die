using UnityEngine;

/// <summary>
/// 타일맵 및 적 재배치 스크립트
/// Area(플레이어 감지 영역) 밖으로 나가면 플레이어 방향으로 재배치
/// → 무한맵 및 적 순환 소환 구현
/// </summary>
public class Reposition : MonoBehaviour
{
    Collider2D coll; // Enemy 재배치 시 생사 확인용 (coll.enabled = isLive 판단)

    void Awake()
    {
        coll = GetComponent<Collider2D>();
    }

    /// <summary>
    /// Area 콜라이더에서 이 오브젝트가 이탈할 때 호출
    /// Tag에 따라 Ground(타일맵) / Enemy 각각 다른 재배치 로직 적용
    /// </summary>
    void OnTriggerExit2D(Collider2D collision)
    {
        // Area 태그(플레이어 감지 영역)가 아니면 무시
        if (!collision.CompareTag("Area")) return;

        Vector3 playerPos = GameManager.instance.player.transform.position;
        Vector3 myPos     = transform.position;

        // 절대값(Abs) 거리: X/Y 중 어느 방향이 더 멀어졌는지 판단
        float diffX = Mathf.Abs(playerPos.x - myPos.x);
        float diffY = Mathf.Abs(playerPos.y - myPos.y);

        // 부호 있는 거리: 이동 방향 결정 (양수=플레이어가 오른쪽/위, 음수=왼쪽/아래)
        float dirX = playerPos.x - myPos.x;
        float dirY = playerPos.y - myPos.y;

        switch (transform.tag)
        {
            case "Ground":
                // X 방향으로 더 멀어졌으면 X축 이동, 아니면 Y축 이동
                // 40 = 타일맵 크기(20) × 2장 → 플레이어 앞쪽 타일맵 위치로 점프
                if (diffX > diffY)
                    transform.Translate(dirX > 0 ? Vector3.right * 40 : Vector3.left  * 40);
                else
                    transform.Translate(dirY > 0 ? Vector3.up    * 40 : Vector3.down  * 40);
                break;

            case "Enemy":
                // 살아있는(콜라이더 활성) 적만 재배치 (시체는 재배치 안 함)
                if (coll.enabled)
                {
                    // 플레이어 방향으로 거리×2 이동 → 화면 밖(플레이어 앞)에 재배치
                    Vector3 dist   = playerPos - myPos;
                    // 랜덤 오프셋: 같은 위치에 몰리지 않게 분산
                    Vector3 random = new Vector3(
                        Random.Range(-3f, 3f),
                        Random.Range(-3f, 3f),
                        0);
                    transform.position = playerPos + dist * 2 + random;
                }
                break;
        }
    }
}
