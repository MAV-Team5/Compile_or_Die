using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("# BGM")]
    public AudioClip bgmClip;
    public float bgmVolume = 0.2f;
    AudioSource bgmPlayer;
    AudioHighPassFilter bgmEffect;

    [Header("# SFX")]
    public AudioClip[] sfxClips;
    public float sfxVolume = 0.5f;
    public int channels = 16;
    AudioSource[] sfxPlayers;
    int channelIndex;

    public enum Sfx
    {
        Dead, Hit, LevelUp = 3, Lose, Melee, Range = 7, Select, Win
    }

    void Awake()
    {
        instance = this;
        Init();
    }

    void Init()
    {
        // BGM 플레이어
        GameObject bgmObject = new GameObject("BGM Player");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop   = true;
        bgmPlayer.volume = bgmVolume;
        bgmPlayer.clip   = bgmClip;

        bgmEffect = Camera.main.GetComponent<AudioHighPassFilter>();

        // SFX 플레이어 (채널 수만큼)
        GameObject sfxObject = new GameObject("SFX Player");
        sfxObject.transform.parent = transform;
        sfxPlayers = new AudioSource[channels];

        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            sfxPlayers[i] = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[i].playOnAwake = false;
            sfxPlayers[i].volume = sfxVolume;
            sfxPlayers[i].bypassListenerEffects = true;
        }
    }

    public void PlayBgm(bool isPlay)
    {
        if (isPlay)
            bgmPlayer.Play();
        else
            bgmPlayer.Stop();
    }

    public void EffectBgm(bool isPlay)
    {
        bgmEffect.enabled = isPlay;
    }

    public void PlaySfx(Sfx sfx)
    {
        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            int loopIndex = (i + channelIndex) % channels;

            if (sfxPlayers[loopIndex].isPlaying)
                continue;

            int randomIndex = 0;
            if (sfx == Sfx.Hit || sfx == Sfx.Melee)
                randomIndex = Random.Range(0, 2);

            channelIndex = loopIndex;
            sfxPlayers[loopIndex].clip = sfxClips[(int)sfx + randomIndex];
            sfxPlayers[loopIndex].Play();
            break;
        }
    }
}
