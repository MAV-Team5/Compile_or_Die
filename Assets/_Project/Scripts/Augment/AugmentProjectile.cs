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

    // ── 웨이브 ──
    Wave wave;
    /// <summary>흔들림 없는 중심선 위치. 유도·사거리는 이 기준이다.</summary>
    Vector2 spine;
    float wavePhase;
    float waveSign;
    float waveAge;

    TrailRenderer[] trails;
    VolleySkin skin;

    void Awake()
    {
        TryGetComponent(out thread);
        TryGetComponent(out skin);
        trails = GetComponentsInChildren<TrailRenderer>(true);
    }

    public void Launch(Vector2 direction, float speed, float lifetime, float maxDistance,
                       int pierce, LayerMask mask, Transform ignoreTarget,
                       System.Action<HitInfo> callback,
                       Homing homingSetting = null, Transform launchTarget = null,
                       HashSet<Transform> volleyHits = null, bool endWithOrigin = false,
                       Wave waveSetting = null, int shotIndex = 0)
    {
        // 쏘기도 전에 원점이 죽었으면 따라갈 것이 없다. 좌표는 이미 잡혔으므로 그냥 놓아준다.
        //
        // ＊ 이 가드가 없으면 연쇄가 통째로 사라진다 —
        //   피해로 적이 죽은 직후 그 적을 원점으로 연쇄를 쏘기 때문에,
        //   피해가 적 체력을 넘는 레벨부터 갑자기 연쇄가 안 되는 것처럼 보인다.
        if (endWithOrigin && ignoreTarget != null && !ignoreTarget.gameObject.activeInHierarchy)
        {
            ignoreTarget = null;
            endWithOrigin = false;
        }

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

        spine = transform.position;
        wave = waveSetting != null && waveSetting.Enabled ? waveSetting : null;
        waveAge = 0f;
        waveSign = wave != null && wave.mirrorPerShot && shotIndex % 2 == 1 ? -1f : 1f;
        wavePhase = wave != null && wave.randomPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;

        if (skin != null) skin.Apply(shotIndex);

        // 풀에서 꺼낸 투사체는 지난 자리에서부터 꼬리가 이어져 보인다
        foreach (TrailRenderer trail in trails) trail.Clear();

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

        spine += velocity * dt;
        Move(dt);

        // 사거리와 수명 중 먼저 닿는 쪽에서 소멸
        travelRemain -= speed * dt;
        lifeRemain   -= dt;

        if (travelRemain <= 0f || lifeRemain <= 0f) Despawn();
    }

    /// <summary>중심선에 좌우 흔들림을 얹어 실제 위치를 정한다. 머리는 실제로 움직인 방향을 본다.</summary>
    void Move(float dt)
    {
        if (wave == null)
        {
            transform.position = spine;
            return;
        }

        waveAge += dt;
        wavePhase += Mathf.PI * 2f * wave.frequency * dt;

        float ramp = wave.rampIn > 0f ? Mathf.Clamp01(waveAge / wave.rampIn) : 1f;
        Vector2 side = new(-heading.y, heading.x);
        Vector2 next = spine + side * (wave.amplitude * ramp * Mathf.Sin(wavePhase) * waveSign);

        Vector2 moved = next - (Vector2)transform.position;
        transform.position = next;

        if (moved.sqrMagnitude > 0.000001f) transform.up = moved;
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

        Vector2 toTarget = (Vector2)chase.position - spine;
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
        wave = null;
        velocity = Vector2.zero;

        // 공용 집합은 형제들이 아직 쓰고 있으니 비우면 안 된다
        ownHits.Clear();
        alreadyHit = ownHits;

        gameObject.SetActive(false);
    }
}
