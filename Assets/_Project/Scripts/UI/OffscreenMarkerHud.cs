using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 밖에 있는 <see cref="OffscreenTarget"/> 을 화면 가장자리에 화살표로 그린다.
/// 씬에 하나만 두면 된다 — 대상이 몇 개든 여기서 한꺼번에 돈다.
///
/// <b>보이면 안 그린다.</b> 화면 안에 들어온 순간 화살표가 사라진다 —
/// 눈에 보이는 것을 가리키는 화살표는 화면만 어지럽힌다.
///
/// <b>LateUpdate 에서 돈다.</b> 카메라(Cinemachine)가 움직인 뒤에 투영해야
/// 화살표가 한 프레임 늦게 따라오지 않는다.
/// </summary>
public class OffscreenMarkerHud : MonoBehaviour
{
    [Header("연결 (비우면 씬에서 찾는다)")]
    [SerializeField] Canvas canvas;

    [Tooltip("화살표 프리팹. Image 가 있어야 하고, TMP 글자가 자식에 있으면 거리를 띄운다.\n" +
             "비우면 코드가 흰 사각형으로 만든다 — 임시로 확인할 때만.")]
    [SerializeField] GameObject markerPrefab;

    [Header("배치")]
    [Tooltip("화면 가장자리에서 안쪽으로 이만큼 띄운다(픽셀). 화살표가 반쯤 잘리는 것을 막는다.")]
    [SerializeField] float edgeMargin = 60f;

    [Tooltip("프리팹이 없을 때 만들 화살표 크기(픽셀).")]
    [SerializeField] Vector2 fallbackSize = new(36f, 36f);

    [Tooltip("그림을 대상 방향으로 회전시킬지.\n" +
             "화살표는 켜고, 아이콘(상자 그림 등)은 꺼야 뒤집혀 보이지 않는다.")]
    [SerializeField] bool rotateIcon = true;

    /// <summary>대상 하나에 딸린 화살표. 대상이 사라지면 같이 꺼진다.</summary>
    class Marker
    {
        public RectTransform Root;
        public Image Icon;
        public TMP_Text Label;
    }

    readonly Dictionary<OffscreenTarget, Marker> markers = new();
    readonly List<OffscreenTarget> stale = new();

    Camera cam;

    void Awake()
    {
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
    }

    void LateUpdate()
    {
        if (canvas == null) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector2 center = new(Screen.width * 0.5f, Screen.height * 0.5f);

        for (int i = 0; i < OffscreenTarget.Active.Count; i++)
        {
            OffscreenTarget target = OffscreenTarget.Active[i];
            if (target == null) continue;

            Draw(target, center);
        }

        HideGone();
    }

    void Draw(OffscreenTarget target, Vector2 center)
    {
        Vector3 world = target.transform.position;

        float distance = Vector2.Distance(world, cam.transform.position);

        // 너무 멀면 안 그린다. 안 그러면 지나쳐 온 것들이 가장자리에 쌓인다
        if (target.showWithin > 0f && distance > target.showWithin)
        {
            Hide(target);
            return;
        }

        Vector3 screen = cam.WorldToScreenPoint(world);

        // 카메라 뒤쪽은 투영이 뒤집혀 나온다. 직교 카메라에서는 안 걸리지만 원근으로 바꿔도 버티게
        if (screen.z < 0f) screen = -screen;

        bool visible = screen.z >= 0f
                    && screen.x >= 0f && screen.x <= Screen.width
                    && screen.y >= 0f && screen.y <= Screen.height;

        // 보이면 화살표가 필요 없다
        if (visible)
        {
            Hide(target);
            return;
        }

        Marker marker = Ensure(target);
        if (marker == null) return;

        marker.Root.gameObject.SetActive(true);
        marker.Root.position = ClampToEdge(screen, center);

        if (rotateIcon)
        {
            Vector2 dir = (Vector2)screen - center;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            marker.Root.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (marker.Label != null)
        {
            marker.Label.gameObject.SetActive(target.showDistance);

            if (target.showDistance) marker.Label.text = distance.ToString("0.0");

            // 화살표가 돌아도 글자는 똑바로 서 있어야 읽힌다
            if (rotateIcon) marker.Label.transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>화면 밖 좌표를 화면 사각형 경계로 끌어당긴다.</summary>
    Vector2 ClampToEdge(Vector2 screen, Vector2 center)
    {
        Vector2 dir = screen - center;

        if (dir.sqrMagnitude < 0.0001f) return center;

        float halfW = Mathf.Max(1f, Screen.width * 0.5f - edgeMargin);
        float halfH = Mathf.Max(1f, Screen.height * 0.5f - edgeMargin);

        // 가로·세로 중 먼저 닿는 변을 고른다. 0으로 나누면 무한대가 나오는데
        // Min 이 반대쪽 값을 고르므로 그대로 두어도 안전하다
        float scaleX = halfW / Mathf.Abs(dir.x);
        float scaleY = halfH / Mathf.Abs(dir.y);

        return center + dir * Mathf.Min(scaleX, scaleY);
    }

    Marker Ensure(OffscreenTarget target)
    {
        if (markers.TryGetValue(target, out Marker found) && found.Root != null) return found;

        GameObject go = markerPrefab != null
            ? Instantiate(markerPrefab, canvas.transform)
            : BuildFallback();

        var marker = new Marker
        {
            Root  = go.GetComponent<RectTransform>(),
            Icon  = go.GetComponentInChildren<Image>(true),
            Label = go.GetComponentInChildren<TMP_Text>(true)
        };

        if (marker.Root == null)
        {
            Debug.LogWarning("[OffscreenMarkerHud] 화살표 프리팹에 RectTransform 이 없다.", this);
            Destroy(go);
            return null;
        }

        if (marker.Icon != null)
        {
            if (target.icon != null) marker.Icon.sprite = target.icon;

            marker.Icon.color = target.tint;
        }

        markers[target] = marker;

        return marker;
    }

    GameObject BuildFallback()
    {
        var go = new GameObject("Marker", typeof(RectTransform), typeof(Image));

        go.transform.SetParent(canvas.transform, false);
        go.GetComponent<RectTransform>().sizeDelta = fallbackSize;

        return go;
    }

    void Hide(OffscreenTarget target)
    {
        if (markers.TryGetValue(target, out Marker marker) && marker.Root != null)
            marker.Root.gameObject.SetActive(false);
    }

    /// <summary>대상이 사라졌으면 화살표도 치운다. 상자는 부서지면 없어진다.</summary>
    void HideGone()
    {
        stale.Clear();

        foreach (KeyValuePair<OffscreenTarget, Marker> pair in markers)
        {
            if (pair.Key != null && pair.Key.isActiveAndEnabled) continue;

            stale.Add(pair.Key);

            if (pair.Value.Root != null) Destroy(pair.Value.Root.gameObject);
        }

        for (int i = 0; i < stale.Count; i++) markers.Remove(stale[i]);
    }
}
