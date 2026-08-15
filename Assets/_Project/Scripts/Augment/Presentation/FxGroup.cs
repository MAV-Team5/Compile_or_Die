using UnityEngine;

/// <summary>
/// 인스펙터에서 한 줄로 접히는 묶음 필드에 붙인다.
/// 자주 안 만지는 설정이 화면을 잡아먹지 않게 하는 것이 목적이다.
/// </summary>
public class FoldAttribute : PropertyAttribute
{
    public readonly string Title;

    /// <summary>접힌 줄 오른쪽에 뜨는 한 줄 안내.</summary>
    public readonly string Note;

    public FoldAttribute(string title, string note = null)
    {
        Title = title;
        Note = note;
    }
}

/// <summary>
/// FxGroup 전용 접이 표시. 부착 위치와 채움 여부를 함께 띄운다.
/// 예) [Fx("발사 연출", "발사 원점")]
/// </summary>
public class FxAttribute : FoldAttribute
{
    public FxAttribute(string title, string anchor) : base(title, anchor) { }
}

/// <summary>
/// 이펙트 + 효과음 한 묶음. 인스펙터에서는 한 줄로 접혀 있다가 펼치면 나온다.
/// 전부 비워도 되며, 비면 아무 연출도 나가지 않는다.
/// </summary>
[System.Serializable]
public class FxGroup
{
    [Tooltip("띄울 이펙트 프리팹. 수명은 프리팹이 스스로 관리한다.")]
    public GameObject vfx;

    [Tooltip("이펙트 크기 배수. 1이면 프리팹 그대로.")]
    public float vfxScale = 1f;

    [Tooltip("함께 낼 효과음.")]
    public AudioClip sfx;

    [Range(0f, 1f)] public float sfxVolume = 1f;

    public bool IsEmpty => vfx == null && sfx == null;

    /// <summary>
    /// 지정한 좌표에서 연출을 낸다.
    /// 방향을 주면 IDirectionalVisual 이 붙은 프리팹이 그쪽을 보게 된다.
    /// </summary>
    public void PlayAt(Vector2 position, Vector2 direction = default)
    {
        VfxSpawner.SpawnAt(vfx, position, vfxScale, direction);
        SfxPlayer.Play(sfx, sfxVolume);
    }
}
