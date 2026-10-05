using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 탐색 증강이 표식을 남겨둔 동안 그 자리에 광원을 하나씩 유지한다.
///
/// <b>증강 단위로 광원 하나</b> — 표식 수만큼 광원을 만들면 표식이 많은 후반에 비용이 무너진다.
/// 광원 수는 표식을 남기는 증강 수로 고정된다.
///
/// <b>갱신은 일정 간격으로만</b> — 표식은 8초 넘게 남아 적이 움직이지만 모양은 꽤 유지된다.
/// 프레임마다 다시 잴 이유가 없어서 간격을 두고, 사이는 <see cref="SearchLight"/> 가 부드럽게 잇는다.
///
/// <b>비어도 바로 끄지 않는 이유</b> — 같은 증강이 다시 탐색하면 지난 표식을 전부 걷어낸 뒤 새로 붙이는데,
/// 그 사이 목록이 잠깐 빈다. 바로 끄면 탐색할 때마다 광원이 깜빡인다.
///
/// 씬에 하나 둔다. 없으면 광원만 안 나오고 탐색은 그대로 동작한다.
/// </summary>
public class SearchLightController : MonoBehaviour
{
    public static SearchLightController Current { get; private set; }

    class Track
    {
        public SearchLight light;
        public float lastSeenTime;
        public bool shown;
    }

    [Tooltip("표식 위치를 다시 재는 간격(초).")]
    [SerializeField, Min(0.02f)] float refreshInterval = 0.1f;

    [Tooltip("표식이 전부 사라져도 이 시간(초) 동안은 광원을 유지한다. 재탐색 순간의 깜빡임을 막는다.")]
    [SerializeField, Min(0f)] float emptyGrace = 0.4f;

    readonly Dictionary<AugmentInstance, Track> tracks = new();
    readonly List<Transform> buffer = new();

    float nextRefreshTime;

    void Awake() => Current = this;

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    /// <summary>증강이 표식을 남길 때 부른다. 이미 등록된 증강이면 아무 일도 없다.</summary>
    public void Register(AugmentInstance owner, SearchLight prefab)
    {
        if (owner == null || prefab == null) return;
        if (tracks.ContainsKey(owner)) return;

        tracks[owner] = new Track { light = Instantiate(prefab, transform) };
    }

    void Update()
    {
        if (Time.time < nextRefreshTime) return;
        nextRefreshTime = Time.time + refreshInterval;

        foreach (var pair in tracks)
            Refresh(pair.Key, pair.Value);
    }

    void Refresh(AugmentInstance owner, Track track)
    {
        SearchRegistry.CollectBy(owner, buffer);

        if (buffer.Count > 0)
        {
            track.lastSeenTime = Time.time;
            Aim(track.light, buffer);
        }

        bool shouldShow = Time.time - track.lastSeenTime <= emptyGrace;

        if (shouldShow == track.shown) return;

        track.shown = shouldShow;
        track.light.SetShown(shouldShow);
    }

    void Aim(SearchLight light, List<Transform> targets)
    {
        if (light.shape == SearchLightShape.Circle) AimCircle(light, targets);
        else AimLine(light, targets);
    }

    /// <summary>중심에서 가장 먼 적까지가 반경이다.</summary>
    static void AimCircle(SearchLight light, List<Transform> targets)
    {
        Vector2 sum = Vector2.zero;
        for (int i = 0; i < targets.Count; i++) sum += (Vector2)targets[i].position;

        Vector2 center = sum / targets.Count;

        float farthest = 0f;
        for (int i = 0; i < targets.Count; i++)
            farthest = Mathf.Max(farthest, Vector2.Distance(center, targets[i].position));

        light.Aim(center, farthest + light.Padding, 0f);
    }

    /// <summary>가장 멀리 떨어진 두 적을 잇는다. 표식이 수십 개여도 이 간격에서는 부담이 없다.</summary>
    static void AimLine(SearchLight light, List<Transform> targets)
    {
        if (targets.Count == 1)
        {
            light.Aim(targets[0].position, light.MinSize, 0f);
            return;
        }

        Vector2 a = targets[0].position;
        Vector2 b = targets[1].position;
        float best = -1f;

        for (int i = 0; i < targets.Count; i++)
        {
            for (int j = i + 1; j < targets.Count; j++)
            {
                float sqr = ((Vector2)targets[i].position - (Vector2)targets[j].position).sqrMagnitude;
                if (sqr <= best) continue;

                best = sqr;
                a = targets[i].position;
                b = targets[j].position;
            }
        }

        Vector2 delta = b - a;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        light.Aim((a + b) * 0.5f, delta.magnitude + light.Padding * 2f, angle);
    }
}
