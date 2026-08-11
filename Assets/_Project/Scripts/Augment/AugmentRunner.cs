using UnityEngine;

/// <summary>증강 1개를 실행한다. Tick은 AugmentManager가 호출한다.</summary>
public class AugmentRunner : MonoBehaviour
{
    [SerializeField] bool logTrigger = false;

    /// <summary>쿨타임이 막 찬 순간 1회. 대기 중에는 다시 울리지 않는다.</summary>
    public event System.Action BecameReady;

    public AugmentInstance Instance { get; private set; }

    bool wasReady;

    public void Setup(AugmentInstance instance)
    {
        Instance = instance;

        AugmentData d = instance.Data;
        if (d.trigger == null)   Debug.LogWarning($"[{d.name}] trigger 미조립", this);
        if (d.targeting == null) Debug.LogWarning($"[{d.name}] targeting 미조립", this);
    }

    public void Tick(float deltaTime)
    {
        if (Instance == null) return;

        AugmentData data = Instance.Data;
        if (data.trigger == null || data.targeting == null) return;

        // ① 발동 판정. 쿨타임은 아직 소비하지 않는다
        bool ready = data.trigger.Evaluate(Instance, deltaTime);

        // 준비 완료로 전환된 프레임에만 알림
        if (ready && !wasReady) BecameReady?.Invoke();
        wasReady = ready;

        if (!ready) return;

        var ctx = new AugmentContext();
        ctx.Begin(transform, Instance);

        // ②③④ 타겟팅 → 전달 → 효과
        bool fired = AugmentPipeline.Run(ctx, data.targeting, data.deliveries, data.effects);

        if (!fired)
        {
            // 대상이 없을 때 쿨타임을 버릴지 유지할지는 증강마다 다르다
            if (data.noTargetPolicy == NoTargetPolicy.Consume)
                data.trigger.Consume(ctx);

            return;
        }

        data.trigger.Consume(ctx);

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
