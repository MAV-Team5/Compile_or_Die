using UnityEngine;
[System.Serializable]
public class CooldownTrigger : TriggerModule
{
    class State { public float timer; }

    public override bool Evaluate(AugmentInstance instance, float deltaTime)
    {
        var s = instance.GetState<State>(this);
        float cd = instance.Stat.cooldown;

        // 쿨타임 미입력(0)은 매 프레임 발동이 되므로 차단
        if (cd <= 0f) return false;

        s.timer = Mathf.Min(s.timer + deltaTime, cd);
        return s.timer >= cd;
    }

    public override void Consume(AugmentInstance instance)
        => instance.GetState<State>(this).timer = 0f;

    public override float Progress(AugmentInstance instance)
    {
        float cd = instance.Stat.cooldown;
        if (cd <= 0f) return 1f;

        return Mathf.Clamp01(instance.GetState<State>(this).timer / cd);
    }
}