using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 효과음 재생기. AudioSource 하나를 재사용해서 오브젝트를 만들지 않는다.
/// 같은 클립이 한꺼번에 겹치는 것도 여기서 막는다.
/// </summary>
public static class SfxPlayer
{
    /// <summary>같은 클립을 이 시간 안에 다시 재생하지 않는다. 폭발이 여러 명을 맞힐 때 귀 아픈 것 방지.</summary>
    const float RetriggerDelay = 0.05f;

    static AudioSource source;
    static readonly Dictionary<AudioClip, float> lastPlayed = new();

    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        float now = Time.unscaledTime;

        if (lastPlayed.TryGetValue(clip, out float last) && now - last < RetriggerDelay)
            return;

        lastPlayed[clip] = now;

        Ensure();

        // 전역 효과음 볼륨을 여기서 한 번만 곱한다. 부르는 쪽은 자기 소리의 상대 크기만 알면 된다
        source.PlayOneShot(clip, volume * SoundSettings.SfxScale);
    }

    static void Ensure()
    {
        if (source != null) return;

        var go = new GameObject("SfxPlayer") { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(go);

        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }
}
