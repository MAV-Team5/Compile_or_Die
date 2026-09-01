using UnityEngine;

/// <summary>
/// 쿨타임마다 여러 발을 짧은 간격으로 연속 발사한다. Iteration 전용.
/// 버스트를 다 쏘고 나서야 실제 쿨타임이 시작된다 — Cooldown 처럼 쿨타임 도중에
/// 다시 도는 것과 다르다.
/// </summary>
[System.Serializable]
[ModuleInfo("쿨타임마다 여러 발 연속 발사", "반복을 다 쏘면 그때부터 쿨타임이 시작된다")]
public class BurstCooldownTrigger : TriggerModule
{
    [Tooltip("한 발 쏘고 다음 발까지의 간격(초). 시트와 무관한 고정값.")]
    public float burstInterval = 0.08f;

    [Tooltip("한 번에 쏘는 반복 횟수. 0이면 시트의 수량(count)을 쓰고, 그것도 0이면 1발.")]
    public int burstCountOverride = 0;

    class State
    {
        public float cooldownTimer;
        public float burstTimer;

        /// <summary>-1 이면 버스트 중이 아니라 쿨타임을 채우는 중.</summary>
        public int shotsRemaining = -1;
    }

    public override bool Evaluate(AugmentInstance instance, float deltaTime)
    {
        var s = instance.GetState<State>(this);
        float cd = instance.Stat.cooldown;

        // 쿨타임 미입력(0)은 매 프레임 발동이 되므로 차단
        if (cd <= 0f) return false;

        // 쿨타임을 다 채워야 버스트가 시작된다
        if (s.shotsRemaining < 0)
        {
            s.cooldownTimer = Mathf.Min(s.cooldownTimer + deltaTime, cd);
            if (s.cooldownTimer < cd) return false;

            int burst = burstCountOverride > 0 ? burstCountOverride : instance.Stat.count;
            s.shotsRemaining = Mathf.Max(1, burst);
            s.burstTimer = burstInterval; // 쿨타임이 찬 즉시 첫 발이 나가도록
        }

        s.burstTimer += deltaTime;
        return s.burstTimer >= burstInterval;
    }

    public override void Consume(AugmentContext ctx)
    {
        var s = ctx.Instance.GetState<State>(this);

        s.burstTimer = 0f;
        s.shotsRemaining--;

        // 버스트 소진 — 여기서부터 진짜 쿨타임이 다시 시작된다
        if (s.shotsRemaining <= 0)
        {
            s.shotsRemaining = -1;
            s.cooldownTimer = 0f;
        }

        // 시전 연출은 부모가 처리한다
        base.Consume(ctx);
    }

    public override float Progress(AugmentInstance instance)
    {
        var s = instance.GetState<State>(this);
        float cd = instance.Stat.cooldown;

        if (cd <= 0f) return 1f;

        // 버스트 중에는 항상 "준비 완료"로 보여준다 — HUD가 매 발마다 깜빡이지 않게
        return s.shotsRemaining >= 0 ? 1f : Mathf.Clamp01(s.cooldownTimer / cd);
    }
}
