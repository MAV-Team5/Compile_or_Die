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

    static readonly int KindCount = System.Enum.GetValues(typeof(StatKind)).Length;

    readonly List<StatModifier> modifiers = new();
    readonly List<Timed> timed = new();

    // 매 프레임 수십 번 조회되므로 합계를 미리 접어둔다.
    // StatKind 를 늘려도 자동으로 따라오게 KindCount 로 잡는다
    readonly float[] addTotal = new float[KindCount];
    readonly float[] percentTotal = new float[KindCount];

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
        for (int i = 0; i < KindCount; i++)
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

    /// <summary>시트 수치에 전역 보정을 얹은 값. 구조체 복사본이라 원본은 안 변한다.</summary>
    public AugmentLevelData Apply(AugmentLevelData raw)
    {
        raw.damage       = Scale(StatKind.Damage,       raw.damage);
        raw.effectDamage = Scale(StatKind.EffectDamage, raw.effectDamage);
        raw.range        = Scale(StatKind.Range,        raw.range);
        raw.effectRange  = Scale(StatKind.EffectRange,  raw.effectRange);
        raw.duration     = Scale(StatKind.Duration,     raw.duration);
        raw.speed        = Scale(StatKind.Speed,        raw.speed);

        // 쿨타임만 반대다. 배율이 오를수록 짧아지고, 아무리 쌓아도 0에 닿지 않는다
        raw.cooldown = Shorten(StatKind.Cooldown, raw.cooldown);

        // 정수 수치는 가산만 받는다. 투사체 1.5개 같은 것이 없기 때문
        raw.count  = Offset(StatKind.Count,  raw.count);
        raw.pierce = Offset(StatKind.Pierce, raw.pierce);
        raw.depth  = Offset(StatKind.Depth,  raw.depth);

        return raw;
    }

    float Scale(StatKind kind, float value)
    {
        int k = (int)kind;
        return (value + addTotal[k]) * (1f + percentTotal[k]);
    }

    float Shorten(StatKind kind, float value)
    {
        int k = (int)kind;

        float speedUp = 1f + percentTotal[k];
        if (speedUp < 0.01f) speedUp = 0.01f;   // 음수 보정이 커도 뒤집히지 않게

        return (value + addTotal[k]) / speedUp;
    }

    int Offset(StatKind kind, int value)
    {
        int result = value + Mathf.RoundToInt(addTotal[(int)kind]);
        return result < 0 ? 0 : result;
    }

    // ── 확인용 ────────────────────────────────────────────

    /// <summary>현재 걸린 보정 요약. 로그·디버그용.</summary>
    public string Describe()
    {
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < KindCount; i++)
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
