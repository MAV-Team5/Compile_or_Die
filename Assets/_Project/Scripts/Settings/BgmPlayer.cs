using UnityEngine;

/// <summary>
/// 배경음 재생기. <see cref="SfxPlayer"/> 와 같은 방식 — 씬에 놓지 않고 스스로 하나 만든다.
///
/// <b>씬을 넘어 살아남는다.</b> 부팅 화면에서 로비로 넘어갈 때 같은 곡이면 이어서 재생한다 —
/// 씬마다 AudioSource 를 두면 넘어갈 때마다 음악이 처음부터 다시 시작돼서
/// "화면이 바뀌었다"가 아니라 "게임이 끊겼다"로 들린다.
///
/// 곡을 정하는 것은 씬 몫이다. <see cref="BgmSource"/> 를 씬에 놓고 클립을 물리면 된다.
/// </summary>
public static class BgmPlayer
{
    static AudioSource source;
    static AudioClip current;

    /// <summary>클립에 적힌 고유 볼륨. 여기에 설정값을 곱한다.</summary>
    static float clipVolume = 1f;

    /// <summary>지금 흐르는 곡. 없으면 null.</summary>
    public static AudioClip Current => current;

    /// <summary>
    /// 곡을 튼다. <b>같은 곡이면 아무 일도 하지 않는다</b> — 씬을 넘어가도 안 끊긴다.
    /// </summary>
    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) { Stop(); return; }

        Ensure();

        clipVolume = Mathf.Clamp01(volume);

        if (current == clip && source.isPlaying)
        {
            // 같은 곡인데 볼륨만 다르게 요청했을 수 있다
            ApplyVolume();
            return;
        }

        current = clip;

        source.clip = clip;
        ApplyVolume();
        source.Play();
    }

    public static void Stop()
    {
        if (source == null) return;

        source.Stop();
        source.clip = null;
        current = null;
    }

    static void ApplyVolume()
    {
        if (source == null) return;

        source.volume = clipVolume * SoundSettings.BgmScale;
    }

    static void Ensure()
    {
        if (source != null) return;

        var go = new GameObject("BgmPlayer") { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(go);

        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;

        // 2D 로 못 박는다. 기본값(3D)이면 리스너와의 거리에 따라 소리가 작아진다
        source.spatialBlend = 0f;

        SoundSettings.Changed += ApplyVolume;
    }
}
