using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화면 설정 — 전체화면 · 해상도 · 프레임 제한.
///
/// 2D 픽셀 게임이라 <see cref="QualitySettings"/> 의 품질 단계는 사실상 아무것도 바꾸지 않는다.
/// 실제로 의미가 있는 것만 남겼다 — 특히 <b>다른 PC 에서 돌릴 때</b> 필요한 것들.
///
/// <b>에디터에서는 해상도·전체화면이 안 먹는다.</b> 게임 뷰가 대신 제어하기 때문이다.
/// 빌드해야 확인할 수 있다.
/// </summary>
public static class DisplaySettings
{
    const string FullscreenKey = "CoD.Display.Fullscreen";
    const string WidthKey = "CoD.Display.Width";
    const string HeightKey = "CoD.Display.Height";
    const string FrameKey = "CoD.Display.Frame";

    /// <summary>프레임 제한 선택지. 0은 수직동기화, -1은 무제한.</summary>
    public static readonly int[] FrameOptions = { 0, 60, 120, 144, -1 };

    public static string FrameLabel(int value)
        => value == 0 ? "VSYNC" : value < 0 ? "UNLIMITED" : $"{value} FPS";

    static bool loaded;
    static bool fullscreen = true;
    static int width;
    static int height;
    static int frame;

    // ── 읽고 쓰기 ─────────────────────────────────────────

    public static bool Fullscreen
    {
        get { Ensure(); return fullscreen; }
        set
        {
            Ensure();
            if (fullscreen == value) return;

            fullscreen = value;
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            PlayerPrefs.Save();

            ApplyScreen();
        }
    }

    public static Vector2Int Resolution
    {
        get { Ensure(); return new Vector2Int(width, height); }
    }

    /// <summary>프레임 제한. 0=수직동기화, -1=무제한, 그 외는 목표 FPS.</summary>
    public static int FrameLimit
    {
        get { Ensure(); return frame; }
        set
        {
            Ensure();
            if (frame == value) return;

            frame = value;
            PlayerPrefs.SetInt(FrameKey, value);
            PlayerPrefs.Save();

            ApplyFrame();
        }
    }

    public static void SetResolution(int w, int h)
    {
        Ensure();

        if (w <= 0 || h <= 0) return;
        if (width == w && height == h) return;

        width = w;
        height = h;

        PlayerPrefs.SetInt(WidthKey, w);
        PlayerPrefs.SetInt(HeightKey, h);
        PlayerPrefs.Save();

        ApplyScreen();
    }

    // ── 고를 수 있는 해상도 ───────────────────────────────

    /// <summary>
    /// 이 모니터가 지원하는 해상도. 같은 크기의 주사율 변형은 하나로 합친다 —
    /// 목록에 1920×1080 이 다섯 번 뜨면 고르기가 어렵다.
    /// </summary>
    public static List<Vector2Int> Available(int minWidth = 1280)
    {
        var list = new List<Vector2Int>();

        // 이 클래스에도 Resolution 이라는 프로퍼티가 있다. 타입 쪽을 또렷하게 적어둔다
        UnityEngine.Resolution[] all = Screen.resolutions;

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].width < minWidth) continue;

            var size = new Vector2Int(all[i].width, all[i].height);
            if (!list.Contains(size)) list.Add(size);
        }

        // 목록이 비는 경우가 있다(일부 리눅스·에디터). 지금 크기라도 넣어준다
        if (list.Count == 0) list.Add(new Vector2Int(Screen.width, Screen.height));

        list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

        return list;
    }

    // ── 적용 ──────────────────────────────────────────────

    /// <summary>게임이 시작될 때 저장된 값을 적용한다. 설정 화면을 안 열어도 지난 설정으로 뜬다.</summary>
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

        fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        width = PlayerPrefs.GetInt(WidthKey, Screen.width);
        height = PlayerPrefs.GetInt(HeightKey, Screen.height);
        frame = PlayerPrefs.GetInt(FrameKey, 0);

        ApplyScreen();
        ApplyFrame();
    }

    static void ApplyScreen()
    {
        if (width <= 0 || height <= 0) return;

        Screen.SetResolution(width, height,
                             fullscreen ? FullScreenMode.FullScreenWindow
                                        : FullScreenMode.Windowed);
    }

    /// <summary>
    /// 수직동기화가 켜져 있으면 <see cref="Application.targetFrameRate"/> 는 무시된다.
    /// 그래서 둘을 같이 정해야 한다 — 한쪽만 바꾸면 왜 안 먹는지 알 수 없다.
    /// </summary>
    static void ApplyFrame()
    {
        if (frame == 0)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            return;
        }

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = frame < 0 ? -1 : frame;
    }
}
