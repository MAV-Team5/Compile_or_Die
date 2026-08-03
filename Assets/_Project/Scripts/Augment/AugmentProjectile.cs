using UnityEngine;

/// <summary>증강 투사체. 적중 시 콜백을 쏘고 스스로 정리한다.</summary>
public class AugmentProjectile : MonoBehaviour
{
    System.Action<HitInfo> onHit;
    LayerMask targetMask;
    Vector2 velocity;
    float lifeRemain;
    int pierceRemain;
    int hitIndex;

    public void Launch(Vector2 direction, float speed, float lifetime, int pierce,
                       LayerMask mask, System.Action<HitInfo> callback)
    {
        velocity     = direction.normalized * speed;
        lifeRemain   = lifetime;
        pierceRemain = Mathf.Max(1, pierce);
        targetMask   = mask;
        onHit        = callback;
        hitIndex     = 0;

        transform.up = direction;
    }

    void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);

        lifeRemain -= Time.deltaTime;
        if (lifeRemain <= 0f) Destroy(gameObject);
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