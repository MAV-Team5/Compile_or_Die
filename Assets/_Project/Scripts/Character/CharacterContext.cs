/// <summary>
/// 지금 조종하는 캐릭터가 누구인지 묻는 곳. <see cref="StageContext"/> 와 같은 형태다.
///
/// 씬 전환에는 오브젝트를 들고 갈 수 없으므로 static 으로 건넨다 —
/// 로비의 캐릭터 선택이 <see cref="Choose"/> 로 넣고 Run 씬을 로드하면,
/// 새 씬의 <see cref="PlayerSetup"/> 이 <see cref="Begin"/> 으로 확정한다.
///
/// <b>순서 규칙</b> — PlayerSetup 이 <c>Awake</c> 에서 확정하므로,
/// 읽는 쪽은 반드시 <c>Start</c> 이후에 <see cref="Active"/> 를 본다.
/// </summary>
public static class CharacterContext
{
    /// <summary>다음 런에 쓸 캐릭터. 선택 화면이 채운다. 비면 씬 기본값을 쓴다.</summary>
    public static CharacterData Selected;

    /// <summary>지금 씬에서 실제로 도는 캐릭터.</summary>
    public static CharacterData Active { get; private set; }

    /// <summary>선택 화면에서 부른다. 씬을 로드하기 전에.</summary>
    public static void Choose(CharacterData character) => Selected = character;

    /// <summary>
    /// 이 씬에서 쓸 캐릭터를 확정한다. PlayerSetup 이 Awake 에서 한 번 부른다.
    /// 고른 것이 없으면 씬에 물려둔 기본값을 쓴다 — 그래야 Run 씬을 바로 재생해도 테스트가 된다.
    /// </summary>
    public static CharacterData Begin(CharacterData fallback)
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
