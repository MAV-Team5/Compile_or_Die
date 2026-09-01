/// <summary>
/// UI 가 내는 소리의 종류. <see cref="UiSoundBank"/> 가 여기에 클립을 물린다.
///
/// <b>파일 이름이 아니라 상황으로 나눈다.</b> "click_003 을 재생" 이라고 쓰면
/// 나중에 소리를 갈아끼울 때 코드를 뒤져야 한다. "버튼을 눌렀다" 라고 쓰면
/// 무슨 소리를 낼지는 뱅크 에셋만 고치면 된다.
///
/// 뒤에 늘려도 된다 — 뱅크는 이름이 아니라 목록으로 찾으므로 순서에 의존하지 않는다.
/// </summary>
public enum UiCue
{
    /// <summary>커서가 올라갔거나 키보드 포커스가 옮겨왔다. 둘은 같은 사건으로 친다.</summary>
    Hover,

    /// <summary>버튼을 눌렀다. 마우스 클릭과 키보드 Submit 둘 다.</summary>
    Click,

    /// <summary>취소하거나 뒤로 갔다.</summary>
    Back,

    /// <summary>패널이 열렸다.</summary>
    Open,

    /// <summary>패널이 닫혔다.</summary>
    Close,

    /// <summary>못 누르는 것을 눌렀다. 자원이 모자라거나 잠긴 것.</summary>
    Denied,

    /// <summary>증강 선택 화면이 떴다.</summary>
    LevelUp,

    /// <summary>증강 카드 한 장이 나타났다. 여러 장이면 차례로 울린다.</summary>
    CardAppear,

    /// <summary>증강 카드 위에 커서나 포커스가 왔다. 버튼 호버와 다른 소리를 쓴다 —
    /// 카드는 메뉴가 아니라 이 게임의 알맹이라 같은 소리면 무게가 안 실린다.</summary>
    CardHover,

    /// <summary>증강을 골랐다.</summary>
    CardPick,

    /// <summary>리롤했다.</summary>
    Reroll,

    /// <summary>슬라이더를 끌거나 값이 한 칸 움직였다. 짧고 작아야 한다.</summary>
    Tick
}
