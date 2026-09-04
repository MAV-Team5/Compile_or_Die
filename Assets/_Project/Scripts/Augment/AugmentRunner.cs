using UnityEngine;

/// <summary>
/// 증강 1개를 실행한다. 보통은 AugmentManager 가 Tick 을 부른다.
///
/// 원점은 자기 transform 이다 — 플레이어 밑에 있으면 플레이어 기준으로,
/// 월드에 떼어놓으면 그 자리를 기준으로 돈다. 소환물이 이 성질을 쓴다.
/// </summary>
public class AugmentRunner : MonoBehaviour
{
    [Tooltip("켜면 관리자를 안 거치고 스스로 매 프레임 돈다.\n" +
             "소환물처럼 월드에 떨어져 나온 증강용. 플레이어의 증강은 꺼둘 것.")]
    [SerializeField] bool selfDriven = false;

    [SerializeField] bool logTrigger = false;

    /// <summary>쿨타임이 막 찬 순간 1회. 대기 중에는 다시 울리지 않는다.</summary>
    public event System.Action BecameReady;

    public AugmentInstance Instance { get; private set; }

    bool wasReady;

    /// <summary>월드에 떨어져 나온 러너는 스스로 돈다. 관리자 목록에 없기 때문이다.</summary>
    void Update()
    {
        if (selfDriven) Tick(Time.deltaTime);
    }

    /// <summary>소환물처럼 코드로 만들어 붙일 때. 인스펙터를 못 만지므로 여기서 켠다.</summary>
    public void DriveSelf() => selfDriven = true;

    public void Setup(AugmentInstance instance)
    {
        Instance = instance;

        // 트리거가 "내 주변" 을 물을 수 있게 원점을 알려준다
        instance.Owner = transform;

        AugmentData d = instance.Data;

        // 내부 증강은 스스로 발동하지 않고 뿌리에 얹히기만 한다 — 트리거가 없는 것이 정상이다.
        // 뽑을 때마다 경고가 뜨면 진짜 미조립을 놓치게 된다
        if (d.rootAugment != null) return;

        if (d.trigger == null)   Debug.LogWarning($"[{d.name}] trigger 미조립", this);
        if (d.targeting == null) Debug.LogWarning($"[{d.name}] targeting 미조립", this);
    }

    public void Tick(float deltaTime)
    {
        if (Instance == null) return;

        AugmentData data = Instance.Data;

        // ★ 에셋이 아니라 Build 를 본다. 내부 증강이 트리거를 갈아끼웠으면 그쪽이 정답이다
        AugmentBuild build = Instance.Build;

        if (build.Trigger == null || build.Targeting == null) return;

        // ① 발동 판정. 쿨타임은 아직 소비하지 않는다
        bool ready = build.Trigger.Evaluate(Instance, deltaTime);

        // 준비 완료로 전환된 프레임에만 알림
        if (ready && !wasReady) BecameReady?.Invoke();
        wasReady = ready;

        if (!ready) return;

        var ctx = new AugmentContext();
        ctx.Begin(transform, Instance);

        // 이번 발동이 이 주기의 첫 발인가. 장탄식에서 "회차마다 첫 발만 강화" 를 만들 수 있게
        ctx.FirstOfCycle = build.Trigger.FirstOfCycle(Instance);

        // ②③④ 타겟팅 → 전달 → 효과
        bool fired = AugmentPipeline.Run(ctx, build.Targeting, build.Deliveries, build.Effects);

        if (!fired)
        {
            // 대상이 없을 때 쿨타임을 버릴지 유지할지는 발동 조건이 정한다
            if (build.Trigger.noTargetPolicy == NoTargetPolicy.Consume)
                build.Trigger.Consume(ctx);

            return;
        }

        build.Trigger.Consume(ctx);

        if (logTrigger)
            Debug.Log($"[{data.displayName}] Lv.{Instance.Level} 발동", this);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (Instance == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Instance.Stat.range);
    }
#endif
}
