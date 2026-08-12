using UnityEngine;

/// <summary>피해 1건. 파이프라인 각 단계가 Amount를 수정한다.</summary>
public class DamageContext
{
    public GameObject Source;
    public IDamageReceiver Target;

    /// <summary>표식 조회용. 파이프라인이 대상의 MarkerHolder 를 찾을 때 쓴다.</summary>
    public Transform TargetTransform;

    /// <summary>원본 피해량. 끝까지 변하지 않는다.</summary>
    public float BaseAmount;

    /// <summary>현재 피해량. 파이프라인이 계속 갱신한다.</summary>
    public float Amount;

    public bool IsCritical;

    public DamageContext(GameObject source, IDamageReceiver target, float baseAmount,
                         Transform targetTransform = null)
    {
        Source = source;
        Target = target;
        TargetTransform = targetTransform;
        BaseAmount = baseAmount;
        Amount = baseAmount;
        IsCritical = false;
    }
}
