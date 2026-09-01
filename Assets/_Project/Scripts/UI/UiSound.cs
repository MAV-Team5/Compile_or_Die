using UnityEngine;

/// <summary>
/// UI 효과음을 내는 통로. 부르는 쪽은 "무슨 일이 일어났는지"만 말한다.
///
/// <code>UiSound.Play(UiCue.Hover);</code>
///
/// 어떤 클립인지는 <see cref="UiSoundBank"/> 가, 실제 재생은 기존 <see cref="SfxPlayer"/> 가 맡는다 —
/// 재생기를 새로 만들지 않는 이유는 그쪽이 이미 AudioSource 하나를 재사용하고
/// 같은 클립이 겹치는 것도 막고 있기 때문이다.
/// </summary>
public static class UiSound
{
    public static void Play(UiCue cue)
    {
        UiSoundBank bank = UiSoundBank.Current;
        if (bank == null) return;

        if (!bank.TryPick(cue, out AudioClip clip, out float volume)) return;

        SfxPlayer.Play(clip, volume);
    }
}
