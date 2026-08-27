/// <summary>
/// 런 사이에 영구히 강해지는 하드웨어 부품.
///
/// <b>순서를 바꾸거나 중간에 끼워 넣지 말 것.</b> 저장된 업그레이드 레벨이
/// 이 값을 키로 쓰므로, 순서가 바뀌면 지난 세이브의 CPU 레벨이 RAM 에 들어간다.
/// 새 부품은 항상 맨 뒤에 붙인다.
/// </summary>
public enum HardwareKind
{
    /// <summary>
    /// 투사체 속도.
    ///
    /// 기획서에는 공격 속도였지만, 뱀서라이크에서 공격 속도는 곧 쿨타임이라
    /// SSD 와 같은 수치가 된다. 그래서 겹치지 않게 투사체 속도로 옮겼다.
    /// </summary>
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
    Mainboard,

    /// <summary>최대 에러량. 에러율 시스템이 생기기 전까지는 잠가둔다.</summary>
    Cooler
}
