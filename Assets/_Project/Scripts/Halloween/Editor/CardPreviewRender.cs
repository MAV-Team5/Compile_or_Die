using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 증강 카드 한 장을 플레이 없이 그려 PNG 로 떨군다. 카드 UI 를 고칠 때 화면을 확인하는 용도.
/// <c>Temp/hw_cardpreview.request</c> 에 증강 에셋 이름(예: "001 Bash")을 한 줄씩 적으면
/// 프로젝트 루트의 <c>CardPreview_&lt;이름&gt;.png</c> 로 저장한다.
/// </summary>
[InitializeOnLoad]
static class CardPreviewRender
{
    const string RequestPath = "Temp/hw_cardpreview.request";
    static double next;

    static CardPreviewRender()
    {
        EditorApplication.update += () =>
        {
            if (EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + 2.0;
            if (!File.Exists(RequestPath) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            string[] names = File.ReadAllLines(RequestPath);
            File.Delete(RequestPath);
            foreach (string n in names) if (!string.IsNullOrWhiteSpace(n)) Render(n.Trim());
        };
    }

    static void Render(string assetName)
    {
        AugmentData data = null;
        foreach (string guid in AssetDatabase.FindAssets($"t:AugmentData {assetName}"))
        {
            var d = AssetDatabase.LoadAssetAtPath<AugmentData>(AssetDatabase.GUIDToAssetPath(guid));
            if (d != null && d.name == assetName) { data = d; break; }
        }
        var prefab = AssetDatabase.LoadAssetAtPath<AugmentCardView>("Assets/_Project/Prefabs/UI_Prefabs/AugmentCard.prefab");
        if (data == null || prefab == null) { Debug.LogWarning($"[CardPreview] 못 찾음: {assetName}"); return; }

        const int W = 800, H = 1300;
        var camGo = new GameObject("PreviewCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
        cam.orthographic = true;
        var rt = new RenderTexture(W, H, 24);
        cam.targetTexture = rt;

        var canvasGo = new GameObject("PreviewCanvas") { hideFlags = HideFlags.HideAndDontSave };
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 5f;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        try
        {
            var card = Object.Instantiate(prefab, canvasGo.transform);
            card.gameObject.SetActive(true);
            card.Adopt(UiTheme.Current, new AugmentCardView.Layout { CardSize = new Vector2(640f, 1100f), ButtonSize = 120f, IconSize = 300f }, 0.3f);
            card.Fill(data, null);

            for (int i = 0; i < 3; i++)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)card.transform);
            }

            // 무엇이 어디 있는지도 함께 남긴다
            var log = new System.Text.StringBuilder();
            foreach (RectTransform r in card.GetComponentsInChildren<RectTransform>(true))
            {
                if (!r.name.StartsWith("Tag") && r.name != "Category" && r.name != "Label" && r.name != "Name") continue;
                var g = r.GetComponent<Graphic>();
                log.AppendLine($"{r.name} active={r.gameObject.activeInHierarchy} pos={r.anchoredPosition} size={r.rect.size} color={(g != null ? g.color.ToString() : "-")}");
            }
            Debug.Log($"[CardPreview] {assetName}\n{log}");

            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes($"CardPreview_{assetName.Replace(' ', '_')}.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
        finally
        {
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(canvasGo);
            Object.DestroyImmediate(camGo);
        }
    }
}
