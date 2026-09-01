using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 화면의 배선. 씬에 놓은 슬라이더·토글·드롭다운을 실제 설정에 물린다.
///
/// <b>이 컴포넌트는 값을 갖고 있지 않다.</b> 값은 전부 <see cref="SoundSettings"/> ·
/// <see cref="DisplaySettings"/> 에 있고 PlayerPrefs 에 저장된다.
/// 여기는 위젯과 그 사이를 잇기만 한다 — 그래서 이 오브젝트를 다른 씬에 복제해도
/// 설정이 갈라지지 않는다.
///
/// 물리지 않은 칸은 조용히 넘어간다. 화면에 넣고 싶은 것만 물리면 된다.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("소리")]
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;

    [Tooltip("슬라이더 옆에 퍼센트를 띄우고 싶을 때만. 비워도 된다.")]
    [SerializeField] TMP_Text masterValue;
    [SerializeField] TMP_Text bgmValue;
    [SerializeField] TMP_Text sfxValue;

    [Header("화면")]
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] TMP_Dropdown resolutionDropdown;
    [SerializeField] TMP_Dropdown frameDropdown;

    /// <summary>드롭다운 순번 ↔ 실제 해상도. 목록은 모니터마다 다르므로 매번 새로 만든다.</summary>
    readonly List<Vector2Int> resolutions = new();

    /// <summary>
    /// 위젯에 값을 넣는 동안인가.
    ///
    /// <c>slider.value = x</c> 는 <c>onValueChanged</c> 를 부른다. 막지 않으면
    /// "설정을 읽어서 화면에 표시" 가 곧바로 "화면 값을 설정에 저장" 으로 되돌아온다.
    /// 값이 같으면 무해하지만, 목록이 아직 안 채워진 드롭다운에서는 0번으로 덮어쓴다.
    /// </summary>
    bool filling;

    void OnEnable()
    {
        Bind();
        Fill();
    }

    // ── 배선 ──────────────────────────────────────────────

    // 리스너를 먼저 비우고 다시 단다. 패널이 여러 번 열려도 두 번 걸리지 않는다
    void Bind()
    {
        Hook(masterSlider, v => SoundSettings.Master = v);
        Hook(bgmSlider, v => SoundSettings.Bgm = v);
        Hook(sfxSlider, v => SoundSettings.Sfx = v);

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.AddListener(OnFullscreen);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(OnResolution);
        }

        if (frameDropdown != null)
        {
            frameDropdown.onValueChanged.RemoveAllListeners();
            frameDropdown.onValueChanged.AddListener(OnFrame);
        }
    }

    void Hook(Slider slider, System.Action<float> apply)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(v =>
        {
            if (filling) return;

            apply(v);
            ShowValues();
        });
    }

    // ── 화면에 채우기 ─────────────────────────────────────

    void Fill()
    {
        filling = true;

        // WithoutNotify 로 넣는다. 그냥 대입하면 onValueChanged 가 울려서
        // 패널을 여는 것만으로 슬라이더 틱 소리가 우수수 난다
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(SoundSettings.Master);
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(SoundSettings.Bgm);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(SoundSettings.Sfx);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(DisplaySettings.Fullscreen);

        FillResolutions();
        FillFrames();

        filling = false;

        ShowValues();
    }

    void FillResolutions()
    {
        if (resolutionDropdown == null) return;

        resolutions.Clear();
        resolutions.AddRange(DisplaySettings.Available());

        var labels = new List<string>(resolutions.Count);
        int current = 0;

        Vector2Int now = DisplaySettings.Resolution;

        for (int i = 0; i < resolutions.Count; i++)
        {
            labels.Add($"{resolutions[i].x} x {resolutions[i].y}");

            if (resolutions[i] == now) current = i;
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(labels);
        resolutionDropdown.SetValueWithoutNotify(current);
        resolutionDropdown.RefreshShownValue();
    }

    void FillFrames()
    {
        if (frameDropdown == null) return;

        int[] options = DisplaySettings.FrameOptions;

        var labels = new List<string>(options.Length);
        int current = 0;

        for (int i = 0; i < options.Length; i++)
        {
            labels.Add(DisplaySettings.FrameLabel(options[i]));

            if (options[i] == DisplaySettings.FrameLimit) current = i;
        }

        frameDropdown.ClearOptions();
        frameDropdown.AddOptions(labels);
        frameDropdown.SetValueWithoutNotify(current);
        frameDropdown.RefreshShownValue();
    }

    void ShowValues()
    {
        Percent(masterValue, SoundSettings.Master);
        Percent(bgmValue, SoundSettings.Bgm);
        Percent(sfxValue, SoundSettings.Sfx);
    }

    static void Percent(TMP_Text text, float value)
    {
        if (text != null) text.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    // ── 위젯이 부르는 것 ──────────────────────────────────

    void OnFullscreen(bool on)
    {
        if (filling) return;

        DisplaySettings.Fullscreen = on;

        // 전체화면을 바꾸면 고를 수 있는 해상도가 달라질 수 있다
        FillResolutions();
    }

    void OnResolution(int index)
    {
        if (filling) return;
        if (index < 0 || index >= resolutions.Count) return;

        DisplaySettings.SetResolution(resolutions[index].x, resolutions[index].y);
    }

    void OnFrame(int index)
    {
        if (filling) return;

        int[] options = DisplaySettings.FrameOptions;
        if (index < 0 || index >= options.Length) return;

        DisplaySettings.FrameLimit = options[index];
    }
}
