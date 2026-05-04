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
        // ─── BGM 플레이어 생성 ───────────────────────────────────────────
        GameObject bgmObject = new GameObject("BGM Player");
        bgmObject.transform.parent = transform;
        bgmPlayer = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;
        bgmPlayer.loop        = true;
        bgmPlayer.volume      = bgmVolume;
        bgmPlayer.clip        = bgmClip;

        // High Pass Filter — Camera.main null 체크
        if (Camera.main != null)
            bgmEffect = Camera.main.GetComponent<AudioHighPassFilter>();

        // ─── SFX 플레이어 생성 (채널 수만큼) ────────────────────────────
        GameObject sfxObject = new GameObject("SFX Player");
        sfxObject.transform.parent = transform;
        sfxPlayers = new AudioSource[channels];

        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            sfxPlayers[i] = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[i].playOnAwake           = false;
            sfxPlayers[i].volume                = sfxVolume;
            sfxPlayers[i].bypassListenerEffects = true;
        }
    }

    public void PlayBgm(bool isPlay)
    {
        if (bgmPlayer == null) return;
        if (isPlay) bgmPlayer.Play();
        else        bgmPlayer.Stop();
    }

    public void EffectBgm(bool isPlay)
    {
        if (bgmEffect == null) return;
        bgmEffect.enabled = isPlay;
    }

    public void PlaySfx(Sfx sfx)
    {
        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            int loopIndex = (i + channelIndex) % channels;

            if (sfxPlayers[loopIndex].isPlaying)
                continue;

            // Hit, Melee는 2개 변형 중 랜덤 선택
            int randomIndex = 0;
            if (sfx == Sfx.Hit || sfx == Sfx.Melee)
                randomIndex = Random.Range(0, 2);

            // sfxClips 배열 범위 체크
            int clipIndex = (int)sfx + randomIndex;
            if (sfxClips == null || clipIndex >= sfxClips.Length || sfxClips[clipIndex] == null)
                break;

            channelIndex = loopIndex;
            sfxPlayers[loopIndex].clip = sfxClips[clipIndex];
            sfxPlayers[loopIndex].Play();
            break;
        }
    }
}
