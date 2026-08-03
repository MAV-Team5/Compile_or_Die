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

        if (!data.trigger.Evaluate(ctx, deltaTime)) return;

        if (logTrigger)
            Debug.Log($"[{data.displayName}] Lv.{Instance.Level} 발동", this);

        // TODO: Targeting → Delivery → Effect
    }
}