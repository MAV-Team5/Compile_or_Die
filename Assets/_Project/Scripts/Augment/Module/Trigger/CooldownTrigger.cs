using System.Threading;
using Unity.VisualScripting;

[System.Serializable]
public class CooldownTrigger : TriggerModule
{
    public float extraDelay;

    class State { public float timer; }

    public override bool Evaluate(AugmentContext ctx, float deltaTime)
    {
        var s = ctx.GetState<State>(this);
        s.timer += deltaTime;
        if(ctx.Stat.cooldown > s.timer)
        {
            return false;
        }
        else
        {
            s.timer -= ctx.Stat.cooldown;   // 초과분을 다음 사이클로 이월
            return true;
        }
        
    }
}