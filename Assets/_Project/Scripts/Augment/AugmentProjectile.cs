using System.Collections.Generic;
using UnityEngine;

/// <summary>증강 투사체. 적중 시 콜백을 쏘고 스스로 정리한다.</summary>
public class AugmentProjectile : MonoBehaviour
{
    System.Action<HitInfo> onHit;
    LayerMask targetMask;

    /// <summary>연쇄 원점. 이 투사체가 태어난 자리의 적이라 다시 때리면 안 된다.</summary>
    Transform ignore;

    /// <summary>이 투사체 혼자 쓰는 기록.</summary>
    readonly HashSet<Transform> ownHits = new();

    /// <summary>
    /// 실제로 참조하는 기록. 볼리 공용 집합을 받으면 그쪽을 가리킨다 —
    /// 그러면 같이 나간 형제 투사체들이 같은 적을 중복해서 못 때린다.
    /// </summary>
    HashSet<Transform> alreadyHit;

    Vector2 velocity;

    /// <summary>비행 방향(정규화). 적중 정보에 실어 하위 파이프라인이 물려받게 한다.</summary>
    Vector2 heading;

    float speed;
    float lifeRemain;
    float travelRemain;
    int pierceRemain;
    int hitIndex;

    /// <summary>실 연출. 없으면 null.</summary>
    ProjectileThread thread;

    /// <summary>원점이 사라지면 이 투사체도 접을지.</summary>
    bool dieWithOrigin;

    // ── 유도 ──
    Homing homing;
    Transform chase;
    float retargetRemain;

    void Awake() => TryGetComponent(out thread);

    public void Launch(Vector2 direction, float speed, float lifetime, float maxDistance,
                       int pierce, LayerMask mask, Transform ignoreTarget,
                       System.Action<HitInfo> callback,
                       Homing homingSetting = null, Transform launchTarget = null,
                       HashSet<Transform> volleyHits = null, bool endWithOrigin = false)
    {
        dieWithOrigin = endWithOrigin;

        // 공용 집합을 받으면 그걸 쓰고, 없으면 혼자 기록한다
        alreadyHit = volleyHits ?? ownHits;

        if (volleyHits == null) ownHits.Clear();

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

        transform.up = direction;

        // 실은 발사 자리를 알아야 한다. 풀에서 꺼낼 때 스스로는 못 잡는다
        if (thread != null) thread.Begin(transform.position, ignoreTarget);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 쏜 자리가 사라졌으면 이 투사체는 갈 곳이 없다.
        // 죽은 적은 풀에서 되살아나므로, 들고 있으면 엉뚱한 적을 원점으로 삼게 된다
        if (dieWithOrigin && ignore != null && !ignore.gameObject.activeInHierarchy)
        {
            Despawn();
            return;
        }

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
        dieWithOrigin = false;
        homing = null;
        chase = null;
        velocity = Vector2.zero;

        // 공용 집합은 형제들이 아직 쓰고 있으니 비우면 안 된다
        ownHits.Clear();
        alreadyHit = ownHits;

        gameObject.SetActive(false);
    }
}
