using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 한 런의 진행과 끝을 맡는다 — 시간을 재고, 보스를 내보내고, 끝을 판정하고, 정산한다.
///
/// <b>런에 대한 사실은 전부 여기에 있다</b> — 흐른 시간·처치 수·남은 리롤·지금 상태.
/// 예전에는 GameManager 와 나뉘어 있어서 "끝났는가"를 두 군데서 물어봐야 했다.
///
/// 씬의 [Run] 오브젝트에 붙인다. 없으면 런이 안 끝나고 시간도 안 흐른다.
/// </summary>
public class RunDirector : MonoBehaviour
{
    /// <summary>씬에 하나. 결과 패널·보스 표식이 이 통로로 찾는다.</summary>
    public static RunDirector Current { get; private set; }

    /// <summary>런 시작으로부터 흐른 시간(초). 디렉터가 없는 씬에서는 0.</summary>
    public static float RunTime => Current != null ? Current.Elapsed : 0f;

    /// <summary>이번 런의 처치 수.</summary>
    public static int KillCount => Current != null ? Current.Kills : 0;

    /// <summary>
    /// 아직 진행 중인가. 디렉터가 없는 씬(증강 테스트 등)에서는 진행 중으로 본다 —
    /// 없다고 게임이 멈춰 보이면 테스트를 못 한다.
    /// </summary>
    public static bool IsPlaying => Current == null || Current.State == RunState.Playing;

    [Header("마무리")]
    [Tooltip("끝난 뒤 결과 씬으로 넘어가기까지의 뜸(초). 죽는 연출을 볼 시간이다.\n" +
             "게임이 멈춘 뒤라 실제 시간으로 잰다.")]
    [SerializeField] float endDelay = 1.2f;

    [Tooltip("결과 씬 이름. 비우면 씬을 안 넘기고 그 자리에 멈춘다.")]
    [SerializeField] string resultScene = "StageResult";

    public RunState State { get; private set; } = RunState.Playing;

    /// <summary>런 중에 주워 모은 비트. 드랍물이 생기면 AddBits 로 쌓인다.</summary>
    public int Bits { get; private set; }

    /// <summary>남은 리롤 수. 증강 선택 화면이 읽어서 표시하고 쓴다.</summary>
    public int Rerolls { get; private set; }

    /// <summary>
    /// 버틴 시간(초). 상한이 없다 — 런의 끝은 시간이 아니라 보스 처치나 사망이 정한다.
    /// 난이도(Spawner)와 정산의 기준으로 쓰인다.
    /// </summary>
    public float Elapsed { get; private set; }

    /// <summary>이번 런의 처치 수.</summary>
    public int Kills { get; private set; }

    readonly List<string> defeated = new();

    /// <summary>이번 런의 수치가 담긴 스테이지. Start 에서 집는다.</summary>
    StageData stage;

    /// <summary>시각 순으로 세운 보스 일정. 스테이지 것을 복사해 쓴다.</summary>
    readonly List<BossSpawn> schedule = new();

    PlayerHealth health;
    bool finishing;

    void Awake()
    {
        Current = this;

        // 씬을 다시 로드해도 static 목록은 살아남는다. 런이 시작되는 이 자리에서 씻는다
        RunLifecycle.ResetStatics();
    }

    /// <summary>스테이지 표를 이번 런의 상태로 옮겨 담는다.</summary>
    void LoadStage()
    {
        stage = StageContext.Active;

        if (stage == null)
        {
            Debug.LogError("[RunDirector] 스테이지가 없다. 보스도 정산도 안 돈다.", this);
            return;
        }

        Rerolls = Mathf.Max(0, stage.startingRerolls);

        // 에셋의 목록을 그대로 쓰면 Spawned 플래그가 에셋에 남아 다음 런에 안 나온다
        schedule.Clear();

        for (int i = 0; i < stage.bosses.Count; i++)
        {
            BossSpawn src = stage.bosses[i];
            if (src == null) continue;

            schedule.Add(new BossSpawn
            {
                enemy = src.enemy,
                nameOverride = src.nameOverride,
                atSeconds = src.atSeconds,
                spawnDistance = src.spawnDistance,
                endsRun = src.endsRun,
                healthScale = src.healthScale,
                speedScale = src.speedScale,
                damageScale = src.damageScale,
                sizeScale = src.sizeScale
            });
        }

        // 시각 순으로 세워두면 매 프레임 앞에서부터 하나씩만 보면 된다
        schedule.Sort((a, b) => a.atSeconds.CompareTo(b.atSeconds));
    }

