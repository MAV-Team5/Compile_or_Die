using UnityEngine;

/// <summary>적중 시 효과음을 낸다. 같은 소리가 겹치는 것은 SfxPlayer 가 막는다.</summary>
[System.Serializable]
public class SfxEffect : EffectModule
{
    public AudioClip clip;

    [Range(0f, 1f)] public float volume = 1f;

    public override void Apply(AugmentContext ctx, HitInfo hit)
        => SfxPlayer.Play(clip, volume);
}
