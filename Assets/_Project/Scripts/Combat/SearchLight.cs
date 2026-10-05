using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>탐색 광원의 모양. 증강이 훑는 형태에 맞춘다.</summary>
public enum SearchLightShape
{
    /// <summary>BFS 처럼 범위가 원인 탐색. 표식이 붙은 적들의 중심과 반경을 따른다.</summary>
    Circle,

    /// <summary>DFS·Linear Search 처럼 직선인 탐색. 표식이 붙은 적들 중 가장 먼 두 점을 잇는다.</summary>
    Line
}

/// <summary>
/// 탐색 증강 하나가 표식을 남겨둔 동안 그 자리를 밝히는 광원.
/// <see cref="SearchLightController"/> 가 "어디에·얼마나 크게" 만 알려주고, 부드럽게 따라가는 것은 여기서 한다.
///
/// <b>Sprite Light 를 쓰는 이유</b> — Point Light 는 반경이 바뀔 때마다 라이트 메쉬가 다시 만들어진다.
/// 스프라이트 광원은 Transform 만 바뀌므로 표식이 8초 넘게 유지되며 적을 따라 움직여도 부담이 없다.
///
/// 프리팹 규격 — Light 2D 를 <b>Sprite</b> 타입으로 두고 스프라이트를 꽂는다.
/// 크기는 스프라이트 실제 크기를 재서 맞추므로 PPU 를 신경 쓸 필요가 없다.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class SearchLight : MonoBehaviour
{
    [Header("모양")]
    public SearchLightShape shape = SearchLightShape.Circle;

    [Tooltip("최소 크기. Circle 은 반지름, Line 은 길이(유닛). 적이 한두 마리만 남아도 이만큼은 밝힌다.")]
    [SerializeField, Min(0.1f)] float minSize = 2f;

    [Tooltip("최대 크기. 너무 넓게 퍼진 표식이 화면 전체를 덮어 비용이 커지는 것을 막는다.")]
    [SerializeField, Min(0.1f)] float maxSize = 6f;

    [Tooltip("적들을 감싸고 남기는 여유(유닛). Line 은 양 끝에 각각 더해진다.")]
    [SerializeField, Min(0f)] float padding = 1f;

    [Tooltip("Line 전용. 광원의 굵기(유닛).")]
    [SerializeField, Min(0.1f)] float lineWidth = 1.5f;

    [Header("밝기")]
    [SerializeField, Min(0f)] float intensity = 0.8f;
    [SerializeField] Color color = Color.white;

    [Header("움직임")]
    [Tooltip("위치를 따라가는 부드러움(초). 클수록 느긋하게 따라온다.")]
    [SerializeField, Min(0.01f)] float moveSmoothTime = 0.15f;

    [Tooltip("크기·각도가 바뀌는 부드러움(초).")]
    [SerializeField, Min(0.01f)] float sizeSmoothTime = 0.25f;

    [SerializeField, Min(0.01f)] float fadeInTime = 0.2f;
    [SerializeField, Min(0.01f)] float fadeOutTime = 0.4f;

    Light2D light2D;
    Vector2 spriteSize = Vector2.one;

    Vector2 targetPosition;
    float targetSize;
    float targetAngle;

    Vector3 positionVelocity;
    float currentSize;
    float sizeVelocity;
    float currentAngle;
    float angleVelocity;

    bool shown;
    bool needSnap = true;
    float fade;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        light2D.color = color;

        if (light2D.lightType != Light2D.LightType.Sprite)
        {
            Debug.LogWarning($"[SearchLight] {name} 의 Light 2D 가 Sprite 타입이 아니다. " +
                             "크기·방향이 반영되지 않는다.", this);
        }

        // 스프라이트의 실제 크기를 재서 유닛 단위로 맞춘다. PPU 가 달라도 그대로 된다
        Sprite sprite = light2D.lightCookieSprite;
        if (sprite != null) spriteSize = sprite.bounds.size;

        currentSize = minSize;
        targetSize = minSize;

        light2D.intensity = 0f;
        light2D.enabled = false;
    }

    /// <summary>표식이 있으면 켜고 없으면 끈다. 꺼진 뒤 다시 켜지면 자리로 바로 옮겨간다.</summary>
    public void SetShown(bool value)
    {
        if (value == shown) return;

        shown = value;

        // 완전히 꺼져 있다가 켜질 때만 처음 자리로 바로 간다.
        // 페이드아웃 도중에 다시 켜지면 미끄러지듯 이어지는 편이 자연스럽다
        if (shown && fade <= 0.001f) needSnap = true;
    }

    /// <summary>
    /// 목표를 정한다. <paramref name="size"/> 는 Circle 이면 반지름, Line 이면 길이다.
    /// 최소·최대는 여기서 자른다.
    /// </summary>
    public void Aim(Vector2 center, float size, float angle)
    {
        targetPosition = center;
        targetSize = Mathf.Clamp(size, minSize, maxSize);

        if (shape != SearchLightShape.Line) return;

        // 직선은 앞뒤가 같아서 180° 뒤집힌 각도도 같은 모양이다.
        // 가까운 쪽으로 골라야 두 끝점의 순서가 바뀔 때 광원이 휙 돌지 않는다
        float delta = Mathf.DeltaAngle(currentAngle, angle);
        if (delta > 90f) delta -= 180f;
        else if (delta < -90f) delta += 180f;

        targetAngle = currentAngle + delta;
    }

    /// <summary>표식 목록에서 크기를 뽑을 때 쓰는 여유값.</summary>
    public float Padding => padding;

    public float MinSize => minSize;
    public float MaxSize => maxSize;

    void Update()
    {
        float dt = Time.deltaTime;

        fade = Mathf.MoveTowards(fade, shown ? 1f : 0f,
                                 dt / (shown ? fadeInTime : fadeOutTime));

        bool visible = fade > 0.001f;
        if (light2D.enabled != visible) light2D.enabled = visible;
        if (!visible) return;

        if (needSnap)
        {
            transform.position = new Vector3(targetPosition.x, targetPosition.y, 0f);
            currentSize = targetSize;
            currentAngle = targetAngle;
            positionVelocity = Vector3.zero;
            sizeVelocity = 0f;
            angleVelocity = 0f;
            needSnap = false;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position,
                new Vector3(targetPosition.x, targetPosition.y, 0f), ref positionVelocity, moveSmoothTime);

            currentSize = Mathf.SmoothDamp(currentSize, targetSize, ref sizeVelocity, sizeSmoothTime);

            if (shape == SearchLightShape.Line)
                currentAngle = Mathf.SmoothDamp(currentAngle, targetAngle, ref angleVelocity, sizeSmoothTime);
        }

        ApplyTransform();
        light2D.intensity = intensity * fade;
    }

    void ApplyTransform()
    {
        if (shape == SearchLightShape.Circle)
        {
            // 반지름 → 지름으로 바꿔 스프라이트 크기로 나눈다
            float scale = currentSize * 2f / Mathf.Max(0.0001f, spriteSize.x);
            transform.localScale = new Vector3(scale, scale, 1f);
            transform.rotation = Quaternion.identity;
            return;
        }

        transform.localScale = new Vector3(
            currentSize / Mathf.Max(0.0001f, spriteSize.x),
            lineWidth / Mathf.Max(0.0001f, spriteSize.y),
            1f);

        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }
}
