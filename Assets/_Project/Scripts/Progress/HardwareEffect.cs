/// <summary>부품이 올린 값이 어디로 가는가.</summary>
public enum HardwareTarget
{
    /// <summary>증강 수치. <see cref="PlayerStats"/> 를 거쳐 보유 증강 전부에 걸린다.</summary>
    Stat,

    /// <summary>경험치 획득량.</summary>
    Exp,

    /// <summary>시야 범위. 증강이 적을 찾는 반경이 아니라 화면 밖 인지 범위다.</summary>
    Vision,

    /// <summary>이동 속도.</summary>
    MoveSpeed,

    /// <summary>크리티컬 확률. <b>아직 판정기가 없다</b> — 주입해도 아무 일도 안 일어난다.</summary>
    Critical,

    /// <summary>런 시작 시 받는 스타트 증강 수. <b>아직 캐릭터 시스템이 없다.</b></summary>
    StartingAugments
}

/// <summary>더할 것인가 곱할 것인가.</summary>
public enum HardwareMode
{
    /// <summary>값을 그대로 더한다. 수량·관통·깊이처럼 정수인 수치는 이쪽뿐이다.</summary>
    Add,

    /// <summary>비율로 올린다. 0.05 가 +5%.</summary>
    Percent
}

/// <summary>
/// 부품 한 레벨이 만드는 변화 하나.
///
/// <b>부품 하나가 여러 개를 가질 수 있다.</b> GPU 는 사거리와 효과범위를 같이 올리는데,
/// 효과를 한 칸으로 묶으면 그런 부품을 표현할 방법이 없어진다.
/// </summary>
[System.Serializable]
public class HardwareEffect
{
    public HardwareTarget target = HardwareTarget.Stat;

    [UnityEngine.Tooltip("target 이 Stat 일 때만 쓴다. 나머지 대상은 올릴 수치가 하나뿐이다.")]
    public StatKind statKind;

    public HardwareMode mode = HardwareMode.Percent;

    [UnityEngine.Tooltip("레벨 1당 오르는 양. Percent 면 0.05 가 +5%.")]
    public float perLevel = 0.05f;

    /// <summary>이 레벨에서 실제로 얹히는 총량.</summary>
    public float AmountAt(int level) => perLevel * level;

    /// <summary>상점 줄에 적을 한 마디. <c>공격력 +15%</c></summary>
    public string Describe(int level)
    {
        float amount = AmountAt(level);

        string body = mode == HardwareMode.Percent
            ? $"{amount * 100f:+0.#;-0.#}%"
            : $"{amount:+0.##;-0.##}";

        return $"{Label} {body}";
    }

    /// <summary>무엇이 오르는가.</summary>
    public string Label => target switch
    {
        HardwareTarget.Stat => StatLabel(statKind),
        HardwareTarget.Exp => "경험치 획득",
        HardwareTarget.Vision => "시야 범위",
        HardwareTarget.MoveSpeed => "이동 속도",
        HardwareTarget.Critical => "크리티컬 확률",
        HardwareTarget.StartingAugments => "스타트 증강",
        _ => target.ToString()
    };

    static string StatLabel(StatKind kind) => kind switch
    {
        StatKind.Damage => "공격력",
        StatKind.EffectDamage => "효과 피해",
        StatKind.Cooldown => "공격 속도",
        StatKind.Range => "사거리",
        StatKind.EffectRange => "효과 범위",
        StatKind.Duration => "지속시간",
        StatKind.Speed => "투사체 속도",
        StatKind.Count => "수량",
        StatKind.Pierce => "관통력",
        StatKind.Depth => "깊이",
        _ => kind.ToString()
    };
}
