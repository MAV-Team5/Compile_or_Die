using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 할로윈 배경. 한 장짜리 그림을 끝없이 깔아주고, <b>B 키</b>로 여러 장 중에서 고를 수 있다.
///
/// <b>끝없이 깔리는 원리</b> — 그림을 상하좌우로 뒤집어 붙인 2×2 타일(<c>HalloweenBG_tile_NN</c>)은
/// 가장자리가 서로 맞아서 이어 붙여도 이음새가 안 보인다. SpriteRenderer 의 Tiled 모드로 넉넉히 깔아두고,
/// 카메라가 움직이면 <b>타일 한 칸 단위로만</b> 따라간다. 칸 단위로 옮기면 그림이 제자리에 있는 것처럼 보인다 —
/// 카메라를 그대로 따라가면 배경이 화면에 붙어 플레이어가 안 움직이는 것처럼 보인다.
///
/// <b>고른 배경은 저장된다</b>(PlayerPrefs). 다음 판에도, 게임을 껐다 켜도 그 배경으로 시작한다.
///
/// 기존 배경(타일맵 + Reposition)과 달리 콜라이더가 없다. 배경은 그리기만 한다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class HalloweenBackground : MonoBehaviour
{
    const string PrefKey = "CoD.Halloween.Background";

    [Tooltip("고를 수 있는 배경들. 모두 같은 크기의 2×2 뒤집기 타일이어야 한다.\n" +
             "에디터 빌더(CoD/Halloween)가 Art/Background/HalloweenBG_tile_NN 을 모아 채운다.")]
    [SerializeField] Sprite[] options;

    [Tooltip("처음 실행했을 때(저장된 선택이 없을 때)의 배경 번호. 0부터.")]
    [Min(0)] [SerializeField] int defaultIndex;

    [Tooltip("배경을 다음 것으로 넘기는 키. 게임 조작에 안 쓰는 키여야 한다.")]
    [SerializeField] Key cycleKey = Key.B;

    SpriteRenderer sr;
    Transform cam;
    Vector2 period;
    int index;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        int count = options != null ? options.Length : 0;
        index = count > 0 ? Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, defaultIndex), 0, count - 1) : 0;

        Apply(announce: false);
    }

    void Update()
    {
        if (options == null || options.Length < 2) return;

        Keyboard kb = Keyboard.current;
        if (kb == null || !kb[cycleKey].wasPressedThisFrame) return;

        index = (index + 1) % options.Length;
        PlayerPrefs.SetInt(PrefKey, index);
        PlayerPrefs.Save();

        Apply(announce: true);
    }

    void Apply(bool announce)
    {
        if (options != null && options.Length > 0 && options[index] != null)
            sr.sprite = options[index];

        // 타일 한 칸 = 스프라이트 하나의 월드 크기. 그림마다 같게 만들지만, 달라도 맞춰 따라가게 매번 다시 잰다
        Vector2 cell = sr.sprite != null ? (Vector2)sr.sprite.bounds.size : Vector2.one * 10f;
        period = cell;
        sr.size = cell * 3f;

        if (announce && LogManager.Instance != null)
            LogManager.Instance.System($"wallpaper {index + 1}/{options.Length}");
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main == null) return;
            cam = Camera.main.transform;
        }

        Vector3 c = cam.position;
        float x = Mathf.Round(c.x / period.x) * period.x;
        float y = Mathf.Round(c.y / period.y) * period.y;

        transform.position = new Vector3(x, y, transform.position.z);
    }
}
