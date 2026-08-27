using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 HUD 조립과 갱신.
/// 상단 타이머 · 하단 경험치바 · 로그 상태줄 · 증강 아이콘 재배치 · 게임오버 표시.
/// 요소들은 코드로 만들어 캔버스에 붙이므로 해상도는 CanvasScaler가 흡수한다.
/// </summary>
public class GameHud : MonoBehaviour
{
    [Header("연결 (비우면 씬에서 찾는다)")]
    [SerializeField] Canvas canvas;
    [SerializeField] TMP_FontAsset font;

    [Header("경험치바")]
    [SerializeField] float expBarHeight = 70f;
    [SerializeField] Color expFillColor = new(0.31f, 0.77f, 0.76f);
    [SerializeField] Color expBackColor = new(0.88f, 0.88f, 0.88f);

    [Header("증강 아이콘 배치")]
    [SerializeField] bool placeAugmentHud = true;
    [SerializeField] Vector2 augmentHudOffset = new(-40f, 40f);
    [SerializeField] float augmentCellSize = 128f;

    [Header("증강 툴팁")]
    [Tooltip("아이콘에 마우스를 올렸을 때 뜨는 설명창의 가로 폭(px). 세로는 내용에 맞춰 늘어난다.")]
    [SerializeField] float tooltipWidth = 560f;

    [Header("로그 배치")]
    [Tooltip("로그 텍스트를 경험치바 위로 띄우는 여백. 경험치바 높이에 더해진다.")]
    [SerializeField] float logRaiseMargin = 40f;

    TMP_Text timerText;
    TMP_Text expText;
    RectTransform expFill;
    RectTransform gameOverPanel;

    PlayerHealth playerHealth;
    LevelSystem levelSystem;

    UiTheme theme;

    void Start()
    {
        theme = UiTheme.Current;

        // 인스펙터에 물려둔 게 있으면 그대로 두고, 비었을 때만 테마 글꼴로 채운다
        if (font == null) font = theme.mono;

        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[GameHud] 캔버스가 없다", this);
            enabled = false;
            return;
        }

        BuildTimer();
        BuildExpBar();
        BuildGameOver();
        PlaceAugmentHud();
        RaiseLogAboveExpBar();

        // 증강 아이콘에 마우스를 올렸을 때 뜰 설명창. 화면에 하나면 된다
        AugmentTooltip.Create(canvas, theme, font, tooltipWidth);

        levelSystem = GameManager.instance.levelSystem;
        levelSystem.ExpChanged += OnExpChanged;
        OnExpChanged(levelSystem.Level, levelSystem.CurrentExp, levelSystem.RequiredExp);

