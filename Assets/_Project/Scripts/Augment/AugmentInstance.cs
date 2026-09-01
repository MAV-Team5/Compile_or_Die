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

    /// <summary>
    /// 내부 증강이 준 보정. 증강을 뽑거나 레벨업할 때만 다시 접는다 —
    /// <see cref="Stat"/> 은 매 프레임 여러 번 읽히므로 그때마다 목록을 훑으면 낭비다.
    /// </summary>
    public readonly float[] BonusAdd = StatMath.NewSlots();
    public readonly float[] BonusPercent = StatMath.NewSlots();

    /// <summary>
    /// 이번에 쓸 3축. 내부 증강이 덮거나 더한 결과이며, 아무도 안 건드렸으면 뿌리 조립 그대로다.
    /// </summary>
    public AugmentBuild Build;

    public AugmentInstance(AugmentData data, int level = 1)
    {
        Data = data;
        Level = level;
        Build = AugmentBuild.Of(data);
    }

    public int MaxLevel => Data.levelStats.Length;

    /// <summary>
    /// 이 증강이 지금 쓰는 수치.
    ///
    /// <code>
    /// 시트 레벨 수치  →  내부 증강 보정  →  플레이어 전역 보정
    /// </code>
    ///
    /// 내부가 먼저다 — 내부 증강은 "이 증강의 스펙" 을 바꾸고,
    /// 전역 보정은 "플레이어가 얼마나 강한가" 라서 그 위에 얹혀야 한다.
    /// </summary>
    public AugmentLevelData Stat
    {
        get
        {
            AugmentLevelData raw = Data.levelStats[Mathf.Clamp(Level - 1, 0, MaxLevel - 1)];

            raw = StatMath.Compose(raw, BonusAdd, BonusPercent);

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