    void OnDestroy()
    {
        if (health != null) health.Died -= OnPlayerDied;
        if (Current == this) Current = null;
    }

    void Start()
    {
        // StageSetup 이 Awake 에서 확정한다. 그래서 여기는 Start 여야 한다
        LoadStage();

        // PlayerHealth 는 GameManager.Awake 가 붙여줄 수도 있어서 Start 에서 찾는다
        if (GameManager.instance != null && GameManager.instance.player != null)
            health = GameManager.instance.player.GetComponent<PlayerHealth>();

        if (health != null) health.Died += OnPlayerDied;
        else Debug.LogWarning("[RunDirector] PlayerHealth 를 못 찾았다. 사망해도 런이 안 끝난다.");

        WarnIfNoEnding();
    }

    void Update()
    {
        if (State != RunState.Playing) return;

        Elapsed += Time.deltaTime;

        ReleaseDueBosses();
    }

    /// <summary>적을 잡았다. Enemy 가 부른다.</summary>
    public void AddKill()
    {
        if (State != RunState.Playing) return;

        Kills++;
    }

    // ── 보스 ──────────────────────────────────────────────

    void ReleaseDueBosses()
    {
        float now = Elapsed;

        for (int i = 0; i < schedule.Count; i++)
        {
            BossSpawn entry = schedule[i];

            // 정렬돼 있으므로 아직 안 될 것을 만나면 뒤는 볼 것도 없다
            if (now < entry.atSeconds) return;
            if (entry.Spawned) continue;

            entry.Spawned = true;
            Release(entry);
        }
    }

    void Release(BossSpawn entry)
    {
        if (!entry.IsValid)
        {
            Debug.LogWarning($"[RunDirector] '{entry.DisplayName}' 에 적 데이터나 프리팹이 없어 건너뛴다.");
            return;
        }

        // 보스는 풀을 쓰지 않는다. 풀 반납도 OnDisable 이라 처치와 구별할 수 없기 때문
        GameObject boss = Instantiate(entry.enemy.prefab, PlaceFor(entry), Quaternion.identity);

        // 잡몹과 같은 통로로 수치를 새긴다
        if (boss.TryGetComponent(out Enemy body)) body.Init(entry.enemy, entry.Scale);

        if (!boss.TryGetComponent(out BossMarker marker)) marker = boss.AddComponent<BossMarker>();

        marker.Bind(this, entry);

        // 등장 문구는 EnemyData 가 정한다. 안 적어뒀으면 기본 경고를 쓴다
        if (LogManager.Instance != null)
        {
            string line = entry.enemy.SpawnMessage ?? $"WARNING: {entry.DisplayName} DETECTED";
            LogManager.Instance.Error(line);
        }
    }

    Vector3 PlaceFor(BossSpawn entry)
    {
        Transform player = GameManager.instance != null && GameManager.instance.player != null
            ? GameManager.instance.player.transform
            : null;

        if (player == null) return Vector3.zero;
        if (entry.spawnDistance <= 0f) return player.position;

        // 어느 쪽에서 올지 모르게 매번 다른 방향으로
        Vector2 direction = Random.insideUnitCircle.normalized;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;

        return player.position + (Vector3)(direction * entry.spawnDistance);
    }

    /// <summary>BossMarker 가 사라질 때 부른다.</summary>
    public void OnBossDown(BossMarker marker)
    {
        if (State != RunState.Playing || marker == null || marker.Origin == null) return;

        defeated.Add(marker.Origin.DisplayName);

        // 처치 로그는 Enemy.Dead 가 EnemyData 문구로 이미 냈다. 여기서는 런 흐름만 남긴다
        if (LogManager.Instance != null)
            LogManager.Instance.System($"{marker.Origin.DisplayName} TERMINATED");

        if (marker.Origin.endsRun) Finish(cleared: true);
    }

