using UnityEngine;

/// <summary>
/// 전역 볼륨. 0~1 세 칸이 전부다.
///
/// <b>왜 AudioMixer 를 안 쓰나</b> — 믹서의 값어치는 여러 AudioSource 를 그룹으로 묶어
/// 한꺼번에 제어하는 데 있는데, 이 프로젝트에는 재생기가 둘뿐이다(효과음·배경음).
/// 묶을 것이 없는 상태에서 믹서를 끼우면 모든 소스에 그룹을 물리는 수고와
/// dB 변환이라는 함정만 늘어난다.
///
/// 나중에 덕킹이나 이펙트가 필요해지면 <b>이 파일만</b> 믹서에 값을 쓰도록 바꾸면 된다 —
/// 슬라이더도 재생기도 그대로다.
///
/// <b>Master 는 <see cref="AudioListener.volume"/> 로 한 번에 건다.</b>
/// 나머지 둘은 각 재생기가 자기 볼륨에 곱한다.
/// </summary>
public static class SoundSettings
{
    const string MasterKey = "CoD.Volume.Master";
    const string BgmKey = "CoD.Volume.Bgm";
    const string SfxKey = "CoD.Volume.Sfx";

    static float master = 1f;
    static float bgm = 0.7f;
    static float sfx = 1f;

    static bool loaded;

    /// <summary>볼륨이 바뀌었다. 재생기들이 자기 값을 다시 계산하려고 듣는다.</summary>
    public static event System.Action Changed;

    public static float Master
    {
        get { Ensure(); return master; }
        set => Set(ref master, value, MasterKey);
    }

    public static float Bgm
    {
        get { Ensure(); return bgm; }
        set => Set(ref bgm, value, BgmKey);
    }

    public static float Sfx
    {
        get { Ensure(); return sfx; }
        set => Set(ref sfx, value, SfxKey);
    }

    /// <summary>효과음이 실제로 낼 크기. 재생기가 자기 볼륨에 곱한다.</summary>
    public static float SfxScale => Sfx;

    /// <summary>배경음이 실제로 낼 크기.</summary>
    public static float BgmScale => Bgm;

    static void Set(ref float field, float value, string key)
    {
        Ensure();

        float clamped = Mathf.Clamp01(value);
        if (Mathf.Approximately(field, clamped)) return;

        field = clamped;

        PlayerPrefs.SetFloat(key, clamped);
        PlayerPrefs.Save();

        Apply();
        Changed?.Invoke();
    }

    /// <summary>
    /// 게임이 시작될 때 저장된 값을 적용한다.
    /// 설정 화면을 한 번도 안 열어도 지난번 볼륨으로 시작해야 한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        loaded = false;
        Ensure();
    }

    static void Ensure()
    {
        if (loaded) return;
        loaded = true;

        master = PlayerPrefs.GetFloat(MasterKey, master);
        bgm = PlayerPrefs.GetFloat(BgmKey, bgm);
        sfx = PlayerPrefs.GetFloat(SfxKey, sfx);

        Apply();
    }

    static void Apply() => AudioListener.volume = master;
}
