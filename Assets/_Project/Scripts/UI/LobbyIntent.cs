/// <summary>
/// 로비(MainB)에 도착하면 어느 화면을 먼저 열지 알려두는 쪽지.
///
/// 씬을 넘길 때는 오브젝트를 들고 갈 수 없으므로 static 으로 건넨다 —
/// StageContext 가 "다음에 플레이할 스테이지"를 건네는 것과 같은 방식이다.
///
/// <b>왜 필요한가</b> — 결과 화면의 업그레이드 버튼과 나가기 버튼은 둘 다 로비로 가지만
/// 도착해서 볼 화면이 다르다. 이것 때문에 업그레이드 화면만 다른 씬으로 떼어내면
/// 로비와 상점이 갈라져 뒤로 가기·비트 표시를 양쪽에 따로 만들어야 한다.
///
/// 쪽지는 <b>한 번 읽으면 사라진다</b>. 안 그러면 상점을 닫고 나서 다시 로비를 볼 때마다
/// 상점이 계속 다시 열린다.
/// </summary>
public static class LobbyIntent
{
    public enum Screen
    {
        /// <summary>평소대로 로비 메뉴.</summary>
        None,

        /// <summary>하드웨어 업그레이드 상점.</summary>
        Upgrade
    }

    static Screen pending = Screen.None;

    /// <summary>씬을 로드하기 전에 적어둔다.</summary>
    public static void Request(Screen screen) => pending = screen;

    /// <summary>읽으면서 지운다. 로비가 시작할 때 한 번만 부른다.</summary>
    public static Screen Consume()
    {
        Screen screen = pending;
        pending = Screen.None;

        return screen;
    }
}
