/// <summary>
/// 런 사이에 영구히 강해지는 하드웨어 부품.
///
/// <b>순서를 바꾸거나 중간에 끼워 넣지 말 것.</b> 저장된 업그레이드 레벨이
/// 이 값을 키로 쓰므로, 순서가 바뀌면 지난 세이브의 CPU 레벨이 RAM 에 들어간다.
/// 새 부품은 항상 맨 뒤에 붙인다.
/// </summary>
public enum HardwareKind
{
    /// <summary>공격 속도.</summary>
    Cpu,

    /// <summary>경험치 획득량.</summary>
    Ram,

    /// <summary>쿨타임 감소.</summary>
    Ssd,

    /// <summary>공격 범위.</summary>
    Gpu,

    /// <summary>전체 공격력.</summary>
    Power,

    /// <summary>시야 범위.</summary>
    Monitor,

    /// <summary>크리티컬 확률.</summary>
    Mouse,

    /// <summary>이동 속도.</summary>
    Keyboard,

    /// <summary>런 시작 시 스타트 증강 수.</summary>
    Mainboard
}
