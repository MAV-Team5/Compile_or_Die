using UnityEngine;

/// <summary>
/// 무기 시스템 스크립트
/// 근접(id=0): Weapon 오브젝트 회전 + Bullet 자식 배치
/// 원거리(id=1+): 타이머로 자동 발사
/// Item.OnClick()에서 Init(ItemData)로 초기화
/// </summary>
public class Weapon : MonoBehaviour
{
    [Header("# 무기 정보")]
    public int id;          // 무기 종류 ID (0=근접, 1=원거리)
    public int prefabId;    // PoolManager.prefabs 배열 인덱스
    public float damage;    // 현재 데미지
    public int count;       // 근접: 동시 배치 개수 / 원거리: 관통력
    public float speed;     // 근접: 회전 속도 / 원거리: 발사 간격(초)

    float timer;            // 원거리 발사 타이머
    Player player;          // 플레이어 참조 (Init에서 GameManager 통해 직접 획득)

    // Awake 제거 — Init()에서 직접 player 참조를 받으므로 불필요

    void Update()
    {
        if (!GameManager.instance.isLive) return;

        switch (id)
        {
            case 0:
                // 근접무기: Z축 회전 (음수 = 시계방향)
                transform.Rotate(Vector3.forward * speed * Time.deltaTime);
                break;
            default:
                // 원거리무기: speed초마다 1발 발사
                timer += Time.deltaTime;
                if (timer > speed)
                {
                    timer = 0;
                    Fire();
                }
                break;
        }
    }

    /// <summary>
    /// Item.OnClick()에서 호출. 무기 생성 시 1회 실행
    /// 부모(Player)를 GameManager에서 직접 참조 → new GameObject() 후에도 안전
    /// </summary>
    public void Init(ItemData data)
    {
        // GameManager를 통해 Player 직접 참조 (Awake의 GetComponentInParent 대체)
        player = GameManager.instance.player;
        transform.parent         = player.transform;
        transform.localPosition  = Vector3.zero;

        name     = "Weapon " + data.itemId;
        id       = data.itemId;
        prefabId = data.prefabId;

        // 캐릭터 특성 반영 (Character 속성은 playerId 기반으로 자동 계산)
        damage = data.damages[0] * Character.WeaponDamage;
        count  = data.counts[0]  + Character.WeaponCount;

        switch (id)
        {
            case 0:
                speed = -150f * Character.WeaponSpeed; // 근접: 회전 속도
                Batch();    // 근접무기 배치
                break;
            default:
                speed = 0.3f * Character.WeaponSpeed;  // 원거리: 발사 간격
                break;
        }

        // 해당 무기 타입의 손 스프라이트 활성화
        // itemType: Melee=0 → hands[0](왼손), Range=1 → hands[1](오른손)
        if ((int)data.itemType < player.hands.Length)
        {
            Hand hand = player.hands[(int)data.itemType];
            hand.spriter.sprite = data.hand;
            hand.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 레벨업 시 호출. 데미지와 개수(관통력) 증가
    /// </summary>
    public void LevelUp(float damage, int count)
    {
        this.damage += damage * Character.WeaponDamage;
        this.count  += count;
        if (id == 0) Batch(); // 근접무기는 개수 변경 시 재배치
    }

    /// <summary>
    /// 근접무기 Bullet 오브젝트를 원형으로 균등 배치
    /// 기존 자식 재활용 후 부족한 만큼만 풀에서 새로 꺼냄
    /// </summary>
    void Batch()
    {
        for (int i = 0; i < count; i++)
        {
            Transform bullet;

            if (i < transform.childCount)
            {
                // 이미 자식으로 있는 Bullet 재활용
                bullet = transform.GetChild(i);
            }
            else
            {
                // 풀에서 새로 꺼내서 자식으로 등록
                bullet = GameManager.instance.pool.Get(prefabId).transform;
                bullet.parent = transform;
            }

            // 위치/회전 초기화
            bullet.localPosition = Vector3.zero;
            bullet.localRotation = Quaternion.identity;

            // 균등 각도 배치: 360 / count * index
            // 예) count=4 → 0°, 90°, 180°, 270°
            Vector3 rotVec = Vector3.forward * 360f * i / count;
            bullet.Rotate(rotVec);

            // 회전된 Up 방향으로 1.5 이동 → 자동으로 원형 배치
            // Space.World: 월드 좌표 기준 이동 (로컬 회전 영향 받지 않게)
            bullet.Translate(bullet.up * 1.5f, Space.World);

            // 근접무기 총알 초기화: per=-100 (무한 관통 기준값)
            bullet.GetComponent<Bullet>().Init(damage, -100, Vector3.zero);
        }
    }

    /// <summary>
    /// 원거리무기 발사. Scanner로 가장 가까운 적 방향으로 총알 발사
    /// </summary>
    void Fire()
    {
        // 탐지된 적이 없으면 발사 안 함
        if (!player.scanner.nearestTarget) return;

        Vector3 targetPos = player.scanner.nearestTarget.position;
        Vector3 dir = (targetPos - transform.position).normalized; // 발사 방향 정규화

        // 풀에서 총알 꺼내기
        Transform bullet = GameManager.instance.pool.Get(prefabId).transform;
        bullet.position = transform.position;

        // 총알 회전: Vector3.up(위쪽)을 적 방향으로 회전
        bullet.rotation = Quaternion.FromToRotation(Vector3.up, dir);

        // 원거리 총알 초기화: per=count (관통력), dir로 velocity 적용
        bullet.GetComponent<Bullet>().Init(damage, count, dir);

        AudioManager.instance.PlaySfx(AudioManager.Sfx.Range);
    }
}
