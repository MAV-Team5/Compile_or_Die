using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 피격 순간에 잠깐 켜지는 광원 풀.
///
/// <b>개수를 고정하는 이유</b> — 2D 라이트 비용은 "광원 수 × 덮는 면적 × 적용 레이어" 에 비례한다.
/// 뱀서라이크는 피격이 폭발적으로 늘어서, 제한이 없으면 후반에 비용도 화면도 같이 무너진다.
/// 그래서 <see cref="PoolManager"/> 처럼 모자라면 늘리지 않고, 꽉 차면 가장 오래된 광원을 재사용한다.
///
/// <b>반경을 안 바꾸는 이유</b> — 반경이 바뀌면 라이트 메쉬가 다시 만들어진다.
/// 생성할 때 한 번만 정하고, 페이드는 Intensity 로만 한다.
///
/// 씬에 하나 둔다. 없으면 피격 광원만 안 나오고 나머지는 그대로 동작한다.
/// </summary>
public class HitLightPool : MonoBehaviour
{
    public static HitLightPool Current { get; private set; }

    class Slot
    {
        public Light2D light;
        public float startTime;
        public bool active;
    }

    [Header("풀")]
    [Tooltip("Point Light 2D 프리팹. 그림자 끔, Target Sorting Layers 는 적·배경 등 꼭 필요한 레이어만.")]
    [SerializeField] Light2D lightPrefab;

    [Tooltip("동시에 켤 수 있는 최대 개수. 넘치면 가장 오래된 광원이 새 위치로 옮겨간다.")]
    [SerializeField, Min(1)] int poolSize = 6;

    [Header("연출")]
    [Tooltip("광원 바깥 반경(유닛). 클수록 비싸다 — 3~5 이하를 권장.")]
    [SerializeField, Min(0.1f)] float radius = 3f;

    [Tooltip("안쪽 반경 비율. 0에 가까울수록 부드럽게 퍼진다.")]
    [SerializeField, Range(0f, 1f)] float innerRatio = 0.2f;

    [SerializeField, Min(0f)] float intensity = 1.2f;
    [SerializeField] Color color = new Color(1f, 0.92f, 0.75f);

    [Tooltip("켜져 있는 시간(초).")]
    [SerializeField, Min(0.05f)] float duration = 0.18f;

    [Tooltip("시간(0~1) → 밝기 배율(0~1). 기본은 직선으로 꺼진다.")]
    [SerializeField] AnimationCurve fade = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Header("남발 방지")]
    [Tooltip("이 거리 안에 이미 켜진 광원이 있으면 새로 안 켜고 그 광원을 다시 밝힌다. 광역기 대응.")]
    [SerializeField, Min(0f)] float mergeRadius = 1.5f;

    [Tooltip("같은 대상이 이 시간 안에 또 맞아도 광원은 한 번만. 다단 히트 무기 대응.")]
    [SerializeField, Min(0f)] float targetCooldown = 0.25f;

    const int CooldownPruneThreshold = 256;

    Slot[] slots;
    readonly Dictionary<int, float> lastHitTime = new();

    void Awake()
    {
        Current = this;

        if (lightPrefab == null)
        {
            Debug.LogWarning("[HitLightPool] lightPrefab 이 비어 있다. 피격 광원이 나오지 않는다.", this);
            enabled = false;
            return;
        }

        slots = new Slot[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            Light2D light = Instantiate(lightPrefab, transform);
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * innerRatio;
            light.color = color;
            light.intensity = 0f;
            light.enabled = false;

            slots[i] = new Slot { light = light };
        }
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    /// <summary>피격 위치에 광원을 켠다. 쿨다운·병합·재사용은 여기서 정리된다.</summary>
    public void Play(Vector2 position, int targetId)
    {
        if (slots == null) return;

        float now = Time.time;

        if (lastHitTime.TryGetValue(targetId, out float last) && now - last < targetCooldown)
            return;

        lastHitTime[targetId] = now;
        PruneCooldowns(now);

        Slot merged = FindMergeable(position);

        if (merged != null)
        {
            // 켜진 채로 위치만 튀면 번쩍임이 산만하다. 자리는 두고 밝기만 되살린다
            Restart(merged, now);
            return;
        }

        Slot slot = PickSlot();
        slot.light.transform.position = new Vector3(position.x, position.y, 0f);
        Restart(slot, now);
    }

    void Update()
    {
        float now = Time.time;

        for (int i = 0; i < slots.Length; i++)
        {
            Slot slot = slots[i];
            if (!slot.active) continue;

            float t = (now - slot.startTime) / duration;

            if (t >= 1f)
            {
                slot.active = false;
                slot.light.enabled = false;
                continue;
            }

            slot.light.intensity = intensity * fade.Evaluate(t);
        }
    }

    /// <summary>근처에 이미 켜진 광원이 있으면 그것을 돌려준다.</summary>
    Slot FindMergeable(Vector2 position)
    {
        float sqrRadius = mergeRadius * mergeRadius;

        for (int i = 0; i < slots.Length; i++)
        {
            Slot slot = slots[i];
            if (!slot.active) continue;

            Vector2 delta = (Vector2)slot.light.transform.position - position;
            if (delta.sqrMagnitude <= sqrRadius) return slot;
        }

        return null;
    }

    /// <summary>꺼진 칸이 있으면 그것을, 다 켜져 있으면 가장 오래된 것을 고른다.</summary>
    Slot PickSlot()
    {
        Slot oldest = slots[0];

        for (int i = 0; i < slots.Length; i++)
        {
            Slot slot = slots[i];
            if (!slot.active) return slot;
            if (slot.startTime < oldest.startTime) oldest = slot;
        }

        return oldest;
    }

    void Restart(Slot slot, float now)
    {
        slot.startTime = now;
        slot.active = true;
        slot.light.intensity = intensity * fade.Evaluate(0f);
        slot.light.enabled = true;
    }

    /// <summary>
    /// 대상이 늘어나도 사전이 무한히 커지지 않게 오래된 기록을 비운다.
    /// 쿨다운이 지난 기록은 없어도 결과가 같다.
    /// </summary>
    void PruneCooldowns(float now)
    {
        if (lastHitTime.Count < CooldownPruneThreshold) return;

        var expired = new List<int>();

        foreach (var pair in lastHitTime)
            if (now - pair.Value >= targetCooldown) expired.Add(pair.Key);

        for (int i = 0; i < expired.Count; i++)
            lastHitTime.Remove(expired[i]);
    }
}