    // ── 끝내기 ────────────────────────────────────────────

    void OnPlayerDied() => Finish(cleared: false);

    /// <summary>비트를 쌓는다. 드랍물이 생기면 여기로 넣으면 된다.</summary>
    public void AddBits(int amount)
    {
        if (State != RunState.Playing || amount <= 0) return;

        Bits += amount;
    }

    /// <summary>남은 리롤이 바뀔 때. 표시하는 쪽이 구독한다.</summary>
    public event System.Action<int> RerollsChanged;

    /// <summary>리롤을 준다. 아이템·증강이 부르는 자리.</summary>
    public void AddRerolls(int amount)
    {
        if (State != RunState.Playing || amount <= 0) return;

        Rerolls += amount;

        RerollsChanged?.Invoke(Rerolls);
    }

    /// <summary>
    /// 리롤 하나를 쓴다. 없으면 아무 일도 없이 false —
    /// 부르는 쪽이 남았는지 따로 확인하지 않아도 되게 판정과 소비를 한 번에 한다.
    /// </summary>
    public bool TrySpendReroll()
    {
        if (Rerolls <= 0) return false;

        Rerolls--;

        RerollsChanged?.Invoke(Rerolls);

        return true;
    }

    void Finish(bool cleared)
    {
        if (finishing) return;

        finishing = true;
        State = cleared ? RunState.Cleared : RunState.Failed;

        RunResult.Last = Settle(cleared);

        PlayerProgress.AddBits(RunResult.Last.Reward);
        PlayerProgress.Save();

        if (LogManager.Instance != null)
        {
            if (cleared) LogManager.Instance.System("COMPILE SUCCEEDED");
            else LogManager.Instance.Error("COMPILE FAILED");
        }

        // 결과 씬으로 넘어가는 동안 적이 달려들지 않게 붙잡는다.
        // 화면이 뭘 하든 이 이유가 남아 있는 한 시간은 안 흐른다
        TimeControl.Hold(this);

        StartCoroutine(GoToResult());
    }

    RunResult Settle(bool cleared)
    {
        int kills = Kills;
        float elapsed = Elapsed;

        var result = new RunResult
        {
            Cleared = cleared,
            Elapsed = elapsed,
            Kills = kills,
            Level = GameManager.instance != null && GameManager.instance.levelSystem != null
                ? GameManager.instance.levelSystem.Level
                : 1,
            BitsCollected = Bits,

            // 계수는 스테이지가 정한다. 스테이지가 없으면 보상도 0
            RewardFromKills = stage != null ? kills * stage.killValue : 0,
            RewardFromTime = stage != null ? Mathf.FloorToInt(elapsed * stage.timeValue) : 0,
            RewardFromClear = cleared && stage != null ? stage.clearBonus : 0,

            BossesDefeated = new List<string>(defeated),
            Damage = RunStats.Ranked(),
            TotalDamage = RunStats.TotalDamage
        };

        result.Reward = Bits
                      + result.RewardFromKills
                      + result.RewardFromTime
                      + result.RewardFromClear;

        return result;
    }

    IEnumerator GoToResult()
    {
        // 게임이 멈춘 뒤라 Time.timeScale 을 따르지 않는 실제 시간으로 센다
        yield return new WaitForSecondsRealtime(endDelay);

        if (string.IsNullOrEmpty(resultScene)) yield break;

        // 다음 씬이 멈춘 채로 시작하지 않게 전부 놓는다.
        // 붙잡던 화면들이 씬과 함께 파괴되면 스스로 놓을 수 없다
        TimeControl.ReleaseAll();

        SceneManager.LoadScene(resultScene);
    }

    // ── 확인용 ────────────────────────────────────────────

    /// <summary>이길 방법이 없는 설정을 조용히 넘어가지 않게 한다.</summary>
    void WarnIfNoEnding()
    {
        if (stage == null) return;

        for (int i = 0; i < schedule.Count; i++)
            if (schedule[i].endsRun && schedule[i].IsValid) return;

        Debug.LogWarning("[RunDirector] endsRun 이 켜진 보스가 없다. 이 런은 죽어야만 끝난다.");
    }
}
