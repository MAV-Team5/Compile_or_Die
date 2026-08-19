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

    [Tooltip("켜면 판정 범위만큼 이펙트도 같이 커진다. 프리팹에 ISizedVisual 이 있어야 먹는다.\n" +
             "끄면 범위가 커져도 프리팹에 그린 크기 그대로 — 불꽃·타격감처럼 크기가 의미 없는 연출용.")]
    public bool scaleWithRange = true;

    [Tooltip("함께 낼 효과음.")]
    public AudioClip sfx;

    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Tooltip("켜면 이펙트가 발동한 대상에 붙어 따라다닌다. 레이더 · 휘두르기 · 오라처럼 몸에 붙는 연출용.\n" +
             "폭발이나 착탄처럼 '그 자리에서 터진' 연출은 꺼둘 것 — 켜면 이펙트가 쫓아다닌다.")]
    public bool attachToSource = false;

    public bool IsEmpty => vfx == null && sfx == null;

    /// <summary>
    /// 지정한 좌표에서 연출을 낸다.
    /// 방향은 IDirectionalVisual, 판정 반경은 ISizedVisual 이 붙은 프리팹에만 전달된다.
    /// source 는 attachToSource 가 켜져 있을 때만 쓰인다.
    /// </summary>
    public void PlayAt(Vector2 position, Vector2 direction = default, float radius = 0f,
                       Transform source = null)
    {
        // 반경을 안 넘기면 ISizedVisual 이 안 불려서 프리팹 크기 그대로 나온다
        VfxSpawner.SpawnAt(vfx, position, vfxScale, direction,
                           scaleWithRange ? radius : 0f,
                           attachToSource ? source : null);

        SfxPlayer.Play(sfx, sfxVolume);
    }
}
