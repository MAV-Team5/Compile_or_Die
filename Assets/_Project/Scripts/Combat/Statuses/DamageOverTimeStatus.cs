using UnityEngine;

/// <summary>
/// 주기마다 피해를 준다. 독 · 화상 · 과부하 계열.
/// 피해는 DamagePipeline 을 그대로 통과하므로 탐색 표식 보정도 함께 받는다.
/// </summary>
[System.Serializable]
[ModuleInfo("주기마다 피해", "한 번에 몰아 때리려면 Damage 효과를 쓸 것")]
public class DamageOverTimeStatus : Status
{
    [Tooltip("피해를 주는 간격(초). 0.5면 초당 두 번.")]
    public float interval = 0.5f;

    [Tooltip("한 번에 줄 피해. 0이면 상태를 걸 때 정한 세기를 그대로 쓴다.\n" +
             "세기는 보통 시트의 효과 피해(effectDamage)에서 온다.")]
    public float damagePerTick = 0f;

    [Fx("틱 연출", "대상 위치")]
    public FxGroup tickFx = new();

    public override void Tick(StatusHolder holder, StatusHolder.Active active, float deltaTime)
    {
        if (interval <= 0f) return;

        active.TickTimer -= deltaTime;
        if (active.TickTimer > 0f) return;

        active.TickTimer = interval;

        float amount = damagePerTick > 0f ? damagePerTick : active.Magnitude;
        if (amount <= 0f || holder.Receiver == null) return;

        // 붙이기를 켜면 걸린 적을 따라다닌다. 화상·중독처럼 몸에 붙는 연출용
        tickFx.PlayAt(holder.transform.position, default, 0f, holder.transform);

        // 절대 규칙 1 — 모든 피해는 DamagePipeline 을 통과한다.
        // 표식 보정도 여기서 함께 얹힌다
        DamagePipeline.Process(new DamageContext(
            null, holder.Receiver, amount, holder.transform)
        {
            // 지속 피해도 건 증강의 분류 색으로 뜬다
            SourceAugment = active.Owner
        });
    }
}
