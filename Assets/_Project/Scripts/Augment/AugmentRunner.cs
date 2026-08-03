using UnityEngine;

/// <summary>증강 1개를 실행한다. Tick은 AugmentManager가 호출한다.</summary>
public class AugmentRunner : MonoBehaviour
{
    [SerializeField] bool logTrigger = true;

    public AugmentInstance Instance { get; private set; }

    public void Setup(AugmentInstance instance)
    {
        Instance = instance;

        AugmentData d = instance.Data;
        if(d.trigger == null)
        {
            Debug.LogWarning($"[{d.name}] Trigger error", this);
        }

        if(d.targeting == null)
        {
            Debug.LogWarning($"[{d.name}] Targeting error", this);
        }

        if(d.deliveries == null)
        {
            Debug.LogWarning($"[{d.name}] Deliveries error", this);
        }

    }

    public void Tick(float deltaTime)
    {
        if(Instance == null) return;

        AugmentData data = Instance.Data;
        if(data.trigger == null || data.targeting == null) return;

        // 1. 발동 판정 — 주문서 없이 인스턴스만
        if(!data.trigger.Evaluate(Instance, deltaTime)) return;

        // 발동 확정 — 이번 발사 전용 주문서
        var ctx = new AugmentContext();
        ctx.Begin(transform, Instance);

        // 2. 타겟팅
        data.targeting.Resolve(ctx);

        if(ctx.Targets.IsEmpty)
        {
            if(data.noTargetPolicy == NoTargetPolicy.Consume)
            {
                data.trigger.Consume(Instance);
            }
            

            return;
        }

        data.trigger.Consume(Instance);
        // 3. 전달
        for(int i = 0; i < data.deliveries.Count; i++)
            data.deliveries[i]?.Execute(ctx, hit => OnHit(ctx, hit));
    }

    void OnHit(AugmentContext ctx, HitInfo hit)
    {
        if(logTrigger)
            Debug.Log($"[{ctx.Instance.Data.displayName}] 적중 #{hit.Index} → {hit.Target.name}", this);

        // ④ Effect 는 다음 단계
    }
}