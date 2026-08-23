using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;   // ColorBlock

/// <summary>
/// 게임 UI 전체가 쓰는 색·글꼴·여백. <c>Create → CoD → UI Theme</c> 로 만든다.
///
/// <b>왜 모으나</b> — 화면마다 회색을 따로 적으면 미묘하게 다 달라진다.
/// "안 예쁘다"의 절반은 배치가 아니라 이 통일감 부재에서 온다.
/// 한 곳에 모아두면 인스펙터에서 값 하나를 바꿔 모든 화면이 같이 움직인다.
///
/// <b>컨셉</b> — 터미널이다. 색은 적게, 배경은 어둡게, 글꼴은 고정폭 하나.
/// 아트 리소스 없이 완성도를 올릴 수 있는 유일한 길이라 규칙을 좁게 잡았다.
/// </summary>
[CreateAssetMenu(fileName = "UiTheme", menuName = "CoD/UI Theme")]
public class UiTheme : ScriptableObject
{
    // ── 찾아 쓰기 ─────────────────────────────────────────

    static UiTheme current;

    /// <summary>
    /// 어디서든 쓰는 통로. 화면마다 인스펙터로 물려주는 수고를 없앤다.
    ///
    /// <c>Assets/Resources/UiTheme.asset</c> 을 자동으로 찾는다.
    /// 없으면 기본값을 즉석에서 만들어 쓰므로 에셋을 안 만들어도 게임은 돈다.
    /// </summary>
    public static UiTheme Current
    {
        get
        {
            if (current != null) return current;

            current = Resources.Load<UiTheme>("UiTheme");

            if (current == null)
            {
                current = CreateInstance<UiTheme>();
                current.name = "UiTheme (임시)";

                Debug.LogWarning("[UiTheme] Resources/UiTheme.asset 이 없어 기본값을 쓴다. " +
                                 "Create → CoD → UI Theme 로 만들어 Resources 폴더에 둘 것.");
            }

            return current;
        }
        set => current = value;
    }

    // ── 글꼴 ──────────────────────────────────────────────

    [Header("글꼴")]
    [Tooltip("고정폭 글꼴. 터미널 느낌의 8할이 여기서 온다. D2Coding · JetBrains Mono 등.")]
    public TMP_FontAsset mono;

    // ── 색 ────────────────────────────────────────────────

    [Header("바탕")]
    [Tooltip("가장 뒤. 화면 전체를 덮는 어두운 바탕.")]
    public Color background = new(0.039f, 0.055f, 0.078f, 1f);

    [Tooltip("패널·카드의 바닥. 배경보다 아주 조금 밝다.")]
    public Color surface = new(0.059f, 0.078f, 0.110f, 1f);

    [Tooltip("한 단 더 들어간 칸(아이콘 자리 등).")]
    public Color surfaceDim = new(0.078f, 0.102f, 0.141f, 1f);

    [Tooltip("테두리·구분선.")]
    public Color line = new(0.122f, 0.157f, 0.200f, 1f);

    [Header("글자")]
    [Tooltip("본문.")]
    public Color text = new(0.784f, 0.827f, 0.878f, 1f);

    [Tooltip("설명·보조 정보. 본문보다 확실히 어두워야 위계가 생긴다.")]
    public Color dim = new(0.353f, 0.400f, 0.459f, 1f);

    [Header("강조")]
    [Tooltip("고른 것·중요한 수치. 터미널 커서 색.")]
    public Color accent = new(0.302f, 0.816f, 0.882f, 1f);

    [Tooltip("성공·회복·클리어.")]
    public Color good = new(0.498f, 0.847f, 0.498f, 1f);

    [Tooltip("경고·피해·실패.")]
    public Color warn = new(1f, 0.420f, 0.420f, 1f);

    // ── 여백 ──────────────────────────────────────────────

    [Header("여백")]
    [Tooltip("여백의 기본 단위(px). 모든 간격을 이 배수로 쓰면 리듬이 맞는다.")]
    public float unit = 8f;

    /// <summary>기본 단위의 n배. 여백을 눈대중 숫자 대신 이걸로 적는다.</summary>
    public float Space(float multiple) => unit * multiple;

    // ── 증강 분류 색 ──────────────────────────────────────

    [System.Serializable]
    public class CategoryTint
    {
        public AugmentCategory category;
        public Color color = Color.white;
    }

    [Header("증강 분류")]
    [Tooltip("분류마다의 색. 터미널 톤을 유지하려면 채도를 너무 올리지 말 것.")]
    public List<CategoryTint> categories = new()
    {
        new() { category = AugmentCategory.Search,     color = new(0.302f, 0.816f, 0.882f) },
        new() { category = AugmentCategory.Sort,       color = new(0.878f, 0.643f, 0.302f) },
        new() { category = AugmentCategory.DataStruct, color = new(0.498f, 0.847f, 0.498f) },
        new() { category = AugmentCategory.Language,   color = new(0.420f, 0.620f, 0.910f) },
        new() { category = AugmentCategory.Optimize,   color = new(0.851f, 0.820f, 0.302f) },
        new() { category = AugmentCategory.Code,       color = new(0.663f, 0.545f, 0.878f) },
        new() { category = AugmentCategory.Item,       color = new(0.910f, 0.514f, 0.302f) }
    };

    public Color ColorOf(AugmentCategory category)
    {
        for (int i = 0; i < categories.Count; i++)
            if (categories[i].category == category) return categories[i].color;

        return text;
    }

    // ── 편의 ──────────────────────────────────────────────

    /// <summary>같은 색을 투명도만 바꿔 쓴다. 색을 새로 적지 않게 하려는 것.</summary>
    public static Color Fade(Color color, float alpha)
        => new(color.r, color.g, color.b, alpha);

    /// <summary>버튼 상태 색을 바탕색 하나에서 만들어 낸다. 네 색을 따로 적지 않아도 된다.</summary>
    public ColorBlock ButtonColors(Color normal)
    {
        return new ColorBlock
        {
            normalColor = normal,
            highlightedColor = Brighten(normal, 0.12f),
            pressedColor = Brighten(normal, -0.08f),
            selectedColor = normal,
            disabledColor = Fade(Brighten(normal, -0.05f), 0.45f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
    }

    static Color Brighten(Color color, float amount)
        => new(Mathf.Clamp01(color.r + amount),
               Mathf.Clamp01(color.g + amount),
               Mathf.Clamp01(color.b + amount),
               color.a);

    /// <summary>이 글꼴을 쓰되, 비어 있으면 테마 글꼴로. 기존에 물려둔 값을 안 깨려는 것.</summary>
    public TMP_FontAsset FontOr(TMP_FontAsset assigned) => assigned != null ? assigned : mono;
}
