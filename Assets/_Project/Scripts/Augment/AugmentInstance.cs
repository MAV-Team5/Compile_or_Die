using System.Collections.Generic;
using UnityEngine;

/// <summary>런타임 증강 1개. 레벨과 모듈 상태를 기억한다.</summary>
public class AugmentInstance
{
    public AugmentData Data { get; }
    public int Level { get; private set; }

    readonly Dictionary<AugmentModule, object> states = new();

    /// <summary>
    /// 모듈이 아니라 <b>증강 개체 단위</b>로 사는 상태. 축이 서로 나눠 쓴다.
    ///
    /// <see cref="GetState{T}"/> 는 모듈을 키로 쓰기 때문에, 트리거와 타겟팅이
    /// 같은 데이터를 봐야 하는 증강(스택의 프레임 목록, 큐의 대기열)에서는 못 쓴다.
    /// 그렇다고 자료구조마다 static 레지스트리를 하나씩 만들면 전역이 계속 늘어난다.
    /// </summary>
    readonly Dictionary<System.Type, object> shared = new();

    /// <summary>
    /// 탐색 표식을 마지막으로 새로 깐 발동 번호.
    /// 한 증강 안에 SearchEffect 가 여럿이어도 발동당 한 번만 해제하려고 증강 단위로 둔다.
    /// </summary>
    public int LastSearchFiringId;

    /// <summary>
    /// 이 증강이 도는 원점. 보통 플레이어지만 소환물이면 그 소환물이다.
    /// <see cref="AugmentRunner.Setup"/> 이 채운다.
    ///
    /// <b>왜 필요한가</b> — 트리거는 <c>Evaluate(instance, dt)</c> 만 받아서
    /// 자기가 어디 서 있는지 모른다. <c>while</c> 조건처럼 "내 주변에 적이 있나" 를
    /// 물어야 하는 트리거는 원점이 없으면 판정 자체를 못 한다.
    /// </summary>
    public Transform Owner;

    /// <summary>
    /// 트리거를 갈아끼운 내부 증강. 안 갈아끼웠으면 null.
    /// <see cref="AugmentManager"/> 가 조립을 다시 접을 때 채운다.
    /// </summary>
    public AugmentInstance TriggerSource;

    /// <summary>
    /// 트리거가 읽어야 할 수치.
    ///
    /// <b>갈아끼운 트리거는 자기 증강의 시트를 본다.</b> 도는 것은 뿌리의 러너지만,
    /// <c>Iteration:while</c> 의 while 반경처럼 <b>그 내부 증강이 들고 온 값</b>은
    /// 뿌리가 아니라 자기 시트에서 읽어야 레벨을 탄다.
    ///
    /// 뿌리의 트리거 그대로면 자기 자신이라 아무것도 안 바뀐다.
    /// </summary>
    public AugmentLevelData TriggerStat => TriggerSource != null ? TriggerSource.Stat : Stat;

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

    /// <summary>
    /// 증강 개체 단위 상태를 가져온다. 없으면 만든다.
    /// 트리거와 타겟팅이 같은 목록을 봐야 할 때 쓴다.
    /// </summary>
    public T GetShared<T>() where T : class, new()
    {
        if (!shared.TryGetValue(typeof(T), out object s))
        {
            s = new T();
            shared[typeof(T)] = s;
        }
        return (T)s;
    }

    /// <summary>
    /// 이미 만들어져 있을 때만 가져온다. <b>없으면 만들지 않는다.</b>
    ///
    /// 밖에서 "이 증강이 스택을 갖고 있나" 를 물을 때 <see cref="GetShared{T}"/> 를 쓰면
    /// 묻는 것만으로 모든 증강에 빈 스택이 생긴다 — 그걸 막으려고 따로 둔다.
    /// </summary>
    public bool TryGetShared<T>(out T value) where T : class
    {
        if (shared.TryGetValue(typeof(T), out object s))
        {
            value = (T)s;
            return true;
        }

        value = null;
        return false;
    }
}