/// <summary>
/// 지금 도는 스테이지가 무엇인지 묻는 곳.
///
/// 씬 전환에는 오브젝트를 들고 갈 수 없으므로 static 으로 건넨다 —
/// 스테이지 선택 화면이 <see cref="Choose"/> 로 넣고 씬을 로드하면,
/// 새 씬의 <see cref="StageSetup"/> 이 <see cref="Begin"/> 으로 확정한다.
///
/// <b>순서 규칙</b> — StageSetup 이 <c>Awake</c> 에서 확정하므로,
/// 읽는 쪽은 반드시 <c>Start</c> 이후에 <see cref="Active"/> 를 본다.
/// 유니티는 모든 Awake 를 끝낸 뒤에야 Start 를 돌리기 때문에 이러면 순서가 보장된다.
/// </summary>
public static class StageContext
{
    /// <summary>다음에 플레이할 스테이지. 스테이지 선택이 채운다. 비면 씬 기본값을 쓴다.</summary>
    public static StageData Selected;

    /// <summary>지금 씬에서 실제로 도는 스테이지.</summary>
    public static StageData Active { get; private set; }

    /// <summary>스테이지 선택에서 부른다. 씬을 로드하기 전에.</summary>
    public static void Choose(StageData stage) => Selected = stage;

    /// <summary>
    /// 이 씬에서 돌 스테이지를 확정한다. StageSetup 이 Awake 에서 한 번 부른다.
    /// 고른 것이 없으면 씬에 물려둔 기본값을 쓴다 — 그래야 씬을 바로 재생해도 테스트가 된다.
    /// </summary>
    public static StageData Begin(StageData fallback)
    {
        Active = Selected != null ? Selected : fallback;
        return Active;
    }

    public static void Clear()
    {
        Selected = null;
        Active = null;
    }
}
