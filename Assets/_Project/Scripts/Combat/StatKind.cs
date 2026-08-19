/// <summary>
/// 전역 배율을 받을 수 있는 증강 수치. 시트의 레벨 수치와 1:1로 대응한다.
///
/// 규칙: 배율은 항상 "좋아지는 방향"이다.
/// Cooldown 만 나눗셈이고(짧아짐) 나머지는 곱셈이다.
/// </summary>
public enum StatKind
{
    /// <summary>피해량. 파워 업그레이드.</summary>
    Damage,

    /// <summary>효과 피해. 탐색 표식·전이 피해.</summary>
    EffectDamage,

    /// <summary>공격 속도. 배율이 클수록 쿨타임이 짧아진다. CPU · SSD 업그레이드.</summary>
    Cooldown,

    /// <summary>사거리. GPU 업그레이드.</summary>
    Range,

    /// <summary>효과 범위. GPU 업그레이드.</summary>
    EffectRange,

    /// <summary>지속시간.</summary>
    Duration,

    /// <summary>투사체 속도.</summary>
    Speed,

    /// <summary>수량. 정수라 가산만 받는다.</summary>
    Count,

    /// <summary>관통력. 정수라 가산만 받는다.</summary>
    Pierce,

    /// <summary>깊이. 정수라 가산만 받는다.</summary>
    Depth
}
