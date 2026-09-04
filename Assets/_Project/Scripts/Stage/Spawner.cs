using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지의 웨이브대로 적을 내보낸다.
///
/// <b>수치는 여기 없다.</b> 무엇이 언제 몇 마리 나오는지는 전부
/// <see cref="StageData.waves"/> 에 있다. 이 파일은 그 표를 읽고 시간을 재는 일만 한다.
///
/// <b>플레이어의 자식이어야 한다.</b> 스폰 지점(Point)이 자식으로 달려 있어
/// 플레이어를 따라다닌다 — 어디로 걸어가든 주변에서 적이 나오게 하려는 것이다.
/// </summary>
public class Spawner : MonoBehaviour
{
    [Tooltip("적이 나올 자리. 비우면 자식 오브젝트를 자동으로 모은다.\n" +
             "자기 자신은 빠지므로 Point 들만 자식으로 두면 된다.")]
    [SerializeField] Transform[] spawnPoints;

    [Header("멀어진 적 회수")]
    [Tooltip("플레이어에게서 이만큼 멀어지면 앞쪽 스폰 지점으로 옮긴다.\n\n" +
             "★ 화면 대각선보다 넉넉히 크게 둘 것 — 보이는 자리에서 사라지면 순간이동이 들킨다.\n" +
             "0이면 회수하지 않는다.")]
    [SerializeField] float recycleDistance = 30f;

    [Tooltip("몇 초마다 훑어볼지. 매 프레임 볼 이유가 없고,\n" +
             "경계에 걸친 적이 붙었다 떨어졌다 하는 것도 막아준다.")]
    [SerializeField] float recycleInterval = 1f;

    [Tooltip("한 번에 옮길 최대 마릿수. 0이면 제한 없음.\n\n" +
             "★ 이게 없으면 뒤에 늘어선 꼬리가 한꺼번에 앞으로 와서 벽처럼 쏟아진다.\n" +
             "조금씩 나눠 옮겨야 사방에서 스며드는 모양이 된다.")]
    [SerializeField] int recycleBatch = 3;

    [Header("설치물(상자) 배치")]
    [Tooltip("플레이어에게서 최소 이만큼 떨어져 나온다.\n\n" +
             "★ 화면 밖이 기본이다 — 화면 안에 두면 몇 발짝이라 지나가다 저절로 먹히고,\n" +
             "  \"주우러 갈까\" 라는 판단이 사라진다. 어디 있는지는 OffscreenTarget 이 알려준다.")]
    [SerializeField] float stationaryMinDistance = 10f;

    [Tooltip("최대 이만큼. 화살표가 뜨는 거리(OffscreenTarget.showWithin)보다 작아야\n" +
             "나오자마자 표시된다.")]
    [SerializeField] float stationaryMaxDistance = 18f;

    [Header("경험치 오브 나열")]
    [Tooltip("한 처치에서 오브가 여러 개 나올 때 서로 떨어질 간격(유닛).\n\n" +
             "한 줄로 나란히 놓는다 — 무작위로 흩뿌리면 겹쳐서 몇 개인지 안 읽힌다.")]
    [SerializeField] float orbSpacing = 0.45f;

    /// <summary>
    /// 씬에 하나. 적이 죽을 때 자기를 낸 웨이브를 찾아오는 통로다.
    ///
    /// <b>스스로 관리한다</b> — Awake 에서 잡고 OnDestroy 에서 놓는다.
    /// RunLifecycle 에 넣지 않은 이유는 그쪽이 RunDirector.Awake 에서 도는데,
    /// 순서에 따라 막 잡은 참조를 도로 비울 수 있기 때문이다.
    /// </summary>
    public static Spawner Current { get; private set; }

    StageData stage;

    /// <summary>이 스포너가 내보낸 적들. 죽은 것은 훑을 때 걸러낸다.</summary>
    readonly List<Transform> alive = new();

    float recycleTimer;

    /// <summary>
    /// 다음에 훑기 시작할 자리. 매번 앞에서부터 보면
    /// 상한에 걸려 뒤쪽 적은 영영 차례가 안 온다.
    /// </summary>
    int scanCursor;

