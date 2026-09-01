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

    [Tooltip("같은 소리를 다시 내기까지의 최소 간격(초). 0이면 기본값 0.05.\n\n" +
             "＊ 이 효과는 적중한 대상마다 불린다. 광역이 열 명을 맞히면 열 번 불리므로,\n" +
             "  관통이나 광역 증강은 0.12 쯤으로 늘려야 시끄럽지 않다.")]
    [Min(0f)] public float minInterval = 0f;

    public override void Apply(AugmentContext ctx, HitInfo hit)
        => SfxPlayer.Play(clip, volume, minInterval);
}
