using UnityEngine;

public class DamageContext
{
    public GameObject Source;
    public IDamageReceiver Target;
    public float BaseAmount;
    public float Amount;
    public bool isCritical;

    public DamageContext(GameObject source, IDamageReceiver target, float baseAmount)
    {
        Source = source;
        Target = target;
        BaseAmount = baseAmount;
        Amount = baseAmount;
        isCritical = false;
    }
}
