/// <summary>
/// 런 사이에 영구히 강해지는 하드웨어 부품.
///
/// <b>순서를 바꾸거나 중간에 끼워 넣지 말 것.</b> 저장된 업그레이드 레벨이
/// 이 값을 키로 쓰므로, 순서가 바뀌면 지난 세이브의 CPU 레벨이 RAM 에 들어간다.
/// 새 부품은 항상 맨 뒤에 붙인다.
/// </summary>
public enum HardwareKind
{
    /// <summary>쿨타임 감소 — 연산 처리.</summary>
    Cpu,

    /// <summary>
    /// 투사체 수 증가 — 동시 처리 데이터 양.
    ///
    /// <b>정수 수치라 반드시 가산으로 넣는다.</b> StatMath 는 수량·관통·깊이에
    /// 승산을 아예 적용하지 않아서, 비율로 넣으면 경고도 없이 아무 일이 안 일어난다.
    /// </summary>
    Ram,

    /// <summary>경험치 획득 범위 — 데이터 접근 속도.</summary>
    Ssd,

    /// <summary>공격(효과) 범위 — 그래픽 처리.</summary>
    Gpu,

    /// <summary>전체 공격력 — 전력 공급.</summary>
    Power,

    /// <summary>시야 범위 — 화면 표시.</summary>
    Monitor,

    /// <summary>사정거리 — 입력 조준.</summary>
    Mouse,

    /// <summary>이동 속도 — 입력 반응.</summary>
    Keyboard,

    /// <summary>런 시작 시 증강 선택 횟수 — 부품 연결/확장.</summary>
    Mainboard,

    /// <summary>최대 체력 — 발열 안정성.</summary>
    Cooler
}
