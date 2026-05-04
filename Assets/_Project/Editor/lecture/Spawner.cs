using UnityEngine;

/// <summary>
/// 몬스터 소환 스크립트
/// 게임 경과 시간에 따라 레벨을 자동 계산하고 타이머로 몬스터 소환
/// </summary>
public class Spawner : MonoBehaviour
{
    [Header("# 소환 데이터")]
    public SpawnData[] spawnData;   // 레벨별 소환 설정 배열 (Inspector에서 직접 입력)

    public Transform[] spawnPoints; // 소환 위치 배열 (자식 Point 오브젝트들)

    [Header("# 레벨 타임")]
    public float levelTime;         // 레벨당 지속 시간 (maxGameTime / spawnData.Length 자동 계산)

    int level;                      // 현재 소환 레벨
    float timer;                    // 소환 타이머

    void Awake()
    {
        // GetComponentsInChildren: 자기 자신(인덱스 0) + 자식 Point들
        // 인덱스 0은 Spawner 자신의 Transform이므로 Spawn()에서 1부터 사용
        spawnPoints = GetComponentsInChildren<Transform>();

        // 레벨당 시간 자동 계산
        // 예) maxGameTime=300, spawnData 6개 → levelTime=50초
        levelTime = GameManager.instance.maxGameTime / spawnData.Length;
    }

    void Update()
    {
        if (!GameManager.instance.isLive) return;

        timer += Time.deltaTime;

        // 현재 레벨 계산: 경과 시간 / 레벨당 시간 (내림)
        // Mathf.Min으로 배열 범위 초과 방지
        level = Mathf.Min(
            Mathf.FloorToInt(GameManager.instance.gameTime / levelTime),
            spawnData.Length - 1
        );

        // 현재 레벨의 소환 간격이 되면 소환
        if (timer > spawnData[level].spawnTime)
        {
            timer = 0;
            Spawn();
        }
    }

    void Spawn()
    {
        // 풀에서 Enemy 꺼내기 (인덱스 0)
        GameObject enemy = GameManager.instance.pool.Get(0);

        // 랜덤 소환 위치 선택 (인덱스 1부터: 자기 자신 제외)
        enemy.transform.position = spawnPoints[Random.Range(1, spawnPoints.Length)].position;

        // 현재 레벨 데이터로 Enemy 초기화
        enemy.GetComponent<Enemy>().Init(spawnData[level]);
    }
}

/// <summary>
/// 레벨별 소환 데이터 구조체
/// [System.Serializable]: Inspector에서 배열 요소로 펼쳐서 편집 가능
/// </summary>
[System.Serializable]
public class SpawnData
{
    public int   spriteType;    // 몬스터 외형 인덱스 (animCon 배열 인덱스와 일치)
    public float spawnTime;     // 이 레벨에서의 소환 간격(초)
    public float health;        // 몬스터 체력
    public float speed;         // 몬스터 이동 속도
}
