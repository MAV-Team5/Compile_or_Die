using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 효과음 재생기. AudioSource 하나를 재사용해서 오브젝트를 만들지 않는다.
/// 같은 클립이 한꺼번에 겹치는 것도 여기서 막는다.
/// </summary>
public static class SfxPlayer
{
    /// <summary>같은 클립을 이 시간 안에 다시 재생하지 않는다. 폭발이 여러 명을 맞힐 때 귀 아픈 것 방지.</summary>
    public const float DefaultInterval = 0.05f;

    static AudioSource source;
    static readonly Dictionary<AudioClip, float> lastPlayed = new();

    /// <param name="minInterval">
    /// 같은 클립을 다시 틀기까지의 최소 간격(초). 0 이하면 <see cref="DefaultInterval"/>.
    ///
    /// 부르는 쪽마다 달라야 한다 — 관통 투사체가 열 명을 스치는 것과
    /// 단발 근접이 한 명을 치는 것은 필요한 간격이 다르다.
    /// </param>
    public static void Play(AudioClip clip, float volume = 1f, float minInterval = 0f)
    {
        if (clip == null) return;

        float gap = minInterval > 0f ? minInterval : DefaultInterval;
        float now = Time.unscaledTime;

        if (lastPlayed.TryGetValue(clip, out float last) && now - last < gap) return;

        lastPlayed[clip] = now;

        Ensure();

        // 전역 효과음 볼륨을 여기서 한 번만 곱한다. 부르는 쪽은 자기 소리의 상대 크기만 알면 된다
        source.PlayOneShot(clip, volume * SoundSettings.SfxScale);
    }

    /// <summary>
    /// 여러 후보 중 하나를 랜덤으로 골라 재생한다.
    ///
    /// <b>같은 소리가 반복되는 것이 효과음이 피곤해지는 제일 큰 원인이다.</b>
    /// 적 사망음처럼 짧은 시간에 수십 번 울리는 것일수록 변형이 필요하다.
    /// </summary>
    public static void PlayAny(AudioClip[] clips, float volume = 1f, float minInterval = 0f)
    {
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];

        // 빈 칸이 섞여 있어도 소리가 사라지지 않게 한 번 더 훑는다
        if (clip == null)
        {
            for (int i = 0; i < clips.Length && clip == null; i++) clip = clips[i];
            if (clip == null) return;
        }

        Play(clip, volume, minInterval);
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
