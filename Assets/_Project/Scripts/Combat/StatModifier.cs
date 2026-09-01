/// <summary>
/// 증강 수치를 전역으로 밀어 올리는 보정 1개.
/// 하드웨어 업그레이드 · 캐릭터 고유 능력 · 한시적 버프가 전부 이 형태로 들어온다.
/// </summary>
public readonly struct StatModifier
{
    public readonly StatKind Kind;

    /// <summary>먼저 더해지는 값. 정수 수치(수량·관통·깊이)는 이것만 쓴다.</summary>
    public readonly float Add;

    /// <summary>나중에 곱해지는 비율. 0.2 면 +20%.</summary>
    public readonly float Percent;

    /// <summary>누가 준 보정인가. 해제할 때 이걸로 찾는다.</summary>
    public readonly object Source;

    public StatModifier(StatKind kind, object source, float add = 0f, float percent = 0f)
    {
        Kind = kind;
        Source = source;
        Add = add;
        Percent = percent;
    }
}
