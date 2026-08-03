using UnityEngine;

/// <summary>증강 1회 발동분. 모듈들이 돌려가며 채운다.</summary>
public class AugmentContext
{
    public Transform Owner;
    public AugmentInstance Instance;
    public AugmentLevelData Stat;
    public readonly TargetSet Targets = new();

    /// <summary>발동 시작 시 초기화. 주문서를 재사용한다.</summary>
    public void Begin(Transform owner, AugmentInstance instance)
    {
        Owner = owner;
        Instance = instance;
        Stat = instance.Stat;
        Targets.Clear();
    }

    /// <summary>
    /// 모듈별 고유한 상태를 가져오는 메소드
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="module"></param>
    /// <returns></returns>
    public T GetState<T>(AugmentModule module) where T : class, new()
        => Instance.GetState<T>(module);
}