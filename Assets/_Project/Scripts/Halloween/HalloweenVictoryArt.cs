using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 치이카와 스테이지에서 세이렌을 잡고 결과 화면(StageResult)으로 넘어오면, 먼작귀들이 돌아온 그림을 배경에 깐다.
///
/// <b>씬을 안 건드린다</b> — 씬이 로드될 때 조건을 보고 런타임에 캔버스를 하나 얹는다.
/// 조건: 마지막 런이 클리어 + 그 스테이지가 치이카와 이벤트 스테이지(<see cref="StageData.chiikawaEvent"/>).
///
/// <list type="bullet">
/// <item>그림 캔버스는 결과 UI 캔버스(정렬 0)보다 <b>뒤</b>(-50)에 둔다 — 결과 글자는 그대로 읽힌다.</item>
/// <item>그림 위에 옅은 어둠을 깔아 글자가 묻히지 않게 한다.</item>
/// <item>결과 UI 쪽에 화면을 꽉 채운 불투명 배경이 있으면 그림이 가려지므로, 그것만 반투명으로 낮춘다.</item>
/// <item>천천히 확대되는 움직임(켄 번스)으로 정지 화면이 밋밋하지 않게 한다.</item>
/// </list>
///
/// 그림은 Resources/HalloweenVictory 에 둔다.
/// </summary>
public class HalloweenVictoryArt : MonoBehaviour
{
    const string SceneName = "StageResult";
    const string ArtPath = "HalloweenVictory";

    const float FadeIn = 1.2f;          // 처음에 그림이 떠오르는 시간
    const float ZoomFrom = 1.0f;
    const float ZoomTo = 1.08f;
    const float ZoomTime = 25f;         // 이 시간 동안 천천히 확대
    const float Dim = 0.35f;            // 그림 위 어둠 — 결과 글자 가독성
    const float CoverAlpha = 0.72f;     // 가려진 불투명 배경을 이 정도로 낮춘다

    RectTransform art;
    CanvasGroup group;
    float age;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneName) return;

        RunResult result = RunResult.Last;
        StageData stage = StageContext.Active;
        if (result == null || !result.Cleared) return;
        if (stage == null || !stage.chiikawaEvent) return;

        var tex = Resources.Load<Texture2D>(ArtPath);
        if (tex == null)
        {
            Debug.LogWarning($"[HalloweenVictoryArt] Resources/{ArtPath} 그림이 없다");
            return;
        }

        var go = new GameObject("Halloween Victory Art");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<HalloweenVictoryArt>().Build(tex, scene);
    }

    void Build(Texture2D tex, Scene scene)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -50;   // 결과 UI(0) 뒤

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        group = gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.alpha = 0f;

        // 그림 — 화면을 꽉 채우도록(넘치는 쪽은 잘림) 비율을 맞춘다
        var artGo = new GameObject("Art", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        artGo.transform.SetParent(transform, false);
        art = (RectTransform)artGo.transform;
        art.anchorMin = Vector2.zero;
        art.anchorMax = Vector2.one;
        art.offsetMin = art.offsetMax = Vector2.zero;

        var raw = artGo.GetComponent<RawImage>();
        raw.texture = tex;
        raw.raycastTarget = false;

        var fitter = artGo.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = tex.width / (float)tex.height;

        // 어둠
        var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dimGo.transform.SetParent(transform, false);
        var dr = (RectTransform)dimGo.transform;
        dr.anchorMin = Vector2.zero;
        dr.anchorMax = Vector2.one;
        dr.offsetMin = dr.offsetMax = Vector2.zero;
        var dim = dimGo.GetComponent<Image>();
        dim.color = new Color(0.02f, 0.02f, 0.06f, Dim);
        dim.raycastTarget = false;

        SeeThroughCovers(scene);
    }

    /// <summary>
    /// 결과 UI 캔버스 바로 아래에서 화면을 꽉 채운 불투명 이미지(배경 판)를 반투명으로 낮춘다.
    /// 버튼·패널처럼 일부만 덮는 것은 건드리지 않는다.
    /// </summary>
    void SeeThroughCovers(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root == gameObject) continue;

            foreach (Canvas c in root.GetComponentsInChildren<Canvas>(true))
            {
                if (!c.isRootCanvas) continue;

                foreach (Transform child in c.transform)
                {
                    var img = child.GetComponent<Image>();
                    var rt = child as RectTransform;
                    if (img == null || rt == null) continue;

                    bool full = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one
                                && rt.offsetMin.sqrMagnitude < 1f && rt.offsetMax.sqrMagnitude < 1f;
                    if (!full || img.color.a < 0.9f) continue;

                    Color col = img.color;
                    col.a = CoverAlpha;
                    img.color = col;
                }
            }
        }
    }

    void Update()
    {
        if (art == null) return;

        age += Time.unscaledDeltaTime;
        group.alpha = Mathf.Clamp01(age / FadeIn);

        float k = Mathf.Clamp01(age / ZoomTime);
        float ease = k * k * (3f - 2f * k);
        art.localScale = Vector3.one * Mathf.Lerp(ZoomFrom, ZoomTo, ease);
    }
}
