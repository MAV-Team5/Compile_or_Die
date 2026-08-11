using System.Collections.Generic;
using UnityEngine;

/// <summary>증강 1회 발동분. 모듈들이 돌려가며 채운다.</summary>
public class AugmentContext
{
    /// <summary>이 파이프라인의 원점. 최초는 플레이어, 연쇄 단계는 적중한 적.</summary>
    public Transform Owner;

    public AugmentInstance Instance;
    public AugmentLevelData Stat;

    public readonly TargetSet Targets = new();

    /// <summary>이번 발동에서 이미 맞은 대상. 연쇄가 같은 적을 재타격하는 것을 막는다.</summary>
    public HashSet<Transform> Excluded { get; private set; } = new();

    /// <summary>연쇄 깊이. 최초 발동은 0.</summary>
    public int Depth { get; private set; }

    /// <summary>연쇄 단계마다 누적되는 피해 배율.</summary>
    public float DamageMultiplier { get; private set; } = 1f;

    /// <summary>최초 발동용 초기화.</summary>
    public void Begin(Transform owner, AugmentInstance instance)
    {
        Owner = owner;
        Instance = instance;
        Stat = instance.Stat;

        Depth = 0;
        DamageMultiplier = 1f;

        Excluded = new HashSet<Transform>();
        Targets.Clear();
    }

    /// <summary>연쇄 단계용 초기화. 제외 목록은 상위와 공유한다.</summary>
    public void BeginChild(Transform owner, AugmentContext parent, float damageMultiplier)
    {
        Owner = owner;
        Instance = parent.Instance;
        Stat = parent.Stat;

        Depth = parent.Depth + 1;
        DamageMultiplier = damageMultiplier;

        Excluded = parent.Excluded;
        Targets.Clear();
    }

    public T GetState<T>(AugmentModule module) where T : class, new()
        => Instance.GetState<T>(module);
}
