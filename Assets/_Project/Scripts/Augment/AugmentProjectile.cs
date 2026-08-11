using System.Collections.Generic;
using UnityEngine;

/// <summary>증강 투사체. 적중 시 콜백을 쏘고 스스로 정리한다.</summary>
public class AugmentProjectile : MonoBehaviour
{
    System.Action<HitInfo> onHit;
    HashSet<Transform> excluded;
    LayerMask targetMask;

    Vector2 velocity;
    float speed;
    float lifeRemain;
    float travelRemain;
    int pierceRemain;
    int hitIndex;

    public void Launch(Vector2 direction, float speed, float lifetime, float maxDistance,
                       int pierce, LayerMask mask, HashSet<Transform> exclude,
                       System.Action<HitInfo> callback)
    {
        this.speed   = speed;
        velocity     = direction.normalized * speed;
        lifeRemain   = lifetime;
        travelRemain = maxDistance;
        pierceRemain = Mathf.Max(1, pierce);
        targetMask   = mask;
        excluded     = exclude;
        onHit        = callback;
        hitIndex     = 0;

        transform.up = direction;
    }

    void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);

        // 사거리와 수명 중 먼저 닿는 쪽에서 소멸
        travelRemain -= speed * Time.deltaTime;
        lifeRemain   -= Time.deltaTime;

        if (travelRemain <= 0f || lifeRemain <= 0f) Despawn();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((targetMask.value & (1 << other.gameObject.layer)) == 0) return;

        // 이미 맞은 대상은 관통 소모 없이 통과시킨다
        if (excluded != null && excluded.Contains(other.transform)) return;

        onHit?.Invoke(new HitInfo
        {
            Target = other.transform,
            Point  = transform.position,
            Index  = hitIndex++
        });

        pierceRemain--;
        if (pierceRemain <= 0) Despawn();
    }

    /// <summary>풀에 반납한다. 콜백 참조를 끊어야 이전 발동의 ctx가 살아남지 않는다.</summary>
    void Despawn()
    {
        onHit = null;
        excluded = null;
        velocity = Vector2.zero;

        gameObject.SetActive(false);
    }
}