        Player player = GameManager.instance.player;
        if (player != null && player.TryGetComponent(out playerHealth))
            playerHealth.Died += OnPlayerDied;
    }

    void OnDestroy()
    {
        if (levelSystem != null) levelSystem.ExpChanged -= OnExpChanged;
        if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
    }

    void Update()
    {
        RefreshTimer();
        RefreshStatusLine();
    }

    // ── 조립 ──────────────────────────────────────────────

    void BuildTimer()
    {
        Image back = UiFactory.CreateImage("TimerBack", canvas.transform, UiTheme.Fade(theme.background, 0.55f));
        UiFactory.Place((RectTransform)back.transform,
                        new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, 0f), new Vector2(700f, 170f));

        timerText = UiFactory.CreateText("TimerText", back.transform, font,
                                         120f, theme.text, TextAlignmentOptions.Center);
        UiFactory.Stretch((RectTransform)timerText.transform, Vector2.zero, Vector2.one);
    }

    void BuildExpBar()
    {
        Image back = UiFactory.CreateImage("ExpBar", canvas.transform, expBackColor);
        var backRect = (RectTransform)back.transform;
        UiFactory.Stretch(backRect, Vector2.zero, new Vector2(1f, 0f));
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.sizeDelta = new Vector2(0f, expBarHeight);
        backRect.anchoredPosition = Vector2.zero;

        Image fill = UiFactory.CreateImage("Fill", back.transform, expFillColor);
        expFill = (RectTransform)fill.transform;
        UiFactory.Stretch(expFill, Vector2.zero, new Vector2(0f, 1f));

        expText = UiFactory.CreateText("Label", back.transform, font,
                                       46f, theme.text, TextAlignmentOptions.Center);
        UiFactory.Stretch((RectTransform)expText.transform, Vector2.zero, Vector2.one);

        // 게이지가 배경(밝은 회색)이든 채움(청록)이든 항상 읽히게 테두리를 준다
        expText.fontMaterial.EnableKeyword("OUTLINE_ON");
        expText.outlineWidth = 0.2f;
        expText.outlineColor = new Color32(0, 0, 0, 255);
    }

    void BuildGameOver()
    {
        Image dim = UiFactory.CreateImage("GameOverPanel", canvas.transform, UiTheme.Fade(theme.background, 0.75f));
        dim.raycastTarget = true;

        gameOverPanel = (RectTransform)dim.transform;
        UiFactory.Stretch(gameOverPanel, Vector2.zero, Vector2.one);

        TMP_Text title = UiFactory.CreateText("Title", gameOverPanel, font,
                                              200f, theme.warn, TextAlignmentOptions.Center);
        UiFactory.Place((RectTransform)title.transform,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, 80f), new Vector2(2400f, 260f));
        title.text = "GAME OVER";

        TMP_Text sub = UiFactory.CreateText("Sub", gameOverPanel, font,
                                            64f, theme.dim, TextAlignmentOptions.Center);
        UiFactory.Place((RectTransform)sub.transform,
                        new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, -120f), new Vector2(2400f, 100f));
        sub.text = " > process terminated : PLAYER_01";

        gameOverPanel.gameObject.SetActive(false);
    }

    /// <summary>기존 AugmentHUD를 경험치바 위 오른쪽 구석으로 옮긴다.</summary>
    void PlaceAugmentHud()
    {
        if (!placeAugmentHud) return;

        AugmentHud hud = FindAnyObjectByType<AugmentHud>();
        if (hud == null) return;

        var rect = (RectTransform)hud.transform;
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(augmentHudOffset.x, expBarHeight + augmentHudOffset.y);

        if (hud.TryGetComponent(out GridLayoutGroup grid))
            grid.cellSize = new Vector2(augmentCellSize, augmentCellSize);
    }

    /// <summary>기존 위치를 유지한 채 경험치바 높이만큼만 위로 밀어 겹침을 막는다.</summary>
    void RaiseLogAboveExpBar()
    {
        if (LogManager.Instance == null) return;

        RectTransform rect = LogManager.Instance.TextRect;
        if (rect == null) return;

        Vector2 pos = rect.anchoredPosition;
        pos.y += expBarHeight + logRaiseMargin;
        rect.anchoredPosition = pos;
    }

    // ── 갱신 ──────────────────────────────────────────────

    void RefreshTimer()
    {
        if (timerText == null) return;

        // 남은 시간이 아니라 버틴 시간이다. 런은 시간으로 끝나지 않는다
        timerText.text = RunResult.Format(RunDirector.RunTime);
    }

    void RefreshStatusLine()
    {
        if (LogManager.Instance == null || playerHealth == null || levelSystem == null) return;

        LogManager.Instance.SetStatusLine(
            $"hp {Mathf.CeilToInt(playerHealth.Current)}/{playerHealth.Max:0} " +
            $"lv {levelSystem.Level} kills {RunDirector.KillCount}");
    }

    void OnExpChanged(int level, int current, int required)
    {
        float ratio = required > 0 ? Mathf.Clamp01((float)current / required) : 0f;
        expFill.anchorMax = new Vector2(ratio, 1f);
        expText.text = $"Lv {level}  {current}/{required}";
    }

    void OnPlayerDied()
    {
        gameOverPanel.SetAsLastSibling();
        gameOverPanel.gameObject.SetActive(true);
    }
}
