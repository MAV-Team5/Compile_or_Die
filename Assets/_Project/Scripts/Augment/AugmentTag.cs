using UnityEngine;

/// <summary>
/// 증강의 기능 태그. 카테고리가 "어떤 개념인가" 라면 태그는 "실제로 무엇을 하는가" 다.
///
/// ★ 숫자는 에셋에 저장되는 값이다 — 바꾸거나 재사용하지 말 것. 새 태그는 빈 번호에 추가한다.
///   카드에 놓이는 순서는 이 숫자가 아니라 <see cref="AugmentTagStyle"/> 표의 순서다.
/// </summary>
public enum AugmentTag
{
    [InspectorName("공격")] Attack = 0,
    [InspectorName("표식")] Mark = 1,
    [InspectorName("제어")] Control = 2,
    [InspectorName("시너지")] Synergy = 3,
    [InspectorName("보조")] Support = 4,

    [InspectorName("단일")] Single = 10,
    [InspectorName("다중")] Multi = 11,

    [InspectorName("투사체")] Projectile = 20,
    [InspectorName("관통")] Pierce = 21,
    [InspectorName("연쇄")] Chain = 22,
    [InspectorName("범위")] Area = 23,
    [InspectorName("폭발")] Explosion = 24,
    [InspectorName("근접")] Melee = 25,
    [InspectorName("연사")] Rapid = 26,
    [InspectorName("유도")] Homing = 27,
    [InspectorName("전이")] Transfer = 28,
    [InspectorName("지속피해")] DamageOverTime = 29,
    [InspectorName("소환")] Summon = 30,
    [InspectorName("넉백")] Knockback = 31,
    [InspectorName("둔화")] Slow = 32,
    [InspectorName("고점")] HighDamage = 33,
    [InspectorName("탐색 연동")] SearchLinked = 34,
}

/// <summary>태그의 표시 이름·색·카드 위 순서. 표의 위쪽일수록 앞에 놓인다.</summary>
public static class AugmentTagStyle
{
    readonly struct Entry
    {
        public readonly AugmentTag Tag;
        public readonly string Label;
        public readonly Color Tint;

        public Entry(AugmentTag tag, string label, string hex)
        {
            Tag = tag;
            Label = label;
            Tint = ColorUtility.TryParseHtmlString(hex, out Color c) ? c : UnityEngine.Color.gray;
        }
    }

    static readonly Entry[] Table =
    {
        new(AugmentTag.Attack,         "공격",     "#FF5C5C"),
        new(AugmentTag.Mark,           "표식",     "#4FC3F7"),
        new(AugmentTag.Control,        "제어",     "#B388FF"),
        new(AugmentTag.Synergy,        "시너지",   "#FFD54F"),
        new(AugmentTag.Support,        "보조",     "#81C784"),

        new(AugmentTag.Single,         "단일",     "#E0E0E0"),
        new(AugmentTag.Multi,          "다중",     "#E0E0E0"),

        new(AugmentTag.Projectile,     "투사체",   "#90A4AE"),
        new(AugmentTag.Pierce,         "관통",     "#FF8A65"),
        new(AugmentTag.Chain,          "연쇄",     "#FFB74D"),
        new(AugmentTag.Area,           "범위",     "#F06292"),
        new(AugmentTag.Explosion,      "폭발",     "#FF7043"),
        new(AugmentTag.Melee,          "근접",     "#E57373"),
        new(AugmentTag.Rapid,          "연사",     "#FFF176"),
        new(AugmentTag.Homing,         "유도",     "#64B5F6"),
        new(AugmentTag.Transfer,       "전이",     "#4DD0E1"),
        new(AugmentTag.DamageOverTime, "지속피해", "#AED581"),
        new(AugmentTag.Summon,         "소환",     "#A1887F"),
        new(AugmentTag.Knockback,      "넉백",     "#9575CD"),
        new(AugmentTag.Slow,           "둔화",     "#7986CB"),
        new(AugmentTag.HighDamage,     "고점",     "#FFD700"),
        new(AugmentTag.SearchLinked,   "탐색 연동", "#4FC3F7"),
    };

    public static string Label(AugmentTag tag) => Find(tag, out Entry e) ? e.Label : tag.ToString();

    public static Color ColorOf(AugmentTag tag) => Find(tag, out Entry e) ? e.Tint : UnityEngine.Color.gray;

    /// <summary>카드 위 순서. 작을수록 앞. 표에 없으면 맨 뒤.</summary>
    public static int Priority(AugmentTag tag)
    {
        for (int i = 0; i < Table.Length; i++)
            if (Table[i].Tag == tag) return i;

        return int.MaxValue;
    }

    static bool Find(AugmentTag tag, out Entry entry)
    {
        for (int i = 0; i < Table.Length; i++)
        {
            if (Table[i].Tag != tag) continue;

            entry = Table[i];
            return true;
        }

        entry = default;
        return false;
    }
}
