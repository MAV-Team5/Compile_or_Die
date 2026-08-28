using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 전역 능력치. 하드웨어 업그레이드와 한시적 버프가 여기 모인다.
///
/// 모든 증강이 AugmentInstance.Stat 을 거쳐 수치를 읽으므로,
/// 여기에 보정을 하나 넣으면 보유한 증강 전부가 한꺼번에 영향을 받는다.
///
///     최종 수치 = (시트 레벨 수치 + 가산) × (1 + 승산)
///     쿨타임만  = (시트 쿨타임 + 가산) ÷ (1 + 승산)   ← 짧아지는 방향
/// </summary>
public class PlayerStats : MonoBehaviour
{
    /// <summary>씬에 하나. 증강은 순수 C# 객체라 이 통로로 찾는다.</summary>
    public static PlayerStats Current { get; private set; }

    /// <summary>하드웨어 업그레이드가 붙기 전까지 인스펙터로 시험해 보는 용도.</summary>
    [System.Serializable]
    public class StartingBonus
    {
        public StatKind kind;

        [Tooltip("먼저 더할 값. 정수 수치(수량·관통·깊이)는 이것만 먹는다.")]
        public float add;

        [Tooltip("나중에 곱할 비율. 0.2 면 +20%. 쿨타임은 이만큼 짧아진다.")]
        public float percent;
    }

    [Tooltip("런 시작부터 걸려 있을 보정. 하드웨어 업그레이드가 붙으면 이 자리를 대신한다.")]
    [SerializeField] List<StartingBonus> startingBonuses = new();

    readonly List<StatModifier> modifiers = new();
    readonly List<Timed> timed = new();

    // 매 프레임 수십 번 조회되므로 합계를 미리 접어둔다.
    // StatKind 를 늘려도 자동으로 따라오게 StatMath 가 칸 수를 정한다
    readonly float[] addTotal = StatMath.NewSlots();
    readonly float[] percentTotal = StatMath.NewSlots();

    struct Timed
    {
        public object Source;
        public float ExpireAt;
    }

    void Awake()
    {
        Current = this;

        for (int i = 0; i < startingBonuses.Count; i++)
        {
            StartingBonus bonus = startingBonuses[i];
            modifiers.Add(new StatModifier(bonus.kind, this, bonus.add, bonus.percent));
        }

        Rebuild();
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    void Update()
    {
        if (timed.Count == 0) return;

        bool expired = false;

        for (int i = timed.Count - 1; i >= 0; i--)
        {
            if (Time.time < timed[i].ExpireAt) continue;

            RemoveInternal(timed[i].Source);
            timed.RemoveAt(i);
            expired = true;
        }

        if (expired) Rebuild();
    }

    // ── 보정 넣고 빼기 ─────────────────────────────────────

    /// <summary>보정을 건다. 같은 Source 로 여러 종류를 걸 수 있다.</summary>
    public void Add(StatModifier modifier)
    {
        modifiers.Add(modifier);
        Rebuild();
    }

    /// <summary>지정 시간 뒤 저절로 풀리는 보정. 같은 Source 로 다시 걸면 시간이 갱신된다.</summary>
    public void AddTimed(StatModifier modifier, float duration)
    {
        Remove(modifier.Source);

        modifiers.Add(modifier);
        timed.Add(new Timed { Source = modifier.Source, ExpireAt = Time.time + duration });

        Rebuild();
    }

    /// <summary>이 Source 가 건 보정을 전부 푼다.</summary>
    public void Remove(object source)
    {
        RemoveInternal(source);
        RemoveTimed(source);
        Rebuild();
    }

    public void Clear()
    {
        modifiers.Clear();
        timed.Clear();
        Rebuild();
    }

    void RemoveInternal(object source)
    {
        for (int i = modifiers.Count - 1; i >= 0; i--)
            if (modifiers[i].Source == source) modifiers.RemoveAt(i);
    }

    void RemoveTimed(object source)
    {
        for (int i = timed.Count - 1; i >= 0; i--)
            if (timed[i].Source == source) timed.RemoveAt(i);
    }

    void Rebuild()
    {
        for (int i = 0; i < StatMath.KindCount; i++)
        {
            addTotal[i] = 0f;
            percentTotal[i] = 0f;
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            int k = (int)modifiers[i].Kind;

            addTotal[k] += modifiers[i].Add;
            percentTotal[k] += modifiers[i].Percent;
        }
    }

    // ── 적용 ──────────────────────────────────────────────

    /// <summary>
    /// 시트 수치에 전역 보정을 얹은 값. 구조체 복사본이라 원본은 안 변한다.
    ///
    /// 식은 <see cref="StatMath"/> 가 소유한다 — 내부 증강 보정도 같은 식을 쓰기 때문에,
    /// 여기에 복사해 두면 언젠가 한쪽만 고쳐서 계산이 갈라진다.
    /// </summary>
    public AugmentLevelData Apply(AugmentLevelData raw)
        => StatMath.Compose(raw, addTotal, percentTotal);

    // ── 조회 ──────────────────────────────────────────────

    /// <summary>이 수치에 걸린 가산 합계. 없으면 0.</summary>
    public float AddOf(StatKind kind) => addTotal[(int)kind];

    /// <summary>이 수치에 걸린 승산 합계. 0.2 면 +20%. 없으면 0.</summary>
    public float PercentOf(StatKind kind) => percentTotal[(int)kind];

    /// <summary>이 수치에 걸린 보정이 하나라도 있는가. 표에서 빈 줄을 걸러낼 때 쓴다.</summary>
    public bool HasBonus(StatKind kind)
        => !Mathf.Approximately(addTotal[(int)kind], 0f)
        || !Mathf.Approximately(percentTotal[(int)kind], 0f);

    // ── 확인용 ────────────────────────────────────────────

    /// <summary>현재 걸린 보정 요약. 로그·디버그용.</summary>
    public string Describe()
    {
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < StatMath.KindCount; i++)
        {
            bool noAdd = Mathf.Approximately(addTotal[i], 0f);
            bool noPercent = Mathf.Approximately(percentTotal[i], 0f);

            if (noAdd && noPercent) continue;

            sb.Append((StatKind)i).Append(' ');

            if (!noAdd) sb.Append($"+{addTotal[i]:0.##} ");
            if (!noPercent) sb.Append($"{percentTotal[i]:+0%;-0%} ");
        }

        return sb.Length > 0 ? sb.ToString().TrimEnd() : "보정 없음";
    }
}
