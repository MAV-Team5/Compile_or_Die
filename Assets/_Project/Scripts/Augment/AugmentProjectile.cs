using UnityEngine;

/// <summary>증강 투사체. 적중 시 콜백을 쏘고 스스로 정리한다.</summary>
public class AugmentProjectile : MonoBehaviour
{
    System.Action<HitInfo> onHit;
    LayerMask targetMask;
    Vector2 velocity;
    float lifeRemain;
    float travelRemain;
    int pierceRemain;
    int hitIndex;

    public void Launch(Vector2 direction, float speed, float lifetime, float maxDistance,
                       int pierce, LayerMask mask, System.Action<HitInfo> callback)
    {
        velocity     = direction.normalized * speed;
        lifeRemain   = lifetime;
        travelRemain = maxDistance;
        pierceRemain = Mathf.Max(1, pierce);
        targetMask   = mask;
        onHit        = callback;
        hitIndex     = 0;

        transform.up = direction;
    }

    void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);

        // 사거리와 수명 중 먼저 닿는 쪽에서 소멸
        travelRemain -= velocity.magnitude * Time.deltaTime;
        lifeRemain   -= Time.deltaTime;

        if (travelRemain <= 0f || lifeRemain <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((targetMask.value & (1 << other.gameObject.layer)) == 0) return;

        onHit?.Invoke(new HitInfo
        {
            Target = other.transform,
            Point  = transform.position,
            Index  = hitIndex++
        });

        pierceRemain--;
        if (pierceRemain <= 0) Destroy(gameObject);
    }
}