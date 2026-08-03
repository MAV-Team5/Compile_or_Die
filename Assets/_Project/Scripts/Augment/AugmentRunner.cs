using UnityEngine;

/// <summary>증강 1개를 실행한다. Tick은 AugmentManager가 호출한다.</summary>
public class AugmentRunner : MonoBehaviour
{
    [SerializeField] bool logTrigger = true;

    public AugmentInstance Instance { get; private set; }

    readonly AugmentContext ctx = new();

    public void Setup(AugmentInstance instance)
    {
        Instance = instance;
    }

    public void Tick(float deltaTime)
    {
        if (Instance == null) return;

        AugmentData data = Instance.Data;
        if (data.trigger == null) return;

        ctx.Begin(transform, Instance);

        // ① 발동 판정
        if (!data.trigger.Evaluate(ctx, deltaTime)) return;

        // ② 타겟팅
        if (data.targeting == null) return;

        data.targeting.Resolve(ctx);

        if (ctx.Targets.IsEmpty) return;

        if (logTrigger)
            Debug.Log($"[{data.displayName}] Lv.{Instance.Level} → 대상 {ctx.Targets.Enemies.Count}개", this);

        // ③④ 아직
    }
}