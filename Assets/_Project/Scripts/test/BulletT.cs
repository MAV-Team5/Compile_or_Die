using UnityEngine;

public class BulletT : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 2f;

    void Start()
    {
        // Rigidbody2D 컴포넌트를 가져와서 속도를 설정합니다.
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            // 여기서 velocity로 수정했습니다! 
            rb.linearVelocity = transform.right * speed;
        }

        // 지정된 시간 뒤에 투사체 삭제        
        Destroy(gameObject, lifeTime);
    }
}