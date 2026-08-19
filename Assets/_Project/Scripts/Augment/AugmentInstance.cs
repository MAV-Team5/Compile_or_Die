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

    /// <summary>
    /// 이 증강이 지금 쓰는 수치. 시트의 레벨 수치에 플레이어 전역 보정을 얹은 값이다.
    /// 모든 모듈이 여기를 거치므로, 하드웨어 업그레이드 하나가 보유 증강 전부에 반영된다.
    /// </summary>
    public AugmentLevelData Stat
    {
        get
        {
            AugmentLevelData raw = Data.levelStats[Mathf.Clamp(Level - 1, 0, MaxLevel - 1)];

            return PlayerStats.Current != null ? PlayerStats.Current.Apply(raw) : raw;
        }
    }

    /// <summary>보정을 뺀 시트 원본. 설명문에 "기본 수치"를 보여줄 때 쓴다.</summary>
    public AugmentLevelData BaseStat
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