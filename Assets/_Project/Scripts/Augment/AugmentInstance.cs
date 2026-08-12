using System.Collections.Generic;
using UnityEngine;

/// <summary>런타임 증강 1개. 레벨과 모듈 상태를 기억한다.</summary>
public class AugmentInstance
{
    public AugmentData Data { get; }
    public int Level { get; private set; }

    readonly Dictionary<AugmentModule, object> states = new();

    /// <summary>
    /// 탐색 표식을 마지막으로 새로 깐 발동 번호.
    /// 한 증강 안에 SearchEffect 가 여럿이어도 발동당 한 번만 해제하려고 증강 단위로 둔다.
    /// </summary>
    public int LastSearchFiringId;

    public AugmentInstance(AugmentData data, int level = 1)
    {
        Data = data;
        Level = level;
    }

    public int MaxLevel => Data.levelStats.Length;

    public AugmentLevelData Stat
        => Data.levelStats[Mathf.Clamp(Level - 1, 0, MaxLevel - 1)];

    public void LevelUp()
    {
        if (Level < MaxLevel) Level++;
    }

    /// <summary>모듈별 상태 보관함. 없으면 만들어서 준다.</summary>
    public T GetState<T>(AugmentModule module) where T : class, new()
    {
        if (!states.TryGetValue(module, out object s))
        {
            s = new T();
            states[module] = s;
        }
        return (T)s;
    }
}