    void Awake()
    {
        Current = this;

        if (spawnPoints == null || spawnPoints.Length == 0) CollectPoints();
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    void Start()
    {
        // StageSetup 이 Awake 에서 확정한다. 그래서 여기는 Start 여야 한다
        stage = StageContext.Active;

        if (stage == null)
        {
            Debug.LogError("[Spawner] 스테이지가 없다. 적이 안 나온다.", this);
            enabled = false;
            return;
        }

        // 지난 런의 진행도가 남아 있으면 웨이브가 시작하자마자 끝난 것으로 잡힌다
        for (int i = 0; i < stage.waves.Count; i++)
        {
            if (stage.waves[i] == null) continue;

            stage.waves[i].Spawned = 0;
            stage.waves[i].Timer = 0f;
            stage.waves[i].ExpCarry = 0f;
        }
    }

    void Update()
    {
        if (!RunDirector.IsPlaying) return;

        float now = RunDirector.RunTime;
        float dt = Time.deltaTime;

        Recycle(dt);

        for (int i = 0; i < stage.waves.Count; i++)
        {
            StageWave wave = stage.waves[i];

            if (wave == null || !wave.IsValid) continue;
            if (!wave.IsActive(now)) continue;

            Advance(wave, i, dt);
        }
    }

    /// <summary>웨이브 하나의 시계를 굴린다. 때가 되면 한 무더기 내보낸다.</summary>
    void Advance(StageWave wave, int index, float dt)
    {
        wave.Timer -= dt;
        if (wave.Timer > 0f) return;

        // 간격이 0이면 한 프레임에 무한히 쏟아진다. StageData 가 경고하지만 여기서도 막는다
        wave.Timer = Mathf.Max(0.05f, wave.interval);

        int count = Mathf.Max(1, wave.burst);

        // 상한이 있으면 넘겨서 내보내지 않는다
        if (wave.maxSpawns > 0) count = Mathf.Min(count, wave.maxSpawns - wave.Spawned);

        for (int i = 0; i < count; i++) Release(wave, index);
    }

    void Release(StageWave wave, int index)
    {
        GameObject go = GameManager.instance.poolManager.Get(wave.enemy.prefab, PoolType.Enemy);
        if (go == null) return;

        // 설치물은 안 움직이므로 화면 밖 링에 두면 영영 못 본다. 눈에 보이는 자리에 놓는다
        go.transform.position = wave.enemy.stationary ? PickNearPoint() : PickPoint();

        // 물리 좌표는 다음 FixedUpdate 까지 transform 을 따라오지 않는다.
        // 그 사이에 증강이 사거리 검색을 하면 갓 스폰한 적이 원점에 있는 것으로 잡힌다
        Physics2D.SyncTransforms();

        // 웨이브 번호를 새겨 둔다. 죽을 때 이 번호로 어느 웨이브가 낸 적인지 찾는다
        if (go.TryGetComponent(out Enemy enemy)) enemy.Init(wave.enemy, wave.Scale, index);

        alive.Add(go.transform);

        wave.Spawned++;
    }

    // ── 경험치 ────────────────────────────────────────────

    /// <summary>
    /// 적이 죽었다고 알려온다. 그 적을 낸 웨이브가 이번에 오브를 몇 개 낼지 정한다.
    ///
    /// <b>왜 적이 직접 안 꺼내나</b> — 못 준 소수를 다음 처치로 넘겨야 총량이 정확해지는데,
    /// 죽으면 사라지는 적은 그 값을 들고 있을 수 없다. 웨이브가 들어야 한다.
    /// </summary>
    /// <param name="waveIndex">보스처럼 웨이브 소속이 아니면 -1.</param>
    public void ReportKill(int waveIndex, Vector3 at)
    {
        if (stage == null) return;
        if (waveIndex < 0 || waveIndex >= stage.waves.Count) return;

        StageWave wave = stage.waves[waveIndex];
        if (wave == null) return;

        int count = wave.TakeOrbCount();
        if (count <= 0) return;

        DropOrbs(wave.expOrb, count, at);
    }

    /// <summary>
    /// 오브를 한 줄로 나란히 놓는다.
    ///
    /// <b>무작위로 흩뿌리지 않는다.</b> 겹치면 몇 개가 떨어졌는지 안 읽히고,
    /// 같은 총량인데 매번 달라 보여 밸런싱할 때 눈으로 확인할 수가 없다.
    /// </summary>
    void DropOrbs(GameObject prefab, int count, Vector3 at)
    {
        // 가운데를 죽은 자리에 맞춘다. 1개면 정확히 그 자리
        float start = -(count - 1) * 0.5f * orbSpacing;

        for (int i = 0; i < count; i++)
        {
            GameObject orb = GameManager.instance.poolManager.Get(prefab, PoolType.Exp);
            if (orb == null) return;

            orb.transform.position = at + new Vector3(start + orbSpacing * i, 0f, 0f);
        }
    }

    // ── 회수 ──────────────────────────────────────────────

    /// <summary>
    /// 플레이어에게서 너무 멀어진 적을 앞쪽으로 옮긴다.
    ///
    /// <b>왜 필요한가</b> — 플레이어가 한 방향으로 달리면 적들이 뒤에 길게 늘어선다.
    /// 그 꼬리는 영영 못 따라오면서 계산만 잡아먹고, 뒤늦게 뭉쳐서 도착하면
    /// 사방에서 몰려오는 느낌이 아니라 한 덩어리로 밀려오는 모양이 된다.
    ///
    /// 풀에 반납했다가 다시 꺼내지 않고 <b>자리만 옮긴다</b> — 더 싸고,
    /// 웨이브의 누적 마릿수도 건드리지 않는다.
    /// </summary>
    void Recycle(float dt)
    {
        if (recycleDistance <= 0f || alive.Count == 0) return;

        recycleTimer -= dt;
        if (recycleTimer > 0f) return;

        recycleTimer = Mathf.Max(0.1f, recycleInterval);

        Prune();
        if (alive.Count == 0) return;

        // 스포너가 플레이어의 자식이라 여기가 곧 플레이어 자리다
        Vector3 center = transform.position;
        float limit = recycleDistance * recycleDistance;

        int budget = recycleBatch > 0 ? recycleBatch : alive.Count;
        int moved = 0;
        int step = 0;

        // 지난번에 멈춘 자리부터 이어서 훑는다. 한 바퀴가 최대다
        for (; step < alive.Count && moved < budget; step++)
        {
            Transform t = alive[(scanCursor + step) % alive.Count];
            if (t == null) continue;

            if ((t.position - center).sqrMagnitude < limit) continue;

            // 설치물은 옮기지 않는다. 주우러 걸어가는 중에 눈앞에서 사라지면 최악이다 —
            // 멀어진 것은 그냥 두고, 플레이어가 돌아오면 그 자리에 그대로 있다
            if (t.TryGetComponent(out Enemy body) && body.IsStationary) continue;

            // 자리만 옮기면 간선이 따라와 화면을 가로지른다. 옮기기 전에 끊는다
            if (t.TryGetComponent(out LinkHolder links)) links.CutAll();

            t.position = PickPoint();
            moved++;
        }

        // 본 만큼 커서를 밀어둔다. 다음 차례는 여기서 이어진다
        scanCursor = (scanCursor + step) % alive.Count;

        // 옮긴 것이 있을 때만. 매번 부르면 헛일이다
        if (moved > 0) Physics2D.SyncTransforms();
    }

    /// <summary>죽어서 풀로 돌아간 것을 목록에서 뺀다.</summary>
    void Prune()
    {
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            Transform t = alive[i];

            if (t == null || !t.gameObject.activeInHierarchy) alive.RemoveAt(i);
        }

        // 목록이 줄면 커서가 범위를 벗어난다
        if (alive.Count > 0) scanCursor %= alive.Count;
        else scanCursor = 0;
    }

