using UnityEngine;

/// <summary>
/// 적중 시 효과음을 낸다. 같은 소리가 한꺼번에 겹치는 것은 SfxPlayer 가 막는다.
/// 적중한 대상마다 따로 울린다.
/// </summary>
[System.Serializable]
[ModuleInfo("적중 시 효과음", "겹침은 SfxPlayer 가 막는다")]
public class SfxEffect : EffectModule
{
    [Required("아무 소리도 나지 않는다")]
    [Tooltip("재생할 효과음.")]
    public AudioClip clip;

    [Range(0f, 1f)] public float volume = 1f;

    public override void Apply(AugmentContext ctx, HitInfo hit)
        => SfxPlayer.Play(clip, volume);
}
