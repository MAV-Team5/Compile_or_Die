using UnityEngine;

/// <summary>
/// 수치에 보정을 얹는 식. <b>이 식은 여기 하나뿐이다.</b>
///
/// <code>
/// 실수      최종 = (시트값 + 가산) × (1 + 승산)
/// 쿨타임    최종 = (시트값 + 가산) ÷ (1 + 승산)      ← 짧아지는 방향
/// 정수      최종 = 시트값 + 반올림(가산)             ← 승산은 안 받는다
/// </code>
///
/// <b>왜 빼놨나</b> — 전역 보정(<see cref="PlayerStats"/>)과 내부 증강 보정이 같은 식을 쓴다.
/// 복사해 두면 언젠가 한쪽만 고쳐서, 하드웨어를 샀을 때와 내부 증강을 뽑았을 때의
/// 계산이 달라지는 사고가 난다.
///
/// <b>승산끼리는 더한다.</b> +30%, +20% 는 +50% 지 ×1.56 이 아니다 —
/// 곱하면 지수로 불어나 몇 겹만 쌓여도 손을 못 댄다.
/// </summary>
public static class StatMath
{
    /// <summary>보정 배열의 칸 수. StatKind 를 늘리면 자동으로 따라온다.</summary>
    public static readonly int KindCount = System.Enum.GetValues(typeof(StatKind)).Length;

    /// <summary>보정 한 벌. 가산과 승산을 StatKind 순서로 담는다.</summary>
    public static float[] NewSlots() => new float[KindCount];

    // ── 얹기 ──────────────────────────────────────────────

    /// <summary>시트 수치에 보정을 얹은 값. 구조체 복사본이라 원본은 안 변한다.</summary>
    public static AugmentLevelData Compose(AugmentLevelData raw, float[] add, float[] percent)
    {
        if (add == null || percent == null) return raw;

        raw.damage       = Scale(StatKind.Damage,       raw.damage,       add, percent);
        raw.effectDamage = Scale(StatKind.EffectDamage, raw.effectDamage, add, percent);
        raw.range        = Scale(StatKind.Range,        raw.range,        add, percent);
        raw.effectRange  = Scale(StatKind.EffectRange,  raw.effectRange,  add, percent);
        raw.duration     = Scale(StatKind.Duration,     raw.duration,     add, percent);
        raw.speed        = Scale(StatKind.Speed,        raw.speed,        add, percent);

        // 쿨타임만 반대다. 배율이 오를수록 짧아지고, 아무리 쌓아도 0에 닿지 않는다
        raw.cooldown = Shorten(StatKind.Cooldown, raw.cooldown, add, percent);

        // 정수 수치는 가산만 받는다. 투사체 1.5개 같은 것이 없기 때문
        raw.count  = Offset(StatKind.Count,  raw.count,  add);
        raw.pierce = Offset(StatKind.Pierce, raw.pierce, add);
        raw.depth  = Offset(StatKind.Depth,  raw.depth,  add);

        return raw;
    }

    static float Scale(StatKind kind, float value, float[] add, float[] percent)
    {
        int k = (int)kind;
        return (value + add[k]) * (1f + percent[k]);
    }

    static float Shorten(StatKind kind, float value, float[] add, float[] percent)
    {
        int k = (int)kind;

        float speedUp = 1f + percent[k];
        if (speedUp < 0.01f) speedUp = 0.01f;   // 음수 보정이 커도 뒤집히지 않게

        return (value + add[k]) / speedUp;
    }

    static int Offset(StatKind kind, int value, float[] add)
    {
        int result = value + Mathf.RoundToInt(add[(int)kind]);
        return result < 0 ? 0 : result;
    }

    // ── 모으기 ────────────────────────────────────────────

    /// <summary>
    /// 내부 증강의 레벨 수치를 보정으로 접어 넣는다. <b>그 증강의 시트가 곧 보정치다.</b>
    ///
    /// 정수 칸(수량·관통·깊이)은 <paramref name="asPercent"/> 와 상관없이 항상 가산이다 —
    /// 4발 × 1.5 는 반올림이 생기고 "몇 발 늘었나"가 안 읽힌다.
    /// </summary>
    public static void Accumulate(AugmentLevelData bonus, bool asPercent,
                                  float[] add, float[] percent)
    {
        float[] real = asPercent ? percent : add;

        real[(int)StatKind.Damage]       += bonus.damage;
        real[(int)StatKind.EffectDamage] += bonus.effectDamage;
        real[(int)StatKind.Cooldown]     += bonus.cooldown;
        real[(int)StatKind.Range]        += bonus.range;
        real[(int)StatKind.EffectRange]  += bonus.effectRange;
        real[(int)StatKind.Duration]     += bonus.duration;
        real[(int)StatKind.Speed]        += bonus.speed;

        add[(int)StatKind.Count]  += bonus.count;
        add[(int)StatKind.Pierce] += bonus.pierce;
        add[(int)StatKind.Depth]  += bonus.depth;
    }

    /// <summary>보정을 0으로 되돌린다. 다시 접기 전에 부른다.</summary>
    public static void Clear(float[] slots)
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++) slots[i] = 0f;
    }

    /// <summary>보정이 하나라도 걸려 있나. 없으면 계산을 통째로 건너뛴다.</summary>
    public static bool Any(float[] slots)
    {
        if (slots == null) return false;

        for (int i = 0; i < slots.Length; i++)
            if (!Mathf.Approximately(slots[i], 0f)) return true;

        return false;
    }
}
