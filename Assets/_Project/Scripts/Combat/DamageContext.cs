using System.Collections.Generic;
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

    /// <summary>어느 증강이 때렸나. 피해 숫자 색을 분류로 고를 때 쓴다.</summary>
    public AugmentInstance SourceAugment;

    /// <summary>효과가 직접 지정한 숫자 스타일. 비어 있으면 분류 색을 따른다.</summary>
    public DamageTextStyle? StyleOverride;

    /// <summary>
    /// 간선을 타고 몇 번 더 번질 수 있나. 0이면 최초 피해라 간선이 정한 값을 쓴다.
    /// 전이될 때마다 하나씩 줄어 무한 확산을 막는다.
    /// </summary>
    public int LinkHops;

    /// <summary>
    /// 이번 전이에서 이미 거쳐간 노드. 되짚기를 막는다.
    /// 최초 피해에서는 비어 있고, LinkHolder 가 만들어 물려준다.
    /// </summary>
    public HashSet<Transform> LinkVisited;

    /// <summary>
    /// 하드웨어(파워) 배율을 이미 먹였는가.
    ///
    /// 전이 피해는 이미 배율이 곱해진 값을 기본값으로 물려받는데,
    /// 그것이 파이프라인을 다시 통과하므로 표시가 없으면 홉마다 배율이 거듭 곱해진다.
    /// </summary>
    public bool HardwareApplied;

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
