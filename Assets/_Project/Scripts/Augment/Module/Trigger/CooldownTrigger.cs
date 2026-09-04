using UnityEngine;

/// <summary>일정 주기마다 발동. 대부분의 증강이 쓴다.</summary>
[System.Serializable]
[ModuleInfo("일정 주기마다 발동", "쿨타임은 레벨 수치를 따른다")]
public class CooldownTrigger : TriggerModule
{
    class State
    {
        public float timer;

        /// <summary>첫 판정을 지났는가. 상태 주머니는 증강을 얻을 때 처음 만들어진다.</summary>
        public bool started;
    }

    public override bool Evaluate(AugmentInstance instance, float deltaTime)
    {
        var s = instance.GetState<State>(this);
        float cd = instance.Stat.cooldown;

        // 쿨타임 미입력(0)은 매 프레임 발동이 되므로 차단
        if (cd <= 0f) return false;

        // 얻자마자 첫 쿨타임을 건너뛴다. 고른 순간 화면에서 뭔가 일어나야
        // "이걸 골랐다" 가 전달된다
        if (!s.started)
        {
            s.started = true;

            if (fireOnAcquire)
            {
                s.timer = cd;
                return true;
            }
        }

        s.timer = Mathf.Min(s.timer + deltaTime, cd);
        return s.timer >= cd;
    }

    public override void Consume(AugmentContext ctx)
    {
        ctx.Instance.GetState<State>(this).timer = 0f;

        // 시전 연출은 부모가 처리한다
        base.Consume(ctx);
    }

    public override float Progress(AugmentInstance instance)
    {
        float cd = instance.Stat.cooldown;
        if (cd <= 0f) return 1f;

        return Mathf.Clamp01(instance.GetState<State>(this).timer / cd);
    }
}
