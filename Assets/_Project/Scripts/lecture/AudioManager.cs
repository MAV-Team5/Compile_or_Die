using System.Collections;
using UnityEngine;

/// <summary>
/// 오디오 시스템 싱글톤 매니저
/// BGM: 1채널 (반복 재생)
/// SFX: 다채널 (동시 다발적 효과음, 기본 16채널)
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance; // 싱글톤

    [Header("# BGM 설정")]
    public AudioClip bgmClip;           // 배경음악 파일 (Inspector에서 연결)
    public float bgmVolume = 0.2f;      // BGM 볼륨 (0~1)
    AudioSource bgmPlayer;              // BGM 전용 AudioSource
    AudioHighPassFilter bgmEffect;      // 레벨업 중 BGM 뮤트 효과 (Main Camera에 부착)

    [Header("# SFX 설정")]
    public AudioClip[] sfxClips;        // 효과음 배열 (순서 중요! 아래 Sfx 열거형과 일치)
    public float sfxVolume = 0.5f;      // SFX 볼륨 (0~1)
    public int channels = 16;           // 동시 재생 가능한 채널 수
    AudioSource[] sfxPlayers;           // SFX 채널 배열
    int channelIndex;                   // 마지막으로 사용한 채널 인덱스

    /// <summary>
    /// 효과음 열거형. sfxClips 배열 인덱스와 순서 일치 필수
    /// Hit, Melee는 2개씩이므로 다음 항목 인덱스를 명시적으로 지정
    /// </summary>
    public enum Sfx
    {
        Dead,           // [0] 몬스터 사망
        Hit,            // [1] 피격 (Hit_0)
                        // [2] 피격 (Hit_1) — 2개 중 랜덤 재생
        LevelUp = 3,    // [3] 레벨업 (명시적 인덱스 지정)
        Lose,           // [4] 게임 오버
        Melee,          // [5] 근접 공격 (Melee_0)
                        // [6] 근접 공격 (Melee_1) — 2개 중 랜덤 재생
        Range = 7,      // [7] 원거리 발사 (명시적 인덱스 지정)
                        // [8] 원거리 발사 (Range_1)
        Select,         // [8] 아이템 선택 / 게임 시작
        Win             // [9] 승리
    }

    void Awake()
    {
        instance = this;
        Init();
    }

    /// <summary>
    /// BGM 플레이어(1개)와 SFX 플레이어(채널 수만큼) 동적 생성
    /// Inspector에서 설정하지 않고 코드로 생성 → Hierarchy 자동 정리
    /// </summary>
    void Init()
    {
        // ─── BGM 플레이어 생성 ──────────────────────────────────────────
        GameObject bgmObject  = new GameObject("BGM Player");
        bgmObject.transform.parent = transform; // AudioManager 자식으로 등록
        bgmPlayer             = bgmObject.AddComponent<AudioSource>();
        bgmPlayer.playOnAwake = false;  // 자동 재생 끄기 (GameStart 시 수동 재생)
        bgmPlayer.loop        = true;   // 반복 재생
        bgmPlayer.volume      = bgmVolume;
        bgmPlayer.clip        = bgmClip;

        // Main Camera의 AudioHighPassFilter 참조 (레벨업 중 효과용)
        // null 체크: Camera.main이 없는 경우 방어
        if (Camera.main != null)
            bgmEffect = Camera.main.GetComponent<AudioHighPassFilter>();

        // ─── SFX 플레이어 생성 (채널 수만큼) ───────────────────────────
        GameObject sfxObject  = new GameObject("SFX Player");
        sfxObject.transform.parent = transform;
        sfxPlayers = new AudioSource[channels];

        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            sfxPlayers[i]                      = sfxObject.AddComponent<AudioSource>();
            sfxPlayers[i].playOnAwake          = false;
            sfxPlayers[i].volume               = sfxVolume;
            // bypassListenerEffects: High Pass Filter 영향 차단
            // BGM에만 필터 적용하고 SFX에는 적용 안 하기 위해 true로 설정
            sfxPlayers[i].bypassListenerEffects = true;
        }
    }

    /// <summary>
    /// BGM 재생 또는 정지
    /// </summary>
    /// <param name="isPlay">true=재생, false=정지</param>
    public void PlayBgm(bool isPlay)
    {
        if (bgmPlayer == null) return;
        if (isPlay) bgmPlayer.Play();
        else        bgmPlayer.Stop();
    }

    /// <summary>
    /// 레벨업 창 표시 시 BGM에 High Pass Filter 적용
    /// 저음 차단 → 게임이 일시정지된 느낌 연출
    /// </summary>
    /// <param name="isPlay">true=필터 ON, false=필터 OFF</param>
    public void EffectBgm(bool isPlay)
    {
        if (bgmEffect == null) return;
        bgmEffect.enabled = isPlay;
    }

    /// <summary>
    /// 효과음 재생. 쉬고 있는 채널을 찾아 재생
    /// channelIndex 기준으로 순환 탐색 → 재생 완료된 채널 우선 사용
    /// </summary>
    /// <param name="sfx">재생할 효과음 종류</param>
    public void PlaySfx(Sfx sfx)
    {
        for (int i = 0; i < sfxPlayers.Length; i++)
        {
            // channelIndex에서 시작해 순환하는 인덱스 계산
            // 예) 16채널, 마지막 사용=5 → 6,7,...,15,0,1,...,4 순으로 탐색
            int loopIndex = (i + channelIndex) % channels;

            // 이미 재생 중인 채널은 건너뜀 (재생 중인 소리 끊김 방지)
            if (sfxPlayers[loopIndex].isPlaying)
                continue;

            // Hit, Melee는 2개 변형 중 랜덤 선택 (다양한 타격감)
            int randomIndex = 0;
            if (sfx == Sfx.Hit || sfx == Sfx.Melee)
                randomIndex = Random.Range(0, 2); // 0 또는 1

            // 배열 범위 체크 (sfxClips 미연결 시 오류 방지)
            int clipIndex = (int)sfx + randomIndex;
            if (sfxClips == null || clipIndex >= sfxClips.Length
                || sfxClips[clipIndex] == null)
                break;

            // 빈 채널 발견 → 재생
            channelIndex               = loopIndex;
            sfxPlayers[loopIndex].clip = sfxClips[clipIndex];
            sfxPlayers[loopIndex].Play();
            break; // 하나만 재생하고 탈출
        }
    }
}
