using System.Collections.Generic;
using UnityEngine;

/// <summary>증강 투사체. 적중 시 콜백을 쏘고 스스로 정리한다.</summary>
public class AugmentProjectile : MonoBehaviour
{
    System.Action<HitInfo> onHit;
    LayerMask targetMask;

    /// <summary>연쇄 원점. 이 투사체가 태어난 자리의 적이라 다시 때리면 안 된다.</summary>
    Transform ignore;

    /// <summary>이 투사체가 이미 맞힌 대상. 관통 중 같은 적을 두 번 때리는 것만 막는다.</summary>
    readonly HashSet<Transform> alreadyHit = new();

    Vector2 velocity;

    /// <summary>비행 방향(정규화). 적중 정보에 실어 하위 파이프라인이 물려받게 한다.</summary>
    Vector2 heading;

    float speed;
    float lifeRemain;
    float travelRemain;
    int pierceRemain;
    int hitIndex;

    // ── 유도 ──
    Homing homing;
    Transform chase;
    float retargetRemain;

    public void Launch(Vector2 direction, float speed, float lifetime, float maxDistance,
                       int pierce, LayerMask mask, Transform ignoreTarget,
                       System.Action<HitInfo> callback,
                       Homing homingSetting = null, Transform launchTarget = null)
    {
        this.speed   = speed;
        heading      = direction.normalized;
        velocity     = heading * speed;
        lifeRemain   = lifetime;
        travelRemain = maxDistance;
        pierceRemain = Mathf.Max(1, pierce);
        targetMask   = mask;
        ignore       = ignoreTarget;
        onHit        = callback;
        hitIndex     = 0;

        homing = homingSetting != null && homingSetting.Enabled ? homingSetting : null;
        chase = launchTarget;
        retargetRemain = 0f;

        alreadyHit.Clear();
        transform.up = direction;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (homing != null) Steer(dt);

        transform.position += (Vector3)(velocity * dt);

        // 사거리와 수명 중 먼저 닿는 쪽에서 소멸
        travelRemain -= speed * dt;
        lifeRemain   -= dt;

        if (travelRemain <= 0f || lifeRemain <= 0f) Despawn();
    }

    /// <summary>목표 쪽으로 조금씩 방향을 튼다. 속력은 그대로 유지된다.</summary>
    void Steer(float dt)
    {
        retargetRemain -= dt;

        // 목표를 잃었거나 주기가 됐을 때만 다시 찾는다. 매 프레임 검색하면 투사체가 많을 때 무겁다
        if (homing.seekRadius > 0f && (chase == null || !chase.gameObject.activeInHierarchy ||
                                       retargetRemain <= 0f))
        {
            retargetRemain = homing.retargetInterval;
            chase = FindTarget();
        }

        if (chase == null || !chase.gameObject.activeInHierarchy) return;

        Vector2 toTarget = (Vector2)chase.position - (Vector2)transform.position;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        heading = Vector3.RotateTowards(
            heading, toTarget.normalized,
            homing.turnSpeed * Mathf.Deg2Rad * dt, 0f);

        velocity = heading * speed;
        transform.up = heading;
    }

    /// <summary>이미 뚫은 적은 빼고 가장 가까운 적. 안 그러면 방금 맞힌 적 주위를 맴돈다.</summary>
    Transform FindTarget()
        => TargetQuery.Nearest(transform.position, homing.seekRadius, alreadyHit);

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((targetMask.value & (1 << other.gameObject.layer)) == 0) return;

        Transform target = other.transform;

        // 태어난 자리의 적은 통과. 형제 투사체가 맞힌 적은 신경 쓰지 않는다
        if (target == ignore) return;

        // 콜라이더 경계에서 흔들려도 같은 적을 두 번 세지 않는다
        if (!alreadyHit.Add(target)) return;

        onHit?.Invoke(new HitInfo
        {
            Target    = target,
            Point     = transform.position,
            Index     = hitIndex++,
            Direction = heading
        });

        // 방금 뚫은 적을 계속 쫓으면 제자리를 맴돈다
        if (chase == target) chase = null;

        pierceRemain--;
        if (pierceRemain <= 0) Despawn();
    }

    /// <summary>풀에 반납한다. 콜백 참조를 끊어야 이전 발동의 ctx가 살아남지 않는다.</summary>
    void Despawn()
    {
        onHit = null;
        ignore = null;
        homing = null;
        chase = null;
        velocity = Vector2.zero;

        alreadyHit.Clear();
        gameObject.SetActive(false);
    }
}
