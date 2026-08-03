using System.Collections.Generic;
using UnityEngine;

/// <summary>런타임 증강 1개. 레벨과 모듈 상태를 기억한다.</summary>
public class AugmentInstance
{
    public AugmentData Data { get; }
    public int Level { get; private set; }

    readonly Dictionary<AugmentModule, object> states = new();

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