    /// <summary>
    /// 설치물이 나올 자리. 플레이어 주변 <b>화면 밖</b>이다.
    ///
    /// 안 움직이는 것을 화면 안에 두면 몇 발짝이라 그냥 지나가다 먹힌다 —
    /// 상자가 "가끔 뜨는 버프" 가 되고 만다. 화면 밖에 두어야
    /// <b>갈까 말까</b> 라는 판단이 생기고, 어디 있는지는 화면 가장자리 화살표가 알려준다.
    /// </summary>
    Vector3 PickNearPoint()
    {
        Vector2 dir = Random.insideUnitCircle;

        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        float distance = Random.Range(stationaryMinDistance, stationaryMaxDistance);

        return transform.position + (Vector3)(dir.normalized * distance);
    }

    Vector3 PickPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return transform.position;

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        return point != null ? point.position : transform.position;
    }

    /// <summary>
    /// 직계 자식만 모은다. GetComponentInChildren 은 재귀라서
    /// Point 밑에 스프라이트를 하나만 넣어도 그것까지 스폰 지점이 된다.
    /// </summary>
    void CollectPoints()
    {
        spawnPoints = new Transform[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
            spawnPoints[i] = transform.GetChild(i);

        if (spawnPoints.Length == 0)
            Debug.LogWarning("[Spawner] 스폰 지점이 없다. 적이 스포너 자리에서 나온다.", this);
    }
}
