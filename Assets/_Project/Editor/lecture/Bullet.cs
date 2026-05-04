using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float damage;
    public int per;   // -100: 근접무기(무한관통), 0 이상: 원거리(관통력)

    Rigidbody2D rigid;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
    }

    public void Init(float damage, int per, Vector3 dir)
    {
        this.damage = damage;
        this.per    = per;

        if (per >= 0)
            rigid.linearVelocity = dir * 15f;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy")) return;
        if (per == -100) return;   // 근접무기: 무한관통

        per--;

        if (per < 0)
        {
            rigid.linearVelocity = Vector2.zero;
            gameObject.SetActive(false);
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Area")) return;
        if (per == -100) return;   // 근접무기는 해당 없음

        rigid.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);
    }
}
